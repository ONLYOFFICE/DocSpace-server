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

import { afterEach, describe, expect, it, vi } from "vitest";
import express from "express";
import { accessLog } from "../accessLog.js";
import { logStream } from "../log.js";
import { listen, send, until } from "./helpers/rawHttp.js";

const LIMIT = 100_000;

/**
 * An app with the access log and one route that reads its body the way the passthrough and the
 * document agent's file endpoint do: a `for await` that is left at LIMIT bytes. Leaving the loop
 * destroys the request, and with it the socket the address would be read from.
 */
function appWithLimit(): express.Express {
  const app = express();
  app.use(accessLog());
  app.post("/upload", (req, res) => {
    const read = async (): Promise<number> => {
      let received = 0;
      for await (const chunk of req) {
        received += Buffer.byteLength(chunk as Buffer);
        if (received > LIMIT) {
          throw new Error("over the limit");
        }
      }
      return received;
    };
    read().then(
      (received) => res.json({ received }),
      () => res.status(413).json({ error: "too large" }),
    );
  });
  return app;
}

const lines = (): string[] => vi.mocked(logStream.write).mock.calls.map(([line]) => String(line));

describe("the access log", () => {
  const uncaught: unknown[] = [];
  const onUncaught = (err: unknown): void => {
    uncaught.push(err);
  };

  afterEach(() => {
    process.off("uncaughtException", onUncaught);
    uncaught.length = 0;
  });

  it("writes one combined line with the caller's address for an answered request", async () => {
    const server = await listen(appWithLimit());
    try {
      const reply = await send(server.port, {
        method: "POST",
        path: "/upload?x=1",
        headers: { "user-agent": "vitest", referer: "http://portal.test/" },
        body: "abc",
      }).done;
      expect(reply.status).toBe(200);
      await until(() => lines().length > 0);
      expect(lines()).toHaveLength(1);
      expect(lines()[0]).toMatch(
        /^(::ffff:)?127\.0\.0\.1 - - \[[^\]]+\] "POST \/upload\?x=1 HTTP\/1\.1" 200 14 "http:\/\/portal\.test\/" "vitest"\n$/,
      );
    } finally {
      await server.close();
    }
  });

  it("writes the line, with the address, when the route stopped reading the body and its socket is gone", async () => {
    process.on("uncaughtException", onUncaught);
    const server = await listen(appWithLimit());
    try {
      const exchange = send(server.port, {
        method: "POST",
        path: "/upload",
        body: Buffer.alloc(8 * LIMIT, 0x78),
        chunked: true,
      });
      // The socket goes with the request, so whether the 413 still reaches the client is the
      // route's matter; here only the log line and the process are looked at.
      await exchange.done;
      await until(() => lines().length > 0);
      expect(uncaught).toEqual([]);
      expect(lines()[0]).toMatch(
        /^(::ffff:)?127\.0\.0\.1 - - \[[^\]]+\] "POST \/upload HTTP\/1\.1" 413 /,
      );
    } finally {
      await server.close();
    }
  });
});
