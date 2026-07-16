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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { aiService } from "../storage/httpClient.js";
import { getForwardedHeaders } from "../requestContext.js";
import { asyncHandler } from "./_helpers.js";
import { isObject } from "../narrow.js";
import logger from "../log.js";

// The .NET publisher captures `CommonLinkUtility.ServerRootPath` as the
// conversion task's BaseUri, and `Request.Url()` (ASC.Common
// HttpRequestExtensions) lets an `?origin=<absolute uri>` query override the
// scheme/host/port it is derived from. Our internal call hits the AI service
// on its internal address (the `Host` header is hop-by-hop and not
// forwarded), so without the override the portal root would resolve from
// the internal host / tenant fallback. Derive the client-facing origin from
// the forwarded headers, the same way the MCP `Referer` is derived in
// systemTools.
function resolvePortalOrigin(): string | undefined {
  const headers = getForwardedHeaders();
  const origin = headers["origin"];
  if (origin) {
    return origin.replace(/\/+$/, "");
  }
  const xfHost = headers["x-forwarded-host"]?.split(",")[0]?.trim();
  if (xfHost) {
    const proto = headers["x-forwarded-proto"]?.split(",")[0]?.trim() || "https";
    return `${proto}://${xfHost}`;
  }
  const referer = headers["referer"];
  if (referer) {
    try {
      const parsed = new URL(referer);
      return `${parsed.protocol}//${parsed.host}`;
    } catch {
      // malformed referer — fall through
    }
  }
  return undefined;
}

// Proxy for the .NET md→docx export pipeline
// (`POST internal/ai/integration/text-to-docx/start`,
// `TextToDocxController.PublishAsync`): validates the payload and forwards
// it with the caller's credentials. The conversion itself is asynchronous —
// the AI Worker converts the markdown via DocumentService and saves the
// resulting .docx into the target folder (an agent room resolves to its
// Result Storage subfolder); completion surfaces to the client as the
// standard `s:modify-folder` create-file socket event.
export const textToDocxController = {
  start: asyncHandler(async (req, res) => {
    const body = isObject(req.body) ? req.body : {};
    const title = typeof body["title"] === "string" ? body["title"].trim() : "";
    const content = typeof body["content"] === "string" ? body["content"] : "";
    const folderId = body["folderId"];

    if (!title || !content) {
      res.status(400).json({ error: "title and content are required" });
      return;
    }
    if (typeof folderId !== "number" && typeof folderId !== "string") {
      res.status(400).json({ error: "folderId is required" });
      return;
    }

    const origin = resolvePortalOrigin();
    logger.info(
      `textToDocx.start title="${title}" folderId=${folderId} contentLength=${content.length} origin=${origin ?? "-"}`,
    );
    await aiService.post(
      "/integration/text-to-docx/start",
      { title, content, folderId },
      origin ? { query: { origin } } : undefined,
    );

    // The publish is fire-and-forget on the .NET side (202-style semantics):
    // the conversion result arrives later via the files socket events.
    res.status(202).json({ success: true });
  }),
};
