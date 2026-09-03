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
import type { Application, RequestHandler, Router } from "express";
import logger from "./log.js";
import { buildOpenApiDocument, docsHtml } from "./openapi.js";
import type { EngineDoc, OpenApiSchemaBundle } from "./openapi.js";
import { createRequire } from "module";
import { API_PREFIX, ENGINE_DOCS, CUSTOM_ROUTE_DOCS, CUSTOM_TAG_DOCS } from "./apiCatalog.js";

// Concrete request/response schemas, produced by the build-time generator
// (`yarn openapi`) and committed. Backs the served document with real types;
// if regenerated stale it simply loses precision, never breaks. Loaded via
// `require` to sidestep JSON import-attribute/module constraints under tsx.
const openApiSchemas = createRequire(import.meta.url)(
  "./generated/openapi-schemas.json",
) as OpenApiSchemaBundle;
import { agentsController } from "./controllers/agentsController.js";
import { textToDocxController } from "./controllers/textToDocxController.js";
import { aiController } from "./controllers/aiController.js";
import { assignmentsController } from "./controllers/assignmentsController.js";
import { attachmentsController } from "./controllers/attachmentsController.js";
import { editorToolsController } from "./controllers/editorToolsController.js";
import { openaiPassthroughController } from "./controllers/openaiPassthroughController.js";
import { preferencesController } from "./controllers/preferencesController.js";
import { profilesController } from "./controllers/profilesController.js";
import { promptsController } from "./controllers/promptsController.js";
import { settingsController } from "./controllers/settingsController.js";
import { threadsController } from "./controllers/threadsController.js";
import { toolsController } from "./controllers/toolsController.js";
import { vectorizationController } from "./controllers/vectorizationController.js";
import { webSearchController } from "./controllers/webSearchController.js";
import { webSearchPassthroughController } from "./controllers/webSearchPassthroughController.js";

export { API_PREFIX };

type ControllerMap = Readonly<Record<string, RequestHandler>>;

// Controller per engine, keyed by `EngineDoc.name`. The route/method data
// lives in `apiCatalog.ts` (shared with the OpenAPI emitter); this map is
// the app-only half that binds each engine method to its handler.
const CONTROLLERS: Readonly<Record<string, ControllerMap>> = {
  ai: aiController,
  assignments: assignmentsController,
  attachments: attachmentsController,
  preferences: preferencesController,
  profiles: profilesController,
  prompts: promptsController,
  threads: threadsController,
  tools: toolsController,
  webSearch: webSearchController,
};

function bindEngine(router: Router, binding: EngineDoc): void {
  const { name, routes } = binding;
  const controller = CONTROLLERS[name];
  if (!controller) {
    throw new Error(`No controller registered for engine ${name}`);
  }
  for (const [methodName, route] of Object.entries(routes)) {
    const handler = controller[methodName];
    if (typeof handler !== "function") {
      throw new Error(`Missing handler ${name}.${methodName} for ${route.method} ${route.path}`);
    }
    const verb = route.method.toLowerCase();
    switch (verb) {
      case "get":
        router.get(`/${route.path}`, handler);
        break;
      case "post":
        router.post(`/${route.path}`, handler);
        break;
      case "put":
        router.put(`/${route.path}`, handler);
        break;
      case "patch":
        router.patch(`/${route.path}`, handler);
        break;
      case "delete":
        router.delete(`/${route.path}`, handler);
        break;
      default:
        throw new Error(`Unsupported HTTP method ${route.method} for ${name}.${methodName}`);
    }
  }
}

