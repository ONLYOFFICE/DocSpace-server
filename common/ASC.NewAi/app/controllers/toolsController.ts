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
import { asyncHandler } from "./_helpers.js";
import { asString } from "../narrow.js";

const engine = new ToolsEngine({ storage });

interface CustomServerBody {
  name: string;
  config: McpServerConfig;
}

interface NameBody {
  name?: string;
}

interface DisabledBody {
  serverType: string;
  toolNames?: string[];
}

interface AllowAlwaysBody {
  serverType: string;
  toolName: string;
  value?: unknown;
}

export const toolsController = {
  addCustomServer: asyncHandler<CustomServerBody>(async (req, res) => {
    const { name, config } = req.body;
    const result = await engine.addCustomServer(name, config);
    res.json(result);
  }),

  updateCustomServer: asyncHandler<CustomServerBody>(async (req, res) => {
    const { name, config } = req.body;
    const result = await engine.updateCustomServer(name, config);
    res.json(result);
  }),

  removeCustomServer: asyncHandler<NameBody>(async (req, res) => {
    const name = req.body?.name ?? asString(req.query["name"]);
    if (!name) {
      res.status(400).json({ error: "name required" });
      return;
    }
    await engine.removeCustomServer(name);
    res.json({ success: true });
  }),

  getCustomServer: asyncHandler(async (req, res) => {
    const name = asString(req.query["name"]);
    if (!name) {
      res.json(null);
      return;
    }
    const config = await engine.getCustomServer(name);
    res.json(config);
  }),

  listCustomServers: asyncHandler(async (_req, res) => {
    const servers = await engine.listCustomServers();
    res.json(servers);
  }),

  replaceAllCustomServers: asyncHandler<Record<string, McpServerConfig>>(async (req, res) => {
    const result = await engine.replaceAllCustomServers(req.body ?? {});
    res.json(result);
  }),

  setDisabled: asyncHandler<DisabledBody>(async (req, res) => {
    const { serverType, toolNames } = req.body;
    await engine.setDisabled(serverType, toolNames ?? []);
    res.json({ success: true });
  }),

  getDisabled: asyncHandler(async (_req, res) => {
    const map = await engine.getDisabled();
    res.json(map);
  }),

  isToolDisabled: asyncHandler(async (req, res) => {
    const serverType = asString(req.query["serverType"]);
    const toolName = asString(req.query["toolName"]);
    if (!serverType || !toolName) {
      res.json({ value: false });
      return;
    }
    const value = await engine.isToolDisabled(serverType, toolName);
    res.json({ value });
  }),

  setAllowAlways: asyncHandler<AllowAlwaysBody>(async (req, res) => {
    const { serverType, toolName, value } = req.body;
    await engine.setAllowAlways(serverType, toolName, Boolean(value));
    res.json({ success: true });
  }),

  getAllowAlways: asyncHandler(async (_req, res) => {
    const tokens = await engine.getAllowAlways();
    res.json(tokens);
  }),

  isAllowAlways: asyncHandler(async (req, res) => {
    const serverType = asString(req.query["serverType"]);
    const toolName = asString(req.query["toolName"]);
    if (!serverType || !toolName) {
      res.json({ value: false });
      return;
    }
    const value = await engine.isAllowAlways(serverType, toolName);
    res.json({ value });
  }),
};
