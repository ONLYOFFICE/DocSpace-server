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

import { describe, expect, it } from "vitest";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { getChatContextMisses } from "../../requestContext.js";
import { AiServiceHttpError } from "../httpClient.js";
import { dtoToMessage, HttpMessagesStorage } from "../messagesStorage.js";
import { inRound, MESSAGE_ID, snapshotOf, THREAD_ID } from "./chatContext.fixtures.js";

// products/ASC.AI/Server/Api/MessageStorageController.cs: POST threads/{threadId}/messages,
// GET messages/{id}, GET threads/{threadId}/messages, PUT messages/{id}, DELETE messages/{id},
// DELETE threads/{threadId}/messages.
const upstream = useUpstream();
const storage = new HttpMessagesStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

/** `MessageDto` (products/ASC.AI/Server/Models/ResponseDto/MessageDto.cs). */
const messageDto = (overrides: Record<string, unknown> = {}) => ({
  id: MESSAGE_ID,
  threadId: THREAD_ID,
  contents:
    '{"role":"assistant","content":[{"type":"text","text":"Hello"}],"status":{"type":"complete"}}',
  timestamp: 1_700_000_002_000,
  ...overrides,
});

const message = {
  role: "assistant",
  content: [{ type: "text", text: "Hello" }],
  status: { type: "complete" },
  id: MESSAGE_ID,
  createdAt: new Date(1_700_000_002_000),
};

const MISSING = Symbol("missing");
function dtoWith(field: string, value: unknown): Record<string, unknown> {
  const dto: Record<string, unknown> = messageDto();
  if (value === MISSING) {
    delete dto[field];
  } else {
    dto[field] = value;
  }
  return dto;
}

describe("dtoToMessage", () => {
  it("maps a full MessageDto: the contents spread, the id, the timestamp as a Date, no threadId", () => {
    expect(dtoToMessage(messageDto())).toEqual(message);
  });

  it.each([null, undefined, "m", 1, [], true])(
    "refuses a DTO that is not an object (%j)",
    (raw) => {
      expect(dtoToMessage(raw)).toBeNull();
    },
  );

  it.each([
    ["missing", MISSING],
    ["null", null],
    ["a number", 5],
  ])("refuses a DTO whose id is %s", (_name, value) => {
    expect(dtoToMessage(dtoWith("id", value))).toBeNull();
  });

  it("takes an empty id", () => {
    expect(dtoToMessage(dtoWith("id", ""))).toMatchObject({ id: "" });
  });

  it.each([
    ["missing", MISSING],
    ["null", null],
    ["a string", "1700000002000"],
    ["not finite", Number.POSITIVE_INFINITY],
  ])("leaves createdAt out when the timestamp is %s", (_name, value) => {
    const { createdAt: _c, ...rest } = message;
    expect(dtoToMessage(dtoWith("timestamp", value))).toEqual(rest);
  });

  it.each([
    ["missing", MISSING, {}],
    ["null", null, {}],
    ["a number", 5, {}],
    ["a list", [1], {}],
    ["an object", { role: "user" }, { role: "user" }],
    ["an empty string", "", { content: "" }],
    ["text that is not JSON", "plain answer", { content: "plain answer" }],
    ["JSON of a list", "[1,2]", { content: "[1,2]" }],
    ["JSON of a number", "42", { content: "42" }],
    ["JSON null", "null", { content: "null" }],
  ])("reads contents %s", (_name, value, body) => {
    expect(dtoToMessage(dtoWith("contents", value))).toEqual({
      ...body,
      id: MESSAGE_ID,
      createdAt: new Date(1_700_000_002_000),
    });
  });

  it("lets the DTO's id win over an id inside the contents", () => {
    expect(dtoToMessage(dtoWith("contents", '{"id":"other","role":"user"}'))).toMatchObject({
      id: MESSAGE_ID,
    });
  });

  it("keeps a createdAt of the contents when the DTO has no timestamp, and replaces it when it has one", () => {
    const contents = '{"role":"user","createdAt":"2020-01-01T00:00:00.000Z"}';
    const withoutTimestamp = { id: "m", contents };
    expect(dtoToMessage(withoutTimestamp)).toEqual({
      role: "user",
      createdAt: "2020-01-01T00:00:00.000Z",
      id: "m",
    });
    expect(dtoToMessage({ ...withoutTimestamp, timestamp: 5 })).toEqual({
      role: "user",
      createdAt: new Date(5),
      id: "m",
    });
  });
});

