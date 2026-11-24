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
import { describe, expect, it } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { getChatContextMisses, getRoundThreadId } from "../../requestContext.js";
import { DocspaceApiHttpError } from "../docspaceFilesApi.js";
import { dtoToThread, HttpThreadsStorage } from "../threadsStorage.js";
import {
  inRound,
  PROFILE_ID,
  snapshotOf,
  THREAD_ID,
} from "../../__tests__/fixtures/chatContext.js";

// The real httpClient and Files API client over a stubbed network.
// products/ASC.AI/Server/Api/ThreadStorageController.cs: POST threads, GET threads/{id},
// GET threads, PUT threads/{id}, PATCH threads/{id}/touch, DELETE threads/{id}.
// GET api/2.0/files/folder/{id}: FoldersController.GetFolderInfo (products/ASC.Files).
const upstream = useUpstream();
const storage = new HttpThreadsStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

/** `ThreadDto` (products/ASC.AI/Server/Models/ResponseDto/ThreadDto.cs). */
const threadDto = (overrides: Record<string, unknown> = {}) => ({
  id: THREAD_ID,
  title: "Quarterly report",
  profileId: PROFILE_ID,
  entityId: "42",
  lastEditDate: 1_700_000_001_000,
  createdAt: 1_700_000_000_500,
  ...overrides,
});

const thread = {
  threadId: THREAD_ID,
  title: "Quarterly report",
  lastEditDate: 1_700_000_001_000,
  profileId: PROFILE_ID,
};

/** The Files API folder of an AI project (`FolderDto`, `roomType` 12) or of a plain room. */
function folder(id: number, roomType: number) {
  upstream.use(
    http.get(`*/api/2.0/files/folder/${id}`, () =>
      HttpResponse.json({ response: { id, title: `Folder ${id}`, roomType } }),
    ),
  );
}

const MISSING = Symbol("missing");
function dtoWith(field: string, value: unknown): Record<string, unknown> {
  const dto: Record<string, unknown> = threadDto();
  if (value === MISSING) {
    delete dto[field];
  } else {
    dto[field] = value;
  }
  return dto;
}

describe("dtoToThread", () => {
  it("maps a full ThreadDto and leaves entityId and createdAt out", () => {
    expect(dtoToThread(threadDto())).toEqual(thread);
  });

  it.each([null, undefined, "t", 1, [], false])(
    "refuses a DTO that is not an object (%j)",
    (raw) => {
      expect(dtoToThread(raw)).toBeNull();
    },
  );

  it.each([
    ["id", MISSING],
    ["id", null],
    ["id", 7],
    ["title", MISSING],
    ["title", null],
    ["title", 7],
    ["lastEditDate", MISSING],
    ["lastEditDate", null],
    ["lastEditDate", "1700000001000"],
    ["lastEditDate", Number.NaN],
  ])("refuses a DTO whose %s is %s", (field, value) => {
    expect(dtoToThread(dtoWith(field, value))).toBeNull();
  });

  it("takes an empty id and an empty title", () => {
    expect(dtoToThread(threadDto({ id: "", title: "" }))).toMatchObject({
      threadId: "",
      title: "",
    });
  });

  it.each([
    ["missing", MISSING],
    ["null", null],
    ["a number", 5],
  ])("leaves profileId out when it is %s", (_name, value) => {
    const { profileId: _p, ...rest } = thread;
    expect(dtoToThread(dtoWith("profileId", value))).toEqual(rest);
  });
});

