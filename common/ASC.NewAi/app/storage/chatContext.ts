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

import type { McpServerConfig, Profile, Thread } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { dtoToProfile } from "./profilesStorage.js";
import { dtoToThread } from "./threadsStorage.js";
import { dtoToMessage } from "./messagesStorage.js";
import { parseMcpServerConfig } from "./mcpServersStorage.js";
import { parseWebSearchConfig } from "./webSearchStorage.js";
import type { DocspaceFolderInfo } from "./docspaceFilesApi.js";
import type { ChatContextScope, ChatContextSnapshot } from "./chatContextSnapshot.js";
import {
  getChatContextMisses,
  getFilesApiReadCount,
  getForwardedHeaders,
  getUpstreamCalls,
  getUpstreamReadCount,
  setChatContextSnapshot,
} from "../requestContext.js";
import nconf from "../../config/index.js";
import type { AppConfig } from "../types.js";
import {
  isObject,
  getArray,
  getBoolean,
  getEntityId,
  getNumber,
  getObject,
  getString,
  type JsonObject,
} from "../narrow.js";
import logger from "../log.js";

const PATH = "/chat-context";

const CHAT_CONTEXT_DISABLED = process.env["NEW_AI_CHAT_CONTEXT"] === "off";
const OPT_OUT_HEADER = "x-newai-chat-context";
const app: AppConfig | undefined = nconf.get("app");
const IS_PRODUCTION = (app?.environment ?? "").toLowerCase() === "production";

// Per-request opt-out (`x-newai-chat-context: off`) for side-by-side
// measurement on a development stand; ignored in production.
function requestOptsOut(): boolean {
  if (IS_PRODUCTION) {
    return false;
  }
  return getForwardedHeaders()[OPT_OUT_HEADER] === "off";
}

export interface ChatContextRequest {
  threadId?: string | undefined;
  entityId?: string | undefined;
  contextEntityId?: string | undefined;
}

// `ChatContextFolderDto` -> the shape the Files API helpers already consume.
// The prompt is left raw here; `safeGetAgentInstruction` sanitizes it at the
// point of use, exactly as for a folder fetched from the Files API.
function parseFolder(raw: unknown): DocspaceFolderInfo | undefined {
  if (!isObject(raw) || getEntityId(raw, "id") === undefined) {
    return undefined;
  }
  const info: DocspaceFolderInfo = {
    isAgent: getBoolean(raw, "isAgent") ?? false,
    title: getString(raw, "title"),
    prompt: getString(raw, "prompt"),
  };
  const folderType = getNumber(raw, "folderType");
  if (folderType !== undefined) {
    info.folderType = folderType;
  }
  const canCreate = getBoolean(raw, "canCreate");
  if (canCreate !== undefined) {
    info.canCreate = canCreate;
  }
  return info;
}

function parseAssignments(raw: unknown): Record<string, string> {
  const result: Record<string, string> = {};
  if (!isObject(raw)) {
    return result;
  }
  for (const [key, value] of Object.entries(raw)) {
    if (typeof value === "string") {
      result[key] = value;
    }
  }
  return result;
}

// `List<McpServerDto>` (`{ name, config }`, config as a JSON string) -> the
// name -> config map `HttpMcpServersStorage.readAll` returns.
function parseMcpServers(raw: unknown): Record<string, McpServerConfig> {
  const result: Record<string, McpServerConfig> = {};
  if (!Array.isArray(raw)) {
    return result;
  }
  for (const item of raw) {
    if (!isObject(item)) {
      continue;
    }
    const name = getString(item, "name");
    if (name === undefined) {
      continue;
    }
    const config = parseMcpServerConfig(item["config"]);
    if (config !== null) {
      result[name] = config;
    }
  }
  return result;
}

function parseScope(raw: unknown, entityId: string | undefined): ChatContextScope | null {
  if (!isObject(raw)) {
    return null;
  }
  const preferences = getObject(raw, "preferences");
  const toolPrefs = getObject(raw, "toolPrefs");
  return {
    entityId,
    folder: parseFolder(raw["folder"]),
    assignments: parseAssignments(raw["assignments"]),
    deepMode: preferences ? (getBoolean(preferences, "deepMode") ?? null) : null,
    toolPrefs: toolPrefs ?? ({} as JsonObject),
    mcpServers: parseMcpServers(raw["mcpServers"]),
  };
}

function parseProfiles(raw: unknown): Profile[] {
  const result: Profile[] = [];
  if (!Array.isArray(raw)) {
    return result;
  }
  for (const item of raw) {
    const profile = dtoToProfile(item);
    if (profile) {
      result.push(profile);
    }
  }
  return result;
}

