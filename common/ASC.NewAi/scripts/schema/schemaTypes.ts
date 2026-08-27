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

/// <reference path="./shims.d.ts" />

// OpenAPI request/response type source. Each `Req_<operationId>` /
// `Res_<operationId>` alias names the concrete TypeScript type of an
// operation's JSON request body / response, derived from the
// `@onlyoffice/ai-chat` engine method signatures. The build-time emitter
// (`scripts/generate-openapi.ts`) runs `ts-json-schema-generator` over this
// file to turn these into JSON Schemas (with JSDoc descriptions) that back
// the OpenAPI document — replacing the generic `object` fallback.
//
// Conventions, mirroring the runtime controllers:
//   • Request body of a single-argument engine method is that argument's
//     type directly (see `unpackPositional`); a multi-argument method's body
//     is an object keyed by the engine parameter names.
//   • Responses unwrap the method's `Promise`; streaming methods
//     (`AsyncGenerator`) are typed by the streamed event.
//   • Some library key maps (`Partial<Record<ActionType, string>>`) are
//     simplified to `Record<string, ...>` — the generator does not expand
//     enum-keyed records, and the wire shape is a plain string-keyed object.
//   • `void`-returning methods declare no `Res_` alias.

import type {
  Profile,
  Thread,
  Prompt,
  PromptFolder,
  Model,
  Attachment,
  McpServerConfig,
  WebSearchConfig,
  ActionType,
  TMCPItem,
  ChatEvent,
  OpenAIStreamChunk,
  ProviderType,
  CreateProfileInput,
  ProfileMutationResult,
  CreatePromptInput,
  PromptMutationResult,
  FolderMutationResult,
  PromptBundle,
  ImportResult,
  ImportMode,
  OpenOrCreateResult,
  AssignmentMutationResult,
  BulkAssignmentResult,
  ResolvedAssignment,
  ToolsMutationResult,
  ToolsBulkResult,
  WebSearchMutationResult,
} from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";

/* ------------------------------ Common --------------------------------- */

// Shared response envelopes, referenced by `openapi.ts` in place of the
// opaque generic-object fallback. Every mutating endpoint with no richer
// payload replies `{ success: true }` (see the controllers); every error
// (401 without a session, and the generic error handler) replies
// `{ error: <message> }`.

/** Generic success acknowledgement for mutations that return no data. */
export type SuccessResponse = { success: boolean };

/** Error body — a single human-readable message. */
export type ErrorResponse = { error: string };

/* ------------------------------- AI ------------------------------------ */

// The AI engine's own request inputs (`SendInput`, `SendStreamInput`, …)
// carry two non-serializable fields — `actionArgs.signal` (`AbortSignal`) and
// `actionArgs.fetch` (a provider `fetch` hook typed against DOM
// `RequestInfo`/`Response`) — that neither JSON Schema nor the generator can
// represent, and that never travel on the wire: the server injects the abort
// signal and provider fetch itself. Rather than import those library inputs
// directly (which collapse to `{}` under `skipTypeCheck`), the bodies below
// mirror their serializable wire subset. Streaming responses are typed by the
// streamed event (`ChatEvent` / `OpenAIStreamChunk`); the stream media type
// (ndjson / SSE) is applied per-operation in `openapi.ts`.

// Wire-serializable subset of the engine's `ActionArgs` — drops the
// engine-injected `signal`/`fetch`; `profile`/`messages` are owned by the
// engine and never sent by the caller.
export type AiActionArgs = {
  /** Extra tools offered to the model for this request. */
  tools?: TMCPItem[];
  /** Enable extended thinking / reasoning for this request. */
  isReasoning?: boolean;
  /** Override the action's baked-in system prompt (replace or append). */
  prompt?: { mode: "replace" | "append"; text: string };
};

export type Req_aiAiSend = {
  /** Which AI action to run — selects the assignment slot and action. */
  actionType: ActionType;
  /** The user turn to send. */
  userMessage: ThreadMessageLike;
  actionArgs?: AiActionArgs;
  /** Optional entity (room) scope for profile resolution. */
  entityId?: string;
};
export type Res_aiAiSend = ThreadMessageLike;

