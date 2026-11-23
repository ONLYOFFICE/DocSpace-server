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

import type { ToolsEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { invoke } from "../../__tests__/helpers/http.js";
import type { CallInit } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { useFastTimers } from "../../__tests__/helpers/time.js";
import {
  delay,
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";

// Characterization of every route of toolsController. The engine class and the MCP server
// registry (`storage.mcpServers`) are stubbed, the system tool source is stubbed (it connects to
// MCP servers), `resolveCustomServers` runs for real over the stubbed registry, and the entity gate
// (`assertEntityAccessible`) runs for real against a stubbed Files API.

const engine = engineMock<ToolsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  ToolsEngine: engineClass(() => engine),
}));
const mcpServers = vi.hoisted(() => ({ readByName: vi.fn(), readAll: vi.fn() }));
vi.mock("../../storage/index.js", () => ({ storage: { mcpServers } }));
const system = vi.hoisted(() => ({ getServerTypes: vi.fn(), getSystemServerConfig: vi.fn() }));
vi.mock("../../tools/systemTools.js", () => ({
  systemToolsSource: { getServerTypes: system.getServerTypes },
  getSystemServerConfig: system.getSystemServerConfig,
}));

const { toolsController } = await import("../toolsController.js");
const { customToolsSource } = await import("../../tools/customTools.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");

const upstream = useUpstream();

const SYSTEM_CONFIG = { type: "http", url: "http://internal-mcp:5158/mcp" };
const URL_A = "https://mcp.example.com/sse";
const SECRET = { Authorization: "Bearer MARKER-secret" };

// GET api/2.0/files/folder/{id} (ASC.Files FoldersController.GetFolderInfoAsync).
function folderAnswers(response: Response | (() => Response)): void {
  upstream.use(
    http.get("*/api/2.0/files/folder/:id", () =>
      typeof response === "function" ? response() : response,
    ),
  );
}
const folderOk = () => docspace({ id: 42, title: "Room", type: 6, roomType: 2 });

async function send(handler: unknown, body: unknown, init: CallInit = {}) {
  return invoke(handler, { ...init, body });
}

beforeEach(() => {
  mockReset(engine);
  mcpServers.readByName.mockReset().mockResolvedValue(null);
  mcpServers.readAll.mockReset().mockResolvedValue({});
  system.getServerTypes.mockReset().mockReturnValue(["sys-files"]);
  system.getSystemServerConfig
    .mockReset()
    .mockImplementation((name: string) => (name === "sys-files" ? SYSTEM_CONFIG : undefined));
});

const NAME_PATH_ERROR =
  'name must not be ".", "..", or contain a path separator or control character';

