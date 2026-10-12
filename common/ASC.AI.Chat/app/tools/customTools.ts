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

import { SystemToolsSource } from "@onlyoffice/ai-chat/core";
import type { McpHttpServerConfig } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { getSystemServerConfig } from "./systemTools.js";
import { PORTAL_MCP_SERVER_NAME } from "../../config/index.js";
import { setCustomServerNames } from "../requestContext.js";
import { isObject } from "../narrow.js";
import logger from "../log.js";

/**
 * Custom MCP servers registered through the `tools/*` routes, resolved per
 * round: the portal-wide map plus the agent-scoped map when the round runs
 * in an agent room (agent entries win on a name collision). Until now the
 * registry was pure configuration — nothing connected it to the chat round,
 * so a registered server's tools never reached the model
 * (Bugs 82989 / 82990).
 *
 * Filtered out here:
 * - the portal MCP server (built-in, always wired via systemTools);
 * - whitelist markers named after a configured system server (they only
 *   toggle the system group on for an agent — see agentServerWhitelist);
 * - entries without an HTTP `url` (a browser-only transport cannot run
 *   server-side).
 *
 * As a side effect the resolved names are stored in the request context so
 * the engine's synchronous `systemServerTypes` callback can mark these
 * tools approval-required, same as the host-configured system servers.
 */
export async function resolveCustomServers(
  entityId?: string,
): Promise<Record<string, McpHttpServerConfig>> {
  const [portal, scoped] = await Promise.all([
    storage.mcpServers.readAll(undefined).catch((err: unknown) => {
      logger.warn(
        `customTools: portal-wide server map read failed: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      return {};
    }),
    entityId
      ? storage.mcpServers.readAll(entityId).catch((err: unknown) => {
          logger.warn(
            `customTools: scoped server map read failed for ${entityId}: ${
              err instanceof Error ? err.message : String(err)
            }`,
          );
          return {};
        })
      : Promise.resolve({}),
  ]);
  const merged = { ...portal, ...scoped };
  const out: Record<string, McpHttpServerConfig> = {};
  for (const [name, config] of Object.entries(merged)) {
    if (name === PORTAL_MCP_SERVER_NAME) continue;
    if (getSystemServerConfig(name)) continue;
    if (!isObject(config) || typeof config.url !== "string" || config.url.length === 0) {
      continue;
    }
    out[name] = config as McpHttpServerConfig;
  }
  setCustomServerNames(Object.keys(out));
  return out;
}

/**
 * Prime the request context with the custom server names for the round's
 * scope. Called by the send handlers before the engine runs, because the
 * engine reads `systemServerTypes` (sync) before the tools adapter fires.
 */
export async function primeCustomServers(entityId?: string): Promise<void> {
  await resolveCustomServers(entityId);
}

/**
 * ToolsAdapter over the registered custom MCP servers. Enumeration and
 * execution run server-side inside the chat round, exactly like the
 * host-configured system servers; nothing is cached (see SystemToolsSource).
 */
export const customToolsSource = new SystemToolsSource({
  servers: resolveCustomServers,
});