describe("create", () => {
  it("posts the message as a JSON string and answers the stored one", async () => {
    upstream.use(http.post("*/internal/ai/threads/:id/messages", () => docspace(messageDto())));

    const created = await storage.create("t/1", {
      role: "assistant",
      content: [{ type: "text", text: "Hello" }],
    });

    expect(created).toEqual(message);
    expect(upstream.seen).toMatchObject([
      {
        method: "POST",
        path: "/internal/ai/threads/t%2F1/messages",
        body: { contents: '{"role":"assistant","content":[{"type":"text","text":"Hello"}]}' },
      },
    ]);
  });

  it.each([
    ["an empty answer", noContent()],
    ["a DTO without id", docspace({ contents: "{}" })],
    ["a list", docspace([messageDto()])],
  ])("throws on %s, after marking the slice stale", async (_name, answer) => {
    upstream.use(http.post("*/internal/ai/threads/:id/messages", () => answer));
    const snapshot = snapshotOf();

    await expect(
      inRound(snapshot, () => storage.create("t", { role: "user", content: [] })),
    ).rejects.toThrow("ai service returned invalid message");
    expect([...snapshot.stale]).toEqual(["messages"]);
  });

  it("rethrows a refused write and leaves the slice fresh", async () => {
    upstream.use(http.post("*/internal/ai/threads/:id/messages", () => docspaceError(404)));
    const snapshot = snapshotOf();

    await expect(
      inRound(snapshot, () => storage.create("t", { role: "user", content: [] })),
    ).rejects.toBeInstanceOf(AiServiceHttpError);
    expect(snapshot.stale.size).toBe(0);
  });
});

describe("readById", () => {
  it("reads the message by its encoded id", async () => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => docspace(messageDto())));
    await expect(storage.readById("m 1")).resolves.toEqual(message);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/messages/m%201");
  });

  it("reads a 404 as absent", async () => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => docspaceError(404)));
    await expect(storage.readById("m")).resolves.toBeNull();
  });

  it.each([400, 403, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => docspaceError(status)));
    await expect(storage.readById("m")).rejects.toMatchObject({ status });
  });

  // DEFECT stor2-3: a GET messages/{id} answer of the wrong shape (no id, an empty body) is read as "the message does not exist" — expected it to be rethrown as unknown, since the C# action answers 404 for an absent message (ts-ai-failure-paths.md §1)
  it.each([
    ["an empty answer", noContent()],
    ["a DTO without id", docspace({ contents: "{}" })],
  ])("reads %s as absent", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => answer));
    await expect(storage.readById("m")).resolves.toBeNull();
  });
});

describe("readThreadId", () => {
  it("answers the threadId of the DTO", async () => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => docspace(messageDto())));
    await expect(storage.readThreadId("m/1")).resolves.toBe(THREAD_ID);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/messages/m%2F1");
  });

  it.each([
    ["threadId missing", docspace({ id: "m" })],
    ["threadId null", docspace(messageDto({ threadId: null }))],
    ["threadId a number", docspace(messageDto({ threadId: 7 }))],
    ["an empty answer", noContent()],
    ["a list", docspace([])],
    ["a 404", docspaceError(404)],
  ])("answers null for %s", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => answer));
    await expect(storage.readThreadId("m")).resolves.toBeNull();
  });

  it("answers an empty threadId as it is", async () => {
    upstream.use(
      http.get("*/internal/ai/messages/:id", () => docspace(messageDto({ threadId: "" }))),
    );
    await expect(storage.readThreadId("m")).resolves.toBe("");
  });

  it.each([403, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/messages/:id", () => docspaceError(status)));
    await expect(storage.readThreadId("m")).rejects.toMatchObject({ status });
  });
});

