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
import { csharpToToolPermissionMode, toolPermissionModeToCsharp } from "../toolPermissionMode.js";

// The C# `ToolPermissionMode` enum: products/ASC.AI/Core/Settings/ToolPermissionMode.cs (Ask = 0,
// Auto = 1, Allow = 2).

describe("csharpToToolPermissionMode", () => {
  it.each([
    [0, "ask"],
    [1, "auto"],
    [2, "allow"],
    [3, null],
    [-1, null],
    [1.5, null],
    [Number.NaN, null],
    ["ask", "ask"],
    ["Auto", "auto"],
    ["ALLOW", "allow"],
    ["0", "ask"],
    ["2", "allow"],
    ["02", "allow"],
    ["3", null],
    ["-1", null],
    ["1.0", null],
    [" 1", null],
    ["", null],
    ["never", null],
    [null, null],
    [undefined, null],
    [true, null],
    [{ mode: 1 }, null],
  ])("reads %j as %s", (raw, expected) => {
    expect(csharpToToolPermissionMode(raw)).toBe(expected);
  });
});

describe("toolPermissionModeToCsharp", () => {
  it.each([
    ["ask", 0],
    ["auto", 1],
    ["allow", 2],
    ["Ask", -1],
    ["never", -1],
  ])("writes %s as %i", (mode, expected) => {
    expect(toolPermissionModeToCsharp(mode as never)).toBe(expected);
  });
});
