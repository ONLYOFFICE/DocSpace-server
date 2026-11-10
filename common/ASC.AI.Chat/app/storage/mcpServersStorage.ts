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

import { aiService, AiServiceHttpError } from "./httpClient.js";
import { isObject, getString } from "../narrow.js";
import type { McpServersStorage, McpServerConfig } from "@onlyoffice/ai-chat/core";
import {
  invalidateChatContext,
  readChatContext,
  reportChatContextMiss,
} from "./chatContextSnapshot.js";

// The custom MCP server registry is one map per portal. The `entityId` the
// library passes with every call (the folder the chat is opened from) is
// accepted for the interface's sake and ignored: a server registered from
// any room lands in the map every listing reads back (Bug 82863). The C#
// storage keeps that map under `entityId: null`.
const PATH = "/mcp-servers";

export function parseMcpServerConfig(raw: unknown): McpServerConfig | null {
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
  async create(name: string, config: McpServerConfig, _entityId?: string): Promise<void> {
    await aiService.post(PATH, {
      name,
      config: JSON.stringify(config),
      entityId: null,
    });
    invalidateChatContext("mcpServers");
  }

  async readByName(name: string, _entityId?: string): Promise<McpServerConfig | null> {
    const snapshot = readChatContext("mcpServers");
    if (snapshot) {
      return snapshot.global.mcpServers[name] ?? null;
    }
    reportChatContextMiss(`mcpServers.readByName(${name})`);
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(name)}`);
      if (!isObject(raw)) {
        return null;
      }
      const cfg = getString(raw, "config");
      return parseMcpServerConfig(cfg);
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
  async readAll(_entityId?: string): Promise<Record<string, McpServerConfig>> {
    const snapshot = readChatContext("mcpServers");
    if (snapshot) {
      return { ...snapshot.global.mcpServers };
    }
    reportChatContextMiss("mcpServers.readAll");
    const raw = await aiService.get(PATH);
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
      const cfg = parseMcpServerConfig(getString(item, "config"));
      if (cfg !== null) {
        result[name] = cfg;
      }
    }
    return result;
  }

  async update(name: string, config: McpServerConfig, _entityId?: string): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(name)}`, {
      config: JSON.stringify(config),
      entityId: null,
    });
    invalidateChatContext("mcpServers");
  }

  async replaceAll(
    servers: Record<string, McpServerConfig>,
    _entityId?: string,
  ): Promise<void> {
    const payload: Record<string, string> = {};
    for (const [n, c] of Object.entries(servers)) {
      payload[n] = JSON.stringify(c);
    }
    await aiService.put(PATH, { servers: payload, entityId: null });
    invalidateChatContext("mcpServers");
  }

  async delete(name: string, _entityId?: string): Promise<void> {
    invalidateChatContext("mcpServers");
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(name)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
