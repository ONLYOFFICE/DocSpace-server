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

// The skills of a DocSpace room: the Markdown files of the `.ai` folder that
// lies in the room's root. `GET api/2.0/files/rooms/{id}/ai` (added on the
// server branch feature/ai-folder) lists that folder in the shape of any
// other folder listing; a room without the folder answers 404, a caller who
// cannot read the room 403 — both propagate as `DocspaceApiHttpError`.
//
// A skill is an Agent Skills Markdown file: YAML frontmatter with `name` and
// `description`, then the instructions. The widget reads the frontmatter of
// every skill once, when the room is connected (`getRoomSkills`), and one
// body on demand through its built-in `read_skill` tool (`getRoomSkill`).
// The skill id is the DocSpace file id, so the body read is a plain file
// read with the caller's own credentials.

import type { RoomSkill } from "@onlyoffice/ai-chat/core";
import { proxyBaseUrl, withTimeout } from "./httpClient.js";
import { countFilesApiRead, getForwardedHeaders } from "../requestContext.js";
import {
  asString,
  getEntityId,
  getNumber,
  getObject,
  getObjectArray,
  getString,
  isObject,
} from "../narrow.js";
import { DocspaceApiHttpError } from "./docspaceFilesApi.js";
import logger from "../log.js";

/** `FilterType.FilesOnly` on the C# side — the listing skips subfolders. */
const FILTER_FILES_ONLY = 1;

/** Page size of the `.ai` listing; a room holds a handful of skills, not thousands. */
const PAGE_SIZE = 100;

/** Pages read at most — a runaway `total` must not turn into an endless loop. */
const MAX_PAGES = 10;

/** Frontmatter reads run this many at a time so a large folder does not fan out into the Files API all at once. */
const READ_CONCURRENCY = 4;

/**
 * Bytes of a skill file read at most. The widget caps a body at 64 KB before
 * it reaches the model; anything far beyond that is not a skill.
 */
const MAX_SKILL_BYTES = 1024 * 1024;

const MARKDOWN_EXTENSION = ".md";

interface AiFolderFile {
  id: string;
  title: string;
}

function isMarkdown(title: string, fileExst: string | undefined): boolean {
  const ext = fileExst ?? title.slice(title.lastIndexOf("."));
  return ext.toLowerCase() === MARKDOWN_EXTENSION;
}

function parseAiFolderPage(raw: unknown): { files: AiFolderFile[]; total: number } | undefined {
  const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
  if (!envelope) {
    return undefined;
  }
  const files: AiFolderFile[] = [];
  for (const entry of getObjectArray(envelope, "files") ?? []) {
    const id = getEntityId(entry, "id");
    const title = getString(entry, "title");
    if (id === undefined || !title) {
      continue;
    }
    if (isMarkdown(title, getString(entry, "fileExst"))) {
      files.push({ id, title });
    }
  }
  return { files, total: getNumber(envelope, "total") ?? files.length };
}

/**
 * Every Markdown file directly inside the room's `.ai` folder, in the
 * server's order. Throws `DocspaceApiHttpError` (404: no room or no `.ai`
 * folder; 403: the caller cannot read the room) like the other Files API
 * reads.
 */
export async function listRoomSkillFiles(roomId: string): Promise<AiFolderFile[]> {
  const files: AiFolderFile[] = [];
  for (let page = 0; page < MAX_PAGES; page += 1) {
    const params = new URLSearchParams({
      filterType: String(FILTER_FILES_ONLY),
      count: String(PAGE_SIZE),
      startIndex: String(page * PAGE_SIZE),
    });
    const url = `${proxyBaseUrl}/api/2.0/files/rooms/${encodeURIComponent(roomId)}/ai?${params}`;
    const { signal, cancel } = withTimeout(undefined);
    countFilesApiRead();
    try {
      const res = await fetch(url, { headers: getForwardedHeaders(), signal });
      if (!res.ok) {
        throw new DocspaceApiHttpError(res.status, res.statusText, url);
      }
      const parsed = parseAiFolderPage(await res.json());
      if (!parsed) {
        logger.warn(`listRoomSkillFiles(${roomId}) -> unparseable response from ${url}`);
        break;
      }
      files.push(...parsed.files);
      if ((page + 1) * PAGE_SIZE >= parsed.total) {
        break;
      }
    } finally {
      cancel();
    }
  }
  return files;
}

// DocSpace pre-signed URLs come back as host-relative paths; `fetch()` in
// Node refuses relative URLs, so resolve them against the portal root.
function resolveAbsoluteUrl(url: string): string {
  if (/^https?:\/\//i.test(url)) {
    return url;
  }
  return new URL(url, proxyBaseUrl.endsWith("/") ? proxyBaseUrl : `${proxyBaseUrl}/`).toString();
}

