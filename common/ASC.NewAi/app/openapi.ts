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
const INTEGER_QUERY_PARAMS = new Set(["limit", "startIndex"]);

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

// Resolve the prose for one parameter of one operation, or `undefined` when
// neither table describes it (see the note above `PARAM_DOCS`).
function paramDescription(operationId: string, name: string): Json {
  const description = OPERATION_PARAM_DOCS[operationId]?.[name] ?? PARAM_DOCS[name];
  return description === undefined ? {} : { description };
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
    "Lists the portal's AI agent rooms. Query parameters are forwarded unchanged to the .NET AI service, which answers with its folder-content payload.",
  aiAgentsCreate:
    "Creates an AI agent room in the .NET AI service and binds the supplied `profileId` to it as a `Chat` assignment. The instruction is stored on the room as a prompt-only chat setting; a failed binding is reported as an error even though the room already exists.",
  aiAgentsNews: "Lists the new items across the caller's AI agent rooms.",
  aiAgentsGet:
    "Returns one AI agent room, enriched with the `profileId` bound to it so an edit form can prefill the profile selector. A missing assignment simply leaves `profileId` out.",
  aiAgentsUpdate:
    "Updates an AI agent room - title, tags, instruction. `profileId` is not part of the room contract: it is stripped from the forwarded body and re-bound as the agent's assignment afterwards.",
  aiAgentsDelete: "Deletes an AI agent room.",
  aiAgentsUpdateQuota: "Changes the storage quota of the given AI agent rooms.",
  aiAgentsResetQuota: "Resets the storage quota of the given AI agent rooms.",

  // Assignments - which profile serves which AI action.
  aiAssignmentsResolveForAction:
    "Resolves the profile bound to an AI action, falling back to the `Default` slot when the action itself has none. Fails when neither slot is set or the bound profile no longer exists - use `try-resolve-for-action` for an empty answer instead.",
  aiAssignmentsTryResolveForAction:
    "Resolves the profile bound to an AI action exactly like `resolve-for-action`, but answers with an empty result instead of failing when nothing is configured.",
  aiAssignmentsAssign:
    "Binds a profile to an AI action, creating the assignment or updating it in place. The profile's declared capabilities are validated against the action, except for the `Default` slot.",
  aiAssignmentsUnassign:
    "Removes the profile binding of an AI action. Does nothing when that slot is already empty.",
  aiAssignmentsBulkAssign:
    "Applies many action-to-profile bindings at once. Every entry is validated first and nothing is written if any of them fails, so the assignment set is never left half-written.",
  aiAssignmentsGetAssignment:
    "Returns the profile bound to one AI action, without the `Default` fallback.",
  aiAssignmentsGetAllAssignments: "Returns the full action-to-profile assignment map of the scope.",
  aiAssignmentsCascadeProfileDelete:
    "Cleans up the assignments pointing at a profile that is about to be deleted: the `Default` slot is promoted to the first remaining profile (or dropped when none is left), and every other slot holding that profile is unbound.",

  // Attachments - message files and images, saved as drafts first.
  aiAttachmentsSaveFile:
    "Stores one file attachment as a draft, carrying the host-extracted text of the file. Prefer `save-files-many` when adding several files at once so they land as one round trip.",
  aiAttachmentsSaveFilesMany:
    "Stores a batch of file attachments as drafts in a single round trip. The returned records keep the order of the input.",
  aiAttachmentsSaveImage:
    "Stores one image attachment as a draft from a `data:` URL. Prefer `save-images-many` when adding several images at once.",
  aiAttachmentsSaveImagesMany:
    "Stores a batch of image attachments as drafts in a single round trip. The returned records keep the order of the input.",
  aiAttachmentsGet: "Returns one attachment by identifier.",
  aiAttachmentsGetMany:
    "Returns a batch of attachments, preserving the requested order; an identifier that no longer exists comes back empty.",
  aiAttachmentsDelete:
    "Permanently deletes one attachment, whether it is still a draft or already linked to a message.",
  aiAttachmentsDeleteMany: "Permanently deletes a batch of attachments in a single round trip.",
  aiAttachmentsLinkToMessage:
    "Binds draft attachments to the chat message that owns them, once that message has been persisted, so deleting the message removes them too. Identifiers that no longer exist are skipped.",

  // Editor tools - DocSpace tools exposed to the document editor's AI plugin.
  aiEditorToolsList:
    "Returns the sanitized catalog of DocSpace tools available to the document editor's AI plugin - the same composed tool set the DocSpace chat sees, minus the web-search pair the editor already has through its own passthrough. Only the name, description, parameters and approval flag of each tool are exposed; transport details never reach the browser.",
  aiEditorToolsCall:
    "Executes one DocSpace tool on behalf of the document editor's AI plugin, server-side and with the caller's forwarded credentials. Whatever the tool produced is returned for the plugin to relay to the model; a failure comes back as an error payload.",

  // Export.
  aiExportTextToDocx:
    "Starts an asynchronous markdown-to-docx export. The response only acknowledges the task: the AI Worker converts the content and saves the .docx into the target folder (an agent room resolves to its result-storage subfolder), and completion reaches the client as the usual folder-modified socket event.",

  // OpenAI passthrough - the editor plugin's external-provider transport.
  aiOpenaiChatCompletions:
    "OpenAI-compatible chat completions for the document editor's AI plugin. The profile is resolved server-side, its credentials are attached, and the body is forwarded to the provider verbatim - the payload is owned by the plugin's SDK on one end and the provider on the other. A client disconnect cancels the provider call.",
  aiOpenaiImagesGenerations:
    "OpenAI-compatible image generation for the document editor's AI plugin. As with the chat-completions passthrough, the profile's credentials are attached server-side and the body reaches the provider unchanged.",

  // Preferences - per-scope chat toggles.
  aiPreferencesGetDeepMode:
    "Returns the deep-mode toggle of the scope, falling back to the configured default when nothing has been persisted.",
  aiPreferencesSetDeepMode:
    "Persists the deep-mode toggle of the scope. Idempotent - there is no need to check whether a value already exists.",
  aiPreferencesClearDeepMode:
    "Drops the persisted deep-mode toggle of the scope, so later reads fall back to the configured default.",
  aiPreferencesIsDeepModeSet:
    "Tells whether the scope has an explicitly persisted deep-mode value, whichever way that value is set.",

  // Profiles - AI provider credentials and model discovery.
  aiProfilesCreate:
    "Creates an AI provider profile. The name must be unique and the credentials are validated against the provider before the profile is stored; the portal's first profile also takes the `Default` assignment slot.",
  aiProfilesUpdate:
    "Updates an AI provider profile, re-checking name uniqueness and the provider credentials.",
  aiProfilesDelete:
    "Deletes an AI provider profile and cleans up the assignments pointing at it - the `Default` slot moves to the first remaining profile, the other slots are unbound.",
  aiProfilesGetById:
    "Returns one AI provider profile, or an empty result when the identifier is unknown.",
  aiProfilesList: "Lists the portal's AI provider profiles.",
  aiProfilesListModels:
    "Lists the models the given profile's provider offers, as reported by the provider itself.",
  aiProfilesListProviderModels:
    "Lists the models a provider offers for the supplied endpoint and key, before any profile is created from them.",
  aiProfilesTestConnection:
    "Checks a stored profile's credentials against its provider and reports the provider's own error when the call fails. Nothing is written.",

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
  aiSettingsGet: "Reports the portal's combined AI configuration and readiness.",
  aiSettingsGetVectorization: "Returns the portal's vectorization settings.",
  aiSettingsSetVectorization: "Updates the portal's vectorization settings.",
  aiSettingsGetUser: "Returns the current user's AI settings.",
  aiSettingsSetUser: "Updates the current user's AI settings.",

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
    "Starts a vectorization task over the supplied portal files. The indexing itself runs asynchronously on the .NET side.",

  // Web search - the portal's provider configuration, plus the editor passthrough.
  aiWebSearchGetActiveConfig:
    "Returns the web-search configuration active in the scope, or an empty result when web search is not configured.",
  aiWebSearchIsConfigured: "Tells whether web search is configured in the scope.",
  aiWebSearchTestConnection:
    "Checks a web-search configuration against the live provider without storing it - for a Test button that must not commit on success.",
  aiWebSearchConfigure:
    "Validates a web-search configuration against the live provider and stores it only when the provider answers, replacing the previous one in a single write.",
  aiWebSearchSetActiveConfig:
    "Stores a web-search configuration without contacting the provider first, for forms that validate locally.",
  aiWebSearchClear:
    "Removes the web-search configuration of the scope. Does nothing when web search was not configured there.",
  aiWebSearchPassthroughSearch:
    "Runs a web search on behalf of the document editor's AI plugin. The plugin only holds a placeholder configuration; the portal's active provider and its key are resolved here and never reach the browser.",
  aiWebSearchPassthroughContents:
    "Fetches web page contents on behalf of the document editor's AI plugin, against the portal's active web-search provider, the same way as the search passthrough.",
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
    "413": "The request body is larger than this route accepts.",
    "429": true,
    "502": true,
  },
  aiOpenaiImagesGenerations: {
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
    responses[code] = jsonResponse(description as string, ERROR_RESPONSE_REF);
  }
  return responses;
}

