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

import { afterAll, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import {
  delay,
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { listen, send, until } from "../../__tests__/helpers/rawHttp.js";
import type { RawReply } from "../../__tests__/helpers/rawHttp.js";
import { useFile, useFolder } from "../../__tests__/fixtures/providers.js";

// The web-search passthrough of the editor plugin through the whole application (the JSON
// parser, the gate, the request context): the portal's web-search config read from the AI
// service, the source entry from the Files API, the search provider stubbed with msw.

const upstream = useUpstream();
const { default: logger } = await import("../../log.js");
const { createApp } = await import("../../createApp.js");
const { aiServiceBaseUrl } = await import("../../storage/httpClient.js");

let server: { port: number; close: () => Promise<void> };
beforeAll(async () => {
  server = await listen(createApp());
});
afterAll(async () => {
  await server.close();
  vi.unstubAllEnvs();
});
beforeEach(() => {
  vi.mocked(logger.warn).mockClear();
  vi.mocked(logger.error).mockClear();
});

const SEARCH = "/api/2.0/ai/websearch/v1/search";
const CONTENTS = "/api/2.0/ai/websearch/v1/contents";
const CALLER = { Authorization: "Bearer caller-token", "Content-Type": "application/json" };
const GATEWAY = `${aiServiceBaseUrl}/api/2.0/ai/gateway`;
const EXA = "https://api.exa.ai";
const SEARCH_ENGINE = "https://search.test/v1";

// products/ASC.AI/Server/Models/ResponseDto/WebSearchConfigDto.cs (WebSearchConfigMapper),
// as `GET internal/ai/web-search` answers it.
function configDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return { provider: "exa", key: "exa-portal-key", baseUrl: null, ...overrides };
}

function useConfig(dto: unknown = configDto()): void {
  upstream.use(
    http.get(`${aiServiceBaseUrl}/internal/ai/web-search`, () => docspace(dto as never)),
  );
}

interface SearchCall {
  path: string;
  headers: Record<string, string>;
  body: unknown;
  signal: AbortSignal;
}

function useSearch(url: string, answer: () => Response | Promise<Response>): SearchCall[] {
  const calls: SearchCall[] = [];
  upstream.use(
    http.post(url, async ({ request }) => {
      const target = new URL(request.url);
      calls.push({
        path: target.pathname + target.search,
        headers: Object.fromEntries(request.headers),
        body: JSON.parse(await request.text()),
        signal: request.signal,
      });
      return answer();
    }),
  );
  return calls;
}

const results = {
  requestId: "r1",
  results: [{ url: "https://example.org", title: "Example", text: "Body" }],
};

function post(
  path: string,
  body: unknown,
  headers: Record<string, string> = CALLER,
): Promise<RawReply> {
  return send(server.port, {
    path,
    headers,
    body: typeof body === "string" ? body : JSON.stringify(body),
  }).done;
}

const json = (reply: RawReply): unknown => JSON.parse(reply.body);

describe("websearch/v1/search with Exa as the portal's provider", () => {
  it("re-encodes the plugin's body for Exa with the portal key, and relays Exa's answer", async () => {
    useConfig();
    const calls = useSearch(`${EXA}/search`, () => HttpResponse.json(results));

    const reply = await post(`${SEARCH}?entityId=17`, {
      engine: "exa",
      query: "docspace",
      numResults: 9,
      type: "neural",
    });

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("application/json");
    expect(reply.headers["cache-control"]).toBe("no-cache, no-transform");
    expect(json(reply)).toEqual(results);
    expect(responseViolations("aiWebSearchPassthroughSearch", 200, json(reply))).toEqual([]);
    expect(calls).toEqual([
      {
        path: "/search",
        headers: { "content-type": "application/json", "x-api-key": "exa-portal-key" },
        body: { query: "docspace", text: true, numResults: 5, livecrawl: "preferred" },
        signal: expect.any(AbortSignal),
      },
    ]);
    // Exa takes no metadata, so the Files API is not asked.
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /internal/ai/web-search",
      "POST /search",
    ]);
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["of the wrong type", 42],
  ])("sends an empty x-api-key when the config's key is %s", async (_what, key) => {
    const dto = configDto();
    if (key === undefined) {
      delete dto["key"];
    } else {
      dto["key"] = key;
    }
    useConfig(dto);
    const calls = useSearch(`${EXA}/search`, () => HttpResponse.json(results));

    await post(SEARCH, { query: "docspace" });

    expect(calls[0]?.headers["x-api-key"]).toBe("");
  });

  it("matches the Exa provider whatever its case", async () => {
    useConfig(configDto({ provider: "EXA" }));
    const calls = useSearch(`${EXA}/search`, () => HttpResponse.json(results));

    await post(SEARCH, { query: "docspace" });

    expect(calls).toHaveLength(1);
  });
});

