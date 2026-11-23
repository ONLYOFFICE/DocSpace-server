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
import { mediaTypeOf, uploadFile } from "../docspaceFileTransfer.js";
import { AiServiceHttpError, proxyBaseUrl } from "../httpClient.js";
import { fileDto, fileDtoBody, inRequest } from "./docspaceFilesApi.fixtures.js";

// The upload of the document agent's files over a stubbed network, with the real httpClient.js
// and requestContext.js. The route is FilesController.InsertFile, `POST api/2.0/files/{folderId}/insert`,
// which answers a FileDto<int>.
const upstream = useUpstream();
const INSERT = `${proxyBaseUrl}/api/2.0/files/:folderId/insert`;

interface Sent {
  path: string;
  headers: Record<string, string>;
  fields: Record<string, string>;
  file: { name: string; type: string; bytes: number[] } | undefined;
  contentType: string | null;
}

/** Answers the insert with `answer` and keeps what the multipart body carried. */
function insert(answer: () => Response): Sent[] {
  const sent: Sent[] = [];
  upstream.use(
    http.post(INSERT, async ({ request }) => {
      const form = await request.formData();
      const fields: Record<string, string> = {};
      let file: Sent["file"];
      for (const [name, value] of form.entries()) {
        if (typeof value === "string") {
          fields[name] = value;
        } else {
          file = {
            name: value.name,
            type: value.type,
            bytes: [...new Uint8Array(await value.arrayBuffer())],
          };
        }
      }
      // Read here, not from `seen`, which records a multipart body after the call may have returned.
      sent.push({
        path: new URL(request.url).pathname,
        headers: Object.fromEntries(request.headers),
        fields,
        file,
        contentType: request.headers.get("content-type"),
      });
      return answer();
    }),
  );
  return sent;
}

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

beforeEach(() => {
  vi.mocked(logger.info).mockClear();
});