function capitalize(name: string): string {
  return name.length > 0 ? name.charAt(0).toUpperCase() + name.slice(1) : name;
}

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
    schema: INTEGER_QUERY_PARAMS.has(name) ? { type: "integer" } : { type: "string" },
  }));
}

function pathParameters(names: readonly string[], operationId: string): Json[] {
  return names.map((name) => ({
    name,
    in: "path",
    ...(paramDescription(operationId, name) as object),
    required: true,
    schema: { type: "string" },
  }));
}

function jsonBody(schema: Json = JSON_OBJECT_SCHEMA): Json {
  return {
    required: true,
    content: { "application/json": { schema } },
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

function responseFor(operations: OperationSchemaLookup, operationId: string): Json {
  const schema = operations[operationId]?.response;
  const streaming = STREAMING_RESPONSES[operationId];
  if (streaming && schema !== undefined) {
    // The schema types a single streamed item; the media type frames the
    // stream. (A JSON `content` block would misrepresent the wire format.)
    return {
      description: streaming.description,
      content: { [streaming.mediaType]: { schema: schema as Json } },
    };
  }
  // No generated schema ⇒ a `void` engine method; every such controller
  // replies `{ success: true }`, so document that rather than a generic object.
  return schema === undefined
    ? jsonResponse("Success.", SUCCESS_RESPONSE_REF)
    : jsonResponse("Success.", schema as Json);
}

function requestBodyFor(operations: OperationSchemaLookup, operationId: string): Json {
  const schema = operations[operationId]?.request;
  return schema === undefined ? jsonBody() : jsonBody(schema as Json);
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
    summary: humanize(methodName),
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
