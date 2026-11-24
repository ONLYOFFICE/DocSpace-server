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

// The request bodies of the seven chat routes: which ones are refused, with what status and
// body, and which ones reach the engine as they came.

import { beforeEach, describe, expect, it, vi } from "vitest";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";
import * as h from "./aiController.harness.js";

// The boundary of the controller: the engine class, the storage, the chat-context aggregate,
// the AI-project binding, the Files reads of the context folders and of the source, the custom
// MCP servers, the tools prompt of the AI service and the thread gate. The request context,
// the HTTP client, the narrowing helpers, the stored-profile check and the telemetry are real.
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  AIEngine: (await import("./aiController.harness.js")).FakeAIEngine,
}));
vi.mock("../../storage/index.js", async () => ({
  storage: (await import("./aiController.harness.js")).storage,
}));
vi.mock("../../storage/chatContext.js", async () => {
  const h = await import("./aiController.harness.js");
  return {
    primeChatContext: h.primeChatContext,
    describeChatContextUsage: h.describeChatContextUsage,
  };
});
vi.mock("../../storage/aiProjectChatContext.js", async () => ({
  applyAiProjectChatContext: (await import("./aiController.harness.js")).applyAiProjectChatContext,
}));
vi.mock("../../storage/contextFolders.js", async (original) => {
  const h = await import("./aiController.harness.js");
  return {
    ...(await original<typeof import("../../storage/contextFolders.js")>()),
    resolveContextFolders: h.resolveContextFolders,
    resolveLocation: h.resolveLocation,
  };
});
vi.mock("../../storage/docspaceFilesApi.js", async (original) => ({
  ...(await original<typeof import("../../storage/docspaceFilesApi.js")>()),
  primeSourceMeta: (await import("./aiController.harness.js")).primeSourceMeta,
}));
vi.mock("../../tools/customTools.js", async () => ({
  customToolsSource: { source: "custom" },
  primeCustomServers: (await import("./aiController.harness.js")).primeCustomServers,
}));
vi.mock("../../tools/httpToolsAdapter.js", async (original) => ({
  ...(await original<typeof import("../../tools/httpToolsAdapter.js")>()),
  safeGetToolsPrompt: (await import("./aiController.harness.js")).safeGetToolsPrompt,
}));
vi.mock("../../tools/systemTools.js", async () => ({
  systemToolsSource: { getServerTypes: (await import("./aiController.harness.js")).getServerTypes },
}));
vi.mock("../../tools/formAnalysisSubAgent.js", () => ({
  formAnalysisSubAgent: { source: "form" },
}));
vi.mock("../../tools/documentAgent.js", () => ({ documentAgentTools: { source: "agent" } }));
vi.mock("../threadsController.js", async () => ({
  assertThreadCreatable: (await import("./aiController.harness.js")).assertThreadCreatable,
}));

const { aiController } = await import("../aiController.js");

const upstream = useUpstream();

const text = (value: string): unknown => ({
  role: "user",
  content: [{ type: "text", text: value }],
});
const parts = (...content: unknown[]): unknown => ({ role: "user", content });
const files = (count: number): unknown[] =>
  Array.from({ length: count }, (_, i) => ({ type: "file", data: "", mimeType: `f${i}` }));
const images = (count: number): unknown[] =>
  Array.from({ length: count }, (_, i) => ({ type: "image", image: `data:,${i}` }));

const EMPTY_MESSAGE = "userMessage must contain non-empty text content or an attachment";
const NOT_AN_OBJECT = "the request body must be an object";

// What `hasPromptContent` refuses, for every route that checks the user message.
const EMPTY_USER_MESSAGES: [string, unknown][] = [
  ["missing", undefined],
  ["null", null],
  ["a string", "hello"],
  ["an array", [{ type: "text", text: "hi" }]],
  ["an object without content", { role: "user" }],
  ["content that is an empty string", { role: "user", content: "" }],
  ["content that is whitespace", { role: "user", content: " \n\t " }],
  ["content that is a number", { role: "user", content: 5 }],
  ["content that is an object", { role: "user", content: { type: "text", text: "hi" } }],
  ["content that is an empty list", { role: "user", content: [] }],
  ["only blank string parts", { role: "user", content: ["", "  "] }],
  ["only a blank text part", { role: "user", content: [{ type: "text", text: "  " }] }],
  ["a text part whose text is a number", { role: "user", content: [{ type: "text", text: 5 }] }],
  ["a text part without text", { role: "user", content: [{ type: "text" }] }],
  ["parts that are null or numbers", { role: "user", content: [null, 7, true] }],
  ["a part of another kind", { role: "user", content: [{ type: "tool-call", toolName: "x" }] }],
];

