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
import { HttpPreferencesStorage } from "../preferencesStorage.js";
import { inRound, scopeOf, snapshotOf } from "../../__tests__/fixtures/chatContext.js";

// GET/PUT/DELETE internal/ai/preferences: products/ASC.AI/Server/Api/PreferencesStorageController.cs;
// GET/PUT internal/ai/config/tool-mode: products/ASC.AI/Server/Api/SettingsController.cs.
const upstream = useUpstream();
const storage = new HttpPreferencesStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

describe("readToolPermissionMode", () => {
  // `ToolPermissionModeDto` (products/ASC.AI/Server/Models/ResponseDto/ToolPermissionModeDto.cs):
  // `mode` is the `ToolPermissionMode` enum, a number on the wire.
  it.each([
    [{ mode: 0 }, "ask"],
    [{ mode: 1 }, "auto"],
    [{ mode: 2 }, "allow"],
    [{ mode: 3 }, null],
    [{ mode: "Ask" }, "ask"],
    [{ mode: null }, null],
    [{}, null],
    [null, null],
    ["auto", null],
    [[1], null],
  ])("reads %j as %s", async (body, expected) => {
    upstream.use(http.get("*/internal/ai/config/tool-mode", () => docspace(body)));
    await expect(storage.readToolPermissionMode()).resolves.toBe(expected);
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/config/tool-mode" }]);
  });

  it("reads a 404 as nothing stored and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/config/tool-mode", () => docspaceError(404)));
    await expect(storage.readToolPermissionMode()).resolves.toBeNull();
    upstream.use(http.get("*/internal/ai/config/tool-mode", () => docspaceError(500)));
    await expect(storage.readToolPermissionMode()).rejects.toMatchObject({ status: 500 });
  });

  it("serves the mode of a primed round, null included, without a request", async () => {
    await expect(
      inRound(snapshotOf({ toolPermissionMode: "allow" }), () => storage.readToolPermissionMode()),
    ).resolves.toBe("allow");
    await expect(inRound(snapshotOf(), () => storage.readToolPermissionMode())).resolves.toBeNull();
    expect(upstream.seen).toEqual([]);
  });

  it("asks once the preferences slice is stale, counting the miss", async () => {
    upstream.use(http.get("*/internal/ai/config/tool-mode", () => docspace({ mode: 0 })));
    const snapshot = snapshotOf({ toolPermissionMode: "allow", stale: new Set(["preferences"]) });
    const result = await inRound(snapshot, async () => ({
      mode: await storage.readToolPermissionMode(),
      misses: getChatContextMisses(),
    }));
    expect(result).toEqual({ mode: "ask", misses: ["preferences.readToolPermissionMode"] });
  });
});

describe("the tool permission mode writes", () => {
  it.each([
    ["createToolPermissionMode", "ask", 0],
    ["updateToolPermissionMode", "auto", 1],
    ["upsertToolPermissionMode", "allow", 2],
  ] as const)("%s(%s) puts mode %i and marks the preferences stale", async (method, mode, sent) => {
    upstream.use(http.put("*/internal/ai/config/tool-mode", () => docspace({ mode: sent })));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage[method](mode));
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/config/tool-mode", body: { mode: sent } },
    ]);
    expect([...snapshot.stale]).toEqual(["preferences"]);
  });

  it("deleteToolPermissionMode writes the C# default, auto", async () => {
    upstream.use(http.put("*/internal/ai/config/tool-mode", () => docspace({ mode: 1 })));
    await storage.deleteToolPermissionMode();
    expect(upstream.seen[0]?.body).toEqual({ mode: 1 });
  });

  it("puts -1 for a mode the library does not have", async () => {
    upstream.use(http.put("*/internal/ai/config/tool-mode", () => docspaceError(400)));
    await expect(storage.createToolPermissionMode("never" as never)).rejects.toMatchObject({
      status: 400,
    });
    expect(upstream.seen[0]?.body).toEqual({ mode: -1 });
  });

  it("leaves the slice fresh when the write is refused", async () => {
    upstream.use(http.put("*/internal/ai/config/tool-mode", () => docspaceError(403)));
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.upsertToolPermissionMode("ask")),
    ).rejects.toMatchObject({
      status: 403,
    });
    expect(snapshot.stale.size).toBe(0);
  });
});

