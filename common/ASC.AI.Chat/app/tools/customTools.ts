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
import { AiServiceHttpError } from "../storage/httpClient.js";
import { resolveAgentEntityId } from "../storage/docspaceFilesApi.js";
import { getSystemServerConfig } from "./systemTools.js";
import { PORTAL_MCP_SERVER_NAME } from "../../config/index.js";
import { setCustomServerNames } from "../requestContext.js";
import { isObject } from "../narrow.js";
import logger from "../log.js";

/**
 * Custom MCP servers registered through the `tools/*` routes, resolved per
 * round: the agent's own map when the round runs in an agent room, the
 * portal-wide map everywhere else (Bug 84383: the two used to be merged, so
 * every agent reached every portal server). Until the registry was wired
 * here it was pure configuration — nothing connected it to the chat round,
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
  options?: { strict?: boolean },
): Promise<Record<string, McpHttpServerConfig>> {
  // A failed read is tolerated so a round still runs with whatever servers
  // could be resolved. A settings write must not: with `strict`, any failure
  // is rethrown instead of leaving the registered names out of the valid
  // set, which turned a refusal (Guest, AI disabled: Bugs 84400, 84401) or a
  // passing upstream outage into a 400 "unknown serverType". Only a 404 —
  // nothing stored for the scope — still reads as an empty map.
  const tolerate = (what: string) => (err: unknown) => {
    if (options?.strict && !(err instanceof AiServiceHttpError && err.status === 404)) {
      throw err;
    }
    logger.warn(
      `customTools: ${what} read failed: ${err instanceof Error ? err.message : String(err)}`,
    );
    return {};
  };
  // An agent reaches only the servers registered in its own map — the agent
  // dialog copies the portal entry into that map when a server is enabled
  // for the agent, so the map is the whitelist. Merging the portal map on
  // top handed every agent every portal server (Bug 84383). Any other scope
  // folds to the portal map in the storage already, so one read is enough.
  const agentId = await resolveAgentEntityId(entityId);
  const merged = agentId
    ? await storage.mcpServers.readAll(agentId).catch(tolerate(`agent server map for ${agentId}`))
    : await storage.mcpServers.readAll(undefined).catch(tolerate("portal-wide server map"));
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