// What it accepts.
const PROMPTS: [string, unknown][] = [
  ["a string content", { role: "user", content: "hi" }],
  ["a bare string part", { role: "user", content: ["  hi  "] }],
  ["a text part after blank ones", parts(null, "  ", { type: "text", text: " hi " })],
  ["a file part alone", parts({ type: "file", data: "", mimeType: "{}" })],
  ["an image part alone", parts({ type: "image", image: "data:," })],
  ["five files and five images", parts(...files(5), ...images(5))],
];

const OVER_LIMIT: [string, unknown, string][] = [
  [
    "six files",
    parts({ type: "text", text: "see" }, ...files(6)),
    "a message may carry at most 5 file and 5 image attachments (got 6 file(s), 0 image(s))",
  ],
  [
    "six images",
    parts({ type: "text", text: "see" }, ...images(6), "junk", null),
    "a message may carry at most 5 file and 5 image attachments (got 0 file(s), 6 image(s))",
  ],
];

beforeEach(() => {
  vi.clearAllMocks();
  h.resetHarness();
  h.engine.send.mockResolvedValue({ role: "assistant", content: "ok" } as never);
  h.engine.sendCustom.mockResolvedValue({ role: "assistant", content: "ok" } as never);
  h.engine.sendWithStream.mockImplementation(() => h.streamOf() as never);
  h.engine.sendWithStreamOpenAI.mockImplementation(() => h.streamOf() as never);
  h.engine.regenerateStream.mockImplementation(() => h.streamOf() as never);
  h.engine.approveToolCall.mockImplementation(() => h.streamOf() as never);
  h.engine.denyToolCall.mockImplementation(() => h.streamOf() as never);
});

describe("send", () => {
  const send = (body: unknown) => h.call(aiController.send, body);
  const valid = { actionType: "Chat", userMessage: text("hi") };

  it.each([
    ["null", null],
    ["a string", "hello"],
    ["a number", 42],
    ["an array", [valid]],
  ])("answers 400 for a body that is %s and primes nothing", async (_what, body) => {
    const reply = await send(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: NOT_AN_OBJECT });
    expect(responseViolations("aiSend", 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
    expect(h.engine.send).not.toHaveBeenCalled();
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["a number", 1],
    ["empty", ""],
    ["an unknown name", "Poem"],
    ["a known name in another case", "chat"],
    ["an array", ["Chat"]],
  ])("answers 400 when actionType is %s", async (_what, actionType) => {
    const reply = await send({ ...valid, actionType });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: "actionType is required and must be a known action type",
    });
    expect(responseViolations("aiSend", 400, reply.body)).toEqual([]);
    expect(h.engine.send).not.toHaveBeenCalled();
  });

  it("answers 400 for an empty object, naming actionType first", async () => {
    const reply = await send({});

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: "actionType is required and must be a known action type",
    });
  });

  it.each(EMPTY_USER_MESSAGES)("answers 400 when userMessage is %s", async (_what, userMessage) => {
    const reply = await send({ actionType: "Chat", userMessage });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: EMPTY_MESSAGE });
    expect(responseViolations("aiSend", 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
    expect(h.engine.send).not.toHaveBeenCalled();
  });

  it.each(OVER_LIMIT)("answers 400 for %s in one message", async (_what, userMessage, error) => {
    const reply = await send({ actionType: "Chat", userMessage });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSend", 400, reply.body)).toEqual([]);
    expect(h.engine.send).not.toHaveBeenCalled();
  });

  it.each(PROMPTS)("runs a message with %s", async (_what, userMessage) => {
    const reply = await send({ actionType: "Translation", userMessage });

    expect(reply.status).toBe(200);
    expect(h.engine.send).toHaveBeenCalledTimes(1);
  });

  it.each([
    ["entityId", 5],
    ["entityId", true],
    ["contextEntityId", { id: 1 }],
    ["contextEntityId", ["8"]],
  ])("answers 400 naming %s when it is %j, after the aggregate read", async (field, value) => {
    const reply = await send({ ...valid, [field]: value });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: `${field} must be a string` });
    expect(responseViolations("aiSend", 400, reply.body)).toEqual([]);
    // The aggregate read is the route's access gate; the id of a wrong type is not sent with it.
    expect(h.primeChatContext).toHaveBeenCalledWith({
      entityId: undefined,
      contextEntityId: undefined,
    });
    expect(h.engine.send).not.toHaveBeenCalled();
  });

  it("answers the 403 of the aggregate read, not 400, for an id of a wrong type", async () => {
    h.primeChatContext.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await send({ ...valid, entityId: 5 });

    expect(reply.status).toBe(403);
    expect(h.engine.send).not.toHaveBeenCalled();
  });

  it.each([
    ["null", null],
    ["a string", "12"],
  ])("runs a message whose entityId is %s", async (_what, entityId) => {
    const reply = await send({ ...valid, entityId });

    expect(reply.status).toBe(200);
    expect(h.primeChatContext).toHaveBeenCalledWith({
      entityId: entityId ?? undefined,
      contextEntityId: undefined,
    });
  });
});

