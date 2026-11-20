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

// A file attached in a chat of an AI project is kept by its conversation.
// The Files service does that for a file that lies in the project room: it
// is moved into the thread's folder of attachments when it is bound to a
// message (`AttachmentsStorageService`, .NET). A file added from the device
// is uploaded into the room by the client. A file picked in the portal lies
// elsewhere, so it is copied into the room here, with the caller's
// credentials, through the chunked upload a project opens to everyone who
// may chat with it - and the copy is what gets attached.
//
// A file that cannot be copied stays attached where it is: one the room
// refuses (a format no text is read from), one over the size held in memory
// here, a third-party file, or any failure on the way.

import { proxyBaseUrl, withTimeout, AiServiceHttpError } from "./httpClient.js";
import { DocspaceApiHttpError, filesApiGet } from "./docspaceFilesApi.js";
import { mediaTypeOf } from "./docspaceFileTransfer.js";
import { FileTooLargeError, readFileBytes } from "./roomSkills.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getEntityId, getNumber, getObject, getString, isObject } from "../narrow.js";
import logger from "../log.js";

/** The largest file copied: its bytes are held in memory on the way. */
export const MAX_COPY_BYTES = 50 * 1024 * 1024;

/** The chunk a portal takes when its settings name none. */
const DEFAULT_CHUNK_BYTES = 10 * 1024 * 1024;

/**
 * How many files are copied at once, in the whole process. A copy holds its
 * file in memory (twice over while the download is assembled), so the number
 * of copies under way, not the number of requests or of files in one, is
 * what bounds the memory they take.
 */
export const MAX_CONCURRENT_COPIES = 2;

const FILE_ID = /^[1-9]\d*$/;

let copiesUnderWay = 0;
const copiesWaiting: (() => void)[] = [];

/** Runs `copy` once one of the {@link MAX_CONCURRENT_COPIES} places is free. */
async function inTurn<T>(copy: () => Promise<T>): Promise<T> {
  if (copiesUnderWay >= MAX_CONCURRENT_COPIES) {
    // The place is handed over by the copy that ends, already counted.
    await new Promise<void>((resolve) => copiesWaiting.push(resolve));
  } else {
    copiesUnderWay += 1;
  }
  try {
    return await copy();
  } finally {
    const next = copiesWaiting.shift();
    if (next) {
      next();
    } else {
      copiesUnderWay -= 1;
    }
  }
}

async function postFiles(
  path: string,
  init: { json?: unknown; form?: FormData },
): Promise<unknown> {
  const url = `${proxyBaseUrl}/api/2.0/files/${path}`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, {
      method: "POST",
      headers:
        init.json === undefined
          ? getForwardedHeaders()
          : { ...getForwardedHeaders(), "Content-Type": "application/json" },
      body: init.json === undefined ? init.form! : JSON.stringify(init.json),
      signal,
    });
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    const raw: unknown = await res.json();
    return isObject(raw) && "response" in raw ? raw["response"] : raw;
  } finally {
    cancel();
  }
}

async function chunkSize(): Promise<number> {
  try {
    const settings = await filesApiGet("settings");
    const size = isObject(settings) ? getNumber(settings, "chunkUploadSize") : undefined;
    return size !== undefined && size > 0 ? size : DEFAULT_CHUNK_BYTES;
  } catch {
    return DEFAULT_CHUNK_BYTES;
  }
}

/**
 * Uploads a file into a folder through an upload session, as the portal's
 * own uploader does: `POST files/{folderId}/session`, then the chunks in
 * order to `POST files/{folderId}/session/{sessionId}`; the answer to the
 * last one carries the new file. A name taken in the folder gets a numbered
 * one.
 */
export async function uploadInChunks(
  folderId: string,
  name: string,
  bytes: Uint8Array,
  mediaType: string,
): Promise<string> {
  const folder = encodeURIComponent(folderId);
  const session = await postFiles(`${folder}/session`, {
    json: { fileName: name, fileSize: bytes.byteLength, relativePath: "", createNewIfExist: true },
  });
  const sessionId = isObject(session) ? getString(session, "id") : undefined;
  if (sessionId === undefined) {
    throw new Error("DocSpace opened an upload session without an id");
  }

  const size = await chunkSize();
  const count = Math.max(1, Math.ceil(bytes.byteLength / size));
  let last: unknown;
  for (let index = 0; index < count; index++) {
    const form = new FormData();
    form.append(
      "file",
      new Blob([bytes.subarray(index * size, (index + 1) * size)], { type: mediaType }),
      name,
    );
    last = await postFiles(`${folder}/session/${encodeURIComponent(sessionId)}`, { form });
  }
  const file = isObject(last) ? (getObject(last, "file") ?? last) : undefined;
  const id = file ? getEntityId(file, "id") : undefined;
  if (id === undefined) {
    throw new Error("DocSpace finished an upload session without a file");
  }
  return id;
}

/**
 * The id to attach for a file of the portal in a chat of the project: the
 * id of its copy in the project room, or the file's own id when it already
 * lies there or cannot be copied.
 */
export async function copyIntoProject(projectId: string, entryId: string): Promise<string> {
  if (!FILE_ID.test(entryId)) {
    return entryId;
  }
  try {
    const info = await filesApiGet(`file/${entryId}`);
    if (!isObject(info)) {
      return entryId;
    }
    const title = getString(info, "title");
    if (title === undefined || getEntityId(info, "folderId") === projectId) {
      return entryId;
    }
    const size = getNumber(info, "pureContentLength");
    if (size !== undefined && size > MAX_COPY_BYTES) {
      logger.info(
        `copyIntoProject: file ${entryId} (${size} bytes) is over the limit, attached where it is`,
      );
      return entryId;
    }
    const copyId = await inTurn(async () => {
      const bytes = await readFileBytes(entryId, MAX_COPY_BYTES);
      return uploadInChunks(projectId, title, bytes, mediaTypeOf(title));
    });
    logger.info(`copyIntoProject: file ${entryId} -> project ${projectId} as file ${copyId}`);
    return copyId;
  } catch (err) {
    if (
      err instanceof FileTooLargeError ||
      err instanceof AiServiceHttpError ||
      err instanceof DocspaceApiHttpError
    ) {
      logger.info(
        `copyIntoProject: file ${entryId} not copied into project ${projectId}, attached where it is: ${err.message}`,
      );
      return entryId;
    }
    throw err;
  }
}

/**
 * The ids to attach for files of the portal in a chat of the project, in
 * the order of `entryIds`. A file named more than once is copied once, and
 * every mention of it gets the same copy.
 */
export async function copyManyIntoProject(
  projectId: string,
  entryIds: readonly string[],
): Promise<string[]> {
  const copies = new Map<string, Promise<string>>();
  for (const id of entryIds) {
    if (!copies.has(id)) {
      copies.set(id, copyIntoProject(projectId, id));
    }
  }
  return Promise.all(entryIds.map((id) => copies.get(id)!));
}