describe("readByThread", () => {
  /** `MessagesPageDto` (products/ASC.AI/Server/Models/ResponseDto/MessagesPageDto.cs), its cursor `MessagesCursorDto`. */
  const page = (ids: string[], cursor: unknown = null) => ({
    items: ids.map((id) => messageDto({ id })),
    cursor,
  });

  it("reads one page of the given size forward", async () => {
    upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page(["a", "b"]))));

    const items = await storage.readByThread("t/1", 2);

    expect(items.map((m) => m.id)).toEqual(["a", "b"]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/threads/t%2F1/messages?count=2");
  });

  it.each([
    [
      "an ISO createdAt",
      { createdAt: "2026-01-01T00:00:00.000Z", id: "c" },
      "2026-01-01T00%3A00%3A00.000Z",
    ],
    [
      "an epoch-ms createdAt",
      { createdAt: 1_767_225_600_000, id: "c" },
      "2026-01-01T00%3A00%3A00.000Z",
    ],
  ])("sends the cursor with %s backwards when asked", async (_name, cursor, sent) => {
    upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page([]))));

    await storage.readByThread("t", 10, cursor as never, "desc");

    expect(upstream.seen[0]?.path).toBe(
      `/internal/ai/threads/t/messages?count=10&direction=desc&cursor.createdAt=${sent}&cursor.id=c`,
    );
  });

  it.each([
    ["an empty createdAt", { createdAt: "", id: "c" }],
    ["a null createdAt", { createdAt: null, id: "c" }],
  ])("sends no cursor for %s", async (_name, cursor) => {
    upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page([]))));
    await storage.readByThread("t", 10, cursor as never, "asc");
    expect(upstream.seen[0]?.path).toBe("/internal/ai/threads/t/messages?count=10");
  });

  it("walks every page forward for the whole thread, whatever direction is asked", async () => {
    upstream.use(
      http.get("*/internal/ai/threads/:id/messages", ({ request }) => {
        const after = new URL(request.url).searchParams.get("cursor.id");
        return after === null
          ? docspace(page(["a", "b"], { createdAt: "2026-01-01T00:00:00Z", id: "b" }))
          : docspace(page(["c"]));
      }),
    );

    const items = await storage.readByThread("t", undefined, undefined, "desc");

    expect(items.map((m) => m.id)).toEqual(["a", "b", "c"]);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/threads/t/messages?count=100",
      "/internal/ai/threads/t/messages?count=100&cursor.createdAt=2026-01-01T00%3A00%3A00Z&cursor.id=b",
    ]);
  });

  it("starts the whole-thread walk at the given cursor", async () => {
    upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page(["x"]))));
    await storage.readByThread("t", undefined, {
      createdAt: "2026-01-01T00:00:00Z",
      id: "c",
    } as never);
    expect(upstream.seen[0]?.path).toBe(
      "/internal/ai/threads/t/messages?count=100&cursor.createdAt=2026-01-01T00%3A00%3A00Z&cursor.id=c",
    );
  });

  it.each([
    ["a bare list of messages", [messageDto({ id: "a" })], ["a"]],
    ["a page without items", { cursor: null }, []],
    ["a page whose items are not a list", { items: "a" }, []],
    ["null", null, []],
    ["a string", "page", []],
    [
      "a page with invalid items",
      { items: [null, { contents: "x" }, messageDto({ id: "b" })] },
      ["b"],
    ],
  ])("reads %s", async (_name, body, ids) => {
    upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(body)));
    const items = await storage.readByThread("t");
    expect(items.map((m) => m.id)).toEqual(ids);
    expect(upstream.seen).toHaveLength(1);
  });

  it.each([
    ["a cursor without id", { createdAt: "2026-01-01T00:00:00Z" }],
    ["a cursor without createdAt", { id: "b" }],
    ["a cursor with an epoch createdAt", { createdAt: 5, id: "b" }],
    ["a cursor that is a string", "next"],
  ])("stops after the first page on %s", async (_name, cursor) => {
    upstream.use(
      http.get("*/internal/ai/threads/:id/messages", () => docspace(page(["a"], cursor))),
    );
    await expect(storage.readByThread("t")).resolves.toHaveLength(1);
    expect(upstream.seen).toHaveLength(1);
  });

  it.each([404, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspaceError(status)));
    await expect(storage.readByThread("t")).rejects.toMatchObject({ status });
  });

  describe("in a primed round", () => {
    const history = [
      dtoToMessage(messageDto({ id: "a" }))!,
      dtoToMessage(messageDto({ id: "b" }))!,
    ];
    const primed = () =>
      snapshotOf({
        requested: { threadId: "t", entityId: undefined, contextEntityId: undefined },
        messages: [...history],
      });

    it("serves the round's whole thread from the snapshot as a copy", async () => {
      const snapshot = primed();
      const result = await inRound(snapshot, async () => ({
        items: await storage.readByThread("t"),
        misses: getChatContextMisses(),
      }));
      expect(result).toEqual({ items: history, misses: [] });
      expect(result.items).not.toBe(snapshot.messages);
      expect(upstream.seen).toEqual([]);
    });

    it.each([
      ["another thread", "u", undefined, undefined],
      ["a paged read", "t", 10, undefined],
      ["a read from a cursor", "t", undefined, { createdAt: "2026-01-01T00:00:00Z", id: "c" }],
    ])("asks the AI service for %s", async (_name, threadId, count, cursor) => {
      upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page(["z"]))));
      const items = await inRound(primed(), () =>
        storage.readByThread(threadId, count, cursor as never),
      );
      expect(items.map((m) => m.id)).toEqual(["z"]);
      expect(upstream.seen).toHaveLength(1);
    });

    it("counts a miss for a whole-thread read it cannot serve", async () => {
      upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page([]))));
      const misses = await inRound(primed(), async () => {
        await storage.readByThread("u");
        return getChatContextMisses();
      });
      expect(misses).toEqual(["messages.readByThread(u)"]);
    });

    it("asks the AI service when the aggregate carries no messages or the slice is stale", async () => {
      upstream.use(http.get("*/internal/ai/threads/:id/messages", () => docspace(page(["z"]))));
      const noMessages = snapshotOf({
        requested: { threadId: "t", entityId: undefined, contextEntityId: undefined },
      });
      await inRound(noMessages, () => storage.readByThread("t"));
      const stale = primed();
      stale.stale.add("messages");
      await inRound(stale, () => storage.readByThread("t"));
      expect(upstream.seen).toHaveLength(2);
    });
  });
});

