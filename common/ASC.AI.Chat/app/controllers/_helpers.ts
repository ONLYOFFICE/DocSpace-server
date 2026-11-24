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

import type { ErrorRequestHandler, Request, Response, NextFunction, RequestHandler } from "express";
import logger from "../log.js";
import { AiServiceHttpError } from "../storage/httpClient.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import { isObject, readProp } from "../narrow.js";
import type { SourceKind } from "../storage/docspaceFilesApi.js";
import { UpstreamShapeError } from "../storage/upstreamShape.js";

// Mirror of the widget composer's per-kind attachment cap (ATTACHMENT_LIMIT
// in the chat library's attachments store): at most 5 file parts and 5
// image parts per user message. The UI cannot exceed it, so only direct API
// callers hit this (Bug 82894).
const MAX_ATTACHMENTS_PER_KIND = 5;

/**
 * The most attachments one request of the attachments routes may name:
 * saved, read or bound to a message. Well above what a message carries (the
 * composer names no more than its free slots), so only a direct API caller
 * reaches it; what it bounds is the work one request can start.
 */
export const MAX_ATTACHMENTS_PER_REQUEST = 100;

/**
 * Validation error when a user message carries more attachment parts than
 * the composer allows, or `null` when the message is within limits (or has
 * no part-array content at all — shape errors are for other validators).
 */
export function attachmentLimitError(userMessage: unknown): string | null {
  if (!isObject(userMessage)) {
    return null;
  }
  const content = userMessage["content"];
  if (!Array.isArray(content)) {
    return null;
  }
  let files = 0;
  let images = 0;
  for (const part of content) {
    if (!isObject(part)) continue;
    if (part["type"] === "file") files += 1;
    else if (part["type"] === "image") images += 1;
  }
  if (files > MAX_ATTACHMENTS_PER_KIND || images > MAX_ATTACHMENTS_PER_KIND) {
    return (
      `a message may carry at most ${MAX_ATTACHMENTS_PER_KIND} file and ` +
      `${MAX_ATTACHMENTS_PER_KIND} image attachments ` +
      `(got ${files} file(s), ${images} image(s))`
    );
  }
  return null;
}

const MESSAGE_ROLES: ReadonlySet<string> = new Set(["user", "assistant", "system"]);

// Whether a message body carries something worth storing: non-blank text
// (a string, a string part or a `text` part) or any typed non-text part - a
// file or image attachment, a tool call, a reasoning block. Mirrors the
// send path's prompt check, which accepts an attachment-only message, and
// additionally lets a tool-call-only assistant message through, since the
// widget persists one when it cancels a client-side tool.
function hasMessageContent(content: unknown): boolean {
  if (typeof content === "string") {
    return content.trim().length > 0;
  }
  if (!Array.isArray(content)) {
    return false;
  }
  for (const part of content) {
    if (typeof part === "string") {
      if (part.trim().length > 0) {
        return true;
      }
      continue;
    }
    if (!isObject(part)) {
      continue;
    }
    const type = part["type"];
    if (type === "text") {
      if (typeof part["text"] === "string" && part["text"].trim().length > 0) {
        return true;
      }
      continue;
    }
    if (typeof type === "string" && type.length > 0) {
      return true;
    }
  }
  return false;
}

/**
 * Validation error for a message handed to the thread storage routes, or
 * `null` when it is storable: an object with a known `role` and a content
 * that is a non-blank string or a part list with at least one real part.
 * The storage keeps whatever it is given verbatim, so an empty or
 * role-less body used to be stored and served back as a message
 * (Bugs 84409, 84410, 84412, 84413).
 */
export function invalidMessageError(message: unknown): string | null {
  if (!isObject(message)) {
    return "message is required and must be an object";
  }
  const role = message["role"];
  if (typeof role !== "string" || !MESSAGE_ROLES.has(role)) {
    return "message.role must be one of user, assistant, system";
  }
  if (!hasMessageContent(message["content"])) {
    return "message.content must be a non-empty string or a list with at least one non-empty part";
  }
  return null;
}

