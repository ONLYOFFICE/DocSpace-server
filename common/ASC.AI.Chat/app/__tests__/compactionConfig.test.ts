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

const { getCompaction } = await import("../../config/index.js");

const ENV = [
  "AI__COMPACTION__ENABLED",
  "AI__COMPACTION__THRESHOLD",
  "AI__COMPACTION__KEEP_RECENT",
  "AI__COMPACTION__DEFAULT_CONTEXT_WINDOW",
];

const withSettings = (compaction: Record<string, unknown> | undefined) => {
  store.values = {
    app: { appsettings: "/nowhere", environment: "test" },
    ai: compaction ? { compaction } : {},
  };
};

afterEach(() => {
  for (const name of ENV) {
    delete process.env[name];
  }
  withSettings(undefined);
});

describe("getCompaction", () => {
  it("is on with nothing configured, leaving every knob to the library", () => {
    expect(getCompaction()).toEqual({ enabled: true });
  });

  it("is switched off from the file or the environment", () => {
    withSettings({ enabled: false });
    expect(getCompaction()).toEqual({ enabled: false });
    withSettings(undefined);
    process.env["AI__COMPACTION__ENABLED"] = "FALSE";
    expect(getCompaction()).toEqual({ enabled: false });
    process.env["AI__COMPACTION__ENABLED"] = "0";
    expect(getCompaction()).toEqual({ enabled: false });
    // Not a flag: the file (here nothing, so the default) decides.
    process.env["AI__COMPACTION__ENABLED"] = "maybe";
    expect(getCompaction()).toEqual({ enabled: true });
  });

  it("reads the appsettings section", () => {
    withSettings({ enabled: true, threshold: 0.7, keepRecent: 6, defaultContextWindow: 32000 });
    expect(getCompaction()).toEqual({
      enabled: true,
      threshold: 0.7,
      keepRecent: 6,
      defaultContextWindow: 32000,
    });
  });

  it("lets the environment win and drops a malformed knob", () => {
    withSettings({ enabled: false, threshold: 0.7 });
    process.env["AI__COMPACTION__ENABLED"] = "1";
    process.env["AI__COMPACTION__THRESHOLD"] = "1.5";
    process.env["AI__COMPACTION__KEEP_RECENT"] = "0";
    // A malformed environment knob falls back to the file's value.
    expect(getCompaction()).toEqual({ enabled: true, threshold: 0.7, keepRecent: 0 });
  });
});
