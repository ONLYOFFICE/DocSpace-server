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

import type { AssignmentsEngine, ThreadsEngine } from "@onlyoffice/ai-chat/core";
import { invoke } from "../../__tests__/helpers/http.js";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import type { Reply } from "../../__tests__/helpers/http.js";

const engine = engineMock<ThreadsEngine>();
const assignments = engineMock<AssignmentsEngine>();
const threadsEngineDeps: { resolveSource?: () => unknown }[] = [];
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  // engineClass, plus a record of what the controller builds the engine with.
  ThreadsEngine: class {
    constructor(deps: { resolveSource?: () => unknown }) {
      threadsEngineDeps.push(deps);
      return engine;
    }
  },
  AssignmentsEngine: engineClass(assignments),
}));
const readThread = vi.fn<(id: string) => Promise<unknown>>();
const readMessage = vi.fn<(id: string) => Promise<unknown>>();
const readProfile = vi.fn<(id: string) => Promise<unknown>>();
// HttpStorageAdapter: the base of the resolve engine's storage in assignmentsController, which
// the thread gate calls; the engine class itself is mocked.
vi.mock("../../storage/index.js", () => ({
  storage: {
    threads: { readById: readThread },
    messages: { readById: readMessage },
    profiles: { readById: readProfile },
  },
  HttpStorageAdapter: class {},
}));
const assertEntityAccessible = vi.fn<(entityId?: string) => Promise<void>>();
const primeSourceMeta = vi.fn<(entityId?: string) => Promise<void>>();
vi.mock("../../storage/docspaceFilesApi.js", async (original) => ({
  ...(await original<typeof import("../../storage/docspaceFilesApi.js")>()),
  assertEntityAccessible,
  primeSourceMeta,
}));
const readAiProjectChatContext = vi.fn<(entityId?: string) => Promise<unknown>>();
vi.mock("../../storage/aiProjectChatContext.js", () => ({ readAiProjectChatContext }));

const { threadsController } = await import("../threadsController.js");
// After the mocks: these modules load the library, whose engine classes are replaced above.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");
const { DocspaceApiHttpError } = await import("../../storage/docspaceFilesApi.js");
const { ActionType } = await import("@onlyoffice/ai-chat/core");

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

const profile = {
  id: "11111111-1111-1111-1111-111111111111",
  name: "OpenAI",
  providerType: "openai" as const,
  baseUrl: "https://api.openai.com/v1",
  modelId: "gpt-4o",
};
const thread = {
  threadId: "t1",
  title: "Report",
  lastEditDate: 1767225600000,
  profileId: profile.id,
};
const userMessage = { role: "user" as const, content: [{ type: "text" as const, text: "hi" }] };
// The engine types `createdAt` as a Date; it leaves as the ISO string of the published schema.
const storedMessage = { ...userMessage, id: "m1", createdAt: new Date("2026-01-01T00:00:00.000Z") };
const storedMessageJson = { ...userMessage, id: "m1", createdAt: "2026-01-01T00:00:00.000Z" };

beforeEach(() => {
  mockReset(engine);
  mockReset(assignments);
  for (const spy of [readThread, readMessage, readProfile, readAiProjectChatContext]) {
    spy.mockReset();
  }
  assertEntityAccessible.mockReset().mockResolvedValue(undefined);
  primeSourceMeta.mockReset().mockResolvedValue(undefined);
  readProfile.mockResolvedValue(profile);
  readAiProjectChatContext.mockResolvedValue(undefined);
});

// A profile id as the AI service issues it.
const PROFILE_UUID = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

describe("the threads engine", () => {
  it("resolves the source of a title request from the request context", () => {
    expect(threadsEngineDeps).toHaveLength(1);
    expect(threadsEngineDeps[0]?.resolveSource?.()).toBeUndefined();
  });
});

