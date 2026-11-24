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
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { http, HttpResponse, delay, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { getFilesApiReadCount, getSourceMeta, setSourceMeta } from "../../requestContext.js";
import {
  assertEntityAccessible,
  canTakeUpload,
  DocspaceApiHttpError,
  envelopeResponse,
  filesApiDownload,
  filesApiDownloadBytes,
  filesApiDownloadText,
  filesApiGet,
  filesApiRequest,
  filesApiSend,
  folderSourceType,
  getFileInfo,
  getFolderInfo,
  getFolderInfoCached,
  getMyDocumentsFolderId,
  primeSourceMeta,
  resolveAiProjectEntityId,
  safeResolveSource,
} from "../docspaceFilesApi.js";
import { proxyBaseUrl } from "../httpClient.js";
import {
  fileDto,
  folderDto,
  inRequest,
  snapshotWithFolders,
} from "../../__tests__/fixtures/filesApi.js";

// The Files API client over a stubbed network: the real httpClient.js (base URL, timeout) and
// requestContext.js (forwarded headers, per-request caches, the snapshot) run. A stub of a
// route answers what the test tells it to; the routes are named above each block.
const upstream = useUpstream();
const FILES = `${proxyBaseUrl}/api/2.0/files`;
const CALLER = { cookie: "asc_auth_key=abc", authorization: "Bearer t", "if-none-match": "x" };

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

beforeEach(() => {
  vi.mocked(logger.warn).mockClear();
  vi.mocked(logger.info).mockClear();
});

// FilesController / FoldersController / SettingsController: any `GET api/2.0/files/{path}`.
describe("filesApiGet", () => {
  it("reads the response of the envelope with the caller's headers and counts the read", async () => {
    upstream.use(http.get(`${FILES}/rooms/skills`, () => HttpResponse.json(folderDto())));

    const result = await inRequest(
      async () => ({
        value: await filesApiGet("rooms/skills", { roomIds: ["7", 8], count: 1 }),
        reads: getFilesApiReadCount(),
      }),
      { headers: CALLER },
    );

    expect(result).toEqual({ value: folderDto()["response"], reads: 1 });
    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]).toMatchObject({
      method: "GET",
      path: "/api/2.0/files/rooms/skills?roomIds=7&roomIds=8&count=1",
    });
    expect(upstream.seen[0]?.headers).toMatchObject(CALLER);
    expect(upstream.seen[0]?.headers["content-type"]).toBeUndefined();
  });

  it.each([
    ["no query", undefined],
    ["an empty query", {}],
    ["an empty list", { ids: [] }],
  ])("sends no query string for %s", async (_case, query) => {
    upstream.use(http.get(`${FILES}/settings`, () => HttpResponse.json({ response: 1 })));

    await expect(filesApiGet("settings", query)).resolves.toBe(1);

    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/settings");
  });

  it.each([
    ["null", null],
    ["a string", "x"],
    ["a list", [1]],
  ])("answers a response that is %s as it is", async (_case, response) => {
    upstream.use(http.get(`${FILES}/x`, () => HttpResponse.json({ response })));

    await expect(filesApiGet("x")).resolves.toEqual(response);
  });

  it.each([
    ["an object without response", { result: 1 }],
    ["a list", [1]],
    ["a number", 5],
    ["null", null],
  ])("refuses an answer that is %s", async (_case, body) => {
    upstream.use(http.get(`${FILES}/x`, () => HttpResponse.json(body)));

    const error = await failure(filesApiGet("x"));

    expect(error).not.toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toEqual(new Error(`DocSpace API: unparseable response from ${FILES}/x`));
  });

  it("refuses an answer that is not JSON", async () => {
    upstream.use(http.get(`${FILES}/x`, () => HttpResponse.text("<html>")));

    expect(await failure(filesApiGet("x"))).toBeInstanceOf(SyntaxError);
  });

  it.each([400, 401, 403, 404, 500, 503])(
    "rejects a %i with a DocspaceApiHttpError that carries the status and the URL",
    async (status) => {
      upstream.use(
        http.get(`${FILES}/x`, () =>
          HttpResponse.json({ error: "secret" }, { status, statusText: "Reason" }),
        ),
      );

      const error = await failure(filesApiGet("x", { a: 1 }));

      expect(error).toBeInstanceOf(DocspaceApiHttpError);
      expect(error).toMatchObject({ status, statusText: "Reason", url: `${FILES}/x?a=1` });
      expect((error as Error).message).toBe(`DocSpace API ${status} Reason for ${FILES}/x?a=1`);
      expect((error as Error).message).not.toContain("secret");
    },
  );

  describe("on fake timers", () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    it("rejects once the Files API has not answered for 300000 ms", async () => {
      upstream.use(
        http.get(`${FILES}/slow`, async () => {
          await delay("infinite");
          return HttpResponse.json({ response: 1 });
        }),
      );
      const pending = failure(filesApiGet("slow"));

      await vi.advanceTimersByTimeAsync(300_000);

      expect(String(await pending)).toContain("upstream timeout after 300000ms");
    });
  });
});

