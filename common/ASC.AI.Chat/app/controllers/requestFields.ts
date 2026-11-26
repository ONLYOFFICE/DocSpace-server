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

import { badRequest } from "../httpErrors.js";

// Readers of the fields of a request body. Each answers 400 naming the field for a value of
// the wrong type, instead of a cast that hands the value on as it came
// (`ts-ai-failure-paths.md` §4).

/**
 * A field that may be left out. `undefined` and `null` are both "not sent": the chat widget
 * sends `null` in the place of a positional argument it omits.
 */
export function optionalString(value: unknown, name: string): string | undefined {
  if (value === undefined || value === null) {
    return undefined;
  }
  if (typeof value !== "string") {
    badRequest(`${name} must be a string`);
  }
  return value;
}

/** The elements of a list that holds ids: each a non-empty string. */
export function nonEmptyStrings(list: readonly unknown[], name: string): string[] {
  const items: string[] = [];
  for (const item of list) {
    if (typeof item !== "string" || item.length === 0) {
      badRequest(`${name} must hold non-empty strings only`);
    }
    items.push(item);
  }
  return items;
}
