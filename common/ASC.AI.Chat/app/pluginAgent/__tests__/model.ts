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

import { custom } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import { vi } from "vitest";

export const PROFILE: Profile = {
  id: "p-chat",
  name: "Chat",
  providerType: "openai",
  baseUrl: "https://api.example.com/v1",
  modelId: "gpt-x",
};

export interface ScriptedCall {
  name: string;
  args?: Record<string, unknown>;
}

/** A turn the provider failed, as the engine's providers end it: no throw, an incomplete message. */
export const PROVIDER_ERROR = Symbol("provider error");

/** One turn of a scripted model: an answer in words, the tools it calls, or a failed request. */
export type ScriptedTurn = string | ScriptedCall[] | typeof PROVIDER_ERROR;

let callIds = 0;

const messageOf = (turn: ScriptedTurn): ThreadMessageLike =>
  turn === PROVIDER_ERROR
    ? ({
        role: "assistant",
        content: "",
        status: { type: "incomplete", reason: "error", error: "429 rate limited" },
      } as ThreadMessageLike)
    : typeof turn === "string"
      ? { role: "assistant", content: [{ type: "text", text: turn }] }
      : ({
          role: "assistant",
          content: turn.map((call) => ({
            type: "tool-call",
            toolCallId: `call-${++callIds}`,
            toolName: call.name,
            args: call.args ?? {},
          })),
        } as ThreadMessageLike);

/**
 * Makes the mocked `custom` action answer the given turns in order, one per call, and records
 * the history each call was made with. A turn past the script answers "done talking".
 * The file that uses it mocks `custom` of `@onlyoffice/ai-chat/core` with `vi.fn()`.
 */
export function scriptModel(turns: ScriptedTurn[]): { histories: ThreadMessageLike[][] } {
  const histories: ThreadMessageLike[][] = [];
  let turn = 0;
  vi.mocked(custom).mockImplementation(async function* (args) {
    histories.push(args.messages);
    const responseMessage = messageOf(turns[turn++] ?? "done talking");
    // A delta first, as the action streams it, then the completed turn.
    yield { role: "assistant", content: "" } as never;
    yield "not an object" as never;
    yield { isEnd: true, responseMessage } as never;
  });
  return { histories };
}

/** The results the model was shown for the tool calls of a history, in order. */
export function resultsIn(history: ThreadMessageLike[]): unknown[] {
  const results: unknown[] = [];
  for (const message of history) {
    if (Array.isArray(message.content)) {
      for (const part of message.content) {
        if (part.type === "tool-call") {
          results.push(part.result);
        }
      }
    }
  }
  return results;
}