describe("mediaTypeOf", () => {
  it.each([
    ["Report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
    ["Sheet.XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
    ["Deck.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"],
    ["a.pdf", "application/pdf"],
    ["a.png", "image/png"],
    ["a.jpg", "image/jpeg"],
    ["a.jpeg", "image/jpeg"],
    ["a.txt", "text/plain"],
    ["a.csv", "text/csv"],
    ["a.md", "text/markdown"],
    ["a.json", "application/json"],
    ["archive.tar.gz", "application/octet-stream"],
    ["README", "application/octet-stream"],
    ["Report.", "application/octet-stream"],
    ["", "application/octet-stream"],
    ["a.toString", "application/octet-stream"],
  ])("answers %j as %s", (title, type) => {
    expect(mediaTypeOf(title)).toBe(type);
  });

  // DEFECT stor1-7: the table is a plain object, so an extension that names a member of Object.prototype answers that member, not a media type — expected application/octet-stream (the function's doc comment, docspaceFileTransfer.ts:65)
  it("answers a member of Object.prototype for an extension that names one", () => {
    expect(mediaTypeOf("notes.constructor")).toBe(Object);
  });
});

describe("uploadFile", () => {
  it("inserts the file as multipart with the caller's credentials and answers the new entry", async () => {
    const sent = insert(() => HttpResponse.json(fileDto({ id: 41, title: "Report (1).docx" })));

    const file = await inRequest(
      () => uploadFile("30", "Report.docx", new Uint8Array([1, 2, 3]), "application/docx"),
      { headers: { cookie: "asc_auth_key=1", authorization: "Bearer t", "if-match": "x" } },
    );

    expect(file).toStrictEqual({ id: "41", title: "Report (1).docx" });
    expect(sent).toEqual([
      {
        path: "/api/2.0/files/30/insert",
        headers: expect.objectContaining({
          cookie: "asc_auth_key=1",
          authorization: "Bearer t",
          "if-match": "x",
        }),
        fields: { title: "Report.docx", createNewIfExist: "true" },
        file: { name: "Report.docx", type: "application/docx", bytes: [1, 2, 3] },
        contentType: expect.stringMatching(/^multipart\/form-data; boundary=/),
      },
    ]);
    expect(logger.info).toHaveBeenCalledWith(
      "uploadFile: Report.docx -> folder 30 as entry 41 (3 bytes)",
    );
  });

  it("encodes the folder id into the path", async () => {
    const sent = insert(() => HttpResponse.json(fileDto()));

    await uploadFile("a/b", "x.txt", new Uint8Array([1]), "text/plain");

    expect(sent.map((s) => s.path)).toEqual(["/api/2.0/files/a%2Fb/insert"]);
  });

  it.each([
    ["the file without the envelope", fileDtoBody(), { id: "40", title: "Report.docx" }],
    ["a string id", fileDto({ id: "sbox-1-40" }), { id: "sbox-1-40", title: "Report.docx" }],
    ["no title", fileDto({ title: undefined }), { id: "40", title: "Asked.docx" }],
    ["a null title", fileDto({ title: null }), { id: "40", title: "Asked.docx" }],
    ["a numeric title", fileDto({ title: 5 }), { id: "40", title: "Asked.docx" }],
    ["an empty title", fileDto({ title: "" }), { id: "40", title: "" }],
  ])("answers %s", async (_case, body, expected) => {
    insert(() => HttpResponse.json(body));

    await expect(
      uploadFile("30", "Asked.docx", new Uint8Array([1]), "application/docx"),
    ).resolves.toStrictEqual(expected);
  });

  it.each([
    ["response is null", { response: null }],
    ["response is a list", { response: [fileDtoBody()] }],
    ["response is a string", { response: "40" }],
    ["the body is a list", [fileDtoBody()]],
    ["the body is null", null],
  ])("refuses an answer when %s", async (_case, body) => {
    insert(() => HttpResponse.json(body));

    const error = await failure(uploadFile("30", "a.txt", new Uint8Array([1]), "text/plain"));

    expect(error).not.toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ message: "DocSpace insert returned an unexpected payload" });
  });

  it.each([
    ["id is missing", fileDto({ id: undefined })],
    ["id is null", fileDto({ id: null })],
    ["id is empty", fileDto({ id: "" })],
    ["id is a boolean", fileDto({ id: true })],
    ["the envelope holds no file", { status: 0 }],
  ])("refuses a file whose %s", async (_case, body) => {
    insert(() => HttpResponse.json(body));

    expect(
      await failure(uploadFile("30", "a.txt", new Uint8Array([1]), "text/plain")),
    ).toMatchObject({ message: "DocSpace insert returned a file without an id" });
  });

  it("refuses an answer that is not JSON", async () => {
    insert(() => HttpResponse.text("ok"));

    expect(
      await failure(uploadFile("30", "a.txt", new Uint8Array([1]), "text/plain")),
    ).toBeInstanceOf(SyntaxError);
  });

  // DEFECT stor1-8: a refusal of the Files API is thrown as an AiServiceHttpError carrying the upstream body — expected a DocspaceApiHttpError, the class of a Files API error (ts-ai-style.md, Upstream calls)
  it.each([400, 403, 413, 500])(
    "rejects a %i with an AiServiceHttpError that carries the body",
    async (status) => {
      insert(() => HttpResponse.text("refused", { status, statusText: "Reason" }));

      const error = await failure(uploadFile("30", "a.txt", new Uint8Array([1]), "text/plain"));

      expect(error).toBeInstanceOf(AiServiceHttpError);
      expect(error).toMatchObject({
        status,
        statusText: "Reason",
        body: "refused",
        url: `${proxyBaseUrl}/api/2.0/files/30/insert`,
      });
    },
  );

  it("rejects a dropped connection", async () => {
    upstream.use(http.post(INSERT, () => HttpResponse.error()));

    expect(
      await failure(uploadFile("30", "a.txt", new Uint8Array([1]), "text/plain")),
    ).toBeInstanceOf(TypeError);
  });
});