export type TypedRequest<ReqBody = unknown, ReqQuery = Record<string, unknown>> = Request<
  Record<string, string>,
  unknown,
  ReqBody,
  ReqQuery
>;

// The query parameters by which a passthrough route (OpenAI, web search) names the entry the
// call is made for. They are read here and stripped from what goes to the provider.
export const ENTITY_ID_PARAM = "entityId";
export const ENTITY_KIND_PARAM = "entityKind";

/** `entityKind` as sent: `file` for a file, a folder (or room) for anything else. */
export function sourceKindOf(value: string | null): SourceKind {
  return value === "file" ? "file" : "folder";
}

type AsyncHandler<ReqBody, ReqQuery> = (
  req: TypedRequest<ReqBody, ReqQuery>,
  res: Response,
  next: NextFunction,
) => Promise<void> | void;

// What is safe to return to the client. Full detail (stack, upstream URL,
// upstream body) is logged server-side only — never echoed back. An
// `AiServiceHttpError` carries the internal `AI_SERVICE_URL` and the
// upstream response body in its `message`, so we forward only its status
// and `statusText`. Errors that opt in via `expose` (e.g. request
// validation) get their message through under a 4xx status; everything
// else collapses to a generic 500.
function clientError(err: unknown): { status: number; message: string } {
  if (err instanceof AiServiceHttpError) {
    return { status: err.status, message: err.statusText || "Upstream error" };
  }
  // A failure relayed from the DocSpace Files API with the caller's own
  // credentials — e.g. a 403 when resolving a room the caller cannot
  // access, or a 404 for a room that does not exist (Bugs 82715, 82816, and
  // every /ai/* route that resolves an entityId). Forward its real
  // status instead of masking it as a generic 500. `message` embeds the
  // internal proxy URL, so relay only the status/reason phrase.
  if (err instanceof DocspaceApiHttpError) {
    return { status: err.status, message: err.statusText || "Upstream error" };
  }
  // The message names the upstream call: it goes to the log, like any other 5xx detail.
  if (err instanceof UpstreamShapeError) {
    return { status: err.status, message: "Bad Gateway" };
  }
  const status = readProp(err, "status");
  const expose = readProp(err, "expose") === true;
  if (expose && typeof status === "number" && status >= 400 && status < 500) {
    return { status, message: err instanceof Error ? err.message : "Bad request" };
  }
  return { status: 500, message: "Internal server error" };
}

/**
 * Library's ApiProvider serializes route arguments as a positional JSON
 * array (`JSON.stringify(args.length === 1 ? args[0] : args)`) — so a
 * call like `assign(actionType, profileId)` arrives as `[a, b]`. Legacy
 * DocSpace clients used named fields. Accept both: positional array
 * mapped to `names`, or a named object with the same keys.
 */
export function unpackPositional<T extends string>(
  body: unknown,
  names: readonly T[],
): Partial<Record<T, unknown>> {
  const [first, ...rest] = names;
  if (first === undefined) {
    return {};
  }
  // Single-arg routes: ApiProvider sends the value directly as the body
  // (no wrapping), so `body` IS the arg — be it a string, array, or
  // object. Wrap it under the single name. Legacy callers that send
  // `{name: value}` are detected via the `name in body` check.
  if (rest.length === 0) {
    if (isObject(body) && first in body) {
      return body as Partial<Record<T, unknown>>;
    }
    if (body === undefined || body === null) {
      return {};
    }
    const out: Partial<Record<T, unknown>> = {};
    out[first] = body;
    return out;
  }
  // Multi-arg routes: ApiProvider sends a positional JSON array. Legacy
  // callers send a named object. Differentiate by shape.
  if (Array.isArray(body)) {
    const out: Partial<Record<T, unknown>> = {};
    names.forEach((name, i) => {
      out[name] = body[i];
    });
    return out;
  }
  if (isObject(body)) {
    return body as Partial<Record<T, unknown>>;
  }
  return {};
}

