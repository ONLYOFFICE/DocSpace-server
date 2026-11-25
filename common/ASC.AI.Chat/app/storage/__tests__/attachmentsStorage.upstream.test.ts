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

import { UpstreamShapeError } from "../upstreamShape.js";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Attachment } from "@onlyoffice/ai-chat/core";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { setAnalyzeEntryIds } from "../../requestContext.js";
import { HttpAttachmentsStorage } from "../attachmentsStorage.js";
import { AiServiceHttpError, aiServiceBaseUrl, proxyBaseUrl } from "../httpClient.js";
import { fileDto, folderDto, inRequest } from "../../__tests__/fixtures/filesApi.js";

// The attachment storage over a stubbed network, with the real httpClient.js, requestContext.js,
// docspaceFilesApi.js, threadFiles.js, projectAttachments.js and the download of roomSkills.js.
// Routes of the AI service: AttachmentsStorageController (products/ASC.AI/Server/Api/
// AttachmentsStorageController.cs) under `internal/ai/attachments`, and
// ThreadArtifactsController `POST internal/ai/threads/{id}/artifacts`. Routes of the Files API:
// `GET files/folder/{id}`, `GET files/@my`, `POST files/{folderId}/insert` and the copy of
// projectAttachments.ts.
const upstream = useUpstream();
const AI = `${aiServiceBaseUrl}/internal/ai`;
const FILES = `${proxyBaseUrl}/api/2.0/files`;
const CALLER = { cookie: "asc_auth_key=1", authorization: "Bearer t", "if-none-match": "etag" };
const ATTACHMENT_ID = "5d0e7a3c-8b1f-4c2a-9e6d-0a1b2c3d4e5f";
const NOW = new Date(2026, 10, 23, 14, 5, 9);
const PNG = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const PNG_B64 = Buffer.from(PNG).toString("base64");

/**
 * `AttachmentDto` (products/ASC.AI/Server/Models/ResponseDto/AttachmentDto.cs) of a document,
 * as the AI service serves it: `type` is the DocSpace `FileType` (Document = 7).
 */
function attachmentDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    id: ATTACHMENT_ID,
    kind: "file",
    type: 7,
    title: "Report.docx",
    content: null,
    dataUrl: null,
    entryId: "40",
    createdAt: 1763900000000,
    canAnalyze: false,
    ...overrides,
  };
}

const fileAttachment = (overrides: Partial<Attachment> = {}): Attachment => ({
  id: ATTACHMENT_ID,
  kind: "file",
  title: "Report.docx",
  createdAt: 1763900000000,
  path: "40/Report.docx",
  type: 65,
  canAnalyze: false,
  ...overrides,
});

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

const logged = (level: "info" | "warn" | "error" | "debug"): string[] =>
  vi.mocked(logger[level]).mock.calls.map((call) => String(call[0]));

beforeEach(() => {
  for (const level of ["info", "warn", "error", "debug"] as const) {
    vi.mocked(logger[level]).mockClear();
  }
  vi.useFakeTimers({ toFake: ["Date"] });
  vi.setSystemTime(NOW);
});

afterEach(() => {
  vi.useRealTimers();
});

describe("readById", () => {
  const read = async (response: unknown): Promise<Attachment | null> => {
    upstream.use(
      http.get(`${AI}/attachments/:id`, () => HttpResponse.json({ response, status: 0 })),
    );
    return new HttpAttachmentsStorage().readById(ATTACHMENT_ID);
  };

  it("maps a document's AttachmentDto, the type taken from its title", async () => {
    await expect(read(attachmentDto())).resolves.toStrictEqual(fileAttachment());
    expect(upstream.seen[0]).toMatchObject({
      method: "GET",
      path: `/internal/ai/attachments/${ATTACHMENT_ID}`,
    });
    expect(logged("info")).toEqual([
      `HttpAttachmentsStorage.readById(${ATTACHMENT_ID}) -> kind=file title=Report.docx path=40/Report.docx hasDataUrl=false`,
    ]);
  });

  it("encodes the id into the path", async () => {
    upstream.use(http.get(`${AI}/attachments/*`, () => HttpResponse.json(attachmentDto())));

    await new HttpAttachmentsStorage().readById("a/b");

    expect(upstream.seen[0]?.path).toBe("/internal/ai/attachments/a%2Fb");
  });

  const { path: _p, ...noPath } = fileAttachment();
  const { type: _t, ...noType } = fileAttachment();
  const { canAnalyze: _c, ...noAnalyze } = fileAttachment();
  it.each([
    ["kind is Image in any case", { kind: "IMAGE" }, fileAttachment({ kind: "image" })],
    ["kind is another word", { kind: "folder" }, fileAttachment()],
    ["kind is empty", { kind: "" }, fileAttachment()],
    ["title is empty", { title: "" }, { ...noType, title: "", path: "40" }],
    [
      "title has an unknown extension",
      { title: "Movie.mp4" },
      { ...noType, title: "Movie.mp4", path: "40/Movie.mp4" },
    ],
    [
      "createdAt is missing",
      { createdAt: undefined },
      fileAttachment({ createdAt: NOW.getTime() }),
    ],
    [
      "createdAt is a string",
      { createdAt: "1763900000000" },
      fileAttachment({ createdAt: NOW.getTime() }),
    ],
    ["content is text", { content: "Hello" }, fileAttachment({ content: "Hello" })],
    ["content is a number", { content: 5 }, fileAttachment()],
    ["content is empty", { content: "" }, fileAttachment({ content: "" })],
    [
      "dataUrl is a data URL",
      { dataUrl: `data:image/png;base64,${PNG_B64}` },
      fileAttachment({ base64: `data:image/png;base64,${PNG_B64}` }),
    ],
    [
      "dataUrl is null and base64 is given",
      { base64: "data:x" },
      fileAttachment({ base64: "data:x" }),
    ],
    [
      "dataUrl wins over base64",
      { dataUrl: "data:a", base64: "data:b" },
      fileAttachment({ base64: "data:a" }),
    ],
    ["dataUrl is a number", { dataUrl: 5 }, fileAttachment()],
    ["entryId is missing", { entryId: undefined }, noPath],
    ["entryId is null", { entryId: null }, noPath],
    ["entryId is a number", { entryId: 40 }, noPath],
    ["entryId is empty", { entryId: "" }, fileAttachment({ path: "/Report.docx" })],
    [
      "entryId is a third-party id",
      { entryId: "sbox-1-a b" },
      fileAttachment({ path: "sbox-1-a b/Report.docx" }),
    ],
    ["type is a FileType that disagrees with the title", { type: 5 }, fileAttachment()],
    // source, and base64 in the rows above, are fields dtoToAttachment reads and AttachmentDto
    // does not declare (contract.dotnet.test.ts): the AI service does not send them.
    ["source is user", { source: "user" }, fileAttachment({ source: "user" })],
    ["source is tool", { source: "tool" }, fileAttachment({ source: "tool" })],
    ["source is another word", { source: "agent" }, fileAttachment()],
    ["canAnalyze is true", { canAnalyze: true }, fileAttachment({ canAnalyze: true })],
    ["canAnalyze is missing", { canAnalyze: undefined }, noAnalyze],
    ["canAnalyze is null", { canAnalyze: null }, noAnalyze],
    ["canAnalyze is a string", { canAnalyze: "true" }, noAnalyze],
  ])("maps the DTO when %s", async (_case, overrides, expected) => {
    await expect(read(attachmentDto(overrides))).resolves.toStrictEqual(expected);
  });

  it.each([
    ["id is missing", attachmentDto({ id: undefined })],
    ["id is null", attachmentDto({ id: null })],
    ["id is a number", attachmentDto({ id: 5 })],
    ["title is missing", attachmentDto({ title: undefined })],
    ["title is null", attachmentDto({ title: null })],
    ["kind is missing", attachmentDto({ kind: undefined })],
    ["kind is a number", attachmentDto({ kind: 1 })],
    ["the answer is a list", [attachmentDto()]],
    ["the answer is null", null],
  ])("rejects the answer as one of an unknown shape when %s", async (_case, response) => {
    await expect(read(response)).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
      message: "GET attachments/{id}: the answer has an unexpected shape",
    });
    expect(logged("warn")).toEqual([]);
  });

  it("answers null and warns for a 404", async () => {
    upstream.use(http.get(`${AI}/attachments/:id`, () => HttpResponse.json({}, { status: 404 })));

    await expect(new HttpAttachmentsStorage().readById(ATTACHMENT_ID)).resolves.toBeNull();
    expect(logged("warn")).toEqual([
      `HttpAttachmentsStorage.readById(${ATTACHMENT_ID}): 404 — DANGLING REF, the chat cannot hydrate this attachment`,
    ]);
  });

  it.each([400, 403, 500])("rejects a %i with an AiServiceHttpError", async (status) => {
    upstream.use(http.get(`${AI}/attachments/:id`, () => HttpResponse.json({}, { status })));

    const error = await failure(new HttpAttachmentsStorage().readById(ATTACHMENT_ID));

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ status });
  });

  it("rejects a dropped connection", async () => {
    upstream.use(http.get(`${AI}/attachments/:id`, () => HttpResponse.error()));

    expect(await failure(new HttpAttachmentsStorage().readById(ATTACHMENT_ID))).toBeInstanceOf(
      TypeError,
    );
  });
});