function parseMessages(raw: unknown): ThreadMessageLike[] | null {
  if (!Array.isArray(raw)) {
    return null;
  }
  const result: ThreadMessageLike[] = [];
  for (const item of raw) {
    const message = dtoToMessage(item);
    if (message) {
      result.push(message);
    }
  }
  return result;
}

function parseThread(raw: unknown): Thread | null {
  return raw === null || raw === undefined ? null : dtoToThread(raw);
}

export function parseChatContext(
  raw: unknown,
  request: ChatContextRequest,
): ChatContextSnapshot | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const global = parseScope(raw["global"], undefined);
  if (!global) {
    return undefined;
  }
  const config = getObject(raw, "config");
  const entity = parseScope(raw["entity"], request.entityId);
  // The C# side skips the second scope when both ids match; a read for
  // `contextEntityId` must then land on the same scope as `entityId`.
  const contextEntity =
    request.contextEntityId !== undefined && request.contextEntityId === request.entityId
      ? entity
      : parseScope(raw["contextEntity"], request.contextEntityId);
  return {
    requested: {
      threadId: request.threadId,
      entityId: request.entityId,
      contextEntityId: request.contextEntityId,
    },
    aiReady: config ? getBoolean(config, "aiReady") : undefined,
    profiles: parseProfiles(getArray(raw, "profiles")),
    global,
    entity,
    contextEntity,
    thread: parseThread(raw["thread"]),
    messages: parseMessages(raw["messages"]),
    webSearch: parseWebSearchConfig(raw["webSearch"]),
    stale: new Set(),
  };
}

/**
 * Fetch the round's aggregate (`GET internal/ai/chat-context`) once and
 * install it as the request's snapshot, so every storage read below the
 * send handler is served in-process (see `chatContextSnapshot.ts`).
 *
 * Failure handling: an authorization failure (401 / 403) is the caller's
 * own — rethrown so the round fails the way each per-entity read would
 * have. Anything else (network, 5xx, an unparseable payload) leaves the
 * request without a snapshot and the round degrades to the per-entity
 * reads, with a warning.
 */
export async function primeChatContext(request: ChatContextRequest): Promise<void> {
  // Kill switch for A/B measurement and rollback: every round then runs
  // on the per-entity reads exactly as before the aggregate existed.
  if (CHAT_CONTEXT_DISABLED || requestOptsOut()) {
    return;
  }
  const query: Record<string, QueryValue> = {
    threadId: request.threadId,
    entityId: request.entityId,
    contextEntityId: request.contextEntityId,
    includeMessages: request.threadId !== undefined,
  };
  let raw: unknown;
  try {
    raw = await aiService.get(PATH, { query });
  } catch (err) {
    if (err instanceof AiServiceHttpError && (err.status === 401 || err.status === 403)) {
      throw err;
    }
    logger.warn(
      `chat-context: aggregate read failed, falling back to per-entity reads: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    return;
  }
  const snapshot = parseChatContext(raw, request);
  if (!snapshot) {
    logger.warn(
      `chat-context: unusable payload, falling back to per-entity reads: ${JSON.stringify(
        raw,
      ).slice(0, 500)}`,
    );
    return;
  }
  setChatContextSnapshot(snapshot);
  logger.info(
    `chat-context: primed threadId=${request.threadId ?? "-"} entityId=${request.entityId ?? "-"} ` +
      `contextEntityId=${request.contextEntityId ?? "-"} profiles=${snapshot.profiles.length} ` +
      `entity=${snapshot.entity ? (snapshot.entity.folder?.isAgent ? "agent" : "folder") : "none"} ` +
      `thread=${snapshot.thread ? "yes" : "no"} messages=${snapshot.messages?.length ?? "-"} ` +
      `webSearch=${snapshot.webSearch ? "yes" : "no"}`,
  );
}

/**
 * One line for the end of a round: how many GETs still reached the AI
 * service (the aggregate itself counts as one) and which reads bypassed
 * the snapshot. The number to watch is `upstreamGets=1` with no misses.
 */
export function describeChatContextUsage(): string {
  const misses = getChatContextMisses();
  const calls = Object.entries(getUpstreamCalls())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([method, n]) => `${method}:${n}`)
    .join(",");
  return (
    `upstreamGets=${getUpstreamReadCount()} upstreamCalls={${calls}} ` +
    `filesApiReads=${getFilesApiReadCount()}` +
    (misses.length > 0 ? ` snapshotMisses=[${misses.join(", ")}]` : "")
  );
}
