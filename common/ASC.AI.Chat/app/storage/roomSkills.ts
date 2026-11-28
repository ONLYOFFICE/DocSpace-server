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

// The skills of a DocSpace room: the Markdown files in the root of an
// Instructions room (`RoomType.InstructionsRoom`). The .NET side checks each
// file's YAML frontmatter (`name`, `description`) when it is saved, refuses a
// file without a valid one, and keeps the pair in its skills catalog; this
// module reads that catalog through the Files API
// (`GET api/2.0/files/rooms/skills?roomIds=…`) with the caller's credentials,
// so a room the caller cannot read is left out.
//
// The widget reads the catalog of every room once, when the room is
// connected (`getRoomSkills`), and one body on demand through its built-in
// `read_skill` tool (`getRoomSkill`). The skill id is the DocSpace file id,
// so the body read is a plain file read with the caller's own credentials.

import type { ContextFolder, RoomSkill } from "@onlyoffice/ai-chat/core";
import { proxyBaseUrl } from "./httpClient.js";
import { countFilesApiRead, getForwardedHeaders } from "../requestContext.js";
import {
  asString,
  getArray,
  getEntityId,
  getNumber,
  getObject,
  getObjectArray,
  getString,
  isObject,
} from "../narrow.js";
import {
  DocspaceApiHttpError,
  filesApiDownloadBytes,
  filesApiDownloadText,
  filesApiGet,
} from "./docspaceFilesApi.js";
import { clientFault } from "../httpErrors.js";
import { UpstreamShapeError } from "./upstreamShape.js";
import { pooled } from "./pooled.js";

// The body readers moved to the clients' module; kept importable from here for their readers.
export { readBodyCapped } from "./httpClient.js";

/** `RoomType.InstructionsRoom` on the C# side — the rooms whose Markdown files are skills. */
const INSTRUCTIONS_ROOM_TYPE = 10;

/** `RoomType.KnowledgeRoom` on the C# side — the rooms whose files are a searchable knowledge base. */
const KNOWLEDGE_ROOM_TYPE = 11;

/** Page size of the rooms listing (`ApiContext.MaxCount`). */
const PAGE_SIZE = 100;

/** Pages read at most — a runaway `total` must not turn into an endless loop. */
const MAX_PAGES = 10;

/** Rooms per catalog read: `roomIds` takes 1 to 20 ids (`AiHubService.MaxRoomsPerType`). */
const SKILLS_BATCH_SIZE = 20;

/**
 * Requests to the Files API in flight for one listing: the pages of the rooms, then the catalog
 * reads. A portal with a thousand Instructions rooms takes 9 more pages and 50 catalog reads,
 * which went out all at once with the caller's session.
 */
const FILES_API_CONCURRENCY = 4;

/**
 * Bytes of a skill file read at most. The widget cuts a body at 64 K
 * characters before it reaches the model (`SKILL_BODY_CAP`), and a UTF-8
 * character is at most four bytes, so this is the largest download the
 * widget could still use in full; the rest of a longer file is never
 * fetched.
 */
const MAX_SKILL_BYTES = 4 * 64 * 1024;

/**
 * The one cloud DocSpace is to the chat widget. The client connects the
 * current room under the same label (`CONTEXT_ROOM_CLOUD` in ui-kit), so a
 * room picked from the list and the room connected on open compare equal.
 * Never shown: the client lists the rooms without the cloud level.
 */
export const CONTEXT_CLOUD = "docspace";

const CONDITIONAL_HEADERS = new Set([
  "if-none-match",
  "if-modified-since",
  "if-match",
  "if-unmodified-since",
  "if-range",
  "range",
]);

/** A room of the skills catalog with its skills, as `AiSkillsDto` carries it. */
export interface CatalogRoom {
  id: string;
  title: string;
  logo: Record<string, unknown> | null;
  skills: RoomSkill[];
}

function parseSkill(raw: unknown): RoomSkill | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const id = getEntityId(raw, "id");
  const name = getString(raw, "name");
  if (id === undefined || !name) {
    return undefined;
  }
  return { id, name, description: getString(raw, "description") ?? "" };
}

/**
 * The rooms of a catalog answer (`{ rooms: [{ id, title, logo, skills }] }`). An answer without
 * the list is unknown, not a catalog without rooms: it throws {@link UpstreamShapeError}.
 */
export function parseSkillsCatalog(raw: unknown): CatalogRoom[] {
  const entries = isObject(raw) ? getObjectArray(raw, "rooms") : undefined;
  if (entries === undefined) {
    throw new UpstreamShapeError("GET files/rooms/skills");
  }
  const rooms: CatalogRoom[] = [];
  for (const entry of entries) {
    const id = getEntityId(entry, "id");
    if (id === undefined) {
      continue;
    }
    const skills: RoomSkill[] = [];
    for (const item of getArray(entry, "skills") ?? []) {
      const skill = parseSkill(item);
      if (skill) {
        skills.push(skill);
      }
    }
    rooms.push({
      id,
      title: getString(entry, "title") ?? "",
      logo: getObject(entry, "logo") ?? null,
      skills,
    });
  }
  return rooms;
}