describe("sendCustom", () => {
  const sendCustom = (body: unknown) => h.call(aiController.sendCustom, body);
  const valid = { systemPrompt: "be brief", userMessage: text("hi") };

  it.each([
    ["null", null],
    ["a string", "hello"],
    ["a number", 42],
    ["an array", [valid]],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await sendCustom(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: NOT_AN_OBJECT });
    expect(responseViolations("aiSendCustom", 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["a number", 3],
    ["empty", ""],
    ["whitespace", "  \n"],
    ["an object", { text: "x" }],
  ])("answers 400 when systemPrompt is %s (Bug 84299)", async (_what, systemPrompt) => {
    const reply = await sendCustom({ ...valid, systemPrompt });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      error: "systemPrompt is required and must be a non-empty string",
    });
    expect(responseViolations("aiSendCustom", 400, reply.body)).toEqual([]);
    expect(h.engine.sendCustom).not.toHaveBeenCalled();
  });

  it("answers 400 for an empty object, naming systemPrompt first", async () => {
    const reply = await sendCustom({});

    expect(reply.body).toEqual({
      error: "systemPrompt is required and must be a non-empty string",
    });
  });

  it.each(EMPTY_USER_MESSAGES)("answers 400 when userMessage is %s", async (_what, userMessage) => {
    const reply = await sendCustom({ systemPrompt: "s", userMessage });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: EMPTY_MESSAGE });
    expect(responseViolations("aiSendCustom", 400, reply.body)).toEqual([]);
    expect(h.engine.sendCustom).not.toHaveBeenCalled();
  });

  it.each(OVER_LIMIT)("answers 400 for %s in one message", async (_what, userMessage, error) => {
    const reply = await sendCustom({ systemPrompt: "s", userMessage });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSendCustom", 400, reply.body)).toEqual([]);
  });

  it.each([
    ["the string false", "false"],
    ["a number", 0],
    ["an object", {}],
    ["an array", [true]],
  ])("answers 400 when isStream is %s (Bug 84300)", async (_what, isStream) => {
    const reply = await sendCustom({ ...valid, isStream });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "isStream must be a boolean" });
    expect(responseViolations("aiSendCustom", 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["false", false],
  ])("answers the one-shot message when isStream is %s", async (_what, isStream) => {
    const reply = await sendCustom({ ...valid, isStream });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ role: "assistant", content: "ok" });
  });

  it.each(PROMPTS)("runs a message with %s", async (_what, userMessage) => {
    const reply = await sendCustom({ systemPrompt: "s", userMessage });

    expect(reply.status).toBe(200);
  });

  it.each([
    ["empty", ""],
    ["null", null],
  ])("skips the profile check when profileId is %s", async (_what, profileId) => {
    const reply = await sendCustom({ ...valid, profileId });

    expect(reply.status).toBe(200);
    expect(h.storage.profiles.readById).not.toHaveBeenCalled();
  });

  it.each([
    ["a number", 7],
    ["an object", { id: "p1" }],
  ])(
    "answers 400 when profileId is %s, after the aggregate read and before the profile read",
    async (_what, profileId) => {
      const reply = await sendCustom({ ...valid, profileId });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "profileId must be a string" });
      expect(responseViolations("aiSendCustom", 400, reply.body)).toEqual([]);
      expect(h.primeChatContext).toHaveBeenCalledTimes(1);
      expect(h.storage.profiles.readById).not.toHaveBeenCalled();
      expect(h.engine.sendCustom).not.toHaveBeenCalled();
    },
  );

  it("answers the 403 of the aggregate read, not 400, for a profileId of a wrong type", async () => {
    h.primeChatContext.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await sendCustom({ ...valid, profileId: 7 });

    expect(reply.status).toBe(403);
    expect(h.engine.sendCustom).not.toHaveBeenCalled();
  });
});

