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

import { afterEach, describe, expect, it, vi } from "vitest";
import {
  delay,
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { getChatContextMisses } from "../../requestContext.js";
import { AiServiceHttpError } from "../httpClient.js";
import { HttpToolPrefsStorage } from "../toolPrefsStorage.js";
import { inRound, scopeOf, snapshotOf } from "../../__tests__/fixtures/chatContext.js";

// GET internal/ai/tool-prefs, PUT internal/ai/tool-prefs/disabled, PUT
// internal/ai/tool-prefs/allow-always: products/ASC.AI/Server/Api/ToolPreferenceStorageController.cs.
const upstream = useUpstream();
const storage = new HttpToolPrefsStorage();

const noContent = () => new HttpResponse(null, { status: 204 });

/**
 * `IReadOnlyDictionary<string, ToolPreference>` as ReadAsync answers it
 * (common/ASC.AI.Integration/ToolPrefs/ToolPreference.cs: `disabled`, `allowAlways`,
 * both `HashSet<string>?`).
 */
const storedPrefs = () => ({
  docspace: { disabled: ["get_folder_content"], allowAlways: ["get_folder_info"] },
  "docspace-integration": { disabled: ["generate_docx"], allowAlways: null },
  "docspace-integration-approval": { disabled: ["send_mail", "generate_docx"], allowAlways: [] },
  jira: { disabled: ["create_issue"], allowAlways: ["get_issue", "list_issues"] },
  github: { disabled: [], allowAlways: [] },
});

function answerPrefs(body: unknown) {
  upstream.use(http.get("*/internal/ai/tool-prefs", () => docspace(body as never)));
}

function acceptPuts() {
  upstream.use(
    http.put("*/internal/ai/tool-prefs/disabled", noContent),
    http.put("*/internal/ai/tool-prefs/allow-always", noContent),
  );
}

describe("readDisabled", () => {
  it("drops the portal server and serves the two integration groups merged under both keys", async () => {
    answerPrefs(storedPrefs());

    await expect(storage.readDisabled("42")).resolves.toEqual({
      "docspace-integration": ["generate_docx", "send_mail"],
      "docspace-integration-approval": ["generate_docx", "send_mail"],
      jira: ["create_issue"],
    });
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/tool-prefs" }]);
  });

  it.each([
    ["the base group", { "docspace-integration": { disabled: ["a"] } }],
    ["the approval group", { "docspace-integration-approval": { disabled: ["a"] } }],
  ])("serves a name disabled only under %s under both keys (Bug 83013)", async (_n, body) => {
    answerPrefs(body);
    await expect(storage.readDisabled()).resolves.toEqual({
      "docspace-integration": ["a"],
      "docspace-integration-approval": ["a"],
    });
  });

  it("asks for the tenant list by scope", async () => {
    answerPrefs({ jira: { disabled: ["create_issue"] } });
    await expect(storage.readDisabled(undefined, "tenant")).resolves.toEqual({
      jira: ["create_issue"],
    });
    expect(upstream.seen[0]?.path).toBe("/internal/ai/tool-prefs?scope=tenant");
  });

  it.each(["user", "personal"])("asks without a scope for the %s scope", async (scope) => {
    answerPrefs({});
    await storage.readDisabled(undefined, scope as never);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/tool-prefs");
  });

  it.each([
    ["an empty answer (204)", noContent()],
    ["null", docspace(null)],
    ["a list", docspace([{ disabled: ["a"] }])],
    ["a string", docspace("prefs")],
    ["a text body", new HttpResponse("x", { headers: { "content-type": "text/plain" } })],
  ])("reads %s as nothing disabled", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => answer));
    await expect(storage.readDisabled()).resolves.toEqual({});
  });

  it.each([
    ["a preference that is null", null],
    ["a preference that is a string", "create_issue"],
    ["a preference that is a list", ["create_issue"]],
    ["disabled missing", { allowAlways: ["x"] }],
    ["disabled null", { disabled: null }],
    ["disabled a string", { disabled: "create_issue" }],
    ["disabled an object", { disabled: { 0: "create_issue" } }],
    ["disabled empty", { disabled: [] }],
    ["disabled with no string", { disabled: [1, null, true, {}] }],
  ])("leaves out a server type with %s", async (_name, pref) => {
    answerPrefs({ jira: pref, github: { disabled: ["push"] } });
    await expect(storage.readDisabled()).resolves.toEqual({ github: ["push"] });
  });

  it("keeps only the strings of a disabled list, in order", async () => {
    answerPrefs({ jira: { disabled: ["b", 1, null, "a", "b"] } });
    await expect(storage.readDisabled()).resolves.toEqual({ jira: ["b", "a", "b"] });
  });

  it("reads a 404 as nothing disabled", async () => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(404)));
    await expect(storage.readDisabled()).resolves.toEqual({});
  });

  it.each([400, 401, 403, 500, 503])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(status)));
    const error = await storage.readDisabled().catch((e: unknown) => e);
    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect((error as AiServiceHttpError).status).toBe(status);
  });

  it("rethrows a dropped connection", async () => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => HttpResponse.error()));
    await expect(storage.readDisabled()).rejects.toThrow();
  });

  describe("on a timeout", () => {
    afterEach(() => {
      vi.useRealTimers();
    });

    it("rethrows when the AI service does not answer", async () => {
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
      upstream.use(
        http.get("*/internal/ai/tool-prefs", async () => {
          await delay("infinite");
          return docspace({});
        }),
      );
      const pending = storage.readDisabled().catch((e: unknown) => e);
      await vi.advanceTimersByTimeAsync(300_000);
      expect(String(await pending)).toContain("upstream timeout after 300000ms");
    });
  });

  it("serves the user list from a primed round without a request", async () => {
    const snapshot = snapshotOf({
      global: scopeOf({ toolPrefs: { docspace: { disabled: ["x"] }, jira: { disabled: ["y"] } } }),
    });
    const result = await inRound(snapshot, async () => ({
      disabled: await storage.readDisabled("42"),
      misses: getChatContextMisses(),
    }));
    expect(result).toEqual({ disabled: { jira: ["y"] }, misses: [] });
    expect(upstream.seen).toEqual([]);
  });

  it("asks the AI service for the tenant list even in a primed round, without counting a miss", async () => {
    answerPrefs({ jira: { disabled: ["t"] } });
    const snapshot = snapshotOf({ global: scopeOf({ toolPrefs: { jira: { disabled: ["u"] } } }) });
    const result = await inRound(snapshot, async () => ({
      disabled: await storage.readDisabled(undefined, "tenant"),
      misses: getChatContextMisses(),
    }));
    expect(result).toEqual({ disabled: { jira: ["t"] }, misses: [] });
    expect(upstream.seen[0]?.path).toBe("/internal/ai/tool-prefs?scope=tenant");
  });

  it("asks the AI service once the slice is stale, and counts the miss", async () => {
    answerPrefs({ jira: { disabled: ["fresh"] } });
    const snapshot = snapshotOf({
      global: scopeOf({ toolPrefs: { jira: { disabled: ["old"] } } }),
      stale: new Set(["toolPrefs"]),
    });
    const result = await inRound(snapshot, async () => ({
      disabled: await storage.readDisabled(),
      misses: getChatContextMisses(),
    }));
    expect(result).toEqual({ disabled: { jira: ["fresh"] }, misses: ["toolPrefs.read"] });
  });
});

