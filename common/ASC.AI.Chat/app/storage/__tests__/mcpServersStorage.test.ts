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
import logger from "../../log.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { getChatContextMisses } from "../../requestContext.js";
import {
  HttpMcpServersStorage,
  isUnreadableMcpServerConfig,
  parseMcpServerConfig,
  UNREADABLE_MCP_SERVER_CONFIG,
} from "../mcpServersStorage.js";
import { inRound, scopeOf, snapshotOf } from "../../__tests__/fixtures/chatContext.js";

// products/ASC.AI/Server/Api/McpServerStorageController.cs: POST mcp-servers,
// GET mcp-servers/{name}, GET mcp-servers, PUT mcp-servers/{name}, PUT mcp-servers,
// DELETE mcp-servers/{name}.
const upstream = useUpstream();
const storage = new HttpMcpServersStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

beforeEach(() => {
  vi.mocked(logger.warn).mockClear();
});

/** `McpServerDto` (products/ASC.AI/Server/Models/ResponseDto/McpServerDto.cs): config is a JSON string. */
const serverDto = (
  name = "jira",
  config = '{"url":"https://mcp.example.com/jira","headers":{"x":"y"}}',
) => ({
  name,
  config,
});

describe("parseMcpServerConfig", () => {
  it.each([
    ["a JSON object string", '{"url":"u"}', { url: "u" }],
    ["an object", { url: "u" }, { url: "u" }],
    ["an empty JSON object", "{}", {}],
    ["null", null, null],
    ["undefined", undefined, null],
    ["text that is not JSON", "{", null],
    ["an empty string", "", null],
    ["JSON of a list", "[1]", null],
    ["JSON of a string", '"u"', null],
    ["JSON null", "null", null],
    ["a list", [{ url: "u" }], null],
    ["a number", 1, null],
  ])("reads %s", (_name, raw, expected) => {
    expect(parseMcpServerConfig(raw)).toEqual(expected);
  });
});

describe("readByName", () => {
  it("reads the server's config by its encoded name, headers included", async () => {
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspace(serverDto())));
    await expect(storage.readByName("my server", "42")).resolves.toEqual({
      url: "https://mcp.example.com/jira",
      headers: { x: "y" },
    });
    expect(upstream.seen[0]?.path).toBe("/internal/ai/mcp-servers/my%20server");
  });

  it("reads an answer of null as none", async () => {
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspace(null)));
    await expect(storage.readByName("jira")).resolves.toBeNull();
  });

  it.each([
    ["a list", [serverDto()]],
    ["a string", "jira"],
  ])("rejects an answer with %s as of an unknown shape, not as none", async (_name, body) => {
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspace(body)));
    await expect(storage.readByName("jira")).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });

  // A stored server whose config cannot be read is neither "no such server" (an update wrote it
  // without its stored headers) nor unknown (a 502 left only a delete able to reach it): it is
  // the stand-in, which the engine finds taken and a write recognizes.
  it.each([
    ["no config", { name: "jira" }],
    ["config null", { name: "jira", config: null }],
    ["config an object", { name: "jira", config: { url: "u" } }],
    ["config not JSON", { name: "jira", config: "{" }],
    ["config a JSON list", { name: "jira", config: "[]" }],
  ])("reads an answer with %s as the stand-in of an unreadable config", async (_name, body) => {
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspace(body)));
    const config = await storage.readByName("jira");
    expect(config).toBe(UNREADABLE_MCP_SERVER_CONFIG);
    expect(isUnreadableMcpServerConfig(config)).toBe(true);
    expect(Object.isFrozen(config)).toBe(true);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "HttpMcpServersStorage: an entry of the registry cannot be read, its config (server jira)",
    );
  });

  it("reads a 404 as none and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspaceError(404)));
    await expect(storage.readByName("jira")).resolves.toBeNull();
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspaceError(500)));
    await expect(storage.readByName("jira")).rejects.toMatchObject({ status: 500 });
  });

  it("serves a primed round's global registry without a request", async () => {
    const snapshot = snapshotOf({
      global: scopeOf({ mcpServers: { jira: { url: "u" } } }),
      entity: scopeOf({ mcpServers: { other: { url: "o" } } }),
    });
    const result = await inRound(snapshot, async () => [
      await storage.readByName("jira", "42"),
      await storage.readByName("other", "42"),
      getChatContextMisses(),
    ]);
    expect(result).toEqual([{ url: "u" }, null, []]);
    expect(upstream.seen).toEqual([]);
  });

  it("asks once the slice is stale, counting the miss", async () => {
    upstream.use(http.get("*/internal/ai/mcp-servers/:name", () => docspace(serverDto())));
    const snapshot = snapshotOf({ stale: new Set(["mcpServers"]) });
    const misses = await inRound(snapshot, async () => {
      await storage.readByName("jira");
      return getChatContextMisses();
    });
    expect(misses).toEqual(["mcpServers.readByName(jira)"]);
  });
});

