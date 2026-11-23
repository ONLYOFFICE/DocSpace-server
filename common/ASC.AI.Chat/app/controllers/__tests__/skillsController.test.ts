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

// The skill routes through the real request handling: the controller, createSkill /
// generateSkill, and the real Files clients (getFolderInfo, uploadFile) answered by msw.
// The storage the routes read (the AI access gate, the profile) and the model call of the
// engine are the boundary and are stubbed.

import { beforeEach, describe, expect, it, vi } from "vitest";
import { invoke, inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

const mocks = vi.hoisted(() => ({
  readDisabled: vi.fn(),
  readByType: vi.fn(),
  readById: vi.fn(),
  customSync: vi.fn(),
}));
vi.mock("../../storage/index.js", () => ({
  storage: {
    toolPrefs: { readDisabled: mocks.readDisabled },
    assignments: { readByType: mocks.readByType },
    profiles: { readById: mocks.readById },
  },
}));
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  customSync: mocks.customSync,
}));

const { AiServiceHttpError } = await import("../../storage/httpClient.js");
const { skillsController } = await import("../skillsController.js");
const { buildSkillTemplate } = await import("../../skills/createSkill.js");

const upstream = useUpstream();

// `FolderDto` (products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs, with the
// `FileEntryDto` base) of an Instructions room, as `GET api/2.0/files/folder/{id}` answers it.
const INSTRUCTIONS_ROOM_DTO = {
  id: 42,
  title: "Skills",
  parentId: 0,
  rootFolderType: 41,
  filesCount: 0,
  foldersCount: 0,
  new: 0,
  mute: false,
  tags: [],
  pinned: false,
  roomType: 10,
  private: false,
  indexing: false,
  denyDownload: false,
  type: 38,
  inRoom: true,
  security: { Read: true, Create: true, EditRoom: true, Delete: true },
  access: 0,
  shared: false,
  created: "2026-11-01T10:00:00.0000000+00:00",
  updated: "2026-11-01T10:00:00.0000000+00:00",
};

// `FileDto<int>` (products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs), the answer of
// `POST api/2.0/files/{folderId}/insert`.
const UPLOADED_FILE_DTO = {
  id: 77,
  title: "Sales report.md",
  folderId: 42,
  version: 1,
  versionGroup: 1,
  contentLength: "1 KB",
  pureContentLength: 1024,
  fileStatus: 0,
  fileExst: ".md",
  fileType: 7,
  viewUrl: "http://portal/filehandler.ashx?action=view&fileid=77",
  created: "2026-11-01T10:00:00.0000000+00:00",
};

const PROFILE = { id: "p1", name: "GPT", modelId: "gpt-x", key: "SECRET-KEY-MARKER" };

interface Call {
  method: string;
  path: string;
  cookie: string | null;
  form?: Record<string, string>;
}

// The requests of the current test, recorded by the handlers themselves: `upstream.seen`
// records a multipart body asynchronously, after the test that sent it may have ended.
const calls: Call[] = [];

function folderRoute(answer: () => Response | Promise<Response>) {
  return http.get("*/api/2.0/files/folder/:id", ({ request }) => {
    const url = new URL(request.url);
    calls.push({ method: "GET", path: url.pathname, cookie: request.headers.get("cookie") });
    return answer();
  });
}

function uploadRoute(answer: () => Response) {
  return http.post("*/api/2.0/files/:folderId/insert", async ({ request }) => {
    const url = new URL(request.url);
    const form: Record<string, string> = {};
    for (const [key, value] of await request.formData()) {
      form[key] =
        typeof value === "string" ? value : `${value.name}|${value.type}|${await value.text()}`;
    }
    calls.push({ method: "POST", path: url.pathname, cookie: request.headers.get("cookie"), form });
    return answer();
  });
}

function answerFolder(dto: unknown = INSTRUCTIONS_ROOM_DTO): void {
  upstream.use(folderRoute(() => docspace(dto as never)));
}

