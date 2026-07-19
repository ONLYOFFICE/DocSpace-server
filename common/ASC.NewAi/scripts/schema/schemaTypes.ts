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
  ProviderType,
  CreateProfileInput,
  ProfileMutationResult,
  CreatePromptInput,
  PromptMutationResult,
  FolderMutationResult,
  PromptBundle,
  ImportResult,
  ImportMode,
  OpenOrCreateInput,
  OpenOrCreateResult,
  AssignmentMutationResult,
  BulkAssignmentResult,
  ResolvedAssignment,
  ToolsMutationResult,
  ToolsBulkResult,
  WebSearchMutationResult,
} from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";

/* ------------------------------- AI ------------------------------------ */

// The AI engine's request inputs (`SendInput`, `SendStreamInput`, …) carry
// non-serializable fields (provider `fetch` hooks typed against DOM
// `RequestInfo`/`Response`) that neither JSON Schema nor the schema
// generator can represent. The AI operations are also streaming (ndjson /
// SSE), so their responses are event streams rather than a single JSON body.
// They are therefore left with the documented generic-object fallback rather
// than a concrete schema. All other engines below are fully typed.

/* --------------------------- Assignments ------------------------------- */

export type Res_newAiAssignmentsResolveForAction = ResolvedAssignment;
export type Res_newAiAssignmentsTryResolveForAction = ResolvedAssignment | null;
export type Req_newAiAssignmentsAssign = {
  /** Action the assignment applies to. */
  actionType: ActionType;
  /** Profile id to bind. */
  profileId: string;
};
export type Res_newAiAssignmentsAssign = AssignmentMutationResult;
export type Req_newAiAssignmentsUnassign = ActionType;
export type Req_newAiAssignmentsBulkAssign = Record<string, string>;
export type Res_newAiAssignmentsBulkAssign = BulkAssignmentResult;
export type Res_newAiAssignmentsGetAssignment = string | null;
export type Res_newAiAssignmentsGetAllAssignments = Record<string, string>;
export type Req_newAiAssignmentsCascadeProfileDelete = string;

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

export type Req_newAiAttachmentsSaveFile = {
  input: SaveFileInput;
  /** Optional entity (room) scope. */
  entityId?: string;
};
export type Res_newAiAttachmentsSaveFile = Attachment;
export type Req_newAiAttachmentsSaveFilesMany = {
  inputs: SaveFileInput[];
  entityId?: string;
};
export type Res_newAiAttachmentsSaveFilesMany = Attachment[];
export type Req_newAiAttachmentsSaveImage = {
  input: SaveImageInput;
  entityId?: string;
};
export type Res_newAiAttachmentsSaveImage = Attachment;
export type Req_newAiAttachmentsSaveImagesMany = {
  inputs: SaveImageInput[];
  entityId?: string;
};
export type Res_newAiAttachmentsSaveImagesMany = Attachment[];
export type Req_newAiAttachmentsGet = string;
export type Res_newAiAttachmentsGet = Attachment | null;
export type Req_newAiAttachmentsGetMany = string[];
export type Res_newAiAttachmentsGetMany = (Attachment | null)[];
export type Req_newAiAttachmentsDelete = string;
export type Req_newAiAttachmentsDeleteMany = string[];
export type Req_newAiAttachmentsLinkToMessage = {
  /** Attachment ids to bind. */
  ids: string[];
  /** Owning message id. */
  messageId: string;
  /** Owning thread id. */
  threadId: string;
};

/* ---------------------------- Preferences ------------------------------ */

export type Res_newAiPreferencesGetDeepMode = boolean;
export type Req_newAiPreferencesSetDeepMode = {
  /** New deep-mode value. */
  value: boolean;
  entityId?: string;
};
export type Req_newAiPreferencesClearDeepMode = string;
export type Res_newAiPreferencesIsDeepModeSet = boolean;

/* ------------------------------ Profiles ------------------------------- */

