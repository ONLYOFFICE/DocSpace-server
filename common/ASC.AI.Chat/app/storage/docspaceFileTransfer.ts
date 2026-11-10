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

// The document agent's files back into DocSpace: a new file in a folder the
// user can write to, through the public Files API under the caller's own
// credentials (`getForwardedHeaders`), so what the user may not create, the
// agent cannot save. The agent reads its inputs itself, through the portal's
// MCP server, by the ids it is handed.

import { proxyBaseUrl, withTimeout, AiServiceHttpError } from "./httpClient.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getEntityId, getString, isObject } from "../narrow.js";
import logger from "../log.js";

export interface UploadedFile {
  /** The new DocSpace entry id (internal int or thirdparty string, as a string). */
  id: string;
  title: string;
}

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
