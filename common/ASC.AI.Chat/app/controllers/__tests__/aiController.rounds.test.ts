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

// What a chat round does around the engine: the prologue every route runs, the pre-flight of
// the rounds that store a message, the prompt fragments, the abort signal, the unpaid gateway,
// the stream framing and the log lines of a stream.

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import logger from "../../log.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import {
  getKnowledgeRoomIds,
  getRoundAttachmentIds,
  getRoundProfileId,
  getRoundThreadId,
  shouldForwardHeadersToProvider,
} from "../../requestContext.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";
import { DocspaceApiHttpError } from "../../storage/docspaceFilesApi.js";
import { buildContextFoldersLines, buildLocationLines } from "../../storage/contextFolders.js";
import * as h from "./aiController.harness.js";
import { ndjsonFrames, schemaViolations, sseFrames } from "../../__tests__/helpers/frames.js";

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
const attachment = (ref: string): unknown => ({
  type: "file",
  data: "",
  mimeType: JSON.stringify({ ref, title: `${ref}.docx`, kind: "file" }),
});

// The events of an ordinary reply, as the engine streams them.
const START = {
  type: "message-start",
  threadId: "t1",
  messageId: "m2",
  message: { id: "m2", role: "assistant", content: [] },
};
const DELTA = {
  type: "message-delta",
  threadId: "t1",
  messageId: "m2",
  message: { id: "m2", role: "assistant", content: [{ type: "text", text: "Hel" }] },
};
const END = {
  type: "message-end",
  threadId: "t1",
  messageId: "m2",
  message: { id: "m2", role: "assistant", content: [{ type: "text", text: "Hello" }] },
};

// OpenAI `chat.completion.chunk` objects as `sendWithStreamOpenAI` emits them.
const CHUNK = {
  id: "chatcmpl-1",
  object: "chat.completion.chunk",
  created: 1_700_000_000,
  model: "gpt-test",
  choices: [{ index: 0, delta: { role: "assistant", content: "Hi" }, finish_reason: null }],
};
const LAST_CHUNK = { ...CHUNK, choices: [{ index: 0, delta: {}, finish_reason: "stop" }] };

const HEAD = [
  "Context:",
  "- You are an AI agent operating inside an ONLYOFFICE Apps workspace, not a generic standalone assistant.",
];
const TAIL = [
  "- Ids of folders, rooms and files are for tool calls only and mean nothing to the user: in your replies, refer to them by their names.",
  "- Today's date is 2026-11-23.",
  "- Prefer answering from the conversation when you can; use tools only when the request clearly needs data or actions in the workspace.",
  "- When you link to a workspace file, use only a link a tool returned for it (its `url` or `webUrl`), copied verbatim. Never compose a link yourself from a file id or title; if no tool returned a link, name the file without one.",
];
const CONTEXT = [...HEAD, ...TAIL].join("\n");

const UNPAID = "The AI Tools service is not paid for the current portal";

type Route =
  | "send"
  | "sendCustom"
  | "sendWithStream"
  | "sendWithStreamOpenAI"
  | "regenerateStream"
  | "approveToolCall"
  | "denyToolCall";

const OPERATION: Record<Route, string> = {
  send: "aiSend",
  sendCustom: "aiSendCustom",
  sendWithStream: "aiSendWithStream",
  sendWithStreamOpenAI: "aiSendWithStreamOpenAI",
  regenerateStream: "aiRegenerateStream",
  approveToolCall: "aiApproveToolCall",
  denyToolCall: "aiDenyToolCall",
};

const signalOf = (input: Record<string, unknown>): AbortSignal =>
  (input["actionArgs"] as { signal: AbortSignal }).signal;

const promptOf = (input: Record<string, unknown>): unknown =>
  (input["actionArgs"] as { prompt?: unknown }).prompt;

// Lets the stream run up to its next wait.
const settle = async (): Promise<void> => {
  for (let i = 0; i < 10; i++) {
    await new Promise<void>((resolve) => setImmediate(resolve));
  }
};

beforeEach(() => {
  vi.clearAllMocks();
  h.resetHarness();
  vi.useFakeTimers({ toFake: ["Date"] });
  vi.setSystemTime(new Date("2026-11-23T10:00:00Z"));
  h.engine.send.mockResolvedValue({ role: "assistant", content: "ok" } as never);
  h.engine.sendCustom.mockResolvedValue({ role: "assistant", content: "ok" } as never);
  h.engine.sendWithStream.mockImplementation(() => h.streamOf(START, END) as never);
  h.engine.sendWithStreamOpenAI.mockImplementation(() => h.streamOf(CHUNK, LAST_CHUNK) as never);
  h.engine.regenerateStream.mockImplementation(() => h.streamOf(START, END) as never);
  h.engine.approveToolCall.mockImplementation(() => h.streamOf(START, END) as never);
  h.engine.denyToolCall.mockImplementation(() => h.streamOf(END) as never);
});

afterEach(() => {
  vi.useRealTimers();
});

// A body each route runs with, nothing refused.
const VALID: Record<Route, Record<string, unknown>> = {
  send: { actionType: "Chat", userMessage: text("hi"), entityId: "7", contextEntityId: "8" },
  sendCustom: { systemPrompt: "s", userMessage: text("hi") },
  sendWithStream: { threadId: "t1", entityId: "7", contextEntityId: "8", userMessage: text("hi") },
  sendWithStreamOpenAI: {
    threadId: "t1",
    entityId: "7",
    contextEntityId: "8",
    userMessage: text("hi"),
  },
  regenerateStream: { threadId: "t1", entityId: "7", contextEntityId: "8", messageId: "m1" },
  approveToolCall: { threadId: "t1", entityId: "7", contextEntityId: "8", messageId: "m1", idx: 0 },
  denyToolCall: { threadId: "t1", entityId: "7", contextEntityId: "8", messageId: "m1", idx: 0 },
};

