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
import { safeGetAgentEntity } from "../storage/docspaceFilesApi.js";
import type { AgentEntityMeta } from "../storage/docspaceFilesApi.js";
import { asyncHandler } from "./_helpers.js";
import { isObject } from "../narrow.js";

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
// vision/OCR data URLs) pass through untouched.

// Only the endpoints the plugin actually reaches through the connector.
// `models` / `embeddings` / `responses` never travel this way: model
// listing bypasses `externalFetch` in the plugin, `useResponsesApi` is
// dropped for external profiles, and embeddings have no call site.
// Query parameters the plugin uses to describe the round: the host entity
// (agent room) it is chatting with, and the conversation the round belongs to.
// Both are consumed here and never forwarded — the rest of the query string is
// relayed to the provider verbatim, and an unknown parameter is at best
// ignored and at worst a 400.
const ENTITY_ID_PARAM = "entityId";
const SESSION_ID_PARAM = "sessionId";

// The conversation-correlation header the ONLYOFFICE route reads, and the
// Anthropic-style cache breakpoint the Claude backend behind it looks for.
// Both mirror `OnlyOfficeProvider` in `@onlyoffice/ai-chat`.
const SESSION_HEADER = "x-session-id";
const CACHE_CONTROL = { type: "ephemeral" } as const;

// Ceiling for parsing the body. Above it the request is still forwarded and
// still carries `metadata` (spliced without parsing), but the cache
// breakpoints are skipped and logged — a vision/OCR payload of that size is a
// one-off image round, where an ephemeral cache write costs more than the
// reuse it would ever earn.
const MAX_PARSE_BYTES = 2 * 1024 * 1024;

// Attach the round's session id as `x-session-id`, mirroring
// `OnlyOfficeProvider`'s constructor. A header configured on the profile
// itself still wins, as it does in the library. What can NOT reach this point
// is the caller's own `x-session-id`: it is dropped from the forwarded set in
// `requestContext` precisely so a client cannot pick the upstream session, so
// the only source here is the query parameter this route consumes explicitly.
function withSessionHeader(
  headers: Record<string, string>,
  sessionId: string | undefined,
): Record<string, string> {
  if (!sessionId) {
    return headers;
  }
  if (Object.keys(headers).some((name) => name.toLowerCase() === SESSION_HEADER)) {
    return headers;
  }
  return { ...headers, [SESSION_HEADER]: sessionId };
}

// Mark one message as a cache breakpoint, mirroring the library exactly: a
// string content becomes a single text part carrying `cache_control`, an array
// content gets it on its LAST text part, and a message with no text at all is
// left alone (nothing to cache). Unlike the library — whose input is typed —
// this runs on untrusted JSON, so non-object content parts are skipped instead
// of dereferenced.
function withCacheControl(message: Record<string, unknown>): Record<string, unknown> {
  const content = message["content"];
  if (typeof content === "string") {
    if (!content) {
      return message;
    }
    return {
      ...message,
      content: [{ type: "text", text: content, cache_control: CACHE_CONTROL }],
    };
  }
  if (!Array.isArray(content)) {
    return message;
  }
  let lastText = -1;
  for (let i = content.length - 1; i >= 0; i--) {
    const part = content[i];
    if (isObject(part) && part["type"] === "text") {
      lastText = i;
      break;
    }
  }
  if (lastText === -1) {
    return message;
  }
  return {
    ...message,
    content: content.map((part, i) =>
      i === lastText && isObject(part) ? { ...part, cache_control: CACHE_CONTROL } : part,
    ),
  };
}

// Anthropic-style prompt-caching breakpoints on the message list: the leading
// system prompt and the last message of the history, the static prefix the
// Claude backend behind the ONLYOFFICE route can reuse across turns. Same two
// positions the library marks in `OnlyOfficeProvider.getStream`.
function markCacheBreakpoints(messages: unknown[]): unknown[] {
  if (messages.length === 0) {
    return messages;
  }
  const last = messages.length - 1;
  return messages.map((message, i) => {
    if (!isObject(message)) {
      return message;
    }
    const isLeadingSystem = i === 0 && message["role"] === "system";
    return i === last || isLeadingSystem ? withCacheControl(message) : message;
  });
}

