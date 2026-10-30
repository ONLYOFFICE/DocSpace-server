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

// The rooms linked to an AI project, as the .NET AI service stores them
// (`GET/POST/DELETE internal/ai/projects/{id}/rooms`, AiProjectsController):
// one flat set of room ids per project, each an active Instructions (type 10)
// or Knowledge (type 11) room, served back split by type and sorted by title.
// The client contract (`ai-projects-api`) keeps the rooms inside the
// project's `aiProject` block and in the create / update body as
// `instructionsRoomIds` / `knowledgeRoomIds`; this module composes that
// shape over the .NET routes, and lists the skills and the knowledge files
// of the linked rooms, which the .NET side does not serve yet.

import { aiService, AiServiceHttpError, proxyBaseUrl, withTimeout } from "./httpClient.js";
import { DocspaceApiHttpError } from "./docspaceFilesApi.js";
import { getRoomSkills } from "./roomSkills.js";
import { countFilesApiRead, getForwardedHeaders } from "../requestContext.js";
import {
  getArray,
  getBoolean,
  getEntityId,
  getNumber,
  getObject,
  getObjectArray,
  getString,
  isObject,
  parseInt10,
} from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import logger from "../log.js";

/** `RoomType.InstructionsRoom` / `RoomType.KnowledgeRoom` on the C# side. */
const INSTRUCTIONS_ROOM_TYPE = 10;
const KNOWLEDGE_ROOM_TYPE = 11;

/** `FilterType.FilesOnly` on the C# side. */
const FILTER_FILES_ONLY = 1;
/** Page size of the Files API (`ApiContext.MaxCount`); a larger `count` answers 400. */
const FILES_PAGE_SIZE = 100;
/** Files of a Knowledge room read at most (10 pages); the listing reports `truncated` past it. */
const KNOWLEDGE_FILES_LIMIT = 1000;
/** `VectorizationStatus` on the C# side. */
const VECTORIZATION_COMPLETED = 1;
const VECTORIZATION_FAILED = 2;
/** Room reads run this many at a time. */
const READ_CONCURRENCY = 4;

/** A linked room as `AiProjectRoomDto` carries it. */
export interface AiProjectLinkedRoom {
  id: number;
  title: string;
  logo: JsonObject | null;
}

export interface AiProjectRooms {
  instructionsRooms: AiProjectLinkedRoom[];
  knowledgeRooms: AiProjectLinkedRoom[];
}

/** A non-fatal problem of a create or update, as the client's `aiProject.warnings` shows it. */
export interface AiProjectWarning {
  code: "roomUnavailable" | "modelNotStored";
  roomId: number | null;
  subjectId: null;
  message: string;
}

/** The room lists of a create / update body; `undefined` keeps the current list. */
export interface AiProjectRoomIds {
  instructionsRoomIds?: number[] | undefined;
  knowledgeRoomIds?: number[] | undefined;
}

function parseRoom(raw: unknown): AiProjectLinkedRoom | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const id = getNumber(raw, "id");
  if (id === undefined) {
    return undefined;
  }
  return { id, title: getString(raw, "title") ?? "", logo: getObject(raw, "logo") ?? null };
}

function parseRooms(raw: unknown, key: string): AiProjectLinkedRoom[] {
  if (!isObject(raw)) {
    return [];
  }
  const rooms: AiProjectLinkedRoom[] = [];
  for (const item of getArray(raw, key) ?? []) {
    const room = parseRoom(item);
    if (room) {
      rooms.push(room);
    }
  }
  return rooms;
}

/** The rooms linked to the project (`GET internal/ai/projects/{id}/rooms`). */
export async function readProjectRooms(projectId: string | number): Promise<AiProjectRooms> {
  const raw = await aiService.get(`/projects/${projectId}/rooms`);
  return {
    instructionsRooms: parseRooms(raw, "instructionsRooms"),
    knowledgeRooms: parseRooms(raw, "knowledgeRooms"),
  };
}