// The two routes that store the user message share the pre-flight.
describe.each([
  ["sendWithStream", "aiSendWithStream"],
  ["sendWithStreamOpenAI", "aiSendWithStreamOpenAI"],
] as const)("%s", (name, operationId) => {
  const route = (body: unknown) => h.call(aiController[name], body);

  it("answers 400 for a null body, before anything is read", async () => {
    const reply = await route(null);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "the request body must be an object" });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
  });

  it.each([
    ["a string", "hello"],
    ["an array", [{ userMessage: text("hi") }]],
    ["an empty object", {}],
  ])("answers 400 for a body that is %s, after the round was primed", async (_what, body) => {
    const reply = await route(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: EMPTY_MESSAGE });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).toHaveBeenCalledWith({
      threadId: undefined,
      entityId: undefined,
      contextEntityId: undefined,
    });
    expect(h.primeCustomServers).toHaveBeenCalledTimes(1);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it.each(EMPTY_USER_MESSAGES)("answers 400 when userMessage is %s", async (_what, userMessage) => {
    const reply = await route({ threadId: "t1", userMessage });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: EMPTY_MESSAGE });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(h.assertThreadCreatable).not.toHaveBeenCalled();
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it.each(OVER_LIMIT)("answers 400 for %s in one message (Bug 82894)", async (_w, msg, error) => {
    const reply = await route({ threadId: "t1", userMessage: msg });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it.each(PROMPTS)("runs a message with %s", async (_what, userMessage) => {
    const reply = await route({ threadId: "t1", userMessage });

    expect(reply.status).toBe(200);
    expect(h.engine[name]).toHaveBeenCalledTimes(1);
  });

  it.each([
    ["empty", ""],
    ["null", null],
  ])("treats a threadId that is %s as a new thread and gates it", async (_what, threadId) => {
    await route({ threadId, entityId: "12", userMessage: text("hi") });

    expect(h.assertThreadCreatable).toHaveBeenCalledWith("12", undefined);
    expect(h.storage.threads.readById).not.toHaveBeenCalled();
  });

  it.each([
    ["threadId", 9],
    ["entityId", 12],
    ["contextEntityId", false],
    ["profileId", { id: "p1" }],
  ])("answers 400 naming %s when it is %j, after the aggregate read", async (field, value) => {
    const reply = await route({ threadId: "t1", userMessage: text("hi"), [field]: value });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: `${field} must be a string` });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    // The aggregate read is the route's access gate; the id of a wrong type is not sent with it.
    expect(h.primeChatContext).toHaveBeenCalledWith({
      threadId: field === "threadId" ? undefined : "t1",
      entityId: undefined,
      contextEntityId: undefined,
    });
    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(h.storage.profiles.readById).not.toHaveBeenCalled();
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("answers the 403 of the aggregate read before a word about an id of a wrong type", async () => {
    h.primeChatContext.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await route({ threadId: 9, userMessage: text("hi") });

    expect(reply.status).toBe(403);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("answers 400 for a profileId the store does not hold", async () => {
    h.storage.profiles.readById.mockResolvedValue(undefined);

    const reply = await route({ threadId: "t1", profileId: "gone", userMessage: text("hi") });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "unknown profileId: gone" });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });
});

