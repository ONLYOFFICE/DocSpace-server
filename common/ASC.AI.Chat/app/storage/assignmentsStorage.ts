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

import { aiService, AiServiceHttpError } from "./httpClient.js";
import { isObject } from "../narrow.js";
import logger from "../log.js";
import type { AssignmentsStorage, ActionType } from "@onlyoffice/ai-chat/core";
import {
  invalidateChatContext,
  readChatContext,
  reportChatContextMiss,
} from "./chatContextSnapshot.js";

// The action -> profile bindings are one map per portal. The `entityId` the
// library passes with every call (the folder the chat is opened from) is
// accepted for the interface's sake and ignored. An AI project's own model
// is served by the Files API (`files/rooms/{id}/model`) and bound to the
// round by `applyAiProjectChatContext`. A request without
// an `entityId` is the portal-wide map on the C# side.
const PATH = "/assignments";

// Case-insensitive `ActionType` lookup: the aggregate serializes the C# enum
// names (`Chat`, `ImageGeneration`, …), which match the library's values, but
// a dictionary key policy on the server would only change their casing.
function lookupAssignment(
  assignments: Record<string, string>,
  actionType: ActionType,
): string | null {
  const direct = assignments[actionType];
  if (typeof direct === "string") {
    return direct;
  }
  const wanted = actionType.toLowerCase();
  for (const [key, value] of Object.entries(assignments)) {
    if (key.toLowerCase() === wanted) {
      return value;
    }
  }
  return null;
}

export class HttpAssignmentsStorage implements AssignmentsStorage {
  async create(actionType: ActionType, profileId: string, _entityId?: string): Promise<void> {
    await aiService.post(PATH, { actionType, profileId });
    invalidateChatContext("assignments");
  }

  async readByType(actionType: ActionType, _entityId?: string): Promise<string | null> {
    // The aggregate's global map went through the same resolver as the
    // per-type endpoint (`Default` substitution included), so a key lookup
    // is equivalent to `GET assignments/{type}`.
    const snapshot = readChatContext("assignments");
    if (snapshot) {
      const profileId = lookupAssignment(snapshot.global.assignments, actionType);
      logger.info(
        `HttpAssignmentsStorage.readByType(${actionType}) via chat-context -> profileId=${profileId ?? "NONE"}`,
      );
      return profileId;
    }
    reportChatContextMiss(`assignments.readByType(${actionType})`);
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(actionType)}`);
      const profileId = typeof raw === "string" ? raw : null;
      // The action -> profile routing. `image_generation` is the one the
      // built-in `generate_image` tool resolves: no assignment here and the
      // tool answers "Image generation provider is not configured." without
      // ever calling a provider.
      logger.info(
        `HttpAssignmentsStorage.readByType(${actionType}) -> profileId=${profileId ?? "NONE"}`,
      );
      return profileId;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        logger.info(
          `HttpAssignmentsStorage.readByType(${actionType}) -> 404 NO ASSIGNMENT`,
        );
        return null;
      }
      throw err;
    }
  }

  async readAll(_entityId?: string): Promise<Partial<Record<ActionType, string>>> {
    const snapshot = readChatContext("assignments");
    if (snapshot) {
      return { ...snapshot.global.assignments } as Partial<Record<ActionType, string>>;
    }
    reportChatContextMiss("assignments.readAll");
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

  async update(actionType: ActionType, profileId: string, _entityId?: string): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(actionType)}`, { profileId });
    invalidateChatContext("assignments");
  }

  async upsertMany(
    assignments: Partial<Record<ActionType, string>>,
    _entityId?: string,
  ): Promise<void> {
    const payload: Record<string, string> = {};
    for (const [k, v] of Object.entries(assignments)) {
      if (typeof v === "string") {
        payload[k] = v;
      }
    }
    await aiService.put(PATH, { assignments: payload });
    invalidateChatContext("assignments");
  }

  async delete(actionType: ActionType, _entityId?: string): Promise<void> {
    invalidateChatContext("assignments");
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(actionType)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteMany(actionTypes: ActionType[], _entityId?: string): Promise<void> {
    if (actionTypes.length === 0) {
      return;
    }
    await aiService.delete(PATH, { body: { actionTypes } });
    invalidateChatContext("assignments");
  }
}