/** Links the rooms; rooms already linked are skipped by the server. */
export async function linkProjectRooms(
  projectId: string | number,
  roomIds: number[],
): Promise<void> {
  if (roomIds.length > 0) {
    await aiService.post(`/projects/${projectId}/rooms`, { roomIds });
  }
}

/** Unlinks the rooms; ids not linked are ignored by the server. */
export async function unlinkProjectRooms(
  projectId: string | number,
  roomIds: number[],
): Promise<void> {
  if (roomIds.length > 0) {
    await aiService.delete(`/projects/${projectId}/rooms`, { body: { roomIds } });
  }
}

// The user-facing message of a refused .NET call (the DocSpace envelope's
// `error.message`), or the bare reason phrase.
function upstreamMessage(err: unknown): string {
  if (err instanceof AiServiceHttpError) {
    try {
      const parsed: unknown = JSON.parse(err.body);
      if (isObject(parsed)) {
        const error = getObject(parsed, "error");
        const message = (error && getString(error, "message")) || getString(parsed, "error");
        if (message) {
          return message;
        }
      }
    } catch {
      // Not JSON: fall through to the status text.
    }
    return err.statusText || `AI service ${err.status}`;
  }
  return err instanceof Error ? err.message : String(err);
}

/**
 * The room ids of a create / update body as the client sends them inside
 * `aiProject`: positive integers, each once, in the order sent. An absent or
 * `null` list is `undefined` (keep); `[]` is an empty list (unlink all).
 */
export function roomIdsOf(settings: JsonObject | undefined): AiProjectRoomIds {
  const list = (key: string): number[] | undefined => {
    const value = settings?.[key];
    if (!Array.isArray(value)) {
      return undefined;
    }
    const ids: number[] = [];
    for (const item of value) {
      const id =
        typeof item === "number" ? item : typeof item === "string" ? parseInt10(item) : undefined;
      if (id !== undefined && Number.isInteger(id) && id > 0 && !ids.includes(id)) {
        ids.push(id);
      }
    }
    return ids;
  };
  return {
    instructionsRoomIds: list("instructionsRoomIds"),
    knowledgeRoomIds: list("knowledgeRoomIds"),
  };
}

/**
 * Bring the project's links to the lists the body asks for: the rooms not
 * linked yet are linked, the linked rooms left out of a sent list are
 * unlinked, a list not sent keeps its rooms. The server tells the room
 * types apart, so both kinds go in one call each. A refused link or unlink
 * does not fail the save: the project's other fields are already written,
 * so the server's message comes back as a `roomUnavailable` warning and the
 * answer shows the rooms as they are.
 */
export async function syncProjectRooms(
  projectId: string | number,
  wanted: AiProjectRoomIds,
  current: AiProjectRooms,
): Promise<AiProjectWarning[]> {
  const toAdd: number[] = [];
  const toRemove: number[] = [];
  const diff = (list: number[] | undefined, linked: AiProjectLinkedRoom[]): void => {
    if (list === undefined) {
      return;
    }
    const linkedIds = new Set(linked.map((room) => room.id));
    toAdd.push(...list.filter((id) => !linkedIds.has(id)));
    toRemove.push(...linked.map((room) => room.id).filter((id) => !list.includes(id)));
  };
  diff(wanted.instructionsRoomIds, current.instructionsRooms);
  diff(wanted.knowledgeRoomIds, current.knowledgeRooms);

  const warnings: AiProjectWarning[] = [];
  const attempt = async (op: () => Promise<void>, ids: number[], what: string): Promise<void> => {
    try {
      await op();
    } catch (err) {
      const message = upstreamMessage(err);
      logger.warn(
        `AI project ${projectId}: ${what} of rooms [${ids.join(", ")}] refused: ${message}`,
      );
      warnings.push({
        code: "roomUnavailable",
        roomId: ids.length === 1 ? (ids[0] ?? null) : null,
        subjectId: null,
        message,
      });
    }
  };
  await attempt(() => unlinkProjectRooms(projectId, toRemove), toRemove, "unlink");
  await attempt(() => linkProjectRooms(projectId, toAdd), toAdd, "link");
  return warnings;
}

