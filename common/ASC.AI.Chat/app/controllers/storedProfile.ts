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

// A route that runs a model on a profile takes the profile from the store,
// never from the request. The client sends the whole profile object, and the
// engine builds the provider from its `baseUrl`, key and headers: used as
// sent, it would let any caller point the service at an address of their
// choice (loopback, a private network, a metadata endpoint), past the check
// the profile routes make with `assertSafeBaseUrl`. Only the id is trusted.

import type { Profile } from "@onlyoffice/ai-chat/core";
import { AiServiceHttpError } from "../storage/httpClient.js";
import { storage } from "../storage/index.js";
import { getString, isObject } from "../narrow.js";

/**
 * The stored profile with this id, or `undefined` for an id the AI service
 * refuses as malformed (400): a string that is no id names no profile. Any
 * other failure of the read is a failure of the service, not an absent
 * profile, and is passed on.
 */
export async function readStoredProfile(id: string): Promise<Profile | undefined> {
  try {
    return await storage.profiles.readById(id);
  } catch (err) {
    if (err instanceof AiServiceHttpError && err.status === 400) {
      return undefined;
    }
    throw err;
  }
}

/**
 * The stored profile a client-sent `profile` names by its id. Answers 404
 * when the object has no id or the store holds no such profile; a store that
 * cannot be read fails the call with its own error.
 */
export async function resolveStoredProfile(sent: unknown): Promise<Profile> {
  const id = isObject(sent) ? getString(sent, "id") : undefined;
  const stored = id ? await readStoredProfile(id) : undefined;
  if (!stored) {
    throw Object.assign(new Error("the AI profile does not exist"), { status: 404, expose: true });
  }
  return stored;
}
