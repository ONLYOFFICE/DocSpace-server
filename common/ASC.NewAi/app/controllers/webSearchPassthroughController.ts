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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { WebSearchEngine } from "@onlyoffice/ai-chat/core";
import type { WebSearchConfig } from "@onlyoffice/ai-chat/core";

import logger from "../log.js";
import { isObject } from "../narrow.js";
import { markForwardHeadersToProvider } from "../requestContext.js";
import { storage } from "../storage/index.js";
import { asyncHandler } from "./_helpers.js";

// Web-search passthrough for the document editor's AI plugin.
//
// In the editor the `web_search` / `web_crawling` tools run inside the
// plugin's client-side engine, but the plugin only holds a placeholder
// config — the real provider (ONLYOFFICE gateway or Exa) and its key
// live in the portal's web-search settings and must not reach the
// browser. The plugin's request arrives here via the same
// `ai_onExternalFetch` bridge as chat completions, always in the
// ONLYOFFICE wire shape (`{ engine: "exa", query | urls, ... }`), and
// this controller re-dispatches it against the portal's active config,
// mirroring the provider branching of ai-chat's web-search source.
//
// The plugin's `formatResponse` treats any non-2xx as an error payload,
// so provider failures pass through with their status and body.

const EXA_BASE_URL = "https://api.exa.ai";

const engine = new WebSearchEngine({ storage });

function isExaProvider(provider: string): boolean {
  return provider.toLowerCase() === "exa";
}

function normalizeBaseUrl(baseUrl: string): string {
  return baseUrl.endsWith("/") ? baseUrl : `${baseUrl}/`;
}

type UpstreamRequest = {
  url: string;
  headers: Record<string, string>;
  body: string;
};

// Rebuild the outgoing request for the portal's active provider. The
// incoming body is always the ONLYOFFICE-branch shape produced by the
// plugin's placeholder config; the Exa branch re-encodes it exactly the
// way ai-chat's own source does for that provider.
function buildUpstream(
  config: WebSearchConfig,
  subPath: "search" | "contents",
  incoming: Record<string, unknown>,
): UpstreamRequest | undefined {
  if (config.isCloudProvider || !isExaProvider(config.provider)) {
    const baseUrl =
      config.baseUrl || (config.isCloudProvider ? config.provider : "");
    if (!baseUrl) {
      return undefined;
    }
    return {
      url: new URL(subPath, normalizeBaseUrl(baseUrl)).href,
      headers: {
        "Content-Type": "application/json",
        ...(config.key ? { Authorization: `Bearer ${config.key}` } : {}),
        ...config.headers,
      },
      body: JSON.stringify(incoming),
    };
  }
  return {
    url: `${EXA_BASE_URL}/${subPath}`,
    headers: {
      "Content-Type": "application/json",
      "x-api-key": config.key ?? "",
    },
    body: JSON.stringify(
      subPath === "search"
        ? {
            query: incoming["query"],
            text: true,
            numResults: 5,
            livecrawl: "preferred",
          }
        : { urls: incoming["urls"], text: true },
    ),
  };
}

function passthrough(subPath: "search" | "contents") {
  return asyncHandler(async (req, res) => {
    // Needed so an ONLYOFFICE-gateway config resolved from storage gets
    // the caller's forwarded auth headers, same as the chat engine does.
    markForwardHeadersToProvider();

    const config = await engine.getActiveConfig(undefined);
    if (!config) {
      res.status(404).json({ error: "Web search is not configured" });
      return;
    }

    const incoming = isObject(req.body) ? req.body : {};
    const upstream = buildUpstream(config, subPath, incoming);
    if (!upstream) {
      res.status(404).json({ error: "Web search is not configured" });
      return;
    }

    const controller = new AbortController();
    res.on("close", () => {
      if (!res.writableEnded && !controller.signal.aborted) {
        controller.abort();
      }
    });

    let response: globalThis.Response;
    try {
      response = await fetch(upstream.url, {
        method: "POST",
        headers: upstream.headers,
        body: upstream.body,
        signal: controller.signal,
      });
    } catch (err) {
      if (err instanceof Error && err.name === "AbortError") {
        return;
      }
      // Detail stays in the log — the error can carry the provider URL.
      logger.error(`web-search passthrough: upstream fetch failed: ${err}`);
      res.status(502).json({ error: "Upstream request failed" });
      return;
    }

    res.status(response.status);
    const contentType = response.headers.get("content-type");
    if (contentType) {
      res.setHeader("Content-Type", contentType);
    }
    res.setHeader("Cache-Control", "no-cache, no-transform");
    res.send(Buffer.from(await response.arrayBuffer()));
  });
}

export const webSearchPassthroughController = {
  search: passthrough("search"),
  contents: passthrough("contents"),
};