/**
 * The `aiProject` block of the client contract over the rooms the server
 * stores. No model is stored with a project yet, so `profile` is `null`
 * (the portal default); every linked room the server lists is active and
 * readable by the caller, so `available` is true and `archived` false.
 */
export function aiProjectBlock(
  folder: JsonObject | undefined,
  rooms: AiProjectRooms,
  warnings: AiProjectWarning[] = [],
): JsonObject {
  const security = folder ? getObject(folder, "security") : undefined;
  const toSource = (room: AiProjectLinkedRoom, roomType: number): JsonObject => ({
    id: room.id,
    title: room.title,
    roomType,
    available: true,
    archived: false,
    logo: room.logo,
  });
  const block: JsonObject = {
    profile: null,
    instructionsRooms: rooms.instructionsRooms.map((room) =>
      toSource(room, INSTRUCTIONS_ROOM_TYPE),
    ),
    knowledgeRooms: rooms.knowledgeRooms.map((room) => toSource(room, KNOWLEDGE_ROOM_TYPE)),
    canEditSettings: security ? getBoolean(security, "EditRoom") === true : false,
  };
  if (warnings.length > 0) {
    block["warnings"] = warnings;
  }
  return block;
}

/**
 * The DocSpace envelope of a project (`{ response: FolderDto, ... }`) with
 * the `aiProject` block set on its `response`. Returned as is when the
 * envelope has no object response to extend.
 */
export function withAiProjectBlock(
  envelope: unknown,
  rooms: AiProjectRooms,
  warnings: AiProjectWarning[] = [],
): unknown {
  if (!isObject(envelope)) {
    return envelope;
  }
  const response = getObject(envelope, "response");
  if (!response) {
    return envelope;
  }
  return {
    ...envelope,
    response: { ...response, aiProject: aiProjectBlock(response, rooms, warnings) },
  };
}

/** The id of the project a create answered, from its DocSpace envelope. */
export function projectIdOf(envelope: unknown): string | undefined {
  if (!isObject(envelope)) {
    return undefined;
  }
  const response = getObject(envelope, "response");
  return response ? getEntityId(response, "id") : undefined;
}

async function mapLimit<T, R>(
  items: T[],
  limit: number,
  fn: (item: T) => Promise<R>,
): Promise<R[]> {
  const results: R[] = new Array<R>(items.length);
  let next = 0;
  const worker = async (): Promise<void> => {
    while (next < items.length) {
      const index = next;
      next += 1;
      const item = items[index];
      if (item !== undefined) {
        results[index] = await fn(item);
      }
    }
  };
  await Promise.all(Array.from({ length: Math.min(limit, items.length) }, worker));
  return results;
}

function isAccessError(err: unknown): boolean {
  return err instanceof DocspaceApiHttpError && (err.status === 403 || err.status === 404);
}

/**
 * The skills of every linked Instructions room, for `GET ai/projects/{id}/skills`:
 * the rooms as the server lists them, each with the frontmatter of its
 * Markdown files (`getRoomSkills`). A room whose files cannot be read comes
 * back with `available: false` and no skills, never as a failure.
 */
export async function listProjectSkills(
  projectId: string | number,
): Promise<{ rooms: JsonObject[] }> {
  const { instructionsRooms } = await readProjectRooms(projectId);
  const rooms = await mapLimit(
    instructionsRooms,
    READ_CONCURRENCY,
    async (room): Promise<JsonObject> => {
      try {
        const skills = await getRoomSkills(String(room.id));
        return { id: room.id, title: room.title, logo: room.logo, available: true, skills };
      } catch (err) {
        if (!isAccessError(err)) {
          logger.warn(
            `AI project ${projectId}: skills of room ${room.id} not read: ${String(err)}`,
          );
        }
        return { id: room.id, title: room.title, logo: room.logo, available: false, skills: [] };
      }
    },
  );
  return { rooms };
}