describe("readAll", () => {
  it("assembles the name to config map from the list", async () => {
    upstream.use(
      http.get("*/internal/ai/mcp-servers", () =>
        docspace([
          serverDto("jira", '{"url":"j"}'),
          serverDto("github", '{"url":"g"}'),
          null,
          "x",
          { config: '{"url":"n"}' },
          { name: 5, config: "{}" },
          { name: "object", config: { url: "o" } },
          { name: "broken", config: "{" },
          { name: "list", config: "[]" },
        ]),
      ),
    );
    await expect(storage.readAll("42")).resolves.toEqual({
      jira: { url: "j" },
      github: { url: "g" },
    });
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/mcp-servers" }]);
    // Each entry left out is named: nothing else shows that the server is stored.
    const left = "HttpMcpServersStorage: an entry of the registry cannot be read, its";
    expect(vi.mocked(logger.warn).mock.calls.map(([line]) => line)).toEqual([
      `${left} name`,
      `${left} name`,
      `${left} name`,
      `${left} name`,
      `${left} config (server object)`,
      `${left} config (server broken)`,
      `${left} config (server list)`,
    ]);
  });

  describe("readAllForWrite", () => {
    it("answers the registry when every entry can be read, past a round's snapshot", async () => {
      upstream.use(
        http.get("*/internal/ai/mcp-servers", () => docspace([serverDto("jira", '{"url":"j"}')])),
      );
      const snapshot = snapshotOf({ global: scopeOf({ mcpServers: { old: { url: "o" } } }) });

      await expect(inRound(snapshot, () => storage.readAllForWrite())).resolves.toEqual({
        jira: { url: "j" },
      });
    });

    it("keeps an entry whose config cannot be read as the stand-in and leaves out a nameless one", async () => {
      upstream.use(
        http.get("*/internal/ai/mcp-servers", () =>
          docspace([
            serverDto("github", '{"url":"g"}'),
            null,
            { config: '{"url":"n"}' },
            { name: "jira" },
            { name: "broken", config: "not json" },
          ]),
        ),
      );

      const registry = await storage.readAllForWrite();

      expect(registry).toEqual({ github: { url: "g" }, jira: {}, broken: {} });
      expect(registry["jira"]).toBe(UNREADABLE_MCP_SERVER_CONFIG);
      expect(registry["broken"]).toBe(UNREADABLE_MCP_SERVER_CONFIG);
      expect(isUnreadableMcpServerConfig(registry["github"])).toBe(false);
      expect(vi.mocked(logger.warn)).toHaveBeenCalledTimes(4);
    });
  });

  // McpServerStorageController always answers a list: anything else is unknown, not "no custom
  // server". Read as empty, it let a replace-all keep none of the stored headers.
  it.each([
    ["an object", () => docspace({ jira: '{"url":"j"}' })],
    ["null", () => docspace(null)],
    ["an empty answer", noContent],
  ])("rejects %s as an answer of an unknown shape, not as no server", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/mcp-servers", answer));
    await expect(storage.readAll()).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });

  it.each([404, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/mcp-servers", () => docspaceError(status)));
    await expect(storage.readAll()).rejects.toMatchObject({ status });
  });

  it("serves a copy of a primed round's registry, and asks once the slice is stale", async () => {
    const servers = { jira: { url: "u" } };
    const snapshot = snapshotOf({ global: scopeOf({ mcpServers: servers }) });
    const served = await inRound(snapshot, () => storage.readAll());
    expect(served).toEqual(servers);
    expect(served).not.toBe(servers);
    expect(upstream.seen).toEqual([]);

    upstream.use(http.get("*/internal/ai/mcp-servers", () => docspace([])));
    snapshot.stale.add("mcpServers");
    const misses = await inRound(snapshot, async () => {
      await storage.readAll();
      return getChatContextMisses();
    });
    expect(misses).toEqual(["mcpServers.readAll"]);
  });
});

