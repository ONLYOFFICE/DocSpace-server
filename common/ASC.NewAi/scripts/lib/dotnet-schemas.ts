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

import { readFileSync } from "fs";
import path from "path";
import { fileURLToPath } from "url";

// Reuse of the .NET AI service's OpenAPI document for the proxy routes.
//
// The `/agents/*` and `/config/*` routes forward verbatim to the .NET AI
// service and return its raw DocSpace response envelope (the `*Wrapper`
// schemas). Rather than re-describe those responses by hand — and drift from
// the C# DTOs' XML docs — this module reads the sibling `ai_2.0.json` (already
// generated from the C# source by the .NET ApiDescription pipeline) and lifts
// the concrete 200 response schemas out of it, so this service's document stops
// emitting the opaque generic-object fallback for these operations.
//
// The document must already exist and contain the mapped operations: it is
// produced by the ASC.AI.Server build into the shared `json/` folder. This
// step reads it as a plain file (staying a pure, offline build step) and
// FAILS LOUDLY if it is missing or incomplete — a silent fallback would write
// a stale/empty shape without any signal (see the throws below).

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// The shared `json/` folder consumed by `OpenapiJoiner`, resolved from a fixed
// location — NOT from the generator's `--out` override — so the input is found
// regardless of where this service's document is written.
const SHARED_JSON_DIR = path.resolve(
  __dirname,
  "..",
  "..",
  "..",
  "Tools",
  "ASC.Api.Documentation",
  "ASC.Api.Documentation",
  "json",
);
const AI_DOCUMENT = path.join(SHARED_JSON_DIR, "ai_2.0.json");

// Prefix applied to every lifted component name (and its `$ref`s) so the
// copied schemas cannot collide with the .NET services' own components once
// `OpenapiJoiner` merges the documents — matching the `Ai` namespace the
// generated TypeScript schemas already use.
const SCHEMA_NAMESPACE = "Ai";

// Local operationId → .NET AI operationId. Each local proxy route forwards
// verbatim to the .NET AI service and returns the same-shaped response as the
// .NET operation named here; kept in sync by hand with `apiCatalog.ts` (the
// custom routes) and the .NET controllers (`AgentsController`,
// `SettingsController`). Only operations with a JSON 200 response belong here —
// fire-and-forget routes (e.g. `vectorization/tasks`, which returns no body)
// are intentionally omitted and fall back to the generic response shape.
const PROXY_RESPONSE_SOURCES: Readonly<Record<string, string>> = {
  aiAgentsList: "getAgents",
  aiAgentsCreate: "createAgent",
  aiAgentsNews: "getAgentsNewItems",
  aiAgentsGet: "getAgentInfo",
  aiAgentsUpdate: "updateAgent",
  aiAgentsDelete: "deleteAgent",
  aiAgentsUpdateQuota: "updateAgentsQuota",
  aiAgentsResetQuota: "resetAgentsQuota",
  aiSettingsGet: "getAiSettings",
  aiSettingsGetVectorization: "getVectorizationSettings",
  aiSettingsSetVectorization: "setVectorizationSettings",
  aiSettingsGetUser: "getAiUserSettings",
  aiSettingsSetUser: "setAiUserSettings",
};

type JsonObject = Record<string, unknown>;

/** Concrete response schemas lifted from the .NET AI document. */
export interface DotnetProxySchemas {
  /** Namespaced component name → its (ref-rewritten) schema. */
  readonly components: Record<string, unknown>;
  /** Local operationId → its 200 response schema (a namespaced `$ref`). */
  readonly responses: Record<string, unknown>;
}

function fail(message: string): never {
  throw new Error(
    `Cannot enrich the proxy routes from ${AI_DOCUMENT}: ${message}. `
    + "Build ASC.AI.Server (which emits ai_2.0.json) before generating the "
    + "OpenAPI document for this service.",
  );
}

function isObject(node: unknown): node is JsonObject {
  return node !== null && typeof node === "object" && !Array.isArray(node);
}

