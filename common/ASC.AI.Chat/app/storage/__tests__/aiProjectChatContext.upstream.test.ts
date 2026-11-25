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

import { UpstreamShapeError } from "../upstreamShape.js";
import { describe, expect, it, vi } from "vitest";
import { afterUpstreamTimeout } from "../../__tests__/helpers/time.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
  stalled,
} from "../../__tests__/helpers/upstream.js";
import {
  applyAiProjectChatContext,
  portalChatProfileId,
  readAiProjectChatContext,
} from "../aiProjectChatContext.js";
import { DocspaceApiHttpError } from "../docspaceFilesApi.js";
import { AiServiceHttpError } from "../httpClient.js";

// Nothing mocked but the network. Files API routes of products/ASC.Files/Server:
// GET api/2.0/files/folder/{id} (FoldersController.GetFolderInfo), GET api/2.0/files/rooms/{id}
// (VirtualRoomsController.GetRoomInfo), GET rooms/{id}/sources, rooms/{id}/model, rooms/{id}/skills
// (Api/AiHubController.cs), GET api/2.0/files/@my. AI service routes:
// GET internal/ai/assignments/{actionType} (AssignmentsStorageController.cs),
// GET internal/ai/profiles/{id} (ProfileStorageController.cs).
const upstream = useUpstream();

const files = (response: unknown) => HttpResponse.json({ response, count: 1, status: 0 });
const PROFILE = "6f1c2a3b-0000-4000-8000-000000000001";

/** The project room as GET rooms/{id} answers it (the `FolderDto` fields the context reads). */
const projectRoomDto = (overrides: Record<string, unknown> = {}) => ({
  id: 42,
  title: "Sales",
  access: 11,
  inRoom: true,
  roomType: 12,
  ...overrides,
});

/** `AiProjectSourcesDto` (products/ASC.Files/Server/ApiModels/ResponseDto/AiProjectSourcesDto.cs). */
const sourcesDto = () => ({
  instructionsRooms: [{ id: 7, title: "Brand voice", logo: { color: "FF6680" } }],
  knowledgeRooms: [
    { id: 9, title: "Docs", logo: null },
    { id: 10, title: "Policies", logo: null },
  ],
});

/** `ProfileDto` (products/ASC.AI/Server/Models/ResponseDto/ProfileDto.cs). */
const profileDto = (capabilities: unknown = 257) => ({
  id: PROFILE,
  name: "GPT-4o",
  providerType: "OpenAi",
  baseUrl: "https://api.openai.com/v1",
  modelId: "gpt-4o",
  capabilities,
  createdAt: 1_700_000_000_000,
});

interface Arrange {
  room?: unknown;
  sources?: unknown;
  /** `AiProjectModelDto` (products/ASC.Files/Server/ApiModels/ResponseDto/AiProjectModelDto.cs). */
  model?: unknown;
  chat?: unknown;
  fallback?: unknown;
  profile?: () => Response | Promise<Response>;
}

function project(arrange: Arrange = {}) {
  upstream.use(
    http.get("*/api/2.0/files/folder/42", () => files({ id: 42, title: "Sales", roomType: 12 })),
    http.get("*/api/2.0/files/rooms/42", () =>
      files("room" in arrange ? arrange.room : projectRoomDto()),
    ),
    http.get("*/api/2.0/files/rooms/42/sources", () => files(arrange.sources ?? sourcesDto())),
    http.get("*/api/2.0/files/rooms/42/model", () =>
      files("model" in arrange ? arrange.model : { profileId: null }),
    ),
    http.get("*/internal/ai/assignments/Chat", () =>
      docspace("chat" in arrange ? (arrange.chat as never) : PROFILE),
    ),
    http.get("*/internal/ai/assignments/Default", () =>
      docspace("fallback" in arrange ? (arrange.fallback as never) : null),
    ),
    http.get("*/internal/ai/profiles/:id", arrange.profile ?? (() => docspace(profileDto()))),
    http.get("*/api/2.0/files/rooms/42/skills", () =>
      files({
        rooms: [
          {
            id: 7,
            title: "Brand voice",
            logo: null,
            skills: [{ id: "5001", name: "tone", description: "Tone." }],
          },
        ],
      }),
    ),
    http.get("*/api/2.0/files/@my", () => files({ current: { id: 1001 } })),
  );
}

