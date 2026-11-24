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

import type { ProfilesEngine } from "@onlyoffice/ai-chat/core";
import { invoke } from "../../__tests__/helpers/http.js";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import type { Reply } from "../../__tests__/helpers/http.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";

const engine = engineMock<ProfilesEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  ProfilesEngine: engineClass(engine),
}));
const readByIdRaw = vi.fn<(id: string) => Promise<unknown>>();
const readAllForClient = vi.fn<() => Promise<unknown>>();
const readDisabled = vi.fn<(entityId?: string) => Promise<unknown>>();
vi.mock("../../storage/index.js", () => ({
  storage: { profiles: { readByIdRaw, readAllForClient }, toolPrefs: { readDisabled } },
}));
const listOnlyofficeGatewayModels = vi.fn<() => Promise<unknown>>();
vi.mock("../../storage/profilesStorage.js", () => ({ listOnlyofficeGatewayModels }));
// The SSRF guard of security.ts runs for real; only its DNS lookup is stubbed.
const lookup = vi.fn<(host: string, opts: unknown) => Promise<unknown>>();
vi.mock("dns/promises", () => ({ lookup, default: { lookup } }));

const { profilesController } = await import("../profilesController.js");
// After the mocks: these modules load the library, whose engine class is replaced above.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");

const upstream = useUpstream();

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

// GET internal/ai/config: SettingsController.GetAiSettingsAsync, AiSettingsDto
// (products/ASC.AI/Server/Models/ResponseDto/AiSettingsDto.cs).
const aiSettings = (systemAiEnabled: boolean) => ({
  vectorizationEnabled: true,
  vectorizationNeedReset: false,
  aiReady: true,
  embeddingModel: "text-embedding-3-small",
  systemAiEnabled,
  recommendedModelForForms: null,
  toolPermissionMode: 1,
});
const portalConfig = (systemAiEnabled: boolean) =>
  upstream.use(http.get("*/internal/ai/config", () => docspace(aiSettings(systemAiEnabled))));

const input = {
  name: "OpenAI",
  providerType: "openai",
  baseUrl: "https://api.openai.com/v1",
  key: "sk-test",
  modelId: "gpt-4o",
};
const stored = {
  id: "11111111-1111-1111-1111-111111111111",
  name: "OpenAI",
  providerType: "openai" as const,
  baseUrl: "https://api.openai.com/v1",
  modelId: "gpt-4o",
  capabilities: 3,
  createdAt: 1767225600000,
};
const model = { id: "gpt-4o", name: "GPT-4o", provider: "openai" as const, capabilities: 3 };

beforeEach(() => {
  mockReset(engine);
  for (const spy of [readByIdRaw, readAllForClient, readDisabled, listOnlyofficeGatewayModels]) {
    spy.mockReset();
  }
  lookup.mockReset().mockResolvedValue([{ address: "104.18.7.192", family: 4 }]);
  readDisabled.mockResolvedValue([]);
});

