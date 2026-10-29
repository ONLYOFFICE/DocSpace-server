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

// The skills of the Instructions rooms a user picked in the composer
// (`actionArgs.contextRooms`), read again for every round. The widget sends
// the list it read when the room was connected; the server reads the
// room's Markdown files afresh (`getRoomSkills`, the Files API, which caches
// on its side), so a skill added, renamed or edited since applies to the
// next round without reconnecting the room. Best-effort per room: a room
// that cannot be read keeps the skills the widget sent.

import { CONTEXT_CLOUD, getRoomSkills } from "./roomSkills.js";
import { getObjectArray, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import logger from "../log.js";

/**
 * Replaces the skills of every DocSpace room in `actionArgs.contextRooms`
 * with the room's current skills. Rooms of another cloud, and entries
 * without an id, are left as sent. A body without context rooms is left
 * alone.
 */
export async function refreshContextRoomSkills(body: unknown): Promise<void> {
  if (!isObject(body) || !isObject(body["actionArgs"])) {
    return;
  }
  const rooms = getObjectArray(body["actionArgs"], "contextRooms");
  if (!rooms || rooms.length === 0) {
    return;
  }
  const refreshed = await Promise.all(
    rooms.map(async (room): Promise<JsonObject> => {
      const id = getString(room, "id");
      if (!id || (room["cloud"] ?? CONTEXT_CLOUD) !== CONTEXT_CLOUD) {
        return room;
      }
      try {
        const skills = await getRoomSkills(id);
        return { ...room, skills: skills.map(({ id: skillId, name, description }) => ({ id: skillId, name, description })) };
      } catch (err) {
        logger.warn(
          `context room ${id}: skills not refreshed, the round keeps the ones sent: ${
            err instanceof Error ? err.message : String(err)
          }`,
        );
        return room;
      }
    }),
  );
  (body["actionArgs"] as JsonObject)["contextRooms"] = refreshed;
}
