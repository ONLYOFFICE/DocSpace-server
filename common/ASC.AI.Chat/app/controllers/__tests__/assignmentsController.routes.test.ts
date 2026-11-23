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

import type { AssignmentsEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { invoke } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";

// Characterization of every route of assignmentsController: the engine class
// and the profile storage are stubbed, the entity gate (`assertEntityAccessible`)
// runs for real against a stubbed Files API.

const engine = engineMock<AssignmentsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  AssignmentsEngine: engineClass(() => engine),
}));
const profiles = vi.hoisted(() => ({ readByIdRaw: vi.fn(), readAllForClient: vi.fn() }));
vi.mock("../../storage/index.js", () => ({ storage: { profiles } }));

const { assignmentsController } = await import("../assignmentsController.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");

const upstream = useUpstream();

// A profile as `HttpProfilesStorage.dtoToProfile` maps it from the AI service's
// `ProfileDto` (products/ASC.AI/Server/Models/ResponseDto/ProfileDto.cs), read
// with forClient=true; `key` and `headers` carry markers to prove they are dropped.
const storedProfile = {
  id: "11111111-1111-1111-1111-111111111111",
  name: "OpenAI GPT-4o",
  providerType: "openai",
  baseUrl: "https://api.openai.com/v1",
  modelId: "gpt-4o",
  createdAt: 1767225600000,
  key: "sk-MARKER-key",
  headers: { "X-Secret": "MARKER-header" },
  reasoning: false,
  reasoningSupport: { thinks: false, canDisable: false, depths: [] },
  capabilities: 7,
  canUseTool: true,
  useResponsesApi: false,
  useProxy: false,
  isCloudProvider: false,
};
const { key: _key, headers: _headers, ...shownProfile } = storedProfile;

// What the engine resolves with: the in-process read with the internal gateway.
const engineResolved = {
  profileId: storedProfile.id,
  profile: { ...storedProfile, baseUrl: "http://internal-gateway:5157/api/2.0/ai/gateway" },
};

// GET api/2.0/files/folder/{id} (ASC.Files FoldersController.GetFolderInfoAsync).
function folderAnswers(response: Response | (() => Response)): void {
  upstream.use(
    http.get("*/api/2.0/files/folder/:id", () =>
      typeof response === "function" ? response() : response,
    ),
  );
}
const folderOk = () => docspace({ id: 42, title: "Room", type: 6, roomType: 2 });

beforeEach(() => {
  mockReset(engine);
  profiles.readByIdRaw.mockReset();
  profiles.readAllForClient.mockReset();
});

const resolveRoutes = [
  ["resolveForAction", "aiAssignmentsResolveForAction", []],
  // The published answer is `AiResolvedAssignment | null`, so ajv also reports the null branch.
  [
    "tryResolveForAction",
    "aiAssignmentsTryResolveForAction",
    ["/ must be null", "/ must match a schema in anyOf"],
  ],
] as const;

