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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { aiService } from "../storage/httpClient.js";
import { isObject } from "../narrow.js";
import { UpstreamShapeError } from "../storage/upstreamShape.js";
import { asyncHandler } from "./_helpers.js";

// The routes relay the AI service's `{ response, ... }` envelope. An answer that is not a JSON
// object (an HTML page of a proxy in front of the service, a text body) is not that envelope:
// it is unknown, answered 502, and never relayed as a 200 (ts-ai-failure-paths.md §1).
function envelopeOf(value: unknown, call: string): Record<string, unknown> {
  if (!isObject(value)) {
    throw new UpstreamShapeError(call);
  }
  return value;
}

// Thin proxy for the .NET AI settings endpoints (SettingsController,
// `internal/ai/config/*`). The body/response are forwarded as-is; `raw: true`
// keeps the DocSpace `{ response, status, ... }` envelope so the client sees
// the same shape it did when these were served directly from .NET.
export const settingsController = {
  // GET config → `GET internal/ai/config`. Reports the combined AI
  // configuration/readiness for the portal.
  getAiSettings: asyncHandler(async (_req, res) => {
    const settings = envelopeOf(await aiService.get("/config", { raw: true }), "GET config");
    res.json(settings);
  }),

  // GET config/vectorization → `GET internal/ai/config/vectorization`.
  getVectorizationSettings: asyncHandler(async (_req, res) => {
    const settings = envelopeOf(
      await aiService.get("/config/vectorization", { raw: true }),
      "GET config/vectorization",
    );
    res.json(settings);
  }),

  // PUT config/vectorization → `PUT internal/ai/config/vectorization`.
  setVectorizationSettings: asyncHandler(async (req, res) => {
    const settings = envelopeOf(
      await aiService.put("/config/vectorization", req.body ?? {}, { raw: true }),
      "PUT config/vectorization",
    );
    res.json(settings);
  }),

  // GET config/user → `GET internal/ai/config/user`.
  getUserSettings: asyncHandler(async (_req, res) => {
    const settings = envelopeOf(
      await aiService.get("/config/user", { raw: true }),
      "GET config/user",
    );
    res.json(settings);
  }),

  // PUT config/user → `PUT internal/ai/config/user`. The AI service keeps
  // the current value for a field the body does not carry, so a body the
  // JSON parser could not read (text/plain, no content type) arrived as `{}`
  // and answered 200 having stored nothing (Bug 84430). The one field the
  // settings carry is required here.
  setUserSettings: asyncHandler(async (req, res) => {
    const body: unknown = req.body;
    if (!isObject(body) || typeof body["chatRecommendedModelVisible"] !== "boolean") {
      res.status(400).json({
        error: "chatRecommendedModelVisible is required and must be a boolean",
      });
      return;
    }
    const settings = envelopeOf(
      await aiService.put("/config/user", body, { raw: true }),
      "PUT config/user",
    );
    res.json(settings);
  }),

  // GET config/tool-mode → `GET internal/ai/config/tool-mode`. The calling
  // user's tool permission mode as the AI service spells it (`{ mode }`, the
  // C# enum). The chat itself reads it through the preferences routes.
  getToolMode: asyncHandler(async (_req, res) => {
    const settings = envelopeOf(
      await aiService.get("/config/tool-mode", { raw: true }),
      "GET config/tool-mode",
    );
    res.json(settings);
  }),

  // PUT config/tool-mode → `PUT internal/ai/config/tool-mode`.
  setToolMode: asyncHandler(async (req, res) => {
    const settings = envelopeOf(
      await aiService.put("/config/tool-mode", req.body ?? {}, { raw: true }),
      "PUT config/tool-mode",
    );
    res.json(settings);
  }),
};