describe.each([
  ["create", "aiProfilesCreate"],
  ["update", "aiProfilesUpdate"],
] as const)("profilesController.%s", (method, operationId) => {
  const body = method === "create" ? input : { ...stored, key: "sk-test" };
  const engineCall = () => (method === "create" ? engine.create : engine.update);

  it("asks the AI service whether profiles are writable, then hands the body to the engine", async () => {
    portalConfig(false);
    engineCall().mockResolvedValue({ success: true, profile: stored });

    const reply = await invoke(profilesController[method], { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true, profile: stored });
    expect(engineCall()).toHaveBeenCalledWith(body);
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual(["GET /internal/ai/config"]);
    expect(lookup).toHaveBeenCalledWith("api.openai.com", { all: true });
    expect(published(operationId, reply)).toEqual([]);
  });

  it("answers 403 on a portal whose profiles the AI gateway manages, before any other work (Bug 83112)", async () => {
    portalConfig(true);

    const reply = await invoke(profilesController[method], {
      body: { ...body, baseUrl: "http://127.0.0.1", providerType: "external" },
    });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({
      error: "AI profiles are read-only on this portal (managed by the AI gateway)",
    });
    expect(lookup).not.toHaveBeenCalled();
    expect(engineCall()).not.toHaveBeenCalled();
    expect(published(operationId, reply)).toEqual([]);
  });

  it.each([
    ["500", () => docspaceError(500, "secret detail"), 500, "Internal Server Error"],
    ["403", () => docspaceError(403, "secret detail"), 403, "Forbidden"],
  ])(
    "relays a %s of the settings read and creates nothing",
    async (_what, answer, status, error) => {
      upstream.use(http.get("*/internal/ai/config", answer));

      const reply = await invoke(profilesController[method], { body });

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error });
      expect(engineCall()).not.toHaveBeenCalled();
      expect(JSON.stringify(reply.body)).not.toContain("secret detail");
      expect(published(operationId, reply)).toEqual([]);
    },
  );

  it("treats a settings answer that is not an object as writable", async () => {
    upstream.use(http.get("*/internal/ai/config", () => docspace("text")));
    engineCall().mockResolvedValue({ success: true, profile: stored });

    const reply = await invoke(profilesController[method], { body });

    expect(reply.status).toBe(200);
  });

  it("refuses the external provider type with 400 (Bug 83114)", async () => {
    portalConfig(false);

    const reply = await invoke(profilesController[method], {
      body: { ...body, providerType: "external" },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error:
        'providerType "external" cannot be used for server-managed profiles: it delegates transport to the host application',
    });
    expect(engineCall()).not.toHaveBeenCalled();
    expect(lookup).not.toHaveBeenCalled();
    expect(published(operationId, reply)).toEqual([]);
  });

  it.each([
    ["a number", 5, "baseUrl must be a string"],
    ["not a URL", "api.openai.com", "baseUrl is not a valid URL"],
    ["an ftp URL", "ftp://files.example.com", "baseUrl must use http or https"],
    [
      "a URL with credentials",
      "https://u:p@api.openai.com",
      "baseUrl must not contain credentials",
    ],
    ["a loopback address", "http://127.0.0.1:11434", "baseUrl host is not allowed"],
    ["the metadata address", "http://169.254.169.254/latest", "baseUrl host is not allowed"],
  ])("answers 400 for a baseUrl that is %s", async (_what, baseUrl, error) => {
    portalConfig(false);

    const reply = await invoke(profilesController[method], { body: { ...body, baseUrl } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engineCall()).not.toHaveBeenCalled();
    expect(published(operationId, reply)).toEqual([]);
  });

  it("answers 400 for a host that resolves to a private address", async () => {
    portalConfig(false);
    lookup.mockResolvedValue([{ address: "10.0.0.5", family: 4 }]);

    const reply = await invoke(profilesController[method], { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "baseUrl host is not allowed" });
  });

  it("answers 400 for a host that does not resolve", async () => {
    portalConfig(false);
    lookup.mockRejectedValue(
      Object.assign(new Error("getaddrinfo ENOTFOUND"), { code: "ENOTFOUND" }),
    );

    const reply = await invoke(profilesController[method], { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "baseUrl host could not be resolved" });
  });

  it.each([
    ["a non-ASCII character", "sk-tést"],
    ["a byte order mark", "﻿sk-test"],
    ["a line break", "sk-\ntest"],
    ["a number", 12345],
    ["an object", {}],
  ])("refuses a key with %s as the engine's own refusal shape (Bug 84428)", async (_what, key) => {
    portalConfig(false);

    const reply = await invoke(profilesController[method], { body: { ...body, key } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      error: {
        field: "key",
        message: "the API key contains characters that cannot be sent in an HTTP header",
        code: "invalid",
      },
    });
    expect(engineCall()).not.toHaveBeenCalled();
    expect(published(operationId, reply)).toEqual([]);
  });

  it.each([
    ["no key", undefined],
    ["a null key", null],
    ["an empty key", ""],
    ["a key with a tab", "sk\ttest"],
  ])("passes a body with %s to the engine", async (_what, key) => {
    portalConfig(false);
    engineCall().mockResolvedValue({ success: true, profile: stored });

    const reply = await invoke(profilesController[method], { body: { ...body, key } });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledTimes(1);
  });

  // The widget's ApiProvider hands a non-2xx body back as a value when it says `success: false`,
  // so the profile form reads the refusal as before.
  it.each([
    [
      "a key the provider refuses",
      { field: "key" as const, message: "Provider validation failed" },
    ],
    ["a taken name", { field: "name" as const, message: "Duplicate name" }],
    ["an unknown provider type", { field: "name" as const, message: "Unknown provider type: x" }],
  ])("answers 400 for a refusal of the engine: %s", async (_what, error) => {
    portalConfig(false);
    const refusal = { success: false as const, error };
    engineCall().mockResolvedValue(refusal);

    const reply = await invoke(profilesController[method], { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(refusal);
    expect(published(operationId, reply)).toEqual([]);
  });

  // The engine compares the name with the stored ones first, so a body without a string name
  // made it throw (500). The other fields are the engine's and the AI service's to judge.
  it.each([
    ["null", null],
    ["a string", "profile"],
    ["an array", [input]],
    ["{}", {}],
    ["a name that is a number", { ...body, name: 5 }],
    ["a name that is null", { ...body, name: null }],
  ])("answers 400 for a body that is %s, before the engine", async (_what, sent) => {
    portalConfig(false);

    const reply = await invoke(profilesController[method], { body: sent });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "name is required and must be a string" });
    expect(published(operationId, reply)).toEqual([]);
    expect(engineCall()).not.toHaveBeenCalled();
  });

  it.each([
    ["no modelId", { ...body, modelId: undefined }],
    ["a modelId that is an object", { ...body, modelId: {} }],
    ["a providerType that is a number", { ...body, providerType: 7 }],
    ["headers that are a string", { ...body, headers: "x" }],
    ["a name of 10 000 characters", { ...body, name: "n".repeat(10_000) }],
  ])("hands a body with %s to the engine, which judges it", async (_what, sent) => {
    portalConfig(false);
    engineCall().mockResolvedValue({ success: true, profile: stored });

    const reply = await invoke(profilesController[method], { body: sent });

    expect(reply.status).toBe(200);
    expect(engineCall()).toHaveBeenCalledWith(sent);
  });

  it("answers 500 without details when the engine throws", async () => {
    portalConfig(false);
    engineCall().mockRejectedValue(new Error("probe failed for https://api.openai.com"));

    const reply = await invoke(profilesController[method], { body });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(published(operationId, reply)).toEqual([]);
  });
});

