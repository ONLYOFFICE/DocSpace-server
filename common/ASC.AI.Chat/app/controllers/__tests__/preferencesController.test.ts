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

import type { PreferencesEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import type { Reply } from "../../__tests__/helpers/http.js";
import { call } from "./promptsControllerData.helpers.js";

const engine = engineMock<PreferencesEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  PreferencesEngine: engineClass(engine),
}));
// The two depth methods are optional on the storage contract, so a test may take them away.
const preferences: {
  readReasoningLevel?: (entityId?: string) => Promise<unknown>;
  upsertReasoningLevel?: (value: string, entityId?: string) => Promise<void>;
} = {};
vi.mock("../../storage/index.js", () => ({ storage: { preferences } }));

const { preferencesController } = await import("../preferencesController.js");
// After the mocks: these modules load the library, whose engine class is replaced above.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

const readReasoningLevel = vi.fn<(entityId?: string) => Promise<unknown>>();
const upsertReasoningLevel = vi.fn<(value: string, entityId?: string) => Promise<void>>();

beforeEach(() => {
  mockReset(engine);
  readReasoningLevel.mockReset();
  upsertReasoningLevel.mockReset().mockResolvedValue(undefined);
  preferences.readReasoningLevel = readReasoningLevel;
  preferences.upsertReasoningLevel = upsertReasoningLevel;
});

const badScope = { error: "entityId must be a room id (a positive integer)" };

// What the routes take as `entityId`, and what they pass on as the scope.
const acceptedScopes: [string, unknown, string | undefined][] = [
  ["absent", undefined, undefined],
  ["null", null, undefined],
  ["empty", "", undefined],
  ["a numeric string", "1234", "1234"],
  ["a positive integer", 1234, "1234"],
];
const refusedScopes: [string, unknown][] = [
  ["a thread id (Bug 84026)", "11111111-1111-1111-1111-111111111111"],
  ["zero", 0],
  ["a negative number", -3],
  ["a fraction", 1.5],
  ["a signed string", "-3"],
  ["a padded string", " 12"],
  ["a boolean", true],
  ["an object", {}],
  ["an array", ["12"]],
];

