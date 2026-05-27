// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import { randomUUID } from "node:crypto";

import { aiService, AiServiceHttpError, proxyBaseUrl } from "./httpClient.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getNumber, getString, isObject } from "../narrow.js";
import logger from "../log.js";
import type { AttachmentsStorage, Attachment } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/attachments";

// In-memory cache for raw-payload drafts (device upload, dnd) that arrive
// without a DocSpace entry id. The C# backend currently has no endpoint
// for raw content, so we synthesize an Attachment record server-side and
// keep it here until the message is sent (or the process restarts —
// drafts don't survive restarts, which is acceptable for a draft).
//
// Lifetime: from `createMany` (or `create`) until `delete`/`deleteMany`
// removes the id. `readById`/`readManyByIds` serve from this map first so
// the chat-widget's image preview can pull the base64 payload back.
const rawAttachmentCache = new Map<string, Attachment>();

// DocSpace pre-signed URLs come back as host-relative paths
// (`/storage/files/...`). `fetch()` in Node refuses relative URLs, so
// resolve them against the DocSpace portal root (same host the AI service
// is proxied through).
function resolveAbsoluteUrl(url: string): string {
  if (/^https?:\/\//i.test(url)) {
    return url;
  }
  return new URL(url, proxyBaseUrl.endsWith("/") ? proxyBaseUrl : `${proxyBaseUrl}/`).toString();
}

// Quick mime sniff by magic bytes; falls back to the `Content-Type` response
// header. Providers reject `data:application/octet-stream;…` for image_url so
// we want a real image/* mime when possible.
function detectImageMime(bytes: Uint8Array, fallback: string | null): string {
  if (bytes.length >= 8
    && bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47) {
    return "image/png";
  }
  if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) {
    return "image/jpeg";
  }
  if (bytes.length >= 4 && bytes[0] === 0x47 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x38) {
    return "image/gif";
  }
  if (bytes.length >= 12
    && bytes[0] === 0x52 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x46
    && bytes[8] === 0x57 && bytes[9] === 0x45 && bytes[10] === 0x42 && bytes[11] === 0x50) {
    return "image/webp";
  }
  if (fallback && fallback.startsWith("image/")) {
    return fallback;
  }
  return "image/png";
}

async function fetchImageAsDataUrl(url: string): Promise<string | null> {
  try {
    // DocSpace pre-signed URLs are usually host-relative (`/storage/...`);
    // resolve against the portal root. Forward auth cookies in case the URL
    // still requires the caller's session (some DocSpace setups gate file
    // streams on the user, not the signature).
    const absolute = resolveAbsoluteUrl(url);
    const res = await fetch(absolute, { headers: getForwardedHeaders() });
    if (!res.ok) {
      logger.warn(`fetchImageAsDataUrl: ${absolute} → ${res.status} ${res.statusText}`);
      return null;
    }
    const buf = new Uint8Array(await res.arrayBuffer());
    const mime = detectImageMime(buf, res.headers.get("content-type"));
    const b64 = Buffer.from(buf).toString("base64");
    return `data:${mime};base64,${b64}`;
  } catch (err) {
    logger.error(`fetchImageAsDataUrl: ${url} failed: ${err instanceof Error ? err.message : String(err)}`);
    return null;
  }
}

// Providers (OpenAI / Anthropic / …) require image_url to be either a public
// URL the model can fetch or a `data:image/*;base64,…` payload. C# returns a
// DocSpace pre-signed URL — fine for previews, useless for the LLM. Inline
// the bytes here so `Attachment.base64` is always a real data URL.
async function inlineImagesAsync(attachments: (Attachment | null)[]): Promise<void> {
  const tasks: Promise<void>[] = [];
  for (const a of attachments) {
    if (!a || a.kind !== "image") {
      continue;
    }
    const src = a.base64;
    if (!src || src.startsWith("data:")) {
      continue;
    }
    tasks.push(
      (async () => {
        const dataUrl = await fetchImageAsDataUrl(src);
        if (dataUrl) {
          a.base64 = dataUrl;
        }
      })(),
    );
  }
  if (tasks.length > 0) {
    await Promise.all(tasks);
  }
}

// The C# `AttachmentsStorageController` exposes a DocSpace-specific shape
// (`POST /integration/attachments { entryIds: [...] }`) and does not provide
// `update`, `deleteByMessage`, or `deleteByThread`. The fields `messageId`,
// `threadId`, and `entityId` aren't carried in `AttachmentDto`.
// Cascade-on-message/thread cleanup is expected to happen server-side.
// Methods missing from the backend are no-ops here with a warning log.

