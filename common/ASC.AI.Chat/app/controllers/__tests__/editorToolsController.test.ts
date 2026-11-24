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

import type { ToolsAdapter } from "@onlyoffice/ai-chat/core";
import type { RequestHandler } from "express";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { invoke } from "../../__tests__/helpers/http.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";

// The editor plugin's tool routes. The composed tools adapter (the MCP servers and the .NET
// integration tools) is the boundary and stands in as one mock; the AI access gate (a read of
// `internal/ai/tool-prefs`) runs for real against msw.

const adapter = engineMock<ToolsAdapter>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  composeToolsAdapters: () => adapter,
}));
vi.mock("../../tools/systemTools.js", () => ({
  systemToolsSource: { getServerTypes: () => ["docspace", "custom-mcp"] },
}));
vi.mock("../../tools/httpToolsAdapter.js", () => ({
  DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE: "docspace-integration-approval",
  HttpToolsAdapter: class {},
}));

const upstream = useUpstream();
const { aiServiceBaseUrl } = await import("../../storage/httpClient.js");
const { requestContextMiddleware, shouldForwardHeadersToProvider } =
  await import("../../requestContext.js");
const { editorToolsController } = await import("../editorToolsController.js");
// After the adapter mock exists: the schema helper loads the library through apiCatalog.ts.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");

/** The handler behind the request context middleware, as the app runs it. */
function inContext(handler: RequestHandler): RequestHandler {
  return (req, res, next) =>
    new Promise<void>((resolve) => {
      requestContextMiddleware(req, res, () => {
        void Promise.resolve(handler(req, res, next)).then(resolve);
      });
    }) as unknown as void;
}

// `GET internal/ai/tool-prefs` (ToolPrefsController), the gate of assertAiAccess.
function gateAnswers(status: number): void {
  upstream.use(
    http.get(`${aiServiceBaseUrl}/internal/ai/tool-prefs`, () =>
      status === 200 ? docspace({}) : docspaceError(status),
    ),
  );
}

const offered = {
  docspace: [
    {
      name: "docspace_search_files",
      description: "Search files",
      inputSchema: { type: "object", properties: { query: { type: "string" } } },
      requireApproval: false,
      transport: { url: "http://mcp.internal:3000", headers: { Authorization: "Bearer mcp" } },
    },
    { name: "docspace_delete_file" },
    { name: "docspace_web_search", description: "Search the web" },
  ],
  "docspace-integration-approval": [{ name: "docspace_create_room", requireApproval: true }],
  "docspace-integration": [{ name: "docspace_web_crawling" }, { name: "docspace_list_rooms" }],
};

beforeEach(() => {
  mockReset(adapter);
  adapter.getTools.mockResolvedValue(offered as never);
});

