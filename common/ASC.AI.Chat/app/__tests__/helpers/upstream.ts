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

import type { DefaultBodyType, JsonBodyType, StrictRequest } from "msw";
import { HttpResponse } from "msw";
import { setupServer } from "msw/node";
import { afterAll, afterEach, beforeAll } from "vitest";

export { http, HttpResponse, delay } from "msw";

export interface SeenRequest {
  method: string;
  /** Path with the query string, without the origin. */
  path: string;
  headers: Record<string, string>;
  /** The JSON body, the text when it is not JSON, undefined when there is none. */
  body: unknown;
}

/**
 * The services this one calls (AI, Files, People, a model provider), stubbed at the network
 * with msw. Unlike a `vi.mock` of `httpClient.js` or a stubbed `fetch`, the real client code
 * runs: its URL, headers, timeout and status mapping are part of what the test sees.
 *
 * Call once at the top of a test file. A request no handler answers fails the test, so a test
 * never reaches the network. `seen` lists the requests of the current test.
 *
 *   const upstream = useUpstream();
 *   upstream.use(http.get("*\/internal\/ai\/profiles", () => docspace([])));
 */
export function useUpstream(): {
  use: ReturnType<typeof setupServer>["use"];
  seen: SeenRequest[];
} {
  const server = setupServer();
  const seen: SeenRequest[] = [];

  const record = async (request: StrictRequest<DefaultBodyType>): Promise<void> => {
    const url = new URL(request.url);
    const text = await request.clone().text();
    let body: unknown;
    if (text !== "") {
      try {
        body = JSON.parse(text);
      } catch {
        body = text;
      }
    }
    seen.push({
      method: request.method,
      path: url.pathname + url.search,
      headers: Object.fromEntries(request.headers),
      body,
    });
  };

  beforeAll(() => {
    server.listen({ onUnhandledRequest: "error" });
    server.events.on("request:start", ({ request }) => {
      void record(request as StrictRequest<DefaultBodyType>);
    });
  });
  afterEach(() => {
    server.resetHandlers();
    seen.length = 0;
  });
  afterAll(() => {
    server.close();
  });

  return { use: server.use.bind(server), seen };
}

/** An answer in the envelope the .NET services wrap a result in. */
export function docspace(response: JsonBodyType, status = 200): Response {
  return HttpResponse.json({ response, count: 1, status: 0, statusCode: status }, { status });
}

/** A failure as a .NET service answers it. */
export function docspaceError(status: number, message = "error"): Response {
  return HttpResponse.json({ error: { message }, status: 1, statusCode: status }, { status });
}
