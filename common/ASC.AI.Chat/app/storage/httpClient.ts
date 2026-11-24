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

import { getAiServiceUrl, getProxyUrl, getUpstreamTimeoutMs } from "../../config/index.js";
import { countUpstreamCall, countUpstreamRead, getForwardedHeaders } from "../requestContext.js";
import { isObject } from "../narrow.js";
import { cancelReader, errorBodyText, readTextCapped } from "./errorBody.js";

export { readTextCapped };
import { jsonAnswer } from "./upstreamShape.js";

// Hard ceiling for a single upstream round-trip so a hung AI service / MCP
// server can't pin a Node socket (and the caller's request) indefinitely.
// Generous by default because tool calls proxied through the .NET service
// can be slow; override with `AI_CHAT_UPSTREAM_TIMEOUT_MS`. Note this guards
// request/response calls only — long-lived chat *streams* are bounded by
// the client-driven abort signal, not by this timeout.
const UPSTREAM_TIMEOUT_MS = getUpstreamTimeoutMs();

/**
 * Build an {@link AbortSignal} that fires on a timeout, on an upstream
 * `external` signal (e.g. client disconnect), or both. Returns a `cancel`
 * that clears the timer and detaches the listener — always call it (in a
 * `finally`) once the request settles so the timer can't leak.
 */
export function withTimeout(
  external: AbortSignal | undefined,
  ms: number = UPSTREAM_TIMEOUT_MS,
): { signal: AbortSignal; cancel: () => void } {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(new Error(`upstream timeout after ${ms}ms`)), ms);
  const onAbort = (): void => controller.abort((external as { reason?: unknown })?.reason);
  if (external) {
    if (external.aborted) {
      onAbort();
    } else {
      external.addEventListener("abort", onAbort, { once: true });
    }
  }
  const cancel = (): void => {
    clearTimeout(timer);
    external?.removeEventListener("abort", onAbort);
  };
  return { signal: controller.signal, cancel };
}

export class AiServiceHttpError extends Error {
  public readonly status: number;
  public readonly statusText: string;
  public readonly body: string;
  public readonly url: string;

  constructor(status: number, statusText: string, body: string, url: string) {
    super(`AI service ${status} ${statusText} for ${url}: ${body}`);
    this.status = status;
    this.statusText = statusText;
    this.body = body;
    this.url = url;
  }
}

const PROXY_BASE_URL = getProxyUrl();
const AI_BASE_URL = getAiServiceUrl();
const API_PREFIX = "/internal/ai";

// A string array is sent as a repeated parameter (`tags=a&tags=b`), the
// way ASP.NET binds a list from the query.
export type QueryValue =
  string | number | boolean | null | undefined | readonly (string | number | boolean)[];

export interface RequestOptions {
  // A `FormData` goes out as multipart (an upload), anything else as JSON.
  body?: unknown;
  query?: Record<string, QueryValue>;
  signal?: AbortSignal;
  // When true, return the upstream JSON exactly as received, keeping the
  // DocSpace `{ response, status, statusCode, count, total }` envelope.
  // Used for routes consumed by the DocSpace client (which reads
  // `response.data.response`); engine-backed storage calls leave this off
  // and get the unwrapped payload.
  raw?: boolean;
}

function unwrapDocSpaceEnvelope(json: unknown): unknown {
  if (isObject(json) && "response" in json && "status" in json) {
    return json.response;
  }
  return json;
}

async function jsonRequest(
  baseUrl: string,
  method: string,
  path: string,
  options: RequestOptions = {},
): Promise<unknown> {
  const { body, query, signal, raw } = options;
  let url = `${baseUrl}${path}`;
  if (query && Object.keys(query).length > 0) {
    const params = new URLSearchParams();
    for (const [k, v] of Object.entries(query)) {
      if (Array.isArray(v)) {
        for (const item of v) {
          params.append(k, String(item));
        }
      } else if (v !== undefined && v !== null) {
        params.set(k, String(v));
      }
    }
    const qs = params.toString();
    if (qs) {
      url += (url.includes("?") ? "&" : "?") + qs;
    }
  }

  const headers: Record<string, string> = { ...getForwardedHeaders() };
  const init: RequestInit = { method, headers };
  if (body instanceof FormData) {
    // No Content-Type: `fetch` derives the multipart boundary from the body.
    init.body = body;
  } else if (body !== undefined) {
    headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(body);
  }

  const { signal: reqSignal, cancel } = withTimeout(signal);
  init.signal = reqSignal;
  try {
    const res = await fetch(url, init);
    if (!res.ok) {
      const text = await errorBodyText(res);
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    if (res.status === 204) {
      return null;
    }
    const contentType = res.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const json = await jsonAnswer(res, `${method} ${path}`);
      return raw ? json : unwrapDocSpaceEnvelope(json);
    }
    // Awaited here, not returned as a promise: the finally below cancels the
    // timeout signal, and that must happen after the body has been read.
    return await res.text();
  } finally {
    cancel();
  }
}

export async function aiServiceRequest(
  method: string,
  path: string,
  options: RequestOptions = {},
): Promise<unknown> {
  if (method === "GET") {
    countUpstreamRead();
  }
  countUpstreamCall(method);
  return jsonRequest(`${AI_BASE_URL}${API_PREFIX}`, method, path, options);
}

