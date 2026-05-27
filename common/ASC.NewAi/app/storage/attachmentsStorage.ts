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

import { aiService, AiServiceHttpError } from "./httpClient.js";
import { getNumber, getString, isObject } from "../narrow.js";
import logger from "../log.js";
import type { AttachmentsStorage, Attachment } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/attachments";

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
    // The C# endpoint creates attachments from DocSpace file entry ids.
    // `input.path` carries the host-supplied entry id; raw-payload drafts
    // (device uploads, dnd) pass an empty string until the backend grows
    // a raw-content path. Forwarded as-is so the C# side can decide.
    const entryIds: string[] = inputs.map((input) => input.path ?? "");
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
    return inputs.map((input, i) => {
      const entryId = entryIds[i] ?? "";
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
  }

  async readById(id: string): Promise<Attachment | null> {
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      return dtoToAttachment(raw);
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
    const raw = await aiService.post(`${PATH}/read`, { ids });
    const list = Array.isArray(raw) ? raw : [];
    const byId = new Map<string, Attachment>();
    for (const item of list) {
      const a = dtoToAttachment(item);
      if (a) {
        byId.set(a.id, a);
      }
    }
    return ids.map((id) => byId.get(id) ?? null);
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
    await aiService.delete(PATH, { body: { ids } });
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
