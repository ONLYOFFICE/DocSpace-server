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

import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { resolveAgentEntityId } from "./docspaceFilesApi.js";
import { isObject } from "../narrow.js";
import type { ToolPrefsStorage } from "@onlyoffice/ai-chat/core";

const BASE_PATH = "/tool-prefs";
const DISABLED_PATH = `${BASE_PATH}/disabled`;
const ALLOW_ALWAYS_PATH = `${BASE_PATH}/allow-always`;

function entityIdQuery(entityId: string | undefined): Record<string, QueryValue> | undefined {
  return entityId ? { entityId } : undefined;
}

// `scopedEntityId` must already be gated via `resolveAgentEntityId`.
async function fetchToolPrefs(scopedEntityId: string | undefined): Promise<unknown> {
  const query = entityIdQuery(scopedEntityId);
  return aiService.get(BASE_PATH, query ? { query } : undefined);
}

async function readToolPrefsRaw(entityId: string | undefined): Promise<unknown> {
  return fetchToolPrefs(await resolveAgentEntityId(entityId));
}

// Same fetch, but a missing prefs row (404) reads as "nothing stored".
async function fetchToolPrefsSafe(scopedEntityId: string | undefined): Promise<unknown> {
  try {
    return await fetchToolPrefs(scopedEntityId);
  } catch (err) {
    if (err instanceof AiServiceHttpError && err.status === 404) {
      return {};
    }
    throw err;
  }
}

function pickStringArray(pref: unknown, key: string): string[] {
  if (!isObject(pref)) {
    return [];
  }
  const value = pref[key];
  if (!Array.isArray(value)) {
    return [];
  }
  const result: string[] = [];
  for (const item of value) {
    if (typeof item === "string") {
      result.push(item);
    }
  }
  return result;
}

function parseDisabled(raw: unknown): Record<string, string[]> {
  if (!isObject(raw)) {
    return {};
  }
  const result: Record<string, string[]> = {};
  for (const [serverType, pref] of Object.entries(raw)) {
    const disabled = pickStringArray(pref, "disabled");
    if (disabled.length > 0) {
      result[serverType] = disabled;
    }
  }
  return result;
}

function parseAllowAlwaysTokens(raw: unknown): string[] {
  if (!isObject(raw)) {
    return [];
  }
  const tokens: string[] = [];
  for (const [serverType, pref] of Object.entries(raw)) {
    for (const toolName of pickStringArray(pref, "allowAlways")) {
      tokens.push(`${serverType}_${toolName}`);
    }
  }
  return tokens;
}

function parseAllowAlwaysGrouped(raw: unknown): Record<string, string[]> {
  if (!isObject(raw)) {
    return {};
  }
  const result: Record<string, string[]> = {};
  for (const [serverType, pref] of Object.entries(raw)) {
    const allowAlways = pickStringArray(pref, "allowAlways");
    if (allowAlways.length > 0) {
      result[serverType] = allowAlways;
    }
  }
  return result;
}

// Engine composes tokens as `${serverType}_${toolName}`. Split on the first
// underscore — tool names may contain `_`, server types are not expected to.
function groupAllowAlwaysTokens(tokens: string[]): Record<string, string[]> {
  const grouped: Record<string, string[]> = {};
  for (const token of tokens) {
    const idx = token.indexOf("_");
    if (idx <= 0 || idx === token.length - 1) {
      continue;
    }
    const serverType = token.slice(0, idx);
    const toolName = token.slice(idx + 1);
    (grouped[serverType] ??= []).push(toolName);
  }
  return grouped;
}

// The engine expresses "cleared" by dropping a server type from the map
// (`ToolsEngine.setDisabled` deletes the key when every tool is enabled),
// but the C# upsert merges by the keys present in the payload — an absent
// key leaves the stored row untouched, and an empty payload is a no-op. To
// keep the library's replace-snapshot semantics, re-add every stored server
// type that vanished from the payload with an explicit empty list so the
// upsert overwrites it.
function withClearedKeys(
  payload: Record<string, string[]>,
  stored: Record<string, string[]>,
): Record<string, string[]> {
  const next: Record<string, string[]> = { ...payload };
  for (const serverType of Object.keys(stored)) {
    next[serverType] ??= [];
  }
  return next;
}

async function putDisabled(
  disabled: Record<string, string[]>,
  entityId: string | undefined,
): Promise<void> {
  const scopedEntityId = await resolveAgentEntityId(entityId);
  const stored = parseDisabled(await fetchToolPrefsSafe(scopedEntityId));
  await aiService.put(DISABLED_PATH, {
    disabled: withClearedKeys(disabled, stored),
    entityId: scopedEntityId,
  });
}

async function putAllowAlways(
  allowAlways: Record<string, string[]>,
  entityId: string | undefined,
): Promise<void> {
  const scopedEntityId = await resolveAgentEntityId(entityId);
  const stored = parseAllowAlwaysGrouped(await fetchToolPrefsSafe(scopedEntityId));
  await aiService.put(ALLOW_ALWAYS_PATH, {
    allowAlways: withClearedKeys(allowAlways, stored),
    entityId: scopedEntityId,
  });
}

export class HttpToolPrefsStorage implements ToolPrefsStorage {
  async createDisabled(
    disabled: Record<string, string[]>,
    entityId?: string,
  ): Promise<void> {
    await putDisabled(disabled, entityId);
  }

  async readDisabled(entityId?: string): Promise<Record<string, string[]>> {
    try {
      const raw = await readToolPrefsRaw(entityId);
      return parseDisabled(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return {};
      }
      throw err;
    }
  }

  async updateDisabled(
    disabled: Record<string, string[]>,
    entityId?: string,
  ): Promise<void> {
    await putDisabled(disabled, entityId);
  }

  async upsertDisabled(
    disabled: Record<string, string[]>,
    entityId?: string,
  ): Promise<void> {
    await putDisabled(disabled, entityId);
  }

  async deleteDisabled(entityId?: string): Promise<void> {
    // No DELETE endpoint on the C# side; clear by upserting an empty map
    // (`putDisabled` re-adds every stored key with an empty list).
    await putDisabled({}, entityId);
  }

  async createAllowAlways(tokens: string[], entityId?: string): Promise<void> {
    await putAllowAlways(groupAllowAlwaysTokens(tokens), entityId);
  }

  async readAllowAlways(entityId?: string): Promise<string[]> {
    try {
      const raw = await readToolPrefsRaw(entityId);
      return parseAllowAlwaysTokens(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return [];
      }
      throw err;
    }
  }

  async updateAllowAlways(tokens: string[], entityId?: string): Promise<void> {
    await putAllowAlways(groupAllowAlwaysTokens(tokens), entityId);
  }

  async upsertAllowAlways(tokens: string[], entityId?: string): Promise<void> {
    await putAllowAlways(groupAllowAlwaysTokens(tokens), entityId);
  }

  async deleteAllowAlways(entityId?: string): Promise<void> {
    await putAllowAlways({}, entityId);
  }
}