// Local component name of a `#/components/schemas/<name>` reference, or null.
function refName(ref: unknown): string | null {
  if (typeof ref !== "string") {
    return null;
  }
  const match = ref.match(/^#\/components\/schemas\/(.+)$/);
  return match ? match[1]! : null;
}

// Every local component name referenced anywhere within `node`.
function collectRefs(node: unknown, acc: Set<string>): void {
  if (Array.isArray(node)) {
    for (const child of node) {
      collectRefs(child, acc);
    }
    return;
  }
  if (isObject(node)) {
    for (const [key, value] of Object.entries(node)) {
      const name = key === "$ref" ? refName(value) : null;
      if (name !== null) {
        acc.add(name);
      } else {
        collectRefs(value, acc);
      }
    }
  }
}

// Rewrite every `#/components/schemas/X` ref to the namespaced `AiX`.
function namespaceRefs(node: unknown): unknown {
  if (Array.isArray(node)) {
    return node.map(namespaceRefs);
  }
  if (isObject(node)) {
    const out: JsonObject = {};
    for (const [key, value] of Object.entries(node)) {
      const name = key === "$ref" ? refName(value) : null;
      out[key] = name !== null
        ? `#/components/schemas/${SCHEMA_NAMESPACE}${name}`
        : namespaceRefs(value);
    }
    return out;
  }
  return node;
}

/**
 * Read the .NET AI OpenAPI document and lift the 200 response schemas of the
 * proxied operations, together with the transitive closure of components they
 * reference. Throws (never falls back silently) when the document is missing,
 * unparseable, or does not contain a mapped operation / referenced component.
 */
export function extractDotnetProxySchemas(): DotnetProxySchemas {
  let raw: string;
  try {
    raw = readFileSync(AI_DOCUMENT, "utf8");
  } catch {
    fail("the document does not exist or is unreadable");
  }

  let document: unknown;
  try {
    document = JSON.parse(raw);
  } catch (err) {
    fail(`the document is not valid JSON (${(err as Error).message})`);
  }

  if (!isObject(document) || !isObject(document["paths"])) {
    fail("the document has no `paths`");
  }
  const componentsNode = isObject(document["components"])
    ? document["components"]["schemas"]
    : undefined;
  if (!isObject(componentsNode)) {
    fail("the document has no `components.schemas`");
  }
  const schemas = componentsNode as JsonObject;

  // operationId → its 200 `application/json` schema, indexed across all paths.
  const responseByOperation = new Map<string, unknown>();
  for (const item of Object.values(document["paths"] as JsonObject)) {
    if (!isObject(item)) {
      continue;
    }
    for (const operation of Object.values(item)) {
      if (!isObject(operation) || typeof operation["operationId"] !== "string") {
        continue;
      }
      const responses = operation["responses"];
      const ok = isObject(responses) ? responses["200"] : undefined;
      const content = isObject(ok) ? ok["content"] : undefined;
      const json = isObject(content) ? content["application/json"] : undefined;
      const schema = isObject(json) ? json["schema"] : undefined;
      if (schema !== undefined) {
        responseByOperation.set(operation["operationId"], schema);
      }
    }
  }

  // Resolve each mapped operation's response schema, collecting the missing
  // ones so a single throw reports every gap at once.
  const responses: Record<string, unknown> = {};
  const rootRefs = new Set<string>();
  const missing: string[] = [];
  for (const [aiOperationId, dotnetOperationId] of Object.entries(PROXY_RESPONSE_SOURCES)) {
    const schema = responseByOperation.get(dotnetOperationId);
    if (schema === undefined) {
      missing.push(dotnetOperationId);
      continue;
    }
    collectRefs(schema, rootRefs);
    responses[aiOperationId] = namespaceRefs(schema);
  }
  if (missing.length > 0) {
    fail(`no 200 JSON response for operation(s): ${missing.sort().join(", ")}`);
  }

  // Transitive closure of referenced components; a dangling ref means the
  // .NET document is internally inconsistent, so fail rather than emit it.
  const closure = new Set<string>();
  const dangling = new Set<string>();
  const stack = [...rootRefs];
  while (stack.length > 0) {
    const name = stack.pop()!;
    if (closure.has(name)) {
      continue;
    }
    const schema = schemas[name];
    if (schema === undefined) {
      dangling.add(name);
      continue;
    }
    closure.add(name);
    const nested = new Set<string>();
    collectRefs(schema, nested);
    for (const next of nested) {
      if (!closure.has(next)) {
        stack.push(next);
      }
    }
  }
  if (dangling.size > 0) {
    fail(`referenced component(s) missing: ${[...dangling].sort().join(", ")}`);
  }

  const components: Record<string, unknown> = {};
  for (const name of closure) {
    components[`${SCHEMA_NAMESPACE}${name}`] = namespaceRefs(schemas[name]);
  }

  return { components, responses };
}