export type Req_aiAiSendCustom = {
  /** Stream the reply (ndjson) when true, else return a single message. */
  isStream: boolean;
  /** Caller-supplied system prompt for this one-turn call. */
  systemPrompt: string;
  userMessage: ThreadMessageLike;
  actionArgs?: AiActionArgs;
};
/**
 * One-shot mode (`isStream: false`) returns the assistant message shown here;
 * streaming mode instead emits a newline-delimited `ChatEvent` stream.
 */
export type Res_aiAiSendCustom = ThreadMessageLike;

// Shared body of the two streaming send endpoints (`sendWithStream` and its
// OpenAI-framed twin) — the `Chat` action is implied, so there is no
// `actionType`.
export type AiSendStreamBody = {
  /** Target thread; a new one is created (with an auto title) when omitted. */
  threadId?: string;
  /** The user turn to send. */
  userMessage: ThreadMessageLike;
  actionArgs?: AiActionArgs;
  /** Optional entity (room) scope for profile resolution. */
  entityId?: string;
  /** Session-level profile override for this request only. */
  profileId?: string;
};

export type Req_aiAiSendWithStream = AiSendStreamBody;
export type Res_aiAiSendWithStream = ChatEvent;

export type Req_aiAiSendWithStreamOpenAI = AiSendStreamBody;
export type Res_aiAiSendWithStreamOpenAI = OpenAIStreamChunk;

export type Req_aiAiRegenerateStream = {
  /** Target thread (must already exist). */
  threadId: string;
  actionArgs?: AiActionArgs;
  entityId?: string;
  profileId?: string;
};
export type Res_aiAiRegenerateStream = ChatEvent;

// Identifies a pending tool call to resume — mirrors the library
// `ToolCallData` (its serializable fields).
export type AiToolCallData = {
  /** Thread the assistant message belongs to. */
  threadId: string;
  /** Storage id of the assistant message holding the tool call. */
  messageId: string;
  /** Index of the tool-call content part inside `message.content`. */
  idx: number;
  /** Snapshot of the assistant message at the time the tool call surfaced. */
  message: ThreadMessageLike;
  actionArgs?: AiActionArgs;
  entityId?: string;
  profileId?: string;
};

export type Req_aiAiApproveToolCall = AiToolCallData & {
  /** Final result of the tool call, as the model should see it. */
  result: unknown;
  /** Persist auto-approve for this tool's name. */
  allowAlways?: boolean;
};
export type Res_aiAiApproveToolCall = ChatEvent;

export type Req_aiAiDenyToolCall = AiToolCallData;
export type Res_aiAiDenyToolCall = ChatEvent;

/* --------------------------- Assignments ------------------------------- */

export type Res_aiAssignmentsResolveForAction = ResolvedAssignment;
export type Res_aiAssignmentsTryResolveForAction = ResolvedAssignment | null;
export type Req_aiAssignmentsAssign = {
  /** Action the assignment applies to. */
  actionType: ActionType;
  /** Profile id to bind. */
  profileId: string;
};
export type Res_aiAssignmentsAssign = AssignmentMutationResult;
export type Req_aiAssignmentsUnassign = ActionType;
export type Req_aiAssignmentsBulkAssign = Record<string, string>;
export type Res_aiAssignmentsBulkAssign = BulkAssignmentResult;
export type Res_aiAssignmentsGetAssignment = string | null;
export type Res_aiAssignmentsGetAllAssignments = Record<string, string>;
export type Req_aiAssignmentsCascadeProfileDelete = string;

/* ---------------------------- Attachments ------------------------------ */

/** A file attachment draft to persist. */
type SaveFileInput = {
  /** Storage path/key of the file. */
  path: string;
  /** File contents. */
  content: string;
  /** File type discriminator. */
  type: number;
  /** Optional display title. */
  title?: string;
};
/** An image attachment draft to persist. */
type SaveImageInput = {
  /** Image name. */
  name: string;
  /** Full `data:image/...;base64,…` data URL. */
  base64: string;
  /** Optional display title. */
  title?: string;
};