describe.each(resolveRoutes)("assignmentsController.%s", (route, operationId, nullBranch) => {
  const handler = assignmentsController[route];

  it("answers the stored profile without key and headers, and asks the engine with the action and entity", async () => {
    folderAnswers(folderOk);
    engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
    profiles.readByIdRaw.mockResolvedValue(storedProfile);

    const reply = await invoke(handler, {
      method: "GET",
      query: { actionType: "Chat", entityId: "42" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ profileId: storedProfile.id, profile: shownProfile });
    expect(JSON.stringify(reply.body)).not.toContain("MARKER");
    expect(JSON.stringify(reply.body)).not.toContain("internal-gateway");
    expect(engine.tryResolveForAction).toHaveBeenCalledWith("Chat", "42");
    expect(engine.resolveForAction).not.toHaveBeenCalled();
    expect(profiles.readByIdRaw).toHaveBeenCalledWith(storedProfile.id);
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /api/2.0/files/folder/42",
    ]);
    expect(responseViolations(operationId, 200, reply.body)).toEqual([]);
  });

  it("does not call the Files API without an entityId and passes undefined to the engine", async () => {
    engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
    profiles.readByIdRaw.mockResolvedValue(storedProfile);

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Default" } });

    expect(reply.status).toBe(200);
    expect(engine.tryResolveForAction).toHaveBeenCalledWith("Default", undefined);
    expect(upstream.seen).toEqual([]);
  });

  it("passes an empty entityId to the engine without asking the Files API", async () => {
    engine.tryResolveForAction.mockResolvedValue(null);

    await invoke(handler, { method: "GET", query: { actionType: "Chat", entityId: "" } });

    expect(engine.tryResolveForAction).toHaveBeenCalledWith("Chat", "");
    expect(upstream.seen).toEqual([]);
  });

  it.each([
    ["an array", ["42"]],
    ["a number", 42],
    ["an object", { id: "42" }],
  ])("treats an entityId that is %s as absent", async (_what, entityId) => {
    engine.tryResolveForAction.mockResolvedValue(null);

    await invoke(handler, { method: "GET", query: { actionType: "Chat", entityId } });

    expect(engine.tryResolveForAction).toHaveBeenCalledWith("Chat", undefined);
    expect(upstream.seen).toEqual([]);
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["empty", ""],
    ["of the wrong case", "chat"],
    ["an unknown action", "Painting"],
    ["an array", ["Chat"]],
    ["a number", 1],
  ])(
    "answers 400 when actionType is %s, before the entity check and the engine",
    async (_what, actionType) => {
      const reply = await invoke(handler, { method: "GET", query: { actionType, entityId: "42" } });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "actionType required" });
      expect(engine.tryResolveForAction).not.toHaveBeenCalled();
      expect(upstream.seen).toEqual([]);
      expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    },
  );

  it("answers 403 for a room the caller cannot open and does not resolve", async () => {
    folderAnswers(docspaceError(403, "denied"));

    const reply = await invoke(handler, {
      method: "GET",
      query: { actionType: "Chat", entityId: "42" },
    });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: 'Entity "42" is not accessible' });
    expect(engine.tryResolveForAction).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
  });

  it("answers 404 for a room that does not exist", async () => {
    folderAnswers(docspaceError(404, "gone"));

    const reply = await invoke(handler, {
      method: "GET",
      query: { actionType: "Chat", entityId: "42" },
    });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: 'Entity "42" not found' });
    expect(engine.tryResolveForAction).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 404, reply.body)).toEqual([]);
  });

  it("answers the Files API's 500 as 500 with its reason phrase only", async () => {
    folderAnswers(() =>
      HttpResponse.text("internal detail", { status: 500, statusText: "Internal Server Error" }),
    );

    const reply = await invoke(handler, {
      method: "GET",
      query: { actionType: "Chat", entityId: "42" },
    });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engine.tryResolveForAction).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine fails with an unrelated plain Error", async () => {
    engine.tryResolveForAction.mockRejectedValue(new Error("boom at http://internal:5157"));

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine throws something that is not an Error", async () => {
    engine.tryResolveForAction.mockRejectedValue("Assigned profile not found: p1");

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });

  it("relays an AI service failure of the profile read with its status and reason phrase only", async () => {
    engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
    profiles.readByIdRaw.mockRejectedValue(
      new AiServiceHttpError(
        500,
        "Internal Server Error",
        "MARKER body",
        "http://ai:5157/profiles/x",
      ),
    );

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations(operationId, 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the profile read times out", async () => {
    engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
    profiles.readByIdRaw.mockRejectedValue(
      new DOMException("The operation timed out.", "TimeoutError"),
    );

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });

  // DEFECT ctl-20: the profile is spread from the storage object, so whatever the storage adds (here the
  // C# ProfileDto's byokProvider of a BYOK profile) leaves in the answer and breaks AiProfile's
  // additionalProperties:false — expected the answer built from the listed profile fields
  // (ts-ai-failure-paths.md §5; AiResolvedAssignment in the published schema)
  it("answers whatever extra field the stored profile carries, byokProvider included", async () => {
    engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
    profiles.readByIdRaw.mockResolvedValue({ ...storedProfile, byokProvider: "openai", extra: 1 });

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      profileId: storedProfile.id,
      profile: { ...shownProfile, byokProvider: "openai", extra: 1 },
    });
    expect(responseViolations(operationId, 200, reply.body)).toEqual([
      "/profile must NOT have additional properties",
      "/profile must NOT have additional properties",
      ...nullBranch,
    ]);
  });
});

