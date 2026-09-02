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

import { Readable, Transform } from "node:stream";
import { pipeline } from "node:stream/promises";
import type { Response } from "express";

import logger from "../log.js";
import { markForwardHeadersToProvider } from "../requestContext.js";
import { storage } from "../storage/index.js";
import { safeGetAgentEntity } from "../storage/docspaceFilesApi.js";
import type { AgentEntityMeta } from "../storage/docspaceFilesApi.js";
import { asyncHandler, startStreamHeartbeat } from "./_helpers.js";
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

// The plugin's round-trip runs browser → CDN/reverse proxy → nginx → here, and
// a proxy in front of the portal drops a request that has produced no response
// bytes yet (Cloudflare on SaaS: 30s). A single chat round can legitimately
// stay silent that long — a whole document in the prompt, a provider queue, an
// extended-thinking block before the first token — and the plugin's SDK then
// sees a truncated stream instead of the answer it is waiting for. So the
// response is kept warm until the provider speaks: SSE comment frames on a
// streaming round, the same keep-alive the engine routes already emit
// (`streamOpenAiSse`). Two windows need it: the wait for the provider's
// headers (nothing is committed yet) and any gap between the chunks being
// relayed.
//
// A one-shot round has no frame to hide a keep-alive in, and image generation
// routinely runs past 30s — so `images/generations` is padded instead: JSON
// tolerates leading whitespace, and the SDK's `response.json()` parses the
// reply exactly as before (the reply was already chunked, since no upstream
// `Content-Length` is forwarded, so the framing does not change either). The
// cost is the status line, spent on the 200 that the padding commits, so a
// provider error arriving after the window reaches the SDK as its envelope in
// a 200 body instead of an `APIError`.
//
// Which is why padding is opt-in per route (`alwaysJson`), not a fallback for
// every non-streaming request: `images/generations` can never answer with a
// stream, so committing a JSON content type there is safe whatever comes back.
// On `chat/completions` a mis-read `stream` flag would commit JSON over an SSE
// body and break a round that works today, and its non-streaming variant is
// not a path the plugin takes.

// How long the provider may stay silent before the response is committed and
// the keep-alive takes over. Counted from the moment the request reached this
// handler, not from when its body finished uploading: the proxy's clock starts
// at the request, so a megabyte vision body must not eat into the window. A
// third of the tightest known limit (Cloudflare, 30s) leaves room for the
// upload and the profile resolve, and for the first beat after it.
const UPSTREAM_HEADERS_GRACE_MS = 10_000;

// The padded path waits longer: a ping frame costs nothing, but padding spends
// the status line, so it is deferred until the proxy limit is genuinely near.
// Most provider errors (a rejected prompt, a bad size) answer well inside this
// and keep their status; an image request's own body is a prompt, so nothing
// of the window is lost to the upload.
const JSON_PAD_GRACE_MS = 20_000;
const SSE_PING = ": ping\n\n";

// Keep-alive byte for a padded JSON reply: valid leading whitespace, ignored
// by every JSON parser. It may only go out BEFORE the body starts — a pad byte
// spliced into the JSON itself would break the parse — so the heartbeat is
// always stopped before the first relayed chunk.
const JSON_PAD = " ";

// A streaming round, decided without parsing: the plugin's SDK serializes its
// bodies with `JSON.stringify` (and so does the re-serialization above), so a
// byte match on the flag is exact — and the body on this route can be
// megabytes of vision data that must not be parsed twice.
function expectsSse(body: Buffer, accept: string | undefined): boolean {
  return (
    body.includes('"stream":true') || (accept ?? "").toLowerCase().includes("text/event-stream")
  );
}