/**
 * The text of one file, read through `GET api/2.0/files/file/{id}/presigneduri`
 * and the download address it mints — both with the caller's credentials, so
 * a file the caller cannot read is refused by DocSpace, not by us. Capped at
 * `MAX_SKILL_BYTES`.
 */
export async function readFileText(fileId: string): Promise<string> {
  const uriUrl = `${proxyBaseUrl}/api/2.0/files/file/${encodeURIComponent(fileId)}/presigneduri`;
  let downloadUrl: string | undefined;
  {
    const { signal, cancel } = withTimeout(undefined);
    countFilesApiRead();
    try {
      const res = await fetch(uriUrl, { headers: getForwardedHeaders(), signal });
      if (!res.ok) {
        throw new DocspaceApiHttpError(res.status, res.statusText, uriUrl);
      }
      const raw: unknown = await res.json();
      downloadUrl = isObject(raw) ? asString(raw["response"]) : undefined;
    } finally {
      cancel();
    }
  }
  if (!downloadUrl) {
    throw new Error(`No download address for file ${fileId}`);
  }
  const url = resolveAbsoluteUrl(downloadUrl);
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const bytes = new Uint8Array(await res.arrayBuffer());
    return new TextDecoder("utf-8").decode(bytes.subarray(0, MAX_SKILL_BYTES));
  } finally {
    cancel();
  }
}

export interface SkillFrontmatter {
  name?: string;
  description?: string;
}

/**
 * The `name` and `description` of an Agent Skills file's YAML frontmatter —
 * the block between the opening and closing `---` lines. Only flat
 * `key: value` lines are read; a value keeps its inner text, minus a pair of
 * surrounding quotes. A file without frontmatter yields nothing.
 */
export function parseSkillFrontmatter(text: string): SkillFrontmatter {
  const source = text.replace(/^﻿/, "");
  const match = /^---[ \t]*\r?\n([\s\S]*?)\r?\n---[ \t]*(?:\r?\n|$)/.exec(source);
  if (!match) {
    return {};
  }
  const result: SkillFrontmatter = {};
  for (const line of (match[1] ?? "").split(/\r?\n/)) {
    const pair = /^([A-Za-z_][\w-]*)\s*:\s*(.*)$/.exec(line);
    if (!pair) {
      continue;
    }
    const key = (pair[1] ?? "").toLowerCase();
    if (key !== "name" && key !== "description") {
      continue;
    }
    let value = (pair[2] ?? "").trim();
    if (
      value.length >= 2 &&
      ((value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'")))
    ) {
      value = value.slice(1, -1);
    }
    if (value) {
      result[key] = value;
    }
  }
  return result;
}

function stripMarkdownExtension(title: string): string {
  return title.toLowerCase().endsWith(MARKDOWN_EXTENSION)
    ? title.slice(0, -MARKDOWN_EXTENSION.length)
    : title;
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

/**
 * The frontmatter of every skill in the room. A file whose frontmatter has
 * no `name` is listed under its file name; a file that cannot be read is
 * skipped with a log line rather than failing the whole list.
 */
export async function getRoomSkills(roomId: string): Promise<RoomSkill[]> {
  const files = await listRoomSkillFiles(roomId);
  const skills = await mapLimit(
    files,
    READ_CONCURRENCY,
    async (file): Promise<RoomSkill | undefined> => {
      try {
        const frontmatter = parseSkillFrontmatter(await readFileText(file.id));
        return {
          id: file.id,
          name: frontmatter.name ?? stripMarkdownExtension(file.title),
          description: frontmatter.description ?? "",
        };
      } catch (error) {
        logger.warn(
          `getRoomSkills(${roomId}): skipping file ${file.id} (${file.title}): ${String(error)}`,
        );
        return undefined;
      }
    },
  );
  return skills.filter((skill): skill is RoomSkill => skill !== undefined);
}

/**
 * The whole file of one skill — the widget reads it as text either way.
 * The id must name a Markdown file of this room's `.ai` folder: a file id
 * from elsewhere is answered as not found, whatever the caller may read.
 */
export async function getRoomSkill(roomId: string, skillId: string): Promise<string> {
  const files = await listRoomSkillFiles(roomId);
  if (!files.some((file) => file.id === skillId)) {
    throw Object.assign(new Error(`Skill ${skillId} is not in the .ai folder of room ${roomId}`), {
      status: 404,
      expose: true,
    });
  }
  return readFileText(skillId);
}
