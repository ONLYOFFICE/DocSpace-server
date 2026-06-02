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
