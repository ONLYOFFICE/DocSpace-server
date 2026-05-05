// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import nconf from "../../config/index.js";
import { getForwardedHeaders } from "../requestContext.js";
import { isObject } from "../narrow.js";
import type { AppConfig } from "../types.js";

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

const BASE_URL = resolveProxyUrl();
const API_PREFIX = "/api/2.0/ai";

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
  let url = `${BASE_URL}${API_PREFIX}${path}`;
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
  if (signal) {
    init.signal = signal;
  }
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(body);
  }

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

export const proxyBaseUrl = BASE_URL;
