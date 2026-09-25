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

// The form-analysis sub-agent: the main chat model gets one tool, `analyze_form`, while the raw
// form-data tools are withheld from it (C# gates them on `FormSubAgent`). Every question about the
// submissions is delegated here and answered by the FormAnalysis model — not the possibly-weak chat
// model — via a hand-driven loop over the bare `custom` action (the engine's own agentic loop is
// thread-bound, so we run the tools and feed results back ourselves until the model answers).

import { custom } from "@onlyoffice/ai-chat/core";
import type { Profile, TMCPItem, ToolsAdapter } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";

import { storage } from "../storage/index.js";
import { HttpToolsAdapter } from "./httpToolsAdapter.js";
import { getResolvedAttachmentId, markForwardHeadersToProvider } from "../requestContext.js";
import { resolveFormsProfile, extractText } from "../forms/suggestedQuestions.js";
import { getString, isObject } from "../narrow.js";
import { FORM_ANALYSIS_TYPE } from "../../config/index.js";
import logger from "../log.js";

const TOOL_NAME = "analyze_form";
// Own serverType so composeToolsAdapters keeps this apart from the DocSpace tool groups.
const SERVER_TYPE = FORM_ANALYSIS_TYPE;
// Runaway-loop guard, not the normal stop (the model ends by answering without a tool call);
// 10 matches common agent defaults (LangChain 15, OpenAI Agents 10).
const MAX_ROUNDS = 20;

const ANALYZE_FORM_TOOL: TMCPItem = {
  name: TOOL_NAME,
  description:
    "Analyze the attached form's submitted data and answer a question about it. Pass the user's " +
    "question verbatim, in natural language; returns a text answer.",
  inputSchema: {
    type: "object",
    properties: {
      question: {
        type: "string",
        description: "The user's question about the form's submissions, in natural language.",
      },
    },
    required: ["question"],
  },
  serverType: SERVER_TYPE,
  requireApproval: false,
  enabled: true,
};

// Flagged so C# emits ONLY the form-data tools (and their prompt) for this sub-agent, never for main.
const formDataTools = new HttpToolsAdapter(true);

type ToolCall = { id: string; name: string; args: Record<string, unknown> };

export const formAnalysisSubAgent: ToolsAdapter = {
  async getTools(): Promise<Record<string, TMCPItem[]>> {
    const attachmentId = getResolvedAttachmentId();
    if (!attachmentId) {
      return {};
    }
    const analysis = await storage.formAnalysis.read(attachmentId);
    return analysis.status === "unavailable" ? {} : { [SERVER_TYPE]: [ANALYZE_FORM_TOOL] };
  },

  async callTool(
    toolName: string,
    args: Record<string, unknown>,
    entityId?: string,
  ): Promise<unknown> {
    if (toolName !== TOOL_NAME) {
      return `Unknown tool "${toolName}"`;
    }
    const question = getString(isObject(args) ? args : {}, "question")?.trim();
    if (!question) {
      return "analyze_form requires a non-empty 'question'.";
    }
    try {
      return await runAnalysis(question, entityId);
    } catch (err) {
      // Contract: stringify the failure, never throw — a throw would abort the main chat stream.
      logger.warn(`analyze_form failed: ${err instanceof Error ? err.message : String(err)}`);
      return "Form analysis could not be completed due to an internal error.";
    }
  },
};

async function runAnalysis(question: string, entityId: string | undefined): Promise<string> {
  markForwardHeadersToProvider();

  const profile = await resolveFormsProfile();
  if (!profile) {
    return "Form analysis is unavailable: no analysis model is configured.";
  }
  // The model that actually runs the analysis — must be the FormAnalysis profile, not the chat model.
  logger.info(
    `analyze_form: running on profile "${profile.name}" (id=${profile.id}, model=${profile.modelId})`,
  );

  // Tools + FormDataRules prompt in one round-trip (C# returns them together).
  const { tools, prompt: systemPrompt } = await formDataTools.listToolset(entityId);
  if (tools.length === 0) {
    return "Form analysis is unavailable: the form's submission table is not accessible.";
  }

  const messages: ThreadMessageLike[] = [{ role: "user", content: question }];
  for (let round = 0; round < MAX_ROUNDS; round += 1) {
    const message = await runTurn(profile, messages, tools, systemPrompt);
    const calls = extractToolCalls(message);
    if (calls.length === 0) {
      return extractText(message) || "The analysis produced no answer.";
    }

    const results = new Map<string, unknown>();
    for (const call of calls) {
      results.set(call.id, await formDataTools.callTool(call.name, call.args, entityId));
    }
    messages.push(attachResults(message, results));
  }

  logger.warn(`analyze_form: reached the ${MAX_ROUNDS}-round budget without a final answer`);
  return "The analysis could not be completed within the allotted steps.";
}

// One model turn: drain the `custom` stream, keep the final assistant message.
async function runTurn(
  profile: Profile,
  messages: ThreadMessageLike[],
  tools: TMCPItem[],
  systemPrompt: string,
): Promise<ThreadMessageLike> {
  let final: ThreadMessageLike = { role: "assistant", content: "" };
  for await (const event of custom({ profile, messages, tools, systemPrompt })) {
    if (!isObject(event)) {
      continue;
    }
    // Completed turn arrives as `{ isEnd, responseMessage }`; deltas are plain messages.
    const responseMessage = event["responseMessage"];
    final = event["isEnd"] === true && isObject(responseMessage) ? responseMessage : event;
  }
  return final;
}

// The model's requested tool calls, carried as `tool-call` content parts.
function extractToolCalls(message: ThreadMessageLike): ToolCall[] {
  const content = message["content"];
  if (!Array.isArray(content)) {
    return [];
  }
  const calls: ToolCall[] = [];
  for (const part of content) {
    if (!isObject(part) || part["type"] !== "tool-call") {
      continue;
    }
    const id = getString(part, "toolCallId");
    const name = getString(part, "toolName");
    if (!id || !name) {
      continue;
    }
    calls.push({ id, name, args: readArgs(part) });
  }
  return calls;
}

function readArgs(part: Record<string, unknown>): Record<string, unknown> {
  const args = part["args"];
  if (isObject(args)) {
    return args;
  }
  const argsText = getString(part, "argsText");
  if (argsText) {
    try {
      const parsed: unknown = JSON.parse(argsText);
      if (isObject(parsed)) {
        return parsed;
      }
    } catch {
      // Malformed args → empty object.
    }
  }
  return {};
}

// Re-attach each result to its `tool-call` part, so the next `custom` turn feeds it back to the model.
function attachResults(
  message: ThreadMessageLike,
  results: Map<string, unknown>,
): ThreadMessageLike {
  const content = message["content"];
  if (!Array.isArray(content)) {
    return message;
  }
  const withResults = content.map((part) => {
    if (isObject(part) && part["type"] === "tool-call") {
      const id = getString(part, "toolCallId");
      if (id && results.has(id)) {
        return { ...part, result: results.get(id) };
      }
    }
    return part;
  });
  return { ...message, content: withResults };
}