describe("assignmentsController.resolveForAction absent answers", () => {
  const handler = assignmentsController.resolveForAction;

  it("answers 404 when nothing is configured for the action", async () => {
    engine.tryResolveForAction.mockResolvedValue(null);

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Vision" } });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "no AI profile is available for ActionType.Vision" });
    expect(profiles.readByIdRaw).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsResolveForAction", 404, reply.body)).toEqual([]);
  });

  // DEFECT ctl-21: an assigned profile that is gone is recognised by the text of the engine's Error message
  // ("Assigned profile not found") — expected a test of the error's class or status
  // (ts-ai-failure-paths.md §1 "Never recognize an error by the text of its message")
  it("answers 404 when the engine says the assigned profile is not found", async () => {
    engine.tryResolveForAction.mockRejectedValue(new Error("Assigned profile not found: p9"));

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "no AI profile is available for ActionType.Chat" });
    expect(profiles.readByIdRaw).not.toHaveBeenCalled();
  });

  it("answers 404 when the resolved profile cannot be read back", async () => {
    engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
    profiles.readByIdRaw.mockResolvedValue(undefined);

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "no AI profile is available for ActionType.Chat" });
  });
});

describe("assignmentsController.tryResolveForAction absent answers", () => {
  const handler = assignmentsController.tryResolveForAction;

  it.each([
    ["nothing is configured", () => engine.tryResolveForAction.mockResolvedValue(null)],
    [
      "the engine says the assigned profile is not found",
      () =>
        engine.tryResolveForAction.mockRejectedValue(new Error("Assigned profile not found: p9")),
    ],
    [
      "the resolved profile cannot be read back",
      () => {
        engine.tryResolveForAction.mockResolvedValue(engineResolved as never);
        profiles.readByIdRaw.mockResolvedValue(undefined);
      },
    ],
  ])("answers null with 200 when %s", async (_what, arrange) => {
    arrange();

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(responseViolations("aiAssignmentsTryResolveForAction", 200, reply.body)).toEqual([]);
  });
});

