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

import { request as httpRequest } from "node:http";
import type { AddressInfo } from "node:net";
import express from "express";
import request from "supertest";
import { afterAll, describe, expect, it, vi } from "vitest";

// The outputs endpoint reading a body as it streams in, on an application with only the
// request context and the document agent's routes: no request log, no network stub between
// supertest and the app. No upstream is reached, every request here fails before the save.

vi.stubEnv("AI__DOCUMENT_AGENT__URL", "http://agent:9838");
vi.stubEnv("AI__DOCUMENT_AGENT__TOKEN", "secret");

const { default: logger } = await import("../../log.js");
const { requestContextMiddleware } = await import("../../requestContext.js");
const { DOCUMENT_AGENT_OUTPUTS_PATH, MAX_FILE_BYTES, registerDocumentAgentFiles } =
  await import("../documentAgentFiles.js");

const app = express();
app.use(requestContextMiddleware);
registerDocumentAgentFiles(app);

afterAll(() => {
  vi.unstubAllEnvs();
});

const AGENT = {
  "x-docagent-token": "secret",
  Authorization: "Bearer user-session",
  "Content-Disposition": 'attachment; filename="Big.pdf"',
};

// Polled every millisecond: the server sees the request and the break within a few.
const FAST = { interval: 1, timeout: 1000 };

describe("the outputs endpoint reading a streamed body", () => {
  it("answers 413 once a body without a declared length passes the limit", async () => {
    const req = request(app).post(`${DOCUMENT_AGENT_OUTPUTS_PATH}?folder=5`).set(AGENT);
    req.write(Buffer.alloc(MAX_FILE_BYTES / 2));
    req.write(Buffer.alloc(MAX_FILE_BYTES / 2 + 1));

    const reply = await req;

    expect(reply.status).toBe(413);
    expect(reply.body).toEqual({ error: `the file is over the limit of ${MAX_FILE_BYTES} bytes` });
  });

  it("logs a body that breaks off before its end", async () => {
    const server = app.listen(0);
    const arrived = vi.fn();
    server.on("request", arrived);
    try {
      const req = httpRequest({
        port: (server.address() as AddressInfo).port,
        method: "POST",
        path: `${DOCUMENT_AGENT_OUTPUTS_PATH}?folder=5`,
        headers: { ...AGENT, "Transfer-Encoding": "chunked" },
      });
      req.on("error", () => undefined);
      req.write(Buffer.from("partial"));
      await vi.waitFor(() => {
        expect(arrived).toHaveBeenCalled();
      }, FAST);

      req.destroy();

      await vi.waitFor(() => {
        expect(vi.mocked(logger.warn).mock.calls).toEqual([
          ["documentAgentFiles: body of Big.pdf not read: aborted"],
        ]);
      }, FAST);
    } finally {
      server.close();
    }
  });
});
