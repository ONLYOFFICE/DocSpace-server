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

// `https://*.example.com` -> any subdomain of example.com on that scheme and
// port, as `SetIsOriginAllowedToAllowWildcardSubdomains` reads it in .NET.
function toOrigin(origin: string): string | RegExp {
  if (!origin.includes("*.")) {
    return origin;
  }
  const [scheme, rest] = origin.split("*.", 2);
  const escape = (s: string | undefined): string =>
    (s ?? "").replace(/[.*+?^${}()|[\]\\/]/g, "\\$&");
  return new RegExp(`^${escape(scheme)}(?:[^./]+\\.)+${escape(rest)}$`);
}

/**
 * The CORS policy of this service, built from `core:cors` the way
 * `BaseStartup` (ASC.Api.Core) builds `DynamicCorsPolicyName` for the .NET
 * services: no value -- no CORS at all; otherwise that one origin (or `*`),
 * any header, any method, and credentials unless it is `*`.
 */
export function corsOptions(coreCors: string | undefined): CorsOptions | null {
  if (!coreCors) {
    return null;
  }
  if (coreCors === "*") {
    return { origin: "*" };
  }
  // A list, not the bare string: with a string `cors` would answer every
  // origin with it, while .NET answers only the origin that matches.
  // Lowercased as `WithOrigins` normalizes it: browsers send a lowercase
  // `Origin`, so `https://Portal.Example.com` must still match.
  return { origin: [toOrigin(coreCors.toLowerCase())], credentials: true };
}
