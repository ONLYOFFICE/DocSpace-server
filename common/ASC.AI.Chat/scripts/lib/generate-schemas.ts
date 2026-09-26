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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import path from "path";
import { fileURLToPath } from "url";
import { createGenerator } from "ts-json-schema-generator";
import { toOpenApiSchemas } from "./draft-to-openapi.js";
import { applySchemaDocs, cleanOperationDescriptions } from "./schemaDocs.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const SCHEMA_TYPES = path.resolve(__dirname, "..", "schema", "schemaTypes.ts");
// The base tsconfig resolves the library types correctly; a narrowed config
// makes the generator resolve them to `{}`. `schemaTypes.ts` pulls its
// `@assistant-ui/react` shim via a triple-slash reference.
const TSCONFIG = path.resolve(__dirname, "..", "..", "tsconfig.json");

// Prefix applied to shared/nested schema names (everything except the
// already operation-scoped `Req_*` / `Res_*`) so component names cannot clash
// with the .NET services' components when `OpenapiJoiner` merges the
// documents — it throws on a same-name-different-content collision (e.g. the
// generic `ProviderType` / `ActionType`).
const SCHEMA_NAMESPACE = "Ai";

function isOperationScoped(name: string): boolean {
  return name.startsWith("Req_") || name.startsWith("Res_");
}

function namespacedName(name: string): string {
  return isOperationScoped(name) ? name : `${SCHEMA_NAMESPACE}${name}`;
}

function rewriteRefs(node: unknown, rename: (name: string) => string): unknown {
  if (Array.isArray(node)) {
    return node.map((n) => rewriteRefs(n, rename));
  }
  if (node !== null && typeof node === "object") {
    const out: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(node)) {
      if (key === "$ref" && typeof value === "string") {
        const match = value.match(/^#\/components\/schemas\/(.+)$/);
        out[key] = match ? `#/components/schemas/${rename(match[1]!)}` : value;
      } else {
        out[key] = rewriteRefs(value, rename);
      }
    }
    return out;
  }
  return node;
}

// Namespace shared schema names and rewrite every `$ref` accordingly.
function namespaceSchemas(schemas: Record<string, unknown>): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [name, schema] of Object.entries(schemas)) {
    out[namespacedName(name)] = rewriteRefs(schema, namespacedName);
  }
  return out;
}

/** Per-operation request/response schema, inlined into the document. */
export interface OperationSchemas {
  request?: unknown;
  response?: unknown;
}

export interface OpenApiSchemaBundle {
  /** Shared, named types → `components.schemas`. */
  components: Record<string, unknown>;
  /** operationId → its request/response schema (inlined, not a component). */
  operations: Record<string, OperationSchemas>;
}

/**
 * Split the generated definitions: the `Req_*` / `Res_*` entries exist only
 * as a wiring convention and are inlined into their operation, while the
 * shared named types stay as reusable components.
 *
 * They are deliberately NOT emitted as components: most are degenerate
 * (a bare `$ref` alias, a primitive, an array or a nullable union), and SDK
 * generators turn every component into a model class — producing ~70 empty
 * or non-compiling classes (`ResAiToolsGetCustomServer` and friends).
 * Inlined, they resolve to the referenced model, a plain `string`/`bool`,
 * or a `List<T>` — and only genuine inline objects become a model.
 */
function splitBundle(schemas: Record<string, unknown>): OpenApiSchemaBundle {
  const components: Record<string, unknown> = {};
  const operations: Record<string, OperationSchemas> = {};

  for (const [name, schema] of Object.entries(schemas)) {
    const match = name.match(/^(Req|Res)_(.+)$/);
    if (!match) {
      components[name] = schema;
      continue;
    }
    const [, kind, operationId] = match as unknown as [string, "Req" | "Res", string];
    const entry = operations[operationId] ?? {};
    if (kind === "Req") {
      entry.request = schema;
    } else {
      entry.response = schema;
    }
    operations[operationId] = entry;
  }

  return { components, operations };
}

/**
 * Run `ts-json-schema-generator` over `schemaTypes.ts` and convert the
 * draft-07 output into OpenAPI 3.0 schemas. `skipTypeCheck` mirrors the CLI
 * `--no-type-check`, needed to tolerate the library's DOM-typed provider
 * fields the parser cannot model (the AI engine inputs, which are
 * intentionally excluded from `schemaTypes.ts`).
 */
export function generateOpenApiSchemas(): OpenApiSchemaBundle {
  const schema = createGenerator({
    path: SCHEMA_TYPES,
    tsconfig: TSCONFIG,
    type: "*",
    skipTypeCheck: true,
    encodeRefs: true,
  }).createSchema("*");

  const definitions = (schema.definitions ?? {}) as Record<string, unknown>;
  const bundle = splitBundle(namespaceSchemas(toOpenApiSchemas(definitions)));

  // Last: the descriptions the library does not declare are filled in here,
  // keyed by the namespaced component names this pipeline has just settled on.
  const { components, unused } = applySchemaDocs(bundle.components);
  for (const entry of unused) {
    const target =
      entry.property === undefined ? entry.schema : `${entry.schema}.${entry.property}`;
    console.warn(`SCHEMA_DOCS entry changed nothing: ${target} (${entry.reason})`);
  }

  return { components, operations: cleanOperationDescriptions(bundle.operations) };
}
