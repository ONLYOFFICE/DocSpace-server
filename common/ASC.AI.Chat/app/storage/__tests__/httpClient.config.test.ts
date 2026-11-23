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

import { afterEach, describe, expect, it, vi } from "vitest";

// What httpClient.js reads when it is loaded: the two base URLs and the timeout. Each test
// loads a fresh copy of the module under its own environment and settings.
type HttpClient = typeof import("../httpClient.js");

async function load(env: Record<string, string>, app?: unknown): Promise<HttpClient> {
  vi.resetModules();
  for (const [name, value] of Object.entries(env)) {
    vi.stubEnv(name, value);
  }
  // The settings file is replaced; the readers of the environment stay the real ones.
  vi.doMock("../../../config/index.js", async (importOriginal) => ({
    ...(await importOriginal<typeof import("../../../config/index.js")>()),
    default: { get: (key: string) => (key === "app" ? app : undefined) },
  }));
  return import("../httpClient.js");
}

afterEach(() => {
  vi.unstubAllEnvs();
  vi.doUnmock("../../../config/index.js");
  vi.useRealTimers();
});

describe("the base URLs", () => {
  it("are taken from API_HOST and AI_SERVICE_URL, without trailing slashes", async () => {
    const client = await load(
      { API_HOST: "http://portal.example//", AI_SERVICE_URL: "http://ai.example/" },
      { proxy: { url: "http://cfg-portal" }, aiService: { url: "http://cfg-ai" } },
    );

    expect(client.proxyBaseUrl).toBe("http://portal.example");
    expect(client.aiServiceBaseUrl).toBe("http://ai.example");
  });

  it("are taken from app.proxy.url and app.aiService.url when the environment names none", async () => {
    const client = await load(
      { API_HOST: "", AI_SERVICE_URL: "" },
      { proxy: { url: "http://cfg-portal/" }, aiService: { url: "http://cfg-ai///" } },
    );

    expect(client.proxyBaseUrl).toBe("http://cfg-portal");
    expect(client.aiServiceBaseUrl).toBe("http://cfg-ai");
  });

  it.each([
    ["no app section", undefined],
    ["an app section without the two entries", {}],
    ["empty URLs", { proxy: { url: "" }, aiService: { url: "" } }],
  ])("fall back to localhost with %s", async (_case, app) => {
    const client = await load({ API_HOST: "", AI_SERVICE_URL: "" }, app);

    expect(client.proxyBaseUrl).toBe("http://localhost:8092");
    expect(client.aiServiceBaseUrl).toBe("http://localhost:5157");
  });
});

describe("AI_CHAT_UPSTREAM_TIMEOUT_MS", () => {
  const timeoutOf = async (value: string | undefined): Promise<string> => {
    const client = await load(
      value === undefined ? {} : { AI_CHAT_UPSTREAM_TIMEOUT_MS: value },
      undefined,
    );
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    const { signal, cancel } = client.withTimeout(undefined);
    vi.advanceTimersByTime(10_000_000);
    cancel();
    return String(signal.reason);
  };

  it.each([
    ["absent", undefined, "Error: upstream timeout after 300000ms"],
    ["empty", "", "Error: upstream timeout after 300000ms"],
    ["not a number", "soon", "Error: upstream timeout after 300000ms"],
    ["a number", "45000", "Error: upstream timeout after 45000ms"],
    ["the lower bound", "1000", "Error: upstream timeout after 1000ms"],
    ["the upper bound", "3600000", "Error: upstream timeout after 3600000ms"],
  ])("when %s sets the default timeout", async (_case, value, reason) => {
    expect(await timeoutOf(value)).toBe(reason);
  });

  it.each([
    ["a value with a unit", "30s"],
    ["zero", "0"],
    ["a negative value", "-5"],
    ["under a second", "999"],
    ["a fraction", "1500.5"],
    ["over an hour", "3600001"],
    ["a value too large for a timer", "99999999999"],
  ])("when %s keeps the default timeout", async (_case, value) => {
    expect(await timeoutOf(value)).toBe("Error: upstream timeout after 300000ms");
  });

  it("does not abort a call at once for a value a timer cannot hold or for zero", async () => {
    for (const value of ["0", "99999999999"]) {
      const client = await load({ AI_CHAT_UPSTREAM_TIMEOUT_MS: value }, undefined);
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
      const { signal, cancel } = client.withTimeout(undefined);
      vi.advanceTimersByTime(299_999);
      expect(signal.aborted).toBe(false);
      cancel();
      vi.useRealTimers();
    }
  });
});
