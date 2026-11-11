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

// GET/PUT document-agent/settings: the tenant settings of the document
// agent, a thin proxy for the AI service's own
// (`internal/ai/document-agent/settings`), which stores them and decides
// who may read (any member of the portal) and write (a portal
// administrator). `raw: true` keeps the portal API's envelope
// (`{ response }`), which the client unwraps as it does for every other
// route. The one thing checked here is what the AI service does not know:
// the agent's own ceilings on the limits.

import { asyncHandler } from "./_helpers.js";
import { aiService } from "../storage/httpClient.js";
import {
  DOCUMENT_AGENT_SETTINGS_PATH,
  documentAgentSettingsSchema,
} from "../storage/documentAgentSettingsStorage.js";

export const documentAgentController = {
  // GET document-agent/settings → `GET internal/ai/document-agent/settings`.
  getSettings: asyncHandler(async (_req, res) => {
    res.json(await aiService.get(DOCUMENT_AGENT_SETTINGS_PATH, { raw: true }));
  }),

  // PUT document-agent/settings → `PUT internal/ai/document-agent/settings`.
  setSettings: asyncHandler(async (req, res) => {
    const parsed = documentAgentSettingsSchema.safeParse(req.body);
    if (!parsed.success) {
      const issues = parsed.error.issues
        .map((issue) => `${issue.path.join(".") || "body"}: ${issue.message}`)
        .join("; ");
      res.status(400).json({ error: `Invalid settings: ${issues}` });
      return;
    }
    res.json(await aiService.put(DOCUMENT_AGENT_SETTINGS_PATH, parsed.data, { raw: true }));
  }),
};
