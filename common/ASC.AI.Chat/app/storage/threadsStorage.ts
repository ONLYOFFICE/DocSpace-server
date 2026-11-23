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
import { DocspaceApiHttpError, resolveAiProjectEntityId } from "./docspaceFilesApi.js";
import { setRoundThreadId } from "../requestContext.js";
import { PAGE_SIZE } from "@onlyoffice/ai-chat/core";
import type { ThreadsStorage, ThreadsCursor } from "@onlyoffice/ai-chat/core";
import type { Thread } from "@onlyoffice/ai-chat/core";
import {
  invalidateChatContext,
  readChatContext,
  reportChatContextMiss,
} from "./chatContextSnapshot.js";

// A thread belongs to the AI project it was started in (`entityId`, the
// project's id) or to none. The C# storage keeps the two apart: a list asked
// for a project answers that project's threads, and a list asked for nothing
// answers the threads of no project. The project is what gives a thread its
// folder of artifacts (see threadFiles.ts).
const PATH = "/threads";

const MAX_PAGES = 1000;

type WireThreadsCursor = { lastEditDate: number; id: string };

type ThreadsPage = { items: Thread[]; next: WireThreadsCursor | null };

function parseThreadsPage(raw: unknown): ThreadsPage {
  const rawItems = Array.isArray(raw) ? raw : isObject(raw) ? (getArray(raw, "items") ?? []) : [];
  const items: Thread[] = [];
  for (const item of rawItems) {
    const thread = dtoToThread(item);
    if (thread) {
      items.push(thread);
    }
  }
  let next: WireThreadsCursor | null = null;
  if (isObject(raw)) {
    const cursor = getObject(raw, "cursor");
    if (cursor) {
      const lastEditDate = getNumber(cursor, "lastEditDate");
      const id = getString(cursor, "id");
      if (lastEditDate !== undefined && id !== undefined) {
        next = { lastEditDate, id };
      }
    }
  }
  return { items, next };
}

export function dtoToThread(raw: unknown): Thread | null {
  if (!isObject(raw)) {
    return null;
  }
  const id = getString(raw, "id");
  const title = getString(raw, "title");
  const lastEditDate = getNumber(raw, "lastEditDate");
  if (id === undefined || title === undefined || lastEditDate === undefined) {
    return null;
  }
  const profileId = getString(raw, "profileId");
  const thread: Thread = {
    threadId: id,
    title,
    lastEditDate,
  };
  if (profileId !== undefined) {
    thread.profileId = profileId;
  }
  return thread;
}

/**
 * The AI project a thread belongs to, or `null`: a thread started anywhere
 * else - a folder, a room, a form's chat in the SDK - belongs to no project
 * and is listed with the caller's other threads. The C# storage would bind a
 * thread to any folder it is handed, so only a project's id is passed on.
 */
async function toProjectId(entityId: string | undefined): Promise<string | null> {
  if (entityId === undefined || !/^[1-9]\d*$/.test(entityId)) {
    return null;
  }
  return (await resolveAiProjectEntityId(entityId)) ?? null;
}

/**
 * The project whose threads a list is read for. A folder the caller cannot
 * open is no project of theirs, so it lists the threads of no project, as a
 * folder that does not exist does: the id of a list is read, never checked.
 * Any other failure of the read stays a failure, since answering the threads
 * of no project for a project that could not be read would show the wrong
 * list as if it were the right one.
 */
async function toListedProjectId(entityId: string | undefined): Promise<string | null> {
  try {
    return await toProjectId(entityId);
  } catch (err) {
    if (err instanceof DocspaceApiHttpError && err.status === 403) {
      return null;
    }
    throw err;
  }
}

export class HttpThreadsStorage implements ThreadsStorage {
  async create(title: string, profileId?: string, entityId?: string): Promise<Thread> {
    const raw = await aiService.post(PATH, {
      title,
      profileId: profileId ?? null,
      entityId: await toProjectId(entityId),
    });
    const thread = dtoToThread(raw);
    if (!thread) {
      throw new Error("ai service returned invalid thread");
    }
    // A round that started without a thread runs in this one from here on.
    setRoundThreadId(thread.threadId);
    return thread;
  }

  async readById(threadId: string): Promise<Thread | null> {
    // The aggregate carries exactly the round's thread; `null` there means
    // the thread is absent or foreign, the same answer the 404 path gives.
    const snapshot = readChatContext("thread");
    if (snapshot && threadId === snapshot.requested.threadId) {
      return snapshot.thread;
    }
    reportChatContextMiss(`threads.readById(${threadId})`);
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(threadId)}`);
      return dtoToThread(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readAll(
    entityId?: string,
    count?: number,
    cursor?: ThreadsCursor,
    query?: string,
  ): Promise<Thread[]> {
    if (cursor && cursor.lastEditDate === null) {
      return [];
    }
    const wireCursor: WireThreadsCursor | undefined =
      cursor && cursor.lastEditDate !== null
        ? { lastEditDate: cursor.lastEditDate, id: cursor.id || cursor.threadId }
        : undefined;

    const projectId = await toListedProjectId(entityId);
    if (count !== undefined) {
      const page = await this.readPage(projectId, count, wireCursor, query);
      return page.items;
    }

    const result: Thread[] = [];
    let next = wireCursor ?? null;
    for (let i = 0; i < MAX_PAGES; i++) {
      const page = await this.readPage(projectId, PAGE_SIZE, next ?? undefined, query);
      result.push(...page.items);
      next = page.next;
      if (!next) {
        break;
      }
    }
    return result;
  }

  private async readPage(
    entityId: string | null,
    count: number,
    cursor: WireThreadsCursor | undefined,
    query: string | undefined,
  ): Promise<ThreadsPage> {
    const params: Record<string, QueryValue> = { count };
    if (entityId !== null) {
      params["entityId"] = entityId;
    }
    if (query) {
      params["query"] = query;
    }
    if (cursor) {
      params["cursor.lastEditDate"] = cursor.lastEditDate;
      params["cursor.id"] = cursor.id;
    }
    const raw = await aiService.get(PATH, { query: params });
    return parseThreadsPage(raw);
  }

  async update(threadId: string, title?: string): Promise<void> {
    if (title === undefined) {
      return;
    }
    await aiService.put(`${PATH}/${encodeURIComponent(threadId)}`, { title });
    invalidateChatContext("thread");
  }

  async touch(
    threadId: string,
    lastEditDate: number,
    updates?: { profileId?: string | null },
  ): Promise<void> {
    const body: { lastEditDate: number; profileId: string | null; clearProfile: boolean } = {
      lastEditDate,
      profileId: null,
      clearProfile: false,
    };
    if (updates && "profileId" in updates) {
      if (updates.profileId === null) {
        body.clearProfile = true;
      } else if (updates.profileId !== undefined) {
        body.profileId = updates.profileId;
      }
    }
    await aiService.patch(`${PATH}/${encodeURIComponent(threadId)}/touch`, body);
    invalidateChatContext("thread");
  }

  async delete(threadId: string): Promise<void> {
    invalidateChatContext("thread");
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(threadId)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