describe("threadsController.create", () => {
  const body = { title: "Report", profileId: profile.id, entityId: "12" };

  it("creates the thread with an explicit profile that exists", async () => {
    engine.create.mockResolvedValue(thread);

    const reply = await invoke(threadsController.create, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(thread);
    expect(assertEntityAccessible).toHaveBeenCalledWith("12");
    expect(readAiProjectChatContext).toHaveBeenCalledWith("12");
    expect(readProfile).toHaveBeenCalledWith(profile.id);
    expect(assignments.tryResolveForAction).not.toHaveBeenCalled();
    expect(engine.create).toHaveBeenCalledWith(body);
    expect(published("aiThreadsCreate", reply)).toEqual([]);
  });

  it("answers the status of the Files API when the entity cannot be reached, and creates nothing", async () => {
    assertEntityAccessible.mockRejectedValue(
      new DocspaceApiHttpError(403, "Forbidden", "http://x"),
    );

    const reply = await invoke(threadsController.create, { body });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(engine.create).not.toHaveBeenCalled();
    expect(published("aiThreadsCreate", reply)).toEqual([]);
  });

  it("answers 403 to a caller who may not chat in the AI project", async () => {
    readAiProjectChatContext.mockResolvedValue({ canChat: false });

    const reply = await invoke(threadsController.create, { body });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "You cannot chat in this AI project" });
    expect(engine.create).not.toHaveBeenCalled();
    expect(published("aiThreadsCreate", reply)).toEqual([]);
  });

  it.each([
    ["no available model", { canChat: true, profileAvailable: false, effectiveProfileId: "p" }],
    ["no effective profile", { canChat: true, profileAvailable: true }],
  ])("answers 409 for an AI project with %s", async (_what, project) => {
    readAiProjectChatContext.mockResolvedValue(project);

    const reply = await invoke(threadsController.create, { body });

    expect(reply.status).toBe(409);
    expect(reply.body).toEqual({ error: "The model of this AI project is not available" });
    expect(engine.create).not.toHaveBeenCalled();
    expect(published("aiThreadsCreate", reply)).toEqual([]);
  });

  it("creates the thread in an AI project whose model is available, without reading the profile", async () => {
    readAiProjectChatContext.mockResolvedValue({
      canChat: true,
      profileAvailable: true,
      effectiveProfileId: profile.id,
    });
    engine.create.mockResolvedValue(thread);

    const reply = await invoke(threadsController.create, { body });

    expect(reply.status).toBe(200);
    expect(readProfile).not.toHaveBeenCalled();
  });

  it.each([
    ["no profileId", { title: "T" }],
    ["an unknown profileId", { title: "T", profileId: "p9" }],
    ["a malformed profileId (Bug 83045)", { title: "T", profileId: "x" }],
  ])("falls back to the Chat assignment for %s", async (what, sent) => {
    if (what.startsWith("an unknown")) readProfile.mockResolvedValue(undefined);
    if (what.startsWith("a malformed")) {
      readProfile.mockRejectedValue(new AiServiceHttpError(400, "Bad Request", "", "u"));
    }
    assignments.tryResolveForAction.mockResolvedValue({ profile } as never);
    engine.create.mockResolvedValue(thread);

    const reply = await invoke(threadsController.create, { body: sent });

    expect(reply.status).toBe(200);
    expect(assignments.tryResolveForAction).toHaveBeenCalledWith(ActionType.Chat, undefined);
  });

  it.each([
    ["nothing", null],
    ["an assignment without a profile", {}],
  ])("answers 404 when the Chat assignment resolves %s", async (_what, resolved) => {
    assignments.tryResolveForAction.mockResolvedValue(resolved as never);

    const reply = await invoke(threadsController.create, { body: { title: "T", entityId: "5" } });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "no AI profile is available for this thread" });
    expect(assignments.tryResolveForAction).toHaveBeenCalledWith(ActionType.Chat, "5");
    expect(engine.create).not.toHaveBeenCalled();
    expect(published("aiThreadsCreate", reply)).toEqual([]);
  });

  it.each([
    [
      "a 500",
      new AiServiceHttpError(500, "Internal Server Error", "", "u"),
      "Internal Server Error",
    ],
    ["a timeout", new Error("upstream timeout after 300000ms"), "Internal server error"],
  ])("answers 500, not 404, when the profile read fails with %s", async (_what, failure, error) => {
    readProfile.mockRejectedValue(failure);

    const reply = await invoke(threadsController.create, { body });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error });
    expect(assignments.tryResolveForAction).not.toHaveBeenCalled();
    expect(engine.create).not.toHaveBeenCalled();
  });

  // HANDLED DOWNSTREAM data-10: threads/create narrows nothing — a body without a title, with a title of the wrong type or a body that is not an object is handed to the engine and stored, although the comment of threads/rename says create refuses a missing or empty title. In production the AI service refuses these bodies with 400 (CreateThreadRequestDto: a required Title of at most 255 characters; ASC.AI.Tests, ThreadsCreateTests); it shows only with the engine or the service stubbed.
  it.each([
    ["{}", {}],
    ["null", null],
    ["a string", "Report"],
    ["an empty title", { title: "" }],
    ["a blank title", { title: "  " }],
    ["a title that is a number", { title: 5 }],
    ["a profileId that is a number", { title: "T", profileId: 5 }],
    ["an entityId that is an object", { title: "T", entityId: {} }],
  ])("hands a body that is %s to the engine", async (_what, sent) => {
    assignments.tryResolveForAction.mockResolvedValue({ profile } as never);
    engine.create.mockResolvedValue(thread);

    const reply = await invoke(threadsController.create, { body: sent });

    expect(reply.status).toBe(200);
    expect(engine.create).toHaveBeenCalledWith(sent);
  });
});

