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