export const aiService = {
  get: (path: string, opts?: RequestOptions): Promise<unknown> =>
    aiServiceRequest("GET", path, opts),
  post: (path: string, body: unknown, opts?: RequestOptions): Promise<unknown> =>
    aiServiceRequest("POST", path, { ...opts, body }),
  put: (path: string, body: unknown, opts?: RequestOptions): Promise<unknown> =>
    aiServiceRequest("PUT", path, { ...opts, body }),
  patch: (path: string, body: unknown, opts?: RequestOptions): Promise<unknown> =>
    aiServiceRequest("PATCH", path, { ...opts, body }),
  delete: (path: string, opts?: RequestOptions): Promise<unknown> =>
    aiServiceRequest("DELETE", path, opts),
};

/**
 * A `GET` of a route the AI service serves outside `internal/ai`: the portal's
 * own `api/2.0/ai/...` routes, such as the model gateway it proxies. `path`
 * starts at the service's root. Same contract as {@link aiService}.
 */
export async function aiServiceRootGet(
  path: string,
  options: RequestOptions = {},
): Promise<unknown> {
  countUpstreamRead();
  countUpstreamCall("GET");
  return jsonRequest(AI_BASE_URL, "GET", path, options);
}

export const proxyBaseUrl = PROXY_BASE_URL;

export const aiServiceBaseUrl = AI_BASE_URL;

/**
 * How {@link providerFetch} bounds a call:
 * - `"body"`: the upstream timeout bounds the call and the reading of its body; the timer is
 *   cleared when the body has been read to its end, has failed or was cancelled, and the caller's
 *   `signal`, when it sends one, ends both earlier. For a caller that reads the answer through code
 *   that passes no signal of its own: the MCP client of `@onlyoffice/ai-chat` reads each answer to
 *   its end with a plain `fetch(url, { method, headers, body })`.
 * - `"none"`: no timeout of its own; the caller's `signal` alone ends the call and the body. For a
 *   relayed stream, ended by the client hanging up, and for a caller that bounds the call and the
 *   reading of its answer with its own {@link withTimeout}.
 */
export type ProviderTimeout = "body" | "none";

/**
 * A request to a third-party address: an OpenAI-compatible provider, a web search provider, an
 * MCP server. The URL is the caller's to check (`assertSafeBaseUrl`) and the headers its to
 * choose: nothing of the caller's session is added here. Answers the `Response` as it came, a
 * non-2xx included, and rejects only when no answer arrived (a refused connection, an abort).
 */
export async function providerFetch(
  url: string | URL,
  init: RequestInit,
  timeout: ProviderTimeout,
): Promise<Response> {
  if (timeout === "none") {
    return fetch(url, init);
  }
  // `AbortSignal.any` rather than `withTimeout`: its `cancel()` would detach the caller's signal
  // along with the timer, and a body the caller can no longer end outlives the caller. The
  // combined signal holds the caller's weakly, so a long-lived transport signal shared by many
  // calls collects no listeners.
  const timer = new AbortController();
  const handle = setTimeout(
    () => timer.abort(new Error(`upstream timeout after ${UPSTREAM_TIMEOUT_MS}ms`)),
    UPSTREAM_TIMEOUT_MS,
  );
  const signal = init.signal ? AbortSignal.any([init.signal, timer.signal]) : timer.signal;
  let res: Response;
  try {
    res = await fetch(url, { ...init, signal });
  } catch (err) {
    clearTimeout(handle);
    throw err;
  }
  if (!res.body) {
    clearTimeout(handle);
    return res;
  }
  // The body is read through a stream of our own, so that the timer ends with it. The abort of the
  // signal errors the body of the fetch, and with it this stream.
  const reader = res.body.getReader();
  const body = new ReadableStream<Uint8Array>({
    async pull(controller) {
      try {
        const { done, value } = await reader.read();
        if (done) {
          clearTimeout(handle);
          controller.close();
        } else {
          controller.enqueue(value);
        }
      } catch (err) {
        clearTimeout(handle);
        controller.error(err);
      }
    },
    cancel(reason) {
      clearTimeout(handle);
      return reader.cancel(reason);
    },
  });
  return new Response(body, {
    status: res.status,
    statusText: res.statusText,
    headers: res.headers,
  });
}

/**
 * The body of a response, or `null` once it proves larger than `limit`
 * bytes: by its declared length, or by what has arrived when it declares
 * none. The rest of an oversized body is not read.
 */
export async function readBodyCapped(res: Response, limit: number): Promise<Uint8Array | null> {
  const declared = Number(res.headers.get("content-length"));
  if (Number.isFinite(declared) && declared > limit) {
    await res.body?.cancel();
    return null;
  }
  if (!res.body) {
    return new Uint8Array(0);
  }
  const reader = res.body.getReader();
  const chunks: Uint8Array[] = [];
  let received = 0;
  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }
      received += value.byteLength;
      if (received > limit) {
        return null;
      }
      chunks.push(value);
    }
  } finally {
    await cancelReader(reader);
  }
  return Buffer.concat(chunks, received);
}
