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

import { proxyBaseUrl, withTimeout } from "./httpClient.js";
import { getForwardedHeaders } from "../requestContext.js";
import { isObject, getNumber, getObject, getString } from "../narrow.js";
import logger from "../log.js";
import { sanitizeInstruction } from "../sanitizeInstruction.js";

// Derived from the DocSpace `FolderDto<int>` (see
// products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs) returned by
// `GET /api/2.0/files/folder/{folderId}` (FoldersController.GetFolderInfo).
export interface DocspaceFolderInfo {
  /** Whether the folder is an AI agent room (`FolderType.AiRoom` on the C# side). */
  isAgent: boolean;
  /**
   * The agent room's stored instruction (`chatSettings.prompt`), when set.
   * Only agent rooms carry one — `undefined` for a regular folder.
   */
  prompt?: string;
}

// Mirrors `FolderType.IsAgent()` in
// products/ASC.Files/Core/Helpers/DocSpaceHelper.cs: an agent is a folder of
// type `FolderType.AiRoom`. The DTO also carries it as `RoomType.AiRoom`.
const FOLDER_TYPE_AI_ROOM = 31;
const ROOM_TYPE_AI_ROOM = 9;

export class DocspaceApiHttpError extends Error {
  public readonly status: number;
  public readonly url: string;

  constructor(status: number, statusText: string, url: string) {
    super(`DocSpace API ${status} ${statusText} for ${url}`);
    this.status = status;
    this.url = url;
  }
}

function parseFolderInfo(raw: unknown): DocspaceFolderInfo | undefined {
  // DocSpace API responses are wrapped in a `{ response: ... }` envelope.
  const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
  if (!envelope) {
    return undefined;
  }
  if (getNumber(envelope, "id") === undefined) {
    return undefined;
  }
  const folderType = getNumber(envelope, "type");
  const roomType = getNumber(envelope, "roomType");
  const chatSettings = getObject(envelope, "chatSettings");
  const prompt = chatSettings ? getString(chatSettings, "prompt") : undefined;
  return {
    isAgent: folderType === FOLDER_TYPE_AI_ROOM || roomType === ROOM_TYPE_AI_ROOM,
    prompt,
  };
}

/**
 * Fetch folder details from the DocSpace Files API
 * (`GET /api/2.0/files/folder/{folderId}`) on behalf of the current user —
 * the caller's credentials are relayed via {@link getForwardedHeaders}.
 * Resolves to `undefined` when the folder does not exist (404) or the
 * response cannot be parsed; propagates other HTTP failures as
 * {@link DocspaceApiHttpError}.
 */
export async function getFolderInfo(
  folderId: string,
): Promise<DocspaceFolderInfo | undefined> {
  const url = `${proxyBaseUrl}/api/2.0/files/folder/${encodeURIComponent(folderId)}`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      return undefined;
    }
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const parsed = parseFolderInfo(await res.json());
    if (!parsed) {
      logger.warn(`getFolderInfo(${folderId}) -> unparseable response from ${url}`);
    }
    return parsed;
  } finally {
    cancel();
  }
}

/**
 * Best-effort fetch of an agent room's stored instruction
 * (`chatSettings.prompt`). Never throws — a failed fetch, an absent scope,
 * or a non-agent folder simply yields an empty string, leaving the system
 * prompt unchanged (mirrors {@link safeGetToolsPrompt}).
 */
export async function safeGetAgentInstruction(
  entityId: string | undefined,
): Promise<string> {
  if (!entityId) {
    return "";
  }
  try {
    const info = await getFolderInfo(entityId);
    // Untrusted: strip markup before the instruction reaches the model prompt
    // so stored HTML can't round-trip into another user's reply (Bug 82726).
    return sanitizeInstruction(info?.prompt ?? "");
  } catch (err) {
    logger.warn(
      `agent instruction fetch failed: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    return "";
  }
}

/**
 * Resolve the Result Storage folder of an agent room. Agent rooms
 * (`FolderType.AiRoom`) own a system subfolder (`FolderType.ResultStorage`)
 * for generated artifacts; `GET /api/2.0/files/{agentId}?searchArea=ResultStorage`
 * swaps the returned `current` folder to that subfolder (see
 * `FileStorageService.GetFolderItemsAsync`). Resolves to the subfolder id,
 * or `undefined` when the agent is inaccessible (404) or has no Result
 * Storage; other HTTP failures propagate as {@link DocspaceApiHttpError}.
 */
export async function getAgentResultStorageId(
  agentId: string,
): Promise<string | undefined> {
  // `count` is [Range(1, …)]-validated on the C# side — 0 is a 400. We only
  // need `current.id`, so ask for the smallest allowed page.
  const url =
    `${proxyBaseUrl}/api/2.0/files/${encodeURIComponent(agentId)}`
    + "?searchArea=ResultStorage&count=1";
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      return undefined;
    }
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const raw: unknown = await res.json();
    const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
    const current = envelope ? getObject(envelope, "current") : undefined;
    const id = current ? getNumber(current, "id") : undefined;
    if (id === undefined) {
      logger.warn(`getAgentResultStorageId(${agentId}) -> no current.id in response from ${url}`);
      return undefined;
    }
    return String(id);
  } finally {
    cancel();
  }
}

/**
 * Gate a client-supplied `entityId` on the Files API: entity-scoped AI data
 * (assignments, preferences, threads, MCP servers, tool prefs) only exists
 * for agent rooms, so anything else — a non-agent folder, a folder the
 * current user cannot see (404), an unparseable response — resolves to
 * `undefined`, falling back to the global scope.
 */
export async function resolveAgentEntityId(
  entityId: string | undefined,
): Promise<string | undefined> {
  if (!entityId) {
    return undefined;
  }
  const folderInfo = await getFolderInfo(entityId);
  return folderInfo?.isAgent ? entityId : undefined;
}
