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

import { UpstreamShapeError } from "../upstreamShape.js";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  delay,
  docspace,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { getUpstreamCalls, getUpstreamReadCount } from "../../requestContext.js";
import {
  AiServiceHttpError,
  aiService,
  aiServiceBaseUrl,
  aiServiceRequest,
  withTimeout,
} from "../httpClient.js";

// The real client over a stubbed network, for what httpClient.test.ts leaves open: answers that
// are not JSON, the timeout, the body of a refusal and the corners of the query string.
const upstream = useUpstream();

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

describe("aiService answers", () => {
  it("answers the text of a text/plain answer", async () => {
    upstream.use(http.get("*/internal/ai/plain", () => HttpResponse.text("hello")));

    await expect(aiService.get("/plain")).resolves.toBe("hello");
  });

  it("answers the text of an answer without a Content-Type", async () => {
    upstream.use(
      http.get("*/internal/ai/bare", () => new HttpResponse(new TextEncoder().encode('{"a":1}'))),
    );

    await expect(aiService.get("/bare")).resolves.toBe('{"a":1}');
  });

  it("answers null for a 204 without reading a body", async () => {
    upstream.use(http.put("*/internal/ai/x", () => new HttpResponse(null, { status: 204 })));

    await expect(aiService.put("/x", { a: 1 })).resolves.toBeNull();
  });

  it("keeps a JSON object that has a response but no status, as it is", async () => {
    upstream.use(http.get("*/internal/ai/half", () => HttpResponse.json({ response: [1] })));

    await expect(aiService.get("/half")).resolves.toEqual({ response: [1] });
  });

  it("unwraps an envelope whose response is null", async () => {
    upstream.use(http.get("*/internal/ai/null", () => docspace(null)));

    await expect(aiService.get("/null")).resolves.toBeNull();
  });

  // What the portal API really sends for an action that returned null: it omits null properties.
  it("reads an envelope without a response as null", async () => {
    upstream.use(
      http.get("*/internal/ai/none", () =>
        HttpResponse.json({ count: 0, status: 0, statusCode: 200 }),
      ),
    );

    await expect(aiService.get("/none")).resolves.toBeNull();
  });

  it("keeps an object that only has a status, as it is", async () => {
    upstream.use(http.get("*/internal/ai/job", () => HttpResponse.json({ status: "done" })));

    await expect(aiService.get("/job")).resolves.toEqual({ status: "done" });
  });

  it("answers a JSON array or a scalar as it came", async () => {
    upstream.use(
      http.get("*/internal/ai/arr", () => HttpResponse.json([1, 2])),
      http.get("*/internal/ai/num", () => HttpResponse.json(5)),
    );

    await expect(aiService.get("/arr")).resolves.toEqual([1, 2]);
    await expect(aiService.get("/num")).resolves.toBe(5);
  });

  it("keeps the envelope of a raw read", async () => {
    upstream.use(http.get("*/internal/ai/raw", () => docspace({ a: 1 })));

    await expect(aiService.get("/raw", { raw: true })).resolves.toEqual({
      response: { a: 1 },
      count: 1,
      status: 0,
      statusCode: 200,
    });
  });

  it("rejects a JSON answer that cannot be parsed", async () => {
    upstream.use(
      http.get(
        "*/internal/ai/broken",
        () => new HttpResponse("{not json", { headers: { "content-type": "application/json" } }),
      ),
    );

    expect(await failure(aiService.get("/broken"))).toEqual(new UpstreamShapeError("GET /broken"));
  });
});