// Any `POST`/`PUT api/2.0/files/{path}` with a JSON body.
describe("filesApiSend", () => {
  it.each(["POST", "PUT"] as const)(
    "sends a %s with the JSON body and the caller's headers and reads the response",
    async (method) => {
      upstream.use(
        http.all(`${FILES}/rooms/30/tags`, () => HttpResponse.json({ response: { id: 30 } })),
      );

      const result = await inRequest(
        async () => ({
          value: await filesApiSend(method, "rooms/30/tags", { names: ["a"] }),
          reads: getFilesApiReadCount(),
        }),
        { headers: CALLER },
      );

      expect(result).toEqual({ value: { id: 30 }, reads: 0 });
      expect(upstream.seen[0]).toMatchObject({
        method,
        path: "/api/2.0/files/rooms/30/tags",
        body: { names: ["a"] },
      });
      expect(upstream.seen[0]?.headers).toMatchObject({
        ...CALLER,
        "content-type": "application/json",
      });
    },
  );

  it.each([
    ["not JSON", () => HttpResponse.text("done")],
    ["empty", () => new HttpResponse(null, { status: 200 })],
    ["a JSON list", () => HttpResponse.json([1])],
    ["a JSON object without response", () => HttpResponse.json({ ok: true })],
  ])("answers undefined when the answer is %s", async (_case, answer) => {
    upstream.use(http.post(`${FILES}/x`, answer));

    await expect(filesApiSend("POST", "x", {})).resolves.toBeUndefined();
  });

  it("rejects a refusal with a DocspaceApiHttpError", async () => {
    upstream.use(
      http.put(`${FILES}/x`, () => HttpResponse.json({}, { status: 403, statusText: "Forbidden" })),
    );

    const error = await failure(filesApiSend("PUT", "x", { a: 1 }));

    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({ status: 403, statusText: "Forbidden", url: `${FILES}/x` });
  });
});

// Any write to `api/2.0/files/{path}` whose answer is read as it came: the uploads
// (`POST {folderId}/insert`, `POST {folderId}/session[/{id}]`) and `DELETE file/{fileId}`.
describe("filesApiRequest", () => {
  it("sends a form as multipart with the caller's headers and answers the JSON as it came", async () => {
    const parts: unknown[] = [];
    upstream.use(
      http.post(`${FILES}/12/insert`, async ({ request }) => {
        const form = await request.formData();
        parts.push({
          title: form.get("title"),
          contentType: request.headers.get("content-type"),
          cookie: request.headers.get("cookie"),
        });
        return HttpResponse.json({ id: 9 });
      }),
    );
    const form = new FormData();
    form.append("title", "a.png");

    const answer = await inRequest(() => filesApiRequest("POST", "12/insert", { form }), {
      headers: CALLER,
    });

    expect(answer).toEqual({ id: 9 });
    expect(parts).toEqual([
      {
        title: "a.png",
        contentType: expect.stringMatching(/^multipart\/form-data; boundary=/),
        cookie: CALLER.cookie,
      },
    ]);
  });

  it.each(["POST", "PUT", "DELETE"] as const)("sends a %s with a JSON body", async (method) => {
    upstream.use(http.all(`${FILES}/file/40`, () => HttpResponse.json({ response: [] })));

    await expect(
      inRequest(() => filesApiRequest(method, "file/40", { json: { immediately: true } }), {
        headers: CALLER,
      }),
    ).resolves.toEqual({ response: [] });

    expect(upstream.seen[0]).toMatchObject({ method, body: { immediately: true } });
    expect(upstream.seen[0]?.headers).toMatchObject({
      ...CALLER,
      "content-type": "application/json",
    });
  });

  it.each([400, 403, 500])(
    "rejects a %i with a DocspaceApiHttpError without the body",
    async (status) => {
      upstream.use(
        http.delete(`${FILES}/file/40`, () =>
          HttpResponse.text("secret", { status, statusText: "Reason" }),
        ),
      );

      const error = await failure(filesApiRequest("DELETE", "file/40", { json: {} }));

      expect(error).toBeInstanceOf(DocspaceApiHttpError);
      expect(error).toMatchObject({ status, statusText: "Reason", url: `${FILES}/file/40` });
      expect((error as Error).message).not.toContain("secret");
    },
  );

  it("refuses an answer that is not JSON", async () => {
    upstream.use(http.post(`${FILES}/x`, () => HttpResponse.text("done")));

    expect(await failure(filesApiRequest("POST", "x", { json: {} }))).toBeInstanceOf(SyntaxError);
  });

  it("rejects once the Files API has not answered for 300000 ms", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(
        http.post(`${FILES}/slow`, async () => {
          await delay("infinite");
          return HttpResponse.json({});
        }),
      );
      const pending = failure(filesApiRequest("POST", "slow", { form: new FormData() }));

      await vi.advanceTimersByTimeAsync(300_000);

      expect(String(await pending)).toContain("upstream timeout after 300000ms");
    } finally {
      vi.useRealTimers();
    }
  });
});

