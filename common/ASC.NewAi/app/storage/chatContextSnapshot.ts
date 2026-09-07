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

import type { McpServerConfig, Profile, Thread, WebSearchConfig } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import type { DocspaceFolderInfo } from "./docspaceFilesApi.js";
import type { JsonObject } from "../narrow.js";
import { getChatContextSnapshot, noteChatContextMiss } from "../requestContext.js";
import logger from "../log.js";

/**
 * The read-only slices a chat round consumes. A write into a slice marks it
 * stale (see {@link invalidateChatContext}) so a later read in the same
 * request goes back to the AI service instead of serving pre-write data.
 */
export type ChatContextSlice =
  | "profiles"
  | "assignments"
  | "preferences"
  | "toolPrefs"
  | "mcpServers"
  | "webSearch"
  | "thread"
  | "messages";

/** One scope of the aggregate: the global one, or a folder's own. */
export interface ChatContextScope {
  entityId: string | undefined;
  /** Absent for the global scope. */
  folder: DocspaceFolderInfo | undefined;
  /** `ActionType` name -> profile id, already resolved by the C# side. */
  assignments: Record<string, string>;
  deepMode: boolean | null;
  /** Raw `serverType -> { disabled, allowAlways }` map as the C# storage serves it. */
  toolPrefs: JsonObject;
  mcpServers: Record<string, McpServerConfig>;
}

/**
 * Everything a chat round reads, fetched in one `GET internal/ai/chat-context`
 * call at the start of the round (see `storage/chatContext.ts`) and held in
 * the request context. Storage read methods serve from it; a missing slice,
 * an unknown scope or a stale slice falls back to the per-entity endpoint.
 */
export interface ChatContextSnapshot {
  requested: {
    threadId: string | undefined;
    entityId: string | undefined;
    contextEntityId: string | undefined;
  };
  aiReady: boolean | undefined;
  profiles: Profile[];
  global: ChatContextScope;
  /** `null` when `entityId` was given but the folder is inaccessible or unknown. */
  entity: ChatContextScope | null;
  /** Same as {@link entity} when both ids match; `null` when inaccessible. */
  contextEntity: ChatContextScope | null;
  /** The requested thread, or `null` when absent, foreign or not requested. */
  thread: Thread | null;
  /** The requested thread's full history, or `null` when not requested. */
  messages: ThreadMessageLike[] | null;
  webSearch: WebSearchConfig | null;
  stale: Set<ChatContextSlice>;
}

/**
 * The current request's snapshot when it holds a fresh copy of `slice`.
 * Undefined outside a primed round (settings routes, thread listing, …) or
 * once a write has invalidated the slice — callers then use the HTTP path.
 */
export function readChatContext(slice: ChatContextSlice): ChatContextSnapshot | undefined {
  const snapshot = getChatContextSnapshot();
  if (!snapshot || snapshot.stale.has(slice)) {
    return undefined;
  }
  return snapshot;
}

/** Whether a snapshot is primed for the current request at all. */
export function hasChatContext(): boolean {
  return getChatContextSnapshot() !== undefined;
}

/**
 * Mark a slice stale after a write so the next same-request read refetches.
 * No-op outside a primed round.
 */
export function invalidateChatContext(slice: ChatContextSlice): void {
  getChatContextSnapshot()?.stale.add(slice);
}

/**
 * Resolve the scope a storage read for `entityId` must be served from.
 *
 * Mirrors `resolveAgentEntityId`: per-entity data only exists for agent
 * rooms, so a non-agent folder folds to the global scope. The C# aggregate
 * builds a folder-keyed scope for any accessible folder, which is why the
 * fold happens here rather than trusting `entity` blindly. Returns
 * `undefined` (a miss) for an id the snapshot does not describe, and for an
 * inaccessible folder — the HTTP path then produces the same 404 / 403 the
 * round would have seen before.
 */
export function chatContextScope(
  snapshot: ChatContextSnapshot,
  entityId: string | undefined,
): ChatContextScope | undefined {
  if (!entityId) {
    return snapshot.global;
  }
  let scope: ChatContextScope | null | undefined;
  if (entityId === snapshot.requested.entityId) {
    scope = snapshot.entity;
  } else if (entityId === snapshot.requested.contextEntityId) {
    scope = snapshot.contextEntity;
  }
  if (!scope) {
    return undefined;
  }
  if (scope.folder && !scope.folder.isAgent) {
    return snapshot.global;
  }
  return scope;
}

/**
 * Folder details from the snapshot, for the two folders a round is scoped
 * to. `hit: false` for any other id and for a folder the aggregate could
 * not read (inaccessible / unknown) — the Files API path decides then.
 */
export function lookupChatContextFolder(
  folderId: string,
): { hit: true; info: DocspaceFolderInfo } | { hit: false } {
  const snapshot = getChatContextSnapshot();
  if (!snapshot) {
    return { hit: false };
  }
  for (const scope of [snapshot.entity, snapshot.contextEntity]) {
    if (scope?.folder && scope.entityId === folderId) {
      return { hit: true, info: scope.folder };
    }
  }
  return { hit: false };
}

/**
 * Record (and log once per request at debug level) a storage read that
 * had to leave a primed round for the network — the signal that the
 * aggregate does not cover a code path yet.
 */
export function reportChatContextMiss(label: string): void {
  if (!hasChatContext()) {
    return;
  }
  noteChatContextMiss(label);
  logger.debug(`chat-context: miss ${label} -> HTTP`);
}