function errorDetails(err: unknown): string {
  if (err instanceof Error) {
    const parts = [`${err.name}: ${err.message}`];
    if (err.stack) {
      parts.push(err.stack);
    }
    const cause = err.cause;
    if (cause !== undefined) {
      parts.push(`cause: ${errorDetails(cause)}`);
    }
    return parts.join("\n");
  }
  try {
    return JSON.stringify(err);
  } catch {
    return String(err);
  }
}

/**
 * The application's error handler, registered after the routes: it answers what was raised
 * before a handler ran (by a body parser) in the `{ error }` JSON of every other refusal, except
 * a body over the limit, which is refused in plain text.
 */
export const errorHandler: ErrorRequestHandler = (err: unknown, req, res, next) => {
  if (res.headersSent) {
    next(err);
    return;
  }
  // The parser's own message quotes the text it could not parse.
  if (readProp(err, "type") === "entity.parse.failed") {
    res.status(400).json({ error: "The request body is not valid JSON" });
    return;
  }
  // A body over the parser's limit is refused in plain text. A stream client reads every JSON
  // line of an answer as a frame of the stream, and `ASC.AI.Tests` pins that the refusal of
  // `send-with-stream` carries none (ChatHiddenFailureTests).
  if (readProp(err, "type") === "entity.too.large") {
    res.status(413).type("text/plain").send("Payload Too Large");
    return;
  }
  const { status, message } = clientError(err);
  if (status >= 500) {
    logger.error(`${req.method} ${req.originalUrl} failed: ${errorDetails(err)}`);
  }
  res.status(status).json({ error: message });
};

export function asyncHandler<ReqBody = unknown, ReqQuery = Record<string, unknown>>(
  handler: AsyncHandler<ReqBody, ReqQuery>,
): RequestHandler<Record<string, string>, unknown, ReqBody, ReqQuery> {
  return async (req, res, next) => {
    try {
      await handler(req, res, next);
    } catch (err) {
      const { status, message } = clientError(err);
      if (status >= 500) {
        logger.error(`${req.method} ${req.originalUrl} failed: ${errorDetails(err)}`);
      }
      if (!res.headersSent) {
        res.status(status).json({ error: message });
      } else {
        res.end();
      }
    }
  };
}

/**
 * Writes one chunk of a stream and, when the response says its buffer is full, waits until it
 * has drained before the next chunk is pulled: a client that reads slowly then slows the
 * stream down instead of having all of it buffered in memory. A response that is closed has
 * nothing to drain.
 */
async function writeAndDrain(res: Response, chunk: string): Promise<void> {
  if (res.write(chunk) !== false || res.destroyed || res.writableEnded) {
    return;
  }
  await new Promise<void>((resolve) => {
    const done = (): void => {
      res.off("drain", done);
      res.off("close", done);
      resolve();
    };
    res.once("drain", done);
    res.once("close", done);
  });
}

// Idle interval after which a keep-alive frame is emitted on an otherwise
// silent stream. A slow tool call (e.g. `generate_image`) can pause token
// output for tens of seconds; without traffic an upstream proxy's read-timeout
// (nginx default 60s) drops the response mid-stream and the browser sees
// `ERR_INCOMPLETE_CHUNKED_ENCODING`. Keep this well under that timeout.
const STREAM_HEARTBEAT_MS = 10_000;

// How often the idle check runs. It has to be a fraction of the threshold, not
// the threshold itself: on an equal interval a beat lands anywhere between one
// and two thresholds after the last write (activity just before a tick pushes
// the beat to the tick after it), and a skipped beat adds another full
// interval — which would put a 10s keep-alive at 30s, the very limit it exists
// to stay under.
const STREAM_HEARTBEAT_CHECK_MS = STREAM_HEARTBEAT_MS / 4;