describe("threadsController.openOrCreate", () => {
  const body = { profile: { id: profile.id, baseUrl: "http://169.254.169.254" }, entityId: "12" };

  it("opens the thread with the stored profile, not the one sent", async () => {
    const result = { threadId: "t1", title: "Report", priorMessages: [storedMessage] };
    engine.openOrCreate.mockResolvedValue(result);

    const reply = await invoke(threadsController.openOrCreate, {
      body: { ...body, threadId: "t1" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ ...result, priorMessages: [storedMessageJson] });
    expect(assertEntityAccessible).toHaveBeenCalledWith("12");
    expect(primeSourceMeta).toHaveBeenCalledWith("12");
    expect(engine.openOrCreate).toHaveBeenCalledWith({ ...body, threadId: "t1", profile });
    expect(readAiProjectChatContext).not.toHaveBeenCalled();
    expect(published("aiThreadsOpenOrCreate", reply)).toEqual([]);
  });

  it("gates the creation of a new thread like threads/create", async () => {
    readAiProjectChatContext.mockResolvedValue({ canChat: false });

    const reply = await invoke(threadsController.openOrCreate, { body });

    expect(reply.status).toBe(403);
    expect(readAiProjectChatContext).toHaveBeenCalledWith("12");
    expect(engine.openOrCreate).not.toHaveBeenCalled();
    expect(published("aiThreadsOpenOrCreate", reply)).toEqual([]);
  });

  it("checks the stored profile's id when it creates outside a project", async () => {
    engine.openOrCreate.mockResolvedValue({ threadId: "t2", title: "", priorMessages: [] });

    const reply = await invoke(threadsController.openOrCreate, {
      body: { profile: { id: profile.id } },
    });

    expect(reply.status).toBe(200);
    expect(assertEntityAccessible).toHaveBeenCalledWith(undefined);
    expect(readProfile).toHaveBeenCalledTimes(2);
    expect(published("aiThreadsOpenOrCreate", reply)).toEqual([]);
  });

  it.each([
    ["null", null],
    ["a string", "t1"],
    ["an array", [body]],
    ["{}", {}],
    ["a profile that is a string", { profile: profile.id }],
    ["a profile that is null", { profile: null }],
    ["a profile that is an array", { profile: [profile] }],
  ])("answers 404 for a body that is %s", async (_what, sent) => {
    const reply = await invoke(threadsController.openOrCreate, { body: sent });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "an AI profile is required to open or create a thread" });
    expect(assertEntityAccessible).not.toHaveBeenCalled();
    expect(published("aiThreadsOpenOrCreate", reply)).toEqual([]);
  });

  it.each([
    ["a profile without an id", { profile: {} }, undefined],
    ["a profile whose id is a number", { profile: { id: 5 } }, undefined],
    ["a profile whose id is empty", { profile: { id: "" } }, undefined],
    ["an unknown profile", { profile: { id: "p9" } }, "unknown"],
    ["a malformed profile id", { profile: { id: "x" } }, "malformed"],
  ])("answers 404 for %s", async (_what, sent, read) => {
    if (read === "unknown") readProfile.mockResolvedValue(undefined);
    if (read === "malformed") {
      readProfile.mockRejectedValue(new AiServiceHttpError(400, "Bad Request", "", "u"));
    }

    const reply = await invoke(threadsController.openOrCreate, { body: sent });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "the AI profile does not exist" });
    expect(engine.openOrCreate).not.toHaveBeenCalled();
    expect(published("aiThreadsOpenOrCreate", reply)).toEqual([]);
  });

  it.each([
    [
      "a 500",
      new AiServiceHttpError(500, "Internal Server Error", "", "u"),
      "Internal Server Error",
    ],
    ["a timeout", new Error("upstream timeout"), "Internal server error"],
  ])("answers 500, not 404, when the profile read fails with %s", async (_what, failure, error) => {
    readProfile.mockRejectedValue(failure);

    const reply = await invoke(threadsController.openOrCreate, { body });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error });
    expect(published("aiThreadsOpenOrCreate", reply)).toEqual([]);
  });

  it("answers the status of the Files API for an entity that cannot be reached", async () => {
    assertEntityAccessible.mockRejectedValue(
      new DocspaceApiHttpError(404, "Not Found", "http://x"),
    );

    const reply = await invoke(threadsController.openOrCreate, { body });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Not Found" });
    expect(primeSourceMeta).not.toHaveBeenCalled();
  });

  it("ignores an entityId that is not a string", async () => {
    engine.openOrCreate.mockResolvedValue({ threadId: "t2", title: "", priorMessages: [] });

    await invoke(threadsController.openOrCreate, {
      body: { profile: { id: profile.id }, entityId: 12 },
    });

    expect(assertEntityAccessible).toHaveBeenCalledWith(undefined);
    expect(primeSourceMeta).toHaveBeenCalledWith(undefined);
  });
});