function answerUpload(dto: unknown = UPLOADED_FILE_DTO): void {
  upstream.use(uploadRoute(() => docspace(dto as never)));
}

const requested = (): string[] => calls.map((c) => `${c.method} ${c.path}`);

beforeEach(() => {
  calls.length = 0;
  mocks.readDisabled.mockReset().mockResolvedValue({});
  mocks.readByType
    .mockReset()
    .mockImplementation(async (type: string) => (type === "Default" ? "p1" : null));
  mocks.readById.mockReset().mockResolvedValue(PROFILE);
  mocks.customSync.mockReset();
});

const forbidden = () => new AiServiceHttpError(403, "Forbidden", "AI is off", "http://ai/x");

describe("skillsController.create", () => {
  it("saves the template into the room as <title>.md and answers the file", async () => {
    answerFolder();
    answerUpload();

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "  Sales report.md " },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ id: "77", title: "Sales report.md" });
    expect(responseViolations("aiSkillsCreate", 200, reply.body)).toEqual([]);
    expect(requested()).toEqual(["GET /api/2.0/files/folder/42", "POST /api/2.0/files/42/insert"]);
    expect(calls[1]!.form).toEqual({
      title: "Sales report.md",
      createNewIfExist: "true",
      file: `Sales report.md|text/markdown|${buildSkillTemplate("Sales report")}`,
    });
    expect(mocks.readDisabled).toHaveBeenCalledWith(undefined);
  });

  it("relays the caller's cookie to the Files API", async () => {
    answerFolder();
    answerUpload();

    const reply = await inRequestContext({ cookie: "asc_auth_key=abc" }, () =>
      invoke(skillsController.create, { body: { destId: 42, title: "Sales report" } }),
    );

    expect(reply.status).toBe(200);
    expect(calls.map((c) => c.cookie)).toEqual(["asc_auth_key=abc", "asc_auth_key=abc"]);
  });

  it("answers 403 from the AI access gate for any body, before any upstream call", async () => {
    mocks.readDisabled.mockRejectedValue(forbidden());

    const reply = await invoke(skillsController.create, { body: null });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiSkillsCreate", 403, reply.body)).toEqual([]);
    expect(calls).toEqual([]);
  });

  const DEST = "destId must be the integer id of an Instructions room";
  const TITLE = "title must be a non-empty string of at most 165 characters";
  it.each([
    ["null", null, "body must be an object"],
    ["a string", "Sales report", "body must be an object"],
    ["an array", ["42", "Sales report"], "body must be an object"],
    ["a number", 42, "body must be an object"],
    ["{}", {}, DEST],
    ["no destId", { title: "T" }, DEST],
    ["a null destId", { destId: null, title: "T" }, DEST],
    ["a boolean destId", { destId: true, title: "T" }, DEST],
    ["an object destId", { destId: { id: 42 }, title: "T" }, DEST],
    ["an empty destId", { destId: "", title: "T" }, DEST],
    ["a destId of 0", { destId: 0, title: "T" }, DEST],
    ["a negative destId", { destId: "-4", title: "T" }, DEST],
    ["a fractional destId", { destId: 4.5, title: "T" }, DEST],
    ["a destId with a leading zero", { destId: "042", title: "T" }, DEST],
    ["a destId that is not a number", { destId: "abc", title: "T" }, DEST],
    ["a destId with a space", { destId: " 42", title: "T" }, DEST],
    ["no title", { destId: "42" }, TITLE],
    ["a null title", { destId: "42", title: null }, TITLE],
    ["a numeric title", { destId: "42", title: 5 }, TITLE],
    ["an array title", { destId: "42", title: ["T"] }, TITLE],
    ["an empty title", { destId: "42", title: "" }, TITLE],
    ["a blank title", { destId: "42", title: "   " }, TITLE],
    ["a title over 165 characters", { destId: "42", title: "a".repeat(166) }, TITLE],
    [
      "a title of 166 characters with its extension",
      { destId: "42", title: `${"a".repeat(163)}.md` },
      TITLE,
    ],
    ["a title that is only an extension", { destId: "42", title: ".md" }, TITLE],
    [
      "a title without a letter or digit",
      { destId: "42", title: "!!! ---" },
      "title must contain a letter or a digit",
    ],
  ])("answers 400 for a body with %s and calls out to nothing", async (_what, body, error) => {
    const reply = await invoke(skillsController.create, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSkillsCreate", 400, reply.body)).toEqual([]);
    expect(calls).toEqual([]);
  });

  it("accepts a title of exactly 165 characters", async () => {
    answerFolder();
    answerUpload({ ...UPLOADED_FILE_DTO, title: `${"a".repeat(165)}.md` });

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "a".repeat(165) },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ id: "77", title: `${"a".repeat(165)}.md` });
  });

  it("accepts a positional array as no body at all", async () => {
    // The skill routes take named fields only: an array is not unpacked.
    const reply = await invoke(skillsController.create, { body: [42, "T"] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "body must be an object" });
  });

  it.each([
    [
      "a folder the Files service does not have",
      () => docspaceError(404),
      404,
      "The folder does not exist",
    ],
    [
      "a room of another type",
      () => docspace({ ...INSTRUCTIONS_ROOM_DTO, type: 19, roomType: 5 }),
      400,
      "Skills are created in an Instructions room only",
    ],
    [
      "a room the caller cannot create in",
      () => docspace({ ...INSTRUCTIONS_ROOM_DTO, security: { Read: true, Create: false } }),
      403,
      "The caller cannot create files in this room",
    ],
    ["a refused folder read", () => docspaceError(403), 403, "Forbidden"],
    ["a folder read that fails", () => docspaceError(500), 500, "Internal Server Error"],
  ])("writes nothing into %s", async (_what, answer, status, error) => {
    upstream.use(folderRoute(answer));

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "Sales report" },
    });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSkillsCreate", status, reply.body)).toEqual([]);
    expect(requested()).toEqual(["GET /api/2.0/files/folder/42"]);
  });

  it.each([
    ["a body without the envelope", () => HttpResponse.json(INSTRUCTIONS_ROOM_DTO)],
    ["a response without an id", () => docspace({ ...INSTRUCTIONS_ROOM_DTO, id: undefined })],
    ["a response with a null id", () => docspace({ ...INSTRUCTIONS_ROOM_DTO, id: null })],
    ["a response that is a string", () => docspace("Skills")],
  ])("answers 502 for a folder read with %s", async (_what, answer) => {
    upstream.use(folderRoute(answer));

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "Sales report" },
    });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
  });

  // FolderDto fields one at a time: what the room check makes of each.
  it.each([
    ["no type, an Instructions roomType", { type: undefined }, 200],
    ["a null type, an Instructions roomType", { type: null }, 200],
    ["a string type, an Instructions roomType", { type: "38" }, 200],
    ["an Instructions type, no roomType", { roomType: undefined }, 200],
    ["an Instructions type, a null roomType", { roomType: null }, 200],
    ["neither type nor roomType", { type: undefined, roomType: undefined }, 400],
    ["both type and roomType as strings", { type: "38", roomType: "10" }, 400],
    ["no security block", { security: undefined }, 200],
    ["a null security block", { security: null }, 200],
    ["a security block without Create", { security: { Read: true } }, 200],
    ["a Create flag that is a string", { security: { Create: "false" } }, 200],
    ["a lower-case create flag of false", { security: { create: false } }, 403],
    ["a string id", { id: "42" }, 200],
  ])("reads a folder DTO with %s", async (_what, change, status) => {
    answerFolder({ ...INSTRUCTIONS_ROOM_DTO, ...change });
    answerUpload();

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "Sales report" },
    });

    expect(reply.status).toBe(status);
    expect(responseViolations("aiSkillsCreate", status, reply.body)).toEqual([]);
  });

  it.each([
    ["the full file", UPLOADED_FILE_DTO, 200, { id: "77", title: "Sales report.md" }],
    [
      "a string id",
      { ...UPLOADED_FILE_DTO, id: "77" },
      200,
      { id: "77", title: "Sales report.md" },
    ],
    [
      "no title",
      { ...UPLOADED_FILE_DTO, title: undefined },
      200,
      { id: "77", title: "Sales report.md" },
    ],
    [
      "a null title",
      { ...UPLOADED_FILE_DTO, title: null },
      200,
      { id: "77", title: "Sales report.md" },
    ],
    [
      "a numeric title",
      { ...UPLOADED_FILE_DTO, title: 5 },
      200,
      { id: "77", title: "Sales report.md" },
    ],
    [
      "a renamed title",
      { ...UPLOADED_FILE_DTO, title: "Sales report (1).md" },
      200,
      { id: "77", title: "Sales report (1).md" },
    ],
    ["no id", { ...UPLOADED_FILE_DTO, id: undefined }, 500, { error: "Internal server error" }],
    ["a null id", { ...UPLOADED_FILE_DTO, id: null }, 500, { error: "Internal server error" }],
    ["an empty id", { ...UPLOADED_FILE_DTO, id: "" }, 500, { error: "Internal server error" }],
    ["a boolean id", { ...UPLOADED_FILE_DTO, id: true }, 500, { error: "Internal server error" }],
    ["a response that is a string", "file", 500, { error: "Internal server error" }],
    ["a null response", null, 500, { error: "Internal server error" }],
  ])("answers an upload with %s", async (_what, dto, status, body) => {
    answerFolder();
    answerUpload(dto);

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "Sales report" },
    });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(body);
    expect(responseViolations("aiSkillsCreate", status, reply.body)).toEqual([]);
  });

  it("answers an upload without the envelope with the file it holds", async () => {
    answerFolder();
    upstream.use(uploadRoute(() => HttpResponse.json(UPLOADED_FILE_DTO)));

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "Sales report" },
    });

    expect(reply.body).toEqual({ id: "77", title: "Sales report.md" });
  });

  it.each([
    [403, "Forbidden"],
    [413, "Payload Too Large"],
    [500, "Internal Server Error"],
  ])("relays a refused upload's status %i and its reason phrase only", async (status, error) => {
    answerFolder();
    upstream.use(uploadRoute(() => docspaceError(status, "internal detail http://files.internal")));

    const reply = await invoke(skillsController.create, {
      body: { destId: "42", title: "Sales report" },
    });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(JSON.stringify(reply.body)).not.toContain("internal");
  });

  it("answers 500 when the folder read times out", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(folderRoute(() => new Promise<Response>(() => {})));

      const pending = invoke(skillsController.create, {
        body: { destId: "42", title: "Sales report" },
      });
      await vi.advanceTimersByTimeAsync(300_000);
      const reply = await pending;

      expect(reply.status).toBe(500);
      expect(reply.body).toEqual({ error: "Internal server error" });
      expect(requested()).toEqual(["GET /api/2.0/files/folder/42"]);
    } finally {
      vi.useRealTimers();
    }
  });
});

