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

import { randomUUID } from "crypto";

export class InMemoryMessagesStorage {
  #byId = new Map();
  #byThread = new Map();

  async create(threadId, message) {
    const stored = {
      ...message,
      id: randomUUID(),
      createdAt: Date.now(),
    };
    this.#byId.set(stored.id, { threadId, message: stored });
    if (!this.#byThread.has(threadId)) {
      this.#byThread.set(threadId, []);
    }
    this.#byThread.get(threadId).push(stored);
    return { ...stored };
  }

  async readById(messageId) {
    const entry = this.#byId.get(messageId);
    return entry ? { ...entry.message } : null;
  }

  async readByThread(threadId, limit, startIndex = 0) {
    const list = this.#byThread.get(threadId) ?? [];
    const sorted = [...list].sort((a, b) => (a.createdAt ?? 0) - (b.createdAt ?? 0));
    const start = startIndex ?? 0;
    const end = limit !== undefined ? start + limit : undefined;
    return sorted.slice(start, end).map((m) => ({ ...m }));
  }

  async update(messageId, message) {
    const entry = this.#byId.get(messageId);
    if (!entry) {
      return;
    }
    const updated = { ...message, id: messageId, createdAt: Date.now() };
    entry.message = updated;
    const list = this.#byThread.get(entry.threadId);
    if (list) {
      const idx = list.findIndex((m) => m.id === messageId);
      if (idx >= 0) {
        list[idx] = updated;
      }
    }
  }

  async delete(messageId) {
    const entry = this.#byId.get(messageId);
    if (!entry) {
      return;
    }
    this.#byId.delete(messageId);
    const list = this.#byThread.get(entry.threadId);
    if (list) {
      const idx = list.findIndex((m) => m.id === messageId);
      if (idx >= 0) {
        list.splice(idx, 1);
      }
    }
  }

  async deleteByThread(threadId) {
    const list = this.#byThread.get(threadId) ?? [];
    for (const m of list) {
      this.#byId.delete(m.id);
    }
    this.#byThread.delete(threadId);
  }

  _clear() {
    this.#byId.clear();
    this.#byThread.clear();
  }
}
