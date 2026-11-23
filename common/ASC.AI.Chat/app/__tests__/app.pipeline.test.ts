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

import request from "supertest";
import { afterAll, describe, expect, it, vi } from "vitest";
import { useUpstream } from "./helpers/upstream.js";
import { publishedOperations, responseViolations } from "./helpers/schema.js";

// The whole application through supertest, with the parts of the configuration it reads pinned
// by the environment, so that the developer's appsettings (`core:cors`, `ai.documentAgent`)
// cannot change what these tests see: CORS off, the document agent off.

vi.stubEnv("AI__DOCUMENT_AGENT__URL", "");
vi.stubEnv("AI__DOCUMENT_AGENT__TOKEN", "");

const { default: logger } = await import("../log.js");
const { createApp } = await import("../createApp.js");
const { API_PREFIX } = await import("../routes.js");

const upstream = useUpstream({ localApp: true });
const app = createApp();

// What the app itself sent anywhere; supertest's own requests to it are not listed.
const calledOut = () => upstream.seen;

afterAll(() => {
  vi.unstubAllEnvs();
});

const AUTH = { Authorization: "Bearer t" };
const JSON_TYPE = { "Content-Type": "application/json" };

describe("building the application", () => {
  it("logs that CORS is off when core:cors is empty", async () => {
    vi.mocked(logger.info).mockClear();
    createApp();
    expect(vi.mocked(logger.info).mock.calls).toEqual([
      ["CORS disabled"],
      [`Registered 89 engine routes across 10 engines under ${API_PREFIX}`],
    ]);
  });

  it("answers no CORS header when CORS is off", async () => {
    const reply = await request(app).get("/isLife").set("Origin", "https://portal.example.com");

    expect(reply.headers["access-control-allow-origin"]).toBeUndefined();
  });

  it("answers a preflight with the 401 of the gate when CORS is off", async () => {
    const reply = await request(app)
      .options(`${API_PREFIX}/threads/create`)
      .set("Origin", "https://portal.example.com")
      .set("Access-Control-Request-Method", "POST");

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized" });
  });
});

describe("the open routes", () => {
  it.each(["/isLife", `${API_PREFIX}/isLife`])("answers %s with a plain OK", async (path) => {
    const reply = await request(app).get(path);

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("text/plain; charset=utf-8");
    expect(reply.text).toBe("OK");
    expect(reply.headers["cache-control"]).toBeUndefined();
  });

  it.each(["/health", `${API_PREFIX}/health`])("answers %s with Healthy", async (path) => {
    const reply = await request(app).get(path);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ status: "Healthy" });
    expect(reply.headers["cache-control"]).toBeUndefined();
  });

  it("serves the OpenAPI document without credentials and without no-store", async () => {
    const reply = await request(app).get(`${API_PREFIX}/openapi.json`);

    expect(reply.status).toBe(200);
    expect(reply.body.openapi).toBe("3.1.1");
    expect(Object.keys(reply.body.paths)).toHaveLength(111);
    expect(reply.headers["cache-control"]).toBeUndefined();
  });

  it("serves the docs page pointing at the document", async () => {
    const reply = await request(app).get(`${API_PREFIX}/docs`);

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("text/html; charset=utf-8");
    expect(reply.text).toContain(
      `<script id="api-reference" data-url="${API_PREFIX}/openapi.json"></script>`,
    );
  });

  it("matches the prefix case-insensitively, as Express routes do", async () => {
    const reply = await request(app).get("/API/2.0/AI/health");

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ status: "Healthy" });
  });

  it("calls no upstream for any of them", async () => {
    for (const path of ["/isLife", "/health", `${API_PREFIX}/openapi.json`, `${API_PREFIX}/docs`]) {
      await request(app).get(path);
    }

    expect(calledOut()).toEqual([]);
  });
});

