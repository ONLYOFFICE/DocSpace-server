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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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

const { getChatSandbox } = await import("../../config/index.js");

const ENV = ["AI__SANDBOX__URL", "AI__SANDBOX__TOKEN", "AI__SANDBOX__IDLE_TTL_SECONDS"];
const ON = { url: "http://gateway:9090/", token: "secret" };

const withSettings = (sandbox: unknown) => {
  store.values = {
    app: { appsettings: "/nowhere", environment: "test" },
    ai: sandbox === undefined ? {} : { sandbox },
  };
};

describe("getChatSandbox", () => {
  beforeEach(() => {
    for (const name of ENV) {
      delete process.env[name];
    }
    vi.spyOn(console, "warn").mockImplementation(() => undefined);
  });

  afterEach(() => {
    for (const name of ENV) {
      delete process.env[name];
    }
  });

  it.each([
    ["no block", undefined],
    ["a block that is not an object", "on"],
    ["no token", { url: ON.url }],
    ["no address", { token: ON.token }],
    ["a blank address", { ...ON, url: "  " }],
    ["an address that is not text", { ...ON, url: 5 }],
  ])("is off with %s", (_name, setting) => {
    withSettings(setting);
    expect(getChatSandbox()).toBeUndefined();
  });

  it("is off without any `ai` section", () => {
    store.values = { app: { appsettings: "/nowhere", environment: "test" } };
    expect(getChatSandbox()).toBeUndefined();
  });

  it("reads the gateway without its trailing slashes, with the default idle lifetime", () => {
    withSettings(ON);
    expect(getChatSandbox()).toEqual({
      sandboxUrl: "http://gateway:9090",
      sandboxToken: "secret",
      idleTtlSeconds: 900,
    });
  });

  it("is switched off by an empty environment variable, whatever the settings name", () => {
    withSettings(ON);
    process.env["AI__SANDBOX__TOKEN"] = "";
    expect(getChatSandbox()).toBeUndefined();
  });

  it("takes the gateway from the environment alone", () => {
    withSettings(undefined);
    process.env["AI__SANDBOX__URL"] = "http://other:1";
    process.env["AI__SANDBOX__TOKEN"] = "t";
    expect(getChatSandbox()).toMatchObject({ sandboxUrl: "http://other:1", sandboxToken: "t" });
  });

  it.each([
    [660, 660],
    [3000, 3000],
    ["1200", 1200],
  ])("takes an idle lifetime of %j seconds from the settings", (value, expected) => {
    withSettings({ ...ON, idleTtlSeconds: value });
    expect(getChatSandbox()?.idleTtlSeconds).toBe(expected);
  });

  it.each([[""], ["abc"], [0], [-5], [600], [659], [3001], [1.5], [null]])(
    "falls back to 900 seconds for an idle lifetime of %j",
    (value) => {
      withSettings({ ...ON, idleTtlSeconds: value });
      expect(getChatSandbox()?.idleTtlSeconds).toBe(900);
    },
  );

  it("lets the environment override the settings, and a value out of range or empty fall back to them", () => {
    withSettings({ ...ON, idleTtlSeconds: 1800 });
    process.env["AI__SANDBOX__IDLE_TTL_SECONDS"] = "1200";
    expect(getChatSandbox()?.idleTtlSeconds).toBe(1200);
    process.env["AI__SANDBOX__IDLE_TTL_SECONDS"] = "659";
    expect(getChatSandbox()?.idleTtlSeconds).toBe(1800);
    process.env["AI__SANDBOX__IDLE_TTL_SECONDS"] = "";
    expect(getChatSandbox()?.idleTtlSeconds).toBe(1800);
  });

  it.each([
    ["a block", "yes", "config: ai:sandbox is yes, not an object"],
    ["an address", { ...ON, url: 9090 }, "config: ai:sandbox:url is 9090, not a text"],
    // The token is not shown: the line names its type.
    ["a token", { ...ON, token: 424242 }, "config: ai:sandbox:token is a number, not a text"],
  ])("says that %s of the wrong type is not taken", (_name, setting, line) => {
    const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
    withSettings(setting);

    expect(getChatSandbox()).toBeUndefined();
    expect(warn.mock.calls).toEqual([
      [`${line}: ignored, the chat's sandbox is not configured by it`],
    ]);
  });

  it("says nothing about an address that is blank or absent", () => {
    const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
    withSettings({ ...ON, url: "  " });
    getChatSandbox();
    withSettings({ token: ON.token });
    getChatSandbox();

    expect(warn).not.toHaveBeenCalled();
  });

  it("says once that a value that was given is not taken", () => {
    const warn = vi.spyOn(console, "warn").mockImplementation(() => undefined);
    withSettings({ ...ON, idleTtlSeconds: 7 });
    getChatSandbox();
    getChatSandbox();
    expect(warn).toHaveBeenCalledTimes(1);
    expect(warn).toHaveBeenCalledWith(
      "config: ai:sandbox:idleTtlSeconds is 7, not a whole number from 660 to 3000: ignored, 900 is used",
    );
  });
});
