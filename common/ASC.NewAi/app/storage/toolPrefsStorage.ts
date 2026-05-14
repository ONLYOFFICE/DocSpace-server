// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { isObject } from "../narrow.js";
import type { ToolPrefsStorage } from "@onlyoffice/ai-chat/core";

const BASE_PATH = "/integration/tool-prefs";
const DISABLED_PATH = `${BASE_PATH}/disabled`;
const ALLOW_ALWAYS_PATH = `${BASE_PATH}/allow-always`;

function entityIdQuery(entityId: string | undefined): Record<string, QueryValue> | undefined {
  return entityId ? { entityId } : undefined;
}

function readToolPrefsRaw(entityId: string | undefined): Promise<unknown> {
  const query = entityIdQuery(entityId);
  return aiService.get(BASE_PATH, query ? { query } : undefined);
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

export class HttpToolPrefsStorage implements ToolPrefsStorage {
  async createDisabled(
    disabled: Record<string, string[]>,
    entityId?: string,
  ): Promise<void> {
    await aiService.put(DISABLED_PATH, { disabled, entityId });
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
    await aiService.put(DISABLED_PATH, { disabled, entityId });
  }

  async upsertDisabled(
    disabled: Record<string, string[]>,
    entityId?: string,
  ): Promise<void> {
    await aiService.put(DISABLED_PATH, { disabled, entityId });
  }

  async deleteDisabled(entityId?: string): Promise<void> {
    // No DELETE endpoint on the C# side; clear by upserting an empty map.
    await aiService.put(DISABLED_PATH, { disabled: {}, entityId });
  }

  async createAllowAlways(tokens: string[], entityId?: string): Promise<void> {
    await aiService.put(ALLOW_ALWAYS_PATH, {
      allowAlways: groupAllowAlwaysTokens(tokens),
      entityId,
    });
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
    await aiService.put(ALLOW_ALWAYS_PATH, {
      allowAlways: groupAllowAlwaysTokens(tokens),
      entityId,
    });
  }

  async upsertAllowAlways(tokens: string[], entityId?: string): Promise<void> {
    await aiService.put(ALLOW_ALWAYS_PATH, {
      allowAlways: groupAllowAlwaysTokens(tokens),
      entityId,
    });
  }

  async deleteAllowAlways(entityId?: string): Promise<void> {
    await aiService.put(ALLOW_ALWAYS_PATH, { allowAlways: {}, entityId });
  }
}
