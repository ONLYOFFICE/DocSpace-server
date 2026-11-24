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

// The document agent settings routes over the real AI service client, answered by msw:
// `GET` / `PUT internal/ai/document-agent/settings` (DocumentAgentSettingsController in
// products/ASC.AI/ASC.AI/Api, which keeps the tenant settings and decides who may read and
// write them).

import { describe, expect, it, vi } from "vitest";
import { invoke, inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

const { documentAgentController } = await import("../documentAgentController.js");

const upstream = useUpstream();
const ROUTE = "*/internal/ai/document-agent/settings";

// What the AI service stores and answers, in the portal envelope.
const SETTINGS = {
  enabled: true,
  profileId: "p-1",
  reasoning: "low",
  maxToolCalls: 10,
  maxFileBytes: 1024,
  taskLog: true,
  taskLogFolderId: "17",
};
const ENVELOPE = { response: SETTINGS, count: 1, status: 0, statusCode: 200 };

interface Call {
  method: string;
  path: string;
  cookie: string | null;
  contentType: string | null;
  body: unknown;
}
const calls: Call[] = [];

function route(method: "get" | "put", answer: () => Response | Promise<Response>) {
  return http[method](ROUTE, async ({ request }) => {
    const text = await request.text();
    calls.push({
      method: request.method,
      path: new URL(request.url).pathname,
      cookie: request.headers.get("cookie"),
      contentType: request.headers.get("content-type"),
      body: text === "" ? undefined : JSON.parse(text),
    });
    return answer();
  });
}

function resetCalls(): void {
  calls.length = 0;
}

describe("documentAgentController.getSettings", () => {
  it("answers the AI service's settings with the portal envelope kept, the caller's cookie relayed", async () => {
    resetCalls();
    upstream.use(route("get", () => docspace(SETTINGS)));

    const reply = await inRequestContext({ cookie: "asc_auth_key=abc" }, () =>
      invoke(documentAgentController.getSettings, { method: "GET" }),
    );

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(ENVELOPE);
    expect(calls).toEqual([
      {
        method: "GET",
        path: "/internal/ai/document-agent/settings",
        cookie: "asc_auth_key=abc",
        contentType: null,
        body: undefined,
      },
    ]);
  });

  it("answers the portal envelope the operation publishes", async () => {
    resetCalls();
    upstream.use(route("get", () => docspace(SETTINGS)));

    const reply = await invoke(documentAgentController.getSettings, { method: "GET" });

    expect(responseViolations("aiSettingsGetDocumentAgent", 200, reply.body)).toEqual([]);
  });

  // DocumentAgentController.ReadSettingsAsync answers a DocumentAgentSettingsDto in the envelope:
  // anything else is unknown, answered 502 and never relayed as a 200.
  it.each<[string, () => Response]>([
    ["an answer without the envelope", () => HttpResponse.json(SETTINGS)],
    ["a 204", () => new HttpResponse(null, { status: 204 })],
    ["a text answer", () => HttpResponse.text("settings")],
    ["a JSON string", () => HttpResponse.json("x")],
    ["a JSON list", () => HttpResponse.json([])],
    ["an empty object", () => HttpResponse.json({})],
    ["a null response", () => docspace(null)],
  ])("answers 502 for %s of the AI service", async (_what, answer) => {
    resetCalls();
    upstream.use(route("get", answer));

    const reply = await invoke(documentAgentController.getSettings, { method: "GET" });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
  });

  it.each([
    [401, "Unauthorized"],
    [403, "Forbidden"],
    [404, "Not Found"],
    [500, "Internal Server Error"],
    [503, "Service Unavailable"],
  ])("relays the AI service's status %i with its reason phrase only", async (status, error) => {
    resetCalls();
    upstream.use(route("get", () => docspaceError(status, "detail of http://ai.internal")));

    const reply = await invoke(documentAgentController.getSettings, { method: "GET" });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSettingsGetDocumentAgent", status, reply.body)).toEqual(
      status === 404
        ? [`aiSettingsGetDocumentAgent publishes no JSON schema for status ${status}`]
        : [],
    );
  });

  it("answers 500 when the AI service does not answer in time", async () => {
    resetCalls();
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(route("get", () => new Promise<Response>(() => {})));

      const pending = invoke(documentAgentController.getSettings, { method: "GET" });
      await vi.advanceTimersByTimeAsync(300_000);
      const reply = await pending;

      expect(reply.status).toBe(500);
      expect(reply.body).toEqual({ error: "Internal server error" });
    } finally {
      vi.useRealTimers();
    }
  });
});

