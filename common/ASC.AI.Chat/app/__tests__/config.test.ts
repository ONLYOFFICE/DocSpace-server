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

import { readFileSync } from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// config/index.ts with nconf as the boundary: the fake answers `get` from `store.values`
// (a `:` in the key walks into nested objects, as nconf does) and records every file it is
// handed. `fs.existsSync` answers whether a NODE_ENV file exists. Environment variables are
// stubbed per test and restored afterwards.

const store = vi.hoisted(() => ({
  values: {} as Record<string, unknown>,
  files: [] as [string, string][],
  calls: [] as string[],
  exists: vi.fn<(file: string) => boolean>(),
}));

vi.mock("nconf", () => {
  const nconf = {
    argv: () => {
      store.calls.push("argv");
      return nconf;
    },
    env: () => {
      store.calls.push("env");
      return nconf;
    },
    file: (name: string, file: string) => {
      store.files.push([name, file]);
      return nconf;
    },
    get: (key?: string) => {
      if (key === undefined) {
        return store.values;
      }
      let value: unknown = store.values;
      for (const part of key.split(":")) {
        value = (value as Record<string, unknown> | undefined)?.[part];
      }
      return value;
    },
  };
  return { default: nconf };
});

vi.mock("fs", async (original) => {
  const real = await original<typeof import("fs")>();
  const existsSync = (file: string) => store.exists(file);
  return { ...real, existsSync, default: { ...real, existsSync } };
});

const CONFIG_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../config");
const APP = { appsettings: "/settings", environment: "test", port: 9837, name: "n" };

const settings = (values: Record<string, unknown>) => {
  store.values = { app: APP, ...values };
};

async function load(values: Record<string, unknown> = {}) {
  store.values = { app: APP, ...values };
  store.files = [];
  store.calls = [];
  vi.resetModules();
  return await import("../../config/index.js");
}

vi.spyOn(console, "log").mockImplementation(() => undefined);
const config = await load();

// `restoreMocks` takes the spies off after each test, so each test gets its own.
let log = vi.spyOn(console, "log");
let warn = vi.spyOn(console, "warn");
beforeEach(() => {
  log = vi.spyOn(console, "log").mockImplementation(() => undefined);
  warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
  settings({});
});

afterEach(() => {
  vi.unstubAllEnvs();
});

// The names config/index.ts reads from the environment, taken from its source: `process.env["X"]`,
// the first argument of the readers that read one by name, and the `AI__MCP__<i>__ENDPOINT`
// family. The roots of the keys it asks nconf for name variables too, as nconf copies the
// environment over the settings (`OTEL_SERVICE_NAME`, `openTelemetry:enable`, `core:cors`).
function environmentReadByConfig(): { names: string[]; families: RegExp[]; roots: string[] } {
  const source = readFileSync(path.join(CONFIG_DIR, "index.ts"), "utf8");
  const all = (pattern: RegExp) => [...source.matchAll(pattern)].map((m) => m[1] ?? "");
  const names = [
    ...all(/process\.env\["([^"]+)"\]/g),
    ...all(/(?:baseUrl|envOrSetting)\(\s*"([^"]+)"/g),
  ];
  const families = [...source.matchAll(/process\.env\[`([^`$]*)\$\{[^}]+\}([^`]*)`\]/g)].map(
    (m) => new RegExp(`^${m[1] ?? ""}\\d+${m[2] ?? ""}$`),
  );
  const roots = all(/nconf\.get\("([^"]+)"\)/g)
    .map((key) => key.split(":")[0] ?? key)
    .filter((root) => root !== "NODE_ENV");
  return { names: [...new Set(names)], families, roots: [...new Set(roots)] };
}

