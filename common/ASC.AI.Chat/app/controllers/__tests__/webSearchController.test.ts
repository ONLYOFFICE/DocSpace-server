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

import type { WebSearchEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { invoke } from "../../__tests__/helpers/http.js";
import type { Reply } from "../../__tests__/helpers/http.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";

// The web-search settings routes. The engine of the library is the boundary (engineMock);
// the access gate (a read of `internal/ai/web-search`), the entry check (the Files API) and
// the base-URL guard run for real, against msw and a stubbed DNS.

const engine = engineMock<WebSearchEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  WebSearchEngine: engineClass(engine),
}));
vi.mock("dns/promises", () => ({
  lookup: vi.fn(() => Promise.resolve([{ address: "93.184.216.34", family: 4 }])),
}));

const upstream = useUpstream();
const { aiServiceBaseUrl, proxyBaseUrl } = await import("../../storage/httpClient.js");
const { webSearchController } = await import("../webSearchController.js");
// After the engine mock exists: the schema helper loads the library through apiCatalog.ts.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");

beforeEach(() => {
  mockReset(engine);
});

// products/ASC.AI/Server/Models/ResponseDto/WebSearchConfigDto.cs, the gate's read.
function gateAnswers(status: number): void {
  upstream.use(
    http.get(`${aiServiceBaseUrl}/internal/ai/web-search`, () =>
      status === 200
        ? docspace({ provider: "exa", key: null, baseUrl: null })
        : docspaceError(status),
    ),
  );
}

// products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs, GET files/folder/{id}.
function folderAnswers(id: string, status: number): void {
  upstream.use(
    http.get(`${proxyBaseUrl}/api/2.0/files/folder/${id}`, () =>
      status === 200
        ? docspace({ id: Number(id), title: "Room", roomType: 5 })
        : docspaceError(status),
    ),
  );
}

const config = { provider: "searxng", baseUrl: "https://search.test" };

const paths = () => upstream.seen.map((r) => `${r.method} ${r.path}`);

