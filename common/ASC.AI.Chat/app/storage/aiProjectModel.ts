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

// The model of an AI project: the portal's `Chat` assignment scoped to the
// project room, kept by the .NET assignments storage
// (`internal/ai/assignments`, `entityId` = the project id), the way the AI
// agents bound their profile. A scoped read never falls back to the
// portal-wide map on the .NET side, so `null` here means "no model of its
// own: the project runs on the portal default". Proxied through this
// service for now; it is meant to move to the Files API later.

import { ActionType, CapabilitiesUI } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { storage } from "./index.js";
import { aiService, AiServiceHttpError } from "./httpClient.js";
import { invalidateChatContext } from "./chatContextSnapshot.js";
import logger from "../log.js";

const PATH = "/assignments";

// `profileId` is a profile UUID (`Profile.id: string` in @onlyoffice/ai-chat;
// a `Guid` on the .NET side).
const UUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

/** Whether a profile can serve a chat round (no capability mask = it can). */
export function canChat(profile: Pick<Profile, "capabilities"> | null | undefined): boolean {
  return (
    !!profile &&
    (typeof profile.capabilities !== "number" || (profile.capabilities & CapabilitiesUI.Chat) !== 0)
  );
}

/**
 * The profile id the project's own `Chat` assignment names, or `null` when
 * it has none. A 404 is "none". A 403 is read as "none" too: the .NET side
 * lets only the project owner, Project managers and Content creators read a
 * scoped assignment (`FileSecurity.CanUseAi`), so for a plain member the
 * project runs on the portal default until the server serves the model to
 * every member.
 */
export async function readProjectProfileId(projectId: string | number): Promise<string | null> {
  try {
    const raw = await aiService.get(`${PATH}/${ActionType.Chat}`, {
      query: { entityId: String(projectId) },
    });
    return typeof raw === "string" && raw.length > 0 ? raw : null;
  } catch (err) {
    if (err instanceof AiServiceHttpError && err.status === 404) {
      return null;
    }
    if (err instanceof AiServiceHttpError && err.status === 403) {
      logger.debug(
        `AI project ${projectId}: the caller may not read the project's model, the portal default applies`,
      );
      return null;
    }
    throw err;
  }
}

// A profile that does not exist or cannot chat would be stored fine and
// then fail every round with model_not_found, so it is checked up front.
async function assertUsableChatProfile(profileId: string): Promise<void> {
  if (!UUID_PATTERN.test(profileId)) {
    badRequest("profileId must be a UUID or null");
  }
  const profile = await storage.profiles.readById(profileId);
  if (!profile) {
    badRequest(`AI profile "${profileId}" does not exist`);
  }
  if (!canChat(profile)) {
    badRequest(`AI profile "${profileId}" does not support chat`);
  }
}

async function upsertProjectProfileId(projectId: string, profileId: string): Promise<void> {
  await aiService.put(PATH, { assignments: { [ActionType.Chat]: profileId }, entityId: projectId });
}

/**
 * Store the project's model. A profile id must name an existing profile
 * that can chat (400 otherwise) and is upserted as the project's scoped
 * `Chat` assignment. `null` asks for the portal default.
 */
export async function writeProjectProfileId(
  projectId: string | number,
  profileId: string | null,
): Promise<void> {
  const entityId = String(projectId);
  if (profileId !== null) {
    await assertUsableChatProfile(profileId);
    await upsertProjectProfileId(entityId, profileId);
    invalidateChatContext("assignments");
    return;
  }
  const current = await readProjectProfileId(entityId);
  if (current === null) {
    return;
  }
  // The .NET side has no scoped delete: `DELETE assignments/{type}` takes no
  // entityId and would remove the portal-wide assignment instead. So the
  // project is pinned to the portal default as it is right now - a later
  // change of the portal default does not reach this project until its
  // model is set again. With no portal default to pin, the scoped
  // assignment is left as it is.
  const portalDefault = await storage.assignments.readByType(ActionType.Chat);
  if (!portalDefault) {
    logger.warn(
      `AI project ${entityId}: no portal Chat model to reset to, the project keeps ${current}`,
    );
    return;
  }
  if (portalDefault !== current) {
    await upsertProjectProfileId(entityId, portalDefault);
    invalidateChatContext("assignments");
  }
}
