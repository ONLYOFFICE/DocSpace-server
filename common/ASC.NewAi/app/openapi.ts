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

import type { RouteSpec } from "@onlyoffice/ai-chat/core";

// OpenAPI document generation for the New AI service.
//
// The engine routes are declared as data (the `DEFAULT_*_ROUTES`
// `RouteSpec` maps from `@onlyoffice/ai-chat/core`) and registered in a
// loop in `routes.ts`. Rather than hand-maintain a second, drift-prone copy
// of that surface as a static spec, this builder derives the OpenAPI 3.0
// document from the very same maps at startup, so a route added to the
// engine package appears in the spec automatically. Custom routes that are
// not backed by an engine (agents, text-to-docx) are described alongside
// their registration via `CustomRouteDoc` entries.

// A minimal structural subset of the OpenAPI object graph — just enough to
// type the builder without pulling in an external schema dependency.
type Json = string | number | boolean | null | Json[] | { [k: string]: Json };
type OpenApiDocument = Record<string, Json>;

// Describes one engine group so its routes get a shared tag and prose.
export interface EngineDoc {
  /** Engine key as used in `routes.ts` (`ai`, `profiles`, …). */
  readonly name: string;
  /** Display tag shown in the docs UI. */
  readonly tag: string;
  /** One-line description of the engine group. */
  readonly description: string;
  /** The engine's `DEFAULT_*_ROUTES` map (method name → route spec). */
  readonly routes: Readonly<Record<string, RouteSpec>>;
}

// Describes a route that is not backed by an `@onlyoffice/ai-chat` engine
// (registered explicitly in `routes.ts`). Kept next to that registration so
// the two stay in sync.
export interface CustomRouteDoc {
  readonly method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  /** Path relative to the API prefix, e.g. `/agents/{id}` (OpenAPI style). */
  readonly path: string;
  readonly tag: string;
  readonly summary: string;
  /**
   * Unique operation id. Required so the merged documentation tool
   * (`OpenapiJoiner`) can name the generated SDK method; it must not clash
   * with any other service's ids, hence the `newAi`-scoped values.
   */
  readonly operationId: string;
  /** Path parameter names present in `path` (the `{name}` segments). */
  readonly pathParams?: readonly string[];
  /** Whether the operation accepts a JSON request body. */
  readonly hasBody?: boolean;
}

export interface OpenApiOptions {
  /**
   * Base path the service is mounted under behind the DocSpace nginx, e.g.
   * `/api/2.0/new-ai`. Every path key is emitted absolute (prefixed with
   * this value, `/api/2.0/new-ai/ai/send`) so both the standalone docs UI
   * and any client show the full, proxy-correct URL, and so the documents
   * merge cleanly with the .NET services.
   */
  readonly apiPrefix: string;
  readonly engines: readonly EngineDoc[];
  readonly customRoutes: readonly CustomRouteDoc[];
}

// Tags are grouped under this single heading (via `x-tagGroups`) and each
// tag name is namespaced with the same prefix (`New AI / AI`, …) so they
// stay distinct from the .NET AI service's `AI / *` tags once merged.
const TAG_GROUP = "New AI";
const TAG_PREFIX = `${TAG_GROUP} / `;

function tag(name: string): string {
  return `${TAG_PREFIX}${name}`;
}

// Query params known to be optional; everything else in a `RouteSpec.params`
// list is treated as required. Numeric params get an `integer` schema.
const OPTIONAL_QUERY_PARAMS = new Set(["limit", "startIndex"]);
const INTEGER_QUERY_PARAMS = new Set(["limit", "startIndex"]);

// A permissive JSON-object response body: the engine handlers forward
// upstream `.NET` DTOs verbatim, so the concrete shape lives on the .NET
// side. The spec documents the transport, not every field.
const JSON_OBJECT_SCHEMA: Json = { type: "object", additionalProperties: true };

function jsonResponse(description: string): Json {
  return {
    description,
    content: { "application/json": { schema: JSON_OBJECT_SCHEMA } },
  };
}

const UNAUTHORIZED_RESPONSE: Json = jsonResponse(
  "Missing `asc_auth_key` cookie or `Authorization` header.",
);

function capitalize(name: string): string {
  return name.length > 0 ? name.charAt(0).toUpperCase() + name.slice(1) : name;
}