// Emit `frame` whenever the stream has been idle for `STREAM_HEARTBEAT_MS`.
// `frame` must be something the client's stream parser ignores: a blank line
// for ndjson (`readNdjson` skips empty lines), an SSE comment for SSE,
// whitespace ahead of a JSON body. Returns `touch` (call after every real
// write to reset the idle timer) and `stop`.
// `canWrite` gates a beat: a relaying caller (the OpenAI passthrough) forwards
// provider bytes verbatim and must not splice a frame into a half-written one.
export function startStreamHeartbeat(
  res: Response,
  frame: string,
  canWrite?: () => boolean,
): { touch: () => void; stop: () => void } {
  let lastActivity = Date.now();
  const timer = setInterval(() => {
    if (res.writableEnded || res.destroyed) {
      return;
    }
    if (canWrite && !canWrite()) {
      return;
    }
    if (Date.now() - lastActivity >= STREAM_HEARTBEAT_MS) {
      res.write(frame);
      lastActivity = Date.now();
    }
  }, STREAM_HEARTBEAT_CHECK_MS);
  timer.unref?.();
  return {
    touch: () => {
      lastActivity = Date.now();
    },
    stop: () => clearInterval(timer),
  };
}

export async function streamNdjson(
  res: Response,
  generator: AsyncIterable<unknown>,
): Promise<void> {
  const iterator = generator[Symbol.asyncIterator]();
  const first = await iterator.next();

  res.setHeader("Content-Type", "application/x-ndjson; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();

  // Blank line: prevents proxy read-timeouts during silent tool calls and is
  // skipped by the lib's `readNdjson` parser.
  const heartbeat = startStreamHeartbeat(res, "\n");
  try {
    let current = first;
    while (!current.done) {
      const event = current.value;
      await writeAndDrain(res, `${JSON.stringify(event)}\n`);
      heartbeat.touch();
      current = await iterator.next();
    }
  } catch (err) {
    logger.error(`stream aborted: ${errorDetails(err)}`);
    // Generic message only — detail is in the server log. The full error can
    // carry the internal AI service URL / upstream body.
    res.write(`${JSON.stringify({ type: "error", message: "stream error" })}\n`);
  } finally {
    heartbeat.stop();
    res.end();
  }
}

// Streams OpenAI-format chunks as Server-Sent Events: each chunk is written
// as a `data:` frame and a normal completion is terminated with the OpenAI
// sentinel `data: [DONE]`. The engine's `sendWithStreamOpenAI` already emits
// a native OpenAI error envelope (`{ error: {...} }`) as an in-stream chunk
// on provider failure; this wrapper only adds a generic error frame (and no
// `[DONE]`, matching OpenAI) if the generator itself throws.
export async function streamOpenAiSse(
  res: Response,
  generator: AsyncIterable<unknown>,
): Promise<void> {
  const iterator = generator[Symbol.asyncIterator]();
  const first = await iterator.next();

  res.setHeader("Content-Type", "text/event-stream; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("Connection", "keep-alive");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();

  // SSE comment line: ignored by any SSE consumer, keeps the connection warm.
  const heartbeat = startStreamHeartbeat(res, ": ping\n\n");
  try {
    let current = first;
    while (!current.done) {
      const chunk = current.value;
      await writeAndDrain(res, `data: ${JSON.stringify(chunk)}\n\n`);
      heartbeat.touch();
      current = await iterator.next();
    }
    res.write("data: [DONE]\n\n");
  } catch (err) {
    logger.error(`openai stream aborted: ${errorDetails(err)}`);
    // Generic message only — detail stays in the server log.
    res.write(
      `data: ${JSON.stringify({
        error: { message: "stream error", type: "server_error", code: null, param: null },
      })}\n\n`,
    );
  } finally {
    heartbeat.stop();
    res.end();
  }
}
