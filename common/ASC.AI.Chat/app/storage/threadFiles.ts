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

// The files of one conversation. A conversation of an AI project (its
// `entityId`) keeps the files its replies produce in a folder of its own
// inside the project, served by the AI service:
// `POST internal/ai/threads/{id}/artifacts` stores one, and the Files service
// lists them (`GET api/2.0/files/rooms/{id}/threads/{threadId}/artifacts`).
// The files the user attached have a folder beside it, filled by the Files
// service when they are bound to a message (see projectAttachments.ts) and
// listed by `GET api/2.0/files/rooms/{id}/threads/{threadId}/attachments`.

import { aiServiceBaseUrl, AiServiceHttpError, withTimeout } from "./httpClient.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getEntityId, getString, isObject } from "../narrow.js";
import logger from "../log.js";

export interface ThreadArtifact {
  /** The ID of the new DocSpace file. */
  id: string;
  title: string;
}

/**
 * Stores a file a reply produced among the conversation's artifacts, with
 * the caller's credentials. Answers `undefined` when the conversation takes
 * none - it belongs to no AI project (400), is not the caller's or lies in
 * a project the caller may not use AI in (403), or is gone (404) - so the
 * caller can keep the file where it kept it before. Other failures throw.
 */
export async function uploadThreadArtifact(
  threadId: string,
  title: string,
  bytes: Uint8Array,
  mediaType: string,
): Promise<ThreadArtifact | undefined> {
  const form = new FormData();
  form.append("title", title);
  form.append("file", new Blob([bytes], { type: mediaType }), title);

  const url = `${aiServiceBaseUrl}/internal/ai/threads/${encodeURIComponent(threadId)}/artifacts`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    // No Content-Type: `fetch` derives the multipart boundary from the body.
    const res = await fetch(url, {
      method: "POST",
      headers: getForwardedHeaders(),
      body: form,
      signal,
    });
    if (res.status === 400 || res.status === 403 || res.status === 404) {
      logger.info(`uploadThreadArtifact: thread ${threadId} takes no artifacts (${res.status})`);
      return undefined;
    }
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    const json: unknown = await res.json();
    const file = isObject(json) && isObject(json["response"]) ? json["response"] : json;
    const id = isObject(file) ? getEntityId(file, "id") : undefined;
    if (!isObject(file) || id === undefined) {
      throw new Error("the AI service returned an artifact without an id");
    }
    logger.info(
      `uploadThreadArtifact: ${title} -> thread ${threadId} as file ${id} (${bytes.length} bytes)`,
    );
    return { id, title: getString(file, "title") ?? title };
  } finally {
    cancel();
  }
}