describe("skillsController.generate", () => {
  const body = { destId: "42", title: "Sales Report", description: "Write weekly sales reports" };
  const SKILL =
    "---\nname: other\ndescription: Writes weekly sales reports.\n---\n# Sales report\n\n1. Do it.";

  beforeEach(() => {
    answerFolder();
  });

  it("answers the file the model wrote, frontmatter written by the service, nothing saved", async () => {
    mocks.customSync.mockResolvedValue({ role: "assistant", content: SKILL });

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      title: "Sales Report",
      name: "sales-report",
      content:
        '---\nname: "sales-report"\ndescription: "Writes weekly sales reports."\n---\n\n# Sales report\n\n1. Do it.\n',
    });
    expect(responseViolations("aiSkillsGenerate", 200, reply.body)).toEqual([]);
    expect(requested()).toEqual(["GET /api/2.0/files/folder/42"]);
    expect(mocks.readByType.mock.calls).toEqual([["Chat"], ["Default"]]);
    expect(mocks.readById).toHaveBeenCalledWith("p1");
    const [request] = mocks.customSync.mock.calls[0]! as [Record<string, unknown>];
    expect(request["profile"]).toBe(PROFILE);
    expect(request["messages"]).toEqual([
      {
        role: "user",
        content:
          "Skill name: sales-report\n\nThe user's description of the skill:\nWrite weekly sales reports",
      },
    ]);
    expect(String(request["systemPrompt"])).toContain(
      "You are given the skill's name and the user's description of the skill they need.",
    );
    expect(request["signal"]).toBeInstanceOf(AbortSignal);
    expect(JSON.stringify(reply.body)).not.toContain("SECRET-KEY-MARKER");
  });

  it("asks the model to name a skill sent without a title", async () => {
    mocks.customSync.mockResolvedValue({
      content: '---\nname: "Weekly Sales"\ndescription: d\n---\n# W\n\nBody',
    });

    const reply = await invoke(skillsController.generate, {
      body: { destId: 42, title: null, description: "Write weekly sales reports" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({
      title: "Weekly Sales",
      name: "weekly-sales",
      content: '---\nname: "weekly-sales"\ndescription: "d"\n---\n\n# W\n\nBody\n',
    });
    const [request] = mocks.customSync.mock.calls[0]! as [Record<string, unknown>];
    expect(request["messages"]).toEqual([
      { role: "user", content: "The user's description of the skill:\nWrite weekly sales reports" },
    ]);
    expect(String(request["systemPrompt"])).toContain("Name the skill and write the file.");
  });

  it("uses the Chat profile when the Chat action has one", async () => {
    mocks.readByType.mockResolvedValue("chat-profile");
    mocks.customSync.mockResolvedValue({ content: SKILL });

    await invoke(skillsController.generate, { body });

    expect(mocks.readByType.mock.calls).toEqual([["Chat"]]);
    expect(mocks.readById).toHaveBeenCalledWith("chat-profile");
  });

  it("answers 403 from the AI access gate for any body, before any upstream call", async () => {
    mocks.readDisabled.mockRejectedValue(forbidden());

    const reply = await invoke(skillsController.generate, { body: "nonsense" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiSkillsGenerate", 403, reply.body)).toEqual([]);
    expect(calls).toEqual([]);
    expect(mocks.customSync).not.toHaveBeenCalled();
  });

  const DEST = "destId must be the integer id of an Instructions room";
  const DESCRIPTION = "description must be a non-empty string of at most 2000 characters";
  it.each([
    ["null", null, "body must be an object"],
    ["a string", "Write reports", "body must be an object"],
    ["an array", ["42", "T", "d"], "body must be an object"],
    ["{}", {}, DEST],
    ["no destId", { description: "d" }, DEST],
    ["a null destId", { destId: null, description: "d" }, DEST],
    ["a boolean destId", { destId: false, description: "d" }, DEST],
    ["an empty destId", { destId: "", description: "d" }, DEST],
    ["a destId of 0", { destId: "0", description: "d" }, DEST],
    ["a fractional destId", { destId: "1.5", description: "d" }, DEST],
    ["a numeric title", { destId: "42", title: 5, description: "d" }, "title must be a string"],
    ["an array title", { destId: "42", title: ["T"], description: "d" }, "title must be a string"],
    ["an object title", { destId: "42", title: {}, description: "d" }, "title must be a string"],
    [
      "a title over 64 characters",
      { destId: "42", title: "a".repeat(65), description: "d" },
      "title must be a string of at most 64 characters",
    ],
    [
      "a title without a letter or digit",
      { destId: "42", title: "!!!", description: "d" },
      "title must contain a letter or a digit",
    ],
    ["no description", { destId: "42", title: "T" }, DESCRIPTION],
    ["a null description", { destId: "42", description: null }, DESCRIPTION],
    ["a numeric description", { destId: "42", description: 5 }, DESCRIPTION],
    ["an array description", { destId: "42", description: ["d"] }, DESCRIPTION],
    ["an empty description", { destId: "42", description: "" }, DESCRIPTION],
    ["a blank description", { destId: "42", description: " \n\t " }, DESCRIPTION],
    [
      "a description over 2000 characters",
      { destId: "42", description: "d".repeat(2001) },
      DESCRIPTION,
    ],
  ])("answers 400 for a body with %s and calls out to nothing", async (_what, input, error) => {
    const reply = await invoke(skillsController.generate, { body: input });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSkillsGenerate", 400, reply.body)).toEqual([]);
    expect(calls).toEqual([]);
    expect(mocks.customSync).not.toHaveBeenCalled();
  });

  it.each([
    ["an empty title", ""],
    ["a blank title", "   "],
    ["a title of exactly 64 characters", "a".repeat(64)],
  ])("accepts %s", async (_what, title) => {
    mocks.customSync.mockResolvedValue({ content: SKILL });

    const reply = await invoke(skillsController.generate, {
      body: { destId: "42", title, description: "d".repeat(2000) },
    });

    expect(reply.status).toBe(200);
  });

  // The description is checked for text only after the room and the profile were read.
  it("answers 400 for a description of control characters after reading the room and the profile", async () => {
    const reply = await invoke(skillsController.generate, {
      body: { destId: "42", description: "\u0000\u0007" },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "description must contain text" });
    expect(requested()).toEqual(["GET /api/2.0/files/folder/42"]);
    expect(mocks.readById).toHaveBeenCalledWith("p1");
    expect(mocks.customSync).not.toHaveBeenCalled();
  });

  it("answers 503 without a profile, before the model is called", async () => {
    mocks.readByType.mockResolvedValue(null);

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(503);
    expect(reply.body).toEqual({ error: "No model is configured for skill generation" });
    expect(responseViolations("aiSkillsGenerate", 503, reply.body)).toEqual([]);
    expect(mocks.readById).not.toHaveBeenCalled();
    expect(mocks.customSync).not.toHaveBeenCalled();
  });

  it("answers 503 when the assigned profile no longer exists", async () => {
    mocks.readById.mockResolvedValue(undefined);

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(503);
    expect(reply.body).toEqual({ error: "No model is configured for skill generation" });
  });

  it("answers 500 when the profile read fails, without its detail", async () => {
    mocks.readById.mockRejectedValue(new Error("profiles at http://ai.internal down"));

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiSkillsGenerate", 500, reply.body)).toEqual([]);
  });

  // DEFECT ctl-41: a 502 relayed from the AI service answers `{ error }` without the `code` the published 502 schema of aiSkillsGenerate requires — expected the published shape for every 502 (TEST-BRIEF response DTOs; app/apiCatalog.ts aiSkillsGenerate)
  it("relays the AI service's status when the assignment read fails", async () => {
    mocks.readByType.mockRejectedValue(
      new AiServiceHttpError(502, "Bad Gateway", "", "http://ai/x"),
    );

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
    expect(responseViolations("aiSkillsGenerate", 502, reply.body)).toEqual([
      "/ must have required property 'code'",
    ]);
  });

  it.each([
    [
      "a rate limit",
      Object.assign(new Error("429 Too many requests"), { status: 429 }),
      429,
      {
        error: "The model provider is rate-limiting requests",
        code: "rate_limit",
        providerMessage: "429 Too many requests",
      },
    ],
    [
      "a lack of credit",
      Object.assign(new Error("402 Insufficient Balance"), { status: 402 }),
      402,
      {
        error: "The model provider refused the request for lack of credit",
        code: "insufficient_funds",
        providerMessage: "402 Insufficient Balance",
      },
    ],
    [
      "the portal's spending limit",
      Object.assign(new Error("Service limit exceeded for the tenant"), { status: 500 }),
      502,
      {
        error: "The model request failed",
        code: "server",
        providerMessage: "Service limit exceeded for the tenant",
      },
    ],
    [
      "an auth failure, without the provider's text",
      Object.assign(new Error("401 Incorrect API key sk-***123"), { status: 401 }),
      502,
      { error: "The model request failed", code: "auth" },
    ],
    [
      "a timeout",
      Object.assign(new Error("aborted"), { name: "AbortError" }),
      504,
      { error: "The model did not answer within the time budget", code: "timeout" },
    ],
  ])(
    "answers %s as its status, code and relayed text",
    async (_what, failure, status, expected) => {
      mocks.customSync.mockRejectedValue(failure);

      const reply = await invoke(skillsController.generate, { body });

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual(expected);
      expect(responseViolations("aiSkillsGenerate", status, reply.body)).toEqual([]);
    },
  );

  it("answers 504 when the model does not answer within the budget", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      mocks.customSync.mockImplementation(
        ({ signal }: { signal: AbortSignal }) =>
          new Promise((_resolve, reject) => {
            signal.addEventListener("abort", () => reject(new Error("stopped")));
          }),
      );

      const pending = invoke(skillsController.generate, { body });
      await vi.advanceTimersByTimeAsync(120_000);
      const reply = await pending;

      expect(reply.status).toBe(504);
      expect(reply.body).toEqual({
        error: "The model did not answer within the time budget",
        code: "timeout",
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it.each([
    ["an empty answer", "", "The model returned an empty skill"],
    [
      "an answer of frontmatter only",
      "---\nname: x\ndescription: d\n---\n",
      "The model returned an empty skill",
    ],
  ])("answers 502 for %s", async (_what, content, error) => {
    mocks.customSync.mockResolvedValue({ content });

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error, code: "empty_answer" });
    expect(responseViolations("aiSkillsGenerate", 502, reply.body)).toEqual([]);
  });

  it("answers 502 for an untitled skill the model left without a name", async () => {
    mocks.customSync.mockResolvedValue({ content: "Just a body" });

    const reply = await invoke(skillsController.generate, {
      body: { destId: "42", description: "Write weekly sales reports" },
    });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({
      error: "The model returned a skill without a name",
      code: "empty_answer",
    });
  });

  it("answers 502 when neither the model nor the user's words give a description", async () => {
    mocks.customSync.mockResolvedValue({ content: "# Sales report\n\nBody" });

    const reply = await invoke(skillsController.generate, {
      body: { destId: "42", title: "Sales", description: "<b></b>" },
    });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({
      error: "The model returned a skill without a description",
      code: "empty_answer",
    });
  });

  it.each([
    [
      "a folder the Files service does not have",
      () => docspaceError(404),
      404,
      "The folder does not exist",
    ],
    [
      "a plain folder",
      () => docspace({ ...INSTRUCTIONS_ROOM_DTO, type: 1, roomType: undefined }),
      400,
      "Skills are created in an Instructions room only",
    ],
    [
      "a room the caller cannot create in",
      () => docspace({ ...INSTRUCTIONS_ROOM_DTO, security: { Create: false } }),
      403,
      "The caller cannot create files in this room",
    ],
    ["a folder read that fails", () => docspaceError(503), 503, "Service Unavailable"],
  ])("answers %s before the model is called", async (_what, answer, status, error) => {
    upstream.use(folderRoute(answer));

    const reply = await invoke(skillsController.generate, { body });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSkillsGenerate", status, reply.body)).toEqual([]);
    expect(mocks.customSync).not.toHaveBeenCalled();
  });
});
