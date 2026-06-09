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

import nconf from "../../config/index.js";
import { getForwardedHeaders } from "../requestContext.js";
import { isObject, parseInt10 } from "../narrow.js";
import type { AppConfig } from "../types.js";

// Hard ceiling for a single upstream round-trip so a hung AI service / MCP
// server can't pin a Node socket (and the caller's request) indefinitely.
// Generous by default because tool calls proxied through the .NET service
// can be slow; override with `NEW_AI_UPSTREAM_TIMEOUT_MS`. Note this guards
// request/response calls only — long-lived chat *streams* are bounded by
// the client-driven abort signal, not by this timeout.
const UPSTREAM_TIMEOUT_MS = parseInt10(process.env["NEW_AI_UPSTREAM_TIMEOUT_MS"], 300_000) ?? 300_000;

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
  const timer = setTimeout(
    () => controller.abort(new Error(`upstream timeout after ${ms}ms`)),
    ms,
  );
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

function resolveProxyUrl(): string {
  const fromEnv = process.env["API_HOST"];
  if (fromEnv) {
    return fromEnv.replace(/\/+$/, "");
  }
  const app: AppConfig | undefined = nconf.get("app");
  const fromConfig = app?.proxy?.url;
  if (fromConfig) {
    return fromConfig.replace(/\/+$/, "");
  }
  return "http://localhost:8092";
}

function resolveAiServiceUrl(): string {
  const fromEnv = process.env["AI_SERVICE_URL"];
  if (fromEnv) {
    return fromEnv.replace(/\/+$/, "");
  }
  const app: AppConfig | undefined = nconf.get("app");
  const fromConfig = app?.aiService?.url;
  if (fromConfig) {
    return fromConfig.replace(/\/+$/, "");
  }
  return "http://localhost:5157";
}

const PROXY_BASE_URL = resolveProxyUrl();
const AI_BASE_URL = resolveAiServiceUrl();
const API_PREFIX = "/internal/ai";

export type QueryValue = string | number | boolean | null | undefined;

export interface RequestOptions {
  body?: unknown;
  query?: Record<string, QueryValue>;
  signal?: AbortSignal;
}

function unwrapDocSpaceEnvelope(json: unknown): unknown {
  if (isObject(json) && "response" in json && "status" in json) {
    return json.response;
  }
  return json;
}

export async function aiServiceRequest(
  method: string,
  path: string,
  options: RequestOptions = {},
): Promise<unknown> {
  const { body, query, signal } = options;
  let url = `${AI_BASE_URL}${API_PREFIX}${path}`;
  if (query && Object.keys(query).length > 0) {
    const params = new URLSearchParams();
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null) {
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
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(body);
  }

  const { signal: reqSignal, cancel } = withTimeout(signal);
  init.signal = reqSignal;
  try {
    const res = await fetch(url, init);
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    if (res.status === 204) {
      return null;
    }
    const contentType = res.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const json: unknown = await res.json();
      return unwrapDocSpaceEnvelope(json);
    }
    return res.text();
  } finally {
    cancel();
  }
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

export const proxyBaseUrl = PROXY_BASE_URL;