describe("the prologue of every round", () => {
  const ROUNDS = [
    "sendWithStream",
    "sendWithStreamOpenAI",
    "regenerateStream",
    "approveToolCall",
    "denyToolCall",
  ] as const;

  it.each(ROUNDS)("%s primes the chat context with the three ids of the body", async (name) => {
    await h.call(aiController[name], VALID[name]);

    expect(h.primeChatContext).toHaveBeenCalledExactlyOnceWith({
      threadId: "t1",
      entityId: "7",
      contextEntityId: "8",
    });
    expect(h.applyAiProjectChatContext).toHaveBeenCalledTimes(1);
    expect(h.primeCustomServers).toHaveBeenCalledTimes(1);
  });

  it("send primes the chat context with the entity ids only", async () => {
    await h.call(aiController.send, VALID.send);

    expect(h.primeChatContext).toHaveBeenCalledExactlyOnceWith({
      entityId: "7",
      contextEntityId: "8",
    });
  });

  it.each([...ROUNDS, "send"] as const)(
    "%s marks the caller's headers for the provider before the engine runs",
    async (name) => {
      let marked: boolean | undefined;
      const method = name;
      (
        h.engine[method] as unknown as { mockImplementation: (f: () => unknown) => void }
      ).mockImplementation(() => {
        marked = shouldForwardHeadersToProvider();
        return method === "send"
          ? Promise.resolve({ role: "assistant", content: "ok" })
          : h.streamOf(END);
      });

      await h.call(aiController[name], VALID[name]);

      expect(marked).toBe(true);
    },
  );

  it.each([...ROUNDS, "send"] as const)(
    "%s answers the refusal of an AI project and goes no further",
    async (name) => {
      h.applyAiProjectChatContext.mockResolvedValue({
        status: 403,
        body: { error: "You cannot chat in this AI project" },
      });

      const reply = await h.call(aiController[name], VALID[name]);

      expect(reply.status).toBe(403);
      expect(reply.body).toEqual({ error: "You cannot chat in this AI project" });
      expect(responseViolations(OPERATION[name], 403, reply.body)).toEqual([]);
      expect(h.primeCustomServers).not.toHaveBeenCalled();
      expect(h.engine[name]).not.toHaveBeenCalled();
    },
  );

  it.each([...ROUNDS, "send"] as const)(
    "%s answers 409 with the code when the project's model is gone",
    async (name) => {
      const refusal = {
        error: "The model of this AI project is not available",
        code: "projectModelUnavailable",
      };
      h.applyAiProjectChatContext.mockResolvedValue({ status: 409, body: refusal });

      const reply = await h.call(aiController[name], VALID[name]);

      expect(reply.status).toBe(409);
      expect(reply.body).toEqual(refusal);
      expect(responseViolations(OPERATION[name], 409, reply.body)).toEqual([]);
      expect(h.engine[name]).not.toHaveBeenCalled();
    },
  );

  it.each([...ROUNDS, "send", "sendCustom"] as const)(
    "%s relays a Files API refusal of the chat context with its status and reason only",
    async (name) => {
      h.primeChatContext.mockRejectedValue(
        new DocspaceApiHttpError(403, "Forbidden", "http://proxy.internal/api/2.0/files/8"),
      );

      const reply = await h.call(aiController[name], VALID[name]);

      expect(reply.status).toBe(403);
      expect(reply.body).toEqual({ error: "Forbidden" });
      expect(responseViolations(OPERATION[name], 403, reply.body)).toEqual([]);
      expect(h.applyAiProjectChatContext).not.toHaveBeenCalled();
    },
  );

  it.each([...ROUNDS, "send", "sendCustom"] as const)(
    "%s answers an AI service outage of the chat context with its status",
    async (name) => {
      h.primeChatContext.mockRejectedValue(
        new AiServiceHttpError(503, "Service Unavailable", "secret body", "http://ai.internal/x"),
      );

      const reply = await h.call(aiController[name], VALID[name]);

      expect(reply.status).toBe(503);
      expect(reply.body).toEqual({ error: "Service Unavailable" });
      expect(responseViolations(OPERATION[name], 503, reply.body)).toEqual([]);
    },
  );
});

describe("the abort signal handed to the engine", () => {
  it("is aborted when the client closes a stream before its end", async () => {
    const held = h.heldStream([START]);
    h.engine.sendWithStream.mockReturnValue(held.stream as never);

    const started = h.start(aiController.sendWithStream, VALID.sendWithStream);
    await settle();
    const signal = signalOf(h.engineInput("sendWithStream"));
    expect(signal.aborted).toBe(false);

    started.res.emit("close");
    expect(signal.aborted).toBe(true);
    held.release();
    const reply = await started.done;

    expect(reply.ended).toBe(true);
    expect(ndjsonFrames(reply.body)).toEqual([START]);
  });

  // HANDLED DOWNSTREAM ai-6: after the close the stream helper still pulls the generator to its end and writes into the closed response; only an engine that honours the signal stops. In production the real engine honours the abort signal and stops (ASC.AI.Tests, MessageStopTests); it shows only with the engine or the service stubbed.
  it("keeps pulling the engine's stream after the client went away", async () => {
    const held = h.heldStream([START], [DELTA, END]);
    h.engine.sendWithStream.mockReturnValue(held.stream as never);

    const started = h.start(aiController.sendWithStream, VALID.sendWithStream);
    await settle();
    started.res.emit("close");
    held.release();
    const reply = await started.done;

    expect(held.pulls()).toBe(4);
    expect(ndjsonFrames(reply.body)).toEqual([START, DELTA, END]);
  });

  it("is not aborted by the close that follows a complete answer", async () => {
    const started = h.start(aiController.sendWithStream, VALID.sendWithStream);
    await started.done;

    started.res.emit("close");

    expect(signalOf(h.engineInput("sendWithStream")).aborted).toBe(false);
  });

  it("is added when the body carries no actionArgs and replaces one that is not an object", async () => {
    await h.call(aiController.send, { ...VALID.send, actionArgs: "junk" });

    const input = h.engineInput("send");
    expect(Object.keys(input["actionArgs"] as object)).toEqual(["signal"]);
    expect(signalOf(input)).toBeInstanceOf(AbortSignal);
  });

  it("replaces the signal the client sent and keeps the other action arguments", async () => {
    await h.call(aiController.send, {
      ...VALID.send,
      actionArgs: { signal: {}, temperature: 0.2 },
    });

    const input = h.engineInput("send");
    expect(input["actionArgs"]).toEqual({ signal: expect.any(AbortSignal), temperature: 0.2 });
  });
});

describe("the knowledge rooms of a round", () => {
  const roomsOf = async (actionArgs: unknown): Promise<number[]> => {
    let rooms: number[] = [];
    h.engine.send.mockImplementation(async () => {
      rooms = getKnowledgeRoomIds();
      return { role: "assistant", content: "ok" } as never;
    });
    await h.call(aiController.send, { ...VALID.send, actionArgs });
    return rooms;
  };

  it("takes each positive integer id once, in the order sent", async () => {
    expect(
      await roomsOf({
        knowledgeRooms: [
          { id: "12" },
          { id: 5 },
          { id: "12" },
          { id: "x" },
          { id: 0 },
          { id: -3 },
          { id: 1.5 },
          { id: null },
          "junk",
          null,
          { id: "9abc" },
          { id: "1e3" },
        ],
      }),
    ).toEqual([12, 5]);
  });

  it("reads the single knowledgeRoom of an older widget", async () => {
    expect(await roomsOf({ knowledgeRoom: { id: "7" } })).toEqual([7]);
  });

  it("prefers the list to the single room", async () => {
    expect(await roomsOf({ knowledgeRooms: [{ id: 3 }], knowledgeRoom: { id: 7 } })).toEqual([3]);
  });

  it("sets none without action arguments", async () => {
    expect(await roomsOf(undefined)).toEqual([]);
  });
});

