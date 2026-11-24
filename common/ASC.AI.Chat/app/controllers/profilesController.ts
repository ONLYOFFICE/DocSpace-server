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

import { ProfilesEngine } from "@onlyoffice/ai-chat/core";
import type {
  Profile,
  CreateProfileInput,
  ProfileMutationResult,
  ProviderType,
} from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { listOnlyofficeGatewayModels } from "../storage/profilesStorage.js";
import { aiService, AiServiceHttpError } from "../storage/httpClient.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { assertAiAccess } from "./aiAccess.js";
import { asString, isObject, readProp } from "../narrow.js";
import { assertSafeBaseUrl } from "../security.js";
import logger from "../log.js";
import type { Response } from "express";
import { clientFault } from "../httpErrors.js";

const engine = new ProfilesEngine({ storage });

// Profiles are read-only when the portal AI gateway is configured (SaaS):
// the C# storage rejects every write with 403 — but only after
// `engine.create`/`update` has already dialed the caller-supplied baseUrl
// to live-validate the key. Check the flag first so no outbound request is
// made for an operation that cannot succeed anyway (Bug 83112).
// `systemAiEnabled` mirrors `AiStatus.GatewayEnabled`
// (AiSettingsService.GetAiSettingsAsync), the same state
// AssertGatewayNotConfigured trips on.
async function assertProfilesWritable(): Promise<void> {
  const config = await aiService.get("/config");
  if (isObject(config) && config["systemAiEnabled"] === true) {
    throw clientFault(403, "AI profiles are read-only on this portal (managed by the AI gateway)");
  }
}

// `providerType: "external"` delegates all HTTP transport to the host
// application's `externalFetch` (a browser-side hook) — a server-created
// profile has no such transport, so `engine.create` blows up inside the
// live credentials probe with an unhandled error (Bug 83114, agreed to be
// a 400). Reject it up front with an actionable message.
function rejectExternalProviderType(
  providerType: unknown,
  res: { status: (code: number) => { json: (body: unknown) => void } },
): boolean {
  if (providerType !== "external") {
    return false;
  }
  res.status(400).json({
    error:
      'providerType "external" cannot be used for server-managed profiles: it delegates transport to the host application',
  });
  return true;
}

// Translate a provider-side failure (raised by the OpenAI-compatible SDK
// while listing models) into a meaningful HTTP status + message instead of
// letting `asyncHandler` collapse everything to a generic 500. The base URL
// and key come from the user's profile form, so most failures are config
// errors that should read as such in the UI.
function describeProviderError(err: unknown): { status: number; message: string } {
  // An unknown providerType never reaches the network — the provider
  // registry lookup throws before any request is made. Surface it as the
  // input error it is instead of collapsing into a 502 "Failed to list
  // provider models" (Bug 83117).
  if (err instanceof Error && err.message.startsWith("Unknown provider type")) {
    return { status: 400, message: err.message };
  }
  // The library's profile lookup throws a plain Error for an unknown id;
  // the routes check existence up front, this is the belt (Bug 84426).
  if (err instanceof Error && err.message.startsWith("Profile not found")) {
    return { status: 404, message: "Profile not found" };
  }
  // A key the SDK cannot put into an HTTP header (non-ASCII, a BOM) fails
  // inside fetch with a bare TypeError before any request leaves; the
  // routes reject such a key up front, this is the belt (Bug 84428).
  if (err instanceof TypeError && /ByteString|invalid header|ERR_INVALID_CHAR/i.test(err.message)) {
    return { status: 400, message: HEADER_UNSAFE_KEY_MESSAGE };
  }
  // OpenAI-SDK HTTP errors expose a numeric `status`.
  const rawStatus = readProp(err, "status");
  const httpStatus = typeof rawStatus === "number" ? rawStatus : undefined;
  if (httpStatus === 401 || httpStatus === 403) {
    return { status: 400, message: "Invalid API key for the AI provider" };
  }
  if (httpStatus === 404) {
    return {
      status: 400,
      message: "Invalid base URL — expected an OpenAI-compatible endpoint (e.g. ending in /v1)",
    };
  }
  if (typeof httpStatus === "number" && httpStatus >= 500) {
    return { status: 502, message: "The AI provider returned an error" };
  }
  if (typeof httpStatus === "number" && httpStatus >= 400) {
    return { status: 400, message: `The AI provider rejected the request (${httpStatus})` };
  }

  // Network-level failures (no HTTP status): walk the cause chain for a
  // known DNS/connection code.
  const codes = new Set([
    "ENOTFOUND",
    "ECONNREFUSED",
    "EAI_AGAIN",
    "ECONNRESET",
    "ETIMEDOUT",
    "UND_ERR_CONNECT_TIMEOUT",
  ]);
  let cur: unknown = err;
  for (let i = 0; i < 5 && cur; i++) {
    const code = readProp(cur, "code");
    if (typeof code === "string" && codes.has(code)) {
      return {
        status: 502,
        message:
          "The AI provider is unreachable — check the base URL and that the service is running",
      };
    }
    cur = readProp(cur, "cause");
  }

  const msg = err instanceof Error ? err.message : String(err);
  if (/connection error|fetch failed|timeout/i.test(msg)) {
    return {
      status: 502,
      message:
        "The AI provider is unreachable — check the base URL and that the service is running",
    };
  }
  return { status: 502, message: "Failed to list provider models" };
}

