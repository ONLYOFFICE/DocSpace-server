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
import { describe, expect, it } from "vitest";

// The whole Express application as app.ts starts it, without a port: parsers, CORS, the
// request context, the open routes, the authentication gate and the 404 handler in their
// real order. The storage is not initialized and no upstream is called by these requests.

const { createApp } = await import("../createApp.js");
const { API_PREFIX } = await import("../routes.js");

const app = createApp();

describe("the application", () => {
  it.each(["/isLife", "/health", `${API_PREFIX}/isLife`, `${API_PREFIX}/health`])(
    "answers %s without credentials",
    async (path) => {
      const reply = await request(app).get(path);

      expect(reply.status).toBe(200);
    },
  );

  it("serves its OpenAPI document without credentials", async () => {
    const reply = await request(app).get(`${API_PREFIX}/openapi.json`);

    expect(reply.status).toBe(200);
    expect(reply.body).toHaveProperty("paths");
  });

  it("answers 401 to a request with neither the session cookie nor Authorization", async () => {
    const reply = await request(app).get(`${API_PREFIX}/profiles/list`);

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized" });
  });

  it.each([
    ["an empty session cookie", { Cookie: "asc_auth_key=" }],
    ["a blank Authorization header", { Authorization: " " }],
  ])("answers 401 to %s", async (_what, headers) => {
    const reply = await request(app).post(`${API_PREFIX}/threads/create`).set(headers).send({});

    expect(reply.status).toBe(401);
  });

  it("answers 404 to a path it does not have", async () => {
    const reply = await request(app).get("/nothing/here").set("Authorization", "Bearer t");

    expect(reply.status).toBe(404);
  });

  it("marks a GET of the API as not to be cached", async () => {
    const reply = await request(app).get(`${API_PREFIX}/nothing`).set("Authorization", "Bearer t");

    expect(reply.headers["cache-control"]).toBe("no-store");
  });
});