describe("the writes", () => {
  const config = { url: "https://mcp.example.com/jira", headers: { authorization: "Bearer t" } };

  it("create posts the config as a JSON string for the portal and marks the slice stale", async () => {
    upstream.use(http.post("*/internal/ai/mcp-servers", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.create("jira", config, "42"));
    expect(upstream.seen).toMatchObject([
      {
        method: "POST",
        path: "/internal/ai/mcp-servers",
        body: { name: "jira", config: JSON.stringify(config), entityId: null },
      },
    ]);
    expect([...snapshot.stale]).toEqual(["mcpServers"]);
  });

  it("update puts the config by the encoded name", async () => {
    upstream.use(http.put("*/internal/ai/mcp-servers/:name", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.update("my server", config, "42"));
    expect(upstream.seen).toMatchObject([
      {
        method: "PUT",
        path: "/internal/ai/mcp-servers/my%20server",
        body: { config: JSON.stringify(config), entityId: null },
      },
    ]);
    expect([...snapshot.stale]).toEqual(["mcpServers"]);
  });

  it("replaceAll puts every config as a JSON string", async () => {
    upstream.use(http.put("*/internal/ai/mcp-servers", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.replaceAll({ jira: config, empty: {} as never }, "42"));
    expect(upstream.seen).toMatchObject([
      {
        method: "PUT",
        path: "/internal/ai/mcp-servers",
        body: { servers: { jira: JSON.stringify(config), empty: "{}" }, entityId: null },
      },
    ]);
    expect([...snapshot.stale]).toEqual(["mcpServers"]);
  });

  it("leaves the slice fresh when a write is refused", async () => {
    upstream.use(
      http.post("*/internal/ai/mcp-servers", () => docspaceError(400)),
      http.put("*/internal/ai/mcp-servers/:name", () => docspaceError(404)),
      http.put("*/internal/ai/mcp-servers", () => docspaceError(500)),
    );
    const snapshot = snapshotOf();
    await expect(inRound(snapshot, () => storage.create("a", config))).rejects.toMatchObject({
      status: 400,
    });
    await expect(inRound(snapshot, () => storage.update("a", config))).rejects.toMatchObject({
      status: 404,
    });
    await expect(inRound(snapshot, () => storage.replaceAll({}))).rejects.toMatchObject({
      status: 500,
    });
    expect(snapshot.stale.size).toBe(0);
  });

  it("delete marks the slice stale first, treats a 404 as done and rethrows a 500", async () => {
    upstream.use(http.delete("*/internal/ai/mcp-servers/:name", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.delete("my server"));
    expect(upstream.seen).toMatchObject([
      { method: "DELETE", path: "/internal/ai/mcp-servers/my%20server" },
    ]);
    expect([...snapshot.stale]).toEqual(["mcpServers"]);

    upstream.use(http.delete("*/internal/ai/mcp-servers/:name", () => docspaceError(404)));
    await expect(storage.delete("a")).resolves.toBeUndefined();
    upstream.use(http.delete("*/internal/ai/mcp-servers/:name", () => docspaceError(500)));
    await expect(storage.delete("a")).rejects.toMatchObject({ status: 500 });
  });
});