describe("readAiProjectChatContext", () => {
  it.each([undefined, "", "abc"])("answers undefined without a request for %j", async (id) => {
    await expect(readAiProjectChatContext(id)).resolves.toBeUndefined();
    expect(upstream.seen).toEqual([]);
  });

  it("answers undefined for a folder that is not an AI project", async () => {
    upstream.use(
      http.get("*/api/2.0/files/folder/42", () => files({ id: 42, title: "Room", roomType: 2 })),
    );
    await expect(readAiProjectChatContext("42")).resolves.toBeUndefined();
    expect(upstream.seen).toHaveLength(1);
  });

  it("reads the project, its sources and the portal's model when it has none of its own", async () => {
    project();
    await expect(readAiProjectChatContext("42")).resolves.toEqual({
      projectId: 42,
      title: "Sales",
      myRole: 11,
      canChat: true,
      profileId: null,
      effectiveProfileId: PROFILE,
      profileAvailable: true,
      instructionsRoomIds: [7],
      knowledgeRoomIds: [9, 10],
      unavailableRoomIds: [],
    });
    expect(upstream.seen.map((r) => r.path).sort()).toEqual([
      "/api/2.0/files/folder/42",
      "/api/2.0/files/rooms/42",
      "/api/2.0/files/rooms/42/model",
      "/api/2.0/files/rooms/42/sources",
      "/internal/ai/assignments/Chat",
      `/internal/ai/profiles/${PROFILE}`,
    ]);
  });

  it.each([
    ["id missing", { id: undefined }, { projectId: 42 }],
    ["id as text", { id: "42" }, { projectId: 42 }],
    ["title missing", { title: undefined }, { title: "" }],
    ["title null", { title: null }, { title: "" }],
    ["access missing", { access: undefined }, { myRole: 0 }],
    ["access as text", { access: "11" }, { myRole: 0 }],
    ["inRoom missing", { inRoom: undefined }, { canChat: false }],
    ["inRoom null", { inRoom: null }, { canChat: false }],
    ["inRoom as text", { inRoom: "true" }, { canChat: false }],
    ["inRoom false", { inRoom: false }, { canChat: false }],
  ])("reads a project room with %s", async (_name, overrides, expected) => {
    project({ room: projectRoomDto(overrides) });
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject(expected);
  });

  it.each([null, "Sales", [projectRoomDto()]])(
    "throws on a project room answer %j",
    async (room) => {
      project({ room });
      await expect(readAiProjectChatContext("42")).rejects.toEqual(
        new UpstreamShapeError("GET files/rooms/{id}"),
      );
    },
  );

  it.each([
    ["no lists", {}, [], []],
    ["null lists", { instructionsRooms: null, knowledgeRooms: null }, [], []],
    ["lists that are not lists", { instructionsRooms: { id: 7 }, knowledgeRooms: "9" }, [], []],
    [
      "rooms without a numeric id",
      {
        instructionsRooms: [{ title: "x" }, { id: "7" }, null, { id: 8 }],
        knowledgeRooms: [{ id: 9.5 }],
      },
      [8],
      [9.5],
    ],
    ["an answer that is not an object", null, [], []],
  ])("reads sources with %s", async (_name, sources, instructions, knowledge) => {
    project();
    upstream.use(http.get("*/api/2.0/files/rooms/42/sources", () => files(sources)));
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject({
      instructionsRoomIds: instructions,
      knowledgeRoomIds: knowledge,
    });
  });

  it("runs on the project's own model ahead of the portal's", async () => {
    project({ model: { profileId: "own" } });
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject({
      profileId: "own",
      effectiveProfileId: "own",
      profileAvailable: true,
    });
    expect(upstream.seen.map((r) => r.path)).not.toContain("/internal/ai/assignments/Chat");
    expect(upstream.seen.map((r) => r.path)).toContain("/internal/ai/profiles/own");
  });

  it.each([
    ["an empty profileId", { profileId: "" }],
    ["a numeric profileId", { profileId: 5 }],
    ["no profileId", {}],
    ["an answer that is not an object", null],
  ])("reads a model with %s as none of its own", async (_name, model) => {
    project({ model });
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject({
      profileId: null,
      effectiveProfileId: PROFILE,
    });
  });

  it.each([
    ["capabilities missing", undefined, true],
    ["capabilities 0", 0, true],
    ["a Chat capability", 1, true],
    ["no Chat capability", 2, false],
    ["capabilities as text", "1", true],
  ])("calls a profile with %s available: %s", async (_name, capabilities, available) => {
    project({ profile: () => docspace(profileDto(capabilities)) });
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject({
      profileAvailable: available,
    });
  });

  it("calls a removed profile (404) unavailable", async () => {
    project({ profile: () => docspaceError(404) });
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject({
      effectiveProfileId: PROFILE,
      profileAvailable: false,
    });
  });

  // Only a 404 says the profile is removed: a failed read is not "unavailable".
  it("rejects when the profile read answers 500", async () => {
    project({ profile: () => docspaceError(500) });

    await expect(readAiProjectChatContext("42")).rejects.toMatchObject({ status: 500 });
  });

  it("rejects with the timeout when the profile read gets no answer", async () => {
    project({ profile: stalled });

    const outcome = await afterUpstreamTimeout(() => readAiProjectChatContext("42"));

    expect(String("error" in outcome && outcome.error)).toContain("upstream timeout after");
  });

  it("answers no model and no profile read when the portal has neither a Chat nor a Default model", async () => {
    project({ chat: null, fallback: null });
    await expect(readAiProjectChatContext("42")).resolves.toMatchObject({
      profileId: null,
      effectiveProfileId: null,
      profileAvailable: false,
    });
    expect(upstream.seen.map((r) => r.path)).not.toContain(`/internal/ai/profiles/${PROFILE}`);
  });

  it.each([
    ["the project room", "*/api/2.0/files/rooms/42", DocspaceApiHttpError],
    ["the sources", "*/api/2.0/files/rooms/42/sources", DocspaceApiHttpError],
    ["the model", "*/api/2.0/files/rooms/42/model", DocspaceApiHttpError],
  ])("propagates a 403 of %s", async (_name, route, type) => {
    project();
    upstream.use(http.get(route, () => new HttpResponse(null, { status: 403 })));
    const error = await readAiProjectChatContext("42").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(type);
    expect(error).toMatchObject({ status: 403 });
    // Promise.all rejects at the first failure; the reads it started go on. Let them land
    // inside this test rather than in the next one.
    if (!route.endsWith("/model")) {
      await vi.waitFor(() => {
        expect(upstream.seen.map((r) => r.path)).toContain(`/internal/ai/profiles/${PROFILE}`);
      });
    }
  });

  it.each([
    ["the Chat assignment", "*/internal/ai/assignments/Chat"],
    ["the profile", "*/internal/ai/profiles/:id"],
  ])("propagates a 500 of %s instead of calling the model unavailable", async (_name, route) => {
    project();
    upstream.use(http.get(route, () => docspaceError(500)));
    const error = await readAiProjectChatContext("42").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ status: 500 });
  });
});

