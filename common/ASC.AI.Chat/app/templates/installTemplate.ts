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
 * bases, which are installed with it, and given the portal's chat model.
 *
 * Every install makes new rooms: nothing is matched against what the portal
 * already holds. A step that fails before the first room fails the install
 * as it is; one that fails after it throws `TemplateInstallIncompleteError`,
 * which carries the step's error and the rooms already made, so that the
 * answer can name them (they stay in the portal). The creation of a room that
 * is not refused with a 4xx and still names no room - a timeout, a dropped
 * connection, a 5xx, a 2xx without an id - throws it as well, for the first
 * room too, with that room as `unconfirmed`: it may exist. The agent's model is the
 * exception: a portal that has no chat model, or does not store it, still
 * gets the agent, and the answer says so in `warnings`. A template that
 * holds a knowledge base is refused with 409 before the first room on a
 * portal where vectorization is not set up, since the Files service makes
 * no Knowledge room there and an agent's Instructions rooms, made first,
 * would be left behind. The agent's
 * instructions are not installed: the Files service has no route that stores
 * them yet, so every agent comes with `instructionsNotSaved` in `warnings`
 * and no request is made. Every room takes its template's colour and cover. An agent
 * carries its template's category as a tag, which is
 * what the Agents list filters categories by; a template's description has
 * nowhere to go in a room and is not installed.
 */

import { portalChatProfileId } from "../storage/aiProjectChatContext.js";
import { mediaTypeOf, uploadFile } from "../storage/docspaceFileTransfer.js";
import { DocspaceApiHttpError, filesApiSend } from "../storage/docspaceFilesApi.js";
import { aiService } from "../storage/httpClient.js";
import { getEntityId, getString, isObject } from "../narrow.js";
import logger from "../log.js";
import { findTemplate, readTemplateFile } from "./catalog.js";
import type { AgentTemplate, SourceTemplate, Template, TemplateCatalog } from "./catalog.js";
import { clientFault } from "../httpErrors.js";
import { mapped, UpstreamShapeError } from "../storage/upstreamShape.js";
import { writeWasRefused } from "../storage/writeOutcome.js";

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
  /**
   * What was skipped: `modelNotSaved`, or `modelUnconfirmed` when the write of the model got
   * no usable answer and the model may be stored; `instructionsNotSaved` for every agent.
   */
  warnings: string[];
}

/** The rooms an installation has made so far. */
export interface InstalledRooms {
  skillSets: InstalledRoom[];
  knowledgeBases: InstalledRoom[];
  /** The agent's room, once it is made. */
  agent?: InstalledRoom;
  /**
   * The room whose creation got an answer that does not say whether it was made
   * ({@link roomWasRefused}): it may exist in the portal under this title, and its id is unknown.
   */
  unconfirmed?: UnconfirmedRoom;
}

export interface UnconfirmedRoom {
  /** The template the room was asked for. */
  templateId: string;
  /** The title the room was asked with. */
  title: string;
}

/**
 * A step of an installation failed after its first room was made, or the creation of a room -
 * the first one included - got an answer that leaves it unknown. `cause` is the step's own
 * error, which decides the status of the answer; `made` lists the rooms that exist and the one
 * that may.
 */
export class TemplateInstallIncompleteError extends Error {
  constructor(
    readonly made: InstalledRooms,
    cause: unknown,
  ) {
    super("The template was installed in part", { cause });
    this.name = "TemplateInstallIncompleteError";
  }
}

/** Whether the installation has made any room yet, or may have. */
function madeAny(made: InstalledRooms): boolean {
  return (
    made.skillSets.length + made.knowledgeBases.length > 0 ||
    made.agent !== undefined ||
    made.unconfirmed !== undefined
  );
}

/**
 * Whether a failed `POST files/rooms` says that no room was made: the Files service refused it
 * with a 4xx. A timeout, a dropped connection, a 5xx and a 2xx that names no room are unknown:
 * the room may exist (ts-ai-failure-paths.md §1, §6).
 */
function roomWasRefused(err: unknown): boolean {
  return err instanceof DocspaceApiHttpError && err.status >= 400 && err.status < 500;
}

/**
 * Makes the room of a template. A failure that leaves the room unknown is noted in `made`
 * before it is thrown, so that the answer says a room may exist that it cannot name.
 */
async function createRoom(
  template: Template,
  roomType: number,
  made: InstalledRooms,
  tags: string[] = [],
): Promise<InstalledRoom> {
  const { id: templateId, name: title, color, cover } = template;
  try {
    const room = await filesApiSend("POST", "rooms", {
      title,
      roomType,
      // The room is drawn as the catalog draws its template.
      ...(color ? { color } : {}),
      ...(cover ? { cover } : {}),
      ...(tags.length > 0 ? { tags } : {}),
    });
    if (!isObject(room)) {
      throw new UpstreamShapeError("POST files/rooms");
    }
    const id = mapped(getEntityId(room, "id"), "POST files/rooms");
    return { templateId, id, title: getString(room, "title") ?? title };
  } catch (err) {
    if (!roomWasRefused(err)) {
      logger.warn(
        `installTemplate: ${templateId}: the room "${title}" may exist, its creation got no usable answer`,
      );
      made.unconfirmed = { templateId, title };
    }
    throw err;
  }
}

