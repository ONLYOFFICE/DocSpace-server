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

// The files of one conversation, for the host's Content tab: what the user
// attached to it. What its replies produced is listed by the Files service
// (see app/storage/threadFiles.ts).

import { asyncHandler } from "./_helpers.js";
import { assertAiAccess } from "./aiAccess.js";
import { asString } from "../narrow.js";
import { storage } from "../storage/index.js";
import { listThreadAttachments } from "../storage/threadFiles.js";
import { extractAttachmentRefIds } from "../tools/httpToolsAdapter.js";

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const threadFilesController = {
  // The messages are read with the caller's credentials: a conversation
  // that is not the caller's answers the AI service's 404.
  attachments: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const threadId = asString(req.query["threadId"]);
    if (threadId === undefined || !GUID.test(threadId)) {
      throw Object.assign(new Error("threadId must be the id of a thread"), {
        status: 400,
        expose: true,
      });
    }
    res.json(
      await listThreadAttachments(threadId, {
        extractRefs: extractAttachmentRefIds,
        messages: storage.messages,
        attachments: storage.attachments,
      }),
    );
  }),
};