describe("envelopeResponse", () => {
  it.each([
    ["the response of an envelope", { response: { id: 1 }, status: 0 }, { id: 1 }],
    ["a null response", { response: null }, null],
    ["an object without one as it is", { id: 2 }, { id: 2 }],
    ["a list as it is", [1], [1]],
    ["null as it is", null, null],
  ])("answers %s", (_case, raw, expected) => {
    expect(envelopeResponse(raw)).toEqual(expected);
  });
});

// The address a file's bytes are served from: a pre-signed link of the portal or of its storage.
describe("filesApiDownload", () => {
  const LINK = "https://storage.example/files/1.png?sig=s";

  it("sends the headers it is given and hands the answer to the reader", async () => {
    upstream.use(http.get("https://storage.example/files/1.png", () => HttpResponse.text("bytes")));

    await expect(
      filesApiDownload(LINK, { cookie: "c=1" }, async (res) => res.text()),
    ).resolves.toBe("bytes");

    expect(upstream.seen[0]?.headers).toMatchObject({ cookie: "c=1" });
  });

  it.each([403, 404, 500])(
    "rejects a %i with a DocspaceApiHttpError before reading",
    async (status) => {
      upstream.use(
        http.get("https://storage.example/files/1.png", () =>
          HttpResponse.text("secret", { status, statusText: "Reason" }),
        ),
      );
      const read = vi.fn(async () => "x");

      const error = await failure(filesApiDownload(LINK, {}, read));

      expect(error).toBeInstanceOf(DocspaceApiHttpError);
      expect(error).toMatchObject({ status, url: LINK });
      expect(read).not.toHaveBeenCalled();
    },
  );

  it("rejects a body that stalls once 300000 ms have passed", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(
        http.get(
          "https://storage.example/files/1.png",
          ({ request }) =>
            new HttpResponse(
              new ReadableStream({
                start(controller) {
                  controller.enqueue(new Uint8Array([1]));
                  request.signal.addEventListener("abort", () =>
                    controller.error(request.signal.reason),
                  );
                },
              }),
            ),
        ),
      );
      const pending = failure(filesApiDownload(LINK, {}, async (res) => res.arrayBuffer()));

      await vi.advanceTimersByTimeAsync(300_000);

      expect(String(await pending)).toContain("upstream timeout after 300000ms");
    } finally {
      vi.useRealTimers();
    }
  });
});

describe("the caller's signal", () => {
  function hanging(method: "get" | "put", path: string) {
    upstream.use(
      http[method](path, async () => {
        await delay("infinite");
        return HttpResponse.json({ response: 1 });
      }),
    );
  }

  it.each([
    ["filesApiGet", (signal: AbortSignal) => filesApiGet("settings", undefined, signal)],
    ["filesApiSend", (signal: AbortSignal) => filesApiSend("PUT", "settings", {}, signal)],
    [
      "filesApiDownload",
      (signal: AbortSignal) =>
        filesApiDownload(`${proxyBaseUrl}/api/2.0/files/settings`, {}, async (r) => r, signal),
    ],
  ])("ends %s before the timeout", async (_name, call) => {
    hanging("get", "*/api/2.0/files/settings");
    hanging("put", "*/api/2.0/files/settings");
    const controller = new AbortController();

    const pending = failure(call(controller.signal));
    controller.abort(new Error("client gone"));

    expect(await pending).toMatchObject({ message: "client gone" });
  });
});

describe("filesApiDownloadText and filesApiDownloadBytes", () => {
  const LINK = "https://storage.example/files/1.txt?sig=s";
  // Streamed: msw settles the cancel of a body read only part-way for a stream only.
  const streamed = (text: string) =>
    new HttpResponse(
      new ReadableStream({
        start(controller) {
          controller.enqueue(new TextEncoder().encode(text));
          controller.close();
        },
      }),
    );

  it("cuts a text at the limit", async () => {
    upstream.use(http.get("https://storage.example/files/1.txt", () => streamed("abcdef")));

    await expect(filesApiDownloadText(LINK, { cookie: "c=1" }, 4)).resolves.toBe("abcd");
    expect(upstream.seen[0]?.headers).toMatchObject({ cookie: "c=1" });
  });

  it("answers the bytes within the limit and null past it", async () => {
    upstream.use(http.get("https://storage.example/files/1.txt", () => streamed("abc")));

    expect(Array.from((await filesApiDownloadBytes(LINK, {}, 3)) ?? [])).toEqual([97, 98, 99]);
    expect(await filesApiDownloadBytes(LINK, {}, 2)).toBeNull();
  });

  it("rejects a refusal with the status and without the body", async () => {
    upstream.use(
      http.get("https://storage.example/files/1.txt", () =>
        HttpResponse.text("secret", { status: 500, statusText: "Boom" }),
      ),
    );

    const error = await failure(filesApiDownloadText(LINK, {}, 4));

    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({ status: 500 });
    expect(String(error)).not.toContain("secret");
  });
});

