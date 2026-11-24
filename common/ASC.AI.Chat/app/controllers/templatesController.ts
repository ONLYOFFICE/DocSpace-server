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

// The templates shipped with the service: the catalog and its installation
// into the caller's portal. See app/templates.

import { asyncHandler, sendError } from "./_helpers.js";
import { assertAiAccess } from "./aiAccess.js";
import { getString, isObject } from "../narrow.js";
import { mayCreateAiRooms } from "../storage/docspacePeopleApi.js";
import { getTemplateCatalog } from "../templates/catalog.js";
import { installTemplate, TemplateInstallIncompleteError } from "../templates/installTemplate.js";
import { badRequest, clientFault } from "../httpErrors.js";

/** The longest id a request may carry; the shipped ids are far shorter. */
const MAX_ID_LENGTH = 200;

export const templatesController = {
  list: asyncHandler(async (_req, res) => {
    await assertAiAccess();
    res.json(await getTemplateCatalog());
  }),
  // Installing creates rooms, an agent above all, which only the portal
  // owner, DocSpace admins and room admins may do: anyone else is refused
  // before the first room is made. The rooms are then created with the
  // caller's credentials, so the Files service still decides on each one.
  install: asyncHandler(async (req, res) => {
    await assertAiAccess();
    if (!(await mayCreateAiRooms())) {
      throw clientFault(
        403,
        "Only the portal owner, portal admins and room admins may install a template",
      );
    }
    const id = isObject(req.body) ? getString(req.body, "id")?.trim() : undefined;
    if (!id || id.length > MAX_ID_LENGTH) {
      badRequest("id must be the id of a template");
    }
    const catalog = await getTemplateCatalog();
    try {
      res.json(await installTemplate(catalog, id));
    } catch (err) {
      if (!(err instanceof TemplateInstallIncompleteError)) {
        throw err;
      }
      // A step failed after rooms were made, and they stay in the portal: the answer keeps the
      // failed step's status and names them, in the fields the success answer uses, so the
      // caller can find them (ts-ai-failure-paths.md §6).
      const { skillSets, knowledgeBases, agent } = err.made;
      sendError(req, res, err.cause, {
        skillSets,
        knowledgeBases,
        ...(agent ? { agent } : {}),
        warning: "installIncomplete",
      });
    }
  }),
};
