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

import type { RouteSpec } from "@onlyoffice/ai-chat/core";
import { EXAMPLE_AT_MS, EXAMPLE_IDS } from "./exampleIds.js";

// OpenAPI document generation for the AI service.
//
// The engine routes are declared as data (the `DEFAULT_*_ROUTES`
// `RouteSpec` maps from `@onlyoffice/ai-chat/core`) and registered in a
// loop in `routes.ts`. Rather than hand-maintain a second, drift-prone copy
// of that surface as a static spec, this builder derives the OpenAPI 3.0
// document from the very same maps at startup, so a route added to the
// engine package appears in the spec automatically. Custom routes that are
// not backed by an engine (agents, text-to-docx) are described alongside
// their registration via `CustomRouteDoc` entries.

// A minimal structural subset of the OpenAPI object graph — just enough to
// type the builder without pulling in an external schema dependency.
type Json = string | number | boolean | null | Json[] | { [k: string]: Json };
type OpenApiDocument = Record<string, Json>;

// Describes one engine group so its routes get a shared tag and prose.
export interface EngineDoc {
  /** Engine key as used in `routes.ts` (`ai`, `profiles`, …). */
  readonly name: string;
  /** Display tag shown in the docs UI. */
  readonly tag: string;
  /** One-line description of the engine group. */
  readonly description: string;
  /** The engine's `DEFAULT_*_ROUTES` map (method name → route spec). */
  readonly routes: Readonly<Record<string, RouteSpec>>;
}

// Describes a route that is not backed by an `@onlyoffice/ai-chat` engine
// (registered explicitly in `routes.ts`). Kept next to that registration so
// the two stay in sync.
export interface CustomRouteDoc {
  readonly method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  /** Path relative to the API prefix, e.g. `/agents/{id}` (OpenAPI style). */
  readonly path: string;
  readonly tag: string;
  readonly summary: string;
  /**
   * Unique operation id. Required so the merged documentation tool
   * (`OpenapiJoiner`) can name the generated SDK method; it must not clash
   * with any other service's ids, hence the `ai`-scoped values.
   */
  readonly operationId: string;
  /** Path parameter names present in `path` (the `{name}` segments). */
  readonly pathParams?: readonly string[];
  /** Whether the operation accepts a JSON request body. */
  readonly hasBody?: boolean;
}

export interface OpenApiOptions {
  /**
   * Base path the service is mounted under behind the DocSpace nginx, e.g.
   * `/api/2.0/ai`. Every path key is emitted absolute (prefixed with
   * this value, `/api/2.0/ai/ai/send`) so both the standalone docs UI
   * and any client show the full, proxy-correct URL, and so the documents
   * merge cleanly with the .NET services.
   */
  readonly apiPrefix: string;
  readonly engines: readonly EngineDoc[];
  readonly customRoutes: readonly CustomRouteDoc[];
  /**
   * Description per tag used by `customRoutes` and not owned by an engine.
   * Every distinct custom tag gets a global `tags` entry regardless; this
   * only supplies its prose, so a missing key degrades the description
   * rather than dropping the declaration.
   */
  readonly customTagDescriptions?: Readonly<Record<string, string>>;
  /**
   * Generated schema bundle from the build-time generator (see
   * `scripts/generate-openapi.ts`). Omit to emit the generic-object
   * fallback for every body/response.
   */
  readonly schemas?: OpenApiSchemaBundle;
}

/** Per-operation request/response schemas, inlined into the document. */
export interface OperationSchemas {
  readonly request?: unknown;
  readonly response?: unknown;
}

/**
 * Generated schemas: shared named types go to `components.schemas`, while
 * each operation's request/response schema is inlined at its media type.
 * Inlining is deliberate — a component per operation would make SDK
 * generators emit a model class for every bare `$ref`, primitive and array
 * alias, which does not compile.
 */
export interface OpenApiSchemaBundle {
  readonly components?: Readonly<Record<string, unknown>>;
  readonly operations?: Readonly<Record<string, OperationSchemas>>;
}

// Tags are grouped under this single heading (via `x-tagGroups`) and each
// tag name is namespaced with the same prefix (`AI / AI`, …). The prefix
// matches the .NET AI service's `AI / *` tags, so the two documents share
// one group and identically named tags collapse once merged.
const TAG_GROUP = "AI";
const TAG_PREFIX = `${TAG_GROUP} / `;

function tag(name: string): string {
  return `${TAG_PREFIX}${name}`;
}

// Query params known to be optional; everything else in a `RouteSpec.params`
// list is treated as required. Numeric params get an `integer` schema.
//
// `RouteSpec.params` is a bare list of names with no optionality attached, so
// required-by-default is the safe assumption - but it has to be corrected here
// for every name the handlers do not in fact insist on, or the document claims
// a stricter contract than the service enforces and the generated SDK forces
// callers to pass an argument the server ignores when absent.
//
// The list below is what the controllers actually do: a required query param
// is rejected with an explicit 400 (`actionType`, `id`, `name`, `profileId`,
// `providerType`, `baseUrl`, `threadId`, `messageId`, and the
// `serverType`/`toolName` pair), while these six are read through
// `asString`/`parseInt10`/`parseThreadsCursor`/`parseMessagesCursor`/
// `parseDirection`, which all yield `undefined` on an absent value and let the
// engine apply its own default. A name means the same thing wherever it
// appears, as with `PARAM_DOCS` - none of the six is guarded in any operation.
const OPTIONAL_QUERY_PARAMS = new Set([
  "limit",
  "startIndex",
  "count",
  "cursor",
  "direction",
  "entityId",
  "folderId",
  "query",
]);
const INTEGER_QUERY_PARAMS = new Set(["limit", "startIndex", "count"]);

// One-line description per parameter name. Engine routes declare their inputs
// as a positional `RouteSpec.params` list of bare names with no prose attached,
// and custom routes list their `{...}` segments the same way, so the text has
// to come from here. A name means the same thing wherever it appears
// (`entityId` is always the scope the chat runs in); the few that do not are
// overridden per operation below.
//
// A parameter with no entry is emitted without `description`: it is an OpenAPI
// lint finding (`oas3-parameter-description`) and reaches the generated SDK
// undocumented. There is deliberately no humanized fallback - it would fill the
// field with noise and hide that debt - so a new engine or custom-route
// parameter belongs in one of these two tables.
const PARAM_DOCS: Readonly<Record<string, string>> = {
  actionType:
    'The AI action the request applies to - one of "Default", "Chat", "Code", "Summarization", "Translation", "TextAnalyze", "ImageGeneration", "OCR", "Vision".',
  count: "The maximum number of items to return in one page.",
  cursor:
    "The keyset pagination cursor: the JSON-encoded sort key of the last item already received. Omit for the first page.",
  direction:
    'The order the message page is read in. Only "desc" turns the read around and pages back from the newest message; omit for the forward read.',
  entityId:
    "The DocSpace entity the request is scoped to - the room, folder or agent workspace the chat is invoked from. Omit for the portal-wide scope.",
  folderId: "The prompt folder identifier. Omit to list the prompts that sit outside any folder.",
  limit: "The maximum number of items to return.",
  messageId: "The globally unique chat message identifier.",
  name: "The custom MCP server name.",
  profileId: "The AI provider profile identifier.",
  query: "The full-text query the thread list is filtered by.",
  serverType: "The MCP server type the tool belongs to.",
  startIndex: "The zero-based index of the first item to return.",
  threadId: "The chat thread identifier.",
  toolName: "The tool name.",
};

// Per-operation overrides, keyed by `operationId` then parameter name. Needed
// for `id`, which the engines reuse for four different entities.
const OPERATION_PARAM_DOCS: Readonly<Record<string, Readonly<Record<string, string>>>> = {
  aiAgentsGet: { id: "The agent identifier." },
  aiAgentsUpdate: { id: "The agent identifier." },
  aiAgentsDelete: { id: "The agent identifier." },
  aiProfilesGetById: { id: "The AI provider profile identifier." },
  aiPromptsGetById: { id: "The saved prompt identifier." },
  aiPromptsGetFolderById: { id: "The prompt folder identifier." },
};

// One realistic value per parameter name, keyed the same way as `PARAM_DOCS`.
// A placeholder teaches nothing and assistants generate parsing code from these,
// so the values here are shaped like the real ones: an agent ID is the integer
// DocSpace uses for a room, a profile ID is a UUID, and the cursor is the
// JSON-encoded sort key the previous page ended on rather than an opaque token.
const PARAM_EXAMPLES: Readonly<Record<string, Json>> = {
  actionType: "Chat",
  count: 20,
  cursor: `{"id":"${EXAMPLE_IDS.thread}","lastEditDate":${EXAMPLE_AT_MS}}`,
  direction: "desc",
  entityId: EXAMPLE_IDS.room,
  folderId: EXAMPLE_IDS.promptFolder,
  limit: 20,
  messageId: EXAMPLE_IDS.message,
  name: "acme-mcp",
  profileId: EXAMPLE_IDS.profile,
  query: "contract",
  serverType: "docspace",
  startIndex: 0,
  threadId: EXAMPLE_IDS.thread,
  toolName: "docspace_get_folder",
};

// Per-operation examples for `id`, which the engines reuse for four entities,
// and for the agent routes, whose `id` is an integer room ID rather than a UUID.
const OPERATION_PARAM_EXAMPLES: Readonly<Record<string, Readonly<Record<string, Json>>>> = {
  aiAgentsGet: { id: EXAMPLE_IDS.room },
  aiAgentsUpdate: { id: EXAMPLE_IDS.room },
  aiAgentsDelete: { id: EXAMPLE_IDS.room },
  aiProfilesGetById: { id: EXAMPLE_IDS.profile },
  aiPromptsGetById: { id: EXAMPLE_IDS.prompt },
  aiPromptsGetFolderById: { id: EXAMPLE_IDS.promptFolder },
  aiOpenaiChatCompletions: { profileId: EXAMPLE_IDS.profile },
  aiOpenaiImagesGenerations: { profileId: EXAMPLE_IDS.profile },
};

// Resolve the prose for one parameter of one operation, or `undefined` when
// neither table describes it (see the note above `PARAM_DOCS`).
function paramDescription(operationId: string, name: string): Json {
  const description = OPERATION_PARAM_DOCS[operationId]?.[name] ?? PARAM_DOCS[name];
  return description === undefined ? {} : { description };
}

// The example belongs on the parameter's schema, which is where the rest of the
// document carries its examples.
function paramExample(operationId: string, name: string): Json {
  const example = OPERATION_PARAM_EXAMPLES[operationId]?.[name] ?? PARAM_EXAMPLES[name];
  return example === undefined ? {} : { examples: [example] };
}