function dtoToAttachment(raw: unknown): Attachment | null {
  if (!isObject(raw)) {
    return null;
  }
  const id = getString(raw, "id");
  const title = getString(raw, "title");
  const kindRaw = getString(raw, "kind");
  const createdAt = getNumber(raw, "createdAt");
  if (id === undefined || title === undefined || kindRaw === undefined) {
    return null;
  }
  const kind = kindRaw.toLowerCase() === "image" ? "image" : "file";
  const result: Attachment = {
    id,
    kind,
    title,
    createdAt: createdAt ?? Date.now(),
  };
  const content = getString(raw, "content");
  if (content !== undefined) {
    result.content = content;
  }
  const base64 = getString(raw, "dataUrl") ?? getString(raw, "base64");
  if (base64 !== undefined) {
    result.base64 = base64;
  }
  // C# echoes the DocSpace entry id (internal int or thirdparty string,
  // both serialized as string) in `entryId`. The chat widget's history
  // chip renders the displayed name via `basename(path)`, so compose the
  // path as `${entryId}/${title}` — keeps the entry id available for
  // openFile/cascade lookups (split on "/") while making basename yield
  // the file title.
  const entryId = getString(raw, "entryId");
  if (entryId !== undefined) {
    result.path = title ? `${entryId}/${title}` : entryId;
  }
  // Forward-compat: pick up `type` (ONLYOFFICE file type code) once C#
  // starts echoing it. Today it isn't included in `AttachmentDto`, so the
  // value is back-filled from the original input in `createMany`.
  const type = getNumber(raw, "type");
  if (type !== undefined) {
    result.type = type;
  }
  return result;
}

export class HttpAttachmentsStorage implements AttachmentsStorage {
  async create(input: Omit<Attachment, "id" | "createdAt">): Promise<Attachment> {
    const [result] = await this.createMany([input]);
    if (!result) {
      throw new Error("ai service returned no attachment");
    }
    return result;
  }

  async createMany(
    inputs: Omit<Attachment, "id" | "createdAt">[],
  ): Promise<Attachment[]> {
    if (inputs.length === 0) {
      return [];
    }

    // Synthesize records for raw-payload drafts (no `input.path`). The C#
    // backend can't accept these yet, so we stash them in a server-local
    // cache and serve them back through `readById`/`readManyByIds`. Keeps
    // the chip preview alive in the composer until the message is sent.
    const synthesized: (Attachment | null)[] = inputs.map((input) => {
      if (input.path) return null;
      const id = randomUUID();
      const rec: Attachment = {
        id,
        kind: input.kind,
        title: input.title,
        createdAt: Date.now(),
        ...(input.content !== undefined ? { content: input.content } : {}),
        ...(input.base64 !== undefined ? { base64: input.base64 } : {}),
        ...(input.type !== undefined ? { type: input.type } : {}),
      };
      rawAttachmentCache.set(id, rec);
      return rec;
    });

    const docspaceIndices: number[] = [];
    const entryIds: string[] = [];
    inputs.forEach((input, i) => {
      if (synthesized[i]) return;
      docspaceIndices.push(i);
      entryIds.push(input.path ?? "");
    });

    if (entryIds.length === 0) {
      // All inputs were raw-payload — skip the C# round-trip entirely.
      return synthesized.map((rec, i) => {
        if (!rec) {
          throw new Error(`createMany: missing synthesized record at index ${i}`);
        }
        return rec;
      });
    }

    const raw = await aiService.post(PATH, { entryIds });
    logger.debug(
      `HttpAttachmentsStorage.createMany: POST ${PATH} entryIds=${JSON.stringify(entryIds)} `
        + `raw response=${JSON.stringify(raw)}`,
    );
    if (!Array.isArray(raw)) {
      logger.error(
        `HttpAttachmentsStorage.createMany: backend returned non-array payload `
          + `(type=${raw === null ? "null" : typeof raw}); raw=${JSON.stringify(raw)}`,
      );
      throw new Error("ai service returned a non-array response for attachments createMany");
    }
    // C# `AttachmentsStorageService` groups output by file kind (internal then
    // thirdparty) and so doesn't preserve input order in mixed-kind batches.
    // `dtoToAttachment` composes `path = "${entryId}/${title}"`; split on the
    // first slash to get the original entry id for the order-aware match.
    const byEntryId = new Map<string, Attachment>();
    const skipped: unknown[] = [];
    for (const item of raw) {
      const a = dtoToAttachment(item);
      if (!a || !a.path) {
        skipped.push(item);
        continue;
      }
      const entryIdKey = a.path.split("/", 1)[0] ?? "";
      byEntryId.set(entryIdKey, a);
    }
    if (skipped.length > 0) {
      logger.warn(
        `HttpAttachmentsStorage.createMany: ${skipped.length} item(s) skipped `
          + `(missing id/title/kind/entryId); skipped=${JSON.stringify(skipped)}`,
      );
    }
    const result = inputs.map((input, i) => {
      const stub = synthesized[i];
      if (stub) return stub;

      const docspaceIdx = docspaceIndices.indexOf(i);
      const entryId = entryIds[docspaceIdx] ?? "";
      const matched = byEntryId.get(entryId);
      if (!matched) {
        logger.error(
          `HttpAttachmentsStorage.createMany: no match for entryId=${entryId}. `
            + `requested=${JSON.stringify(entryIds)} `
            + `backend entryIds=${JSON.stringify([...byEntryId.keys()])} `
            + `raw=${JSON.stringify(raw)}`,
        );
        throw new Error(`ai service did not return attachment for entryId=${entryId}`);
      }
      // C# `AttachmentDto` doesn't echo `type` (ONLYOFFICE file type code),
      // so fall back to the value the caller supplied on input. Once C#
      // starts echoing `type`, `dtoToAttachment` will already have set it
      // and we won't overwrite.
      if (matched.type === undefined && input.type !== undefined) {
        matched.type = input.type;
      }
      return matched;
    });
    await inlineImagesAsync(result);
    return result;
  }

