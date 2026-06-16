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

import { aiService, AiServiceHttpError } from "./httpClient.js";
import { isObject, getString, getNumber } from "../narrow.js";
import type { PromptsStorage, Prompt } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/prompts";
const FOLDERS_PATH = "/integration/prompt-folders";

function parsePrompt(raw: unknown): Prompt | null {
  if (!isObject(raw)) {
    return null;
  }
  const id = getString(raw, "id");
  const name = getString(raw, "name");
  const text = getString(raw, "text");
  const createdAt = getNumber(raw, "createdAt");
  const updatedAt = getNumber(raw, "updatedAt");
  if (!id || name === undefined || text === undefined || createdAt === undefined || updatedAt === undefined) {
    return null;
  }
  const folderId = getString(raw, "folderId");
  const prompt: Prompt = { id, name, text, createdAt, updatedAt };
  if (folderId) {
    prompt.folderId = folderId;
  }
  return prompt;
}

function parsePromptList(raw: unknown): Prompt[] {
  if (!Array.isArray(raw)) {
    throw new Error("AI service returned a non-array prompt list");
  }
  return raw.map((item, i) => {
    const p = parsePrompt(item);
    if (!p) {
      throw new Error(`AI service returned an invalid prompt at index ${i}`);
    }
    return p;
  });
}

export class HttpPromptsStorage implements PromptsStorage {
  async create(input: Omit<Prompt, "id" | "createdAt" | "updatedAt">): Promise<Prompt> {
    const body: Record<string, unknown> = { name: input.name, text: input.text };
    if (input.folderId) {
      body["folderId"] = input.folderId;
    }
    const raw = await aiService.post(PATH, body);
    const prompt = parsePrompt(raw);
    if (!prompt) {
      throw new Error("AI service returned an invalid prompt payload");
    }
    return prompt;
  }

  async createMany(
    prompts: Omit<Prompt, "id" | "createdAt" | "updatedAt">[],
  ): Promise<Prompt[]> {
    if (prompts.length === 0) {
      return [];
    }
    // The C# `POST /integration/prompts/batch` endpoint currently
    // returns `NoContent` and so cannot deliver server-assigned ids
    // back to us. Until it returns the persisted list we fan out as
    // N parallel single-creates — slower for large bundles, but
    // preserves input order and is correct.
    return Promise.all(prompts.map((p) => this.create(p)));
  }

  async readById(id: string): Promise<Prompt | null> {
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      return parsePrompt(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readAll(): Promise<Prompt[]> {
    const raw = await aiService.get(PATH);
    return parsePromptList(raw).sort((a, b) => b.createdAt - a.createdAt);
  }

  async readByFolderId(folderId: string | null): Promise<Prompt[]> {
    if (folderId === null) {
      const all = await this.readAll();
      return all.filter((p) => !p.folderId);
    }
    const raw = await aiService.get(
      `${FOLDERS_PATH}/${encodeURIComponent(folderId)}/prompts`,
    );
    return parsePromptList(raw).sort((a, b) => b.createdAt - a.createdAt);
  }

  async update(
    id: string,
    updates: { name?: string; text?: string; folderId?: string | null },
  ): Promise<void> {
    const body: Record<string, unknown> = {};
    if (updates.name !== undefined) {
      body["name"] = updates.name;
    }
    if (updates.text !== undefined) {
      body["text"] = updates.text;
    }
    if ("folderId" in updates) {
      body["changeFolder"] = true;
      body["folderId"] = updates.folderId ?? null;
    }
    await aiService.put(`${PATH}/${encodeURIComponent(id)}`, body);
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

  async deleteByFolder(folderId: string): Promise<void> {
    // Server-side folder delete cascades; this is a fallback for callers
    // that intentionally want to clear a folder without removing it.
    const prompts = await this.readByFolderId(folderId);
    await Promise.all(prompts.map((p) => this.delete(p.id)));
  }
}
