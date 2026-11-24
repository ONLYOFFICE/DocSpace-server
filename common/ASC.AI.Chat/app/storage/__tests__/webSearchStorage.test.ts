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

import { describe, expect, it } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { getChatContextMisses, markForwardHeadersToProvider } from "../../requestContext.js";
import { aiServiceBaseUrl } from "../httpClient.js";
import {
  HttpWebSearchStorage,
  parseWebSearchConfig,
  withWebSearchProviderOverrides,
} from "../webSearchStorage.js";
import { inRound, snapshotOf } from "../../__tests__/fixtures/chatContext.js";
import { UpstreamShapeError } from "../upstreamShape.js";

// GET/PUT/DELETE internal/ai/web-search: products/ASC.AI/Server/Api/WebSearchStorageController.cs.
const upstream = useUpstream();
const storage = new HttpWebSearchStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

/** `WebSearchConfigDto` (products/ASC.AI/Server/Models/ResponseDto/WebSearchConfigDto.cs). */
const webSearchDto = () => ({ provider: "exa", key: "exa-key", baseUrl: "https://api.exa.ai" });

describe("parseWebSearchConfig", () => {
  it("maps a full WebSearchConfigDto", () => {
    expect(parseWebSearchConfig(webSearchDto())).toEqual({
      provider: "exa",
      key: "exa-key",
      baseUrl: "https://api.exa.ai",
    });
  });

  // isCloudProvider is not a property of WebSearchConfigDto (contract.dotnet.test.ts): the AI service does not
  // send it. This and the two isCloudProvider rows below pin what the reader would do if it did.
  it("takes isCloudProvider when the answer carries it", () => {
    expect(parseWebSearchConfig({ provider: "onlyoffice", isCloudProvider: true })).toEqual({
      provider: "onlyoffice",
      isCloudProvider: true,
    });
  });

  it.each([null, undefined, "exa", 1, []])("answers null for %j", (raw) => {
    expect(parseWebSearchConfig(raw)).toBeNull();
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["empty", ""],
    ["a number", 1],
  ])("answers null when provider is %s", (_name, provider) => {
    const raw: Record<string, unknown> = { ...webSearchDto(), provider };
    if (provider === undefined) {
      delete raw["provider"];
    }
    expect(parseWebSearchConfig(raw)).toBeNull();
  });

  it.each([
    ["key", null],
    ["key", 5],
    ["baseUrl", null],
    ["baseUrl", false],
    ["isCloudProvider", null],
    ["isCloudProvider", "true"],
  ])("leaves %s %j out", (field, value) => {
    expect(parseWebSearchConfig({ provider: "exa", [field]: value })).toEqual({ provider: "exa" });
  });

  it("keeps an empty key and base URL", () => {
    expect(parseWebSearchConfig({ provider: "exa", key: "", baseUrl: "" })).toEqual({
      provider: "exa",
      key: "",
      baseUrl: "",
    });
  });
});