describe("profilesController.delete", () => {
  it.each([
    ["the bare id as the body", { body: "p1" }],
    ["a named body", { body: { id: "p1" } }],
    ["the query", { query: { id: "p1" } }],
  ])("deletes the profile named by %s", async (_what, init) => {
    engine.delete.mockResolvedValue(undefined);

    const reply = await invoke(profilesController.delete, { method: "DELETE", ...init });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.delete).toHaveBeenCalledWith("p1");
    expect(published("aiProfilesDelete", reply)).toEqual([]);
  });

  it.each([
    ["null", { body: null }],
    ["an empty id", { body: "" }],
    ["{}", { body: {} }],
    ["an id that is a number", { body: { id: 1 } }],
    ["a repeated query id", { query: { id: ["a", "b"] } }],
  ])("answers 400 for %s", async (_what, init) => {
    const reply = await invoke(profilesController.delete, init);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "id required" });
    expect(engine.delete).not.toHaveBeenCalled();
    expect(published("aiProfilesDelete", reply)).toEqual([]);
  });

  it("relays a 403 of the AI service", async () => {
    engine.delete.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "x", "u"));

    const reply = await invoke(profilesController.delete, { body: "p1" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(published("aiProfilesDelete", reply)).toEqual([]);
  });
});

describe("profilesController.listProviderModels", () => {
  const body = { providerType: "openai", baseUrl: "https://api.openai.com/v1", apiKey: "sk-test" };

  it("lists the models of the provider named in the body", async () => {
    engine.listProviderModels.mockResolvedValue([model]);

    const reply = await invoke(profilesController.listProviderModels, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([model]);
    expect(readDisabled).toHaveBeenCalledWith(undefined);
    expect(engine.listProviderModels).toHaveBeenCalledWith(body);
    expect(published("aiProfilesListProviderModels", reply)).toEqual([]);
  });

  it("sends an empty key when the body has none", async () => {
    engine.listProviderModels.mockResolvedValue([]);

    await invoke(profilesController.listProviderModels, { body: { ...body, apiKey: undefined } });

    expect(engine.listProviderModels).toHaveBeenCalledWith({ ...body, apiKey: "" });
  });

  it("answers the 403 of the access gate for any body, before any other work (Bug 82824)", async () => {
    readDisabled.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    for (const sent of [null, {}, body, { ...body, baseUrl: "http://127.0.0.1" }]) {
      const reply = await invoke(profilesController.listProviderModels, { body: sent });

      expect(reply.status).toBe(403);
      expect(reply.body).toEqual({ error: "Forbidden" });
      expect(published("aiProfilesListProviderModels", reply)).toEqual([]);
    }
    expect(lookup).not.toHaveBeenCalled();
    expect(engine.listProviderModels).not.toHaveBeenCalled();
    expect(listOnlyofficeGatewayModels).not.toHaveBeenCalled();
  });

  it.each([
    ["null", null, "providerType required", "providerType"],
    ["{}", {}, "providerType required", "providerType"],
    ["a string", "openai", "providerType required", "providerType"],
    [
      "an empty providerType",
      { ...body, providerType: "" },
      "providerType required",
      "providerType",
    ],
    [
      "a null providerType",
      { ...body, providerType: null },
      "providerType required",
      "providerType",
    ],
    ["no baseUrl", { providerType: "openai" }, "baseUrl required", "baseUrl"],
    ["an empty baseUrl", { ...body, baseUrl: "" }, "baseUrl required", "baseUrl"],
    [
      "a non-ASCII apiKey",
      { ...body, apiKey: "ключ" },
      "the API key contains characters that cannot be sent in an HTTP header",
      "apiKey",
    ],
    [
      "an apiKey that is a number",
      { ...body, apiKey: 7 },
      "the API key contains characters that cannot be sent in an HTTP header",
      "apiKey",
    ],
  ])(
    "answers 400 naming the field for a body that is %s (Bug 83116)",
    async (_what, sent, error, field) => {
      const reply = await invoke(profilesController.listProviderModels, { body: sent });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error, field });
      expect(engine.listProviderModels).not.toHaveBeenCalled();
      expect(published("aiProfilesListProviderModels", reply)).toEqual([]);
    },
  );

  it.each([
    ["a number", 5, "baseUrl must be a string"],
    ["a loopback URL", "http://localhost.:11434", "baseUrl host is not allowed"],
    ["a private address", "http://192.168.1.10/v1", "baseUrl host is not allowed"],
  ])("answers 400 without a field for a baseUrl that is %s", async (_what, baseUrl, error) => {
    lookup.mockResolvedValue([{ address: "127.0.0.1", family: 4 }]);

    const reply = await invoke(profilesController.listProviderModels, {
      body: { ...body, baseUrl },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.listProviderModels).not.toHaveBeenCalled();
    expect(published("aiProfilesListProviderModels", reply)).toEqual([]);
  });

  // HANDLED DOWNSTREAM data-7: a providerType that is not a string (a number, an object) passes the check `!providerType` and reaches the engine. In production the engine refuses it through the real stack with 400 "Unknown provider type" (ASC.AI.Tests, ProfileProviderTypeTests); it shows only with the engine or the service stubbed.
  it("hands a providerType that is a number to the engine", async () => {
    engine.listProviderModels.mockResolvedValue([]);

    const reply = await invoke(profilesController.listProviderModels, {
      body: { ...body, providerType: 5 },
    });

    expect(reply.status).toBe(200);
    expect(engine.listProviderModels).toHaveBeenCalledWith({ ...body, providerType: 5 });
  });

  it("serves the onlyoffice provider from the gateway catalog (Bug 83113)", async () => {
    listOnlyofficeGatewayModels.mockResolvedValue([{ ...model, provider: "onlyoffice" }]);

    const reply = await invoke(profilesController.listProviderModels, {
      body: { ...body, providerType: "onlyoffice" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([{ ...model, provider: "onlyoffice" }]);
    expect(engine.listProviderModels).not.toHaveBeenCalled();
    expect(published("aiProfilesListProviderModels", reply)).toEqual([]);
  });

  it.each([
    ["an Error", new Error("gateway 503")],
    ["a non-Error", "gateway down"],
  ])(
    "falls back to the provider listing when the gateway fails with %s",
    async (_what, failure) => {
      listOnlyofficeGatewayModels.mockRejectedValue(failure);
      engine.listProviderModels.mockResolvedValue([model]);

      const reply = await invoke(profilesController.listProviderModels, {
        body: { ...body, providerType: "onlyoffice" },
      });

      expect(reply.status).toBe(200);
      expect(reply.body).toEqual([model]);
      expect(logger.warn).toHaveBeenCalledWith(
        `gateway models listing failed, falling back to the provider listing: ${
          failure instanceof Error ? failure.message : failure
        }`,
      );
    },
  );

  const unreachable =
    "The AI provider is unreachable — check the base URL and that the service is running";
  const withStatus = (status: number) =>
    Object.assign(new Error(`${status} status code`), { status });
  const withCode = (code: string) => Object.assign(new Error("connect failed"), { code });
  const nested = (depth: number, code: string): unknown => {
    let err: unknown = withCode(code);
    for (let i = 1; i < depth; i++) {
      err = Object.assign(new Error("wrapped"), { cause: err });
    }
    return err;
  };

  it.each([
    [
      "an unknown provider type",
      new Error("Unknown provider type: foo"),
      400,
      "Unknown provider type: foo",
    ],
    ["a profile that does not exist", new Error("Profile not found: x"), 404, "Profile not found"],
    [
      "a key fetch cannot put into a header",
      new TypeError("Cannot convert argument to a ByteString"),
      400,
      "the API key contains characters that cannot be sent in an HTTP header",
    ],
    [
      "an invalid header value",
      new TypeError("Invalid header value ERR_INVALID_CHAR"),
      400,
      "the API key contains characters that cannot be sent in an HTTP header",
    ],
    ["a 401 of the provider", withStatus(401), 400, "Invalid API key for the AI provider"],
    ["a 403 of the provider", withStatus(403), 400, "Invalid API key for the AI provider"],
    [
      "a 404 of the provider",
      withStatus(404),
      400,
      "Invalid base URL — expected an OpenAI-compatible endpoint (e.g. ending in /v1)",
    ],
    ["a 429 of the provider", withStatus(429), 400, "The AI provider rejected the request (429)"],
    ["a 500 of the provider", withStatus(500), 502, "The AI provider returned an error"],
    ["a 503 of the provider", withStatus(503), 502, "The AI provider returned an error"],
    ["a 302 of the provider", withStatus(302), 502, "Failed to list provider models"],
    ["ENOTFOUND", withCode("ENOTFOUND"), 502, unreachable],
    ["ECONNREFUSED in the cause chain", nested(3, "ECONNREFUSED"), 502, unreachable],
    [
      "UND_ERR_CONNECT_TIMEOUT five errors deep",
      nested(5, "UND_ERR_CONNECT_TIMEOUT"),
      502,
      unreachable,
    ],
    ["ETIMEDOUT six errors deep", nested(6, "ETIMEDOUT"), 502, "Failed to list provider models"],
    ["an unknown error code", withCode("EPIPE"), 502, "Failed to list provider models"],
    [
      "a TypeError of another kind",
      new TypeError("x is not a function"),
      502,
      "Failed to list provider models",
    ],
    ["a connection error message", new Error("Connection error."), 502, unreachable],
    ["a fetch failure message", new Error("fetch failed"), 502, unreachable],
    ["a timeout message", new Error("Request timeout"), 502, unreachable],
    ["a thrown string", "weird", 502, "Failed to list provider models"],
    ["a thrown null", null, 502, "Failed to list provider models"],
  ])("answers %s as %i", async (_what, failure, status, error) => {
    engine.listProviderModels.mockRejectedValue(failure);

    const reply = await invoke(profilesController.listProviderModels, { body });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(logger.warn).toHaveBeenCalledWith(
      expect.stringContaining(
        `listProviderModels failed (openai @ https://api.openai.com/v1) -> ${status}: `,
      ),
    );
    expect(published("aiProfilesListProviderModels", reply)).toEqual(
      status === 404
        ? ["aiProfilesListProviderModels publishes no JSON schema for status 404"]
        : [],
    );
  });
});

describe("profilesController.listModels", () => {
  const query = { profileId: stored.id };

  it("lists the models of a profile that exists", async () => {
    readByIdRaw.mockResolvedValue(stored);
    engine.listModels.mockResolvedValue([model]);

    const reply = await invoke(profilesController.listModels, { method: "GET", query });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([model]);
    expect(readByIdRaw).toHaveBeenCalledWith(stored.id);
    expect(engine.listModels).toHaveBeenCalledWith(stored.id);
    expect(published("aiProfilesListModels", reply)).toEqual([]);
  });

  it.each([[{}], [{ profileId: "" }], [{ profileId: ["a", "b"] }]])(
    "answers 400 for the query %j",
    async (sent) => {
      const reply = await invoke(profilesController.listModels, { method: "GET", query: sent });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "profileId required" });
      expect(readByIdRaw).not.toHaveBeenCalled();
      expect(published("aiProfilesListModels", reply)).toEqual([]);
    },
  );

  it("answers 404 for a profile the store does not have (Bug 84426)", async () => {
    readByIdRaw.mockResolvedValue(undefined);

    const reply = await invoke(profilesController.listModels, { method: "GET", query });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Profile not found" });
    expect(engine.listModels).not.toHaveBeenCalled();
    expect(published("aiProfilesListModels", reply)).toEqual([]);
  });

  it.each([
    [
      "a 500",
      new AiServiceHttpError(500, "Internal Server Error", "", "u"),
      500,
      "Internal Server Error",
    ],
    ["a timeout", new Error("upstream timeout after 300000ms"), 500, "Internal server error"],
  ])(
    "answers 5xx, not 404, when the profile read fails with %s",
    async (_what, failure, status, error) => {
      readByIdRaw.mockRejectedValue(failure);

      const reply = await invoke(profilesController.listModels, { method: "GET", query });

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error });
      expect(published("aiProfilesListModels", reply)).toEqual([]);
    },
  );

  it("relays a failure of the AI service inside the engine with its own status (Bug 82971)", async () => {
    readByIdRaw.mockResolvedValue(stored);
    engine.listModels.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "x", "u"));

    const reply = await invoke(profilesController.listModels, { method: "GET", query });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it.each([
    [
      "a 401 of the provider",
      Object.assign(new Error("401"), { status: 401 }),
      400,
      "Invalid API key for the AI provider",
    ],
    ["a thrown string", "weird", 502, "Failed to list provider models"],
  ])("maps %s like the provider listing", async (_what, failure, status, error) => {
    readByIdRaw.mockResolvedValue(stored);
    engine.listModels.mockRejectedValue(failure);

    const reply = await invoke(profilesController.listModels, { method: "GET", query });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(logger.warn).toHaveBeenCalledWith(
      expect.stringContaining(`listModels failed (profileId=${stored.id}) -> ${status}: `),
    );
    expect(published("aiProfilesListModels", reply)).toEqual([]);
  });
});