describe("an image attachment", () => {
  const imageDto = (dataUrl: unknown) =>
    attachmentDto({ kind: "image", title: "Pic.png", entryId: "41", dataUrl });
  const readImage = async (dataUrl: unknown, headers: Record<string, string> = CALLER) => {
    upstream.use(http.get(`${AI}/attachments/:id`, () => HttpResponse.json(imageDto(dataUrl))));
    return inRequest(() => new HttpAttachmentsStorage().readById(ATTACHMENT_ID), { headers });
  };
  /** The portal's storage handler; keeps the headers of every download. */
  const storage = (answer: () => Response, origin = proxyBaseUrl): Record<string, string>[] => {
    const seen: Record<string, string>[] = [];
    upstream.use(
      http.get(`${origin}/storage/files/41/pic.png`, ({ request }) => {
        seen.push(Object.fromEntries(request.headers));
        return answer();
      }),
    );
    return seen;
  };
  const bytes =
    (b: number[], type = "application/octet-stream") =>
    () =>
      HttpResponse.arrayBuffer(new Uint8Array(b).buffer, { headers: { "content-type": type } });

  it("is inlined from the portal with the caller's session, without the conditional headers", async () => {
    const downloads = storage(bytes(PNG));

    const read = await readImage("/storage/files/41/pic.png?auth=sig");

    expect(read?.base64).toBe(`data:image/png;base64,${PNG_B64}`);
    expect(downloads).toEqual([
      expect.objectContaining({ cookie: "asc_auth_key=1", authorization: "Bearer t" }),
    ]);
    expect(downloads[0]?.["if-none-match"]).toBeUndefined();
    expect(logged("info")).toEqual([
      `HttpAttachmentsStorage.readById(${ATTACHMENT_ID}) -> kind=image title=Pic.png path=41/Pic.png hasDataUrl=true`,
    ]);
  });

  it("is inlined from an absolute address on the portal's own origin with the caller's session", async () => {
    const downloads = storage(bytes(PNG));

    await readImage(`${proxyBaseUrl}/storage/files/41/pic.png?auth=sig`);

    expect(downloads[0]?.["cookie"]).toBe("asc_auth_key=1");
  });

  it("is inlined from another host without the caller's cookie or token", async () => {
    const downloads = storage(bytes(PNG), "https://bucket.s3.example");

    const read = await readImage(
      "https://bucket.s3.example/storage/files/41/pic.png?X-Amz-Signature=s",
    );

    expect(read?.base64).toBe(`data:image/png;base64,${PNG_B64}`);
    expect(downloads[0]?.["cookie"]).toBeUndefined();
    expect(downloads[0]?.["authorization"]).toBeUndefined();
  });

  it.each([
    ["JPEG bytes", [0xff, 0xd8, 0xff, 0xe0], "application/octet-stream", "image/jpeg"],
    ["GIF bytes", [0x47, 0x49, 0x46, 0x38, 0x39, 0x61], "application/octet-stream", "image/gif"],
    [
      "WebP bytes",
      [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50],
      "application/octet-stream",
      "image/webp",
    ],
    [
      "a RIFF that is not WebP",
      [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x41, 0x56, 0x45],
      "image/x-riff",
      "image/x-riff",
    ],
    [
      "a PNG signature shorter than 8 bytes",
      [0x89, 0x50, 0x4e, 0x47],
      "image/svg+xml",
      "image/svg+xml",
    ],
    ["unknown bytes with an image Content-Type", [1, 2, 3], "image/svg+xml", "image/svg+xml"],
    ["unknown bytes with another Content-Type", [1, 2, 3], "text/html", "image/png"],
    ["no bytes", [], "application/octet-stream", "image/png"],
  ])("is sniffed from %s", async (_case, body, contentType, mime) => {
    storage(bytes(body, contentType));

    const read = await readImage("/storage/files/41/pic.png");

    expect(read?.base64).toBe(`data:${mime};base64,${Buffer.from(body).toString("base64")}`);
  });

  it("keeps its address and warns, without the signature, when the download is refused", async () => {
    storage(() => HttpResponse.json({}, { status: 403, statusText: "Forbidden" }));

    const read = await readImage("/storage/files/41/pic.png?auth=sig");

    expect(read?.base64).toBe("/storage/files/41/pic.png?auth=sig");
    expect(logged("warn")).toEqual([
      `fetchImageAsDataUrl: ${proxyBaseUrl}/storage/files/41/pic.png → 403 Forbidden`,
    ]);
  });

  it("keeps its address and logs an error, without the signature, when the download fails", async () => {
    upstream.use(http.get(`${proxyBaseUrl}/storage/files/41/pic.png`, () => HttpResponse.error()));

    const read = await readImage("/storage/files/41/pic.png?auth=sig");

    expect(read?.base64).toBe("/storage/files/41/pic.png?auth=sig");
    expect(logged("error")).toEqual([
      expect.stringMatching(
        new RegExp(`^fetchImageAsDataUrl: ${proxyBaseUrl}/storage/files/41/pic\\.png failed: `),
      ),
    ]);
    expect(logged("error")[0]).not.toContain("sig");
  });

  it("keeps an address that cannot be parsed and logs an error", async () => {
    const read = await readImage("http://[bad");

    expect(read?.base64).toBe("http://[bad");
    expect(logged("error")).toEqual([
      expect.stringMatching(/^fetchImageAsDataUrl: the image failed: /),
    ]);
    expect(upstream.seen).toHaveLength(1);
  });

  it.each([
    ["a data URL", "data:image/png;base64,AA=="],
    ["an empty address", ""],
    ["no address", null],
  ])("is not downloaded when it carries %s", async (_case, dataUrl) => {
    await readImage(dataUrl);

    expect(upstream.seen).toHaveLength(1);
  });

  it("of kind file is not downloaded", async () => {
    upstream.use(
      http.get(`${AI}/attachments/:id`, () =>
        HttpResponse.json(attachmentDto({ dataUrl: "/storage/files/41/pic.png" })),
      ),
    );

    const read = await new HttpAttachmentsStorage().readById(ATTACHMENT_ID);

    expect(read?.base64).toBe("/storage/files/41/pic.png");
    expect(upstream.seen).toHaveLength(1);
  });
});

