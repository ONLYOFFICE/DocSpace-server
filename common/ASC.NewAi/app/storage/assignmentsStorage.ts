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

import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { isObject } from "../narrow.js";
import type { AssignmentsStorage, ActionType } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/assignments";

function entityIdQuery(entityId: string | undefined): Record<string, QueryValue> | undefined {
  return entityId ? { entityId } : undefined;
}

export class HttpAssignmentsStorage implements AssignmentsStorage {
  async create(actionType: ActionType, profileId: string, entityId?: string): Promise<void> {
    await aiService.post(PATH, { actionType, profileId, entityId });
  }

  async readByType(actionType: ActionType, entityId?: string): Promise<string | null> {
    try {
      const raw = await aiService.get(
        `${PATH}/${encodeURIComponent(actionType)}`,
        entityId ? { query: entityIdQuery(entityId) } : undefined,
      );
      return typeof raw === "string" ? raw : null;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readAll(entityId?: string): Promise<Partial<Record<ActionType, string>>> {
    const raw = await aiService.get(
      PATH,
      entityId ? { query: entityIdQuery(entityId) } : undefined,
    );
    if (!isObject(raw)) {
      return {};
    }
    const result: Partial<Record<ActionType, string>> = {};
    for (const [key, value] of Object.entries(raw)) {
      if (typeof value === "string") {
        Object.assign(result, { [key]: value });
      }
    }
    return result;
  }

  async update(actionType: ActionType, profileId: string, entityId?: string): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(actionType)}`, { profileId, entityId });
  }

  async upsertMany(
    assignments: Partial<Record<ActionType, string>>,
    entityId?: string,
  ): Promise<void> {
    const payload: Record<string, string> = {};
    for (const [k, v] of Object.entries(assignments)) {
      if (typeof v === "string") {
        payload[k] = v;
      }
    }
    await aiService.put(PATH, { assignments: payload, entityId });
  }

  async delete(actionType: ActionType, entityId?: string): Promise<void> {
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(actionType)}`, {
        query: entityIdQuery(entityId),
      });
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteMany(actionTypes: ActionType[], entityId?: string): Promise<void> {
    if (actionTypes.length === 0) {
      return;
    }
    await aiService.delete(PATH, {
      body: { actionTypes },
      query: entityIdQuery(entityId),
    });
  }
}
