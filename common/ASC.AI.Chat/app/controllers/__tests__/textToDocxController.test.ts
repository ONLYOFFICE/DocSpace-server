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

import { beforeEach, describe, expect, it, vi } from "vitest";
import { invoke } from "../../__tests__/helpers/http.js";
import {
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

// The markdown export: the body is checked here and published to
// `POST internal/ai/text-to-docx/start` (products/ASC.AI/Server/Api/TextToDocxController.cs,
// PublishAsync, which answers 204), stubbed with msw.

const upstream = useUpstream();
const { default: logger } = await import("../../log.js");
const { aiServiceBaseUrl } = await import("../../storage/httpClient.js");
const { textToDocxController } = await import("../textToDocxController.js");

const START = `${aiServiceBaseUrl}/internal/ai/text-to-docx/start`;

function aiAnswers(status: number): void {
  upstream.use(
    http.post(START, () =>
      status === 204 ? new HttpResponse(null, { status }) : docspaceError(status),
    ),
  );
}

const start = (body: unknown) => invoke(textToDocxController.start, { body });

const valid = { title: "Report", content: "# Report\n\nBody", folderId: 17 };

beforeEach(() => {
  vi.mocked(logger.info).mockClear();
});

describe("textToDocxController.start", () => {
  it("publishes the export and answers 202", async () => {
    aiAnswers(204);

    const reply = await start({ ...valid, title: "  Report  ", content: "  # Report  ", extra: 1 });

    expect(reply.status).toBe(202);
    expect(reply.body).toEqual({ success: true });
    expect(responseViolations("aiExportTextToDocx", 202, reply.body)).toEqual([]);
    expect(upstream.seen).toEqual([
      {
        method: "POST",
        path: "/internal/ai/text-to-docx/start",
        headers: { "content-type": "application/json" },
        body: { title: "Report", content: "  # Report  ", folderId: 17 },
      },
    ]);
    expect(logger.info).toHaveBeenCalledWith(
      'textToDocx.start title="Report" folderId=17 format=Docx contentLength=12',
    );
  });

  it.each(["Docx", "Pdf", "Md"])("forwards the format %s", async (format) => {
    aiAnswers(204);

    const reply = await start({ ...valid, format });

    expect(reply.status).toBe(202);
    expect(upstream.seen[0]?.body).toEqual({ ...valid, format });
  });

  it("forwards a folder id given as a string", async () => {
    aiAnswers(204);

    await start({ ...valid, folderId: "my" });

    expect(upstream.seen[0]?.body).toEqual({ ...valid, folderId: "my" });
  });

  // The AI service refuses these with 400 (MdTextToDocxTaskQueue; ASC.AI.Tests, TextToDocxPublishTests).
  it.each([
    ["an empty string", ""],
    ["a negative number", -1],
  ])("hands a folderId that is %s to the AI service, which refuses it", async (_what, folderId) => {
    aiAnswers(204);

    const reply = await start({ ...valid, folderId });

    expect(reply.status).toBe(202);
    expect(upstream.seen[0]?.body).toEqual({ ...valid, folderId });
  });

  // The Files side reads a number as an integer: a fraction made it throw instead of refuse.
  it("answers 400 for a folderId that is a fraction, before the call", async () => {
    const reply = await start({ ...valid, folderId: 1.5 });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "folderId is required" });
    expect(upstream.seen).toEqual([]);
  });

  const required = { error: "title and content are required" };
  const folder = { error: "folderId is required" };
  const format = { error: "format must be one of Docx, Pdf, Md" };

  // A null body is not in the table: node-mocks-http hands the handler `{}` for it.
  it.each([
    ["a string", "Report", required],
    ["an array", [valid], required],
    ["an empty object", {}, required],
    ["no title", { content: "c", folderId: 1 }, required],
    ["a null title", { ...valid, title: null }, required],
    ["a title that is a number", { ...valid, title: 1 }, required],
    ["an empty title", { ...valid, title: "" }, required],
    ["a blank title", { ...valid, title: "  " }, required],
    ["no content", { title: "t", folderId: 1 }, required],
    ["a null content", { ...valid, content: null }, required],
    ["a content that is an array", { ...valid, content: ["c"] }, required],
    ["an empty content", { ...valid, content: "" }, required],
    ["a blank content", { ...valid, content: " \n\t" }, required],
    ["no folderId", { title: "t", content: "c" }, folder],
    ["a null folderId", { ...valid, folderId: null }, folder],
    ["a folderId that is a boolean", { ...valid, folderId: true }, folder],
    ["a folderId that is an object", { ...valid, folderId: { id: 1 } }, folder],
    ["a format in lower case", { ...valid, format: "docx" }, format],
    ["an unknown format", { ...valid, format: "Html" }, format],
    ["a null format", { ...valid, format: null }, format],
    ["a format that is a number", { ...valid, format: 1 }, format],
  ])("answers 400 to a body with %s and publishes nothing", async (_what, body, error) => {
    const reply = await start(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(error);
    expect(responseViolations("aiExportTextToDocx", 400, reply.body)).toEqual([]);
    expect(upstream.seen).toEqual([]);
  });

  it.each([
    [400, "Bad Request"],
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])("answers the AI service's %i with its reason phrase only", async (status, error) => {
    aiAnswers(status);

    const reply = await start(valid);

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiExportTextToDocx", status, reply.body)).toEqual([]);
  });
});