export type Req_newAiProfilesCreate = CreateProfileInput;
export type Res_newAiProfilesCreate = ProfileMutationResult;
export type Req_newAiProfilesUpdate = Profile;
export type Res_newAiProfilesUpdate = ProfileMutationResult;
export type Req_newAiProfilesDelete = string;
export type Res_newAiProfilesListModels = Model[];
export type Req_newAiProfilesListProviderModels = {
  /** Provider whose catalog to list. */
  providerType: ProviderType;
  /** Provider API base URL. */
  baseUrl: string;
  /** Provider API key. */
  apiKey: string;
};
export type Res_newAiProfilesListProviderModels = Model[];
export type Req_newAiProfilesTestConnection = string;
export type Res_newAiProfilesTestConnection = true | { message?: string };
export type Res_newAiProfilesGetById = Profile;
export type Res_newAiProfilesList = Profile[];

/* ------------------------------- Prompts ------------------------------- */

export type Req_newAiPromptsCreate = CreatePromptInput;
export type Res_newAiPromptsCreate = PromptMutationResult;
export type Req_newAiPromptsUpdate = {
  /** Prompt id to update. */
  id: string;
  /** Fields to change. */
  updates: {
    name?: string;
    text?: string;
    folderId?: string | null;
  };
};
export type Res_newAiPromptsUpdate = PromptMutationResult;
export type Req_newAiPromptsMove = {
  /** Prompt id to move. */
  id: string;
  /** Target folder id, or `null` for root. */
  folderId: string | null;
};
export type Res_newAiPromptsMove = PromptMutationResult;
export type Req_newAiPromptsDelete = string;
export type Res_newAiPromptsList = Prompt[];
export type Req_newAiPromptsCreateFolder = string;
export type Res_newAiPromptsCreateFolder = FolderMutationResult;
export type Req_newAiPromptsRenameFolder = {
  /** Folder id to rename. */
  id: string;
  /** New folder name. */
  name: string;
};
export type Res_newAiPromptsRenameFolder = FolderMutationResult;
export type Req_newAiPromptsDeleteFolder = string;
export type Res_newAiPromptsListFolders = PromptFolder[];
export type Res_newAiPromptsExport = PromptBundle;
export type Req_newAiPromptsImportBundle = {
  /** Bundle to restore. */
  bundle: PromptBundle;
  /** Import options. */
  options?: { mode?: ImportMode };
};
export type Res_newAiPromptsImportBundle = ImportResult;
export type Res_newAiPromptsGetById = Prompt | null;
export type Res_newAiPromptsGetFolderById = PromptFolder | null;

/* ------------------------------- Threads ------------------------------- */

export type Req_newAiThreadsCreate = {
  /** Thread title. */
  title: string;
  /** Optional profile to bind. */
  profileId?: string;
  /** Optional entity (room) scope. */
  entityId?: string;
};
export type Res_newAiThreadsCreate = Thread;
export type Req_newAiThreadsOpenOrCreate = OpenOrCreateInput;
export type Res_newAiThreadsOpenOrCreate = OpenOrCreateResult;
export type Req_newAiThreadsAppendUserMessage = {
  threadId: string;
  /** Message to persist (id/createdAt are storage-assigned). */
  message: ThreadMessageLike;
  profileId?: string;
};
export type Res_newAiThreadsAppendUserMessage = ThreadMessageLike;
export type Req_newAiThreadsTouch = {
  threadId: string;
  profileId?: string;
};
export type Req_newAiThreadsRename = {
  threadId: string;
  /** New thread title. */
  title: string;
};
export type Req_newAiThreadsDelete = string;
export type Req_newAiThreadsClearMessages = string;
export type Req_newAiThreadsRegenerateTitle = {
  threadId: string;
  /** Profile used to regenerate the title. */
  profile: Profile;
};
export type Res_newAiThreadsRegenerateTitle = string;
export type Res_newAiThreadsList = Thread[];
export type Res_newAiThreadsReadMessages = ThreadMessageLike[];
export type Res_newAiThreadsGetById = Thread | null;
export type Res_newAiThreadsGetMessageById = ThreadMessageLike | null;
export type Req_newAiThreadsUpdateMessage = {
  messageId: string;
  /** Replacement message content. */
  message: ThreadMessageLike;
};
export type Req_newAiThreadsDeleteMessage = string;

