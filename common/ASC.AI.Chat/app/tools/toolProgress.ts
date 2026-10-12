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

// Live progress of a long server-side tool. The engine yields nothing while
// an in-engine tool runs, so a delegated task (the document agent, minutes of
// work) is a silent card until it ends. This wraps the engine's event stream:
// while the engine is quiet, lines a tool reports through the request
// context become `message-delta` events whose message is the round's
// assistant message with a text part of the work so far after the pending
// tool call. The widget renders a delta as the message's current state, so
// the work shows up live under the card; the engine's own events then take
// over with the real result, and the text part, never persisted, goes away.

import type { ChatEvent } from "@onlyoffice/ai-chat/core";
import { isObject } from "../narrow.js";
import { setToolProgressSink } from "../requestContext.js";

type StreamMessage = Extract<ChatEvent, { type: "message-delta" }>["message"];

/** The message the progress is drawn on: the round's assistant message as last seen. */
export interface ProgressBase {
  message: StreamMessage;
  messageId: string;
}

const MAX_LINES = 40;

/**
 * Base from a request body that carries the pending tool call (approve /
 * deny): the client sends back the assistant message with the call.
 */
export function progressBaseOf(body: unknown): ProgressBase | undefined {
  if (!isObject(body)) {
    return undefined;
  }
  const message = body["message"];
  const messageId = body["messageId"];
  if (!isObject(message) || typeof messageId !== "string" || messageId.length === 0) {
    return undefined;
  }
  return { message: message as StreamMessage, messageId };
}

// The message with the work so far as one text part after the last tool-call
// part (the one being executed); the tool card itself stays pending.
function withProgress(base: ProgressBase, lines: readonly string[]): StreamMessage {
  const content: unknown = base.message.content;
  const parts: unknown[] =
    typeof content === "string"
      ? [{ type: "text", text: content }]
      : Array.isArray(content)
        ? [...content]
        : [];
  let at = parts.length;
  for (let i = parts.length - 1; i >= 0; i--) {
    const part = parts[i];
    if (isObject(part) && part["type"] === "tool-call") {
      at = i + 1;
      break;
    }
  }
  // A markdown list: one item per step, the steps stay apart when rendered.
  const text = lines.map((line) => `- ${line}`).join("\n");
  parts.splice(at, 0, { type: "text", text });
  return { ...base.message, content: parts } as StreamMessage;
}

/**
 * The engine's events, with progress deltas interleaved while the engine is
 * quiet. The base message comes from `initial` (a request that carries the
 * pending call) and is updated from every engine event that carries one, so
 * a tool the engine runs on its own after a round of streaming draws on the
 * message of that round.
 */
export async function* withToolProgress(
  stream: AsyncIterable<ChatEvent>,
  initial: ProgressBase | undefined,
): AsyncGenerator<ChatEvent, void, undefined> {
  let base = initial;
  const lines: string[] = [];
  // Lines arrive from the tool; one waiter at a time takes the next one.
  const pending: string[] = [];
  let wake: (() => void) | undefined;
  setToolProgressSink((line) => {
    pending.push(line);
    wake?.();
  });
  const nextLine = (): Promise<void> =>
    new Promise((resolve) => {
      wake = () => {
        wake = undefined;
        resolve();
      };
    });

  const iterator = stream[Symbol.asyncIterator]();
  try {
    let next = iterator.next();
    for (;;) {
      // Drain what the tool reported, then wait for either side.
      if (pending.length > 0 && base) {
        lines.push(...pending.splice(0, pending.length));
        if (lines.length > MAX_LINES) {
          lines.splice(0, lines.length - MAX_LINES);
        }
        yield {
          type: "message-delta",
          message: withProgress(base, lines),
          messageId: base.messageId,
        };
        continue;
      }
      pending.length = 0;
      const outcome = await Promise.race([
        next.then((result) => ({ result })),
        nextLine().then(() => ({ result: undefined })),
      ]);
      if (outcome.result === undefined) {
        continue;
      }
      const { done, value } = outcome.result;
      if (done) {
        return;
      }
      // The engine spoke: its message is the truth from here, the work so far is over.
      lines.length = 0;
      const seen = progressBaseOf(value);
      if (seen) {
        base = seen;
      }
      yield value;
      next = iterator.next();
    }
  } finally {
    setToolProgressSink(undefined);
    wake = undefined;
    await iterator.return?.().catch(() => undefined);
  }
}
