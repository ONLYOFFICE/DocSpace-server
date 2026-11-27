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

import os from "node:os";
import path from "node:path";
import { vi } from "vitest";

// The service reads the environment as well as the appsettings: config/index.ts reads its
// variables straight from `process.env`, and nconf copies the whole environment when the
// config module is loaded (`nconf.env()`), so a name with `:` in it overrides the settings
// tree and a top-level name (`OTEL_EXPORTER_OTLP_ENDPOINT`, `logLevel`) answers `nconf.get`.
// A shell that exports `core__cors` or `AI__MCP__0__ENDPOINT` would change what a test sees,
// so every such variable is removed here, before any test file imports the config module.
// `vi.stubEnv` in a test still works: `vi.unstubAllEnvs` puts back the cleared state.
// `NODE_ENV` stays: vitest sets it to `test`, and config/ holds no `test.json`.
const SERVICE_ENV_NAMES = new Set([
  "API_HOST",
  "AI_SERVICE_URL",
  "core__cors",
  "logConsole",
  "logPath",
  "logLevel",
  "aws",
  "openTelemetry",
  "core",
  "ai",
  "app",
]);
const SERVICE_ENV_PREFIXES = ["AI__", "AI_CHAT_", "OTEL_"];
for (const name of Object.keys(process.env)) {
  if (
    SERVICE_ENV_NAMES.has(name) ||
    name.includes(":") ||
    SERVICE_ENV_PREFIXES.some((prefix) => name.startsWith(prefix))
  ) {
    delete process.env[name];
  }
}

// The service reads the appsettings of the installation it runs in (config/config.json points
// at ../buildtools/config). A test must see the same values on every machine, so the directory
// is pointed at one that does not exist: CORS off, no document agent, no MCP servers, the
// defaults of the code everywhere. A test that needs a setting stubs its environment variable.
process.env["app:appsettings"] = path.join(os.tmpdir(), "asc-ai-chat-no-appsettings");

// config/index.ts prints these two lines each time it is loaded, and tests reload it often.
// Not a spy: `restoreMocks` would undo one before the first test. Everything else still prints.
const log = console.log.bind(console);
console.log = (...args: unknown[]): void => {
  const first = args[0];
  if (typeof first === "string" && /^(NODE_ENV|environment): /.test(first)) {
    return;
  }
  log(...args);
};

// One logger stub for every test file (vitest.config.ts, setupFiles): the real one creates
// the log directory and writes files. A test that asserts on a line imports the logger and
// reads the spy, `expect(logger.warn).toHaveBeenCalledWith(...)`; a file that needs another
// shape still declares its own vi.mock, which wins over this one.
vi.mock("../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
  logStream: { write: vi.fn() },
  logDir: "/nowhere",
}));
