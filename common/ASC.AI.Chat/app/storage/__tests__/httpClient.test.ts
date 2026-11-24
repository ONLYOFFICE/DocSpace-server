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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  delay,
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import {
  AiServiceHttpError,
  aiService,
  providerFetch,
  readBodyCapped,
  readTextCapped,
  withTimeout,
} from "../httpClient.js";

// The real client over a stubbed network: nothing of httpClient.js or requestContext.js is
// mocked here, which is what the other suites cannot show.
const upstream = useUpstream();

describe("aiService", () => {
  it("calls /internal/ai of the AI service and unwraps the DocSpace envelope", async () => {
    upstream.use(http.get("*/internal/ai/profiles", () => docspace([{ id: "p1" }])));

    await expect(aiService.get("/profiles")).resolves.toEqual([{ id: "p1" }]);
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/profiles" }]);
  });

  it("keeps the envelope when asked for the raw answer", async () => {
    upstream.use(http.get("*/internal/ai/profiles", () => docspace([])));

    await expect(aiService.get("/profiles", { raw: true })).resolves.toMatchObject({
      response: [],
      statusCode: 200,
    });
  });

  it("sends a list as a repeated query parameter and leaves out null and undefined", async () => {
    upstream.use(http.get("*/internal/ai/threads", () => docspace([])));

    await aiService.get("/threads", {
      query: { tags: ["a", "b"], count: 5, skip: null, x: undefined },
    });

    expect(upstream.seen[0]?.path).toBe("/internal/ai/threads?tags=a&tags=b&count=5");
  });

  it("sends a body as JSON, and no Content-Type without one", async () => {
    upstream.use(
      http.post("*/internal/ai/threads", () => docspace({ id: "t1" })),
      http.delete("*/internal/ai/threads/t1", () => new HttpResponse(null, { status: 204 })),
    );

    await aiService.post("/threads", { title: "T" });
    await expect(aiService.delete("/threads/t1")).resolves.toBeNull();

    expect(upstream.seen[0]).toMatchObject({ body: { title: "T" } });
    expect(upstream.seen[0]?.headers["content-type"]).toBe("application/json");
    expect(upstream.seen[1]?.headers["content-type"]).toBeUndefined();
  });

  it("forwards the caller's credentials of the request it runs in", async () => {
    upstream.use(http.get("*/internal/ai/profiles", () => docspace([])));

    await inRequestContext({ cookie: "asc_auth_key=abc", authorization: "Bearer t" }, () =>
      aiService.get("/profiles"),
    );

    expect(upstream.seen[0]?.headers).toMatchObject({
      cookie: "asc_auth_key=abc",
      authorization: "Bearer t",
    });
  });

  it.each([400, 403, 404, 409, 500, 503])(
    "rejects a %i with an AiServiceHttpError that carries the status",
    async (status) => {
      upstream.use(
        http.get("*/internal/ai/profiles/x", () => docspaceError(status, "secret detail")),
      );

      const error = await aiService.get("/profiles/x").catch((e: unknown) => e);

      expect(error).toBeInstanceOf(AiServiceHttpError);
      expect((error as AiServiceHttpError).status).toBe(status);
    },
  );

  it("aborts a call when the caller's signal fires", async () => {
    upstream.use(
      http.get("*/internal/ai/slow", async () => {
        await delay("infinite");
        return docspace(null);
      }),
    );
    const caller = new AbortController();

    const pending = aiService.get("/slow", { signal: caller.signal });
    caller.abort(new Error("client went away"));

    await expect(pending).rejects.toThrow();
  });
});

describe("withTimeout", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("aborts with the timeout as the reason once the time is up", () => {
    const { signal, cancel } = withTimeout(undefined, 10);

    vi.advanceTimersByTime(10);

    expect(String(signal.reason)).toContain("upstream timeout after 10ms");
    cancel();
  });

  it("does not fire after cancel", () => {
    const { signal, cancel } = withTimeout(undefined, 10);
    cancel();

    vi.advanceTimersByTime(30);

    expect(signal.aborted).toBe(false);
  });
});