describe("send", () => {
  it("answers the engine's message and hands it the body with the signal", async () => {
    // The assistant message as the engine returns it (ThreadMessageLike of @onlyoffice/ai-chat).
    const message = {
      id: "22222222-2222-2222-2222-222222222222",
      role: "assistant",
      content: [{ type: "text", text: "Bonjour" }],
    };
    h.engine.send.mockResolvedValue(message as never);

    const reply = await h.call(aiController.send, VALID.send);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(message);
    expect(responseViolations("aiSend", 200, reply.body)).toEqual([]);
    expect(h.engineInput("send")).toEqual({
      ...VALID.send,
      actionArgs: { signal: expect.any(AbortSignal) },
    });
    expect(h.primeSourceMeta).toHaveBeenCalledExactlyOnceWith("8");
    expect(upstream.seen).toEqual([]);
  });

  it("answers 500 without the error's text when the engine throws", async () => {
    h.engine.send.mockRejectedValue(new Error("provider exploded at http://10.0.0.1"));

    const reply = await h.call(aiController.send, VALID.send);

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiSend", 500, reply.body)).toEqual([]);
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      expect.stringMatching(/^POST \/api\/2\.0\/ai\/ai\/route failed: Error: provider exploded/),
    );
  });
});

describe("the source a round is attributed to", () => {
  it.each([
    [
      "the first context folder",
      { actionArgs: { folderContexts: [{ id: 31 }, { id: 32 }] } },
      "31",
    ],
    ["the location", { contextEntityId: "8", entityId: "7" }, "8"],
    ["the project", { entityId: "7" }, "7"],
    ["an empty location falls to the project", { contextEntityId: "", entityId: "7" }, "7"],
    ["nothing", {}, undefined],
  ])("is %s", async (_what, ids, scope) => {
    await h.call(aiController.sendWithStream, { threadId: "t1", userMessage: text("hi"), ...ids });

    expect(h.primeSourceMeta).toHaveBeenCalledExactlyOnceWith(scope);
  });

  it.each([
    ["the top-level scope", { contextEntityId: "8", actionArgs: { entityId: "99" } }, "8"],
    ["actionArgs.entityId", { actionArgs: { entityId: "99" } }, "99"],
    ["nothing for a numeric actionArgs.entityId", { actionArgs: { entityId: 99 } }, undefined],
    ["nothing for actionArgs that are not an object", { actionArgs: ["99"] }, undefined],
    ["nothing without actionArgs", {}, undefined],
  ])("of sendCustom is %s", async (_what, ids, scope) => {
    await h.call(aiController.sendCustom, { systemPrompt: "s", userMessage: text("hi"), ...ids });

    expect(h.primeChatContext).toHaveBeenCalledExactlyOnceWith({ entityId: scope });
    expect(h.primeSourceMeta).toHaveBeenCalledExactlyOnceWith(scope);
  });
});

describe("sendCustom", () => {
  const body = { systemPrompt: "be brief", userMessage: text("hi") };

  it("answers the one-shot message as JSON", async () => {
    const reply = await h.call(aiController.sendCustom, body);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ role: "assistant", content: "ok" });
    expect(responseViolations("aiSendCustom", 200, reply.body)).toEqual([]);
    expect(h.engineInput("sendCustom")).toEqual({
      ...body,
      actionArgs: { signal: expect.any(AbortSignal) },
    });
  });

  it.each([
    ["isStream is true", true],
    ["isStream is false", false],
  ])("streams NDJSON whenever the engine answers a stream, %s", async (_what, isStream) => {
    h.engine.sendCustom.mockReturnValue(h.streamOf(START, END) as never);

    const reply = await h.call(aiController.sendCustom, { ...body, isStream });

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("application/x-ndjson; charset=utf-8");
    expect(ndjsonFrames(reply.body)).toEqual([START, END]);
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/send-custom: stream completed after 2 event(s); toolCalls=<none>; chatContext=hit",
    );
  });

  it("answers JSON when a stream was asked for and the engine answers one message", async () => {
    const reply = await h.call(aiController.sendCustom, { ...body, isStream: true });

    expect(reply.headers["content-type"]).toBe("application/json");
    expect(reply.body).toEqual({ role: "assistant", content: "ok" });
  });

  it.each([
    ["a bare string", "plain", "plain"],
    [
      "an object whose async iterator is not a function",
      { [Symbol.asyncIterator]: 5, a: 1 },
      { a: 1 },
    ],
  ])("answers JSON when the engine answers %s", async (_what, result, json) => {
    h.engine.sendCustom.mockReturnValue(result as never);

    const reply = await h.call(aiController.sendCustom, { ...body, isStream: true });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(json);
  });

  it.each([
    ["no profile", undefined],
    ["a profile of another provider", { id: "p1", providerType: "openai" }],
  ])("answers 400 or runs on the profile check: %s", async (_what, profile) => {
    h.storage.profiles.readById.mockResolvedValue(profile);

    const reply = await h.call(aiController.sendCustom, { ...body, profileId: "p1" });

    expect(reply.status).toBe(profile ? 200 : 400);
    expect(upstream.seen).toEqual([]);
  });

  it("refuses a malformed profileId as unknown (the AI service answers 400)", async () => {
    h.storage.profiles.readById.mockRejectedValue(
      new AiServiceHttpError(400, "Bad Request", "", "http://ai.internal/profiles/x"),
    );

    const reply = await h.call(aiController.sendCustom, { ...body, profileId: "x" });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "unknown profileId: x" });
  });

  it.each([
    ["answers 500", new AiServiceHttpError(500, "Internal Server Error", "b", "u"), 500],
    ["times out", new Error("upstream timeout after 300000ms"), 500],
  ])("passes on a profile read that %s", async (_what, error, status) => {
    h.storage.profiles.readById.mockRejectedValue(error);

    const reply = await h.call(aiController.sendCustom, { ...body, profileId: "p1" });

    expect(reply.status).toBe(status);
    expect(h.engine.sendCustom).not.toHaveBeenCalled();
    expect(responseViolations("aiSendCustom", 500, reply.body)).toEqual([]);
  });
});