/* -------------------------------- Tools -------------------------------- */

export type Req_newAiToolsAddCustomServer = {
  /** Server name (unique within scope). */
  name: string;
  /** Server transport configuration. */
  config: McpServerConfig;
  entityId?: string;
};
export type Res_newAiToolsAddCustomServer = ToolsMutationResult;
export type Req_newAiToolsUpdateCustomServer = {
  name: string;
  config: McpServerConfig;
  entityId?: string;
};
export type Res_newAiToolsUpdateCustomServer = ToolsMutationResult;
export type Req_newAiToolsRemoveCustomServer = {
  name: string;
  entityId?: string;
};
export type Res_newAiToolsGetCustomServer = McpServerConfig | null;
export type Res_newAiToolsListCustomServers = Record<string, McpServerConfig>;
export type Res_newAiToolsListSystemTools = Record<string, TMCPItem[]>;
export type Req_newAiToolsReplaceAllCustomServers = {
  /** Full replacement set, keyed by server name. */
  map: Record<string, McpServerConfig>;
  entityId?: string;
};
export type Res_newAiToolsReplaceAllCustomServers = ToolsBulkResult;
export type Req_newAiToolsSetDisabled = {
  serverType: string;
  /** Tool names to disable. */
  toolNames: string[];
  entityId?: string;
};
export type Res_newAiToolsGetDisabled = Record<string, string[]>;
export type Res_newAiToolsIsToolDisabled = boolean;
export type Req_newAiToolsSetAllowAlways = {
  serverType: string;
  toolName: string;
  /** Whether the tool is always allowed. */
  value: boolean;
  entityId?: string;
};
export type Res_newAiToolsGetAllowAlways = string[];
export type Res_newAiToolsIsAllowAlways = boolean;

/* ----------------------------- Web search ------------------------------ */

export type Res_newAiWebSearchGetActiveConfig = WebSearchConfig | null;
export type Res_newAiWebSearchIsConfigured = boolean;
export type Req_newAiWebSearchTestConnection = WebSearchConfig;
export type Res_newAiWebSearchTestConnection = true | { message?: string };
export type Req_newAiWebSearchConfigure = {
  config: WebSearchConfig;
  entityId?: string;
};
export type Res_newAiWebSearchConfigure = WebSearchMutationResult;
export type Req_newAiWebSearchSetActiveConfig = {
  config: WebSearchConfig;
  entityId?: string;
};
export type Req_newAiWebSearchClear = string;

/* --------------------- Custom routes (agents / export) ----------------- */

export type Req_newAiExportTextToDocx = {
  /** Document title (also the file name). */
  title: string;
  /** Markdown content to convert. */
  content: string;
  /** Target folder id (int or string). */
  folderId: string | number;
};
/** Accepted-for-processing acknowledgement (conversion is asynchronous). */
export type Res_newAiExportTextToDocx = { success: boolean };
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

export type Req_newAiAgentsCreate = AgentRoomFields & {
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

export type Req_newAiAgentsUpdate = AgentRoomFields & {
  /** Profile id to rebind (optional). */
  profileId?: string;
  /** Chat settings (`ChatSettings`); requires a valid provider/model. */
  chatSettings?: Record<string, unknown>;
  /** Whether form results are sent to an external DB. */
  sendFormToExternalDB?: boolean;
  /** Whether forms are saved as XLSX. */
  saveFormAsXLSX?: boolean;
};

export type Req_newAiAgentsDelete = {
  /** Delete the room after the editing session finishes. */
  deleteAfter?: boolean;
};

export type Req_newAiAgentsUpdateQuota = {
  /** Agent (room) ids to update. */
  roomIds: RoomId[];
  /** New quota in bytes; a negative value disables the custom quota. */
  quota: number;
};

export type Req_newAiAgentsResetQuota = {
  /** Agent (room) ids to reset to the tenant default quota. */
  roomIds: RoomId[];
};
