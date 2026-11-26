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
import os from "os";
import path from "path";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// The config module reads appsettings through nconf at import time; a fake
// store keeps the test off the disk and lets each case name its settings.
const store = vi.hoisted(() => ({
  values: {
    app: { appsettings: "/nowhere", environment: "test" },
  } as Record<string, unknown>,
}));

vi.mock("nconf", () => {
  const nconf = {
    argv: () => nconf,
    env: () => nconf,
    file: () => nconf,
    get: (key: string) => store.values[key],
  };
  return { default: nconf };
});

const { getPluginAgent } = await import("../../config/index.js");

const PREFIX = "AI__PLUGIN_AGENT__";
const FIELDS = [
  "SANDBOX_URL",
  "SANDBOX_TOKEN",
  "TASK_TIMEOUT_SECONDS",
  "MAX_TOOL_CALLS",
  "MAX_CONCURRENT_TASKS",
  "SANDBOX_TTL_SECONDS",
  "SERVICE_URL",
  "REGISTRY_URL",
  "CACHE_DIR",
  "CACHE_MAX_MB",
];
const MIB = 1024 * 1024;
const ON = { sandboxUrl: "http://gateway:9090/", sandboxToken: "secret" };

const withSettings = (pluginAgent: unknown) => {
  store.values = {
    app: { appsettings: "/nowhere", environment: "test", port: 9837 },
    ai: pluginAgent === undefined ? {} : { pluginAgent },
  };
};

const clearEnv = () => {
  for (const field of FIELDS) {
    delete process.env[PREFIX + field];
  }
};

