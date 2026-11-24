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
import type { Reply } from "../../__tests__/helpers/http.js";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { invoke } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { settingsController } from "../settingsController.js";

// Nothing is mocked here: the real httpClient.js calls the AI service, which msw answers.
const upstream = useUpstream();

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

// The answers of products/ASC.AI/Server/Api/SettingsController.cs, in the envelope the .NET
// services wrap a result in. AiSettingsDto.cs, VectorizationSettingsDto.cs, AiUserSettingsDto.cs
// and ToolPermissionModeDto.cs under products/ASC.AI/Server/Models/ResponseDto/.
const aiSettings = {
  vectorizationEnabled: true,
  vectorizationNeedReset: false,
  aiReady: true,
  embeddingModel: "text-embedding-3-small",
  systemAiEnabled: false,
  recommendedModelForForms: "gpt-4o",
  toolPermissionMode: 1,
};
const vectorization = { type: 1, needReset: false };
const userSettings = { chatRecommendedModelVisible: true, toolPermissionMode: 0 };
const toolMode = { mode: 2 };
const envelope = (response: unknown) => ({ response, count: 1, status: 0, statusCode: 200 });

describe.each([
  ["getAiSettings", "aiSettingsGet", "/internal/ai/config", aiSettings],
  [
    "getVectorizationSettings",
    "aiSettingsGetVectorization",
    "/internal/ai/config/vectorization",
    vectorization,
  ],
  ["getUserSettings", "aiSettingsGetUser", "/internal/ai/config/user", userSettings],
  ["getToolMode", "aiSettingsGetToolMode", "/internal/ai/config/tool-mode", toolMode],
] as const)("settingsController.%s", (method, operationId, path, dto) => {
  it("answers the AI service's answer with its envelope", async () => {
    upstream.use(http.get(`*${path}`, () => docspace(dto)));

    const reply = await invoke(settingsController[method], { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(envelope(dto));
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([`GET ${path}`]);
    expect(published(operationId, reply)).toEqual([]);
  });

  it.each([
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])("relays a %i of the AI service without its body", async (status, error) => {
    upstream.use(http.get(`*${path}`, () => docspaceError(status, "secret detail")));

    const reply = await invoke(settingsController[method], { method: "GET" });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(JSON.stringify(reply.body)).not.toContain("secret");
    expect(published(operationId, reply)).toEqual([]);
  });
});

it("forwards the caller's credentials to the AI service", async () => {
  upstream.use(http.get("*/internal/ai/config/user", () => docspace(userSettings)));

  await inRequestContext({ cookie: "asc_auth_key=abc", authorization: "Bearer t" }, () =>
    invoke(settingsController.getUserSettings, { method: "GET" }),
  );

  expect(upstream.seen[0]?.headers).toMatchObject({
    cookie: "asc_auth_key=abc",
    authorization: "Bearer t",
  });
});

// products/ASC.AI/Server/Api/SettingsController.cs answers every one of these routes with a DTO in
// the .NET envelope; a body that is not a JSON object came from something else in front of it.
it.each([
  ["getAiSettings", "aiSettingsGet", "get", "/internal/ai/config", undefined],
  [
    "getVectorizationSettings",
    "aiSettingsGetVectorization",
    "get",
    "/internal/ai/config/vectorization",
    undefined,
  ],
  [
    "setVectorizationSettings",
    "aiSettingsSetVectorization",
    "put",
    "/internal/ai/config/vectorization",
    { type: 1 },
  ],
  ["getUserSettings", "aiSettingsGetUser", "get", "/internal/ai/config/user", undefined],
  [
    "setUserSettings",
    "aiSettingsSetUser",
    "put",
    "/internal/ai/config/user",
    { chatRecommendedModelVisible: true },
  ],
  ["getToolMode", "aiSettingsGetToolMode", "get", "/internal/ai/config/tool-mode", undefined],
  ["setToolMode", "aiSettingsSetToolMode", "put", "/internal/ai/config/tool-mode", { mode: 1 }],
] as const)(
  "%s answers 502 when the AI service answers a body that is not a JSON object",
  async (method, operationId, verb, path, body) => {
    upstream.use(http[verb](`*${path}`, () => HttpResponse.text("<html>proxy</html>")));

    const reply = await invoke(settingsController[method], {
      method: verb === "get" ? "GET" : "PUT",
      body,
    });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
    expect(JSON.stringify(reply.body)).not.toContain("proxy");
    expect(published(operationId, reply)).toEqual([]);
  },
);

describe.each([
  [
    "setVectorizationSettings",
    "aiSettingsSetVectorization",
    "/internal/ai/config/vectorization",
    vectorization,
  ],
  ["setToolMode", "aiSettingsSetToolMode", "/internal/ai/config/tool-mode", toolMode],
] as const)("settingsController.%s", (method, operationId, path, dto) => {
  // CONTRACT data-18: the body is forwarded to the AI service as it came, whatever its fields. Pinned by openspec/specs/ai-agents/spec.md ("403 others whatever the body") and ASC.AI.Tests (SettingsVectorizationPermissionsTests): the AI service judges the body, after its access check.
  it.each([
    ["a well-formed body", { type: 1, key: "sk-x" }, { type: 1, key: "sk-x" }],
    ["{}", {}, {}],
    ["null", null, {}],
    ["no body", undefined, {}],
    ["a string", "x", "x"],
    ["an array", [1], [1]],
    ["unknown fields", { mode: "nonsense", extra: true }, { mode: "nonsense", extra: true }],
  ])("forwards %s to the AI service and answers its envelope", async (_what, body, sent) => {
    upstream.use(http.put(`*${path}`, () => docspace(dto)));

    const reply = await invoke(settingsController[method], { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(envelope(dto));
    expect(upstream.seen).toMatchObject([{ method: "PUT", path, body: sent }]);
    expect(published(operationId, reply)).toEqual([]);
  });

  it("relays a 400 of the AI service", async () => {
    upstream.use(
      http.put(`*${path}`, () => docspaceError(400, "The mode is not one of the declared members")),
    );

    const reply = await invoke(settingsController[method], { method: "PUT", body: { mode: 9 } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "Bad Request" });
    expect(published(operationId, reply)).toEqual([]);
  });
});

describe("settingsController.setUserSettings", () => {
  it.each([true, false])("stores chatRecommendedModelVisible %s", async (value) => {
    upstream.use(
      http.put("*/internal/ai/config/user", () =>
        docspace({ ...userSettings, chatRecommendedModelVisible: value }),
      ),
    );

    const reply = await invoke(settingsController.setUserSettings, {
      method: "PUT",
      body: { chatRecommendedModelVisible: value },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(envelope({ ...userSettings, chatRecommendedModelVisible: value }));
    expect(upstream.seen).toMatchObject([
      {
        method: "PUT",
        path: "/internal/ai/config/user",
        body: { chatRecommendedModelVisible: value },
      },
    ]);
    expect(published("aiSettingsSetUser", reply)).toEqual([]);
  });

  it("forwards the other fields of the body as they came", async () => {
    upstream.use(http.put("*/internal/ai/config/user", () => docspace(userSettings)));

    await invoke(settingsController.setUserSettings, {
      body: { chatRecommendedModelVisible: true, toolPermissionMode: 2, x: 1 },
    });

    expect(upstream.seen[0]?.body).toEqual({
      chatRecommendedModelVisible: true,
      toolPermissionMode: 2,
      x: 1,
    });
  });

  it.each([
    ["null", null],
    ["no body (Bug 84430)", undefined],
    ["a string", "true"],
    ["an array", [true]],
    ["{}", {}],
    ["a null value", { chatRecommendedModelVisible: null }],
    ["the string true", { chatRecommendedModelVisible: "true" }],
    ["the number 1", { chatRecommendedModelVisible: 1 }],
  ])("answers 400 for a body that is %s, with no call", async (_what, body) => {
    const reply = await invoke(settingsController.setUserSettings, { method: "PUT", body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: "chatRecommendedModelVisible is required and must be a boolean",
    });
    expect(upstream.seen).toEqual([]);
    expect(published("aiSettingsSetUser", reply)).toEqual([]);
  });
});