describe("the environment the tests start in (setup.ts)", () => {
  const read = environmentReadByConfig();

  it("finds the variables of config/index.ts in its source", () => {
    expect(read.names).toEqual(
      expect.arrayContaining([
        "core__cors",
        "API_HOST",
        "AI_SERVICE_URL",
        "AI__DOCUMENT_AGENT__URL",
        "AI__DOCUMENT_AGENT__TOKEN",
        "AI__COMPACTION__ENABLED",
        "AI_CHAT_UPSTREAM_TIMEOUT_MS",
        "OTEL_METRIC_EXPORT_INTERVAL",
      ]),
    );
    expect(read.families.map(String)).toEqual(["/^AI__MCP__\\d+__ENDPOINT$/"]);
    expect(read.roots).toEqual(
      expect.arrayContaining(["app", "ai", "core", "openTelemetry", "OTEL_SERVICE_NAME"]),
    );
  });

  it("has none of them set, whatever the shell exports", () => {
    const set = Object.keys(process.env).filter(
      (name) =>
        read.names.includes(name) ||
        read.families.some((family) => family.test(name)) ||
        // setup.ts sets this one itself, to a directory that does not exist.
        (name !== "app:appsettings" && read.roots.includes(name.split(":")[0] ?? name)),
    );

    expect(set).toEqual([]);
  });
});

describe("loading the configuration", () => {
  it("reads argv, the environment, config.json and the three appsettings files", async () => {
    await load({ NODE_ENV: undefined });

    expect(store.calls).toEqual(["argv", "env"]);
    expect(store.files).toEqual([
      ["config", path.join(CONFIG_DIR, "config.json")],
      ["appsettingsWithEnv", "/settings/appsettings.test.json"],
      ["appsettings", "/settings/appsettings.json"],
      ["appsettingsServices", "/settings/appsettings.services.json"],
    ]);
    expect(store.exists).not.toHaveBeenCalled();
    expect(log.mock.calls).toEqual([["NODE_ENV: undefined"], ["environment: test"]]);
  });

  it("resolves a relative appsettings directory against the config directory", async () => {
    await load({ app: { ...APP, appsettings: "../../x", environment: "Development" } });

    expect(store.files.slice(1)).toEqual([
      ["appsettingsWithEnv", path.join(CONFIG_DIR, "../../x", "appsettings.Development.json")],
      ["appsettings", path.join(CONFIG_DIR, "../../x", "appsettings.json")],
      ["appsettingsServices", path.join(CONFIG_DIR, "../../x", "appsettings.services.json")],
    ]);
  });

  it("adds the NODE_ENV file when it exists", async () => {
    store.exists.mockReturnValue(true);
    await load({ NODE_ENV: "production" });

    expect(store.exists).toHaveBeenCalledWith(path.join(CONFIG_DIR, "production.json"));
    expect(store.files.slice(0, 2)).toEqual([
      ["config", path.join(CONFIG_DIR, "config.json")],
      ["config", path.join(CONFIG_DIR, "production.json")],
    ]);
    expect(log).toHaveBeenCalledWith("NODE_ENV: production");
  });

  it("skips a NODE_ENV file that does not exist", async () => {
    store.exists.mockReturnValue(false);
    await load({ NODE_ENV: "test" });

    expect(store.exists).toHaveBeenCalledWith(path.join(CONFIG_DIR, "test.json"));
    expect(store.files).toHaveLength(4);
  });

  it.each(["development", ""])("does not look for a file for NODE_ENV %j", async (nodeEnv) => {
    await load({ NODE_ENV: nodeEnv });

    expect(store.exists).not.toHaveBeenCalled();
    expect(store.files).toHaveLength(4);
  });

  it("fails to load without the app section", async () => {
    store.values = {};
    vi.resetModules();

    await expect(import("../../config/index.js")).rejects.toThrow(TypeError);
  });
});

describe("the constants", () => {
  it("name the tool groups and the portal's MCP server", () => {
    expect([
      config.PORTAL_MCP_SERVER_NAME,
      config.DOCSPACE_INTEGRATION_SERVER_TYPE,
      config.DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
      config.WEB_SEARCH_TYPE,
      config.IMAGE_GENERATION_TYPE,
      config.DOCUMENT_AGENT_TYPE,
      config.FORM_ANALYSIS_TYPE,
    ]).toEqual([
      "docspace",
      "docspace-integration",
      "docspace-integration-approval",
      "web-search",
      "image-generation",
      "document-agent",
      "form-analysis",
    ]);
  });

  it("export nconf itself as the default", () => {
    settings({ logLevel: "warn" });

    expect(config.default.get("logLevel")).toBe("warn");
  });
});

