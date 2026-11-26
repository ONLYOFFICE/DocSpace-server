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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

// The loop of a plugin task: the model is asked, the tools it calls are run, their results go
// back to it, until a tool says that the task is done. Hand-driven over the bare `custom`
// action, as `formAnalysisSubAgent` is: the engine's own loop is bound to a chat thread.
//
// What a task of many minutes needs on top of that: a budget of tool calls, a word to a model
// that stops to ask a question nobody will answer, and a history folded into a summary before
// it outgrows the model's context window.

import { compactHistory, custom } from "@onlyoffice/ai-chat/core";
import type { Profile, TMCPItem } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";

import type { CompactionConfig } from "../../config/index.js";
import { extractText } from "../forms/suggestedQuestions.js";
import logger from "../log.js";
import { isObject } from "../narrow.js";
import { attachResults, extractToolCalls } from "../tools/formAnalysisSubAgent.js";

/** How many turns in a row a model that answered without a tool is told to go on, before the task ends. */
export const MAX_NUDGES = 2;
export const NUDGE =
  "Nobody can answer you: this task runs without a person. Decide yourself, make reasonable " +
  "assumptions, and go on until the plugin is handed over with deliver_plugin.";
/** What the model reads for a tool that answered nothing: a result is never empty. */
export const NO_OUTPUT = "(no output)";

/**
 * The model's provider failed a turn. The engine's providers do not throw for it: the turn ends
 * as an assistant message that is empty and marked incomplete, which read as an answer would be
 * a model that stopped talking.
 */
export class ModelTurnError extends Error {
  constructor() {
    super("the model's provider failed the turn");
    this.name = "ModelTurnError";
  }
}

// The library's own defaults, for a knob the installation leaves alone and a model whose window
// neither the profile nor the installation states.
const DEFAULT_THRESHOLD = 0.8;
const DEFAULT_KEEP_RECENT = 4;
const DEFAULT_CONTEXT_WINDOW = 128_000;

/** What one tool call comes to: text for the model, and the task's result when it ends the task. */
export interface ToolOutcome<T> {
  text: string;
  done?: T;
}

export interface LoopOptions<T> {
  profile: Profile;
  systemPrompt: string;
  task: string;
  tools: TMCPItem[];
  maxToolCalls: number;
  compaction: CompactionConfig;
  signal: AbortSignal;
  callTool: (name: string, args: Record<string, unknown>) => Promise<ToolOutcome<T>>;
}

export type LoopEnd<T> =
  | { kind: "done"; value: T; toolCalls: number }
  /** The budget of tool calls was spent before a tool ended the task. */
  | { kind: "budget"; toolCalls: number }
  /** The model kept answering without calling a tool; `text` is its last answer. */
  | { kind: "stopped"; text: string; toolCalls: number };

async function runTurn<T>(
  options: LoopOptions<T>,
  messages: ThreadMessageLike[],
): Promise<ThreadMessageLike> {
  let final: ThreadMessageLike = { role: "assistant", content: "" };
  const stream = custom({
    profile: options.profile,
    messages,
    tools: options.tools,
    systemPrompt: options.systemPrompt,
    signal: options.signal,
  });
  for await (const event of stream) {
    if (!isObject(event)) {
      continue;
    }
    // A completed turn arrives as `{ isEnd, responseMessage }`; the deltas are plain messages.
    const responseMessage = event["responseMessage"];
    final = event["isEnd"] === true && isObject(responseMessage) ? responseMessage : event;
  }
  const status: unknown = final.status;
  if (isObject(status) && status["type"] === "incomplete" && status["reason"] === "error") {
    // What the provider said stays in the engine's own log; the message may carry its body.
    throw new ModelTurnError();
  }
  return final;
}

/** The history to send next: folded when it nears the window, as it is otherwise. */
async function compacted<T>(
  options: LoopOptions<T>,
  messages: ThreadMessageLike[],
): Promise<ThreadMessageLike[]> {
  if (!options.compaction.enabled) {
    return messages;
  }
  try {
    const result = await compactHistory(
      { profile: options.profile, messages, signal: options.signal },
      {
        contextWindow:
          options.profile.contextWindow ??
          options.compaction.defaultContextWindow ??
          DEFAULT_CONTEXT_WINDOW,
        threshold: options.compaction.threshold ?? DEFAULT_THRESHOLD,
        keepRecent: options.compaction.keepRecent ?? DEFAULT_KEEP_RECENT,
      },
    );
    return result ? result.messages : messages;
  } catch (err) {
    if (options.signal.aborted) {
      throw err;
    }
    // The summary is a help, not a step of the task: without it the history goes on as it is.
    logger.warn(`pluginAgent: the history was not compacted: ${String(err)}`);
    return messages;
  }
}

/**
 * Runs the loop to its end. Rejects with the signal's reason when it fires, with a
 * {@link ModelTurnError} when the provider fails a turn, and with whatever a tool throws; every
 * other end is a {@link LoopEnd}.
 */
export async function runLoop<T>(options: LoopOptions<T>): Promise<LoopEnd<T>> {
  let messages: ThreadMessageLike[] = [{ role: "user", content: options.task }];
  let toolCalls = 0;
  let nudges = 0;

  for (;;) {
    options.signal.throwIfAborted();
    const message = await runTurn(options, messages);
    options.signal.throwIfAborted();
    const calls = extractToolCalls(message);

    if (calls.length === 0) {
      if (nudges === MAX_NUDGES) {
        return { kind: "stopped", text: extractText(message), toolCalls };
      }
      nudges += 1;
      messages = [...messages, message, { role: "user", content: NUDGE }];
      continue;
    }
    // The count is of turns in a row without a tool: a model that goes on working starts anew.
    nudges = 0;

    const results = new Map<string, unknown>();
    for (const call of calls) {
      if (toolCalls === options.maxToolCalls) {
        return { kind: "budget", toolCalls };
      }
      toolCalls += 1;
      const outcome = await options.callTool(call.name, call.args);
      if (outcome.done !== undefined) {
        return { kind: "done", value: outcome.done, toolCalls };
      }
      // A provider attaches a result to its call only when there is one: an empty text would
      // leave the call without an answer, and the next request refused.
      results.set(call.id, outcome.text === "" ? NO_OUTPUT : outcome.text);
    }
    messages = await compacted(options, [...messages, attachResults(message, results)]);
  }
}
