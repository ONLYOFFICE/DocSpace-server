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
import { http, HttpResponse, passthrough } from "msw";
import { setupServer } from "msw/node";
import { afterAll, afterEach, beforeAll } from "vitest";

export { http, HttpResponse, delay, passthrough } from "msw";

export interface SeenRequest {
  method: string;
  /** Path with the query string, without the origin. */
  path: string;
  headers: Record<string, string>;
  /**
   * The JSON body, the text when it is not JSON, undefined when there is none. Filled in a
   * moment after the entry appears: assert on it after the call under test has returned, and
   * read a multipart body in the handler instead.
   */
  body: unknown;
}

export interface UpstreamOptions {
  /**
   * Let requests to 127.0.0.1 through to the real network and keep them out of `seen`. For a
   * file that drives `createApp()` with supertest, whose own requests go there.
   */
  localApp?: boolean;
}

const LOCAL_APP = /^http:\/\/127\.0\.0\.1(:\d+)?\//;

/**
 * The services this one calls (AI, Files, People, a model provider), stubbed at the network
 * with msw. Unlike a `vi.mock` of `httpClient.js` or a stubbed `fetch`, the real client code
 * runs: its URL, headers, timeout and status mapping are part of what the test sees.
 *
 * Call once at the top of a test file. A request no handler answers fails the test, so a test
 * never reaches the network. `seen` lists the requests of the current test, in the order they
 * were made.
 *
 *   const upstream = useUpstream();
 *   upstream.use(http.get("*\/internal\/ai\/profiles", () => docspace([])));
 *
 * What msw does not do: it does not end the body of an answer when the request is aborted, and
 * it reads an answer to its end, so a stalled or over-long body is a `ReadableStream` the
 * handler drives from `request.signal`. A client that does not use the global `fetch` (the
 * A2A SDK over undici) is not intercepted at all.
 */
export function useUpstream(options: UpstreamOptions = {}): {
  use: ReturnType<typeof setupServer>["use"];
  seen: SeenRequest[];
} {
  const server = setupServer(
    ...(options.localApp ? [http.all(LOCAL_APP, () => passthrough())] : []),
  );
  const seen: SeenRequest[] = [];

  const record = (request: StrictRequest<DefaultBodyType>): void => {
    if (options.localApp && LOCAL_APP.test(request.url)) {
      return;
    }
    const url = new URL(request.url);
    const entry: SeenRequest = {
      method: request.method,
      path: url.pathname + url.search,
      headers: Object.fromEntries(request.headers),
      body: undefined,
    };
    // The entry is listed at once, so `seen` keeps the order of the requests and none of them
    // spills into the next test; the body follows when it has been read.
    seen.push(entry);
    void request
      .clone()
      .text()
      .then((text) => {
        if (text === "") {
          return;
        }
        try {
          entry.body = JSON.parse(text);
        } catch {
          entry.body = text;
        }
      })
      .catch(() => undefined);
  };

  beforeAll(() => {
    server.listen({ onUnhandledRequest: "error" });
    server.events.on("request:start", ({ request }) => {
      record(request as StrictRequest<DefaultBodyType>);
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
