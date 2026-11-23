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

// An MCP server over the streamable HTTP transport, stubbed at the network with msw: the
// JSON-RPC client of @onlyoffice/ai-chat (SystemToolsSource) runs for real against it.

import { http, HttpResponse } from "./upstream.js";
import type { useUpstream } from "./upstream.js";

export interface McpStubOptions {
  /** `tools` of the `tools/list` result. */
  tools?: unknown[];
  /** The JSON-RPC answer to `tools/call`, by tool name; `result` by default `{ content: [] }`. */
  call?: (name: string, args: unknown) => Record<string, unknown>;
  /** The session id `initialize` hands out; none for a stateless server. */
  sessionId?: string;
}

export interface McpStubRequest {
  method: string;
  /** The JSON-RPC method, or "DELETE" for the session termination. */
  rpc: string;
  headers: Record<string, string>;
  body: unknown;
}

/**
 * Answers `initialize`, `notifications/initialized`, `tools/list`, `tools/call` and DELETE at
 * `url`, and lists the requests this one server received (`useUpstream().seen` carries no host,
 * and two servers are enumerated in parallel).
 */
export function mcpServer(
  upstream: ReturnType<typeof useUpstream>,
  url: string,
  options: McpStubOptions = {},
): McpStubRequest[] {
  const received: McpStubRequest[] = [];
  const sessionHeaders: Record<string, string> =
    options.sessionId === undefined ? {} : { "Mcp-Session-Id": options.sessionId };
  upstream.use(
    http.post(url, async ({ request }) => {
      const message = (await request.json()) as {
        id?: string;
        method: string;
        params?: { name?: string; arguments?: unknown };
      };
      received.push({
        method: "POST",
        rpc: message.method,
        headers: Object.fromEntries(request.headers),
        body: message,
      });
      switch (message.method) {
        case "initialize":
          return HttpResponse.json(
            {
              jsonrpc: "2.0",
              id: message.id,
              result: {
                protocolVersion: "2025-06-18",
                capabilities: { tools: {} },
                serverInfo: { name: "stub", version: "1" },
              },
            },
            { headers: sessionHeaders },
          );
        case "notifications/initialized":
          return new HttpResponse(null, { status: 202 });
        case "tools/list":
          return HttpResponse.json({
            jsonrpc: "2.0",
            id: message.id,
            result: { tools: options.tools ?? [] },
          });
        case "tools/call":
          return HttpResponse.json({
            jsonrpc: "2.0",
            id: message.id,
            ...(options.call?.(message.params?.name ?? "", message.params?.arguments) ?? {
              result: { content: [] },
            }),
          });
        default:
          return HttpResponse.json({ jsonrpc: "2.0", id: message.id, error: { code: -32601 } });
      }
    }),
    http.delete(url, ({ request }) => {
      received.push({
        method: "DELETE",
        rpc: "DELETE",
        headers: Object.fromEntries(request.headers),
        body: undefined,
      });
      return new HttpResponse(null, { status: 200 });
    }),
  );
  return received;
}