// Splice a `metadata` object into a JSON request body without parsing it.
//
// The body is deliberately never deserialized on this path (vision/OCR data
// URLs reach megabytes, and the wire format is owned by the plugin's SDK on
// one end and the provider on the other), so the pair is inserted right after
// the opening brace and every other byte survives untouched. Inserting first
// also means a `metadata` the plugin sent itself parses last and wins, on
// every JSON parser that takes the last duplicate key (Node, System.Text.Json).
//
// Returns the body unchanged when it is not a JSON object — an empty body, a
// top-level array, anything non-JSON — so a malformed request still reaches
// the provider as-is and fails there, not here.
function spliceMetadata(
  body: Buffer,
  contentType: string | undefined,
  entity: AgentEntityMeta,
): Buffer {
  if (!contentType || !contentType.toLowerCase().includes("json")) {
    return body;
  }
  const metadata: Record<string, string> = {};
  if (entity.entityId) {
    metadata["agent_id"] = entity.entityId;
  }
  if (entity.entityTitle) {
    metadata["agent_title"] = entity.entityTitle;
  }
  if (Object.keys(metadata).length === 0) {
    return body;
  }
  const text = body.toString("utf8");
  const open = text.indexOf("{");
  // Only leading whitespace may precede the brace; anything else means this
  // is not a JSON object body.
  if (open === -1 || text.slice(0, open).trim().length > 0) {
    return body;
  }
  const rest = text.slice(open + 1);
  // `{}` — an object with no members takes no separator.
  const separator = rest.trimStart().startsWith("}") ? "" : ",";
  const field = `"metadata":${JSON.stringify(metadata)}${separator}`;
  return Buffer.from(`${text.slice(0, open + 1)}${field}${rest}`, "utf8");
}

// Add everything the ONLYOFFICE route expects on top of the plugin's own
// request: the `metadata` object describing the agent, and (streaming chat
// only) the prompt-caching breakpoints. Parsing is the accurate path — it can
// place breakpoints inside the message list — but the body on this route is
// occasionally megabytes of vision data, so above `MAX_PARSE_BYTES` we fall
// back to splicing `metadata` in and say in the log what was dropped.
//
// `stream: true` gates the breakpoints, matching the library: a one-shot round
// has no follow-up turn to reuse the cache, so the write would be pure cost.
// A body that is not a JSON object, or that fails to parse, is forwarded
// untouched and fails at the provider, exactly as it did before.
function withOnlyofficeExtras(
  body: Buffer,
  contentType: string | undefined,
  entity: AgentEntityMeta,
  cacheBreakpoints: boolean,
  route: string,
): Buffer {
  if (!contentType || !contentType.toLowerCase().includes("json")) {
    return body;
  }
  if (!cacheBreakpoints) {
    return spliceMetadata(body, contentType, entity);
  }
  if (body.length > MAX_PARSE_BYTES) {
    logger.info(
      `${route}: body ${body.length}B over ${MAX_PARSE_BYTES}B — prompt-cache breakpoints skipped, metadata still sent`,
    );
    return spliceMetadata(body, contentType, entity);
  }
  let parsed: unknown;
  try {
    parsed = JSON.parse(body.toString("utf8"));
  } catch {
    return body;
  }
  if (!isObject(parsed)) {
    return body;
  }
  const next: Record<string, unknown> = { ...parsed };
  if (parsed["stream"] === true && Array.isArray(parsed["messages"])) {
    next["messages"] = markCacheBreakpoints(parsed["messages"]);
  }
  return spliceMetadata(Buffer.from(JSON.stringify(next), "utf8"), contentType, entity);
}

