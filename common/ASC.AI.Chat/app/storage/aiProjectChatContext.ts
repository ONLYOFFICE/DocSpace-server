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

// The chat context of an AI project (`GET internal/ai/projects/{id}/chat-context`,
// AiProjectChatContextController). A round whose `entityId` is a project
// runs on what the project says, not on what the composer sent: the model
// is the project's (or the portal default), and only the linked rooms the
// caller can still read are connected — the Instructions rooms as context
// (skills) rooms, the Knowledge rooms as knowledge bases.

import { aiService } from "./httpClient.js";
import { resolveAiProjectEntityId } from "./docspaceFilesApi.js";
import { getArray, getBoolean, getNumber, getObjectArray, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import logger from "../log.js";

/** `AiProjectChatContextDto` on the C# side. */
export interface AiProjectChatContext {
  projectId: number;
  title: string;
  /** 1 project owner, 9 Project manager, 11 Member, 0 none (an admin outside the project). */
  myRole: number;
  canChat: boolean;
  /** The stored project model; `null` = the portal default. */
  profileId: string | null;
  /** The model the round runs on; `null` when none is configured. */
  effectiveProfileId: string | null;
  profileAvailable: boolean;
  instructionsRoomIds: number[];
  knowledgeRoomIds: number[];
  unavailableRoomIds: number[];
}

/** What a round handler answers instead of streaming when the project refuses it. */
export interface AiProjectRoundRefusal {
  status: number;
  body: { error: string; code?: string };
}

function idList(obj: JsonObject, key: string): number[] {
  return (getArray(obj, key) ?? []).filter(
    (v): v is number => typeof v === "number" && Number.isInteger(v) && v > 0,
  );
}

function parseChatContext(raw: unknown): AiProjectChatContext | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const projectId = getNumber(raw, "projectId");
  if (projectId === undefined) {
    return undefined;
  }
  return {
    projectId,
    title: getString(raw, "title") ?? "",
    myRole: getNumber(raw, "myRole") ?? 0,
    canChat: getBoolean(raw, "canChat") === true,
    profileId: getString(raw, "profileId") ?? null,
    effectiveProfileId: getString(raw, "effectiveProfileId") ?? null,
    profileAvailable: getBoolean(raw, "profileAvailable") === true,
    instructionsRoomIds: idList(raw, "instructionsRoomIds"),
    knowledgeRoomIds: idList(raw, "knowledgeRoomIds"),
    unavailableRoomIds: idList(raw, "unavailableRoomIds"),
  };
}

/**
 * The project's chat context when `entityId` is an AI project, `undefined`
 * otherwise. A project the caller cannot see answers 404 upstream, which
 * propagates as such.
 */
export async function readAiProjectChatContext(
  entityId: string | undefined,
): Promise<AiProjectChatContext | undefined> {
  const projectId = await resolveAiProjectEntityId(entityId);
  if (!projectId) {
    return undefined;
  }
  const context = parseChatContext(await aiService.get(`/projects/${projectId}/chat-context`));
  if (!context) {
    throw new Error(`chat-context of AI project ${projectId}: unparseable response`);
  }
  return context;
}

// The linked Instructions rooms with their skills, as the project stores them
// (`GET internal/ai/projects/{id}/skills`), in the `actionArgs.contextRooms`
// shape: only the ids of `roomIds` (the readable ones, in linked order).
// Best-effort: a failed read runs the round without skills.
async function linkedSkillRooms(projectId: number, roomIds: number[]): Promise<JsonObject[]> {
  if (roomIds.length === 0) {
    return [];
  }
  try {
    const raw = await aiService.get(`/projects/${projectId}/skills`);
    const rooms = isObject(raw) ? (getObjectArray(raw, "rooms") ?? []) : [];
    const byId = new Map<number, JsonObject>();
    for (const room of rooms) {
      const id = getNumber(room, "id");
      if (id !== undefined && getBoolean(room, "available") === true) {
        byId.set(id, room);
      }
    }
    return roomIds.flatMap((id) => {
      const room = byId.get(id);
      if (!room) {
        return [];
      }
      return [
        {
          cloud: "docspace",
          id: String(id),
          skills: (getObjectArray(room, "skills") ?? []).map((skill) => ({
            id: getString(skill, "id") ?? "",
            name: getString(skill, "name") ?? "",
            description: getString(skill, "description") ?? "",
          })),
        },
      ];
    });
  } catch (err) {
    logger.warn(
      `AI project ${projectId}: skills not read, the round runs without them: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    return [];
  }
}

/**
 * Bind a chat round to its AI project. When the round's `entityId` is a
 * project, rewrites the request body in place from the project's stored
 * settings only — nothing the composer sent for these fields is kept:
 * `profileId` becomes the project's effective model, `actionArgs.contextRooms`
 * the linked Instructions rooms with their skills and
 * `actionArgs.knowledgeRooms` the linked Knowledge rooms (each limited to the
 * rooms the caller can read, in linked order). `contextEntityId` is the
 * caller's and is left as sent. Returns the refusal to answer instead of the
 * round — 403 when the caller may not chat in the project, 409
 * `projectModelUnavailable` when its model is gone or cannot chat — or `null`
 * to go on.
 */
export async function applyAiProjectChatContext(
  body: unknown,
): Promise<AiProjectRoundRefusal | null> {
  if (!isObject(body)) {
    return null;
  }
  const entityId = typeof body["entityId"] === "string" ? body["entityId"] : undefined;
  const context = await readAiProjectChatContext(entityId);
  if (!context) {
    return null;
  }
  if (!context.canChat) {
    return {
      status: 403,
      body: { error: "You cannot chat in this AI project" },
    };
  }
  if (!context.profileAvailable || !context.effectiveProfileId) {
    return {
      status: 409,
      body: {
        error: "The model of this AI project is not available",
        code: "projectModelUnavailable",
      },
    };
  }

  body["profileId"] = context.effectiveProfileId;

  const actionArgs = isObject(body["actionArgs"]) ? body["actionArgs"] : {};
  const contextRooms = await linkedSkillRooms(context.projectId, context.instructionsRoomIds);

  const knowledgeRooms = context.knowledgeRoomIds.map((id) => ({
    cloud: "docspace",
    id: String(id),
  }));

  const nextArgs: JsonObject = { ...actionArgs };
  if (contextRooms.length > 0) {
    nextArgs["contextRooms"] = contextRooms;
  } else {
    delete nextArgs["contextRooms"];
  }
  if (knowledgeRooms.length > 0) {
    nextArgs["knowledgeRooms"] = knowledgeRooms;
  } else {
    delete nextArgs["knowledgeRooms"];
  }
  delete nextArgs["knowledgeRoom"];
  body["actionArgs"] = nextArgs;

  if (context.unavailableRoomIds.length > 0) {
    logger.debug(
      `AI project ${context.projectId}: rooms ${context.unavailableRoomIds.join(",")} not usable by the caller`,
    );
  }
  return null;
}