// FoldersController.GetFolderInfo: `GET api/2.0/files/folder/{folderId}`.
describe("getFolderInfo", () => {
  const readFolder = async (body: JsonBodyType): Promise<unknown> => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(body)));
    return getFolderInfo("30");
  };

  it("maps a room of the Rooms section", async () => {
    await expect(readFolder(folderDto())).resolves.toStrictEqual({
      title: "Marketing",
      roomType: 5,
      canCreate: true,
    });
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/folder/30");
  });

  it("encodes the folder id into the path", async () => {
    upstream.use(http.get(`${FILES}/folder/:id`, () => HttpResponse.json(folderDto())));

    await getFolderInfo("a/b c");

    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/folder/a%2Fb%20c");
  });

  it("forwards the caller's headers and counts the read", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    const reads = await inRequest(
      async () => {
        await getFolderInfo("30");
        return getFilesApiReadCount();
      },
      { headers: CALLER },
    );

    expect(reads).toBe(1);
    expect(upstream.seen[0]?.headers).toMatchObject(CALLER);
  });

  const base = { title: "Marketing", roomType: 5, canCreate: true };
  it.each([
    ["id is a string", { id: "sbox-1-30" }, base],
    ["title is missing", { title: undefined }, { ...base, title: undefined }],
    ["title is null", { title: null }, { ...base, title: undefined }],
    ["title is a number", { title: 7 }, { ...base, title: undefined }],
    ["title is empty", { title: "" }, { ...base, title: "" }],
    ["roomType is missing", { roomType: undefined }, { title: "Marketing", canCreate: true }],
    ["roomType is null", { roomType: null }, { title: "Marketing", canCreate: true }],
    ["roomType is a string", { roomType: "5" }, { title: "Marketing", canCreate: true }],
    ["type is a FolderType", { type: 15 }, { ...base, folderType: 15 }],
    ["type is null", { type: null }, base],
    ["type is a string", { type: "15" }, base],
    ["security is missing", { security: undefined }, { title: "Marketing", roomType: 5 }],
    ["security is null", { security: null }, { title: "Marketing", roomType: 5 }],
    ["security is a list", { security: ["Create"] }, { title: "Marketing", roomType: 5 }],
    ["security is a string", { security: "Create" }, { title: "Marketing", roomType: 5 }],
    ["security has no Create", { security: { Read: true } }, { title: "Marketing", roomType: 5 }],
    ["Create is false", { security: { Create: false } }, { ...base, canCreate: false }],
    ["Create is in lower case", { security: { create: false } }, { ...base, canCreate: false }],
    ["Create is in upper case", { security: { CREATE: true } }, base],
    ["Create is a string", { security: { Create: "true" } }, { title: "Marketing", roomType: 5 }],
    [
      "the first boolean Create wins",
      { security: { create: "x", Create: false, CREATE: true } },
      { ...base, canCreate: false },
    ],
    [
      "it is a section root (no parent, a root type that holds rooms)",
      { parentId: 0, rootFolderType: 41, roomType: undefined },
      { title: "Marketing", canCreate: true, sectionRoot: true },
    ],
    ["it has no parent under a root that holds no rooms", { parentId: 0, rootFolderType: 5 }, base],
    ["parentId is the string 0", { parentId: "0", rootFolderType: 41 }, base],
    ["parentId is missing", { parentId: undefined, rootFolderType: 41 }, base],
    ["rootFolderType is null", { parentId: 0, rootFolderType: null }, base],
    ["rootFolderType is a string", { parentId: 0, rootFolderType: "14" }, base],
  ])("maps the DTO when %s", async (_case, overrides, expected) => {
    await expect(readFolder(folderDto(overrides))).resolves.toStrictEqual(expected);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it.each([14, 20, 30, 34, 36, 41, 42])(
    "names a folder without a parent under root type %i a section root",
    async (rootFolderType) => {
      await expect(readFolder(folderDto({ parentId: 0, rootFolderType }))).resolves.toMatchObject({
        sectionRoot: true,
      });
    },
  );

  it.each([
    ["id is missing", folderDto({ id: undefined })],
    ["id is null", folderDto({ id: null })],
    ["id is a boolean", folderDto({ id: true })],
    ["id is empty", folderDto({ id: "" })],
    ["response is missing", { status: 0 }],
    ["response is null", { response: null }],
    ["response is a list", { response: [folderDto()["response"]] }],
    ["response is a string", { response: "30" }],
    ["the body is a list", [folderDto()]],
  ])("rejects the answer as one of an unknown shape when %s", async (_case, body) => {
    await expect(readFolder(body)).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
      message: "GET files/folder/{id}: the answer has an unexpected shape",
    });
  });

  it("answers undefined for a 404 without a warning", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json({}, { status: 404 })));

    await expect(getFolderInfo("30")).resolves.toBeUndefined();
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it.each([400, 401, 403, 500, 502])("rejects a %i with a DocspaceApiHttpError", async (status) => {
    upstream.use(
      http.get(`${FILES}/folder/30`, () => HttpResponse.json({}, { status, statusText: "Reason" })),
    );

    const error = await failure(getFolderInfo("30"));

    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({ status, statusText: "Reason", url: `${FILES}/folder/30` });
  });

  it("rejects a dropped connection", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.error()));

    expect(await failure(getFolderInfo("30"))).toBeInstanceOf(TypeError);
  });
});

