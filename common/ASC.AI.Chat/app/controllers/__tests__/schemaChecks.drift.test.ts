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

import { readdirSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

// ts-ai-tests.md §9: every answer of a route handler is checked against the schema the service
// publishes for it. A test file of a controller that never calls `responseViolations` checks none,
// so a new one fails here. The files below predate the rule and drive their handlers through a
// hand-made response double (§2); each leaves this list when it moves to `invoke` and checks its
// answers. Not tests of one route: the helpers, the error boundary and the access gates (the
// thread creation gate of threadsController.upstream).
const NOT_ROUTE_TESTS =
  /^(helpers\.|errorHandler\.|aiAccess\.|cacheBreakpoints\.|storedProfile\.|schemaChecks\.|threadsController\.upstream\.)/;
const PENDING = new Set([
  "aiController.engine.test.ts",
  "aiRoundPreflight.test.ts",
  "assignmentsController.test.ts",
  "attachmentsController.test.ts",
  "documentAgentController.test.ts",
  "metaController.test.ts",
  "threadsController.test.ts",
  "toolsController.test.ts",
]);

const dir = fileURLToPath(new URL(".", import.meta.url));
const files = readdirSync(dir).filter((f) => f.endsWith(".test.ts") && !NOT_ROUTE_TESTS.test(f));
const checks = (f: string) => readFileSync(`${dir}${f}`, "utf8").includes("responseViolations(");

describe("controller tests check their answers against the published schema", () => {
  it.each(files.filter((f) => !PENDING.has(f)))("%s calls responseViolations", (f) => {
    expect(checks(f)).toBe(true);
  });

  // A pending file that starts checking leaves the list, so that it cannot stop again unnoticed.
  it.each([...PENDING])("%s is still pending and exists", (f) => {
    expect(files).toContain(f);
    expect(checks(f)).toBe(false);
  });
});
