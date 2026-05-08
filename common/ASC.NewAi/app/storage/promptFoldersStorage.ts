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
import type { PromptFoldersStorage, PromptFolder } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/prompt-folders";

function parseFolder(raw: unknown): PromptFolder | null {
  if (!isObject(raw)) {
    return null;
  }
  const id = getString(raw, "id");
  const name = getString(raw, "name");
  const createdAt = getNumber(raw, "createdAt");
  const updatedAt = getNumber(raw, "updatedAt");
  if (!id || name === undefined || createdAt === undefined || updatedAt === undefined) {
    return null;
  }
  return { id, name, createdAt, updatedAt };
}

function parseFolderList(raw: unknown): PromptFolder[] {
  if (!Array.isArray(raw)) {
    throw new Error("AI service returned a non-array prompt folder list");
  }
  return raw.map((item, i) => {
    const f = parseFolder(item);
    if (!f) {
      throw new Error(`AI service returned an invalid prompt folder at index ${i}`);
    }
    return f;
  });
}

export class HttpPromptFoldersStorage implements PromptFoldersStorage {
  async create(input: Omit<PromptFolder, "id" | "createdAt" | "updatedAt">): Promise<PromptFolder> {
    const raw = await aiService.post(PATH, { name: input.name });
    const folder = parseFolder(raw);
    if (!folder) {
      throw new Error("AI service returned an invalid prompt folder payload");
    }
    return folder;
  }

  async createMany(
    folders: Omit<PromptFolder, "id" | "createdAt" | "updatedAt">[],
  ): Promise<PromptFolder[]> {
    if (folders.length === 0) {
      return [];
    }
    const raw = await aiService.post(`${PATH}/batch`, {
      names: folders.map((f) => f.name),
    });
    const persisted = parseFolderList(raw);
    if (persisted.length !== folders.length) {
      throw new Error(
        `AI service returned ${persisted.length} folders for ${folders.length} inputs`,
      );
    }
    return persisted;
  }

  async readById(id: string): Promise<PromptFolder | null> {
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      return parseFolder(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readAll(): Promise<PromptFolder[]> {
    const raw = await aiService.get(PATH);
    return parseFolderList(raw).sort((a, b) => b.createdAt - a.createdAt);
  }

  async update(id: string, name: string): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(id)}`, { name });
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
}
