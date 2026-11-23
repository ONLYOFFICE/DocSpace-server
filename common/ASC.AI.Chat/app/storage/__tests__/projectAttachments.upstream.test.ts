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

import type { JsonBodyType } from "msw";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { AiServiceHttpError, proxyBaseUrl } from "../httpClient.js";
import {
  copyIntoProject,
  copyManyIntoProject,
  MAX_COPY_BYTES,
  uploadInChunks,
} from "../projectAttachments.js";
import { fileDto, inRequest } from "./docspaceFilesApi.fixtures.js";

// The copy of a portal file into an AI project over a stubbed network: the real httpClient.js,
// requestContext.js, docspaceFilesApi.js and roomSkills.js download run. Routes:
//   GET  api/2.0/files/file/{id}                  FilesController.GetFileInfo (FileDto)
//   GET  api/2.0/files/file/{id}/presigneduri     FilesController.GetPresignedUri (string)
//   GET  api/2.0/files/settings                   SettingsController.GetFilesSettings (chunkUploadSize)
//   POST api/2.0/files/{folderId}/session         UploadController.CreateUploadSessionInFolder
//                                                 (ChunkedUploadSessionDto, Core/ApiModels/ResponseDto/ChunkedUploadSessionDto.cs)
//   POST api/2.0/files/{folderId}/session/{id}    UploadController.UploadSession
//                                                 (UploadSessionResponseDto, Server/ApiModels/ResponseDto/UploadSessionResponseDto.cs)
const upstream = useUpstream();
const FILES = `${proxyBaseUrl}/api/2.0/files`;
const CALLER = { cookie: "asc_auth_key=1", authorization: "Bearer t", "if-none-match": "etag" };

const sessionDto = (overrides: Record<string, unknown> = {}) => ({
  response: {
    id: "4b1f7a6e-0c1d-4f2a-9b3c-5d6e7f8a9b0c",
    path: [6, 12],
    created: "2026-11-23T10:00:00.0000000Z",
    expired: "2026-11-23T22:00:00.0000000Z",
    location: `${FILES}/12/session/4b1f7a6e-0c1d-4f2a-9b3c-5d6e7f8a9b0c`,
    bytesTotal: 6,
    ...overrides,
  },
  count: 1,
  status: 0,
  statusCode: 200,
});
const SESSION = "4b1f7a6e-0c1d-4f2a-9b3c-5d6e7f8a9b0c";

const chunkDto = (overrides: Record<string, unknown> = {}) => ({
  response: {
    id: 77,
    folderId: 12,
    version: 1,
    title: "Report.docx",
    providerKey: null,
    uploaded: true,
    file: { id: 77, title: "Report.docx", folderId: 12 },
    ...overrides,
  },
  count: 1,
  status: 0,
  statusCode: 201,
});

interface Chunk {
  path: string;
  headers: Record<string, string>;
  name: string;
  type: string;
  bytes: number[];
}

/** The project room's upload session; keeps the session request and every chunk. */
function session(
  options: {
    session?: unknown;
    chunk?: unknown;
    chunkUploadSize?: unknown;
    settingsStatus?: number;
  } = {},
): { opened: { headers: Record<string, string>; body: unknown }[]; chunks: Chunk[] } {
  const opened: { headers: Record<string, string>; body: unknown }[] = [];
  const chunks: Chunk[] = [];
  upstream.use(
    http.get(`${FILES}/settings`, () =>
      options.settingsStatus
        ? HttpResponse.json({}, { status: options.settingsStatus })
        : HttpResponse.json({
            response: {
              chunkUploadSize: "chunkUploadSize" in options ? options.chunkUploadSize : 4,
              maxUploadThreadCount: 4,
            },
          }),
    ),
    http.post(`${FILES}/:folderId/session`, async ({ request }) => {
      opened.push({ headers: Object.fromEntries(request.headers), body: await request.json() });
      return HttpResponse.json(options.session ?? sessionDto());
    }),
    http.post(`${FILES}/:folderId/session/:sessionId`, async ({ request }) => {
      const file = (await request.formData()).get("file") as File;
      chunks.push({
        path: new URL(request.url).pathname,
        headers: Object.fromEntries(request.headers),
        name: file.name,
        type: file.type,
        bytes: [...new Uint8Array(await file.arrayBuffer())],
      });
      return HttpResponse.json(options.chunk ?? chunkDto(), { status: 201 });
    }),
  );
  return { opened, chunks };
}