describe("aiService refusals", () => {
  it("carries the status, the reason phrase, the body and the URL of a refusal", async () => {
    upstream.use(
      http.get("*/internal/ai/profiles/x", () =>
        HttpResponse.text("upstream detail", { status: 502, statusText: "Bad Gateway" }),
      ),
    );

    const error = (await failure(aiService.get("/profiles/x"))) as AiServiceHttpError;

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({
      status: 502,
      statusText: "Bad Gateway",
      body: "upstream detail",
      url: `${aiServiceBaseUrl}/internal/ai/profiles/x`,
    });
    expect(error.message).toBe(
      `AI service 502 Bad Gateway for ${aiServiceBaseUrl}/internal/ai/profiles/x: upstream detail`,
    );
  });

  it("carries an empty body when the body of a refusal cannot be read", async () => {
    upstream.use(
      http.get(
        "*/internal/ai/torn",
        () =>
          new HttpResponse(
            new ReadableStream({
              start(controller) {
                controller.error(new Error("connection reset"));
              },
            }),
            { status: 500, statusText: "Internal Server Error" },
          ),
      ),
    );

    const error = (await failure(aiService.get("/torn"))) as AiServiceHttpError;

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ status: 500, body: "" });
  });

  it("rejects a dropped connection with the fetch error, not an AiServiceHttpError", async () => {
    upstream.use(http.get("*/internal/ai/down", () => HttpResponse.error()));

    const error = await failure(aiService.get("/down"));

    expect(error).toBeInstanceOf(TypeError);
    expect(error).not.toBeInstanceOf(AiServiceHttpError);
  });
});

describe("aiService requests", () => {
  it.each([
    ["put", "PUT"],
    ["patch", "PATCH"],
    ["post", "POST"],
  ] as const)("sends %s with its body as JSON", async (method, verb) => {
    upstream.use(http.all("*/internal/ai/items/1", () => docspace({ ok: true })));

    await expect(aiService[method]("/items/1", { name: "n" })).resolves.toEqual({ ok: true });

    expect(upstream.seen).toEqual([
      expect.objectContaining({
        method: verb,
        path: "/internal/ai/items/1",
        body: { name: "n" },
      }),
    ]);
    expect(upstream.seen[0]?.headers["content-type"]).toBe("application/json");
  });

  it("sends a body that is null as the JSON null", async () => {
    upstream.use(http.post("*/internal/ai/items", () => docspace(null)));

    await aiService.post("/items", null);

    expect(upstream.seen[0]?.headers["content-type"]).toBe("application/json");
  });

  it("sends a body with a DELETE when one is given", async () => {
    upstream.use(http.delete("*/internal/ai/items", () => new HttpResponse(null, { status: 204 })));

    await aiService.delete("/items", { body: { ids: ["a"] } });

    expect(upstream.seen[0]).toMatchObject({ method: "DELETE", body: { ids: ["a"] } });
  });

  it("appends the query to a path that already has one", async () => {
    upstream.use(http.get("*/internal/ai/q", () => docspace([])));

    await aiService.get("/q?a=1", { query: { b: true, c: 0, d: "x y" } });

    expect(upstream.seen[0]?.path).toBe("/internal/ai/q?a=1&b=true&c=0&d=x+y");
  });

  it.each([
    ["an empty query", {}],
    ["a query of null and undefined only", { a: null, b: undefined }],
    ["an empty list", { tags: [] }],
  ])("sends no query string for %s", async (_case, query) => {
    upstream.use(http.get("*/internal/ai/q", () => docspace([])));

    await aiService.get("/q", { query });

    expect(upstream.seen[0]?.path).toBe("/internal/ai/q");
  });

  it("sends the numbers and booleans of a list as text", async () => {
    upstream.use(http.get("*/internal/ai/q", () => docspace([])));

    await aiService.get("/q", { query: { ids: [1, true, "z"] } });

    expect(upstream.seen[0]?.path).toBe("/internal/ai/q?ids=1&ids=true&ids=z");
  });

  it("forwards no credentials outside a request", async () => {
    upstream.use(http.get("*/internal/ai/q", () => docspace([])));

    await aiService.get("/q");

    expect(upstream.seen[0]?.headers["cookie"]).toBeUndefined();
    expect(upstream.seen[0]?.headers["authorization"]).toBeUndefined();
  });

  it("leaves out the caller's hop-by-hop and session headers", async () => {
    upstream.use(http.get("*/internal/ai/q", () => docspace([])));

    await inRequestContext(
      {
        cookie: "c=1",
        "x-session-id": "foreign",
        "mcp-session-id": "foreign",
        "accept-encoding": "br",
        "x-custom": "kept",
      },
      () => aiService.get("/q"),
    );

    const headers = upstream.seen[0]?.headers ?? {};
    expect(headers["cookie"]).toBe("c=1");
    expect(headers["x-custom"]).toBe("kept");
    expect(headers["x-session-id"]).toBeUndefined();
    expect(headers["mcp-session-id"]).toBeUndefined();
  });

  it("counts a GET as a read and every call by its method, inside a request", async () => {
    upstream.use(http.all("*/internal/ai/c", () => docspace(null)));

    const counts = await inRequestContext({}, async () => {
      await aiService.get("/c");
      await aiService.get("/c");
      await aiService.post("/c", {});
      await aiServiceRequest("DELETE", "/c");
      return { reads: getUpstreamReadCount(), calls: getUpstreamCalls() };
    });

    expect(counts).toEqual({ reads: 2, calls: { GET: 2, POST: 1, DELETE: 1 } });
  });
});