describe("documentAgentController.setSettings", () => {
  it.each<[string, () => Response]>([
    ["an answer without the envelope", () => HttpResponse.json(SETTINGS)],
    ["a 204", () => new HttpResponse(null, { status: 204 })],
    ["a text answer", () => HttpResponse.text("settings")],
    ["a null response", () => docspace(null)],
  ])("answers 502 for %s of the AI service to the write", async (_what, answer) => {
    resetCalls();
    upstream.use(route("put", answer));

    const reply = await invoke(documentAgentController.setSettings, {
      method: "PUT",
      body: SETTINGS,
    });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
  });

  it("writes the settings the tab sends and answers what the AI service stored", async () => {
    resetCalls();
    upstream.use(route("put", () => docspace(SETTINGS)));

    const reply = await inRequestContext({ cookie: "asc_auth_key=abc" }, () =>
      invoke(documentAgentController.setSettings, { method: "PUT", body: SETTINGS }),
    );

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(ENVELOPE);
    expect(calls).toEqual([
      {
        method: "PUT",
        path: "/internal/ai/document-agent/settings",
        cookie: "asc_auth_key=abc",
        contentType: "application/json",
        body: SETTINGS,
      },
    ]);
    expect(responseViolations("aiSettingsSetDocumentAgent", 200, reply.body)).toEqual([]);
  });

  it("drops the fields it does not know before the write", async () => {
    resetCalls();
    upstream.use(route("put", () => docspace(SETTINGS)));

    await invoke(documentAgentController.setSettings, {
      method: "PUT",
      body: { ...SETTINGS, tenantId: 7, key: "x" },
    });

    expect(calls[0]!.body).toEqual(SETTINGS);
  });

  it.each([
    ["no profile and no log folder", { profileId: null, taskLogFolderId: null }],
    ["the lowest limits", { maxToolCalls: 1, maxFileBytes: 1 }],
    ["the agent's ceilings", { maxToolCalls: 150, maxFileBytes: 32 * 1024 * 1024 }],
    ["reasoning off", { reasoning: "off" }],
    ["reasoning max", { reasoning: "max" }],
    ["the default reasoning", { reasoning: "default" }],
  ])("accepts %s", async (_what, change) => {
    resetCalls();
    upstream.use(route("put", () => docspace(SETTINGS)));

    const reply = await invoke(documentAgentController.setSettings, {
      method: "PUT",
      body: { ...SETTINGS, ...change },
    });

    expect(reply.status).toBe(200);
    expect(calls[0]!.body).toEqual({ ...SETTINGS, ...change });
  });

  const REASONING = "Expected 'default' | 'off' | 'low' | 'medium' | 'high' | 'max'";
  const field = (key: string, value: unknown): Record<string, unknown> => {
    const body: Record<string, unknown> = { ...SETTINGS };
    if (value === undefined) {
      delete body[key];
    } else {
      body[key] = value;
    }
    return body;
  };
  it.each([
    ["null", null, "body: Expected object, received null"],
    ["a string", "settings", "body: Expected object, received string"],
    ["an array", [SETTINGS], "body: Expected object, received array"],
    ["a number", 5, "body: Expected object, received number"],
    [
      "{}",
      {},
      "enabled: Required; profileId: Required; reasoning: Required; maxToolCalls: Required; maxFileBytes: Required; taskLog: Required; taskLogFolderId: Required",
    ],
    ["no enabled", field("enabled", undefined), "enabled: Required"],
    ["a null enabled", field("enabled", null), "enabled: Expected boolean, received null"],
    ["a string enabled", field("enabled", "true"), "enabled: Expected boolean, received string"],
    ["no profileId", field("profileId", undefined), "profileId: Required"],
    [
      "an empty profileId",
      field("profileId", ""),
      "profileId: String must contain at least 1 character(s)",
    ],
    ["a numeric profileId", field("profileId", 5), "profileId: Expected string, received number"],
    ["no reasoning", field("reasoning", undefined), "reasoning: Required"],
    ["a null reasoning", field("reasoning", null), `reasoning: ${REASONING}, received null`],
    [
      "an unknown reasoning",
      field("reasoning", "xhigh"),
      `reasoning: Invalid enum value. ${REASONING}, received 'xhigh'`,
    ],
    [
      "an empty reasoning",
      field("reasoning", ""),
      `reasoning: Invalid enum value. ${REASONING}, received ''`,
    ],
    ["a numeric reasoning", field("reasoning", 5), `reasoning: ${REASONING}, received number`],
    ["no maxToolCalls", field("maxToolCalls", undefined), "maxToolCalls: Required"],
    [
      "a null maxToolCalls",
      field("maxToolCalls", null),
      "maxToolCalls: Expected number, received null",
    ],
    [
      "a string maxToolCalls",
      field("maxToolCalls", "10"),
      "maxToolCalls: Expected number, received string",
    ],
    [
      "a maxToolCalls of 0",
      field("maxToolCalls", 0),
      "maxToolCalls: Number must be greater than or equal to 1",
    ],
    [
      "a negative maxToolCalls",
      field("maxToolCalls", -1),
      "maxToolCalls: Number must be greater than or equal to 1",
    ],
    [
      "a maxToolCalls over 150",
      field("maxToolCalls", 151),
      "maxToolCalls: Number must be less than or equal to 150",
    ],
    [
      "a fractional maxToolCalls",
      field("maxToolCalls", 1.5),
      "maxToolCalls: Expected integer, received float",
    ],
    ["no maxFileBytes", field("maxFileBytes", undefined), "maxFileBytes: Required"],
    [
      "a null maxFileBytes",
      field("maxFileBytes", null),
      "maxFileBytes: Expected number, received null",
    ],
    [
      "a string maxFileBytes",
      field("maxFileBytes", "1"),
      "maxFileBytes: Expected number, received string",
    ],
    [
      "a maxFileBytes of 0",
      field("maxFileBytes", 0),
      "maxFileBytes: Number must be greater than or equal to 1",
    ],
    [
      "a maxFileBytes over 32 MiB",
      field("maxFileBytes", 32 * 1024 * 1024 + 1),
      "maxFileBytes: Number must be less than or equal to 33554432",
    ],
    [
      "a fractional maxFileBytes",
      field("maxFileBytes", 1.5),
      "maxFileBytes: Expected integer, received float",
    ],
    ["no taskLog", field("taskLog", undefined), "taskLog: Required"],
    ["a null taskLog", field("taskLog", null), "taskLog: Expected boolean, received null"],
    ["a string taskLog", field("taskLog", "false"), "taskLog: Expected boolean, received string"],
    ["no taskLogFolderId", field("taskLogFolderId", undefined), "taskLogFolderId: Required"],
    [
      "an empty taskLogFolderId",
      field("taskLogFolderId", ""),
      "taskLogFolderId: String must contain at least 1 character(s)",
    ],
    [
      "a numeric taskLogFolderId",
      field("taskLogFolderId", 17),
      "taskLogFolderId: Expected string, received number",
    ],
  ])("answers 400 for a body with %s and writes nothing", async (_what, body, issues) => {
    resetCalls();

    const reply = await invoke(documentAgentController.setSettings, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: `Invalid settings: ${issues}` });
    expect(responseViolations("aiSettingsSetDocumentAgent", 400, reply.body)).toEqual([]);
    expect(calls).toEqual([]);
  });

  it.each([
    [400, "Bad Request"],
    [403, "Forbidden"],
    [404, "Not Found"],
    [500, "Internal Server Error"],
  ])("relays the AI service's refusal %i with its reason phrase only", async (status, error) => {
    resetCalls();
    upstream.use(route("put", () => docspaceError(status, "profile p-1 at http://ai.internal")));

    const reply = await invoke(documentAgentController.setSettings, {
      method: "PUT",
      body: SETTINGS,
    });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiSettingsSetDocumentAgent", status, reply.body)).toEqual([]);
  });
});