/** Makes the room of a skill set or a knowledge base, adds it to `made` and fills it. */
async function installSource(
  template: SourceTemplate,
  made: InstalledRooms,
): Promise<InstalledRoom> {
  const room = await createRoom(template, ROOM_TYPES[template.kind], made);
  (template.kind === "skillSet" ? made.skillSets : made.knowledgeBases).push(room);
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
  // Both lists are checked before the first room is made.
  const skillTemplates = sources(template.skills);
  const knowledgeTemplates = sources(template.knowledges);
  const made: InstalledRooms = { skillSets: [], knowledgeBases: [] };
  let agent: InstalledRoom;
  try {
    for (const source of skillTemplates) {
      await installSource(source, made);
    }
    for (const source of knowledgeTemplates) {
      await installSource(source, made);
    }
    agent = await createRoom(template, ROOM_TYPES.agent, made, [template.category]);
    made.agent = agent;
    const roomIds = [...made.skillSets, ...made.knowledgeBases].map((room) => Number(room.id));
    if (roomIds.length > 0) {
      await filesApiSend("POST", `rooms/${encodeURIComponent(agent.id)}/sources`, { roomIds });
    }
  } catch (err) {
    if (!madeAny(made)) {
      throw err;
    }
    throw new TemplateInstallIncompleteError(made, err);
  }
  const { skillSets, knowledgeBases } = made;

  const warnings: string[] = [];
  // A template names no model, so the agent is given the one the portal
  // chats on now and keeps it when the portal's choice changes later.
  // Set once the write of the model has started: only then can a failure have stored it.
  let writing = false;
  try {
    const profileId = await portalChatProfileId();
    if (profileId) {
      writing = true;
      await filesApiSend("PUT", `rooms/${encodeURIComponent(agent.id)}/model`, { profileId });
    } else {
      logger.warn(`installTemplate: ${template.id}: model not saved (the portal has no chat model)`);
      warnings.push("modelNotSaved");
    }
  } catch (err) {
    // The rooms and the agent are made by now, so a read of the portal's
    // model or a write of it that fails in any way - a refusal, a timeout, a
    // dropped connection - is told as a warning and never loses the caller
    // the ids of what was made. Only a refusal of the write (a 4xx), or a
    // failure before it, says that the model is not stored: after a timeout,
    // a dropped connection or a 5xx of the write it may be
    // (ts-ai-failure-paths.md §6).
    const unconfirmed = writing && !writeWasRefused(err);
    logger.warn(
      `installTemplate: ${template.id}: model ${unconfirmed ? "unconfirmed, its write got no usable answer" : "not saved"} (${
        err instanceof DocspaceApiHttpError
          ? err.status
          : err instanceof Error
            ? err.message
            : String(err)
      })`,
    );
    warnings.push(unconfirmed ? "modelUnconfirmed" : "modelNotSaved");
  }
  // Nothing stores an agent's instructions yet (the Files service has no
  // `rooms/{id}/instructions`), so they are skipped and the caller is told.
  warnings.push("instructionsNotSaved");
  logger.info(`installTemplate: ${template.id} -> agent ${agent.id}`);
  return { ...agent, kind: "agent", skillSets, knowledgeBases, warnings };
}

/** Whether installing the template makes a Knowledge room. */
function makesKnowledgeRoom(template: Template): boolean {
  return template.kind === "agent"
    ? template.knowledges.length > 0
    : template.kind === "knowledgeBase";
}

// The Files service refuses a Knowledge room on a portal without
// vectorization (`CheckKnowledgeRoomCreationAsync`, a 403). It checks the
// provider alone, so settings that only wait for a reset still pass. A
// failed read is not "off": it throws, and nothing is made either way.
async function assertKnowledgeRoomsAllowed(template: Template): Promise<void> {
  const config = await aiService.get("/config");
  // The settings are always an object: another shape is unknown, not "off".
  if (!isObject(config)) {
    throw new UpstreamShapeError("GET config");
  }
  const enabled =
    config["vectorizationEnabled"] === true || config["vectorizationNeedReset"] === true;
  if (!enabled) {
    logger.info(`installTemplate: ${template.id}: refused, vectorization is not set up`);
    throw clientFault(
      409,
      "This template holds a knowledge base, which needs vectorization to be set up on the portal",
    );
  }
}

/** Installs the template with this id, or answers 404 for an id the catalog does not hold. */
export async function installTemplate(
  catalog: TemplateCatalog,
  id: string,
): Promise<InstalledTemplate> {
  const template = findTemplate(catalog, id);
  if (!template) {
    throw clientFault(404, "Template not found");
  }
  if (makesKnowledgeRoom(template)) {
    await assertKnowledgeRoomsAllowed(template);
  }
  if (template.kind === "agent") {
    return installAgent(template, catalog);
  }
  const made: InstalledRooms = { skillSets: [], knowledgeBases: [] };
  try {
    const room = await installSource(template, made);
    return { ...room, kind: template.kind, skillSets: [], knowledgeBases: [], warnings: [] };
  } catch (err) {
    if (!madeAny(made)) {
      throw err;
    }
    throw new TemplateInstallIncompleteError(made, err);
  }
}