describe("create", () => {
  it("posts the thread of no project and names it the thread of the round", async () => {
    upstream.use(http.post("*/internal/ai/threads", () => docspace(threadDto())));

    const result = await inRequestContext({}, async () => ({
      created: await storage.create("Quarterly report", PROFILE_ID),
      round: getRoundThreadId(),
    }));

    expect(result).toEqual({ created: thread, round: THREAD_ID });
    expect(upstream.seen).toMatchObject([
      {
        method: "POST",
        path: "/internal/ai/threads",
        body: { title: "Quarterly report", profileId: PROFILE_ID, entityId: null },
      },
    ]);
  });

  it("binds the thread to the AI project it was started in", async () => {
    folder(42, 12);
    upstream.use(http.post("*/internal/ai/threads", () => docspace(threadDto())));

    await storage.create("T", undefined, "42");

    expect(upstream.seen.map((r) => [r.method, r.path])).toEqual([
      ["GET", "/api/2.0/files/folder/42"],
      ["POST", "/internal/ai/threads"],
    ]);
    expect(upstream.seen[1]?.body).toEqual({ title: "T", profileId: null, entityId: "42" });
  });

  it.each([
    ["a plain room", () => folder(42, 2)],
    [
      "a folder that does not exist",
      () => upstream.use(http.get("*/api/2.0/files/folder/42", () => docspaceError(404))),
    ],
  ])("binds no project for %s", async (_name, arrange) => {
    arrange();
    upstream.use(http.post("*/internal/ai/threads", () => docspace(threadDto())));
    await storage.create("T", undefined, "42");
    expect(upstream.seen[1]?.body).toEqual({ title: "T", profileId: null, entityId: null });
  });

  it.each([403, 500])(
    "refuses the thread when the folder answers %i, before posting",
    async (status) => {
      upstream.use(http.get("*/api/2.0/files/folder/42", () => docspaceError(status)));
      const error = await storage.create("T", undefined, "42").catch((e: unknown) => e);
      expect(error).toBeInstanceOf(DocspaceApiHttpError);
      expect((error as DocspaceApiHttpError).status).toBe(status);
      expect(upstream.seen.map((r) => r.method)).toEqual(["GET"]);
    },
  );

  it.each([
    ["an empty answer", noContent()],
    ["a DTO without title", docspace({ id: "t", lastEditDate: 1 })],
  ])("throws on %s and names no thread of the round", async (_name, answer) => {
    upstream.use(http.post("*/internal/ai/threads", () => answer));
    const result = await inRequestContext({}, async () => ({
      error: await storage.create("T").catch((e: unknown) => e),
      round: getRoundThreadId(),
    }));
    expect(result.error).toBeInstanceOf(UpstreamShapeError);
    expect(result.round).toBeUndefined();
  });

  it("rethrows a refused post", async () => {
    upstream.use(http.post("*/internal/ai/threads", () => docspaceError(400)));
    await expect(storage.create("T")).rejects.toMatchObject({ status: 400 });
  });
});

describe("readById", () => {
  it("reads the thread by its encoded id", async () => {
    upstream.use(http.get("*/internal/ai/threads/:id", () => docspace(threadDto())));
    await expect(storage.readById("a/b")).resolves.toEqual(thread);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/threads/a%2Fb");
  });

  it("reads a 404 as absent and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/threads/:id", () => docspaceError(404)));
    await expect(storage.readById("t")).resolves.toBeNull();
    upstream.use(http.get("*/internal/ai/threads/:id", () => docspaceError(500)));
    await expect(storage.readById("t")).rejects.toMatchObject({ status: 500 });
  });

  it.each([
    ["an empty answer", noContent()],
    ["a DTO without title", docspace({ id: "t", lastEditDate: 1 })],
  ])("rejects %s as an answer of an unknown shape", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/threads/:id", () => answer));
    await expect(storage.readById("t")).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });

  it("serves the round's thread from the snapshot, null included, without a request", async () => {
    const requested = { threadId: "t", entityId: undefined, contextEntityId: undefined };
    const result = await inRound(snapshotOf({ requested, thread }), async () => ({
      found: await storage.readById("t"),
      misses: getChatContextMisses(),
    }));
    expect(result).toEqual({ found: thread, misses: [] });
    await expect(
      inRound(snapshotOf({ requested }), () => storage.readById("t")),
    ).resolves.toBeNull();
    expect(upstream.seen).toEqual([]);
  });

  it("asks for another thread and for a stale slice, counting the miss", async () => {
    upstream.use(http.get("*/internal/ai/threads/:id", () => docspace(threadDto())));
    const requested = { threadId: "t", entityId: undefined, contextEntityId: undefined };
    const misses = await inRound(snapshotOf({ requested, thread }), async () => {
      await storage.readById("u");
      return getChatContextMisses();
    });
    expect(misses).toEqual(["threads.readById(u)"]);
    await inRound(snapshotOf({ requested, thread, stale: new Set(["thread"]) }), () =>
      storage.readById("t"),
    );
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/threads/u",
      "/internal/ai/threads/t",
    ]);
  });
});

