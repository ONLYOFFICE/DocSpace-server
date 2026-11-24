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

import request from "supertest";
import { describe, expect, it, vi } from "vitest";
import type { RouteSpec } from "@onlyoffice/ai-chat/core";
import { buildOpenApiDocument, docsHtml } from "../openapi.js";
import type { CustomRouteDoc, EngineDoc, OpenApiOptions } from "../openapi.js";
import { EXAMPLE_AT_ISO, EXAMPLE_AT_MS, EXAMPLE_IDS } from "../exampleIds.js";
import { openApiDocument, responseViolations } from "./helpers/schema.js";

type Json = Record<string, unknown>;

const { createApp } = await import("../createApp.js");
const servedApp = createApp();
vi.unstubAllEnvs();

const engine = (name: string, routes: Record<string, RouteSpec>): EngineDoc => ({
  name,
  tag: `Tag ${name}`,
  description: `About ${name}`,
  routes,
});

const custom = (doc: Partial<CustomRouteDoc> & Pick<CustomRouteDoc, "path" | "operationId">) =>
  ({ method: "POST", tag: "Extra", summary: "Do it", ...doc }) as CustomRouteDoc;

const build = (options: Partial<OpenApiOptions>): Json =>
  buildOpenApiDocument({ apiPrefix: "/p", engines: [], customRoutes: [], ...options }) as Json;

const pathsOf = (doc: Json) => doc["paths"] as Record<string, Record<string, Json>>;

const ERROR_REF = { $ref: "#/components/schemas/AiErrorResponse" };
const SUCCESS_REF = { $ref: "#/components/schemas/AiSuccessResponse" };
const json = (description: string, schema: unknown) => ({
  description,
  content: { "application/json": { schema } },
});
const UNAUTHORIZED = json("Missing `asc_auth_key` cookie or `Authorization` header.", ERROR_REF);
const FORBIDDEN = json(
  "AI is disabled for this portal, the caller is a guest, or the room named by `entityId` is one " +
    "the caller cannot open. Relayed from the DocSpace AI service or the Files API.",
  ERROR_REF,
);
const FAILED = json(
  "Unhandled failure. The reason is logged server-side and never echoed back.",
  ERROR_REF,
);
const BAD_GATEWAY = json(
  "The AI provider could not be reached or answered with a failure of its own, or a DocSpace " +
    "service answered with a body this service cannot read.",
  ERROR_REF,
);
const UNAVAILABLE = json(
  "The DocSpace AI service or the Files API is unavailable; its status is relayed as it came.",
  ERROR_REF,
);
// The line this service writes into a chat stream that failed: declared by the document itself.
const STREAM_ERROR_EVENT = {
  type: "object",
  properties: {
    type: {
      type: "string",
      const: "error",
      description: "Marks the line as the failure of the stream.",
    },
    message: {
      type: "string",
      description: "A generic text; it never carries the cause.",
      examples: ["stream error"],
    },
  },
  required: ["type", "message"],
  additionalProperties: false,
};
const BAD_REQUEST = json(
  "The request body or query is malformed, or a required value is missing.",
  ERROR_REF,
);
const TOO_LARGE = {
  description: "The request body is larger than 100 KB, the JSON parser's limit on this route.",
  content: { "text/plain": { schema: { type: "string", example: "Payload Too Large" } } },
};

describe("buildOpenApiDocument with no routes", () => {
  it("emits the document frame", () => {
    expect(build({})).toEqual({
      openapi: "3.1.1",
      info: {
        title: "ONLYOFFICE DocSpace AI Service API",
        version: "2.0",
        description:
          "HTTP API of the AI service. Requests are authenticated with the " +
          "DocSpace `asc_auth_key` session cookie or an `Authorization` header " +
          "and forwarded to the AI engine and the .NET AI integration service.",
        contact: {
          name: "API Support",
          email: "support@onlyoffice.com",
          url: "https://helpdesk.onlyoffice.com/hc/en-us",
        },
        license: { name: "AGPL-3.0-only", url: "https://www.gnu.org/licenses/agpl-3.0.html" },
      },
      servers: [
        {
          url: "{baseUrl}",
          description: "The production DocSpace portal, at the customer's own domain.",
          variables: { baseUrl: { default: "", description: "Default URL" } },
        },
      ],
      security: [{ cookieAuth: [] }, { bearerAuth: [] }],
      tags: [],
      "x-tagGroups": [{ name: "AI", tags: [] }],
      paths: {},
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
        schemas: { AiStreamErrorEvent: STREAM_ERROR_EVENT },
      },
    });
  });
});