describe("threadsController.appendUserMessage", () => {
  it.each([
    ["a named body", { threadId: "t1", message: userMessage, profileId: PROFILE_UUID }],
    ["a positional body", ["t1", userMessage, PROFILE_UUID]],
  ])("stores a user message from %s", async (_what, body) => {
    engine.appendUserMessage.mockResolvedValue(storedMessage);

    const reply = await invoke(threadsController.appendUserMessage, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ messageId: storedMessageJson });
    expect(engine.appendUserMessage).toHaveBeenCalledWith("t1", userMessage, PROFILE_UUID);
    expect(published("aiThreadsAppendUserMessage", reply)).toEqual([]);
  });

  it.each([
    ["a string", "hello"],
    ["an attachment-only part list", [{ type: "file", data: "x", mimeType: "text/plain" }]],
    ["a string part", ["hello"]],
    [
      "five files and five images",
      [
        ...Array.from({ length: 5 }, () => ({ type: "file" })),
        ...Array.from({ length: 5 }, () => ({ type: "image" })),
      ],
    ],
  ])("stores a message whose content is %s", async (_what, content) => {
    engine.appendUserMessage.mockResolvedValue(storedMessage);

    const reply = await invoke(threadsController.appendUserMessage, {
      body: ["t1", { role: "user", content }],
    });

    expect(reply.status).toBe(200);
  });

  const noContent =
    "message.content must be a non-empty string or a list with at least one non-empty part";
  it.each([
    ["null", null, "threadId required"],
    ["a string", "t1", "threadId required"],
    ["{}", {}, "threadId required"],
    ["an empty threadId", ["", userMessage], "threadId required"],
    ["a threadId that is a number", [1, userMessage], "threadId required"],
    ["no message", ["t1"], "message is required and must be an object"],
    ["a message that is a string", ["t1", "hi"], "message is required and must be an object"],
    [
      "a message that is an array",
      ["t1", [userMessage]],
      "message is required and must be an object",
    ],
    ["no role", ["t1", { content: "hi" }], "message.role must be one of user, assistant, system"],
    [
      "an unknown role",
      ["t1", { role: "tool", content: "hi" }],
      "message.role must be one of user, assistant, system",
    ],
    [
      "a role that is a number",
      ["t1", { role: 1, content: "hi" }],
      "message.role must be one of user, assistant, system",
    ],
    ["no content", ["t1", { role: "user" }], noContent],
    ["a blank string content", ["t1", { role: "user", content: "  " }], noContent],
    ["an empty part list", ["t1", { role: "user", content: [] }], noContent],
    [
      "blank parts only",
      [
        "t1",
        {
          role: "user",
          content: [" ", null, 5, { type: "text", text: " " }, { type: "text" }, { type: "" }, {}],
        },
      ],
      noContent,
    ],
    [
      "a content that is an object",
      ["t1", { role: "user", content: { type: "text", text: "hi" } }],
      noContent,
    ],
    [
      "an assistant message (Bug 84410)",
      ["t1", { role: "assistant", content: "hi" }],
      "append-user-message stores user messages only",
    ],
    [
      "a system message",
      ["t1", { role: "system", content: "hi" }],
      "append-user-message stores user messages only",
    ],
    [
      "six files (Bug 82894)",
      [
        "t1",
        {
          role: "user",
          content: [
            { type: "text", text: "x" },
            ...Array.from({ length: 6 }, () => ({ type: "file" })),
          ],
        },
      ],
      "a message may carry at most 5 file and 5 image attachments (got 6 file(s), 0 image(s))",
    ],
    [
      "six images",
      ["t1", { role: "user", content: Array.from({ length: 6 }, () => ({ type: "image" })) }],
      "a message may carry at most 5 file and 5 image attachments (got 0 file(s), 6 image(s))",
    ],
  ])("answers 400 for a body with %s", async (_what, body, error) => {
    const reply = await invoke(threadsController.appendUserMessage, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.appendUserMessage).not.toHaveBeenCalled();
    expect(published("aiThreadsAppendUserMessage", reply)).toEqual([]);
  });

  // The engine writes the message and binds the profile side by side, and the AI service
  // refuses a profile id that is no UUID: without this check the message would be stored
  // while the caller reads 400.
  it.each([
    ["a number", 7],
    ["an object", { id: "p1" }],
    ["a string that is no UUID", "p1"],
  ])(
    "answers 400 for a profileId that is %s, before anything is stored",
    async (_what, profileId) => {
      const reply = await invoke(threadsController.appendUserMessage, {
        body: ["t1", userMessage, profileId],
      });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "profileId must be the id of a profile (a UUID)" });
      expect(engine.appendUserMessage).not.toHaveBeenCalled();
      expect(published("aiThreadsAppendUserMessage", reply)).toEqual([]);
    },
  );

  it.each([
    ["a UUID", PROFILE_UUID],
    ["a UUID without hyphens", "3fa85f6457174562b3fc2c963f66afa6"],
    ["null, as the widget sends an omitted argument", null],
    ["an empty string", ""],
  ])("hands a profileId that is %s to the engine as it came", async (_what, profileId) => {
    engine.appendUserMessage.mockResolvedValue(storedMessage);

    const reply = await invoke(threadsController.appendUserMessage, {
      body: ["t1", userMessage, profileId],
    });

    expect(reply.status).toBe(200);
    expect(engine.appendUserMessage).toHaveBeenCalledWith("t1", userMessage, profileId);
  });

  // CONTRACT data-12: a text of any length is stored; the route has no limit of its own besides the 100 KB of the JSON parser. Pinned by openspec/specs/ai-agents/spec.md ("store a user message verbatim") and ASC.AI.Tests (ThreadMessagesAppendTests stores a text of 20 000 characters).
  it("stores a part list of 1 000 text parts", async () => {
    engine.appendUserMessage.mockResolvedValue(storedMessage);
    const content = Array.from({ length: 1000 }, () => ({ type: "text", text: "x" }));

    const reply = await invoke(threadsController.appendUserMessage, {
      body: ["t1", { role: "user", content }],
    });

    expect(reply.status).toBe(200);
  });
});