// These three hand the body to the engine without a check of their own.
describe.each([
  ["regenerateStream", "aiRegenerateStream"],
  ["approveToolCall", "aiApproveToolCall"],
  ["denyToolCall", "aiDenyToolCall"],
] as const)("%s", (name, operationId) => {
  const route = (body: unknown) => h.call(aiController[name], body);

  it("answers 400 for a null body, before anything is read", async () => {
    const reply = await route(null);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "the request body must be an object" });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it.each([
    ["threadId", 9],
    ["entityId", 12],
    ["contextEntityId", false],
    ["profileId", { id: "p1" }],
    ["profileId", ["p1"]],
  ])("answers 400 naming %s when it is %j, after the aggregate read", async (field, value) => {
    const reply = await route({ threadId: "t1", messageId: "m1", [field]: value });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: `${field} must be a string` });
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
    // The aggregate read is the route's access gate; the id of a wrong type is not sent with it.
    expect(h.primeChatContext).toHaveBeenCalledWith({
      threadId: field === "threadId" ? undefined : "t1",
      entityId: undefined,
      contextEntityId: undefined,
    });
    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("answers the 403 of the aggregate read before a word about an id of a wrong type", async () => {
    h.primeChatContext.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "u"));

    const reply = await route({ threadId: 9 });

    expect(reply.status).toBe(403);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  // HANDLED DOWNSTREAM ai-3: the route checks the types of the ids and nothing else of the body (a missing threadId, messageId, toolCallId) and runs the engine on it. In production the AI service refuses the missing ids (a Guid route parameter, 400) and the engine a tool call that is not pending (409); it shows only with the engine or the service stubbed.
  it.each([
    ["a string", "hello"],
    ["an array", ["t1", "m1"]],
  ])("hands a body that is %s to the engine as it came", async (_what, body) => {
    const reply = await route(body);

    expect(reply.status).toBe(200);
    expect(h.engine[name]).toHaveBeenCalledWith(body);
    expect(upstream.seen).toEqual([]);
  });

  // HANDLED DOWNSTREAM ai-3: the route checks the types of the ids and nothing else of the body (a missing threadId, messageId, toolCallId) and runs the engine on it. In production the AI service refuses the missing ids (a Guid route parameter, 400) and the engine a tool call that is not pending (409); it shows only with the engine or the service stubbed.
  it("hands an empty object to the engine with only the round's own additions", async () => {
    const reply = await route({});

    expect(reply.status).toBe(200);
    const input = h.engineInput(name);
    expect(Object.keys(input)).toEqual(["actionArgs"]);
    expect(Object.keys(input["actionArgs"] as object)).toEqual(
      name === "regenerateStream" ? ["signal", "prompt"] : ["signal"],
    );
  });
});

// The 402 of the unpaid gateway reads /config only outside a primed round.
// `allowAlways` stores a standing permission, and the engine reads it by truthiness.
describe("approveToolCall, allowAlways", () => {
  const approve = (body: unknown) => h.call(aiController.approveToolCall, body);

  it.each([
    ["the string false", "false"],
    ["the string true", "true"],
    ["a number", 1],
    ["an object", {}],
  ])("answers 400 when allowAlways is %s and runs nothing", async (_what, allowAlways) => {
    const reply = await approve({ threadId: "t1", messageId: "m1", idx: 0, allowAlways });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "allowAlways must be a boolean" });
    expect(responseViolations("aiApproveToolCall", 400, reply.body)).toEqual([]);
    expect(h.primeChatContext).toHaveBeenCalledTimes(1);
    expect(h.engine.approveToolCall).not.toHaveBeenCalled();
  });

  it.each([[true], [false], [null], [undefined]])(
    "runs the approval when allowAlways is %j",
    async (allowAlways) => {
      const reply = await approve({ threadId: "t1", messageId: "m1", idx: 0, allowAlways });

      expect(reply.status).toBe(200);
      expect(h.engine.approveToolCall).toHaveBeenCalledTimes(1);
    },
  );
});