const openAiError = (res: Response, status: number, message: string, type: string) =>
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
  sessionId?: string,
): Record<string, string> {
  const headers: Record<string, string> = withSessionHeader(
    {
      "Content-Type": contentType ?? "application/json",
      ...(profile.headers ?? {}),
    },
    sessionId,
  );
  const hasAuthHeader = Object.keys(headers).some((name) => name.toLowerCase() === "authorization");
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

// Hard cap on the buffered body, enforced both up front (declared
// Content-Length) and while reading (the nginx `location ~* /ai` enforces
// the same limit) — legitimate vision/OCR data URLs stay well below.
const MAX_BODY_BYTES = 100 * 1024 * 1024;

class BodyTooLargeError extends Error {}

// Read the raw request body into a single buffer, rejecting anything over
// MAX_BODY_BYTES as it arrives (the declared-length check upstream only
// catches honest clients).
async function readRequestBody(req: Readable): Promise<Buffer> {
  const chunks: Buffer[] = [];
  let total = 0;
  for await (const chunk of req) {
    const buf: Buffer = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk as string);
    total += buf.length;
    if (total > MAX_BODY_BYTES) {
      throw new BodyTooLargeError();
    }
    chunks.push(buf);
  }
  return Buffer.concat(chunks);
}

function passthrough(subPath: string, cacheBreakpoints = false) {
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

    const base = profile.baseUrl.endsWith("/") ? profile.baseUrl.slice(0, -1) : profile.baseUrl;
    const queryIndex = req.originalUrl.indexOf("?");
    const params = new URLSearchParams(
      queryIndex >= 0 ? req.originalUrl.slice(queryIndex + 1) : "",
    );
    const entityIdParam = params.get(ENTITY_ID_PARAM) ?? undefined;
    const sessionIdParam = params.get(SESSION_ID_PARAM) ?? undefined;
    params.delete(ENTITY_ID_PARAM);
    params.delete(SESSION_ID_PARAM);
    const remaining = params.toString();
    const search = remaining.length > 0 ? `?${remaining}` : "";
    const target = `${base}/${subPath}${search}`;

    const declaredLength = Number(req.headers["content-length"]);
    if (Number.isFinite(declaredLength) && declaredLength > MAX_BODY_BYTES) {
      openAiError(res, 413, "Request body too large", "invalid_request_error");
      return;
    }

    // Buffer the body and send it whole, exactly like the OpenAI SDK does.
    // A stream body is not an option here: fetch() strips `Content-Length`
    // for streams (a forbidden header per the spec) and uploads chunked,
    // and the internal gateway proxy (AiGatewayProxyController) attaches
    // the body only when `Request.ContentLength is > 0` — a chunked upload
    // arrives at the provider empty ("unexpected end of JSON input").
    // A buffer body gets an automatic `Content-Length`, matching the SDK's
    // serialized-JSON requests byte for byte.
    let body: Buffer;
    try {
      body = await readRequestBody(req);
    } catch (err) {
      if (err instanceof BodyTooLargeError) {
        openAiError(res, 413, "Request body too large", "invalid_request_error");
        return;
      }
      // Client disconnected mid-upload; nothing to answer.
      logger.warn(`openai passthrough: reading request body failed: ${err}`);
      return;
    }

    // Everything `OnlyOfficeProvider` adds on the engine paths, added here by
    // the host instead — the engine is not involved on this route. Only for the
    // ONLYOFFICE provider: a third-party OpenAI-compatible backend has no use
    // for these fields and may reject an unknown one. The agent title is
    // resolved server-side from the Files API under the caller's credentials,
    // so the plugin can only name an entity, never describe one it does not
    // own; a non-agent scope yields no metadata.
    if (profile.providerType === "onlyoffice") {
      const entity = entityIdParam ? await safeGetAgentEntity(entityIdParam) : {};
      body = withOnlyofficeExtras(
        body,
        req.headers["content-type"],
        entity,
        cacheBreakpoints,
        `openai passthrough ${subPath}`,
      );
    }

    let upstream: globalThis.Response;
    try {
      upstream = await fetch(target, {
        method: "POST",
        headers: providerHeaders(req.headers["content-type"], profile, sessionIdParam),
        body,
        signal: upstreamAbortSignal(res),
      });
    } catch (err) {
      if ((err instanceof Error && err.name === "AbortError") || res.destroyed) {
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
  // Prompt-cache breakpoints are chat-only: image generation carries no
  // message list, and the library marks them in `getStream` alone.
  chatCompletions: passthrough("chat/completions", true),
  imagesGenerations: passthrough("images/generations"),
};
