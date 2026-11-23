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

import express from "express";
import type { RouteSpec } from "@onlyoffice/ai-chat/core";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { EngineDoc } from "../openapi.js";

// bindEngine is reached through registerRoutes with a catalog this file controls: the engine
// list is the real one, replaced per test by the entries a case needs.

const catalog = vi.hoisted(() => ({ engines: [] as EngineDoc[] }));
vi.mock("../apiCatalog.js", async (original) => ({
  ...(await original<typeof import("../apiCatalog.js")>()),
  ENGINE_DOCS: catalog.engines,
}));

const { default: logger } = await import("../log.js");
const { default: registerRoutes } = await import("../routes.js");

const engine = (name: string, routes: Record<string, RouteSpec>): EngineDoc => ({
  name,
  tag: name,
  description: name,
  routes,
});

const use = (...engines: EngineDoc[]) => {
  catalog.engines.splice(0, catalog.engines.length, ...engines);
};

afterEach(() => {
  use();
});

interface Layer {
  name: string;
  route?: { path: string; methods: Record<string, boolean> };
  handle: { stack?: Layer[] };
}

const boundRoutes = (app: express.Express): string[] => {
  const router = (app as unknown as { _router: { stack: Layer[] } })._router.stack.find(
    (l) => l.name === "router",
  );
  return (router?.handle.stack ?? []).flatMap(({ route }) =>
    route ? Object.keys(route.methods).map((m) => `${m} ${route.path}`) : [],
  );
};

describe("bindEngine", () => {
  it("binds each HTTP method, in any case, to the engine's handler", () => {
    use(
      engine("threads", {
        list: { method: "GET", path: "t/get" },
        create: { method: "post", path: "t/post" },
        rename: { method: "PUT", path: "t/put" },
        touch: { method: "PATCH", path: "t/patch" },
        delete: { method: "Delete", path: "t/delete" },
      } as unknown as Record<string, RouteSpec>),
    );
    const app = express();

    registerRoutes(app);

    expect(boundRoutes(app).slice(-5)).toEqual([
      "get /t/get",
      "post /t/post",
      "put /t/put",
      "patch /t/patch",
      "delete /t/delete",
    ]);
    expect(logger.info).toHaveBeenCalledWith(
      "Registered 5 engine routes across 1 engines under /api/2.0/ai",
    );
  });

  it("refuses an engine without a controller", () => {
    use(engine("nope", { list: { method: "GET", path: "x" } }));

    expect(() => registerRoutes(express())).toThrow("No controller registered for engine nope");
  });

  it("refuses a route whose method has no handler", () => {
    use(engine("ai", { missing: { method: "POST", path: "ai/missing" } }));

    expect(() => registerRoutes(express())).toThrow(
      "Missing handler ai.missing for POST ai/missing",
    );
  });

  it("refuses an HTTP method it cannot bind", () => {
    use(engine("ai", { send: { method: "HEAD", path: "ai/send" } as unknown as RouteSpec }));

    expect(() => registerRoutes(express())).toThrow("Unsupported HTTP method HEAD for ai.send");
  });

  it("registers no engine route for an empty catalog", () => {
    const app = express();

    registerRoutes(app);

    expect(boundRoutes(app)).toHaveLength(31);
    expect(logger.info).toHaveBeenCalledWith(
      "Registered 0 engine routes across 0 engines under /api/2.0/ai",
    );
  });
});
