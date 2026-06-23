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
import { isObject, getString, getNumber, getBoolean } from "../narrow.js";
import {
  getForwardedHeaders,
  shouldForwardHeadersToProvider,
} from "../requestContext.js";
import type { ProfilesStorage, Profile } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/profiles";

// When the current request opted in (chat-stream handlers) and the profile
// is the host-configured ONLYOFFICE AI provider, merge the forwarded client
// headers into the profile's own headers so they reach the upstream provider
// with each request. The profile's configured headers win over the forwarded
// ones, so an explicit gateway token / Authorization is never clobbered.
function withForwardedProviderHeaders(
  profile: Profile | undefined,
): Profile | undefined {
  if (
    !profile ||
    profile.providerType !== "onlyoffice" ||
    !shouldForwardHeadersToProvider()
  ) {
    return profile;
  }
  return {
    ...profile,
    headers: { ...getForwardedHeaders(), ...(profile.headers ?? {}) },
  };
}

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
  const canUseTool = getBoolean(raw, "canUseTool");
  if (canUseTool !== undefined) {
    profile.canUseTool = canUseTool;
  }
  const useResponsesApi = getBoolean(raw, "useResponsesApi");
  if (useResponsesApi !== undefined) {
    profile.useResponsesApi = useResponsesApi;
  }
  const useProxy = getBoolean(raw, "useProxy");
  if (useProxy !== undefined) {
    profile.useProxy = useProxy;
  }
  const isCloudProvider = getBoolean(raw, "isCloudProvider");
  if (isCloudProvider !== undefined) {
    profile.isCloudProvider = isCloudProvider;
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
    canUseTool: input.canUseTool ?? null,
    useResponsesApi: input.useResponsesApi ?? null,
    useProxy: input.useProxy ?? null,
    isCloudProvider: input.isCloudProvider ?? null,
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
      return withForwardedProviderHeaders(dtoToProfile(raw));
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