describe.each([
  ["getDeepMode", "aiPreferencesGetDeepMode", true],
  ["isDeepModeSet", "aiPreferencesIsDeepModeSet", false],
  ["getToolPermissionMode", "aiPreferencesGetToolPermissionMode", "ask"],
] as const)("preferencesController.%s", (method, operationId, value) => {
  it.each(acceptedScopes)(
    "answers the engine's value for an entityId that is %s",
    async (_what, entityId, scope) => {
      engine[method].mockResolvedValue(value as never);

      const reply = await call(preferencesController[method], {
        method: "GET",
        query: entityId === undefined ? {} : { entityId },
      });

      expect(reply.status).toBe(200);
      expect(reply.body).toBe(value);
      expect(engine[method]).toHaveBeenCalledWith(scope);
      expect(published(operationId, reply)).toEqual([]);
    },
  );

  it.each(refusedScopes)(
    "answers 400 for an entityId that is %s (Bug 84417)",
    async (_what, entityId) => {
      const reply = await call(preferencesController[method], {
        method: "GET",
        query: { entityId },
      });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual(badScope);
      expect(engine[method]).not.toHaveBeenCalled();
      expect(published(operationId, reply)).toEqual([]);
    },
  );

  it("relays a 403 of the AI service", async () => {
    engine[method].mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await call(preferencesController[method], { method: "GET" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(published(operationId, reply)).toEqual([]);
  });
});

// DEFECT data-16: the string "0" is taken as a room id while the number 0 is refused, although the message says a room id is a positive integer — expected 400 for "0" (preferencesController.ts scopeOf; its own error message)
it("takes the string 0 as a scope", async () => {
  engine.getDeepMode.mockResolvedValue(false);

  const reply = await call(preferencesController.getDeepMode, {
    method: "GET",
    query: { entityId: "0" },
  });

  expect(reply.status).toBe(200);
  expect(engine.getDeepMode).toHaveBeenCalledWith("0");
});

describe("preferencesController.setDeepMode", () => {
  it.each([
    ["a named body", { value: false, entityId: "12" }, false, "12"],
    ["a positional body", [true, 12], true, "12"],
    ["a positional body without a scope", [true], true, undefined],
  ])("stores the value of %s", async (_what, body, value, scope) => {
    const reply = await call(preferencesController.setDeepMode, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.setDeepMode).toHaveBeenCalledWith(value, scope);
    expect(published("aiPreferencesSetDeepMode", reply)).toEqual([]);
  });

  it.each([
    ["null", null],
    ["a string", "true"],
    ["{}", {}],
    ["a value that is the string false (Bug 82813)", { value: "false" }],
    ["no value (Bug 82814)", { entityId: "12" }],
    ["a value that is null", { value: null }],
    ["a value that is 1", [1]],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await call(preferencesController.setDeepMode, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "value is required and must be a boolean" });
    expect(engine.setDeepMode).not.toHaveBeenCalled();
    expect(published("aiPreferencesSetDeepMode", reply)).toEqual([]);
  });

  it.each(refusedScopes)("answers 400 for an entityId that is %s", async (_what, entityId) => {
    const reply = await call(preferencesController.setDeepMode, {
      body: { value: true, entityId },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(badScope);
    expect(engine.setDeepMode).not.toHaveBeenCalled();
    expect(published("aiPreferencesSetDeepMode", reply)).toEqual([]);
  });
});

describe("preferencesController.clearDeepMode", () => {
  it.each([
    ["no body", undefined, undefined],
    ["null", null, undefined],
    ["the bare id", "12", "12"],
    ["a named body", { entityId: 12 }, "12"],
  ])("clears the value for %s", async (_what, body, scope) => {
    const reply = await call(preferencesController.clearDeepMode, { method: "DELETE", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.clearDeepMode).toHaveBeenCalledWith(scope);
    expect(published("aiPreferencesClearDeepMode", reply)).toEqual([]);
  });

  it.each([
    ["a thread id", "11111111-1111-1111-1111-111111111111"],
    ["an object without entityId", { scope: "12" }],
    ["an array", ["12"]],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await call(preferencesController.clearDeepMode, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(badScope);
    expect(engine.clearDeepMode).not.toHaveBeenCalled();
    expect(published("aiPreferencesClearDeepMode", reply)).toEqual([]);
  });
});

describe("preferencesController.getReasoningLevel", () => {
  it("answers the stored level in one read", async () => {
    readReasoningLevel.mockResolvedValue("high");

    const reply = await call(preferencesController.getReasoningLevel, {
      method: "GET",
      query: { entityId: "12" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toBe("high");
    expect(readReasoningLevel).toHaveBeenCalledWith("12");
    expect(engine.getReasoningLevel).not.toHaveBeenCalled();
    expect(published("aiPreferencesGetReasoningLevel", reply)).toEqual([]);
  });

  it("answers off when nothing is stored", async () => {
    readReasoningLevel.mockResolvedValue(undefined);

    const reply = await call(preferencesController.getReasoningLevel, { method: "GET" });

    expect(reply.body).toBe("off");
    expect(published("aiPreferencesGetReasoningLevel", reply)).toEqual([]);
  });

  it("answers off when the storage cannot read a level", async () => {
    delete preferences.readReasoningLevel;

    const reply = await call(preferencesController.getReasoningLevel, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toBe("off");
  });

  it("answers 400 for an entityId that is not a room id", async () => {
    const reply = await call(preferencesController.getReasoningLevel, {
      method: "GET",
      query: { entityId: "abc" },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(badScope);
    expect(readReasoningLevel).not.toHaveBeenCalled();
    expect(published("aiPreferencesGetReasoningLevel", reply)).toEqual([]);
  });

  it("answers 500, not off, when the read fails", async () => {
    readReasoningLevel.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "", "u"),
    );

    const reply = await call(preferencesController.getReasoningLevel, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
  });
});

describe("preferencesController.setReasoningLevel", () => {
  it.each(["off", "low", "medium", "high", "max"])("stores the level %s", async (value) => {
    const reply = await call(preferencesController.setReasoningLevel, {
      method: "PUT",
      body: [value, "12"],
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(upsertReasoningLevel).toHaveBeenCalledWith(value, "12");
    expect(engine.setReasoningLevel).not.toHaveBeenCalled();
    expect(published("aiPreferencesSetReasoningLevel", reply)).toEqual([]);
  });

  it("answers success without a write when the storage cannot store a level", async () => {
    delete preferences.upsertReasoningLevel;

    const reply = await call(preferencesController.setReasoningLevel, { body: { value: "low" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
  });

  it.each([
    ["null", null],
    ["{}", {}],
    ["an unknown level", { value: "extreme" }],
    ["an upper-case level", { value: "HIGH" }],
    ["a level that is a number", [2]],
    ["a level that is null", { value: null }],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await call(preferencesController.setReasoningLevel, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: "value is required and must be one of: off, low, medium, high, max",
    });
    expect(upsertReasoningLevel).not.toHaveBeenCalled();
    expect(published("aiPreferencesSetReasoningLevel", reply)).toEqual([]);
  });

  it("answers 400 for an entityId that is not a room id", async () => {
    const reply = await call(preferencesController.setReasoningLevel, { body: ["low", "t-1"] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(badScope);
    expect(upsertReasoningLevel).not.toHaveBeenCalled();
  });
});

describe("preferencesController.setToolPermissionMode", () => {
  it.each(["ask", "auto", "allow"])("stores the mode %s", async (value) => {
    const reply = await call(preferencesController.setToolPermissionMode, {
      method: "PUT",
      body: { value, entityId: 7 },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.setToolPermissionMode).toHaveBeenCalledWith(value, "7");
    expect(published("aiPreferencesSetToolPermissionMode", reply)).toEqual([]);
  });

  it.each([
    ["null", null],
    ["{}", {}],
    ["an unknown mode", { value: "never" }],
    ["the C# spelling", { value: "Auto" }],
    ["the C# number", { value: 1 }],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await call(preferencesController.setToolPermissionMode, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "value is required and must be one of: ask, auto, allow" });
    expect(engine.setToolPermissionMode).not.toHaveBeenCalled();
    expect(published("aiPreferencesSetToolPermissionMode", reply)).toEqual([]);
  });

  it("answers 400 for an entityId that is not a room id", async () => {
    const reply = await call(preferencesController.setToolPermissionMode, { body: ["ask", -1] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(badScope);
    expect(engine.setToolPermissionMode).not.toHaveBeenCalled();
  });
});
