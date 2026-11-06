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
// agent, read by anyone signed in (the tab needs them to render), written
// by a portal administrator only. The answer wears the portal API's
// envelope (`{ response }`), which the client unwraps as it does for every
// other route.

import { asyncHandler } from "./_helpers.js";
import { isPortalAdmin } from "../storage/docspacePeopleApi.js";
import {
  type DocumentAgentSettings,
  documentAgentSettingsSchema,
  documentAgentSettingsStorage,
  tenantKey,
} from "../storage/documentAgentSettingsStorage.js";
import logger from "../log.js";

const envelope = (settings: DocumentAgentSettings) => ({
  response: settings,
  count: 1,
  status: 0,
  statusCode: 200,
});

export const documentAgentController = {
  getSettings: asyncHandler(async (_req, res) => {
    res.json(envelope(await documentAgentSettingsStorage().read(tenantKey())));
  }),

  setSettings: asyncHandler(async (req, res) => {
    if (!(await isPortalAdmin())) {
      res.status(403).json({ error: "Only a portal administrator may change these settings" });
      return;
    }
    const parsed = documentAgentSettingsSchema.safeParse(req.body);
    if (!parsed.success) {
      const issues = parsed.error.issues
        .map((issue) => `${issue.path.join(".") || "body"}: ${issue.message}`)
        .join("; ");
      res.status(400).json({ error: `Invalid settings: ${issues}` });
      return;
    }
    const key = tenantKey();
    const saved = await documentAgentSettingsStorage().write(key, parsed.data);
    logger.info(
      `documentAgentSettings: saved for ${key}: enabled=${saved.enabled} profile=${saved.profileId ?? "chat"} ` +
        `reasoning=${saved.reasoning} maxToolCalls=${saved.maxToolCalls} maxFileBytes=${saved.maxFileBytes} ` +
        `taskLog=${saved.taskLog} folder=${saved.taskLogFolderId ?? "results"}`,
    );
    res.json(envelope(saved));
  }),
};
