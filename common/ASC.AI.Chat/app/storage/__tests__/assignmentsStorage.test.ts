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

import { ActionType } from "@onlyoffice/ai-chat/core";
import { describe, expect, it, vi } from "vitest";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { getChatContextMisses } from "../../requestContext.js";
import { HttpAssignmentsStorage } from "../assignmentsStorage.js";
import { inRound, PROFILE_ID, scopeOf, snapshotOf } from "./chatContext.fixtures.js";

// products/ASC.AI/Server/Api/AssignmentsStorageController.cs: POST assignments,
// GET assignments/{actionType} (`Guid?`), GET assignments (`Dictionary<string, Guid>`),
// PUT assignments/{actionType}, PUT assignments, DELETE assignments/{actionType}, DELETE assignments.
const upstream = useUpstream();
const storage = new HttpAssignmentsStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

describe("readByType", () => {
  it("reads the profile id of the action and logs it", async () => {
    upstream.use(http.get("*/internal/ai/assignments/:type", () => docspace(PROFILE_ID)));
    await expect(storage.readByType(ActionType.ImageGeneration, "42")).resolves.toBe(PROFILE_ID);
    expect(upstream.seen).toMatchObject([
      { method: "GET", path: "/internal/ai/assignments/ImageGeneration" },
    ]);
    expect(vi.mocked(logger.info).mock.calls).toEqual([
      [`HttpAssignmentsStorage.readByType(ImageGeneration) -> profileId=${PROFILE_ID}`],
    ]);
  });

  it("reads a bare text answer as the profile id", async () => {
    upstream.use(
      http.get(
        "*/internal/ai/assignments/:type",
        () => new HttpResponse("p-1", { headers: { "content-type": "text/plain" } }),
      ),
    );
    await expect(storage.readByType(ActionType.Chat)).resolves.toBe("p-1");
  });

  it.each([
    ["null (no assignment)", () => docspace(null)],
    ["an empty answer", noContent],
    ["a number", () => docspace(5)],
    ["an object", () => docspace({ profileId: PROFILE_ID })],
  ])("reads %s as no assignment", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/assignments/:type", answer));
    await expect(storage.readByType(ActionType.Chat)).resolves.toBeNull();
    expect(vi.mocked(logger.info).mock.calls).toEqual([
      ["HttpAssignmentsStorage.readByType(Chat) -> profileId=NONE"],
    ]);
  });

  it("reads a 404 as no assignment with its own log line", async () => {
    upstream.use(http.get("*/internal/ai/assignments/:type", () => docspaceError(404)));
    await expect(storage.readByType(ActionType.Chat)).resolves.toBeNull();
    expect(vi.mocked(logger.info).mock.calls).toEqual([
      ["HttpAssignmentsStorage.readByType(Chat) -> 404 NO ASSIGNMENT"],
    ]);
  });

  it.each([400, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/assignments/:type", () => docspaceError(status)));
    await expect(storage.readByType(ActionType.Chat)).rejects.toMatchObject({ status });
  });

  it("encodes the action type into the path", async () => {
    upstream.use(http.get("*/internal/ai/assignments/:type", () => docspace(null)));
    await storage.readByType("a/b" as never);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/assignments/a%2Fb");
  });

  describe("in a primed round", () => {
    const snapshot = () =>
      snapshotOf({
        global: scopeOf({ assignments: { Chat: "p-chat", imagegeneration: "p-image" } }),
        entity: scopeOf({ assignments: { Code: "p-entity" } }),
      });

    it.each([
      [ActionType.Chat, "p-chat"],
      [ActionType.ImageGeneration, "p-image"],
      [ActionType.Code, null],
    ])("serves %s from the global map, any casing, without a request", async (type, expected) => {
      const result = await inRound(snapshot(), async () => ({
        id: await storage.readByType(type),
        misses: getChatContextMisses(),
      }));
      expect(result).toEqual({ id: expected, misses: [] });
      expect(upstream.seen).toEqual([]);
      expect(vi.mocked(logger.info).mock.calls).toEqual([
        [
          `HttpAssignmentsStorage.readByType(${type}) via chat-context -> profileId=${expected ?? "NONE"}`,
        ],
      ]);
    });

    it("asks once the slice is stale, counting the miss", async () => {
      upstream.use(http.get("*/internal/ai/assignments/:type", () => docspace("p-fresh")));
      const stale = snapshot();
      stale.stale.add("assignments");
      const result = await inRound(stale, async () => ({
        id: await storage.readByType(ActionType.Chat),
        misses: getChatContextMisses(),
      }));
      expect(result).toEqual({ id: "p-fresh", misses: ["assignments.readByType(Chat)"] });
    });
  });
});