describe("providerFetch", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("sends the request as given and answers a refusal as it came", async () => {
    upstream.use(
      http.post("https://provider.example/v1/chat", () =>
        HttpResponse.json({ error: "no" }, { status: 429, statusText: "Too Many" }),
      ),
    );

    const res = await providerFetch(
      "https://provider.example/v1/chat",
      { method: "POST", headers: { authorization: "Bearer k" }, body: '{"a":1}' },
      "none",
    );

    expect(res.status).toBe(429);
    expect(await res.json()).toEqual({ error: "no" });
    expect(upstream.seen).toMatchObject([
      { method: "POST", body: { a: 1 }, headers: { authorization: "Bearer k" } },
    ]);
  });

  it("adds nothing of the caller's session", async () => {
    upstream.use(http.get("https://provider.example/x", () => HttpResponse.text("ok")));

    await inRequestContext({ cookie: "asc_auth_key=k", authorization: "Bearer s" }, () =>
      providerFetch("https://provider.example/x", {}, "body"),
    );

    expect(upstream.seen[0]?.headers).not.toHaveProperty("cookie");
    expect(upstream.seen[0]?.headers).not.toHaveProperty("authorization");
  });

  it("without a timeout waits past the upstream timeout and ends on the caller's signal", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.get("https://provider.example/stream", async () => {
        await delay("infinite");
        return HttpResponse.text("never");
      }),
    );
    const controller = new AbortController();
    let settled = false;

    const call = providerFetch(
      "https://provider.example/stream",
      { signal: controller.signal },
      "none",
    )
      .catch((err: unknown) => err)
      .finally(() => {
        settled = true;
      });
    await vi.advanceTimersByTimeAsync(400_000);
    expect(settled).toBe(false);
    controller.abort();

    expect(await call).toMatchObject({ name: "AbortError" });
  });

  it("bounds the wait for the headers by the upstream timeout", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.get("https://mcp.example/mcp", async () => {
        await delay("infinite");
        return HttpResponse.text("never");
      }),
    );

    const call = providerFetch("https://mcp.example/mcp", {}, "body").catch((err: unknown) => err);
    await vi.advanceTimersByTimeAsync(300_000);

    expect(await call).toMatchObject({ message: "upstream timeout after 300000ms" });
  });

  it("ends the wait for the headers on the caller's signal", async () => {
    upstream.use(
      http.get("https://mcp.example/mcp", async () => {
        await delay("infinite");
        return HttpResponse.text("never");
      }),
    );
    const controller = new AbortController();

    const call = providerFetch(
      "https://mcp.example/mcp",
      { signal: controller.signal },
      "body",
    ).catch((err: unknown) => err);
    controller.abort(new Error("closed"));

    expect(await call).toMatchObject({ message: "closed" });
  });

  it("keeps the caller's signal on the body once the headers are in", async () => {
    upstream.use(
      http.get(
        "https://mcp.example/mcp",
        ({ request }) =>
          new HttpResponse(
            new ReadableStream({
              start(stream) {
                stream.enqueue(new TextEncoder().encode("data: 1\n\n"));
                request.signal.addEventListener("abort", () => stream.error(request.signal.reason));
              },
            }),
            { headers: { "content-type": "text/event-stream" } },
          ),
      ),
    );
    const controller = new AbortController();
    const res = await providerFetch(
      "https://mcp.example/mcp",
      { signal: controller.signal },
      "body",
    );
    const read = res.text().catch((err: unknown) => err);

    controller.abort(new Error("transport closed"));
    // The abort errors the body at once: the read has settled before the next macrotask,
    // which a body read on its own would not, as the stream never ends.
    const outcome = await Promise.race([
      read,
      new Promise((resolve) => setImmediate(() => resolve("still reading"))),
    ]);

    expect(outcome).not.toBe("still reading");
    expect(outcome).toBeInstanceOf(Error);
  });

  // The MCP client of the library reads the answer without a signal of its own.
  it("ends a body that stalls after the headers at the upstream timeout, with no signal of the caller", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.post(
        "https://mcp.example/mcp",
        ({ request }) =>
          new HttpResponse(
            new ReadableStream({
              start(stream) {
                stream.enqueue(new TextEncoder().encode("data: 1\n\n"));
                request.signal.addEventListener("abort", () => stream.error(request.signal.reason));
              },
            }),
            { headers: { "content-type": "text/event-stream" } },
          ),
      ),
    );
    const res = await providerFetch("https://mcp.example/mcp", { method: "POST" }, "body");
    let settled = false;
    const read = res
      .text()
      .catch((err: unknown) => err)
      .finally(() => {
        settled = true;
      });

    await vi.advanceTimersByTimeAsync(299_000);
    expect(settled).toBe(false);
    await vi.advanceTimersByTimeAsync(1_000);

    expect(await read).toBeInstanceOf(Error);
  });

  it("keeps the timer while the body is read and clears it once the body has ended", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(http.get("https://mcp.example/mcp", () => HttpResponse.text("ok")));

    const res = await providerFetch("https://mcp.example/mcp", {}, "body");

    expect(vi.getTimerCount()).toBe(1);
    expect(await res.text()).toBe("ok");
    expect(vi.getTimerCount()).toBe(0);
  });

  // The cancel of the stub's body does not settle here, so the test reads the timer as the cancel
  // starts rather than awaiting it: the timer is cleared before the fetch body is cancelled.
  it("clears the timer when the body is cancelled", async () => {
    upstream.use(http.get("https://mcp.example/mcp", () => HttpResponse.text("ok")));
    const set = vi.spyOn(globalThis, "setTimeout");
    const cleared = vi.spyOn(globalThis, "clearTimeout");

    const res = await providerFetch("https://mcp.example/mcp", {}, "body");
    const timer = set.mock.results[set.mock.calls.findIndex(([, ms]) => ms === 300_000)]?.value;
    cleared.mockClear();
    void res.body?.cancel().catch(() => undefined);

    expect(timer).toBeDefined();
    expect(cleared).toHaveBeenCalledWith(timer);
  });

  it("clears the timer for an answer without a body and keeps its status", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.get("https://mcp.example/mcp", () => new HttpResponse(null, { status: 204 })),
    );

    const res = await providerFetch("https://mcp.example/mcp", {}, "body");

    expect(res.status).toBe(204);
    expect(vi.getTimerCount()).toBe(0);
  });

  it("keeps the status, the reason and the headers of the answer", async () => {
    upstream.use(
      http.get("https://mcp.example/mcp", () =>
        HttpResponse.text("no", { status: 503, statusText: "Busy", headers: { "x-a": "b" } }),
      ),
    );

    const res = await providerFetch("https://mcp.example/mcp", {}, "body");

    expect([res.status, res.statusText, res.headers.get("x-a")]).toEqual([503, "Busy", "b"]);
    expect(await res.text()).toBe("no");
  });
});

describe("readBodyCapped", () => {
  const body = (bytes: number[], headers: Record<string, string> = {}) =>
    new Response(new Uint8Array(bytes), { headers });

  it("answers the bytes of a body within the limit", async () => {
    expect(Array.from((await readBodyCapped(body([1, 2, 3]), 3)) ?? [])).toEqual([1, 2, 3]);
  });

  it("answers null for a body that declares more than the limit, or proves larger", async () => {
    expect(await readBodyCapped(body([1], { "content-length": "9" }), 3)).toBeNull();
    expect(await readBodyCapped(body([1, 2, 3, 4]), 3)).toBeNull();
  });

  it("answers no bytes for a response without a body", async () => {
    expect(await readBodyCapped(new Response(null), 3)).toEqual(new Uint8Array(0));
  });
});

describe("readTextCapped", () => {
  it("answers the whole text within the limit and cuts a longer one at the limit", async () => {
    expect(await readTextCapped(new Response("héllo"), 100)).toBe("héllo");
    expect(await readTextCapped(new Response("abcdef"), 4)).toBe("abcd");
  });

  it("answers an empty text for a response without a body", async () => {
    expect(await readTextCapped(new Response(null), 4)).toBe("");
  });
});
