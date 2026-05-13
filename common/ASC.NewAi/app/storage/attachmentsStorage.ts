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
// `update`, `deleteByMessage`, or `deleteByThread`. The fields `path`,
// `messageId`, `threadId`, and `entityId` aren't carried in `AttachmentDto`.
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
    // `input.path` carries the host-supplied entry id; without it the
    // backend has nothing to look up (e.g. image drafts with raw base64
    // are not supported).
    const entryIds: string[] = [];
    for (const input of inputs) {
      if (!input.path) {
        throw new Error(
          "HttpAttachmentsStorage.createMany requires `input.path` "
            + "(DocSpace entry id) on every item; raw payload attachments "
            + "are not supported by the backend.",
        );
      }
      entryIds.push(input.path);
    }
    const raw = await aiService.post(PATH, { entryIds });
    const list = Array.isArray(raw) ? raw : [];
    // The C# `CreateManyAsync` accepts a HashSet and may not preserve order;
    // re-align the response to the input order by matching on `path`.
    const byPath = new Map<string, Attachment>();
    for (const item of list) {
      const a = dtoToAttachment(item);
      if (!a) {
        continue;
      }
      // The backend echoes the entry id as the attachment's path/title; the
      // mapper above sets `path` to undefined, so fall back to title.
      const key = a.title;
      byPath.set(key, a);
    }
    return entryIds.map((entryId) => {
      const matched = byPath.get(entryId);
      if (!matched) {
        throw new Error(`ai service did not return attachment for entryId=${entryId}`);
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
