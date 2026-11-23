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

import { EventEmitter } from "node:events";
import type { RequestHandler } from "express";
import type { RequestMethod } from "node-mocks-http";
import { createRequest, createResponse } from "node-mocks-http";
import { requestContextMiddleware } from "../../requestContext.js";

export interface CallInit {
  method?: RequestMethod;
  url?: string;
  body?: unknown;
  query?: Record<string, unknown>;
  params?: Record<string, string>;
  headers?: Record<string, string>;
  cookies?: Record<string, string>;
}

export interface Reply {
  status: number;
  /** The parsed JSON body, or the text that was written (a stream: every chunk, joined). */
  body: unknown;
  headers: Record<string, unknown>;
  /** False when the handler returned without ending the response. */
  ended: boolean;
  /** What the handler passed to `next`, when it did. */
  nextError: unknown;
}

/**
 * Calls one Express handler (a controller method, a middleware) with a request and a response
 * made by node-mocks-http, waits for it, and answers what a client would have received.
 * Whatever sits in front of the handler in the real app (parsers, the auth gate, the request
 * context) is not run: for those, hand `createApp()` to supertest. It answers when the handler
 * returns, so a stream the client closes half-way needs a response of the test's own.
 */
export async function invoke(handler: unknown, init: CallInit = {}): Promise<Reply> {
  const req = createRequest({
    method: init.method ?? "POST",
    url: init.url ?? "/",
    query: init.query ?? {},
    params: init.params ?? {},
    headers: init.headers ?? {},
    cookies: init.cookies ?? {},
  });
  // Set after the request is made: node-mocks-http replaces a falsy body (null, "", 0, false)
  // with {}, and those bodies are what the strict:false JSON parser of the app lets through.
  if ("body" in init) {
    req.body = init.body;
  }
  const res = createResponse({ eventEmitter: EventEmitter, req });
  let nextError: unknown;
  await (handler as RequestHandler)(req, res, (err?: unknown) => {
    nextError = err;
  });
  return {
    status: res.statusCode,
    body: res._isJSON() ? res._getJSONData() : res._getData(),
    headers: res._getHeaders(),
    ended: res._isEndCalled(),
    nextError,
  };
}

/**
 * Runs `fn` inside the real request context of a request with these headers, as the
 * middleware of the app sets it up. For code that reads the forwarded headers or the
 * per-request caches; it needs the real `requestContext.js`, so not in a file that mocks it.
 */
export function inRequestContext<T>(headers: Record<string, string>, fn: () => T): T {
  const req = createRequest({ headers });
  let result: T | undefined;
  let ran = false;
  requestContextMiddleware(req, createResponse(), () => {
    result = fn();
    ran = true;
  });
  if (!ran) {
    throw new Error("the request context middleware did not call next");
  }
  return result as T;
}
