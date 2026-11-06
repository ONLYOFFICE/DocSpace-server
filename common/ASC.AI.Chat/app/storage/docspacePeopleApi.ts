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

// The calling user, from the portal's People API under the caller's own
// session: who they are and whether they administer the portal.

import { getForwardedHeaders } from "../requestContext.js";
import { DocspaceApiHttpError } from "./docspaceFilesApi.js";
import { proxyBaseUrl, withTimeout } from "./httpClient.js";
import { getBoolean, getObject, isObject } from "../narrow.js";

export interface DocspaceSelf {
  isAdmin: boolean;
  isOwner: boolean;
}

/** `GET people/@self`, the two flags the settings routes need. */
export async function getSelf(): Promise<DocspaceSelf> {
  const url = `${proxyBaseUrl}/api/2.0/people/@self`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const raw: unknown = await res.json();
    const self = isObject(raw) ? getObject(raw, "response") : undefined;
    if (!self) {
      throw new Error(`DocSpace API: unparseable response from ${url}`);
    }
    return {
      isAdmin: getBoolean(self, "isAdmin") === true,
      isOwner: getBoolean(self, "isOwner") === true,
    };
  } finally {
    cancel();
  }
}

/** Whether the caller may change the portal's settings. */
export async function isPortalAdmin(): Promise<boolean> {
  const self = await getSelf();
  return self.isAdmin || self.isOwner;
}
