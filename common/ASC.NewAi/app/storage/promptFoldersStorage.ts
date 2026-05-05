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
import type { PromptFoldersStorage, PromptFolder } from "@onlyoffice/ai-chat/core";

export class InMemoryPromptFoldersStorage implements PromptFoldersStorage {
  readonly #folders = new Map<string, PromptFolder>();

  async create(input: Omit<PromptFolder, "id" | "createdAt" | "updatedAt">): Promise<PromptFolder> {
    const now = Date.now();
    const stored: PromptFolder = {
      ...input,
      id: randomUUID(),
      createdAt: now,
      updatedAt: now,
    };
    this.#folders.set(stored.id, stored);
    return { ...stored };
  }

  async createMany(folders: PromptFolder[]): Promise<void> {
    for (const f of folders) {
      this.#folders.set(f.id, { ...f });
    }
  }

  async readById(id: string): Promise<PromptFolder | null> {
    const f = this.#folders.get(id);
    return f ? { ...f } : null;
  }

  async readAll(): Promise<PromptFolder[]> {
    return [...this.#folders.values()]
      .map((f) => ({ ...f }))
      .sort((a, b) => b.createdAt - a.createdAt);
  }

  async update(id: string, name: string): Promise<void> {
    const f = this.#folders.get(id);
    if (!f) {
      return;
    }
    f.name = name;
    f.updatedAt = Date.now();
  }

  async delete(id: string): Promise<void> {
    this.#folders.delete(id);
  }

  _clear(): void {
    this.#folders.clear();
  }
}