describe("readManyByIds and findMissingIds", () => {
  const readMany = (response: unknown) =>
    upstream.use(
      http.post(`${AI}/attachments/read`, () => HttpResponse.json({ response, status: 0 })),
    );

  it("reads the records in the order asked, null for those the backend does not return", async () => {
    readMany([
      attachmentDto({ id: "b", title: "B.xlsx" }),
      attachmentDto({ id: "a" }),
      { id: "broken" },
    ]);

    const read = await new HttpAttachmentsStorage().readManyByIds(["a", "missing", "b"]);

    expect(read).toStrictEqual([
      fileAttachment({ id: "a" }),
      null,
      fileAttachment({ id: "b", title: "B.xlsx", path: "40/B.xlsx", type: 257 }),
    ]);
    expect(upstream.seen[0]).toMatchObject({
      method: "POST",
      path: "/internal/ai/attachments/read",
      body: { ids: ["a", "missing", "b"] },
    });
    expect(logged("warn")).toEqual([
      "HttpAttachmentsStorage.readManyByIds: 1/3 DANGLING REF(s) not returned by the backend: missing",
    ]);
  });

  it("asks nothing for no ids", async () => {
    await expect(new HttpAttachmentsStorage().readManyByIds([])).resolves.toEqual([]);
    await expect(new HttpAttachmentsStorage().findMissingIds([])).resolves.toEqual([]);
    expect(upstream.seen).toHaveLength(0);
  });

  it("names the ids the backend does not hold", async () => {
    readMany([attachmentDto({ id: "a" })]);

    await expect(new HttpAttachmentsStorage().findMissingIds(["a", "b", "a"])).resolves.toEqual([
      "b",
    ]);
  });

  it.each([
    ["an object", attachmentDto()],
    ["null", null],
  ])("rejects the answer as one of an unknown shape when it is %s", async (_case, response) => {
    readMany(response);

    const shape = { name: "UpstreamShapeError", status: 502 };
    await expect(new HttpAttachmentsStorage().readManyByIds([ATTACHMENT_ID])).rejects.toMatchObject(
      shape,
    );
    await expect(
      new HttpAttachmentsStorage().findMissingIds([ATTACHMENT_ID]),
    ).rejects.toMatchObject(shape);
  });

  it.each([404, 500])("rejects a %i", async (status) => {
    upstream.use(http.post(`${AI}/attachments/read`, () => HttpResponse.json({}, { status })));

    expect(await failure(new HttpAttachmentsStorage().readManyByIds(["a"]))).toMatchObject({
      status,
    });
  });
});

describe("update, delete and the cascades", () => {
  it("binds the records to a message", async () => {
    upstream.use(http.put(`${AI}/attachments`, () => new HttpResponse(null, { status: 204 })));

    await new HttpAttachmentsStorage().update("a", { messageId: "m1", threadId: "t1" });
    await new HttpAttachmentsStorage().updateManyByIds(["a", "b"], { messageId: "m2" });

    expect(upstream.seen.map((r) => [r.method, r.path, r.body])).toEqual([
      ["PUT", "/internal/ai/attachments", { ids: ["a"], messageId: "m1" }],
      ["PUT", "/internal/ai/attachments", { ids: ["a", "b"], messageId: "m2" }],
    ]);
  });

  it("skips a patch without a message and no ids at all", async () => {
    await new HttpAttachmentsStorage().update("a", { title: "x" });
    await new HttpAttachmentsStorage().updateManyByIds([], { messageId: "m" });

    expect(upstream.seen).toHaveLength(0);
    expect(logged("debug")).toEqual([
      "HttpAttachmentsStorage.updateManyByIds skipped: no messageId in patch; count=1",
    ]);
  });

  it("passes a refused binding on", async () => {
    upstream.use(http.put(`${AI}/attachments`, () => HttpResponse.json({}, { status: 404 })));

    expect(
      await failure(new HttpAttachmentsStorage().update("a", { messageId: "m" })),
    ).toMatchObject({
      status: 404,
    });
  });

  it("deletes one record under its encoded id and takes a 404 as deleted", async () => {
    upstream.use(
      http.delete(`${AI}/attachments/*`, ({ request }) =>
        request.url.endsWith("gone")
          ? HttpResponse.json({}, { status: 404 })
          : new HttpResponse(null, { status: 204 }),
      ),
    );

    await new HttpAttachmentsStorage().delete("a/b");
    await new HttpAttachmentsStorage().delete("gone");

    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/attachments/a%2Fb",
      "/internal/ai/attachments/gone",
    ]);
  });

  it.each([403, 500])("passes a %i of a delete on", async (status) => {
    upstream.use(http.delete(`${AI}/attachments/:id`, () => HttpResponse.json({}, { status })));

    expect(await failure(new HttpAttachmentsStorage().delete("a"))).toMatchObject({ status });
  });

  it("deletes many records with their ids in the body, and nothing for none", async () => {
    upstream.use(http.delete(`${AI}/attachments`, () => new HttpResponse(null, { status: 204 })));

    await new HttpAttachmentsStorage().deleteMany([]);
    await new HttpAttachmentsStorage().deleteMany(["a", "b"]);

    expect(upstream.seen.map((r) => [r.method, r.path, r.body])).toEqual([
      ["DELETE", "/internal/ai/attachments", { ids: ["a", "b"] }],
    ]);
  });

  it("passes a refused delete of many on", async () => {
    upstream.use(http.delete(`${AI}/attachments`, () => HttpResponse.json({}, { status: 404 })));

    expect(await failure(new HttpAttachmentsStorage().deleteMany(["a"]))).toMatchObject({
      status: 404,
    });
  });

  it("leaves the cascades of a message and a thread to the backend", async () => {
    await new HttpAttachmentsStorage().deleteByMessage("m1");
    await new HttpAttachmentsStorage().deleteByThread("t1");

    expect(upstream.seen).toHaveLength(0);
    expect(logged("debug")).toEqual([
      "HttpAttachmentsStorage.deleteByMessage is a no-op (cascade is server-side); messageId=m1",
      "HttpAttachmentsStorage.deleteByThread is a no-op (cascade is server-side); threadId=t1",
    ]);
  });
});