describe("the Server-Timing header", () => {
  it.each([
    ["an open route", "/isLife"],
    ["a 401", `${API_PREFIX}/threads/list`],
    ["a 404", "/nothing"],
  ])("is set on %s", async (_what, path) => {
    const reply = await request(app).get(path);

    expect(reply.headers["server-timing"]).toMatch(/^express-request-time;dur=\d+ms$/);
  });

  it("is not set again when the headers were already sent", () => {
    const timing = (app as unknown as { _router: { stack: { handle: unknown }[] } })._router
      .stack[2]?.handle as (req: unknown, res: unknown, next: () => void) => void;
    const calls: unknown[][] = [];
    const set: string[] = [];
    const res = {
      headersSent: true,
      writeHead: (...args: unknown[]) => {
        calls.push(args);
        return res;
      },
      setHeader: (name: string) => {
        set.push(name);
      },
    };
    const next = vi.fn();

    timing({}, res, next);
    res.writeHead(200, { a: "b" });

    expect(next).toHaveBeenCalledOnce();
    expect(calls).toEqual([[200, { a: "b" }]]);
    expect(set).toEqual([]);
  });
});

describe("the authentication gate", () => {
  it.each(publishedOperations().map((op) => [op.operationId, op.method, op.path] as const))(
    "answers 401 to %s without credentials and calls nothing",
    async (operationId, method, path) => {
      const url = path.replace(/\{[^}]+\}/g, "x");
      const call = request(app)[method as "get" | "post" | "put" | "patch" | "delete"](url);
      const reply = method === "get" ? await call : await call.set(JSON_TYPE).send("{}");

      expect(reply.status).toBe(401);
      expect(reply.body).toEqual({ error: "Unauthorized" });
      expect(reply.headers["cache-control"]).toBeUndefined();
      expect(responseViolations(operationId, 401, reply.body)).toEqual([]);
      expect(calledOut()).toEqual([]);
    },
  );

  it("covers every published operation", () => {
    expect(publishedOperations()).toHaveLength(116);
  });

  it("logs the method and the full URL of a rejected request", async () => {
    await request(app).get(`${API_PREFIX}/threads/list?params[0]=1`);

    expect(logger.warn).toHaveBeenCalledWith(
      `Unauthenticated request rejected: GET ${API_PREFIX}/threads/list?params[0]=1`,
    );
  });

  it.each([
    ["an empty session cookie", { Cookie: "asc_auth_key=" }],
    ["a blank session cookie", { Cookie: "asc_auth_key=%20%20" }],
    ["another cookie only", { Cookie: "other=1" }],
    ["a blank Authorization header", { Authorization: " " }],
    ["an empty Authorization header", { Authorization: "" }],
  ])("answers 401 to %s", async (_what, headers) => {
    const reply = await request(app).get(`${API_PREFIX}/threads/list`).set(headers);

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized" });
  });

  it.each([
    ["the session cookie", { Cookie: "asc_auth_key=abc" }],
    ["an Authorization header", { Authorization: "Bearer t" }],
    ["any non-blank Authorization value", { Authorization: "x" }],
  ])("lets %s through to the routes", async (_what, headers) => {
    const reply = await request(app).get(`${API_PREFIX}/nothing`).set(headers);

    expect(reply.status).toBe(404);
    expect(reply.headers["cache-control"]).toBe("no-store");
  });
});

describe("Cache-Control", () => {
  it.each(["post", "put", "delete"] as const)(
    "is not set on a %s that passed the gate",
    async (method) => {
      const reply = await request(app)[method](`${API_PREFIX}/nothing`).set(AUTH);

      expect(reply.status).toBe(404);
      expect(reply.headers["cache-control"]).toBeUndefined();
    },
  );

  it("is not set on a GET outside the API prefix", async () => {
    const reply = await request(app).get("/nothing").set(AUTH);

    expect(reply.headers["cache-control"]).toBeUndefined();
  });
});

