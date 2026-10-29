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
import type { QueryValue } from "../storage/httpClient.js";
import { asyncHandler } from "./_helpers.js";
import type { TypedRequest } from "./_helpers.js";
import type { Response } from "express";
import { isObject, getObject, getString } from "../narrow.js";
import type { JsonObject } from "../narrow.js";

// AI projects. A project is a DocSpace room of type 12 whose settings block
// (`aiProject`: the chat profile, the linked Instructions rooms and the
// linked Knowledge rooms) lives with the room. Every operation is delegated
// to the .NET AI service (`internal/ai/projects…`, AiProjectsController), as
// agentsController delegates agents; the answer is the upstream DocSpace
// envelope unchanged. The contract, method by method, is
// client/packages/client/src/pages/AiProjects/spec/AiProjects-API-for-CSharp.md.

// `profileId` is a profile UUID (`Profile.id: string` in
// @onlyoffice/ai-chat; a `Guid` on the .NET side).
const UUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

// Mirrors agentsController: a profile that does not exist or cannot chat
// would be stored fine and then fail every round with model_not_found.
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

// Forward the caller's flat string query params to the .NET endpoint
// unchanged. Array/object query values are dropped rather than guessed at.
export function forwardQuery(query: Record<string, unknown>): Record<string, QueryValue> {
  const out: Record<string, QueryValue> = {};
  for (const [key, value] of Object.entries(query)) {
    if (typeof value === "string") {
      out[key] = value;
    }
  }
  return out;
}

export function bodyOf(raw: unknown): JsonObject {
  return isObject(raw) ? raw : {};
}

// The create / update body is forwarded as is; only the model is checked
// here, before the room is touched, as agentsController does (Bug 82922):
// `aiProject.profileId` is a UUID of a chat profile, or null for the portal
// default.
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
  // FolderContentDto of the projects root. The query (filterValue, sortBy,
  // sortOrder, startIndex, count, subjectId, role, hasInstructions,
  // hasKnowledge, linkedRoomId) is forwarded as is to `GET internal/ai/projects`.
  getProjects: projectHandler(async (req, res) => {
    const content = await aiService.get("/projects", {
      query: forwardQuery(req.query),
      raw: true,
    });
    res.json(content);
  }),

  // POST projects — creates a project (`POST internal/ai/projects`). Body:
  // title, color, cover, logo and the `aiProject` block (profileId,
  // instructionsRoomIds, knowledgeRoomIds). The caller becomes its owner.
  createProject: projectHandler(async (req, res) => {
    const body = bodyOf(req.body);
    await assertProjectProfile(body);
    await assertKnowledgeRoomsLinkable(body);
    res.json(await aiService.post("/projects", body, { raw: true }));
  }),

  // GET projects/{id} — one project with its `aiProject` block
  // (`GET internal/ai/projects/{id}`).
  getProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.get(`/projects/${id}`, { raw: true }));
  }),

  // PUT projects/{id} — changes the title, logo or `aiProject` block
  // (`PUT internal/ai/projects/{id}`). An absent field keeps its value.
  updateProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    const body = bodyOf(req.body);
    await assertProjectProfile(body);
    await assertKnowledgeRoomsLinkable(body);
    res.json(await aiService.put(`/projects/${id}`, body, { raw: true }));
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

  // PUT projects/{id}/pin — pins the project for the caller
  // (`PUT internal/ai/projects/{id}/pin`).
  pinProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/pin`, {}, { raw: true }));
  }),

  // PUT projects/{id}/unpin — unpins the project for the caller
  // (`PUT internal/ai/projects/{id}/unpin`).
  unpinProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/unpin`, {}, { raw: true }));
  }),

  // PUT projects/{id}/mute — turns the project's notifications off or on for
  // the caller (`PUT internal/ai/projects/{id}/mute`, body `{ mute }`).
  muteProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/mute`, bodyOf(req.body), { raw: true }));
  }),

  // PUT projects/{id}/duplicate — copies the project without its members
  // (`PUT internal/ai/projects/{id}/duplicate`); answers the file operation.
  duplicateProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/duplicate`, {}, { raw: true }));
  }),

  // DELETE projects/{id}/logo — drops the uploaded logo
  // (`DELETE internal/ai/projects/{id}/logo`).
  deleteLogo: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.delete(`/projects/${id}/logo`, { raw: true }));
  }),

  // GET projects/{id}/history — the project's history events
  // (`GET internal/ai/projects/{id}/history`; fromDate, toDate, startIndex,
  // count forwarded).
  getHistory: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(
      await aiService.get(`/projects/${id}/history`, {
        query: forwardQuery(req.query),
        raw: true,
      }),
    );
  }),

  // GET projects/{id}/share — the members and their roles
  // (`GET internal/ai/projects/{id}/share`; filterType, filterValue,
  // startIndex, count forwarded).
  getMembers: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(
      await aiService.get(`/projects/${id}/share`, {
        query: forwardQuery(req.query),
        raw: true,
      }),
    );
  }),

  // PUT projects/{id}/share — invites, re-roles or removes members
  // (`PUT internal/ai/projects/{id}/share`, body `{ invitations, notify,
  // message, culture, force }`).
  setMembers: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/share`, bodyOf(req.body), { raw: true }));
  }),

  // POST projects/{id}/resend — sends the invitation again
  // (`POST internal/ai/projects/{id}/resend`, body `{ usersIds, resendAll }`).
  resendInvitations: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.post(`/projects/${id}/resend`, bodyOf(req.body), { raw: true }));
  }),

  // PUT projects/{id}/leave — the caller leaves the project
  // (`PUT internal/ai/projects/{id}/leave`).
  leaveProject: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/leave`, {}, { raw: true }));
  }),

  // PUT projects/{id}/owner — hands the project to another user
  // (`PUT internal/ai/projects/{id}/owner`, body `{ userId, keepAsManager }`).
  changeOwner: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/owner`, bodyOf(req.body), { raw: true }));
  }),

  // GET projects/{id}/links — the invitation link
  // (`GET internal/ai/projects/{id}/links`).
  getInviteLinks: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.get(`/projects/${id}/links`, { raw: true }));
  }),

  // PUT projects/{id}/links — creates, changes or removes the invitation link
  // (`PUT internal/ai/projects/{id}/links`, body `{ linkId, access, title,
  // expirationDate, maxUseCount }`).
  setInviteLink: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.put(`/projects/${id}/links`, bodyOf(req.body), { raw: true }));
  }),

  // GET projects/{id}/skills — the skills of the linked Instructions rooms,
  // grouped by room (`GET internal/ai/projects/{id}/skills`).
  getSkills: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(await aiService.get(`/projects/${id}/skills`, { raw: true }));
  }),

  // GET projects/{id}/knowledge — the files of the linked Knowledge rooms
  // with their indexing state (`GET internal/ai/projects/{id}/knowledge`).
  getKnowledgeFiles: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    res.json(
      await aiService.get(`/projects/${id}/knowledge`, {
        query: forwardQuery(req.query),
        raw: true,
      }),
    );
  }),
};
