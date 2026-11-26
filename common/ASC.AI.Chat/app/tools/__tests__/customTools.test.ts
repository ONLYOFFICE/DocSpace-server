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

import { beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { getCustomServerNames } from "../../requestContext.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";
import { storage } from "../../storage/index.js";
import { mcpServer } from "../../__tests__/helpers/mcpStub.js";

vi.mock("../../../config/index.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../../config/index.js")>()),
  getMcpServers: () => [
    { id: "1", name: "docspace", endpoint: "http://mcp.test/mcp" },
    { id: "2", name: "sysmcp", endpoint: "http://sys.test/mcp" },
  ],
}));

const { customToolsSource, primeCustomServers, resolveCustomServers } =
  await import("../customTools.js");

const upstream = useUpstream();

const JIRA = { url: "https://jira.example.com/mcp", headers: { "x-api-key": "k" } };
const WIKI = { url: "https://wiki.example.com/mcp" };

// GET internal/ai/mcp-servers answers `IReadOnlyList<McpServerDto>` of
// products/ASC.AI/Server/Models/ResponseDto/McpServerDto.cs: `{ name, config }`, config a JSON string.
const REGISTRY = [
  { name: "docspace", config: JSON.stringify({ url: "https://evil.example.com/mcp" }) },
  { name: "sysmcp", config: JSON.stringify({ url: "https://shadow.example.com/mcp" }) },
  { name: "jira", config: JSON.stringify(JIRA) },
  { name: "wiki", config: JSON.stringify(WIKI) },
  { name: "browser", config: JSON.stringify({ command: "npx", args: ["server"] }) },
  { name: "empty", config: JSON.stringify({ url: "" }) },
  { name: "numeric", config: JSON.stringify({ url: 5 }) },
  { name: "nullurl", config: JSON.stringify({ url: null }) },
];

function answerRegistry(body: unknown = REGISTRY): void {
  upstream.use(http.get("*/internal/ai/mcp-servers", () => docspace(body as never)));
}

beforeEach(() => {
  for (const level of ["log", "debug", "info", "warn", "error"] as const) {
    vi.spyOn(console, level).mockImplementation(() => undefined);
  }
});