  async readById(id: string): Promise<Attachment | null> {
    const cached = rawAttachmentCache.get(id);
    if (cached) return cached;
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      const a = dtoToAttachment(raw);
      if (a) {
        await inlineImagesAsync([a]);
      }
      return a;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readManyByIds(ids: string[]): Promise<(Attachment | null)[]> {
    if (ids.length === 0) {
      return [];
    }

    // Split into cached (raw-payload) vs remote ids — only the latter
    // need to hit C#.
    const remoteIds: string[] = [];
    for (const id of ids) {
      if (!rawAttachmentCache.has(id)) remoteIds.push(id);
    }

    const byId = new Map<string, Attachment>();
    if (remoteIds.length > 0) {
      const raw = await aiService.post(`${PATH}/read`, { ids: remoteIds });
      const list = Array.isArray(raw) ? raw : [];
      for (const item of list) {
        const a = dtoToAttachment(item);
        if (a) {
          byId.set(a.id, a);
        }
      }
    }

    const result = ids.map((id) => rawAttachmentCache.get(id) ?? byId.get(id) ?? null);
    await inlineImagesAsync(result);
    return result;
  }

  async update(id: string, patch: Partial<Attachment>): Promise<void> {
    await this.updateManyByIds([id], patch);
  }

  async updateManyByIds(ids: string[], patch: Partial<Attachment>): Promise<void> {
    if (ids.length === 0) {
      return;
    }
    // The C# side only supports message-binding via `PUT /integration/attachments`
    // — `{ids, messageId}`. Other patches (threadId, entityId, content, etc.)
    // are not actionable on the backend and are silently skipped.
    if (patch.messageId === undefined) {
      logger.debug(
        `HttpAttachmentsStorage.updateManyByIds skipped: no messageId in patch; count=${ids.length}`,
      );
      return;
    }
    await aiService.put(PATH, { ids, messageId: patch.messageId });
  }

  async delete(id: string): Promise<void> {
    if (rawAttachmentCache.delete(id)) {
      return;
    }
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(id)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteMany(ids: string[]): Promise<void> {
    if (ids.length === 0) {
      return;
    }
    const remoteIds: string[] = [];
    for (const id of ids) {
      if (!rawAttachmentCache.delete(id)) {
        remoteIds.push(id);
      }
    }
    if (remoteIds.length === 0) return;
    await aiService.delete(PATH, { body: { ids: remoteIds } });
  }

  async deleteByMessage(messageId: string): Promise<void> {
    // Cascade on message delete is handled server-side; no client-side action.
    logger.debug(
      `HttpAttachmentsStorage.deleteByMessage is a no-op (cascade is server-side); messageId=${messageId}`,
    );
  }

  async deleteByThread(threadId: string): Promise<void> {
    // Cascade on thread delete is handled server-side; no client-side action.
    logger.debug(
      `HttpAttachmentsStorage.deleteByThread is a no-op (cascade is server-side); threadId=${threadId}`,
    );
  }
}
