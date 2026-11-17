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

/**
 * Template installation: puts a shipped template into the caller's portal
 * through the Files API, with the caller's own credentials, the way the
 * client would by hand. A skill set becomes an Instructions room holding its
 * skills, a knowledge base a Knowledge room holding its files, and an agent
 * an AI project room linked to the rooms of its skill sets and knowledge
 * bases, which are installed with it, and given the portal's chat model and
 * its instructions.
 *
 * Every install makes new rooms: nothing is matched against what the portal
 * already holds. A step the Files service refuses fails the install with its
 * status, and the rooms made before it stay. The agent's model and its
 * instructions are the exceptions: a portal that has no chat model, or does
 * not store one of the two, still gets the agent, and the answer says so in
 * `warnings`. A template's description and category have nowhere to go in a
 * room and are not installed.
 */

import { portalChatProfileId } from "../storage/aiProjectChatContext.js";
import { mediaTypeOf, uploadFile } from "../storage/docspaceFileTransfer.js";
import { DocspaceApiHttpError, filesApiSend } from "../storage/docspaceFilesApi.js";
import { getEntityId, getString, isObject } from "../narrow.js";
import logger from "../log.js";
import { findTemplate, readTemplateFile } from "./catalog.js";
import type { AgentTemplate, SourceTemplate, Template, TemplateCatalog } from "./catalog.js";

/** `RoomType` on the C# side. */
const ROOM_TYPES = { skillSet: 10, knowledgeBase: 11, agent: 12 } as const;

export interface InstalledRoom {
  /** The template the room was made from. */
  templateId: string;
  /** The ID of the new room. */
  id: string;
  title: string;
}

export interface InstalledTemplate extends InstalledRoom {
  kind: Template["kind"];
  /** The Instructions rooms made for an agent's skill sets; empty otherwise. */
  skillSets: InstalledRoom[];
  /** The Knowledge rooms made for an agent's knowledge bases; empty otherwise. */
  knowledgeBases: InstalledRoom[];
  /** What was skipped: `modelNotSaved`, `instructionsNotSaved`. */
  warnings: string[];
}

function httpError(status: number, message: string): Error {
  return Object.assign(new Error(message), { status, expose: true });
}

async function createRoom(
  templateId: string,
  title: string,
  roomType: number,
): Promise<InstalledRoom> {
  const room = await filesApiSend("POST", "rooms", { title, roomType });
  const id = isObject(room) ? getEntityId(room, "id") : undefined;
  if (!isObject(room) || id === undefined) {
    throw new Error("DocSpace returned a room without an id");
  }
  return { templateId, id, title: getString(room, "title") ?? title };
}

async function installSource(template: SourceTemplate): Promise<InstalledRoom> {
  const room = await createRoom(template.id, template.name, ROOM_TYPES[template.kind]);
  for (const file of template.files) {
    await uploadFile(room.id, file, await readTemplateFile(template, file), mediaTypeOf(file));
  }
  logger.info(
    `installTemplate: ${template.id} -> room ${room.id} (${template.files.length} files)`,
  );
  return room;
}

async function installAgent(
  template: AgentTemplate,
  catalog: TemplateCatalog,
): Promise<InstalledTemplate> {
  const sources = (ids: string[]): SourceTemplate[] =>
    ids.map((id) => {
      const source = findTemplate(catalog, id);
      if (!source || source.kind === "agent") {
        throw new Error(`Template ${template.id} links an unknown template: ${id}`);
      }
      return source;
    });
  const skillSets: InstalledRoom[] = [];
  for (const source of sources(template.skills)) {
    skillSets.push(await installSource(source));
  }
  const knowledgeBases: InstalledRoom[] = [];
  for (const source of sources(template.knowledges)) {
    knowledgeBases.push(await installSource(source));
  }

  const agent = await createRoom(template.id, template.name, ROOM_TYPES.agent);
  const roomIds = [...skillSets, ...knowledgeBases].map((room) => Number(room.id));
  if (roomIds.length > 0) {
    await filesApiSend("POST", `rooms/${encodeURIComponent(agent.id)}/sources`, { roomIds });
  }

  const warnings: string[] = [];
  // A template names no model, so the agent is given the one the portal
  // chats on now and keeps it when the portal's choice changes later.
  try {
    const profileId = await portalChatProfileId().catch((err: unknown) => {
      logger.warn(
        `installTemplate: ${template.id}: the portal's chat model not read: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      return null;
    });
    if (!profileId) {
      throw new DocspaceApiHttpError(404, "Not Found", "the portal has no chat model");
    }
    await filesApiSend("PUT", `rooms/${encodeURIComponent(agent.id)}/model`, { profileId });
  } catch (err) {
    if (!(err instanceof DocspaceApiHttpError)) {
      throw err;
    }
    logger.warn(`installTemplate: ${template.id}: model not saved (${err.status})`);
    warnings.push("modelNotSaved");
  }
  try {
    await filesApiSend("PUT", `rooms/${encodeURIComponent(agent.id)}/instructions`, {
      instructions: template.instructions,
    });
  } catch (err) {
    if (!(err instanceof DocspaceApiHttpError)) {
      throw err;
    }
    logger.warn(`installTemplate: ${template.id}: instructions not saved (${err.status})`);
    warnings.push("instructionsNotSaved");
  }
  logger.info(`installTemplate: ${template.id} -> agent ${agent.id}`);
  return { ...agent, kind: "agent", skillSets, knowledgeBases, warnings };
}

/** Installs the template with this id, or answers 404 for an id the catalog does not hold. */
export async function installTemplate(
  catalog: TemplateCatalog,
  id: string,
): Promise<InstalledTemplate> {
  const template = findTemplate(catalog, id);
  if (!template) {
    throw httpError(404, "Template not found");
  }
  if (template.kind === "agent") {
    return installAgent(template, catalog);
  }
  const room = await installSource(template);
  return { ...room, kind: template.kind, skillSets: [], knowledgeBases: [], warnings: [] };
}