// Turn a `camelCase`/`kebab` token into a human title, e.g.
// `sendWithStream` → "Send with stream".
function humanize(name: string): string {
  const words = name
    .replace(/[-_]/g, " ")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .toLowerCase()
    .trim();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

function queryParameters(params: readonly string[]): Json[] {
  return params.map((name) => ({
    name,
    in: "query",
    required: !OPTIONAL_QUERY_PARAMS.has(name),
    schema: INTEGER_QUERY_PARAMS.has(name)
      ? { type: "integer" }
      : { type: "string" },
  }));
}

function pathParameters(names: readonly string[]): Json[] {
  return names.map((name) => ({
    name,
    in: "path",
    required: true,
    schema: { type: "string" },
  }));
}

function jsonBody(): Json {
  return {
    required: true,
    content: { "application/json": { schema: JSON_OBJECT_SCHEMA } },
  };
}

// Build the operation object for one engine route. GET routes expose their
// positional `params` as query parameters (matching the library's
// `ApiProvider`, which serializes GET args as `params[i]=value`); non-GET
// routes carry a JSON body.
function engineOperation(
  engine: EngineDoc,
  methodName: string,
  spec: RouteSpec,
): Json {
  const isGet = spec.method === "GET";
  const operation: Record<string, Json> = {
    tags: [tag(engine.tag)],
    // lowerCamelCase, `newAi`-scoped so it stays unique across engines and
    // does not clash with the .NET services' ids once merged.
    operationId: `newAi${capitalize(engine.name)}${capitalize(methodName)}`,
    summary: humanize(methodName),
    responses: {
      "200": jsonResponse("Success."),
      "401": UNAUTHORIZED_RESPONSE,
    },
  };
  if (isGet && spec.params && spec.params.length > 0) {
    operation["parameters"] = queryParameters(spec.params);
  }
  if (!isGet) {
    operation["requestBody"] = jsonBody();
  }
  return operation;
}

function customOperation(route: CustomRouteDoc): Json {
  const operation: Record<string, Json> = {
    tags: [tag(route.tag)],
    operationId: route.operationId,
    summary: route.summary,
    responses: {
      "200": jsonResponse("Success."),
      "401": UNAUTHORIZED_RESPONSE,
    },
  };
  const params: Json[] = [];
  if (route.pathParams && route.pathParams.length > 0) {
    params.push(...pathParameters(route.pathParams));
  }
  if (params.length > 0) {
    operation["parameters"] = params;
  }
  if (route.hasBody) {
    operation["requestBody"] = jsonBody();
  }
  return operation;
}

// Add an operation to `paths` under `path`+`method`, merging with any
// operation already registered for that path (different verbs share a
// path item object).
function addOperation(
  paths: Record<string, Json>,
  path: string,
  method: string,
  operation: Json,
): void {
  const item = (paths[path] as Record<string, Json>) ?? {};
  item[method.toLowerCase()] = operation;
  paths[path] = item;
}

/** Build the full OpenAPI 3.0 document for the service. */
export function buildOpenApiDocument(options: OpenApiOptions): OpenApiDocument {
  const { apiPrefix, engines, customRoutes } = options;

  // Path keys are absolute: prefixed with the service's proxy route so both
  // the docs UI and clients show the full `/api/2.0/new-ai/...` URL.
  const key = (relative: string): string => `${apiPrefix}${relative}`;

  const paths: Record<string, Json> = {};

  for (const engine of engines) {
    for (const [methodName, spec] of Object.entries(engine.routes)) {
      // Engine `RouteSpec.path` is relative and prefix-less (`ai/send`).
      addOperation(
        paths,
        key(`/${spec.path}`),
        spec.method,
        engineOperation(engine, methodName, spec),
      );
    }
  }

  for (const route of customRoutes) {
    addOperation(paths, key(route.path), route.method, customOperation(route));
  }

  // Health probes are open (registered before the auth gate) and carry no
  // security requirement.
  addOperation(paths, key("/health"), "get", {
    tags: [tag("System")],
    operationId: "newAiHealth",
    summary: "Health check",
    security: [],
    responses: { "200": jsonResponse("Service is healthy.") },
  });
  addOperation(paths, key("/isLife"), "get", {
    tags: [tag("System")],
    operationId: "newAiIsLife",
    summary: "Liveness probe",
    security: [],
    responses: { "200": { description: "Service is alive." } },
  });

  // `name` carries the full `New AI / …` value (used by operations, the
  // `x-tagGroups` grouping and the URL slug); `x-displayName` is the short
  // label the docs UI shows in the sidebar (matching the .NET services).
  const tagEntry = (name: string, description: string): Json => ({
    name: tag(name),
    description,
    "x-displayName": name,
  });

  const tags: Json[] = [
    ...engines.map((e) => tagEntry(e.tag, e.description)),
    tagEntry("Agents", "AI agent rooms (delegated to the .NET AI service)."),
    tagEntry("Export", "Markdown → docx export."),
    tagEntry("System", "Health and liveness probes."),
  ];

  // Group every tag under a single "New AI" heading in the merged reference
  // (a Redocly/Scalar extension; harmless for the standalone document).
  const tagGroup: Json = {
    name: TAG_GROUP,
    tags: (tags as Array<{ name: string }>).map((t) => t.name),
  };

  // Shared `{baseUrl}` server template (default empty = same origin),
  // matching the .NET service documents. Paths already carry the full
  // `/api/2.0/new-ai/...` route, so requests resolve to the proxied URL.
  const servers: Json = [
    {
      url: "{baseUrl}",
      description: "Server configuration",
      variables: { baseUrl: { default: "", description: "Default URL" } },
    },
  ];

  return {
    openapi: "3.0.3",
    info: {
      title: "ONLYOFFICE DocSpace New AI Service API",
      version: "2.0",
      description:
        "HTTP API of the New AI service. Requests are authenticated with the "
        + "DocSpace `asc_auth_key` session cookie or an `Authorization` header "
        + "and forwarded to the AI engine and the .NET AI integration service.",
    },
    servers,
    // Applied to every operation unless overridden (health probes clear it).
    security: [{ cookieAuth: [] }, { bearerAuth: [] }],
    tags,
    "x-tagGroups": [tagGroup],
    paths,
    components: {
      securitySchemes: {
        cookieAuth: {
          type: "apiKey",
          in: "cookie",
          name: "asc_auth_key",
          description: "DocSpace session cookie sent by the browser.",
        },
        bearerAuth: {
          type: "http",
          scheme: "bearer",
          description: "Bearer token or API key for programmatic callers.",
        },
      },
    },
  };
}

// Self-contained HTML for the Scalar API reference UI. The DocSpace .NET
// services expose their docs through Scalar too, so this keeps the New AI
// service consistent. The script is loaded from the CDN; `specUrl` is the
// same-origin path to the generated document.
export function docsHtml(specUrl: string): string {
  return `<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>New AI Service API</title>
  </head>
  <body>
    <script id="api-reference" data-url="${specUrl}"></script>
    <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
  </body>
</html>
`;
}
