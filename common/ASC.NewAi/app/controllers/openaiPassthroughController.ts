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

import { Readable } from "node:stream";
import { pipeline } from "node:stream/promises";
import type { Response } from "express";

import logger from "../log.js";
import { markForwardHeadersToProvider } from "../requestContext.js";
import { storage } from "../storage/index.js";
import { asyncHandler } from "./_helpers.js";

// OpenAI-compatible passthrough for the document editor's AI plugin.
//
// In the editor the plugin runs with `providerType: "external"` profiles:
// its bundled OpenAI SDK builds ordinary provider requests, and the host
// (DocSpace doceditor) relays them here via the `ai_onExternalFetch`
// connector bridge with `[external]` rewritten to
// `/api/2.0/ai/openai/{profileId}/v1`. This controller resolves the
// profile server-side and forwards the request body to the provider
// verbatim — the wire format is owned by the plugin's SDK on one end and
// the provider on the other, so no translation happens here.
//
// The request body is intentionally NOT parsed: `app.ts` skips the JSON
// body parser for this path so the raw bytes (which can be megabytes for
// vision/OCR data URLs) stream through untouched.

// Only the endpoints the plugin actually reaches through the connector.
// `models` / `embeddings` / `responses` never travel this way: model
// listing bypasses `externalFetch` in the plugin, `useResponsesApi` is
// dropped for external profiles, and embeddings have no call site.
const openAiError = (
  res: Response,
  status: number,
  message: string,
  type: string,
) =>
  res.status(status).json({
    error: { message, type, code: null, param: null },
  });

// The plugin never sends `Authorization` for external profiles (their key
// is empty on the client by design); credentials are attached here from
// the server-side profile, mirroring the ai-chat client factory: profile
// headers win, the key becomes a Bearer token only when no explicit auth
// header is configured.
function providerHeaders(
  contentType: string | undefined,
  profile: { key?: string; headers?: Record<string, string> },
): Record<string, string> {
  const headers: Record<string, string> = {
    "Content-Type": contentType ?? "application/json",
    ...(profile.headers ?? {}),
  };
  const hasAuthHeader = Object.keys(headers).some(
    (name) => name.toLowerCase() === "authorization",
  );
  if (profile.key && !hasAuthHeader) {
    headers["Authorization"] = `Bearer ${profile.key}`;
  }
  return headers;
}

// A client disconnect must cancel the provider call: the plugin's abort
// path closes its fetch, the doceditor bridge aborts its request to us,
// and this signal propagates the cancellation upstream. `writableEnded`
// filters out the normal-completion close.
function upstreamAbortSignal(res: Response): AbortSignal {
  const controller = new AbortController();
  res.on("close", () => {
    if (!res.writableEnded && !controller.signal.aborted) {
      controller.abort();
    }
  });
  return controller.signal;
}

async function readRawBody(req: AsyncIterable<Buffer>): Promise<Buffer> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) {
    chunks.push(chunk as Buffer);
  }
  return Buffer.concat(chunks);
}

function passthrough(subPath: string) {
  return asyncHandler(async (req, res) => {
    // Forwarded auth headers must be marked before the profile resolve so
    // `onlyoffice`-provider profiles get the caller's credentials merged
    // in (same contract as every aiController method).
    markForwardHeadersToProvider();

    const profileId = req.params["profileId"] ?? "";
    const profile = await storage.profiles.readById(profileId);
    if (!profile || !profile.baseUrl) {
      openAiError(res, 404, "Profile not found", "invalid_request_error");
      return;
    }

    const base = profile.baseUrl.endsWith("/")
      ? profile.baseUrl.slice(0, -1)
      : profile.baseUrl;
    const queryIndex = req.originalUrl.indexOf("?");
    const search = queryIndex >= 0 ? req.originalUrl.slice(queryIndex) : "";
    const target = `${base}/${subPath}${search}`;

    const body = await readRawBody(req);

    let upstream: globalThis.Response;
    try {
      upstream = await fetch(target, {
        method: "POST",
        headers: providerHeaders(req.headers["content-type"], profile),
        body,
        signal: upstreamAbortSignal(res),
      });
    } catch (err) {
      if (err instanceof Error && err.name === "AbortError") {
        // Client is gone; nothing to answer.
        return;
      }
      // Detail stays in the log — the error can carry the provider URL.
      logger.error(`openai passthrough: upstream fetch failed: ${err}`);
      openAiError(res, 502, "Upstream request failed", "server_error");
      return;
    }

    // Provider errors (4xx/5xx) pass through with their status and body so
    // the plugin's SDK raises a proper APIError instead of a generic one.
    res.status(upstream.status);
    const contentType = upstream.headers.get("content-type");
    if (contentType) {
      res.setHeader("Content-Type", contentType);
    }
    res.setHeader("Cache-Control", "no-cache, no-transform");
    res.setHeader("X-Accel-Buffering", "no");
    res.flushHeaders?.();

    if (!upstream.body) {
      res.end();
      return;
    }

    try {
      await pipeline(
        Readable.fromWeb(upstream.body as import("node:stream/web").ReadableStream),
        res,
      );
    } catch (err) {
      // Aborted mid-stream (client disconnect) or upstream drop; headers
      // are already sent, so just terminate the response.
      logger.warn(`openai passthrough: stream ended early: ${err}`);
      res.end();
    }
  });
}

export const openaiPassthroughController = {
  chatCompletions: passthrough("chat/completions"),
  imagesGenerations: passthrough("images/generations"),
};