// Prose per operation, keyed by `operationId`. Engine routes carry no prose at
// all - their `summary` is `humanize(methodName)`, derived from the route map -
// and a custom route only declares a one-line `summary`, so the paragraph a
// reader of the reference (and of the generated SDK method doc) actually needs
// has to come from here. The engine entries below are distilled from the
// `@onlyoffice/ai-chat` engine classes' own JSDoc, the custom ones from the
// controllers in `app/controllers`.
//
// An operation with no entry is emitted without `description`: it is an OpenAPI
// lint finding (`operation-description`) and reaches the generated SDK
// undocumented. As with `PARAM_DOCS` there is deliberately no humanized
// fallback - it would fill the field with noise and hide that debt - so a new
// engine method or custom route belongs here.
const OPERATION_DOCS: Readonly<Record<string, string>> = {
  // AI - chat rounds and tool-call resumption.
  aiAiSend:
    "Runs one AI action and returns the whole answer as a single JSON document. The model is the profile bound to `actionType`, falling back to the `Default` assignment slot, so this operation accepts no `profileId` of its own. Nothing is persisted - no thread is opened, no message is stored and no title is generated - which makes it the one to use for a stand-alone completion rather than for a conversation. `entityId` and `contextEntityId` set the scope of the round, which decides the workspace context and the custom MCP servers it may reach. For a conversation that keeps its history, use `POST api/2.0/ai/ai/send-with-stream` instead.",
  aiAiSendCustom:
    "Runs a free-form one-turn call against a system prompt supplied in the request, with no thread, no history and nothing persisted. The model is the explicit `profileId` when it resolves, otherwise the `Default` assignment slot. The shape of the answer depends on the body rather than on the route: with `isStream` set it arrives as a newline-delimited stream of chat events, and without it as a single JSON document, so a client has to handle both. Use `POST api/2.0/ai/ai/send` when the prompt should come from the portal's own action configuration instead of from the caller.",
  aiAiSendWithStream:
    "Runs one chat round and streams it back as newline-delimited `ChatEvent` objects. Omitting `threadId` opens a new thread, which requires that `entityId` names a room the caller can open and that a profile resolves for it; the user message and the reply are persisted either way, and a new thread also gets a generated title. The model is settled in a fixed order - an agent's assignment in scope overrides everything, then the explicit `profileId`, then the one stored on the thread, then the `Chat` assignment - and the effective profile is checked before the stream opens, so an unknown one fails with 400 rather than as an error buried in a 200. A tool call pauses the round and ends the stream; resume it with `POST api/2.0/ai/ai/approve-tool-call` or `POST api/2.0/ai/ai/deny-tool-call`.",
  aiAiSendWithStreamOpenAI:
    'The same chat round as `send-with-stream`, re-encoded as a server-sent-events stream of OpenAI `chat.completion.chunk` objects terminated by a `[DONE]` sentinel. Thread handling, persistence, title generation and the profile pre-flight are identical, and a tool call ends the stream with `finish_reason: "tool_calls"` instead of a pause event - resume it through the same approve and deny operations. Unlike `send-with-stream` it does not reject an empty user message and does not enforce the per-kind attachment cap, so validate both before calling. Choose this route only for a client that already speaks the OpenAI wire format; `POST api/2.0/ai/ai/send-with-stream` is the native one.',
  aiAiRegenerateStream:
    "Re-rolls the last assistant reply of an existing thread: every message after the last user message - the previous reply and any tool-call hops - is dropped, and a fresh reply is streamed as newline-delimited `ChatEvent` objects against the unchanged prompt. The thread has to exist already, `threadId` is required, and no title is generated. The dropped messages are gone for good, so this is a destructive operation on the thread's tail rather than a retry that keeps both answers. Unlike `send-with-stream` the profile is not verified before the stream opens, so an unusable model surfaces as an error frame inside the 200 rather than as a 4xx.",
  aiAiApproveToolCall:
    "Resumes a chat round that a tool call has paused, and streams the continuation as newline-delimited `ChatEvent` objects. The result supplied in the request is persisted onto the assistant message that issued the call, so the tool is not executed here - the caller runs it and reports the outcome. The round continues against the augmented history and may pause again on a further tool call. Call `POST api/2.0/ai/ai/deny-tool-call` instead to refuse the call and let the model answer without it.",
  aiAiDenyToolCall:
    'Refuses the tool call a chat round is paused on and resumes it immediately, streaming the continuation as newline-delimited `ChatEvent` objects. The literal `"User deny tool call"` is persisted in place of the tool result, so the model sees an explicit refusal rather than a missing answer and may reply without the tool or ask for something else. Nothing is executed and no result is accepted from the caller. Use `POST api/2.0/ai/ai/approve-tool-call` to supply a result instead.',

  // Agents - delegated to the .NET AI service, with the profile binding kept here.
  aiAgentsList:
    "Lists the portal's AI agent rooms. The query is forwarded unchanged to the DocSpace AI service, so it takes the same paging, sorting and filtering parameters as an ordinary room listing, and the answer is that service's folder-content payload rather than a shape of this API's own. Array and object query values are dropped rather than guessed at, so send flat strings. The profile bound to each agent is not included here - read one agent with `GET api/2.0/ai/agents/{id}` for that.",
  aiAgentsCreate:
    "Creates an AI agent room and binds a model to it, in that order. `profileId` is required, has to be a UUID, has to name an existing profile, and that profile has to support chat - an image-only model is refused here rather than failing on every later request. `prompt` is required and is stored on the room as its standing instruction with any markup stripped, so it cannot round-trip HTML into another user's reply. The two steps are not atomic: when the room is created but the model binding fails, the call reports an error and the room is left behind, so re-bind it with `PUT api/2.0/ai/agents/{id}` rather than creating a second one.",
  aiAgentsNews:
    "Lists the unread items across the caller's AI agent rooms, so a badge can be rendered without walking each room. It takes no parameters and is scoped to the caller by the DocSpace AI service. The answer is that service's new-items payload. This is a read-only operation and does not mark anything as seen.",
  aiAgentsGet:
    "Returns one AI agent room, enriched with the `profileId` currently bound to it so an edit form can prefill its model selector. The ID is the room's integer identifier, and a non-integer value is refused rather than passed on to fail opaquely upstream. The binding lives in an assignment rather than on the room, so it is looked up separately: a missing or unreadable assignment simply leaves `profileId` out of the answer instead of failing the call. The standing instruction comes back on the room as `chatSettings.prompt`.",
  aiAgentsUpdate:
    "Changes an AI agent room - its title, tags or standing instruction - and optionally rebinds its model. The ID has to be the room's integer identifier. `profileId` is not part of the room contract: it is taken out of the forwarded body and applied afterwards as the agent's assignment, and it has to be a UUID naming an existing chat-capable profile. An instruction sent as `chatSettings.prompt` has its markup stripped, as on create; note that when `chatSettings` is present the upstream service still requires the rest of that object to be valid, so send it whole.",
  aiAgentsDelete:
    "Deletes an AI agent room. The ID has to be the room's integer identifier, and the body is forwarded to the DocSpace AI service unchanged, so it accepts the same options as deleting an ordinary room - `deleteAfter` among them. Deletion is asynchronous there: the answer is a file-operation payload to poll, not a completed result. The agent's model binding is deliberately left behind, because the upstream assignment API has no per-entry delete, so an orphaned assignment row survives the room.",
  aiAgentsUpdateQuota:
    "Sets the storage quota of the listed AI agent rooms in one call, forwarding `roomIds` and `quota` to the DocSpace AI service unchanged. The answer is that service's payload, one updated room per entry. A quota applies to the room's stored files, not to the model usage of its chats. Use `PUT api/2.0/ai/agents/resetquota` to return rooms to the portal default instead of naming a number.",
  aiAgentsResetQuota:
    "Returns the listed AI agent rooms to the portal's default storage quota, forwarding `roomIds` to the DocSpace AI service unchanged. The answer is that service's payload, one updated room per entry. This is the counterpart of `PUT api/2.0/ai/agents/agentquota` and takes no quota value of its own. Rooms already on the default are unaffected.",

  // Assignments - which profile serves which AI action.
  aiAssignmentsResolveForAction:
    "Returns the profile that will serve one AI action, falling back to the `Default` slot when the action has no profile of its own. `actionType` is required and has to be one of the known actions - an unknown or misspelled value is rejected rather than resolved to the default. `entityId` narrows the lookup to a room, and a room with no assignment of its own degrades to the portal-wide one. This fails when neither slot is set or the bound profile is gone, so use `GET api/2.0/ai/assignments/try-resolve-for-action` when an unconfigured portal should answer empty instead.",
  aiAssignmentsTryResolveForAction:
    "Returns the profile that will serve one AI action, exactly as `GET api/2.0/ai/assignments/resolve-for-action` does, but answers with an empty result rather than failing when nothing is configured. `actionType` is required and is validated the same way, and `entityId` narrows the lookup to a room. This is the operation to call when the absence of a profile is a normal state to render - a settings screen, or a feature that hides itself. Both operations are read-only.",
  aiAssignmentsAssign:
    "Binds a profile to one AI action portal-wide, creating the assignment or replacing it in place, and returns the result. Both `actionType` and `profileId` are required. The profile's declared capabilities are checked against the action, so a model that cannot generate images cannot be bound to `ImageGeneration` - the `Default` slot is exempt, because it stands in for every action. There is no room-scoped form of this write: a room's own binding is created by the agent that owns it, while reads accept an `entityId`.",
  aiAssignmentsUnassign:
    "Clears the portal-wide binding of one AI action, after which the action falls back to the `Default` slot. `actionType` is required and may be sent in the body or as a query parameter. An action whose slot is already empty is not reported as an error - the call answers success either way, so it is safe to repeat. Clearing `Default` itself leaves the actions that relied on it unresolvable.",
  aiAssignmentsBulkAssign:
    "Applies many action-to-profile bindings in one write, which is how a settings screen saves the whole set. The body is a plain map of action type to profile ID, and every entry is validated before anything is written: one unknown action or one non-string profile ID rejects the request whole, so the set is never left half-applied. Each entry behaves as the single assign operation does, capability checks included. The answer carries the resulting assignment set.",
  aiAssignmentsGetAssignment:
    "Returns the profile bound to one AI action, without applying the `Default` fallback - an empty answer means this action has no profile of its own, not that nothing is configured. `actionType` is required and is read from the query. Use `GET api/2.0/ai/assignments/resolve-for-action` to learn which profile would actually serve the action. This reads the portal-wide binding and accepts no `entityId`.",
  aiAssignmentsGetAllAssignments:
    "Returns every action-to-profile binding of a scope as one map, which is what a settings screen loads. `entityId` narrows it to a room and has to name one the caller can open; a room that is not an agent room degrades to the portal-wide set rather than answering empty, and omitting the parameter reads the portal-wide set directly. Actions with no binding are simply absent from the map. The `Default` slot is reported as an entry of its own rather than being folded into the others.",
  aiAssignmentsCascadeProfileDelete:
    "Detaches a profile from every assignment that points at it, which is the cleanup step before the profile itself is removed. The `Default` slot is promoted to the first remaining profile, or dropped when none is left, and every other slot holding the profile is cleared. `profileId` is required and may be sent in the body or as a query parameter. `DELETE api/2.0/ai/profiles/delete` already does this, so call it directly only when the profile is being removed by some other means.",

  // Attachments - message files and images, saved as drafts first.
  aiAttachmentsSaveFile:
    "Stores one file attachment as a draft and returns it, so its ID can be attached to a message later. `input` carries the host `path` - the DocSpace entry ID the AI backend resolves server-side - the text `content` already extracted from that file, the ONLYOFFICE numeric file `type`, and optionally a `title`; the text is what the model reads, so this operation does not open the file itself. Archives are refused outright, whatever their declared name says. Drafts are not bound to a conversation until `POST api/2.0/ai/attachments/link-to-message` is called, so an unlinked draft outlives the round that created it.",
  aiAttachmentsSaveFilesMany:
    'Stores several file attachments as drafts in one round trip and returns them in the order they were sent. Each entry is validated exactly as the single-file operation validates its `input`, and the first bad one rejects the whole batch with its index named in the message - nothing is stored. `inputs` has to be present and an array: an absent or null value is a malformed request rather than an empty batch, and only an explicit empty array means "no files". Follow up with `POST api/2.0/ai/attachments/link-to-message` to bind the drafts to a message.',
  aiAttachmentsGet:
    'Returns one attachment by its ID, whether it is still a draft or already bound to a message. The ID is required and has to be a non-empty string. An ID that no longer exists is not reported as 404: the answer is a null body with status 200, so treat a missing payload as "no such attachment". Use `POST api/2.0/ai/attachments/get-many` to read several at once.',
  aiAttachmentsGetMany:
    "Returns several attachments in one call, aligned by position with the `ids` that were sent, so the answer can be zipped straight onto the request. An ID that no longer exists leaves its slot empty rather than shortening the list, which is how a caller tells which of them are gone. `ids` has to be present and non-empty - an empty batch is rejected rather than answered with an empty list. Nothing is changed by the call.",
  aiAttachmentsDelete:
    "Permanently deletes one attachment, whether it is still a draft or already bound to a message. The ID is not validated here, so a malformed one surfaces as an error relayed from storage rather than as a 400, and an ID that does not exist answers success without deleting anything. Deleting a bound attachment leaves the message in place without it. The deletion cannot be undone.",
  aiAttachmentsDeleteMany:
    "Permanently deletes several attachments in one round trip. `ids` is optional and an absent value is treated as an empty list, so a malformed request quietly deletes nothing instead of failing. IDs that do not exist are skipped without being reported, so the answer confirms only that the call was accepted. The deletions cannot be undone.",
  aiAttachmentsLinkToMessage:
    "Binds draft attachments to the chat message that owns them, after that message has been persisted, so that deleting the message removes them too. All three of `ids`, `messageId` and `threadId` are required, and the references are verified rather than trusted: an unknown message answers 404, a message that belongs to a different thread answers 400, and attachments that no longer exist answer 404 naming each missing ID. That verification exists because the underlying binding call skips unknown IDs silently, which used to report success for a link that had not happened. Drafts stay unbound until this succeeds.",

  // Editor tools - DocSpace tools exposed to the document editor's AI plugin.
  aiEditorToolsList:
    "Returns the catalogue of DocSpace tools the document editor's AI plugin may offer the model - the same composed set the DocSpace chat sees, minus the two web-search tools the editor already reaches through its own passthrough. `entityId` scopes the catalogue to a room, which decides the room-specific tools it contains. Each entry carries exactly four fields: the tool name, its description, its input schema, and whether calling it requires an approval dialog; nothing else is exposed, because the raw listings of system servers carry transport details that must not reach a browser. The approval flag follows the same policy the chat engine applies, and a read-only tool comes back needing none - execute a tool with `POST api/2.0/ai/editor-tools/call`, which accepts only the names this catalogue reports.",
  aiEditorToolsCall:
    "Executes one DocSpace tool on behalf of the document editor's AI plugin, server-side and under the caller's own credentials, so the browser never holds the transport. `name` has to be one of the tools `GET api/2.0/ai/editor-tools/list` reports; anything else, including a tool the editor is not allowed to reach, is refused. The result is always returned as a string - a structured result is serialised - because the plugin relays it to the model verbatim. A tool that fails does so inside that string as an error payload rather than as an HTTP status, so check the content before trusting it.",

  // Export.
  aiExportTextToDocx:
    "Queues a markdown-to-docx export and answers 202 as soon as the job is accepted, without waiting for it. `title`, `content` and `folderId` are all required, and a `content` of only whitespace counts as missing even though it is not empty. The conversion runs in the AI worker, which saves the .docx into the target folder - an agent room resolves to its own result-storage subfolder - so there is nothing to poll here: completion arrives as the ordinary folder-modified socket event. This route accepts a body of up to 15 MB rather than the 100 KB the rest of the API allows, because a whole thread transcript is sent in one request.",

  // OpenAI passthrough - the editor plugin's external-provider transport.
  aiOpenaiChatCompletions:
    "OpenAI-compatible chat completions for the document editor's AI plugin. The profile is resolved server-side, its credentials are attached, and the body is forwarded to the provider verbatim - the payload is owned by the plugin's SDK on one end and the provider on the other. A client disconnect cancels the provider call.",
  aiOpenaiImagesGenerations:
    "OpenAI-compatible image generation for the document editor's AI plugin, working exactly as the chat-completions passthrough does: the profile named by `profileId` is resolved server-side, its credentials are attached, and the body reaches the provider unchanged. The provider's status and body are relayed verbatim, so its 429 and its own error envelope surface as they stand. A body larger than this route accepts is refused before it is forwarded. A client disconnect aborts the provider call.",

  // Preferences - per-scope extended-thinking settings. One value is stored
  // per scope (the depth); deep mode is its on/off view.
  aiPreferencesGetDeepMode:
    'Returns the deep-mode toggle of a scope, as a bare boolean: whether the stored extended-thinking depth is above `off`. `entityId` picks a room and omitting it reads the portal-wide preference. A scope that has never had a value stored falls back to the configured default, so the answer never distinguishes "off" from "unset" - ask `GET api/2.0/ai/preferences/is-deep-mode-set` for that. This is a read-only operation.',
  aiPreferencesSetDeepMode:
    'Stores the deep-mode toggle of a scope. `false` stores the `off` depth; `true` keeps the depth already stored and falls back to the default depth (`medium`) when none is. `value` has to be a real boolean: a string, a number or an absent value is rejected rather than coerced, so the string "false" cannot silently switch the setting on and an empty request cannot silently switch it off. `entityId` picks a room and omitting it writes the portal-wide preference. It is idempotent, so there is no need to read the current value first.',
  aiPreferencesClearDeepMode:
    "Removes the stored extended-thinking setting of a scope (the depth and, with it, the deep-mode toggle), after which reads fall back to the configured default rather than to false. `entityId` picks a room and omitting it clears the portal-wide preference. Clearing a scope that has no stored value is not an error. This differs from storing false, which is an explicit choice a later read reports as set.",
  aiPreferencesIsDeepModeSet:
    "Tells whether a scope has an explicitly persisted extended-thinking setting of its own, as opposed to inheriting the configured default. `entityId` picks a room and omitting it asks about the portal-wide preference. A true answer means a value was stored, whether that value is on or off - read the value itself with `GET api/2.0/ai/preferences/get-deep-mode`. This is the check a settings screen uses to show an explicit override rather than an inherited state.",
  aiPreferencesGetReasoningLevel:
    "Returns the effective extended-thinking depth of the scope: `off` while deep mode is off, otherwise the persisted depth (`low`, `medium`, `high`, `max`), falling back to the default depth (`medium`) when none has been stored. `entityId` picks a room and omitting it reads the portal-wide preference. Providers clamp the depth to what the model accepts.",
  aiPreferencesSetReasoningLevel:
    "Persists the extended-thinking depth of the scope as its single stored value: a depth turns deep mode on at that depth, `off` turns it off and replaces the stored depth (a later deep-mode `true` without a depth lands on `medium`). `entityId` picks a room and omitting it writes the portal-wide preference. Idempotent.",

  // Profiles - AI provider credentials and model discovery.
  aiProfilesCreate:
    'Creates an AI provider profile - the endpoint, credentials and model that a chat round runs on - and returns it. The name has to be unique, the credentials are probed against the live provider before anything is stored, and the portal\'s first profile also takes the `Default` assignment slot. Two inputs are refused outright: a `baseUrl` pointing at a private network address, and `providerType: "external"`, which delegates transport to the host application and therefore cannot work for a profile the server manages. On a portal running the AI gateway, profiles are managed centrally and this operation answers 403.',
  aiProfilesUpdate:
    'Replaces a stored AI provider profile and returns it, re-checking name uniqueness and probing the credentials against the live provider again. The same two inputs are refused as on create - a private-network `baseUrl` and `providerType: "external"` - and the whole profile is overwritten by the one supplied rather than merged. On a portal running the AI gateway this answers 403, because profiles are managed centrally there. A profile that is bound to an action or an agent keeps those bindings.',
  aiProfilesDelete:
    "Deletes an AI provider profile and cleans up every assignment pointing at it: the `Default` slot moves to the first remaining profile and the other slots are left unbound. The ID is required and may be sent in the body or as a query parameter. An unknown ID is not reported - the call answers success without deleting anything. Threads already bound to the profile keep the stored reference, so a round on such a thread falls back to whatever the scope resolves to.",
  aiProfilesGetById:
    "Returns one AI provider profile by its ID, with its secrets stripped: neither the API key nor the custom headers are ever sent back, on any portal. The ID is required and is read from the query, and an unknown one answers 404. The `baseUrl` in the answer is the one that was stored, not the internal gateway address a round actually dials, so it cannot be used to reach the provider directly. Use `GET api/2.0/ai/profiles/list` to enumerate profiles instead of reading them one by one.",
  aiProfilesList:
    "Lists the portal's AI provider profiles with their secrets stripped, the same way the single-profile read does. It takes no parameters and is not paginated, because a portal holds few profiles. On a portal running the AI gateway the answer is synthesised from the gateway's own catalogue rather than from stored records. The IDs in the answer are what the assignment operations and every round's `profileId` accept.",
  aiProfilesListModels:
    "Lists the models a stored profile's provider currently offers, asking the provider itself rather than reading a cached list. `profileId` is required and is read from the query. A failure is reported with the provider's own verdict: an unusable key comes back as 400 and a provider that is unreachable or broken as 502, while a missing profile or a caller without access keeps the status the portal gave it. Use `POST api/2.0/ai/profiles/list-provider-models` to probe an endpoint that has no profile yet.",
  aiProfilesListProviderModels:
    "Lists the models an endpoint offers for credentials supplied in the request, before any profile exists - this is what a provider-setup form calls to fill its model picker. `providerType` and `baseUrl` are both required, and a 400 for either names the offending input in a `field` member so the form can highlight it; a `baseUrl` pointing at a private network address is refused as well. For `providerType: \"onlyoffice\"` the answer comes from the portal gateway's catalogue, which carries richer capability data than the provider's own listing and matches what `GET api/2.0/ai/profiles/list` reports; a portal without that gateway falls back to asking the provider. A provider that is unreachable or broken is reported as 502, and one that rejects the key as 400.",
  aiProfilesTestConnection:
    "Probes a stored profile's credentials against its provider and reports the outcome in the answer, writing nothing - this is what a Test button calls so that a failure does not commit anything. `profileId` is required and may be sent in the body or as a query parameter. The result is carried in the body rather than in the status, so a failed probe still answers 200 and the caller has to read the payload. To validate credentials that are not stored yet, use `POST api/2.0/ai/profiles/list-provider-models`.",

  // Prompts - the saved prompt library and its folders.
  aiPromptsCreate:
    "Saves a new prompt in the caller's own prompt library and returns it. The name has to be non-empty and unique inside its folder, and `folderId` has to name an existing folder - omit it to save the prompt at the root. Prompts are per-user: another user's library is never visible here, and no permission beyond having AI enabled is needed. The answer carries the stored prompt including the ID to use with the update, move and delete operations.",
  aiPromptsUpdate:
    "Changes a saved prompt and returns the stored result. Only the fields present in `updates` are written, so a partial object leaves the rest of the prompt alone. The name and the folder reference are re-validated whenever either changes, which means an update can fail on a name another prompt in the same folder already uses. Use `PUT api/2.0/ai/prompts/move` to change only the folder.",
  aiPromptsMove:
    "Moves a saved prompt into another folder, or to the root when `folderId` is omitted or null. The name is re-validated in the target folder, so the move fails when a prompt of that name already sits there - rename it first with `PUT api/2.0/ai/prompts/update`. Nothing about the prompt other than its folder changes. The answer carries the moved prompt.",
  aiPromptsDelete:
    "Deletes one saved prompt from the caller's library. The ID may be sent in the body or as a query parameter, and it is required. An ID that does not exist, or that belongs to another user, is not reported: the call answers success without deleting anything. The deletion is permanent.",
  aiPromptsList:
    "Lists the caller's saved prompts, newest first. `folderId` scopes the answer to one folder, and omitting it - or sending it empty - lists the prompts that sit at the root rather than every prompt, because the client fetcher cannot tell an absent value from a null one. There is therefore no way to ask for the whole library in one call: walk the folders from `GET api/2.0/ai/prompts/list-folders`, or take everything at once with `GET api/2.0/ai/prompts/export`. The prompts of other users are never included.",
  aiPromptsGetById:
    'Returns one saved prompt by its ID. The ID is required and is read from the query. An ID that is unknown, or that belongs to another user, is not reported as 404: the answer is an empty body with status 200, so treat a missing payload as "no such prompt". Prompt IDs come from `GET api/2.0/ai/prompts/list` or from the answer of the create operation.',
  aiPromptsCreateFolder:
    "Creates a folder in the caller's prompt library and returns it. The name has to be non-empty and unique across that library. Folders do not nest: there is one flat level, so a folder cannot be created inside another. The answer carries the folder ID to use as `folderId` when saving or moving prompts.",
  aiPromptsRenameFolder:
    "Renames a folder in the caller's prompt library, validating the new name against the folders already there. The prompts inside it are untouched and keep their IDs. The answer carries the renamed folder. A name that another folder already uses is rejected.",
  aiPromptsDeleteFolder:
    "Deletes a folder together with every prompt inside it, permanently. The ID is required and may be sent in the body or as a query parameter. Unlike deleting a prompt, this checks first: a folder that does not exist, and one that belongs to another user, both answer 404 - the two cases are deliberately indistinguishable, so a foreign folder cannot be probed. Move the prompts out with `PUT api/2.0/ai/prompts/move` first if they should survive.",
  aiPromptsListFolders:
    "Lists every folder of the caller's prompt library, newest first, with no parameters and no pagination. Folders are flat, so the answer is a single list rather than a tree. The prompts inside them are not included - read those with `GET api/2.0/ai/prompts/list` per folder. Another user's folders are never listed.",
  aiPromptsGetFolderById:
    "Returns one folder of the caller's prompt library by its ID, without the prompts inside it. The ID is required and is read from the query. An unknown or foreign ID is not reported as 404: the answer is an empty body with status 200. This differs from the delete operation on the same ID, which does answer 404.",
  aiPromptsExport:
    "Builds a versioned bundle of every prompt and folder in the caller's library and returns it, with no parameters. The bundle is self-contained: it carries its own format version so an older export can still be read back, and it is the input `POST api/2.0/ai/prompts/import-bundle` expects. This is also the only way to read the whole library at once, since listing is folder-scoped. Nothing is changed by the call.",
  aiPromptsImportBundle:
    "Writes a bundle produced by `GET api/2.0/ai/prompts/export` back into the caller's library. `mode` decides how: `replace` deletes the current prompts and folders before writing, and `merge` writes the bundle on top of what is already there. The folder references inside the bundle are validated before anything is written, so a corrupt bundle is rejected whole rather than applied halfway. `replace` is destructive and cannot be undone - export first if the current library matters.",

  // Settings - proxied to the .NET AI service.
  aiSettingsGet:
    "Reports the portal's AI configuration and whether AI is usable at all, which is the first call a client makes before offering any AI feature. It takes no parameters and is proxied unchanged to the DocSpace AI service, so the answer is that service's settings payload. Among other things it says whether the portal runs on the central AI gateway, which decides whether provider profiles can be edited here at all. This is a read-only operation.",
  aiSettingsGetVectorization:
    "Returns the portal's vectorization settings - the embedding provider and the options used when portal content is indexed for retrieval. It takes no parameters and is proxied unchanged to the DocSpace AI service. Vectorization is a portal-wide setting, so there is no room-scoped form of it. Change it with `PUT api/2.0/ai/config/vectorization`.",
  aiSettingsSetVectorization:
    "Replaces the portal's vectorization settings and returns the stored result. The body is proxied unchanged to the DocSpace AI service, which validates it, so a rejected value is reported with that service's own verdict rather than being checked here. Changing the embedding provider does not re-index anything already indexed - start that separately with `POST api/2.0/ai/vectorization/tasks`. This is a portal-wide setting and requires the permissions the AI service demands for it.",
  aiSettingsGetUser:
    "Returns the AI settings of the calling user, as opposed to the portal-wide ones. It takes no parameters - the user is the authenticated caller, and there is no way to read somebody else's settings - and is proxied unchanged to the DocSpace AI service. Use `GET api/2.0/ai/config` for the portal-wide configuration. This is a read-only operation.",
  aiSettingsSetUser:
    "Replaces the AI settings of the calling user and returns the stored result. The body is proxied unchanged to the DocSpace AI service, which validates it, so a rejected value comes back with that service's verdict. Only the caller's own settings can be written. Portal-wide configuration is not touched by this operation.",

  // Threads - chat threads and their messages.
  aiThreadsCreate:
    "Creates a chat thread with a title supplied by the caller and returns it. A scoped thread requires that `entityId` names a room the caller can open, and a model has to resolve for the scope - an explicit `profileId`, or the room's `Chat` assignment - otherwise there is nothing to run the thread against and the call answers 404. In an agent room the agent's own assignment overrides any `profileId` sent with the request, so a thread there always starts on the agent's model. Use `POST api/2.0/ai/threads/open-or-create` instead when the title should be generated from the first user message.",
  aiThreadsOpenOrCreate:
    "Opens a chat thread and returns it with its history, or creates one whose title is generated from the first message supplied in the request. That first message is not persisted: follow up with `POST api/2.0/ai/threads/append-user-message` to store it, or start the round directly with `POST api/2.0/ai/ai/send-with-stream`. Unlike `create` this takes a whole resolved `profile` object rather than an ID, and a request without one answers 404 because no model could be bound. A supplied `entityId` has to be a room the caller can open; anything that is not an agent room folds to the global scope instead of being rejected.",
  aiThreadsAppendUserMessage:
    "Stores a user message in a thread and bumps its last-edit date so the thread resurfaces at the top of the list. The per-kind attachment cap of the composer is enforced here as well, so a direct API call cannot exceed what the UI allows. Passing `profileId` rebinds the thread to another model, which is how a mid-conversation model switch is recorded. The answer carries the new message's ID; the message is stored as sent and no reply is generated - run a round with `POST api/2.0/ai/ai/send-with-stream` for that.",
  aiThreadsTouch:
    "Bumps a thread's last-edit date without adding a message, which resurfaces it in the list. Passing `profileId` also rebinds the thread to another model, so this is the operation to call when a model switch alone should count as activity. Nothing else about the thread changes and the answer only confirms the write. It is idempotent: repeating it simply moves the date forward again.",
  aiThreadsRename:
    "Replaces a thread's title with the one supplied and bumps its last-edit date. Both `threadId` and a title with at least one non-whitespace character are required - a blank title is rejected rather than silently stored, so a thread cannot end up nameless. The answer only confirms the write. To have the model produce a title instead of supplying one, use `POST api/2.0/ai/threads/regenerate-title`.",
  aiThreadsDelete:
    "Deletes a thread together with every message in it. The thread has to exist: unlike the other operations that take a `threadId`, this one checks first and answers 404 for an unknown or already-deleted thread rather than reporting success. The deletion is permanent and the messages cannot be recovered. To empty a thread but keep it, use `DELETE api/2.0/ai/threads/clear-messages`.",
  aiThreadsClearMessages:
    "Removes every message of a thread while keeping the thread, its title and its model binding, and bumps its last-edit date. The messages are gone for good. Unlike `delete` this does not verify that the thread exists, so clearing an unknown `threadId` reports success rather than 404. The answer only confirms the write.",
  aiThreadsRegenerateTitle:
    "Asks the model to produce a title from the thread's first user message, stores it, and returns the new title. Both `threadId` and a resolved `profile` object are required; a thread with no user message yet has nothing to title and fails. This costs a model call, unlike `POST api/2.0/ai/threads/rename`, which just stores the string it is given. An `entityMeta` sent with the request is only read for its `entityId` hint - the source itself is resolved server-side under the caller's credentials, so a client cannot attribute the call to somebody else's room.",
  aiThreadsList:
    'Lists the threads of a scope, most recently edited first, and searches their titles case-insensitively when `query` is given. Every parameter is optional: omitting `entityId` lists the global scope, and omitting `count` lets the engine apply its own page size. Pagination is by cursor, and the cursor is a JSON object passed as a string in the query - `{"id": <last thread id>, "lastEditDate": <its date>}` - taken from the last entry of the previous page. A cursor that is not valid JSON, or that lacks an `id`, is ignored rather than rejected, and the read silently starts from the first page again.',
  aiThreadsReadMessages:
    "Reads the messages of one thread, oldest first, with the same string-encoded JSON cursor as the thread list. `direction` turns the read around, and only the exact value `desc` does so - anything else, including a misspelling, reads forward. Omitting `threadId` is not an error: the call answers 200 with an empty list, so an empty result does not distinguish a thread with no messages from a request that forgot the ID. A malformed cursor is ignored and the read starts from the beginning.",
  aiThreadsGetById:
    "Returns one thread by its ID, without its messages - read those with `GET api/2.0/ai/threads/read-messages`. `threadId` is required and an unknown one answers 404, so the result is never an empty body. The answer carries the thread's title, its model binding and its last-edit date. This is a read-only operation and does not bump that date.",
  aiThreadsGetMessageById:
    'Returns one message by its ID, wherever it sits, without needing the thread it belongs to. `messageId` is required. Unlike `GET api/2.0/ai/threads/get-by-id` an unknown ID is not reported as 404: the answer is an empty body with status 200, so a client has to treat a missing payload as "no such message". Message IDs come from the thread history or from the answer of `POST api/2.0/ai/threads/append-user-message`.',
  aiThreadsUpdateMessage:
    "Replaces the content of one stored message, which is how the edit and regenerate flows change a message outside the streaming lifecycle. The whole message is overwritten by the one supplied rather than merged, so send a complete object. Neither the ID nor the payload is validated here, so a malformed request surfaces as an error relayed from storage rather than as a 400. The answer only confirms the write.",
  aiThreadsDeleteMessage:
    "Deletes one message and leaves the rest of the thread untouched. `messageId` is required and may be sent either in the body or as a query parameter. An unknown ID is not reported: the call answers success without having deleted anything, so verify with `GET api/2.0/ai/threads/read-messages` when it matters. The deletion is permanent.",

  // Tools - custom MCP servers and per-tool preferences.
  aiToolsAddCustomServer:
    "Registers a custom MCP server under the given name so the model may call its tools. The name becomes a URL path segment, so it may not be `.`, `..`, or contain a path separator or a control character. `config` may be omitted in two cases: a name matching a host-configured system server pins the entry to that server's canonical settings as a whitelist marker, and a name already registered portal-wide copies the portal-level configuration into this scope; anything else without a config is rejected. `entityId` scopes the registration and has to name a room the caller can open - a room that is not an agent room folds to the portal-wide scope, while an unreachable one is refused so it cannot silently rewrite the portal's own registry.",
  aiToolsUpdateCustomServer:
    "Replaces the stored configuration of a registered custom MCP server, under the same name and scope rules as the add operation. The name is re-validated as a routable path segment, and an omitted `config` resolves the same way - to a system server's canonical settings, or to the portal-level entry of that name. `entityId` has to name a room the caller can open. The answer carries the stored registry entry.",
  aiToolsRemoveCustomServer:
    "Unregisters a custom MCP server from the scope, so the model is no longer offered its tools. The name is required and may be sent in the body or as a query parameter, and `entityId` has to name a room the caller can open. A name that is not registered is not reported: the call answers success without removing anything. The server itself is untouched - only this portal's registration is dropped.",
  aiToolsGetCustomServer:
    "Returns the stored configuration of one registered custom MCP server. The name is required and is read from the query; `entityId` picks the scope, and omitting it reads the portal-wide registry. A name that is not registered answers a null body with status 200 rather than 404. The configuration of a system server is returned empty on purpose: those run server-side only, so neither their endpoint nor their credentials are handed to a browser.",
  aiToolsListCustomServers:
    "Lists the custom MCP servers registered in the scope as a map of name to configuration. `entityId` picks the scope and omitting it lists the portal-wide registry. The configuration of any entry that names a host-configured system server comes back empty, for the same reason as in the single-server read, and the portal's own built-in MCP server is left out of the list entirely because it is always enabled and cannot be configured. The names in the answer are what the disable and always-allow operations accept as `serverType`.",
  aiToolsReplaceAllCustomServers:
    'Replaces the whole custom MCP server registry of the scope with the supplied map in one write, which makes it the operation a settings screen saves with. `map` is required: without it the registry would be emptied, so a missing or non-object value is rejected rather than treated as "none". Every name in the map is validated as a routable path segment and every configuration is resolved before anything is written, so a map with one bad entry changes nothing. `entityId` has to name a room the caller can open - this is the operation where an unreachable one would otherwise have wiped the portal-wide registry.',
  aiToolsListSystemTools:
    "Lists every tool the scope can offer the model, as a map of server type to tool group. The answer merges two sources - the host-configured system servers and the live tools of the scope's registered custom MCP servers - and names the system ones separately in `system`, so a client can tell the two apart. `errors` carries the reason a registered server delivered no tools, which is the text to show on a permission card, because the browser cannot reach a server-executed MCP server to find out for itself. The connections are opened server-side, so one request is enough and the client never speaks MCP itself; the portal's own built-in server is left out because it is always enabled.",
  aiToolsSetDisabled:
    "Switches off the listed tools of one server type in the scope, so the model is no longer offered them. `serverType` has to be a key the round's tool filter actually matches - a host-configured system server, one of the two DocSpace integration groups, web search, image generation, or one of the scope's registered custom servers - and an unknown value is rejected with the list of valid ones in the message, rather than stored and silently ignored. `toolNames` replaces the previous selection for that server type, so send the full list and pass an empty one to switch everything back on. `entityId` has to name a room the caller can open.",
  aiToolsGetDisabled:
    "Returns the tools switched off in the scope, as a map of server type to tool names. `entityId` picks the scope and omitting it reads the portal-wide setting. An absent server type means nothing is switched off for it, so an empty answer means every tool is on offer. Use `GET api/2.0/ai/tools/is-tool-disabled` to ask about one tool instead of reading the whole map.",
  aiToolsIsToolDisabled:
    "Tells whether one named tool of one server type is switched off in the scope. Both `serverType` and `toolName` are required and are read from the query; `entityId` picks the scope. The answer is a bare boolean. It reflects only the disable list - a tool that is on offer may still require approval, which `GET api/2.0/ai/tools/is-allow-always` reports.",
  aiToolsSetAllowAlways:
    "Adds one tool to the scope's always-allow list, or takes it off, which decides whether a call to it pauses the round for approval. `value` is coerced to a boolean, so any truthy value adds and any falsy one removes. Unlike the disable operation, `serverType` is not validated here: an unknown one is stored and then simply never matches, so a wrong value fails silently. `entityId` has to name a room the caller can open.",
  aiToolsGetAllowAlways:
    "Returns the always-allow list of the scope - the tools whose calls run without pausing the round for approval. `entityId` picks the scope and omitting it reads the portal-wide setting. An empty answer means every tool call has to be approved through `POST api/2.0/ai/ai/approve-tool-call`. Use `GET api/2.0/ai/tools/is-allow-always` to ask about a single tool.",
  aiToolsIsAllowAlways:
    "Tells whether one named tool runs without an approval pause in the scope. Both `serverType` and `toolName` are required and are read from the query; `entityId` picks the scope. The answer is a bare boolean. A false answer means a call to that tool pauses the round, and the caller resumes it with the approve or deny operation.",

  // Vectorization.
  aiVectorizationStartTask:
    "Queues the indexing of the portal files named in the body so their contents can be retrieved during a chat round. The body is proxied unchanged to the DocSpace AI service, which validates it and owns the job. Indexing is asynchronous and fire-and-forget: the answer acknowledges the request without carrying a job handle, so there is nothing to poll and progress is not reported here. The embedding provider used is the one in `GET api/2.0/ai/config/vectorization`, and changing that setting does not re-index anything already indexed - queue it again for that.",

  // Web search - the portal's provider configuration, plus the editor passthrough.
  aiWebSearchGetActiveConfig:
    "Returns the web-search configuration in force for a scope - the provider, its endpoint and its settings. `entityId` picks a room and has to name one the caller can open; omitting it reads the portal-wide configuration, and a room with none of its own falls back to that. An unconfigured scope answers an empty result rather than 404. The provider key is not part of the answer, so a client cannot read it back after storing it.",
  aiWebSearchIsConfigured:
    "Tells whether web search is available in a scope, as a bare boolean, which is the cheap check for hiding or showing the feature. `entityId` picks a room and has to name one the caller can open. It reports the same state as `GET api/2.0/ai/web-search/get-active-config` without transferring the configuration itself. A true answer means a provider is stored, not that the provider is currently reachable - probe that with `POST api/2.0/ai/web-search/test-connection`.",
  aiWebSearchTestConnection:
    "Probes a web-search configuration against the live provider and reports the outcome, storing nothing - this is what a Test button calls so that a failure commits no state. The configuration is taken from the request rather than from storage, so credentials that were never saved can be checked. A `baseUrl` pointing at a private network address is refused before any request leaves the portal. The verdict is carried in the body rather than in the status, so a failed probe still answers 200 and the caller has to read the payload.",
  aiWebSearchConfigure:
    "Validates a web-search configuration against the live provider and stores it only if the provider answers, which makes it the safe way to save a form in one step. `entityId` scopes the configuration to a room and has to name one the caller can open; omitting it configures the portal. A `baseUrl` pointing at a private network address is refused. Use `PUT api/2.0/ai/web-search/set-active-config` when the configuration should be stored without a provider round trip.",
  aiWebSearchSetActiveConfig:
    "Stores a web-search configuration without contacting the provider first, for a form that has already validated its input or for restoring a known-good configuration. `entityId` scopes it to a room and has to name one the caller can open. A `baseUrl` pointing at a private network address is still refused, because that check is local. Nothing guarantees the stored provider works: follow up with `POST api/2.0/ai/web-search/test-connection`, or use `PUT api/2.0/ai/web-search/configure` to have the store gated on a live probe.",
  aiWebSearchClear:
    "Removes the portal's web-search configuration, after which web search is unavailable everywhere it was not configured separately. This is not scoped: it takes no `entityId` and any body sent with it is ignored, so it cannot be used to clear one room's configuration. Clearing an already-unconfigured portal is not an error and the call answers success either way. The stored provider key is destroyed with the configuration and has to be entered again.",
  aiWebSearchPassthroughSearch:
    "Runs a web search on behalf of the document editor's AI plugin, which holds only a placeholder configuration - the portal's active provider and its key are resolved here, so neither ever reaches the browser. The portal-wide configuration is used regardless of the document being edited, and a portal without one answers 404. The provider's own status, body and content type are relayed as they stand, so a provider that rate-limits answers 429 and one that is unreachable answers 502. Closing the connection aborts the upstream request.",
  aiWebSearchPassthroughContents:
    "Fetches the contents of web pages on behalf of the document editor's AI plugin, against the portal's active web-search provider, exactly as the search passthrough does. The portal-wide configuration is used and a portal without one answers 404. The provider's status, body and content type are relayed verbatim, so its 429 and its failures surface unchanged. This is the follow-up to `POST api/2.0/ai/websearch/v1/search`, which returns the results whose contents this operation retrieves.",
};