describe("an engine operation", () => {
  it("is a GET with query parameters, typed and marked optional by name", () => {
    const doc = build({
      engines: [
        engine("widgets", {
          listAll: { method: "GET", path: "widgets/list-all", params: ["limit", "flavour"] },
        }),
      ],
    });

    expect(pathsOf(doc)["/p/widgets/list-all"]).toEqual({
      get: {
        tags: ["AI / Tag widgets"],
        operationId: "aiWidgetsListAll",
        summary: "List all",
        responses: {
          "200": json("Success.", SUCCESS_REF),
          "401": UNAUTHORIZED,
          "403": FORBIDDEN,
          "500": FAILED,
          "502": BAD_GATEWAY,
          "503": UNAVAILABLE,
        },
        parameters: [
          {
            name: "limit",
            in: "query",
            description: "The maximum number of items to return.",
            required: false,
            schema: { type: "integer", examples: [20] },
          },
          { name: "flavour", in: "query", required: true, schema: { type: "string" } },
        ],
      },
    });
  });

  it("has no parameters for a GET with an empty params list", () => {
    const doc = build({
      engines: [engine("widgets", { list: { method: "GET", path: "w", params: [] } })],
    });

    expect(pathsOf(doc)["/p/w"]?.["get"]).not.toHaveProperty("parameters");
  });

  it("takes a free-form JSON body and declares the 400 and 413 of the parser for a non-GET", () => {
    const doc = build({
      engines: [engine("widgets", { make_one: { method: "POST", path: "w/make" } })],
    });

    expect(pathsOf(doc)["/p/w/make"]?.["post"]).toEqual({
      tags: ["AI / Tag widgets"],
      operationId: "aiWidgetsMake_one",
      summary: "Make one",
      responses: {
        "200": json("Success.", SUCCESS_REF),
        "400": BAD_REQUEST,
        "401": UNAUTHORIZED,
        "403": FORBIDDEN,
        "413": TOO_LARGE,
        "500": FAILED,
        "502": BAD_GATEWAY,
        "503": UNAVAILABLE,
      },
      requestBody: {
        required: true,
        content: { "application/json": { schema: { type: "object", additionalProperties: true } } },
        "x-codegen-request-body-name": "AiWidgetsMake_oneRequest",
      },
    });
  });

  it("scopes the ai engine's ids without repeating it, and keeps an empty method name", () => {
    const doc = build({
      engines: [
        engine("ai", { ping: { method: "POST", path: "ai/ping" } }),
        engine("x", { "": { method: "DELETE", path: "x" } }),
      ],
    });

    expect(pathsOf(doc)["/p/ai/ping"]?.["post"]?.["operationId"]).toBe("aiPing");
    expect(pathsOf(doc)["/p/x"]?.["delete"]?.["operationId"]).toBe("aiX");
    expect(pathsOf(doc)["/p/x"]?.["delete"]?.["summary"]).toBe("");
  });

  it("merges two methods on one path into one path item", () => {
    const doc = build({
      engines: [
        engine("w", {
          read: { method: "GET", path: "w/item" },
          write: { method: "PUT", path: "w/item" },
        }),
      ],
    });

    expect(Object.keys(pathsOf(doc)["/p/w/item"] ?? {})).toEqual(["get", "put"]);
  });

  it("lets a later route of the same path and method replace the earlier one", () => {
    const doc = build({
      engines: [
        engine("w", {
          first: { method: "GET", path: "w/item" },
          second: { method: "GET", path: "w/item" },
        }),
      ],
    });

    expect(pathsOf(doc)["/p/w/item"]?.["get"]?.["operationId"]).toBe("aiWSecond");
  });
});

