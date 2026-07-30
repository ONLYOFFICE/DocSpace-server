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
import { generateOpenApiSchemas } from "./lib/generate-schemas.js";
import { extractDotnetProxySchemas } from "./lib/dotnet-schemas.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Concrete request/response schemas are also written here as a committed
// artifact so the live service (`routes.ts`) serves the typed document
// without running the generator at startup.
const SCHEMAS_ARTIFACT = path.resolve(
  __dirname,
  "..",
  "app",
  "generated",
  "openapi-schemas.json",
);

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

// 1. Generate concrete request/response schemas from the TypeScript types.
const schemas = generateOpenApiSchemas();

// 1a. Enrich the proxy routes with the concrete response schemas the .NET AI
// service already emits (the DocSpace envelope `*Wrapper`s), so they stop
// falling back to the opaque generic object. The descriptions ultimately come
// from the C# DTOs' XML docs — this reuses the .NET OpenAPI document rather
// than re-deriving them, and fails loudly if it is absent/incomplete.
const proxySchemas = extractDotnetProxySchemas();
for (const [name, schema] of Object.entries(proxySchemas.components)) {
  const existing = schemas.components[name];
  if (existing !== undefined && JSON.stringify(existing) !== JSON.stringify(schema)) {
    throw new Error(
      `Schema name collision while merging .NET proxy schemas: "${name}" `
      + "already exists with different content.",
    );
  }
  schemas.components[name] = schema;
}
for (const [operationId, response] of Object.entries(proxySchemas.responses)) {
  schemas.operations[operationId] = { ...schemas.operations[operationId], response };
}

mkdirSync(path.dirname(SCHEMAS_ARTIFACT), { recursive: true });
writeFileSync(SCHEMAS_ARTIFACT, `${JSON.stringify(schemas, null, 2)}\n`, "utf8");

// 2. Build the OpenAPI document, backing each operation with its schema.
const document = buildOpenApiDocument({
  apiPrefix: API_PREFIX,
  engines: ENGINE_DOCS,
  customRoutes: CUSTOM_ROUTE_DOCS,
  schemas,
});

mkdirSync(path.dirname(output), { recursive: true });
// Trailing newline: matches the repo's `insert_final_newline` convention.
writeFileSync(output, `${JSON.stringify(document, null, 2)}\n`, "utf8");

const operationCount = Object.values(document["paths"] as Record<string, object>)
  .reduce((sum, item) => sum + Object.keys(item).length, 0);
const componentCount = Object.keys(schemas.components).length;
const typedOperationCount = Object.keys(schemas.operations).length;

console.log(
  `Wrote OpenAPI document (${operationCount} operations, ${typedOperationCount} typed, `
  + `${componentCount} shared schemas) to ${output}`,
);