// Resolve the prose for one operation, or `undefined` when the table does not
// describe it (see the note above `OPERATION_DOCS`).
function operationDescription(operationId: string): Json {
  const description = OPERATION_DOCS[operationId];
  return description === undefined ? {} : { description };
}

// A permissive JSON-object response body: the engine handlers forward
// upstream `.NET` DTOs verbatim, so the concrete shape lives on the .NET
// side. The spec documents the transport, not every field.
const JSON_OBJECT_SCHEMA: Json = { type: "object", additionalProperties: true };

// Shared response components emitted from `schemaTypes.ts` (the generator
// namespaces every schema with the `Ai` prefix). Referenced in place of
// the opaque generic object so the 401 and the no-body success fallback carry
// a concrete shape: `{ error }` and `{ success }` respectively.
const ERROR_RESPONSE_REF: Json = { $ref: "#/components/schemas/AiErrorResponse" };
const SUCCESS_RESPONSE_REF: Json = { $ref: "#/components/schemas/AiSuccessResponse" };

function jsonResponse(description: string, schema: Json = JSON_OBJECT_SCHEMA): Json {
  return {
    description,
    content: { "application/json": { schema } },
  };
}

const UNAUTHORIZED_RESPONSE: Json = jsonResponse(
  "Missing `asc_auth_key` cookie or `Authorization` header.",
  ERROR_RESPONSE_REF,
);

