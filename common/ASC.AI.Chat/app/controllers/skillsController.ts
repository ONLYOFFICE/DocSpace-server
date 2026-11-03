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

import { asyncHandler } from "./_helpers.js";
import { assertAiAccess } from "./aiAccess.js";
import { getEntityId, getString, isObject } from "../narrow.js";
import { ModelCallError } from "../meta/common.js";
import {
  generateSkill,
  MAX_NAME_LENGTH,
  MAX_REQUEST_CHARS,
  NoSkillProfileError,
} from "../skills/generateSkill.js";
import type { GenerateSkillInput } from "../skills/generateSkill.js";

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

function asInput(body: unknown): GenerateSkillInput {
  if (!isObject(body)) {
    badRequest("body must be an object");
  }
  // Skills live in Instructions rooms, which are internal folders: a
  // positive integer id.
  const destId = getEntityId(body, "destId");
  if (destId === undefined || !/^[1-9]\d*$/.test(destId)) {
    badRequest("destId must be the integer id of an Instructions room");
  }
  const title = getString(body, "title")?.trim() ?? "";
  if (!title || title.length > MAX_NAME_LENGTH) {
    badRequest(`title must be a non-empty string of at most ${MAX_NAME_LENGTH} characters`);
  }
  const description = getString(body, "description")?.trim() ?? "";
  if (!description || description.length > MAX_REQUEST_CHARS) {
    badRequest(`description must be a non-empty string of at most ${MAX_REQUEST_CHARS} characters`);
  }
  return { destId, title, description };
}

export const skillsController = {
  generate: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const input = asInput(req.body);
    try {
      res.json(await generateSkill(input));
    } catch (err) {
      // The 5xx outcomes with a reason, which `asyncHandler` would flatten
      // into a bare 500 (see metaController).
      if (err instanceof ModelCallError) {
        res.status(err.status).json({
          error: err.message,
          code: err.code,
          ...(err.providerMessage !== undefined && { providerMessage: err.providerMessage }),
        });
        return;
      }
      if (err instanceof NoSkillProfileError) {
        res.status(err.status).json({ error: err.message });
        return;
      }
      throw err;
    }
  }),
};