describe("readAllowAlways", () => {
  it("answers one token per server type and tool, the portal server included", async () => {
    answerPrefs(storedPrefs());
    await expect(storage.readAllowAlways("42")).resolves.toEqual([
      "docspace_get_folder_info",
      "jira_get_issue",
      "jira_list_issues",
    ]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/tool-prefs");
  });

  it.each([
    ["allowAlways missing", { disabled: ["a"] }],
    ["allowAlways null", { allowAlways: null }],
    ["allowAlways a string", { allowAlways: "get_issue" }],
    ["allowAlways with no string", { allowAlways: [1, null] }],
    ["a preference that is null", null],
  ])("answers no token for %s", async (_name, pref) => {
    answerPrefs({ jira: pref });
    await expect(storage.readAllowAlways()).resolves.toEqual([]);
  });

  it.each([
    ["null", docspace(null)],
    ["a list", docspace([])],
    ["an empty answer", noContent()],
  ])("answers no token for %s", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => answer));
    await expect(storage.readAllowAlways()).resolves.toEqual([]);
  });

  it("reads a 404 as no token and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(404)));
    await expect(storage.readAllowAlways()).resolves.toEqual([]);
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(500)));
    await expect(storage.readAllowAlways()).rejects.toMatchObject({ status: 500 });
  });

  it("serves the tokens from a primed round without a request", async () => {
    const snapshot = snapshotOf({
      global: scopeOf({ toolPrefs: { jira: { allowAlways: ["get_issue"] } } }),
    });
    await expect(inRound(snapshot, () => storage.readAllowAlways())).resolves.toEqual([
      "jira_get_issue",
    ]);
    expect(upstream.seen).toEqual([]);
  });
});

