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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { delay, http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { mcpServer } from "./systemToolsMcpStub.js";

const DOCSPACE_URL = "http://mcp.test/mcp";
const OTHER_URL = "http://other.test/mcp";

const state = vi.hoisted(() => ({
  servers: [
    { id: "1", name: "docspace", endpoint: "http://mcp.test/mcp" },
    { id: "2", name: "other", endpoint: "http://other.test/mcp" },
  ],
  portal: undefined as string | undefined,
}));

vi.mock("../../../config/index.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../../config/index.js")>()),
  getMcpServers: () => state.servers,
  mcpPortalBaseUrl: () => state.portal,
}));

const {
  authorizationValue,
  getSystemServerConfig,
  sessionCookie,
  systemServerHeaders,
  systemToolsSource,
} = await import("../systemTools.js");

const upstream = useUpstream();

beforeEach(() => {
  state.portal = undefined;
  // The library's own logger writes to the console; printing its stacks costs a test 100+ ms.
  for (const level of ["log", "debug", "info", "warn", "error"] as const) {
    vi.spyOn(console, level).mockImplementation(() => undefined);
  }
});

// Headers a browser sends with a chat request, as the request context keeps them.
const BROWSER = {
  cookie: "lang=en; asc_auth_key=KEY",
  origin: "https://portal.example.com",
  accept: "text/html",
  "accept-language": "en",
  "user-agent": "UA",
};

const LIBRARY_HEADERS = {
  accept: "application/json, text/event-stream",
  "content-type": "application/json",
};

const READ_TOOL = {
  name: "get_room",
  description: "Read a room",
  inputSchema: { type: "object" },
  annotations: { readOnlyHint: true },
};
const WRITE_TOOL = { name: "delete_room", inputSchema: {}, annotations: { readOnlyHint: "true" } };
const PLAIN_TOOL = { name: "create_room", inputSchema: {} };

describe("module setup", () => {
  afterEach(() => {
    state.servers = [
      { id: "1", name: "docspace", endpoint: DOCSPACE_URL },
      { id: "2", name: "other", endpoint: OTHER_URL },
    ];
  });

  it("names the configured servers in the log when loaded", async () => {
    vi.resetModules();
    await import("../systemTools.js");
    const log = (await import("../../log.js")).default;

    expect(log.info).toHaveBeenCalledWith("System MCP tools configured: docspace, other");
  });

  it("says that none are configured, lists no tool and warns about it", async () => {
    state.servers = [];
    vi.resetModules();
    const mod = await import("../systemTools.js");
    const log = (await import("../../log.js")).default;

    expect(log.info).toHaveBeenCalledWith("No system MCP tools configured (ai.mcp empty)");
    expect(mod.systemToolsSource.getServerTypes()).toEqual([]);
    await expect(mod.systemToolsSource.getTools()).resolves.toEqual({});
    expect(log.info).toHaveBeenCalledWith(
      "systemTools.getTools(entityId=-, attachments=0) configured servers=[<none>] — enumerating",
    );
    expect(log.warn).toHaveBeenCalledWith(
      "systemTools.getTools(entityId=-) -> no tools from any server",
    );
    expect(upstream.seen).toEqual([]);
  });
});

describe("getSystemServerConfig", () => {
  it("answers the endpoint of a configured server", () => {
    expect(getSystemServerConfig("docspace")).toEqual({ url: DOCSPACE_URL });
    expect(getSystemServerConfig("other")).toEqual({ url: OTHER_URL });
  });

  it.each(["unknown", "constructor", "toString", "__proto__", "hasOwnProperty", ""])(
    "answers undefined for %j (Bug 82986)",
    (name) => {
      expect(getSystemServerConfig(name)).toBeUndefined();
    },
  );

  it("lists the configured names as server types", () => {
    expect(systemToolsSource.getServerTypes()).toEqual(["docspace", "other"]);
  });
});