// FilesController.GetFileInfo: `GET api/2.0/files/file/{fileId}`.
describe("getFileInfo", () => {
  const readFile = async (body: JsonBodyType): Promise<unknown> => {
    upstream.use(http.get(`${FILES}/file/40`, () => HttpResponse.json(body)));
    return getFileInfo("40");
  };
  const base = {
    title: "Report.docx",
    folderId: "30",
    fileExst: ".docx",
    webUrl: "http://portal.example/doceditor?fileid=40&version=1",
  };

  it("maps a file", async () => {
    await expect(readFile(fileDto())).resolves.toStrictEqual(base);
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/file/40");
  });

  it("encodes the file id into the path and counts the read", async () => {
    upstream.use(http.get(`${FILES}/file/:id`, () => HttpResponse.json(fileDto())));

    const reads = await inRequest(async () => {
      await getFileInfo("x/y");
      return getFilesApiReadCount();
    });

    expect(reads).toBe(1);
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/file/x%2Fy");
  });

  const { folderId: _f, ...noFolder } = base;
  const { fileExst: _e, ...noExst } = base;
  const { webUrl: _w, ...noUrl } = base;
  it.each([
    ["id is a string", { id: "sbox-1-40" }, base],
    ["title is missing", { title: undefined }, { ...base, title: undefined }],
    ["title is null", { title: null }, { ...base, title: undefined }],
    ["title is a number", { title: 1 }, { ...base, title: undefined }],
    ["folderId is a string", { folderId: "sbox-1-30" }, { ...base, folderId: "sbox-1-30" }],
    ["folderId is missing", { folderId: undefined }, noFolder],
    ["folderId is null", { folderId: null }, noFolder],
    ["folderId is empty", { folderId: "" }, noFolder],
    ["folderId is a boolean", { folderId: true }, noFolder],
    ["fileExst is missing", { fileExst: undefined }, noExst],
    ["fileExst is null", { fileExst: null }, noExst],
    ["fileExst is empty", { fileExst: "" }, noExst],
    ["fileExst is a number", { fileExst: 1 }, noExst],
    ["webUrl is missing", { webUrl: undefined }, noUrl],
    ["webUrl is null", { webUrl: null }, noUrl],
    ["webUrl is empty", { webUrl: "" }, noUrl],
    ["webUrl is a number", { webUrl: 1 }, noUrl],
  ])("maps the DTO when %s", async (_case, overrides, expected) => {
    await expect(readFile(fileDto(overrides))).resolves.toStrictEqual(expected);
  });

  it.each([
    ["id is missing", fileDto({ id: undefined })],
    ["id is null", fileDto({ id: null })],
    ["id is empty", fileDto({ id: "" })],
    ["id is a boolean", fileDto({ id: false })],
    ["response is missing", { status: 0 }],
    ["response is null", { response: null }],
    ["the body is a string", "x"],
  ])("rejects the answer as one of an unknown shape when %s", async (_case, body) => {
    await expect(readFile(body)).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
      message: "GET files/file/{id}: the answer has an unexpected shape",
    });
  });

  it("answers undefined for a 404", async () => {
    upstream.use(http.get(`${FILES}/file/40`, () => HttpResponse.json({}, { status: 404 })));

    await expect(getFileInfo("40")).resolves.toBeUndefined();
  });

  it.each([403, 500])("rejects a %i with a DocspaceApiHttpError", async (status) => {
    upstream.use(http.get(`${FILES}/file/40`, () => HttpResponse.json({}, { status })));

    expect(await failure(getFileInfo("40"))).toMatchObject({
      status,
      url: `${FILES}/file/40`,
    });
  });
});

