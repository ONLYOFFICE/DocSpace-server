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
import request from "supertest";
import { afterAll, describe, expect, it, vi } from "vitest";

// What the routes make of a storage that rejects with something that is not an Error: the
// storages stand in for the Files API here, since a real fetch only ever rejects with one.

vi.stubEnv("AI__DOCUMENT_AGENT__URL", "http://agent:9838");
vi.stubEnv("AI__DOCUMENT_AGENT__TOKEN", "secret");

const getFileInfo = vi.fn();
vi.mock("../../storage/docspaceFilesApi.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/docspaceFilesApi.js")>()),
  getFileInfo,
}));
const uploadFile = vi.fn();
vi.mock("../../storage/docspaceFileTransfer.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/docspaceFileTransfer.js")>()),
  uploadFile,
}));

const { default: logger } = await import("../../log.js");
const { requestContextMiddleware } = await import("../../requestContext.js");
const { DOCUMENT_AGENT_FILES_PATH, DOCUMENT_AGENT_OUTPUTS_PATH, registerDocumentAgentFiles } =
  await import("../documentAgentFiles.js");

const app = express();
app.use(express.json({ strict: false }));
app.use(requestContextMiddleware);
registerDocumentAgentFiles(app);

afterAll(() => {
  vi.unstubAllEnvs();
});

const AGENT = { "x-docagent-token": "secret", Authorization: "Bearer user-session" };
const MCP = { ...AGENT, Accept: "application/json, text/event-stream" };

function rpc(text: string): unknown {
  const line = text.split("\n").find((l) => l.startsWith("data: "));
  return line === undefined ? undefined : JSON.parse(line.slice("data: ".length));
}

describe("the document agent's routes when a storage rejects with a string", () => {
  it("answers the link tool's generic error and logs the string", async () => {
    getFileInfo.mockRejectedValue("socket closed");

    const reply = await request(app)
      .post(DOCUMENT_AGENT_FILES_PATH)
      .set(MCP)
      .send({
        jsonrpc: "2.0",
        id: 1,
        method: "tools/call",
        params: { name: "get_file_link", arguments: { fileId: "30" } },
      });

    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 1,
      result: { content: [{ type: "text", text: "file 30 could not be read" }], isError: true },
    });
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "documentAgentFiles: no link for file 30: socket closed",
    );
  });

  it("answers the read's generic error and logs the string", async () => {
    getFileInfo.mockRejectedValue("socket closed");

    const reply = await request(app)
      .post(DOCUMENT_AGENT_FILES_PATH)
      .set(MCP)
      .send({
        jsonrpc: "2.0",
        id: 2,
        method: "resources/read",
        params: { uri: "workspace://files/30" },
      });

    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 2,
      error: { code: -32603, message: "file 30 could not be read" },
    });
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "documentAgentFiles: file 30 not read: socket closed",
    );
  });

  it("answers 502 to a delivery whose save rejects with a string", async () => {
    uploadFile.mockRejectedValue("socket closed");

    const reply = await request(app)
      .post(`${DOCUMENT_AGENT_OUTPUTS_PATH}?folder=5`)
      .set({ ...AGENT, "Content-Disposition": 'attachment; filename="a.docx"' })
      .send(Buffer.from("x"));

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({
      error:
        "the workspace gave no usable answer; the file may have been saved all the same, so do not save it again before checking the folder",
    });
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "documentAgentFiles: a.docx may have been saved into folder 5: socket closed",
    );
  });
});
