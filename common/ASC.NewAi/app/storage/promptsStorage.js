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

export class InMemoryPromptsStorage {
  #prompts = new Map();

  async create(input) {
    const now = Date.now();
    const stored = {
      ...input,
      id: randomUUID(),
      createdAt: now,
      updatedAt: now,
    };
    this.#prompts.set(stored.id, stored);
    return { ...stored };
  }

  async createMany(prompts) {
    for (const p of prompts) {
      this.#prompts.set(p.id, { ...p });
    }
  }

  async readById(id) {
    const p = this.#prompts.get(id);
    return p ? { ...p } : null;
  }

  async readAll() {
    return [...this.#prompts.values()]
      .map((p) => ({ ...p }))
      .sort((a, b) => (b.createdAt ?? 0) - (a.createdAt ?? 0));
  }

  async readByFolderId(folderId) {
    return [...this.#prompts.values()]
      .filter((p) => (folderId === null ? !p.folderId : p.folderId === folderId))
      .map((p) => ({ ...p }))
      .sort((a, b) => (b.createdAt ?? 0) - (a.createdAt ?? 0));
  }

  async update(id, updates) {
    const p = this.#prompts.get(id);
    if (!p) {
      return;
    }
    if (updates.name !== undefined) {
      p.name = updates.name;
    }
    if (updates.text !== undefined) {
      p.text = updates.text;
    }
    if ("folderId" in updates) {
      if (updates.folderId === null) {
        delete p.folderId;
      } else {
        p.folderId = updates.folderId;
      }
    }
    p.updatedAt = Date.now();
  }

  async delete(id) {
    this.#prompts.delete(id);
  }

  async deleteByFolder(folderId) {
    for (const [id, p] of this.#prompts.entries()) {
      if (p.folderId === folderId) {
        this.#prompts.delete(id);
      }
    }
  }

  _clear() {
    this.#prompts.clear();
  }
}
