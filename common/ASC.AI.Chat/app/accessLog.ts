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

import morgan from "morgan";
import type { RequestHandler } from "express";
import type { IncomingMessage } from "node:http";
import { logStream } from "./log.js";

// The caller's address, read when the request arrives. morgan writes its line when the answer
// is finished, and by then the socket of a request that was answered before its body was read
// is gone: reading `req.ip` at that moment throws outside any handler.
const addresses = new WeakMap<IncomingMessage, string | undefined>();

morgan.token("client-addr", (req) => addresses.get(req));

// morgan's `combined`, with the address taken from the map above instead of `:remote-addr`.
const ACCESS_LOG_FORMAT =
  ':client-addr - :remote-user [:date[clf]] ":method :url HTTP/:http-version" :status :res[content-length] ":referrer" ":user-agent"';

/** The access log: one line per answered request, in the Apache combined format. */
export function accessLog(): RequestHandler[] {
  return [
    (req, _res, next) => {
      addresses.set(req, req.ip);
      next();
    },
    morgan(ACCESS_LOG_FORMAT, { stream: logStream }),
  ];
}
