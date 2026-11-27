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
// session: who they are, whether they administer the portal and whether
// they may create rooms.

import { getForwardedHeaders } from "../requestContext.js";
import { refusedAnswer } from "./docspaceFilesApi.js";
import { proxyBaseUrl, withTimeout } from "./httpClient.js";
import { getBoolean, getObject, getString, isObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";
import { errorBodyText } from "./errorBody.js";
import { jsonAnswer, mapped } from "./upstreamShape.js";

export interface DocspaceSelf {
  /** A DocSpace admin other than the owner. */
  isAdmin: boolean;
  isOwner: boolean;
  isRoomAdmin: boolean;
}

/** `GET people/@self`, the role flags the service's checks need. */
export async function getSelf(): Promise<DocspaceSelf> {
  const url = `${proxyBaseUrl}/api/2.0/people/@self`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    const raw = await jsonAnswer(res, "GET people/@self");
    const self = mapped(isObject(raw) ? getObject(raw, "response") : undefined, "GET people/@self");
    return {
      isAdmin: getBoolean(self, "isAdmin") === true,
      isOwner: getBoolean(self, "isOwner") === true,
      isRoomAdmin: getBoolean(self, "isRoomAdmin") === true,
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

/**
 * Whether the caller may create agents and the other AI rooms: the portal
 * owner, DocSpace admins and room admins may, Users and Guests may not.
 */
export async function mayCreateAiRooms(): Promise<boolean> {
  const self = await getSelf();
  return self.isOwner || self.isAdmin || self.isRoomAdmin;
}

/** What the portal answered an upload of a plugin package with. */
export type WebPluginInstall =
  | { installed: true; name: string; version: string; enabled: boolean }
  /** A refusal of the portal: its status and the reason it gave. */
  | { installed: false; status: number; message: string };

/** `WebPluginDto` as an installation reports it: the name, the version and whether it is on. */
export function parseWebPlugin(
  dto: JsonObject | undefined,
): { name: string; version: string; enabled: boolean } | undefined {
  const name = dto ? getString(dto, "name") : undefined;
  const version = dto ? getString(dto, "version") : undefined;
  const enabled = dto ? getBoolean(dto, "enabled") : undefined;
  return name === undefined || version === undefined || enabled === undefined
    ? undefined
    : { name, version, enabled };
}

/**
 * `POST settings/webplugins` (WebPluginsController.AddWebPluginFromFile): installs a plugin
 * package for the portal under the caller's own session, which needs the right to change the
 * portal's settings. A 4xx is the portal's judgement of the caller or of the package and comes
 * back as a value with the reason the portal gave; any other failure throws, and after it the
 * package may or may not be installed.
 */
export async function addWebPlugin(title: string, bytes: Uint8Array): Promise<WebPluginInstall> {
  const url = `${proxyBaseUrl}/api/2.0/settings/webplugins`;
  const form = new FormData();
  form.append("file", new Blob([bytes], { type: "application/zip" }), title);
  const { signal, cancel } = withTimeout(undefined);
  try {
    // No Content-Type here: `fetch` derives the multipart boundary from the body.
    const res = await fetch(url, {
      method: "POST",
      headers: getForwardedHeaders(),
      body: form,
      signal,
    });
    if (res.status >= 400 && res.status < 500) {
      return {
        installed: false,
        status: res.status,
        message: refusalMessage(await errorBodyText(res)),
      };
    }
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    const raw = await jsonAnswer(res, "POST settings/webplugins");
    const plugin = mapped(
      parseWebPlugin(isObject(raw) ? getObject(raw, "response") : undefined),
      "POST settings/webplugins",
    );
    return { installed: true, ...plugin };
  } finally {
    cancel();
  }
}

/** `error.message` of a refusal in the portal's envelope; empty when the body is something else. */
function refusalMessage(body: string): string {
  try {
    const parsed: unknown = JSON.parse(body);
    const error = isObject(parsed) ? getObject(parsed, "error") : undefined;
    return (error ? getString(error, "message") : undefined) ?? "";
  } catch {
    return "";
  }
}
