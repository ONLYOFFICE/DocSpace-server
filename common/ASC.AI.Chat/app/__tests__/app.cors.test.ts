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
import { afterAll, describe, expect, it, vi } from "vitest";

// CORS as createApp reads it from `core__cors` when the app is built. The app is built once per
// value; the environment is restored afterwards.

const { default: logger } = await import("../log.js");
const { createApp } = await import("../createApp.js");
const { API_PREFIX } = await import("../routes.js");

const build = (value: string) => {
  vi.stubEnv("core__cors", value);
  vi.mocked(logger.info).mockClear();
  const app = createApp();
  const logged = vi.mocked(logger.info).mock.calls.map((c) => c[0]);
  return { app, logged };
};

const single = build("https://Portal.Example.com");
const wildcard = build("https://*.example.com");
const any = build("*");
vi.unstubAllEnvs();

afterAll(() => {
  vi.unstubAllEnvs();
});

const preflight = (app: typeof single.app, origin: string) =>
  request(app)
    .options(`${API_PREFIX}/threads/create`)
    .set("Origin", origin)
    .set("Access-Control-Request-Method", "POST")
    .set("Access-Control-Request-Headers", "content-type,x-custom");

describe("CORS for one origin", () => {
  it("logs the configured value as it was given", () => {
    expect(single.logged[0]).toBe("CORS enabled for https://Portal.Example.com");
  });

  it("answers a preflight of that origin with 204 before the gate", async () => {
    const reply = await preflight(single.app, "https://portal.example.com");

    expect(reply.status).toBe(204);
    expect(reply.headers["access-control-allow-origin"]).toBe("https://portal.example.com");
    expect(reply.headers["access-control-allow-credentials"]).toBe("true");
    expect(reply.headers["access-control-allow-methods"]).toBe("GET,HEAD,PUT,PATCH,POST,DELETE");
    expect(reply.headers["access-control-allow-headers"]).toBe("content-type,x-custom");
    expect(reply.text).toBe("");
  });

  it("answers a preflight of another origin with 204 and no allow-origin", async () => {
    const reply = await preflight(single.app, "https://evil.example.com");

    expect(reply.status).toBe(204);
    expect(reply.headers["access-control-allow-origin"]).toBeUndefined();
    expect(reply.headers["access-control-allow-credentials"]).toBe("true");
  });

  it("adds the CORS headers to an ordinary answer, a 401 included", async () => {
    const reply = await request(single.app)
      .get(`${API_PREFIX}/threads/list`)
      .set("Origin", "https://portal.example.com");

    expect(reply.status).toBe(401);
    expect(reply.headers["access-control-allow-origin"]).toBe("https://portal.example.com");
    expect(reply.headers["access-control-allow-credentials"]).toBe("true");
    expect(reply.headers["vary"]).toBe("Origin");
  });
});

describe("CORS for a wildcard subdomain", () => {
  it("answers a subdomain and not the bare domain", async () => {
    const sub = await preflight(wildcard.app, "https://a.example.com");
    const bare = await preflight(wildcard.app, "https://example.com");

    expect(sub.headers["access-control-allow-origin"]).toBe("https://a.example.com");
    expect(bare.headers["access-control-allow-origin"]).toBeUndefined();
  });
});

describe("CORS for *", () => {
  it("answers every origin with * and no credentials", async () => {
    const reply = await preflight(any.app, "https://anything.example");

    expect(any.logged[0]).toBe("CORS enabled for *");
    expect(reply.status).toBe(204);
    expect(reply.headers["access-control-allow-origin"]).toBe("*");
    expect(reply.headers["access-control-allow-credentials"]).toBeUndefined();
  });
});