describe.each([
  ["getActiveConfig", "aiWebSearchGetActiveConfig"],
  ["isConfigured", "aiWebSearchIsConfigured"],
] as const)("webSearchController.%s", (method, operationId) => {
  const call = (query: Record<string, unknown>) =>
    invoke(webSearchController[method], { method: "GET", query });
  const engineCall = () =>
    method === "getActiveConfig" ? engine.getActiveConfig : engine.isConfigured;
  const answer = method === "getActiveConfig" ? { provider: "searxng" } : true;

  beforeEach(() => {
    engine.getActiveConfig.mockResolvedValue({ provider: "searxng" });
    engine.isConfigured.mockResolvedValue(true);
  });

  it.each([
    ["absent", {}],
    ["empty", { entityId: "" }],
    ["blank", { entityId: "  " }],
    ["null", { entityId: null }],
  ])(
    "reads the portal-wide scope when entityId is %s, without asking the Files API",
    async (_what, query) => {
      const reply = await call(query);

      expect(reply.status).toBe(200);
      expect(reply.body).toEqual(answer);
      expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
      expect(engineCall()).toHaveBeenCalledWith(undefined);
      expect(upstream.seen).toEqual([]);
    },
  );

  it("reads the scope of an entry the caller can open", async () => {
    folderAnswers("17", 200);

    const reply = await call({ entityId: "17" });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith("17");
    expect(paths()).toEqual(["GET /api/2.0/files/folder/17"]);
  });

  it.each([
    ["repeated (an array)", ["1", "2"]],
    ["a number", 17],
  ])("answers 400 when entityId is %s", async (_what, entityId) => {
    const reply = await call({ entityId });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(engineCall()).not.toHaveBeenCalled();
  });

  it.each([
    [404, { error: 'Entity "17" not found' }],
    [403, { error: 'Entity "17" is not accessible' }],
    [500, { error: "Internal Server Error" }],
  ])("answers %i when the Files API answers so for the entry", async (status, body) => {
    folderAnswers("17", status);

    const reply = await call({ entityId: "17" });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(body);
    expect(responseViolations(operationId, status, reply.body)).toEqual([]);
    expect(engineCall()).not.toHaveBeenCalled();
  });

  it("answers 500 when the engine fails", async () => {
    engineCall().mockRejectedValue(new Error("boom"));

    const reply = await call({});

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("webSearchController.getActiveConfig", () => {
  it("answers null when nothing is configured", async () => {
    engine.getActiveConfig.mockResolvedValue(null);

    const reply = await invoke(webSearchController.getActiveConfig, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(responseViolations("aiWebSearchGetActiveConfig", 200, reply.body)).toEqual([]);
  });
});

const configErrors = {
  required: "config is required and must be an object",
  provider: "config.provider is required and must be a non-empty string",
  key: "config.key must be a string",
  baseUrl: "config.baseUrl must be a string",
  isCloudProvider: "config.isCloudProvider must be a boolean",
  headers: "config.headers must be an object",
  headerValue: "config.headers values must be strings",
};

// Each field of AiWebSearchConfig missing, null, of the wrong type and empty.
const badConfigs: Array<[string, unknown, string]> = [
  ["null", null, configErrors.required],
  ["a string", "exa", configErrors.required],
  ["an array", [config], configErrors.required],
  ["no provider", { key: "k" }, configErrors.provider],
  ["a null provider", { provider: null }, configErrors.provider],
  ["a provider that is a number", { provider: 1 }, configErrors.provider],
  ["an empty provider", { provider: "" }, configErrors.provider],
  ["a blank provider", { provider: "  " }, configErrors.provider],
  ["a key that is a number", { provider: "exa", key: 1 }, configErrors.key],
  ["a baseUrl that is a number", { provider: "exa", baseUrl: 1 }, configErrors.baseUrl],
  [
    "an isCloudProvider that is a string",
    { provider: "exa", isCloudProvider: "true" },
    configErrors.isCloudProvider,
  ],
  ["headers that are a string", { provider: "exa", headers: "X-A: 1" }, configErrors.headers],
  ["headers that are an array", { provider: "exa", headers: [] }, configErrors.headers],
  [
    "a header value that is a number",
    { provider: "exa", headers: { "X-A": 1 } },
    configErrors.headerValue,
  ],
  [
    "a baseUrl that is not a URL",
    { provider: "exa", baseUrl: "search" },
    "baseUrl is not a valid URL",
  ],
  [
    "a baseUrl of another scheme",
    { provider: "exa", baseUrl: "ftp://search.test" },
    "baseUrl must use http or https",
  ],
  [
    "a baseUrl with credentials",
    { provider: "exa", baseUrl: "https://u:p@search.test" },
    "baseUrl must not contain credentials",
  ],
  [
    "a baseUrl on a private address",
    { provider: "exa", baseUrl: "http://10.0.0.5" },
    "baseUrl host is not allowed",
  ],
];

// Fields set to null count as "not set" and pass.
const nullFields = {
  provider: "exa",
  key: null,
  baseUrl: null,
  isCloudProvider: null,
  headers: null,
};

describe("webSearchController.testConnection", () => {
  const call = (body: unknown) => invoke(webSearchController.testConnection, { body });

  it("answers what the engine's probe answers when it passes", async () => {
    gateAnswers(200);
    engine.testConnection.mockResolvedValue(true);

    const reply = await call(config);

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(true);
    expect(responseViolations("aiWebSearchTestConnection", 200, reply.body)).toEqual([]);
    expect(engine.testConnection).toHaveBeenCalledWith(config);
    expect(paths()).toEqual(["GET /internal/ai/web-search"]);
  });

  it("answers a refused probe with 200 and the engine's field error", async () => {
    gateAnswers(200);
    engine.testConnection.mockResolvedValue({ field: "key", message: "Invalid key" });

    const reply = await call(config);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ field: "key", message: "Invalid key" });
    expect(responseViolations("aiWebSearchTestConnection", 200, reply.body)).toEqual([]);
  });

  it("takes null optional fields as not set", async () => {
    gateAnswers(404);
    engine.testConnection.mockResolvedValue(true);

    const reply = await call(nullFields);

    expect(reply.status).toBe(200);
    expect(engine.testConnection).toHaveBeenCalledWith({ provider: "exa" });
  });

  it("hands the engine the documented fields only", async () => {
    gateAnswers(404);
    engine.testConnection.mockResolvedValue(true);
    const full = {
      provider: "onlyoffice",
      key: "k",
      baseUrl: "https://search.test",
      isCloudProvider: false,
      headers: { "X-Tenant": "t" },
    };

    const reply = await call({ ...full, extra: "x" });

    expect(reply.status).toBe(200);
    expect(engine.testConnection).toHaveBeenCalledWith(full);
  });

  it.each(badConfigs)(
    "answers 400 to a config that is %s, and probes nothing",
    async (_what, body, error) => {
      gateAnswers(404);

      const reply = await call(body);

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error });
      expect(responseViolations("aiWebSearchTestConnection", 400, reply.body)).toEqual([]);
      expect(engine.testConnection).not.toHaveBeenCalled();
    },
  );

  it.each([
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])(
    "answers the gate's %i before the body is judged (Bugs 83234, 83235)",
    async (status, error) => {
      gateAnswers(status);

      const reply = await call(null);

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error });
      expect(responseViolations("aiWebSearchTestConnection", status, reply.body)).toEqual([]);
      expect(engine.testConnection).not.toHaveBeenCalled();
      expect(paths()).toEqual(["GET /internal/ai/web-search"]);
    },
  );
});