function commitSseHeaders(res: Response): void {
  res.status(200);
  res.setHeader("Content-Type", "text/event-stream; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("Connection", "keep-alive");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();
}

function commitJsonHeaders(res: Response): void {
  res.status(200);
  res.setHeader("Content-Type", "application/json; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();
}

// Report a failure that happened after the keep-alive already committed a 200:
// the status line is spent, so the SDK has to read the error from inside the
// stream. The provider's own OpenAI-shaped envelope is forwarded when it sent
// one (compacted to a single line — an SSE frame cannot contain a raw
// newline); anything else becomes a generic envelope, with the detail left in
// the server log where it cannot leak the provider URL.
function writeSseError(res: Response, detail: string, status: number): void {
  let envelope: unknown;
  try {
    const parsed: unknown = JSON.parse(detail);
    if (isObject(parsed) && isObject(parsed["error"])) {
      envelope = parsed;
    }
  } catch {
    /* fall through to the generic envelope */
  }
  envelope ??= {
    error: {
      message: `Upstream request failed with status ${status}`,
      type: "server_error",
      code: null,
      param: null,
    },
  };
  res.write(`data: ${JSON.stringify(envelope)}\n\n`);
}

const errorEnvelope = (message: string, type: string) => ({
  error: { message, type, code: null, param: null },
});

const openAiError = (res: Response, status: number, message: string, type: string) =>
  res.status(status).json(errorEnvelope(message, type));

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

function passthrough(subPath: string, cacheBreakpoints = false, alwaysJson = false) {
  return asyncHandler(async (req, res) => {
    const startedAt = Date.now();

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

    const streaming = expectsSse(body, req.headers["accept"]);

    // A keep-alive frame may only go out on a line boundary: the bytes here
    // are the provider's, and splicing a frame into a half-written `data:`
    // line would corrupt it. A completed line is enough — this route is
    // OpenAI-compatible by contract, where every event is a single `data:`
    // line, so the blank line of an injected comment can at worst dispatch an
    // already-complete event one chunk early.
    let atFrameBoundary = true;
    const canPing = () => atFrameBoundary;

    // Settled, not awaited: the grace window below races this promise, and a
    // rejection reaching the race unhandled would take the process down.
    const upstreamResult = fetch(target, {
      method: "POST",
      headers: providerHeaders(req.headers["content-type"], profile, sessionIdParam),
      body,
      signal: upstreamAbortSignal(res),
    }).then(
      (response) => ({ response }) as { response: globalThis.Response },
      (error: unknown) => ({ error }) as { error: unknown },
    );

    // Keep-alive started before the provider answered. Once it exists the
    // response is committed as a 200 — SSE or padded JSON, per `earlyKind` —
    // so every outcome below has to be reported in the body instead of on the
    // status line.
    let earlyHeartbeat: { touch: () => void; stop: () => void } | null = null;
    let earlyKind: "sse" | "json" | null = null;

    // What this round could be kept alive with, if anything: whitespace on a
    // route whose answer is always a JSON body, an SSE comment on a streaming
    // round, nothing otherwise. `alwaysJson` wins over the request's own flag:
    // on such a route a stream cannot come back, so a `stream` flag that only
    // looks set must not commit an SSE content type over a JSON reply.
    const keepAlive = alwaysJson ? "json" : streaming ? "sse" : null;

    if (keepAlive) {
      let graceTimer: ReturnType<typeof setTimeout> | undefined;
      const graceMs = keepAlive === "sse" ? UPSTREAM_HEADERS_GRACE_MS : JSON_PAD_GRACE_MS;
      const grace = new Promise<"grace">((resolve) => {
        graceTimer = setTimeout(
          () => resolve("grace"),
          Math.max(0, graceMs - (Date.now() - startedAt)),
        );
      });
      const first = await Promise.race([upstreamResult.then(() => "upstream" as const), grace]);
      if (graceTimer) {
        clearTimeout(graceTimer);
      }

      if (first === "grace" && !res.headersSent && !res.destroyed) {
        logger.info(
          `openai passthrough ${subPath}: no provider headers after ${Date.now() - startedAt}ms — keeping the response alive (${keepAlive})`,
        );
        if (keepAlive === "sse") {
          commitSseHeaders(res);
          res.write(SSE_PING);
          earlyHeartbeat = startStreamHeartbeat(res, SSE_PING, canPing);
        } else {
          commitJsonHeaders(res);
          res.write(JSON_PAD);
          earlyHeartbeat = startStreamHeartbeat(res, JSON_PAD);
        }
        earlyKind = keepAlive;
      }
    }

    const settled = await upstreamResult;

    if ("error" in settled) {
      const err = settled.error;
      if ((err instanceof Error && err.name === "AbortError") || res.destroyed) {
        // Client is gone; nothing to answer.
        earlyHeartbeat?.stop();
        return;
      }
      // Detail stays in the log — the error can carry the provider URL.
      logger.error(`openai passthrough: upstream fetch failed: ${err}`);
      if (earlyHeartbeat) {
        earlyHeartbeat.stop();
        if (earlyKind === "sse") {
          writeSseError(res, "", 502);
        } else {
          res.write(JSON.stringify(errorEnvelope("Upstream request failed", "server_error")));
        }
        res.end();
        return;
      }
      openAiError(res, 502, "Upstream request failed", "server_error");
      return;
    }

    const upstream = settled.response;
    const contentType = upstream.headers.get("content-type");
    const upstreamIsSse = (contentType ?? "").toLowerCase().includes("text/event-stream");

    if (earlyKind === "json") {
      // The padding has to stop before the body starts: a pad byte spliced
      // into the JSON would break the parse.
      earlyHeartbeat?.stop();
      if (upstreamIsSse) {
        // Unreachable on the routes that enable padding, and unrecoverable if
        // it ever happens: the JSON content type is already on the wire.
        logger.error(
          `openai passthrough ${subPath}: padded reply met a text/event-stream body — the round cannot be relayed`,
        );
      } else if (
        !upstream.ok ||
        !(contentType ?? "application/json").toLowerCase().includes("json")
      ) {
        // The 200 and the JSON content type are on the wire already, so the
        // reply is relayed as-is and the SDK reads the provider's own error
        // envelope out of the body.
        logger.warn(
          `openai passthrough ${subPath}: provider answered ${upstream.status} ${
            contentType ?? "no content-type"
          } after the keep-alive was committed — status downgraded to 200`,
        );
      }
    }

    if (earlyKind === "sse" && !(upstream.ok && upstreamIsSse)) {
      // The 200 is already on the wire, so neither a provider error nor a
      // non-stream answer can be relayed as-is: both are re-framed as a
      // single SSE event the plugin's SDK can raise.
      earlyHeartbeat?.stop();
      const detail = await upstream.text().catch(() => "");
      logger.warn(
        `openai passthrough ${subPath}: provider answered ${upstream.status} ${
          contentType ?? "no content-type"
        } after the keep-alive was committed: ${detail.slice(0, 500)}`,
      );
      writeSseError(res, detail, upstream.status);
      res.end();
      return;
    }

    if (!earlyHeartbeat) {
      // Provider errors (4xx/5xx) pass through with their status and body so
      // the plugin's SDK raises a proper APIError instead of a generic one.
      res.status(upstream.status);
      if (contentType) {
        res.setHeader("Content-Type", contentType);
      }
      res.setHeader("Cache-Control", "no-cache, no-transform");
      res.setHeader("X-Accel-Buffering", "no");
      res.flushHeaders?.();
    }

    // Only an SSE body has a frame the client's parser will ignore, so only
    // that one keeps a heartbeat running while it is relayed — a padded JSON
    // reply must receive nothing more until its body is out.
    const heartbeat =
      earlyKind === "json"
        ? null
        : (earlyHeartbeat ?? (upstreamIsSse ? startStreamHeartbeat(res, SSE_PING, canPing) : null));

    if (!upstream.body) {
      heartbeat?.stop();
      res.end();
      return;
    }

    // The bytes are relayed by `pipeline` exactly as before — it owns
    // backpressure and teardown on a client disconnect — with the keep-alive
    // reading the traffic off a transparent transform on the way through.
    const relay = new Transform({
      transform(chunk: Buffer | string, _encoding, callback) {
        const buf: Buffer = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk);
        heartbeat?.touch();
        atFrameBoundary = buf.length === 0 || buf.subarray(-1).toString("latin1") === "\n";
        callback(null, buf);
      },
    });

    try {
      await pipeline(
        Readable.fromWeb(upstream.body as import("node:stream/web").ReadableStream),
        relay,
        res,
      );
    } catch (err) {
      // Aborted mid-stream (client disconnect) or upstream drop; headers
      // are already sent, so just terminate the response.
      logger.warn(`openai passthrough: stream ended early: ${err}`);
      res.end();
    } finally {
      heartbeat?.stop();
    }
  });
}

export const openaiPassthroughController = {
  // Prompt-cache breakpoints are chat-only: image generation carries no
  // message list, and the library marks them in `getStream` alone.
  chatCompletions: passthrough("chat/completions", true),
  // Padding is enabled here alone: an image round is the one that reliably
  // outlives a 30s proxy limit, and this route always answers with JSON.
  imagesGenerations: passthrough("images/generations", false, true),
};
