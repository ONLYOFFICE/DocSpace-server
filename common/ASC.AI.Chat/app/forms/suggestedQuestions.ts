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

// Starter-question generation for a form's submissions. The model call runs here
// (all AI work goes through this service, via the ai-chat engine); the C# backend
// only supplies the submission schema and caches the result — see
// storage/formAnalysisStorage and the `form-analysis` internal endpoints.

import { customSync } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { ActionType } from "@onlyoffice/ai-chat/core";

import { storage } from "../storage/index.js";
import type { FormColumn, FormSchema, SuggestedQuestion } from "../storage/formAnalysisStorage.js";
import { markForwardHeadersToProvider } from "../requestContext.js";
import { getString, isObject } from "../narrow.js";
import logger from "../log.js";

const MAX_QUESTIONS = 4;
const MAX_QUESTION_LENGTH = 120;
const MAX_PROMPT_LENGTH = 320;

// One-shot budget (no retry — the client waits on the socket). Keep it under the client's
// give-up timeout (SUGGESTED_QUESTIONS_TIMEOUT_MS, 60s) so a finished generation still
// reaches a listening client.
const GENERATION_BUDGET_MS = 45_000;

const SYSTEM_PROMPT =
  "You generate starter analytics questions for a form-submission dataset.\n" +
  "Reply with a JSON array only. No prose, no markdown, no code fences, no reasoning.";

export type SuggestedQuestionsResult = {
  status: "ready" | "pending" | "unavailable";
  questions: SuggestedQuestion[];
};

const UNAVAILABLE: SuggestedQuestionsResult = { status: "unavailable", questions: [] };
const PENDING: SuggestedQuestionsResult = { status: "pending", questions: [] };

// In-process single-flight so two viewers (or a double-poll) of the same form
// share one model call instead of each launching its own — the coalescing the
// deleted C# GetOrSetAsync used to provide. Keyed by attachment id; cleared when
// the generation settles.
const inflight = new Map<string, Promise<SuggestedQuestionsResult>>();

// The forms model is resolved through the C# "FormAnalysis" assignment slot
// (ActionType.FormAnalysis + AiConfiguration.FormAnalysisModel gateway default,
// applied server-side). The ai-chat package's ActionType enum has not published
// this member yet, so it is named by its wire string.
const FORM_ANALYSIS_ACTION = "FormAnalysis" as ActionType;

async function resolveFormsProfile(): Promise<Profile | undefined> {
  // Prefer the dedicated FormAnalysis slot; fall back to the general chat model
  // (Default) so analysis works wherever a chat model is configured, even when
  // neither a gateway default nor an explicit FormAnalysis assignment exists.
  const profileId =
    (await storage.assignments.readByType(FORM_ANALYSIS_ACTION)) ??
    (await storage.assignments.readByType(ActionType.Default));
  if (!profileId) {
    return undefined;
  }
  return storage.profiles.readById(profileId);
}

// Keep aligned with C# FormSchemaFormatter.FormatColumn (still used by the chat's
// form-data tools) so both prompts describe a column the same way.
function formatColumn(column: FormColumn): string {
  const label = column.label ? ` "${column.label}"` : "";
  const values = column.values && column.values.length > 0 ? ` [${column.values.join("/")}]` : "";
  return `- ${column.name}${label} (${column.type})${values}`;
}

function buildUserPrompt(schema: FormSchema): string {
  const columnList = schema.columns.map(formatColumn).join("\n");
  return (
    `Form: "${schema.title}" — ${schema.rowCount} submissions collected.\n` +
    `Columns (name "label" (type) [allowed values]):\n${columnList}\n\n` +
    `Write ${MAX_QUESTIONS} starter questions an analyst would ask about these submissions.\n` +
    "- Every question must be answerable from the columns above.\n" +
    "- Prefer distributions, counts, comparisons and time trends over single-record lookups.\n" +
    "- Never invent a column or a value, and never repeat a question.\n" +
    '- "question": at most 60 characters, phrased as a plain button label a non-technical person ' +
    'understands. Refer to fields in everyday words; never put a raw field key or identifier (a "col_..." ' +
    "name, or a run-together/camelCase key) in it — rephrase it into readable words.\n" +
    '- "prompt": one sentence, the full request sent to the analysis assistant, naming the exact columns it needs.\n' +
    `- Write both fields in ${schema.cultureName} (${schema.culture}).\n` +
    'Reply with exactly this shape: [{"question":"...","prompt":"..."}]'
  );
}

