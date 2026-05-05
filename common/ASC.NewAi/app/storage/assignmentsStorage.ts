// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import { aiService, AiServiceHttpError } from "./httpClient.js";
import { isObject } from "../narrow.js";
import type { AssignmentsStorage, ActionType } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/assignments";

export class HttpAssignmentsStorage implements AssignmentsStorage {
  async create(actionType: ActionType, profileId: string): Promise<void> {
    await aiService.post(PATH, { actionType, profileId });
  }

  async readByType(actionType: ActionType): Promise<string | null> {
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(actionType)}`);
      return typeof raw === "string" ? raw : null;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readAll(): Promise<Partial<Record<ActionType, string>>> {
    const raw = await aiService.get(PATH);
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

  async update(actionType: ActionType, profileId: string): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(actionType)}`, { profileId });
  }

  async upsertMany(assignments: Partial<Record<ActionType, string>>): Promise<void> {
    const payload: Record<string, string> = {};
    for (const [k, v] of Object.entries(assignments)) {
      if (typeof v === "string") {
        payload[k] = v;
      }
    }
    await aiService.put(PATH, { assignments: payload });
  }

  async delete(actionType: ActionType): Promise<void> {
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(actionType)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteMany(actionTypes: ActionType[]): Promise<void> {
    if (actionTypes.length === 0) {
      return;
    }
    await aiService.delete(PATH, { body: { actionTypes } });
  }
}