describe("request bodies from the schema bundle", () => {
  const withRequest = (request: unknown) =>
    build({
      engines: [engine("w", { save: { method: "POST", path: "w/save" } })],
      schemas: { operations: { aiWSave: { request } } },
    });

  it("promotes a body with properties to the <OperationId>Request component", () => {
    const schema = { type: "object", properties: { a: { type: "string" } } };
    const doc = withRequest(schema);

    expect(pathsOf(doc)["/p/w/save"]?.["post"]?.["requestBody"]).toEqual({
      required: true,
      content: { "application/json": { schema: { $ref: "#/components/schemas/AiWSaveRequest" } } },
    });
    expect((doc["components"] as Json)["schemas"]).toEqual({
      AiWSaveRequest: schema,
      AiStreamErrorEvent: STREAM_ERROR_EVENT,
    });
  });

  it("names a $ref body after the referenced type", () => {
    const doc = withRequest({ $ref: "#/components/schemas/AiThing" });

    expect(pathsOf(doc)["/p/w/save"]?.["post"]?.["requestBody"]).toEqual({
      required: true,
      content: { "application/json": { schema: { $ref: "#/components/schemas/AiThing" } } },
      "x-codegen-request-body-name": "AiThing",
    });
  });

  it("keeps a bare-string body inline under the operation's name", () => {
    const doc = withRequest({ type: "string" });

    expect(pathsOf(doc)["/p/w/save"]?.["post"]?.["requestBody"]).toEqual({
      required: true,
      content: { "application/json": { schema: { type: "string" } } },
      "x-codegen-request-body-name": "AiWSaveRequest",
    });
  });

  it("keeps a non-object schema inline without a name", () => {
    const doc = withRequest(["not", "a", "schema"]);

    expect(pathsOf(doc)["/p/w/save"]?.["post"]?.["requestBody"]).toEqual({
      required: true,
      content: { "application/json": { schema: ["not", "a", "schema"] } },
    });
  });

  it("refuses a promoted body whose name a generated schema already has", () => {
    expect(() =>
      build({
        engines: [engine("w", { save: { method: "POST", path: "w/save" } })],
        schemas: {
          components: { AiWSaveRequest: {} },
          operations: { aiWSave: { request: { type: "object", properties: { a: {} } } } },
        },
      }),
    ).toThrow('Request body component "AiWSaveRequest" collides with a generated schema.');
  });

  it("inlines a generated response schema and keeps shared components", () => {
    const doc = build({
      engines: [engine("w", { get: { method: "GET", path: "w/get" } })],
      schemas: {
        components: { AiShared: { type: "string" } },
        operations: { aiWGet: { response: { type: "number" } } },
      },
    });

    expect(pathsOf(doc)["/p/w/get"]?.["get"]?.["responses"]).toMatchObject({
      "200": json("Success.", { type: "number" }),
    });
    expect((doc["components"] as Json)["schemas"]).toEqual({
      AiShared: { type: "string" },
      AiStreamErrorEvent: STREAM_ERROR_EVENT,
    });
  });
});