describe("authorizationValue", () => {
  it.each<[string, Record<string, string>, string | undefined]>([
    ["the auth cookie", { cookie: "a=1; asc_auth_key=KEY; b=2" }, "KEY"],
    ["the authorization cookie", { cookie: "authorization=AUTH" }, "AUTH"],
    [
      "the auth cookie before the authorization cookie",
      { cookie: "authorization=A; asc_auth_key=K" },
      "K",
    ],
    ["the cookie before the header", { cookie: "asc_auth_key=K", authorization: "Bearer H" }, "K"],
    ["the header without a cookie", { authorization: "Bearer H" }, "Bearer H"],
    [
      "the header when the cookie has neither",
      { cookie: "a=1", authorization: "Bearer H" },
      "Bearer H",
    ],
    ["a trimmed name and value", { cookie: " asc_auth_key = K2 ;x=1" }, "K2"],
    ["a value with an equals sign", { cookie: "asc_auth_key=a=b" }, "a=b"],
    ["an empty cookie value", { cookie: "asc_auth_key=", authorization: "H" }, ""],
    ["nothing for parts without a name", { cookie: "=asc_auth_key; asc_auth_key" }, undefined],
    ["nothing without either", {}, undefined],
    ["nothing for an empty cookie header", { cookie: "" }, undefined],
  ])("reads %s", (_name, headers, expected) => {
    expect(authorizationValue(headers)).toBe(expected);
  });
});

describe("sessionCookie", () => {
  it.each<[string, Record<string, string>, string | undefined]>([
    [
      "only the auth cookie of several",
      { cookie: "lang=en; asc_auth_key=KEY; x=1" },
      "asc_auth_key=KEY",
    ],
    [
      "the authorization cookie in its place",
      { cookie: "authorization=A; lang=en" },
      "authorization=A",
    ],
    [
      "the auth cookie before the authorization cookie",
      { cookie: "authorization=A; asc_auth_key=K" },
      "asc_auth_key=K",
    ],
    ["an empty auth cookie", { cookie: "asc_auth_key=" }, "asc_auth_key="],
    ["nothing for other cookies", { cookie: "lang=en" }, undefined],
    ["nothing for an authorization header", { authorization: "Bearer H" }, undefined],
    ["nothing without a cookie", {}, undefined],
  ])("answers %s", (_name, headers, expected) => {
    expect(inRequestContext(headers, () => sessionCookie())).toBe(expected);
  });

  it("answers undefined outside a request", () => {
    expect(sessionCookie()).toBeUndefined();
  });
});

describe("systemServerHeaders", () => {
  it.each<[string, string | undefined, Record<string, string>, Record<string, string>]>([
    [
      "the configured portal address over everything, one trailing slash",
      "http://host.docker.internal:8092///",
      { origin: "https://portal.example.com", cookie: "asc_auth_key=K" },
      { referer: "http://host.docker.internal:8092/", authorization: "K" },
    ],
    [
      "the configured address without a slash",
      "https://portal.example.com",
      {},
      { referer: "https://portal.example.com/" },
    ],
    [
      "the origin",
      undefined,
      { origin: "https://portal.example.com/" },
      { referer: "https://portal.example.com/" },
    ],
    [
      "the origin before the forwarded host",
      undefined,
      { origin: "https://a.example.com", "x-forwarded-host": "b.example.com" },
      { referer: "https://a.example.com/" },
    ],
    [
      "localhost rewritten to the Docker host",
      undefined,
      { origin: "http://localhost:8092" },
      { referer: "http://host.docker.internal:8092/" },
    ],
    [
      "127.0.0.1 rewritten",
      undefined,
      { origin: "http://127.0.0.1" },
      { referer: "http://host.docker.internal/" },
    ],
    [
      "[::1] rewritten",
      undefined,
      { origin: "http://[::1]:5000" },
      { referer: "http://host.docker.internal:5000/" },
    ],
    [
      "an origin that is not a URL as it is",
      undefined,
      { origin: "not a url" },
      { referer: "not a url/" },
    ],
    [
      "the first forwarded host and protocol",
      undefined,
      { "x-forwarded-host": " portal.example.com , proxy", "x-forwarded-proto": "http, https" },
      { referer: "http://portal.example.com/" },
    ],
    [
      "https for a forwarded host without a protocol",
      undefined,
      { "x-forwarded-host": "portal.example.com" },
      { referer: "https://portal.example.com/" },
    ],
    [
      "https for an empty forwarded protocol",
      undefined,
      { "x-forwarded-host": "portal.example.com", "x-forwarded-proto": " " },
      { referer: "https://portal.example.com/" },
    ],
    [
      "a forwarded localhost rewritten",
      undefined,
      { "x-forwarded-host": "localhost:8092", "x-forwarded-proto": "http" },
      { referer: "http://host.docker.internal:8092/" },
    ],
    [
      "the referer's origin when the forwarded host is blank",
      undefined,
      { "x-forwarded-host": " ", referer: "https://portal.example.com/rooms/shared/?folder=1" },
      { referer: "https://portal.example.com/" },
    ],
    [
      "a loopback referer rewritten",
      undefined,
      { referer: "http://localhost:8092/x" },
      { referer: "http://host.docker.internal:8092/" },
    ],
    ["nothing for a malformed referer", undefined, { referer: "garbage" }, {}],
    ["nothing without any of them", undefined, {}, {}],
    ["no authorization for an empty auth cookie", undefined, { cookie: "asc_auth_key=" }, {}],
    [
      "the authorization header",
      undefined,
      { authorization: "Bearer H" },
      { authorization: "Bearer H" },
    ],
  ])("derives %s", (_name, portal, headers, expected) => {
    state.portal = portal;

    expect(inRequestContext(headers, () => systemServerHeaders())).toEqual(expected);
  });
});

