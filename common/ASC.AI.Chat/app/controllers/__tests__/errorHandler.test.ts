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

import { describe, expect, it } from "vitest";
import express from "express";
import request from "supertest";
import logger from "../../log.js";
import { clientFault } from "../../httpErrors.js";
import { errorHandler } from "../_helpers.js";
import { UpstreamShapeError } from "../../storage/upstreamShape.js";

/** An app whose only route hands `err` to Express, as a middleware that failed would. */
function appThatFails(err: unknown, writeFirst = false): express.Express {
  const app = express();
  app.get("/fail", (_req, res, next) => {
    if (writeFirst) {
      res.write("partial");
    }
    next(err);
  });
  app.use(errorHandler);
  return app;
}

describe("errorHandler", () => {
  it("answers an unknown error with 500, without its text, and logs it", async () => {
    const reply = await request(appThatFails(new Error("secret detail"))).get("/fail?x=1");

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(reply.text).not.toContain("secret detail");
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringContaining("GET /fail?x=1 failed: Error: secret detail"),
    );
  });

  it("answers an error written for the caller with its status and message, without a log line", async () => {
    const reply = await request(appThatFails(clientFault(409, "already there"))).get("/fail");

    expect(reply.status).toBe(409);
    expect(reply.body).toEqual({ error: "already there" });
    expect(logger.error).not.toHaveBeenCalled();
  });

  it("answers an upstream body of an unknown shape with 502, logs the call and names it to nobody", async () => {
    const reply = await request(appThatFails(new UpstreamShapeError("GET threads/{id}"))).get(
      "/fail",
    );

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
    expect(reply.text).not.toContain("threads");
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringContaining("GET threads/{id}: the answer has an unexpected shape"),
    );
  });

  it("answers a value that is not an error with 500", async () => {
    const reply = await request(appThatFails("just a string")).get("/fail");

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });

  it("drops the connection when the answer has already started", async () => {
    const failure = await request(appThatFails(new Error("late"), true))
      .get("/fail")
      .then(
        () => undefined,
        (err: unknown) => err,
      );

    expect(failure).toBeInstanceOf(Error);
  });
});