describe("readAll", () => {
  /** `ThreadsPageDto`, its cursor `ThreadsCursorDto` (products/ASC.AI/Server/Models). */
  const page = (ids: string[], cursor: unknown = null) => ({
    items: ids.map((id) => threadDto({ id })),
    cursor,
  });

  it("reads one page with the search text and the cursor", async () => {
    upstream.use(http.get("*/internal/ai/threads", () => docspace(page(["a"]))));

    const items = await storage.readAll(
      undefined,
      20,
      { lastEditDate: 5, id: "c", threadId: "x" } as never,
      "q a",
    );

    expect(items.map((t) => t.threadId)).toEqual(["a"]);
    expect(upstream.seen[0]?.path).toBe(
      "/internal/ai/threads?count=20&query=q+a&cursor.lastEditDate=5&cursor.id=c",
    );
  });

  it("takes the cursor's threadId when its id is empty", async () => {
    upstream.use(http.get("*/internal/ai/threads", () => docspace(page([]))));
    await storage.readAll(undefined, 20, { lastEditDate: 5, id: "", threadId: "x" } as never, "");
    expect(upstream.seen[0]?.path).toBe(
      "/internal/ai/threads?count=20&cursor.lastEditDate=5&cursor.id=x",
    );
  });

  it("answers no thread without a request for a cursor past the end", async () => {
    await expect(
      storage.readAll(undefined, 20, { lastEditDate: null, id: "c", threadId: "c" } as never),
    ).resolves.toEqual([]);
    expect(upstream.seen).toEqual([]);
  });

  it("walks every page of an AI project's threads", async () => {
    folder(42, 12);
    upstream.use(
      http.get("*/internal/ai/threads", ({ request }) =>
        new URL(request.url).searchParams.has("cursor.id")
          ? docspace(page(["c"]))
          : docspace(page(["a", "b"], { lastEditDate: 9, id: "b" })),
      ),
    );

    const items = await storage.readAll("42");

    expect(items.map((t) => t.threadId)).toEqual(["a", "b", "c"]);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/api/2.0/files/folder/42",
      "/internal/ai/threads?count=100&entityId=42",
      "/internal/ai/threads?count=100&entityId=42&cursor.lastEditDate=9&cursor.id=b",
    ]);
  });

  it("lists the threads of no project for a folder the caller cannot open, and fails on a 500 of the folder", async () => {
    upstream.use(
      http.get("*/api/2.0/files/folder/42", () => docspaceError(403)),
      http.get("*/internal/ai/threads", () => docspace(page(["a"]))),
    );
    await expect(storage.readAll("42", 5)).resolves.toHaveLength(1);
    expect(upstream.seen[1]?.path).toBe("/internal/ai/threads?count=5");

    upstream.use(http.get("*/api/2.0/files/folder/42", () => docspaceError(500)));
    await expect(storage.readAll("42", 5)).rejects.toBeInstanceOf(DocspaceApiHttpError);
  });

  it.each([
    ["a bare list", [threadDto({ id: "a" })], ["a"]],
    ["an empty page", { items: [], cursor: null }, []],
    ["a page with invalid items", { items: [null, { id: "x" }, threadDto({ id: "b" })] }, ["b"]],
  ])("reads %s", async (_name, body, ids) => {
    upstream.use(http.get("*/internal/ai/threads", () => docspace(body)));
    await expect(storage.readAll()).resolves.toEqual(
      ids.map((id) => ({ ...thread, threadId: id })),
    );
  });

  // ThreadsPageDto carries `items`; a page that cannot be read is unknown, not "no threads".
  it.each([
    ["a page without items", { cursor: null }],
    ["a page whose items are not a list", { items: "a" }],
    ["an empty object", {}],
    ["null", null],
    ["a string", "x"],
  ])("throws UpstreamShapeError for %s", async (_name, body) => {
    upstream.use(http.get("*/internal/ai/threads", () => docspace(body)));
    await expect(storage.readAll()).rejects.toBeInstanceOf(UpstreamShapeError);
  });

  it.each([
    ["a cursor without id", { lastEditDate: 1 }],
    ["a cursor with a text date", { lastEditDate: "1", id: "b" }],
  ])("stops after the first page on %s", async (_name, cursor) => {
    upstream.use(http.get("*/internal/ai/threads", () => docspace(page(["a"], cursor))));
    await storage.readAll();
    expect(upstream.seen).toHaveLength(1);
  });
});