describe("profilesController.testConnection", () => {
  it.each([
    ["the bare id as the body", { body: stored.id }],
    ["a named body", { body: { profileId: stored.id } }],
    ["the query", { query: { profileId: stored.id } }],
  ])("tests the profile named by %s", async (_what, init) => {
    readByIdRaw.mockResolvedValue(stored);
    engine.testConnection.mockResolvedValue(true);

    const reply = await invoke(profilesController.testConnection, init);

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(true);
    expect(engine.testConnection).toHaveBeenCalledWith(stored.id);
    expect(published("aiProfilesTestConnection", reply)).toEqual([]);
  });

  it("answers a failed test with the engine's error data and 200", async () => {
    readByIdRaw.mockResolvedValue(stored);
    engine.testConnection.mockResolvedValue({ field: "key", message: "Invalid API key" });

    const reply = await invoke(profilesController.testConnection, { body: stored.id });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ field: "key", message: "Invalid API key" });
    expect(published("aiProfilesTestConnection", reply)).toEqual([]);
  });

  it.each([
    ["null", { body: null }],
    ["{}", { body: {} }],
    ["an empty id", { body: "" }],
    ["an id that is a number", { body: { profileId: 1 } }],
  ])("answers 400 for %s", async (_what, init) => {
    const reply = await invoke(profilesController.testConnection, init);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "profileId required" });
    expect(published("aiProfilesTestConnection", reply)).toEqual([]);
  });

  it("answers 404 for a profile the store does not have (Bug 84427)", async () => {
    readByIdRaw.mockResolvedValue(undefined);

    const reply = await invoke(profilesController.testConnection, { body: stored.id });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Profile not found" });
    expect(engine.testConnection).not.toHaveBeenCalled();
    expect(published("aiProfilesTestConnection", reply)).toEqual([]);
  });

  it("answers 500, not 404, when the profile read fails", async () => {
    readByIdRaw.mockRejectedValue(new AiServiceHttpError(500, "Internal Server Error", "", "u"));

    const reply = await invoke(profilesController.testConnection, { body: stored.id });

    expect(reply.status).toBe(500);
    expect(engine.testConnection).not.toHaveBeenCalled();
  });
});

