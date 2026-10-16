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
import { isObject } from "../narrow.js";
import { primeSourceMeta } from "../storage/docspaceFilesApi.js";
import { generateMeta, MAX_FILES, MAX_FOLDERS, NoContentError } from "../meta/generateMeta.js";
import type { GenerateMetaInput } from "../meta/generateMeta.js";

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

function asIdList(value: unknown, name: string): string[] {
  if (value === undefined || value === null) {
    return [];
  }
  if (!Array.isArray(value)) {
    badRequest(`${name} must be an array of ids`);
  }
  return value.map((item) => {
    if (typeof item === "number" && Number.isInteger(item)) {
      return String(item);
    }
    if (typeof item === "string" && item.trim()) {
      return item.trim();
    }
    return badRequest(`${name} must contain non-empty string or integer ids`);
  });
}

function asInput(body: unknown): GenerateMetaInput {
  if (!isObject(body)) {
    badRequest("body must be an object");
  }
  const files = asIdList(body["files"], "files");
  const folders = asIdList(body["folders"], "folders");
  const prompt = body["prompt"];
  if (prompt !== undefined && prompt !== null && typeof prompt !== "string") {
    badRequest("prompt must be a string");
  }
  if (files.length === 0 && folders.length === 0) {
    badRequest("files or folders is required");
  }
  if (files.length > MAX_FILES) {
    badRequest(`files holds at most ${MAX_FILES} ids`);
  }
  if (folders.length > MAX_FOLDERS) {
    badRequest(`folders holds at most ${MAX_FOLDERS} ids`);
  }
  return { files, folders, prompt: typeof prompt === "string" ? prompt.trim() : undefined };
}

export const metaController = {
  generate: asyncHandler(async (req, res) => {
    const input = asInput(req.body);
    // Attribute the model round to the first folder the request names, the
    // way every other model-bound route attributes its round to its scope.
    await primeSourceMeta(input.folders[0]);
    try {
      res.json(await generateMeta(input));
    } catch (err) {
      if (err instanceof NoContentError) {
        res.status(err.status).json({ error: err.message, skipped: err.skipped });
        return;
      }
      throw err;
    }
  }),
};