// Error statuses this service can actually answer with, and what each one
// means across the API. `OPERATION_ERRORS` picks the codes per operation and
// may replace the text where an operation rejects something specific.
//
// Every code below was traced to the code that produces it. The single error
// boundary is `clientError` in `controllers/_helpers.ts`, which forwards the
// status of a relayed `AiServiceHttpError` / `DocspaceApiHttpError`, lets a
// 4xx with `expose: true` through, and collapses everything else to 500.
//
// Two traps, both of which would make the document untrue in the other
// direction, so do not "fill them in for symmetry":
//   - 429 is never produced here. The service has no rate limiter; the status
//     only appears when a passthrough operation relays a provider's answer.
//   - 403 is almost never raised locally. For anything resolving an agent
//     entity, a 403 from the Files API is deliberately turned into a 404
//     (`storage/docspaceFilesApi.ts`, "don't reveal it"), so those operations
//     declare 404 and say that it covers both cases.
const ERROR_DESCRIPTIONS: Readonly<Record<string, string>> = {
  "400": "The request body or query is malformed, or a required value is missing.",
  "402":
    "The portal has no paid AI quota left, so the profile bound to this action cannot be dispatched.",
  "403":
    "AI is disabled for this portal, or the caller is a guest. Relayed from the DocSpace AI service.",
  "404":
    "The referenced object does not exist, or the caller cannot access it - the two are " +
    "deliberately indistinguishable, so a room the caller may not open answers 404 rather than 403.",
  "413": "The request body is larger than 100 KB, the JSON parser's limit on this route.",
  "429": "Relayed verbatim from the AI provider, which is rate-limiting this portal's key.",
  "500": "Unhandled failure. The reason is logged server-side and never echoed back.",
  "502": "The AI provider could not be reached, or answered with a failure of its own.",
};

