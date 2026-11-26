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

// The template routes through the real catalog of the service and the real Files, People
// and AI service clients, answered by msw. The storage the routes read (the AI access gate,
// the portal's chat model assignment) is the boundary and is stubbed.

import { beforeEach, describe, expect, it, vi } from "vitest";
import { invoke, inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { fileDto } from "../../__tests__/fixtures/filesApi.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

const mocks = vi.hoisted(() => ({ readDisabled: vi.fn(), readByType: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: {
    toolPrefs: { readDisabled: mocks.readDisabled },
    assignments: { readByType: mocks.readByType },
  },
}));

const { AiServiceHttpError } = await import("../../storage/httpClient.js");
const { readTemplateCatalog } = await import("../../templates/catalog.js");
const { templatesController } = await import("../templatesController.js");

const upstream = useUpstream();
const catalog = await readTemplateCatalog();

// `EmployeeFullDto` (common/ASC.Api.Core/Model/EmployeeFullDto.cs), the answer of
// `GET api/2.0/people/@self`.
const SELF_DTO = {
  id: "66faa6e4-f133-11ea-b126-00ffeec8b4ef",
  displayName: "Room Admin",
  avatar: "/static/images/default_user_photo_size_82-82.png",
  hasAvatar: false,
  isAnonim: false,
  email: "admin@example.com",
  status: 1,
  activationStatus: 1,
  isAdmin: false,
  isRoomAdmin: true,
  isOwner: false,
  isVisitor: false,
  isCollaborator: false,
  isLDAP: false,
  isSSO: false,
};

// `AiSettingsDto` (products/ASC.AI/Server/Models/ResponseDto/AiSettingsDto.cs), the answer of
// `GET internal/ai/config` (SettingsController.GetConfig).
const AI_CONFIG_DTO = {
  vectorizationEnabled: true,
  vectorizationNeedReset: false,
  aiReady: true,
  embeddingModel: "text-embedding-3-small",
  systemAiEnabled: true,
  recommendedModelForForms: null,
  toolPermissionMode: 0,
};

// `FolderDto` of a new room (products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs), the
// answer of `POST api/2.0/files/rooms` (VirtualRoomsController.CreateRoomAsync).
const roomDto = (id: number, title: string, roomType: number) => ({
  id,
  title,
  roomType,
  parentId: 0,
  filesCount: 0,
  foldersCount: 0,
  new: 0,
  mute: false,
  tags: [],
  pinned: false,
  private: false,
  indexing: false,
  denyDownload: false,
  security: { Read: true, Create: true },
  created: "2026-11-01T10:00:00.0000000+00:00",
});

interface Call {
  step: string;
  body?: unknown;
}

const calls: Call[] = [];
let failAt = 0;
let failure: () => Response = () => docspaceError(500);
let nextRoomId = 100;

/** Records the request as the next step, and fails it when it is step `failAt` (1-based). */
function step(label: string, body: unknown, answer: () => Response): Response {
  calls.push(body === undefined ? { step: label } : { step: label, body });
  return calls.length === failAt ? failure() : answer();
}

function answerAll(): void {
  upstream.use(
    http.get("*/api/2.0/people/@self", () =>
      step("GET people/@self", undefined, () => docspace(SELF_DTO)),
    ),
    http.get("*/internal/ai/config", () =>
      step("GET ai/config", undefined, () => docspace(AI_CONFIG_DTO)),
    ),
    http.post("*/api/2.0/files/rooms", async ({ request }) => {
      const body = (await request.json()) as { title: string; roomType: number };
      return step("POST rooms", body, () =>
        docspace(roomDto(++nextRoomId, body.title, body.roomType)),
      );
    }),
    http.post("*/api/2.0/files/rooms/:id/sources", async ({ request, params }) =>
      step(`POST rooms/${String(params["id"])}/sources`, await request.json(), () => docspace({})),
    ),
    http.put("*/api/2.0/files/rooms/:id/model", async ({ request, params }) =>
      step(`PUT rooms/${String(params["id"])}/model`, await request.json(), () => docspace({})),
    ),
    http.post("*/api/2.0/files/:folderId/insert", async ({ request, params }) => {
      const form = await request.formData();
      const file = form.get("file");
      const title = String(form.get("title"));
      return step(
        `upload ${String(params["folderId"])}/${title}`,
        file instanceof File ? file.type : undefined,
        () => HttpResponse.json(fileDto({ id: 900 + calls.length, title })),
      );
    }),
  );
}

beforeEach(() => {
  calls.length = 0;
  failAt = 0;
  failure = () => docspaceError(500);
  nextRoomId = 100;
  mocks.readDisabled.mockReset().mockResolvedValue({});
  mocks.readByType
    .mockReset()
    .mockImplementation(async (type: string) => (type === "Chat" ? "p-chat" : null));
  answerAll();
});

const forbidden = () => new AiServiceHttpError(403, "Forbidden", "AI is off", "http://ai/x");
const AGENT = "agents/document/contract-reviewer";

describe("templatesController.list", () => {
  it("answers the shipped catalog", async () => {
    const reply = await invoke(templatesController.list, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(JSON.parse(JSON.stringify(catalog)));
    expect(responseViolations("aiTemplatesList", 200, reply.body)).toEqual([]);
    expect(mocks.readDisabled).toHaveBeenCalledWith(undefined);
    expect(calls).toEqual([]);
  });

  it("answers 403 from the AI access gate", async () => {
    mocks.readDisabled.mockRejectedValue(forbidden());

    const reply = await invoke(templatesController.list, { method: "GET" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiTemplatesList", 403, reply.body)).toEqual([]);
  });

  it("answers 500 when the access gate cannot be read", async () => {
    mocks.readDisabled.mockRejectedValue(new TypeError("fetch failed"));

    const reply = await invoke(templatesController.list, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiTemplatesList", 500, reply.body)).toEqual([]);
  });
});

describe("templatesController.install", () => {
  it("installs an agent with its skill set and knowledge base, the caller's cookie relayed", async () => {
    const seenCookies: (string | null)[] = [];
    upstream.use(
      http.get("*/api/2.0/people/@self", ({ request }) => {
        seenCookies.push(request.headers.get("cookie"));
        return step("GET people/@self", undefined, () => docspace(SELF_DTO));
      }),
    );

    const reply = await inRequestContext({ cookie: "asc_auth_key=abc" }, () =>
      invoke(templatesController.install, { body: { id: `  ${AGENT} ` } }),
    );

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      templateId: AGENT,
      id: "103",
      title: "Contract Reviewer",
      kind: "agent",
      skillSets: [
        { templateId: "skill-sets/contract-review", id: "101", title: "Contract review" },
      ],
      knowledgeBases: [
        {
          templateId: "knowledge-bases/contract-standards",
          id: "102",
          title: "Contract standards",
        },
      ],
      warnings: ["instructionsNotSaved"],
    });
    expect(responseViolations("aiTemplatesInstall", 200, reply.body)).toEqual([]);
    expect(seenCookies).toEqual(["asc_auth_key=abc"]);
    const agent = catalog.agents.find((a) => a.id === AGENT)!;
    const skillSet = catalog.skillSets.find((s) => s.id === "skill-sets/contract-review")!;
    const knowledge = catalog.knowledgeBases.find(
      (k) => k.id === "knowledge-bases/contract-standards",
    )!;
    expect(calls).toEqual([
      { step: "GET people/@self" },
      { step: "GET ai/config" },
      {
        step: "POST rooms",
        body: { title: skillSet.name, roomType: 10, color: skillSet.color, cover: skillSet.cover },
      },
      { step: "upload 101/clause-risk-check.md", body: "text/markdown" },
      { step: "upload 101/version-compare.md", body: "text/markdown" },
      {
        step: "POST rooms",
        body: {
          title: knowledge.name,
          roomType: 11,
          color: knowledge.color,
          cover: knowledge.cover,
        },
      },
      {
        step: "upload 102/Standard clauses.docx",
        body: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      },
      { step: "upload 102/Review checklist.md", body: "text/markdown" },
      {
        step: "POST rooms",
        body: {
          title: agent.name,
          roomType: 12,
          color: agent.color,
          cover: agent.cover,
          tags: ["Document"],
        },
      },
      { step: "POST rooms/103/sources", body: { roomIds: [101, 102] } },
      { step: "PUT rooms/103/model", body: { profileId: "p-chat" } },
    ]);
  });

  it("installs a skill set without asking for vectorization", async () => {
    const reply = await invoke(templatesController.install, {
      body: { id: "skill-sets/budgeting" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      templateId: "skill-sets/budgeting",
      id: "101",
      title: "Budgeting",
      kind: "skillSet",
      skillSets: [],
      knowledgeBases: [],
      warnings: [],
    });
    expect(responseViolations("aiTemplatesInstall", 200, reply.body)).toEqual([]);
    expect(calls.map((c) => c.step)).toEqual([
      "GET people/@self",
      "POST rooms",
      "upload 101/budget-builder.md",
      "upload 101/variance-analysis.md",
    ]);
  });

  it("answers 403 from the AI access gate for any body, before any upstream call", async () => {
    mocks.readDisabled.mockRejectedValue(forbidden());

    const reply = await invoke(templatesController.install, { body: null });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiTemplatesInstall", 403, reply.body)).toEqual([]);
    expect(calls).toEqual([]);
  });

  const ROLE_REFUSAL = {
    error: "Only the portal owner, portal admins and room admins may install a template",
  };

  it("refuses a caller who may not create rooms for any body, before any room is made", async () => {
    upstream.use(
      http.get("*/api/2.0/people/@self", () =>
        step("GET people/@self", undefined, () => docspace({ ...SELF_DTO, isRoomAdmin: false })),
      ),
    );

    const reply = await invoke(templatesController.install, { body: "nonsense" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual(ROLE_REFUSAL);
    expect(responseViolations("aiTemplatesInstall", 403, reply.body)).toEqual([]);
    expect(calls).toEqual([{ step: "GET people/@self" }]);
  });

  // EmployeeFullDto fields one at a time: who the role check lets through.
  it.each([
    ["the owner", { isOwner: true, isRoomAdmin: false }, 200],
    ["a DocSpace admin", { isAdmin: true, isRoomAdmin: false }, 200],
    ["a room admin", {}, 200],
    ["a user", { isRoomAdmin: false }, 403],
    ["no isRoomAdmin", { isRoomAdmin: undefined }, 403],
    ["a null isRoomAdmin", { isRoomAdmin: null }, 403],
    ["a string isRoomAdmin", { isRoomAdmin: "true" }, 403],
    ["a string isOwner", { isOwner: "true", isRoomAdmin: false }, 403],
    ["a numeric isAdmin", { isAdmin: 1, isRoomAdmin: false }, 403],
  ])("answers a caller who is %s", async (_what, change, status) => {
    upstream.use(
      http.get("*/api/2.0/people/@self", () =>
        step("GET people/@self", undefined, () => docspace({ ...SELF_DTO, ...change })),
      ),
    );

    const reply = await invoke(templatesController.install, {
      body: { id: "skill-sets/budgeting" },
    });

    expect(reply.status).toBe(status);
    if (status === 403) {
      expect(reply.body).toEqual(ROLE_REFUSAL);
    }
  });

  it.each([
    ["a 401", () => docspaceError(401), 401, "Unauthorized"],
    ["a 500", () => docspaceError(500, "at http://people.internal"), 500, "Internal Server Error"],
    ["a body without the envelope", () => HttpResponse.json(SELF_DTO), 502, "Bad Gateway"],
    ["a null response", () => docspace(null), 502, "Bad Gateway"],
    ["a string body", () => HttpResponse.json("x"), 502, "Bad Gateway"],
    ["a body that is not JSON", () => HttpResponse.text("{"), 502, "Bad Gateway"],
  ])(
    "answers a caller read that answers %s before any room is made",
    async (_what, answer, status, error) => {
      upstream.use(
        http.get("*/api/2.0/people/@self", () => step("GET people/@self", undefined, answer)),
      );

      const reply = await invoke(templatesController.install, {
        body: { id: "skill-sets/budgeting" },
      });

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error });
      expect(calls).toEqual([{ step: "GET people/@self" }]);
    },
  );

  const BAD_ID = { error: "id must be the id of a template" };
  it.each([
    ["null", null],
    ["a string", AGENT],
    ["an array", [AGENT]],
    ["a number", 7],
    ["{}", {}],
    ["a null id", { id: null }],
    ["a numeric id", { id: 7 }],
    ["an array id", { id: [AGENT] }],
    ["an empty id", { id: "" }],
    ["a blank id", { id: "   " }],
    ["an id over 200 characters", { id: "a".repeat(201) }],
  ])(
    "answers 400 for a body that is %s, after the role check, making no room",
    async (_what, body) => {
      const reply = await invoke(templatesController.install, { body });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual(BAD_ID);
      expect(responseViolations("aiTemplatesInstall", 400, reply.body)).toEqual([]);
      expect(calls).toEqual([{ step: "GET people/@self" }]);
    },
  );

  it.each([
    ["an unknown id", "agents/none"],
    ["an id of 200 characters", "a".repeat(200)],
    ["a category folder", "agents/document"],
    ["an agent id with its extension", `${AGENT}.json`],
  ])("answers 404 for %s, making no room", async (_what, id) => {
    const reply = await invoke(templatesController.install, { body: { id } });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Template not found" });
    expect(responseViolations("aiTemplatesInstall", 404, reply.body)).toEqual([]);
    expect(calls).toEqual([{ step: "GET people/@self" }]);
  });

  it.each([
    ["off", { vectorizationEnabled: false, vectorizationNeedReset: false }],
    ["absent from the answer", { aiReady: true }],
    ["a string", { vectorizationEnabled: "true", vectorizationNeedReset: "true" }],
  ])(
    "answers 409 for an agent with a knowledge base where vectorization is %s",
    async (_what, config) => {
      upstream.use(
        http.get("*/internal/ai/config", () =>
          step("GET ai/config", undefined, () => docspace(config)),
        ),
      );

      const reply = await invoke(templatesController.install, { body: { id: AGENT } });

      expect(reply.status).toBe(409);
      expect(reply.body).toEqual({
        error:
          "This template holds a knowledge base, which needs vectorization to be set up on the portal",
      });
      expect(responseViolations("aiTemplatesInstall", 409, reply.body)).toEqual([]);
      expect(calls.map((c) => c.step)).toEqual(["GET people/@self", "GET ai/config"]);
    },
  );

  it("installs a knowledge base while the vectorization settings wait for a reset", async () => {
    upstream.use(
      http.get("*/internal/ai/config", () =>
        step("GET ai/config", undefined, () =>
          docspace({ ...AI_CONFIG_DTO, vectorizationEnabled: false, vectorizationNeedReset: true }),
        ),
      ),
    );

    const reply = await invoke(templatesController.install, {
      body: { id: "knowledge-bases/contract-standards" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toMatchObject({ kind: "knowledgeBase", id: "101", warnings: [] });
  });

  it("gives the agent the Default profile when the Chat action has none", async () => {
    mocks.readByType.mockImplementation(async (type: string) =>
      type === "Default" ? "p-default" : null,
    );

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(200);
    expect(calls.at(-1)).toEqual({ step: "PUT rooms/103/model", body: { profileId: "p-default" } });
  });

  it.each([
    ["has no chat model", async () => null],
    [
      "cannot read its chat model",
      async () => {
        throw new AiServiceHttpError(500, "Internal Server Error", "", "http://ai/x");
      },
    ],
  ])("keeps the agent and warns when the portal %s, writing no model", async (_what, read) => {
    mocks.readByType.mockImplementation(read);

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(200);
    expect(reply.body).toMatchObject({
      id: "103",
      warnings: ["modelNotSaved", "instructionsNotSaved"],
    });
    expect(responseViolations("aiTemplatesInstall", 200, reply.body)).toEqual([]);
    expect(calls.map((c) => c.step)).not.toContain("PUT rooms/103/model");
  });

  // The agent's install, step by step: 1 people/@self, 2 ai/config, 3 skill set room,
  // 4-5 its skills, 6 knowledge room, 7-8 its files, 9 agent room, 10 sources, 11 model.
  const STEPS = [
    "GET people/@self",
    "GET ai/config",
    "POST rooms",
    "upload 101/clause-risk-check.md",
    "upload 101/version-compare.md",
    "POST rooms",
    "upload 102/Standard clauses.docx",
    "upload 102/Review checklist.md",
    "POST rooms",
    "POST rooms/103/sources",
    "PUT rooms/103/model",
  ];

  it.each([
    [1, 500, "Internal Server Error"],
    [2, 500, "Internal Server Error"],
  ])("makes nothing when step %i fails with %i", async (n, status, error) => {
    failAt = n;

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(calls.map((c) => c.step)).toEqual(STEPS.slice(0, n));
  });

  const SKILL_ROOM = {
    templateId: "skill-sets/contract-review",
    id: "101",
    title: "Contract review",
  };
  const KNOWLEDGE_ROOM = {
    templateId: "knowledge-bases/contract-standards",
    id: "102",
    title: "Contract standards",
  };
  const AGENT_ROOM = { templateId: AGENT, id: "103", title: "Contract Reviewer" };

  // A step after the first room keeps its status and names every room made before it
  // (ts-ai-failure-paths.md §6): they stay in the portal, as nothing undoes them.
  // The room a `POST rooms` was asked for when its answer does not say whether it was made.
  const unconfirmed = ({ templateId, title }: { templateId: string; title: string }) => ({
    templateId,
    title,
  });

  // A 500 of `POST rooms` (steps 3, 6 and 9) does not say that no room was made: the room is
  // named as `unconfirmed`, the first one included.
  it.each([
    [3, [], [], undefined, unconfirmed(SKILL_ROOM)],
    [4, [SKILL_ROOM], [], undefined, undefined],
    [5, [SKILL_ROOM], [], undefined, undefined],
    [6, [SKILL_ROOM], [], undefined, unconfirmed(KNOWLEDGE_ROOM)],
    [7, [SKILL_ROOM], [KNOWLEDGE_ROOM], undefined, undefined],
    [8, [SKILL_ROOM], [KNOWLEDGE_ROOM], undefined, undefined],
    [9, [SKILL_ROOM], [KNOWLEDGE_ROOM], undefined, unconfirmed(AGENT_ROOM)],
    [10, [SKILL_ROOM], [KNOWLEDGE_ROOM], AGENT_ROOM, undefined],
  ])(
    "names the rooms already made when step %i fails with 500",
    async (n, skillSets, knowledgeBases, agent, maybe) => {
      failAt = n;

      const reply = await invoke(templatesController.install, { body: { id: AGENT } });

      expect(reply.status).toBe(500);
      expect(reply.body).toEqual({
        error: "Internal Server Error",
        skillSets,
        knowledgeBases,
        ...(agent ? { agent } : {}),
        ...(maybe ? { unconfirmed: maybe } : {}),
        warning: "installIncomplete",
      });
      expect(responseViolations("aiTemplatesInstall", 500, reply.body)).toEqual([]);
      expect(calls.map((c) => c.step)).toEqual(STEPS.slice(0, n));
    },
  );

  // Each of these answers leaves a room that may exist in the portal with no id to find it by
  // (ts-ai-failure-paths.md §6): the answer is never the bare error.
  it.each([
    ["an empty 200", () => HttpResponse.text("", { status: 200 }), 502, "Bad Gateway"],
    ["a dropped connection", () => HttpResponse.error(), 500, "Internal server error"],
  ])(
    "names the room as unconfirmed when its creation gets %s, for the first room and a later one",
    async (_what, answer, status, error) => {
      failure = answer;

      failAt = 2;
      const first = await invoke(templatesController.install, {
        body: { id: "skill-sets/budgeting" },
      });
      failAt = 6;
      calls.length = 0;
      const later = await invoke(templatesController.install, { body: { id: AGENT } });

      expect(first.status).toBe(status);
      expect(first.body).toEqual({
        error,
        skillSets: [],
        knowledgeBases: [],
        unconfirmed: { templateId: "skill-sets/budgeting", title: "Budgeting" },
        warning: "installIncomplete",
      });
      expect(responseViolations("aiTemplatesInstall", status, first.body)).toEqual([]);
      expect(later.status).toBe(status);
      expect(later.body).toEqual({
        error,
        skillSets: [SKILL_ROOM],
        knowledgeBases: [],
        unconfirmed: unconfirmed(KNOWLEDGE_ROOM),
        warning: "installIncomplete",
      });
      expect(responseViolations("aiTemplatesInstall", status, later.body)).toEqual([]);
    },
  );

  it("names the rooms already made when a step fails without an answer", async () => {
    failAt = 7;
    failure = () => HttpResponse.error();

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({
      error: "Internal server error",
      skillSets: [SKILL_ROOM],
      knowledgeBases: [KNOWLEDGE_ROOM],
      warning: "installIncomplete",
    });
    expect(responseViolations("aiTemplatesInstall", 500, reply.body)).toEqual([]);
  });

  it.each([
    ["skill-sets/budgeting", 3, "skillSets", "Budgeting"],
    ["knowledge-bases/contract-standards", 4, "knowledgeBases", "Contract standards"],
  ])("names the room of %s when its file upload (step %i) fails", async (id, n, list, title) => {
    failAt = n;
    failure = () => docspaceError(503);

    const reply = await invoke(templatesController.install, { body: { id } });

    expect(reply.status).toBe(503);
    expect(reply.body).toEqual({
      error: "Service Unavailable",
      skillSets: [],
      knowledgeBases: [],
      [list]: [{ templateId: id, id: "101", title }],
      warning: "installIncomplete",
    });
    expect(responseViolations("aiTemplatesInstall", 503, reply.body)).toEqual([]);
  });

  it("answers a bare error when the room of a skill set is refused", async () => {
    failAt = 2;
    failure = () => docspaceError(403);

    const reply = await invoke(templatesController.install, {
      body: { id: "skill-sets/budgeting" },
    });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(calls.map((c) => c.step)).toEqual(["GET people/@self", "POST rooms"]);
  });

  it.each([
    [403, "Forbidden"],
    [413, "Payload Too Large"],
  ])("relays a refused upload's status %i with the room already made", async (status, error) => {
    failAt = 4;
    failure = () => docspaceError(status);

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({
      error,
      skillSets: [SKILL_ROOM],
      knowledgeBases: [],
      warning: "installIncomplete",
    });
    expect(responseViolations("aiTemplatesInstall", status, reply.body)).toEqual([]);
  });

  it.each([
    [403, "Forbidden"],
    [404, "Not Found"],
  ])("relays a refused room's status %i", async (status, error) => {
    failAt = 3;
    failure = () => docspaceError(status);

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiTemplatesInstall", status, reply.body)).toEqual([]);
  });

  it.each([
    [400, "Bad Request"],
    [500, "Internal Server Error"],
  ])("keeps the agent and warns when the model write fails with %i", async (status) => {
    failAt = 11;
    failure = () => docspaceError(status);

    const reply = await invoke(templatesController.install, { body: { id: AGENT } });

    expect(reply.status).toBe(200);
    expect(reply.body).toMatchObject({
      id: "103",
      warnings: ["modelNotSaved", "instructionsNotSaved"],
    });
    expect(calls.map((c) => c.step)).toEqual(STEPS);
  });

  function answerRoomsWith(change: Record<string, unknown>): void {
    upstream.use(
      http.post("*/api/2.0/files/rooms", async ({ request }) => {
        const body = (await request.json()) as { title: string; roomType: number };
        return step("POST rooms", body, () =>
          docspace({ ...roomDto(++nextRoomId, body.title, body.roomType), ...change }),
        );
      }),
    );
  }

  // FolderDto of a new room, field by field.
  it.each([
    ["a string id", { id: "555" }, "555", "Budgeting"],
    ["a different title", { title: "Budgeting (2)" }, "101", "Budgeting (2)"],
    ["no title", { title: undefined }, "101", "Budgeting"],
    ["a null title", { title: null }, "101", "Budgeting"],
    ["a numeric title", { title: 7 }, "101", "Budgeting"],
  ])("reads a new room with %s", async (_what, change, id, title) => {
    answerRoomsWith(change);

    const reply = await invoke(templatesController.install, {
      body: { id: "skill-sets/budgeting" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      templateId: "skill-sets/budgeting",
      id,
      title,
      kind: "skillSet",
      skillSets: [],
      knowledgeBases: [],
      warnings: [],
    });
    expect(calls[2]!.step).toBe(`upload ${id}/budget-builder.md`);
  });

  it.each([
    ["no id", { id: undefined }],
    ["a null id", { id: null }],
    ["an empty id", { id: "" }],
    ["a boolean id", { id: true }],
  ])(
    "answers 502 naming the room as unconfirmed for a new room with %s, uploading nothing",
    async (_what, change) => {
      answerRoomsWith(change);

      const reply = await invoke(templatesController.install, {
        body: { id: "skill-sets/budgeting" },
      });

      expect(reply.status).toBe(502);
      expect(reply.body).toEqual({
        error: "Bad Gateway",
        skillSets: [],
        knowledgeBases: [],
        unconfirmed: { templateId: "skill-sets/budgeting", title: "Budgeting" },
        warning: "installIncomplete",
      });
      expect(responseViolations("aiTemplatesInstall", 502, reply.body)).toEqual([]);
      expect(calls.map((c) => c.step)).toEqual(["GET people/@self", "POST rooms"]);
    },
  );

  it.each([
    ["no envelope", () => HttpResponse.json(roomDto(5, "x", 10))],
    ["a string response", () => docspace("room")],
    ["a body that is not JSON", () => HttpResponse.text("ok")],
  ])(
    "answers 502 naming the room as unconfirmed for a new room answered with %s",
    async (_what, answer) => {
      upstream.use(http.post("*/api/2.0/files/rooms", () => step("POST rooms", undefined, answer)));

      const reply = await invoke(templatesController.install, {
        body: { id: "skill-sets/budgeting" },
      });

      expect(reply.status).toBe(502);
      expect(reply.body).toEqual({
        error: "Bad Gateway",
        skillSets: [],
        knowledgeBases: [],
        unconfirmed: { templateId: "skill-sets/budgeting", title: "Budgeting" },
        warning: "installIncomplete",
      });
      expect(responseViolations("aiTemplatesInstall", 502, reply.body)).toEqual([]);
    },
  );

  it("answers 500 when the vectorization settings read times out, making no room", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.get("*/internal/ai/config", () => new Promise<Response>(() => {})));

      const pending = invoke(templatesController.install, { body: { id: AGENT } });
      await vi.advanceTimersByTimeAsync(300_000);
      const reply = await pending;

      expect(reply.status).toBe(500);
      expect(reply.body).toEqual({ error: "Internal server error" });
      expect(calls.map((c) => c.step)).toEqual(["GET people/@self"]);
    } finally {
      vi.useRealTimers();
    }
  });
});
