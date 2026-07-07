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

import type { Request, Response, NextFunction, RequestHandler } from "express";
import logger from "../log.js";
import { AiServiceHttpError } from "../storage/httpClient.js";

export type TypedRequest<ReqBody = unknown, ReqQuery = Record<string, unknown>> = Request<
  Record<string, string>,
  unknown,
  ReqBody,
  ReqQuery
>;

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
  const status = (err as { status?: unknown })?.status;
  const expose = (err as { expose?: unknown })?.expose === true;
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
  if (names.length === 0) {
    return {};
  }
  // Single-arg routes: ApiProvider sends the value directly as the body
  // (no wrapping), so `body` IS the arg — be it a string, array, or
  // object. Wrap it under the single name. Legacy callers that send
  // `{name: value}` are detected via the `name in body` check.
  if (names.length === 1) {
    const first = names[0] as T;
    if (
      typeof body === "object"
      && body !== null
      && !Array.isArray(body)
      && first in body
    ) {
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
  if (typeof body === "object" && body !== null) {
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
    const cause = (err as { cause?: unknown }).cause;
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

export function asyncHandler<ReqBody = unknown, ReqQuery = Record<string, unknown>>(
  handler: AsyncHandler<ReqBody, ReqQuery>,
): RequestHandler<Record<string, string>, unknown, ReqBody, ReqQuery> {
  return async (req, res, next) => {
    try {
      await handler(req, res, next);
    } catch (err) {
      logger.error(`${req.method} ${req.originalUrl} failed: ${errorDetails(err)}`);
      if (!res.headersSent) {
        const { status, message } = clientError(err);
        res.status(status).json({ error: message });
      } else {
        res.end();
      }
    }
  };
}

// Idle interval after which a keep-alive frame is emitted on an otherwise
// silent stream. A slow tool call (e.g. `generate_image`) can pause token
// output for tens of seconds; without traffic an upstream proxy's read-timeout
// (nginx default 60s) drops the response mid-stream and the browser sees
// `ERR_INCOMPLETE_CHUNKED_ENCODING`. Keep this well under that timeout.
const STREAM_HEARTBEAT_MS = 10_000;

// Emit `frame` whenever the stream has been idle for `STREAM_HEARTBEAT_MS`.
// `frame` must be something the client's stream parser ignores: a blank line
// for ndjson (`readNdjson` skips empty lines), an SSE comment for SSE. Returns
// `touch` (call after every real write to reset the idle timer) and `stop`.
function startStreamHeartbeat(res: Response, frame: string): { touch: () => void; stop: () => void } {
  let lastActivity = Date.now();
  const timer = setInterval(() => {
    if (res.writableEnded || res.destroyed) {
      return;
    }
    if (Date.now() - lastActivity >= STREAM_HEARTBEAT_MS) {
      res.write(frame);
      lastActivity = Date.now();
    }
  }, STREAM_HEARTBEAT_MS);
  timer.unref?.();
  return {
    touch: () => { lastActivity = Date.now(); },
    stop: () => clearInterval(timer),
  };
}

export async function streamNdjson(
  res: Response,
  generator: AsyncIterable<unknown>,
): Promise<void> {
  res.setHeader("Content-Type", "application/x-ndjson; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();

  // Blank line: prevents proxy read-timeouts during silent tool calls and is
  // skipped by the lib's `readNdjson` parser.
  const heartbeat = startStreamHeartbeat(res, "\n");
  try {
    for await (const event of generator) {
      res.write(`${JSON.stringify(event)}\n`);
      heartbeat.touch();
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
  res.setHeader("Content-Type", "text/event-stream; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("Connection", "keep-alive");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();

  // SSE comment line: ignored by any SSE consumer, keeps the connection warm.
  const heartbeat = startStreamHeartbeat(res, ": ping\n\n");
  try {
    for await (const chunk of generator) {
      res.write(`data: ${JSON.stringify(chunk)}\n\n`);
      heartbeat.touch();
    }
    res.write("data: [DONE]\n\n");
  } catch (err) {
    logger.error(`openai stream aborted: ${errorDetails(err)}`);
    // Generic message only — detail stays in the server log.
    res.write(
      `data: ${JSON.stringify({
        error: { message: "stream error", type: "server_error" },
      })}\n\n`,
    );
  } finally {
    heartbeat.stop();
    res.end();
  }
}