describe("createMany of portal files", () => {
  const saved = (response: unknown) =>
    upstream.use(http.post(`${AI}/attachments`, () => HttpResponse.json({ response, status: 0 })));

  it("asks nothing for no inputs", async () => {
    await expect(new HttpAttachmentsStorage().createMany([])).resolves.toEqual([]);
    expect(upstream.seen).toHaveLength(0);
  });

  it("saves the files by their entry ids, in the order of the inputs, outside any folder", async () => {
    saved([
      attachmentDto({ id: "b", title: "B.pdf", entryId: "sbox-2" }),
      attachmentDto({ id: "a", entryId: "40" }),
    ]);

    const result = await new HttpAttachmentsStorage().createMany([
      { kind: "file", title: "Report.docx", path: "40" },
      { kind: "file", title: "B.pdf", path: "sbox-2", type: 999 },
    ]);

    expect(result).toStrictEqual([
      fileAttachment({ id: "a" }),
      fileAttachment({ id: "b", title: "B.pdf", path: "sbox-2/B.pdf", type: 999 }),
    ]);
    expect(upstream.seen.map((r) => [r.method, r.path, r.body])).toEqual([
      ["POST", "/internal/ai/attachments", { entryIds: ["40", "sbox-2"], analyzeEntryIds: [] }],
    ]);
  });

  it("asks for the analysis of the forms the request names", async () => {
    saved([attachmentDto({ canAnalyze: true })]);

    await inRequest(async () => {
      setAnalyzeEntryIds(["40", "99"]);
      return new HttpAttachmentsStorage().createMany([
        { kind: "file", title: "Report.docx", path: "40" },
      ]);
    });

    expect(upstream.seen[0]?.body).toEqual({ entryIds: ["40"], analyzeEntryIds: ["40", "99"] });
  });

  it("reads the folder of a room that is no project and attaches the file itself", async () => {
    upstream.use(http.get(`${FILES}/folder/12`, () => HttpResponse.json(folderDto({ id: 12 }))));
    saved([attachmentDto()]);

    await new HttpAttachmentsStorage().createMany([
      { kind: "file", title: "Report.docx", path: "40", entityId: "12" },
    ]);

    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/api/2.0/files/folder/12",
      "/internal/ai/attachments",
    ]);
  });

  it("reads no folder for an id that is not a number", async () => {
    saved([attachmentDto()]);

    await new HttpAttachmentsStorage().createMany([
      { kind: "file", title: "Report.docx", path: "40", entityId: "@my" },
    ]);

    expect(upstream.seen.map((r) => r.path)).toEqual(["/internal/ai/attachments"]);
  });

  it.each([403, 500])(
    "attaches the file itself and warns when the folder cannot be read (%i)",
    async (status) => {
      upstream.use(
        http.get(`${FILES}/folder/12`, () => HttpResponse.json({}, { status, statusText: "R" })),
      );
      saved([attachmentDto()]);

      await expect(
        new HttpAttachmentsStorage().createMany([
          { kind: "file", title: "Report.docx", path: "40", entityId: "12" },
        ]),
      ).resolves.toStrictEqual([fileAttachment()]);
      expect(logged("warn")).toEqual([
        `HttpAttachmentsStorage.createMany: folder 12 not read, attached without a copy: DocSpace API ${status} R for ${FILES}/folder/12`,
      ]);
    },
  );

  it("attaches the copy of a portal file in a chat of an AI project and analyzes the copy", async () => {
    upstream.use(
      http.get(`${FILES}/folder/37`, () => HttpResponse.json(folderDto({ id: 37, roomType: 12 }))),
      http.get(`${FILES}/file/40`, () => HttpResponse.json(fileDto({ id: 40, folderId: 5 }))),
      http.get(`${FILES}/file/40/presigneduri`, () =>
        HttpResponse.json({ response: "/storage/files/40.docx?auth=sig" }),
      ),
      http.get(`${proxyBaseUrl}/storage/files/40.docx`, () =>
        HttpResponse.arrayBuffer(new Uint8Array([1, 2]).buffer),
      ),
      http.get(`${FILES}/settings`, () =>
        HttpResponse.json({ response: { chunkUploadSize: 1024 } }),
      ),
      http.post(`${FILES}/37/session`, () => HttpResponse.json({ response: { id: "s1" } })),
      http.post(`${FILES}/37/session/s1`, () =>
        HttpResponse.json({ response: { uploaded: true, file: { id: 77 } } }),
      ),
    );
    saved([attachmentDto({ entryId: "77" })]);

    const result = await inRequest(async () => {
      setAnalyzeEntryIds(["40"]);
      return new HttpAttachmentsStorage().createMany([
        { kind: "file", title: "Report.docx", path: "40", entityId: "37" },
        { kind: "file", title: "Report.docx", path: "40" },
      ]);
    });

    expect(result).toStrictEqual([
      fileAttachment({ path: "77/Report.docx" }),
      fileAttachment({ path: "77/Report.docx" }),
    ]);
    expect(result[0]).toBe(result[1]);
    expect(upstream.seen.at(-1)?.body).toEqual({ entryIds: ["77", "77"], analyzeEntryIds: ["77"] });
  });

  it("refuses a draft without a DocSpace entry before any request", async () => {
    const error = await failure(
      new HttpAttachmentsStorage().createMany([
        { kind: "file", title: "a.txt", path: "40" },
        { kind: "file", title: "draft.txt", content: "x" },
      ]),
    );

    expect(error).toMatchObject({
      message: "raw-payload attachments (no DocSpace entry id) are not supported by the backend",
    });
    expect(upstream.seen).toHaveLength(0);
  });

  it("refuses an entry the backend does not return with 404 (Bug 82893)", async () => {
    saved([attachmentDto({ entryId: "40" })]);

    const error = await failure(
      new HttpAttachmentsStorage().createMany([
        { kind: "file", title: "Report.docx", path: "40" },
        { kind: "file", title: "Secret.docx", path: "41" },
      ]),
    );

    expect(error).toMatchObject({
      message: "attachment entry not found or inaccessible: 41",
      status: 404,
      expose: true,
    });
    expect(logged("error")).toEqual([
      'HttpAttachmentsStorage.createMany: no match for entryId=41. requested=["40","41"] backend entryIds=["40"]',
    ]);
  });

  it("skips and warns about records it cannot read, then refuses the entry they stood for", async () => {
    saved([attachmentDto({ entryId: undefined }), attachmentDto({ title: undefined }), "x"]);

    const error = await failure(
      new HttpAttachmentsStorage().createMany([{ kind: "file", title: "Report.docx", path: "40" }]),
    );

    expect(error).toMatchObject({ status: 404 });
    expect(logged("warn")).toEqual([
      "HttpAttachmentsStorage.createMany: 3 of 3 item(s) skipped (missing id/title/kind/entryId)",
    ]);
  });

  it.each([
    ["an object", attachmentDto(), "object"],
    ["null", null, "null"],
    ["a string", "ok", "string"],
  ])("refuses an answer that is %s as one of an unknown shape", async (_case, response, type) => {
    saved(response);

    const error = await failure(
      new HttpAttachmentsStorage().createMany([{ kind: "file", title: "Report.docx", path: "40" }]),
    );

    expect(error).not.toBeInstanceOf(AiServiceHttpError);
    expect(error).toEqual(new UpstreamShapeError("POST attachments"));
    expect(logged("error")[0]).toMatch(
      new RegExp(
        `^HttpAttachmentsStorage\\.createMany: backend returned non-array payload \\(type=${type}\\)`,
      ),
    );
  });

  it.each([400, 403, 500])("passes a %i of the save on", async (status) => {
    upstream.use(http.post(`${AI}/attachments`, () => HttpResponse.json({}, { status })));

    expect(
      await failure(
        new HttpAttachmentsStorage().createMany([
          { kind: "file", title: "Report.docx", path: "40" },
        ]),
      ),
    ).toMatchObject({ status });
  });

  it("inlines the images it saved", async () => {
    saved([
      attachmentDto({ kind: "image", title: "Pic.png", dataUrl: "/storage/files/41/pic.png" }),
    ]);
    upstream.use(
      http.get(`${proxyBaseUrl}/storage/files/41/pic.png`, () =>
        HttpResponse.arrayBuffer(new Uint8Array(PNG).buffer),
      ),
    );

    const [image] = await new HttpAttachmentsStorage().createMany([
      { kind: "image", title: "Pic.png", path: "40" },
    ]);

    expect(image?.base64).toBe(`data:image/png;base64,${PNG_B64}`);
  });

  it("logs the shape of the inputs it was handed", async () => {
    saved([attachmentDto({ entryId: "40" }), attachmentDto({ id: "b", entryId: "41" })]);

    await new HttpAttachmentsStorage().createMany([
      { kind: "file", path: "40" } as Omit<Attachment, "id" | "createdAt">,
      {
        kind: "file",
        title: "B.docx",
        path: "41",
        source: "user",
        entityId: "@my",
        messageId: "m",
        threadId: "t",
        base64: "abc",
      },
    ]);

    expect(logged("info")[0]).toBe(
      "HttpAttachmentsStorage.createMany: 2 input(s) " +
        "#0{kind=file source=user entityId=- path=40 title=- base64=0B messageId=- threadId=-} " +
        "#1{kind=file source=user entityId=@my path=41 title=B.docx base64=3B messageId=m threadId=t}",
    );
  });

  it("create answers the one record it saved", async () => {
    saved([attachmentDto()]);

    await expect(
      new HttpAttachmentsStorage().create({ kind: "file", title: "Report.docx", path: "40" }),
    ).resolves.toStrictEqual(fileAttachment());
  });
});