/** The portal's file 40 and its download. */
function portalFile(
  info: JsonBodyType = fileDto({ id: 40, folderId: 5 }),
  download: () => Response = () =>
    HttpResponse.arrayBuffer(new Uint8Array([1, 2, 3, 4, 5, 6]).buffer),
): { downloads: Record<string, string>[] } {
  const downloads: Record<string, string>[] = [];
  upstream.use(
    http.get(`${FILES}/file/40`, () => HttpResponse.json(info)),
    http.get(`${FILES}/file/40/presigneduri`, () =>
      HttpResponse.json({ response: "https://portal.example/storage/files/40.docx?auth=sig" }),
    ),
    http.get(`${proxyBaseUrl}/storage/files/40.docx`, ({ request }) => {
      downloads.push(Object.fromEntries(request.headers));
      return download();
    }),
  );
  return { downloads };
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
  vi.mocked(logger.warn).mockClear();
});

describe("uploadInChunks over the network", () => {
  it("opens a session, sends the chunks in order with the caller's credentials and answers the file", async () => {
    const { opened, chunks } = session();

    const id = await inRequest(
      () =>
        uploadInChunks("12", "Report.docx", new Uint8Array([1, 2, 3, 4, 5, 6]), "application/x"),
      { headers: CALLER },
    );

    expect(id).toBe("77");
    expect(opened).toEqual([
      {
        headers: expect.objectContaining({ ...CALLER, "content-type": "application/json" }),
        body: { fileName: "Report.docx", fileSize: 6, relativePath: "", createNewIfExist: true },
      },
    ]);
    expect(chunks).toEqual([
      {
        path: `/api/2.0/files/12/session/${SESSION}`,
        headers: expect.objectContaining(CALLER),
        name: "Report.docx",
        type: "application/x",
        bytes: [1, 2, 3, 4],
      },
      {
        path: `/api/2.0/files/12/session/${SESSION}`,
        headers: expect.objectContaining(CALLER),
        name: "Report.docx",
        type: "application/x",
        bytes: [5, 6],
      },
    ]);
    expect(chunks[0]?.headers["content-type"]).toMatch(/^multipart\/form-data; boundary=/);
  });

  it("encodes the folder and the session id into the paths", async () => {
    const { chunks } = session({ session: sessionDto({ id: "s/1" }) });
    const paths: string[] = [];
    upstream.use(
      http.post(`${FILES}/*`, async ({ request }) => {
        paths.push(new URL(request.url).pathname);
        if (paths.length === 1) {
          return HttpResponse.json(sessionDto({ id: "s/1" }));
        }
        return HttpResponse.json(chunkDto());
      }),
    );

    await uploadInChunks("a/b", "x.docx", new Uint8Array([1]), "application/x");

    expect(chunks).toEqual([]);
    expect(paths).toEqual(["/api/2.0/files/a%2Fb/session", "/api/2.0/files/a%2Fb/session/s%2F1"]);
  });

  it("sends one empty chunk for an empty file", async () => {
    const { chunks } = session();

    await uploadInChunks("12", "empty.txt", new Uint8Array(0), "text/plain");

    expect(chunks.map((c) => c.bytes)).toEqual([[]]);
  });

  it("sends a file of exactly one chunk in one", async () => {
    const { chunks } = session();

    await uploadInChunks("12", "x", new Uint8Array([1, 2, 3, 4]), "text/plain");

    expect(chunks.map((c) => c.bytes)).toEqual([[1, 2, 3, 4]]);
  });

  // The default of 10 MiB itself is not pinned: a file over it takes half a second to post here.
  it.each([
    ["zero", 0],
    ["negative", -1],
    ["a string", "4"],
    ["null", null],
  ])(
    "falls back to the default chunk when chunkUploadSize is %s",
    async (_case, chunkUploadSize) => {
      const { chunks } = session({ chunkUploadSize });

      await uploadInChunks("12", "x", new Uint8Array([1, 2, 3, 4, 5, 6]), "text/plain");

      expect(chunks.map((c) => c.bytes.length)).toEqual([6]);
    },
  );

  it("falls back to the default chunk when the settings are not an object", async () => {
    const { chunks } = session();
    upstream.use(http.get(`${FILES}/settings`, () => HttpResponse.json({ response: null })));

    await uploadInChunks("12", "x", new Uint8Array([1, 2, 3, 4, 5, 6]), "text/plain");

    expect(chunks.map((c) => c.bytes.length)).toEqual([6]);
  });

  it.each([500, 403])(
    "falls back to the default chunk when the settings cannot be read (%i)",
    async (settingsStatus) => {
      const { chunks } = session({ settingsStatus });

      await uploadInChunks("12", "x", new Uint8Array([1, 2, 3, 4, 5, 6]), "text/plain");

      expect(chunks.map((c) => c.bytes.length)).toEqual([6]);
    },
  );

  it("reads a session answered without the envelope", async () => {
    const { chunks } = session({ session: { id: "bare" } });

    await uploadInChunks("12", "x", new Uint8Array([1]), "text/plain");

    expect(chunks[0]?.path).toBe("/api/2.0/files/12/session/bare");
  });

  it.each([
    ["id is missing", sessionDto({ id: undefined })],
    ["id is null", sessionDto({ id: null })],
    ["id is a number", sessionDto({ id: 5 })],
    ["response is null", { response: null, status: 0 }],
    ["the answer is a list", [sessionDto()]],
  ])("refuses a session whose %s, sending no chunk", async (_case, answer) => {
    const { chunks } = session({ session: answer });

    expect(
      await failure(uploadInChunks("12", "x", new Uint8Array([1]), "text/plain")),
    ).toMatchObject({ message: "DocSpace opened an upload session without an id" });
    expect(chunks).toEqual([]);
  });

  it.each([
    ["the file's string id", chunkDto({ file: { id: "sbox-77" } }), "sbox-77"],
    ["the answer's own id when it carries no file", chunkDto({ file: undefined }), "77"],
    ["the answer's own id when the file is null", chunkDto({ file: null }), "77"],
    ["the id of an answer without the envelope", { file: { id: 78 } }, "78"],
  ])("answers %s", async (_case, chunk, expected) => {
    session({ chunk });

    await expect(uploadInChunks("12", "x", new Uint8Array([1]), "text/plain")).resolves.toBe(
      expected,
    );
  });

  it.each([
    ["the file has no id", chunkDto({ file: { title: "x" } })],
    ["the file's id is empty", chunkDto({ file: { id: "" } })],
    ["neither the file nor the answer has an id", chunkDto({ id: undefined, file: undefined })],
    ["the answer is a list", [chunkDto()]],
    ["response is null", { response: null, status: 0 }],
  ])("refuses the last chunk's answer when %s", async (_case, chunk) => {
    session({ chunk });

    expect(
      await failure(uploadInChunks("12", "x", new Uint8Array([1]), "text/plain")),
    ).toMatchObject({ message: "DocSpace finished an upload session without a file" });
  });

  // DEFECT stor1-10: a refusal of the Files API's upload session is thrown as an AiServiceHttpError carrying the upstream body — expected a DocspaceApiHttpError (ts-ai-style.md, Upstream calls)
  it.each([
    ["the session", `${FILES}/:folderId/session`],
    ["a chunk", `${FILES}/:folderId/session/:sessionId`],
  ])("rejects a refusal of %s with an AiServiceHttpError", async (_case, route) => {
    session();
    upstream.use(
      http.post(route, () => HttpResponse.text("quota", { status: 402, statusText: "Payment" })),
    );

    const error = await failure(uploadInChunks("12", "x", new Uint8Array([1]), "text/plain"));

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ status: 402, statusText: "Payment", body: "quota" });
  });
});

