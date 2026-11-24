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

import { ActionType } from "@onlyoffice/ai-chat/core";
import { describe, expect, it } from "vitest";
import {
  asActionType,
  asNumber,
  asString,
  getArray,
  getBoolean,
  getEntityId,
  getNumber,
  getObject,
  getObjectArray,
  getString,
  isObject,
  parseInt10,
  parseInteger,
} from "../narrow.js";
import { sanitizeInstruction } from "../sanitizeInstruction.js";

class Thing {
  a = 1;
}

describe("isObject", () => {
  it.each([
    [{}, true],
    [{ a: 1 }, true],
    [Object.create(null), true],
    [new Thing(), true],
    [new Date(0), true],
    [null, false],
    [[], false],
    [[1], false],
    ["{}", false],
    [1, false],
    [undefined, false],
    [true, false],
  ])("is %j -> %s", (value, expected) => {
    expect(isObject(value)).toBe(expected);
  });
});

describe("the field readers", () => {
  const obj = {
    str: "s",
    empty: "",
    num: 3,
    zero: 0,
    nan: NaN,
    inf: Infinity,
    numStr: "7",
    bool: false,
    obj: { x: 1 },
    arr: [1, { a: 1 }, null, [2], "x", { b: 2 }],
    nul: null,
  };

  it.each([
    ["str", "s"],
    ["empty", ""],
    ["num", undefined],
    ["nul", undefined],
    ["missing", undefined],
  ])("getString of %s", (key, expected) => {
    expect(getString(obj, key)).toBe(expected);
  });

  it.each([
    ["num", 3],
    ["zero", 0],
    ["nan", undefined],
    ["inf", undefined],
    ["numStr", undefined],
    ["missing", undefined],
  ])("getNumber of %s", (key, expected) => {
    expect(getNumber(obj, key)).toBe(expected);
  });

  it.each([
    ["num", "3"],
    ["zero", "0"],
    ["numStr", "7"],
    ["str", "s"],
    ["empty", undefined],
    ["nan", undefined],
    ["inf", undefined],
    ["bool", undefined],
    ["nul", undefined],
    ["missing", undefined],
  ])("getEntityId of %s", (key, expected) => {
    expect(getEntityId(obj, key)).toBe(expected);
  });

  it.each([
    ["bool", false],
    ["str", undefined],
    ["num", undefined],
    ["missing", undefined],
  ])("getBoolean of %s", (key, expected) => {
    expect(getBoolean(obj, key)).toBe(expected);
  });

  it.each([
    ["obj", { x: 1 }],
    ["arr", undefined],
    ["nul", undefined],
    ["str", undefined],
    ["missing", undefined],
  ])("getObject of %s", (key, expected) => {
    expect(getObject(obj, key)).toEqual(expected);
  });

  it("getObject answers the same object, not a copy", () => {
    expect(getObject(obj, "obj")).toBe(obj.obj);
  });

  it.each([
    ["arr", obj.arr],
    ["obj", undefined],
    ["str", undefined],
    ["missing", undefined],
  ])("getArray of %s", (key, expected) => {
    expect(getArray(obj, key)).toBe(expected);
  });

  it("getObjectArray keeps only the object entries", () => {
    expect(getObjectArray(obj, "arr")).toEqual([{ a: 1 }, { b: 2 }]);
    expect(getObjectArray(obj, "obj")).toBeUndefined();
    expect(getObjectArray({ list: [] }, "list")).toEqual([]);
  });

  it("reads an own field named like a prototype member as absent unless set", () => {
    expect(getString({}, "toString")).toBeUndefined();
    expect(getObject({}, "constructor")).toBeUndefined();
  });
});

describe("asString and asNumber", () => {
  it.each([
    ["s", "s"],
    ["", ""],
    [1, undefined],
    [null, undefined],
    [undefined, undefined],
    [["s"], undefined],
  ])("asString(%j)", (value, expected) => {
    expect(asString(value)).toBe(expected);
  });

  it.each([
    [1, 1],
    [-1.5, -1.5],
    [0, 0],
    [NaN, undefined],
    [-Infinity, undefined],
    ["1", undefined],
    [null, undefined],
  ])("asNumber(%j)", (value, expected) => {
    expect(asNumber(value)).toBe(expected);
  });
});