describe("resolveCustomServers", () => {
  it("keeps the registered HTTP servers and leaves out the portal's, the system ones and those without a url", async () => {
    answerRegistry();

    const result = await inRequestContext({}, async () => ({
      servers: await resolveCustomServers("123"),
      names: getCustomServerNames(),
    }));

    expect(result).toEqual({ servers: { jira: JIRA, wiki: WIKI }, names: ["jira", "wiki"] });
    expect(upstream.seen).toEqual([
      expect.objectContaining({ method: "GET", path: "/internal/ai/mcp-servers" }),
    ]);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it.each<[string, unknown, Record<string, unknown>]>([
    ["a null answer", null, {}],
    ["an object answer", { name: "jira", config: "{}" }, {}],
    ["an empty list", [], {}],
    ["items that are not objects", [null, "jira", 5], {}],
    ["an item without a name", [{ config: JSON.stringify(WIKI) }], {}],
    ["an item with a numeric name", [{ name: 5, config: JSON.stringify(WIKI) }], {}],
    ["an item without a config", [{ name: "wiki" }], {}],
    ["an item with a null config", [{ name: "wiki", config: null }], {}],
    ["an item with an object config", [{ name: "wiki", config: WIKI }], {}],
    ["an item with a config that is not JSON", [{ name: "wiki", config: "{url" }], {}],
    ["an item with a JSON array config", [{ name: "wiki", config: "[1]" }], {}],
    ["an item with an empty name", [{ name: "", config: JSON.stringify(WIKI) }], { "": WIKI }],
    [
      "an item with extra fields",
      [{ name: "wiki", config: JSON.stringify({ ...WIKI, transport: "http" }), entityId: 4 }],
      { wiki: { ...WIKI, transport: "http" } },
    ],
  ])("maps %s", async (_name, body, expected) => {
    answerRegistry(body);

    await expect(resolveCustomServers()).resolves.toEqual(expected);
  });

  it.each([401, 403, 500, 503])(
    "answers an empty map and warns when the read fails with %i",
    async (status) => {
      upstream.use(http.get("*/internal/ai/mcp-servers", () => docspaceError(status, "detail")));

      const result = await inRequestContext({}, async () => ({
        servers: await resolveCustomServers(),
        names: getCustomServerNames(),
      }));

      expect(result).toEqual({ servers: {}, names: [] });
      expect(logger.warn).toHaveBeenCalledWith(
        expect.stringMatching(
          new RegExp(`^customTools: portal-wide server map read failed: AI service ${status} `),
        ),
      );
    },
  );

  it("answers an empty map when the AI service cannot be reached", async () => {
    upstream.use(http.get("*/internal/ai/mcp-servers", () => HttpResponse.error()));

    await expect(resolveCustomServers()).resolves.toEqual({});
    expect(logger.warn).toHaveBeenCalledWith(
      "customTools: portal-wide server map read failed: Failed to fetch",
    );
  });

  it("warns with the text of a non-Error failure", async () => {
    vi.spyOn(storage.mcpServers, "readAll").mockRejectedValueOnce("down");

    await expect(resolveCustomServers()).resolves.toEqual({});
    expect(logger.warn).toHaveBeenCalledWith(
      "customTools: portal-wide server map read failed: down",
    );
  });

  describe("strict", () => {
    it("answers the servers like the tolerant read", async () => {
      answerRegistry();

      await expect(resolveCustomServers(undefined, { strict: true })).resolves.toEqual({
        jira: JIRA,
        wiki: WIKI,
      });
    });

    it("reads a 404 as an empty registry", async () => {
      upstream.use(http.get("*/internal/ai/mcp-servers", () => docspaceError(404)));

      await expect(resolveCustomServers(undefined, { strict: true })).resolves.toEqual({});
      expect(logger.warn).toHaveBeenCalledWith(
        expect.stringMatching(/^customTools: portal-wide server map read failed: AI service 404 /),
      );
    });

    it.each([400, 401, 403, 500, 503])(
      "rethrows the AiServiceHttpError of a %i (Bugs 84400, 84401)",
      async (status) => {
        upstream.use(http.get("*/internal/ai/mcp-servers", () => docspaceError(status)));

        const error = await resolveCustomServers(undefined, { strict: true }).catch(
          (e: unknown) => e,
        );

        expect(error).toBeInstanceOf(AiServiceHttpError);
        expect((error as AiServiceHttpError).status).toBe(status);
        expect(logger.warn).not.toHaveBeenCalled();
      },
    );

    it("rethrows a network failure", async () => {
      upstream.use(http.get("*/internal/ai/mcp-servers", () => HttpResponse.error()));

      await expect(resolveCustomServers(undefined, { strict: true })).rejects.toThrow(
        "Failed to fetch",
      );
    });

    it("leaves the names of the request untouched when it rethrows", async () => {
      upstream.use(http.get("*/internal/ai/mcp-servers", () => docspaceError(500)));

      const names = await inRequestContext({}, async () => {
        await resolveCustomServers(undefined, { strict: true }).catch(() => undefined);
        return getCustomServerNames();
      });

      expect(names).toEqual([]);
    });
  });
});

describe("primeCustomServers", () => {
  it("stores the names in the request and answers nothing", async () => {
    answerRegistry();

    const result = await inRequestContext({}, async () => ({
      value: await primeCustomServers(),
      names: getCustomServerNames(),
    }));

    expect(result).toEqual({ value: undefined, names: ["jira", "wiki"] });
  });
});

describe("customToolsSource", () => {
  it("has no fixed server types", () => {
    expect(customToolsSource.getServerTypes()).toEqual([]);
  });

  it("lists the tools of each registered server with its own headers and none of the caller's", async () => {
    answerRegistry([
      { name: "jira", config: JSON.stringify(JIRA) },
      { name: "wiki", config: JSON.stringify(WIKI) },
    ]);
    const jira = mcpServer(upstream, JIRA.url, {
      tools: [{ name: "find_issue", inputSchema: {} }],
    });
    const wiki = mcpServer(upstream, WIKI.url, { tools: [] });

    const grouped = await inRequestContext(
      {
        cookie: "asc_auth_key=KEY",
        authorization: "Bearer H",
        origin: "https://portal.example.com",
      },
      () => customToolsSource.getTools("5"),
    );

    expect(grouped).toEqual({ jira: [{ name: "find_issue", inputSchema: {} }], wiki: [] });
    expect(jira[0]?.headers).toEqual({
      "x-api-key": "k",
      accept: "application/json, text/event-stream",
      "content-type": "application/json",
    });
    expect(wiki[0]?.headers).toEqual({
      accept: "application/json, text/event-stream",
      "content-type": "application/json",
    });
  });

  it("lists nothing when the registry cannot be read", async () => {
    upstream.use(http.get("*/internal/ai/mcp-servers", () => docspaceError(500)));

    await expect(customToolsSource.getTools()).resolves.toEqual({});
  });
});