describe("the disabled writes", () => {
  const writes = [
    [
      "createDisabled",
      (d: Record<string, string[]>, s?: "tenant") => storage.createDisabled(d, "42", s),
    ],
    [
      "updateDisabled",
      (d: Record<string, string[]>, s?: "tenant") => storage.updateDisabled(d, "42", s),
    ],
    [
      "upsertDisabled",
      (d: Record<string, string[]>, s?: "tenant") => storage.upsertDisabled(d, "42", s),
    ],
  ] as const;

  it.each(writes)(
    "%s puts the map with every stored server type it drops cleared, without the portal server",
    async (_name, write) => {
      answerPrefs({
        jira: { disabled: ["create_issue"] },
        github: { disabled: ["push"], allowAlways: ["pull"] },
        docspace: { disabled: ["legacy"] },
      });
      acceptPuts();

      await write({ jira: ["delete_issue"], docspace: ["get_folder_content"] });

      expect(upstream.seen.map((r) => [r.method, r.path])).toEqual([
        ["GET", "/internal/ai/tool-prefs"],
        ["PUT", "/internal/ai/tool-prefs/disabled"],
      ]);
      expect(upstream.seen[1]?.body).toEqual({
        disabled: { jira: ["delete_issue"], github: [] },
        entityId: null,
      });
      expect(upstream.seen[1]?.headers["content-type"]).toBe("application/json");
    },
  );

  it.each(writes)(
    "%s writes the tenant list by scope, read from the tenant list",
    async (_n, write) => {
      answerPrefs({ github: { disabled: ["push"] } });
      acceptPuts();

      await write({ jira: ["x"] }, "tenant");

      expect(upstream.seen.map((r) => r.path)).toEqual([
        "/internal/ai/tool-prefs?scope=tenant",
        "/internal/ai/tool-prefs/disabled",
      ]);
      expect(upstream.seen[1]?.body).toEqual({
        disabled: { jira: ["x"], github: [] },
        entityId: null,
        scope: "tenant",
      });
    },
  );

  it("deleteDisabled clears every stored server type", async () => {
    answerPrefs({
      jira: { disabled: ["a"] },
      github: { disabled: ["b"] },
      docspace: { disabled: ["c"] },
    });
    acceptPuts();

    await storage.deleteDisabled("42");

    expect(upstream.seen[1]?.body).toEqual({ disabled: { jira: [], github: [] }, entityId: null });
  });

  it("deleteDisabled of the tenant list clears the tenant list", async () => {
    answerPrefs({ jira: { disabled: ["a"] } });
    acceptPuts();

    await storage.deleteDisabled(undefined, "tenant");

    expect(upstream.seen[1]?.body).toEqual({
      disabled: { jira: [] },
      entityId: null,
      scope: "tenant",
    });
  });

  it("puts the map as given when nothing is stored (404)", async () => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(404)));
    acceptPuts();

    await storage.createDisabled({ jira: ["a"] });

    expect(upstream.seen[1]?.body).toEqual({ disabled: { jira: ["a"] }, entityId: null });
  });

  it.each([500, 403])("writes nothing when the stored list answers %i", async (status) => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(status)));

    await expect(storage.createDisabled({ jira: ["a"] })).rejects.toMatchObject({ status });
    expect(upstream.seen.map((r) => r.method)).toEqual(["GET"]);
  });

  it("takes the stored list from a primed round, puts, and marks the slice stale", async () => {
    acceptPuts();
    const snapshot = snapshotOf({
      global: scopeOf({ toolPrefs: { github: { disabled: ["b"] } } }),
    });

    await inRound(snapshot, () => storage.createDisabled({ jira: ["a"] }));

    expect(upstream.seen.map((r) => r.method)).toEqual(["PUT"]);
    expect(upstream.seen[0]?.body).toEqual({
      disabled: { jira: ["a"], github: [] },
      entityId: null,
    });
    expect([...snapshot.stale]).toEqual(["toolPrefs"]);
  });

  it("leaves the slice fresh when the put fails", async () => {
    upstream.use(http.put("*/internal/ai/tool-prefs/disabled", () => docspaceError(500)));
    const snapshot = snapshotOf();

    await expect(
      inRound(snapshot, () => storage.createDisabled({ jira: ["a"] })),
    ).rejects.toMatchObject({
      status: 500,
    });
    expect(snapshot.stale.size).toBe(0);
  });
});