describe("websearch/v1/contents with Exa as the portal's provider", () => {
  it("sends the URLs and asks for the text", async () => {
    useConfig();
    const calls = useSearch(`${EXA}/contents`, () => HttpResponse.json(results));

    const reply = await post(CONTENTS, { engine: "exa", urls: ["https://example.org"], extra: 1 });

    expect(reply.status).toBe(200);
    expect(responseViolations("aiWebSearchPassthroughContents", 200, json(reply))).toEqual([]);
    expect(calls[0]?.body).toEqual({ urls: ["https://example.org"], text: true });
  });
});

describe("the ONLYOFFICE gateway as the portal's provider", () => {
  it("sends the plugin's body to the gateway with the caller's headers and the source as metadata", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: null }));
    useFolder(upstream, "17", "Contracts");
    const calls = useSearch(`${GATEWAY}/search`, () => HttpResponse.json(results));

    const reply = await post(
      `${SEARCH}?entityId=17`,
      { engine: "exa", query: "docspace", numResults: 3 },
      { ...CALLER, Cookie: "asc_auth_key=caller-cookie" },
    );

    expect(reply.status).toBe(200);
    expect(calls).toEqual([
      {
        path: "/api/2.0/ai/gateway/search",
        headers: {
          authorization: "Bearer caller-token",
          cookie: "asc_auth_key=caller-cookie",
          "content-type": "application/json",
        },
        body: {
          metadata: { source_id: "17", source_type: "Room", source_title: "Contracts" },
          engine: "exa",
          query: "docspace",
          numResults: 3,
        },
        signal: expect.any(AbortSignal),
      },
    ]);
  });

  it("lets a metadata object the plugin sent win", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: null }));
    useFolder(upstream, "17", "Contracts");
    const calls = useSearch(`${GATEWAY}/contents`, () => HttpResponse.json(results));

    await post(`${CONTENTS}?entityId=17`, {
      urls: ["https://a.test"],
      metadata: { source_id: "x" },
    });

    expect(calls[0]?.body).toEqual({ urls: ["https://a.test"], metadata: { source_id: "x" } });
  });

  it("resolves a file entry through the file route", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: null }));
    useFile(upstream, "17", "Offer.docx");
    const calls = useSearch(`${GATEWAY}/search`, () => HttpResponse.json(results));

    await post(`${SEARCH}?entityId=17&entityKind=file`, { query: "docspace" });

    expect(calls[0]?.body).toEqual({
      metadata: { source_id: "17", source_type: "File", source_title: "Offer.docx" },
      query: "docspace",
    });
  });

  it("sends no metadata without an entry", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: null }));
    const calls = useSearch(`${GATEWAY}/search`, () => HttpResponse.json(results));

    await post(SEARCH, { query: "docspace" });

    expect(calls[0]?.body).toEqual({ query: "docspace" });
    expect(upstream.seen).toHaveLength(2);
  });

  it("sends no metadata when the Files API fails, and logs the failure", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: null }));
    upstream.use(http.get("*/api/2.0/files/folder/17", () => docspaceError(500)));
    const calls = useSearch(`${GATEWAY}/search`, () => HttpResponse.json(results));

    const reply = await post(`${SEARCH}?entityId=17`, { query: "docspace" });

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toEqual({ query: "docspace" });
    expect(vi.mocked(logger.warn).mock.calls[0]?.[0]).toMatch(
      /^source entity fetch failed \(folder 17\): /,
    );
  });

  // DEFECT passthrough-6: a key stored with the ONLYOFFICE config goes out as `Authorization: Bearer <key>` next to the caller's own `authorization`, and the two merge into one comma-joined header — expected one Authorization (the library's OnlyOfficeProvider contract, "profile headers win")
  it("joins the stored key and the caller's Authorization into one header", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: "stored-key" }));
    const calls = useSearch(`${GATEWAY}/search`, () => HttpResponse.json(results));

    await post(SEARCH, { query: "docspace" });

    expect(calls[0]?.headers["authorization"]).toBe("Bearer stored-key, Bearer caller-token");
  });
});

