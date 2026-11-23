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

// The error boundary of every controller method: what a caller sees for each kind of error,
// what is logged, and what happens once the headers are out.

import { beforeEach, describe, expect, it, vi } from "vitest";
import logger from "../../log.js";
import { invoke } from "../../__tests__/helpers/http.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";
import { DocspaceApiHttpError } from "../../storage/docspaceFilesApi.js";
import { asyncHandler } from "../_helpers.js";

const throwing = (error: unknown) =>
  asyncHandler(() => {
    throw error;
  });

const INTERNAL_AI = "http://ai.internal:5157/internal/ai/profiles/p1";
const INTERNAL_FILES = "http://proxy.internal:8092/api/2.0/files/rooms/12";

beforeEach(() => {
  vi.clearAllMocks();
});

describe("asyncHandler", () => {
  it("leaves the answer to a handler that does not throw", async () => {
    const handler = asyncHandler(async (_req, res) => {
      res.status(201).json({ ok: true });
    });

    const reply = await invoke(handler, { body: {} });

    expect(reply.status).toBe(201);
    expect(reply.body).toEqual({ ok: true });
    expect(reply.nextError).toBeUndefined();
    expect(vi.mocked(logger.error)).not.toHaveBeenCalled();
  });

  it("passes the request, the response and next to the handler", async () => {
    const seen: unknown[] = [];
    const handler = asyncHandler((req, res, next) => {
      seen.push(req.body, typeof res.json, typeof next);
      res.end();
    });

    await invoke(handler, { body: { a: 1 } });

    expect(seen).toEqual([{ a: 1 }, "function", "function"]);
  });

  it.each([
    [
      "an AI service 404",
      new AiServiceHttpError(404, "Not Found", "no row", INTERNAL_AI),
      404,
      "Not Found",
    ],
    [
      "an AI service 403",
      new AiServiceHttpError(403, "Forbidden", "AI is off", INTERNAL_AI),
      403,
      "Forbidden",
    ],
    [
      "an AI service 503",
      new AiServiceHttpError(503, "Service Unavailable", "trace", INTERNAL_AI),
      503,
      "Service Unavailable",
    ],
    [
      "an AI service error without a reason phrase",
      new AiServiceHttpError(502, "", "x", INTERNAL_AI),
      502,
      "Upstream error",
    ],
    [
      "a Files API 403",
      new DocspaceApiHttpError(403, "Forbidden", INTERNAL_FILES),
      403,
      "Forbidden",
    ],
    [
      "a Files API 404",
      new DocspaceApiHttpError(404, "Not Found", INTERNAL_FILES),
      404,
      "Not Found",
    ],
    [
      "a Files API error without a reason phrase",
      new DocspaceApiHttpError(500, "", INTERNAL_FILES),
      500,
      "Upstream error",
    ],
  ])("relays %s with its status and reason phrase only", async (_what, error, status, message) => {
    const reply = await invoke(throwing(error));

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error: message });
    expect(JSON.stringify(reply.body)).not.toContain("internal");
  });

  const exposed = (message: string, fields: Record<string, unknown>): Error =>
    Object.assign(new Error(message), fields);

  it.each([
    [
      "an exposed 400",
      exposed("threadId required", { status: 400, expose: true }),
      400,
      "threadId required",
    ],
    [
      "an exposed 404",
      exposed("the AI profile does not exist", { status: 404, expose: true }),
      404,
      "the AI profile does not exist",
    ],
    ["an exposed 499", exposed("edge", { status: 499, expose: true }), 499, "edge"],
    [
      "an exposed 399",
      exposed("redirect", { status: 399, expose: true }),
      500,
      "Internal server error",
    ],
    [
      "an exposed 500",
      exposed("no model at http://10.0.0.1", { status: 503, expose: true }),
      500,
      "Internal server error",
    ],
    [
      "an exposed status that is a string",
      exposed("x", { status: "404", expose: true }),
      500,
      "Internal server error",
    ],
    ["a 404 that is not exposed", exposed("secret", { status: 404 }), 500, "Internal server error"],
    [
      "expose that is not true",
      exposed("secret", { status: 404, expose: "yes" }),
      500,
      "Internal server error",
    ],
    ["a plain Error", new Error("db password wrong"), 500, "Internal server error"],
    [
      "an exposed object that is not an Error",
      { status: 409, expose: true, message: "taken" },
      409,
      "Bad request",
    ],
    ["a string", "boom", 500, "Internal server error"],
    ["null", null, 500, "Internal server error"],
    ["undefined", undefined, 500, "Internal server error"],
  ])("answers %s as %i", async (_what, error, status, message) => {
    const reply = await invoke(throwing(error));

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error: message });
  });

  it("answers a rejected promise of an async handler like a throw", async () => {
    const handler = asyncHandler(async () => {
      await Promise.resolve();
      throw new AiServiceHttpError(409, "Conflict", "", INTERNAL_AI);
    });

    const reply = await invoke(handler);

    expect(reply.status).toBe(409);
    expect(reply.body).toEqual({ error: "Conflict" });
  });

  it("logs a 5xx with the method, the URL, the error and its causes", async () => {
    const root = new Error("ECONNREFUSED");
    const error = new Error("fetch failed", { cause: root });

    await invoke(throwing(error), { method: "POST", url: "/api/2.0/ai/threads/list?x=1" });

    expect(vi.mocked(logger.error)).toHaveBeenCalledTimes(1);
    const line = String(vi.mocked(logger.error).mock.calls[0]?.[0]);
    expect(
      line.startsWith(
        "POST /api/2.0/ai/threads/list?x=1 failed: Error: fetch failed\nError: fetch failed\n",
      ),
    ).toBe(true);
    expect(line).toContain("\ncause: Error: ECONNREFUSED\nError: ECONNREFUSED\n");
  });

  it("logs an Error without a stack by its name and message", async () => {
    const error = new TypeError("x is not a function");
    error.stack = undefined;

    await invoke(throwing(error), { method: "GET", url: "/a" });

    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      "GET /a failed: TypeError: x is not a function",
    );
  });

  it.each([
    [
      "an object, as JSON",
      { code: "E1", status: 500 },
      'GET /a failed: {"code":"E1","status":500}',
    ],
    ["a string, as JSON", "boom", 'GET /a failed: "boom"'],
    ["undefined", undefined, "GET /a failed: undefined"],
  ])("logs a thrown value that is %s", async (_what, error, line) => {
    await invoke(throwing(error), { method: "GET", url: "/a" });

    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(line);
  });

  it("logs a value JSON cannot write by its string form", async () => {
    const circular: Record<string, unknown> = {};
    circular["self"] = circular;

    await invoke(throwing(circular), { method: "GET", url: "/a" });

    expect(vi.mocked(logger.error)).toHaveBeenCalledWith("GET /a failed: [object Object]");
  });

  it("logs a cause that is not an Error", async () => {
    const error = new Error("outer", { cause: { status: 503 } });
    error.stack = undefined;

    await invoke(throwing(error), { method: "GET", url: "/a" });

    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      'GET /a failed: Error: outer\ncause: {"status":503}',
    );
  });

  it("does not log a 4xx", async () => {
    await invoke(throwing(new AiServiceHttpError(404, "Not Found", "", INTERNAL_AI)));
    await invoke(throwing(Object.assign(new Error("bad"), { status: 400, expose: true })));

    expect(vi.mocked(logger.error)).not.toHaveBeenCalled();
  });

  // DEFECT ai-13: after the headers are sent, an error ends the response cleanly with res.end(), so a cut answer looks complete — expected next(err), so Express drops the connection (ts-ai-express.md §1)
  it("ends a response whose headers are already sent and passes nothing to next", async () => {
    const handler = asyncHandler(async (_req, res) => {
      res.status(200).setHeader("Content-Type", "text/plain");
      res.write("partial");
      throw new Error("later step failed");
    });

    const reply = await invoke(handler, { method: "GET", url: "/a" });

    expect(reply.status).toBe(200);
    expect(reply.body).toBe("partial");
    expect(reply.ended).toBe(true);
    expect(reply.nextError).toBeUndefined();
    expect(vi.mocked(logger.error)).toHaveBeenCalledTimes(1);
  });
});