interface ListProviderModelsBody {
  providerType?: ProviderType;
  baseUrl?: string;
  apiKey?: string;
}

const HEADER_UNSAFE_KEY_MESSAGE =
  "the API key contains characters that cannot be sent in an HTTP header";

// An API key travels as a Bearer header, and header values are ByteStrings:
// printable ASCII (plus tab) only. Anything else makes the SDK throw a bare
// TypeError from inside fetch, which read as a provider failure (502)
// instead of the input error it is (Bug 84428). Every provider puts a
// non-empty key into the header, Ollama included, so the rule is uniform.
function headerSafeKeyError(value: unknown): string | undefined {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }
  if (typeof value !== "string" || !/^[\t\x20-\x7E]*$/.test(value)) {
    return HEADER_UNSAFE_KEY_MESSAGE;
  }
  return undefined;
}

// The engine's own refusal shape (`{ success: false, error }`, the field
// named), so the widget's profile form shows the message on the key input:
// the chat client hands a non-2xx body back as a value only in this shape
// and reports any other one as a bare "HTTP 400".
function sendKeyRefusal(res: Response, message: string): void {
  res.status(400).json({ success: false, error: { field: "key", message, code: "invalid" } });
}

// A refusal of the engine (a taken name, an unknown provider type, a key the
// provider refuses, an unknown id on update) is a 400 in the same
// `{ success: false, error }` shape: the widget's ApiProvider hands a non-2xx
// body of that shape back as a value, so its profile form reads it as before
// (ts-ai-failure-paths.md §2). The profiles engine sets no `error.code`, and
// the published document lists 400 alone for these writes, so every refusal
// is one status.
function sendProfileMutation(res: Response, result: ProfileMutationResult): void {
  if (!result.success) {
    res.status(400).json({ success: false, error: result.error });
    return;
  }
  // The engine answers `create` with the stored profile and `update` with the body it was sent,
  // spread: whatever else either carries stays here.
  res.json({ success: true, profile: profileForSettings(result.profile) });
}

// The profile as an answer carries it: the fields of `AiProfile` in the
// published document, by name. Spread from the storage, it carried whatever
// the AI service adds to its ProfileDto (the BYOK provider of a profile that
// runs on the portal's own key is one), and `key` and `headers` never leave
// through these answers (ts-ai-failure-paths.md §5). An absent field is left
// undefined, which the JSON answer omits.
export type ProfileAnswer = Omit<Profile, "key" | "headers"> & { useResponsesApi?: boolean };

export function profileForResponse(profile: Profile): ProfileAnswer {
  // Not a field of the library's `Profile`: the AI service's ProfileDto
  // carries it and `dtoToProfile` reads it.
  const useResponsesApi = readProp(profile, "useResponsesApi");
  return {
    id: profile.id,
    name: profile.name,
    providerType: profile.providerType,
    basedOn: profile.basedOn,
    baseUrl: profile.baseUrl,
    modelId: profile.modelId,
    reasoning: profile.reasoning,
    reasoningSupport: profile.reasoningSupport,
    contextWindow: profile.contextWindow,
    capabilities: profile.capabilities,
    canUseTool: profile.canUseTool,
    useResponsesApi: typeof useResponsesApi === "boolean" ? useResponsesApi : undefined,
    isCloudProvider: profile.isCloudProvider,
    useProxy: profile.useProxy,
    createdAt: profile.createdAt,
  };
}