// A body that never sends a byte. msw hands the stub's stream to the client as it is, so the
// stream ends the way a socket does when the request is aborted: on the request's signal.
function stalledBody(signal: AbortSignal): ReadableStream<Uint8Array> {
  return new ReadableStream({
    start(controller) {
      signal.addEventListener("abort", () => controller.error(signal.reason), { once: true });
    },
  });
}

describe("aiService timeout", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("rejects a call the AI service does not answer once 300000 ms have passed, not before", async () => {
    upstream.use(
      http.get("*/internal/ai/slow", async () => {
        await delay("infinite");
        return docspace(null);
      }),
    );
    let settled = false;
    const pending = failure(aiService.get("/slow")).finally(() => {
      settled = true;
    });

    await vi.advanceTimersByTimeAsync(299_999);
    expect(settled).toBe(false);
    await vi.advanceTimersByTimeAsync(1);

    const error = await pending;
    expect(String(error)).toContain("upstream timeout after 300000ms");
  });

  it("rejects a call whose body stalls after the headers", async () => {
    upstream.use(
      http.get(
        "*/internal/ai/stall",
        ({ request }) =>
          new HttpResponse(stalledBody(request.signal), {
            headers: { "content-type": "application/json" },
          }),
      ),
    );
    const pending = failure(aiService.get("/stall"));

    await vi.advanceTimersByTimeAsync(300_000);

    expect(String(await pending)).toContain("upstream timeout after 300000ms");
  });

  it("rejects a text answer whose body stalls after the headers", async () => {
    upstream.use(
      http.get(
        "*/internal/ai/stall-text",
        ({ request }) =>
          new HttpResponse(stalledBody(request.signal), {
            headers: { "content-type": "text/plain" },
          }),
      ),
    );
    const pending = failure(aiService.get("/stall-text"));

    await vi.advanceTimersByTimeAsync(300_000);

    expect(String(await pending)).toContain("upstream timeout after 300000ms");
  });

  it("clears its timer once the answer is read", async () => {
    upstream.use(http.get("*/internal/ai/fast", () => docspace(1)));

    await expect(aiService.get("/fast")).resolves.toBe(1);

    expect(vi.getTimerCount()).toBe(0);
  });

  it("clears its timer after a refusal", async () => {
    upstream.use(http.get("*/internal/ai/no", () => HttpResponse.text("x", { status: 400 })));

    await failure(aiService.get("/no"));

    expect(vi.getTimerCount()).toBe(0);
  });
});

describe("withTimeout", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("is aborted at once, with the caller's reason, when the caller's signal already fired", () => {
    const caller = new AbortController();
    const reason = new Error("gone");
    caller.abort(reason);

    const { signal, cancel } = withTimeout(caller.signal, 1000);

    expect(signal.aborted).toBe(true);
    expect(signal.reason).toBe(reason);
    cancel();
  });

  it("follows the caller's signal with the caller's reason", () => {
    const caller = new AbortController();
    const { signal, cancel } = withTimeout(caller.signal, 1000);
    const reason = new Error("client went away");

    caller.abort(reason);

    expect(signal.reason).toBe(reason);
    cancel();
  });

  it("no longer follows the caller's signal after cancel", () => {
    const caller = new AbortController();
    const { signal, cancel } = withTimeout(caller.signal, 1000);

    cancel();
    caller.abort(new Error("late"));

    expect(signal.aborted).toBe(false);
  });

  it("times out after 300000 ms by default", () => {
    const { signal, cancel } = withTimeout(undefined);

    vi.advanceTimersByTime(299_999);
    expect(signal.aborted).toBe(false);
    vi.advanceTimersByTime(1);

    expect(signal.reason).toEqual(new Error("upstream timeout after 300000ms"));
    cancel();
  });
});
