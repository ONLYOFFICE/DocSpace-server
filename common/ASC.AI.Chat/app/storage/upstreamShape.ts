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

import type { JsonObject } from "../narrow.js";
import { isObject } from "../narrow.js";
import { clientFault } from "../httpErrors.js";

/**
 * An upstream answered 200 with a body the mapper cannot read. That is neither "absent" (the
 * services answer 404 for that) nor "empty": what the upstream holds is unknown, so the caller
 * gets a 502 and can retry (`ts-ai-failure-paths.md` §1). The message names the call, never
 * the body.
 */
export class UpstreamShapeError extends Error {
  readonly status = 502;

  constructor(call: string) {
    super(`${call}: the answer has an unexpected shape`);
    this.name = "UpstreamShapeError";
  }
}

/** What a mapper made of a 200 answer; `null` or `undefined` means it could not read the body. */
export function mapped<T>(value: T | null | undefined, call: string): T {
  if (value === null || value === undefined) {
    throw new UpstreamShapeError(call);
  }
  return value;
}

/**
 * The answer of a single-object read where nothing stored is a legal answer: an object, or `null`
 * for a 204 or a `response: null`. Anything else (a list, a string, a number) is unknown.
 */
export function objectOrNone(raw: unknown, call: string): JsonObject | null {
  if (raw === null || raw === undefined) {
    return null;
  }
  if (!isObject(raw)) {
    throw new UpstreamShapeError(call);
  }
  return raw;
}

/** The answer of a call that always answers a list. */
export function listAnswer(raw: unknown, call: string): unknown[] {
  if (!Array.isArray(raw)) {
    throw new UpstreamShapeError(call);
  }
  return raw;
}

/**
 * The answer of a call relayed with the portal API's envelope (`raw: true`): a JSON object that
 * carries `response`. Anything else - a text or HTML body of a proxy in front of the service,
 * `{}`, an `{ error }` sent with 200 - is not that envelope: it is unknown, answered 502 and
 * never relayed as a 200 (ts-ai-failure-paths.md §1). Every route that relays an envelope reads
 * it here. An action that returns nothing is wrapped without `response`
 * (`{ count, status, statusCode }`): its route passes `emptyResult`, and the envelope is then
 * known by its `status`.
 */
export function envelopeAnswer(raw: unknown, call: string, emptyResult = false): JsonObject {
  if (!isObject(raw) || !("response" in raw || (emptyResult && "status" in raw))) {
    throw new UpstreamShapeError(call);
  }
  return raw;
}

/**
 * An id that names no entity once it stands in a path. Empty, `GET <path>/` reaches the list
 * route of the same path, whose array the single-entity mapper cannot read. `.` and `..` pass
 * `encodeURIComponent` unchanged, and the URL parser then removes them as dot segments:
 * `threads/.` is requested as `threads/`, `threads/..` as the parent. A read by id answers
 * "absent" for such an id without calling out.
 */
export function namesNoEntity(id: string): boolean {
  return id === "" || id === "." || id === "..";
}

/**
 * Refuses an upstream path one of whose segments {@link namesNoEntity}: the request would reach
 * another route than the one written (a list, a bulk write or delete, the parent path). The
 * three clients call it for every path they are given, so a write or a delete by such an id
 * answers 404 like any id the upstream does not know, without a request. `path` is the encoded
 * path, with or without a leading slash and a query string.
 */
export function assertEntityPath(path: string): void {
  const [pathname = ""] = path.split("?", 1);
  const segments = pathname.replace(/^\//, "").split("/");
  if (segments.some(namesNoEntity)) {
    throw clientFault(404, "Not Found");
  }
}

/**
 * The JSON of a 2xx answer. A body that is not JSON is an answer of an unknown shape
 * ({@link UpstreamShapeError}); any other failure of the read (a timeout, a dropped
 * connection) is thrown as it came.
 */
export async function jsonAnswer(res: Response, call: string): Promise<unknown> {
  try {
    return (await res.json()) as unknown;
  } catch (err) {
    if (err instanceof SyntaxError) {
      throw new UpstreamShapeError(call);
    }
    throw err;
  }
}