describe("readReasoningLevel and readDeepMode", () => {
  // `PreferencesDto` (products/ASC.AI/Server/Models/ResponseDto/PreferencesDto.cs): `depth`,
  // the `ReasoningDepth` enum (common/ASC.AI.Integration/Profiles/ReasoningConfig.cs) as a string.
  it.each([
    [{ depth: "none" }, "off", false],
    [{ depth: "low" }, "low", true],
    [{ depth: "medium" }, "medium", true],
    [{ depth: "High" }, "high", true],
    [{ depth: "xhigh" }, "max", true],
    [{ depth: "max" }, "max", true],
    [{ depth: "minimal" }, null, null],
    [{ depth: 2 }, null, null],
    [{ depth: null }, null, null],
    [{}, null, null],
    [null, null, null],
    [[], null, null],
  ])("reads %j as level %s and deep mode %s", async (body, level, deep) => {
    upstream.use(http.get("*/internal/ai/preferences", () => docspace(body)));
    await expect(storage.readReasoningLevel("42")).resolves.toBe(level);
    await expect(storage.readDeepMode("42")).resolves.toBe(deep);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/preferences",
      "/internal/ai/preferences",
    ]);
  });

  it("reads nothing stored (204) as null", async () => {
    upstream.use(http.get("*/internal/ai/preferences", noContent));
    await expect(storage.readReasoningLevel()).resolves.toBeNull();
  });

  it("reads a 404 as null and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/preferences", () => docspaceError(404)));
    await expect(storage.readReasoningLevel()).resolves.toBeNull();
    upstream.use(http.get("*/internal/ai/preferences", () => docspaceError(500)));
    await expect(storage.readReasoningLevel()).rejects.toMatchObject({ status: 500 });
    await expect(storage.readDeepMode()).rejects.toMatchObject({ status: 500 });
  });

  it("serves the global level of a primed round without a request", async () => {
    const snapshot = snapshotOf({ global: scopeOf({ reasoningLevel: "low" }) });
    const result = await inRound(snapshot, async () => [
      await storage.readReasoningLevel("42"),
      await storage.readDeepMode("42"),
    ]);
    expect(result).toEqual(["low", true]);
    await expect(inRound(snapshotOf(), () => storage.readDeepMode())).resolves.toBeNull();
    expect(upstream.seen).toEqual([]);
  });

  it("asks once the slice is stale, counting the miss", async () => {
    upstream.use(http.get("*/internal/ai/preferences", () => docspace({ depth: "none" })));
    const snapshot = snapshotOf({
      global: scopeOf({ reasoningLevel: "low" }),
      stale: new Set(["preferences"]),
    });
    const result = await inRound(snapshot, async () => ({
      level: await storage.readReasoningLevel(),
      misses: getChatContextMisses(),
    }));
    expect(result).toEqual({ level: "off", misses: ["preferences.readReasoningLevel"] });
  });
});

