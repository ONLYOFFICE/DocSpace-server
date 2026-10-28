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

import { aiService } from "./httpClient.js";
import { getObjectArray, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import { dtoToProfile } from "./profilesStorage.js";
import type { DocspaceProfile } from "./profilesStorage.js";
import { invalidateChatContext } from "./chatContextSnapshot.js";

const PATH = "/byok";

export interface ByokKeyInput {
  /** BYOK provider id from the model's `byok_providers`, e.g. `anthropic`. */
  provider: string;
  /** Provider API key. */
  key: string;
}

export interface ByokModel {
  /** Model id, used in the path when setting its BYOK key. */
  id: string;
  /** Model display name. */
  name: string;
}

export interface ByokProvider {
  /** BYOK provider id, passed as `provider` when setting a BYOK key. */
  id: string;
  /** Provider display name. */
  name: string;
  /** Models that can be called through this provider with the portal's own key. */
  models: ByokModel[];
}

export class HttpByokKeysStorage {
  async set(id: string, input: ByokKeyInput): Promise<DocspaceProfile> {
    const raw = await aiService.put(`${PATH}/keys/${encodeURIComponent(id)}`, input);
    invalidateChatContext("profiles");
    const profile = dtoToProfile(raw);
    if (!profile) {
      throw new Error("ai service returned invalid profile");
    }
    return profile;
  }

  async delete(id: string): Promise<void> {
    await aiService.delete(`${PATH}/keys/${encodeURIComponent(id)}`);
    invalidateChatContext("profiles");
  }

  async readProviders(): Promise<ByokProvider[]> {
    const raw = await aiService.get(`${PATH}/providers`);
    if (!Array.isArray(raw)) {
      return [];
    }
    return raw.filter(isObject).flatMap((item) => {
      const provider = dtoToByokProvider(item);
      return provider ? [provider] : [];
    });
  }
}

function dtoToByokModel(raw: JsonObject): ByokModel | undefined {
  const id = getString(raw, "id");
  const name = getString(raw, "name");
  return id === undefined || name === undefined ? undefined : { id, name };
}

function dtoToByokProvider(raw: JsonObject): ByokProvider | undefined {
  const id = getString(raw, "id");
  const name = getString(raw, "name");
  if (id === undefined || name === undefined) {
    return undefined;
  }
  const models = (getObjectArray(raw, "models") ?? []).flatMap((item) => {
    const model = dtoToByokModel(item);
    return model ? [model] : [];
  });
  return { id, name, models };
}