describe("the published error answers", () => {
  it("lists the 402 of every route that sends it", async () => {
    h.storage.profiles.readById.mockResolvedValue({ id: "p1", providerType: "onlyoffice" });
    upstream.use(
      http.get("*/internal/ai/config", () =>
        HttpResponse.json({ aiReady: false }, { status: 200 }),
      ),
    );

    const custom = await h.call(aiController.sendCustom, {
      systemPrompt: "s",
      userMessage: text("hi"),
      profileId: "p1",
    });
    const stream = await h.call(aiController.sendWithStream, {
      threadId: "t1",
      profileId: "p1",
      userMessage: text("hi"),
    });
    const openai = await h.call(aiController.sendWithStreamOpenAI, {
      threadId: "t1",
      profileId: "p1",
      userMessage: text("hi"),
    });

    expect([custom.status, stream.status, openai.status]).toEqual([402, 402, 402]);
    expect(responseViolations("aiSendCustom", 402, custom.body)).toEqual([]);
    expect(responseViolations("aiSendWithStream", 402, stream.body)).toEqual([]);
    expect(responseViolations("aiSendWithStreamOpenAI", 402, openai.body)).toEqual([]);
  });
});

// ts-ai-failure-paths.md §3: the access gate comes before the body. A body of JSON `null` is the
// one these rounds refuse before their own gated read, so the gate is asked first: with the AI
// service refusing the caller (a guest, or AI switched off), the answer is 403, not 400.
describe.each([
  ["sendWithStream", "aiSendWithStream"],
  ["sendWithStreamOpenAI", "aiSendWithStreamOpenAI"],
  ["regenerateStream", "aiRegenerateStream"],
  ["approveToolCall", "aiApproveToolCall"],
  ["denyToolCall", "aiDenyToolCall"],
] as const)("%s behind the access gate", (name, operationId) => {
  it("answers 403 for a null body when the caller has no AI access", async () => {
    h.storage.toolPrefs.readDisabled.mockRejectedValueOnce(
      new AiServiceHttpError(403, "Forbidden", "", "http://ai/tool-prefs"),
    );

    const reply = await h.call(aiController[name], null);

    expect(reply.status).toBe(403);
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
  });

  // The aggregate read is the gate only when it installs a snapshot; switched off, failed or
  // unusable, it made no gated call, and the round asks the gate before any refusal of the body.
  it("answers 403 for an id of a wrong type when the aggregate read installed no snapshot", async () => {
    h.primeChatContext.mockResolvedValue(false);
    h.storage.toolPrefs.readDisabled.mockRejectedValueOnce(
      new AiServiceHttpError(403, "Forbidden", "", "http://ai/tool-prefs"),
    );

    const reply = await h.call(aiController[name], { threadId: "t1", entityId: 12 });

    expect(reply.status).toBe(403);
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("asks no second gate when the aggregate read installed a snapshot", async () => {
    h.primeChatContext.mockResolvedValue(true);

    const reply = await h.call(aiController[name], { threadId: "t1", entityId: 12 });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
    expect(h.storage.toolPrefs.readDisabled).not.toHaveBeenCalled();
  });
});

// The two plain sends check their body before anything is primed, so the gate is asked first.
describe.each([
  ["send", "aiSend"],
  ["sendCustom", "aiSendCustom"],
] as const)("%s behind the access gate", (name, operationId) => {
  it.each([
    ["null", null],
    ["an empty object", {}],
  ])("answers 403 for a body that is %s when the caller has no AI access", async (_what, body) => {
    h.storage.toolPrefs.readDisabled.mockRejectedValueOnce(
      new AiServiceHttpError(403, "Forbidden", "", "http://ai/tool-prefs"),
    );

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(403);
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
    expect(h.primeChatContext).not.toHaveBeenCalled();
    expect(h.engine[name]).not.toHaveBeenCalled();
  });
});