describe("getFolderInfoCached", () => {
  it("reads a folder once per request, however many readers ask", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    const [a, b] = await inRequest(() =>
      Promise.all([getFolderInfoCached("30"), getFolderInfoCached("30")]),
    );

    expect(a).toStrictEqual({ title: "Marketing", roomType: 5, canCreate: true });
    expect(b).toBe(a);
    expect(upstream.seen).toHaveLength(1);
  });

  it("reads a folder again in another request", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    await inRequest(() => getFolderInfoCached("30"));
    await inRequest(() => getFolderInfoCached("30"));

    expect(upstream.seen).toHaveLength(2);
  });

  it("reads a folder every time outside a request", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    await getFolderInfoCached("30");
    await getFolderInfoCached("30");

    expect(upstream.seen).toHaveLength(2);
  });

  it("keeps a 404 for the rest of the request", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json({}, { status: 404 })));

    const results = await inRequest(async () => [
      await getFolderInfoCached("30"),
      await getFolderInfoCached("30"),
    ]);

    expect(results).toEqual([undefined, undefined]);
    expect(upstream.seen).toHaveLength(1);
  });

  it("reads a folder again after a failed read in the same request", async () => {
    let calls = 0;
    upstream.use(
      http.get(`${FILES}/folder/30`, () =>
        ++calls === 1 ? HttpResponse.json({}, { status: 500 }) : HttpResponse.json(folderDto()),
      ),
    );

    const results = await inRequest(async () => [
      await failure(getFolderInfoCached("30")),
      await getFolderInfoCached("30"),
    ]);

    expect(results[0]).toBeInstanceOf(DocspaceApiHttpError);
    expect(results[1]).toMatchObject({ title: "Marketing" });
    expect(upstream.seen).toHaveLength(2);
  });

  it.each([
    ["the round's entity", snapshotWithFolders({ id: "30", folder: { title: "Snap" } })],
    [
      "the round's context entity",
      snapshotWithFolders(
        { id: "7", folder: { title: "Other" } },
        { id: "30", folder: { title: "Snap" } },
      ),
    ],
  ])("serves %s from the primed snapshot without a read", async (_case, snapshot) => {
    await expect(inRequest(() => getFolderInfoCached("30"), { snapshot })).resolves.toEqual({
      title: "Snap",
    });
    expect(upstream.seen).toHaveLength(0);
  });

  it.each([
    ["another folder", snapshotWithFolders({ id: "7", folder: { title: "Other" } })],
    ["a folder the aggregate could not read", snapshotWithFolders({ id: "30", folder: undefined })],
    ["no folder at all", snapshotWithFolders()],
  ])("reads the Files API when the snapshot describes %s", async (_case, snapshot) => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    await expect(inRequest(() => getFolderInfoCached("30"), { snapshot })).resolves.toMatchObject({
      title: "Marketing",
    });
    expect(upstream.seen).toHaveLength(1);
  });
});

describe("folderSourceType", () => {
  it.each([
    [{ folderType: 15 }, "Form"],
    [{ roomType: 1 }, "Form"],
    [{ folderType: 15, roomType: 5 }, "Form"],
    [{ folderType: 16 }, "Room"],
    [{ folderType: 19 }, "Room"],
    [{ folderType: 22 }, "Room"],
    [{ folderType: 29 }, "Room"],
    [{ roomType: 2 }, "Room"],
    [{ roomType: 5 }, "Room"],
    [{ roomType: 6 }, "Room"],
    [{ roomType: 8 }, "Room"],
    [{ folderType: 40, roomType: 12 }, "Folder"],
    [{ folderType: 14 }, "Folder"],
    [{ roomType: 3 }, "Folder"],
    [{}, "Folder"],
  ] as const)("classifies %j as %s", (info, expected) => {
    expect(folderSourceType(info)).toBe(expected);
  });
});

describe("canTakeUpload", () => {
  it.each([
    ["no folder", undefined, false],
    ["a section root", { sectionRoot: true, canCreate: true }, false],
    ["the Rooms root by its type", { folderType: 14 }, false],
    ["the Archive", { folderType: 20 }, false],
    ["the room templates", { folderType: 30 }, false],
    ["the AI agents root", { folderType: 34 }, false],
    ["the Forms root", { folderType: 36 }, false],
    ["the AI rooms root", { folderType: 41 }, false],
    ["the private rooms root", { folderType: 42 }, false],
    ["a folder without the Create right", { canCreate: false }, false],
    ["a folder with the Create right", { canCreate: true }, true],
    ["a folder without a security block", {}, true],
    ["My documents", { folderType: 5, canCreate: true }, true],
    ["a section root that is not marked", { sectionRoot: false }, true],
  ])("answers for %s", (_case, info, expected) => {
    expect(canTakeUpload(info)).toBe(expected);
  });
});

