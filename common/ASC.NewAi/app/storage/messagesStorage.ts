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

import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { isObject, getString, getNumber } from "../narrow.js";
import type { MessagesStorage } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";

const THREADS_PATH = "/integration/threads";
const MESSAGES_PATH = "/integration/messages";

function serializeContents(message: unknown): string {
  return JSON.stringify(message);
}

function parseContents(contents: unknown): Record<string, unknown> {
  if (typeof contents !== "string") {
    if (isObject(contents)) {
      return contents;
    }
    return {};
  }
  try {
    const parsed: unknown = JSON.parse(contents);
    if (isObject(parsed)) {
      return parsed;
    }
    return { content: contents };
  } catch {
    return { content: contents };
  }
}

function dtoToMessage(raw: unknown): ThreadMessageLike | null {
  if (!isObject(raw)) {
    return null;
  }
  const id = getString(raw, "id");
  const timestamp = getNumber(raw, "timestamp");
  if (id === undefined) {
    return null;
  }
  const body = parseContents(raw["contents"]);
  const message: ThreadMessageLike = { ...body, id };
  if (timestamp !== undefined) {
    message["createdAt"] = timestamp;
  }
  return message;
}

export class HttpMessagesStorage implements MessagesStorage {
  async create(
    threadId: string,
    message: Omit<ThreadMessageLike, "id" | "createdAt">,
  ): Promise<ThreadMessageLike> {
    const raw = await aiService.post(
      `${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`,
      { contents: serializeContents(message) },
    );
    const result = dtoToMessage(raw);
    if (!result) {
      throw new Error("ai service returned invalid message");
    }
    return result;
  }

  async readById(messageId: string): Promise<ThreadMessageLike | null> {
    try {
      const raw = await aiService.get(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`);
      return dtoToMessage(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readByThread(
    threadId: string,
    limit?: number,
    startIndex?: number,
  ): Promise<ThreadMessageLike[]> {
    const query: Record<string, QueryValue> = {};
    if (limit !== undefined) {
      query["limit"] = limit;
    }
    if (startIndex !== undefined) {
      query["startIndex"] = startIndex;
    }
    const raw = await aiService.get(
      `${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`,
      Object.keys(query).length > 0 ? { query } : undefined,
    );
    if (!Array.isArray(raw)) {
      return [];
    }
    const result: ThreadMessageLike[] = [];
    for (const item of raw) {
      const m = dtoToMessage(item);
      if (m) {
        result.push(m);
      }
    }
    return result;
  }

  async update(messageId: string, message: ThreadMessageLike): Promise<void> {
    await aiService.put(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`, {
      contents: serializeContents(message),
    });
  }

  async delete(messageId: string): Promise<void> {
    try {
      await aiService.delete(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteByThread(threadId: string): Promise<void> {
    try {
      await aiService.delete(`${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
