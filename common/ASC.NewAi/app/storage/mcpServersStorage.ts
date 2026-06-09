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
import { isObject, getString } from "../narrow.js";
import type { McpServersStorage, McpServerConfig } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/mcp-servers";

function entityIdQuery(entityId: string | undefined): Record<string, QueryValue> | undefined {
  return entityId ? { entityId } : undefined;
}

function parseConfig(raw: unknown): McpServerConfig | null {
  if (raw === null || raw === undefined) {
    return null;
  }
  if (typeof raw === "string") {
    try {
      const parsed: unknown = JSON.parse(raw);
      return isObject(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }
  return isObject(raw) ? raw : null;
}

export class HttpMcpServersStorage implements McpServersStorage {
  async create(name: string, config: McpServerConfig, entityId?: string): Promise<void> {
    await aiService.post(PATH, {
      name,
      config: JSON.stringify(config),
      entityId: entityId ?? null,
    });
  }

  async readByName(name: string, entityId?: string): Promise<McpServerConfig | null> {
    try {
      const query = entityIdQuery(entityId);
      const raw = await aiService.get(
        `${PATH}/${encodeURIComponent(name)}`,
        query ? { query } : undefined,
      );
      if (!isObject(raw)) {
        return null;
      }
      const cfg = getString(raw, "config");
      return parseConfig(cfg);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  // The C# `McpServerStorageController` returns `IReadOnlyList<McpServerDto>`
  // (each `{ name, config }`), so we have to assemble the name → config map
  // client-side rather than reading an object back.
  async readAll(entityId?: string): Promise<Record<string, McpServerConfig>> {
    const query = entityIdQuery(entityId);
    const raw = await aiService.get(PATH, query ? { query } : undefined);
    if (!Array.isArray(raw)) {
      return {};
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
      const cfg = parseConfig(getString(item, "config"));
      if (cfg !== null) {
        result[name] = cfg;
      }
    }
    return result;
  }

  async update(name: string, config: McpServerConfig, entityId?: string): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(name)}`, {
      config: JSON.stringify(config),
      entityId: entityId ?? null,
    });
  }

  async replaceAll(
    servers: Record<string, McpServerConfig>,
    entityId?: string,
  ): Promise<void> {
    const payload: Record<string, string> = {};
    for (const [n, c] of Object.entries(servers)) {
      payload[n] = JSON.stringify(c);
    }
    await aiService.put(PATH, { servers: payload, entityId: entityId ?? null });
  }

  async delete(name: string, entityId?: string): Promise<void> {
    try {
      const query = entityIdQuery(entityId);
      await aiService.delete(
        `${PATH}/${encodeURIComponent(name)}`,
        query ? { query } : undefined,
      );
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
