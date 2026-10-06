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
import { isToolPermissionMode, type ToolPermissionMode } from "@onlyoffice/ai-chat/core";

// The C# `ASC.AI.Core.Settings.ToolPermissionMode` enum (`Ask = 0, Auto = 1,
// Allow = 2`) and the chat library's `"ask" | "auto" | "allow"` are two
// spellings of one thing. The enum has no string converter, so on the wire
// it is a number — but a string member name is accepted too, in any casing,
// in case a converter is registered later. Writes always send the number:
// System.Text.Json takes it with or without a converter.

const BY_NUMBER: readonly ToolPermissionMode[] = ["ask", "auto", "allow"];

/**
 * A C# mode (number or member name) as the library's mode; `null` for a
 * missing or unknown value so callers can tell "nothing stored" from a mode.
 */
export function csharpToToolPermissionMode(raw: unknown): ToolPermissionMode | null {
  if (typeof raw === "number") {
    return BY_NUMBER[raw] ?? null;
  }
  if (typeof raw === "string") {
    const lower = raw.toLowerCase();
    if (isToolPermissionMode(lower)) {
      return lower;
    }
    const asNumber = Number(raw);
    return /^\d+$/.test(raw) ? (BY_NUMBER[asNumber] ?? null) : null;
  }
  return null;
}

/** A library mode as the C# enum value the settings endpoint accepts. */
export function toolPermissionModeToCsharp(mode: ToolPermissionMode): number {
  return BY_NUMBER.indexOf(mode);
}