describe("editorToolsController.list", () => {
  it("answers four fields per tool, approval by the server type, without the web-search pair", async () => {
    let forwarded: boolean | undefined;
    adapter.getTools.mockImplementation(() => {
      forwarded = shouldForwardHeadersToProvider();
      return Promise.resolve(offered as never);
    });

    const reply = await invoke(inContext(editorToolsController.list), {
      method: "GET",
      query: { entityId: "17" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      tools: [
        {
          name: "docspace_search_files",
          description: "Search files",
          inputSchema: { type: "object", properties: { query: { type: "string" } } },
          requireApproval: false,
        },
        {
          name: "docspace_delete_file",
          description: "",
          inputSchema: { type: "object", properties: {} },
          requireApproval: true,
        },
        {
          name: "docspace_create_room",
          description: "",
          inputSchema: { type: "object", properties: {} },
          requireApproval: true,
        },
        {
          name: "docspace_list_rooms",
          description: "",
          inputSchema: { type: "object", properties: {} },
          requireApproval: false,
        },
      ],
    });
    expect(JSON.stringify(reply.body)).not.toContain("mcp.internal");
    expect(responseViolations("aiEditorToolsList", 200, reply.body)).toEqual([]);
    expect(adapter.getTools).toHaveBeenCalledWith("17", { attachmentId: [] });
    expect(forwarded).toBe(true);
  });

  it.each([
    ["absent", {}],
    ["repeated (an array)", { entityId: ["1", "2"] }],
  ])("lists the portal-wide tools when entityId is %s", async (_what, query) => {
    const reply = await invoke(editorToolsController.list, { method: "GET", query });

    expect(reply.status).toBe(200);
    expect(adapter.getTools).toHaveBeenCalledWith(undefined, { attachmentId: [] });
  });

  it("answers an empty list when no server offers a tool", async () => {
    adapter.getTools.mockResolvedValue({});

    const reply = await invoke(editorToolsController.list, { method: "GET" });

    expect(reply.body).toEqual({ tools: [] });
    expect(responseViolations("aiEditorToolsList", 200, reply.body)).toEqual([]);
  });

  it("answers 500 when the adapter fails", async () => {
    adapter.getTools.mockRejectedValue(new Error("mcp down"));

    const reply = await invoke(editorToolsController.list, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiEditorToolsList", 500, reply.body)).toEqual([]);
  });
});

describe("editorToolsController.call", () => {
  const call = (body: unknown) => invoke(inContext(editorToolsController.call), { body });

  it("runs an offered tool with its arguments and entry, and answers the result as text", async () => {
    gateAnswers(200);
    let forwarded: boolean | undefined;
    adapter.callTool.mockImplementation(() => {
      forwarded = shouldForwardHeadersToProvider();
      return Promise.resolve("3 files");
    });

    const reply = await call({
      name: "  docspace_search_files ",
      arguments: { query: "q" },
      entityId: "17",
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ result: "3 files" });
    expect(responseViolations("aiEditorToolsCall", 200, reply.body)).toEqual([]);
    expect(adapter.getTools).toHaveBeenCalledWith("17", { attachmentId: [] });
    expect(adapter.callTool).toHaveBeenCalledWith("docspace_search_files", { query: "q" }, "17");
    expect(forwarded).toBe(true);
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /internal/ai/tool-prefs",
    ]);
  });

  it.each([
    ["an object", { files: [1] }, '{"files":[1]}'],
    ["undefined", undefined, "{}"],
    ["null", null, "{}"],
    ["a number", 5, "5"],
  ])("answers a result that is %s as text", async (_what, result, text) => {
    gateAnswers(404);
    adapter.callTool.mockResolvedValue(result);

    const reply = await call({ name: "docspace_list_rooms" });

    expect(reply.body).toEqual({ result: text });
    expect(adapter.callTool).toHaveBeenCalledWith("docspace_list_rooms", {}, undefined);
  });

  it.each([
    ["arguments that are a string", { name: "docspace_list_rooms", arguments: "x" }],
    ["arguments that are an array", { name: "docspace_list_rooms", arguments: [1] }],
    ["arguments that are null", { name: "docspace_list_rooms", arguments: null }],
    ["an entityId that is a number", { name: "docspace_list_rooms", entityId: 17 }],
  ])("runs the tool with empty arguments and no entry for %s", async (_what, body) => {
    gateAnswers(404);
    adapter.callTool.mockResolvedValue("ok");

    const reply = await call(body);

    expect(reply.status).toBe(200);
    expect(adapter.callTool).toHaveBeenCalledWith("docspace_list_rooms", {}, undefined);
  });

  // CONTRACT passthrough-13: the tool name is judged before the AI access gate, so a guest or a portal with AI switched off gets 400 "Unknown tool name" for an empty or excluded name, not 403. Pinned by EditorToolAiDisabledTests.BodyValidation_RunsBeforeTheSwitchIsChecked and EmptyName_IsValidatedBeforeTheSwitchIsChecked (products/ASC.AI/ASC.AI.Tests/Tests/EditorToolTests/EditorToolAiDisabledTests.cs).
  it.each([
    ["a string", "docspace_list_rooms"],
    ["an array", [{ name: "docspace_list_rooms" }]],
    ["an empty object", {}],
    ["a null name", { name: null }],
    ["a name that is a number", { name: 1 }],
    ["an empty name", { name: "" }],
    ["a blank name", { name: "   " }],
    ["the editor's own web search", { name: "docspace_web_search" }],
    ["the editor's own web crawling", { name: " docspace_web_crawling " }],
  ])("answers 400 to a body that is %s, before the access gate", async (_what, body) => {
    const reply = await call(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "Unknown tool name" });
    expect(responseViolations("aiEditorToolsCall", 400, reply.body)).toEqual([]);
    expect(upstream.seen).toEqual([]);
    expect(adapter.getTools).not.toHaveBeenCalled();
    expect(adapter.callTool).not.toHaveBeenCalled();
  });

  it.each([
    ["a tool no server offers", "docspace_format_disk"],
    ["a name that differs in case", "DOCSPACE_LIST_ROOMS"],
  ])("answers 400 to %s and runs nothing (Bug 84394)", async (_what, name) => {
    gateAnswers(404);

    const reply = await call({ name });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "Unknown tool name" });
    expect(adapter.callTool).not.toHaveBeenCalled();
  });

  it.each([
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])("answers the gate's %i and runs nothing (Bugs 84390, 84391)", async (status, error) => {
    gateAnswers(status);

    const reply = await call({ name: "docspace_list_rooms", entityId: "17" });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiEditorToolsCall", status, reply.body)).toEqual([]);
    expect(adapter.getTools).not.toHaveBeenCalled();
    expect(adapter.callTool).not.toHaveBeenCalled();
  });

  it("answers 500 when the tool throws", async () => {
    gateAnswers(404);
    adapter.callTool.mockRejectedValue(new Error("mcp down"));

    const reply = await call({ name: "docspace_list_rooms" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});
