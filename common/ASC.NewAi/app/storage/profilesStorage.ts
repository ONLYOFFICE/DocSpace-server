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
import { isObject, getString, getNumber, getBoolean } from "../narrow.js";
import type { ProfilesStorage, Profile } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/profiles";

function dtoToProfile(raw: unknown): Profile | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const id = getString(raw, "id");
  const name = getString(raw, "name");
  const providerType = getString(raw, "providerType");
  const baseUrl = getString(raw, "baseUrl");
  const modelId = getString(raw, "modelId");
  const createdAt = getNumber(raw, "createdAt");
  if (
    id === undefined
    || name === undefined
    || providerType === undefined
    || baseUrl === undefined
    || modelId === undefined
    || createdAt === undefined
  ) {
    return undefined;
  }
  const profile: Profile = {
    id,
    name,
    providerType,
    baseUrl,
    modelId,
    createdAt,
  };
  const key = getString(raw, "key");
  if (key !== undefined) {
    profile.key = key;
  }
  const reasoning = getBoolean(raw, "reasoning");
  if (reasoning !== undefined) {
    profile.reasoning = reasoning;
  }
  const capabilities = getNumber(raw, "capabilities");
  if (capabilities !== undefined) {
    profile.capabilities = capabilities;
  }
  return profile;
}

function toCreateBody(input: Omit<Profile, "id" | "createdAt"> | Profile): Record<string, unknown> {
  return {
    name: input.name,
    providerType: input.providerType,
    baseUrl: input.baseUrl,
    key: input.key ?? null,
    modelId: input.modelId,
    reasoning: input.reasoning ?? null,
    capabilities: input.capabilities ?? null,
  };
}

export class HttpProfilesStorage implements ProfilesStorage {
  async create(profile: Omit<Profile, "id" | "createdAt">): Promise<Profile> {
    const raw = await aiService.post(PATH, toCreateBody(profile));
    const result = dtoToProfile(raw);
    if (!result) {
      throw new Error("ai service returned invalid profile");
    }
    return result;
  }

  async createMany(profiles: Omit<Profile, "id" | "createdAt">[]): Promise<Profile[]> {
    const raw = await aiService.post(`${PATH}/batch`, {
      profiles: profiles.map(toCreateBody),
    });
    if (!Array.isArray(raw)) {
      return [];
    }
    const result: Profile[] = [];
    for (const item of raw) {
      const profile = dtoToProfile(item);
      if (profile) {
        result.push(profile);
      }
    }
    return result;
  }

  async readById(id: string): Promise<Profile | undefined> {
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      return dtoToProfile(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return undefined;
      }
      throw err;
    }
  }

  async readAll(): Promise<Profile[]> {
    const raw = await aiService.get(PATH);
    if (!Array.isArray(raw)) {
      return [];
    }
    const result: Profile[] = [];
    for (const item of raw) {
      const profile = dtoToProfile(item);
      if (profile) {
        result.push(profile);
      }
    }
    return result;
  }

  async update(profile: Profile): Promise<void> {
    await aiService.put(`${PATH}/${encodeURIComponent(profile.id)}`, toCreateBody(profile));
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