// The profile as the settings cards read it: the listed fields and the provider key. The answer
// of a write goes to the DocSpace admin who sent the key, and the widget puts it into its list as
// it came; the list carries the key for an administrator alone, since the AI service reads it
// `forClient` and drops the key for everyone else (spec ai-agents, "Key in the list for an
// administrator"). `headers` and whatever else the object carries do not leave.
export function profileForSettings(profile: Profile): ProfileAnswer & { key?: string } {
  const answer: ProfileAnswer & { key?: string } = profileForResponse(profile);
  if (typeof profile.key === "string") {
    answer.key = profile.key;
  }
  return answer;
}

// 404 for a profile the caller cannot see, before the engine is involved:
// the library's lookup throws a plain Error for a missing profile, which
// collapsed into 502 "Failed to list provider models" on list-models and
// into a generic 500 on test-connection (Bugs 84426, 84427). The raw read
// carries no key and applies no provider override, it only has to exist.
async function profileExists(id: string): Promise<boolean> {
  return (await storage.profiles.readByIdRaw(id)) !== undefined;
}

export const profilesController = {
  create: asyncHandler<CreateProfileInput>(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): with AI off, or for a caller
    // without AI access, 403 for any body, and no outbound request to the sent baseUrl.
    await assertAiAccess();
    await assertProfilesWritable();
    // The engine compares the name with the stored ones before anything else: a body without a
    // string name made it throw, which answered 500.
    const sent: unknown = req.body;
    if (!isObject(sent) || typeof sent["name"] !== "string") {
      res.status(400).json({ error: "name is required and must be a string" });
      return;
    }
    if (rejectExternalProviderType(req.body?.providerType, res)) {
      return;
    }
    await assertSafeBaseUrl(req.body?.baseUrl);
    const keyError = headerSafeKeyError(req.body?.key);
    if (keyError) {
      sendKeyRefusal(res, keyError);
      return;
    }
    sendProfileMutation(res, await engine.create(req.body));
  }),

  update: asyncHandler<Profile>(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): with AI off, or for a caller
    // without AI access, 403 for any body, and no outbound request to the sent baseUrl.
    await assertAiAccess();
    await assertProfilesWritable();
    // The engine compares the name with the stored ones before anything else: a body without a
    // string name made it throw, which answered 500.
    const sent: unknown = req.body;
    if (!isObject(sent) || typeof sent["name"] !== "string") {
      res.status(400).json({ error: "name is required and must be a string" });
      return;
    }
    if (rejectExternalProviderType(req.body?.providerType, res)) {
      return;
    }
    await assertSafeBaseUrl(req.body?.baseUrl);
    const keyError = headerSafeKeyError(req.body?.key);
    if (keyError) {
      sendKeyRefusal(res, keyError);
      return;
    }
    sendProfileMutation(res, await engine.update(req.body));
  }),

  delete: asyncHandler(async (req, res) => {
    const { id } = unpackPositional(req.body, ["id"] as const);
    const idStr = typeof id === "string" ? id : asString(req.query["id"]);
    if (!idStr) {
      res.status(400).json({ error: "id required" });
      return;
    }
    await engine.delete(idStr);
    res.json({ success: true });
  }),

  listProviderModels: asyncHandler<ListProviderModelsBody>(async (req, res) => {
    // Nothing below touches the AI service — the listing goes straight to
    // the provider named in the body — so a Guest used to get an outbound
    // request made on the portal's behalf instead of the 403 every other
    // profiles route answers (Bug 82824).
    await assertAiAccess();
    const { providerType, baseUrl, apiKey } = req.body ?? {};
    // One error per actually-missing field, so the client knows which
    // input to highlight (Bug 83116).
    if (!providerType) {
      res.status(400).json({ error: "providerType required", field: "providerType" });
      return;
    }
    if (!baseUrl) {
      res.status(400).json({ error: "baseUrl required", field: "baseUrl" });
      return;
    }
    const keyError = headerSafeKeyError(apiKey);
    if (keyError) {
      res.status(400).json({ error: keyError, field: "apiKey" });
      return;
    }
    await assertSafeBaseUrl(baseUrl);
    if (providerType === "onlyoffice") {
      // The onlyoffice provider's OpenAI-compatible /models returns bare
      // ids, so the engine stamps every model with the broad default
      // capability mask — while GET /profiles/list synthesizes its answer
      // from the gateway's rich catalog. Serve this listing from that same
      // catalog so both methods agree (Bug 83113); the runtime override
      // (withOnlyofficeProviderOverrides) already points every onlyoffice
      // profile at this gateway regardless of the submitted baseUrl. Fall
      // back to the provider listing when the gateway is unavailable
      // (self-hosted portals without the AI gateway).
      try {
        res.json(await listOnlyofficeGatewayModels());
        return;
      } catch (err) {
        logger.warn(
          `gateway models listing failed, falling back to the provider listing: ${
            err instanceof Error ? err.message : String(err)
          }`,
        );
      }
    }
    try {
      const models = await engine.listProviderModels({
        providerType,
        baseUrl,
        apiKey: apiKey ?? "",
      });
      res.json(models);
    } catch (err) {
      const { status, message } = describeProviderError(err);
      logger.warn(
        `listProviderModels failed (${providerType} @ ${baseUrl}) -> ${status}: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      res.status(status).json({ error: message });
    }
  }),

  listModels: asyncHandler(async (req, res) => {
    const profileId = asString(req.query["profileId"]);
    if (!profileId) {
      res.status(400).json({ error: "profileId required" });
      return;
    }
    if (!(await profileExists(profileId))) {
      res.status(404).json({ error: "Profile not found" });
      return;
    }
    try {
      const models = await engine.listModels(profileId);
      res.json(models);
    } catch (err) {
      // A failure from the C# storage hop (the profile read inside
      // `engine.listModels`) carries the real verdict — 403 for guests or
      // disabled AI, 404 for a missing profile. `describeProviderError`
      // would misread its 401/403 as a provider auth failure and answer
      // 400 "Invalid API key" (Bug 82971) — rethrow so asyncHandler
      // forwards the upstream status instead.
      if (err instanceof AiServiceHttpError) {
        throw err;
      }
      const { status, message } = describeProviderError(err);
      logger.warn(
        `listModels failed (profileId=${profileId}) -> ${status}: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      res.status(status).json({ error: message });
    }
  }),

  testConnection: asyncHandler(async (req, res) => {
    const { profileId } = unpackPositional(req.body, ["profileId"] as const);
    const idStr = typeof profileId === "string" ? profileId : asString(req.query["profileId"]);
    if (!idStr) {
      res.status(400).json({ error: "profileId required" });
      return;
    }
    if (!(await profileExists(idStr))) {
      res.status(404).json({ error: "Profile not found" });
      return;
    }
    const result = await engine.testConnection(idStr);
    res.json(result);
  }),

  getById: asyncHandler(async (req, res) => {
    const id = asString(req.query["id"]);
    if (!id) {
      res.status(400).json({ error: "id required" });
      return;
    }
    // Read WITHOUT the onlyoffice provider override: the override rewrites
    // `baseUrl` to the internal service address and merges the caller's
    // forwarded auth headers — meant for in-process provider calls, never
    // for an HTTP response (Bug 82821). The former justification for the
    // full profile — the doceditor Next passthrough — was removed; the
    // editor now goes through /openai/{profileId}/v1, which resolves the
    // profile server-side. `key` and `headers` are left out on top: no
    // consumer reads them (on SaaS `key` is the "onlyoffice" placeholder;
    // on self-hosted it is the tenant's real provider secret).
    const profile = await storage.profiles.readByIdRaw(id);
    if (!profile) {
      res.status(404).json({ error: "Profile not found" });
      return;
    }
    res.json(profileForResponse(profile));
  }),

  // `readAllForClient`, not `engine.list()`: the engine reads the full
  // profiles its provider calls need, while this answer reaches the browser
  // - the chat model picker and the editor read it as any user - so the AI
  // service drops `key` and `headers` for everyone but an administrator,
  // whose settings cards edit the key. Each entry leaves by its listed fields
  // (see profileForSettings).
  list: asyncHandler(async (_req, res) => {
    const profiles = await storage.profiles.readAllForClient();
    res.json(profiles.map(profileForSettings));
  }),
};