describe("assignmentsController.assign", () => {
  const handler = assignmentsController.assign;

  it.each([
    ["the positional array the widget sends", ["Chat", "p1"]],
    ["named fields", { actionType: "Chat", profileId: "p1" }],
    ["named fields with extra ones", { actionType: "Chat", profileId: "p1", entityId: "42" }],
  ])("assigns with %s and answers the engine's result", async (_what, body) => {
    engine.assign.mockResolvedValue({ success: true });

    const reply = await invoke(handler, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.assign).toHaveBeenCalledWith("Chat", "p1");
    expect(responseViolations("aiAssignmentsAssign", 200, reply.body)).toEqual([]);
  });

  it("passes a very long profileId to the engine unchanged", async () => {
    engine.assign.mockResolvedValue({ success: true });
    const long = "p".repeat(10_000);

    const reply = await invoke(handler, { method: "PUT", body: ["Chat", long] });

    expect(reply.status).toBe(200);
    expect(engine.assign).toHaveBeenCalledWith("Chat", long);
  });

  it.each([
    ["null", null],
    ["a string", "Chat"],
    ["an empty array", []],
    ["{}", {}],
    ["a number", 7],
    ["without actionType", { profileId: "p1" }],
    ["with actionType null", { actionType: null, profileId: "p1" }],
    ["with actionType a number", { actionType: 1, profileId: "p1" }],
    ["with actionType empty", { actionType: "", profileId: "p1" }],
    ["with an unknown actionType", { actionType: "Painting", profileId: "p1" }],
    ["without profileId", { actionType: "Chat" }],
    ["with profileId null", { actionType: "Chat", profileId: null }],
    ["with profileId a number", { actionType: "Chat", profileId: 5 }],
    ["with profileId an object", { actionType: "Chat", profileId: { id: "p1" } }],
    ["with profileId empty", { actionType: "Chat", profileId: "" }],
    ["a positional array missing the profileId", ["Chat"]],
  ])("answers 400 for a body that is %s and does not reach the engine", async (_what, body) => {
    const reply = await invoke(handler, { method: "PUT", body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "actionType and profileId required" });
    expect(engine.assign).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsAssign", 400, reply.body)).toEqual([]);
  });

  // DEFECT ctl-22: a refusal of the engine ({ success: false, error }) is sent with 200 — expected 400/404
  // by error.code through a helper like sendMutation (ts-ai-failure-paths.md §2)
  it("sends a refusal of the engine with 200", async () => {
    const refusal = {
      success: false as const,
      error: { field: "name" as const, message: "unknown profile" },
    };
    engine.assign.mockResolvedValue(refusal);

    const reply = await invoke(handler, { method: "PUT", body: ["Chat", "p1"] });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(refusal);
    expect(responseViolations("aiAssignmentsAssign", 200, reply.body)).toEqual([]);
  });

  it("relays an AI service 403 of the engine's write with its reason phrase only", async () => {
    engine.assign.mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { method: "PUT", body: ["Chat", "p1"] });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiAssignmentsAssign", 403, reply.body)).toEqual([]);
  });

  it("relays an AI service 503 although the route does not publish it", async () => {
    engine.assign.mockRejectedValue(
      new AiServiceHttpError(503, "Service Unavailable", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { method: "PUT", body: ["Chat", "p1"] });

    expect(reply.status).toBe(503);
    expect(reply.body).toEqual({ error: "Service Unavailable" });
    expect(responseViolations("aiAssignmentsAssign", 503, reply.body)).toEqual([]);
  });
});