describe("withWebSearchProviderOverrides", () => {
  const onlyoffice = { provider: "onlyoffice", key: "k", headers: { "x-own": "1" } };

  it("passes null and another provider through unchanged", () => {
    expect(withWebSearchProviderOverrides(null)).toBeNull();
    const exa = { provider: "exa" };
    expect(withWebSearchProviderOverrides(exa)).toBe(exa);
  });

  it("passes the onlyoffice provider through unchanged unless the request forwards headers", async () => {
    expect(withWebSearchProviderOverrides(onlyoffice)).toBe(onlyoffice);
    const inRequest = await inRequestContext({ cookie: "c" }, async () =>
      withWebSearchProviderOverrides(onlyoffice),
    );
    expect(inRequest).toBe(onlyoffice);
  });

  it("points the onlyoffice provider at the AI service gateway with the caller's headers, its own winning", async () => {
    const result = await inRequestContext(
      { cookie: "asc_auth_key=abc", "x-own": "caller", host: "portal" },
      async () => {
        markForwardHeadersToProvider();
        return withWebSearchProviderOverrides(onlyoffice);
      },
    );
    expect(result).toEqual({
      provider: "onlyoffice",
      key: "k",
      isCloudProvider: true,
      baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway/`,
      headers: { cookie: "asc_auth_key=abc", "x-own": "1" },
    });
  });

  it("forwards the caller's headers when the config has none", async () => {
    const result = await inRequestContext({ cookie: "c" }, async () => {
      markForwardHeadersToProvider();
      return withWebSearchProviderOverrides({ provider: "onlyoffice" });
    });
    expect(result?.headers).toEqual({ cookie: "c" });
  });
});

describe("read", () => {
  it("reads the portal's config", async () => {
    upstream.use(http.get("*/internal/ai/web-search", () => docspace(webSearchDto())));
    await expect(storage.read("42")).resolves.toEqual({
      provider: "exa",
      key: "exa-key",
      baseUrl: "https://api.exa.ai",
    });
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/web-search" }]);
  });

  it.each([
    ["nothing configured (204)", noContent],
    ["null", () => docspace(null)],
    ["a 404", () => docspaceError(404)],
  ])("reads %s as none", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/web-search", answer));
    await expect(storage.read()).resolves.toBeNull();
  });

  // A config the AI service answers but that cannot be read is unknown, not "not configured".
  it.each([
    ["a string", "x"],
    ["an empty object", {}],
    ["a list", []],
    ["a config without provider", { key: "k" }],
    ["a config with an empty provider", { provider: "" }],
  ])("throws UpstreamShapeError for %s", async (_name, body) => {
    upstream.use(http.get("*/internal/ai/web-search", () => docspace(body)));
    await expect(storage.read()).rejects.toBeInstanceOf(UpstreamShapeError);
  });

  it("throws UpstreamShapeError for a text answer", async () => {
    upstream.use(
      http.get(
        "*/internal/ai/web-search",
        () => new HttpResponse("exa", { headers: { "content-type": "text/plain" } }),
      ),
    );
    await expect(storage.read()).rejects.toBeInstanceOf(UpstreamShapeError);
  });

  it.each([403, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/web-search", () => docspaceError(status)));
    await expect(storage.read()).rejects.toMatchObject({ status });
  });

  it("serves a primed round's config, with the overrides, without a request", async () => {
    const snapshot = snapshotOf({ webSearch: { provider: "onlyoffice" } });
    const result = await inRound(
      snapshot,
      async () => {
        markForwardHeadersToProvider();
        return { config: await storage.read(), misses: getChatContextMisses() };
      },
      { cookie: "c" },
    );
    expect(result).toEqual({
      config: {
        provider: "onlyoffice",
        isCloudProvider: true,
        baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway/`,
        headers: { cookie: "c" },
      },
      misses: [],
    });
    await expect(inRound(snapshotOf(), () => storage.read())).resolves.toBeNull();
    expect(upstream.seen).toEqual([]);
  });

  it("asks once the slice is stale, counting the miss", async () => {
    upstream.use(http.get("*/internal/ai/web-search", () => docspace(webSearchDto())));
    const snapshot = snapshotOf({ webSearch: { provider: "old" }, stale: new Set(["webSearch"]) });
    const result = await inRound(snapshot, async () => ({
      config: await storage.read(),
      misses: getChatContextMisses(),
    }));
    expect(result.config?.provider).toBe("exa");
    expect(result.misses).toEqual(["webSearch.read"]);
  });
});

describe("the writes", () => {
  it.each(["create", "update", "upsert"] as const)(
    "%s puts the named fields only and marks the slice stale",
    async (method) => {
      upstream.use(http.put("*/internal/ai/web-search", noContent));
      const snapshot = snapshotOf();
      await inRound(snapshot, () =>
        storage[method](
          {
            provider: "exa",
            key: "k",
            baseUrl: "https://api.exa.ai",
            isCloudProvider: false,
            headers: { authorization: "x" },
          },
          "42",
        ),
      );
      expect(upstream.seen).toMatchObject([
        {
          method: "PUT",
          path: "/internal/ai/web-search",
          body: {
            provider: "exa",
            key: "k",
            baseUrl: "https://api.exa.ai",
            isCloudProvider: false,
          },
        },
      ]);
      expect(upstream.seen[0]?.body).not.toHaveProperty("headers");
      expect([...snapshot.stale]).toEqual(["webSearch"]);
    },
  );

  it("puts the provider alone when nothing else is set", async () => {
    upstream.use(http.put("*/internal/ai/web-search", noContent));
    await storage.create({ provider: "exa" });
    expect(upstream.seen[0]?.body).toEqual({ provider: "exa" });
  });

  it("leaves the slice fresh when the put is refused", async () => {
    upstream.use(http.put("*/internal/ai/web-search", () => docspaceError(400)));
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.upsert({ provider: "exa" })),
    ).rejects.toMatchObject({
      status: 400,
    });
    expect(snapshot.stale.size).toBe(0);
  });

  it("delete marks the slice stale first, treats a 404 as done and rethrows a 500", async () => {
    upstream.use(http.delete("*/internal/ai/web-search", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.delete("42"));
    expect(upstream.seen).toMatchObject([{ method: "DELETE", path: "/internal/ai/web-search" }]);
    expect([...snapshot.stale]).toEqual(["webSearch"]);

    upstream.use(http.delete("*/internal/ai/web-search", () => docspaceError(404)));
    await expect(storage.delete()).resolves.toBeUndefined();
    upstream.use(http.delete("*/internal/ai/web-search", () => docspaceError(500)));
    await expect(storage.delete()).rejects.toMatchObject({ status: 500 });
  });
});