// The gateway's AI Tools wallet: the round's aggregate answers, or else /config.
// GET internal/ai/config is SettingsController.GetAiSettingsAsync (products/ASC.AI/Server/Api/
// SettingsController.cs), answering AiSettingsDto (Server/Models/ResponseDto/AiSettingsDto.cs).
describe.each([
  ["sendCustom", { systemPrompt: "s", userMessage: text("hi"), profileId: "p1" }],
  ["sendWithStream", { threadId: "t1", userMessage: text("hi"), profileId: "p1" }],
  ["sendWithStreamOpenAI", { threadId: "t1", userMessage: text("hi"), profileId: "p1" }],
] as const)("the unpaid gateway on %s", (name, body) => {
  // A full AiSettingsDto as the AI service serializes it.
  const settings = {
    vectorizationEnabled: true,
    vectorizationNeedReset: false,
    aiReady: false,
    embeddingModel: "text-embedding-3-small",
    systemAiEnabled: true,
    recommendedModelForForms: "gpt-5.4",
    toolPermissionMode: 1,
  };
  const config = (answer: () => Response | Promise<Response>) =>
    upstream.use(http.get("*/internal/ai/config", answer));

  beforeEach(() => {
    h.storage.profiles.readById.mockResolvedValue({ id: "p1", providerType: "onlyoffice" });
  });

  it("answers 402 from the round's aggregate without reading /config", async () => {
    h.chat.snapshot = { aiReady: false };

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(402);
    expect(reply.body).toEqual({ error: UNPAID });
    expect(upstream.seen).toEqual([]);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("runs when the aggregate says the gateway is ready, without reading /config", async () => {
    h.chat.snapshot = { aiReady: true };

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(200);
    expect(upstream.seen).toEqual([]);
  });

  // AiSettingsDto carries no localized reason, so the 402 is the service's own sentence and
  // /config is read once, for aiReady only (Bug 84023).
  it("answers 402 after one /config read when the round has no aggregate", async () => {
    config(() => HttpResponse.json(settings));

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(402);
    expect(reply.body).toEqual({ error: UNPAID });
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual(["GET /internal/ai/config"]);
  });

  it.each([
    ["aiReady true", () => HttpResponse.json({ ...settings, aiReady: true })],
    ["no aiReady", () => HttpResponse.json({ embeddingModel: "m" })],
    ["a body that is not an object", () => HttpResponse.json(["x"])],
  ])("runs when /config answers %s", async (_what, answer) => {
    config(answer);

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(200);
  });

  // HANDLED DOWNSTREAM ai-8: a /config read that fails without a refusal of the caller's own counts as "paid" and the round opens its stream. In production the gateway refuses an unpaid portal itself (AiGatewayProxyController.cs, 403), so nothing is billed: the caller gets an error frame instead of the 402; it shows only with the engine or the service stubbed.
  it.each([
    ["answers 503", () => HttpResponse.json({ error: "down" }, { status: 503 }), "AI service 503"],
    ["drops the connection", () => HttpResponse.error(), "TypeError: Failed to fetch"],
  ])("runs when /config %s, with a warning", async (_what, answer, logged) => {
    config(answer);

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(200);
    expect(h.engine[name]).toHaveBeenCalledTimes(1);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining(
        `ai-tools wallet check: /config not read, the gateway decides: ${logged}`,
      ),
    );
  });

  it("passes on a /config read the AI service refuses with 403", async () => {
    config(() => HttpResponse.json({ error: "no" }, { status: 403 }));

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(403);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("asks nothing for a profile of another provider", async () => {
    h.storage.profiles.readById.mockResolvedValue({ id: "p1", providerType: "anthropic" });
    h.chat.snapshot = { aiReady: false };

    const reply = await h.call(aiController[name], body);

    expect(reply.status).toBe(200);
    expect(upstream.seen).toEqual([]);
  });
});

describe.each(["sendWithStream", "sendWithStreamOpenAI"] as const)("pre-flight of %s", (name) => {
  it("runs the round on the thread's own model when the body names none (Bug 82860)", async () => {
    h.storage.threads.readById.mockResolvedValue({ id: "t1", profileId: "p-thread" });
    let seen: unknown[] = [];
    (
      h.engine[name] as unknown as { mockImplementation: (f: () => unknown) => void }
    ).mockImplementation(() => {
      seen = [getRoundProfileId(), getRoundThreadId()];
      return h.streamOf(END);
    });

    await h.call(aiController[name], { threadId: "t1", userMessage: text("hi") });

    expect(h.storage.threads.readById).toHaveBeenCalledWith("t1");
    expect(h.storage.profiles.readById).toHaveBeenCalledWith("p-thread");
    expect(h.engineInput(name)["profileId"]).toBe("p-thread");
    expect(seen).toEqual(["p-thread", "t1"]);
  });

  it("keeps no profile when the thread has none and checks nothing", async () => {
    h.storage.threads.readById.mockResolvedValue({ id: "t1", profileId: null });

    const reply = await h.call(aiController[name], { threadId: "t1", userMessage: text("hi") });

    expect(reply.status).toBe(200);
    expect(h.engineInput(name)["profileId"]).toBeUndefined();
    expect(h.storage.profiles.readById).not.toHaveBeenCalled();
  });

  it("does not read the thread when the body names a profile", async () => {
    await h.call(aiController[name], { threadId: "t1", profileId: "p1", userMessage: text("hi") });

    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(h.storage.profiles.readById).toHaveBeenCalledWith("p1");
  });

  it.each([
    [
      "answers 500",
      new AiServiceHttpError(500, "Internal Server Error", "", "u"),
      500,
      "Internal Server Error",
    ],
    ["is refused", new AiServiceHttpError(404, "Not Found", "", "u"), 404, "Not Found"],
  ])("answers the status when the thread read %s", async (_w, error, status, message) => {
    h.storage.threads.readById.mockRejectedValue(error);

    const reply = await h.call(aiController[name], { threadId: "t1", userMessage: text("hi") });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error: message });
    expect(responseViolations(OPERATION[name], status, reply.body)).toEqual([]);
    expect(h.engine[name]).not.toHaveBeenCalled();
  });

  it("gates a new thread and relays the gate's refusal", async () => {
    h.assertThreadCreatable.mockRejectedValue(
      Object.assign(new Error("No AI model is configured"), { status: 409, expose: true }),
    );

    const reply = await h.call(aiController[name], {
      entityId: "12",
      profileId: "p1",
      userMessage: text("hi"),
    });

    expect(h.assertThreadCreatable).toHaveBeenCalledExactlyOnceWith("12", "p1");
    expect(reply.status).toBe(409);
    expect(reply.body).toEqual({ error: "No AI model is configured" });
    expect(responseViolations(OPERATION[name], 409, reply.body)).toEqual([]);
    expect(h.storage.profiles.readById).not.toHaveBeenCalled();
  });

  it("answers the timeout of the profile read as 500, not as an unknown profile", async () => {
    h.storage.profiles.readById.mockRejectedValue(new Error("upstream timeout after 300000ms"));

    const reply = await h.call(aiController[name], {
      threadId: "t1",
      profileId: "p1",
      userMessage: text("hi"),
    });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("the prompt of a streaming round", () => {
  const stream = (body: Record<string, unknown>) =>
    h.call(aiController.sendWithStream, { threadId: "t1", userMessage: text("hi"), ...body });

  it("appends the workspace context to the round's prompt", async () => {
    await stream({});

    expect(promptOf(h.engineInput("sendWithStream"))).toEqual({ mode: "append", text: CONTEXT });
    expect(h.resolveContextFolders).toHaveBeenCalledExactlyOnceWith([]);
    expect(h.resolveLocation).not.toHaveBeenCalled();
  });

  it("puts the tools fragment first and the context after a blank line", async () => {
    h.safeGetToolsPrompt.mockResolvedValue("TOOLS FRAGMENT");

    await stream({});

    expect(promptOf(h.engineInput("sendWithStream"))).toEqual({
      mode: "append",
      text: `TOOLS FRAGMENT\n\n${CONTEXT}`,
    });
  });

  it("keeps the mode and text of the client's own prompt override", async () => {
    h.safeGetToolsPrompt.mockResolvedValue("TOOLS");

    await stream({ actionArgs: { prompt: { mode: "replace", text: "Be terse.", extra: 1 } } });

    expect(promptOf(h.engineInput("sendWithStream"))).toEqual({
      mode: "replace",
      text: `Be terse.\n\nTOOLS\n\n${CONTEXT}`,
      extra: 1,
    });
  });

  it.each([
    ["a string", "Be terse."],
    ["an object without text", { mode: "replace" }],
  ])("replaces a client prompt that is %s", async (_what, prompt) => {
    await stream({ actionArgs: { prompt } });

    expect(promptOf(h.engineInput("sendWithStream"))).toEqual({ mode: "append", text: CONTEXT });
  });

  it("describes the user's location by what the Files API says of it", async () => {
    const location = { id: "8", title: "Quarterly\nreports", kind: "room" as const };
    h.resolveLocation.mockResolvedValue(location);

    await stream({ contextEntityId: "8", entityId: "7" });

    expect(h.resolveLocation).toHaveBeenCalledExactlyOnceWith("8");
    expect(promptOf(h.engineInput("sendWithStream"))).toEqual({
      mode: "append",
      text: [...HEAD, ...buildLocationLines(location), ...TAIL].join("\n"),
    });
  });

  it("lists the context folders and leaves out a location that is one of them", async () => {
    const folders = [
      { id: "8", title: "Docs", kind: "folder" as const, canCreate: true },
      { id: "9", kind: "room" as const },
    ];
    h.resolveContextFolders.mockResolvedValue(folders);

    await stream({
      contextEntityId: "8",
      actionArgs: { folderContexts: [{ id: 8 }, { id: "9" }, { id: "8" }] },
    });

    expect(h.resolveContextFolders).toHaveBeenCalledExactlyOnceWith(["8", "9"]);
    expect(h.resolveLocation).not.toHaveBeenCalled();
    expect(promptOf(h.engineInput("sendWithStream"))).toEqual({
      mode: "append",
      text: [...HEAD, ...buildContextFoldersLines(folders), ...TAIL].join("\n"),
    });
  });

  it("asks the tools fragment for the location, else the project", async () => {
    await stream({ contextEntityId: "8", entityId: "7" });
    await stream({ entityId: "7" });

    expect(h.safeGetToolsPrompt.mock.calls.map((c) => [c[1], c[2]])).toEqual([
      ["8", []],
      ["7", []],
    ]);
  });

  it("hands the message's own attachments to the tools fragment and the round", async () => {
    let roundAttachments: string[] = [];
    h.engine.sendWithStream.mockImplementation(() => {
      roundAttachments = getRoundAttachmentIds();
      return h.streamOf(END) as never;
    });

    await stream({
      userMessage: {
        role: "user",
        content: [
          { type: "text", text: "fill" },
          attachment("a1"),
          attachment("a2"),
          attachment("a1"),
        ],
      },
    });

    expect(h.safeGetToolsPrompt.mock.calls[0]?.[2]).toEqual(["a1", "a2"]);
    expect(roundAttachments).toEqual(["a1", "a2"]);
    expect(h.storage.messages.readByThread).not.toHaveBeenCalled();
  });

  it("falls back to the attachments of the thread's history, newest first", async () => {
    h.storage.messages.readByThread.mockResolvedValue([
      { role: "user", content: [attachment("old"), attachment("both")] },
      { role: "assistant", content: "done" },
      { role: "user", content: [attachment("new"), attachment("both")] },
    ]);
    let roundAttachments: string[] = ["unset"];
    h.engine.sendWithStream.mockImplementation(() => {
      roundAttachments = getRoundAttachmentIds();
      return h.streamOf(END) as never;
    });

    await stream({});

    expect(h.storage.messages.readByThread).toHaveBeenCalledExactlyOnceWith("t1");
    expect(h.safeGetToolsPrompt.mock.calls[0]?.[2]).toEqual(["new", "both", "old"]);
    // The document agent sees the message's own attachments only.
    expect(roundAttachments).toEqual([]);
  });

  it.each([
    ["404", 404],
    ["400", 400],
  ])("carries no history attachments when the history read answers %s", async (_w, status) => {
    h.storage.messages.readByThread.mockRejectedValue(
      new AiServiceHttpError(status, "Not Found", "", "u"),
    );

    const reply = await stream({});

    expect(reply.status).toBe(200);
    expect(h.safeGetToolsPrompt.mock.calls[0]?.[2]).toEqual([]);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      `withToolsPrompt: thread t1 history not found, form context not carried over: AI service ${status}`,
    );
  });

  it.each([
    ["answers 500", new AiServiceHttpError(500, "Internal Server Error", "", "u")],
    ["times out", new Error("upstream timeout after 300000ms")],
    ["fails with something that is not an Error", "socket hang up"],
  ])("fails the round before its stream when the history read %s", async (_w, error) => {
    h.storage.messages.readByThread.mockRejectedValue(error);

    const reply = await stream({});

    expect(reply.status).toBe(500);
    expect(JSON.stringify(reply.body)).not.toContain("upstream timeout");
    expect(h.safeGetToolsPrompt).not.toHaveBeenCalled();
    expect(h.engine.sendWithStream).not.toHaveBeenCalled();
  });

  it("fails the round before its stream when the round's form cannot be read", async () => {
    h.safeGetToolsPrompt.mockRejectedValue(
      new AiServiceHttpError(503, "Service Unavailable", "", "u"),
    );

    const reply = await stream({});

    expect(reply.status).toBe(503);
    expect(h.engine.sendWithStream).not.toHaveBeenCalled();
  });

  it("reads no history for a new thread", async () => {
    await h.call(aiController.sendWithStream, { userMessage: text("hi") });

    expect(h.storage.messages.readByThread).not.toHaveBeenCalled();
  });

  it("logs the round's ids as the client sent them", async () => {
    await stream({ entityId: "7", contextEntityId: "8", profileId: "p1" });
    await h.call(aiController.sendWithStream, { userMessage: text("hi") });

    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/send-with-stream: round threadId=t1 entityId=7 contextEntityId=8 profileId=p1",
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/send-with-stream: round threadId=<new> entityId=- contextEntityId=- profileId=-",
    );
  });
});

describe("sendWithStream", () => {
  it("streams the engine's events as NDJSON with the headers that keep proxies out", async () => {
    h.engine.sendWithStream.mockReturnValue(h.streamOf(START, DELTA, END) as never);

    const reply = await h.call(aiController.sendWithStream, VALID.sendWithStream);

    expect(reply.status).toBe(200);
    expect(reply.ended).toBe(true);
    expect(reply.headers).toEqual({
      "content-type": "application/x-ndjson; charset=utf-8",
      "cache-control": "no-cache, no-transform",
      "x-accel-buffering": "no",
    });
    expect(reply.body).toBe(
      `${JSON.stringify(START)}\n${JSON.stringify(DELTA)}\n${JSON.stringify(END)}\n`,
    );
    for (const frame of ndjsonFrames(reply.body)) {
      expect(schemaViolations("AiChatEvent", frame)).toEqual([]);
    }
  });

  it("ends a stream that fails mid-way with a generic error frame and status 200", async () => {
    h.engine.sendWithStream.mockReturnValue(
      h.failingStream(new Error("provider said http://10.1.2.3 secret"), START) as never,
    );

    const reply = await h.call(aiController.sendWithStream, VALID.sendWithStream);

    expect(reply.status).toBe(200);
    expect(reply.ended).toBe(true);
    const frames = ndjsonFrames(reply.body);
    expect(frames).toEqual([START, { type: "error", message: "stream error" }]);
    expect(String(reply.body)).not.toContain("10.1.2.3");
    expect(schemaViolations("AiStreamErrorEvent", frames[1])).toEqual([]);
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      expect.stringMatching(
        /^ai\/send-with-stream: stream threw after 1 event\(s\): provider said http:\/\/10\.1\.2\.3 secret\nError: provider said/,
      ),
    );
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      expect.stringMatching(/^stream aborted: Error: provider said/),
    );
  });

  it.each([
    ["a plain Error", new Error("boom"), 500, "Internal server error"],
    [
      "an AI service 404",
      new AiServiceHttpError(404, "Not Found", "body", "http://ai/x"),
      404,
      "Not Found",
    ],
  ])(
    "answers JSON when the stream fails before its first event with %s",
    async (_w, error, status, message) => {
      h.engine.sendWithStream.mockReturnValue(h.failingStream(error) as never);

      const reply = await h.call(aiController.sendWithStream, VALID.sendWithStream);

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({ error: message });
      expect(responseViolations("aiSendWithStream", status, reply.body)).toEqual([]);
      expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
        expect.stringMatching(/^ai\/send-with-stream: stream threw after 0 event\(s\): /),
      );
    },
  );

  it("logs a stream that threw something that is not an Error", async () => {
    h.engine.sendWithStream.mockReturnValue(h.failingStream("bare string", START) as never);

    await h.call(aiController.sendWithStream, VALID.sendWithStream);

    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      "ai/send-with-stream: stream threw after 1 event(s): bare string",
    );
  });
});

