// Shared test doubles for the metadata modules. The real modules pull in the
// config loader, the file logger and the storage aggregate; none of that is
// under test here, so each test file registers these with `vi.mock` before
// importing the module under test.

import { vi } from "vitest";

export class FakeDocspaceApiHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly url: string,
  ) {
    super(`DocSpace API ${status} ${statusText} for ${url}`);
  }
}

export const logger = { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() };

export const PROXY = "http://portal";

export function withTimeout(external: AbortSignal | undefined): {
  signal: AbortSignal;
  cancel: () => void;
} {
  return { signal: external ?? new AbortController().signal, cancel: () => {} };
}

export const headers = { cookie: "asc_auth_key=test" };

/** A JSON `Response` the way the portal answers: `{ response: ... }`. */
export function portalJson(response: unknown, status = 200): Response {
  return new Response(JSON.stringify({ response }), {
    status,
    headers: { "content-type": "application/json" },
  });
}

export function textResponse(text: string, status = 200): Response {
  return new Response(text, { status, headers: { "content-type": "text/plain" } });
}

/** A `fetch` double that answers by URL substring and method, recording calls. */
export function fakeFetch(
  routes: { match: (url: string, init?: RequestInit) => boolean; answer: () => Response }[],
) {
  const calls: { url: string; init?: RequestInit }[] = [];
  const fetchMock = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
    calls.push({ url, init });
    const route = routes.find((r) => r.match(url, init));
    if (!route) {
      return new Response("not found", { status: 404, statusText: "Not Found" });
    }
    return route.answer();
  });
  return { fetchMock, calls };
}

/** An assistant message as `customSync` returns it, with one text part. */
export function assistantText(text: string) {
  return { role: "assistant", content: [{ type: "text", text }] };
}