describe("getAppConfig and getRootConfig", () => {
  it("answer the app section and the whole tree as nconf has them", () => {
    settings({ logLevel: "info" });

    expect(config.getAppConfig()).toBe(APP);
    expect(config.getRootConfig()).toEqual({ app: APP, logLevel: "info" });
  });

  it("answer undefined for an absent app section", () => {
    store.values = {};

    expect(config.getAppConfig()).toBeUndefined();
  });
});

describe.each([
  ["getProxyUrl", "API_HOST", "proxy", "http://localhost:8092"],
  ["getAiServiceUrl", "AI_SERVICE_URL", "aiService", "http://localhost:5157"],
] as const)("%s", (reader, envName, entry, fallback) => {
  const read = (): string => config[reader]();
  const withEntry = (url: unknown) => settings({ app: { ...APP, [entry]: { url } } });

  it.each([
    ["absent", undefined],
    ["empty", ""],
  ])("takes the settings entry when %s is the environment variable", (_case, value) => {
    vi.stubEnv(envName, value);
    withEntry("http://file.example/");

    expect(read()).toBe("http://file.example");
  });

  it.each([
    ["absent", undefined],
    ["empty", ""],
    ["not a string", 42],
  ])("falls back to the default when the settings entry is %s", (_case, url) => {
    vi.stubEnv(envName, undefined);
    withEntry(url);

    expect(read()).toBe(fallback);
  });

  it("falls back to the default without an app section entry", () => {
    vi.stubEnv(envName, "");
    settings({ app: undefined });

    expect(read()).toBe(fallback);
  });

  it.each([
    ["a value", "http://env.example", "http://env.example"],
    ["a trailing slash", "http://env.example/", "http://env.example"],
    ["several trailing slashes", "http://env.example/base//", "http://env.example/base"],
  ])("takes the environment variable with %s over the settings entry", (_case, value, url) => {
    vi.stubEnv(envName, value);
    withEntry("http://file.example");

    expect(read()).toBe(url);
  });
});

describe("chatContextDisabled", () => {
  it.each([
    ["absent", undefined, false],
    ["empty", "", false],
    ["off", "off", true],
    ["OFF", "OFF", false],
    ["false", "false", false],
    ["0", "0", false],
    ["on", "on", false],
  ])("is %s -> %s", (_case, value, disabled) => {
    vi.stubEnv("AI_CHAT_CONTEXT", value);

    expect(config.chatContextDisabled()).toBe(disabled);
  });
});

describe("mcpPortalBaseUrl", () => {
  it("is undefined when unset or empty", () => {
    expect(config.mcpPortalBaseUrl()).toBeUndefined();
    vi.stubEnv("AI__MCP_PORTAL_BASE_URL", "");
    expect(config.mcpPortalBaseUrl()).toBeUndefined();
  });

  it.each(["http://host.docker.internal:8092/", " "])("is %j as given", (value) => {
    vi.stubEnv("AI__MCP_PORTAL_BASE_URL", value);
    expect(config.mcpPortalBaseUrl()).toBe(value);
  });
});

describe("coreCors", () => {
  it.each(["", "*", "https://portal"])(
    "takes core__cors=%j from the environment over the file",
    (value) => {
      settings({ core: { cors: "https://file" } });
      vi.stubEnv("core__cors", value);

      expect(config.coreCors()).toBe(value);
    },
  );

  it("takes core:cors from the file without the environment variable", () => {
    settings({ core: { cors: "https://file" } });

    expect(config.coreCors()).toBe("https://file");
  });

  it.each([undefined, 42, ["https://a"], { a: 1 }, null])(
    "is undefined for a file value %j",
    (value) => {
      settings({ core: { cors: value } });

      expect(config.coreCors()).toBeUndefined();
    },
  );
});

describe("allowPrivateBaseUrl", () => {
  it("is off without the variable", () => {
    expect(config.allowPrivateBaseUrl()).toBe(false);
  });

  it.each([
    ["true", true],
    ["1", true],
    ["TRUE", false],
    ["True", false],
    [" true", false],
    ["yes", false],
    ["0", false],
    ["false", false],
    ["", false],
  ])("reads %j as %s", (value, expected) => {
    vi.stubEnv("AI__ALLOW_PRIVATE_BASE_URL", value);

    expect(config.allowPrivateBaseUrl()).toBe(expected);
  });
});

