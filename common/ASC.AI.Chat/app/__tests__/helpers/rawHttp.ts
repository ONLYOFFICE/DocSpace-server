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

import type { AddressInfo } from "node:net";
import { connect } from "node:net";
import type { Server } from "node:http";
import type { Express } from "express";

// A raw HTTP/1.1 client for the tests of the passthroughs. It speaks to the app over a plain
// TCP socket, which msw does not intercept (msw patches `http.request` and `fetch`, the way the
// app reaches its upstreams), and it exposes what supertest hides: the bytes of a reply as they
// arrive, before the reply ends, and a client that goes away mid-request.

export interface RawRequest {
  method?: string;
  path: string;
  headers?: Record<string, string>;
  /** Sent with a Content-Length, unless `chunked` is set. */
  body?: Buffer | string;
  /** Send the body with `Transfer-Encoding: chunked` and no Content-Length. */
  chunked?: boolean;
}

export interface RawReply {
  status: number;
  /** Header names in lower case. */
  headers: Record<string, string>;
  /** The body as the client sees it, with the chunked framing removed. */
  body: string;
}

export interface Exchange {
  /** The reply as far as it has arrived; undefined before the status line and headers. */
  current(): RawReply | undefined;
  /**
   * Resolves when the reply is complete (its last chunk, or its Content-Length) or the server
   * has closed the connection. A reply that keeps the connection open (`Connection: keep-alive`
   * set by the handler) is complete without a close; the client then closes the socket.
   */
  done: Promise<RawReply>;
  /** The client goes away: the socket is destroyed. */
  abort(): void;
  /** True once the socket is closed, by either side. */
  closed(): boolean;
}

function dechunk(raw: string): { body: string; complete: boolean } {
  let out = "";
  let rest = raw;
  for (;;) {
    const lineEnd = rest.indexOf("\r\n");
    if (lineEnd < 0) {
      return { body: out, complete: false };
    }
    const size = Number.parseInt(rest.slice(0, lineEnd), 16);
    if (!Number.isFinite(size)) {
      return { body: out, complete: false };
    }
    if (size === 0) {
      return { body: out, complete: true };
    }
    const start = lineEnd + 2;
    if (rest.length < start + size) {
      return { body: out, complete: false };
    }
    out += rest.slice(start, start + size);
    rest = rest.slice(start + size + 2);
  }
}

function parse(bytes: Buffer): (RawReply & { complete: boolean }) | undefined {
  const text = bytes.toString("latin1");
  const headEnd = text.indexOf("\r\n\r\n");
  if (headEnd < 0) {
    return undefined;
  }
  const [statusLine = "", ...lines] = text.slice(0, headEnd).split("\r\n");
  const headers: Record<string, string> = {};
  for (const line of lines) {
    const colon = line.indexOf(":");
    headers[line.slice(0, colon).trim().toLowerCase()] = line.slice(colon + 1).trim();
  }
  const rawBody = text.slice(headEnd + 4);
  const length = headers["content-length"];
  const { body, complete } =
    headers["transfer-encoding"] === "chunked"
      ? dechunk(rawBody)
      : { body: rawBody, complete: length !== undefined && rawBody.length >= Number(length) };
  return {
    status: Number(statusLine.split(" ")[1]),
    headers,
    body: Buffer.from(body, "latin1").toString("utf8"),
    complete,
  };
}

export function send(port: number, request: RawRequest): Exchange {
  const socket = connect(port, "127.0.0.1");
  const chunks: Buffer[] = [];
  let isClosed = false;
  const body =
    request.body === undefined
      ? undefined
      : Buffer.isBuffer(request.body)
        ? request.body
        : Buffer.from(request.body, "utf8");
  const head = [
    `${request.method ?? "POST"} ${request.path} HTTP/1.1`,
    "Host: chat.test",
    "Connection: close",
    ...Object.entries(request.headers ?? {}).map(([name, value]) => `${name}: ${value}`),
  ];
  if (body !== undefined) {
    head.push(
      request.chunked ? "Transfer-Encoding: chunked" : `Content-Length: ${String(body.length)}`,
    );
  }
  socket.write(`${head.join("\r\n")}\r\n\r\n`);
  if (body !== undefined) {
    if (request.chunked) {
      socket.write(`${body.length.toString(16)}\r\n`);
      socket.write(body);
      socket.write("\r\n0\r\n\r\n");
    } else {
      socket.write(body);
    }
  }
  // The server may close a connection whose upload it refused while the client still writes.
  socket.on("error", () => undefined);
  const done = new Promise<RawReply>((resolve) => {
    const finish = (): void => {
      const reply = parse(Buffer.concat(chunks));
      resolve(
        reply
          ? { status: reply.status, headers: reply.headers, body: reply.body }
          : { status: 0, headers: {}, body: "" },
      );
    };
    socket.on("data", (chunk: Buffer) => {
      chunks.push(chunk);
      if (parse(Buffer.concat(chunks))?.complete) {
        finish();
        socket.destroy();
      }
    });
    socket.on("close", () => {
      isClosed = true;
      finish();
    });
  });
  return {
    current: () => {
      const reply = parse(Buffer.concat(chunks));
      return reply && { status: reply.status, headers: reply.headers, body: reply.body };
    },
    done,
    abort: () => socket.destroy(),
    closed: () => isClosed,
  };
}

/** One turn of the real event loop: socket I/O happens here, whatever the fake timers do. */
export function ioTurn(): Promise<void> {
  return new Promise((resolve) => setImmediate(resolve));
}

/** Waits on real I/O until `condition` holds; fails after `turns` turns of the event loop. */
export async function until(condition: () => boolean, turns = 2000): Promise<void> {
  for (let i = 0; i < turns; i += 1) {
    if (condition()) {
      return;
    }
    await ioTurn();
  }
  throw new Error("the condition did not hold in time");
}

export async function listen(app: Express): Promise<{ port: number; close: () => Promise<void> }> {
  const server: Server = await new Promise((resolve) => {
    const started = app.listen(0, "127.0.0.1", () => resolve(started));
  });
  return {
    port: (server.address() as AddressInfo).port,
    close: () =>
      new Promise((resolve) => {
        server.closeAllConnections();
        server.close(() => resolve());
      }),
  };
}
