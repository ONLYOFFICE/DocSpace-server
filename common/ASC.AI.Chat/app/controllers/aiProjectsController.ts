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

import { CapabilitiesUI } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { aiService, AiServiceHttpError } from "../storage/httpClient.js";
import {
  listProjectKnowledge,
  listProjectSkills,
  projectIdOf,
  readProjectRooms,
  roomIdsOf,
  syncProjectRooms,
  withAiProjectBlock,
  type AiProjectWarning,
} from "../storage/aiProjectRooms.js";
import { asyncHandler } from "./_helpers.js";
import type { TypedRequest } from "./_helpers.js";
import type { Response } from "express";
import { isObject, getObject, getString } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import logger from "../log.js";

// AI projects. A project is a DocSpace room of type 12 that links
// Instructions rooms and Knowledge rooms. The room itself (list, create,
// get, update, delete) is delegated to the .NET AI service
// (`internal/ai/projects…`, AiProjectsController), which stores the linked
// rooms as one flat set per project behind `GET/POST/DELETE
// internal/ai/projects/{id}/rooms`. The client keeps the contract of
// `ai-projects-api`: a project answers with an `aiProject` block (profile,
// `instructionsRooms`, `knowledgeRooms`, `canEditSettings`, `warnings`) and
// the create / update body carries the rooms as `aiProject.instructionsRoomIds`
// / `knowledgeRoomIds`; this controller composes the one over the other
// (see storage/aiProjectRooms.ts). The list answers the rooms without the
// block: nothing in the list shows the linked rooms, and a project is read
// again when it is opened or picked.

// `profileId` is a profile UUID (`Profile.id: string` in
// @onlyoffice/ai-chat; a `Guid` on the .NET side).
const UUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

// A profile that does not exist or cannot chat would be stored fine and
// then fail every round with model_not_found, so it is checked up front.
async function assertUsableChatProfile(profileId: string): Promise<void> {
  const profile = await storage.profiles.readById(profileId);
  if (!profile) {
    badRequest(`AI profile "${profileId}" does not exist`);
  }
  if (
    typeof profile.capabilities === "number" &&
    (profile.capabilities & CapabilitiesUI.Chat) === 0
  ) {
    badRequest(`AI profile "${profileId}" does not support chat`);
  }
}

// Project ids are int folder ids on the .NET side (`RoomIdRequestDto<int>`).
// Reject anything non-integer up front with a clean 400 instead of letting
// the upstream model-binder fail opaquely.
export function projectIdParam(value: string | undefined): string {
  if (!value || !/^\d+$/.test(value)) {
    badRequest("project id must be a positive integer");
  }
  return value;
}

/** The query a project route forwards: a string per name, or the strings of a repeated name. */
export type ForwardedQuery = Record<string, string | string[]>;

// Forward the caller's string query params to the .NET endpoint unchanged. A
// repeated parameter (`tags=a&tags=b`, which Express parses into an array)
// goes through as the same repeated parameter, so the rooms-style list
// filters bind on the .NET side as they do for `files/rooms`. Object query
// values are dropped rather than guessed at.
export function forwardQuery(query: Record<string, unknown>): ForwardedQuery {
  const out: ForwardedQuery = {};
  for (const [key, value] of Object.entries(query)) {
    if (typeof value === "string") {
      out[key] = value;
    } else if (Array.isArray(value)) {
      const strings = value.filter((v): v is string => typeof v === "string");
      if (strings.length > 0) {
        out[key] = strings;
      }
    }
  }
  return out;
}

export function bodyOf(raw: unknown): JsonObject {
  return isObject(raw) ? raw : {};
}

// The model of the create / update body is checked before the room is
// touched (Bug 82922): `aiProject.profileId` is a UUID of a chat profile, or
// null for the portal default.
export async function assertProjectProfile(body: JsonObject): Promise<void> {
  const settings = getObject(body, "aiProject");
  if (!settings || !("profileId" in settings) || settings["profileId"] === null) {
    return;
  }
  const profileId = settings["profileId"];
  if (typeof profileId !== "string" || !UUID_PATTERN.test(profileId)) {
    badRequest("aiProject.profileId must be a UUID or null");
  }
  await assertUsableChatProfile(profileId);
}

