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
import { systemToolsSource } from "../tools/systemTools.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asString } from "../narrow.js";

const engine = new ToolsEngine({ storage, systemToolsSource });

export const toolsController = {
  addCustomServer: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["name", "config", "entityId"] as const);
    const result = await engine.addCustomServer(
      args.name as string,
      args.config as McpServerConfig,
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  updateCustomServer: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["name", "config", "entityId"] as const);
    const result = await engine.updateCustomServer(
      args.name as string,
      args.config as McpServerConfig,
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
    res.json(config);
  }),

  listCustomServers: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const servers = await engine.listCustomServers(entityId);
    res.json(servers);
  }),

  listSystemTools: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const tools = await engine.listSystemTools(entityId);
    res.json(tools);
  }),

  replaceAllCustomServers: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["map", "entityId"] as const);
    const map = (args.map as Record<string, McpServerConfig>) ?? {};
    const result = await engine.replaceAllCustomServers(
      map,
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
