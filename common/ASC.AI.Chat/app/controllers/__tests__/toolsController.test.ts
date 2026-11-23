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

const getCustomServer = vi.fn();
const listCustomServers = vi.fn();
const updateCustomServer = vi.fn();
const replaceAllCustomServers = vi.fn();
const readByName = vi.fn();
const readAll = vi.fn();
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@onlyoffice/ai-chat/core")>();
  return {
    ...actual,
    ToolsEngine: class {
      getCustomServer = getCustomServer;
      listCustomServers = listCustomServers;
      updateCustomServer = updateCustomServer;
      replaceAllCustomServers = replaceAllCustomServers;
    },
  };
});
vi.mock("../../tools/customTools.js", () => ({
  customToolsSource: {},
  resolveCustomServers: vi.fn(),
}));
vi.mock("../../storage/index.js", () => ({
  storage: { mcpServers: { readByName, readAll } },
}));
const getSystemServerConfig = vi.fn();
vi.mock("../../tools/systemTools.js", () => ({ systemToolsSource: {}, getSystemServerConfig }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: class extends Error {},
  assertEntityAccessible: vi.fn(async () => {}),
}));

const { toolsController } = await import("../toolsController.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const call = async (handler: unknown, req: { query?: unknown; body?: unknown }) => {
  const res = { status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  const next = vi.fn();
  await (handler as Handler)({ query: {}, body: {}, ...req }, res, next);
  const failure = next.mock.calls[0];
  if (failure) throw failure[0];
  return res.json.mock.calls[0]?.[0];
};

const read = (name: string) => call(toolsController.getCustomServer, { query: { name } });

describe("toolsController.getCustomServer", () => {
  beforeEach(() => {
    getCustomServer.mockReset();
    getSystemServerConfig.mockReset().mockReturnValue(undefined);
  });

  it("answers a custom server without the headers it is called with", async () => {
    getCustomServer.mockResolvedValue({
      url: "https://mcp.example.com/sse",
      headers: { Authorization: "Bearer secret" },
    });
    expect(await read("crm")).toEqual({ url: "https://mcp.example.com/sse" });
  });

  it("answers a system server empty and an unknown name null", async () => {
    getCustomServer.mockResolvedValue({ url: "http://internal:5158/mcp", headers: { a: "b" } });
    getSystemServerConfig.mockReturnValue({ url: "http://internal:5158/mcp" });
    expect(await read("docspace-tools")).toEqual({});

    getCustomServer.mockResolvedValue(null);
    expect(await read("gone")).toBeNull();
  });
});

describe("toolsController custom server headers", () => {
  const URL = "https://mcp.example.com/sse";
  const SECRET = { Authorization: "Bearer secret" };

  beforeEach(() => {
    listCustomServers.mockReset();
    updateCustomServer.mockReset().mockResolvedValue({ success: true });
    replaceAllCustomServers.mockReset().mockResolvedValue({ success: true });
    readByName.mockReset().mockResolvedValue(null);
    readAll.mockReset().mockResolvedValue({});
    getSystemServerConfig.mockReset().mockReturnValue(undefined);
  });

  it("lists the custom servers without their headers", async () => {
    listCustomServers.mockResolvedValue({
      crm: { url: URL, headers: SECRET },
      plain: { url: "https://plain.example.com/mcp" },
    });
    expect(await call(toolsController.listCustomServers, {})).toEqual({
      crm: { url: URL },
      plain: { url: "https://plain.example.com/mcp" },
    });
  });

  it("keeps the stored headers when the listed map is saved back", async () => {
    readAll.mockResolvedValue({ crm: { url: URL, headers: SECRET } });
    await call(toolsController.replaceAllCustomServers, {
      body: { map: { crm: { url: URL, timeout: 5 }, fresh: { url: "https://new.example.com" } } },
    });
    expect(replaceAllCustomServers.mock.calls[0]?.[0]).toEqual({
      crm: { url: URL, timeout: 5, headers: SECRET },
      fresh: { url: "https://new.example.com" },
    });
  });

  it("does not carry the stored headers to another url", async () => {
    readAll.mockResolvedValue({ crm: { url: URL, headers: SECRET } });
    await call(toolsController.replaceAllCustomServers, {
      body: { map: { crm: { url: "https://elsewhere.example.com" } } },
    });
    expect(replaceAllCustomServers.mock.calls[0]?.[0]).toEqual({
      crm: { url: "https://elsewhere.example.com" },
    });
  });

  it("lets explicit headers replace or remove the stored ones", async () => {
    readByName.mockResolvedValue({ url: URL, headers: SECRET });
    await call(toolsController.updateCustomServer, {
      body: { name: "crm", config: { url: URL, headers: {} } },
    });
    expect(updateCustomServer.mock.calls[0]?.[1]).toEqual({ url: URL, headers: {} });

    await call(toolsController.updateCustomServer, {
      body: { name: "crm", config: { url: URL } },
    });
    expect(updateCustomServer.mock.calls[1]?.[1]).toEqual({ url: URL, headers: SECRET });
  });
});