/**
 * The skills of the given Instructions rooms, by room id —
 * `GET api/2.0/files/rooms/skills?roomIds=…` (one `roomIds` item per room),
 * 20 rooms a call. A room that does not
 * exist, is not an Instructions room under the AI rooms root, or that the
 * caller cannot read is absent from the map.
 */
export async function getRoomsSkills(roomIds: string[]): Promise<Map<string, RoomSkill[]>> {
  const ids = [...new Set(roomIds)];
  const batches: string[][] = [];
  for (let i = 0; i < ids.length; i += SKILLS_BATCH_SIZE) {
    batches.push(ids.slice(i, i + SKILLS_BATCH_SIZE));
  }
  const answers = await pooled(batches, FILES_API_CONCURRENCY, (batch) =>
    filesApiGet("rooms/skills", { roomIds: batch }),
  );
  const result = new Map<string, RoomSkill[]>();
  for (const answer of answers) {
    for (const room of parseSkillsCatalog(answer)) {
      result.set(room.id, room.skills);
    }
  }
  return result;
}

// The `response` of a rooms listing page: `{ folders: FolderDto[], total }`. A page without the
// list is unknown, not a page without rooms.
function parseRoomsPage(response: unknown): { rooms: ContextFolder["rooms"]; total: number } {
  const folders = isObject(response) ? getObjectArray(response, "folders") : undefined;
  if (!isObject(response) || folders === undefined) {
    throw new UpstreamShapeError("GET files/rooms");
  }
  const rooms: ContextFolder["rooms"] = [];
  for (const entry of folders) {
    const id = getEntityId(entry, "id");
    const name = getString(entry, "title");
    if (id !== undefined && name) {
      rooms.push({ id, name });
    }
  }
  return { rooms, total: getNumber(response, "total") ?? rooms.length };
}

/** The search area of the root that keeps the Instructions and Knowledge rooms. */
const AI_HUB_SEARCH_AREA = "AiHub";

/**
 * The rooms of one type the caller can read — `GET api/2.0/files/rooms?type=…`,
 * paged. Instructions and Knowledge rooms are kept under their own root (the
 * "AI rooms" section, `searchArea=AiHub`), never in Rooms, so the listing
 * asks for that search area.
 */
async function listRoomsOfType(roomType: number): Promise<ContextFolder["rooms"]> {
  const readPage = (page: number): Promise<unknown> =>
    filesApiGet("rooms", {
      type: roomType,
      searchArea: AI_HUB_SEARCH_AREA,
      count: PAGE_SIZE,
      startIndex: page * PAGE_SIZE,
    });
  // The first page tells the total, so the other pages (at most
  // MAX_PAGES - 1 of them) are read FILES_API_CONCURRENCY at a time; they
  // are joined in page order.
  const first = parseRoomsPage(await readPage(0));
  const pageCount = Math.min(Math.ceil(first.total / PAGE_SIZE), MAX_PAGES);
  const rest = Array.from({ length: Math.max(pageCount - 1, 0) }, (_, i) => i + 1);
  // A page that cannot be read fails the listing: the rooms of the pages
  // before it are not the caller's rooms.
  const others = await pooled(rest, FILES_API_CONCURRENCY, async (page) =>
    parseRoomsPage(await readPage(page)),
  );
  return [first, ...others].flatMap((page) => page.rooms);
}

/**
 * The Instructions rooms the caller can read that hold at least one skill
 * file, as one cloud entry — `GET api/2.0/files/rooms?type=10`, paged, then
 * the skills of those rooms, 20 rooms a call (`getRoomsSkills`). The server
 * already leaves out rooms the caller may not see. Empty when there is none,
 * so the widget shows no picker at all.
 */
export async function getContextFolders(): Promise<ContextFolder[]> {
  const rooms = await listRoomsOfType(INSTRUCTIONS_ROOM_TYPE);
  if (rooms.length === 0) {
    return [];
  }
  // An Instructions room with no skill in it connects nothing, so such a
  // room is not offered either.
  const skills = await getRoomsSkills(rooms.map((room) => room.id));
  const offered = rooms.filter((room) => (skills.get(room.id)?.length ?? 0) > 0);
  return offered.length > 0 ? [{ cloud: CONTEXT_CLOUD, rooms: offered }] : [];
}

/**
 * The Knowledge rooms the caller can read, as one cloud entry — `GET
 * api/2.0/files/rooms?type=11`, paged. The composer offers them as the
 * chat's knowledge base; the connected one scopes the .NET knowledge search
 * tool. Empty when there is none, so the widget shows no knowledge row.
 */
