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

// What the controller hands the AIEngine it builds at load: the storage, the pace of the
// periodic save of a streaming message, the hooks it reads back from the request context.

import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { getCompaction } from "../../../config/index.js";
import type { FakeAIEngine } from "./aiController.harness.js";

// The boundary of the controller: the engine class, the storage, the chat-context aggregate,
// the AI-project binding, the Files reads of the context folders and of the source, the custom
// MCP servers, the tools prompt of the AI service and the thread gate. The request context,
// the HTTP client, the narrowing helpers, the stored-profile check and the telemetry are real.
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  AIEngine: (await import("./aiController.harness.js")).FakeAIEngine,
}));
vi.mock("../../storage/index.js", async () => ({
  storage: (await import("./aiController.harness.js")).storage,
}));
vi.mock("../../storage/chatContext.js", async () => {
  const h = await import("./aiController.harness.js");
  return {
    primeChatContext: h.primeChatContext,
    describeChatContextUsage: h.describeChatContextUsage,
  };
});
vi.mock("../../storage/aiProjectChatContext.js", async () => ({
  applyAiProjectChatContext: (await import("./aiController.harness.js")).applyAiProjectChatContext,
}));
vi.mock("../../storage/contextFolders.js", async (original) => {
  const h = await import("./aiController.harness.js");
  return {
    ...(await original<typeof import("../../storage/contextFolders.js")>()),
    resolveContextFolders: h.resolveContextFolders,
    resolveLocation: h.resolveLocation,
  };
});
vi.mock("../../storage/docspaceFilesApi.js", async (original) => ({
  ...(await original<typeof import("../../storage/docspaceFilesApi.js")>()),
  primeSourceMeta: (await import("./aiController.harness.js")).primeSourceMeta,
}));
vi.mock("../../tools/customTools.js", async () => ({
  customToolsSource: { source: "custom" },
  primeCustomServers: (await import("./aiController.harness.js")).primeCustomServers,
}));
vi.mock("../../tools/httpToolsAdapter.js", async (original) => ({
  ...(await original<typeof import("../../tools/httpToolsAdapter.js")>()),
  safeGetToolsPrompt: (await import("./aiController.harness.js")).safeGetToolsPrompt,
}));
vi.mock("../../tools/systemTools.js", async () => ({
  systemToolsSource: { getServerTypes: (await import("./aiController.harness.js")).getServerTypes },
}));
vi.mock("../../tools/formAnalysisSubAgent.js", () => ({
  formAnalysisSubAgent: { source: "form" },
}));
vi.mock("../../tools/documentAgent.js", () => ({ documentAgentTools: { source: "agent" } }));
vi.mock("../threadsController.js", async () => ({
  assertThreadCreatable: (await import("./aiController.harness.js")).assertThreadCreatable,
}));

interface EngineDeps {
  storage: unknown;
  persistIntervalMs: number;
  resolveSource: () => unknown;
  toolProgress: boolean;
  toolsAdapter: unknown;
  systemServerTypes: () => string[];
  compaction: () => unknown;
}

// A fresh load of the controller, so the module-level engine is built again. The stand-ins
// are reached through the mocked modules: `vi.resetModules` keeps what their factories made.
async function load(persistInterval?: string): Promise<{
  deps: EngineDeps;
  storage: unknown;
  getServerTypes: { mockReturnValue: (value: string[]) => void };
  context: typeof import("../../requestContext.js");
  inRequestContext: typeof import("../../__tests__/helpers/http.js").inRequestContext;
}> {
  vi.resetModules();
  vi.stubEnv("AI_CHAT_PERSIST_INTERVAL_MS", persistInterval);
  const core = await import("@onlyoffice/ai-chat/core");
  const calls = (core.AIEngine as unknown as typeof FakeAIEngine).deps;
  calls.length = 0;
  await import("../aiController.js");
  expect(calls).toHaveLength(1);
  const { storage } = await import("../../storage/index.js");
  const { systemToolsSource } = await import("../../tools/systemTools.js");
  // The request context of this load: the one the controller just imported.
  const context = await import("../../requestContext.js");
  const { inRequestContext } = await import("../../__tests__/helpers/http.js");
  return {
    deps: calls[0] as EngineDeps,
    storage,
    getServerTypes: systemToolsSource.getServerTypes as unknown as {
      mockReturnValue: (value: string[]) => void;
    },
    context,
    inRequestContext,
  };
}

// The first load transforms the controller's whole import graph; later loads reuse it.
beforeAll(async () => {
  await load();
  vi.unstubAllEnvs();
});

afterEach(() => {
  vi.unstubAllEnvs();
});

describe("the engine the controller builds", () => {
  it.each([
    ["absent", undefined, 10_000],
    ["empty", "", 10_000],
    ["not a number", "soon", 10_000],
    ["zero", "0", 10_000],
    ["negative", "-5", 10_000],
    ["a positive integer", "2500", 2500],
    ["the lower bound", "1000", 1000],
    ["under the lower bound", "999", 10_000],
    // 50 ms was taken before: about twenty writes a second for each streaming round.
    ["the library's own 150 ms", "150", 10_000],
    ["the upper bound", "600000", 600_000],
    ["over the upper bound", "600001", 10_000],
    ["a number with a unit", "30s", 10_000],
    ["a fraction", "1.9", 10_000],
    ["very large", "999999999999", 10_000],
  ])(
    "saves a streaming message every AI_CHAT_PERSIST_INTERVAL_MS ms when it is %s",
    async (_w, value, ms) => {
      const { deps } = await load(value);

      expect(deps.persistIntervalMs).toBe(ms);
    },
  );

  it("hands the engine the storage, tool progress and the composed tools adapter", async () => {
    const { deps, storage } = await load();

    expect(deps.storage).toBe(storage);
    expect(deps.toolProgress).toBe(true);
    expect(deps.toolsAdapter).toBeDefined();
    expect(Object.keys(deps).sort()).toEqual([
      "compaction",
      "persistIntervalMs",
      "resolveSource",
      "storage",
      "systemServerTypes",
      "toolProgress",
      "toolsAdapter",
    ]);
  });

  it("reads the round's source back from the request context", async () => {
    const { deps, context, inRequestContext } = await load();
    const source = { id: "8", type: "Room", title: "Reports" };

    const seen = inRequestContext({}, () => {
      context.setSourceMeta(source as never);
      return deps.resolveSource();
    });

    expect(seen).toEqual(source);
    expect(deps.resolveSource()).toBeUndefined();
  });

  it("asks for approval for system, custom, integration, document agent and plugin agent tools", async () => {
    const { deps, getServerTypes, context, inRequestContext } = await load();
    getServerTypes.mockReturnValue(["portal-mcp"]);

    const types = inRequestContext({}, () => {
      context.setCustomServerNames(["my-server"]);
      return deps.systemServerTypes();
    });

    expect(types).toEqual([
      "portal-mcp",
      "my-server",
      "docspace-integration-approval",
      "document-agent",
      "plugin-agent",
      "plugin-install",
    ]);
  });

  it("resolves the compaction setting on every round", async () => {
    const { deps } = await load();

    expect(deps.compaction()).toEqual(getCompaction());
  });
});