describe.each([
  ["addCustomServer", "aiToolsAddCustomServer"],
  ["updateCustomServer", "aiToolsUpdateCustomServer"],
] as const)("toolsController.%s", (route, operationId) => {
  const handler = toolsController[route];
  const engineCall = () => engine[route];

  it("answers the engine's result and hands it the name and the config as given", async () => {
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, { name: "crm", config: { url: URL_A, timeout: 5 } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engineCall()).toHaveBeenCalledWith("crm", { url: URL_A, timeout: 5 }, undefined);
    expect(mcpServers.readByName).toHaveBeenCalledWith("crm", undefined);
    expect(upstream.seen).toEqual([]);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("reads a positional body and checks the entityId with the Files API first", async () => {
    folderAnswers(folderOk);
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, ["crm", { url: URL_A }, "42"]);

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith("crm", { url: URL_A }, "42");
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /api/2.0/files/folder/42",
    ]);
  });

  it("allows a name with spaces and printable punctuation", async () => {
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, { name: "My CRM (prod)!", config: { url: URL_A } });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith("My CRM (prod)!", { url: URL_A }, undefined);
  });

  // CONTRACT ctl-1: a refusal of the engine ({ success: false, error }) is sent with 200. Pinned by the "Soft refusals" scenario of openspec/specs/ai-agents/spec.md and ASC.AI.Tests (McpServerCreateTests, McpServerNameTests), which read the refusal from a 200.
  it("sends a refusal of the engine with 200", async () => {
    const refusal = {
      success: false as const,
      error: { field: "url" as const, message: "url is invalid", code: "invalid" as const },
    };
    engineCall().mockResolvedValue(refusal as never);

    const reply = await send(handler, { name: "crm", config: { url: "nope" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(refusal);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("pins an entry named after a system server to the system config, whatever is sent", async () => {
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, {
      name: "sys-files",
      config: { url: "https://attacker.example.com", headers: SECRET },
    });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith("sys-files", SYSTEM_CONFIG, undefined);
    expect(mcpServers.readByName).not.toHaveBeenCalled();
  });

  it.each([
    ["absent", undefined],
    ["null", null],
    ["an empty object", {}],
    ["a number", 5],
    ["a boolean", true],
  ])("re-saves the stored entry when the config is %s", async (_what, config) => {
    mcpServers.readByName.mockResolvedValue({ url: URL_A, headers: SECRET });
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, { name: "crm", config });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith("crm", { url: URL_A, headers: SECRET }, undefined);
  });

  it.each([
    ["absent", undefined],
    ["null", null],
    ["an empty object", {}],
    ["a number", 5],
  ])(
    "answers 400 when the config is %s and nothing is stored under the name",
    async (_w, config) => {
      const reply = await send(handler, { name: "crm", config });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({
        error: 'No config provided and no portal-level server named "crm"',
      });
      expect(engineCall()).not.toHaveBeenCalled();
      expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    },
  );

  // The engine refuses a config that is not an object as a value (`{ success: false, error }`,
  // answered 200: the "Soft refusals" of openspec/specs/ai-agents/spec.md), so it is handed over.
  it("hands a config that is a string to the engine, which refuses it", async () => {
    const refusal = { success: false as const, error: { field: "url" as const, message: "no" } };
    engineCall().mockResolvedValue(refusal as never);

    const reply = await send(handler, { name: "crm", config: "https://mcp.example.com" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(refusal);
    expect(engineCall()).toHaveBeenCalledWith("crm", "https://mcp.example.com", undefined);
  });

  // The engine's to refuse, as above (ASC.AI.Tests, McpServerConfigValidationTests).
  it("hands a config that is an array to the engine", async () => {
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, { name: "crm", config: [URL_A] });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith("crm", [URL_A], undefined);
  });

  it("keeps the stored headers when the config has none and points at the same url", async () => {
    mcpServers.readByName.mockResolvedValue({ url: URL_A, headers: SECRET });
    engineCall().mockResolvedValue({ success: true });

    await send(handler, { name: "crm", config: { url: URL_A, timeout: 9 } });

    expect(engineCall()).toHaveBeenCalledWith(
      "crm",
      { url: URL_A, timeout: 9, headers: SECRET },
      undefined,
    );
  });

  it.each([
    ["another url", { url: "https://other.example.com" }, { url: URL_A, headers: SECRET }],
    ["no url", { command: "run" }, { url: URL_A, headers: SECRET }],
    ["explicit empty headers", { url: URL_A, headers: {} }, { url: URL_A, headers: SECRET }],
    ["a stored entry without headers", { url: URL_A }, { url: URL_A }],
    ["stored headers that are not an object", { url: URL_A }, { url: URL_A, headers: "x" }],
  ])("stores the config as given for %s", async (_what, config, stored) => {
    mcpServers.readByName.mockResolvedValue(stored);
    engineCall().mockResolvedValue({ success: true });

    await send(handler, { name: "crm", config });

    expect(engineCall()).toHaveBeenCalledWith("crm", config, undefined);
  });

  it.each([
    ["null", null, "name is required"],
    ["a string", "crm", "name is required"],
    ["an empty array", [], "name is required"],
    ["{}", {}, "name is required"],
    ["a name that is null", { name: null, config: { url: URL_A } }, "name is required"],
    ["a name that is a number", { name: 5, config: { url: URL_A } }, "name is required"],
    ["an empty name", { name: "", config: { url: URL_A } }, "name is required"],
    ["a blank name", { name: "   ", config: { url: URL_A } }, "name is required"],
    ["the name .", { name: ".", config: { url: URL_A } }, NAME_PATH_ERROR],
    ["the name ..", { name: "..", config: { url: URL_A } }, NAME_PATH_ERROR],
    ["a name with /", { name: "a/b", config: { url: URL_A } }, NAME_PATH_ERROR],
    ["a name with \\", { name: "a\\b", config: { url: URL_A } }, NAME_PATH_ERROR],
    ["a name with a NUL", { name: "a\u0000b", config: { url: URL_A } }, NAME_PATH_ERROR],
    ["a name with a newline", { name: "a\nb", config: { url: URL_A } }, NAME_PATH_ERROR],
    ["a name with DEL", { name: "a\u007fb", config: { url: URL_A } }, NAME_PATH_ERROR],
  ])("answers 400 for a body that is %s, before any read", async (_what, body, error) => {
    const reply = await send(handler, body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(mcpServers.readByName).not.toHaveBeenCalled();
    expect(engineCall()).not.toHaveBeenCalled();
    expect(upstream.seen).toEqual([]);
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("accepts a name of any length", async () => {
    engineCall().mockResolvedValue({ success: true });
    const name = "n".repeat(10_000);

    const reply = await send(handler, { name, config: { url: URL_A } });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith(name, { url: URL_A }, undefined);
  });

  it.each([
    ["an empty string", ""],
    ["null", null],
  ])("skips the entity check when the entityId is %s", async (_what, entityId) => {
    engineCall().mockResolvedValue({ success: true });

    const reply = await send(handler, { name: "crm", config: { url: URL_A }, entityId });

    expect(reply.status).toBe(200);
    expect(upstream.seen).toEqual([]);
    // `null` is how the widget sends an argument it omits: the engine gets none.
    expect(engineCall()).toHaveBeenCalledWith("crm", { url: URL_A }, entityId ?? undefined);
  });

  it.each([
    ["a number", 42],
    ["an object", { id: 42 }],
  ])("answers 400 for an entityId that is %s, before the entity check", async (_w, entityId) => {
    const reply = await send(handler, { name: "crm", config: { url: URL_A }, entityId });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
    expect(upstream.seen).toEqual([]);
    expect(engineCall()).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it.each([
    [403, 'Entity "42" is not accessible'],
    [404, 'Entity "42" not found'],
  ])("answers %i when the Files API answers it for the entityId", async (status, error) => {
    folderAnswers(docspaceError(status, "MARKER-upstream"));

    const reply = await send(handler, { name: "crm", config: { url: URL_A }, entityId: "42" });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(mcpServers.readByName).not.toHaveBeenCalled();
    expect(engineCall()).not.toHaveBeenCalled();
    expect(responseViolations(operationId, status, reply.body)).toEqual([]);
  });

  it("answers the Files API's 500 as 500 with its reason phrase only", async () => {
    folderAnswers(() =>
      HttpResponse.text("MARKER-upstream", { status: 500, statusText: "Internal Server Error" }),
    );

    const reply = await send(handler, { name: "crm", config: { url: URL_A }, entityId: "42" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engineCall()).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the stored entry cannot be read", async () => {
    mcpServers.readByName.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER-body", "http://internal/x"),
    );

    const reply = await send(handler, { name: "crm", config: { url: URL_A } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engineCall()).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });

  it("relays the status of the AI service when the engine's write fails", async () => {
    engineCall().mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", "MARKER-body", "http://internal/x"),
    );

    const reply = await send(handler, { name: "crm", config: { url: URL_A } });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
  });

  it("answers 500 without details when the engine throws a plain Error", async () => {
    engineCall().mockRejectedValue(new Error("boom at http://internal:5157"));

    const reply = await send(handler, { name: "crm", config: { url: URL_A } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("toolsController entity check timeout", () => {
  useFastTimers(60_000);

  it("answers 500 when the Files API does not answer the entity check in time", async () => {
    upstream.use(
      http.get("*/api/2.0/files/folder/:id", async () => {
        await delay("infinite");
        return docspace({});
      }),
    );

    const reply = await send(toolsController.setAllowAlways, {
      serverType: "web-search",
      toolName: "search",
      value: true,
      entityId: "42",
    });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(engine.setAllowAlways).not.toHaveBeenCalled();
  });
});

describe("toolsController.removeCustomServer", () => {
  const handler = toolsController.removeCustomServer;
  const operationId = "aiToolsRemoveCustomServer";

  it("removes the server named in the body and answers success", async () => {
    engine.removeCustomServer.mockResolvedValue(undefined);

    const reply = await send(handler, { name: "crm" }, { method: "DELETE" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.removeCustomServer).toHaveBeenCalledWith("crm", undefined);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("reads a positional body and checks the entityId first", async () => {
    folderAnswers(folderOk);
    engine.removeCustomServer.mockResolvedValue(undefined);

    const reply = await send(handler, ["crm", "42"], { method: "DELETE" });

    expect(reply.status).toBe(200);
    expect(engine.removeCustomServer).toHaveBeenCalledWith("crm", "42");
    expect(upstream.seen.map((r) => r.path)).toEqual(["/api/2.0/files/folder/42"]);
  });

  it.each([
    ["null", null],
    ["a string", "crm"],
    ["{}", {}],
    ["a name that is a number", { name: 5 }],
  ])("falls back to the query name when the body is %s", async (_what, body) => {
    engine.removeCustomServer.mockResolvedValue(undefined);

    const reply = await send(handler, body, { method: "DELETE", query: { name: "crm" } });

    expect(reply.status).toBe(200);
    expect(engine.removeCustomServer).toHaveBeenCalledWith("crm", undefined);
  });

  it("does not take the entityId from the query", async () => {
    engine.removeCustomServer.mockResolvedValue(undefined);

    await send(handler, {}, { method: "DELETE", query: { name: "crm", entityId: "42" } });

    expect(upstream.seen).toEqual([]);
    expect(engine.removeCustomServer).toHaveBeenCalledWith("crm", undefined);
  });

  it("drops an entityId that is not a string", async () => {
    engine.removeCustomServer.mockResolvedValue(undefined);

    await send(handler, { name: "crm", entityId: 42 }, { method: "DELETE" });

    expect(upstream.seen).toEqual([]);
    expect(engine.removeCustomServer).toHaveBeenCalledWith("crm", undefined);
  });

  // The remove route does not check the name like add/update do; it forwards it as given.
  it("hands a name with a path separator to the engine", async () => {
    engine.removeCustomServer.mockResolvedValue(undefined);

    const reply = await send(handler, { name: "a/b" }, { method: "DELETE" });

    expect(reply.status).toBe(200);
    expect(engine.removeCustomServer).toHaveBeenCalledWith("a/b", undefined);
  });

  it.each([
    ["null", null, {}],
    ["{}", {}, {}],
    ["an empty name", { name: "" }, {}],
    ["a query name that is an array", {}, { name: ["crm"] }],
    ["an empty query name", {}, { name: "" }],
  ])("answers 400 when the name is missing: %s", async (_what, body, query) => {
    const reply = await send(handler, body, { method: "DELETE", query });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "name required" });
    expect(engine.removeCustomServer).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("answers 404 when the entity does not exist", async () => {
    folderAnswers(docspaceError(404));

    const reply = await send(handler, { name: "crm", entityId: "42" }, { method: "DELETE" });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: 'Entity "42" not found' });
    expect(engine.removeCustomServer).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 404, reply.body)).toEqual([]);
  });

  it("relays the AI service's status when the delete fails", async () => {
    engine.removeCustomServer.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await send(handler, { name: "crm" }, { method: "DELETE" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });
});

describe("toolsController.getCustomServer", () => {
  const handler = toolsController.getCustomServer;
  const operationId = "aiToolsGetCustomServer";
  const get = (query: Record<string, unknown>) => invoke(handler, { method: "GET", query });

  it("answers the config without its headers and passes the entityId", async () => {
    engine.getCustomServer.mockResolvedValue({ url: URL_A, headers: SECRET, timeout: 5 } as never);

    const reply = await get({ name: "crm", entityId: "42" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ url: URL_A, timeout: 5 });
    expect(JSON.stringify(reply.body)).not.toContain("MARKER");
    expect(engine.getCustomServer).toHaveBeenCalledWith("crm", "42");
    expect(upstream.seen).toEqual([]);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("answers a system server as an empty object", async () => {
    engine.getCustomServer.mockResolvedValue({ ...SYSTEM_CONFIG, headers: SECRET } as never);

    const reply = await get({ name: "sys-files" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({});
    expect(engine.getCustomServer).toHaveBeenCalledWith("sys-files", undefined);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("answers null for an unknown name", async () => {
    engine.getCustomServer.mockResolvedValue(null);

    const reply = await get({ name: "gone" });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("ignores an entityId that is not a string", async () => {
    engine.getCustomServer.mockResolvedValue(null);

    await get({ name: "crm", entityId: ["42"] });

    expect(engine.getCustomServer).toHaveBeenCalledWith("crm", undefined);
  });

  it.each([
    ["missing", {}],
    ["empty", { name: "" }],
    ["an array", { name: ["crm"] }],
  ])("answers 400 when the name is %s", async (_what, query) => {
    const reply = await get(query);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "name required" });
    expect(engine.getCustomServer).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("relays the AI service's status when the read fails", async () => {
    engine.getCustomServer.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await get({ name: "crm" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });
});

describe("toolsController.listCustomServers", () => {
  const handler = toolsController.listCustomServers;
  const operationId = "aiToolsListCustomServers";

  it("lists the servers without headers, empties system ones and leaves out the portal server", async () => {
    engine.listCustomServers.mockResolvedValue({
      crm: { url: URL_A, headers: SECRET },
      "sys-files": { ...SYSTEM_CONFIG, headers: SECRET },
      docspace: { url: "http://legacy/mcp" },
      local: { command: "run", args: ["a"] },
    } as never);

    const reply = await invoke(handler, { method: "GET", query: { entityId: "42" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      crm: { url: URL_A },
      "sys-files": {},
      local: { command: "run", args: ["a"] },
    });
    expect(JSON.stringify(reply.body)).not.toContain("MARKER");
    expect(engine.listCustomServers).toHaveBeenCalledWith("42");
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("answers an empty map when nothing is registered", async () => {
    engine.listCustomServers.mockResolvedValue({});

    const reply = await invoke(handler, { method: "GET", query: { entityId: 42 } });

    expect(reply.body).toEqual({});
    expect(engine.listCustomServers).toHaveBeenCalledWith(undefined);
  });

  it("relays the AI service's status when the read fails", async () => {
    engine.listCustomServers.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });
});

describe("toolsController.listSystemTools", () => {
  const handler = toolsController.listSystemTools;
  const operationId = "aiToolsListSystemTools";
  const tool = (name: string, serverType: string) => ({
    name,
    description: `${name} tool`,
    inputSchema: { type: "object", properties: {} },
    enabled: true,
    serverType,
  });

  it("merges the system groups without the portal server and the custom groups, and names the system ones", async () => {
    engine.listSystemTools.mockResolvedValue({
      groups: {
        "sys-files": [tool("read", "sys-files")],
        docspace: [tool("portal", "docspace")],
      },
    } as never);
    const custom = vi.spyOn(customToolsSource, "getToolsWithErrors").mockResolvedValue({
      groups: { crm: [tool("lookup", "crm")] },
      errors: { broken: "connect ECONNREFUSED" },
    } as never);

    const reply = await invoke(handler, { method: "GET", query: { entityId: "42" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      groups: { "sys-files": [tool("read", "sys-files")], crm: [tool("lookup", "crm")] },
      errors: { broken: "connect ECONNREFUSED" },
      system: ["sys-files"],
    });
    expect(engine.listSystemTools).toHaveBeenCalledWith("42");
    expect(custom).toHaveBeenCalledWith("42");
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("reads a system listing that is a plain group map", async () => {
    engine.listSystemTools.mockResolvedValue({
      "sys-files": [tool("read", "sys-files")],
      docspace: [],
    } as never);
    vi.spyOn(customToolsSource, "getToolsWithErrors").mockResolvedValue({
      groups: {},
      errors: {},
    } as never);

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.body).toEqual({
      groups: { "sys-files": [tool("read", "sys-files")] },
      errors: {},
      system: ["sys-files"],
    });
    expect(engine.listSystemTools).toHaveBeenCalledWith(undefined);
  });

  it("lets a custom group replace a system group of the same name, still listed as system", async () => {
    engine.listSystemTools.mockResolvedValue({ "sys-files": [tool("read", "sys-files")] } as never);
    vi.spyOn(customToolsSource, "getToolsWithErrors").mockResolvedValue({
      groups: { "sys-files": [tool("other", "sys-files")] },
      errors: {},
    } as never);

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.body).toEqual({
      groups: { "sys-files": [tool("other", "sys-files")] },
      errors: {},
      system: ["sys-files"],
    });
  });

  it("answers 500 when the system listing is not an object", async () => {
    engine.listSystemTools.mockResolvedValue(null as never);
    vi.spyOn(customToolsSource, "getToolsWithErrors").mockResolvedValue({
      groups: {},
      errors: {},
    } as never);

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the custom servers cannot be enumerated", async () => {
    engine.listSystemTools.mockResolvedValue({} as never);
    vi.spyOn(customToolsSource, "getToolsWithErrors").mockRejectedValue(new Error("MARKER"));

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("toolsController.replaceAllCustomServers", () => {
  const handler = toolsController.replaceAllCustomServers;
  const operationId = "aiToolsReplaceAllCustomServers";

  it("re-pins system entries, keeps stored headers and hands the normalized map to the engine", async () => {
    mcpServers.readAll.mockResolvedValue({
      crm: { url: URL_A, headers: SECRET },
      old: { url: "https://old.example.com", headers: SECRET },
    });
    engine.replaceAllCustomServers.mockResolvedValue({ success: true });

    const reply = await send(handler, {
      map: {
        crm: { url: URL_A },
        "sys-files": {},
        old: null,
        fresh: { url: "https://new.example.com" },
      },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.replaceAllCustomServers).toHaveBeenCalledWith(
      {
        crm: { url: URL_A, headers: SECRET },
        "sys-files": SYSTEM_CONFIG,
        old: { url: "https://old.example.com", headers: SECRET },
        fresh: { url: "https://new.example.com" },
      },
      undefined,
    );
    expect(mcpServers.readAll).toHaveBeenCalledTimes(1);
    expect(mcpServers.readAll).toHaveBeenCalledWith(undefined);
    expect(mcpServers.readByName).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("hands an empty map to the engine, which clears the registry", async () => {
    engine.replaceAllCustomServers.mockResolvedValue({ success: true });

    const reply = await send(handler, { map: {} });

    expect(reply.status).toBe(200);
    expect(engine.replaceAllCustomServers).toHaveBeenCalledWith({}, undefined);
  });

  it("reads a positional body and checks the entityId first", async () => {
    folderAnswers(folderOk);
    engine.replaceAllCustomServers.mockResolvedValue({ success: true });

    const reply = await send(handler, [{ crm: { url: URL_A } }, "42"]);

    expect(reply.status).toBe(200);
    expect(engine.replaceAllCustomServers).toHaveBeenCalledWith({ crm: { url: URL_A } }, "42");
    expect(upstream.seen.map((r) => r.path)).toEqual(["/api/2.0/files/folder/42"]);
  });

  // CONTRACT ctl-5: a refusal of the engine ({ success: false, errors }) is sent with 200. Pinned by ASC.AI.Tests (McpServerReplaceAllTests), which reads the refusal and its `errors` from a 200.
  it("sends a refusal of the engine with 200", async () => {
    const refusal = {
      success: false as const,
      errors: [{ name: "crm", error: { field: "url" as const, message: "url is invalid" } }],
    };
    engine.replaceAllCustomServers.mockResolvedValue(refusal);

    const reply = await send(handler, { map: { crm: { url: "nope" } } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(refusal);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it.each([
    ["null", null],
    ["a string", "map"],
    ["{}", {}],
    ["an empty array", []],
    ["a map that is null", { map: null }],
    ["a map that is a string", { map: "crm" }],
    ["a map that is an array", { map: [{ url: URL_A }] }],
    ["a map that is a number", { map: 5 }],
  ])("answers 400 for a body that is %s, before any read", async (_what, body) => {
    const reply = await send(handler, body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "map is required and must be an object" });
    expect(mcpServers.readAll).not.toHaveBeenCalled();
    expect(engine.replaceAllCustomServers).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it.each([
    ["an empty name", "", "name is required"],
    ["a blank name", " ", "name is required"],
    ["the name ..", "..", NAME_PATH_ERROR],
    ["a name with /", "a/b", NAME_PATH_ERROR],
  ])("answers 400 for a map with %s, after the registry read", async (_what, name, error) => {
    const reply = await send(handler, { map: { ok: { url: URL_A }, [name]: { url: URL_A } } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(mcpServers.readAll).toHaveBeenCalledTimes(1);
    expect(engine.replaceAllCustomServers).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("answers 400 for an entry without config and nothing stored under its name", async () => {
    const reply = await send(handler, { map: { crm: {} } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: 'No config provided and no portal-level server named "crm"',
    });
    expect(engine.replaceAllCustomServers).not.toHaveBeenCalled();
  });

  // The engine's to refuse, entry by entry (`errors`), as for the single-server routes.
  it("hands an entry whose config is a string to the engine", async () => {
    engine.replaceAllCustomServers.mockResolvedValue({ success: true });

    const reply = await send(handler, { map: { crm: "https://mcp.example.com" } });

    expect(reply.status).toBe(200);
    expect(engine.replaceAllCustomServers).toHaveBeenCalledWith(
      { crm: "https://mcp.example.com" },
      undefined,
    );
  });

  // No cap on the servers of one registry: ASC.AI.Tests pins it (McpServerReplaceAllTests,
  // ReplaceAll_ALargeMap_IsStoredWhole).
  it("hands a map of 1000 entries to the engine", async () => {
    engine.replaceAllCustomServers.mockResolvedValue({ success: true });
    const map = Object.fromEntries(
      Array.from({ length: 1000 }, (_v, i) => [`s${i}`, { url: `${URL_A}/${i}` }]),
    );

    const reply = await send(handler, { map });

    expect(reply.status).toBe(200);
    expect(Object.keys(engine.replaceAllCustomServers.mock.calls[0]?.[0] ?? {})).toHaveLength(1000);
  });

  it("answers 400 for an entityId that is a number, before the entity check and the registry", async () => {
    const reply = await send(handler, { map: {}, entityId: 42 });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
    expect(upstream.seen).toEqual([]);
    expect(engine.replaceAllCustomServers).not.toHaveBeenCalled();
  });

  it("answers 404 for an entity that does not exist, before reading the registry", async () => {
    folderAnswers(docspaceError(404));

    const reply = await send(handler, { map: {}, entityId: "42" });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: 'Entity "42" not found' });
    expect(mcpServers.readAll).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 404, reply.body)).toEqual([]);
  });

  it("answers 500 and writes nothing when the registry cannot be read", async () => {
    mcpServers.readAll.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await send(handler, { map: { crm: { url: URL_A } } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engine.replaceAllCustomServers).not.toHaveBeenCalled();
  });
});

describe("toolsController.setDisabled", () => {
  const handler = toolsController.setDisabled;
  const operationId = "aiToolsSetDisabled";
  const VALID_TYPES =
    "crm, docspace-integration, docspace-integration-approval, document-agent, image-generation, sys-files, web-search";

  it.each([
    ["sys-files"],
    ["docspace-integration"],
    ["docspace-integration-approval"],
    ["web-search"],
    ["image-generation"],
    ["document-agent"],
    ["crm"],
  ])("stores the list for the server type %s", async (serverType) => {
    mcpServers.readAll.mockResolvedValue({ crm: { url: URL_A } });
    engine.setDisabled.mockResolvedValue(undefined);

    const reply = await send(handler, { serverType, toolNames: ["a", "b"] }, { method: "PUT" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.setDisabled).toHaveBeenCalledWith(serverType, ["a", "b"], undefined, undefined);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it.each([
    ["user", "user"],
    ["tenant", "tenant"],
    ["an empty string", ""],
    ["null", null],
  ])("passes the scope %s on", async (_what, scope) => {
    engine.setDisabled.mockResolvedValue(undefined);

    await send(handler, { serverType: "web-search", toolNames: [], scope }, { method: "PUT" });

    expect(engine.setDisabled).toHaveBeenCalledWith(
      "web-search",
      [],
      undefined,
      scope === "user" || scope === "tenant" ? scope : undefined,
    );
  });

  it("reads a positional body and checks the entityId first", async () => {
    folderAnswers(folderOk);
    engine.setDisabled.mockResolvedValue(undefined);

    const reply = await send(handler, ["web-search", ["x"], "42", "tenant"], { method: "PUT" });

    expect(reply.status).toBe(200);
    expect(engine.setDisabled).toHaveBeenCalledWith("web-search", ["x"], "42", "tenant");
    expect(upstream.seen.map((r) => r.path)).toEqual(["/api/2.0/files/folder/42"]);
  });

  it("leaves out of the valid types the portal server, system-named, url-less and malformed entries", async () => {
    mcpServers.readAll.mockResolvedValue({
      docspace: { url: URL_A },
      "sys-files": { url: URL_A },
      local: { command: "run" },
      empty: { url: "" },
      bad: "x",
    });

    const reply = await send(handler, { serverType: "local", toolNames: [] }, { method: "PUT" });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error:
        'unknown serverType "local"; valid values: docspace-integration, docspace-integration-approval, document-agent, image-generation, sys-files, web-search',
    });
  });

  it("reads a 404 of the registry as no custom servers", async () => {
    mcpServers.readAll.mockRejectedValue(
      new AiServiceHttpError(404, "Not Found", "", "http://internal/x"),
    );
    engine.setDisabled.mockResolvedValue(undefined);

    const reply = await send(
      handler,
      { serverType: "web-search", toolNames: [] },
      { method: "PUT" },
    );

    expect(reply.status).toBe(200);
  });

  it.each([
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])(
    "answers %i when the registry read fails with it, and writes nothing",
    async (status, text) => {
      mcpServers.readAll.mockRejectedValue(
        new AiServiceHttpError(status, text, "MARKER", "http://internal/x"),
      );

      const reply = await send(
        handler,
        { serverType: "web-search", toolNames: [] },
        { method: "PUT" },
      );

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error: text });
      expect(engine.setDisabled).not.toHaveBeenCalled();
      expect(responseViolations(operationId, status, reply.body)).toEqual([]);
    },
  );

  it.each([
    ["an unknown serverType", { serverType: "nope", toolNames: [] }, "nope"],
    ["no serverType", { toolNames: [] }, "undefined"],
    ["a serverType that is null", { serverType: null, toolNames: [] }, "null"],
    ["a serverType that is a number", { serverType: 7, toolNames: [] }, "7"],
    ["an empty serverType", { serverType: "", toolNames: [] }, ""],
  ])("answers 400 for %s, listing the valid types", async (_what, body, shown) => {
    mcpServers.readAll.mockResolvedValue({ crm: { url: URL_A } });

    const reply = await send(handler, body, { method: "PUT" });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: `unknown serverType "${shown}"; valid values: ${VALID_TYPES}`,
    });
    expect(engine.setDisabled).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it.each([
    ["null", null, "toolNames is required and must be an array of tool names"],
    ["a string", "web-search", "toolNames is required and must be an array of tool names"],
    ["{}", {}, "toolNames is required and must be an array of tool names"],
    [
      "toolNames that is a string (Bug 84402)",
      { serverType: "web-search", toolNames: "search" },
      "toolNames is required and must be an array of tool names",
    ],
    [
      "toolNames that is null",
      { serverType: "web-search", toolNames: null },
      "toolNames is required and must be an array of tool names",
    ],
    [
      "toolNames with a number",
      { serverType: "web-search", toolNames: ["a", 1] },
      "toolNames must contain non-empty strings only",
    ],
    [
      "toolNames with an empty string",
      { serverType: "web-search", toolNames: [""] },
      "toolNames must contain non-empty strings only",
    ],
    [
      "toolNames with a blank string",
      { serverType: "web-search", toolNames: ["  "] },
      "toolNames must contain non-empty strings only",
    ],
  ])("answers 400 for a body that is %s, before the registry read", async (_what, body, error) => {
    const reply = await send(handler, body, { method: "PUT" });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(mcpServers.readAll).not.toHaveBeenCalled();
    expect(engine.setDisabled).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it.each([
    ["an unknown string", "everyone", 'unknown scope "everyone"; valid values: tenant, user'],
    ["a number", 1, 'unknown scope "1"; valid values: tenant, user'],
    ["an array", ["user"], 'unknown scope "user"; valid values: tenant, user'],
    ["an upper-case value", "USER", 'unknown scope "USER"; valid values: tenant, user'],
  ])(
    "answers 400 for a scope that is %s, before toolNames is checked",
    async (_w, scope, error) => {
      const reply = await send(handler, { serverType: "web-search", scope }, { method: "PUT" });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error });
      expect(engine.setDisabled).not.toHaveBeenCalled();
      expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    },
  );

  // HANDLED DOWNSTREAM ctl-8: toolNames has no limit and repeated names are not collapsed; 1000 names with duplicates reach the engine as sent. In production the AI service stores the names as a set (UpsertToolPrefsRequestDto: HashSet), so repeated names collapse; the size is bounded by the JSON parser; it shows only with the engine or the service stubbed.
  it("hands 1000 tool names with duplicates to the engine as sent", async () => {
    engine.setDisabled.mockResolvedValue(undefined);
    const toolNames = Array.from({ length: 1000 }, (_v, i) => `t${i % 10}`);

    const reply = await send(handler, { serverType: "web-search", toolNames }, { method: "PUT" });

    expect(reply.status).toBe(200);
    expect(engine.setDisabled).toHaveBeenCalledWith("web-search", toolNames, undefined, undefined);
  });

  it("answers 403 for an entity the caller cannot reach, before the scope and the body are checked", async () => {
    folderAnswers(docspaceError(403));

    const reply = await send(handler, { entityId: "42", scope: "nope" }, { method: "PUT" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: 'Entity "42" is not accessible' });
    expect(mcpServers.readAll).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
  });

  it("relays the AI service's status when the write fails", async () => {
    engine.setDisabled.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await send(
      handler,
      { serverType: "web-search", toolNames: [] },
      { method: "PUT" },
    );

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
  });
});

describe("toolsController.getDisabled", () => {
  const handler = toolsController.getDisabled;
  const operationId = "aiToolsGetDisabled";

  it.each([
    ["absent", {}, undefined],
    ["user", { scope: "user" }, "user"],
    ["tenant", { scope: "tenant" }, "tenant"],
    ["empty", { scope: "" }, undefined],
  ])("answers the map of the scope %s", async (_what, query, scope) => {
    engine.getDisabled.mockResolvedValue({ "web-search": ["search"] });

    const reply = await invoke(handler, { method: "GET", query: { ...query, entityId: "42" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ "web-search": ["search"] });
    expect(engine.getDisabled).toHaveBeenCalledWith("42", scope);
    expect(upstream.seen).toEqual([]);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it.each([
    ["an unknown value", "all", 'unknown scope "all"; valid values: tenant, user'],
    ["an array", ["tenant", "user"], 'unknown scope "tenant,user"; valid values: tenant, user'],
  ])("answers 400 for a scope that is %s", async (_what, scope, error) => {
    const reply = await invoke(handler, { method: "GET", query: { scope } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.getDisabled).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("relays the AI service's status when the read fails", async () => {
    engine.getDisabled.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });
});

describe.each([
  ["isToolDisabled", "aiToolsIsToolDisabled"],
  ["isAllowAlways", "aiToolsIsAllowAlways"],
] as const)("toolsController.%s", (route, operationId) => {
  const handler = toolsController[route];

  it.each([[true], [false]])("answers %s from the engine", async (value) => {
    engine[route].mockResolvedValue(value);

    const reply = await invoke(handler, {
      method: "GET",
      query: { serverType: "web-search", toolName: "search", entityId: "42" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(value);
    expect(engine[route]).toHaveBeenCalledWith("web-search", "search", "42");
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it.each([
    ["no serverType", { toolName: "search" }],
    ["an empty serverType", { serverType: "", toolName: "search" }],
    ["a serverType that is an array", { serverType: ["a"], toolName: "search" }],
    ["no toolName", { serverType: "web-search" }],
    ["an empty toolName", { serverType: "web-search", toolName: "" }],
    ["a toolName that is an array", { serverType: "web-search", toolName: ["a"] }],
  ])("answers 400 for %s", async (_what, query) => {
    const reply = await invoke(handler, { method: "GET", query });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "serverType and toolName required" });
    expect(engine[route]).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("relays the AI service's status when the read fails", async () => {
    engine[route].mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await invoke(handler, {
      method: "GET",
      query: { serverType: "web-search", toolName: "search" },
    });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engine[route]).toHaveBeenCalledWith("web-search", "search", undefined);
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });
});

describe("toolsController.setAllowAlways", () => {
  const handler = toolsController.setAllowAlways;
  const operationId = "aiToolsSetAllowAlways";
  const valid = { serverType: "web-search", toolName: "search", value: true };

  it.each([[true], [false]])("stores the value %s", async (value) => {
    engine.setAllowAlways.mockResolvedValue(undefined);

    const reply = await send(handler, { ...valid, value }, { method: "PUT" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.setAllowAlways).toHaveBeenCalledWith("web-search", "search", value, undefined);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("reads a positional body and checks the entityId first", async () => {
    folderAnswers(folderOk);
    engine.setAllowAlways.mockResolvedValue(undefined);

    const reply = await send(handler, ["web-search", "search", false, "42"], { method: "PUT" });

    expect(reply.status).toBe(200);
    expect(engine.setAllowAlways).toHaveBeenCalledWith("web-search", "search", false, "42");
    expect(upstream.seen.map((r) => r.path)).toEqual(["/api/2.0/files/folder/42"]);
  });

  // The serverType is not checked against the known groups here, unlike set-disabled.
  it("stores a value for a serverType no group has", async () => {
    engine.setAllowAlways.mockResolvedValue(undefined);

    const reply = await send(handler, { ...valid, serverType: "nope" }, { method: "PUT" });

    expect(reply.status).toBe(200);
    expect(engine.setAllowAlways).toHaveBeenCalledWith("nope", "search", true, undefined);
  });

  const SERVER = "serverType is required and must be a non-empty string";
  const TOOL = "toolName is required and must be a non-empty string";
  const VALUE = "value is required and must be a boolean";
  it.each([
    ["null", null, SERVER],
    ["a string", "web-search", SERVER],
    ["{}", {}, SERVER],
    ["no serverType", { toolName: "search", value: true }, SERVER],
    ["a serverType that is null", { ...valid, serverType: null }, SERVER],
    ["a serverType that is a number", { ...valid, serverType: 1 }, SERVER],
    ["a blank serverType", { ...valid, serverType: " " }, SERVER],
    ["no toolName", { serverType: "web-search", value: true }, TOOL],
    ["a toolName that is null", { ...valid, toolName: null }, TOOL],
    ["a toolName that is an array", { ...valid, toolName: ["search"] }, TOOL],
    ["an empty toolName", { ...valid, toolName: "" }, TOOL],
    ["no value", { serverType: "web-search", toolName: "search" }, VALUE],
    ["a value that is null", { ...valid, value: null }, VALUE],
    ["the string false (Bug 84405)", { ...valid, value: "false" }, VALUE],
    ["a value that is 1", { ...valid, value: 1 }, VALUE],
  ])("answers 400 for a body that is %s", async (_what, body, error) => {
    const reply = await send(handler, body, { method: "PUT" });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.setAllowAlways).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });

  it("answers 404 for an entity that does not exist, before the body is checked", async () => {
    folderAnswers(docspaceError(404));

    const reply = await send(handler, { entityId: "42" }, { method: "PUT" });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: 'Entity "42" not found' });
    expect(responseViolations(operationId, 404, reply.body)).toEqual([]);
  });

  it("relays the AI service's status when the write fails", async () => {
    engine.setAllowAlways.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await send(handler, valid, { method: "PUT" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
  });
});

describe("toolsController.getAllowAlways", () => {
  const handler = toolsController.getAllowAlways;
  const operationId = "aiToolsGetAllowAlways";

  it("answers the engine's tokens and passes the entityId", async () => {
    engine.getAllowAlways.mockResolvedValue(["web-search:search", "crm:lookup"]);

    const reply = await invoke(handler, { method: "GET", query: { entityId: "42" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(["web-search:search", "crm:lookup"]);
    expect(engine.getAllowAlways).toHaveBeenCalledWith("42");
    expect(upstream.seen).toEqual([]);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("ignores an entityId that is not a string", async () => {
    engine.getAllowAlways.mockResolvedValue([]);

    await invoke(handler, { method: "GET", query: { entityId: ["42"] } });

    expect(engine.getAllowAlways).toHaveBeenCalledWith(undefined);
  });

  it("relays the AI service's status when the read fails", async () => {
    engine.getAllowAlways.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://internal/x"),
    );

    const reply = await invoke(handler, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });
});