describe("the allow-always writes", () => {
  const writes = [
    ["createAllowAlways", (t: string[]) => storage.createAllowAlways(t, "42")],
    ["updateAllowAlways", (t: string[]) => storage.updateAllowAlways(t, "42")],
    ["upsertAllowAlways", (t: string[]) => storage.upsertAllowAlways(t, "42")],
  ] as const;

  it.each(writes)(
    "%s groups the tokens on the first underscore and clears the stored server types it drops",
    async (_name, write) => {
      answerPrefs({ github: { allowAlways: ["pull"] }, jira: { allowAlways: ["old"] } });
      acceptPuts();

      await write([
        "jira_get_issue",
        "jira_list_issues",
        "docspace_get_folder_info",
        "web-search_search_web",
        "nounderscore",
        "_leading",
        "trailing_",
      ]);

      expect(upstream.seen.map((r) => [r.method, r.path])).toEqual([
        ["GET", "/internal/ai/tool-prefs"],
        ["PUT", "/internal/ai/tool-prefs/allow-always"],
      ]);
      expect(upstream.seen[1]?.body).toEqual({
        allowAlways: {
          jira: ["get_issue", "list_issues"],
          docspace: ["get_folder_info"],
          "web-search": ["search_web"],
          github: [],
        },
        entityId: null,
      });
    },
  );

  it("puts the tokens as given when the stored list is empty (204)", async () => {
    upstream.use(http.get("*/internal/ai/tool-prefs", noContent));
    acceptPuts();
    await storage.upsertAllowAlways(["jira_a"]);
    expect(upstream.seen[1]?.body).toEqual({ allowAlways: { jira: ["a"] }, entityId: null });
  });

  it("deleteAllowAlways clears every stored server type", async () => {
    answerPrefs({ github: { allowAlways: ["pull"] }, jira: { disabled: ["x"] } });
    acceptPuts();

    await storage.deleteAllowAlways("42");

    expect(upstream.seen[1]?.body).toEqual({ allowAlways: { github: [] }, entityId: null });
  });

  it("puts the tokens as given when nothing is stored (404), and writes nothing on a 500", async () => {
    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(404)));
    acceptPuts();
    await storage.createAllowAlways(["jira_a"]);
    expect(upstream.seen[1]?.body).toEqual({ allowAlways: { jira: ["a"] }, entityId: null });

    upstream.use(http.get("*/internal/ai/tool-prefs", () => docspaceError(500)));
    await expect(storage.createAllowAlways(["jira_a"])).rejects.toMatchObject({ status: 500 });
    expect(upstream.seen.map((r) => r.method)).toEqual(["GET", "PUT", "GET"]);
  });

  it("takes the stored list from a primed round and marks the slice stale after the put", async () => {
    acceptPuts();
    const snapshot = snapshotOf({
      global: scopeOf({ toolPrefs: { github: { allowAlways: ["p"] } } }),
    });

    await inRound(snapshot, () => storage.createAllowAlways(["jira_a"]));

    expect(upstream.seen.map((r) => r.method)).toEqual(["PUT"]);
    expect(upstream.seen[0]?.body).toEqual({
      allowAlways: { jira: ["a"], github: [] },
      entityId: null,
    });
    expect([...snapshot.stale]).toEqual(["toolPrefs"]);
  });
});