// `true` uses the shared text above; a string replaces it for this operation.
type ErrorSpec = Readonly<Record<string, string | true>>;

// Codes every operation declares:
//   401 - the global auth gate (`routes.ts`), already emitted separately;
//   500 - the `clientError` catch-all, reachable on every route;
//   403 - the AI-feature and guest gates of the .NET service, relayed by any
//         handler that touches its storage;
//   413 - the body parser, on every operation that carries a request body.
const ALWAYS_ERRORS: readonly string[] = ["403", "500"];

// Per-operation codes beyond the four above, keyed by `operationId`. There is
// deliberately no fallback: an operation missing from this table declares only
// the always-on set, and the gap stays visible instead of being papered over
// with a guess (the same principle as `OPERATION_DOCS` and `PARAM_DOCS`).
const OPERATION_ERRORS: Readonly<Record<string, ErrorSpec>> = {
  // AI - chat rounds. Only the two streaming entry points validate and bill
  // before the stream opens; `send`/`sendCustom` dispatch straight away.
  aiAiSendWithStream: {
    "400":
      "The prompt is empty, more attachments were sent than the limit allows, or no AI " +
      "profile could be resolved for the requested action.",
    "402": true,
    "404": "The `entityId` names a room the caller cannot open, or no live profile is bound to it.",
  },
  aiAiSendWithStreamOpenAI: {
    "400": "The prompt is empty, or no AI profile could be resolved for the requested action.",
    "402": true,
  },

  // Agents - the room ID is an integer on the .NET side and a non-integer one
  // is refused here; the create and update pair also validate the profile.
  aiAgentsCreate: {
    "400":
      "`profileId` is missing, is not a UUID, names no existing profile, or names one that " +
      "does not support chat; or `prompt` is missing.",
  },
  aiAgentsGet: { "400": "The agent ID is not a positive integer." },
  aiAgentsUpdate: {
    "400":
      "The agent ID is not a positive integer, or `profileId` is not a UUID, names no " +
      "existing profile, or names one that does not support chat.",
  },
  aiAgentsDelete: { "400": "The agent ID is not a positive integer." },

  // Assignments - every write validates its action type first; reading all
  // assignments of a room resolves the room.
  aiAssignmentsResolveForAction: { "400": "`actionType` is missing." },
  aiAssignmentsTryResolveForAction: { "400": "`actionType` is missing." },
  aiAssignmentsAssign: { "400": "`actionType` or `profileId` is missing." },
  aiAssignmentsUnassign: { "400": "`actionType` is missing." },
  aiAssignmentsBulkAssign: {
    "400":
      "The body is not a map of action type to profile ID, or one of its keys is not a " +
      "known action type.",
  },
  aiAssignmentsGetAssignment: { "400": "`actionType` is missing." },
  aiAssignmentsGetAllAssignments: { "404": true },
  aiAssignmentsCascadeProfileDelete: { "400": "`profileId` is missing." },

  // Attachments - `linkToMessage` is the only one that resolves a message.
  aiAttachmentsSaveFile: { "400": "The attachment payload is malformed." },
  aiAttachmentsSaveFilesMany: {
    "400": "`inputs` is not an array, or one of its entries is malformed.",
  },
  aiAttachmentsGet: { "400": "The attachment ID is missing." },
  aiAttachmentsGetMany: { "400": "The list of attachment IDs is malformed." },
  aiAttachmentsLinkToMessage: {
    "400": "The attachment or message reference is malformed.",
    "404": "The message or the attachment does not exist.",
  },

  // Editor tools.
  aiEditorToolsCall: { "400": "The tool name is not one this portal exposes." },

  // Export - asynchronous, so the success code is 202 rather than 200.
  aiExportTextToDocx: {
    "400": "`title`, `content` or `folderId` is missing.",
    "413": "The transcript is larger than 15 MB, this route's own parser limit.",
  },

  // OpenAI passthrough - the provider's answer is relayed as it stands, so any
  // status it returns can reach the caller, 429 included.
  aiOpenaiChatCompletions: {
    "404": "No profile with this identifier exists for the caller.",
    "413": "The request body is larger than this route accepts.",
    "429": true,
    "502": true,
  },
  aiOpenaiImagesGenerations: {
    "404": "No profile with this identifier exists for the caller.",
    "413": "The request body is larger than this route accepts.",
    "429": true,
    "502": true,
  },

  // Preferences.
  aiPreferencesSetDeepMode: { "400": "`value` is missing or is not a boolean." },

  // Profiles - creating and updating are refused outright while the portal
  // runs on the AI gateway, and both validate the provider URL.
  aiProfilesCreate: {
    "400": "The provider URL is missing, malformed, or points at a private network address.",
    "403": "AI profiles are read-only on this portal because they are managed by the AI gateway.",
  },
  aiProfilesUpdate: {
    "400": "The provider URL is missing, malformed, or points at a private network address.",
    "403": "AI profiles are read-only on this portal because they are managed by the AI gateway.",
  },
  aiProfilesDelete: { "400": "The profile ID is missing." },
  aiProfilesListProviderModels: {
    "400":
      "`baseUrl` is missing, points at a private network address, or the provider rejected " +
      "the supplied API key.",
    "502": true,
  },
  aiProfilesListModels: {
    "400": "`profileId` is missing, or the provider rejected the profile's API key.",
    "502": true,
  },
  aiProfilesTestConnection: { "400": "`profileId` is missing." },
  aiProfilesGetById: {
    "400": "The profile ID is missing.",
    "404": "No profile has this ID.",
  },

  // Prompts.
  aiPromptsDelete: { "400": "The prompt ID is missing." },
  aiPromptsDeleteFolder: {
    "400": "The folder ID is missing.",
    "404": "No prompt folder has this ID.",
  },
  aiPromptsGetById: { "400": "The prompt ID is missing." },
  aiPromptsGetFolderById: { "400": "The folder ID is missing." },

  // Threads - the create pair resolves both the room and a live profile.
  aiThreadsCreate: {
    "404":
      "The `entityId` names a room the caller cannot open, or no live AI profile is bound " +
      "to it, so there is no model to run the thread against.",
  },
  aiThreadsOpenOrCreate: {
    "404":
      "The `entityId` names a room the caller cannot open, or no live AI profile is bound to it.",
  },
  aiThreadsAppendUserMessage: { "400": "The message is longer than the limit allows." },
  aiThreadsRename: { "400": "`threadId` or the new title is missing." },
  aiThreadsDelete: {
    "400": "`threadId` is missing.",
    "404": "No thread has this ID.",
  },
  aiThreadsClearMessages: { "400": "`threadId` is missing." },
  aiThreadsRegenerateTitle: { "400": "`threadId` is missing." },
  aiThreadsGetById: {
    "400": "`threadId` is missing.",
    "404": "No thread has this ID.",
  },
  aiThreadsGetMessageById: { "400": "`messageId` is missing." },
  aiThreadsDeleteMessage: { "400": "`messageId` is missing." },

  // Tools - every operation that names a custom server validates the name,
  // and every one that is room-scoped resolves the room.
  aiToolsAddCustomServer: {
    "400": "The server name is missing or is not routable.",
    "404": true,
  },
  aiToolsUpdateCustomServer: {
    "400": "The server name is missing or is not routable.",
    "404": true,
  },
  aiToolsRemoveCustomServer: { "400": "The server name is missing.", "404": true },
  aiToolsGetCustomServer: { "400": "The server name is missing." },
  aiToolsReplaceAllCustomServers: {
    "400": "The body is not a map of server name to configuration, or a name is not routable.",
    "404": true,
  },
  aiToolsSetDisabled: { "400": "The list of tools to disable is malformed.", "404": true },
  aiToolsIsToolDisabled: { "400": "`serverType` or `toolName` is missing." },
  aiToolsSetAllowAlways: { "404": true },
  aiToolsIsAllowAlways: { "400": "`serverType` or `toolName` is missing." },

  // Web search - the four room-scoped operations resolve the room; the two
  // that accept a configuration validate its URL; the passthrough pair relays
  // the provider's answer.
  aiWebSearchGetActiveConfig: { "404": true },
  aiWebSearchIsConfigured: { "404": true },
  aiWebSearchTestConnection: {
    "400": "The provider URL is missing, malformed, or points at a private network address.",
  },
  aiWebSearchConfigure: {
    "400": "The provider URL is missing, malformed, or points at a private network address.",
    "404": true,
  },
  aiWebSearchSetActiveConfig: {
    "400": "The provider URL is missing, malformed, or points at a private network address.",
    "404": true,
  },
  aiWebSearchPassthroughSearch: {
    "404": "Web search is not configured for this portal.",
    "429": true,
    "502": true,
  },
  aiWebSearchPassthroughContents: {
    "404": "Web search is not configured for this portal.",
    "429": true,
    "502": true,
  },
};

// Success codes that are not 200. `text-to-docx` hands the conversion to the
// .NET side and answers before it finishes.
const OPERATION_SUCCESS_CODES: Readonly<Record<string, string>> = {
  aiExportTextToDocx: "202",
};

// Assemble the error half of an operation's `responses`. `hasBody` adds the
// body-parser's 413, which cannot occur on a route that takes no body.
function errorResponses(operationId: string, hasBody: boolean): Record<string, Json> {
  const spec = OPERATION_ERRORS[operationId] ?? {};
  const codes = new Set<string>([...ALWAYS_ERRORS, ...Object.keys(spec)]);
  if (hasBody) {
    codes.add("413");
  }
  const responses: Record<string, Json> = {};
  for (const code of [...codes].sort()) {
    const override = spec[code];
    const description = typeof override === "string" ? override : ERROR_DESCRIPTIONS[code];
    const envelopes = OPERATION_ERROR_SCHEMAS[operationId];
    responses[code] = jsonResponse(
      description as string,
      envelopes?.[code] ?? envelopes?.["*"] ?? ERROR_RESPONSE_REF,
    );
  }
  return responses;
}

function capitalize(name: string): string {
  return name.length > 0 ? name.charAt(0).toUpperCase() + name.slice(1) : name;
}

// Titles for engine operations whose `humanize`d method name does not make a
// usable one, keyed by `operationId`.
//
// `humanize` is a good default for a compound method name (`sendWithStream` →
// "Send with stream"), but it fails in two ways that matter. A single-word
// method yields a title that says nothing without the path (`Send`, `Delete`,
// `List`) and, worse, collides with the same word in another engine - four
// operations were called "Delete" and three "Create", which makes them
// indistinguishable in a tool list, a sidebar and eight generated SDKs. And it
// splits acronyms, so `sendWithStreamOpenAI` came out as "Send with stream open
// ai" and `getById` as "Get by id".
//
// Only those cases are listed here; a compound name that reads correctly keeps
// its derived title. There is no lint signal for this - the `operation-summary`
// rule only checks that a summary is truthy, which `humanize` always satisfies.
const ENGINE_SUMMARIES: Readonly<Record<string, string>> = {
  aiAiSend: "Run an AI action",
  aiAiSendWithStreamOpenAI: "Stream a chat in OpenAI format",

  aiAssignmentsAssign: "Bind a profile to an action",
  aiAssignmentsUnassign: "Clear an action's profile",

  aiAttachmentsGet: "Get one attachment",
  aiAttachmentsDelete: "Delete one attachment",

  aiProfilesCreate: "Create a provider profile",
  aiProfilesUpdate: "Update a provider profile",
  aiProfilesDelete: "Delete a provider profile",
  aiProfilesList: "List provider profiles",
  aiProfilesGetById: "Get a provider profile",
  aiProfilesTestConnection: "Test a profile's provider",

  aiPromptsCreate: "Save a prompt",
  aiPromptsUpdate: "Update a saved prompt",
  aiPromptsMove: "Move a prompt to a folder",
  aiPromptsDelete: "Delete a saved prompt",
  aiPromptsList: "List saved prompts",
  aiPromptsExport: "Export the prompt library",
  aiPromptsGetById: "Get a saved prompt",
  aiPromptsGetFolderById: "Get a prompt folder",

  aiThreadsCreate: "Create a chat thread",
  aiThreadsDelete: "Delete a chat thread",
  aiThreadsList: "List chat threads",
  aiThreadsRename: "Rename a chat thread",
  aiThreadsTouch: "Bump a thread's activity",
  aiThreadsGetById: "Get a chat thread",
  aiThreadsGetMessageById: "Get one chat message",

  aiWebSearchConfigure: "Configure and verify web search",
  aiWebSearchClear: "Clear the web-search configuration",
  aiWebSearchTestConnection: "Test a web-search provider",
};

