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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { proxyBaseUrl, withTimeout } from "./httpClient.js";
import { DocspaceApiHttpError } from "./docspaceFilesApi.js";
import { getForwardedHeaders } from "../requestContext.js";
import { isObject, getString } from "../narrow.js";
import logger from "../log.js";

/**
 * List the MCP servers enabled on an agent room, by name. Backed by the
 * public DocSpace AI API (`GET /api/2.0/ai/rooms/{roomId}/servers`,
 * `McpController.GetRoomServersAsync`), called on behalf of the current
 * user. The room↔server links are the agent's MCP whitelist: a server is
 * available to the agent only while attached to its room, so a server
 * added to the platform later stays off until the agent is re-edited.
 *
 * Resolves to `[]` when the room is not found (404) or has no servers;
 * other HTTP failures propagate as {@link DocspaceApiHttpError}.
 */
export async function getAgentMcpServerNames(agentId: string): Promise<string[]> {
  const url = `${proxyBaseUrl}/api/2.0/ai/rooms/${encodeURIComponent(agentId)}/servers`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      return [];
    }
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const raw: unknown = await res.json();
    const list = isObject(raw) ? raw["response"] : undefined;
    if (!Array.isArray(list)) {
      logger.warn(`getAgentMcpServerNames(${agentId}) -> unparseable response from ${url}`);
      return [];
    }
    const names: string[] = [];
    for (const item of list) {
      if (!isObject(item)) {
        continue;
      }
      const name = getString(item, "name");
      if (name) {
        names.push(name);
      }
    }
    return names;
  } finally {
    cancel();
  }
}