describe("safeResolveSource", () => {
  it.each([undefined, ""])("answers undefined without a read for the id %j", async (id) => {
    await expect(safeResolveSource(id)).resolves.toBeUndefined();
    await expect(safeResolveSource(id, "file")).resolves.toBeUndefined();
    expect(upstream.seen).toHaveLength(0);
  });

  it.each([
    [{ type: 15, roomType: 1 }, "Form"],
    [{ roomType: 5 }, "Room"],
    [{ roomType: undefined }, "Folder"],
  ])("names the folder %j a %s with its title", async (overrides, type) => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto(overrides))));

    await expect(safeResolveSource("30")).resolves.toStrictEqual({
      id: "30",
      type,
      title: "Marketing",
    });
  });

  it("names a file a File with its title", async () => {
    upstream.use(http.get(`${FILES}/file/40`, () => HttpResponse.json(fileDto())));

    await expect(safeResolveSource("40", "file")).resolves.toStrictEqual({
      id: "40",
      type: "File",
      title: "Report.docx",
    });
  });

  it.each([
    ["folder", "folder/30"],
    ["file", "file/30"],
  ] as const)("answers undefined for a %s that is gone", async (kind, path) => {
    upstream.use(http.get(`${FILES}/${path}`, () => HttpResponse.json({}, { status: 404 })));

    await expect(safeResolveSource("30", kind)).resolves.toBeUndefined();
  });

  it.each([
    ["folder", "folder/30", 500],
    ["file", "file/30", 403],
  ] as const)(
    "answers undefined and warns when the %s cannot be read",
    async (kind, path, status) => {
      upstream.use(
        http.get(`${FILES}/${path}`, () => HttpResponse.json({}, { status, statusText: "R" })),
      );

      await expect(safeResolveSource("30", kind)).resolves.toBeUndefined();
      expect(logger.warn).toHaveBeenCalledWith(
        `source entity fetch failed (${kind} 30): DocSpace API ${status} R for ${FILES}/${path}`,
      );
    },
  );

  it("reads a file once per request", async () => {
    upstream.use(http.get(`${FILES}/file/40`, () => HttpResponse.json(fileDto())));

    await inRequest(async () => {
      await safeResolveSource("40", "file");
      await safeResolveSource("40", "file");
    });

    expect(upstream.seen).toHaveLength(1);
  });

  it("reads a file again after a failed read in the same request", async () => {
    let calls = 0;
    upstream.use(
      http.get(`${FILES}/file/40`, () =>
        ++calls === 1 ? HttpResponse.json({}, { status: 500 }) : HttpResponse.json(fileDto()),
      ),
    );

    const results = await inRequest(async () => [
      await safeResolveSource("40", "file"),
      await safeResolveSource("40", "file"),
    ]);

    expect(results).toEqual([undefined, { id: "40", type: "File", title: "Report.docx" }]);
    expect(upstream.seen).toHaveLength(2);
  });

  it("keeps files and folders of the same id apart", async () => {
    upstream.use(
      http.get(`${FILES}/file/30`, () => HttpResponse.json(fileDto({ id: 30 }))),
      http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())),
    );

    const results = await inRequest(async () => [
      await safeResolveSource("30", "file"),
      await safeResolveSource("30", "folder"),
    ]);

    expect(results).toEqual([
      { id: "30", type: "File", title: "Report.docx" },
      { id: "30", type: "Room", title: "Marketing" },
    ]);
  });
});

describe("primeSourceMeta", () => {
  it("remembers the resolved source on the request", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    const result = await inRequest(async () => {
      const source = await primeSourceMeta("30");
      return { source, stored: getSourceMeta() };
    });

    expect(result).toEqual({
      source: { id: "30", type: "Room", title: "Marketing" },
      stored: { id: "30", type: "Room", title: "Marketing" },
    });
  });

  it("clears a source an earlier resolution left when the new one resolves to nothing", async () => {
    const stored = await inRequest(async () => {
      setSourceMeta({ id: "1", type: "File" });
      await primeSourceMeta(undefined, "file");
      return getSourceMeta();
    });

    expect(stored).toBeUndefined();
  });
});

// FoldersController.GetMyFolder: `GET api/2.0/files/@my?count=1`.
describe("getMyDocumentsFolderId", () => {
  const my = (current: unknown) => ({ response: { files: [], folders: [], current, total: 0 } });
  const readMy = async (body: JsonBodyType): Promise<unknown> => {
    upstream.use(http.get(`${FILES}/@my`, () => HttpResponse.json(body)));
    return getMyDocumentsFolderId();
  };

  it("answers the id of the current folder, asking for one entry", async () => {
    await expect(
      inRequest(() => readMy(my({ id: 4, title: "My documents", rootFolderType: 5 })), {
        headers: CALLER,
      }),
    ).resolves.toBe("4");
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/@my?count=1");
    expect(upstream.seen[0]?.headers).toMatchObject(CALLER);
  });

  it("answers a string id as it is", async () => {
    await expect(readMy(my({ id: "sbox-4" }))).resolves.toBe("sbox-4");
  });

  it.each([
    ["current is missing", { response: { files: [] } }],
    ["current is null", my(null)],
    ["current is a list", my([{ id: 4 }])],
    ["current.id is missing", my({ title: "My documents" })],
    ["current.id is null", my({ id: null })],
    ["current.id is empty", my({ id: "" })],
    ["current.id is a boolean", my({ id: true })],
    ["response is missing", { status: 0 }],
    ["the body is a list", [my({ id: 4 })]],
  ])("answers undefined and warns when %s", async (_case, body) => {
    await expect(readMy(body)).resolves.toBeUndefined();
    expect(logger.warn).toHaveBeenCalledWith(
      `getMyDocumentsFolderId: no current.id in the response from ${FILES}/@my?count=1`,
    );
  });

  it.each([403, 404])("answers undefined and warns for a %i", async (status) => {
    upstream.use(
      http.get(`${FILES}/@my`, () => HttpResponse.json({}, { status, statusText: "Reason" })),
    );

    await expect(getMyDocumentsFolderId()).resolves.toBeUndefined();
    expect(logger.warn).toHaveBeenCalledWith(
      `getMyDocumentsFolderId: ${FILES}/@my?count=1 -> ${status} Reason`,
    );
  });

  it.each([400, 401, 500, 503])(
    "rejects a %i with a DocspaceApiHttpError, not as a missing section",
    async (status) => {
      upstream.use(
        http.get(`${FILES}/@my`, () =>
          HttpResponse.json({ error: "secret" }, { status, statusText: "Reason" }),
        ),
      );

      const error = await failure(getMyDocumentsFolderId());

      expect(error).toBeInstanceOf(DocspaceApiHttpError);
      expect(error).toMatchObject({ status, url: `${FILES}/@my?count=1` });
      expect((error as Error).message).not.toContain("secret");
      expect(logger.warn).not.toHaveBeenCalled();
    },
  );

  describe("on fake timers", () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    it("rejects once the Files API has not answered for 300000 ms", async () => {
      upstream.use(
        http.get(`${FILES}/@my`, async () => {
          await delay("infinite");
          return HttpResponse.json(my({ id: 4 }));
        }),
      );
      const pending = failure(getMyDocumentsFolderId());

      await vi.advanceTimersByTimeAsync(300_000);

      expect(String(await pending)).toContain("upstream timeout after 300000ms");
    });
  });

  it("rejects a dropped connection", async () => {
    upstream.use(http.get(`${FILES}/@my`, () => HttpResponse.error()));

    expect(await failure(getMyDocumentsFolderId())).toBeInstanceOf(TypeError);
  });

  it("does not count as a read of the Files API", async () => {
    upstream.use(http.get(`${FILES}/@my`, () => HttpResponse.json(my({ id: 4 }))));

    await expect(
      inRequest(async () => {
        await getMyDocumentsFolderId();
        return getFilesApiReadCount();
      }),
    ).resolves.toBe(0);
  });
});