describe("assignmentsController.unassign", () => {
  const handler = assignmentsController.unassign;

  it.each([
    ["the bare string the widget sends", "Chat", {}],
    ["a named field", { actionType: "Chat" }, {}],
    ["the query, with no body", undefined, { actionType: "Chat" }],
    ["the query, with a null body", null, { actionType: "Chat" }],
    ["the body ahead of the query", "Chat", { actionType: "Vision" }],
  ])("unassigns the action given as %s", async (_what, body, query) => {
    engine.unassign.mockResolvedValue(undefined);

    const reply = await invoke(handler, { method: "DELETE", query, body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.unassign).toHaveBeenCalledWith("Chat");
    expect(responseViolations("aiAssignmentsUnassign", 200, reply.body)).toEqual([]);
  });

  it.each([
    ["no body and no query", undefined, {}],
    ["an empty string", "", {}],
    ["an unknown action", "Painting", {}],
    ["a number", 1, {}],
    ["an array", ["Chat"], {}],
    ["a named null", { actionType: null }, {}],
    ["a named number", { actionType: 3 }, {}],
    // `{}` lacks the key, so unpackPositional wraps the whole object as the value and the query is not read.
    ["{} with the action in the query", {}, { actionType: "Chat" }],
    ["a named number with the action in the query", { actionType: 3 }, { actionType: "Chat" }],
    ["an unknown action in the query", undefined, { actionType: "Painting" }],
    ["an array in the query", undefined, { actionType: ["Chat"] }],
  ])("answers 400 for %s", async (_what, body, query) => {
    const reply = await invoke(handler, { method: "DELETE", query, body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "actionType required" });
    expect(engine.unassign).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsUnassign", 400, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine fails", async () => {
    engine.unassign.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { method: "DELETE", body: "Chat" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiAssignmentsUnassign", 500, reply.body)).toEqual([]);
  });
});

describe("assignmentsController.bulkAssign", () => {
  const handler = assignmentsController.bulkAssign;

  it("hands the whole map to the engine and answers its result", async () => {
    engine.bulkAssign.mockResolvedValue({ success: true });

    const reply = await invoke(handler, { method: "PUT", body: { Chat: "p1", Default: "p2" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.bulkAssign).toHaveBeenCalledWith({ Chat: "p1", Default: "p2" });
    expect(responseViolations("aiAssignmentsBulkAssign", 200, reply.body)).toEqual([]);
  });

  it("hands an empty map and an empty profileId to the engine as they are", async () => {
    engine.bulkAssign.mockResolvedValue({ success: true });

    await invoke(handler, { method: "PUT", body: {} });
    await invoke(handler, { method: "PUT", body: { Chat: "" } });

    expect(engine.bulkAssign.mock.calls).toEqual([[{}], [{ Chat: "" }]]);
  });

  it.each([
    ["null", null, "body must be an object mapping actionType to profileId"],
    ["a string", "Chat", "body must be an object mapping actionType to profileId"],
    ["an array", [["Chat", "p1"]], "body must be an object mapping actionType to profileId"],
    ["a number", 5, "body must be an object mapping actionType to profileId"],
    ["absent", undefined, "body must be an object mapping actionType to profileId"],
    ["with an unknown action", { Chat: "p1", Painting: "p2" }, 'invalid actionType "Painting"'],
    ["with an action of the wrong case", { chat: "p1" }, 'invalid actionType "chat"'],
    ["with a null profileId", { Chat: null }, 'profileId for actionType "Chat" must be a string'],
    ["with a numeric profileId", { Chat: 5 }, 'profileId for actionType "Chat" must be a string'],
    [
      "with an object profileId",
      { Chat: { id: "p1" } },
      'profileId for actionType "Chat" must be a string',
    ],
  ])("answers 400 for a body that is %s and writes nothing", async (_what, body, error) => {
    const reply = await invoke(handler, { method: "PUT", body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.bulkAssign).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsBulkAssign", 400, reply.body)).toEqual([]);
  });

  // DEFECT ctl-24: a refusal of the engine ({ success: false, errors }) is sent with 200 — expected a 4xx
  // (ts-ai-failure-paths.md §2)
  it("sends a refusal of the engine with 200", async () => {
    const refusal = {
      success: false as const,
      errors: [
        {
          actionType: "Chat" as never,
          error: { field: "name" as const, message: "unknown profile" },
        },
      ],
    };
    engine.bulkAssign.mockResolvedValue(refusal);

    const reply = await invoke(handler, { method: "PUT", body: { Chat: "p1" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(refusal);
    expect(responseViolations("aiAssignmentsBulkAssign", 200, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine fails", async () => {
    engine.bulkAssign.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { method: "PUT", body: { Chat: "p1" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("assignmentsController.getAssignment", () => {
  const handler = assignmentsController.getAssignment;

  it.each([
    ["the profile id", "p1"],
    ["null for an empty slot", null],
  ])("answers %s", async (_what, value) => {
    engine.getAssignment.mockResolvedValue(value);

    const reply = await invoke(handler, { method: "GET", query: { actionType: "OCR" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(value);
    expect(engine.getAssignment).toHaveBeenCalledWith("OCR");
    expect(responseViolations("aiAssignmentsGetAssignment", 200, reply.body)).toEqual([]);
  });

  it.each([
    ["missing", undefined],
    ["empty", ""],
    ["unknown", "Painting"],
    ["an array", ["Chat"]],
  ])("answers 400 when actionType is %s", async (_what, actionType) => {
    const reply = await invoke(handler, { method: "GET", query: { actionType } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "actionType required" });
    expect(engine.getAssignment).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsGetAssignment", 400, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine fails", async () => {
    engine.getAssignment.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { method: "GET", query: { actionType: "Chat" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiAssignmentsGetAssignment", 500, reply.body)).toEqual([]);
  });
});

describe("assignmentsController.getAllAssignments", () => {
  const handler = assignmentsController.getAllAssignments;
  const forClient = (id: string, capabilities?: number | null) => ({
    id,
    name: `Profile ${id}`,
    providerType: "openai",
    baseUrl: "https://api.openai.com/v1",
    modelId: "gpt-4o",
    createdAt: 1767225600000,
    ...(capabilities === undefined ? {} : { capabilities }),
  });

  it("answers the id map as the engine gives it, without reading the profiles", async () => {
    engine.getAllAssignments.mockResolvedValue({ Chat: "p1", Default: "p2" });

    const reply = await invoke(handler, { method: "GET", query: {} });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ Chat: "p1", Default: "p2" });
    expect(engine.getAllAssignments).toHaveBeenCalledWith(undefined);
    expect(profiles.readAllForClient).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsGetAllAssignments", 200, reply.body)).toEqual([]);
  });

  it.each([["full"], ["Profile"], [""]])("answers the id map for expand=%s", async (expand) => {
    engine.getAllAssignments.mockResolvedValue({ Chat: "p1" });

    const reply = await invoke(handler, { method: "GET", query: { expand } });

    expect(reply.body).toEqual({ Chat: "p1" });
    expect(profiles.readAllForClient).not.toHaveBeenCalled();
  });

  it("answers the assigned profiles and the counts with expand=profile", async () => {
    engine.getAllAssignments.mockResolvedValue({
      Chat: "p1",
      Default: "p2",
      Vision: "gone",
      OCR: "",
      Code: "p4",
    });
    profiles.readAllForClient.mockResolvedValue([
      forClient("p1", 1),
      forClient("p2"),
      forClient("p3", 2),
      forClient("p4", 0),
      forClient("p5", null),
      forClient("p6", 3),
    ]);
    folderAnswers(folderOk);

    const reply = await invoke(handler, {
      method: "GET",
      query: { expand: "profile", entityId: "42" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      assignments: { Chat: forClient("p1", 1), Default: forClient("p2"), Code: forClient("p4", 0) },
      profilesTotal: 6,
      // p1 (chat bit), p2 (none), p4 (0), p5 (null), p6 (chat bit among others); not p3.
      chatProfilesTotal: 5,
    });
    expect(engine.getAllAssignments).toHaveBeenCalledWith("42");
    expect(responseViolations("aiAssignmentsGetAllAssignments", 200, reply.body)).toEqual([]);
  });

  it("answers empty assignments and zero counts for a portal with no profiles", async () => {
    engine.getAllAssignments.mockResolvedValue({});
    profiles.readAllForClient.mockResolvedValue([]);

    const reply = await invoke(handler, { method: "GET", query: { expand: "profile" } });

    expect(reply.body).toEqual({ assignments: {}, profilesTotal: 0, chatProfilesTotal: 0 });
    expect(responseViolations("aiAssignmentsGetAllAssignments", 200, reply.body)).toEqual([]);
  });

  // DEFECT ctl-25: with expand=profile the stored profiles are echoed as the storage gives them, so a BYOK
  // profile's byokProvider (C# ProfileDto.ByokProvider) leaves in the answer and breaks AiProfile's
  // additionalProperties:false — expected the profiles built from the listed fields (ts-ai-failure-paths.md §5)
  it("echoes the extra fields of a stored profile with expand=profile", async () => {
    engine.getAllAssignments.mockResolvedValue({ Chat: "p1" });
    profiles.readAllForClient.mockResolvedValue([
      { ...forClient("p1", 1), byokProvider: "openai" },
    ]);

    const reply = await invoke(handler, { method: "GET", query: { expand: "profile" } });

    expect(reply.body).toEqual({
      assignments: { Chat: { ...forClient("p1", 1), byokProvider: "openai" } },
      profilesTotal: 1,
      chatProfilesTotal: 1,
    });
    expect(responseViolations("aiAssignmentsGetAllAssignments", 200, reply.body)).toEqual([
      // the AiAssignmentsMap branch of the oneOf
      "/assignments must be string",
      "/profilesTotal must be string",
      "/chatProfilesTotal must be string",
      // the AiAssignmentsExpanded branch
      "/assignments/Chat must NOT have additional properties",
      "/ must match exactly one schema in oneOf",
    ]);
  });

  it.each([
    [403, { error: 'Entity "42" is not accessible' }],
    [404, { error: 'Entity "42" not found' }],
  ])(
    "answers %i for an entity the Files API answers so and reads nothing",
    async (status, body) => {
      folderAnswers(docspaceError(status));

      const reply = await invoke(handler, {
        method: "GET",
        query: { entityId: "42", expand: "profile" },
      });

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual(body);
      expect(engine.getAllAssignments).not.toHaveBeenCalled();
      expect(profiles.readAllForClient).not.toHaveBeenCalled();
      expect(responseViolations("aiAssignmentsGetAllAssignments", status, reply.body)).toEqual([]);
    },
  );

  it("answers 500, not an empty answer, when the profile list read fails", async () => {
    engine.getAllAssignments.mockResolvedValue({ Chat: "p1" });
    profiles.readAllForClient.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://ai/profiles"),
    );

    const reply = await invoke(handler, { method: "GET", query: { expand: "profile" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations("aiAssignmentsGetAllAssignments", 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the profile list read times out", async () => {
    engine.getAllAssignments.mockResolvedValue({ Chat: "p1" });
    profiles.readAllForClient.mockRejectedValue(
      new DOMException("The operation timed out.", "TimeoutError"),
    );

    const reply = await invoke(handler, { method: "GET", query: { expand: "profile" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("assignmentsController.cascadeProfileDelete", () => {
  const handler = assignmentsController.cascadeProfileDelete;

  it.each([
    ["an empty string body falls back to the query", "", { profileId: "p1" }],
    ["a named empty string falls back to the query", { profileId: "" }, { profileId: "p1" }],
    ["a named number falls back to the query", { profileId: 5 }, { profileId: "p1" }],
    ["an array body falls back to the query", ["p1"], { profileId: "p1" }],
    ["the body wins over the query", "p1", { profileId: "p2" }],
  ])("takes the id when %s", async (_what, body, query) => {
    engine.cascadeProfileDelete.mockResolvedValue(undefined);

    const reply = await invoke(handler, { method: "DELETE", query, body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.cascadeProfileDelete).toHaveBeenCalledWith("p1");
    expect(responseViolations("aiAssignmentsCascadeProfileDelete", 200, reply.body)).toEqual([]);
  });

  it.each([
    ["null", null, {}],
    ["{}", {}, {}],
    ["a number", 5, {}],
    ["a named null", { profileId: null }, {}],
    ["an empty string in the query", undefined, { profileId: "" }],
    ["an array in the query", undefined, { profileId: ["p1"] }],
  ])("answers 400 for a body that is %s", async (_what, body, query) => {
    const reply = await invoke(handler, { method: "DELETE", query, body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "profileId required" });
    expect(engine.cascadeProfileDelete).not.toHaveBeenCalled();
    expect(responseViolations("aiAssignmentsCascadeProfileDelete", 400, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine fails", async () => {
    engine.cascadeProfileDelete.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { method: "DELETE", body: "p1" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiAssignmentsCascadeProfileDelete", 500, reply.body)).toEqual([]);
  });
});