describe("sendWithStreamOpenAI", () => {
  it("frames the chunks as SSE and ends with [DONE]", async () => {
    const reply = await h.call(aiController.sendWithStreamOpenAI, VALID.sendWithStreamOpenAI);

    expect(reply.status).toBe(200);
    expect(reply.headers).toEqual({
      "content-type": "text/event-stream; charset=utf-8",
      "cache-control": "no-cache, no-transform",
      connection: "keep-alive",
      "x-accel-buffering": "no",
    });
    expect(reply.body).toBe(
      `data: ${JSON.stringify(CHUNK)}\n\ndata: ${JSON.stringify(LAST_CHUNK)}\n\ndata: [DONE]\n\n`,
    );
    for (const frame of sseFrames(reply.body).slice(0, -1)) {
      expect(schemaViolations("AiOpenAIStreamChunk", frame)).toEqual([]);
    }
    expect(promptOf(h.engineInput("sendWithStreamOpenAI"))).toEqual({
      mode: "append",
      text: [...HEAD, ...buildLocationLines({ id: "8" }), ...TAIL].join("\n"),
    });
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/send-with-stream-openai: stream completed after 2 event(s); toolCalls=<none>; chatContext=hit",
    );
  });

  it("ends a stream that fails mid-way with an OpenAI error frame and no [DONE]", async () => {
    h.engine.sendWithStreamOpenAI.mockReturnValue(
      h.failingStream(new Error("socket reset by 10.0.0.9"), CHUNK) as never,
    );

    const reply = await h.call(aiController.sendWithStreamOpenAI, VALID.sendWithStreamOpenAI);

    expect(reply.status).toBe(200);
    const frames = sseFrames(reply.body);
    expect(frames).toEqual([
      CHUNK,
      { error: { message: "stream error", type: "server_error", code: null, param: null } },
    ]);
    expect(String(reply.body)).not.toContain("10.0.0.9");
    expect(schemaViolations("AiOpenAIStreamError", frames[1])).toEqual([]);
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      expect.stringMatching(/^openai stream aborted: Error: socket reset/),
    );
  });

  it("passes the engine's own error envelope through as a chunk", async () => {
    const envelope = {
      error: { message: "Rate limited", type: "rate_limit", code: "rate_limit", param: null },
    };
    h.engine.sendWithStreamOpenAI.mockReturnValue(h.streamOf(envelope) as never);

    const reply = await h.call(aiController.sendWithStreamOpenAI, VALID.sendWithStreamOpenAI);

    expect(sseFrames(reply.body)).toEqual([envelope, "[DONE]"]);
    expect(schemaViolations("AiOpenAIStreamChunk", envelope)).toEqual([]);
  });
});

