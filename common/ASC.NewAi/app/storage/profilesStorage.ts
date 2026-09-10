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

import { aiService, AiServiceHttpError, aiServiceBaseUrl, withTimeout } from "./httpClient.js";
import { isObject, getString, getNumber, getBoolean, getArray } from "../narrow.js";
import logger from "../log.js";
import {
  countUpstreamCall,
  countUpstreamRead,
  getForwardedHeaders,
  shouldForwardHeadersToProvider,
} from "../requestContext.js";
import { CapabilitiesUI, reasoningSupportFromCatalog } from "@onlyoffice/ai-chat/core";
import type {
  Model,
  OpenRouterReasoningMeta,
  ProfilesStorage,
  Profile,
} from "@onlyoffice/ai-chat/core";
import {
  FULL_REASONING_SUPPORT,
  NO_REASONING_SUPPORT,
  reasoningConfigToSupport,
  supportToReasoningConfig,
  type ReasoningSupport,
} from "./reasoningDepth.js";
import {
  invalidateChatContext,
  readChatContext,
  reportChatContextMiss,
} from "./chatContextSnapshot.js";

const PATH = "/profiles";
const ONLYOFFICE_GATEWAY_PATH = "/api/2.0/ai/gateway";

export function withOnlyofficeProviderOverrides(
  profile: Profile | undefined,
): Profile | undefined {
  if (!profile || profile.providerType !== "onlyoffice") {
    return profile;
  }
  const resolved: Profile = {
    ...profile,
    baseUrl: `${aiServiceBaseUrl}${ONLYOFFICE_GATEWAY_PATH}`,
  };
  if (shouldForwardHeadersToProvider()) {
    resolved.headers = { ...getForwardedHeaders(), ...(profile.headers ?? {}) };
  }
  return resolved;
}

