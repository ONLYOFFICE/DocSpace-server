// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { beforeEach, describe, expect, it, vi } from "vitest";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { AiServiceHttpError, aiServiceBaseUrl } from "../httpClient.js";
import { uploadThreadArtifact } from "../threadFiles.js";
import { fileDto, fileDtoBody, inRequest } from "../../__tests__/fixtures/filesApi.js";

// A conversation's artifacts over a stubbed network, with the real httpClient.js and
// requestContext.js. The route is ThreadArtifactsController.UploadAsync,
// `POST internal/ai/threads/{id}/artifacts`, which answers a FileDto<int>.
const upstream = useUpstream();
const ARTIFACTS = `${aiServiceBaseUrl}/internal/ai/threads/:id/artifacts`;

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

const upload = () => uploadThreadArtifact("t-1", "image.png", new Uint8Array([7, 8]), "image/png");

beforeEach(() => {
  vi.mocked(logger.info).mockClear();
});

describe("uploadThreadArtifact over the network", () => {
  it("posts the multipart file with the caller's credentials and answers the stored file", async () => {
    const parts: unknown[] = [];
    upstream.use(
      http.post(ARTIFACTS, async ({ request }) => {
        const form = await request.formData();
        const file = form.get("file") as File;
        parts.push({
          title: form.get("title"),
          name: file.name,
          type: file.type,
          bytes: [...new Uint8Array(await file.arrayBuffer())],
          contentType: request.headers.get("content-type"),
          path: new URL(request.url).pathname,
          cookie: request.headers.get("cookie"),
          authorization: request.headers.get("authorization"),
        });
        return HttpResponse.json(fileDto({ id: 55, title: "image (1).png" }));
      }),
    );

    const result = await inRequest(upload, {
      headers: { cookie: "asc_auth_key=1", authorization: "Bearer t" },
    });

    expect(result).toStrictEqual({ id: "55", title: "image (1).png" });
    expect(parts).toEqual([
      {
        title: "image.png",
        name: "image.png",
        type: "image/png",
        bytes: [7, 8],
        contentType: expect.stringMatching(/^multipart\/form-data; boundary=/),
        path: "/internal/ai/threads/t-1/artifacts",
        cookie: "asc_auth_key=1",
        authorization: "Bearer t",
      },
    ]);
    expect(logger.info).toHaveBeenCalledWith(
      "uploadThreadArtifact: image.png -> thread t-1 as file 55 (2 bytes)",
    );
  });

  it("encodes the thread id into the path", async () => {
    // Read in the handler: `seen` records a multipart body after the call may have returned.
    const paths: string[] = [];
    upstream.use(
      http.post(`${aiServiceBaseUrl}/internal/ai/threads/*`, ({ request }) => {
        paths.push(new URL(request.url).pathname);
        return HttpResponse.json(fileDto());
      }),
    );

    await uploadThreadArtifact("a/b", "x.png", new Uint8Array([1]), "image/png");

    expect(paths).toEqual(["/internal/ai/threads/a%2Fb/artifacts"]);
  });

  it.each([
    ["the file without the envelope", fileDtoBody(), { id: "40", title: "Report.docx" }],
    ["a string id", fileDto({ id: "sbox-40" }), { id: "sbox-40", title: "Report.docx" }],
    ["no title", fileDto({ title: undefined }), { id: "40", title: "image.png" }],
    ["a null title", fileDto({ title: null }), { id: "40", title: "image.png" }],
    ["a numeric title", fileDto({ title: 9 }), { id: "40", title: "image.png" }],
    ["an empty title", fileDto({ title: "" }), { id: "40", title: "" }],
  ])("answers %s", async (_case, body, expected) => {
    upstream.use(http.post(ARTIFACTS, () => HttpResponse.json(body)));

    await expect(upload()).resolves.toStrictEqual(expected);
  });

  it.each([
    ["id is missing", fileDto({ id: undefined })],
    ["id is null", fileDto({ id: null })],
    ["id is empty", fileDto({ id: "" })],
    ["id is a boolean", fileDto({ id: true })],
    ["response is null", { response: null, status: 0 }],
    ["response is a list", { response: [fileDtoBody()], status: 0 }],
    ["response is a string", { response: "40", status: 0 }],
    ["the body is a list", [fileDtoBody()]],
    ["the body is null", null],
  ])("refuses an answer when %s", async (_case, body) => {
    upstream.use(http.post(ARTIFACTS, () => HttpResponse.json(body)));

    const error = await failure(upload());

    expect(error).not.toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ message: "the AI service returned an artifact without an id" });
  });

  it("refuses an answer that is not JSON", async () => {
    upstream.use(http.post(ARTIFACTS, () => HttpResponse.text("ok")));

    expect(await failure(upload())).toBeInstanceOf(SyntaxError);
  });

  it.each([400, 403, 404])(
    "answers undefined and logs it for a thread that takes no artifacts (%i)",
    async (status) => {
      upstream.use(http.post(ARTIFACTS, () => HttpResponse.json({}, { status })));

      await expect(upload()).resolves.toBeUndefined();
      expect(logger.info).toHaveBeenCalledWith(
        `uploadThreadArtifact: thread t-1 takes no artifacts (${status})`,
      );
    },
  );

  it.each([401, 413, 500, 503])(
    "rejects a %i with an AiServiceHttpError that carries the body",
    async (status) => {
      upstream.use(
        http.post(ARTIFACTS, () => HttpResponse.text("too big", { status, statusText: "Reason" })),
      );

      const error = await failure(upload());

      expect(error).toBeInstanceOf(AiServiceHttpError);
      expect(error).toMatchObject({
        status,
        statusText: "Reason",
        body: "too big",
        url: `${aiServiceBaseUrl}/internal/ai/threads/t-1/artifacts`,
      });
    },
  );

  it("rejects a dropped connection", async () => {
    upstream.use(http.post(ARTIFACTS, () => HttpResponse.error()));

    expect(await failure(upload())).toBeInstanceOf(TypeError);
  });
});