describe("regenerateStream", () => {
  const capture = (): { seen: unknown[] } => {
    const box = { seen: [] as unknown[] };
    h.engine.regenerateStream.mockImplementation(() => {
      box.seen = [getRoundProfileId(), getRoundThreadId()];
      return h.streamOf(START, END) as never;
    });
    return box;
  };

  it("runs on the profile the body names without reading the thread", async () => {
    const box = capture();

    const reply = await h.call(aiController.regenerateStream, {
      ...VALID.regenerateStream,
      profileId: "p9",
    });

    expect(reply.status).toBe(200);
    expect(ndjsonFrames(reply.body)).toEqual([START, END]);
    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(box.seen).toEqual(["p9", "t1"]);
  });

  it.each([
    ["the thread's model", { id: "t1", profileId: "p-thread" }, "p-thread"],
    ["no model for a thread without one", { id: "t1", profileId: null }, undefined],
    ["no model for a thread that is gone", null, undefined],
  ])("runs on %s when the body names none", async (_what, thread, profile) => {
    h.storage.threads.readById.mockResolvedValue(thread);
    const box = capture();

    await h.call(aiController.regenerateStream, VALID.regenerateStream);

    expect(h.storage.threads.readById).toHaveBeenCalledExactlyOnceWith("t1");
    expect(box.seen).toEqual([profile, "t1"]);
    expect(h.engineInput("regenerateStream")["profileId"]).toBeUndefined();
  });

  it("answers the status of a thread read the AI service refuses", async () => {
    h.storage.threads.readById.mockRejectedValue(new AiServiceHttpError(404, "Not Found", "", "u"));

    const reply = await h.call(aiController.regenerateStream, VALID.regenerateStream);

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Not Found" });
    expect(responseViolations("aiRegenerateStream", 404, reply.body)).toEqual([]);
    expect(h.engine.regenerateStream).not.toHaveBeenCalled();
  });

  it("gives the regenerated round the same prompt envelope as the original", async () => {
    h.safeGetToolsPrompt.mockResolvedValue("TOOLS");

    await h.call(aiController.regenerateStream, VALID.regenerateStream);

    expect(promptOf(h.engineInput("regenerateStream"))).toEqual({
      mode: "append",
      text: `TOOLS\n\n${[...HEAD, ...buildLocationLines({ id: "8" }), ...TAIL].join("\n")}`,
    });
    expect(h.primeSourceMeta).toHaveBeenCalledExactlyOnceWith("8");
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/regenerate-stream: stream completed after 2 event(s); toolCalls=<none>; chatContext=hit",
    );
  });
});

