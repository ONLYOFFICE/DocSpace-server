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

import { createRequire } from "node:module";
import { describe, expect, it, vi } from "vitest";
import { openApiDocument, publishedOperations } from "./helpers/schema.js";

// The routes the app registers against the document it publishes about them. The app and the
// document are built from the same catalog (`apiCatalog.ts`); these tests catch a route
// registered in `routes.ts` without a `CUSTOM_ROUTE_DOCS` entry, and the reverse.

const { createApp } = await import("../createApp.js");
const { API_PREFIX, CUSTOM_ROUTE_DOCS, CUSTOM_TAG_DOCS, ENGINE_DOCS } =
  await import("../apiCatalog.js");
vi.unstubAllEnvs();

interface Layer {
  name: string;
  regexp: RegExp;
  route?: { path: string; methods: Record<string, boolean> };
  handle: { stack?: Layer[] };
}

const app = createApp() as unknown as { _router: { stack: Layer[] } };
const apiRouter = app._router.stack.find((l) => l.name === "router");
if (!apiRouter?.handle.stack) {
  throw new Error("the API router is not mounted");
}

const toOpenApi = (path: string): string => path.replace(/:([A-Za-z]+)/g, "{$1}");
const routesOf = (stack: Layer[]): string[] =>
  stack.flatMap(({ route }) =>
    route
      ? Object.keys(route.methods).map((m) => `${m.toUpperCase()} ${toOpenApi(route.path)}`)
      : [],
  );

const OPEN_ROUTES = ["GET /isLife", "GET /health", "GET /openapi.json", "GET /docs"];
const registered = routesOf(apiRouter.handle.stack);
const documented = publishedOperations().map(
  (op) => `${op.method.toUpperCase()} ${op.path.slice(API_PREFIX.length)}`,
);
const engineRoutes = ENGINE_DOCS.flatMap((e) =>
  Object.values(e.routes).map((r) => `${r.method.toUpperCase()} /${r.path}`),
);

describe("the API router and the OpenAPI document", () => {
  it("mounts the API router under the prefix, case-insensitively", () => {
    expect(apiRouter.regexp.source).toBe("^\\/api\\/2\\.0\\/ai\\/?(?=\\/|$)");
    expect(apiRouter.regexp.flags).toBe("i");
  });

  it("registers the open routes first, then the gate and the no-store middleware", () => {
    const stack = apiRouter.handle.stack ?? [];
    const order = stack.slice(0, 6).map((l) => (l.route ? routesOf([l])[0] : "middleware"));

    expect(order).toEqual([...OPEN_ROUTES, "middleware", "middleware"]);
    expect(stack.slice(6).every((l) => l.route !== undefined)).toBe(true);
  });

  it("registers each route once", () => {
    expect(new Set(registered).size).toBe(registered.length);
  });

  it("documents every registered route except the open ones", () => {
    const undocumented = registered.filter(
      (r) => !documented.includes(r) && !OPEN_ROUTES.includes(r),
    );

    expect(undocumented).toEqual([]);
  });

  it("registers every documented operation", () => {
    expect(documented.filter((d) => !registered.includes(d))).toEqual([]);
  });

  it("keeps the open routes out of the document", () => {
    expect(documented.filter((d) => OPEN_ROUTES.includes(d))).toEqual([]);
  });

  it("publishes each operationId once", () => {
    const ids = Object.values(openApiDocument["paths"] as Record<string, Record<string, unknown>>)
      .flatMap((item) => Object.values(item))
      .map((op) => (op as { operationId?: unknown }).operationId);

    expect(ids).toHaveLength(116);
    expect(new Set(ids).size).toBe(ids.length);
    expect(ids.every((id) => typeof id === "string" && /^ai[A-Z]/.test(id))).toBe(true);
  });

  it("registers exactly CUSTOM_ROUTE_DOCS besides the engine routes", () => {
    const custom = registered.filter((r) => !engineRoutes.includes(r) && !OPEN_ROUTES.includes(r));
    const declared = CUSTOM_ROUTE_DOCS.map((d) => `${d.method} ${d.path}`);

    expect(custom).toEqual(declared);
  });

  it("names every path parameter of a custom route in pathParams", () => {
    for (const doc of CUSTOM_ROUTE_DOCS) {
      const inPath = [...doc.path.matchAll(/\{([^}]+)\}/g)].map((m) => m[1]);
      expect([doc.operationId, inPath]).toEqual([doc.operationId, doc.pathParams ?? []]);
    }
  });

  it("describes every tag of a custom route in CUSTOM_TAG_DOCS", () => {
    const engineTags = new Set(ENGINE_DOCS.map((e) => e.tag));
    const customTags = [...new Set(CUSTOM_ROUTE_DOCS.map((d) => d.tag))].filter(
      (t) => !engineTags.has(t),
    );

    expect(customTags.sort()).toEqual(Object.keys(CUSTOM_TAG_DOCS).sort());
  });

  it("registers the document agent's file routes outside the prefix and the document", () => {
    const appRoutes = routesOf(app._router.stack);

    expect(appRoutes).toContain("POST /document-agent/files/outputs");
    // `app.all`: every method Node knows, so the agent's MCP transport may use any of them.
    expect(appRoutes).toEqual(
      expect.arrayContaining([
        "GET /document-agent/files/mcp",
        "POST /document-agent/files/mcp",
        "DELETE /document-agent/files/mcp",
      ]),
    );
    expect(appRoutes.slice(0, 2)).toEqual(["GET /isLife", "GET /health"]);
  });
});

describe("the generated schema bundle", () => {
  const bundle = createRequire(import.meta.url)("../generated/openapi-schemas.json") as {
    operations: Record<string, unknown>;
  };
  const published = new Set(publishedOperations().map((op) => op.operationId));

  // DEFECT app-8: app/generated/openapi-schemas.json keeps request/response schemas for four
  // operations the service does not publish (the unmounted save-image routes, Bug 83289, and two
  // `aiProjects*` operations no route serves) — expected every key of `operations` to name a
  // published operationId; the file is written by `yarn openapi` from scripts/schema/schemaTypes.ts
  // (ts-ai-style.md, Checks).
  it("carries schemas for operations nobody publishes", () => {
    expect(Object.keys(bundle.operations).filter((id) => !published.has(id))).toEqual([
      "aiAttachmentsSaveImage",
      "aiAttachmentsSaveImagesMany",
      "aiProjectsSetModel",
      "aiProjectsGetModel",
    ]);
  });
});

describe("the engine catalog", () => {
  it("lists the ten engines in the order the router binds them", () => {
    expect(ENGINE_DOCS.map((e) => [e.name, e.tag])).toEqual([
      ["ai", "AI"],
      ["assignments", "Assignments"],
      ["attachments", "Attachments"],
      ["context", "Context"],
      ["preferences", "Preferences"],
      ["profiles", "Profiles"],
      ["prompts", "Prompts"],
      ["threads", "Threads"],
      ["tools", "Tools"],
      ["webSearch", "Web search"],
    ]);
  });

  it("serves no save-image route and adds the suggested-questions long-poll (Bug 83289)", () => {
    const attachments = ENGINE_DOCS.find((e) => e.name === "attachments")?.routes ?? {};

    expect(Object.keys(attachments)).not.toContain("saveImage");
    expect(Object.keys(attachments)).not.toContain("saveImagesMany");
    expect(attachments["getSuggestedQuestions"]).toEqual({
      method: "POST",
      path: "attachments/suggested-questions",
    });
  });

  it("has the prefix the portal's proxy routes to the service", () => {
    expect(API_PREFIX).toBe("/api/2.0/ai");
  });
});