export default function registerRoutes(app: Application): void {
  app.get("/isLife", (_req, res) => {
    res.sendStatus(200);
  });
  app.get("/health", (_req, res) => {
    res.status(200).json({ status: "Healthy" });
  });

  const router = express.Router();

  router.get("/isLife", (_req, res) => {
    res.sendStatus(200);
  });
  router.get("/health", (_req, res) => {
    res.status(200).json({ status: "Healthy" });
  });

  // OpenAPI document and Scalar docs UI. Built once from the same route maps
  // the router registers below, so the spec cannot drift from the routes.
  // Registered before the auth gate: the document describes only the API
  // shape (no secrets) and the docs UI must be reachable without a session.
  const openApiDocument = buildOpenApiDocument({
    apiPrefix: API_PREFIX,
    engines: ENGINE_DOCS,
    customRoutes: CUSTOM_ROUTE_DOCS,
    customTagDescriptions: CUSTOM_TAG_DOCS,
    schemas: openApiSchemas,
  });
  router.get("/openapi.json", (_req, res) => {
    res.json(openApiDocument);
  });
  router.get("/docs", (_req, res) => {
    res.type("html").send(docsHtml(`${API_PREFIX}/openapi.json`));
  });

  // Auth gate: this service does no auth of its own and blindly forwards the
  // caller's credentials downstream, so an unauthenticated request would
  // reach the engine / .NET integration with no DocSpace session. Reject
  // anything that carries neither the `asc_auth_key` session cookie (what the
  // browser sends) nor an `Authorization` header (Bearer/API-key callers)
  // up front with 401, before any engine work. The actual credential is
  // validated downstream; `httpClient` / MCP forwarding relay both. Health
  // endpoints above stay open.
  router.use((req, res, next) => {
    const cookies = (req as { cookies?: Record<string, unknown> }).cookies;
    const authKey = cookies?.["asc_auth_key"];
    const hasCookie = typeof authKey === "string" && authKey.trim().length > 0;
    const authHeader = req.headers.authorization;
    const hasHeader = typeof authHeader === "string" && authHeader.trim().length > 0;
    if (!hasCookie && !hasHeader) {
      logger.warn(`Unauthenticated request rejected: ${req.method} ${req.originalUrl}`);
      res.status(401).json({ error: "Unauthorized" });
      return;
    }
    next();
  });

  // GET responses are user/entity-scoped and must never be cached by the
  // browser or any intermediate proxy — switching account or `entityId`
  // would otherwise serve a stale snapshot from the previous scope.
  router.use((req, res, next) => {
    if (req.method === "GET") {
      res.setHeader("Cache-Control", "no-store");
    }
    next();
  });

  // Custom routes not backed by an @onlyoffice/ai-chat engine. Agent
  // operations are delegated to the .NET AI service (see agentsController).
  // Literal sub-paths (`news`, `agentquota`, `resetquota`) are registered
  // before the parameterized `/agents/:id` so Express does not capture them
  // as an id.
  // Async markdown → docx export via the .NET AI Worker (see
  // textToDocxController); completion is signalled by the files socket
  // create event, not by this response.
  router.post("/text-to-docx", textToDocxController.start);

  router.get("/agents", agentsController.getAgents);
  router.get("/agents/news", agentsController.getAgentsNews);
  router.get("/agents/:id", agentsController.getAgentInfo);
  router.post("/agents", agentsController.createAgent);
  router.put("/agents/agentquota", agentsController.updateAgentsQuota);
  router.put("/agents/resetquota", agentsController.resetAgentsQuota);
  router.put("/agents/:id", agentsController.updateAgent);
  router.delete("/agents/:id", agentsController.deleteAgent);

  router.get("/config", settingsController.getAiSettings);
  router.get("/config/vectorization", settingsController.getVectorizationSettings);
  router.put("/config/vectorization", settingsController.setVectorizationSettings);
  router.get("/config/user", settingsController.getUserSettings);
  router.put("/config/user", settingsController.setUserSettings);

  router.post("/vectorization/tasks", vectorizationController.startTask);

  // OpenAI-compatible passthrough for the document editor's AI plugin
  // (external-provider transport). Explicit sub-paths only — the allowlist
  // is the registration itself. The request body is raw here: `app.ts`
  // skips the JSON body parser for `/openai/*`.
  router.post(
    "/openai/:profileId/v1/chat/completions",
    openaiPassthroughController.chatCompletions,
  );
  router.post(
    "/openai/:profileId/v1/images/generations",
    openaiPassthroughController.imagesGenerations,
  );

  // DocSpace tools for the editor AI plugin: sanitized catalog of the same
  // composed adapter the chat engine uses, plus server-side execution with
  // the caller's forwarded credentials (see editorToolsController).
  router.get("/editor-tools/list", editorToolsController.list);
  router.post("/editor-tools/call", editorToolsController.call);

  // Web-search passthrough for the editor AI plugin: the plugin holds a
  // placeholder config, the portal's active provider and key are resolved
  // here (see webSearchPassthroughController).
  router.post(
    "/websearch/v1/search",
    webSearchPassthroughController.search,
  );
  router.post(
    "/websearch/v1/contents",
    webSearchPassthroughController.contents,
  );

  let total = 0;
  for (const binding of ENGINE_DOCS) {
    bindEngine(router, binding);
    total += Object.keys(binding.routes).length;
  }
  logger.info(
    `Registered ${total} engine routes across ${ENGINE_DOCS.length} engines under ${API_PREFIX}`,
  );

  app.use(API_PREFIX, router);

  app.use((req, res) => {
    logger.warn(`Route not found: ${req.method} ${req.originalUrl}`);
    res.sendStatus(404);
  });
}
