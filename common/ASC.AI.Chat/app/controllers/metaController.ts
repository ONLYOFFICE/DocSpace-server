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
import { isObject } from "../narrow.js";
import { primeSourceMeta } from "../storage/docspaceFilesApi.js";
import {
  generateMeta,
  MAX_FILES,
  MAX_FOLDERS,
  ModelCallError,
  NoContentError,
  NoProfileError,
} from "../meta/generateMeta.js";
import type { GenerateMetaInput } from "../meta/generateMeta.js";
import { fillMeta } from "../meta/fillMeta.js";
import type { FillMetaInput } from "../meta/fillMeta.js";
import type { EntryKind } from "../meta/portalMetadata.js";

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
  const ids = value.map((item) => {
    if (typeof item === "number" && Number.isInteger(item)) {
      return String(item);
    }
    if (typeof item === "string" && item.trim()) {
      return item.trim();
    }
    return badRequest(`${name} must contain non-empty string or integer ids`);
  });
  return [...new Set(ids)];
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

function asFillInput(body: unknown): FillMetaInput {
  if (!isObject(body)) {
    badRequest("body must be an object");
  }
  const kind = body["kind"];
  if (kind !== "file" && kind !== "folder") {
    badRequest('kind must be "file" or "folder"');
  }
  const rawId = body["id"];
  const id =
    typeof rawId === "number" && Number.isInteger(rawId)
      ? String(rawId)
      : typeof rawId === "string"
        ? rawId.trim()
        : "";
  // Metadata lives on internal entries only (positive integer ids); a
  // third-party id has no metadata to fill and the Files API would answer
  // 404 anyway.
  if (!/^[1-9]\d*$/.test(id)) {
    badRequest("id must be the integer id of a file or folder");
  }
  const prompt = body["prompt"];
  if (prompt !== undefined && prompt !== null && typeof prompt !== "string") {
    badRequest("prompt must be a string");
  }
  const overwrite = body["overwrite"];
  if (overwrite !== undefined && overwrite !== null && typeof overwrite !== "boolean") {
    badRequest("overwrite must be a boolean");
  }
  const dryRun = body["dryRun"];
  if (dryRun !== undefined && dryRun !== null && typeof dryRun !== "boolean") {
    badRequest("dryRun must be a boolean");
  }
  return {
    kind: kind as EntryKind,
    id,
    prompt: typeof prompt === "string" ? prompt.trim() : undefined,
    overwrite: overwrite === true,
    // Writing is opt-in: the user confirms the proposed values first.
    dryRun: dryRun !== false,
  };
}

// The outcomes `asyncHandler` would otherwise flatten: a 422 that has to
// carry the skipped list, and the 5xx statuses with a reason (the handler
// relays 4xx statuses only and turns everything else into a bare 500).
function answerMetaError(
  err: unknown,
  res: { status: (code: number) => { json: (b: unknown) => unknown } },
): boolean {
  if (err instanceof NoContentError) {
    res.status(err.status).json({ error: err.message, skipped: err.skipped });
    return true;
  }
  if (err instanceof NoProfileError || err instanceof ModelCallError) {
    res.status(err.status).json({ error: err.message });
    return true;
  }
  return false;
}

export const metaController = {
  generate: asyncHandler(async (req, res) => {
    // A caller without AI access gets the 403 every other AI route answers,
    // before any file of theirs is read.
    await assertAiAccess();
    const input = asInput(req.body);
    // Attribute the model round to the first folder the request names (or
    // its first file), the way every other model-bound route attributes its
    // round to its scope.
    if (input.folders[0] !== undefined) {
      await primeSourceMeta(input.folders[0]);
    } else if (input.files[0] !== undefined) {
      await primeSourceMeta(input.files[0], "file");
    }
    try {
      res.json(await generateMeta(input));
    } catch (err) {
      if (!answerMetaError(err, res)) {
        throw err;
      }
    }
  }),

  fill: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const input = asFillInput(req.body);
    await primeSourceMeta(input.id, input.kind);
    try {
      res.json(await fillMeta(input));
    } catch (err) {
      if (!answerMetaError(err, res)) {
        throw err;
      }
    }
  }),
};