describe("a custom operation", () => {
  it("carries its path parameters and no body unless it says so", () => {
    const doc = build({
      customRoutes: [
        custom({
          method: "DELETE",
          path: "/things/{thingId}",
          operationId: "aiThingDelete",
          pathParams: ["thingId"],
        }),
      ],
      customTagDescriptions: { Extra: "Extra things." },
    });

    expect(pathsOf(doc)["/p/things/{thingId}"]?.["delete"]).toEqual({
      tags: ["AI / Extra"],
      operationId: "aiThingDelete",
      summary: "Do it",
      responses: {
        "200": json("Success.", SUCCESS_REF),
        "401": UNAUTHORIZED,
        "403": FORBIDDEN,
        "500": FAILED,
        "502": BAD_GATEWAY,
        "503": UNAVAILABLE,
      },
      parameters: [{ name: "thingId", in: "path", required: true, schema: { type: "string" } }],
    });
    expect(doc["tags"]).toEqual([
      { name: "AI / Extra", description: "Extra things.", "x-displayName": "Extra" },
    ]);
  });

  it("has no parameters for an empty pathParams list, and a body when hasBody is set", () => {
    const doc = build({
      customRoutes: [
        custom({ path: "/things", operationId: "aiThingMake", pathParams: [], hasBody: true }),
      ],
    });
    const operation = pathsOf(doc)["/p/things"]?.["post"];

    expect(operation).not.toHaveProperty("parameters");
    expect(operation?.["requestBody"]).toEqual({
      required: true,
      content: { "application/json": { schema: { type: "object", additionalProperties: true } } },
      "x-codegen-request-body-name": "AiThingMakeRequest",
    });
  });

  it("declares a custom tag without a description with a generated one", () => {
    const doc = build({ customRoutes: [custom({ path: "/a", operationId: "aiA", tag: "Zeta" })] });

    expect(doc["tags"]).toEqual([
      { name: "AI / Zeta", description: "Zeta operations.", "x-displayName": "Zeta" },
    ]);
  });

  it("does not declare a tag twice when an engine owns it", () => {
    const doc = build({
      engines: [engine("w", {})],
      customRoutes: [custom({ path: "/a", operationId: "aiA", tag: "Tag w" })],
    });

    expect(doc["tags"]).toEqual([
      { name: "AI / Tag w", description: "About w", "x-displayName": "Tag w" },
    ]);
  });

  it("sorts the tags by name in the en collation and groups them under AI", () => {
    const doc = build({
      engines: [engine("b", {}), engine("a", {})],
      customRoutes: [custom({ path: "/a", operationId: "aiA", tag: "AI" })],
    });

    expect(doc["x-tagGroups"]).toEqual([
      { name: "AI", tags: ["AI / AI", "AI / Tag a", "AI / Tag b"] },
    ]);
  });
});