describe("createMany of a generated image", () => {
  const NAME = "generated_image_2026-11-23_14-05-09";
  const tool = (overrides: Partial<Attachment> = {}): Omit<Attachment, "id" | "createdAt"> => ({
    kind: "image",
    title: "generate_image",
    source: "tool",
    base64: PNG_B64,
    ...overrides,
  });
  const record = (entryId: string) =>
    attachmentDto({ id: `ref-${entryId}`, kind: "image", title: "generate_image", entryId });
  const recorded = (entryId: string): Attachment => ({
    id: `ref-${entryId}`,
    kind: "image",
    title: "generate_image",
    createdAt: 1763900000000,
    path: `${entryId}/generate_image`,
    canAnalyze: false,
    source: "tool",
  });

  interface Insert {
    folder: string;
    headers: Record<string, string>;
    title: unknown;
    createNewIfExist: unknown;
    file: { name: string; type: string; bytes: number[] };
  }

  /** The Files API: folders, My documents, the insert (each answered as the test says). */
  function filesApi(options: {
    folder?: (id: string) => Response;
    my?: () => Response;
    insert?: (folder: string) => Response;
  }): Insert[] {
    const inserts: Insert[] = [];
    upstream.use(
      http.get(`${FILES}/folder/:id`, ({ params }) =>
        options.folder
          ? options.folder(String(params["id"]))
          : HttpResponse.json({}, { status: 404 }),
      ),
      http.get(`${FILES}/@my`, () =>
        options.my ? options.my() : HttpResponse.json({ response: { current: { id: 4 } } }),
      ),
      http.post(`${FILES}/:folderId/insert`, async ({ request, params }) => {
        const form = await request.formData();
        const file = form.get("file") as File;
        const folder = String(params["folderId"]);
        inserts.push({
          folder,
          headers: Object.fromEntries(request.headers),
          title: form.get("title"),
          createNewIfExist: form.get("createNewIfExist"),
          file: {
            name: file.name,
            type: file.type,
            bytes: [...new Uint8Array(await file.arrayBuffer())],
          },
        });
        return options.insert
          ? options.insert(folder)
          : HttpResponse.json(fileDto({ id: 90, title: `${NAME}.png`, folderId: Number(folder) }));
      }),
    );
    return inserts;
  }

  // FilesController.DeleteFile: `DELETE api/2.0/files/file/{fileId}`, answering the started operations.
  const removals = (answer: () => Response = () => HttpResponse.json({ response: [] })) => {
    const removed: { id: string; single: string | null; body: unknown }[] = [];
    upstream.use(
      http.delete(`${FILES}/file/:id`, async ({ request, params }) => {
        removed.push({
          id: String(params["id"]),
          single: new URL(request.url).searchParams.get("returnSingleOperation"),
          body: await request.json(),
        });
        return answer();
      }),
    );
    return removed;
  };

  const saveRecords = () => {
    const bodies: unknown[] = [];
    upstream.use(
      http.post(`${AI}/attachments`, async ({ request }) => {
        const body = (await request.json()) as { entryIds: string[] };
        bodies.push(body);
        return HttpResponse.json({ response: body.entryIds.map(record), status: 0 });
      }),
    );
    return bodies;
  };

  const expectPlaceholder = (result: Attachment[], stage: string, entityId = "-") => {
    expect(result).toStrictEqual([
      {
        id: expect.stringMatching(
          /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/,
        ),
        kind: "image",
        title: "generate_image",
        createdAt: NOW.getTime(),
        source: "tool",
      },
    ]);
    // The last two lines; a failed insert or a missing record logs its own line before them.
    expect(logged("error").slice(-2)).toEqual([
      expect.stringMatching(
        new RegExp(`^uploadToolImage: entityId=${entityId} FAILED at stage=${stage}: `),
      ),
      `uploadToolImage: returning an UNRESOLVABLE placeholder ref=${result[0]?.id} — no attachment record was confirmed, so the stream carries a ref the chat can never hydrate (broken image in the message; a stored file and its removal are in the lines above)`,
    ]);
  };

  it("is inserted into the round's folder with the caller's session and recorded as the tool's", async () => {
    const inserts = filesApi({ folder: () => HttpResponse.json(folderDto({ id: 12 })) });
    const bodies = saveRecords();

    const result = await inRequest(
      () => new HttpAttachmentsStorage().createMany([tool({ entityId: "12" })]),
      { headers: CALLER },
    );

    expect(result).toStrictEqual([recorded("90")]);
    expect(inserts).toEqual([
      {
        folder: "12",
        headers: expect.objectContaining(CALLER),
        title: `${NAME}.png`,
        createNewIfExist: "true",
        file: { name: `${NAME}.png`, type: "image/png", bytes: PNG },
      },
    ]);
    expect(bodies).toEqual([{ entryIds: ["90"], analyzeEntryIds: [] }]);
    expect(logged("info")).toContain(
      "uploadToolImage: ok entityId=12 folder=12 entryId=90 ref=ref-90 title=generate_image hasDataUrl=false",
    );
    expect(logged("info")).toContain(
      `insertGeneratedImage: POST ${FILES}/12/insert title=${NAME}.png mime=image/png bytes=8 auth=[cookie,authorization] forwarded=[authorization,cookie,if-none-match]`,
    );
  });

  it.each([
    ["a PNG data URL", `data:image/png;base64,${PNG_B64}`, "image/png", ".png", PNG],
    ["a JPEG data URL", "data:image/jpeg;base64,/9j/", "image/jpeg", ".jpg", [0xff, 0xd8, 0xff]],
    ["a WebP data URL", "data:image/webp;base64,AA==", "image/webp", ".webp", [0]],
    ["a GIF data URL", "data:image/gif;base64,AA==", "image/gif", ".gif", [0]],
    ["an SVG data URL", "data:image/svg+xml;base64,AA==", "image/svg+xml", ".png", [0]],
    ["bare JPEG base64", "/9j/", "image/jpeg", ".jpg", [0xff, 0xd8, 0xff]],
    ["bare base64 of unknown bytes", "AQID", "image/png", ".png", [1, 2, 3]],
  ])("is named and typed from %s", async (_case, base64, type, ext, bytes) => {
    const inserts = filesApi({});
    saveRecords();

    await new HttpAttachmentsStorage().createMany([tool({ base64 })]);

    expect(inserts[0]?.file).toEqual({ name: `${NAME}${ext}`, type, bytes });
    expect(inserts[0]?.title).toBe(`${NAME}${ext}`);
  });

  it("goes to My documents when the round names no folder, without a warning", async () => {
    const inserts = filesApi({});
    saveRecords();

    await expect(new HttpAttachmentsStorage().createMany([tool()])).resolves.toStrictEqual([
      recorded("90"),
    ]);
    expect(inserts.map((i) => i.folder)).toEqual(["4"]);
    expect(upstream.seen.map((r) => r.path)).not.toContain("/api/2.0/files/folder/undefined");
    expect(logged("warn")).toEqual([]);
  });

  it.each([
    [
      "a folder the caller cannot create in",
      () => HttpResponse.json(folderDto({ security: { Create: false } })),
      "folderType=? canCreate=false",
    ],
    [
      "the root of a section",
      () => HttpResponse.json(folderDto({ parentId: 0, rootFolderType: 14, type: 14 })),
      "folderType=14 canCreate=true",
    ],
    [
      "a folder that is gone",
      () => HttpResponse.json({}, { status: 404 }),
      "folderType=? canCreate=?",
    ],
  ])("falls back to My documents from %s, with a warning", async (_case, folder, detail) => {
    const inserts = filesApi({ folder });
    saveRecords();

    await expect(
      new HttpAttachmentsStorage().createMany([tool({ entityId: "12" })]),
    ).resolves.toStrictEqual([recorded("90")]);
    expect(inserts.map((i) => i.folder)).toEqual(["4"]);
    expect(logged("warn")).toEqual([
      `uploadToolImage: scope 12 cannot take an upload (${detail}) -> falling back to My documents 4`,
    ]);
  });

  it("logs a folder without a title or a security block as unknown", async () => {
    filesApi({
      folder: () =>
        HttpResponse.json(folderDto({ title: undefined, security: undefined, type: 5 })),
      insert: () => HttpResponse.json(fileDto({ id: 90, folderId: "sbox-12", title: undefined })),
    });
    saveRecords();

    await new HttpAttachmentsStorage().createMany([
      tool({ entityId: "12", title: undefined as unknown as string }),
    ]);

    expect(logged("info")).toEqual(
      expect.arrayContaining([
        `uploadToolImage: begin entityId=12 title=- base64=${PNG_B64.length}B payload=bare-base64`,
        "uploadToolImage: folder-info entityId=12 -> title=- folderType=5 canCreate=?",
        `insertGeneratedImage: POST ${FILES}/12/insert -> 200 entryId=90 title=? folderId=sbox-12`,
      ]),
    );
  });

  it("logs the folder it read and the one it found missing", async () => {
    filesApi({
      folder: (id) =>
        id === "12" ? HttpResponse.json(folderDto()) : HttpResponse.json({}, { status: 404 }),
    });
    saveRecords();

    await new HttpAttachmentsStorage().createMany([
      tool({ entityId: "12" }),
      tool({ entityId: "13" }),
    ]);

    expect(logged("info")).toEqual(
      expect.arrayContaining([
        "uploadToolImage: folder-info entityId=12 -> title=Marketing folderType=? canCreate=true",
        "uploadToolImage: folder-info entityId=13 -> NOT FOUND (404) — the scope is not a folder this user can read",
      ]),
    );
  });

  it("retries into My documents when the insert into the folder is refused with 403", async () => {
    const inserts = filesApi({
      folder: () => HttpResponse.json(folderDto()),
      insert: (folder) =>
        folder === "12"
          ? HttpResponse.text("denied", { status: 403, statusText: "Forbidden" })
          : HttpResponse.json(fileDto({ id: 91 })),
    });
    saveRecords();

    await expect(
      new HttpAttachmentsStorage().createMany([tool({ entityId: "12" })]),
    ).resolves.toStrictEqual([recorded("91")]);
    expect(inserts.map((i) => i.folder)).toEqual(["12", "4"]);
    expect(logged("warn")).toEqual([
      "uploadToolImage: insert into folder 12 was refused (403) -> retrying into My documents 4",
    ]);
  });

  it.each([
    [
      "the retry is refused too",
      { insert: () => HttpResponse.json({}, { status: 403 }) },
      "insert-retry",
      ["12", "4"],
    ],
    [
      "My documents cannot be resolved for the retry",
      {
        insert: () => HttpResponse.json({}, { status: 403 }),
        my: () => HttpResponse.json({}, { status: 500 }),
      },
      "my-documents",
      ["12"],
    ],
  ])("answers a placeholder when %s", async (_case, options, stage, folders) => {
    const inserts = filesApi({ folder: () => HttpResponse.json(folderDto()), ...options });
    saveRecords();

    const result = await new HttpAttachmentsStorage().createMany([tool({ entityId: "12" })]);

    expectPlaceholder(result, stage, "12");
    expect(inserts.map((i) => i.folder)).toEqual(folders);
    expect(logged("error")).toHaveLength(folders.length + 2);
  });

  it("answers a placeholder when My documents itself refuses the insert with 403", async () => {
    const inserts = filesApi({ insert: () => HttpResponse.json({}, { status: 403 }) });
    saveRecords();

    const result = await new HttpAttachmentsStorage().createMany([tool()]);

    expectPlaceholder(result, "my-documents");
    expect(inserts.map((i) => i.folder)).toEqual(["4"]);
    expect(logged("error")[1]).toMatch(
      /^uploadToolImage: entityId=- FAILED at stage=my-documents: DocSpace API 403 /,
    );
  });

  it("logs a refused insert as a Files API error with its status, not its body, and does not retry another status", async () => {
    const inserts = filesApi({
      insert: () => HttpResponse.text("upstream body", { status: 500, statusText: "Boom" }),
    });
    saveRecords();

    const result = await new HttpAttachmentsStorage().createMany([tool()]);

    expect(inserts).toHaveLength(1);
    expect(logged("error")[0]).toBe(`insertGeneratedImage: POST ${FILES}/4/insert -> 500 Boom`);
    expect(result[0]?.source).toBe("tool");
    expect(logged("error")[1]).toMatch(
      /^uploadToolImage: entityId=- FAILED at stage=insert: DocSpace API 500 Boom for /,
    );
  });

  it.each([
    ["the answer is a list", [fileDto()]],
    ["the response is null", { response: null }],
    ["the file has no id", fileDto({ id: undefined })],
    ["the file's id is a boolean", fileDto({ id: true })],
  ])("answers a placeholder when %s", async (_case, body) => {
    filesApi({ insert: () => HttpResponse.json(body) });
    saveRecords();

    const result = await new HttpAttachmentsStorage().createMany([tool()]);

    expect(logged("error")[1]).toContain(
      "uploadToolImage: entityId=- FAILED at stage=insert: POST files/{folderId}/insert: the answer has an unexpected shape",
    );
    expect(result[0]?.id).not.toBe("ref-90");
  });

  it("takes a string id of the inserted file and the file without the envelope", async () => {
    filesApi({ insert: () => HttpResponse.json({ id: "sbox-9", title: "t" }) });
    saveRecords();

    await expect(new HttpAttachmentsStorage().createMany([tool()])).resolves.toStrictEqual([
      recorded("sbox-9"),
    ]);
    expect(logged("info")).toContain(
      `insertGeneratedImage: POST ${FILES}/4/insert -> 200 entryId=sbox-9 title=t folderId=?`,
    );
  });

  it.each([
    [
      "the round names no folder",
      undefined,
      'the round names no folder and "My documents" could not be resolved',
    ],
    [
      "the folder cannot take the upload",
      "12",
      'the chat scope 12 cannot take an upload and "My documents" could not be resolved',
    ],
  ])(
    "answers a placeholder when %s and the caller has no My documents",
    async (_case, entityId, message) => {
      filesApi({
        folder: () => HttpResponse.json({}, { status: 404 }),
        my: () => HttpResponse.json({}, { status: 403 }),
      });

      const result = await new HttpAttachmentsStorage().createMany([tool({ entityId })]);

      expectPlaceholder(result, "my-documents", entityId ?? "-");
      expect(logged("error")[0]).toContain(message);
    },
  );

  it.each([
    ["the round names no folder", undefined],
    ["the folder cannot take the upload", "12"],
  ])(
    "answers a placeholder naming the unknown answer when %s and My documents cannot be read",
    async (_case, entityId) => {
      const message = "GET files/@my: the answer has an unexpected shape";
      filesApi({
        folder: () => HttpResponse.json({}, { status: 404 }),
        my: () => HttpResponse.json({ response: { current: null } }),
      });

      const result = await new HttpAttachmentsStorage().createMany([tool({ entityId })]);

      expectPlaceholder(result, "my-documents", entityId ?? "-");
      expect(logged("error")[0]).toContain(message);
    },
  );

  it("answers a placeholder when the folder read fails", async () => {
    filesApi({ folder: () => HttpResponse.json({}, { status: 500 }) });

    const result = await new HttpAttachmentsStorage().createMany([tool({ entityId: "12" })]);

    expectPlaceholder(result, "folder-info", "12");
  });

  it("answers a placeholder and removes the inserted file when the record is refused", async () => {
    const inserts = filesApi({});
    const removed = removals();
    upstream.use(http.post(`${AI}/attachments`, () => HttpResponse.json({}, { status: 500 })));

    const result = await new HttpAttachmentsStorage().createMany([tool()]);

    expectPlaceholder(result, "attachment-record");
    expect(inserts).toHaveLength(1);
    // returnSingleOperation: without it the portal would dequeue every finished operation of the user.
    expect(removed).toEqual([
      { id: "90", single: "true", body: { deleteAfter: false, immediately: true } },
    ]);
    expect(logged("warn")).toContain(
      "uploadToolImage: asked to delete file 90 (a file operation), its attachment record was not confirmed",
    );
  });

  it.each([
    ["is refused", () => HttpResponse.json({}, { status: 403, statusText: "Forbidden" })],
    ["drops the connection", () => HttpResponse.error()],
  ])("answers a placeholder and logs the file's id when its removal %s", async (_case, answer) => {
    filesApi({});
    const removed = removals(answer);
    upstream.use(http.post(`${AI}/attachments`, () => HttpResponse.json({}, { status: 500 })));

    const result = await new HttpAttachmentsStorage().createMany([tool()]);

    expectPlaceholder(result, "attachment-record");
    expect(removed.map((r) => r.id)).toEqual(["90"]);
    expect(logged("error")[0]).toMatch(
      /^uploadToolImage: file 90 is left without an attachment record, its removal failed: /,
    );
  });

  it("removes nothing when the image was never stored", async () => {
    filesApi({ insert: () => HttpResponse.json({}, { status: 500 }) });

    await new HttpAttachmentsStorage().createMany([tool()]);

    expect(upstream.seen.some((r) => r.method === "DELETE")).toBe(false);
  });

  it("rejects an image without a payload before any request, with an error in the log", async () => {
    const error = await failure(
      new HttpAttachmentsStorage().createMany([tool({ base64: undefined, entityId: "12" })]),
    );

    expect(error).toMatchObject({ message: "tool image attachment is missing its base64 payload" });
    expect(upstream.seen).toHaveLength(0);
    expect(logged("error")).toEqual([
      "uploadToolImage: no base64 payload (entityId=12 title=generate_image) — the generated image cannot be persisted",
    ]);
  });

  it("logs an image without a payload, a folder or a title with dashes", async () => {
    await failure(
      new HttpAttachmentsStorage().createMany([
        tool({ base64: undefined, title: undefined as unknown as string }),
      ]),
    );

    expect(logged("error")).toEqual([
      "uploadToolImage: no base64 payload (entityId=- title=-) — the generated image cannot be persisted",
    ]);
  });

  // HANDLED DOWNSTREAM stor1-17: a batch of a generated image and a draft without an entry uploads and records the image, then refuses the batch, leaving the image behind. In production the engine creates a tool image one at a time and every caller of createMany passes one input, so such a batch does not occur; it shows only with the engine or the service stubbed.
  it("stores the image of a batch it then refuses for a draft without an entry", async () => {
    const inserts = filesApi({});
    const bodies = saveRecords();

    const error = await failure(
      new HttpAttachmentsStorage().createMany([tool(), { kind: "file", title: "draft.txt" }]),
    );

    expect(error).toMatchObject({ message: expect.stringMatching(/^raw-payload attachments/) });
    expect(inserts).toHaveLength(1);
    expect(bodies).toHaveLength(1);
  });

  describe("in a conversation of an AI project", () => {
    const artifacts = (answer: () => Response) => {
      const posted: { path: string; title: unknown; name: string }[] = [];
      upstream.use(
        http.post(`${AI}/threads/:id/artifacts`, async ({ request }) => {
          const form = await request.formData();
          posted.push({
            path: new URL(request.url).pathname,
            title: form.get("title"),
            name: (form.get("file") as File).name,
          });
          return answer();
        }),
      );
      return posted;
    };

    it("is stored among the conversation's artifacts and recorded, without a folder read", async () => {
      const posted = artifacts(() =>
        HttpResponse.json({ response: { id: 55, title: "x" }, status: 0 }),
      );
      const bodies = saveRecords();

      await expect(
        new HttpAttachmentsStorage().createMany([tool({ threadId: "t-1", entityId: "12" })]),
      ).resolves.toStrictEqual([recorded("55")]);
      expect(posted).toEqual([
        { path: "/internal/ai/threads/t-1/artifacts", title: `${NAME}.png`, name: `${NAME}.png` },
      ]);
      expect(bodies).toEqual([{ entryIds: ["55"], analyzeEntryIds: [] }]);
      expect(upstream.seen.some((r) => r.path.startsWith("/api/2.0/files"))).toBe(false);
      expect(logged("info")).toContain(
        "uploadToolImage: ok threadId=t-1 artifact entryId=55 ref=ref-55",
      );
    });

    it("goes to the round's folder when the conversation takes no artifacts", async () => {
      artifacts(() => HttpResponse.json({}, { status: 400 }));
      const inserts = filesApi({ folder: () => HttpResponse.json(folderDto()) });
      saveRecords();

      await expect(
        new HttpAttachmentsStorage().createMany([tool({ threadId: "t-1", entityId: "12" })]),
      ).resolves.toStrictEqual([recorded("90")]);
      expect(inserts.map((i) => i.folder)).toEqual(["12"]);
    });

    it("answers a placeholder when the artifact upload fails", async () => {
      artifacts(() => HttpResponse.json({}, { status: 500 }));

      const result = await new HttpAttachmentsStorage().createMany([tool({ threadId: "t-1" })]);

      expectPlaceholder(result, "thread-artifact");
    });

    it("answers a placeholder when the artifact's record is refused", async () => {
      artifacts(() => HttpResponse.json({ response: { id: 55 }, status: 0 }));
      const removed = removals();
      upstream.use(
        http.post(`${AI}/attachments`, () => HttpResponse.json({ response: [], status: 0 })),
      );

      const result = await new HttpAttachmentsStorage().createMany([tool({ threadId: "t-1" })]);

      expectPlaceholder(result, "attachment-record");
      expect(removed.map((r) => r.id)).toEqual(["55"]);
      expect(logged("error")[1]).toMatch(
        /^uploadToolImage: entityId=- FAILED at stage=attachment-record: attachment entry not found or inaccessible: 55/,
      );
    });
  });

  it("keeps the order of a batch of portal files and generated images", async () => {
    filesApi({});
    upstream.use(
      http.post(`${AI}/attachments`, async ({ request }) => {
        const body = (await request.json()) as { entryIds: string[] };
        return HttpResponse.json({
          response: body.entryIds.map((id) =>
            id === "90" ? record("90") : attachmentDto({ id: `f-${id}`, entryId: id }),
          ),
          status: 0,
        });
      }),
    );

    const result = await new HttpAttachmentsStorage().createMany([
      { kind: "file", title: "Report.docx", path: "40" },
      tool(),
    ]);

    expect(result.map((a) => a.id)).toEqual(["f-40", "ref-90"]);
  });
});
