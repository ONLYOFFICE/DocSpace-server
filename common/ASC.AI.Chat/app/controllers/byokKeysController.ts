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

import { storage } from "../storage/index.js";
import { AiServiceHttpError } from "../storage/httpClient.js";
import { asyncHandler } from "./_helpers.js";
import { getObject, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";

function readBody(body: unknown): JsonObject {
  return isObject(body) ? body : {};
}

function readString(body: JsonObject, field: string): string {
  return getString(body, field) ?? "";
}

function upstreamMessage(body: string): string | undefined {
  try {
    const json: unknown = JSON.parse(body);
    const error = isObject(json) ? getObject(json, "error") : undefined;
    return error ? getString(error, "message") : undefined;
  } catch {
    return undefined;
  }
}

async function withValidationMessage<T>(call: Promise<T>): Promise<T> {
  try {
    return await call;
  } catch (err) {
    if (err instanceof AiServiceHttpError && err.status === 400) {
      const message = upstreamMessage(err.body);
      if (message) {
        throw Object.assign(new Error(message), { status: 400, expose: true });
      }
    }
    throw err;
  }
}

export const byokKeysController = {
  list: asyncHandler(async (_req, res) => {
    res.json(await storage.byokKeys.readAll());
  }),

  providers: asyncHandler(async (_req, res) => {
    res.json(await storage.byokKeys.readProviders());
  }),

  set: asyncHandler(async (req, res) => {
    const body = readBody(req.body);
    const key = await withValidationMessage(
      storage.byokKeys.set(req.params["id"] ?? "", {
        provider: readString(body, "provider"),
        key: readString(body, "key"),
      }),
    );
    res.json(key);
  }),

  delete: asyncHandler(async (req, res) => {
    await storage.byokKeys.delete(req.params["id"] ?? "");
    res.status(204).end();
  }),
};
