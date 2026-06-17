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

import type { Response } from "express";
import { AIEngine, composeToolsAdapters } from "@onlyoffice/ai-chat/core";
import type {
  SendInput,
  SendCustomInput,
  SendStreamInput,
  RegenerateStreamInput,
  ApproveToolCallInput,
  DenyToolCallInput,
} from "@onlyoffice/ai-chat/core";
import logger from "../log.js";
import { markForwardHeadersToProvider } from "../requestContext.js";
import { storage } from "../storage/index.js";
import { asyncHandler, streamNdjson, streamOpenAiSse } from "./_helpers.js";
import { isObject } from "../narrow.js";
import {
  HttpToolsAdapter,
  safeGetToolsPrompt,
  DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
} from "../tools/httpToolsAdapter.js";
import { systemToolsSource } from "../tools/systemTools.js";

// Client-side code passes `actionArgs.signal: AbortSignal` so it can
// cancel an in-flight stream. Going through JSON the signal collapses
// to `{}` — provider SDKs then crash with
// `signal.addEventListener is not a function`. Replace it with a real
// AbortSignal wired to the HTTP request so client disconnects abort
// the upstream call too.
function responseAbortSignal(res: Response): AbortSignal {
  const controller = new AbortController();
  // Fires both on early client disconnect and on normal response end.
  // Use `writableEnded` to filter out the normal-completion case so we
  // only propagate real cancellations to the upstream provider.
  res.on("close", () => {
    if (!res.writableEnded && !controller.signal.aborted) {
      controller.abort();
    }
  });
  return controller.signal;
}

function withRequestSignal<T>(res: Response, body: T): T {
  if (!isObject(body)) {
    return body;
  }
  const actionArgs = body["actionArgs"];
  if (!isObject(actionArgs)) {
    return body;
  }
  return {
    ...body,
    actionArgs: { ...actionArgs, signal: responseAbortSignal(res) },
  };
}

// Fold the DocSpace tools system-prompt fragment into the action's prompt
// override. The fragment ships alongside the tool list from
// `tools/list`; the engine's own `getTools` reuses the cached fetch, so
// this adds no extra round-trip. An existing client override is kept and
// the fragment appended to its text; otherwise an `append` override is set.
// Append `fragment` to the action's system-prompt override (the engine
// folds it onto the baked-in default via `resolveSystemPrompt`). Keeps an
// existing override's text and adds the fragment after a blank line;
// otherwise sets a fresh `append` override.
function appendActionPrompt<T>(body: T, fragment: string): T {
  if (!isObject(body) || !fragment) {
    return body;
  }
  const actionArgs = isObject(body["actionArgs"]) ? body["actionArgs"] : {};
  const existing = actionArgs["prompt"];
  const prompt =
    isObject(existing) && typeof existing["text"] === "string"
      ? { ...existing, text: `${existing["text"] as string}\n\n${fragment}` }
      : { mode: "append", text: fragment };
  return {
    ...body,
    actionArgs: { ...actionArgs, prompt },
  } as T;
}

async function withToolsPrompt<T>(body: T): Promise<T> {
  if (!isObject(body)) {
    return body;
  }
  const entityId =
    typeof body["entityId"] === "string" ? body["entityId"] : undefined;
  const fragment = await safeGetToolsPrompt(toolsAdapter, entityId);
  return fragment ? appendActionPrompt(body, fragment) : body;
}

// Build the context fragment that tells the agent where it is operating and
// how to scope tool calls. The workspace lines are emitted only when an
// `entityId` (the agent/room scope) is present.
function buildContextFragment(entityId: string | undefined): string {
  const today = new Date().toISOString().slice(0, 10);
  const lines = [
    "Context:",
    "- You are an AI agent operating inside a workspace, not a generic standalone assistant.",
  ];
  if (entityId) {
    lines.push(
      `- This conversation is scoped to a single agent workspace (id: ${entityId}). Any tool you call reads or modifies data within the current user's workspace, on their behalf and with their permissions — never assume access beyond that scope.`,
      `- When a tool needs a workspace, folder, or scope identifier and the user did not specify one, use this workspace id (${entityId}) — e.g. scope searches and listings to it.`,
    );
  }
  lines.push(
    `- Today's date is ${today}.`,
    "- Prefer answering from the conversation when you can; use tools only when the request clearly needs data or actions in the workspace.",
  );
  return lines.join("\n");
}

