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
import {
  markForwardHeadersToProvider,
  getCustomServerNames,
  getChatContextSnapshot,
  getSourceMeta,
  setKnowledgeRoomIds,
  setRoundAttachmentIds,
  setRoundProfileId,
  setRoundThreadId,
} from "../requestContext.js";
import { primeChatContext, describeChatContextUsage } from "../storage/chatContext.js";
import { applyAiProjectChatContext } from "../storage/aiProjectChatContext.js";
import {
  buildContextFoldersLines,
  buildLocationLines,
  contextFolderIdsOf,
  resolveContextFolders,
  resolveLocation,
  type ChatLocation,
  type ContextFolder,
} from "../storage/contextFolders.js";
import { customToolsSource, primeCustomServers } from "../tools/customTools.js";
import { storage } from "../storage/index.js";
import { aiService, AiServiceHttpError } from "../storage/httpClient.js";
import { asyncHandler, streamNdjson, streamOpenAiSse, attachmentLimitError } from "./_helpers.js";
import { observeChatStream } from "../telemetry/chatStream.js";
import type { StreamDialect } from "../telemetry/chatStream.js";
import { assertThreadCreatable } from "./threadsController.js";
import { readStoredProfile } from "./storedProfile.js";
import { assertAiAccess } from "./aiAccess.js";
import { asActionType, isObject, parseInteger } from "../narrow.js";
import {
  HttpToolsAdapter,
  safeGetToolsPrompt,
  extractAttachmentRefIds,
  DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
} from "../tools/httpToolsAdapter.js";
import { systemToolsSource } from "../tools/systemTools.js";
import { formAnalysisSubAgent } from "../tools/formAnalysisSubAgent.js";
import { documentAgentTools } from "../tools/documentAgent.js";
import { DOCUMENT_AGENT_TYPE, getCompaction, getPersistIntervalMs } from "../../config/index.js";
import { primeSourceMeta } from "../storage/docspaceFilesApi.js";

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
  // Always wire the abort signal, even when the request carries no
  // `actionArgs`. The UI sends an `actionArgs` object (with a signal that
  // collapses to `{}` over JSON), but a direct API client omits it entirely
  // — and without the signal a client disconnect never reaches the provider,
  // so generation runs to completion and keeps burning tokens after the user
  // has stopped waiting. Create the object when missing so cancellation works
  // for every caller.
  const actionArgs = isObject(body["actionArgs"]) ? body["actionArgs"] : {};
  // The knowledge bases the composer connected ride in `actionArgs`; the
  // engine never reads them, the tool context does (every action handler
  // passes through here, so the ids are in place before `getTools`).
  setKnowledgeRoomIds(knowledgeRoomIdsOf(actionArgs));
  return {
    ...body,
    actionArgs: { ...actionArgs, signal: responseAbortSignal(res) },
  };
}

// `actionArgs.knowledgeRooms[].id` as the room ids the .NET side expects —
// each a DocSpace room id, once, in the order sent. An older widget sends a
// single `actionArgs.knowledgeRoom`; it is read the same way.
function knowledgeRoomIdsOf(actionArgs: Record<string, unknown>): number[] {
  const rooms = actionArgs["knowledgeRooms"];
  const list: unknown[] = Array.isArray(rooms) ? rooms : [actionArgs["knowledgeRoom"]];
  const ids: number[] = [];
  for (const room of list) {
    if (!isObject(room)) {
      continue;
    }
    const id = room["id"];
    // The whole value has to be an integer: "9abc" names no room, it is not room 9.
    const parsed = parseInteger(id);
    if (parsed !== undefined && parsed > 0 && !ids.includes(parsed)) {
      ids.push(parsed);
    }
  }
  return ids;
}

// Fold the DocSpace tools system-prompt fragment into the action's prompt
// override. The fragment ships alongside the tool list from the .NET
// `tools/list`; nothing along this path is cached (deliberate — see
// SystemToolsSource), so the prompt fetch is its own round-trip next to
// the engine's `getTools`.
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
      ? { ...existing, text: `${existing["text"]}\n\n${fragment}` }
      : { mode: "append", text: fragment };
  return {
    ...body,
    actionArgs: { ...actionArgs, prompt },
  };
}