// Turn a `camelCase`/`kebab` token into a human title, e.g.
// `sendWithStream` → "Send with stream".
function humanize(name: string): string {
  const words = name
    .replace(/[-_]/g, " ")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .toLowerCase()
    .trim();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

function queryParameters(params: readonly string[], operationId: string): Json[] {
  return params.map((name) => ({
    name,
    in: "query",
    ...(paramDescription(operationId, name) as object),
    required: !OPTIONAL_QUERY_PARAMS.has(name),
    schema: {
      ...(INTEGER_QUERY_PARAMS.has(name) ? { type: "integer" } : { type: "string" }),
      ...(paramExample(operationId, name) as object),
    },
  }));
}

function pathParameters(names: readonly string[], operationId: string): Json[] {
  return names.map((name) => ({
    name,
    in: "path",
    ...(paramDescription(operationId, name) as object),
    required: true,
    schema: { type: "string", ...(paramExample(operationId, name) as object) },
  }));
}

// Prose and an example for a request body whose schema alone does not say what
// to send, keyed by `operationId`.
//
// Two shapes need it. A single-argument engine route is serialized by the
// library's `ApiProvider` as the bare value - `JSON.stringify(arg)` - so its
// body is a naked string or array rather than an object, and the generated
// schema is a bare `{"type": "string"}` with nothing to read (`unpackPositional`
// also accepts the legacy `{name: value}` form, but the bare value is the
// documented one). A free-form body is an object whose shape is owned
// elsewhere - by the provider's own API, or by the .NET service the request is
// proxied to - so the generator has no properties to emit.
//
// A body with a generated object schema needs no entry: its properties carry
// their own prose.
interface RequestBodyDoc {
  readonly description: string;
  /** Example value for the body, emitted on the schema as `examples`. */
  readonly example?: Json;
}

const REQUEST_BODY_DOCS: Readonly<Record<string, RequestBodyDoc>> = {
  // Single-argument routes: the body is the value itself.
  aiAssignmentsCascadeProfileDelete: {
    description:
      "The profile to detach from every assignment. May be sent as the `profileId` " +
      "query parameter instead of in the body.",
    example: { profileId: EXAMPLE_IDS.profile },
  },
  aiAttachmentsDelete: {
    description: "The ID of the attachment to delete, as a bare JSON string.",
    example: EXAMPLE_IDS.attachment,
  },
  aiAttachmentsDeleteMany: {
    description: "The IDs of the attachments to delete, as a bare JSON array of strings.",
    example: [EXAMPLE_IDS.attachment, EXAMPLE_IDS.attachmentSecond],
  },
  aiAttachmentsGet: {
    description: "The ID of the attachment to read, as a bare JSON string.",
    example: EXAMPLE_IDS.attachment,
  },
  aiAttachmentsGetMany: {
    description:
      "The IDs of the attachments to read, as a bare JSON array of strings. The answer is " +
      "aligned with this array by position.",
    example: [EXAMPLE_IDS.attachment, EXAMPLE_IDS.attachmentSecond],
  },
  aiPreferencesClearDeepMode: {
    description:
      "The ID of the room whose preference is cleared, as a bare JSON string. Send an empty " +
      "body to clear the portal-wide preference.",
    example: EXAMPLE_IDS.room,
  },
  aiProfilesDelete: {
    description: "The ID of the profile to delete, as a bare JSON string.",
    example: EXAMPLE_IDS.profile,
  },
  aiProfilesTestConnection: {
    description: "The ID of the profile to probe, as a bare JSON string.",
    example: EXAMPLE_IDS.profile,
  },
  aiPromptsCreateFolder: {
    description: "The name of the folder to create, as a bare JSON string.",
    example: "Contract review",
  },
  aiPromptsDelete: {
    description: "The ID of the prompt to delete, as a bare JSON string.",
    example: EXAMPLE_IDS.prompt,
  },
  aiPromptsDeleteFolder: {
    description: "The ID of the folder to delete, as a bare JSON string.",
    example: EXAMPLE_IDS.promptFolder,
  },
  aiThreadsClearMessages: {
    description: "The ID of the thread to empty, as a bare JSON string.",
    example: EXAMPLE_IDS.thread,
  },
  aiThreadsDelete: {
    description: "The ID of the thread to delete, as a bare JSON string.",
    example: EXAMPLE_IDS.thread,
  },
  aiThreadsDeleteMessage: {
    description: "The ID of the message to delete, as a bare JSON string.",
    example: EXAMPLE_IDS.message,
  },
  aiWebSearchClear: {
    description:
      "Ignored. The operation always clears the portal-wide configuration, so send an empty " +
      "body; a value here does not scope it to a room.",
  },

  // Free-form bodies: the shape belongs to another contract.
  aiAssignmentsBulkAssign: {
    description:
      "A map of action type to profile ID. Every key has to be a known action type and every " +
      "value a profile ID; one bad entry rejects the whole map.",
    example: {
      Chat: EXAMPLE_IDS.profile,
      Default: EXAMPLE_IDS.profile,
    },
  },
  aiEditorToolsCall: {
    description:
      "The tool to run: `name` from `GET api/2.0/ai/editor-tools/list`, `arguments` matching " +
      "that tool's input schema, and an optional `entityId` for the room to run it in.",
    example: { name: "docspace_get_folder", arguments: { folderId: "1234" }, entityId: "1234" },
  },
  aiOpenaiChatCompletions: {
    description:
      "An OpenAI Chat Completions request, forwarded to the provider byte for byte. The shape " +
      "is the provider's, not this API's, so consult the provider's own reference; the model " +
      "and the credentials come from the profile in the path and must not be sent here.",
  },
  aiOpenaiImagesGenerations: {
    description:
      "An OpenAI image-generation request, forwarded to the provider byte for byte. The shape " +
      "is the provider's, not this API's, and the credentials come from the profile in the path.",
  },
  aiSettingsSetUser: {
    description:
      "The user's AI settings, proxied unchanged to the DocSpace AI service, which owns and " +
      "validates the shape. Read the current one with `GET api/2.0/ai/config/user` and send " +
      "it back changed.",
  },
  aiSettingsSetVectorization: {
    description:
      "The portal's vectorization settings, proxied unchanged to the DocSpace AI service, which " +
      "owns and validates the shape. Read the current one with " +
      "`GET api/2.0/ai/config/vectorization` and send it back changed.",
  },
  aiVectorizationStartTask: {
    description:
      "The files to index, proxied unchanged to the DocSpace AI service, which owns and " +
      "validates the shape.",
  },
  aiWebSearchPassthroughSearch: {
    description:
      "A search request in the shape the portal's active web-search provider expects, forwarded " +
      "to it unchanged. The endpoint and the key come from the stored configuration and must " +
      "not be sent here.",
  },
  aiWebSearchPassthroughContents: {
    description:
      "A page-contents request in the shape the portal's active web-search provider expects, " +
      "forwarded to it unchanged. The endpoint and the key come from the stored configuration.",
  },
};

function jsonBody(schema: Json = JSON_OBJECT_SCHEMA, operationId?: string): Json {
  const doc = operationId === undefined ? undefined : REQUEST_BODY_DOCS[operationId];
  // The example goes on the schema rather than on the media type: that is where
  // every other example in this document sits, and it keeps
  // `oas3-valid-media-example` out of the picture.
  const described =
    doc?.example === undefined
      ? schema
      : ({ ...(schema as { [k: string]: Json }), examples: [doc.example] } as Json);
  return {
    required: true,
    ...(doc === undefined ? {} : { description: doc.description }),
    content: { "application/json": { schema: described } },
  };
}

// Concrete request/response schemas (from `ts-json-schema-generator`) are
// inlined per operation. Operations without a generated schema (`.NET`-
// forwarded agent listings, dual-mode `sendCustom`) fall back to the generic
// object.
type OperationSchemaLookup = Readonly<Record<string, OperationSchemas>>;

// AI operations whose 200 body is a stream, not a single JSON document. The
// generated `Res_*` schema describes ONE streamed item; the media type here
// reflects the framing (newline-delimited JSON vs. SSE). Applied by
// `responseFor` when building the success response.
const STREAMING_RESPONSES: Readonly<Record<string, { mediaType: string; description: string }>> = {
  aiAiSendWithStream: {
    mediaType: "application/x-ndjson",
    description: "Newline-delimited stream of chat events — one JSON `ChatEvent` object per line.",
  },
  aiAiRegenerateStream: {
    mediaType: "application/x-ndjson",
    description: "Newline-delimited stream of chat events — one JSON `ChatEvent` object per line.",
  },
  aiAiApproveToolCall: {
    mediaType: "application/x-ndjson",
    description: "Newline-delimited stream of chat events — one JSON `ChatEvent` object per line.",
  },
  aiAiDenyToolCall: {
    mediaType: "application/x-ndjson",
    description: "Newline-delimited stream of chat events — one JSON `ChatEvent` object per line.",
  },
  aiAiSendWithStreamOpenAI: {
    mediaType: "text/event-stream",
    description:
      "Server-sent events stream of OpenAI `chat.completion.chunk` objects, terminated by a `[DONE]` sentinel.",
  },
};

// What the success response carries, per operation. The generated schema says
// what the shape is; this says what it means - which of a mutation's fields to
// read, what an empty answer stands for, whose payload is being relayed. A
// single shared "Success." on ninety-six operations said none of that.
//
// The streaming operations are absent: `STREAMING_RESPONSES` describes their
// framing instead. An operation missing from both falls back to "Success.",
// which should never be what a reader sees.
const SUCCESS_DESCRIPTIONS: Readonly<Record<string, string>> = {
  aiAiSend: "The assistant's reply as one message. Nothing was persisted.",
  aiAiSendCustom:
    "The assistant's reply as one message, or a newline-delimited stream of chat events when " +
    "`isStream` was set. Nothing was persisted.",

  aiAgentsList: "The agent rooms, in the DocSpace AI service's folder-content envelope.",
  aiAgentsCreate: "The created agent room, with the model already bound to it.",
  aiAgentsNews: "The unread items of the caller's agent rooms.",
  aiAgentsGet: "The agent room, with `profileId` added when a model is bound to it.",
  aiAgentsUpdate: "The updated agent room.",
  aiAgentsDelete:
    "The queued file operation. Deletion runs asynchronously, so poll DocSpace for its outcome.",
  aiAgentsUpdateQuota: "The updated agent rooms, one entry each.",
  aiAgentsResetQuota: "The updated agent rooms, one entry each.",

  aiAssignmentsResolveForAction: "The profile that will serve the action.",
  aiAssignmentsTryResolveForAction:
    "The profile that will serve the action, or an empty result when none is configured.",
  aiAssignmentsAssign:
    "Whether the binding was stored. A failure is reported in `error` rather than as a status.",
  aiAssignmentsUnassign: "Confirms the action now has no profile of its own.",
  aiAssignmentsBulkAssign:
    "Whether the set was stored, with `errors` listing the entries that were refused.",
  aiAssignmentsGetAssignment:
    "The profile bound to the action, or an empty result when it has none of its own.",
  aiAssignmentsGetAllAssignments:
    "The scope's bindings as a map of action type to profile ID. An action with no binding is absent.",
  aiAssignmentsCascadeProfileDelete: "Confirms no assignment points at the profile any more.",

  aiAttachmentsSaveFile: "The stored draft, whose ID links it to a message later.",
  aiAttachmentsSaveFilesMany: "The stored drafts, in the order they were sent.",
  aiAttachmentsGet: "The attachment, or a null body when no attachment has that ID.",
  aiAttachmentsGetMany:
    "The attachments, aligned by position with the IDs that were sent. A missing one leaves its " +
    "slot empty.",
  aiAttachmentsDelete: "Confirms the request was accepted, whether or not anything was deleted.",
  aiAttachmentsDeleteMany:
    "Confirms the request was accepted, whether or not anything was deleted.",
  aiAttachmentsLinkToMessage: "Confirms the attachments are now bound to the message.",

  aiEditorToolsList: "The tools the editor plugin may offer the model, four fields each.",
  aiEditorToolsCall:
    "The tool's output as a string. A tool that failed reports it inside that string.",

  aiExportTextToDocx:
    "Confirms the export was queued. The .docx arrives in the target folder later, announced by " +
    "a folder-modified socket event.",

  aiOpenaiChatCompletions:
    "The provider's own response, relayed verbatim with its status and content type.",
  aiOpenaiImagesGenerations:
    "The provider's own response, relayed verbatim with its status and content type.",

  aiPreferencesGetDeepMode:
    "Whether deep mode is on, falling back to the configured default when the scope has no value " +
    "of its own.",
  aiPreferencesSetDeepMode: "Confirms the preference was stored.",
  aiPreferencesClearDeepMode:
    "Confirms the scope has no preference of its own and now inherits the default.",
  aiPreferencesIsDeepModeSet:
    "Whether the scope has a preference of its own, whichever way that preference is set.",

  aiProfilesCreate:
    "Whether the profile was created, with it in `profile`. A refusal is reported in `error` " +
    "rather than as a status.",
  aiProfilesUpdate: "Whether the profile was updated, with the stored profile in `profile`.",
  aiProfilesDelete: "Confirms the request was accepted, whether or not a profile was deleted.",
  aiProfilesList: "The portal's profiles, with their keys and headers stripped.",
  aiProfilesGetById: "The profile, with its key and headers stripped.",
  aiProfilesListModels: "The models the profile's provider currently offers.",
  aiProfilesListProviderModels: "The models the endpoint offers for the supplied credentials.",
  aiProfilesTestConnection:
    "The outcome of the probe. A failed probe is reported here, not as a status.",

  aiPromptsCreate: "Whether the prompt was saved, with it in `prompt`.",
  aiPromptsUpdate: "Whether the prompt was updated, with the stored prompt in `prompt`.",
  aiPromptsMove: "Whether the prompt was moved, with the moved prompt in `prompt`.",
  aiPromptsDelete: "Confirms the request was accepted, whether or not a prompt was deleted.",
  aiPromptsList: "The prompts of the scope, newest first.",
  aiPromptsGetById: "The prompt, or an empty body when no prompt of the caller's has that ID.",
  aiPromptsCreateFolder: "Whether the folder was created, with it in `folder`.",
  aiPromptsRenameFolder: "Whether the folder was renamed, with the stored folder in `folder`.",
  aiPromptsDeleteFolder: "Confirms the folder and the prompts inside it are gone.",
  aiPromptsListFolders: "Every folder of the caller's library, newest first.",
  aiPromptsGetFolderById:
    "The folder, or an empty body when no folder of the caller's has that ID.",
  aiPromptsExport: "The whole library as a versioned bundle, ready to import.",
  aiPromptsImportBundle:
    "Whether the bundle was written, how many prompts it imported, and what was refused.",

  aiSettingsGet: "The portal's AI configuration and whether AI is usable at all.",
  aiSettingsGetVectorization: "The portal's vectorization settings.",
  aiSettingsSetVectorization: "The stored vectorization settings.",
  aiSettingsGetUser: "The calling user's AI settings.",
  aiSettingsSetUser: "The calling user's stored AI settings.",

  aiThreadsCreate: "The created thread.",
  aiThreadsOpenOrCreate:
    "The thread that was opened or created, with its prior messages. A created one carries the " +
    "generated title.",
  aiThreadsAppendUserMessage: "The stored message, with the ID storage assigned to it.",
  aiThreadsTouch: "Confirms the thread's activity date moved forward.",
  aiThreadsRename: "Confirms the new title was stored.",
  aiThreadsDelete: "Confirms the thread and its messages are gone.",
  aiThreadsClearMessages: "Confirms the request was accepted. It does not mean the thread existed.",
  aiThreadsRegenerateTitle: "The newly generated title, already stored on the thread.",
  aiThreadsList: "The threads of the scope, most recently edited first.",
  aiThreadsReadMessages:
    "The thread's messages, oldest first unless `direction` reversed them. An empty list also " +
    "means the request carried no thread ID.",
  aiThreadsGetById: "The thread, without its messages.",
  aiThreadsGetMessageById: "The message, or an empty body when no message has that ID.",
  aiThreadsUpdateMessage: "Confirms the replacement was stored.",
  aiThreadsDeleteMessage:
    "Confirms the request was accepted, whether or not a message was deleted.",

  aiToolsAddCustomServer: "Whether the server was registered, with the stored entry.",
  aiToolsUpdateCustomServer: "Whether the server was updated, with the stored entry.",
  aiToolsRemoveCustomServer:
    "Confirms the request was accepted, whether or not a registration was removed.",
  aiToolsGetCustomServer:
    "The stored configuration, empty for a system server and null when the name is not " +
    "registered.",
  aiToolsListCustomServers:
    "The scope's registrations as a map of name to configuration, system entries emptied and the " +
    "portal's built-in server left out.",
  aiToolsListSystemTools:
    "The scope's tools grouped by server type, the system group keys named in `system`, and the " +
    "reason a registered server delivered none in `errors`.",
  aiToolsReplaceAllCustomServers:
    "Whether the registry was replaced, with `errors` listing what was refused.",
  aiToolsSetDisabled: "Confirms the new disable list was stored for that server type.",
  aiToolsGetDisabled:
    "The switched-off tools as a map of server type to tool names. An absent type means nothing " +
    "is switched off for it.",
  aiToolsIsToolDisabled: "Whether that one tool is switched off in the scope.",
  aiToolsSetAllowAlways: "Confirms the always-allow list was updated.",
  aiToolsGetAllowAlways:
    "The tools that run without an approval pause. An empty list means every call needs approval.",
  aiToolsIsAllowAlways: "Whether that one tool runs without an approval pause.",

  aiVectorizationStartTask:
    "Confirms the indexing was queued. It carries no job handle, so there is nothing to poll.",

  aiWebSearchGetActiveConfig:
    "The configuration in force for the scope, without the provider key, or an empty result when " +
    "web search is not configured.",
  aiWebSearchIsConfigured: "Whether a web-search provider is stored for the scope.",
  aiWebSearchTestConnection:
    "The outcome of the probe. A failed probe is reported here, not as a status.",
  aiWebSearchConfigure: "Whether the configuration was stored, after the provider answered.",
  aiWebSearchSetActiveConfig: "Confirms the configuration was stored, unverified.",
  aiWebSearchClear: "Confirms the portal has no web-search configuration any more.",
  aiWebSearchPassthroughSearch:
    "The provider's own response, relayed verbatim with its status and content type.",
  aiWebSearchPassthroughContents:
    "The provider's own response, relayed verbatim with its status and content type.",
};

// The four proxy operations answer with the provider's body, byte for byte. Nothing
// here reshapes it, so the document says what it is rather than inventing a schema.
const PROVIDER_RELAY_SCHEMA: Json = {
  type: "object",
  description:
    "Relayed from the provider unchanged. The shape is the provider's, not this " +
    "service's, and it varies by provider and model.",
  additionalProperties: true,
};

// The success schema normally comes from the engine method's return type. These
// operations do not return what their engine method returns - the controller wraps
// it, replaces it, or relays somebody else's body - so the shape is declared here
// and wins over the generated one. Every entry was read off the controller.
const OPERATION_RESPONSE_SCHEMAS: Readonly<Record<string, Json>> = {
  // `res.json({ messageId })` - the identifier only, not the stored message.
  aiThreadsAppendUserMessage: {
    type: "object",
    properties: {
      messageId: {
        type: "string",
        description: "Identifier of the message that was appended to the thread.",
        examples: [EXAMPLE_IDS.message],
      },
    },
    required: ["messageId"],
    additionalProperties: false,
  },

  // `res.json({ title })` - the engine returns the bare string, the controller wraps it.
  aiThreadsRegenerateTitle: {
    type: "object",
    properties: {
      title: {
        type: "string",
        description: "The regenerated thread title.",
        examples: ["Quarterly report review"],
      },
    },
    required: ["title"],
    additionalProperties: false,
  },

  // `res.json({ groups, errors, system })`: the engine's plain tool map is merged with
  // the registered custom servers, and the system server names are listed separately
  // so a client can tell the two apart.
  aiToolsListSystemTools: {
    type: "object",
    properties: {
      groups: {
        type: "object",
        description:
          "Tools by server name, covering both the host-configured system servers and " +
          "the custom MCP servers registered for this scope.",
        additionalProperties: {
          type: "array",
          items: { $ref: "#/components/schemas/AiTMCPItem" },
        },
      },
      errors: {
        type: "object",
        description:
          "Why a registered custom server could not be reached, keyed by server name. " +
          "A server that answered is absent from this map.",
        additionalProperties: { type: "string" },
      },
      system: {
        type: "array",
        description:
          "Names of the host-configured system servers among the keys of `groups`; " +
          "everything else there was registered as a custom server.",
        items: { type: "string" },
      },
    },
    required: ["groups", "errors", "system"],
    additionalProperties: false,
  },

  // `res.json({ tools })` - flattened across servers, with the excluded ones dropped.
  aiEditorToolsList: {
    type: "object",
    properties: {
      tools: {
        type: "array",
        description: "The tools the editor may offer, flattened across every server.",
        items: {
          type: "object",
          properties: {
            name: {
              type: "string",
              description: "Tool name, as it is passed back to the call endpoint.",
              examples: ["docspace_search_files"],
            },
            description: {
              type: "string",
              description: "What the tool does, empty when the server declares nothing.",
            },
            inputSchema: {
              type: "object",
              description: "JSON Schema of the tool arguments.",
              additionalProperties: true,
            },
            requireApproval: {
              type: "boolean",
              description:
                "Whether the editor has to ask the user before running the tool. Read-only " +
                "operations arrive with this off.",
              examples: [true],
            },
          },
          required: ["name", "description", "inputSchema", "requireApproval"],
          additionalProperties: false,
        },
      },
    },
    required: ["tools"],
    additionalProperties: false,
  },

  // `res.json({ result })` - always a string: a non-string tool result is JSON-encoded
  // before it is sent, because the editor plugin relays it to the model verbatim.
  aiEditorToolsCall: {
    type: "object",
    properties: {
      result: {
        type: "string",
        description:
          "What the tool produced, as text. A structured result is JSON-encoded, and a " +
          "tool that failed reports its error here rather than through a status code.",
      },
    },
    required: ["result"],
    additionalProperties: false,
  },

  // The controller resolves the agent's profile assignment and writes `profileId`
  // onto the room before answering. It is absent when the agent has no assignment
  // or the lookup failed, so it is documented as an addition, not a promise.
  aiAgentsGet: {
    allOf: [
      { $ref: "#/components/schemas/AiFolderIntegerWrapper" },
      {
        type: "object",
        properties: {
          response: {
            type: "object",
            properties: {
              profileId: {
                type: "string",
                description:
                  "The AI profile bound to this agent, added by this service on top of " +
                  "what the internal service returns. Absent when the agent has no " +
                  "profile assigned.",
                examples: [EXAMPLE_IDS.profile],
              },
            },
          },
        },
      },
    ],
  },

  // Relayed from the .NET service with `raw: true`, so the DocSpace envelope is passed
  // through untouched. The method behind it returns no value, which is why the envelope
  // carries no `response`.
  aiVectorizationStartTask: {
    type: "object",
    properties: {
      count: {
        type: "integer",
        description: "Envelope field from the internal service; 0 for this operation.",
        examples: [0],
      },
      status: {
        type: "integer",
        description: "Envelope status flag from the internal service.",
        examples: [0],
      },
      statusCode: {
        type: "integer",
        description: "HTTP status the internal service answered with.",
        examples: [200],
      },
    },
    required: ["count", "status", "statusCode"],
    additionalProperties: true,
  },

  aiOpenaiChatCompletions: PROVIDER_RELAY_SCHEMA,
  aiOpenaiImagesGenerations: PROVIDER_RELAY_SCHEMA,
  aiWebSearchPassthroughSearch: PROVIDER_RELAY_SCHEMA,
  aiWebSearchPassthroughContents: PROVIDER_RELAY_SCHEMA,
};

// The OpenAI proxies report failures in OpenAI's own error envelope - including the
// ones this service raises itself, so that a client written against the OpenAI SDK
// can read them. Every other operation answers with `AiErrorResponse`.
const OPENAI_ERROR_SCHEMA: Json = {
  type: "object",
  properties: {
    error: {
      type: "object",
      properties: {
        message: { type: "string", description: "Human-readable description of the failure." },
        type: {
          type: "string",
          description: "OpenAI error class, for example `invalid_request_error`.",
          examples: ["invalid_request_error"],
        },
        code: {
          type: ["string", "null"],
          description: "Machine-readable code, when the provider supplies one.",
        },
        param: {
          type: ["string", "null"],
          description: "The request parameter at fault, when the failure names one.",
        },
      },
      required: ["message", "type"],
      additionalProperties: true,
    },
  },
  required: ["error"],
  additionalProperties: false,
};

// `listProviderModels` names the input at fault so the client can highlight it
// (Bug 83116), but only when it is the one rejecting a missing `providerType` or
// `baseUrl`. `AiErrorResponse` closes itself to extra properties, so that body needs
// a schema of its own.
const FIELD_ERROR_SCHEMA: Json = {
  type: "object",
  properties: {
    error: {
      type: "string",
      description: "The error message, ready to be shown to the caller.",
      examples: ["providerType required"],
    },
    field: {
      type: "string",
      description: "Name of the request field that was missing or rejected.",
      examples: ["providerType"],
    },
  },
  required: ["error", "field"],
  additionalProperties: false,
};

// Only the two missing-input checks name the field. The private-network rejection
// and a key the provider refused both answer with the plain error body, so the 400
// has to admit either shape.
const FIELD_OR_PLAIN_ERROR_SCHEMA: Json = {
  anyOf: [FIELD_ERROR_SCHEMA, ERROR_RESPONSE_REF],
};

// operationId -> status code -> schema, with `*` standing for every code of that
// operation. Anything not listed answers with `AiErrorResponse`.
const OPERATION_ERROR_SCHEMAS: Readonly<Record<string, Readonly<Record<string, Json>>>> = {
  aiOpenaiChatCompletions: { "*": OPENAI_ERROR_SCHEMA },
  aiOpenaiImagesGenerations: { "*": OPENAI_ERROR_SCHEMA },
  aiProfilesListProviderModels: { "400": FIELD_OR_PLAIN_ERROR_SCHEMA },
};

function responseFor(operations: OperationSchemaLookup, operationId: string): Json {
  const declared = OPERATION_RESPONSE_SCHEMAS[operationId];
  const schema = declared ?? operations[operationId]?.response;
  const streaming = STREAMING_RESPONSES[operationId];
  if (streaming && schema !== undefined) {
    // The schema types a single streamed item; the media type frames the
    // stream. (A JSON `content` block would misrepresent the wire format.)
    return {
      description: streaming.description,
      content: { [streaming.mediaType]: { schema: schema as Json } },
    };
  }
  const description = SUCCESS_DESCRIPTIONS[operationId] ?? "Success.";
  // No generated schema ⇒ a `void` engine method; every such controller
  // replies `{ success: true }`, so document that rather than a generic object.
  return schema === undefined
    ? jsonResponse(description, SUCCESS_RESPONSE_REF)
    : jsonResponse(description, schema as Json);
}

// The request schema normally comes from the engine method's parameters. These
// operations read the request themselves and expect a different shape, so what the
// controller actually parses is declared here and wins over the generated schema.
const OPERATION_REQUEST_SCHEMAS: Readonly<Record<string, Json>> = {
  // `req.body?.profileId ?? req.query.profileId` - a bare string body leaves both
  // undefined and is rejected with 400.
  aiAssignmentsCascadeProfileDelete: {
    type: "object",
    properties: {
      profileId: {
        type: "string",
        description:
          "The profile whose assignments are removed. May be sent as the `profileId` " +
          "query parameter instead of in the body.",
        examples: [EXAMPLE_IDS.profile],
      },
    },
    required: ["profileId"],
    additionalProperties: false,
  },

  // The controller reads exactly these three keys and ignores anything else.
  aiEditorToolsCall: {
    type: "object",
    properties: {
      name: {
        type: "string",
        description:
          "Name of the tool to run, as listed by the tools endpoint. A name that is " +
          "unknown or excluded from the editor is rejected with 400.",
        examples: ["docspace_get_folder"],
      },
      arguments: {
        type: "object",
        description:
          "Arguments for the tool, shaped by that tool's own input schema. Treated as " +
          "empty when it is not an object.",
        additionalProperties: true,
        examples: [{ folderId: "1234" }],
      },
      entityId: {
        type: "string",
        description: "Room the call is scoped to. Left out for a portal-wide call.",
        examples: [EXAMPLE_IDS.room],
      },
    },
    required: ["name"],
    additionalProperties: false,
  },

  // The engine method takes the key as a required parameter, but the controller passes
  // an empty string when it is absent, so a caller may leave it out - a provider that
  // needs no key, or one whose key is already stored.
  aiProfilesListProviderModels: {
    type: "object",
    properties: {
      providerType: {
        $ref: "#/components/schemas/AiProviderType",
        description: "Provider whose catalog to list.",
      },
      baseUrl: {
        type: "string",
        description: "Provider API base URL.",
        examples: ["https://api.openai.com/v1"],
      },
      apiKey: {
        type: "string",
        description:
          "Provider API key. Omit it for a provider that needs none; the request is " +
          "then made without one.",
      },
    },
    required: ["providerType", "baseUrl"],
    additionalProperties: false,
  },

  // Relayed to the internal service as-is; that service expects the file identifiers.
  aiVectorizationStartTask: {
    type: "object",
    properties: {
      files: {
        type: "array",
        description: "Identifiers of the files to vectorize.",
        items: { type: "integer" },
        examples: [[1234, 1235]],
      },
    },
    required: ["files"],
    additionalProperties: false,
  },
};

function requestBodyFor(operations: OperationSchemaLookup, operationId: string): Json {
  const schema = OPERATION_REQUEST_SCHEMAS[operationId] ?? operations[operationId]?.request;
  return schema === undefined
    ? jsonBody(JSON_OBJECT_SCHEMA, operationId)
    : jsonBody(schema as Json, operationId);
}

// Build the operation object for one engine route. GET routes expose their
// positional `params` as query parameters (matching the library's
// `ApiProvider`, which serializes GET args as `params[i]=value`); non-GET
// routes carry a JSON body.
function engineOperation(
  engine: EngineDoc,
  methodName: string,
  spec: RouteSpec,
  operations: OperationSchemaLookup,
): Json {
  const isGet = spec.method === "GET";
  // lowerCamelCase, `ai`-scoped so it stays unique across engines and
  // does not clash with the .NET services' ids once merged.
  const operationId = `ai${capitalize(engine.name)}${capitalize(methodName)}`;
  const operation: Record<string, Json> = {
    tags: [tag(engine.tag)],
    operationId,
    summary: ENGINE_SUMMARIES[operationId] ?? humanize(methodName),
    ...(operationDescription(operationId) as object),
    responses: {
      [OPERATION_SUCCESS_CODES[operationId] ?? "200"]: responseFor(operations, operationId),
      "401": UNAUTHORIZED_RESPONSE,
      ...errorResponses(operationId, !isGet),
    },
  };
  if (isGet && spec.params && spec.params.length > 0) {
    operation["parameters"] = queryParameters(spec.params, operationId);
  }
  if (!isGet) {
    operation["requestBody"] = requestBodyFor(operations, operationId);
  }
  return operation;
}

// Query parameters of a custom route. Engine routes get theirs from
// `RouteSpec.params`, but a custom route has no such list, so a route that reads
// the query says so here. These carry their own types and prose because they are
// richer than the bare-name mechanism above: the agents listing forwards every
// string query parameter it receives straight to the internal service, and these
// are the ones that service reads (GetAgentListRequestDto).
const OPERATION_QUERY_PARAMS: Readonly<Record<string, readonly Json[]>> = {
  aiAgentsList: [
    {
      name: "subjectId",
      in: "query",
      description: "Show only the agent rooms this user takes part in.",
      required: false,
      schema: { type: "string", examples: [EXAMPLE_IDS.profile] },
    },
    {
      name: "subjectOwnerId",
      in: "query",
      description: "Show only the agent rooms owned by this user.",
      required: false,
      schema: { type: "string", examples: [EXAMPLE_IDS.profile] },
    },
    {
      name: "excludeSubject",
      in: "query",
      description:
        "Invert the user filter: leave out what `subjectId` selects instead of " + "keeping it.",
      required: false,
      schema: { type: "boolean", examples: [false] },
    },
    {
      name: "tags",
      in: "query",
      description: "Show only the agent rooms carrying these tags, comma-separated.",
      required: false,
      schema: { type: "string", examples: ["ai,assistant"] },
    },
    {
      name: "withoutTags",
      in: "query",
      description: "Show only the agent rooms that carry no tags at all.",
      required: false,
      schema: { type: "boolean", examples: [false] },
    },
    {
      name: "quotaFilter",
      in: "query",
      description: "Filter by quota kind: 0 for all, 1 for the default quota, 2 for a custom one.",
      required: false,
      schema: { type: "integer", examples: [0] },
    },
    {
      name: "filterValue",
      in: "query",
      description: "Show only the agent rooms whose title matches this text.",
      required: false,
      schema: { type: "string", examples: ["assistant"] },
    },
    {
      name: "sortBy",
      in: "query",
      description: "Field to sort by, for example `DateAndTime`.",
      required: false,
      schema: { type: "string", examples: ["DateAndTime"] },
    },
    {
      name: "sortOrder",
      in: "query",
      description: "Sort direction, `ascending` or `descending`.",
      required: false,
      schema: { type: "string", examples: ["descending"] },
    },
    {
      name: "startIndex",
      in: "query",
      description: "Index of the first entry to return; 0 starts at the beginning.",
      required: false,
      schema: { type: "integer", examples: [0] },
    },
    {
      name: "count",
      in: "query",
      description: "How many entries to return. The internal service applies its own default.",
      required: false,
      schema: { type: "integer", examples: [25] },
    },
  ],
};

function customOperation(route: CustomRouteDoc, operations: OperationSchemaLookup): Json {
  const operation: Record<string, Json> = {
    tags: [tag(route.tag)],
    operationId: route.operationId,
    summary: route.summary,
    ...(operationDescription(route.operationId) as object),
    responses: {
      [OPERATION_SUCCESS_CODES[route.operationId] ?? "200"]: responseFor(
        operations,
        route.operationId,
      ),
      "401": UNAUTHORIZED_RESPONSE,
      ...errorResponses(route.operationId, route.hasBody === true),
    },
  };
  const params: Json[] = [];
  if (route.pathParams && route.pathParams.length > 0) {
    params.push(...pathParameters(route.pathParams, route.operationId));
  }
  const query = OPERATION_QUERY_PARAMS[route.operationId];
  if (query) {
    params.push(...query);
  }
  if (params.length > 0) {
    operation["parameters"] = params;
  }
  if (route.hasBody) {
    operation["requestBody"] = requestBodyFor(operations, route.operationId);
  }
  return operation;
}

// Add an operation to `paths` under `path`+`method`, merging with any
// operation already registered for that path (different verbs share a
// path item object).
function addOperation(
  paths: Record<string, Json>,
  path: string,
  method: string,
  operation: Json,
): void {
  const item = (paths[path] as Record<string, Json>) ?? {};
  item[method.toLowerCase()] = operation;
  paths[path] = item;
}

/** Build the full OpenAPI 3.0 document for the service. */
export function buildOpenApiDocument(options: OpenApiOptions): OpenApiDocument {
  const { apiPrefix, engines, customRoutes, customTagDescriptions = {}, schemas } = options;
  const operations: OperationSchemaLookup = schemas?.operations ?? {};

  // Path keys are absolute: prefixed with the service's proxy route so both
  // the docs UI and clients show the full `/api/2.0/ai/...` URL.
  const key = (relative: string): string => `${apiPrefix}${relative}`;

  const paths: Record<string, Json> = {};

  for (const engine of engines) {
    for (const [methodName, spec] of Object.entries(engine.routes)) {
      // Engine `RouteSpec.path` is relative and prefix-less (`ai/send`).
      addOperation(
        paths,
        key(`/${spec.path}`),
        spec.method,
        engineOperation(engine, methodName, spec, operations),
      );
    }
  }

  for (const route of customRoutes) {
    addOperation(paths, key(route.path), route.method, customOperation(route, operations));
  }

  // The `/health` and `/isLife` probes are intentionally left out of the
  // document: they are infrastructure endpoints (registered before the auth
  // gate) and not part of the public API surface. They remain served by the
  // app — this only hides them from the generated docs/SDKs.

  // `name` carries the full `AI / …` value (used by operations, the
  // `x-tagGroups` grouping and the URL slug); `x-displayName` is the short
  // label the docs UI shows in the sidebar (matching the .NET services).
  const tagEntry = (name: string, description: string): Json => ({
    name: tag(name),
    description,
    "x-displayName": name,
  });

  // Declared from the routes themselves rather than a hand-kept list: every
  // tag an operation carries must also appear in the document's global `tags`
  // (`operation-tag-defined`), and a custom route added with a brand-new tag
  // would otherwise silently emit an undeclared one.
  const engineTags = new Set(engines.map((e) => e.tag));
  const customTags = [...new Set(customRoutes.map((r) => r.tag))].filter((t) => !engineTags.has(t));

  // Sorted by name for `openapi-tags-alphabetical`, which compares with `String.localeCompare` -- the same
  // call is used here so the order is the rule's own, not an approximation of it. The locale is pinned:
  // with no second argument `localeCompare` uses the environment's default collation (OS-derived, so it
  // differs between developer machines and CI), which would make the emitted tag order machine-dependent.
  // `en` matches the language the document is written in. Note this puts `AI / Agents` before `AI / AI`,
  // where a code-unit comparison would not. Sorting here rather than at the two sources keeps engine and
  // custom tags interleaved, and `x-tagGroups` below inherits the order.
  const tags: Json[] = [
    ...engines.map((e) => tagEntry(e.tag, e.description)),
    ...customTags.map((t) => tagEntry(t, customTagDescriptions[t] ?? `${t} operations.`)),
  ].sort((a, b) => (a as { name: string }).name.localeCompare((b as { name: string }).name, "en"));

  // Group every tag under a single "AI" heading in the merged reference
  // (a Redocly/Scalar extension; harmless for the standalone document).
  const tagGroup: Json = {
    name: TAG_GROUP,
    tags: (tags as Array<{ name: string }>).map((t) => t.name),
  };

  // Shared `{baseUrl}` server template (default empty = same origin),
  // matching the .NET service documents. Paths already carry the full
  // `/api/2.0/ai/...` route, so requests resolve to the proxied URL.
  // The description names the environment on purpose (`server-environment-described` in the
  // documentation tool's SDK/.spectral.yaml): `{baseUrl}` resolves to the customer's own portal,
  // so this is the public production surface. Kept identical to the .NET service documents so the
  // joiner sees one server, not two.
  const servers: Json = [
    {
      url: "{baseUrl}",
      description: "The production DocSpace portal, at the customer's own domain.",
      variables: { baseUrl: { default: "", description: "Default URL" } },
    },
  ];

  return {
    openapi: "3.1.1",
    info: {
      title: "ONLYOFFICE DocSpace AI Service API",
      version: "2.0",
      description:
        "HTTP API of the AI service. Requests are authenticated with the " +
        "DocSpace `asc_auth_key` session cookie or an `Authorization` header " +
        "and forwarded to the AI engine and the .NET AI integration service.",
      // Same support contact the .NET service documents declare, so the merged document is consistent.
      contact: {
        name: "API Support",
        email: "support@onlyoffice.com",
        url: "https://helpdesk.onlyoffice.com/hc/en-us",
      },
      // Same licence the .NET service documents declare, so the merged document is consistent.
      // `url` rather than the 3.1-only `identifier`: the two are mutually exclusive and the url is
      // what SDK generators and the api reference render.
      license: {
        name: "AGPL-3.0-only",
        url: "https://www.gnu.org/licenses/agpl-3.0.html",
      },
    },
    servers,
    // Applied to every operation unless overridden (health probes clear it).
    security: [{ cookieAuth: [] }, { bearerAuth: [] }],
    tags,
    "x-tagGroups": [tagGroup],
    paths,
    components: {
      securitySchemes: {
        cookieAuth: {
          type: "apiKey",
          in: "cookie",
          name: "asc_auth_key",
          description: "DocSpace session cookie sent by the browser.",
        },
        bearerAuth: {
          type: "http",
          scheme: "bearer",
          description: "Bearer token or API key for programmatic callers.",
        },
      },
      // Shared named types referenced by the inlined operation schemas;
      // empty when no schema bundle is supplied.
      schemas: { ...(schemas?.components ?? {}) } as Json,
    },
  };
}

// Self-contained HTML for the Scalar API reference UI. The DocSpace .NET
// services expose their docs through Scalar too, so this keeps the AI
// service consistent. The script is loaded from the CDN; `specUrl` is the
// same-origin path to the generated document.
export function docsHtml(specUrl: string): string {
  return `<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>AI Service API</title>
  </head>
  <body>
    <script id="api-reference" data-url="${specUrl}"></script>
    <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
  </body>
</html>
`;
}