describe("an unknown path", () => {
  it("answers 404 Not Found as plain text and logs it", async () => {
    const reply = await request(app).delete("/nothing/here?x=1").set(AUTH);

    expect(reply.status).toBe(404);
    expect(reply.headers["content-type"]).toBe("text/plain; charset=utf-8");
    expect(reply.text).toBe("Not Found");
    expect(logger.warn).toHaveBeenCalledWith("Route not found: DELETE /nothing/here?x=1");
  });

  it("answers 404 without credentials outside the API prefix", async () => {
    const reply = await request(app).get("/nothing");

    expect(reply.status).toBe(404);
  });

  // DEFECT app-1: the 404 of an unknown API path is the plain-text `Not Found` of
  // `res.sendStatus` — expected the `{ error }` JSON every other answer of the API uses
  // (ts-ai-style.md, "Errors and the error boundary"; ts-ai-express.md §1).
  it("answers an unknown API path with plain text, not JSON", async () => {
    const reply = await request(app).get(`${API_PREFIX}/nothing`).set(AUTH);

    expect(reply.status).toBe(404);
    expect(reply.text).toBe("Not Found");
  });

  // The engine's save-image routes are deliberately not served (Bug 83289).
  it.each(["save-image", "save-images-many"])("answers 404 to attachments/%s", async (route) => {
    const reply = await request(app)
      .post(`${API_PREFIX}/attachments/${route}`)
      .set(AUTH)
      .set(JSON_TYPE)
      .send("{}");

    expect(reply.status).toBe(404);
    expect(calledOut()).toEqual([]);
  });
});

