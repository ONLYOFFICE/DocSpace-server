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

// Whole files in and out of DocSpace for the document agent: the bytes of a
// file the user can read, and a new file in a folder the user can write to.
// Both go through the public Files API under the caller's own credentials
// (`getForwardedHeaders`), so what the user may not open, the agent cannot
// read, and what the user may not create, the agent cannot save.

import { proxyBaseUrl, withTimeout, AiServiceHttpError } from "./httpClient.js";
import { DocspaceApiHttpError, getFileInfo } from "./docspaceFilesApi.js";
import { getForwardedHeaders, countFilesApiRead } from "../requestContext.js";
import { asString, getEntityId, getString, isObject } from "../narrow.js";
import logger from "../log.js";

export interface DownloadedFile {
  /** The DocSpace file title, extension included. */
  title: string;
  bytes: Uint8Array;
  mediaType: string;
}

export interface UploadedFile {
  /** The new DocSpace entry id (internal int or thirdparty string, as a string). */
  id: string;
  title: string;
}

// Request headers that describe the widget's own request, not the file:
// relayed to a download they could turn it into a bodiless 304.
const CONDITIONAL_HEADERS = new Set([
  "if-none-match",
  "if-modified-since",
  "if-match",
  "if-unmodified-since",
  "if-range",
  "range",
]);

const MEDIA_TYPES: Record<string, string> = {
  docx: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  xlsx: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  pptx: "application/vnd.openxmlformats-officedocument.presentationml.presentation",
  pdf: "application/pdf",
  png: "image/png",
  jpg: "image/jpeg",
  jpeg: "image/jpeg",
  txt: "text/plain",
  csv: "text/csv",
  md: "text/markdown",
  json: "application/json",
};

/** The media type a file name implies, or octet-stream. */
export function mediaTypeOf(title: string): string {
  const ext = title.includes(".") ? title.split(".").pop()?.toLowerCase() : undefined;
  return (ext === undefined ? undefined : MEDIA_TYPES[ext]) ?? "application/octet-stream";
}

export class FileTooLargeError extends Error {
  constructor(title: string, size: number, limit: number) {
    super(`file ${title} is ${size} bytes, the limit is ${limit}`);
    this.name = "FileTooLargeError";
  }
}

// A pre-signed download URL comes back host-relative or on the portal's
// public host; either way the bytes are fetched through the proxy the
// service is configured with, the same way `roomSkills.readFileText` does.
function throughProxy(url: string): string {
  const base = proxyBaseUrl.endsWith("/") ? proxyBaseUrl : `${proxyBaseUrl}/`;
  if (!/^https?:\/\//i.test(url)) {
    return new URL(url, base).toString();
  }
  const { pathname, search } = new URL(url);
  return new URL(`${pathname}${search}`, base).toString();
}

/**
 * The bytes of one DocSpace file: `GET files/file/{id}` for the title, then
 * `GET files/file/{id}/presigneduri` and the download address it mints. A
 * body past `maxBytes` is abandoned with {@link FileTooLargeError} once the
 * limit is crossed, so an oversized file costs neither the full download nor
 * the memory.
 */
export async function downloadFile(fileId: string, maxBytes: number): Promise<DownloadedFile> {
  const info = await getFileInfo(fileId);
  if (!info) {
    throw new DocspaceApiHttpError(404, `file ${fileId} not found`, `files/file/${fileId}`);
  }
  const title = info.title ?? `file-${fileId}`;

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
    throw new Error(`no download address for file ${fileId}`);
  }

  const url = throughProxy(downloadUrl);
  const { signal, cancel } = withTimeout(undefined);
  try {
    const headers = Object.fromEntries(
      Object.entries(getForwardedHeaders()).filter(
        ([name]) => !CONDITIONAL_HEADERS.has(name.toLowerCase()),
      ),
    );
    const res = await fetch(url, { headers, signal });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const declared = Number(res.headers.get("content-length") ?? "0");
    if (declared > maxBytes) {
      await res.body?.cancel().catch(() => undefined);
      throw new FileTooLargeError(title, declared, maxBytes);
    }
    const bytes = await readCapped(res, title, maxBytes);
    const mediaType = res.headers.get("content-type")?.split(";")[0]?.trim() || mediaTypeOf(title);
    return { title, bytes, mediaType };
  } finally {
    cancel();
  }
}

async function readCapped(res: Response, title: string, limit: number): Promise<Uint8Array> {
  if (!res.body) {
    return new Uint8Array();
  }
  const reader = res.body.getReader();
  const chunks: Uint8Array[] = [];
  let received = 0;
  let complete = false;
  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }
      received += value.length;
      if (received > limit) {
        throw new FileTooLargeError(title, received, limit);
      }
      chunks.push(value);
    }
    complete = true;
  } finally {
    if (!complete) {
      // The rest of an oversized body is not wanted: let the socket go.
      await reader.cancel().catch(() => undefined);
    }
    reader.releaseLock();
  }
  const out = new Uint8Array(received);
  let offset = 0;
  for (const chunk of chunks) {
    out.set(chunk, offset);
    offset += chunk.length;
  }
  return out;
}

/**
 * A new file in `folderId` through `POST files/{folderId}/insert`, the route
 * the generated-image upload uses. A same-named file gets a numbered title
 * server-side (`createNewIfExist`), nothing is overwritten.
 */
export async function uploadFile(
  folderId: string,
  title: string,
  bytes: Uint8Array,
  mediaType: string,
): Promise<UploadedFile> {
  const form = new FormData();
  form.append("title", title);
  form.append("createNewIfExist", "true");
  form.append("file", new Blob([bytes], { type: mediaType }), title);

  const url = `${proxyBaseUrl}/api/2.0/files/${encodeURIComponent(folderId)}/insert`;
  // No Content-Type: `fetch` derives the multipart boundary from the body.
  const headers = getForwardedHeaders();
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { method: "POST", headers, body: form, signal });
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    const json: unknown = await res.json();
    const file = isObject(json) && "response" in json ? json["response"] : json;
    if (!isObject(file)) {
      throw new Error("DocSpace insert returned an unexpected payload");
    }
    const id = getEntityId(file, "id");
    if (id === undefined) {
      throw new Error("DocSpace insert returned a file without an id");
    }
    const saved = getString(file, "title") ?? title;
    logger.info(
      `uploadFile: ${title} -> folder ${folderId} as entry ${id} (${bytes.length} bytes)`,
    );
    return { id, title: saved };
  } finally {
    cancel();
  }
}