describe("the writes", () => {
  it("update without a title does nothing", async () => {
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.update("t"));
    expect(upstream.seen).toEqual([]);
    expect(snapshot.stale.size).toBe(0);
  });

  it("update puts the title and marks the thread stale", async () => {
    upstream.use(http.put("*/internal/ai/threads/:id", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.update("a/b", ""));
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/threads/a%2Fb", body: { title: "" } },
    ]);
    expect([...snapshot.stale]).toEqual(["thread"]);
  });

  it.each([
    ["no updates", undefined, { lastEditDate: 7, profileId: null, clearProfile: false }],
    ["an empty update", {}, { lastEditDate: 7, profileId: null, clearProfile: false }],
    [
      "profileId undefined",
      { profileId: undefined },
      { lastEditDate: 7, profileId: null, clearProfile: false },
    ],
    [
      "profileId null",
      { profileId: null },
      { lastEditDate: 7, profileId: null, clearProfile: true },
    ],
    ["a profileId", { profileId: "p" }, { lastEditDate: 7, profileId: "p", clearProfile: false }],
  ])("touch with %s patches %j", async (_name, updates, body) => {
    upstream.use(http.patch("*/internal/ai/threads/:id/touch", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.touch("a/b", 7, updates));
    expect(upstream.seen).toMatchObject([
      { method: "PATCH", path: "/internal/ai/threads/a%2Fb/touch", body },
    ]);
    expect([...snapshot.stale]).toEqual(["thread"]);
  });

  it("update and touch leave the slice fresh when refused", async () => {
    upstream.use(
      http.put("*/internal/ai/threads/:id", () => docspaceError(404)),
      http.patch("*/internal/ai/threads/:id/touch", () => docspaceError(404)),
    );
    const snapshot = snapshotOf();
    await expect(inRound(snapshot, () => storage.update("t", "x"))).rejects.toMatchObject({
      status: 404,
    });
    await expect(inRound(snapshot, () => storage.touch("t", 1))).rejects.toMatchObject({
      status: 404,
    });
    expect(snapshot.stale.size).toBe(0);
  });

  it("delete marks the slice stale first, treats a 404 as done and rethrows a 500", async () => {
    upstream.use(http.delete("*/internal/ai/threads/:id", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.delete("a/b"));
    expect(upstream.seen).toMatchObject([{ method: "DELETE", path: "/internal/ai/threads/a%2Fb" }]);
    expect([...snapshot.stale]).toEqual(["thread"]);

    upstream.use(http.delete("*/internal/ai/threads/:id", () => docspaceError(404)));
    await expect(storage.delete("t")).resolves.toBeUndefined();

    upstream.use(http.delete("*/internal/ai/threads/:id", () => docspaceError(500)));
    const failed = snapshotOf();
    await expect(inRound(failed, () => storage.delete("t"))).rejects.toMatchObject({ status: 500 });
    expect([...failed.stale]).toEqual(["thread"]);
  });
});