describe.each([
  ["configure", "aiWebSearchConfigure"],
  ["setActiveConfig", "aiWebSearchSetActiveConfig"],
] as const)("webSearchController.%s", (method, operationId) => {
  const call = (body: unknown) => invoke(webSearchController[method], { method: "PUT", body });
  const engineCall = () => (method === "configure" ? engine.configure : engine.setActiveConfig);
  const success = method === "configure" ? { success: true, config } : { success: true };

  beforeEach(() => {
    engine.configure.mockResolvedValue({ success: true, config });
    engine.setActiveConfig.mockResolvedValue(undefined);
  });

  it.each([
    ["a positional array", [config]],
    ["a named config", { config }],
    ["a config named body", { body: config }],
    ["a positional array with an empty entityId", [config, ""]],
    ["a named config with a null entityId", { config, entityId: null }],
  ])("stores the portal-wide config sent as %s", async (_what, body) => {
    gateAnswers(404);

    const reply = await call(body);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(success);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
    expect(engineCall()).toHaveBeenCalledWith(config, undefined);
    expect(paths()).toEqual(["GET /internal/ai/web-search"]);
  });

  it("stores the config of an entry the caller can open", async () => {
    gateAnswers(200);
    folderAnswers("17", 200);

    const reply = await call([config, "17"]);

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith(config, "17");
    expect(paths()).toEqual(["GET /internal/ai/web-search", "GET /api/2.0/files/folder/17"]);
  });

  it.each([
    ["null", null],
    ["a string", "exa"],
    ["an empty array", []],
    ["an empty object", {}],
    ["the config unwrapped (Bug 82812)", config],
  ])("answers 400 to a body that is %s", async (_what, body) => {
    gateAnswers(404);

    const reply = await call(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: configErrors.required });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(engineCall()).not.toHaveBeenCalled();
  });

  it.each(badConfigs)("answers 400 to a config that is %s", async (_what, bad, error) => {
    gateAnswers(404);

    const reply = await call({ config: bad });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engineCall()).not.toHaveBeenCalled();
  });

  it.each([
    ["a number", 17],
    ["an array", ["17"]],
  ])("answers 400 when entityId is %s", async (_what, entityId) => {
    gateAnswers(404);

    const reply = await call([config, entityId]);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
  });

  it.each([
    [404, { error: 'Entity "17" not found' }],
    [403, { error: 'Entity "17" is not accessible' }],
  ])(
    "answers %i for an entry the Files API answers so for, before the body is judged",
    async (status, error) => {
      gateAnswers(404);
      folderAnswers("17", status);

      const reply = await call([null, "17"]);

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual(error);
      expect(responseViolations(operationId, status, reply.body)).toEqual([]);
      expect(engineCall()).not.toHaveBeenCalled();
    },
  );

  it.each([
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])(
    "answers the gate's %i before the entry or the body is looked at (Bugs 84312, 84431)",
    async (status, error) => {
      gateAnswers(status);

      const reply = await call([null, "17"]);

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error });
      expect(responseViolations(operationId, status, reply.body)).toEqual([]);
      expect(paths()).toEqual(["GET /internal/ai/web-search"]);
    },
  );

  it("answers 500 when the engine fails", async () => {
    gateAnswers(404);
    engineCall().mockRejectedValue(new Error("boom"));

    const reply = await call([config]);

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("webSearchController.configure", () => {
  it("answers a configuration the provider probe refused with 400 and the engine's result (Bug 83994)", async () => {
    gateAnswers(404);
    const refusal = {
      success: false as const,
      error: { field: "key" as const, message: "Invalid key" },
    };
    engine.configure.mockResolvedValue(refusal);

    const reply: Reply = await invoke(webSearchController.configure, { body: [config] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(refusal);
    expect(responseViolations("aiWebSearchConfigure", 400, reply.body)).toEqual([]);
  });
});

describe("webSearchController.clear", () => {
  it("clears the config and answers success", async () => {
    engine.clear.mockResolvedValue(undefined);

    const reply = await invoke(webSearchController.clear, { method: "DELETE", body: "17" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(responseViolations("aiWebSearchClear", 200, reply.body)).toEqual([]);
    expect(engine.clear).toHaveBeenCalledWith();
  });

  it("answers the status of an AI-service failure", async () => {
    const { AiServiceHttpError } = await import("../../storage/httpClient.js");
    engine.clear.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await invoke(webSearchController.clear, { method: "DELETE" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiWebSearchClear", 403, reply.body)).toEqual([]);
  });
});