describe("resolveAiProjectEntityId", () => {
  it.each([undefined, "", "abc", "12a", "-1", "1.5", "sbox-1"])(
    "answers undefined without a read for %j",
    async (id) => {
      await expect(resolveAiProjectEntityId(id)).resolves.toBeUndefined();
      expect(upstream.seen).toHaveLength(0);
    },
  );

  it.each([
    ["a room type of an AI project", { roomType: 12 }, "37"],
    ["a folder type of an AI project", { type: 40, roomType: undefined }, "37"],
    ["another room type", { roomType: 5 }, undefined],
    ["no type at all", { roomType: undefined }, undefined],
    ["the room type as a string", { roomType: "12" }, undefined],
  ])("answers for a folder with %s", async (_case, overrides, expected) => {
    upstream.use(
      http.get(`${FILES}/folder/37`, () => HttpResponse.json(folderDto({ id: 37, ...overrides }))),
    );

    await expect(resolveAiProjectEntityId("37")).resolves.toBe(expected);
  });

  it("answers undefined for a folder that is gone", async () => {
    upstream.use(http.get(`${FILES}/folder/37`, () => HttpResponse.json({}, { status: 404 })));

    await expect(resolveAiProjectEntityId("37")).resolves.toBeUndefined();
  });

  it.each([403, 500])("rejects a %i", async (status) => {
    upstream.use(http.get(`${FILES}/folder/37`, () => HttpResponse.json({}, { status })));

    expect(await failure(resolveAiProjectEntityId("37"))).toMatchObject({ status });
  });

  it("serves a project of the snapshot without a read", async () => {
    const snapshot = snapshotWithFolders({ id: "37", folder: { folderType: 40 } });

    await expect(inRequest(() => resolveAiProjectEntityId("37"), { snapshot })).resolves.toBe("37");
    expect(upstream.seen).toHaveLength(0);
  });
});

describe("assertEntityAccessible", () => {
  it.each([undefined, ""])("passes the global scope %j without a read", async (id) => {
    await expect(assertEntityAccessible(id)).resolves.toBeUndefined();
    expect(upstream.seen).toHaveLength(0);
  });

  it("passes a folder the caller can read", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json(folderDto())));

    await expect(assertEntityAccessible("30")).resolves.toBeUndefined();
  });

  it("passes a folder the snapshot describes without a read", async () => {
    const snapshot = snapshotWithFolders({ id: "30", folder: {} });

    await expect(inRequest(() => assertEntityAccessible("30"), { snapshot })).resolves.toBe(
      undefined,
    );
    expect(upstream.seen).toHaveLength(0);
  });

  it("refuses a folder that is gone with 404", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json({}, { status: 404 })));

    const error = await failure(assertEntityAccessible("30"));

    expect(error).toBeInstanceOf(Error);
    expect(error).toMatchObject({ message: 'Entity "30" not found', status: 404, expose: true });
  });

  it("refuses a folder the caller may not open with 403 (Bug 83998)", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json({}, { status: 403 })));

    const error = await failure(assertEntityAccessible("30"));

    expect(error).not.toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({
      message: 'Entity "30" is not accessible',
      status: 403,
      expose: true,
    });
  });

  it.each([401, 500, 503])("passes on a %i as the DocspaceApiHttpError it is", async (status) => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json({}, { status })));

    const error = await failure(assertEntityAccessible("30"));

    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({ status });
  });

  it("passes on a dropped connection as it is", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.error()));

    expect(await failure(assertEntityAccessible("30"))).toBeInstanceOf(TypeError);
  });

  it("answers 502, not 404, for a folder whose DTO cannot be read", async () => {
    upstream.use(http.get(`${FILES}/folder/30`, () => HttpResponse.json({ response: {} })));

    expect(await failure(assertEntityAccessible("30"))).toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });
});