describe("another provider with a base URL of its own", () => {
  it("is sent the plugin's body with the portal key as a Bearer token", async () => {
    useConfig(configDto({ provider: "searxng", key: "portal-key", baseUrl: SEARCH_ENGINE }));
    useFolder(upstream, "17", "Contracts");
    const calls = useSearch(`${SEARCH_ENGINE}/search`, () => HttpResponse.json(results));

    await post(
      `${SEARCH}?entityId=17`,
      { query: "docspace" },
      { ...CALLER, Cookie: "asc_auth_key=c" },
    );

    expect(calls[0]).toEqual({
      path: "/v1/search",
      headers: { authorization: "Bearer portal-key", "content-type": "application/json" },
      body: {
        metadata: { source_id: "17", source_type: "Room", source_title: "Contracts" },
        query: "docspace",
      },
      signal: expect.any(AbortSignal),
    });
  });

  it("is sent no Authorization without a key", async () => {
    useConfig(configDto({ provider: "searxng", key: null, baseUrl: `${SEARCH_ENGINE}/` }));
    const calls = useSearch(`${SEARCH_ENGINE}/search`, () => HttpResponse.json(results));

    await post(SEARCH, { query: "docspace" });

    expect(calls[0]?.headers).toEqual({ "content-type": "application/json" });
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["empty", ""],
    ["of the wrong type", 42],
  ])("answers 404 when the base URL is %s", async (_what, baseUrl) => {
    const dto = configDto({ provider: "searxng" });
    if (baseUrl === undefined) {
      delete dto["baseUrl"];
    } else {
      dto["baseUrl"] = baseUrl;
    }
    useConfig(dto);

    const reply = await post(SEARCH, { query: "docspace" });

    expect(reply.status).toBe(404);
    expect(json(reply)).toEqual({ error: "Web search is not configured" });
    expect(responseViolations("aiWebSearchPassthroughSearch", 404, json(reply))).toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });

  // DEFECT passthrough-7: a stored config marked as a cloud provider but without a base URL takes the provider name for a URL, `new URL` throws, and the route answers 500 — expected the 404 "Web search is not configured" the same branch gives a config without a base URL (ts-ai-failure-paths.md §4: a wrong input is never a 500)
  // Not reachable with the AI service of this repository: WebSearchConfigDto has no
  // isCloudProvider (contract.dotnet.test.ts), so a stored config never carries it.
  it("answers 500 for a cloud provider without a base URL", async () => {
    useConfig(configDto({ provider: "exa", isCloudProvider: true }));

    const reply = await post(SEARCH, { query: "docspace" });

    expect(reply.status).toBe(500);
    expect(json(reply)).toEqual({ error: "Internal server error" });
  });
});