describe("portalChatProfileId", () => {
  it("answers the Chat model, else the Default one, else null", async () => {
    project({ chat: "c" });
    await expect(portalChatProfileId()).resolves.toBe("c");
    project({ chat: null, fallback: "d" });
    await expect(portalChatProfileId()).resolves.toBe("d");
    project({ chat: null, fallback: null });
    await expect(portalChatProfileId()).resolves.toBeNull();
  });
});

describe("applyAiProjectChatContext", () => {
  it.each([null, "body", 1, []])("leaves a body %j alone without a request", async (body) => {
    await expect(applyAiProjectChatContext(body)).resolves.toBeNull();
    expect(upstream.seen).toEqual([]);
  });

  it.each([42, null, { id: 42 }])(
    "reads an entityId %j that is not text as no project",
    async (entityId) => {
      const body = { entityId, actionArgs: {} };
      await expect(applyAiProjectChatContext(body)).resolves.toBeNull();
      expect(body).toEqual({ entityId, actionArgs: {} });
      expect(upstream.seen).toEqual([]);
    },
  );

  it("replaces what the composer sent with the project's model, skills rooms, knowledge rooms and My documents", async () => {
    project();
    const body: Record<string, unknown> = {
      entityId: "42",
      profileId: "composer",
      contextEntityId: "5",
      actionArgs: {
        contextRooms: [{ cloud: "docspace", id: "99" }],
        knowledgeRooms: [{ cloud: "docspace", id: "98" }],
        knowledgeRoom: { id: "97" },
        folderContext: { id: "" },
        other: "kept",
      },
    };

    await expect(applyAiProjectChatContext(body)).resolves.toBeNull();

    expect(body).toEqual({
      entityId: "42",
      profileId: PROFILE,
      contextEntityId: "1001",
      actionArgs: {
        contextRooms: [
          {
            cloud: "docspace",
            id: "7",
            skills: [{ id: "5001", name: "tone", description: "Tone." }],
          },
        ],
        knowledgeRooms: [
          { cloud: "docspace", id: "9" },
          { cloud: "docspace", id: "10" },
        ],
        other: "kept",
      },
    });
    expect(upstream.seen.map((r) => r.path)).toContain("/api/2.0/files/@my?count=1");
  });

  it("drops the rooms the composer sent when the project links none, and reads no catalog", async () => {
    project({ sources: { instructionsRooms: [], knowledgeRooms: [] } });
    const body: Record<string, unknown> = {
      entityId: "42",
      actionArgs: {
        contextRooms: [{ id: "99" }],
        knowledgeRooms: [{ id: "98" }],
        folderContexts: [{ id: 300 }],
      },
    };

    await applyAiProjectChatContext(body);

    expect(body).toEqual({
      entityId: "42",
      profileId: PROFILE,
      contextEntityId: "300",
      actionArgs: { folderContexts: [{ id: 300 }] },
    });
    expect(upstream.seen.map((r) => r.path)).not.toContain("/api/2.0/files/rooms/42/skills");
  });

  it("starts from empty arguments when the composer sent none", async () => {
    project({ sources: { instructionsRooms: [], knowledgeRooms: [{ id: 9 }] } });
    const body: Record<string, unknown> = { entityId: "42", actionArgs: "x" };

    await applyAiProjectChatContext(body);

    expect(body["actionArgs"]).toEqual({ knowledgeRooms: [{ cloud: "docspace", id: "9" }] });
  });

  it("refuses with 403 a caller who is not in the project", async () => {
    project({ room: projectRoomDto({ inRoom: false, access: 0 }) });
    const body = { entityId: "42", profileId: "composer" };
    await expect(applyAiProjectChatContext(body)).resolves.toEqual({
      status: 403,
      body: { error: "You cannot chat in this AI project" },
    });
    expect(body).toEqual({ entityId: "42", profileId: "composer" });
  });

  it("refuses with 409 a project whose model cannot chat", async () => {
    project({ profile: () => docspace(profileDto(2)) });
    await expect(applyAiProjectChatContext({ entityId: "42" })).resolves.toEqual({
      status: 409,
      body: {
        error: "The model of this AI project is not available",
        code: "projectModelUnavailable",
      },
    });
  });

  it("runs the round without the skills when the catalog answers 500", async () => {
    project();
    upstream.use(
      http.get("*/api/2.0/files/rooms/42/skills", () => new HttpResponse(null, { status: 500 })),
    );
    const body: Record<string, unknown> = { entityId: "42" };
    await applyAiProjectChatContext(body);
    expect(body["actionArgs"]).toMatchObject({
      contextRooms: [{ cloud: "docspace", id: "7", skills: [] }],
    });
  });
});