describe("approveToolCall", () => {
  const capture = (): { seen: unknown[] } => {
    const box = { seen: [] as unknown[] };
    h.engine.approveToolCall.mockImplementation(() => {
      box.seen = [getRoundProfileId(), getRoundAttachmentIds()];
      return h.streamOf(END) as never;
    });
    return box;
  };

  it("resumes the round on the thread's model with the last user message's attachments", async () => {
    h.storage.threads.readById.mockResolvedValue({ id: "t1", profileId: "p-thread" });
    h.storage.messages.readByThread.mockResolvedValue([
      { role: "user", content: [attachment("first")] },
      { role: "user", content: [{ type: "text", text: "fill" }, attachment("last")] },
      { role: "assistant", content: [{ type: "tool-call", toolName: "fill_form" }] },
      "junk",
    ]);
    const box = capture();

    const reply = await h.call(aiController.approveToolCall, VALID.approveToolCall);

    expect(reply.status).toBe(200);
    expect(ndjsonFrames(reply.body)).toEqual([END]);
    expect(box.seen).toEqual(["p-thread", ["last"]]);
    expect(h.engineInput("approveToolCall")).toEqual({
      ...VALID.approveToolCall,
      actionArgs: { signal: expect.any(AbortSignal) },
    });
    expect(h.safeGetToolsPrompt).not.toHaveBeenCalled();
    expect(h.primeSourceMeta).toHaveBeenCalledExactlyOnceWith("8");
  });

  it("prefers the profile the body names to the thread's", async () => {
    h.storage.threads.readById.mockResolvedValue({ id: "t1", profileId: "p-thread" });
    const box = capture();

    await h.call(aiController.approveToolCall, { ...VALID.approveToolCall, profileId: "p1" });

    expect(box.seen).toEqual(["p1", []]);
  });

  it("reads a thread that is gone as no requested profile", async () => {
    h.storage.threads.readById.mockResolvedValue(null);
    const box = capture();

    await h.call(aiController.approveToolCall, VALID.approveToolCall);

    expect(box.seen).toEqual([undefined, []]);
  });

  it("answers 400 and reads nothing of the thread when threadId is missing", async () => {
    const reply = await h.call(aiController.approveToolCall, {
      ...VALID.approveToolCall,
      threadId: undefined,
    });

    expect(reply.status).toBe(400);
    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(h.storage.messages.readByThread).not.toHaveBeenCalled();
    expect(h.engine.approveToolCall).not.toHaveBeenCalled();
  });

  it("reads nothing of the thread when threadId is empty, and leaves the id to the engine", async () => {
    await h.call(aiController.approveToolCall, { ...VALID.approveToolCall, threadId: "" });

    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(h.storage.messages.readByThread).not.toHaveBeenCalled();
    expect(h.engine.approveToolCall).toHaveBeenCalledTimes(1);
  });

  it.each([404, 400])(
    "still runs the approved tool when the thread read answers %s",
    async (status) => {
      h.storage.messages.readByThread.mockRejectedValue(
        new AiServiceHttpError(status, "Not Found", "", "u"),
      );
      const box = capture();

      const reply = await h.call(aiController.approveToolCall, VALID.approveToolCall);

      expect(reply.status).toBe(200);
      expect(box.seen).toEqual([undefined, []]);
      expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
        `primeRoundFromThread: thread t1 not found: AI service ${status}`,
      );
    },
  );

  it.each([
    ["answers 500", new AiServiceHttpError(500, "Internal Server Error", "", "u")],
    ["times out", new Error("upstream timeout after 300000ms")],
    ["throws something that is not an Error", "dropped"],
  ])("runs no approved tool when the thread read %s", async (_w, error) => {
    h.storage.messages.readByThread.mockRejectedValue(error);

    const reply = await h.call(aiController.approveToolCall, VALID.approveToolCall);

    expect(reply.status).toBe(500);
    expect(h.engine.approveToolCall).not.toHaveBeenCalled();
  });
});

describe("denyToolCall", () => {
  it("streams the engine's answer without reading the thread or building a prompt", async () => {
    const reply = await h.call(aiController.denyToolCall, VALID.denyToolCall);

    expect(reply.status).toBe(200);
    expect(ndjsonFrames(reply.body)).toEqual([END]);
    expect(h.storage.threads.readById).not.toHaveBeenCalled();
    expect(h.storage.messages.readByThread).not.toHaveBeenCalled();
    expect(h.safeGetToolsPrompt).not.toHaveBeenCalled();
    expect(h.engineInput("denyToolCall")).toEqual({
      ...VALID.denyToolCall,
      actionArgs: { signal: expect.any(AbortSignal) },
    });
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/deny-tool-call: stream completed after 1 event(s); toolCalls=<none>; chatContext=hit",
    );
  });
});