describe("the portal's web-search config", () => {
  it("answers 404 when the AI service has none", async () => {
    upstream.use(http.get(`${aiServiceBaseUrl}/internal/ai/web-search`, () => docspaceError(404)));

    const reply = await post(SEARCH, { query: "docspace" });

    expect(reply.status).toBe(404);
    expect(json(reply)).toEqual({ error: "Web search is not configured" });
    expect(responseViolations("aiWebSearchPassthroughSearch", 404, json(reply))).toEqual([]);
  });

  it("answers the AI service's 403 before the body is judged or anything is sent", async () => {
    upstream.use(http.get(`${aiServiceBaseUrl}/internal/ai/web-search`, () => docspaceError(403)));

    const reply = await post(`${SEARCH}?entityId=17`, null);

    expect(reply.status).toBe(403);
    expect(json(reply)).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiWebSearchPassthroughSearch", 403, json(reply))).toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });

  it("answers 500 when the AI service fails, not 404", async () => {
    upstream.use(http.get(`${aiServiceBaseUrl}/internal/ai/web-search`, () => docspaceError(500)));

    const reply = await post(SEARCH, { query: "docspace" });

    expect(reply.status).toBe(500);
    expect(json(reply)).toEqual({ error: "Internal Server Error" });
    expect(responseViolations("aiWebSearchPassthroughSearch", 500, json(reply))).toEqual([]);
  });

  // products/ASC.AI/Server/Models/ResponseDto/WebSearchConfigDto.cs: the required `provider`.
  // DEFECT passthrough-8: an AI-service answer whose provider is missing, null, empty or not a string is read as "not configured" and answered 404 — expected a 5xx, a body of the wrong shape is unknown, not absent (ts-ai-failure-paths.md §1)
  it.each([
    ["missing", undefined],
    ["null", null],
    ["empty", ""],
    ["of the wrong type", 42],
  ])("answers 404 when the config's provider is %s", async (_what, provider) => {
    const dto = configDto();
    if (provider === undefined) {
      delete dto["provider"];
    } else {
      dto["provider"] = provider;
    }
    useConfig(dto);

    const reply = await post(SEARCH, { query: "docspace" });

    expect(reply.status).toBe(404);
    expect(json(reply)).toEqual({ error: "Web search is not configured" });
  });

  it("answers 404 when the AI service answers something that is not an object", async () => {
    useConfig([configDto()]);

    const reply = await post(SEARCH, { query: "docspace" });

    expect(reply.status).toBe(404);
  });
});

describe("the request body", () => {
  const queryError = { error: "query is required and must be a non-empty string", field: "query" };
  const urlsError = {
    error: "urls is required and must be a non-empty array of strings",
    field: "urls",
  };

  it.each([
    ["null", "null", queryError],
    ["a string", '"docspace"', queryError],
    ["an array", '[{"query":"docspace"}]', queryError],
    ["an empty object", {}, queryError],
    [
      "an engine that is a number",
      { engine: 1, query: "q" },
      { error: "engine must be a string", field: "engine" },
    ],
    ["a null query", { query: null }, queryError],
    ["a query that is a number", { query: 5 }, queryError],
    ["an empty query", { query: "" }, queryError],
    ["a blank query", { query: "  " }, queryError],
    [
      "numResults of 0",
      { query: "q", numResults: 0 },
      { error: "numResults must be a positive integer", field: "numResults" },
    ],
    [
      "a negative numResults",
      { query: "q", numResults: -2 },
      { error: "numResults must be a positive integer", field: "numResults" },
    ],
    [
      "a fractional numResults",
      { query: "q", numResults: 2.5 },
      { error: "numResults must be a positive integer", field: "numResults" },
    ],
    [
      "numResults as a string",
      { query: "q", numResults: "3" },
      { error: "numResults must be a positive integer", field: "numResults" },
    ],
  ])(
    "answers 400 to a search body that is %s, and sends nothing to the provider",
    async (_what, body, error) => {
      useConfig();

      const reply = await post(SEARCH, body);

      expect(reply.status).toBe(400);
      expect(json(reply)).toEqual(error);
      expect(responseViolations("aiWebSearchPassthroughSearch", 400, json(reply))).toEqual([]);
      expect(upstream.seen).toHaveLength(1);
    },
  );

  it.each([
    ["null", "null", urlsError],
    ["an empty object", {}, urlsError],
    [
      "an engine that is an object",
      { engine: {}, urls: ["https://a.test"] },
      { error: "engine must be a string", field: "engine" },
    ],
    ["urls that are null", { urls: null }, urlsError],
    ["urls that are a string", { urls: "https://a.test" }, urlsError],
    ["an empty urls list", { urls: [] }, urlsError],
    ["a url that is a number", { urls: [1] }, urlsError],
    ["an empty url", { urls: ["https://a.test", ""] }, urlsError],
    ["a blank url", { urls: ["  "] }, urlsError],
  ])("answers 400 to a contents body that is %s", async (_what, body, error) => {
    useConfig();

    const reply = await post(CONTENTS, body);

    expect(reply.status).toBe(400);
    expect(json(reply)).toEqual(error);
    expect(responseViolations("aiWebSearchPassthroughContents", 400, json(reply))).toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });

  it("accepts null for the optional engine and numResults", async () => {
    useConfig();
    const calls = useSearch(`${EXA}/search`, () => HttpResponse.json(results));

    const reply = await post(SEARCH, { engine: null, query: "q", numResults: null });

    expect(reply.status).toBe(200);
    expect(calls).toHaveLength(1);
  });

  it("asks the Files API for the entry before it judges the body", async () => {
    useConfig(configDto({ provider: "onlyoffice", key: null }));
    useFolder(upstream, "17", "Contracts");

    const reply = await post(`${SEARCH}?entityId=17`, {});

    expect(reply.status).toBe(400);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/web-search",
      "/api/2.0/files/folder/17",
    ]);
  });

  it("answers 413 for a body over the 100 kB of the JSON parser", async () => {
    const reply = await post(SEARCH, { query: "x".repeat(100 * 1024) });

    expect(reply.status).toBe(413);
    expect(upstream.seen).toHaveLength(0);
  });
});