describe("threadsController.touch", () => {
  it.each([
    ["a named body", { threadId: "t1", profileId: "p1" }, "p1"],
    ["a positional body", ["t1"], undefined],
  ])("touches the thread named by %s", async (_what, body, profileId) => {
    engine.touch.mockResolvedValue(undefined);

    const reply = await invoke(threadsController.touch, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.touch).toHaveBeenCalledWith("t1", profileId);
    expect(published("aiThreadsTouch", reply)).toEqual([]);
  });

  it.each([[null], ["t1"], [{}], [{ threadId: "" }], [{ threadId: 7 }]])(
    "answers 400 for the body %j",
    async (body) => {
      const reply = await invoke(threadsController.touch, { body });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "threadId is required" });
      expect(published("aiThreadsTouch", reply)).toEqual([]);
    },
  );
});

describe("threadsController.rename", () => {
  it.each([
    ["a named body", { threadId: "t1", title: "New" }],
    ["a positional body", ["t1", "New"]],
  ])("renames the thread from %s", async (_what, body) => {
    engine.rename.mockResolvedValue(undefined);

    const reply = await invoke(threadsController.rename, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.rename).toHaveBeenCalledWith("t1", "New");
    expect(published("aiThreadsRename", reply)).toEqual([]);
  });

  const noTitle = "title is required and must be a non-empty string";
  it.each([
    ["null", null, "threadId required"],
    ["a string", "t1", "threadId required"],
    ["{}", {}, "threadId required"],
    ["an empty threadId", ["", "T"], "threadId required"],
    ["a threadId that is a number", [1, "T"], "threadId required"],
    ["no title (Bug 83094)", ["t1"], noTitle],
    ["a null title", ["t1", null], noTitle],
    ["an empty title", ["t1", ""], noTitle],
    ["a blank title", ["t1", "   "], noTitle],
    ["a title that is a number", ["t1", 5], noTitle],
  ])("answers 400 for a body with %s", async (_what, body, error) => {
    const reply = await invoke(threadsController.rename, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.rename).not.toHaveBeenCalled();
    expect(published("aiThreadsRename", reply)).toEqual([]);
  });

  // HANDLED DOWNSTREAM data-13: a title of any length is stored. In production the AI service refuses a title over 255 characters with 400 (UpdateThreadRequestDto; ASC.AI.Tests, ThreadTitleLengthTests); it shows only with the engine or the service stubbed.
  it("renames the thread to a title of 10 000 characters", async () => {
    const reply = await invoke(threadsController.rename, { body: ["t1", "t".repeat(10_000)] });

    expect(reply.status).toBe(200);
    expect(engine.rename).toHaveBeenCalledWith("t1", "t".repeat(10_000));
  });
});

describe("threadsController.delete", () => {
  it.each([
    ["the bare id as the body", { body: "t1" }],
    ["a named body", { body: { threadId: "t1" } }],
    ["the query", { query: { threadId: "t1" } }],
  ])("deletes a thread that exists, named by %s", async (_what, init) => {
    readThread.mockResolvedValue(thread);

    const reply = await invoke(threadsController.delete, { method: "DELETE", ...init });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(readThread).toHaveBeenCalledWith("t1");
    expect(engine.delete).toHaveBeenCalledWith("t1");
    expect(published("aiThreadsDelete", reply)).toEqual([]);
  });

  it.each([
    ["null", { body: null }],
    ["an empty id", { body: "" }],
    ["{}", { body: {} }],
    ["an id that is a number", { body: { threadId: 3 } }],
  ])("answers 400 for %s", async (_what, init) => {
    const reply = await invoke(threadsController.delete, init);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "threadId required" });
    expect(readThread).not.toHaveBeenCalled();
    expect(published("aiThreadsDelete", reply)).toEqual([]);
  });

  it("answers 404 for a thread the store does not have (Bug 83095)", async () => {
    readThread.mockResolvedValue(null);

    const reply = await invoke(threadsController.delete, { body: "t9" });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "thread not found" });
    expect(engine.delete).not.toHaveBeenCalled();
    expect(published("aiThreadsDelete", reply)).toEqual([]);
  });

  it.each([
    ["a 500", new AiServiceHttpError(500, "Internal Server Error", "", "u")],
    ["a timeout", new Error("upstream timeout")],
  ])("answers 500, not 404, when the thread read fails with %s", async (_what, failure) => {
    readThread.mockRejectedValue(failure);

    const reply = await invoke(threadsController.delete, { body: "t1" });

    expect(reply.status).toBe(500);
    expect(engine.delete).not.toHaveBeenCalled();
  });
});