export async function getKnowledgeFolders(): Promise<ContextFolder[]> {
  const rooms = await listRoomsOfType(KNOWLEDGE_ROOM_TYPE);
  return rooms.length > 0 ? [{ cloud: CONTEXT_CLOUD, rooms }] : [];
}

// The download address names the portal as the browser sees it: the path
// of the file handler on the host of the request that minted it. Only the
// path and query matter to the handler (the file, the version and its key
// are all in the query), so the fetch goes to the proxy this service already
// reaches the portal through. Fetching the browser-facing host from the
// server itself is what fails on a real domain — a name the machine cannot
// resolve back to itself, a hairpin NAT, a self-signed certificate — while
// the same address works on localhost, where the two hosts coincide. A
// host-relative address (`/storage/files/...`) resolves against the proxy
// the same way.
export function throughProxy(url: string, base: string = proxyBaseUrl): string {
  const root = base.endsWith("/") ? base : `${base}/`;
  if (!/^https?:\/\//i.test(url)) {
    return new URL(url, root).toString();
  }
  const { pathname, search } = new URL(url);
  return new URL(`${pathname}${search}`, root).toString();
}

/**
 * `GET api/2.0/files/file/{id}/presigneduri` with the caller's credentials: the
 * download address it mints, as the portal names it (its public host, or
 * host-relative); see {@link throughProxy} for where to fetch it from.
 */
export async function presignedDownloadUrl(fileId: string): Promise<string> {
  const downloadUrl = asString(
    await filesApiGet(`file/${encodeURIComponent(fileId)}/presigneduri`),
  );
  if (!downloadUrl) {
    throw new UpstreamShapeError("GET files/file/{id}/presigneduri");
  }
  return downloadUrl;
}

/**
 * The text of one file, read through `GET api/2.0/files/file/{id}/presigneduri`
 * and the download address it mints — both with the caller's credentials, so
 * a file the caller cannot read is refused by DocSpace, not by us. Capped at
 * `MAX_SKILL_BYTES`.
 */
export async function readFileText(fileId: string): Promise<string> {
  const url = throughProxy(await presignedDownloadUrl(fileId));
  return filesApiDownloadText(url, downloadHeaders(), MAX_SKILL_BYTES);
}

/** A file the caller may read is larger than the limit its reader set. */
export class FileTooLargeError extends Error {
  constructor(fileId: string, limit: number) {
    super(`file ${fileId} is over the limit of ${limit} bytes`);
    this.name = "FileTooLargeError";
  }
}

/**
 * The file's bytes, as they are in the workspace, under the caller's
 * session: the pre-signed address of {@link presignedDownloadUrl} fetched
 * through the proxy. Read whole, so `limit` caps what is held in memory: a
 * larger file throws {@link FileTooLargeError} as soon as the limit is
 * passed, by the declared length or by the bytes received.
 */
export async function readFileBytes(fileId: string, limit: number): Promise<Uint8Array> {
  const url = throughProxy(await presignedDownloadUrl(fileId));
  const bytes = await filesApiDownloadBytes(url, downloadHeaders(), limit);
  if (bytes === null) {
    throw new FileTooLargeError(fileId, limit);
  }
  return bytes;
}

// The caller's conditional headers describe the widget's request, not the
// file: relayed, they could turn the download into a bodiless 304.
export function downloadHeaders(): Record<string, string> {
  return Object.fromEntries(
    Object.entries(getForwardedHeaders()).filter(
      ([name]) => !CONDITIONAL_HEADERS.has(name.toLowerCase()),
    ),
  );
}

/**
 * The skills of one Instructions room, from the catalog. Throws
 * `DocspaceApiHttpError` 404 when the room is not an Instructions room the
 * caller can read (the catalog leaves such a room out).
 */
export async function getRoomSkills(roomId: string): Promise<RoomSkill[]> {
  const skills = (await getRoomsSkills([roomId])).get(roomId);
  if (!skills) {
    throw new DocspaceApiHttpError(
      404,
      "Not Found",
      `api/2.0/files/rooms/skills?roomIds=${encodeURIComponent(roomId)}`,
    );
  }
  return skills;
}

/**
 * The whole file of one skill — the widget reads it as text either way.
 * The id must name a skill of this Instructions room: a file id from
 * elsewhere is answered as not found, whatever the caller may read.
 */
export async function getRoomSkill(roomId: string, skillId: string): Promise<string> {
  const skills = await getRoomSkills(roomId);
  if (!skills.some((skill) => skill.id === skillId)) {
    throw clientFault(404, `Skill ${skillId} is not a skill of room ${roomId}`);
  }
  return readFileText(skillId);
}
