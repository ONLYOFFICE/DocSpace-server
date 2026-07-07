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

process.env["NODE_ENV"] = process.env["NODE_ENV"] ?? "development";

import http from "http";
import express from "express";
import morgan from "morgan";
import cookieParser from "cookie-parser";
import bodyParser from "body-parser";
import cors from "cors";
import logger, { logStream } from "./app/log.js";
import { getAppConfig } from "./config/index.js";
import registerRoutes from "./app/routes.js";
import { requestContextMiddleware } from "./app/requestContext.js";
import { storage } from "./app/storage/index.js";

const config = getAppConfig();

await storage.init();
logger.info("Storage initialized");

const app = express();

// CORS is off by default: the chat UI reaches this service same-origin via
// the DocSpace nginx (`/api/2.0/new-ai`), so no cross-origin request is
// expected. A blanket `cors()` would emit `Access-Control-Allow-Origin: *`
// on an authenticated, user-scoped API — undesirable. Set
// `NEW_AI_CORS_ORIGINS` (comma-separated) only if a real cross-origin
// caller exists; an explicit allowlist is then honored with credentials.
const corsOrigins = (process.env["NEW_AI_CORS_ORIGINS"] ?? "")
  .split(",")
  .map((o) => o.trim())
  .filter(Boolean);

app
  .use(morgan("combined", { stream: logStream }))
  .use(cookieParser())
  // strict:false lets bare JSON primitives through; @onlyoffice/ai-chat's
  // ApiProvider serializes single-arg routes as `JSON.stringify(arg)` — e.g.
  // `DELETE profiles/delete` arrives with body `"uuid"`, which strict-mode
  // would reject. Handlers normalize via `unpackPositional` afterwards.
  .use(bodyParser.json({ strict: false }))
  .use(bodyParser.urlencoded({ extended: false }));

if (corsOrigins.length > 0) {
  app.use(cors({ origin: corsOrigins, credentials: true }));
  logger.info(`CORS enabled for origins: ${corsOrigins.join(", ")}`);
}

app.use(requestContextMiddleware);

registerRoutes(app);

const httpServer = http.createServer(app);

httpServer.listen(config.port, () => {
  logger.info(
    `Start NewAi Service listening on port ${config.port} `
      + `appsettings path='${config.appsettings}'`,
  );
});