// Knowledge rooms are searched through vectorization: while it is off on the
// portal (`GET internal/ai/config` → `vectorizationEnabled: false`) linking
// one to a project would promise a knowledge base that no round can search,
// so a non-empty `aiProject.knowledgeRoomIds` is refused. `[]` (unlinking)
// and an absent list go through. A config that cannot be read does not
// block the save: the C# side validates the rooms again.
export async function assertKnowledgeRoomsLinkable(body: JsonObject): Promise<void> {
  const settings = getObject(body, "aiProject");
  const roomIds = settings?.["knowledgeRoomIds"];
  if (!Array.isArray(roomIds) || roomIds.length === 0) {
    return;
  }
  const config = await aiService.get("/config").catch(() => undefined);
  if (isObject(config) && config["vectorizationEnabled"] === false) {
    badRequest("Knowledge rooms cannot be linked while vectorization is disabled on this portal");
  }
}

// The .NET side stores no model with a project yet: a chosen model cannot
// be kept, so the save goes on with the portal default and says so in the
// answer's `aiProject.warnings` instead of failing or dropping it silently.
export function modelNotStoredWarning(body: JsonObject): AiProjectWarning[] {
  const settings = getObject(body, "aiProject");
  if (!settings || typeof settings["profileId"] !== "string") {
    return [];
  }
  return [
    {
      code: "modelNotStored",
      roomId: null,
      subjectId: null,
      message:
        "The model is not stored with the project yet; the project runs on the portal default model",
    },
  ];
}

// The create / update body without its `aiProject` block: the .NET room
// routes take `title`, `color`, `cover`, `tags` and `logo` and refuse any
// other property, so the block is taken apart here (rooms -> the rooms
// routes, model -> a warning) and never forwarded.
export function splitProjectBody(body: JsonObject): {
  room: JsonObject;
  settings: JsonObject | undefined;
} {
  const { aiProject, ...room } = body;
  return { room, settings: isObject(aiProject) ? aiProject : undefined };
}

// The C# side words its 4xx answers for the user (AiProjectResource), so a
// project route relays that text instead of the bare reason phrase that
// `asyncHandler` keeps for every other upstream failure. Only the message of
// the DocSpace envelope (`{ error: { message } }`) goes out — never the
// upstream URL, stack or raw body; 5xx keep the generic handling.
function upstreamMessage(body: string): string | undefined {
  try {
    const parsed: unknown = JSON.parse(body);
    if (!isObject(parsed)) {
      return undefined;
    }
    const error = getObject(parsed, "error");
    return (error && getString(error, "message")) || getString(parsed, "error") || undefined;
  } catch {
    return undefined;
  }
}

function projectHandler(handler: (req: TypedRequest, res: Response) => Promise<void>) {
  return asyncHandler(async (req, res) => {
    try {
      await handler(req, res);
    } catch (err) {
      if (
        err instanceof AiServiceHttpError &&
        err.status >= 400 &&
        err.status < 500 &&
        !res.headersSent
      ) {
        res.status(err.status).json({
          error: upstreamMessage(err.body) ?? (err.statusText || "Upstream error"),
        });
        return;
      }
      throw err;
    }
  });
}

