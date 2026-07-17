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

// Build-time OpenAPI emitter. Writes the New AI service's OpenAPI document
// as `newai_2.0.json` into the shared documentation folder, so it can be
// merged with the .NET services' documents by ASC.Api.Documentation.
//
// This is a pure, offline step: it imports only the declarative route
// catalog (which depends solely on `@onlyoffice/ai-chat/core`), never the
// Express app, storage or appsettings — so it runs with no DB/config and no
// running server. Invoked via `yarn openapi` (see package.json) and wired
// into the ASC.Api.Documentation build via an MSBuild `<Exec>` target.
//
// The document is emitted in "merge" shape: absolute path keys prefixed with
// the service's route (`/api/2.0/new-ai/...`) and the shared `{baseUrl}`
// server template — matching the .NET service documents so `OpenapiJoiner`
// can combine them without rewriting paths.

import { mkdirSync, writeFileSync } from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { buildOpenApiDocument } from "../app/openapi.js";
import { API_PREFIX, ENGINE_DOCS, CUSTOM_ROUTE_DOCS } from "../app/apiCatalog.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Default output: the ASC.Api.Documentation `json/` folder consumed by the
// joiner (../../Tools/... relative to this script). Overridable with
// `--out <path>` for ad-hoc runs.
const DEFAULT_OUTPUT = path.resolve(
  __dirname,
  "..",
  "..",
  "Tools",
  "ASC.Api.Documentation",
  "ASC.Api.Documentation",
  "json",
  "newai_2.0.json",
);

function parseOutput(argv: string[]): string {
  const i = argv.indexOf("--out");
  if (i !== -1 && argv[i + 1]) {
    return path.resolve(argv[i + 1]!);
  }
  return DEFAULT_OUTPUT;
}

const output = parseOutput(process.argv.slice(2));

const document = buildOpenApiDocument({
  apiPrefix: API_PREFIX,
  engines: ENGINE_DOCS,
  customRoutes: CUSTOM_ROUTE_DOCS,
});

mkdirSync(path.dirname(output), { recursive: true });
// Trailing newline: matches the repo's `insert_final_newline` convention.
writeFileSync(output, `${JSON.stringify(document, null, 2)}\n`, "utf8");

const operationCount = Object.values(document["paths"] as Record<string, object>)
  .reduce((sum, item) => sum + Object.keys(item).length, 0);

console.log(`Wrote OpenAPI document (${operationCount} operations) to ${output}`);