function withContextPrompt<T>(body: T): T {
  if (!isObject(body)) {
    return body;
  }
  const entityId =
    typeof body["entityId"] === "string" ? body["entityId"] : undefined;
  return appendActionPrompt(body, buildContextFragment(entityId));
}

const toolsAdapter = new HttpToolsAdapter();
const engine = new AIEngine({
  storage,
  // System (host-configured MCP) tools run server-side and pause for UI
  // approval; most DocSpace integration tools run silently. Compose both.
  toolsAdapter: composeToolsAdapters(systemToolsSource, toolsAdapter),
  // Approval-required server types: the MCP servers plus the DocSpace
  // integration tools explicitly grouped under the approval serverType
  // (e.g. document/presentation/form generation).
  systemServerTypes: () => [
    ...systemToolsSource.getServerTypes(),
    DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
  ],
});

function isAsyncIterable(value: unknown): value is AsyncIterable<unknown> {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  if (!(Symbol.asyncIterator in value)) {
    return false;
  }
  return typeof Reflect.get(value, Symbol.asyncIterator) === "function";
}

function describeChatError(error: unknown): string {
  if (!isObject(error)) {
    return String(error);
  }
  const parts: string[] = [];
  if (typeof error["code"] === "string") {
    parts.push(`code=${error["code"]}`);
  }
  if (typeof error["message"] === "string") {
    parts.push(`message=${error["message"]}`);
  }
  if (isObject(error["cause"])) {
    parts.push(`cause=(${describeChatError(error["cause"])})`);
  }
  return parts.length > 0 ? parts.join(", ") : JSON.stringify(error);
}

// Describe a `tool-call-pending` event for the server log: the tool name
// lives in the tool-call content part at `event.idx`, alongside the
// approval flags. A hanging tool surfaces here (we see which tool was
// requested) but never reaches `message-end` — the absence of a
// "stream completed" line for the same route is the tell.
function describeToolCallPending(event: Record<string, unknown>): string {
  const idx = typeof event["idx"] === "number" ? event["idx"] : -1;
  const message = isObject(event["message"]) ? event["message"] : null;
  const content =
    message && Array.isArray(message["content"]) ? message["content"] : [];
  const part = idx >= 0 && isObject(content[idx]) ? content[idx] : null;
  const toolName =
    part && typeof part["toolName"] === "string" ? part["toolName"] : "?";
  const toolCallId =
    part && typeof part["toolCallId"] === "string" ? part["toolCallId"] : "?";
  return `tool=${toolName} callId=${toolCallId} idx=${idx} serverExecuted=${
    event["serverExecuted"] === true
  } autoAllow=${event["autoAllow"] === true}`;
}

// Scan an event's streamed message for tool-call content parts and record
// each tool's name against whether its `result` is populated yet. Adapter
// tools (web search, image generation, DocSpace) execute inside the engine
// and never surface a `tool-call-pending` to this tap — the only trace they
// leave here is the tool-call part inside the assistant message, gaining a
// `result` once the engine ran it. So `executed=true` in the completion
// summary is the positive proof a tool actually fired and returned.
function trackToolCalls(
  event: unknown,
  seen: Map<string, boolean>,
): void {
  if (!isObject(event)) {
    return;
  }
  const message = isObject(event["message"]) ? event["message"] : null;
  const content = message && Array.isArray(message["content"]) ? message["content"] : [];
  for (const part of content) {
    if (!isObject(part) || part["type"] !== "tool-call") {
      continue;
    }
    const name = typeof part["toolName"] === "string" ? part["toolName"] : "?";
    const hasResult = part["result"] !== undefined && part["result"] !== null;
    // Latch to true: a later delta without the result must not flip it back.
    seen.set(name, (seen.get(name) ?? false) || hasResult);
  }
}

