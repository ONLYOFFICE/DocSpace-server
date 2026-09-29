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

import { WebSearchEngine } from "@onlyoffice/ai-chat/core";
import type { WebSearchConfig } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { isObject } from "../narrow.js";
import { assertSafeBaseUrl } from "../security.js";
import { assertEntityAccessible } from "../storage/docspaceFilesApi.js";

async function checkConfigUrl(config: WebSearchConfig): Promise<void> {
  await assertSafeBaseUrl(config.baseUrl);
}

const engine = new WebSearchEngine({ storage });

// testConnection is the one route here that never touches the C# storage —
// the probe goes straight to the search provider — so it inherits none of
// the downstream gates. Borrow them: a throwaway config read runs the same
// [AiFeature] (AI disabled) and employee-type (Guest) checks and rethrows
// their 403 before any outbound connection is made (Bugs 83234 / 83235).
async function assertWebSearchAccess(): Promise<void> {
  await storage.webSearch.read();
}

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

/** A field a client actually sent a value for; `null` counts as "not set". */
function isSet(value: unknown): boolean {
  return value !== undefined && value !== null;
}

/**
 * Reads the config argument of `configure` / `setActiveConfig`.
 *
 * `ApiProvider` and the widget send a positional array, so `unpackPositional` names the
 * first element `body`. A named object is passed through untouched, and the documented
 * name for that key is `config` - accept both rather than silently reading `undefined`.
 */
function unpackConfig(body: unknown): { body: unknown; entityId: unknown } {
  const args = unpackPositional(body, ["body", "entityId"] as const);
  return {
    body: args.body ?? (args as { config?: unknown }).config,
    entityId: args.entityId,
  };
}

/**
 * Narrow a request argument to a {@link WebSearchConfig}, or reject the
 * request with 400.
 *
 * Everything the engine cannot work with has to be refused here, because
 * the engine does not guard its input: `validateConfigShape` reads
 * `config.provider` straight away, so an absent or non-object config threw
 * a TypeError that the error boundary could only report as 500 (Bug 82812).
 * The cases that got there in practice were a body carrying the config
 * unwrapped (no `body`/`config` key, so `unpackConfig` yields `undefined`)
 * and an empty `{}`.
 *
 * Optional fields are type-checked rather than dropped: a mistyped `key`
 * or `baseUrl` is a malformed request, not a request without one. `null`
 * is the exception — it is the wire form of "not set" for a client that
 * serializes every field, and the read path (`parseWebSearchConfig`) has
 * always taken it as absent, so it is accepted here too.
 */
function asConfig(value: unknown): WebSearchConfig {
  if (!isObject(value)) {
    badRequest("config is required and must be an object");
  }
  const { provider, key, baseUrl, isCloudProvider, headers } = value as Record<string, unknown>;
  if (typeof provider !== "string" || !provider.trim()) {
    badRequest("config.provider is required and must be a non-empty string");
  }
  if (isSet(key) && typeof key !== "string") {
    badRequest("config.key must be a string");
  }
  if (isSet(baseUrl) && typeof baseUrl !== "string") {
    badRequest("config.baseUrl must be a string");
  }
  if (isSet(isCloudProvider) && typeof isCloudProvider !== "boolean") {
    badRequest("config.isCloudProvider must be a boolean");
  }
  if (isSet(headers) && !isObject(headers)) {
    badRequest("config.headers must be an object");
  }
  return value as unknown as WebSearchConfig;
}

/**
 * Narrow the optional `entityId` argument, or reject with 400.
 *
 * Absent, null and empty all mean "portal-wide scope" — the widget omits the
 * argument that way. Anything else that is not a string (a repeated query
 * parameter arrives as an array) is a malformed request: it used to be
 * silently dropped by `asString`, so a scoped read answered with the
 * portal-wide configuration instead of refusing the request.
 */
function asEntityId(value: unknown): string | undefined {
  if (value === undefined || value === null) {
    return undefined;
  }
  if (typeof value !== "string") {
    badRequest("entityId must be a string");
  }
  return value.trim() ? value : undefined;
}

export const webSearchController = {
  getActiveConfig: asyncHandler(async (req, res) => {
    const entityId = asEntityId(req.query["entityId"]);
    // A room config must not be readable by someone who cannot open the
    // room (Bug 82901). Inaccessible/unknown rooms surface as 404 — the
    // same convention as threads/create.
    await assertEntityAccessible(entityId);
    const config = await engine.getActiveConfig(entityId);
    res.json(config);
  }),

  isConfigured: asyncHandler(async (req, res) => {
    const entityId = asEntityId(req.query["entityId"]);
    await assertEntityAccessible(entityId);
    const value = await engine.isConfigured(entityId);
    res.json(value);
  }),

  testConnection: asyncHandler<WebSearchConfig>(async (req, res) => {
    // The access gate runs first: a Guest or an AI-disabled portal must get
    // its 403 rather than a report on the shape of the body it sent.
    await assertWebSearchAccess();
    const config = asConfig(req.body);
    await checkConfigUrl(config);
    const result = await engine.testConnection(config);
    res.json(result);
  }),

  configure: asyncHandler(async (req, res) => {
    const args = unpackConfig(req.body);
    const entityId = asEntityId(args.entityId);
    await assertEntityAccessible(entityId);
    const config = asConfig(args.body);
    await checkConfigUrl(config);
    const result = await engine.configure(config, entityId);
    res.json(result);
  }),

  setActiveConfig: asyncHandler(async (req, res) => {
    const args = unpackConfig(req.body);
    const entityId = asEntityId(args.entityId);
    await assertEntityAccessible(entityId);
    const config = asConfig(args.body);
    await checkConfigUrl(config);
    await engine.setActiveConfig(config, entityId);
    res.json({ success: true });
  }),

  clear: asyncHandler(async (_req, res) => {
    await engine.clear();
    res.json({ success: true });
  }),
};
