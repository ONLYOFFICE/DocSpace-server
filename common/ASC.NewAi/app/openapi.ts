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
    "Runs one AI action: the profile bound to `actionType` (falling back to the `Default` slot) is dispatched against a single-message history. Nothing is persisted - no thread, no title generation, no storage writes.",
  aiAiSendCustom:
    "Runs a free-form one-turn call against a caller-supplied system prompt. No thread, no history and no persistence. The profile is the explicit `profileId` when it resolves, otherwise the `Default` assignment slot.",
  aiAiSendWithStream:
    "Starts a chat round and streams it back as newline-delimited `ChatEvent` objects. The thread is opened or created, the user message and the reply are persisted, a new thread gets a generated title, and a tool call pauses the round until it is approved or denied.",
  aiAiSendWithStreamOpenAI:
    'The same chat round as `send-with-stream`, re-encoded as an OpenAI Chat Completions stream of `chat.completion.chunk` objects. Storage, title generation and tool-call pauses are identical - only the wire shape differs; a tool call ends the stream with `finish_reason: "tool_calls"`.',
  aiAiRegenerateStream:
    "Re-rolls the last assistant reply in an existing thread: every message after the last user message (the previous reply plus any tool-call hops) is dropped and a fresh reply is streamed against the unchanged prompt. The thread must already exist and no title is generated.",
  aiAiApproveToolCall:
    "Resumes a chat round paused on a tool call. The supplied result is persisted onto the assistant message that issued the call and the stream continues with the augmented history.",
  aiAiDenyToolCall:
    'Denies the pending tool call and resumes the chat immediately, with `"User deny tool call"` standing in for the tool result.',

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
    "Saves a new prompt. The name must be non-empty and unique inside its folder, and `folderId` must point at an existing folder - omit it for the root.",
  aiPromptsUpdate:
    "Updates a saved prompt. The name and the folder reference are re-validated whenever either of them changes.",
  aiPromptsMove:
    "Moves a saved prompt into another folder, or to the root. The name is re-validated in the target folder, so the move fails when a prompt of that name is already there.",
  aiPromptsDelete: "Deletes a saved prompt. Does nothing when it no longer exists.",
  aiPromptsList:
    "Lists saved prompts. Scope the answer to one folder, ask for the root-level prompts only, or omit the folder to get every prompt newest first.",
  aiPromptsGetById: "Returns one saved prompt, or an empty result when the identifier is unknown.",
  aiPromptsCreateFolder:
    "Creates a prompt folder. The name must be non-empty and unique across the portal - prompt folders do not nest.",
  aiPromptsRenameFolder:
    "Renames a prompt folder, validating the new name against the existing folders.",
  aiPromptsDeleteFolder: "Deletes a prompt folder together with the prompts inside it.",
  aiPromptsListFolders: "Lists the prompt folders, newest first.",
  aiPromptsGetFolderById:
    "Returns one prompt folder, or an empty result when the identifier is unknown.",
  aiPromptsExport:
    "Builds a self-contained, versioned bundle of every saved prompt and folder, ready for `import-bundle`.",
  aiPromptsImportBundle:
    "Restores a prompt bundle. `replace` wipes the current prompts and folders before writing the bundle, `merge` writes the bundle on top of what is already there; both validate the folder references inside the bundle before any write, so a corrupt bundle is rejected whole.",

  // Settings - proxied to the .NET AI service.
  aiSettingsGet: "Reports the portal's combined AI configuration and readiness.",
  aiSettingsGetVectorization: "Returns the portal's vectorization settings.",
  aiSettingsSetVectorization: "Updates the portal's vectorization settings.",
  aiSettingsGetUser: "Returns the current user's AI settings.",
  aiSettingsSetUser: "Updates the current user's AI settings.",

  // Threads - chat threads and their messages.
  aiThreadsCreate:
    "Creates a chat thread with a caller-supplied title. Use `open-or-create` instead when the title should be generated from the first user message.",
  aiThreadsOpenOrCreate:
    "Opens a chat thread and returns its history, or creates one with a title generated from the supplied first message. That first message is not persisted - the caller decides whether to follow up with `append-user-message`.",
  aiThreadsAppendUserMessage:
    "Persists a user message in a thread and bumps the thread's last-edit date so it resurfaces in the sidebar. Optionally rebinds the thread to another profile when the model changed mid-conversation.",
  aiThreadsTouch:
    "Bumps a thread's last-edit date, and optionally rebinds it to another profile, when something other than a new message - a model switch, say - should resurface it.",
  aiThreadsRename:
    "Renames a chat thread and bumps its last-edit date so the new title shows up in the sidebar.",
  aiThreadsDelete: "Deletes a chat thread together with its messages.",
  aiThreadsClearMessages:
    "Drops every message of a thread while keeping the thread itself, and bumps its last-edit date.",
  aiThreadsRegenerateTitle:
    "Generates a fresh title from the thread's first user message and persists it. Fails when the thread has no user message yet.",
  aiThreadsList:
    "Lists the chat threads of the scope, most recently edited first. Supports cursor pagination and a server-side case-insensitive title search.",
  aiThreadsReadMessages:
    "Reads the messages of a thread, with the same cursor pagination as the thread list.",
  aiThreadsGetById: "Returns one chat thread, or an empty result when the identifier is unknown.",
  aiThreadsGetMessageById: "Returns one chat message by its globally unique identifier.",
  aiThreadsUpdateMessage:
    "Replaces the content of a chat message - used by the edit and regenerate flows that change a message outside the streaming lifecycle.",
  aiThreadsDeleteMessage: "Deletes one chat message, leaving the rest of the thread untouched.",

  // Tools - custom MCP servers and per-tool preferences.
  aiToolsAddCustomServer: "Registers a custom MCP server in the scope under the given name.",
  aiToolsUpdateCustomServer: "Updates the configuration of a registered custom MCP server.",
  aiToolsRemoveCustomServer: "Removes a custom MCP server from the registry.",
  aiToolsGetCustomServer:
    "Returns the configuration of one custom MCP server, or an empty result when it is not registered.",
  aiToolsListCustomServers: "Lists the custom MCP servers registered in the scope, keyed by name.",
  aiToolsReplaceAllCustomServers:
    "Replaces the whole custom MCP server registry of the scope with the supplied map.",
  aiToolsListSystemTools:
    "Lists the tools of the host-configured system MCP servers, grouped by server type. The servers are connected and listed server-side, so the client renders its permission cards from one request and never opens an MCP connection of its own.",
  aiToolsSetDisabled:
    "Marks the listed tools of one server type as switched off, so the model is no longer offered them.",
  aiToolsGetDisabled: "Returns the switched-off tools of the scope, grouped by server type.",
  aiToolsIsToolDisabled: "Tells whether one tool of a server type is switched off.",
  aiToolsSetAllowAlways:
    "Adds a tool to the always-allow list, or removes it - the tools on that list run without an approval dialog.",
  aiToolsGetAllowAlways: "Lists the tools on the always-allow list of the scope.",
  aiToolsIsAllowAlways: "Tells whether one tool is on the always-allow list.",

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
      "200": responseFor(operations, operationId),
      "401": UNAUTHORIZED_RESPONSE,
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
      "200": responseFor(operations, route.operationId),
      "401": UNAUTHORIZED_RESPONSE,
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