describe("getMaxImageSize", () => {
  const DEFAULT = 20 * 1024 * 1024;

  it.each([
    ["absent", undefined, DEFAULT],
    ["null", null, DEFAULT],
    ["empty", "", DEFAULT],
    ["non-numeric", "20mb", DEFAULT],
    ["zero", 0, DEFAULT],
    ["negative", -1, DEFAULT],
    ["infinite", Infinity, DEFAULT],
    ["an object", { a: 1 }, DEFAULT],
    ["a number", 5242880, 5242880],
    ["a numeric string", "1048576", 1048576],
    ["true", true, DEFAULT],
    ["a fraction of a byte", 0.5, DEFAULT],
    ["a fraction", 1048576.5, DEFAULT],
    ["one byte", 1, 1],
    ["the upper bound", 1024 ** 3, 1024 ** 3],
    ["over the upper bound", 1024 ** 3 + 1, DEFAULT],
    ["far too large", 1e15, DEFAULT],
  ])("is %s -> as configured or the default", (_what, value, expected) => {
    settings({ ai: { maxImageSize: value } });

    expect(config.getMaxImageSize()).toBe(expected);
  });
});

describe("getMcpServers", () => {
  it("is empty without ai or ai.mcp, and says nothing", () => {
    expect(config.getMcpServers()).toEqual([]);
    settings({ ai: {} });
    expect(config.getMcpServers()).toEqual([]);
    settings({ ai: { mcp: null } });
    expect(config.getMcpServers()).toEqual([]);
    expect(warn).not.toHaveBeenCalled();
  });

  it.each([{ 0: { id: "a" } }, "x", 1])("ignores a non-array ai.mcp (%j) with a warning", (mcp) => {
    settings({ ai: { mcp } });

    expect(config.getMcpServers()).toEqual([]);
    expect(warn).toHaveBeenCalledExactlyOnceWith(
      "ai.mcp is not an array — ignoring (a ':'-keyed env var may have nested over it; use AI__MCP__<i>__ENDPOINT)",
    );
  });

  it("keeps the id, name and endpoint of each valid entry", () => {
    settings({
      ai: {
        mcp: [
          { id: "1", name: "docspace", endpoint: "http://mcp:8000/mcp", headers: { a: "b" } },
          { id: "2", name: "other", endpoint: "http://other/mcp" },
        ],
      },
    });

    expect(config.getMcpServers()).toEqual([
      { id: "1", name: "docspace", endpoint: "http://mcp:8000/mcp" },
      { id: "2", name: "other", endpoint: "http://other/mcp" },
    ]);
  });

  it.each([
    ["a null entry", null],
    ["a string entry", "x"],
    ["no id", { name: "n", endpoint: "e" }],
    ["a numeric id", { id: 1, name: "n", endpoint: "e" }],
    ["no name", { id: "1", endpoint: "e" }],
    ["an empty name", { id: "1", name: "", endpoint: "e" }],
    ["a numeric name", { id: "1", name: 2, endpoint: "e" }],
    ["no endpoint", { id: "1", name: "n" }],
    ["an empty endpoint", { id: "1", name: "n", endpoint: "" }],
    ["a numeric endpoint", { id: "1", name: "n", endpoint: 5 }],
  ])("drops an entry with %s", (_what, entry) => {
    settings({ ai: { mcp: [entry, { id: "ok", name: "ok", endpoint: "e" }] } });

    expect(config.getMcpServers()).toEqual([{ id: "ok", name: "ok", endpoint: "e" }]);
  });

  it("accepts an empty id", () => {
    settings({ ai: { mcp: [{ id: "", name: "n", endpoint: "e" }] } });

    expect(config.getMcpServers()).toEqual([{ id: "", name: "n", endpoint: "e" }]);
  });

  it("applies the endpoint override by the entry's index in the file", () => {
    settings({
      ai: {
        mcp: [
          { id: "bad" },
          { id: "1", name: "docspace", endpoint: "http://stale/mcp" },
          { id: "2", name: "none" },
        ],
      },
    });
    vi.stubEnv("AI__MCP__0__ENDPOINT", "http://ignored/mcp");
    vi.stubEnv("AI__MCP__1__ENDPOINT", "http://onlyoffice-docspace-mcp:8000/mcp");
    vi.stubEnv("AI__MCP__2__ENDPOINT", "http://added/mcp");

    expect(config.getMcpServers()).toEqual([
      { id: "1", name: "docspace", endpoint: "http://onlyoffice-docspace-mcp:8000/mcp" },
      { id: "2", name: "none", endpoint: "http://added/mcp" },
    ]);
  });

  it("drops an entry whose override is set but empty", () => {
    settings({ ai: { mcp: [{ id: "1", name: "n", endpoint: "http://file/mcp" }] } });
    vi.stubEnv("AI__MCP__0__ENDPOINT", "");

    expect(config.getMcpServers()).toEqual([]);
  });
});