interface KnowledgeFile {
  /** The DocSpace file id, a number as the Files API answers it (the client retries vectorization by it). */
  id: number;
  title: string;
  fileExst: string;
  indexed: boolean;
  vectorizationStatus: number | null;
  security: { Vectorization: boolean };
}

// The files of a Knowledge room (`GET api/2.0/files/{roomId}`, files only),
// read page by page up to the limit, with their vectorization state and the
// room's total.
async function listKnowledgeFiles(
  roomId: number,
): Promise<{ files: KnowledgeFile[]; total: number }> {
  const files: KnowledgeFile[] = [];
  let total = 0;
  for (let startIndex = 0; startIndex < KNOWLEDGE_FILES_LIMIT; startIndex += FILES_PAGE_SIZE) {
    const page = await readKnowledgeFilesPage(roomId, startIndex);
    files.push(...page.files);
    total = page.total;
    if (startIndex + FILES_PAGE_SIZE >= total || page.files.length === 0) {
      break;
    }
  }
  return { files, total };
}

async function readKnowledgeFilesPage(
  roomId: number,
  startIndex: number,
): Promise<{ files: KnowledgeFile[]; total: number }> {
  const params = new URLSearchParams({
    filterType: String(FILTER_FILES_ONLY),
    count: String(FILES_PAGE_SIZE),
    startIndex: String(startIndex),
  });
  const url = `${proxyBaseUrl}/api/2.0/files/${encodeURIComponent(String(roomId))}?${params}`;
  const { signal, cancel } = withTimeout(undefined);
  countFilesApiRead();
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const raw: unknown = await res.json();
    const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
    if (!envelope) {
      throw new Error(`listKnowledgeFiles(${roomId}) -> unparseable response from ${url}`);
    }
    const files: KnowledgeFile[] = [];
    for (const entry of getObjectArray(envelope, "files") ?? []) {
      const id = getNumber(entry, "id");
      const title = getString(entry, "title");
      if (id === undefined || !title) {
        continue;
      }
      const status = getNumber(entry, "vectorizationStatus") ?? null;
      const security = getObject(entry, "security");
      files.push({
        id,
        title,
        fileExst: getString(entry, "fileExst") ?? title.slice(title.lastIndexOf(".")),
        indexed: status === VECTORIZATION_COMPLETED,
        vectorizationStatus: status,
        security: {
          Vectorization: security ? getBoolean(security, "Vectorization") === true : false,
        },
      });
    }
    return { files, total: getNumber(envelope, "total") ?? startIndex + files.length };
  } finally {
    cancel();
  }
}

/**
 * The files of every linked Knowledge room, for `GET ai/projects/{id}/knowledge`:
 * the rooms as the server lists them, each with its files (at most 1000, read
 * in pages of 100, `truncated` when the room holds more), their vectorization state and the
 * room's counters. A room that cannot be read comes back with
 * `available: false` and no files, never as a failure.
 */
export async function listProjectKnowledge(
  projectId: string | number,
): Promise<{ rooms: JsonObject[] }> {
  const { knowledgeRooms } = await readProjectRooms(projectId);
  const rooms = await mapLimit(
    knowledgeRooms,
    READ_CONCURRENCY,
    async (room): Promise<JsonObject> => {
      const base = { id: room.id, title: room.title, logo: room.logo };
      try {
        const { files, total } = await listKnowledgeFiles(room.id);
        return {
          ...base,
          available: true,
          totalFiles: total,
          indexedFiles: files.filter((file) => file.indexed).length,
          failedFiles: files.filter((file) => file.vectorizationStatus === VECTORIZATION_FAILED)
            .length,
          truncated: total > files.length,
          files,
        };
      } catch (err) {
        if (!isAccessError(err)) {
          logger.warn(`AI project ${projectId}: files of room ${room.id} not read: ${String(err)}`);
        }
        return {
          ...base,
          available: false,
          totalFiles: 0,
          indexedFiles: 0,
          failedFiles: 0,
          truncated: false,
          files: [],
        };
      }
    },
  );
  return { rooms };
}