export const aiProjectsController = {
  // GET projects — lists the AI projects the caller may see, as the
  // FolderContentDto of the projects root, without the `aiProject` block.
  // The query (filterValue, sortBy, sortOrder, startIndex, count, subjectId,
  // excludeSubject, subjectOwnerId, tags (one value holding a JSON array of
  // names, as `files/rooms` takes it), withoutTags) is forwarded as is to
  // `GET internal/ai/projects`.
  getProjects: projectHandler(async (req, res) => {
    const content = await aiService.get("/projects", {
      query: forwardQuery(req.query),
      raw: true,
    });
    res.json(content);
  }),

  // POST projects — creates a project (`POST internal/ai/projects`: title,
  // color, cover, logo, tags) and links the rooms of its `aiProject` block
  // (`POST internal/ai/projects/{id}/rooms`). The caller becomes its owner.
  // The answer is the project with its block; a room the server refused to
  // link, or a model it cannot store yet, is reported in `aiProject.warnings`.
  createProject: projectHandler(async (req, res) => {
    const body = bodyOf(req.body);
    await assertProjectProfile(body);
    await assertKnowledgeRoomsLinkable(body);
    const { room, settings } = splitProjectBody(body);
    const envelope = await aiService.post("/projects", room, { raw: true });
    const id = projectIdOf(envelope);
    if (id === undefined) {
      logger.warn("ai/projects create: the answer carries no project id, the rooms are not linked");
      res.json(envelope);
      return;
    }
    const warnings = modelNotStoredWarning(body);
    const wanted = roomIdsOf(settings);
    warnings.push(
      ...(await syncProjectRooms(id, wanted, { instructionsRooms: [], knowledgeRooms: [] })),
    );
    const rooms = await readProjectRooms(id);
    res.json(withAiProjectBlock(envelope, rooms, warnings));
  }),

  // GET projects/{id} — one project with its `aiProject` block, composed from
  // `GET internal/ai/projects/{id}` and `GET internal/ai/projects/{id}/rooms`.
  getProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    const [envelope, rooms] = await Promise.all([
      aiService.get(`/projects/${id}`, { raw: true }),
      readProjectRooms(id),
    ]);
    res.json(withAiProjectBlock(envelope, rooms));
  }),

  // PUT projects/{id} — changes the title, logo, tags (`PUT internal/ai/projects/{id}`;
  // an absent field keeps its value, a present `tags` replaces the set) and
  // brings the linked rooms to the lists of the `aiProject` block
  // (`POST` / `DELETE internal/ai/projects/{id}/rooms`): a list sent links
  // and unlinks to match it, a list not sent keeps its rooms. A body with
  // only the block reads the room instead of writing it.
  updateProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    const body = bodyOf(req.body);
    await assertProjectProfile(body);
    await assertKnowledgeRoomsLinkable(body);
    const { room, settings } = splitProjectBody(body);
    const [envelope, current] = await Promise.all([
      Object.keys(room).length > 0
        ? aiService.put(`/projects/${id}`, room, { raw: true })
        : aiService.get(`/projects/${id}`, { raw: true }),
      readProjectRooms(id),
    ]);
    const warnings = modelNotStoredWarning(body);
    const wanted = roomIdsOf(settings);
    let rooms = current;
    if (wanted.instructionsRoomIds !== undefined || wanted.knowledgeRoomIds !== undefined) {
      warnings.push(...(await syncProjectRooms(id, wanted, current)));
      rooms = await readProjectRooms(id);
    }
    res.json(withAiProjectBlock(envelope, rooms, warnings));
  }),

  // DELETE projects/{id} — deletes the project (`DELETE internal/ai/projects/{id}`,
  // body `{ deleteAfter }`); answers the file operation to poll.
  deleteProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    const operation = await aiService.delete(`/projects/${id}`, {
      body: req.body ?? {},
      raw: true,
    });
    res.json(operation);
  }),

  // GET projects/{id}/skills — the skills of the linked Instructions rooms,
  // grouped by room: the rooms from `GET internal/ai/projects/{id}/rooms`,
  // the skills from each room's Markdown files (storage/roomSkills.ts).
  getSkills: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await listProjectSkills(id));
  }),

  // GET projects/{id}/knowledge — the files of the linked Knowledge rooms
  // with their indexing state: the rooms from `GET internal/ai/projects/{id}/rooms`,
  // the files from the Files API.
  getKnowledgeFiles: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await listProjectKnowledge(id));
  }),
};
