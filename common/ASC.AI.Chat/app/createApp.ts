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
import type { Express } from "express";
import cookieParser from "cookie-parser";
import bodyParser from "body-parser";
import cors from "cors";
import logger from "./log.js";
import { accessLog } from "./accessLog.js";
import { coreCors } from "../config/index.js";
import { corsOptions } from "./cors.js";
import registerRoutes, { API_PREFIX } from "./routes.js";
import { requestContextMiddleware } from "./requestContext.js";
import { errorHandler } from "./controllers/_helpers.js";
import { DOCUMENT_AGENT_OUTPUTS_PATH } from "./tools/documentAgentFiles.js";

/**
 * The Express application with its middleware and routes, not yet listening. `app.ts` starts
 * it; a test hands it to supertest. Nothing here opens a port, starts telemetry or reads the
 * storage, so building it has no effect outside the process.
 */
export function createApp(): Express {
  const app = express();
  app.disable("x-powered-by");

  // Server-Timing header so DevTools can tell server time from network time;
  // set at header flush, so for streamed replies it is time-to-first-byte.
  app.use((_req, res, next) => {
    const startedAt = process.hrtime.bigint();
    const writeHead = res.writeHead.bind(res);
    res.writeHead = ((...args: Parameters<typeof writeHead>) => {
      if (!res.headersSent) {
        const elapsedMs = (process.hrtime.bigint() - startedAt) / 1_000_000n;
        res.setHeader("Server-Timing", `express-request-time;dur=${elapsedMs}ms`);
      }
      return writeHead(...args);
    }) as typeof res.writeHead;
    next();
  });

  // strict:false lets bare JSON primitives through; @onlyoffice/ai-chat's
  // ApiProvider serializes single-arg routes as `JSON.stringify(arg)` — e.g.
  // `DELETE profiles/delete` arrives with body `"uuid"`, which strict-mode
  // would reject. Handlers normalize via `unpackPositional` afterwards.
  const jsonParser = bodyParser.json({ strict: false });

  // The OpenAI passthrough (`openaiPassthroughController`) forwards the raw
  // request bytes to the provider, so its body must stay unparsed — both to
  // avoid a lossy parse/re-serialize hop and because vision/OCR payloads
  // (base64 data URLs) easily exceed the parser's 100kb default limit.
  const OPENAI_PASSTHROUGH_PREFIX = `${API_PREFIX}/openai/`;

  // The docx export carries a whole thread transcript in one request, so a
  // long thread blows past the parser's 100kb default and 413s before the
  // handler runs (Bug 83096). User messages are capped by the default parser
  // on their own routes, so only assistant replies grow a transcript — 15mb
  // covers even extreme threads. The downstream .NET hop accepts ~28mb
  // (Kestrel default); a front proxy's own body cap still applies before us.
  const TEXT_TO_DOCX_PATH = `${API_PREFIX}/text-to-docx`;
  const docxJsonParser = bodyParser.json({ strict: false, limit: "15mb" });

  // The document agent delivers result files as raw bodies of their own media
  // type (see documentAgentFiles.ts receive); a .json file would otherwise be
  // parsed here and the handler left with no bytes to save.

  app
    .use(accessLog())
    .use(cookieParser())
    .use((req, res, next) => {
      // The path as the router compares it: without regard to case, and with or without one
      // trailing slash. Otherwise another spelling of a route gets another parser.
      const path = req.path.toLowerCase();
      const names = (route: string): boolean =>
        path === route.toLowerCase() || path === `${route.toLowerCase()}/`;
      if (path.startsWith(OPENAI_PASSTHROUGH_PREFIX) || names(DOCUMENT_AGENT_OUTPUTS_PATH)) {
        next();
        return;
      }
      if (names(TEXT_TO_DOCX_PATH)) {
        docxJsonParser(req, res, next);
        return;
      }
      jsonParser(req, res, next);
    })
    .use(bodyParser.urlencoded({ extended: false }));

  // CORS as the .NET services have it, from `core:cors` (see `app/cors.ts`).
  // Before the routes and their auth gate: a preflight carries no
  // credentials, and `cors()` answers it here and ends it.
  const corsPolicy = corsOptions(coreCors());
  if (corsPolicy) {
    app.use(cors(corsPolicy));
  }
  logger.info(corsPolicy ? `CORS enabled for ${coreCors()}` : "CORS disabled");

  app.use(requestContextMiddleware);

  registerRoutes(app);

  // Last: what was raised before a handler ran (a body that does not parse, one over the limit).
  app.use(errorHandler);

  return app;
}
