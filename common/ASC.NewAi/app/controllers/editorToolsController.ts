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

import { composeToolsAdapters } from "@onlyoffice/ai-chat/core";

import { asString, isObject } from "../narrow.js";
import { markForwardHeadersToProvider } from "../requestContext.js";
import {
  DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
  HttpToolsAdapter,
} from "../tools/httpToolsAdapter.js";
import { systemToolsSource } from "../tools/systemTools.js";
import { asyncHandler } from "./_helpers.js";

// DocSpace tools for the document editor's AI plugin.
//
// The editor chat runs its engine client-side (in the plugin), but the
// DocSpace tools — .NET integration tools and admin-configured MCP
// servers — execute here, with the caller's forwarded credentials and
// the server's connections. The plugin registers them as a HostToolGroup
// whose handlers call these endpoints over the same ai_onExternalFetch
// bridge as chat completions and web search.
//
// The tool CATALOG mirrors what the DocSpace chat engine sees (the same
// composed adapter), so tool names and behavior match the DocSpace chat
// exactly. The web-search pair is excluded: the editor already has the
// engine-built-in web_search / web_crawling via the web-search
// passthrough, and a second identical search tool would only confuse
// the model.

const EXCLUDED_TOOLS = new Set(["docspace_web_search", "docspace_web_crawling"]);

const adapter = composeToolsAdapters(systemToolsSource, new HttpToolsAdapter());

// Server types whose tools require an approval dialog before execution —
// the same policy the DocSpace chat engine applies via systemServerTypes.
function approvalServerTypes(): Set<string> {
  return new Set([
    ...systemToolsSource.getServerTypes(),
    DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
  ]);
}

export const editorToolsController = {
  // Sanitized catalog: exactly four fields per tool. Never forward the
  // raw TMCPItem entries — system-server listings can carry transport
  // details that must not reach the browser.
  list: asyncHandler(async (req, res) => {
    markForwardHeadersToProvider();
    const entityId = asString(req.query["entityId"]);

    const grouped = await adapter.getTools(entityId, { attachmentId: [] });
    const approval = approvalServerTypes();

    const tools: Array<{
      name: string;
      description: string;
      inputSchema: Record<string, unknown>;
      requireApproval: boolean;
    }> = [];
    for (const [serverType, items] of Object.entries(grouped)) {
      for (const item of items) {
        if (EXCLUDED_TOOLS.has(item.name)) continue;
        tools.push({
          name: item.name,
          description: item.description ?? "",
          inputSchema: (item.inputSchema ?? {
            type: "object",
            properties: {},
          }) as Record<string, unknown>,
          requireApproval: approval.has(serverType),
        });
      }
    }

    res.json({ tools });
  }),

  call: asyncHandler(async (req, res) => {
    markForwardHeadersToProvider();
    const body = isObject(req.body) ? req.body : {};
    const name = typeof body["name"] === "string" ? body["name"] : "";
    const args = isObject(body["arguments"])
      ? (body["arguments"] as Record<string, unknown>)
      : {};
    const entityId =
      typeof body["entityId"] === "string" ? body["entityId"] : undefined;

    if (!name || EXCLUDED_TOOLS.has(name)) {
      res.status(400).json({ error: "Unknown tool name" });
      return;
    }

    // The adapter returns whatever the tool produced; the plugin relays it
    // to the model as a string. Tool failures come back as `{ error }`
    // payloads from the adapters themselves, so the model sees the error
    // text instead of a broken round.
    const result = await adapter.callTool(name, args, entityId);
    res.json({
      result: typeof result === "string" ? result : JSON.stringify(result ?? {}),
    });
  }),
};