// Sniff stream events to surface provider failures in server logs. The
// engine emits `message-incomplete` with `status.reason === "error"` and a
// `ChatErrorPayload` instead of throwing, so without this tap the cause of
// a broken stream is invisible from the server side — it only shows up in
// the browser DevTools network tab. Also traces tool-call lifecycle so a
// tool that hangs (pending surfaced, stream never completes) is visible.
async function* logStreamErrors<T>(
  route: string,
  iter: AsyncIterable<T>,
): AsyncIterable<T> {
  let eventCount = 0;
  const toolCalls = new Map<string, boolean>();
  try {
    for await (const event of iter) {
      eventCount += 1;
      trackToolCalls(event, toolCalls);
      if (isObject(event) && event["type"] === "message-incomplete") {
        const message = isObject(event["message"]) ? event["message"] : null;
        const status = message && isObject(message["status"]) ? message["status"] : null;
        if (status && status["reason"] === "error") {
          logger.warn(
            `${route}: provider returned message-incomplete — ${describeChatError(status["error"])}`,
          );
        } else if (status) {
          logger.warn(
            `${route}: message-incomplete reason=${String(status["reason"])}`,
          );
        }
      } else if (isObject(event) && event["type"] === "tool-call-pending") {
        logger.info(
          `${route}: tool-call-pending — ${describeToolCallPending(event)}`,
        );
      }
      yield event;
    }
    const toolSummary =
      toolCalls.size > 0
        ? [...toolCalls]
            .map(([name, executed]) => `${name}(executed=${executed})`)
            .join(", ")
        : "<none>";
    logger.info(
      `${route}: stream completed after ${eventCount} event(s); toolCalls=${toolSummary}`,
    );
  } catch (err) {
    logger.error(
      `${route}: stream threw after ${eventCount} event(s): ${
        err instanceof Error ? `${err.message}\n${err.stack}` : String(err)
      }`,
    );
    throw err;
  }
}

export const aiController = {
  send: asyncHandler<SendInput>(async (req, res) => {
    const result = await engine.send(req.body);
    res.json(result);
  }),

  sendCustom: asyncHandler<SendCustomInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    const result = engine.sendCustom(body);
    if (body.isStream) {
      if (isAsyncIterable(result)) {
        await streamNdjson(res, logStreamErrors("ai/send-custom", result));
      } else {
        res.json(await result);
      }
      return;
    }
    if (isAsyncIterable(result)) {
      await streamNdjson(res, logStreamErrors("ai/send-custom", result));
      return;
    }
    res.json(await result);
  }),

  sendWithStream: asyncHandler<SendStreamInput>(async (req, res) => {
    markForwardHeadersToProvider();
    const body = withContextPrompt(await withToolsPrompt(withRequestSignal(res, req.body)));
    await streamNdjson(
      res,
      logStreamErrors("ai/send-with-stream", engine.sendWithStream(body)),
    );
  }),

  // OpenAI-compatible streaming chat: same input as sendWithStream, but the
  // engine emits OpenAI `chat.completion.chunk` objects (and a native
  // OpenAI error envelope on provider failure), which we frame as SSE.
  sendWithStreamOpenAI: asyncHandler<SendStreamInput>(async (req, res) => {
    markForwardHeadersToProvider();
    const body = withContextPrompt(await withToolsPrompt(withRequestSignal(res, req.body)));
    await streamOpenAiSse(
      res,
      logStreamErrors("ai/send-with-stream-openai", engine.sendWithStreamOpenAI(body)),
    );
  }),

  regenerateStream: asyncHandler<RegenerateStreamInput>(async (req, res) => {
    const body = await withToolsPrompt(withRequestSignal(res, req.body));
    await streamNdjson(
      res,
      logStreamErrors("ai/regenerate-stream", engine.regenerateStream(body)),
    );
  }),

  approveToolCall: asyncHandler<ApproveToolCallInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    await streamNdjson(
      res,
      logStreamErrors("ai/approve-tool-call", engine.approveToolCall(body)),
    );
  }),

  denyToolCall: asyncHandler<DenyToolCallInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    await streamNdjson(
      res,
      logStreamErrors("ai/deny-tool-call", engine.denyToolCall(body)),
    );
  }),
};
