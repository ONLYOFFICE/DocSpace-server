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

import { storage } from "../storage/index.js";
import { AiServiceHttpError } from "../storage/httpClient.js";
import { filesApiGet } from "../storage/docspaceFilesApi.js";
import { canChat, readProjectProfileId, writeProjectProfileId } from "../storage/aiProjectModel.js";
import { asyncHandler } from "./_helpers.js";
import type { TypedRequest } from "./_helpers.js";
import type { Response } from "express";
import { getBoolean, getNumber, getObject, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";

// The model of an AI project (`GET/PUT projects/{id}/model`). A project is
// a Files room of type 12 that the client manages through the Files API;
// its model is the project-scoped Chat assignment of the .NET AI service
// (storage/aiProjectModel.ts), proxied here until the Files API serves it.
// The room is read first with the caller's credentials, so a project the
// caller cannot see answers the Files API's 403 / 404.

/** `RoomType.AiProjectRoom` on the C# side. */
const AI_PROJECT_ROOM_TYPE = 12;

function fail(status: number, message: string): never {
  throw Object.assign(new Error(message), { status, expose: true });
}

// Project ids are int folder ids on the .NET side (`RoomIdRequestDto<int>`).
export function projectIdParam(value: string | undefined): string {
  if (!value || !/^\d+$/.test(value)) {
    fail(400, "project id must be a positive integer");
  }
  return value;
}

// The project room as the caller sees it; anything but an AI project is 404.
async function readProjectRoom(projectId: string): Promise<JsonObject> {
  const room = await filesApiGet(`rooms/${projectId}`);
  if (!isObject(room) || getNumber(room, "roomType") !== AI_PROJECT_ROOM_TYPE) {
    fail(404, "The project does not exist or is not an AI project");
  }
  return room;
}

// The project's model as the client shows it: `null` runs on the portal
// default. `available` is false for a profile that is gone or cannot chat.
async function modelAnswer(projectId: string): Promise<JsonObject> {
  const profileId = await readProjectProfileId(projectId);
  if (profileId === null) {
    return docspaceEnvelope({ profile: null });
  }
  const profile = await storage.profiles.readById(profileId).catch(() => undefined);
  return docspaceEnvelope({
    profile: {
      id: profileId,
      name: profile?.name ?? "",
      provider: profile?.providerType ?? null,
      available: canChat(profile),
    },
  });
}

// The DocSpace envelope the client's request layer unwraps
// (`response.data.response`).
function docspaceEnvelope(response: unknown): JsonObject {
  return { response, count: 1, status: 0, statusCode: 200 } as JsonObject;
}

// The C# side words its 4xx answers for the user, so they are relayed with
// the message of the DocSpace envelope (`{ error: { message } }`) instead of
// the bare reason phrase; never the upstream URL, stack or raw body.
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

export const aiProjectModelController = {
  // GET projects/{id}/model — the project's own model, or `null` for the
  // portal default.
  getModel: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    await readProjectRoom(id);
    res.json(await modelAnswer(id));
  }),

  // PUT projects/{id}/model — body `{ profileId: string | null }`. Owner and
  // Project managers only (the room's `security.EditRoom`).
  setModel: projectHandler(async (req, res) => {
    const id = projectIdParam(req.params["id"]);
    const body: unknown = req.body;
    if (!isObject(body) || !("profileId" in body)) {
      fail(400, "profileId is required (a UUID or null)");
    }
    const profileId = body["profileId"];
    if (profileId !== null && typeof profileId !== "string") {
      fail(400, "profileId must be a UUID or null");
    }
    const room = await readProjectRoom(id);
    const security = getObject(room, "security");
    if (!security || getBoolean(security, "EditRoom") !== true) {
      fail(403, "You cannot change the model of this AI project");
    }
    await writeProjectProfileId(id, profileId);
    res.json(await modelAnswer(id));
  }),
};