// The model may wrap the array in prose or a code fence despite the instruction;
// take the first top-level JSON array in the text.
function extractJsonArray(text: string): string | undefined {
  const withoutThink = text.includes("</think>")
    ? text.slice(text.lastIndexOf("</think>") + 8)
    : text;
  const start = withoutThink.indexOf("[");
  const end = withoutThink.lastIndexOf("]");
  return start >= 0 && end > start ? withoutThink.slice(start, end + 1) : undefined;
}

function parseQuestions(text: string): SuggestedQuestion[] {
  const array = extractJsonArray(text);
  if (!array) {
    return [];
  }
  let parsed: unknown;
  try {
    parsed = JSON.parse(array);
  } catch {
    return [];
  }
  if (!Array.isArray(parsed)) {
    return [];
  }
  const result: SuggestedQuestion[] = [];
  for (const raw of parsed) {
    if (!isObject(raw)) {
      continue;
    }
    const question = getString(raw, "question")?.trim();
    const prompt = getString(raw, "prompt")?.trim();
    if (
      question &&
      prompt &&
      question.length <= MAX_QUESTION_LENGTH &&
      prompt.length <= MAX_PROMPT_LENGTH
    ) {
      result.push({ question, prompt });
    }
    if (result.length >= MAX_QUESTIONS) {
      break;
    }
  }
  return result;
}

function extractText(message: unknown): string {
  if (!isObject(message)) {
    return "";
  }
  const content = message["content"];
  if (typeof content === "string") {
    return content;
  }
  if (!Array.isArray(content)) {
    return "";
  }
  return content
    .map((part) =>
      isObject(part) && part["type"] === "text" ? (getString(part, "text") ?? "") : "",
    )
    .join("");
}

// Run the model for one form, then cache the result in C#. Assumes the caller has
// already confirmed the schema (status "generate").
async function generate(
  attachmentId: string,
  schema: FormSchema,
): Promise<SuggestedQuestionsResult> {
  markForwardHeadersToProvider();

  const profile = await resolveFormsProfile();
  if (!profile) {
    return UNAVAILABLE;
  }

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), GENERATION_BUDGET_MS);
  try {
    const message = await customSync({
      profile,
      messages: [{ role: "user", content: buildUserPrompt(schema) }],
      systemPrompt: SYSTEM_PROMPT,
      signal: controller.signal,
    });
    const questions = parseQuestions(extractText(message));
    if (questions.length === 0) {
      return PENDING;
    }
    await storage.formAnalysis.saveQuestions(attachmentId, questions);
    return { status: "ready", questions };
  } catch (err) {
    logger.warn(
      `suggestedQuestions: generation failed for attachment ${attachmentId}: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    return PENDING;
  } finally {
    clearTimeout(timer);
  }
}

/**
 * Kick off generation without blocking: "ready" (cached), "unavailable", or "pending" —
 * the model runs in the background and the result is pushed over the socket, so the client
 * subscribes instead of polling.
 */
export async function getSuggestedQuestions(
  attachmentId: string,
): Promise<SuggestedQuestionsResult> {
  const analysis = await storage.formAnalysis.read(attachmentId);
  if (analysis.status === "unavailable") {
    return UNAVAILABLE;
  }
  if (analysis.status === "ready") {
    return { status: "ready", questions: analysis.questions };
  }

  // Run once in the background (single-flight), answer "pending" now. The job inherits this
  // request's AsyncLocalStorage context, so its saveQuestions call keeps the forwarded auth.
  if (!inflight.has(attachmentId)) {
    const job = generate(attachmentId, analysis.schema).finally(() =>
      inflight.delete(attachmentId),
    );
    inflight.set(attachmentId, job);
    job.catch((err) =>
      logger.warn(
        `suggestedQuestions: background generation failed for attachment ${attachmentId}: ${
          err instanceof Error ? err.message : String(err)
        }`,
      ),
    );
  }
  return PENDING;
}