describe("the reasoning level writes", () => {
  it.each([
    ["createReasoningLevel", "off", "none"],
    ["updateReasoningLevel", "low", "low"],
    ["upsertReasoningLevel", "max", "max"],
    ["upsertReasoningLevel", "high", "high"],
  ] as const)(
    "%s(%s) puts depth %s for the user, whatever the entity",
    async (method, level, depth) => {
      upstream.use(http.put("*/internal/ai/preferences", noContent));
      const snapshot = snapshotOf();
      await inRound(snapshot, () => storage[method](level, "42"));
      expect(upstream.seen).toMatchObject([
        { method: "PUT", path: "/internal/ai/preferences", body: { depth, entityId: null } },
      ]);
      expect([...snapshot.stale]).toEqual(["preferences"]);
    },
  );

  it("leaves the slice fresh when the write is refused", async () => {
    upstream.use(http.put("*/internal/ai/preferences", () => docspaceError(400)));
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.createReasoningLevel("low")),
    ).rejects.toMatchObject({
      status: 400,
    });
    expect(snapshot.stale.size).toBe(0);
  });

  it.each([
    ["deleteReasoningLevel", () => storage.deleteReasoningLevel("42")],
    ["deleteDeepMode", () => storage.deleteDeepMode("42")],
  ])(
    "%s deletes the row, marks the slice stale first and treats a 404 as done",
    async (_n, call) => {
      upstream.use(http.delete("*/internal/ai/preferences", noContent));
      const snapshot = snapshotOf();
      await inRound(snapshot, call);
      expect(upstream.seen).toMatchObject([{ method: "DELETE", path: "/internal/ai/preferences" }]);
      expect([...snapshot.stale]).toEqual(["preferences"]);

      upstream.use(http.delete("*/internal/ai/preferences", () => docspaceError(404)));
      await expect(call()).resolves.toBeUndefined();
      upstream.use(http.delete("*/internal/ai/preferences", () => docspaceError(500)));
      const failed = snapshotOf();
      await expect(inRound(failed, call)).rejects.toMatchObject({ status: 500 });
      expect([...failed.stale]).toEqual(["preferences"]);
    },
  );
});

describe("the deep mode writes", () => {
  const writes = [
    ["createDeepMode", (v: boolean) => storage.createDeepMode(v, "42")],
    ["updateDeepMode", (v: boolean) => storage.updateDeepMode(v, "42")],
    ["upsertDeepMode", (v: boolean) => storage.upsertDeepMode(v, "42")],
  ] as const;

  it.each(writes)("%s(false) puts depth none without reading", async (_n, write) => {
    upstream.use(http.put("*/internal/ai/preferences", noContent));
    await write(false);
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/preferences", body: { depth: "none", entityId: null } },
    ]);
  });

  it.each(writes)("%s(true) keeps a stored depth above none", async (_n, write) => {
    upstream.use(http.get("*/internal/ai/preferences", () => docspace({ depth: "high" })));
    await write(true);
    expect(upstream.seen.map((r) => r.method)).toEqual(["GET"]);
  });

  it.each([
    ["nothing stored", noContent],
    ["depth none", () => docspace({ depth: "none" })],
    ["an unknown depth", () => docspace({ depth: "minimal" })],
    ["a 404", () => docspaceError(404)],
  ])("true puts the default depth, medium, over %s", async (_n, answer) => {
    upstream.use(
      http.get("*/internal/ai/preferences", answer),
      http.put("*/internal/ai/preferences", noContent),
    );
    await storage.upsertDeepMode(true);
    expect(upstream.seen.map((r) => [r.method, r.body])).toEqual([
      ["GET", undefined],
      ["PUT", { depth: "medium", entityId: null }],
    ]);
  });

  it("true writes nothing when the stored depth cannot be read", async () => {
    upstream.use(http.get("*/internal/ai/preferences", () => docspaceError(503)));
    await expect(storage.createDeepMode(true)).rejects.toMatchObject({ status: 503 });
    expect(upstream.seen.map((r) => r.method)).toEqual(["GET"]);
  });

  it("true reads the stored depth from a primed round", async () => {
    upstream.use(http.put("*/internal/ai/preferences", noContent));
    await inRound(snapshotOf({ global: scopeOf({ reasoningLevel: "max" }) }), () =>
      storage.createDeepMode(true),
    );
    expect(upstream.seen).toEqual([]);
    const off = snapshotOf({ global: scopeOf({ reasoningLevel: "off" }) });
    await inRound(off, () => storage.createDeepMode(true));
    expect(upstream.seen.map((r) => r.body)).toEqual([{ depth: "medium", entityId: null }]);
    expect([...off.stale]).toEqual(["preferences"]);
  });
});