export function dtoToProfile(raw: unknown): Profile | undefined {
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
  // The C# `reasoning` column is a `ReasoningConfig` object (a bare boolean
  // before the depth migration). The widget still reads the legacy boolean
  // as "does this model think at all" and the composer's Effort row follows
  // `reasoningSupport`, so both are derived from the one object.
  const reasoningSupport = reasoningConfigToSupport(raw["reasoning"]);
  if (reasoningSupport !== undefined) {
    profile.reasoning = reasoningSupport.thinks;
    profile.reasoningSupport = reasoningSupport;
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

// The C# storage takes a `ReasoningConfig` object where it used to take a
// boolean. The widget copies the catalogue's `reasoningSupport` onto the
// profile at save time; when only the legacy boolean is set (an older
// client, a profile created through the API) it is widened to the support
// the boolean has always implied — every depth with an off switch, or none.
function toReasoningConfig(
  input: Pick<Profile, "reasoning" | "reasoningSupport">,
): Record<string, unknown> | null {
  if (input.reasoningSupport) {
    return supportToReasoningConfig(input.reasoningSupport);
  }
  if (input.reasoning === undefined) {
    return null;
  }
  return supportToReasoningConfig(input.reasoning ? FULL_REASONING_SUPPORT : NO_REASONING_SUPPORT);
}

function toCreateBody(input: Omit<Profile, "id" | "createdAt"> | Profile): Record<string, unknown> {
  return {
    name: input.name,
    providerType: input.providerType,
    baseUrl: input.baseUrl,
    key: input.key ?? null,
    modelId: input.modelId,
    reasoning: toReasoningConfig(input),
    capabilities: input.capabilities ?? null,
    canUseTool: input.canUseTool ?? null,
    useResponsesApi: input.useResponsesApi ?? null,
    useProxy: input.useProxy ?? null,
    isCloudProvider: input.isCloudProvider ?? null,
  };
}

// The provider a round (or the image tool) is about to call, AFTER the
// onlyoffice override — so `baseUrl` is the portal's own gateway path for
// paid profiles and the raw provider URL otherwise. The override rewrites
// `baseUrl` to the INTERNAL gateway address and merges the caller's
// forwarded auth headers — exactly what in-process provider calls (chat
// engine, openai passthrough) need, and exactly what must never leave the
// process; HTTP responses use `readByIdRaw` instead (Bug 82821). `key` is
// only reported as present/absent; never logged. `source` is the raw DTO
// (HTTP path) or the literal "chat-context" (served from the round snapshot).
function describeReasoningSupport(profile: Profile): string {
  const support = profile.reasoningSupport;
  if (!support) {
    return profile.reasoning === undefined ? "-" : String(profile.reasoning);
  }
  return `${support.thinks ? "thinks" : "no"}/${support.canDisable ? "off" : "always"}/` +
    `[${support.depths.join(",")}]`;
}

function logResolvedProfile(id: string, profile: Profile | undefined, source: unknown): void {
  const via = source === "chat-context" ? " via chat-context" : "";
  logger.info(
    profile
      ? `HttpProfilesStorage.readById(${id})${via} -> providerType=${profile.providerType} ` +
          `model=${profile.modelId} baseUrl=${profile.baseUrl} hasKey=${profile.key !== undefined} ` +
          `capabilities=${profile.capabilities ?? "-"} canUseTool=${profile.canUseTool ?? "-"} ` +
          `reasoning=${describeReasoningSupport(profile)} ` +
          `useProxy=${profile.useProxy ?? "-"} isCloud=${profile.isCloudProvider ?? "-"} ` +
          `headers=[${Object.keys(profile.headers ?? {}).sort().join(",")}]`
      : source === "chat-context"
        ? `HttpProfilesStorage.readById(${id}) via chat-context -> NOT FOUND`
        : `HttpProfilesStorage.readById(${id}) -> UNUSABLE payload (a required field is missing): ` +
            `${JSON.stringify(source).slice(0, 500)}`,
  );
}

export class HttpProfilesStorage implements ProfilesStorage {
  async create(profile: Omit<Profile, "id" | "createdAt">): Promise<Profile> {
    const raw = await aiService.post(PATH, toCreateBody(profile));
    invalidateChatContext("profiles");
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
    invalidateChatContext("profiles");
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
    // A primed round holds the portal's full profile list — the id either
    // resolves there or does not exist for this user.
    const snapshot = readChatContext("profiles");
    if (snapshot) {
      const profile = withOnlyofficeProviderOverrides(
        snapshot.profiles.find((p) => p.id === id),
      );
      logResolvedProfile(id, profile, "chat-context");
      return profile;
    }
    reportChatContextMiss(`profiles.readById(${id})`);
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      const profile = withOnlyofficeProviderOverrides(dtoToProfile(raw));
      logResolvedProfile(id, profile, raw);
      return profile;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        logger.warn(`HttpProfilesStorage.readById(${id}) -> 404 NOT FOUND`);
        return undefined;
      }
      throw err;
    }
  }

  // Same read as `readById` but WITHOUT `withOnlyofficeProviderOverrides`:
  // the profile exactly as the C# storage serves it (public gateway baseUrl
  // on SaaS — the same object `readAll` returns). This is the only variant
  // an HTTP response may echo: the override's internal service address and
  // forwarded auth headers must never reach a client (Bug 82821).
  async readByIdRaw(id: string): Promise<Profile | undefined> {
    const snapshot = readChatContext("profiles");
    if (snapshot) {
      return snapshot.profiles.find((p) => p.id === id);
    }
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      return dtoToProfile(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        logger.warn(`HttpProfilesStorage.readByIdRaw(${id}) -> 404 NOT FOUND`);
        return undefined;
      }
      throw err;
    }
  }

  async readAll(): Promise<Profile[]> {
    const snapshot = readChatContext("profiles");
    if (snapshot) {
      return snapshot.profiles;
    }
    reportChatContextMiss("profiles.readAll");
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
    invalidateChatContext("profiles");
  }

  async delete(id: string): Promise<void> {
    invalidateChatContext("profiles");
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

// ---------------------------------------------------------------------------
// ONLYOFFICE gateway model catalog

const lowered = (values: unknown[] | undefined): string[] =>
  (values ?? []).filter((v): v is string => typeof v === "string").map((v) => v.toLowerCase());

// Mirror of the C# ProfileStorageService.MapCapabilities/HasCapability
// mapping (products/ASC.AI), which builds GET /ai/profiles/list from this
// same catalog: type chat/image -> Chat/Image, an "image" input modality ->
// Vision, an "image" output modality -> Image, "tools" -> Tools, and the
// entry's `reasoning` object (OpenRouter's shape: `mandatory`,
// `supported_efforts`, `default_effort`) -> the profile's `ReasoningConfig`,
// with the "reasoning" capability as the thinks flag where the object is
// null or absent. Embedding models are skipped there too.
function mapGatewayModel(raw: unknown): Model | undefined {
  if (!isObject(raw)) {
    return undefined;
  }
  const id = getString(raw, "id");
  if (id === undefined) {
    return undefined;
  }
  const type = getString(raw, "type")?.toLowerCase();
  if (type === "embedding") {
    return undefined;
  }
  const capabilityNames = lowered(getArray(raw, "capabilities"));
  let capabilities: number = CapabilitiesUI.None;
  if (type === "chat") {
    capabilities |= CapabilitiesUI.Chat;
  } else if (type === "image") {
    capabilities |= CapabilitiesUI.Image;
  }
  if (lowered(getArray(raw, "input_modalities")).includes("image")) {
    capabilities |= CapabilitiesUI.Vision;
  }
  if (lowered(getArray(raw, "output_modalities")).includes("image")) {
    capabilities |= CapabilitiesUI.Image;
  }
  if (capabilityNames.includes("tools")) {
    capabilities |= CapabilitiesUI.Tools;
  }
  const hasReasoningCapability = capabilityNames.includes("reasoning");
  const model: Model = {
    id,
    name: getString(raw, "alias") ?? id,
    provider: "onlyoffice",
    reasoning: hasReasoningCapability,
    capabilities,
  };
  // The catalogue's verdict on extended thinking, when it gives one. The
  // ONLYOFFICE route proxies OpenRouter's catalogue one-to-one, so the
  // library's converter reads the object as-is; a missing field leaves the
  // id-based table to answer. A `null` object means "does not think" to the
  // converter, but the C# mapping still trusts the "reasoning" capability
  // there (thinks, switchable, no depth) — mirrored so both listings agree.
  const reasoningMeta = parseReasoningMeta(raw["reasoning"]);
  let reasoningSupport = reasoningSupportFromCatalog(reasoningMeta, id);
  if (reasoningMeta === null && hasReasoningCapability) {
    reasoningSupport = { thinks: true, canDisable: true, depths: [] } satisfies ReasoningSupport;
  }
  if (reasoningSupport !== undefined) {
    model.reasoningSupport = reasoningSupport;
    model.reasoning = reasoningSupport.thinks;
  }
  return model;
}

function parseReasoningMeta(raw: unknown): OpenRouterReasoningMeta | null | undefined {
  if (raw === null) {
    return null;
  }
  if (!isObject(raw)) {
    return undefined;
  }
  const meta: OpenRouterReasoningMeta = {};
  const mandatory = getBoolean(raw, "mandatory");
  if (mandatory !== undefined) {
    meta.mandatory = mandatory;
  }
  const defaultEnabled = getBoolean(raw, "default_enabled");
  if (defaultEnabled !== undefined) {
    meta.default_enabled = defaultEnabled;
  }
  const efforts = getArray(raw, "supported_efforts");
  if (efforts !== undefined) {
    meta.supported_efforts = efforts.filter((v): v is string => typeof v === "string");
  }
  const defaultEffort = getString(raw, "default_effort");
  if (defaultEffort !== undefined) {
    meta.default_effort = defaultEffort;
  }
  return meta;
}

/**
 * List the ONLYOFFICE gateway model catalog through the portal's gateway
 * proxy (`/api/2.0/ai/gateway/models` — the C# side signs the gateway key
 * and swaps in `customer/models` for paid portals).
 *
 * The onlyoffice provider's own OpenAI-compatible `/models` listing carries
 * bare ids only, so the engine stamps every model with the broad default
 * capability mask — while `GET /ai/profiles/list` synthesizes its answer
 * from this rich catalog. Serving `list-provider-models` from the same
 * catalog keeps the two methods consistent (Bug 83113).
 */
export async function listOnlyofficeGatewayModels(signal?: AbortSignal): Promise<Model[]> {
  const url = `${aiServiceBaseUrl}${ONLYOFFICE_GATEWAY_PATH}/models`;
  countUpstreamRead();
  countUpstreamCall("GET");
  const { signal: reqSignal, cancel } = withTimeout(signal);
  try {
    const res = await fetch(url, {
      headers: { ...getForwardedHeaders() },
      signal: reqSignal,
    });
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    const json: unknown = await res.json();
    const data = isObject(json) ? getArray(json, "data") : undefined;
    if (!data) {
      throw new Error(`gateway models listing returned no data array (${url})`);
    }
    return data
      .map(mapGatewayModel)
      .filter((m): m is Model => m !== undefined);
  } finally {
    cancel();
  }
}
