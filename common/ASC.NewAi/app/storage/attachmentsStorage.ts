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
import type { AttachmentsStorage, Attachment } from "@onlyoffice/ai-chat/core";

// Placeholder in-memory store. Replace with an HTTP-backed implementation
// once an AttachmentsStorageController exists in ASC.AI/Server/Api/Integration.
export class InMemoryAttachmentsStorage implements AttachmentsStorage {
  readonly #items = new Map<string, Attachment>();

  async create(input: Omit<Attachment, "id" | "createdAt">): Promise<Attachment> {
    const stored: Attachment = { ...input, id: randomUUID(), createdAt: Date.now() };
    this.#items.set(stored.id, stored);
    return { ...stored };
  }

  async readById(id: string): Promise<Attachment | null> {
    const a = this.#items.get(id);
    return a ? { ...a } : null;
  }

  async readManyByIds(ids: string[]): Promise<(Attachment | null)[]> {
    return ids.map((id) => {
      const a = this.#items.get(id);
      return a ? { ...a } : null;
    });
  }

  async update(id: string, patch: Partial<Attachment>): Promise<void> {
    const a = this.#items.get(id);
    if (!a) {
      return;
    }
    this.#items.set(id, { ...a, ...patch, id: a.id, createdAt: a.createdAt });
  }

  async delete(id: string): Promise<void> {
    this.#items.delete(id);
  }

  async deleteMany(ids: string[]): Promise<void> {
    for (const id of ids) {
      this.#items.delete(id);
    }
  }

  async deleteByMessage(messageId: string): Promise<void> {
    for (const [id, a] of this.#items.entries()) {
      if (a.messageId === messageId) {
        this.#items.delete(id);
      }
    }
  }

  async deleteByThread(threadId: string): Promise<void> {
    for (const [id, a] of this.#items.entries()) {
      if (a.threadId === threadId) {
        this.#items.delete(id);
      }
    }
  }

  _clear(): void {
    this.#items.clear();
  }
}
