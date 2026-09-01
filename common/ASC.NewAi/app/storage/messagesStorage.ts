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
import { isObject, getString, getNumber, getObject, getArray } from "../narrow.js";
import { PAGE_SIZE } from "@onlyoffice/ai-chat/core";
import type { MessagesStorage, MessagesCursor, MessagesDirection } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";

const THREADS_PATH = "/threads";
const MESSAGES_PATH = "/messages";

const MAX_PAGES = 1000;

type WireMessagesCursor = { createdAt: string; id: string };

type MessagesPage = { items: ThreadMessageLike[]; next: WireMessagesCursor | null };

// The library's `MessagesCursor` carries an ISO string, but a message that
// reached the store before this service started sending one (a live round,
// a cached page) still has an epoch-ms `createdAt` — accept both rather
// than silently degrade to an unpaginated read.
function toWireCursor(cursor: MessagesCursor | undefined): WireMessagesCursor | undefined {
  if (!cursor) {
    return undefined;
  }
  const createdAt: unknown = cursor.createdAt;
  if (typeof createdAt === "string" && createdAt.length > 0) {
    return { createdAt, id: cursor.id };
  }
  if (typeof createdAt === "number") {
    return { createdAt: new Date(createdAt).toISOString(), id: cursor.id };
  }
  return undefined;
}

function parseMessagesPage(raw: unknown): MessagesPage {
  const rawItems = Array.isArray(raw) ? raw : isObject(raw) ? (getArray(raw, "items") ?? []) : [];
  const items: ThreadMessageLike[] = [];
  for (const item of rawItems) {
    const message = dtoToMessage(item);
    if (message) {
      items.push(message);
    }
  }
  let next: WireMessagesCursor | null = null;
  if (isObject(raw)) {
    const cursor = getObject(raw, "cursor");
    if (cursor) {
      const createdAt = getString(cursor, "createdAt");
      const id = getString(cursor, "id");
      if (createdAt !== undefined && id !== undefined) {
        next = { createdAt, id };
      }
    }
  }
  return { items, next };
}

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
    // A `Date`, not the raw epoch ms the C# DTO carries — it reaches the
    // browser as an ISO string. The library builds the keyset cursor with
    // `messagesCursorOf`, which keeps only a `Date` or an ISO `string` and
    // yields `createdAt: null` for anything else; a null cursor is dropped
    // below, the same first page comes back, and the page walker stops
    // after 100 messages.
    message["createdAt"] = new Date(timestamp);
  }
  return message;
}

export class HttpMessagesStorage implements MessagesStorage {
  async create(
    threadId: string,
    message: Omit<ThreadMessageLike, "id" | "createdAt">,
  ): Promise<ThreadMessageLike> {
    const raw = await aiService.post(`${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`, {
      contents: serializeContents(message),
    });
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

  // The thread a message belongs to, or null when the message does not
  // exist. `ThreadMessageLike` has no thread field, so `readById` drops the
  // C# DTO's `threadId`; attachments/link-to-message needs it to verify the
  // caller-supplied pair (Bug 82771 reopen).
  async readThreadId(messageId: string): Promise<string | null> {
    try {
      const raw = await aiService.get(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`);
      return (isObject(raw) ? getString(raw, "threadId") : undefined) ?? null;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readByThread(
    threadId: string,
    count?: number,
    cursor?: MessagesCursor,
    direction?: MessagesDirection,
  ): Promise<ThreadMessageLike[]> {
    const path = `${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`;
    const wireCursor = toWireCursor(cursor);

    if (count !== undefined) {
      const page = await this.readPage(path, count, wireCursor, direction);
      return page.items;
    }

    // No page size — the caller wants the whole thread. That read is
    // always forward: the pages are concatenated in the order they
    // arrive, so walking backwards would hand back a reversed history.
    const result: ThreadMessageLike[] = [];
    let next = wireCursor ?? null;
    for (let i = 0; i < MAX_PAGES; i++) {
      const page = await this.readPage(path, PAGE_SIZE, next ?? undefined);
      result.push(...page.items);
      next = page.next;
      if (!next) {
        break;
      }
    }
    return result;
  }

  private async readPage(
    path: string,
    count: number,
    cursor: WireMessagesCursor | undefined,
    direction?: MessagesDirection,
  ): Promise<MessagesPage> {
    const params: Record<string, QueryValue> = { count };
    if (direction === "desc") {
      params["direction"] = direction;
    }
    if (cursor) {
      params["cursor.createdAt"] = cursor.createdAt;
      params["cursor.id"] = cursor.id;
    }
    const raw = await aiService.get(path, { query: params });
    return parseMessagesPage(raw);
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
