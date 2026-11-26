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

import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
const engine = {
  send: vi.fn(),
  sendCustom: vi.fn(),
  sendWithStream: vi.fn(),
  sendWithStreamOpenAI: vi.fn(),
  regenerateStream: vi.fn(),
};
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@onlyoffice/ai-chat/core")>();
  return {
    ...actual,
    AIEngine: class {
      send = engine.send;
      sendCustom = engine.sendCustom;
      sendWithStream = engine.sendWithStream;
      sendWithStreamOpenAI = engine.sendWithStreamOpenAI;
      regenerateStream = engine.regenerateStream;
    },
    composeToolsAdapters: () => ({}),
  };
});
const setRoundProfileId = vi.fn();
const setRoundThreadId = vi.fn();
vi.mock("../../requestContext.js", () => ({
  markForwardHeadersToProvider: vi.fn(),
  getCustomServerNames: vi.fn(() => []),
  getChatContextSnapshot: vi.fn(),
  getSourceMeta: vi.fn(),
  setKnowledgeRoomIds: vi.fn(),
  setRoundAttachmentIds: vi.fn(),
  setRoundProfileId,
  setRoundThreadId,
}));
vi.mock("../../storage/chatContext.js", () => ({
  primeChatContext: vi.fn(async () => {}),
  describeChatContextUsage: vi.fn(() => ""),
}));
vi.mock("../../storage/aiProjectChatContext.js", () => ({
  applyAiProjectChatContext: vi.fn(async () => null),
}));
vi.mock("../../storage/contextFolders.js", () => ({
  buildContextFoldersLines: vi.fn(() => []),
  buildLocationLines: vi.fn(() => []),
  contextFolderIdsOf: vi.fn(() => []),
  resolveContextFolders: vi.fn(async () => []),
  resolveLocation: vi.fn(async () => undefined),
}));
vi.mock("../../tools/customTools.js", () => ({
  customToolsSource: {},
  primeCustomServers: vi.fn(async () => {}),
}));
const readThread = vi.fn();
vi.mock("../../storage/index.js", () => ({ storage: { threads: { readById: readThread } } }));
vi.mock("../../storage/httpClient.js", () => ({
  AiServiceHttpError: class extends Error {},
  aiService: { get: vi.fn(async () => ({})) },
}));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: class extends Error {},
  primeSourceMeta: vi.fn(async () => {}),
}));
vi.mock("../../telemetry/chatStream.js", () => ({
  observeChatStream: (_: unknown, s: unknown) => s,
}));
const assertThreadCreatable = vi.fn();
vi.mock("../threadsController.js", () => ({ assertThreadCreatable }));
const readStoredProfile = vi.fn();
vi.mock("../storedProfile.js", () => ({ readStoredProfile }));
vi.mock("../../tools/httpToolsAdapter.js", () => ({
  HttpToolsAdapter: class {},
  safeGetToolsPrompt: vi.fn(async () => ""),
  extractAttachmentRefIds: vi.fn(() => []),
}));
vi.mock("../../tools/systemTools.js", () => ({ systemToolsSource: { getServerTypes: () => [] } }));
vi.mock("../../tools/formAnalysisSubAgent.js", () => ({ formAnalysisSubAgent: {} }));
vi.mock("../../tools/documentAgent.js", () => ({ documentAgentTools: {} }));

const { aiController } = await import("../aiController.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const run = async (name: keyof typeof aiController, body: unknown) => {
  const res = {
    status: vi.fn(),
    json: vi.fn(),
    on: vi.fn(),
    writableEnded: false,
  };
  res.status.mockReturnValue(res);
  await (aiController[name] as unknown as Handler)({ body, query: {} }, res, vi.fn());
  return { status: res.status.mock.calls[0]?.[0] as number | undefined, res };
};

const text = (value: string) => ({ role: "user", content: [{ type: "text", text: value }] });
const files = (count: number) => ({
  role: "user",
  content: [
    { type: "text", text: "see" },
    ...Array.from({ length: count }, () => ({ type: "file", id: "a" })),
  ],
});

beforeEach(() => {
  for (const mock of Object.values(engine)) {
    mock.mockReset();
  }
  assertThreadCreatable.mockReset().mockResolvedValue(undefined);
  readStoredProfile.mockReset().mockResolvedValue({ id: "p1", providerType: "openai" });
  readThread.mockReset().mockResolvedValue({ id: "t1", profileId: "p-thread" });
  setRoundProfileId.mockReset();
  setRoundThreadId.mockReset();
});

