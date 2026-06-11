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

import { ProfilesEngine } from "@onlyoffice/ai-chat/core";
import type { Profile, CreateProfileInput, ProviderType } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asString } from "../narrow.js";
import { assertSafeBaseUrl } from "../security.js";

const engine = new ProfilesEngine({ storage });

interface ListProviderModelsBody {
  providerType?: ProviderType;
  baseUrl?: string;
  apiKey?: string;
}

export const profilesController = {
  create: asyncHandler<CreateProfileInput>(async (req, res) => {
    assertSafeBaseUrl(req.body?.baseUrl);
    const result = await engine.create(req.body);
    res.json(result);
  }),

  update: asyncHandler<Profile>(async (req, res) => {
    assertSafeBaseUrl(req.body?.baseUrl);
    const result = await engine.update(req.body);
    res.json(result);
  }),

  delete: asyncHandler(async (req, res) => {
    const { id } = unpackPositional(req.body, ["id"] as const);
    const idStr = typeof id === "string" ? id : asString(req.query["id"]);
    if (!idStr) {
      res.status(400).json({ error: "id required" });
      return;
    }
    await engine.delete(idStr);
    res.json({ success: true });
  }),

  listProviderModels: asyncHandler<ListProviderModelsBody>(async (req, res) => {
    const { providerType, baseUrl, apiKey } = req.body ?? {};
    if (!providerType || !baseUrl) {
      res.status(400).json({ error: "providerType and baseUrl required" });
      return;
    }
    assertSafeBaseUrl(baseUrl);
    const models = await engine.listProviderModels({
      providerType,
      baseUrl,
      apiKey: apiKey ?? "",
    });
    res.json(models);
  }),

  listModels: asyncHandler(async (req, res) => {
    const profileId = asString(req.query["profileId"]);
    if (!profileId) {
      res.status(400).json({ error: "profileId required" });
      return;
    }
    const models = await engine.listModels(profileId);
    res.json(models);
  }),

  testConnection: asyncHandler(async (req, res) => {
    const { profileId } = unpackPositional(req.body, ["profileId"] as const);
    const idStr = typeof profileId === "string" ? profileId : asString(req.query["profileId"]);
    if (!idStr) {
      res.status(400).json({ error: "profileId required" });
      return;
    }
    const result = await engine.testConnection(idStr);
    res.json(result);
  }),

  getById: asyncHandler(async (req, res) => {
    const id = asString(req.query["id"]);
    if (!id) {
      res.status(400).json({ error: "id required" });
      return;
    }
    const profile = await engine.getById(id);
    res.json(profile ?? null);
  }),

  list: asyncHandler(async (_req, res) => {
    const profiles = await engine.list();
    res.json(profiles);
  }),
};
