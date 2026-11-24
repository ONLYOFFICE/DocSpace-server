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

import type { McpServerConfig, Thread } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { dtoToProfile } from "./profilesStorage.js";
import type { DocspaceProfile } from "./profilesStorage.js";
import { dtoToThread } from "./threadsStorage.js";
import { dtoToMessage } from "./messagesStorage.js";
import { parseMcpServerConfig } from "./mcpServersStorage.js";
import { parseWebSearchConfig } from "./webSearchStorage.js";
import type { DocspaceFolderInfo } from "./docspaceFilesApi.js";
import type { ChatContextScope, ChatContextSnapshot } from "./chatContextSnapshot.js";
import { depthToLevel } from "./reasoningDepth.js";
import { csharpToToolPermissionMode } from "./toolPermissionMode.js";
import {
  getChatContextMisses,
  getFilesApiReadCount,
  getForwardedHeaders,
  getUpstreamCalls,
  getUpstreamReadCount,
  setChatContextSnapshot,
} from "../requestContext.js";
import nconf, { chatContextDisabled } from "../../config/index.js";
import type { AppConfig } from "../types.js";
import { isObject, getBoolean, getEntityId, getNumber, getObject, getString } from "../narrow.js";
import logger from "../log.js";

const PATH = "/chat-context";

const CHAT_CONTEXT_DISABLED = chatContextDisabled();
const OPT_OUT_HEADER = "x-ai-chat-context";
const app: AppConfig | undefined = nconf.get("app");
const IS_PRODUCTION = (app?.environment ?? "").toLowerCase() === "production";

// Per-request opt-out (`x-ai-chat-context: off`) for side-by-side
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
function parseFolder(raw: unknown): DocspaceFolderInfo | undefined {
  if (!isObject(raw) || getEntityId(raw, "id") === undefined) {
    return undefined;
  }
  const info: DocspaceFolderInfo = {
    title: getString(raw, "title"),
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

// The aggregate's required fields (ChatContextDto.cs) and its non-null
// optional ones are read whole or not at all: a field of the wrong shape
// makes the whole aggregate unreadable, and `primeChatContext` then installs
// no snapshot, so the round reads each entity on its own. A snapshot of
// empties would answer "no profiles", "no assignment" or "no thread" for what
// is only unknown (`ts-ai-failure-paths.md` §1). One malformed element of a
// list or a map is dropped, the rest kept.

function parseAssignments(raw: unknown): Record<string, string> | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const result: Record<string, string> = {};
  for (const [key, value] of Object.entries(raw)) {
    if (typeof value === "string") {
      result[key] = value;
    }
  }
  return result;
}

// `List<McpServerDto>` (`{ name, config }`, config as a JSON string) -> the
// name -> config map `HttpMcpServersStorage.readAll` returns.
function parseMcpServers(raw: unknown): Record<string, McpServerConfig> | undefined {
  if (!Array.isArray(raw)) {
    return undefined;
  }
  const result: Record<string, McpServerConfig> = {};
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

/** A scope of the aggregate; `undefined` when it cannot be read as `ChatContextScopeDto`. */
function parseScope(raw: unknown, entityId: string | undefined): ChatContextScope | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const assignments = parseAssignments(raw["assignments"]);
  const toolPrefs = getObject(raw, "toolPrefs");
  const mcpServers = parseMcpServers(raw["mcpServers"]);
  if (assignments === undefined || toolPrefs === undefined || mcpServers === undefined) {
    return undefined;
  }
  const preferences = getObject(raw, "preferences");
  return {
    entityId,
    folder: parseFolder(raw["folder"]),
    assignments,
    reasoningLevel: preferences ? depthToLevel(preferences["depth"]) : null,
    toolPrefs,
    mcpServers,
  };
}

/** An optional scope: `null` when the aggregate carries none, `undefined` when it is unreadable. */
function parseOptionalScope(
  raw: unknown,
  entityId: string | undefined,
): ChatContextScope | null | undefined {
  return raw === null || raw === undefined ? null : parseScope(raw, entityId);
}

function parseProfiles(raw: unknown): DocspaceProfile[] | undefined {
  if (!Array.isArray(raw)) {
    return undefined;
  }
  const result: DocspaceProfile[] = [];
  for (const item of raw) {
    const profile = dtoToProfile(item);
    if (profile) {
      result.push(profile);
    }
  }
  return result;
}

// Messages that cannot be read leave the slice `null`, which the messages
// storage reads as "not in the aggregate" and asks for on its own.
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

/** A nullable field read by `parse`: `null` when absent, `undefined` when present and unreadable. */
function parseNullable<T>(raw: unknown, parse: (value: unknown) => T | null): T | null | undefined {
  if (raw === null || raw === undefined) {
    return null;
  }
  return parse(raw) ?? undefined;
}

export function parseChatContext(
  raw: unknown,
  request: ChatContextRequest,
): ChatContextSnapshot | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const global = parseScope(raw["global"], undefined);
  const profiles = parseProfiles(raw["profiles"]);
  const entity = parseOptionalScope(raw["entity"], request.entityId);
  // The C# side skips the second scope when both ids match; a read for
  // `contextEntityId` must then land on the same scope as `entityId`.
  const contextEntity =
    request.contextEntityId !== undefined && request.contextEntityId === request.entityId
      ? entity
      : parseOptionalScope(raw["contextEntity"], request.contextEntityId);
  const thread = parseNullable(raw["thread"], dtoToThread);
  const webSearch = parseNullable(raw["webSearch"], parseWebSearchConfig);
  if (
    global === undefined ||
    profiles === undefined ||
    entity === undefined ||
    contextEntity === undefined ||
    thread === undefined ||
    webSearch === undefined
  ) {
    return undefined;
  }
  const config = getObject(raw, "config");
  return {
    requested: {
      threadId: request.threadId,
      entityId: request.entityId,
      contextEntityId: request.contextEntityId,
    },
    aiReady: config ? getBoolean(config, "aiReady") : undefined,
    toolPermissionMode: config ? csharpToToolPermissionMode(config["toolPermissionMode"]) : null,
    profiles,
    global,
    entity,
    contextEntity,
    thread,
    messages: parseMessages(raw["messages"]),
    webSearch,
    documentAgentEnabled: getBoolean(raw, "documentAgentEnabled"),
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
    // The message of an upstream error holds the internal address and the upstream body.
    logger.warn(
      `chat-context: aggregate read failed, falling back to per-entity reads: ${
        err instanceof AiServiceHttpError
          ? `AI service ${err.status}`
          : err instanceof Error
            ? err.message
            : String(err)
      }`,
    );
    return;
  }
  const snapshot = parseChatContext(raw, request);
  if (!snapshot) {
    // The aggregate carries the thread's messages: the log gets its shape, not its content.
    logger.warn(
      `chat-context: unusable payload, falling back to per-entity reads: ${
        isObject(raw) ? `an object with ${Object.keys(raw).sort().join(",")}` : typeof raw
      }`,
    );
    return;
  }
  setChatContextSnapshot(snapshot);
  logger.info(
    `chat-context: primed threadId=${request.threadId ?? "-"} entityId=${request.entityId ?? "-"} ` +
      `contextEntityId=${request.contextEntityId ?? "-"} profiles=${snapshot.profiles.length} ` +
      `entity=${snapshot.entity ? "folder" : "none"} ` +
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
