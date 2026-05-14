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

import type { Request, Response, NextFunction, RequestHandler } from "express";
import logger from "../log.js";

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

function errorMessage(err: unknown): string {
  if (err instanceof Error) {
    return err.message;
  }
  return String(err);
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
        res.status(500).json({ error: errorMessage(err) });
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
    res.write(`${JSON.stringify({ type: "error", message: errorMessage(err) })}\n`);
  } finally {
    res.end();
  }
}
