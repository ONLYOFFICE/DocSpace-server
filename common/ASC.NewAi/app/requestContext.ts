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

import { AsyncLocalStorage } from "async_hooks";
import type { IncomingHttpHeaders } from "http";
import type { Request, Response, NextFunction } from "express";
import type { ForwardedHeaders, RequestContext } from "./types.js";

const HOP_BY_HOP = new Set<string>([
  "host",
  "content-length",
  "content-type",
  "connection",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
  "expect",
  "accept-encoding",
]);

const als = new AsyncLocalStorage<RequestContext>();

function pickForwardableHeaders(rawHeaders: IncomingHttpHeaders | undefined): ForwardedHeaders {
  const out: ForwardedHeaders = {};
  if (!rawHeaders) {
    return out;
  }
  for (const [name, value] of Object.entries(rawHeaders)) {
    if (value === undefined || value === null) {
      continue;
    }
    const lower = name.toLowerCase();
    if (HOP_BY_HOP.has(lower)) {
      continue;
    }
    out[lower] = Array.isArray(value) ? value.join(", ") : String(value);
  }
  return out;
}

export function requestContextMiddleware(req: Request, _res: Response, next: NextFunction): void {
  const ctx: RequestContext = {
    headers: pickForwardableHeaders(req.headers),
  };
  als.run(ctx, () => next());
}

export function getForwardedHeaders(): ForwardedHeaders {
  return als.getStore()?.headers ?? {};
}

// Opt the current request into forwarding the client headers down to the
// provider (see RequestContext.forwardHeadersToProvider). Set by the
// chat-stream handlers; read when the provider profile is resolved.
export function markForwardHeadersToProvider(): void {
  const store = als.getStore();
  if (store) {
    store.forwardHeadersToProvider = true;
  }
}

export function shouldForwardHeadersToProvider(): boolean {
  return als.getStore()?.forwardHeadersToProvider === true;
}