// Both stored rounds check the same things before the stream opens.
describe.each(["sendWithStream", "sendWithStreamOpenAI"] as const)("%s pre-flight", (name) => {
  it("refuses an empty user message", async () => {
    expect((await run(name, { threadId: "t1", userMessage: text("  ") })).status).toBe(400);
    expect(engine[name]).not.toHaveBeenCalled();
  });

  it("refuses more attachments of a kind than the composer allows", async () => {
    expect((await run(name, { threadId: "t1", userMessage: files(6) })).status).toBe(400);
    expect(engine[name]).not.toHaveBeenCalled();
  });

  it("gates the thread a round without a threadId creates", async () => {
    assertThreadCreatable.mockRejectedValue(
      Object.assign(new Error("no"), { status: 403, expose: true }),
    );
    const outcome = await run(name, { entityId: "12", profileId: "p1", userMessage: text("hi") });
    expect(assertThreadCreatable).toHaveBeenCalledWith("12", "p1");
    expect(outcome.status).toBe(403);
    expect(engine[name]).not.toHaveBeenCalled();
  });

  it("checks the thread's own model when the request names none", async () => {
    readStoredProfile.mockResolvedValue(undefined);
    const outcome = await run(name, { threadId: "t1", userMessage: text("hi") });
    expect(readStoredProfile).toHaveBeenCalledWith("p-thread");
    expect(setRoundProfileId).toHaveBeenCalledWith("p-thread");
    expect(outcome.status).toBe(400);
    expect(engine[name]).not.toHaveBeenCalled();
  });
});

// The round the composer sends: nothing in the pre-flight stops it.
describe.each(["sendWithStream", "sendWithStreamOpenAI"] as const)("%s of the composer", (name) => {
  it.each([
    [
      "a message in an existing thread",
      { threadId: "t1", profileId: "p1", userMessage: text("hi") },
    ],
    ["the first message of a new chat", { profileId: "p1", userMessage: text("hi") }],
    ["a message with five files and no profile named", { threadId: "t1", userMessage: files(5) }],
  ])("runs %s", async (_label, body) => {
    engine[name].mockReturnValue((async function* () {})());
    const res = {
      status: vi.fn(),
      json: vi.fn(),
      on: vi.fn(),
      once: vi.fn(),
      off: vi.fn(),
      removeListener: vi.fn(),
      setHeader: vi.fn(),
      flushHeaders: vi.fn(),
      write: vi.fn(() => true),
      end: vi.fn(),
      writableEnded: false,
      headersSent: false,
      socket: { setNoDelay: vi.fn(), setTimeout: vi.fn() },
    };
    res.status.mockReturnValue(res);
    const next = vi.fn();
    await (aiController[name] as unknown as Handler)({ body, query: {}, on: vi.fn() }, res, next);
    expect(next).not.toHaveBeenCalled();
    expect(res.status).not.toHaveBeenCalledWith(400);
    expect(engine[name]).toHaveBeenCalledTimes(1);
  });
});

describe("sendCustom", () => {
  it("refuses a profileId that names no profile instead of running on another model", async () => {
    readStoredProfile.mockResolvedValue(undefined);
    const outcome = await run("sendCustom", {
      systemPrompt: "be brief",
      userMessage: text("hi"),
      profileId: "gone",
    });
    expect(outcome.status).toBe(400);
    expect(engine.sendCustom).not.toHaveBeenCalled();
  });
});

describe("send", () => {
  it("hands the engine a signal that aborts when the client goes away", async () => {
    engine.send.mockResolvedValue({ text: "ok" });
    const { res } = await run("send", { actionType: "Chat", userMessage: text("hi") });
    const sent = engine.send.mock.calls[0]?.[0] as { actionArgs: { signal: AbortSignal } };
    expect(sent.actionArgs.signal.aborted).toBe(false);
    const onClose = res.on.mock.calls.find(([event]) => event === "close")?.[1] as () => void;
    onClose();
    expect(sent.actionArgs.signal.aborted).toBe(true);
  });
});

describe("regenerateStream", () => {
  it("runs the document agent on the thread's own model when the request names none", async () => {
    engine.regenerateStream.mockReturnValue((async function* () {})());
    await run("regenerateStream", { threadId: "t1", messageId: "m1" });
    expect(setRoundProfileId).toHaveBeenCalledWith("p-thread");
    expect(setRoundThreadId).toHaveBeenCalledWith("t1");
  });
});

// The entry a round is billed to: where the user chats. Inside an AI project
// that is the project, not the folder the project context set as the round's
// location (the picked folder, else My documents); elsewhere the folder or
// room the user is in, and a composer-picked context folder only without one.
const { primeSourceMeta } = await import("../../storage/docspaceFilesApi.js");
const { contextFolderIdsOf } = await import("../../storage/contextFolders.js");

describe("source attribution", () => {
  const prime = vi.mocked(primeSourceMeta);
  const pickedFolders = vi.mocked(contextFolderIdsOf);

  beforeEach(() => {
    prime.mockClear();
    pickedFolders.mockReset().mockReturnValue([]);
  });

  it("bills a round in an AI project to the project, not to its folder", async () => {
    pickedFolders.mockReturnValue(["555"]);
    await run("sendWithStream", {
      threadId: "t1",
      entityId: "42",
      contextEntityId: "4",
      userMessage: text("hi"),
    });
    expect(prime).toHaveBeenCalledWith("42");
  });

  it("bills a round outside a project to the room the user is in", async () => {
    pickedFolders.mockReturnValue(["555"]);
    await run("sendWithStream", { threadId: "t1", contextEntityId: "10", userMessage: text("hi") });
    expect(prime).toHaveBeenCalledWith("10");
  });

  it("falls back to a picked context folder only when the round has no location", async () => {
    pickedFolders.mockReturnValue(["555"]);
    await run("sendWithStream", { threadId: "t1", userMessage: text("hi") });
    expect(prime).toHaveBeenCalledWith("555");
  });
});