describe("parseInt10", () => {
  it.each([
    [undefined, 9, 9],
    [null, 9, 9],
    ["", 9, 9],
    [undefined, undefined, undefined],
    [5, 9, 5],
    [5.7, 9, 5.7],
    [-3, 9, -3],
    [NaN, 9, 9],
    [Infinity, 9, 9],
    ["42", 9, 42],
    ["42abc", 9, 42],
    ["30s", 9, 30],
    [" 7", 9, 7],
    ["-8", 9, -8],
    ["0x10", 9, 0],
    ["1e3", 9, 1],
    ["010", 9, 10],
    ["abc", 9, 9],
    ["abc", undefined, undefined],
    [" ", 9, 9],
    [true, 9, 9],
    [{}, 9, 9],
    [[1], 9, 9],
  ])("parseInt10(%j, %j) is %j", (value, fallback, expected) => {
    expect(parseInt10(value, fallback)).toBe(expected);
  });
});

describe("parseInteger", () => {
  it.each([
    ["42", 42],
    ["0", 0],
    ["-0", 0],
    ["-8", -8],
    ["010", 10],
    ["9007199254740991", Number.MAX_SAFE_INTEGER],
    [65, 65],
    [-3, -3],
    [0, 0],
  ])("reads %j as %j", (value, expected) => {
    expect(parseInteger(value)).toBe(expected);
  });

  it.each([
    "65abc",
    "20abc",
    "30s",
    "1.5",
    "1.0",
    "1e3",
    "0x10",
    "+7",
    " 7",
    "7 ",
    "",
    " ",
    "-",
    "abc",
    "9007199254740992",
    65.5,
    NaN,
    Infinity,
    2 ** 53,
    undefined,
    null,
    true,
    {},
    [1],
    ["1"],
  ])("refuses %j", (value) => {
    expect(parseInteger(value)).toBeUndefined();
  });
});

describe("asActionType", () => {
  it("accepts every ActionType value", () => {
    for (const value of Object.values(ActionType)) {
      expect(asActionType(value)).toBe(value);
    }
  });

  it.each([undefined, null, 1, "", "nope", ["x"]])("refuses %j", (value) => {
    expect(asActionType(value)).toBeUndefined();
  });

  it("refuses an ActionType key that is not one of its values", () => {
    const keys = Object.keys(ActionType).filter(
      (k) => !(Object.values(ActionType) as string[]).includes(k),
    );
    for (const key of keys) {
      expect(asActionType(key)).toBeUndefined();
    }
  });
});

describe("sanitizeInstruction (Bug 82726)", () => {
  it.each([
    ["", ""],
    ["plain text", "plain text"],
    ["a <b>bold</b> word", "a bold word"],
    ['<img src=x onerror="alert(1)">after', "after"],
    ["<script>alert(1)</script>kept", "kept"],
    ["<style>p{}</style>kept", "kept"],
    ["<SCRIPT>x</SCRIPT>kept", "kept"],
    ['<a href="javascript:x">link</a>', "link"],
    ["b > a", "b &gt; a"],
    ["Tom & Jerry", "Tom & Jerry"],
    ["<!-- c -->text", "text"],
    ["line1\nline2", "line1\nline2"],
    ["<unknown-tag>x</unknown-tag>", "x"],
  ])("turns %j into %j", (input, expected) => {
    expect(sanitizeInstruction(input)).toBe(expected);
  });

  it("keeps a bare < as inert text instead of dropping what follows it", () => {
    expect(sanitizeInstruction("1 < 2 and 3 > 2")).toBe("1 &lt; 2 and 3 &gt; 2");
    expect(sanitizeInstruction("a < b")).toBe("a &lt; b");
    expect(sanitizeInstruction("a <b>bold</b> < c")).toBe("a bold &lt; c");
  });

  it("answers an empty string for a falsy value that slipped past the type", () => {
    expect(sanitizeInstruction(undefined as unknown as string)).toBe("");
    expect(sanitizeInstruction(null as unknown as string)).toBe("");
  });
});