describe("the served document", () => {
  it("is the document the helpers validate against", async () => {
    const reply = await request(servedApp).get("/api/2.0/ai/openapi.json");

    expect(reply.body).toEqual(JSON.parse(JSON.stringify(openApiDocument)));
  });

  it("declares the 401 of the gate on every operation", () => {
    const operations = Object.values(pathsOf(openApiDocument)).flatMap((item) =>
      Object.values(item),
    );

    expect(operations).toHaveLength(116);
    for (const operation of operations) {
      expect((operation["responses"] as Json)["401"]).toEqual(UNAUTHORIZED);
    }
  });

  // The parser's refusal is answered by `errorHandler` before any handler runs.
  const PARSER_REFUSAL = { error: "The request body is not valid JSON" };
  const UNPARSED = ["aiOpenaiChatCompletions", "aiOpenaiImagesGenerations"];
  const bodyOperations = Object.values(pathsOf(openApiDocument))
    .flatMap((item) => Object.values(item))
    .filter((operation) => "requestBody" in operation)
    .map((operation) => String(operation["operationId"]));

  it("publishes the parser's 400 on every operation whose body is parsed", () => {
    const parsed = bodyOperations.filter((id) => !UNPARSED.includes(id));

    expect(parsed.length).toBeGreaterThan(50);
    for (const operationId of parsed) {
      expect([operationId, responseViolations(operationId, 400, PARSER_REFUSAL)]).toEqual([
        operationId,
        [],
      ]);
    }
  });

  const badRequestOf = (operationId: string): Json =>
    (
      Object.values(pathsOf(openApiDocument))
        .flatMap((item) => Object.values(item))
        .find((operation) => operation["operationId"] === operationId)?.["responses"] as Json
    )["400"] as Json;

  it("names the parser's 400 in an operation's own 400 text, and not where the body is relayed raw", () => {
    expect(badRequestOf("aiAssignmentsAssign")["description"]).toBe(
      "`actionType` or `profileId` is missing. A body that is not valid JSON is refused with 400 as well.",
    );
    expect(badRequestOf("aiOpenaiChatCompletions")["description"]).toBe(
      "Relayed from the provider, which refused the request body.",
    );
  });

  it("admits the plain error beside an operation's own 400 envelope, once", () => {
    expect(badRequestOf("aiPromptsCreate")["content"]).toEqual({
      "application/json": {
        schema: { anyOf: [{ $ref: "#/components/schemas/AiPromptMutationResult" }, ERROR_REF] },
      },
    });
    expect(badRequestOf("aiProfilesCreate")["content"]).toEqual({
      "application/json": {
        schema: { anyOf: [{ $ref: "#/components/schemas/AiProfileMutationResult" }, ERROR_REF] },
      },
    });
  });

  const responseOf = (operationId: string, status: string): Json =>
    (
      Object.values(pathsOf(openApiDocument))
        .flatMap((item) => Object.values(item))
        .find((operation) => operation["operationId"] === operationId)?.["responses"] as Json
    )[status] as Json;

  it("keeps the parser's plain-text 413 beside an operation's own JSON 413", () => {
    const content = responseOf("aiTemplatesInstall", "413")["content"] as Json;

    expect(Object.keys(content)).toEqual(["text/plain", "application/json"]);
    expect(responseViolations("aiTemplatesInstall", 413, { error: "Payload Too Large" })).toEqual(
      [],
    );
    expect(Object.keys(responseOf("aiSkillsCreate", "413")["content"] as Json)).toEqual([
      "text/plain",
    ]);
  });

  it("publishes the rooms a failed template install made on the statuses it relays", () => {
    const incomplete = {
      error: "Service Unavailable",
      skillSets: [{ templateId: "skill-sets/budgeting", id: "101", title: "Budgeting" }],
      knowledgeBases: [],
      warning: "installIncomplete",
    };
    for (const status of [400, 403, 404, 413, 500, 502, 503]) {
      expect([status, responseViolations("aiTemplatesInstall", status, incomplete)]).toEqual([
        status,
        [],
      ]);
    }
    expect(responseViolations("aiTemplatesInstall", 409, incomplete)).not.toEqual([]);
  });

  it("answers text-to-docx with 202", () => {
    const responses = pathsOf(openApiDocument)["/api/2.0/ai/text-to-docx"]?.["post"]?.["responses"];

    expect(Object.keys(responses as Json)).toEqual([
      "202",
      "400",
      "401",
      "403",
      "413",
      "500",
      "502",
      "503",
    ]);
  });
});

describe("docsHtml", () => {
  it("is the Scalar page reading the given document", () => {
    expect(docsHtml("/x/openapi.json")).toBe(`<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>AI Service API</title>
  </head>
  <body>
    <script id="api-reference" data-url="/x/openapi.json"></script>
    <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
  </body>
</html>
`);
  });

  it("puts the URL into the attribute as it is given", () => {
    expect(docsHtml('a" onload="x')).toContain('data-url="a" onload="x"');
  });
});

describe("the example ids", () => {
  it("are one distinct placeholder per entity", () => {
    expect(EXAMPLE_IDS).toEqual({
      profile: "00000000-0000-0000-0000-000000000000",
      thread: "11111111-1111-1111-1111-111111111111",
      message: "22222222-2222-2222-2222-222222222222",
      prompt: "33333333-3333-3333-3333-333333333333",
      promptFolder: "44444444-4444-4444-4444-444444444444",
      attachment: "55555555-5555-5555-5555-555555555555",
      attachmentSecond: "66666666-6666-6666-6666-666666666666",
      room: "1234",
      project: "1240",
    });
    expect(new Set(Object.values(EXAMPLE_IDS)).size).toBe(9);
  });

  it("give the same instant in milliseconds and in ISO-8601", () => {
    expect(new Date(EXAMPLE_AT_MS).toISOString()).toBe(EXAMPLE_AT_ISO);
  });
});