describe("threadsController.clearMessages and deleteMessage", () => {
  it.each([
    ["clearMessages", "threadId", "aiThreadsClearMessages"],
    ["deleteMessage", "messageId", "aiThreadsDeleteMessage"],
  ] as const)(
    "%s takes the id from the body, a named body or the query",
    async (method, field, op) => {
      for (const init of [
        { body: "x1" },
        { body: { [field]: "x1" } },
        { query: { [field]: "x1" } },
      ]) {
        const reply = await invoke(threadsController[method], { method: "DELETE", ...init });

        expect(reply.status).toBe(200);
        expect(reply.body).toEqual({ success: true });
        expect(published(op, reply)).toEqual([]);
      }
      expect(engine[method].mock.calls).toEqual([["x1"], ["x1"], ["x1"]]);
    },
  );

  it.each([
    ["clearMessages", "threadId", "aiThreadsClearMessages"],
    ["deleteMessage", "messageId", "aiThreadsDeleteMessage"],
  ] as const)("%s answers 400 without an id", async (method, field, op) => {
    for (const init of [
      { body: null },
      { body: "" },
      { body: { [field]: 1 } },
      { query: { [field]: [] } },
    ]) {
      const reply = await invoke(threadsController[method], init);

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: `${field} required` });
      expect(published(op, reply)).toEqual([]);
    }
    expect(engine[method]).not.toHaveBeenCalled();
  });
});

describe("threadsController.regenerateTitle", () => {
  it.each([
    ["no entity", ["t1", { id: profile.id }], undefined],
    [
      "an entity hint",
      ["t1", { id: profile.id }, { entityId: "12", source: { type: "forged" } }],
      "12",
    ],
    ["an entity hint that is not an object", ["t1", { id: profile.id }, "12"], undefined],
    [
      "an entity hint whose id is a number",
      { threadId: "t1", profile: { id: profile.id }, entityMeta: { entityId: 12 } },
      undefined,
    ],
  ])("regenerates the title with the stored profile, for %s", async (_what, body, entityId) => {
    engine.regenerateTitle.mockResolvedValue("Quarterly report");

    const reply = await invoke(threadsController.regenerateTitle, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ title: "Quarterly report" });
    expect(primeSourceMeta).toHaveBeenCalledWith(entityId);
    expect(engine.regenerateTitle).toHaveBeenCalledWith("t1", profile);
    expect(published("aiThreadsRegenerateTitle", reply)).toEqual([]);
  });

  const required = "threadId (string) and profile (object) are required";
  it.each([
    ["null", null],
    ["a string", "t1"],
    ["{}", {}],
    ["a threadId that is a number", [1, profile]],
    ["no profile (Bug 82828)", ["t1"]],
    ["a profile that is a string", ["t1", profile.id]],
    ["a profile that is null", ["t1", null]],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await invoke(threadsController.regenerateTitle, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: required });
    expect(primeSourceMeta).not.toHaveBeenCalled();
    expect(published("aiThreadsRegenerateTitle", reply)).toEqual([]);
  });

  it("answers 400 for an empty threadId, before the engine", async () => {
    const reply = await invoke(threadsController.regenerateTitle, {
      body: ["", { id: profile.id }],
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "threadId (string) and profile (object) are required" });
    expect(engine.regenerateTitle).not.toHaveBeenCalled();
  });

  it("answers 404 for a profile the store does not have", async () => {
    readProfile.mockResolvedValue(undefined);

    const reply = await invoke(threadsController.regenerateTitle, { body: ["t1", { id: "p9" }] });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "the AI profile does not exist" });
    expect(engine.regenerateTitle).not.toHaveBeenCalled();
    expect(published("aiThreadsRegenerateTitle", reply)).toEqual([]);
  });
});