describe("getDocumentAgent beyond the URL and the token", () => {
  const AGENT = { url: "http://agent:9838", token: "secret" };
  const ENV_NAMES = [
    "AI__DOCUMENT_AGENT__URL",
    "AI__DOCUMENT_AGENT__TOKEN",
    "AI__DOCUMENT_AGENT__TASK_TIMEOUT_SECONDS",
    "AI__DOCUMENT_AGENT__MAX_TOOL_CALLS",
    "AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS",
    "AI__DOCUMENT_AGENT__PORTAL_URL",
    "AI__DOCUMENT_AGENT__FILES_URL",
  ];
  beforeEach(() => {
    for (const name of ENV_NAMES) {
      vi.stubEnv(name, undefined);
    }
  });

  it("fills the defaults", () => {
    settings({ ai: { documentAgent: AGENT } });

    expect(config.getDocumentAgent()).toEqual({
      url: "http://agent:9838",
      token: "secret",
      taskTimeoutSeconds: 1800,
      maxToolCalls: undefined,
      shareSystemServers: false,
      portalUrl: undefined,
      filesUrl: "http://localhost:9837",
    });
  });

  it("is off for a non-object documentAgent", () => {
    settings({ ai: { documentAgent: "http://agent" } });

    expect(config.getDocumentAgent()).toBeUndefined();
  });

  it("trims the URL and the token and strips trailing slashes of the URLs", () => {
    settings({
      ai: {
        documentAgent: {
          url: " http://agent:9838/// ",
          token: " secret ",
          portalUrl: "http://portal//",
          filesUrl: "http://files/",
        },
      },
    });

    expect(config.getDocumentAgent()).toMatchObject({
      url: "http://agent:9838",
      token: "secret",
      portalUrl: "http://portal",
      filesUrl: "http://files",
    });
  });

  it.each([
    ["absent", undefined, undefined, 1800],
    ["from the file", undefined, 120, 120],
    ["a numeric string in the file", undefined, "300", 300],
    ["from the environment over the file", "60", 120, 60],
    ["an empty environment value", "", 120, 120],
    ["zero", "0", 0, 1800],
    ["negative", "-5", -5, 1800],
    ["a fraction", "1.5", 2.5, 1800],
    ["non-numeric", "30s", "a", 1800],
  ])("takes the task timeout %s", (_what, env, file, expected) => {
    settings({ ai: { documentAgent: { ...AGENT, taskTimeoutSeconds: file } } });
    if (env !== undefined) {
      vi.stubEnv("AI__DOCUMENT_AGENT__TASK_TIMEOUT_SECONDS", env);
    }

    expect(config.getDocumentAgent()?.taskTimeoutSeconds).toBe(expected);
  });

  it.each([
    ["absent", undefined, undefined, undefined],
    ["from the file", undefined, 25, 25],
    ["from the environment over the file", "10", 25, 10],
    ["zero", "0", 0, undefined],
    ["non-numeric in the environment", "x", 7, 7],
  ])("takes maxToolCalls %s", (_what, env, file, expected) => {
    settings({ ai: { documentAgent: { ...AGENT, maxToolCalls: file } } });
    if (env !== undefined) {
      vi.stubEnv("AI__DOCUMENT_AGENT__MAX_TOOL_CALLS", env);
    }

    expect(config.getDocumentAgent()?.maxToolCalls).toBe(expected);
  });

  it.each([
    [86_400, 86_400],
    [86_401, 1800],
    [1e12, 1800],
  ])("bounds the task timeout at a day: %s -> %s", (value, expected) => {
    settings({ ai: { documentAgent: { ...AGENT, taskTimeoutSeconds: value } } });

    expect(config.getDocumentAgent()?.taskTimeoutSeconds).toBe(expected);
  });

  it.each([
    [10_000, 10_000],
    [10_001, undefined],
  ])("bounds the tool calls of a task: %s -> %s", (value, expected) => {
    settings({ ai: { documentAgent: { ...AGENT, maxToolCalls: value } } });

    expect(config.getDocumentAgent()?.maxToolCalls).toBe(expected);
  });

  it.each([
    ["true", undefined, true],
    ["1", undefined, true],
    ["TRUE", true, false],
    ["false", true, false],
    ["yes", true, false],
    ["", true, true],
    [undefined, true, true],
    [undefined, "true", false],
    [undefined, undefined, false],
  ])("reads share-system-servers %j over the file's %j as %s", (env, file, expected) => {
    settings({ ai: { documentAgent: { ...AGENT, shareSystemServers: file } } });
    if (env !== undefined) {
      vi.stubEnv("AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS", env);
    }

    expect(config.getDocumentAgent()?.shareSystemServers).toBe(expected);
  });

  it("takes the portal and files URLs from the environment over the file, trimmed", () => {
    settings({ ai: { documentAgent: { ...AGENT, portalUrl: "http://p", filesUrl: "http://f" } } });
    vi.stubEnv("AI__DOCUMENT_AGENT__PORTAL_URL", " http://env-portal/ ");
    vi.stubEnv("AI__DOCUMENT_AGENT__FILES_URL", "http://env-files:1/");

    expect(config.getDocumentAgent()).toMatchObject({
      portalUrl: "http://env-portal",
      filesUrl: "http://env-files:1",
    });
  });
});