describe("copyIntoProject over the network", () => {
  it("downloads the file without the caller's conditional headers and uploads it into the room", async () => {
    const { downloads } = portalFile();
    const { opened, chunks } = session();

    await expect(inRequest(() => copyIntoProject("12", "40"), { headers: CALLER })).resolves.toBe(
      "77",
    );

    expect(downloads).toEqual([
      expect.objectContaining({ cookie: "asc_auth_key=1", authorization: "Bearer t" }),
    ]);
    expect(downloads[0]?.["if-none-match"]).toBeUndefined();
    expect(opened[0]?.body).toEqual({
      fileName: "Report.docx",
      fileSize: 6,
      relativePath: "",
      createNewIfExist: true,
    });
    expect(chunks.map((c) => [c.type, c.bytes])).toEqual([
      ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", [1, 2, 3, 4]],
      ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", [5, 6]],
    ]);
    expect(logger.info).toHaveBeenCalledWith("copyIntoProject: file 40 -> project 12 as file 77");
  });

  it.each(["0", "012", "-1", "abc", "sbox-1-40", "1.5", ""])(
    "attaches %j where it is without a request",
    async (id) => {
      await expect(copyIntoProject("12", id)).resolves.toBe(id);
      expect(upstream.seen).toHaveLength(0);
    },
  );

  it.each([
    ["it already lies in the project room", fileDto({ id: 40, folderId: 12 })],
    ["its folder id is the project's as a string", fileDto({ id: 40, folderId: "12" })],
    ["it has no title", fileDto({ id: 40, folderId: 5, title: undefined })],
    ["its title is null", fileDto({ id: 40, folderId: 5, title: null })],
    ["its title is a number", fileDto({ id: 40, folderId: 5, title: 7 })],
    ["the Files API describes it as a string", { response: "40", status: 0 }],
    ["the Files API describes it as null", { response: null, status: 0 }],
  ])("attaches the file where it is when %s", async (_case, info) => {
    portalFile(info);

    await expect(copyIntoProject("12", "40")).resolves.toBe("40");
    expect(upstream.seen.map((r) => r.path)).toEqual(["/api/2.0/files/file/40"]);
  });

  it("attaches a file over the limit where it is and says so", async () => {
    portalFile(fileDto({ id: 40, folderId: 5, pureContentLength: MAX_COPY_BYTES + 1 }));

    await expect(copyIntoProject("12", "40")).resolves.toBe("40");
    expect(upstream.seen).toHaveLength(1);
    expect(logger.info).toHaveBeenCalledWith(
      `copyIntoProject: file 40 (${MAX_COPY_BYTES + 1} bytes) is over the limit, attached where it is`,
    );
  });

  it.each([
    ["exactly the limit", MAX_COPY_BYTES],
    ["missing", undefined],
    ["null", null],
    ["a string", "999999999999"],
  ])("copies a file whose pureContentLength is %s", async (_case, pureContentLength) => {
    portalFile(fileDto({ id: 40, folderId: 5, pureContentLength }));
    session();

    await expect(copyIntoProject("12", "40")).resolves.toBe("77");
  });

  // A refusal of the Files API — any status, a 5xx included — is logged as an ordinary refusal.
  it.each([
    ["the file cannot be read (404)", `${FILES}/file/40`, 404],
    ["the file cannot be read (500)", `${FILES}/file/40`, 500],
    ["no download address is given (403)", `${FILES}/file/40/presigneduri`, 403],
    ["the download is refused (503)", `${proxyBaseUrl}/storage/files/40.docx`, 503],
  ])("attaches the file where it is and logs at info when %s", async (_case, route, status) => {
    portalFile();
    session();
    upstream.use(http.get(route, () => HttpResponse.json({}, { status })));

    await expect(copyIntoProject("12", "40")).resolves.toBe("40");
    expect(logger.info).toHaveBeenCalledTimes(1);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("attaches the file where it is and logs at info when the room refuses the upload", async () => {
    portalFile();
    session();
    upstream.use(
      http.post(`${FILES}/:folderId/session`, () => HttpResponse.json({}, { status: 400 })),
    );

    await expect(copyIntoProject("12", "40")).resolves.toBe("40");
    expect(logger.info).toHaveBeenCalledTimes(1);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it.each([
    ["the Files API drops the connection", `${FILES}/file/40`],
    ["the download drops the connection", `${proxyBaseUrl}/storage/files/40.docx`],
  ])("attaches the file where it is and warns when %s", async (_case, route) => {
    portalFile();
    session();
    upstream.use(http.get(route, () => HttpResponse.error()));

    await expect(copyIntoProject("12", "40")).resolves.toBe("40");
    expect(logger.warn).toHaveBeenCalledWith(
      expect.stringMatching(
        /^copyIntoProject: file 40 not copied into project 12, attached where it is: /,
      ),
    );
  });

  it("attaches the file where it is and warns when the session comes without an id", async () => {
    portalFile();
    session({ session: sessionDto({ id: null }) });

    await expect(copyIntoProject("12", "40")).resolves.toBe("40");
    expect(logger.warn).toHaveBeenCalledWith(
      "copyIntoProject: file 40 not copied into project 12, attached where it is: DocSpace opened an upload session without an id",
    );
  });
});

describe("copyManyIntoProject over the network", () => {
  it("answers nothing for no files", async () => {
    await expect(copyManyIntoProject("12", [])).resolves.toEqual([]);
    expect(upstream.seen).toHaveLength(0);
  });

  it("keeps the order of the ids and copies a repeated one once", async () => {
    portalFile();
    const { opened } = session();

    await expect(copyManyIntoProject("12", ["40", "x", "40"])).resolves.toEqual(["77", "x", "77"]);
    expect(opened).toHaveLength(1);
  });
});