describe("threadsController.list", () => {
  it("lists with every parameter of the query", async () => {
    engine.list.mockResolvedValue([thread]);

    const reply = await invoke(threadsController.list, {
      method: "GET",
      query: {
        entityId: "12",
        count: "20",
        cursor: JSON.stringify({ id: "t0", lastEditDate: 1767225600000 }),
        query: "report",
      },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([thread]);
    expect(engine.list).toHaveBeenCalledWith(
      "12",
      20,
      { lastEditDate: 1767225600000, threadId: "t0", id: "t0" },
      "report",
    );
    expect(published("aiThreadsList", reply)).toEqual([]);
  });

  it.each([
    ["absent", undefined, undefined],
    ["empty", "", undefined],
    ["not JSON", "{id", undefined],
    ["a JSON array", "[1]", undefined],
    ["a JSON string", '"t0"', undefined],
    ["without lastEditDate", '{"id":"t0"}', undefined],
    ["with a lastEditDate string", '{"id":"t0","lastEditDate":"1"}', undefined],
    ["with an id that is a number", '{"id":5,"lastEditDate":1}', undefined],
    [
      "with threadId and a null lastEditDate",
      '{"threadId":"t0","lastEditDate":null}',
      { lastEditDate: null, threadId: "t0", id: "t0" },
    ],
    [
      "with both ids",
      '{"id":"a","threadId":"b","lastEditDate":2}',
      { lastEditDate: 2, threadId: "a", id: "a" },
    ],
  ])("reads a cursor that is %s", async (_what, cursor, parsed) => {
    engine.list.mockResolvedValue([]);

    await invoke(threadsController.list, {
      method: "GET",
      query: cursor === undefined ? {} : { cursor },
    });

    expect(engine.list).toHaveBeenCalledWith(undefined, undefined, parsed, undefined);
  });

  // HANDLED DOWNSTREAM data-15: `count` has no bounds — a negative, a huge or a half-numeric value ("20abc" read as 20) goes to the engine, a value that is not a number is dropped silently. In production the AI service bounds `count` with 400 (ReadAllThreadsRequestDto: Range(1, 1000); ASC.AI.Tests, ThreadsListTests); it shows only with the engine or the service stubbed.
  it.each([
    ["-5", -5],
    ["1000000", 1000000],
    ["20abc", 20],
    ["abc", undefined],
  ])("hands a count of %j to the engine as %j", async (count, parsed) => {
    engine.list.mockResolvedValue([]);

    const reply = await invoke(threadsController.list, { method: "GET", query: { count } });

    expect(reply.status).toBe(200);
    expect(engine.list).toHaveBeenCalledWith(undefined, parsed, undefined, undefined);
  });

  it("ignores repeated entityId and query parameters", async () => {
    engine.list.mockResolvedValue([]);

    await invoke(threadsController.list, {
      method: "GET",
      query: { entityId: ["1", "2"], query: ["a"] },
    });

    expect(engine.list).toHaveBeenCalledWith(undefined, undefined, undefined, undefined);
  });
});

describe("threadsController.readMessages", () => {
  it("reads with every parameter of the query", async () => {
    engine.readMessages.mockResolvedValue([storedMessage]);

    const reply = await invoke(threadsController.readMessages, {
      method: "GET",
      query: {
        threadId: "t1",
        count: "10",
        cursor: '{"id":"m0","createdAt":"x"}',
        direction: "desc",
      },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([storedMessageJson]);
    expect(engine.readMessages).toHaveBeenCalledWith(
      "t1",
      10,
      { id: "m0", createdAt: "x" },
      "desc",
    );
    expect(published("aiThreadsReadMessages", reply)).toEqual([]);
  });

  it.each([
    ["", undefined],
    ["{id", undefined],
    ['{"createdAt":"x"}', undefined],
    ['["m0"]', undefined],
    [["a", "b"], undefined],
    ['{"id":"m0"}', { id: "m0", createdAt: null }],
    ['{"id":"m0","createdAt":true}', { id: "m0", createdAt: null }],
    // An epoch-ms position of a message stored before ISO strings, which the storage takes too.
    ['{"id":"m0","createdAt":1767225600000}', { id: "m0", createdAt: "2026-01-01T00:00:00.000Z" }],
    ['{"id":"m0","createdAt":1e300}', { id: "m0", createdAt: null }],
  ])("reads the cursor %j as %j", async (cursor, parsed) => {
    engine.readMessages.mockResolvedValue([]);

    await invoke(threadsController.readMessages, {
      method: "GET",
      query: { threadId: "t1", cursor },
    });

    expect(engine.readMessages).toHaveBeenCalledWith("t1", undefined, parsed, undefined);
  });

  it.each([["asc"], ["DESC"], [["desc"]]])(
    "reads forward for a direction of %j",
    async (direction) => {
      engine.readMessages.mockResolvedValue([]);

      await invoke(threadsController.readMessages, {
        method: "GET",
        query: { threadId: "t1", direction },
      });

      expect(engine.readMessages).toHaveBeenCalledWith("t1", undefined, undefined, undefined);
    },
  );

  it.each([[{}], [{ threadId: "" }], [{ threadId: ["t1"] }]])(
    "answers 400 for the query %j (Bug 84408)",
    async (query) => {
      const reply = await invoke(threadsController.readMessages, { method: "GET", query });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "threadId required" });
      expect(engine.readMessages).not.toHaveBeenCalled();
      expect(published("aiThreadsReadMessages", reply)).toEqual([]);
    },
  );
});

describe("threadsController.getById and getMessageById", () => {
  it("answers the thread", async () => {
    engine.getById.mockResolvedValue(thread);

    const reply = await invoke(threadsController.getById, {
      method: "GET",
      query: { threadId: "t1" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(thread);
    expect(engine.getById).toHaveBeenCalledWith("t1");
    expect(published("aiThreadsGetById", reply)).toEqual([]);
  });

  it("answers 404 for a thread the store does not have (Bug 82718)", async () => {
    engine.getById.mockResolvedValue(null);

    const reply = await invoke(threadsController.getById, {
      method: "GET",
      query: { threadId: "t9" },
    });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "thread not found" });
    expect(published("aiThreadsGetById", reply)).toEqual([]);
  });

  it("answers the message", async () => {
    engine.getMessageById.mockResolvedValue(storedMessage);

    const reply = await invoke(threadsController.getMessageById, {
      method: "GET",
      query: { messageId: "m1" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(storedMessageJson);
    expect(engine.getMessageById).toHaveBeenCalledWith("m1");
    expect(published("aiThreadsGetMessageById", reply)).toEqual([]);
  });

  it("answers 404 for a message the store does not have (Bug 84411)", async () => {
    engine.getMessageById.mockResolvedValue(null);

    const reply = await invoke(threadsController.getMessageById, {
      method: "GET",
      query: { messageId: "m9" },
    });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "message not found" });
    expect(published("aiThreadsGetMessageById", reply)).toEqual([]);
  });

  it.each([
    ["getById", "threadId", "aiThreadsGetById"],
    ["getMessageById", "messageId", "aiThreadsGetMessageById"],
  ] as const)("%s answers 400 without an id", async (method, field, op) => {
    for (const query of [{}, { [field]: "" }, { [field]: ["a"] }]) {
      const reply = await invoke(threadsController[method], { method: "GET", query });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: `${field} required` });
      expect(published(op, reply)).toEqual([]);
    }
    expect(engine[method]).not.toHaveBeenCalled();
  });
});

describe("threadsController.updateMessage", () => {
  it.each([
    ["a named body", { messageId: "m1", message: userMessage }],
    ["a positional body", ["m1", userMessage]],
  ])("replaces the message from %s", async (_what, body) => {
    readMessage.mockResolvedValue(storedMessage);

    const reply = await invoke(threadsController.updateMessage, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(readMessage).toHaveBeenCalledWith("m1");
    expect(engine.updateMessage).toHaveBeenCalledWith("m1", userMessage);
    expect(published("aiThreadsUpdateMessage", reply)).toEqual([]);
  });

  it("rewrites an assistant message that cancels a tool call (Bug 84413)", async () => {
    readMessage.mockResolvedValue({ ...storedMessage, role: "assistant" });
    const message = { role: "assistant", content: [{ type: "tool-call", toolCallId: "c1" }] };

    const reply = await invoke(threadsController.updateMessage, { body: ["m1", message] });

    expect(reply.status).toBe(200);
    expect(engine.updateMessage).toHaveBeenCalledWith("m1", message);
  });

  it.each([
    ["null", null, "messageId required"],
    ["a string", "m1", "messageId required"],
    ["an empty messageId", ["", userMessage], "messageId required"],
    ["a messageId that is a number", [1, userMessage], "messageId required"],
    ["no message", ["m1"], "message is required and must be an object"],
    [
      "an empty content (Bug 84412)",
      ["m1", { role: "user", content: "" }],
      "message.content must be a non-empty string or a list with at least one non-empty part",
    ],
    [
      "an unknown role",
      ["m1", { role: "bot", content: "x" }],
      "message.role must be one of user, assistant, system",
    ],
  ])("answers 400 for a body with %s, before reading the message", async (_what, body, error) => {
    const reply = await invoke(threadsController.updateMessage, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(readMessage).not.toHaveBeenCalled();
    expect(published("aiThreadsUpdateMessage", reply)).toEqual([]);
  });

  it("answers 400 when the role differs from the stored one", async () => {
    readMessage.mockResolvedValue(storedMessage);

    const reply = await invoke(threadsController.updateMessage, {
      body: ["m1", { role: "assistant", content: "x" }],
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "the role of a stored message cannot be changed" });
    expect(engine.updateMessage).not.toHaveBeenCalled();
    expect(published("aiThreadsUpdateMessage", reply)).toEqual([]);
  });

  it("answers 404 for a message the store does not have", async () => {
    readMessage.mockResolvedValue(null);

    const reply = await invoke(threadsController.updateMessage, { body: ["m9", userMessage] });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "message not found" });
    expect(published("aiThreadsUpdateMessage", reply)).toEqual([]);
  });

  it.each([
    ["a 500", new AiServiceHttpError(500, "Internal Server Error", "", "u")],
    ["a timeout", new Error("upstream timeout")],
  ])("answers 500, not 404, when the message read fails with %s", async (_what, failure) => {
    readMessage.mockRejectedValue(failure);

    const reply = await invoke(threadsController.updateMessage, { body: ["m1", userMessage] });

    expect(reply.status).toBe(500);
    expect(engine.updateMessage).not.toHaveBeenCalled();
  });
});