describe("the provider's answer", () => {
  it("relays a 4xx with its status, content type and body", async () => {
    useConfig();
    useSearch(`${EXA}/search`, () =>
      HttpResponse.json({ error: "Invalid API key" }, { status: 401 }),
    );

    const reply = await post(SEARCH, { query: "q" });

    expect(reply.status).toBe(401);
    expect(reply.headers["content-type"]).toBe("application/json");
    expect(json(reply)).toEqual({ error: "Invalid API key" });
  });

  it("relays an answer without a content type with none", async () => {
    useConfig();
    useSearch(
      `${EXA}/search`,
      () => new HttpResponse(new TextEncoder().encode("raw"), { status: 429 }),
    );

    const reply = await post(SEARCH, { query: "q" });

    expect(reply.status).toBe(429);
    expect(reply.body).toBe("raw");
  });

  // DEFECT passthrough-10: the provider's answer is read whole with `arrayBuffer()`, with no byte limit — expected a capped read (ts-ai-failure-paths.md §7)
  it("relays an answer of any size whole", async () => {
    useConfig();
    const big = { results: [{ text: "x".repeat(3 * 1024 * 1024) }] };
    useSearch(`${EXA}/search`, () => HttpResponse.json(big));

    const reply = await post(SEARCH, { query: "q" });

    expect(reply.body.length).toBe(JSON.stringify(big).length);
  });

  it("answers a provider 5xx with a fixed 502 and keeps its body in the log", async () => {
    useConfig();
    useSearch(
      `${EXA}/search`,
      () => new HttpResponse("panic: main.Request{Query:...}", { status: 500 }),
    );

    const reply = await post(SEARCH, { query: "q" });

    expect(reply.status).toBe(502);
    expect(json(reply)).toEqual({ error: "Upstream request failed" });
    expect(responseViolations("aiWebSearchPassthroughSearch", 502, json(reply))).toEqual([]);
    expect(logger.error).toHaveBeenCalledWith(
      "web-search passthrough search: upstream answered 500: panic: main.Request{Query:...}",
    );
  });

  it("answers 502 when the provider cannot be reached, and keeps the URL out of the answer", async () => {
    useConfig();
    useSearch(`${EXA}/contents`, () => HttpResponse.error());

    const reply = await post(CONTENTS, { urls: ["https://a.test"] });

    expect(reply.status).toBe(502);
    expect(json(reply)).toEqual({ error: "Upstream request failed" });
    expect(responseViolations("aiWebSearchPassthroughContents", 502, json(reply))).toEqual([]);
    expect(vi.mocked(logger.error).mock.calls[0]?.[0]).toMatch(
      /^web-search passthrough: upstream fetch failed: /,
    );
  });
});

describe("a provider that does not answer", () => {
  // DEFECT passthrough-11: the call to the search provider has no timeout: a provider that stalls holds the request until the client goes away — expected a bounded call, `withTimeout` with `cancel()` in a `finally` (ts-ai-style.md, Upstream calls; ts-ai-failure-paths.md §7)
  it("waits for it until the client goes away, then aborts the call and logs nothing", async () => {
    vi.useFakeTimers({
      toFake: ["setTimeout", "clearTimeout", "setInterval", "clearInterval", "Date"],
    });
    try {
      useConfig();
      const calls = useSearch(`${EXA}/search`, async () => {
        await delay("infinite");
        return HttpResponse.json(results);
      });
      const exchange = send(server.port, {
        path: SEARCH,
        headers: CALLER,
        body: '{"query":"q"}',
      });
      await until(() => calls.length === 1);

      await vi.advanceTimersByTimeAsync(3_600_000);
      for (let i = 0; i < 30; i += 1) {
        await new Promise((resolve) => setImmediate(resolve));
      }
      expect(exchange.current()).toBeUndefined();

      exchange.abort();
      await until(() => calls[0]?.signal.aborted === true);
      for (let i = 0; i < 30; i += 1) {
        await new Promise((resolve) => setImmediate(resolve));
      }

      expect(logger.error).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });
});
