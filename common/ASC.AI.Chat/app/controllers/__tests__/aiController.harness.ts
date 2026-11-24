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

// What the aiController tests share: the stand-ins for the modules the controller talks to
// (the engine class, the storage, the chat-context aggregate, the project binding, the context
// folders, the tools prompt, the source attribution, the thread gate), a caller that runs a
// handler inside the real request context, and generators to stand for the engine's streams.
// The test files hand these to `vi.mock` from their factories, so the spies here are the ones
// the controller calls.

import { EventEmitter } from "node:events";
import type { RequestHandler, Response } from "express";
import { createRequest, createResponse } from "node-mocks-http";
import type { MockResponse } from "node-mocks-http";
import type { AIEngine } from "@onlyoffice/ai-chat/core";
import { vi } from "vitest";
import { engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { requestContextMiddleware, setChatContextSnapshot } from "../../requestContext.js";
import type { ChatContextSnapshot } from "../../storage/chatContextSnapshot.js";

export const engine = engineMock<AIEngine>();

/** The dependencies each `new AIEngine(deps)` was given, in order. */
export const engineDeps: unknown[] = [];

export class FakeAIEngine {
  // `vi.resetModules` keeps the results of the mock factories, so a test that loads the
  // controller again reaches the list through the class the factory handed out.
  static readonly deps = engineDeps;

  constructor(deps: unknown) {
    engineDeps.push(deps);
    return engine;
  }
}

export const storage = {
  threads: { readById: vi.fn() },
  messages: { readByThread: vi.fn() },
  profiles: { readById: vi.fn() },
};

/** What the next `primeChatContext` puts on the request context, when anything. */
export const chat: { snapshot: Partial<ChatContextSnapshot> | undefined } = { snapshot: undefined };

export const primeChatContext = vi.fn();
export const describeChatContextUsage = vi.fn();
export const applyAiProjectChatContext = vi.fn();
export const resolveContextFolders = vi.fn();
export const resolveLocation = vi.fn();
export const primeCustomServers = vi.fn();
export const safeGetToolsPrompt = vi.fn();
export const primeSourceMeta = vi.fn();
export const assertThreadCreatable = vi.fn();
export const getServerTypes = vi.fn();

/** Puts every stand-in back to the answer of a plain round that nothing refuses. */
export function resetHarness(): void {
  mockReset(engine);
  chat.snapshot = undefined;
  storage.threads.readById.mockReset().mockResolvedValue(null);
  storage.messages.readByThread.mockReset().mockResolvedValue([]);
  storage.profiles.readById.mockReset().mockResolvedValue({ id: "p1", providerType: "openai" });
  primeChatContext.mockReset().mockImplementation(async () => {
    if (chat.snapshot) {
      setChatContextSnapshot(chat.snapshot as ChatContextSnapshot);
    }
  });
  describeChatContextUsage.mockReset().mockReturnValue("chatContext=hit");
  applyAiProjectChatContext.mockReset().mockResolvedValue(null);
  resolveContextFolders.mockReset().mockResolvedValue([]);
  resolveLocation.mockReset().mockImplementation(async (id: string) => ({ id }));
  primeCustomServers.mockReset().mockResolvedValue(undefined);
  safeGetToolsPrompt.mockReset().mockResolvedValue("");
  primeSourceMeta.mockReset().mockResolvedValue(undefined);
  assertThreadCreatable.mockReset().mockResolvedValue(undefined);
  getServerTypes.mockReset().mockReturnValue([]);
}

export interface Reply {
  status: number;
  body: unknown;
  headers: Record<string, unknown>;
  ended: boolean;
  nextError: unknown;
}

export interface Started {
  res: MockResponse<Response>;
  done: Promise<Reply>;
}

/**
 * Starts one handler inside the real request context, as the app's middleware sets it up, and
 * hands back the response before the handler settles: a test can close it mid-stream.
 */
export function start(
  handler: unknown,
  body: unknown,
  headers: Record<string, string> = {},
): Started {
  const req = createRequest({
    method: "POST",
    url: "/api/2.0/ai/ai/route",
    body: body as never,
    headers,
  });
  // node-mocks-http turns a missing or null body into `{}`; the JSON parser of the app runs
  // with `strict: false` and hands a handler `null`, a string or a number as they came.
  req.body = body;
  const res = createResponse<Response>({ eventEmitter: EventEmitter, req });
  let nextError: unknown;
  let run: Promise<unknown> = Promise.resolve();
  requestContextMiddleware(req, res, () => {
    const returned: unknown = (handler as RequestHandler)(req, res, (err?: unknown) => {
      nextError = err;
    });
    // `_helpers` is loaded late: it loads the chat library, whose engine classes the test
    // files replace with `vi.mock`.
    run = Promise.resolve(returned)
      .then(() => import("../_helpers.js"))
      .then(({ handlerSettled }) => handlerSettled(res));
  });
  const done = run.then(() => ({
    status: res.statusCode,
    body: res._isJSON() ? res._getJSONData() : res._getData(),
    headers: res._getHeaders(),
    ended: res._isEndCalled(),
    nextError,
  }));
  return { res, done };
}

export function call(
  handler: unknown,
  body: unknown,
  headers: Record<string, string> = {},
): Promise<Reply> {
  return start(handler, body, headers).done;
}

/**
 * A generator the test steers: it yields `before`, waits for `release()`, then yields `after`
 * and ends. `pulls` counts the events it was asked for, the wait included.
 */
export function heldStream(
  before: unknown[],
  after: unknown[] = [],
): {
  stream: AsyncGenerator<never>;
  release: () => void;
  pulls: () => number;
} {
  let release: () => void = () => {};
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  let count = 0;
  async function* run(): AsyncGenerator<never> {
    for (const event of before) {
      count += 1;
      yield event as never;
    }
    count += 1;
    await gate;
    for (const event of after) {
      yield event as never;
      count += 1;
    }
  }
  return { stream: run(), release, pulls: () => count };
}

export async function* streamOf(...events: unknown[]): AsyncGenerator<never> {
  for (const event of events) {
    yield event as never;
  }
}

export async function* failingStream(error: unknown, ...before: unknown[]): AsyncGenerator<never> {
  for (const event of before) {
    yield event as never;
  }
  throw error;
}

/** The body the engine method `name` was called with the first time. */
export function engineInput(name: keyof AIEngine): Record<string, unknown> {
  const spy = engine[name] as unknown as { mock: { calls: unknown[][] } };
  const first = spy.mock.calls[0];
  if (!first) {
    throw new Error(`engine.${String(name)} was not called`);
  }
  return first[0] as Record<string, unknown>;
}