describe("systemToolsSource.getTools", () => {
  it("opens a session with each server, with the caller's headers, the referer and the authorization", async () => {
    const docspace = mcpServer(upstream, DOCSPACE_URL, {
      sessionId: "s1",
      tools: [READ_TOOL, WRITE_TOOL, PLAIN_TOOL],
    });
    const other = mcpServer(upstream, OTHER_URL, { tools: [] });

    const grouped = await inRequestContext(BROWSER, () =>
      systemToolsSource.getTools("12", { attachmentId: ["a", "b"] }),
    );

    expect(grouped).toEqual({
      docspace: [{ ...READ_TOOL, requireApproval: false }, WRITE_TOOL, PLAIN_TOOL],
      other: [],
    });
    const forwarded = {
      cookie: "lang=en; asc_auth_key=KEY",
      origin: "https://portal.example.com",
      "user-agent": "UA",
      referer: "https://portal.example.com/",
      authorization: "KEY",
      ...LIBRARY_HEADERS,
    };
    expect(docspace).toEqual(
      [
        {
          method: "POST",
          rpc: "initialize",
          headers: forwarded,
          body: {
            jsonrpc: "2.0",
            id: "init-docspace",
            method: "initialize",
            params: {
              protocolVersion: "2025-06-18",
              capabilities: {},
              clientInfo: { name: "ai-chat", version: "1.0.0" },
            },
          },
        },
        {
          method: "POST",
          rpc: "notifications/initialized",
          headers: { ...forwarded, "mcp-protocol-version": "2025-06-18", "mcp-session-id": "s1" },
          body: { jsonrpc: "2.0", method: "notifications/initialized" },
        },
        {
          method: "POST",
          rpc: "tools/list",
          headers: { ...forwarded, "mcp-protocol-version": "2025-06-18", "mcp-session-id": "s1" },
          body: { jsonrpc: "2.0", id: "tools-docspace", method: "tools/list", params: {} },
        },
        {
          method: "DELETE",
          rpc: "DELETE",
          headers: {
            ...forwarded,
            "content-type": undefined,
            "mcp-protocol-version": "2025-06-18",
            "mcp-session-id": "s1",
          },
          body: undefined,
        },
      ].map((r) => ({
        ...r,
        headers: JSON.parse(JSON.stringify(r.headers)) as Record<string, string>,
      })),
    );
    expect(other.map((r) => r.rpc)).toEqual([
      "initialize",
      "notifications/initialized",
      "tools/list",
    ]);
    expect(vi.mocked(logger.info).mock.calls.map((c) => c[0])).toEqual(
      expect.arrayContaining([
        "systemTools.getTools(entityId=12, attachments=2) configured servers=[docspace, other] — enumerating",
        expect.stringMatching(
          /^systemTools\.getTools server=docspace -> 3 tool\(s\) in \d+ms: \[get_room, delete_room, create_room\]$/,
        ),
        expect.stringMatching(
          /^systemTools\.getTools server=other -> 0 tool\(s\) in \d+ms: \[<none>\]$/,
        ),
        `MCP fetch -> POST ${DOCSPACE_URL} (authorization=true, referer=https://portal.example.com/, headers=[cookie,origin,user-agent])`,
        `MCP fetch <- 200 OK for ${DOCSPACE_URL}`,
      ]),
    );
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("sends the configured portal address as the referer", async () => {
    state.portal = "http://host.docker.internal:8092";
    const docspace = mcpServer(upstream, DOCSPACE_URL);
    mcpServer(upstream, OTHER_URL);

    await inRequestContext(BROWSER, () => systemToolsSource.getTools());

    expect(docspace[0]?.headers["referer"]).toBe("http://host.docker.internal:8092/");
  });

  // DEFECT tools-6: a forwarded `mcp-protocol-version` is sent next to the library's `MCP-Protocol-Version` and the two are joined into one value — expected the library's header to win, as the comment above forwardingFetch says; the spread compares keys case-sensitively (systemTools.ts:210)
  it("joins a forwarded protocol version with the library's and derives referer and authorization", async () => {
    const docspace = mcpServer(upstream, DOCSPACE_URL, { sessionId: "own" });
    mcpServer(upstream, OTHER_URL);

    await inRequestContext(
      {
        "mcp-protocol-version": "1999-01-01",
        referer: "https://portal.example.com/x",
        authorization: "Bearer H",
      },
      () => systemToolsSource.getTools(),
    );

    expect(docspace[1]?.headers).toEqual({
      ...LIBRARY_HEADERS,
      "mcp-protocol-version": "1999-01-01, 2025-06-18",
      "mcp-session-id": "own",
      referer: "https://portal.example.com/",
      authorization: "Bearer H",
    });
  });

  it("sends no referer and no authorization outside a request", async () => {
    const docspace = mcpServer(upstream, DOCSPACE_URL);
    mcpServer(upstream, OTHER_URL);

    await systemToolsSource.getTools();

    expect(docspace[0]?.headers).toEqual(LIBRARY_HEADERS);
    expect(logger.info).toHaveBeenCalledWith(
      `MCP fetch -> POST ${DOCSPACE_URL} (authorization=false, referer=-, headers=[])`,
    );
  });

  // DEFECT tools-3: the body of an MCP server's error answer is written to the log (first 500 characters) — expected the status only ([ts-ai-style.md, State and logging])
  it("answers an empty group for a server that refuses, and logs its answer", async () => {
    upstream.use(
      http.post(DOCSPACE_URL, () =>
        HttpResponse.text("denied", { status: 401, statusText: "Unauthorized" }),
      ),
    );
    mcpServer(upstream, OTHER_URL, { tools: [PLAIN_TOOL] });

    const grouped = await systemToolsSource.getTools();

    expect(grouped).toEqual({ docspace: [], other: [PLAIN_TOOL] });
    expect(logger.warn).toHaveBeenCalledWith(
      `MCP fetch <- 401 Unauthorized for ${DOCSPACE_URL} body=denied`,
    );
    expect(logger.error).not.toHaveBeenCalled();
  });

  it("cuts the logged error body at 500 characters", async () => {
    upstream.use(
      http.post(DOCSPACE_URL, () => HttpResponse.text("x".repeat(600), { status: 500 })),
    );
    mcpServer(upstream, OTHER_URL);

    await systemToolsSource.getTools();

    expect(logger.warn).toHaveBeenCalledWith(
      `MCP fetch <- 500 Internal Server Error for ${DOCSPACE_URL} body=${"x".repeat(500)}`,
    );
  });

  it("logs an empty body when the error answer cannot be read", async () => {
    upstream.use(
      http.post(
        DOCSPACE_URL,
        () =>
          new HttpResponse(
            new ReadableStream({
              start(controller) {
                controller.error(new Error("reset"));
              },
            }),
            { status: 502, statusText: "Bad Gateway" },
          ),
      ),
    );
    mcpServer(upstream, OTHER_URL);

    await expect(systemToolsSource.getTools()).resolves.toEqual({ docspace: [], other: [] });
    expect(logger.warn).toHaveBeenCalledWith(
      `MCP fetch <- 502 Bad Gateway for ${DOCSPACE_URL} body=`,
    );
  });

  // DEFECT tools-4: when every configured server fails the answer is `{ docspace: [], other: [] }` and the "no tools from any server" warning is not written — it fires only when no server is configured (systemTools.ts:379)
  it("does not warn about missing tools when every server fails", async () => {
    upstream.use(
      http.post(DOCSPACE_URL, () => HttpResponse.error()),
      http.post(OTHER_URL, () => HttpResponse.error()),
    );

    await expect(systemToolsSource.getTools("1")).resolves.toEqual({ docspace: [], other: [] });
    expect(logger.error).toHaveBeenCalledWith(
      `MCP fetch failed for ${DOCSPACE_URL}: Failed to fetch`,
    );
    expect(logger.error).toHaveBeenCalledWith(`MCP fetch failed for ${OTHER_URL}: Failed to fetch`);
    expect(logger.warn).not.toHaveBeenCalledWith(
      expect.stringContaining("no tools from any server"),
    );
  });

  it("answers an empty map and logs when a listed tool cannot be read", async () => {
    mcpServer(upstream, DOCSPACE_URL, { tools: [null] });
    mcpServer(upstream, OTHER_URL, { tools: [PLAIN_TOOL] });

    await expect(systemToolsSource.getTools()).resolves.toEqual({});
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringMatching(
        /^systemTools\.getTools failed after \d+ms: Cannot read properties of null/,
      ),
    );
  });

  describe("on fake timers", () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    it("gives up on a server that does not answer within the upstream timeout", async () => {
      upstream.use(
        http.post(DOCSPACE_URL, async () => {
          await delay("infinite");
          return HttpResponse.text("never");
        }),
      );
      mcpServer(upstream, OTHER_URL);

      const pending = systemToolsSource.getTools();
      await vi.advanceTimersByTimeAsync(299_999);
      expect(logger.error).not.toHaveBeenCalled();
      await vi.advanceTimersByTimeAsync(1);

      await expect(pending).resolves.toEqual({ docspace: [], other: [] });
      expect(logger.error).toHaveBeenCalledWith(
        `MCP fetch failed for ${DOCSPACE_URL}: upstream timeout after 300000ms`,
      );
    });
  });
});