describe("the body parsers", () => {
  const big = (bytes: number): string => JSON.stringify({ text: "x".repeat(bytes) });

  // DEFECT app-2: a body that does not parse gets Express's default HTML error page (with the
  // stack outside production), a status the operation does not publish — expected 400 `{ error }`
  // JSON from an error handler registered last, documented (ts-ai-express.md §1, §8).
  it("answers a broken JSON body with 400 and Express's HTML page, before the gate", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/threads/create`)
      .set(JSON_TYPE)
      .send("{bad");

    expect(reply.status).toBe(400);
    expect(reply.headers["content-type"]).toBe("text/html; charset=utf-8");
    expect(reply.text).toContain("<pre>SyntaxError: ");
    expect(responseViolations("aiThreadsCreate", 400, reply.body)).toEqual([
      "aiThreadsCreate publishes no JSON schema for status 400",
    ]);
    expect(calledOut()).toEqual([]);
  });

  // DEFECT app-3: a body over the limit gets Express's default HTML page, not the published
  // `AiErrorResponse` of the 413 — expected 413 `{ error }` JSON (ts-ai-express.md §1, §8).
  it("answers a JSON body over 100 kB with 413 and Express's HTML page", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/threads/create`)
      .set(AUTH)
      .set(JSON_TYPE)
      .send(big(100 * 1024));

    expect(reply.status).toBe(413);
    expect(reply.headers["content-type"]).toBe("text/html; charset=utf-8");
    expect(reply.text).toContain("<pre>PayloadTooLargeError: request entity too large");
    expect(responseViolations("aiThreadsCreate", 413, reply.body)).toEqual([
      "/ must have required property 'error'",
    ]);
  });

  it("takes a JSON body just under 100 kB", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/threads/create`)
      .set(JSON_TYPE)
      .send(big(100 * 1024 - 20));

    expect(reply.status).toBe(401);
  });

  it("takes a bare JSON string, as strict is off", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/nothing`)
      .set(AUTH)
      .set(JSON_TYPE)
      .send('"uuid"');

    expect(reply.status).toBe(404);
  });

  it("takes a bare JSON null", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/nothing`)
      .set(AUTH)
      .set(JSON_TYPE)
      .send("null");

    expect(reply.status).toBe(404);
  });

  it("takes a text-to-docx body over 100 kB (Bug 83096)", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/text-to-docx`)
      .set(JSON_TYPE)
      .send(big(200 * 1024));

    expect(reply.status).toBe(401);
  });

  it("answers a text-to-docx body over 15 MB with 413", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/text-to-docx`)
      .set(JSON_TYPE)
      .send(big(15 * 1024 * 1024));

    expect(reply.status).toBe(413);
    expect(reply.text).toContain("PayloadTooLargeError");
    expect(responseViolations("aiExportTextToDocx", 413, reply.body)).toEqual([
      "/ must have required property 'error'",
    ]);
  });

  it("parses a text-to-docx body: a broken one answers 400 before the gate", async () => {
    const reply = await request(app).post(`${API_PREFIX}/text-to-docx`).set(JSON_TYPE).send("{bad");

    expect(reply.status).toBe(400);
    expect(reply.text).toContain("<pre>SyntaxError: ");
  });

  // DEFECT app-4: the parser is picked by an exact, case-sensitive `req.path`, while the router
  // matches `/AI/TEXT-TO-DOCX` and `/text-to-docx/` too: those spellings reach the same handler
  // with the 100 kB limit — expected the same parser for every spelling the router accepts
  // (ts-ai-express.md §2, §8).
  it.each([`${API_PREFIX}/TEXT-TO-DOCX`, `${API_PREFIX}/text-to-docx/`])(
    "gives %s the default 100 kB limit",
    async (path) => {
      const reply = await request(app)
        .post(path)
        .set(JSON_TYPE)
        .send(big(200 * 1024));

      expect(reply.status).toBe(413);
    },
  );

  it("leaves the OpenAI passthrough unparsed: a broken JSON body reaches the gate", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/openai/p1/v1/chat/completions`)
      .set(JSON_TYPE)
      .send("{bad");

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized" });
  });

  it("leaves the OpenAI passthrough unparsed: a body over 100 kB reaches the gate", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/openai/p1/v1/images/generations`)
      .set(JSON_TYPE)
      .send(big(200 * 1024));

    expect(reply.status).toBe(401);
  });

  // DEFECT app-5: the passthrough is recognised by a case-sensitive prefix: `/OPENAI/...` is
  // routed to the same handler but its body is parsed (and a broken one refused with 400) —
  // expected the raw body for every spelling the router accepts (ts-ai-express.md §2, §8).
  it("parses the body of the passthrough spelled in upper case", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/OPENAI/p1/v1/chat/completions`)
      .set(JSON_TYPE)
      .send("{bad");

    expect(reply.status).toBe(400);
  });

  it("leaves the document-agent outputs path unparsed: a broken JSON body reaches it", async () => {
    const reply = await request(app)
      .post("/document-agent/files/outputs")
      .set(JSON_TYPE)
      .send("{bad");

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "not found" });
  });

  it("parses the document-agent outputs path with a trailing slash", async () => {
    const reply = await request(app)
      .post("/document-agent/files/outputs/")
      .set(JSON_TYPE)
      .send("{bad");

    expect(reply.status).toBe(400);
  });

  // DEFECT app-6: `bodyParser.urlencoded` is mounted for every route though no handler reads a
  // form body — expected no parser without a reader (ts-ai-express.md §2, §8).
  it("parses a form body on every route: one over 100 kB answers 413", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/threads/create`)
      .set("Content-Type", "application/x-www-form-urlencoded")
      .send(`a=${"x".repeat(100 * 1024)}`);

    expect(reply.status).toBe(413);
  });

  // DEFECT app-6: the same parser runs on the raw passthrough — expected none there either
  // (ts-ai-express.md §2, §8).
  it("parses a form body even on the OpenAI passthrough", async () => {
    const reply = await request(app)
      .post(`${API_PREFIX}/openai/p1/v1/chat/completions`)
      .set("Content-Type", "application/x-www-form-urlencoded")
      .send(`a=${"x".repeat(100 * 1024)}`);

    expect(reply.status).toBe(413);
  });
});

describe("hardening headers", () => {
  // DEFECT app-7: every answer carries `X-Powered-By: Express` — expected it removed with
  // `app.disable("x-powered-by")` in createApp (ts-ai-express.md §4, §8).
  it("announces Express in X-Powered-By", async () => {
    const reply = await request(app).get("/isLife");

    expect(reply.headers["x-powered-by"]).toBe("Express");
  });
});
