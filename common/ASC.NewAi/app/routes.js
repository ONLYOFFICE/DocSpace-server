// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import express from "express";
import {
  DEFAULT_AI_ROUTES,
  DEFAULT_ASSIGNMENTS_ROUTES,
  DEFAULT_PREFERENCES_ROUTES,
  DEFAULT_PROFILES_ROUTES,
  DEFAULT_PROMPTS_ROUTES,
  DEFAULT_THREADS_ROUTES,
  DEFAULT_TOOLS_ROUTES,
  DEFAULT_WEB_SEARCH_ROUTES,
} from "@onlyoffice/ai-chat/core";
import logger from "./log.js";
import { aiController } from "./controllers/aiController.js";
import { assignmentsController } from "./controllers/assignmentsController.js";
import { preferencesController } from "./controllers/preferencesController.js";
import { profilesController } from "./controllers/profilesController.js";
import { promptsController } from "./controllers/promptsController.js";
import { threadsController } from "./controllers/threadsController.js";
import { toolsController } from "./controllers/toolsController.js";
import { webSearchController } from "./controllers/webSearchController.js";

export const API_PREFIX = "/api/2.0/new-ai";

const ENGINE_BINDINGS = [
  { name: "ai", routes: DEFAULT_AI_ROUTES, controller: aiController },
  { name: "assignments", routes: DEFAULT_ASSIGNMENTS_ROUTES, controller: assignmentsController },
  { name: "preferences", routes: DEFAULT_PREFERENCES_ROUTES, controller: preferencesController },
  { name: "profiles", routes: DEFAULT_PROFILES_ROUTES, controller: profilesController },
  { name: "prompts", routes: DEFAULT_PROMPTS_ROUTES, controller: promptsController },
  { name: "threads", routes: DEFAULT_THREADS_ROUTES, controller: threadsController },
  { name: "tools", routes: DEFAULT_TOOLS_ROUTES, controller: toolsController },
  { name: "webSearch", routes: DEFAULT_WEB_SEARCH_ROUTES, controller: webSearchController },
];

function bindEngine(router, { name, routes, controller }) {
  for (const [methodName, route] of Object.entries(routes)) {
    const handler = controller[methodName];
    if (typeof handler !== "function") {
      throw new Error(`Missing handler ${name}.${methodName} for ${route.method} ${route.path}`);
    }
    const verb = route.method.toLowerCase();
    router[verb](`/${route.path}`, handler.bind(controller));
  }
}

export default function registerRoutes(app, config) {
  app.get("/isLife", (req, res) => res.sendStatus(200));
  app.get("/health", (req, res) => res.status(200).json({ status: "Healthy" }));

  const router = express.Router();

  router.get("/isLife", (req, res) => res.sendStatus(200));
  router.get("/health", (req, res) => res.status(200).json({ status: "Healthy" }));

  let total = 0;
  for (const binding of ENGINE_BINDINGS) {
    bindEngine(router, binding);
    total += Object.keys(binding.routes).length;
  }
  logger.info(`Registered ${total} engine routes across ${ENGINE_BINDINGS.length} engines under ${API_PREFIX}`);

  app.use(API_PREFIX, router);

  app.use((req, res) => {
    logger.warn(`Route not found: ${req.method} ${req.originalUrl}`);
    res.sendStatus(404);
  });
}
