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

// The chat context of an AI project: what a round whose `entityId` is a
// project runs on, instead of what the composer sent. A project is a room
// (`FolderType.AiProjectRoom`) whose sources are linked Instructions and
// Knowledge rooms; both are read from the Files API with the caller's
// credentials — the room (`GET api/2.0/files/rooms/{id}`: the caller's
// `access` and `inRoom`) and its sources (`GET api/2.0/files/rooms/{id}/sources`).
// The project's model is its own (`GET api/2.0/files/rooms/{id}/model`),
// else the portal's Chat assignment; the
// Instructions rooms are connected as context (skills) rooms, the Knowledge
// rooms as knowledge bases. Every room the server lists is active and
// readable by the caller.

import { storage } from "./index.js";
import {
  filesApiGet,
  getMyDocumentsFolderId,
  resolveAiProjectEntityId,
} from "./docspaceFilesApi.js";
import { refreshContextRoomSkills } from "./contextRoomSkills.js";
import { CONTEXT_CLOUD, parseSkillsCatalog } from "./roomSkills.js";
import { ActionType, CapabilitiesUI } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { getBoolean, getNumber, getObjectArray, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import logger from "../log.js";

/** The ids of the rooms linked to a project, by kind, in the order the server lists them. */
interface AiProjectSources {
  instructionsRoomIds: number[];
  knowledgeRoomIds: number[];
}

function roomIdsOf(raw: unknown, key: string): number[] {
  const rooms = isObject(raw) ? getObjectArray(raw, key) : undefined;
  return (rooms ?? [])
    .map((room) => getNumber(room, "id"))
    .filter((id): id is number => id !== undefined);
}

/** The rooms linked to the project (`GET api/2.0/files/rooms/{id}/sources`), sorted by title. */
async function readProjectSources(projectId: string): Promise<AiProjectSources> {
  const raw = await filesApiGet(`rooms/${encodeURIComponent(projectId)}/sources`);
  return {
    instructionsRoomIds: roomIdsOf(raw, "instructionsRooms"),
    knowledgeRoomIds: roomIdsOf(raw, "knowledgeRooms"),
  };
}

export interface AiProjectChatContext {
  projectId: number;
  title: string;
  /** The caller's `access` on the room: 1 project owner, 9 Project manager, 2 Member, 0 none (an admin outside the project). */
  myRole: number;
  /** Members only: owner, Project manager, Member (the room's `inRoom`). */
  canChat: boolean;
  /** The project's own model (its scoped Chat assignment); `null` = the portal default. */
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

/** Whether a profile can serve a chat round (no capability mask = it can). */
function canChat(profile: Pick<Profile, "capabilities"> | null | undefined): boolean {
  return (
    !!profile &&
    (typeof profile.capabilities !== "number" || (profile.capabilities & CapabilitiesUI.Chat) !== 0)
  );
}

// The project's own model (`GET files/rooms/{id}/model`, readable by every
// member), or `null` when none is selected or its profile was removed.
async function readProjectProfileId(projectId: string): Promise<string | null> {
  const raw = await filesApiGet(`rooms/${encodeURIComponent(projectId)}/model`);
  const profileId = isObject(raw) ? getString(raw, "profileId") : undefined;
  return profileId || null;
}

// The model a project round runs on: the project's own, else the portal's
// Chat model, and whether it names a profile that exists and can chat. The
// availability is that of the effective profile: a project model that is
// gone is not replaced by the portal default.
async function effectiveProfile(
  projectId: string,
): Promise<{ own: string | null; id: string | null; available: boolean }> {
  const own = await readProjectProfileId(projectId);
  const id = own ?? (await storage.assignments.readByType(ActionType.Chat).catch(() => null));
  if (!id) {
    return { own, id: null, available: false };
  }
  const profile = await storage.profiles.readById(id).catch(() => undefined);
  return { own, id, available: canChat(profile) };
}

/**
 * The project's chat context when `entityId` is an AI project, `undefined`
 * otherwise. A project the caller cannot see answers 403 or 404 upstream,
 * which propagates as such.
 */
export async function readAiProjectChatContext(
  entityId: string | undefined,
): Promise<AiProjectChatContext | undefined> {
  const projectId = await resolveAiProjectEntityId(entityId);
  if (!projectId) {
    return undefined;
  }
  const [project, sources, profile] = await Promise.all([
    filesApiGet(`rooms/${encodeURIComponent(projectId)}`),
    readProjectSources(projectId),
    effectiveProfile(projectId),
  ]);
  if (!isObject(project)) {
    throw new Error(`AI project ${projectId}: unparseable answer`);
  }
  return {
    projectId: getNumber(project, "id") ?? Number(projectId),
    title: getString(project, "title") ?? "",
    myRole: getNumber(project, "access") ?? 0,
    canChat: getBoolean(project, "inRoom") === true,
    profileId: profile.own,
    effectiveProfileId: profile.id,
    profileAvailable: profile.available,
    instructionsRoomIds: sources.instructionsRoomIds,
    knowledgeRoomIds: sources.knowledgeRoomIds,
    unavailableRoomIds: [],
  };
}

// The linked Instructions rooms with their current skills, in the
// `actionArgs.contextRooms` shape: one read of the project's skills catalog
// (`GET api/2.0/files/rooms/{id}/skills`), kept in linked order. Best-effort:
// when the catalog cannot be read the round runs without the skills.
async function linkedSkillRooms(projectId: number, roomIds: number[]): Promise<JsonObject[]> {
  if (roomIds.length === 0) {
    return [];
  }
  let catalog = new Map<string, { id: string; name: string; description: string }[]>();
  try {
    const rooms = parseSkillsCatalog(
      await filesApiGet(`rooms/${encodeURIComponent(String(projectId))}/skills`),
    );
    catalog = new Map(rooms.map((room) => [room.id, room.skills]));
  } catch (err) {
    logger.warn(
      `AI project ${projectId}: skills not read, the round runs without them: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
  }
  return roomIds.map((id) => ({
    cloud: CONTEXT_CLOUD,
    id: String(id),
    skills: (catalog.get(String(id)) ?? []).map(({ id: skillId, name, description }) => ({
      id: skillId,
      name,
      description,
    })),
  }));
}

/**
 * Bind a chat round to its AI project. When the round's `entityId` is a
 * project, rewrites the request body in place from the project's stored
 * settings only — nothing the composer sent for these fields is kept:
 * `profileId` becomes the project's effective model, `actionArgs.contextRooms`
 * the linked Instructions rooms with their skills and
 * `actionArgs.knowledgeRooms` the linked Knowledge rooms (each limited to the
 * rooms the caller can read, in linked order). The Context folder is the one
 * the caller picked in the project (`actionArgs.folderContext`); with none
 * picked it is the caller's My documents, which a project has no location
 * of its own to offer — set as `contextEntityId` (the scope of the file
 * tools) and as the folder context (the location line of the prompt).
 * Returns the refusal to answer instead of the round — 403 when the caller
 * may not chat in the project, 409 `projectModelUnavailable` when its model
 * is gone or cannot chat — or `null` to go on.
 *
 * Outside a project the body keeps what the composer sent, except that the
 * skills of the picked Instructions rooms are read again for this round.
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
    await refreshContextRoomSkills(body);
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
  const folderContext = isObject(nextArgs["folderContext"]) ? nextArgs["folderContext"] : undefined;
  const pickedFolderId = folderContext ? getString(folderContext, "id") : undefined;
  if (pickedFolderId) {
    body["contextEntityId"] = pickedFolderId;
  } else {
    const myDocumentsId = await getMyDocumentsFolderId();
    if (myDocumentsId) {
      body["contextEntityId"] = myDocumentsId;
      nextArgs["folderContext"] = { id: myDocumentsId };
    } else {
      logger.warn(
        `AI project ${context.projectId}: My documents not resolved, the round has no folder context`,
      );
      delete body["contextEntityId"];
      delete nextArgs["folderContext"];
    }
  }
  body["actionArgs"] = nextArgs;

  if (context.unavailableRoomIds.length > 0) {
    logger.debug(
      `AI project ${context.projectId}: rooms ${context.unavailableRoomIds.join(",")} not usable by the caller`,
    );
  }
  return null;
}