describe("the writes", () => {
  it("update puts the message as a JSON string and marks the slice stale afterwards", async () => {
    upstream.use(http.put("*/internal/ai/messages/:id", noContent));
    const snapshot = snapshotOf();

    await inRound(snapshot, () => storage.update("m/1", { id: "m/1", role: "user", content: [] }));

    expect(upstream.seen).toMatchObject([
      {
        method: "PUT",
        path: "/internal/ai/messages/m%2F1",
        body: { contents: '{"id":"m/1","role":"user","content":[]}' },
      },
    ]);
    expect([...snapshot.stale]).toEqual(["messages"]);
  });

  it("update rethrows a refusal and leaves the slice fresh", async () => {
    upstream.use(http.put("*/internal/ai/messages/:id", () => docspaceError(404)));
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.update("m", { id: "m", role: "user", content: [] })),
    ).rejects.toMatchObject({ status: 404 });
    expect(snapshot.stale.size).toBe(0);
  });

  it.each([
    [
      "delete",
      "*/internal/ai/messages/:id",
      "/internal/ai/messages/m%2F1",
      () => storage.delete("m/1"),
    ],
    [
      "deleteByThread",
      "*/internal/ai/threads/:id/messages",
      "/internal/ai/threads/m%2F1/messages",
      () => storage.deleteByThread("m/1"),
    ],
  ])(
    "%s deletes, treats a 404 as done and marks the slice stale first",
    async (_n, route, path, call) => {
      upstream.use(http.delete(route, noContent));
      const snapshot = snapshotOf();
      await inRound(snapshot, call);
      expect(upstream.seen).toMatchObject([{ method: "DELETE", path }]);
      expect([...snapshot.stale]).toEqual(["messages"]);

      upstream.use(http.delete(route, () => docspaceError(404)));
      await expect(call()).resolves.toBeUndefined();

      upstream.use(http.delete(route, () => docspaceError(500)));
      const failed = snapshotOf();
      await expect(inRound(failed, call)).rejects.toMatchObject({ status: 500 });
      expect([...failed.stale]).toEqual(["messages"]);
    },
  );
});