describe("getPluginAgent", () => {
  beforeEach(() => {
    clearEnv();
    vi.spyOn(console, "warn").mockImplementation(() => undefined);
  });
  afterEach(clearEnv);

  it("is off without the sandbox's URL and token", () => {
    for (const setting of [
      undefined,
      "text",
      {},
      { sandboxUrl: "http://gateway:9090" },
      { sandboxToken: "secret" },
      { sandboxUrl: " ", sandboxToken: "secret" },
      { sandboxUrl: 5, sandboxToken: "secret" },
    ]) {
      withSettings(setting);
      expect(getPluginAgent()).toBeUndefined();
    }
  });

  it("answers the defaults of the code when only the sandbox is named", () => {
    withSettings(ON);
    expect(getPluginAgent()).toEqual({
      sandboxUrl: "http://gateway:9090",
      sandboxToken: "secret",
      taskTimeoutSeconds: 1800,
      maxToolCalls: 150,
      maxConcurrentTasks: 2,
      sandboxTtlSeconds: 600,
      serviceUrl: "http://localhost:9837",
      registryUrl: "https://registry.npmjs.org",
      cacheDir: path.join(os.tmpdir(), "asc-ai-chat", "npm-cache"),
      cacheMaxBytes: 2048 * MIB,
    });
  });

  it("is switched off by an environment variable that is set but empty", () => {
    withSettings(ON);
    process.env[PREFIX + "SANDBOX_URL"] = "";
    expect(getPluginAgent()).toBeUndefined();

    delete process.env[PREFIX + "SANDBOX_URL"];
    process.env[PREFIX + "SANDBOX_TOKEN"] = " ";
    expect(getPluginAgent()).toBeUndefined();
  });

  it("prefers the environment to the appsettings", () => {
    withSettings({
      ...ON,
      taskTimeoutSeconds: 100,
      serviceUrl: "http://chat:1",
      registryUrl: "http://mirror:2",
      cacheDir: "/from/settings",
    });
    process.env[PREFIX + "SANDBOX_URL"] = "http://other:1//";
    process.env[PREFIX + "SANDBOX_TOKEN"] = "other";
    process.env[PREFIX + "TASK_TIMEOUT_SECONDS"] = "200";
    process.env[PREFIX + "SERVICE_URL"] = "http://chat-env:3/";
    process.env[PREFIX + "REGISTRY_URL"] = "http://mirror-env:4/";
    process.env[PREFIX + "CACHE_DIR"] = "/from/env";
    expect(getPluginAgent()).toMatchObject({
      sandboxUrl: "http://other:1",
      sandboxToken: "other",
      taskTimeoutSeconds: 200,
      serviceUrl: "http://chat-env:3",
      registryUrl: "http://mirror-env:4",
      cacheDir: "/from/env",
    });
  });

  it("takes the addresses and the directory from the appsettings, without trailing slashes", () => {
    withSettings({
      ...ON,
      serviceUrl: " http://chat:1/ ",
      registryUrl: "http://mirror:2//",
      cacheDir: "/var/cache/npm",
    });
    expect(getPluginAgent()).toMatchObject({
      serviceUrl: "http://chat:1",
      registryUrl: "http://mirror:2",
      cacheDir: "/var/cache/npm",
    });
  });

  // Each number: its name in the appsettings and in the environment, both bounds and the default.
  const NUMBERS = [
    ["taskTimeoutSeconds", "TASK_TIMEOUT_SECONDS", 60, 7200, 1800],
    ["maxToolCalls", "MAX_TOOL_CALLS", 1, 10_000, 150],
    ["maxConcurrentTasks", "MAX_CONCURRENT_TASKS", 1, 16, 2],
    ["sandboxTtlSeconds", "SANDBOX_TTL_SECONDS", 60, 3600, 600],
  ] as const;

  describe.each(NUMBERS)("%s", (key, field, min, max, fallback) => {
    it("takes both bounds, from the appsettings and from the environment", () => {
      for (const value of [min, max]) {
        withSettings({ ...ON, [key]: value });
        expect(getPluginAgent()?.[key]).toBe(value);

        withSettings(ON);
        process.env[PREFIX + field] = String(value);
        expect(getPluginAgent()?.[key]).toBe(value);
        delete process.env[PREFIX + field];
      }
    });

    it.each([
      ["below the minimum", min - 1],
      ["above the maximum", max + 1],
      ["zero", 0],
      ["negative", -5],
      ["a fraction", min + 0.5],
      ["not a number", "soon"],
      ["empty", ""],
      ["null", null],
      ["an object", {}],
    ])("falls back to the default when the setting is %s", (_name, value) => {
      withSettings({ ...ON, [key]: value });
      expect(getPluginAgent()?.[key]).toBe(fallback);
    });

    it("falls back to the appsettings when the environment value is out of range", () => {
      withSettings({ ...ON, [key]: max });
      process.env[PREFIX + field] = String(max + 1);
      expect(getPluginAgent()?.[key]).toBe(max);

      process.env[PREFIX + field] = "soon";
      expect(getPluginAgent()?.[key]).toBe(max);
    });

    it("says once which value was refused and what is used instead", () => {
      const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
      withSettings({ ...ON, [key]: max + 7 });
      getPluginAgent();
      getPluginAgent();
      const lines = warn.mock.calls.map((call) => String(call[0]));
      expect(lines.filter((line) => line.includes(`ai:pluginAgent:${key}`))).toEqual([
        `config: ai:pluginAgent:${key} is ${max + 7}, not a whole number from ${min} to ${max}: ignored, ${fallback} is used`,
      ]);
    });
  });

  it("reads the cache size in MiB within 64 and 65536", () => {
    for (const [value, expected] of [
      [64, 64],
      [65536, 65536],
      [63, 2048],
      [65537, 2048],
      [0, 2048],
      [-1, 2048],
      ["many", 2048],
    ] as const) {
      withSettings({ ...ON, cacheMaxMb: value });
      expect(getPluginAgent()?.cacheMaxBytes).toBe(expected * MIB);
    }
    withSettings(ON);
    process.env[PREFIX + "CACHE_MAX_MB"] = "128";
    expect(getPluginAgent()?.cacheMaxBytes).toBe(128 * MIB);
  });
});
