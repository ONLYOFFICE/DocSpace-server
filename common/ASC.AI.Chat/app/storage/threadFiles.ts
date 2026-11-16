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
// The files the user attached are not copied anywhere: a message carries
// references to attachment records, each naming a DocSpace file where it
// already is, so their list is put together here from the messages.

import { aiServiceBaseUrl, AiServiceHttpError, withTimeout } from "./httpClient.js";
import { DocspaceApiHttpError, filesApiGet } from "./docspaceFilesApi.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getEntityId, getString, isObject } from "../narrow.js";
import logger from "../log.js";

/** Attachment records one `attachments/read` call takes. */
const ATTACHMENTS_BATCH = 50;

export interface ThreadArtifact {
  /** The ID of the new DocSpace file. */
  id: string;
  title: string;
}

/**
 * Stores a file a reply produced among the conversation's artifacts, with
 * the caller's credentials. Answers `undefined` when the conversation takes
 * none - it belongs to no AI project (400), or is not the caller's (404) -
 * so the caller can keep the file where it kept it before. Other failures
 * throw.
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
    if (res.status === 400 || res.status === 404) {
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

export interface ThreadFilesSources {
  /** The attachment references a message carries (`extractAttachmentRefIds`). */
  extractRefs: (message: unknown) => string[];
  messages: { readByThread(threadId: string): Promise<unknown[]> };
  attachments: { readManyByIds(ids: string[]): Promise<({ path?: string } | null)[]> };
}

/** The attachment references of the user's messages, oldest message first, each once. */
export function collectUserAttachmentRefs(
  messages: unknown[],
  extractRefs: (message: unknown) => string[],
): string[] {
  const refs: string[] = [];
  for (const message of messages) {
    // A reply's references are images its tools made: those are artifacts.
    if (!isObject(message) || message["role"] !== "user") {
      continue;
    }
    for (const ref of extractRefs(message)) {
      if (!refs.includes(ref)) {
        refs.push(ref);
      }
    }
  }
  return refs;
}

/** The DocSpace file id of an attachment record: the head of its `<entryId>/<title>` path. */
export function entryIdOf(attachment: { path?: string } | null): string | undefined {
  const entryId = attachment?.path?.split("/")[0];
  return entryId ? entryId : undefined;
}

/**
 * The files the user attached in a conversation, as the Files service
 * describes them now (`GET api/2.0/files/file/{id}`), in the order they were
 * first attached. A file that is gone, or that the caller can no longer
 * read, is left out. A conversation that is not the caller's answers the AI
 * service's own 404.
 */
export async function listThreadAttachments(
  threadId: string,
  sources: ThreadFilesSources,
): Promise<unknown[]> {
  const refs = collectUserAttachmentRefs(
    await sources.messages.readByThread(threadId),
    sources.extractRefs,
  );
  const entryIds: string[] = [];
  for (let start = 0; start < refs.length; start += ATTACHMENTS_BATCH) {
    const records = await sources.attachments.readManyByIds(
      refs.slice(start, start + ATTACHMENTS_BATCH),
    );
    for (const record of records) {
      const entryId = entryIdOf(record);
      if (entryId !== undefined && !entryIds.includes(entryId)) {
        entryIds.push(entryId);
      }
    }
  }
  const files = await Promise.all(
    entryIds.map(async (entryId) => {
      try {
        return await filesApiGet(`file/${encodeURIComponent(entryId)}`);
      } catch (err) {
        if (err instanceof DocspaceApiHttpError && (err.status === 403 || err.status === 404)) {
          return undefined;
        }
        throw err;
      }
    }),
  );
  return files.filter((file) => isObject(file));
}