describe("getTelemetrySettings", () => {
  const OFF = {
    enabled: false,
    otlpEndpoint: undefined,
    otlpProtocol: "http/protobuf",
    serviceName: "ASC.AI.Chat",
  };

  it("is off with the defaults when nothing is configured", () => {
    expect(config.getTelemetrySettings()).toEqual(OFF);
  });

  it("takes every value as configured", () => {
    settings({
      openTelemetry: { enable: true, ServiceName: "DocSpace.AI.Chat" },
      OTEL_EXPORTER_OTLP_ENDPOINT: "http://collector:4317",
      OTEL_EXPORTER_OTLP_PROTOCOL: " grpc ",
      OTEL_SERVICE_NAME: "FromEnv",
    });

    expect(config.getTelemetrySettings()).toEqual({
      enabled: true,
      otlpEndpoint: "http://collector:4317",
      otlpProtocol: "grpc",
      serviceName: "DocSpace.AI.Chat",
    });
  });

  it.each([
    ["true", true, true],
    ['"true"', "true", true],
    ['"TRUE"', "TRUE", false],
    ["1", 1, false],
    ['"1"', "1", false],
    ["false", false, false],
    ["an object", { a: 1 }, false],
    ["null", null, false],
  ])("reads openTelemetry:enable %s as %s", (_what, enable, expected) => {
    settings({ openTelemetry: { enable } });

    expect(config.getTelemetrySettings().enabled).toBe(expected);
  });

  it.each([
    ["absent", undefined, undefined],
    ["empty", "", undefined],
    ["a number", 4318, undefined],
    ["an object", { url: "x" }, undefined],
    ["a URL", "http://c:4318/", "http://c:4318/"],
  ])("reads an endpoint that is %s", (_what, endpoint, expected) => {
    settings({ OTEL_EXPORTER_OTLP_ENDPOINT: endpoint });

    expect(config.getTelemetrySettings().otlpEndpoint).toBe(expected);
  });

  it.each([
    ["absent", undefined, "http/protobuf"],
    ["empty", "", "http/protobuf"],
    ["blank", "   ", "http/protobuf"],
    ["a number", 1, "http/protobuf"],
    ["an array", ["grpc"], "http/protobuf"],
    ["padded", " http/json ", "http/json"],
  ])("reads a protocol that is %s", (_what, protocol, expected) => {
    settings({ OTEL_EXPORTER_OTLP_PROTOCOL: protocol });

    expect(config.getTelemetrySettings().otlpProtocol).toBe(expected);
  });

  it.each([
    ["the configured name", "Configured", "FromEnv", "Configured"],
    ["OTEL_SERVICE_NAME for an empty configured name", "", "FromEnv", "FromEnv"],
    ["OTEL_SERVICE_NAME for a non-string configured name", 5, "FromEnv", "FromEnv"],
    ["the default for an empty OTEL_SERVICE_NAME", undefined, "", "ASC.AI.Chat"],
    ["the default for a non-string OTEL_SERVICE_NAME", undefined, { a: 1 }, "ASC.AI.Chat"],
  ])("names the service with %s", (_what, configured, env, expected) => {
    settings({ openTelemetry: { ServiceName: configured }, OTEL_SERVICE_NAME: env });

    expect(config.getTelemetrySettings().serviceName).toBe(expected);
  });

  it("is off for an openTelemetry section that is not an object", () => {
    settings({ openTelemetry: "on" });

    expect(config.getTelemetrySettings()).toEqual(OFF);
  });
});

