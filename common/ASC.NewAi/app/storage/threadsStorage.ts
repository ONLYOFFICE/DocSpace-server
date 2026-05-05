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
import { isObject, getString, getNumber } from "../narrow.js";
import type { ThreadsStorage } from "@onlyoffice/ai-chat/core";
import type { Thread } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/threads";

function dtoToThread(raw: unknown): Thread | null {
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

export class HttpThreadsStorage implements ThreadsStorage {
  async create(title: string, profileId?: string): Promise<Thread> {
    const raw = await aiService.post(PATH, {
      title,
      profileId: profileId ?? null,
    });
    const thread = dtoToThread(raw);
    if (!thread) {
      throw new Error("ai service returned invalid thread");
    }
    return thread;
  }

  async readById(threadId: string): Promise<Thread | null> {
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

  async readAll(): Promise<Thread[]> {
    const raw = await aiService.get(PATH);
    if (!Array.isArray(raw)) {
      return [];
    }
    const result: Thread[] = [];
    for (const item of raw) {
      const thread = dtoToThread(item);
      if (thread) {
        result.push(thread);
      }
    }
    return result;
  }

  async update(threadId: string, title?: string): Promise<void> {
    if (title === undefined) {
      return;
    }
    await aiService.put(`${PATH}/${encodeURIComponent(threadId)}`, { title });
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
  }

  async delete(threadId: string): Promise<void> {
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