describe("readAll", () => {
  it("reads the portal's map, keeping only string ids", async () => {
    upstream.use(
      http.get("*/internal/ai/assignments", () =>
        docspace({ Chat: PROFILE_ID, Code: null, Vision: 3, Default: "p-default" }),
      ),
    );
    await expect(storage.readAll("42")).resolves.toEqual({
      Chat: PROFILE_ID,
      Default: "p-default",
    });
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/assignments" }]);
  });

  // DEFECT stor2-8: a GET assignments answer that is not an object (a list, null, an empty body) is read as "no assignment at all" — expected it to be rethrown as unknown, since the C# action always answers a dictionary (ts-ai-failure-paths.md §1)
  it.each([
    ["a list", () => docspace([PROFILE_ID])],
    ["null", () => docspace(null)],
    ["an empty answer", noContent],
  ])("reads %s as no assignment", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/assignments", answer));
    await expect(storage.readAll()).resolves.toEqual({});
  });

  it("rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/assignments", () => docspaceError(500)));
    await expect(storage.readAll()).rejects.toMatchObject({ status: 500 });
  });

  it("serves a copy of a primed round's global map, and asks once the slice is stale", async () => {
    const assignments = { Chat: "p" };
    const snapshot = snapshotOf({ global: scopeOf({ assignments }) });
    const served = await inRound(snapshot, () => storage.readAll());
    expect(served).toEqual(assignments);
    expect(served).not.toBe(assignments);
    expect(upstream.seen).toEqual([]);

    upstream.use(http.get("*/internal/ai/assignments", () => docspace({})));
    snapshot.stale.add("assignments");
    const misses = await inRound(snapshot, async () => {
      await storage.readAll();
      return getChatContextMisses();
    });
    expect(misses).toEqual(["assignments.readAll"]);
  });
});

describe("the writes", () => {
  it("create posts the action and the profile and marks the slice stale", async () => {
    upstream.use(http.post("*/internal/ai/assignments", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.create(ActionType.Chat, "p", "42"));
    expect(upstream.seen).toMatchObject([
      {
        method: "POST",
        path: "/internal/ai/assignments",
        body: { actionType: "Chat", profileId: "p" },
      },
    ]);
    expect([...snapshot.stale]).toEqual(["assignments"]);
  });

  it("update puts the profile by the encoded action type", async () => {
    upstream.use(http.put("*/internal/ai/assignments/:type", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.update(ActionType.OCR, "p", "42"));
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/assignments/OCR", body: { profileId: "p" } },
    ]);
    expect([...snapshot.stale]).toEqual(["assignments"]);
  });

  it("upsertMany puts the string ids only", async () => {
    upstream.use(http.put("*/internal/ai/assignments", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () =>
      storage.upsertMany({ Chat: "p", Code: undefined, Vision: 3 as never }, "42"),
    );
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/assignments", body: { assignments: { Chat: "p" } } },
    ]);
    expect([...snapshot.stale]).toEqual(["assignments"]);
  });

  it("leaves the slice fresh when a write is refused", async () => {
    upstream.use(
      http.post("*/internal/ai/assignments", () => docspaceError(400)),
      http.put("*/internal/ai/assignments/:type", () => docspaceError(400)),
      http.put("*/internal/ai/assignments", () => docspaceError(400)),
      http.delete("*/internal/ai/assignments", () => docspaceError(400)),
    );
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.create(ActionType.Chat, "p")),
    ).rejects.toBeDefined();
    await expect(
      inRound(snapshot, () => storage.update(ActionType.Chat, "p")),
    ).rejects.toBeDefined();
    await expect(inRound(snapshot, () => storage.upsertMany({ Chat: "p" }))).rejects.toBeDefined();
    await expect(
      inRound(snapshot, () => storage.deleteMany([ActionType.Chat])),
    ).rejects.toBeDefined();
    expect(snapshot.stale.size).toBe(0);
  });

  it("delete marks the slice stale first, treats a 404 as done and rethrows a 500", async () => {
    upstream.use(http.delete("*/internal/ai/assignments/:type", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.delete(ActionType.Chat, "42"));
    expect(upstream.seen).toMatchObject([
      { method: "DELETE", path: "/internal/ai/assignments/Chat" },
    ]);
    expect([...snapshot.stale]).toEqual(["assignments"]);

    upstream.use(http.delete("*/internal/ai/assignments/:type", () => docspaceError(404)));
    await expect(storage.delete(ActionType.Chat)).resolves.toBeUndefined();
    upstream.use(http.delete("*/internal/ai/assignments/:type", () => docspaceError(500)));
    await expect(storage.delete(ActionType.Chat)).rejects.toMatchObject({ status: 500 });
  });

  it("deleteMany asks nothing for no action types", async () => {
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.deleteMany([]));
    expect(upstream.seen).toEqual([]);
    expect(snapshot.stale.size).toBe(0);
  });

  it("deleteMany sends the action types in the body of one DELETE and marks the slice stale", async () => {
    upstream.use(http.delete("*/internal/ai/assignments", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.deleteMany([ActionType.Chat, ActionType.Code], "42"));
    expect(upstream.seen).toMatchObject([
      {
        method: "DELETE",
        path: "/internal/ai/assignments",
        body: { actionTypes: ["Chat", "Code"] },
      },
    ]);
    expect(upstream.seen[0]?.headers["content-type"]).toBe("application/json");
    expect([...snapshot.stale]).toEqual(["assignments"]);
  });
});
