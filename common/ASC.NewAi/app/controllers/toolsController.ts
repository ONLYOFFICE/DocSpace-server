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

import { ToolsEngine } from "@onlyoffice/ai-chat/core";
import type { McpServerConfig } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import {
  systemToolsSource,
  getSystemServerConfig,
} from "../tools/systemTools.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asString, isObject } from "../narrow.js";

const engine = new ToolsEngine({ storage, systemToolsSource });

// Resolve the config to store for an entry. Entries named after a
// configured system server are whitelist markers (see agentServerWhitelist
// in tools/systemTools.ts): they are pinned to the canonical system config
// so a marker never shadows the system group with a broken connection.
// When a caller attaches an existing portal-level server to an entity
// without sending a config (the agent dialog flow), the portal-scope
// config is copied.
async function resolveConfig(
  name: string,
  provided: McpServerConfig | undefined,
): Promise<McpServerConfig> {
  const system = getSystemServerConfig(name);
  if (system) {
    return system;
  }
  if (provided && Object.keys(provided).length > 0) {
    return provided;
  }
  const portal = await storage.mcpServers.readByName(name, undefined);
  if (portal) {
    return portal;
  }
  throw Object.assign(
    new Error(`No config provided and no portal-level server named "${name}"`),
    { status: 400, expose: true },
  );
}

// System-server entries are whitelist markers pinned to the canonical
// internal endpoint (see resolveConfig above). Redact — never drop — the
// config on the way out: system servers run server-side only (see
// tools/systemTools.ts), so the browser must not receive a config it
// would try to start itself, and the internal endpoint must not leak.
// The name still round-trips: the agent dialog pre-selects by key, and
// saving the whole map from the chat config editor re-pins the entry
// through resolveConfig.
function redactSystemServer(
  name: string,
  config: McpServerConfig,
): McpServerConfig {
  return getSystemServerConfig(name) ? {} : config;
}

export const toolsController = {
  addCustomServer: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["name", "config", "entityId"] as const);
    const name = args.name as string;
    const result = await engine.addCustomServer(
      name,
      await resolveConfig(name, args.config as McpServerConfig | undefined),
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  updateCustomServer: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["name", "config", "entityId"] as const);
    const name = args.name as string;
    const result = await engine.updateCustomServer(
      name,
      await resolveConfig(name, args.config as McpServerConfig | undefined),
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  removeCustomServer: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["name", "entityId"] as const);
    const name = typeof args.name === "string" ? args.name : asString(req.query["name"]);
    if (!name) {
      res.status(400).json({ error: "name required" });
      return;
    }
    const entityId = typeof args.entityId === "string" ? args.entityId : undefined;
    await engine.removeCustomServer(name, entityId);
    res.json({ success: true });
  }),

  getCustomServer: asyncHandler(async (req, res) => {
    const name = asString(req.query["name"]);
    if (!name) {
      res.status(400).json({ error: "name required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const config = await engine.getCustomServer(name, entityId);
    res.json(config === null ? null : redactSystemServer(name, config));
  }),

  listCustomServers: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const servers = await engine.listCustomServers(entityId);
    const redacted: Record<string, McpServerConfig> = {};
    for (const [name, config] of Object.entries(servers)) {
      redacted[name] = redactSystemServer(name, config);
    }
    res.json(redacted);
  }),

  listSystemTools: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const tools = await engine.listSystemTools(entityId);
    res.json(tools);
  }),

  replaceAllCustomServers: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["map", "entityId"] as const);
    // `map` is required. Without it the loop below yields an empty map and
    // replaceAll wipes every registered MCP server for the scope, silently
    // destroying the configuration on a malformed request (Bug 82864). Reject
    // a missing/invalid map with a 400 instead.
    if (!isObject(args.map)) {
      res.status(400).json({ error: "map is required and must be an object" });
      return;
    }
    const map = args.map as Record<string, McpServerConfig>;
    const normalized: Record<string, McpServerConfig> = {};
    for (const [name, config] of Object.entries(map)) {
      normalized[name] = await resolveConfig(name, config);
    }
    const result = await engine.replaceAllCustomServers(
      normalized,
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  setDisabled: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["serverType", "toolNames", "entityId"] as const);
    await engine.setDisabled(
      args.serverType as string,
      (args.toolNames as string[]) ?? [],
      args.entityId as string | undefined,
    );
    res.json({ success: true });
  }),

  getDisabled: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const map = await engine.getDisabled(entityId);
    res.json(map);
  }),

  isToolDisabled: asyncHandler(async (req, res) => {
    const serverType = asString(req.query["serverType"]);
    const toolName = asString(req.query["toolName"]);
    if (!serverType || !toolName) {
      res.status(400).json({ error: "serverType and toolName required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const value = await engine.isToolDisabled(serverType, toolName, entityId);
    res.json(value);
  }),

  setAllowAlways: asyncHandler(async (req, res) => {
    const args = unpackPositional(
      req.body,
      ["serverType", "toolName", "value", "entityId"] as const,
    );
    await engine.setAllowAlways(
      args.serverType as string,
      args.toolName as string,
      Boolean(args.value),
      args.entityId as string | undefined,
    );
    res.json({ success: true });
  }),

  getAllowAlways: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const tokens = await engine.getAllowAlways(entityId);
    res.json(tokens);
  }),

  isAllowAlways: asyncHandler(async (req, res) => {
    const serverType = asString(req.query["serverType"]);
    const toolName = asString(req.query["toolName"]);
    if (!serverType || !toolName) {
      res.status(400).json({ error: "serverType and toolName required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const value = await engine.isAllowAlways(serverType, toolName, entityId);
    res.json(value);
  }),
};
