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

import { Linter } from "eslint";
import tseslint from "typescript-eslint";
import { describe, expect, it } from "vitest";
import config from "../../eslint.config.js";

// The `.catch()` that answers a constant (ts-ai-failure-paths.md §1): the selectors of
// eslint.config.js, run on the forms the rule names and on the ones it leaves alone.

const restricted = config.find(
  (entry) => entry.files?.includes("**/*.ts") && entry.rules?.["no-restricted-syntax"],
)?.rules?.["no-restricted-syntax"];

const linter = new Linter({ configType: "flat" });
const messages = (code: string): string[] =>
  linter
    .verify(
      code,
      [
        {
          files: ["**/*.ts"],
          languageOptions: { parser: tseslint.parser },
          rules: { "no-restricted-syntax": restricted },
        },
      ],
      "app/x.ts",
    )
    .map((m) => m.message);
const swallowed = (code: string): number =>
  messages(code).filter((m) => m.startsWith("A .catch() that answers a constant")).length;
const fannedOut = (code: string): number =>
  messages(code).filter((m) => m.startsWith("Promise.all over a mapped list")).length;

describe("the rule against a .catch() that answers a constant", () => {
  it("is in the configuration of the service code", () => {
    expect(restricted).toBeDefined();
  });

  it.each([
    ["an arrow answering null", "const v = p.catch(() => null);"],
    ["an arrow answering an empty list", "const v = p.catch((e) => []);"],
    ["a block that logs and answers null", "const v = p.catch((e) => { log(e); return null; });"],
    ["a block answering undefined", "const v = p.catch(() => { return undefined; });"],
    ["a block answering an empty object", "const v = p.catch(function () { return {}; });"],
    ["a bare return", "const v = p.catch((e) => { log(e); return; });"],
    ["a block that only logs, read as the value", "const v = await p.catch((e) => { log(e); });"],
    ["an empty block before finally", "p.catch(() => {}).finally(done);"],
  ])("refuses %s", (_what, code) => {
    expect(swallowed(code)).toBe(1);
  });

  it.each([
    [
      "a handler that rethrows",
      "const v = p.catch((e) => { if (absent(e)) return null; throw e; });",
    ],
    ["a side branch that drops a cache", "p.catch(() => { cache = undefined; });"],
    ["a handler that answers a value it read", "const v = p.catch((e) => fallback(e));"],
  ])("leaves %s alone", (_what, code) => {
    expect(swallowed(code)).toBe(0);
  });
});

// Calls made in a loop for one request go through `pooled` (ts-ai-style.md, "Upstream calls").
describe("the rule against Promise.all over a mapped list", () => {
  it.each([
    ["Promise.all over a map", "await Promise.all(ids.map((id) => read(id)));"],
    ["Promise.allSettled over a map", "await Promise.allSettled(ids.map(read));"],
    ["a map of a slice", "await Promise.all(ids.slice(0, 20).map((id) => read(id)));"],
  ])("refuses %s", (_what, code) => {
    expect(fannedOut(code)).toBe(1);
  });

  it.each([
    ["a fixed list of calls", "const [a, b] = await Promise.all([readA(), readB()]);"],
    ["pooled", "await pooled(ids, 4, (id) => read(id));"],
    ["a list of workers", "await Promise.all(Array.from({ length: 4 }, worker));"],
  ])("leaves %s alone", (_what, code) => {
    expect(fannedOut(code)).toBe(0);
  });
});