export type Req_aiAttachmentsSaveFile = {
  input: SaveFileInput;
  /** Optional entity (room) scope. */
  entityId?: string;
};
export type Res_aiAttachmentsSaveFile = Attachment;
export type Req_aiAttachmentsSaveFilesMany = {
  inputs: SaveFileInput[];
  entityId?: string;
};
export type Res_aiAttachmentsSaveFilesMany = Attachment[];
export type Req_aiAttachmentsSaveImage = {
  input: SaveImageInput;
  entityId?: string;
};
export type Res_aiAttachmentsSaveImage = Attachment;
export type Req_aiAttachmentsSaveImagesMany = {
  inputs: SaveImageInput[];
  entityId?: string;
};
export type Res_aiAttachmentsSaveImagesMany = Attachment[];
export type Req_aiAttachmentsGet = string;
export type Res_aiAttachmentsGet = Attachment | null;
export type Req_aiAttachmentsGetMany = string[];
export type Res_aiAttachmentsGetMany = (Attachment | null)[];
export type Req_aiAttachmentsDelete = string;
export type Req_aiAttachmentsDeleteMany = string[];
export type Req_aiAttachmentsLinkToMessage = {
  /** Attachment ids to bind. */
  ids: string[];
  /** Owning message id. */
  messageId: string;
  /** Owning thread id. */
  threadId: string;
};

/* ---------------------------- Preferences ------------------------------ */

export type Res_aiPreferencesGetDeepMode = boolean;
export type Req_aiPreferencesSetDeepMode = {
  /** New deep-mode value. */
  value: boolean;
  entityId?: string;
};
export type Req_aiPreferencesClearDeepMode = string;
export type Res_aiPreferencesIsDeepModeSet = boolean;

/* ------------------------------ Profiles ------------------------------- */

export type Req_aiProfilesCreate = CreateProfileInput;
export type Res_aiProfilesCreate = ProfileMutationResult;
export type Req_aiProfilesUpdate = Profile;
export type Res_aiProfilesUpdate = ProfileMutationResult;
export type Req_aiProfilesDelete = string;
export type Res_aiProfilesListModels = Model[];
export type Req_aiProfilesListProviderModels = {
  /** Provider whose catalog to list. */
  providerType: ProviderType;
  /** Provider API base URL. */
  baseUrl: string;
  /** Provider API key. */
  apiKey: string;
};
export type Res_aiProfilesListProviderModels = Model[];
export type Req_aiProfilesTestConnection = string;
export type Res_aiProfilesTestConnection = true | { message?: string };
// `key` and `headers` are stripped from the HTTP response (Bug 82821) —
// see profilesController.getById.
export type Res_aiProfilesGetById = Omit<Profile, "key" | "headers">;
export type Res_aiProfilesList = Profile[];

/* ------------------------------- Prompts ------------------------------- */

export type Req_aiPromptsCreate = CreatePromptInput;
export type Res_aiPromptsCreate = PromptMutationResult;
export type Req_aiPromptsUpdate = {
  /** Prompt id to update. */
  id: string;
  /** Fields to change. */
  updates: {
    name?: string;
    text?: string;
    folderId?: string | null;
  };
};
export type Res_aiPromptsUpdate = PromptMutationResult;
export type Req_aiPromptsMove = {
  /** Prompt id to move. */
  id: string;
  /** Target folder id, or `null` for root. */
  folderId: string | null;
};
export type Res_aiPromptsMove = PromptMutationResult;
export type Req_aiPromptsDelete = string;
export type Res_aiPromptsList = Prompt[];
export type Req_aiPromptsCreateFolder = string;
export type Res_aiPromptsCreateFolder = FolderMutationResult;
export type Req_aiPromptsRenameFolder = {
  /** Folder id to rename. */
  id: string;
  /** New folder name. */
  name: string;
};
export type Res_aiPromptsRenameFolder = FolderMutationResult;
export type Req_aiPromptsDeleteFolder = string;
export type Res_aiPromptsListFolders = PromptFolder[];
export type Res_aiPromptsExport = PromptBundle;
export type Req_aiPromptsImportBundle = {
  /** Bundle to restore. */
  bundle: PromptBundle;
  /** Import options. */
  options?: { mode?: ImportMode };
};
export type Res_aiPromptsImportBundle = ImportResult;
export type Res_aiPromptsGetById = Prompt | null;
export type Res_aiPromptsGetFolderById = PromptFolder | null;