// What the stream tap writes to the log while it passes the events on unchanged.
describe("the log of a stream", () => {
  const run = async (...events: unknown[]) => {
    h.engine.denyToolCall.mockReturnValue(h.streamOf(...events) as never);
    const reply = await h.call(aiController.denyToolCall, VALID.denyToolCall);
    expect(ndjsonFrames(reply.body)).toEqual(events);
  };
  const toolCall = (part: Record<string, unknown>) => ({
    type: "message-delta",
    message: { role: "assistant", content: [{ type: "tool-call", ...part }] },
  });

  it("warns about a provider error with its code, message and causes", async () => {
    await run({
      type: "message-incomplete",
      message: {
        role: "assistant",
        content: [],
        status: {
          type: "incomplete",
          reason: "error",
          error: {
            code: "rate_limit",
            message: "slow down",
            cause: { code: "http_429", cause: { message: "429 Too Many Requests", cause: "x" } },
          },
        },
      },
    });

    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "ai/deny-tool-call: provider returned message-incomplete — code=rate_limit, message=slow down, cause=(code=http_429, cause=(message=429 Too Many Requests))",
    );
  });

  it.each([
    ["a string", "boom", "boom"],
    ["an object without code or message", { status: 500 }, '{"status":500}'],
    ["absent", undefined, "undefined"],
  ])("describes a provider error that is %s", async (_what, error, described) => {
    await run({
      type: "message-incomplete",
      message: { status: { reason: "error", error } },
    });

    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      `ai/deny-tool-call: provider returned message-incomplete — ${described}`,
    );
  });

  it("warns about an answer cut for another reason", async () => {
    await run({ type: "message-incomplete", message: { status: { reason: "length" } } });

    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "ai/deny-tool-call: message-incomplete reason=length",
    );
  });

  it("says nothing of an incomplete message without a status", async () => {
    await run({ type: "message-incomplete", message: {} }, { type: "message-incomplete" });

    expect(vi.mocked(logger.warn)).not.toHaveBeenCalled();
  });

  it("logs a pending tool call by name, id and flags", async () => {
    await run(
      {
        type: "tool-call-pending",
        idx: 1,
        serverExecuted: true,
        autoAllow: false,
        message: {
          role: "assistant",
          content: [
            { type: "text", text: "Let me" },
            { type: "tool-call", toolName: "create_room", toolCallId: "call_7" },
          ],
        },
      },
      { type: "tool-call-pending" },
      { type: "tool-call-pending", idx: 0, message: { content: ["junk"] } },
    );

    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/deny-tool-call: tool-call-pending — tool=create_room callId=call_7 idx=1 serverExecuted=true autoAllow=false",
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/deny-tool-call: tool-call-pending — tool=? callId=? idx=-1 serverExecuted=false autoAllow=false",
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/deny-tool-call: tool-call-pending — tool=? callId=? idx=0 serverExecuted=false autoAllow=false",
    );
  });

  it("sums up which tools ran, latching a result once seen", async () => {
    await run(
      toolCall({ toolName: "web_search", toolCallId: "c1" }),
      toolCall({ toolName: "web_search", toolCallId: "c1", result: "found" }),
      toolCall({ toolName: "web_search", toolCallId: "c1", result: null }),
      toolCall({ toolName: "generate_image", toolCallId: "c2" }),
      toolCall({ toolCallId: "c3" }),
      { type: "message-delta", message: { content: [{ type: "text", text: "x" }, "junk"] } },
      "not an object",
    );

    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/deny-tool-call: stream completed after 7 event(s); toolCalls=web_search(executed=true), generate_image(executed=false), ?(executed=false); chatContext=hit",
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "ai/deny-tool-call: tool result web_search (callId=c1) -> found",
    );
    expect(
      vi.mocked(logger.info).mock.calls.filter(([line]) => String(line).includes("tool result")),
    ).toHaveLength(1);
  });

  it.each([
    [
      "an image as base64",
      { data: { base64: "QUJD" } },
      "info",
      "{data.base64=4B} (not persisted — no ref)",
    ],
    ["a stored image", JSON.stringify({ data: { ref: "att-1" } }), "info", "{data.ref=att-1}"],
    [
      "an error",
      { error: { code: "insufficient_funds", message: "402 from gateway" } },
      "error",
      'ERROR {"code":"insufficient_funds","message":"402 from gateway"}',
    ],
    ["a plain object", { data: { rows: 3 } }, "info", '{"data":{"rows":3}}'],
    ["a JSON array", [1, 2], "info", "[1,2]"],
    ["a number", 42, "info", "42"],
  ])("describes a tool result that is %s", async (_what, result, level, described) => {
    await run(toolCall({ toolName: "t", toolCallId: "c9", result }));

    const log = level === "error" ? logger.error : logger.info;
    expect(vi.mocked(log)).toHaveBeenCalledWith(
      `ai/deny-tool-call: tool result t (callId=c9) -> ${described}`,
    );
  });

  it("cuts a long result at 500 characters and an error at 700", async () => {
    const long = "y".repeat(600);
    const longError = { error: "e".repeat(800) };
    await run(
      toolCall({ toolName: "t", toolCallId: "a", result: long }),
      toolCall({ toolName: "t", toolCallId: "b", result: longError }),
    );

    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      `ai/deny-tool-call: tool result t (callId=a) -> ${"y".repeat(500)}… (600B)`,
    );
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      `ai/deny-tool-call: tool result t (callId=b) -> ERROR ${JSON.stringify(longError.error).slice(0, 700)}`,
    );
  });

  it("names a result keyed by the tool name when the call has no id, and logs it once", async () => {
    const event = toolCall({ toolName: "ocr", result: "text" });
    await run(event, event);

    expect(
      vi.mocked(logger.info).mock.calls.filter(([line]) => String(line).includes("tool result")),
    ).toEqual([["ai/deny-tool-call: tool result ocr (callId=ocr) -> text"]]);
  });

  it("describes a result that JSON cannot write by its string form", async () => {
    // A tool result is JSON on the wire; an in-process value that is not (a function) still logs.
    const result = (): void => {};
    h.engine.denyToolCall.mockReturnValue(
      h.streamOf(toolCall({ toolName: "f", toolCallId: "cf", result })) as never,
    );

    await h.call(aiController.denyToolCall, VALID.denyToolCall);

    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      `ai/deny-tool-call: tool result f (callId=cf) -> ${String(result)}`,
    );
  });
});
