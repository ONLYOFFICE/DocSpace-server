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

export async function streamNdjson(
  res: Response,
  generator: AsyncIterable<unknown>,
): Promise<void> {
  res.setHeader("Content-Type", "application/x-ndjson; charset=utf-8");
  res.setHeader("Cache-Control", "no-cache, no-transform");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders?.();

  try {
    for await (const event of generator) {
      res.write(`${JSON.stringify(event)}\n`);
    }
  } catch (err) {
    logger.error(`stream aborted: ${errorDetails(err)}`);
    // Generic message only — detail is in the server log. The full error can
    // carry the internal AI service URL / upstream body.
    res.write(`${JSON.stringify({ type: "error", message: "stream error" })}\n`);
  } finally {
    res.end();
  }
}