describe("systemToolsSource.callTool", () => {
  const ROOM_RESULT = { content: [{ type: "text", text: "room 1" }] };

  it("lists the tools, calls the one with that name in its own session and answers the result as JSON", async () => {
    const docspace = mcpServer(upstream, DOCSPACE_URL, {
      sessionId: "s1",
      tools: [READ_TOOL],
      call: () => ({ result: { ...ROOM_RESULT, structuredContent: { id: 1 } } }),
    });
    const other = mcpServer(upstream, OTHER_URL, { tools: [PLAIN_TOOL] });

    const result = await inRequestContext(BROWSER, () =>
      systemToolsSource.callTool("get_room", { id: 1 }, "12"),
    );

    expect(result).toBe(JSON.stringify(ROOM_RESULT));
    expect(docspace.map((r) => r.rpc)).toEqual([
      "initialize",
      "notifications/initialized",
      "tools/list",
      "DELETE",
      "initialize",
      "notifications/initialized",
      "tools/call",
      "DELETE",
    ]);
    expect(docspace[6]?.body).toEqual({
      jsonrpc: "2.0",
      id: expect.stringMatching(/^call-docspace-get_room-\d+$/),
      method: "tools/call",
      params: { name: "get_room", arguments: { id: 1 } },
    });
    expect(docspace[6]?.headers).toMatchObject({
      authorization: "KEY",
      referer: "https://portal.example.com/",
    });
    expect(other.map((r) => r.rpc)).toEqual([
      "initialize",
      "notifications/initialized",
      "tools/list",
    ]);
    expect(vi.mocked(logger.info).mock.calls.map((c) => c[0])).toEqual(
      expect.arrayContaining([
        "systemTools.callTool name=get_room args=[id] entityId=12",
        "systemTools.callTool name=get_room ok",
      ]),
    );
  });

  it("passes the call options on, so the progress token is sent", async () => {
    const docspace = mcpServer(upstream, DOCSPACE_URL, {
      tools: [READ_TOOL],
      call: () => ({ result: ROOM_RESULT }),
    });
    mcpServer(upstream, OTHER_URL);

    await systemToolsSource.callTool("get_room", {}, undefined, { onprogress: () => undefined });

    expect(docspace.find((r) => r.rpc === "tools/call")?.body).toMatchObject({
      params: {
        name: "get_room",
        arguments: {},
        _meta: { progressToken: expect.stringMatching(/^call-docspace-get_room-\d+$/) },
      },
    });
    expect(logger.info).toHaveBeenCalledWith(
      "systemTools.callTool name=get_room args=[] entityId=-",
    );
  });

  it.each<[string, unknown, string]>([
    [
      "the last line of the error text",
      {
        isError: true,
        content: [
          { type: "text", text: "step 1\n  GET /api/2.0/files/rooms: 401 Unauthorized  \n\n" },
        ],
      },
      "GET /api/2.0/files/rooms: 401 Unauthorized",
    ],
    [
      "the first part that carries text",
      {
        isError: true,
        content: [
          null,
          { type: "image" },
          { text: 5 },
          { type: "text", text: "first" },
          { text: "second" },
        ],
      },
      "first",
    ],
    ["a placeholder without content", { isError: true }, "<no error text>"],
    [
      "a placeholder for content that is not a list",
      { isError: true, content: "boom" },
      "<no error text>",
    ],
    [
      "a placeholder for blank text",
      { isError: true, content: [{ text: "  \n " }] },
      "<no error text>",
    ],
    [
      "300 characters of a long line",
      { isError: true, content: [{ text: "e".repeat(400) }] },
      "e".repeat(300),
    ],
  ])("warns with %s of an error result", async (_name, result, expected) => {
    mcpServer(upstream, DOCSPACE_URL, { tools: [READ_TOOL], call: () => ({ result }) });
    mcpServer(upstream, OTHER_URL);

    await expect(systemToolsSource.callTool("get_room", {})).resolves.toBe(JSON.stringify(result));
    expect(logger.warn).toHaveBeenCalledWith(
      `systemTools.callTool name=get_room returned an error result: ${expected}`,
    );
    expect(logger.info).not.toHaveBeenCalledWith("systemTools.callTool name=get_room ok");
  });

  it.each<[string, unknown]>([
    ["isError false", { isError: false, content: [{ text: "fine" }] }],
    ["isError as a string", { isError: "true", content: [{ text: "x" }] }],
    ["a string result", "plain"],
    ["a null result", null],
  ])("logs ok for %s", async (_name, result) => {
    mcpServer(upstream, DOCSPACE_URL, { tools: [READ_TOOL], call: () => ({ result }) });
    mcpServer(upstream, OTHER_URL);

    await expect(systemToolsSource.callTool("get_room", {})).resolves.toBe(JSON.stringify(result));
    expect(logger.info).toHaveBeenCalledWith("systemTools.callTool name=get_room ok");
    expect(logger.warn).not.toHaveBeenCalled();
  });

  // DEFECT tools-5: a call the library answers with `{"error": ...}` (unknown tool, JSON-RPC error, a server that fails the call) is logged at info as "ok" — expected a warn like the isError result, the case the comment above describeToolErrorResult says was invisible (systemTools.ts:303)
  it.each<[string, (string | undefined)[], () => Record<string, unknown>, string]>([
    [
      "an unknown tool",
      [],
      () => ({ result: {} }),
      JSON.stringify({ error: "Unknown system tool: get_room" }),
    ],
    [
      "a JSON-RPC error",
      ["get_room"],
      () => ({ error: { code: -32000, message: "bad" } }),
      JSON.stringify({ error: "MCP tool error (-32000): bad" }),
    ],
  ])("answers %s as an error text and logs ok", async (_name, names, call, expected) => {
    mcpServer(upstream, DOCSPACE_URL, {
      tools: names.map((name) => ({ name, inputSchema: {} })),
      call,
    });
    mcpServer(upstream, OTHER_URL);

    await expect(systemToolsSource.callTool("get_room", {})).resolves.toBe(expected);
    expect(logger.info).toHaveBeenCalledWith("systemTools.callTool name=get_room ok");
  });

  it("logs and rethrows when the listing cannot be searched", async () => {
    mcpServer(upstream, DOCSPACE_URL, { tools: [null] });
    mcpServer(upstream, OTHER_URL);

    await expect(systemToolsSource.callTool("get_room", {})).rejects.toBeInstanceOf(TypeError);
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringMatching(
        /^systemTools\.callTool name=get_room failed: Cannot read properties of null/,
      ),
    );
  });
});
