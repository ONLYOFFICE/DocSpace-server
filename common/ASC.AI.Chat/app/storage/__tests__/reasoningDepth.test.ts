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

import { describe, expect, it } from "vitest";
import {
  DEFAULT_REASONING_LEVEL,
  depthsToLevels,
  depthToLevel,
  FULL_REASONING_SUPPORT,
  isReasoningLevel,
  levelToDepth,
  NO_REASONING_SUPPORT,
  parseCsharpReasoningDepth,
  REASONING_DEPTH_LEVELS,
  REASONING_LEVELS,
  reasoningConfigToSupport,
  supportToReasoningConfig,
} from "../reasoningDepth.js";

// The C# side: the `ReasoningDepth` enum and the `ReasoningConfig` class in
// common/ASC.AI.Integration/Profiles/ReasoningConfig.cs.

describe("the constants", () => {
  it("lists the levels and depths lowest first and defaults to medium", () => {
    expect(REASONING_LEVELS).toEqual(["off", "low", "medium", "high", "max"]);
    expect(REASONING_DEPTH_LEVELS).toEqual(["low", "medium", "high", "max"]);
    expect(DEFAULT_REASONING_LEVEL).toBe("medium");
    expect(FULL_REASONING_SUPPORT).toEqual({
      thinks: true,
      canDisable: true,
      depths: ["low", "medium", "high", "max"],
    });
    expect(NO_REASONING_SUPPORT).toEqual({ thinks: false, canDisable: false, depths: [] });
  });
});

describe("isReasoningLevel", () => {
  it.each([
    ["off", true],
    ["max", true],
    ["none", false],
    ["xhigh", false],
    ["Low", false],
    [1, false],
    [null, false],
    [undefined, false],
  ])("is %s for %j", (value, expected) => {
    expect(isReasoningLevel(value)).toBe(expected);
  });
});

describe("depthToLevel", () => {
  it.each([
    ["none", "off"],
    ["NONE", "off"],
    ["low", "low"],
    ["Medium", "medium"],
    ["high", "high"],
    ["xhigh", "max"],
    ["XHigh", "max"],
    ["max", "max"],
    ["off", null],
    ["minimal", null],
    ["", null],
    [" low", null],
    [2, null],
    [null, null],
    [undefined, null],
    [{}, null],
  ])("reads %j as %s", (raw, expected) => {
    expect(depthToLevel(raw)).toBe(expected);
  });
});

describe("parseCsharpReasoningDepth", () => {
  it.each([
    ["none", "none"],
    ["Low", "low"],
    ["MEDIUM", "medium"],
    ["high", "high"],
    ["XHigh", "xhigh"],
    ["max", "max"],
    ["off", undefined],
    ["minimal", undefined],
    ["", undefined],
    [3, undefined],
    [null, undefined],
  ])("parses %j as %s", (raw, expected) => {
    expect(parseCsharpReasoningDepth(raw)).toBe(expected);
  });
});

describe("levelToDepth", () => {
  it.each([
    ["off", "none"],
    ["low", "low"],
    ["medium", "medium"],
    ["high", "high"],
    ["max", "max"],
  ] as const)("writes %s as %s", (level, depth) => {
    expect(levelToDepth(level)).toBe(depth);
  });
});

describe("depthsToLevels", () => {
  it.each([
    [
      ["max", "none", "low", "xhigh", "LOW", "bogus", 3, null],
      ["low", "max"],
    ],
    [
      ["high", "medium"],
      ["medium", "high"],
    ],
    [[], []],
    [["none"], []],
    ["low", []],
    [null, []],
    [{ 0: "low" }, []],
  ])("reads %j as %j", (raw, expected) => {
    expect(depthsToLevels(raw)).toEqual(expected);
  });
});

describe("reasoningConfigToSupport", () => {
  // `ReasoningConfig` as the profile storage serves it: thinks, canDisable, depths, defaultDepth.
  const config = () => ({
    thinks: true,
    canDisable: false,
    depths: ["low", "medium", "high", "xhigh"],
    defaultDepth: "medium",
  });

  it("maps a full ReasoningConfig", () => {
    expect(reasoningConfigToSupport(config())).toEqual({
      thinks: true,
      canDisable: false,
      depths: ["low", "medium", "high", "max"],
      defaultDepth: "medium",
    });
  });

  it("reads the legacy boolean as full or no support", () => {
    expect(reasoningConfigToSupport(true)).toBe(FULL_REASONING_SUPPORT);
    expect(reasoningConfigToSupport(false)).toBe(NO_REASONING_SUPPORT);
  });

  it.each([null, undefined, "true", 1, [true]])("answers undefined for %j", (raw) => {
    expect(reasoningConfigToSupport(raw)).toBeUndefined();
  });

  it.each([
    ["thinks", undefined],
    ["thinks", null],
    ["thinks", "true"],
    ["canDisable", undefined],
    ["canDisable", null],
    ["canDisable", 0],
  ])("answers undefined when %s is %j", (field, value) => {
    const raw: Record<string, unknown> = { ...config(), [field]: value };
    if (value === undefined) {
      delete raw[field];
    }
    expect(reasoningConfigToSupport(raw)).toBeUndefined();
  });

  it.each([
    [undefined, []],
    [null, []],
    ["low", []],
    [["max", "bogus"], ["max"]],
  ])("reads depths %j as %j", (depths, expected) => {
    expect(reasoningConfigToSupport({ ...config(), depths })?.depths).toEqual(expected);
  });

  it.each([undefined, null, "none", "bogus", 2])("leaves defaultDepth %j out", (defaultDepth) => {
    const support = reasoningConfigToSupport({ ...config(), defaultDepth });
    expect(support).not.toHaveProperty("defaultDepth");
  });

  it("folds a defaultDepth of xhigh into max", () => {
    expect(reasoningConfigToSupport({ ...config(), defaultDepth: "xhigh" })?.defaultDepth).toBe(
      "max",
    );
  });
});

describe("supportToReasoningConfig", () => {
  it("writes the support with its default depth", () => {
    expect(
      supportToReasoningConfig({
        thinks: true,
        canDisable: true,
        depths: ["low", "max"],
        defaultDepth: "high",
      }),
    ).toEqual({ thinks: true, canDisable: true, depths: ["low", "max"], defaultDepth: "high" });
  });

  it("writes an unknown default depth as null", () => {
    expect(supportToReasoningConfig(NO_REASONING_SUPPORT)).toEqual({
      thinks: false,
      canDisable: false,
      depths: [],
      defaultDepth: null,
    });
  });
});