describe("getCompaction beyond the existing table", () => {
  const ENV_NAMES = [
    "AI__COMPACTION__ENABLED",
    "AI__COMPACTION__THRESHOLD",
    "AI__COMPACTION__KEEP_RECENT",
    "AI__COMPACTION__DEFAULT_CONTEXT_WINDOW",
  ];
  beforeEach(() => {
    for (const name of ENV_NAMES) {
      vi.stubEnv(name, undefined);
    }
  });

  it("ignores a compaction section that is not an object", () => {
    settings({ ai: { compaction: "off" } });

    expect(config.getCompaction()).toEqual({ enabled: true });
  });

  it("reads a flag trimmed and in any case", () => {
    vi.stubEnv("AI__COMPACTION__ENABLED", " False ");

    expect(config.getCompaction()).toEqual({ enabled: false });
  });

  it("stays on for the string false in the file", () => {
    settings({ ai: { compaction: { enabled: "false" } } });

    expect(config.getCompaction()).toEqual({ enabled: true });
  });

  it.each([
    ["1", 1],
    ["0.5", 0.5],
    ["0", undefined],
    ["-0.5", undefined],
    ["1.0001", undefined],
    ["abc", undefined],
    ["", undefined],
  ])("reads the threshold %j", (env, expected) => {
    vi.stubEnv("AI__COMPACTION__THRESHOLD", env);

    expect(config.getCompaction().threshold).toBe(expected);
  });

  it.each([
    ["0", 0],
    ["12", 12],
    ["-1", undefined],
    ["2.5", undefined],
    ["x", undefined],
    ["", undefined],
    ["  ", undefined],
  ])("reads keepRecent %j", (env, expected) => {
    vi.stubEnv("AI__COMPACTION__KEEP_RECENT", env);

    expect(config.getCompaction().keepRecent).toBe(expected);
  });

  it.each([
    ["0", undefined],
    ["64000", 64000],
    ["100000000", 100_000_000],
    ["100000001", undefined],
    ["1e12", undefined],
    ["-1", undefined],
  ])("reads the default context window %j", (env, expected) => {
    vi.stubEnv("AI__COMPACTION__DEFAULT_CONTEXT_WINDOW", env);

    expect(config.getCompaction().defaultContextWindow).toBe(expected);
  });

  it.each([
    ["a numeric string threshold", { threshold: "0.25" }, { threshold: 0.25 }],
    ["a string keepRecent", { keepRecent: "3" }, { keepRecent: 3 }],
    ["an object knob", { keepRecent: { n: 1 } }, {}],
    ["a boolean knob", { threshold: true }, {}],
  ])("reads %s from the file", (_what, compaction, expected) => {
    settings({ ai: { compaction } });

    expect(config.getCompaction()).toEqual({ enabled: true, ...expected });
  });

  it("reads the file's knobs under environment variables exported empty", () => {
    settings({ ai: { compaction: { threshold: 0.5, keepRecent: 4 } } });
    vi.stubEnv("AI__COMPACTION__THRESHOLD", "");
    vi.stubEnv("AI__COMPACTION__KEEP_RECENT", "");

    expect(config.getCompaction()).toEqual({ enabled: true, threshold: 0.5, keepRecent: 4 });
  });
});
