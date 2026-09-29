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

import type { CorsOptions } from "cors";

type CorsOrigin = string | RegExp;

/** `AI_CHAT_CORS_ORIGINS` value that turns CORS off for this service alone. */
export const CORS_OFF = "none";

// `https://*.example.com` -> any subdomain of example.com, on that scheme
// and port -- the form the .NET API accepts through
// `SetIsOriginAllowedToAllowWildcardSubdomains`.
function toOrigin(entry: string): CorsOrigin {
  if (!entry.includes("*.")) {
    return entry;
  }
  const escaped = entry
    .split("*.")
    .map((part) => part.replace(/[.+?^${}()|[\]\\/]/g, "\\$&"))
    .join("(?:[^./]+\\.)+");
  return new RegExp(`^${escaped}$`);
}

/**
 * The CORS policy of this service, or `null` for none.
 *
 * It follows the portal's own API (`BaseStartup` in ASC.Api.Core), which
 * reads `core:cors` from the shared appsettings: `*` answers any origin but
 * never with credentials, so a cross-origin caller has to carry its own
 * `Authorization` header -- an API key or an OAuth token -- and a browser
 * never attaches the portal's session cookie to it; a list of origins is
 * answered with credentials, for that origin alone. An empty `core:cors`
 * turns CORS off, as it does for the .NET API.
 *
 * `AI_CHAT_CORS_ORIGINS` overrides the shared value for this service only:
 * `none` turns CORS off here while the rest of the portal keeps it, a list
 * replaces `core:cors`, and unset or empty follows it.
 *
 * Where the two services differ: this one splits either value on commas, so
 * several origins can be listed; the .NET API passes `core:cors` to
 * `WithOrigins` whole, so a comma-separated `core:cors` works here and
 * matches nothing there. A single origin, a wildcard subdomain and `*` mean
 * the same to both. .NET also admits, per request, the origins of the OAuth
 * app a Bearer JWT was issued to (`DynamicCorsPolicyProvider`); this service
 * does not, so with an explicit list an OAuth app's origin has to be in it.
 *
 * Nothing here is reachable without a credential: the auth gate in
 * `routes.ts` still refuses a request that carries neither the cookie nor a
 * header, and the credential is validated downstream as before.
 */
export function resolveCorsOptions(
  envOrigins: string | undefined,
  coreCors: unknown,
): CorsOptions | null {
  const override = envOrigins?.trim() ?? "";
  if (override.toLowerCase() === CORS_OFF) {
    return null;
  }

  const raw = override || (typeof coreCors === "string" ? coreCors : "");

  const entries = raw
    .split(",")
    .map((origin) => origin.trim())
    .filter(Boolean);

  if (entries.length === 0) {
    return null;
  }

  if (entries.includes("*")) {
    return { origin: "*", credentials: false };
  }

  return { origin: entries.map(toOrigin), credentials: true };
}

/** One line for the startup log. */
export function describeCors(options: CorsOptions | null): string {
  if (!options) {
    return "CORS disabled";
  }
  if (options.origin === "*") {
    return "CORS enabled for any origin, without credentials";
  }
  const origins = (options.origin as CorsOrigin[]).map(String).join(", ");
  return `CORS enabled for origins: ${origins}`;
}