describe("profilesController.getById", () => {
  it("answers the stored profile without its key and headers (Bug 82821)", async () => {
    readByIdRaw.mockResolvedValue({
      ...stored,
      key: "MARKER-KEY",
      headers: { "X-Api": "MARKER-H" },
    });

    const reply = await invoke(profilesController.getById, {
      method: "GET",
      query: { id: stored.id },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(stored);
    expect(JSON.stringify(reply.body)).not.toContain("MARKER");
    expect(published("aiProfilesGetById", reply)).toEqual([]);
  });

  it("answers the listed profile fields only, whatever else the stored profile carries", async () => {
    readByIdRaw.mockResolvedValue({ ...stored, byokProvider: "openai", extra: 1 });

    const reply = await invoke(profilesController.getById, {
      method: "GET",
      query: { id: stored.id },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(stored);
    expect(published("aiProfilesGetById", reply)).toEqual([]);
  });

  it.each([[{}], [{ id: "" }], [{ id: ["a"] }]])("answers 400 for the query %j", async (query) => {
    const reply = await invoke(profilesController.getById, { method: "GET", query });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "id required" });
    expect(readByIdRaw).not.toHaveBeenCalled();
    expect(published("aiProfilesGetById", reply)).toEqual([]);
  });

  it("answers 404 for a profile the store does not have", async () => {
    readByIdRaw.mockResolvedValue(undefined);

    const reply = await invoke(profilesController.getById, { method: "GET", query: { id: "x" } });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Profile not found" });
    expect(published("aiProfilesGetById", reply)).toEqual([]);
  });

  it("answers 500, not 404, when the read fails", async () => {
    readByIdRaw.mockRejectedValue(new AiServiceHttpError(500, "Internal Server Error", "", "u"));

    const reply = await invoke(profilesController.getById, { method: "GET", query: { id: "x" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
  });
});

describe("profilesController.list", () => {
  it("answers the profiles as the store reads them for a client", async () => {
    readAllForClient.mockResolvedValue([stored]);

    const reply = await invoke(profilesController.list, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([stored]);
    expect(published("aiProfilesList", reply)).toEqual([]);
  });

  it("relays a 403 of the AI service", async () => {
    readAllForClient.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await invoke(profilesController.list, { method: "GET" });

    expect(reply.status).toBe(403);
    expect(published("aiProfilesList", reply)).toEqual([]);
  });
});