// The two ids a chat request may carry, as the client sends them:
//
// - `entityId` is present only inside an AI project and is the project id
//   (`applyAiProjectChatContext` binds the round to the project from it);
//   everywhere else it is absent. Threads and the per-user AI data are
//   global, so it scopes nothing else.
// - `contextEntityId` is the folder or room the user is in (the Files
//   section). Inside a project the client sends none and
//   `applyAiProjectChatContext` sets it to the picked folder context or to
//   My documents.
//
// The engine itself resolves `contextEntityId ?? entityId` as the id it
// hands the tools adapters and the attachments storage (the folder a
// generated image is uploaded into), so a project round without a folder
// context would otherwise run its tools against the project id.
function stringField(body: unknown, key: string): string | undefined {
  if (!isObject(body)) {
    return undefined;
  }
  const value = body[key];
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

// The ids a chat body carries (`threadId`, `entityId`, `contextEntityId`,
// `profileId`) are strings on the wire: the engine's input types say so, and
// the web client and the .NET suite send them as strings. `null` stays what it
// was, an absent id. Any other type is the caller's mistake and answers 400
// naming the field in every round (send, send-custom, the two stream routes,
// regenerate, approve and deny), so that a number never reaches the aggregate
// read, the thread read or the profile read (ts-ai-failure-paths.md §4).
type ChatIdField = "threadId" | "entityId" | "contextEntityId" | "profileId";
const STREAM_ID_FIELDS: readonly ChatIdField[] = [
  "threadId",
  "entityId",
  "contextEntityId",
  "profileId",
];

function idTypeError(body: unknown, fields: readonly ChatIdField[]): string | null {
  if (!isObject(body)) {
    return null;
  }
  for (const field of fields) {
    const value = body[field];
    if (value !== undefined && value !== null && typeof value !== "string") {
      return `${field} must be a string`;
    }
  }
  return null;
}

// An id of the body as the aggregate read takes it: a string as sent, else
// absent. A round whose ids are of a wrong type is refused right after that
// read (see idTypeError).
function idOf(body: unknown, field: ChatIdField): string | undefined {
  if (!isObject(body)) {
    return undefined;
  }
  const value = body[field];
  return typeof value === "string" ? value : undefined;
}

// The aggregate read of a round that takes its ids from the body, then the
// refusal of an id of a wrong type. Answers `false` when it has responded.
// The aggregate read is the round's access gate when it installs a snapshot.
// When it installs none (switched off by `AI_CHAT_CONTEXT` or the
// `x-ai-chat-context: off` header, failed with anything but 401/403, or an
// unusable answer) no gated call has been made, so the gate is asked here,
// before any refusal of the body (ts-ai-failure-paths.md §3).
// The fields a resume of a round cannot do without (`RegenerateStreamInput` and `ToolCallData`
// of the library), present and of their type. Left out, the engine went on with `undefined`:
// `GET messages/undefined` to the AI service, then its status or a 409, where the document
// promises a 400. Their values are the engine's to judge: an unknown thread is 404, an unknown
// message or an index out of range 409 (openspec ai-agents "Decisions on a tool call").
type ResumeField = "threadId" | "messageId" | "idx";

function missingFieldError(body: unknown, fields: readonly ResumeField[]): string | null {
  for (const field of fields) {
    const value = isObject(body) ? body[field] : undefined;
    if (field === "idx") {
      if (typeof value !== "number" || !Number.isInteger(value)) {
        return "idx is required and must be an integer";
      }
    } else if (typeof value !== "string") {
      return `${field} is required and must be a string`;
    }
  }
  return null;
}

async function primeRound(body: unknown, res: Response): Promise<boolean> {
  const primed = await primeChatContext({
    threadId: idOf(body, "threadId"),
    entityId: idOf(body, "entityId"),
    contextEntityId: idOf(body, "contextEntityId"),
  });
  if (!primed) {
    await assertAiAccess();
  }
  const error = idTypeError(body, STREAM_ID_FIELDS);
  if (error) {
    res.status(400).json({ error });
    return false;
  }
  return true;
}

// The folders and rooms the user selected as the chat's context in the
// composer (`actionArgs.folderContexts`, up to `CONTEXT_FOLDERS_LIMIT`), by
// id; empty when none is selected.
function contextFolderIdsOfBody(body: unknown): string[] {
  return isObject(body) ? contextFolderIdsOf(body["actionArgs"]) : [];
}

// The id the engine hands the tools adapters for the round — mirrors its
// own `contextEntityId ?? entityId` resolution, so the tools fragment of the
// prompt describes the tool set the engine actually wires up.
function toolsScopeOf(body: unknown): string | undefined {
  return stringField(body, "contextEntityId") ?? stringField(body, "entityId");
}

// The folder the round is attributed to: the first of the context folders
// the user selected, else the folder or room they are in.
function locationOf(body: unknown): string | undefined {
  return contextFolderIdsOfBody(body)[0] ?? stringField(body, "contextEntityId");
}

// The entry the round is attributed to for usage accounting: where the user
// chats. Inside an AI project that is the project itself - never the folder
// `applyAiProjectChatContext` set as the round's `contextEntityId` (the
// picked folder, else My documents), which only scopes the file tools.
// Elsewhere it is the folder or room the user is in; only a round with no
// location falls back to a context folder picked in the composer.
function sourceScopeOf(body: unknown): string | undefined {
  return (
    stringField(body, "entityId") ??
    stringField(body, "contextEntityId") ??
    contextFolderIdsOfBody(body)[0]
  );
}

// A thread read that answers "this thread is not there": 404, or the 400 the
// AI service gives a malformed id. Every other failure (a 5xx, a timeout, a
// dropped connection) says nothing about the thread and is passed on, so the
// caller gets a 5xx and can retry (ts-ai-failure-paths.md §1).
function isThreadAbsent(err: unknown): err is AiServiceHttpError {
  return err instanceof AiServiceHttpError && (err.status === 404 || err.status === 400);
}

// The round's profile and attachments for a request that resumes a round
// (tool-call approval): the profile the round runs on, ranked as
// sendWithStream ranks it (the caller's explicit choice, then the thread's
// own), and the attachments of the thread's last user message, the one the
// resumed round answers. A thread that is not there leaves the document agent
// tool to its own fallbacks; a read that failed otherwise fails the request
// before the approved tool runs.
async function primeRoundFromThread(threadId: unknown, requestedProfileId: unknown): Promise<void> {
  if (typeof threadId !== "string" || threadId.length === 0) {
    return;
  }
  try {
    const [thread, history] = await Promise.all([
      storage.threads.readById(threadId),
      storage.messages.readByThread(threadId),
    ]);
    const requested = typeof requestedProfileId === "string" ? requestedProfileId : undefined;
    setRoundProfileId(requested ?? thread?.profileId ?? undefined);
    let lastUser: unknown;
    for (let i = history.length - 1; i >= 0; i--) {
      const message: unknown = history[i];
      if (isObject(message) && message["role"] === "user") {
        lastUser = message;
        break;
      }
    }
    setRoundAttachmentIds(lastUser ? extractAttachmentRefIds(lastUser) : []);
  } catch (err) {
    if (!isThreadAbsent(err)) {
      throw err;
    }
    logger.warn(`primeRoundFromThread: thread ${threadId} not found: AI service ${err.status}`);
  }
}

// Attachment refs from the thread's persisted history, newest message first
// so the most recently attached form wins. A thread that is not there carries
// no prior context; a read that failed otherwise fails the round before its
// stream opens.
async function historyAttachmentRefIds(threadId: unknown): Promise<string[]> {
  if (typeof threadId !== "string" || threadId.length === 0) {
    return [];
  }
  let history: unknown[];
  try {
    history = await storage.messages.readByThread(threadId);
  } catch (err) {
    if (!isThreadAbsent(err)) {
      throw err;
    }
    logger.warn(
      `withToolsPrompt: thread ${threadId} history not found, form context not carried over: AI service ${err.status}`,
    );
    return [];
  }
  const ids: string[] = [];
  const seen = new Set<string>();
  for (let i = history.length - 1; i >= 0; i--) {
    for (const ref of extractAttachmentRefIds(history[i])) {
      if (!seen.has(ref)) {
        seen.add(ref);
        ids.push(ref);
      }
    }
  }
  return ids;
}

async function withToolsPrompt<T>(body: T): Promise<T> {
  if (!isObject(body)) {
    return body;
  }
  // The tools fragment describes the tool set the engine actually wires up
  // for the round, so it follows the id the engine hands the adapters,
  // unlike the location fragment below.
  const contextScope = toolsScopeOf(body);
  // Keep form-data tools active for the whole conversation: when the current
  // message has no attachment of its own, fall back to the form(s) already in
  // the thread history so follow-up questions still resolve it.
  let attachmentId = extractAttachmentRefIds(body["userMessage"]);
  // The message's own attachments, for the document agent tool: it hands
  // them to the agent, and `callTool` carries no refs of its own.
  setRoundAttachmentIds(attachmentId);
  if (attachmentId.length === 0) {
    attachmentId = await historyAttachmentRefIds(body["threadId"]);
  }
  const fragment = await safeGetToolsPrompt(toolsAdapter, contextScope, attachmentId);
  return fragment ? appendActionPrompt(body, fragment) : body;
}

// Build the context fragment that tells the model where it is operating and
// how to scope tool calls. The location lines are emitted only when the
// user is somewhere (`location`) that is not already one of the context
// folders; the context folders - the folders and rooms the user selected for
// the chat, several of them at once - follow as a list the model can reach
// by id through the tools.
function buildContextFragment(
  location: ChatLocation | undefined,
  contextFolders: readonly ContextFolder[] = [],
): string {
  const today = new Date().toISOString().slice(0, 10);
  const lines = [
    "Context:",
    "- You are an AI agent operating inside an ONLYOFFICE Apps workspace, not a generic standalone assistant.",
  ];
  if (location) {
    lines.push(...buildLocationLines(location));
  }
  lines.push(...buildContextFoldersLines(contextFolders));
  lines.push(
    "- Ids of folders, rooms and files are for tool calls only and mean nothing to the user: in your replies, refer to them by their names.",
    `- Today's date is ${today}.`,
    "- Prefer answering from the conversation when you can; use tools only when the request clearly needs data or actions in the workspace.",
    "- When you link to a workspace file, use only a link a tool returned for it (its `url` or `webUrl`), copied verbatim. Never compose a link yourself from a file id or title; if no tool returned a link, name the file without one.",
  );
  return lines.join("\n");
}

async function withContextPrompt<T>(body: T): Promise<T> {
  if (!isObject(body)) {
    return body;
  }
  // The model is told two things. Where the USER is: the folder or room
  // they are in (`contextEntityId`). And what they selected as the chat's
  // context: the folders and rooms of `actionArgs.folderContexts`, read here
  // under the caller's credentials so the names and rights are the server's.
  // The project id (`entityId`) is never a location - inside a project
  // `applyAiProjectChatContext` supplies `contextEntityId`.
  // The location is read as the context folders are, for its name.
  const contextFolders = await resolveContextFolders(contextFolderIdsOfBody(body));
  const entityId = stringField(body, "contextEntityId");
  const location =
    entityId && !contextFolders.some((folder) => folder.id === entityId)
      ? await resolveLocation(entityId)
      : undefined;
  return appendActionPrompt(body, buildContextFragment(location, contextFolders));
}

// Attribute the round to the DocSpace entry it runs for — the project the
// chat runs in, or else the folder or room the user works in (see
// `sourceScopeOf`). The resolved source lands
// on the request context; the engine reads it through its `resolveSource`
// dep and the library's ONLYOFFICE provider turns it into the request's
// `metadata` object (`source_id` / `source_type` / `source_title`) on every
// request of the round, tool-call resume rounds and web search included, so
// the backend can attribute usage. Server-resolved on purpose: type and title come from the
// Files API under the caller's credentials, so a client cannot claim someone
// else's entry. A round with no resolvable scope leaves the context empty and
// the field stays absent.
//
// `scope` overrides where the entry is read from, for the one input shape that
// carries no top-level scope of its own (see {@link customScopeOf}). It is only
// ever a hint: the source itself is always re-resolved here.
async function withSourceMetadata<T>(body: T, scope?: string): Promise<T> {
  await primeSourceMeta(scope ?? sourceScopeOf(body));
  return body;
}

// Scope of a `sendCustom` round. `SendCustomInput` is `isStream` /
// `systemPrompt` / `userMessage` / `actionArgs` / `profileId` — it has no
// `entityId` of its own, so the only place a caller can name the entry is
// `actionArgs.entityId` (the field the library once carried the metadata pair
// in). Treated as a hint and re-resolved server-side by `withSourceMetadata`,
// so a client still cannot claim someone else's entry (mirrors
// threads/regenerate-title).
//
// `sourceScopeOf` still wins when present: a host that sends the round with
// an explicit top-level scope keeps describing it that way.
function customScopeOf(body: unknown): string | undefined {
  const scope = sourceScopeOf(body);
  if (scope) {
    return scope;
  }
  if (!isObject(body)) {
    return undefined;
  }
  const actionArgs = body["actionArgs"];
  if (!isObject(actionArgs)) {
    return undefined;
  }
  const entityId = actionArgs["entityId"];
  return typeof entityId === "string" ? entityId : undefined;
}

// How often a streaming assistant message is flushed to the C# service
// (`PUT /internal/ai/messages/{id}`) while the round is in progress. The
// library default (150 ms) is tuned for a local adapter; here every write
// is an HTTP hop, so pace them to a few seconds. The first create and the
// terminal write are unconditional; this only paces the intermediate ones,
// and the library salvages the staged text if the stream dies early.
// Override with `AI_CHAT_PERSIST_INTERVAL_MS`; anything that is not a
// positive integer keeps the default here rather than falling through to
// the library's 150 ms fallback.
const PERSIST_INTERVAL_MS = getPersistIntervalMs();

const toolsAdapter = new HttpToolsAdapter();
const engine = new AIEngine({
  storage,
  persistIntervalMs: PERSIST_INTERVAL_MS,
  // The entry the round is billed to. Every model-bound handler resolves it
  // server-side into the request context first (`withSourceMetadata`), so
  // the hook only reads that value back: the library then carries it into
  // every request of the round — chat, tool-call resume rounds, the title,
  // the built-in image tool — and into the web-search body as `metadata`.
  // Being wired, the hook also makes the library ignore any `source` a
  // client put in `actionArgs`.
  resolveSource: () => getSourceMeta(),
  // A tool the engine runs reports MCP-shaped progress (the document agent's
  // steps, an MCP server's `notifications/progress`), and the engine streams
  // it on the pending tool card until the result arrives.
  toolProgress: true,
  // System (host-configured MCP) tools and registered custom MCP servers
  // run server-side and pause for UI approval; most DocSpace integration
  // tools run silently. Compose all three — before customToolsSource was
  // wired in, the custom-server registry was pure configuration and its
  // tools never reached the model (Bugs 82989 / 82990).
  // formAnalysisSubAgent adds the `analyze_form` delegating tool; its serverType is silent
  // (absent from systemServerTypes), so it runs without UI approval.
  // documentAgentTools adds the `document_agent` delegating tool when an agent is configured;
  // its serverType is approval-required, a delegated task may run for minutes. The tool itself
  // asks for no approval (it only creates files), so only the "ask" permission mode stops it.
  toolsAdapter: composeToolsAdapters(
    systemToolsSource,
    customToolsSource,
    toolsAdapter,
    formAnalysisSubAgent,
    documentAgentTools,
  ),
  // Approval-required server types: the MCP servers (host-configured and
  // custom — the latter resolved per request, see primeCustomServers) plus
  // the DocSpace integration tools explicitly grouped under the approval
  // serverType (e.g. document/presentation/form generation).
  systemServerTypes: () => [
    ...systemToolsSource.getServerTypes(),
    ...getCustomServerNames(),
    DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
    DOCUMENT_AGENT_TYPE,
  ],
  // Fold a thread that nears its model's context window into a summary
  // message before the round. The summary is stored through the ordinary
  // messages route (`contents` keeps the whole message, marker included),
  // so the C# side needs nothing new. On unless `ai.compaction.enabled`
  // / `AI__COMPACTION__ENABLED` is false; resolved per round.
  compaction: () => getCompaction(),
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
  const content = message && Array.isArray(message["content"]) ? message["content"] : [];
  const part = idx >= 0 && isObject(content[idx]) ? content[idx] : null;
  const toolName = part && typeof part["toolName"] === "string" ? part["toolName"] : "?";
  const toolCallId = part && typeof part["toolCallId"] === "string" ? part["toolCallId"] : "?";
  return `tool=${toolName} callId=${toolCallId} idx=${idx} serverExecuted=${
    event["serverExecuted"] === true
  } autoAllow=${event["autoAllow"] === true}`;
}

// Describe an in-engine tool result for the log without dumping it: an image
// result carries megabytes of base64, and the interesting part is never the
// payload — it is whether the tool answered with data, with a persisted ref,
// or with an error (an unconfigured image profile, a provider/gateway refusal
// such as a 402 from the paid route). Those errors are returned by the engine
// as an ordinary tool result and are otherwise invisible server-side: the
// library logs them only to its own console logger.
function describeToolResult(result: unknown): string {
  const text = typeof result === "string" ? result : JSON.stringify(result);
  if (text === undefined) {
    return String(result);
  }
  try {
    const parsed: unknown = JSON.parse(text);
    if (isObject(parsed)) {
      const data = isObject(parsed["data"]) ? parsed["data"] : null;
      if (data && typeof data["base64"] === "string") {
        return `{data.base64=${data["base64"].length}B} (not persisted — no ref)`;
      }
      if (data && typeof data["ref"] === "string") {
        return `{data.ref=${data["ref"]}}`;
      }
      if (parsed["error"] !== undefined) {
        return `ERROR ${JSON.stringify(parsed["error"]).slice(0, 700)}`;
      }
    }
  } catch {
    // Not JSON — fall through to the truncated raw form.
  }
  return `${text.slice(0, 500)}${text.length > 500 ? `… (${text.length}B)` : ""}`;
}

// Scan an event's streamed message for tool-call content parts and record
// each tool's name against whether its `result` is populated yet. Adapter
// tools (web search, image generation, DocSpace) execute inside the engine
// and never surface a `tool-call-pending` to this tap — the only trace they
// leave here is the tool-call part inside the assistant message, gaining a
// `result` once the engine ran it. So `executed=true` in the completion
// summary is the positive proof a tool actually fired and returned. Each
// result is also described once (`logged`, keyed by tool-call id) so a tool
// that answered with an error is visible in the log, not only in the browser.
function trackToolCalls(
  event: unknown,
  seen: Map<string, boolean>,
  logged: Set<string>,
  route: string,
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
    if (!hasResult) {
      continue;
    }
    const callId = typeof part["toolCallId"] === "string" ? part["toolCallId"] : name;
    if (logged.has(callId)) {
      continue;
    }
    logged.add(callId);
    const described = describeToolResult(part["result"]);
    const line = `${route}: tool result ${name} (callId=${callId}) -> ${described}`;
    if (described.startsWith("ERROR")) {
      logger.error(line);
    } else {
      logger.info(line);
    }
  }
}

// Sniff stream events to surface provider failures in server logs. The
// engine emits `message-incomplete` with `status.reason === "error"` and a
// `ChatErrorPayload` instead of throwing, so without this tap the cause of
// a broken stream is invisible from the server side — it only shows up in
// the browser DevTools network tab. Also traces tool-call lifecycle so a
// tool that hangs (pending surfaced, stream never completes) is visible.
async function* logStreamErrors<T>(route: string, iter: AsyncIterable<T>): AsyncIterable<T> {
  let eventCount = 0;
  const toolCalls = new Map<string, boolean>();
  const loggedResults = new Set<string>();
  try {
    for await (const event of iter) {
      eventCount += 1;
      trackToolCalls(event, toolCalls, loggedResults, route);
      if (isObject(event) && event["type"] === "message-incomplete") {
        const message = isObject(event["message"]) ? event["message"] : null;
        const status = message && isObject(message["status"]) ? message["status"] : null;
        if (status && status["reason"] === "error") {
          logger.warn(
            `${route}: provider returned message-incomplete — ${describeChatError(status["error"])}`,
          );
        } else if (status) {
          logger.warn(`${route}: message-incomplete reason=${String(status["reason"])}`);
        }
      } else if (isObject(event) && event["type"] === "tool-call-pending") {
        logger.info(`${route}: tool-call-pending — ${describeToolCallPending(event)}`);
      }
      yield event;
    }
    const toolSummary =
      toolCalls.size > 0
        ? [...toolCalls].map(([name, executed]) => `${name}(executed=${executed})`).join(", ")
        : "<none>";
    logger.info(
      `${route}: stream completed after ${eventCount} event(s); toolCalls=${toolSummary}; ` +
        describeChatContextUsage(),
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

// Telemetry (span + metrics) outside, error logging inside.
function tapStream<T>(
  route: string,
  iter: AsyncIterable<T>,
  dialect?: StreamDialect,
): AsyncIterable<T> {
  return observeChatStream(route, logStreamErrors(route, iter), dialect);
}

// A user message must carry something for the model before a stream is
// opened: non-whitespace text, or at least one attachment. `content` is
// either a plain string or an array of parts; text lives on
// `{ type: "text", text }` parts (and bare string parts), mirroring the
// engine's own text extraction, attachments on `file` / `image` parts. A
// truly empty prompt otherwise reaches the provider and streams back
// nothing (Bug 82720). An attachment-only message is a valid prompt — the
// composer sends one since ai-chat 0.5.115 (a dropped file with no text),
// and the engine builds the provider request from the attachment alone.
function hasPromptContent(userMessage: unknown): boolean {
  if (!isObject(userMessage)) {
    return false;
  }
  const content = userMessage["content"];
  if (typeof content === "string") {
    return content.trim().length > 0;
  }
  if (!Array.isArray(content)) {
    return false;
  }
  for (const part of content) {
    if (typeof part === "string") {
      if (part.trim().length > 0) {
        return true;
      }
      continue;
    }
    if (!isObject(part)) {
      continue;
    }
    if (part["type"] === "file" || part["type"] === "image") {
      return true;
    }
    if (
      part["type"] === "text" &&
      typeof part["text"] === "string" &&
      part["text"].trim().length > 0
    ) {
      return true;
    }
  }
  return false;
}

// Validation error when the effective profileId does not reference an
// existing profile, or null when it does (or when no profileId is supplied —
// the engine then resolves the scope's assignment itself). A malformed id
// makes the C# lookup fail with 400; that is just as much "no such profile"
// to the caller, so both collapse to the same message. A lookup that fails
// any other way is the service failing, and is passed on with its status.
async function unknownProfileIdError(profileId: string | undefined): Promise<string | null> {
  if (!profileId) {
    return null;
  }
  const profile = await readStoredProfile(profileId);
  if (!profile) {
    return `unknown profileId: ${profileId}`;
  }
  return aiToolsUnpaidError(profile.providerType);
}

// What a round that stores its user message checks before the stream opens,
// for `send-with-stream` and its OpenAI-framed twin alike. Answers `false`
// when it has already responded with the refusal. Runs after the AI-project
// binding, so the profile checked is the one the round will use.
async function preflightStreamRound(body: SendStreamInput, res: Response): Promise<boolean> {
  // Reject an empty prompt before opening the stream: a user message with
  // neither non-whitespace text nor an attachment otherwise reaches the
  // provider and streams back nothing (Bug 82720). A file-only message
  // passes — the composer sends one when a file is dropped with no text.
  if (!hasPromptContent(body.userMessage)) {
    res.status(400).json({
      error: "userMessage must contain non-empty text content or an attachment",
    });
    return false;
  }
  // The same per-kind attachment cap as threads/append-user-message —
  // these endpoints persist the user message through the same engine, so
  // they are the other way around the composer's limit (Bug 82894).
  const limitError = attachmentLimitError(body.userMessage);
  if (limitError) {
    res.status(400).json({ error: limitError });
    return false;
  }
  // A new thread (no threadId) is created implicitly here, so gate it like
  // threads/create (review #6): `assertThreadCreatable` throws the refusal
  // for an entity the caller cannot use or a round no profile resolves for.
  if (!body.threadId) {
    await assertThreadCreatable(body.entityId, body.profileId);
  }
  // Without a profileId the round runs on the model the thread was created
  // with (Bug 82860). Pre-fill it from the thread's stored value so the
  // checks below and the document agent tool see the profile the round
  // really uses; a session-level profileId is honored for the request
  // without mutating persisted state.
  if (!body.profileId && body.threadId) {
    const thread = await storage.threads.readById(body.threadId);
    if (thread?.profileId) {
      body.profileId = thread.profileId;
    }
  }
  // The effective profile, for the document agent tool: it runs the agent
  // on the chat's own model, and `callTool` carries no profile.
  setRoundProfileId(body.profileId);
  setRoundThreadId(body.threadId);
  // The effective profileId must reference an existing profile BEFORE the
  // stream opens: a malformed or unknown id otherwise surfaces only as an
  // opaque {"type":"error"} frame inside a 200 stream (Bug 83045), or the
  // engine silently falls back to another model (Bug 83160).
  const profileError = await unknownProfileIdError(body.profileId);
  if (profileError === AI_TOOLS_UNPAID_ERROR) {
    res.status(402).json({ error: AI_TOOLS_UNPAID_ERROR });
    return false;
  }
  if (profileError) {
    res.status(400).json({ error: profileError });
    return false;
  }
  return true;
}

// The 402 body for the unpaid gateway, and the sentinel by which the callers
// tell it apart from the unknown-profile validation error (400) in
// unknownProfileIdError's single string channel. It is English: the AI
// service serves no localized wording for this state (AiSettingsDto has no
// such field), and this service has no resources of its own.
const AI_TOOLS_UNPAID_ERROR = "The AI Tools service is not paid for the current portal";

// 402 message when the effective profile rides the portal gateway while the
// tenant's AI Tools wallet service is not paid, null otherwise. Without
// this check the round opens a 200 NDJSON stream and the billing failure
// surfaces only as an in-stream error frame (Bug 83344). `aiReady` (the C#
// AiAccessibility status) is false here in exactly one reachable state —
// the unpaid gateway wallet: the admin kill-switch already failed the
// profile read above with 403, and non-gateway providers have no wallet.
async function aiToolsUnpaidError(providerType: string | undefined): Promise<string | null> {
  if (providerType !== "onlyoffice") {
    return null;
  }
  // The round's aggregate already carries the portal's AI settings.
  const primed = getChatContextSnapshot()?.aiReady;
  if (primed !== undefined) {
    return primed ? null : AI_TOOLS_UNPAID_ERROR;
  }
  let config: unknown;
  try {
    config = await aiService.get("/config");
  } catch (err) {
    // A refusal of the caller's own (401, 403) is passed on. Any other failure
    // leaves the wallet unknown, and unknown is safe to read as paid here: the
    // check only moves the gateway's refusal out of the stream, and the
    // gateway refuses an unpaid portal itself (AiGatewayProxyController.cs), so nothing is
    // billed and the caller gets an error frame instead of the 402.
    if (err instanceof AiServiceHttpError && err.status < 500) {
      throw err;
    }
    logger.warn(
      `ai-tools wallet check: /config not read, the gateway decides: ${
        err instanceof AiServiceHttpError ? `AI service ${err.status}` : String(err)
      }`,
    );
    return null;
  }
  if (isObject(config) && config["aiReady"] === false) {
    return AI_TOOLS_UNPAID_ERROR;
  }
  return null;
}

export const aiController = {
  send: asyncHandler<SendInput>(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body.
    await assertAiAccess();
    // The body used to reach the engine unchecked: an unknown or missing
    // actionType died in the assignment resolver and a missing userMessage
    // in the attachment resolver, both as 500 (Bug 84294). Checked before
    // anything is primed so a refused call has no side effects.
    if (!isObject(req.body)) {
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    if (!asActionType(req.body.actionType)) {
      res.status(400).json({ error: "actionType is required and must be a known action type" });
      return;
    }
    if (!hasPromptContent(req.body.userMessage)) {
      res.status(400).json({
        error: "userMessage must contain non-empty text content or an attachment",
      });
      return;
    }
    {
      const limitError = attachmentLimitError(req.body.userMessage);
      if (limitError) {
        res.status(400).json({ error: limitError });
        return;
      }
    }
    // Without this the provider request runs without the forwarded auth /
    // request headers, so a correct call comes back with an empty message
    // plus an auth error, or fails outright with a 500 (Bugs 82833, 82835).
    // Mirrors sendWithStream. (No profile pinning here: SendInput carries no
    // profileId, so there is nothing for a caller to override.)
    markForwardHeadersToProvider();
    await primeChatContext({
      entityId: idOf(req.body, "entityId"),
      contextEntityId: idOf(req.body, "contextEntityId"),
    });
    // The ids are checked after the access gate at the top of the handler.
    {
      const idError = idTypeError(req.body, ["entityId", "contextEntityId"]);
      if (idError) {
        res.status(400).json({ error: idError });
        return;
      }
    }
    // A round inside an AI project runs on the project's model and linked
    // rooms, whatever the composer sent (see storage/aiProjectChatContext.ts).
    {
      const refusal = await applyAiProjectChatContext(req.body);
      if (refusal) {
        res.status(refusal.status).json(refusal.body);
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    const result = await engine.send(await withSourceMetadata(withRequestSignal(res, req.body)));
    res.json(result);
  }),

  sendCustom: asyncHandler<SendCustomInput>(async (req, res) => {
    // A missing systemPrompt went to the provider as an undefined system
    // message and came back as a 500 (Bug 84299); a string `isStream` such
    // as "false" counted as true and switched the answer to a stream
    // (Bug 84300). Both are the caller's mistake, so 400, before anything
    // is primed, and after the access gate (ts-ai-failure-paths.md §3).
    await assertAiAccess();
    if (!isObject(req.body)) {
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    if (typeof req.body.systemPrompt !== "string" || req.body.systemPrompt.trim().length === 0) {
      res.status(400).json({ error: "systemPrompt is required and must be a non-empty string" });
      return;
    }
    if (!hasPromptContent(req.body.userMessage)) {
      res.status(400).json({
        error: "userMessage must contain non-empty text content or an attachment",
      });
      return;
    }
    {
      const limitError = attachmentLimitError(req.body.userMessage);
      if (limitError) {
        res.status(400).json({ error: limitError });
        return;
      }
    }
    const isStream: unknown = req.body.isStream;
    if (isStream !== undefined && isStream !== null && typeof isStream !== "boolean") {
      res.status(400).json({ error: "isStream must be a boolean" });
      return;
    }
    // As with `send`, the forwarded headers must be marked before the
    // provider call or a correct request fails with a 500 (Bug 82836).
    markForwardHeadersToProvider();
    await primeChatContext({ entityId: customScopeOf(req.body) });
    // The ids are checked after the access gate at the top of the handler.
    {
      const idError = idTypeError(req.body, ["profileId"]);
      if (idError) {
        res.status(400).json({ error: idError });
        return;
      }
    }
    // A profileId that names no profile would otherwise fall back to another
    // model without a word, and an unpaid AI Tools wallet would answer inside
    // the stream (Bugs 83160, 83344): refuse both up front, as the stored
    // rounds do.
    {
      const profileError = await unknownProfileIdError(req.body.profileId);
      if (profileError === AI_TOOLS_UNPAID_ERROR) {
        res.status(402).json({ error: AI_TOOLS_UNPAID_ERROR });
        return;
      }
      if (profileError) {
        res.status(400).json({ error: profileError });
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    // Attribute the round to its source for the backend's usage accounting, as
    // every other model-bound route does. The engine's `resolveSource` reads it
    // from the request context, so both the streaming (`sendMessage`) and
    // one-shot (`sendMessageSync`) branches carry it.
    const body = await withSourceMetadata(
      withRequestSignal(res, req.body),
      customScopeOf(req.body),
    );
    const result = engine.sendCustom(body);
    if (body.isStream) {
      if (isAsyncIterable(result)) {
        await streamNdjson(res, tapStream("ai/send-custom", result));
      } else {
        res.json(await result);
      }
      return;
    }
    if (isAsyncIterable(result)) {
      await streamNdjson(res, tapStream("ai/send-custom", result));
      return;
    }
    res.json(await result);
  }),

  sendWithStream: asyncHandler<SendStreamInput>(async (req, res) => {
    markForwardHeadersToProvider();
    // A body of JSON `null` (the parser runs with `strict: false`; a request without a body
    // arrives as `{}`): the reads below would throw on it. The gate is asked first, so that a
    // caller without AI access gets the 403 before a word about the body
    // (ts-ai-failure-paths.md §3); any other body reaches the gated read of the round itself.
    if (req.body === null || req.body === undefined) {
      await assertAiAccess();
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    // One aggregate read for the whole round — every storage read below
    // (profiles, assignments, prefs, MCP servers, thread, history, folder
    // metadata) is served from it. See storage/chatContext.ts.
    if (!(await primeRound(req.body, res))) {
      return;
    }
    // A round inside an AI project runs on the project's model and linked
    // rooms, whatever the composer sent (see storage/aiProjectChatContext.ts).
    {
      const refusal = await applyAiProjectChatContext(req.body);
      if (refusal) {
        res.status(refusal.status).json(refusal.body);
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    if (!(await preflightStreamRound(req.body, res))) {
      return;
    }
    const body = await withContextPrompt(
      await withToolsPrompt(await withSourceMetadata(withRequestSignal(res, req.body))),
    );
    // The round's ids as the client sent them (see `toolsScopeOf`). An
    // in-engine tool upload (a generated image) targets
    // `contextEntityId ?? entityId`; with neither it goes to My documents.
    logger.info(
      `ai/send-with-stream: round threadId=${req.body.threadId ?? "<new>"} ` +
        `entityId=${req.body.entityId ?? "-"} contextEntityId=${req.body.contextEntityId ?? "-"} ` +
        `profileId=${req.body.profileId ?? "-"}`,
    );
    await streamNdjson(res, tapStream("ai/send-with-stream", engine.sendWithStream(body)));
  }),

  // OpenAI-compatible streaming chat: same input as sendWithStream, but the
  // engine emits OpenAI `chat.completion.chunk` objects (and a native
  // OpenAI error envelope on provider failure), which we frame as SSE.
  sendWithStreamOpenAI: asyncHandler<SendStreamInput>(async (req, res) => {
    markForwardHeadersToProvider();
    // A body of JSON `null` (the parser runs with `strict: false`; a request without a body
    // arrives as `{}`): the reads below would throw on it. The gate is asked first, so that a
    // caller without AI access gets the 403 before a word about the body
    // (ts-ai-failure-paths.md §3); any other body reaches the gated read of the round itself.
    if (req.body === null || req.body === undefined) {
      await assertAiAccess();
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    if (!(await primeRound(req.body, res))) {
      return;
    }
    // A round inside an AI project runs on the project's model and linked
    // rooms, whatever the composer sent (see storage/aiProjectChatContext.ts).
    {
      const refusal = await applyAiProjectChatContext(req.body);
      if (refusal) {
        res.status(refusal.status).json(refusal.body);
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    // The same pre-flight as sendWithStream: this route stores the user
    // message and creates the thread through the same engine path.
    if (!(await preflightStreamRound(req.body, res))) {
      return;
    }
    const body = await withContextPrompt(
      await withToolsPrompt(await withSourceMetadata(withRequestSignal(res, req.body))),
    );
    await streamOpenAiSse(
      res,
      tapStream("ai/send-with-stream-openai", engine.sendWithStreamOpenAI(body), "openai"),
    );
  }),

  regenerateStream: asyncHandler<RegenerateStreamInput>(async (req, res) => {
    markForwardHeadersToProvider();
    // A body that is no object (the parser runs with `strict: false`, so JSON `null`, a string
    // or a number arrive as they are; a request without a body arrives as `{}`), or one without
    // the ids of the round it resumes, is refused. The gate is asked first, so that a caller
    // without AI access gets the 403 before a word about the body (ts-ai-failure-paths.md §3).
    if (!isObject(req.body)) {
      await assertAiAccess();
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    if (!(await primeRound(req.body, res))) {
      return;
    }
    {
      const missing = missingFieldError(req.body, ["threadId"]);
      if (missing) {
        res.status(400).json({ error: missing });
        return;
      }
    }
    // A round inside an AI project runs on the project's model and linked
    // rooms, whatever the composer sent (see storage/aiProjectChatContext.ts).
    {
      const refusal = await applyAiProjectChatContext(req.body);
      if (refusal) {
        res.status(refusal.status).json(refusal.body);
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    // The effective profile and thread, for the document agent tool (see
    // sendWithStream): the sent profile, else the thread's own model.
    {
      const thread = req.body.profileId
        ? undefined
        : await storage.threads.readById(req.body.threadId);
      setRoundProfileId(req.body.profileId ?? thread?.profileId ?? undefined);
      setRoundThreadId(req.body.threadId);
    }
    // Same prompt envelope as sendWithStream: the regenerated round must
    // see the identical workspace-context fragment the original reply had.
    const body = await withContextPrompt(
      await withToolsPrompt(await withSourceMetadata(withRequestSignal(res, req.body))),
    );
    await streamNdjson(res, tapStream("ai/regenerate-stream", engine.regenerateStream(body)));
  }),

  approveToolCall: asyncHandler<ApproveToolCallInput>(async (req, res) => {
    markForwardHeadersToProvider();
    // A body that is no object (the parser runs with `strict: false`, so JSON `null`, a string
    // or a number arrive as they are; a request without a body arrives as `{}`), or one without
    // the ids of the round it resumes, is refused. The gate is asked first, so that a caller
    // without AI access gets the 403 before a word about the body (ts-ai-failure-paths.md §3).
    if (!isObject(req.body)) {
      await assertAiAccess();
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    if (!(await primeRound(req.body, res))) {
      return;
    }
    {
      const missing = missingFieldError(req.body, ["threadId", "messageId", "idx"]);
      if (missing) {
        res.status(400).json({ error: missing });
        return;
      }
    }
    // A round inside an AI project runs on the project's model and linked
    // rooms, whatever the composer sent (see storage/aiProjectChatContext.ts).
    {
      const refusal = await applyAiProjectChatContext(req.body);
      if (refusal) {
        res.status(refusal.status).json(refusal.body);
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    // `allowAlways` stores a standing permission for the tool, and the engine reads it by
    // truthiness: the string "false" would grant it (ts-ai-failure-paths.md §4).
    {
      const allowAlways: unknown = isObject(req.body) ? req.body["allowAlways"] : undefined;
      if (allowAlways !== undefined && allowAlways !== null && typeof allowAlways !== "boolean") {
        res.status(400).json({ error: "allowAlways must be a boolean" });
        return;
      }
    }
    // An approved tool runs in THIS request, not in the one that streamed the
    // round: the document agent tool needs the round's profile and the
    // attachments of the message it answers, resolved from the thread.
    await primeRoundFromThread(req.body.threadId, req.body.profileId);
    const body = await withSourceMetadata(withRequestSignal(res, req.body));
    await streamNdjson(res, tapStream("ai/approve-tool-call", engine.approveToolCall(body)));
  }),

  denyToolCall: asyncHandler<DenyToolCallInput>(async (req, res) => {
    markForwardHeadersToProvider();
    // A body that is no object (the parser runs with `strict: false`, so JSON `null`, a string
    // or a number arrive as they are; a request without a body arrives as `{}`), or one without
    // the ids of the round it resumes, is refused. The gate is asked first, so that a caller
    // without AI access gets the 403 before a word about the body (ts-ai-failure-paths.md §3).
    if (!isObject(req.body)) {
      await assertAiAccess();
      res.status(400).json({ error: "the request body must be an object" });
      return;
    }
    if (!(await primeRound(req.body, res))) {
      return;
    }
    {
      const missing = missingFieldError(req.body, ["threadId", "messageId", "idx"]);
      if (missing) {
        res.status(400).json({ error: missing });
        return;
      }
    }
    // A round inside an AI project runs on the project's model and linked
    // rooms, whatever the composer sent (see storage/aiProjectChatContext.ts).
    {
      const refusal = await applyAiProjectChatContext(req.body);
      if (refusal) {
        res.status(refusal.status).json(refusal.body);
        return;
      }
    }
    // Resolve the round's custom MCP servers into the request context so
    // the engine's sync systemServerTypes callback sees their names
    // (approval gating) before the tools adapter fires.
    await primeCustomServers();
    const body = await withSourceMetadata(withRequestSignal(res, req.body));
    await streamNdjson(res, tapStream("ai/deny-tool-call", engine.denyToolCall(body)));
  }),
};