/* ------------------------------- Threads ------------------------------- */

export type Req_aiThreadsCreate = {
  /** Thread title. */
  title: string;
  /** Optional profile to bind. */
  profileId?: string;
  /** Optional entity (room) scope. */
  entityId?: string;
};
export type Res_aiThreadsCreate = Thread;
// Mirrored by hand instead of aliasing the lib's OpenOrCreateInput: its
// `entityMeta` is Pick<ActionArgs, ...>, and resolving ActionArgs drags the
// DOM-typed ProviderFetch function type into the schema graph, which
// ts-json-schema-generator cannot parse. Field set must track the lib type.
export type Req_aiThreadsOpenOrCreate = {
  threadId?: string;
  /** Profile the title generation runs on. */
  profile: Profile;
  profileId: string;
  /** First user message a fresh thread derives its title from. */
  firstMessage: ThreadMessageLike;
  /** Opaque scope token persisted on a freshly created thread. */
  entityId?: string;
  /**
   * Optional entity hint (lib 0.5.64): only `entityId` is read; the pair is
   * re-resolved server-side before reaching the provider as metadata.
   */
  entityMeta?: { entityId?: string; entityTitle?: string };
};
export type Res_aiThreadsOpenOrCreate = OpenOrCreateResult;
export type Req_aiThreadsAppendUserMessage = {
  threadId: string;
  /** Message to persist (id/createdAt are storage-assigned). */
  message: ThreadMessageLike;
  profileId?: string;
};
export type Res_aiThreadsAppendUserMessage = ThreadMessageLike;
export type Req_aiThreadsTouch = {
  threadId: string;
  profileId?: string;
};
export type Req_aiThreadsRename = {
  threadId: string;
  /** New thread title. */
  title: string;
};
export type Req_aiThreadsDelete = string;
export type Req_aiThreadsClearMessages = string;
export type Req_aiThreadsRegenerateTitle = {
  threadId: string;
  /** Profile used to regenerate the title. */
  profile: Profile;
  /**
   * Optional entity hint (lib 0.5.64): only `entityId` is read; the pair is
   * re-resolved server-side before reaching the provider as metadata.
   */
  entityMeta?: { entityId?: string; entityTitle?: string };
};
export type Res_aiThreadsRegenerateTitle = string;
export type Res_aiThreadsList = Thread[];
export type Res_aiThreadsReadMessages = ThreadMessageLike[];
export type Res_aiThreadsGetById = Thread | null;
export type Res_aiThreadsGetMessageById = ThreadMessageLike | null;
export type Req_aiThreadsUpdateMessage = {
  messageId: string;
  /** Replacement message content. */
  message: ThreadMessageLike;
};
export type Req_aiThreadsDeleteMessage = string;

/* -------------------------------- Tools -------------------------------- */

export type Req_aiToolsAddCustomServer = {
  /** Server name (unique within scope). */
  name: string;
  /** Server transport configuration. */
  config: McpServerConfig;
  entityId?: string;
};
export type Res_aiToolsAddCustomServer = ToolsMutationResult;
export type Req_aiToolsUpdateCustomServer = {
  name: string;
  config: McpServerConfig;
  entityId?: string;
};
export type Res_aiToolsUpdateCustomServer = ToolsMutationResult;
export type Req_aiToolsRemoveCustomServer = {
  name: string;
  entityId?: string;
};
export type Res_aiToolsGetCustomServer = McpServerConfig | null;
export type Res_aiToolsListCustomServers = Record<string, McpServerConfig>;
export type Res_aiToolsListSystemTools = Record<string, TMCPItem[]>;
export type Req_aiToolsReplaceAllCustomServers = {
  /** Full replacement set, keyed by server name. */
  map: Record<string, McpServerConfig>;
  entityId?: string;
};
export type Res_aiToolsReplaceAllCustomServers = ToolsBulkResult;
export type Req_aiToolsSetDisabled = {
  serverType: string;
  /** Tool names to disable. */
  toolNames: string[];
  entityId?: string;
};
export type Res_aiToolsGetDisabled = Record<string, string[]>;
export type Res_aiToolsIsToolDisabled = boolean;
export type Req_aiToolsSetAllowAlways = {
  serverType: string;
  toolName: string;
  /** Whether the tool is always allowed. */
  value: boolean;
  entityId?: string;
};
export type Res_aiToolsGetAllowAlways = string[];
export type Res_aiToolsIsAllowAlways = boolean;

