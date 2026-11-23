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

import { invoke } from "../../__tests__/helpers/http.js";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Reply } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";

const byokKeys = {
  readAll: vi.fn<() => Promise<unknown>>(),
  readProviders: vi.fn<() => Promise<unknown>>(),
  set: vi.fn<(id: string, input: { provider: string; key: string }) => Promise<unknown>>(),
  delete: vi.fn<(id: string) => Promise<void>>(),
};
vi.mock("../../storage/index.js", () => ({ storage: { byokKeys } }));

const { byokKeysController } = await import("../byokKeysController.js");

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

const key = { id: "rev-1", name: "Claude Sonnet", provider: "anthropic", image: "<svg/>" };
const provider = {
  id: "anthropic",
  name: "Anthropic",
  models: [{ id: "rev-1", name: "Claude Sonnet" }],
};
// A refusal of PUT internal/ai/byok/{id} as the .NET services write one: the envelope of
// docspaceError in helpers/upstream.ts.
const dotnetError = (message: string) =>
  JSON.stringify({ error: { message }, status: 1, statusCode: 400 });

beforeEach(() => {
  for (const spy of Object.values(byokKeys)) {
    spy.mockReset();
  }
});

describe("byokKeysController.list and providers", () => {
  it("answers the keys as the storage reads them", async () => {
    byokKeys.readAll.mockResolvedValue([key]);

    const reply = await invoke(byokKeysController.list, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([key]);
    expect(published("aiByokKeysList", reply)).toEqual([]);
  });

  it("answers the providers as the storage reads them", async () => {
    byokKeys.readProviders.mockResolvedValue([provider]);

    const reply = await invoke(byokKeysController.providers, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([provider]);
    expect(published("aiByokProviders", reply)).toEqual([]);
  });

  it.each([
    ["list", "aiByokKeysList", "readAll"],
    ["providers", "aiByokProviders", "readProviders"],
  ] as const)("%s relays a 403 of the AI service without its body", async (method, op, read) => {
    byokKeys[read].mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", dotnetError("secret"), "u"),
    );

    const reply = await invoke(byokKeysController[method], { method: "GET" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(published(op, reply)).toEqual([]);
  });
});

describe("byokKeysController.set", () => {
  it("sets the key of the model named in the path", async () => {
    byokKeys.set.mockResolvedValue(key);

    const reply = await invoke(byokKeysController.set, {
      method: "PUT",
      params: { id: "rev-1" },
      body: { provider: "anthropic", key: "sk-ant", extra: 1 },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(key);
    expect(byokKeys.set).toHaveBeenCalledWith("rev-1", { provider: "anthropic", key: "sk-ant" });
    expect(published("aiByokKeysSet", reply)).toEqual([]);
  });

  // DEFECT data-20: a body that is not an object, or a provider or key that is missing or of the wrong type, is sent to the AI service as an empty string instead of being refused — expected 400 naming the field (ts-ai-failure-paths.md §4)
  it.each([
    ["null", null, { provider: "", key: "" }],
    ["a string", "sk-ant", { provider: "", key: "" }],
    ["an array", ["anthropic", "sk"], { provider: "", key: "" }],
    ["{}", {}, { provider: "", key: "" }],
    ["a provider that is a number", { provider: 1, key: "sk" }, { provider: "", key: "sk" }],
    [
      "a key that is null",
      { provider: "anthropic", key: null },
      { provider: "anthropic", key: "" },
    ],
  ])("sends a body that is %s with empty strings", async (_what, body, input) => {
    byokKeys.set.mockResolvedValue(key);

    const reply = await invoke(byokKeysController.set, { params: { id: "rev-1" }, body });

    expect(reply.status).toBe(200);
    expect(byokKeys.set).toHaveBeenCalledWith("rev-1", input);
  });

  it("sends an empty id when the path has none", async () => {
    byokKeys.set.mockResolvedValue(key);

    await invoke(byokKeysController.set, { body: { provider: "anthropic", key: "sk" } });

    expect(byokKeys.set).toHaveBeenCalledWith("", { provider: "anthropic", key: "sk" });
  });

  it("answers a 400 of the AI service with the message the service wrote", async () => {
    byokKeys.set.mockRejectedValue(
      new AiServiceHttpError(
        400,
        "Bad Request",
        dotnetError("The key is not valid for anthropic"),
        "u",
      ),
    );

    const reply = await invoke(byokKeysController.set, {
      params: { id: "rev-1" },
      body: { provider: "anthropic", key: "bad" },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "The key is not valid for anthropic" });
    expect(published("aiByokKeysSet", reply)).toEqual([]);
  });

  it.each([
    ["not JSON", "<html>Bad Request</html>"],
    ["JSON without an error", JSON.stringify({ status: 1 })],
    ["JSON whose error is a string", JSON.stringify({ error: "bad" })],
    ["JSON whose error has no message", JSON.stringify({ error: { code: 1 } })],
    ["a JSON array", "[]"],
  ])(
    "answers a 400 of the AI service whose body is %s with its reason phrase",
    async (_what, body) => {
      byokKeys.set.mockRejectedValue(new AiServiceHttpError(400, "Bad Request", body, "u"));

      const reply = await invoke(byokKeysController.set, { params: { id: "rev-1" }, body: {} });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "Bad Request" });
    },
  );

  it("relays another status of the AI service without its message", async () => {
    byokKeys.set.mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", dotnetError("secret"), "u"),
    );

    const reply = await invoke(byokKeysController.set, { params: { id: "rev-1" }, body: {} });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(published("aiByokKeysSet", reply)).toEqual([]);
  });

  it("answers 500 for an answer of the AI service the storage cannot map", async () => {
    byokKeys.set.mockRejectedValue(new Error("ai service returned invalid byok key"));

    const reply = await invoke(byokKeysController.set, { params: { id: "rev-1" }, body: {} });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(published("aiByokKeysSet", reply)).toEqual([]);
  });
});

describe("byokKeysController.delete", () => {
  it("deletes the key of the model named in the path and answers the published { success }", async () => {
    byokKeys.delete.mockResolvedValue(undefined);

    const reply = await invoke(byokKeysController.delete, {
      method: "DELETE",
      params: { id: "rev-1" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(byokKeys.delete).toHaveBeenCalledWith("rev-1");
    expect(published("aiByokKeysDelete", reply)).toEqual([]);
  });

  it("sends an empty id when the path has none", async () => {
    byokKeys.delete.mockResolvedValue(undefined);

    await invoke(byokKeysController.delete, { method: "DELETE" });

    expect(byokKeys.delete).toHaveBeenCalledWith("");
  });

  it("relays a 404 of the AI service", async () => {
    byokKeys.delete.mockRejectedValue(new AiServiceHttpError(404, "Not Found", "", "u"));

    const reply = await invoke(byokKeysController.delete, {
      method: "DELETE",
      params: { id: "x" },
    });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Not Found" });
    expect(published("aiByokKeysDelete", reply)).toEqual([]);
  });
});
