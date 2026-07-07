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
import logger from "../log.js";

const engine = new ProfilesEngine({ storage });

// Translate a provider-side failure (raised by the OpenAI-compatible SDK
// while listing models) into a meaningful HTTP status + message instead of
// letting `asyncHandler` collapse everything to a generic 500. The base URL
// and key come from the user's profile form, so most failures are config
// errors that should read as such in the UI.
function describeProviderError(err: unknown): { status: number; message: string } {
  // OpenAI-SDK HTTP errors expose a numeric `status`.
  const httpStatus =
    typeof (err as { status?: unknown })?.status === "number"
      ? (err as { status: number }).status
      : undefined;
  if (httpStatus === 401 || httpStatus === 403) {
    return { status: 400, message: "Invalid API key for the AI provider" };
  }
  if (httpStatus === 404) {
    return {
      status: 400,
      message:
        "Invalid base URL — expected an OpenAI-compatible endpoint (e.g. ending in /v1)",
    };
  }
  if (typeof httpStatus === "number" && httpStatus >= 500) {
    return { status: 502, message: "The AI provider returned an error" };
  }
  if (typeof httpStatus === "number" && httpStatus >= 400) {
    return { status: 400, message: `The AI provider rejected the request (${httpStatus})` };
  }

  // Network-level failures (no HTTP status): walk the cause chain for a
  // known DNS/connection code.
  const codes = new Set([
    "ENOTFOUND",
    "ECONNREFUSED",
    "EAI_AGAIN",
    "ECONNRESET",
    "ETIMEDOUT",
    "UND_ERR_CONNECT_TIMEOUT",
  ]);
  let cur: unknown = err;
  for (let i = 0; i < 5 && cur; i++) {
    const code = (cur as { code?: unknown }).code;
    if (typeof code === "string" && codes.has(code)) {
      return {
        status: 502,
        message:
          "The AI provider is unreachable — check the base URL and that the service is running",
      };
    }
    cur = (cur as { cause?: unknown }).cause;
  }

  const msg = err instanceof Error ? err.message : String(err);
  if (/connection error|fetch failed|timeout/i.test(msg)) {
    return {
      status: 502,
      message:
        "The AI provider is unreachable — check the base URL and that the service is running",
    };
  }
  return { status: 502, message: "Failed to list provider models" };
}

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
    try {
      const models = await engine.listProviderModels({
        providerType,
        baseUrl,
        apiKey: apiKey ?? "",
      });
      res.json(models);
    } catch (err) {
      const { status, message } = describeProviderError(err);
      logger.warn(
        `listProviderModels failed (${providerType} @ ${baseUrl}) -> ${status}: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      res.status(status).json({ error: message });
    }
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