/* ----------------------------- Web search ------------------------------ */

export type Res_aiWebSearchGetActiveConfig = WebSearchConfig | null;
export type Res_aiWebSearchIsConfigured = boolean;
export type Req_aiWebSearchTestConnection = WebSearchConfig;
export type Res_aiWebSearchTestConnection = true | { message?: string };
export type Req_aiWebSearchConfigure = {
  config: WebSearchConfig;
  entityId?: string;
};
export type Res_aiWebSearchConfigure = WebSearchMutationResult;
export type Req_aiWebSearchSetActiveConfig = {
  config: WebSearchConfig;
  entityId?: string;
};
export type Req_aiWebSearchClear = string;

/* --------------------- Custom routes (agents / export) ----------------- */

export type Req_aiExportTextToDocx = {
  /** Document title (also the file name). */
  title: string;
  /** Markdown content to convert. */
  content: string;
  /** Target folder id (int or string). */
  folderId: string | number;
};
/** Accepted-for-processing acknowledgement (conversion is asynchronous). */
export type Res_aiExportTextToDocx = { success: boolean };
/**
 * A DocSpace room id: an integer for native rooms, a string for
 * third-party-backed ones.
 */
type RoomId = number | string;

/**
 * Room fields shared by agent creation and update, mirroring the .NET
 * `CreateAgentRequestDto` / `UpdateRoomRequest` (see
 * `products/ASC.AI/Server/Api/AgentsController.cs`). The nested objects
 * (`lifetime`, `watermark`, `logo`) are DocSpace room DTOs forwarded
 * verbatim; they are documented as open objects rather than duplicating the
 * full .NET shape here.
 */
type AgentRoomFields = {
  /** Agent (room) title. */
  title?: string;
  /** Room quota in bytes. */
  quota?: number;
  /** Whether room content is indexed for search. */
  indexing?: boolean;
  /** Whether downloading room content is denied. */
  denyDownload?: boolean;
  /** Room data lifetime policy (`RoomDataLifetimeDto`). */
  lifetime?: Record<string, unknown>;
  /** Watermark settings (`WatermarkRequestDto`). */
  watermark?: Record<string, unknown>;
  /** Room logo (`LogoRequest`). */
  logo?: Record<string, unknown>;
  /** Room tags. */
  tags?: string[];
  /** Room accent color. */
  color?: string;
  /** Room cover image id. */
  cover?: string;
};

export type Req_aiAgentsCreate = AgentRoomFields & {
  /** Profile id bound to the agent. */
  profileId: string;
  /** Agent system prompt; stored as the room's `chatSettings.prompt`. */
  prompt: string;
  /** Whether the agent room is private. */
  private?: boolean;
  /** Initial share entries (`FileShareParams`). */
  share?: Record<string, unknown>[];
  /** Whether to attach the default DocSpace MCP tool server. */
  attachDefaultTools?: boolean;
};

export type Req_aiAgentsUpdate = AgentRoomFields & {
  /** Profile id to rebind (optional). */
  profileId?: string;
  /** Chat settings (`ChatSettings`); requires a valid provider/model. */
  chatSettings?: Record<string, unknown>;
  /** Whether form results are sent to an external DB. */
  sendFormToExternalDB?: boolean;
  /** Whether forms are saved as XLSX. */
  saveFormAsXLSX?: boolean;
};

export type Req_aiAgentsDelete = {
  /** Delete the room after the editing session finishes. */
  deleteAfter?: boolean;
};

export type Req_aiAgentsUpdateQuota = {
  /** Agent (room) ids to update. */
  roomIds: RoomId[];
  /** New quota in bytes; a negative value disables the custom quota. */
  quota: number;
};

export type Req_aiAgentsResetQuota = {
  /** Agent (room) ids to reset to the tenant default quota. */
  roomIds: RoomId[];
};
