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

// The two metadata routes as a client sees them: every answer, success and each error status, is
// checked against the schema `GET openapi.json` publishes for it, and each body field is run
// through its own table of bad values. The access gate is the AI service's tool-prefs read
// (`assertAiAccess`), stubbed at the storage; the generators are stubbed, their error classes
// are the real ones.

import request from "supertest";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { invoke } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

const readDisabled = vi.fn<(entityId?: string) => Promise<unknown>>();
vi.mock("../../storage/index.js", () => ({ storage: { toolPrefs: { readDisabled } } }));
const primeSourceMeta = vi.fn<(id: string | undefined, kind?: string) => Promise<unknown>>();
vi.mock("../../storage/docspaceFilesApi.js", async (original) => ({
  ...(await original<typeof import("../../storage/docspaceFilesApi.js")>()),
  primeSourceMeta,
}));
const generateMeta = vi.fn<(input: unknown) => Promise<unknown>>();
vi.mock("../../meta/generateMeta.js", async (original) => ({
  ...(await original<typeof import("../../meta/generateMeta.js")>()),
  generateMeta,
}));
const fillMeta = vi.fn<(input: unknown) => Promise<unknown>>();
vi.mock("../../meta/fillMeta.js", async (original) => ({
  ...(await original<typeof import("../../meta/fillMeta.js")>()),
  fillMeta,
}));

const { metaController } = await import("../metaController.js");
const { createApp } = await import("../../createApp.js");
const { API_PREFIX } = await import("../../routes.js");
const { ModelCallError, NoContentError, NoProfileError } =
  await import("../../meta/generateMeta.js");
const { NothingToFillError } = await import("../../meta/fillMeta.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");
const { DocspaceApiHttpError } = await import("../../storage/docspaceFilesApi.js");

const GENERATED = {
  fields: [
    { name: "Counterparty", value: "Acme Ltd" },
    { name: "Contract number", value: null },
  ],
  sources: [{ id: "10", title: "Contract.docx", truncated: false }],
  skipped: [{ id: "11", title: "Scan.png", reason: "unsupported" }],
};

const FILLED = {
  dryRun: true,
  metadata: {
    templates: [
      {
        id: 1,
        name: "Contract",
        visible: true,
        fields: [{ id: 7, name: "Amount", type: 2, options: [], value: { numberValue: 10 } }],
      },
    ],
    customFields: [{ name: "Project", value: "" }],
  },
  filled: [
    { fieldId: 7, name: "Amount", value: { numberValue: 12 } },
    { name: "Project", value: "Apollo" },
  ],
  unresolved: [{ fieldId: 8, name: "Signed on", reason: "not_found" }],
  sources: [{ id: "10", title: "Contract.docx", truncated: true }],
  skipped: [],
};

// The fields each route reads, with every bad value of each; the message is the route's own.
const ENTRY_CASES: [string, unknown, string][] = [
  ["a string", "file", "body must be an object"],
  ["a number", 10, "body must be an object"],
  ["an array", [{ kind: "file", id: 10 }], "body must be an object"],
  ["{}", {}, 'kind must be "file"'],
  ["kind null", { kind: null, id: 10 }, 'kind must be "file"'],
  ["kind a number", { kind: 1, id: 10 }, 'kind must be "file"'],
  ["kind empty", { kind: "", id: 10 }, 'kind must be "file"'],
  ["kind in another case", { kind: "File", id: 10 }, 'kind must be "file"'],
  ["kind room", { kind: "room", id: 10 }, 'kind must be "file"'],
  ["id missing", { kind: "file" }, "id must be the integer id of a file"],
  ["id null", { kind: "file", id: null }, "id must be the integer id of a file"],
  ["id a boolean", { kind: "file", id: true }, "id must be the integer id of a file"],
  ["id an object", { kind: "file", id: { id: 10 } }, "id must be the integer id of a file"],
  ["id empty", { kind: "file", id: "" }, "id must be the integer id of a file"],
  ["id blank", { kind: "file", id: "   " }, "id must be the integer id of a file"],
  ["id zero", { kind: "file", id: 0 }, "id must be the integer id of a file"],
  ["id negative", { kind: "file", id: -3 }, "id must be the integer id of a file"],
  ["id with a leading zero", { kind: "file", id: "010" }, "id must be the integer id of a file"],
  ["id in exponent form", { kind: "file", id: "1e3" }, "id must be the integer id of a file"],
  ["id Infinity", { kind: "file", id: Infinity }, "id must be the integer id of a file"],
  ["prompt a number", { kind: "file", id: 10, prompt: 5 }, "prompt must be a string"],
  ["prompt a boolean", { kind: "file", id: 10, prompt: false }, "prompt must be a string"],
  ["prompt an object", { kind: "file", id: 10, prompt: {} }, "prompt must be a string"],
];

beforeEach(() => {
  readDisabled.mockResolvedValue({});
  primeSourceMeta.mockResolvedValue(undefined);
});

describe("metaController.generate answers", () => {
  it.each(ENTRY_CASES)(
    "answers 400 for a body with %s, before the entry is primed",
    async (_what, body, error) => {
      const reply = await invoke(metaController.generate, { body });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error });
      expect(responseViolations("aiMetaGenerate", 400, reply.body)).toEqual([]);
      expect(readDisabled).toHaveBeenCalledWith(undefined);
      expect(primeSourceMeta).not.toHaveBeenCalled();
      expect(generateMeta).not.toHaveBeenCalled();
    },
  );

  // DEFECT ctl-72: an integer id above the portal's 32-bit file ids passes the check and is primed and handed to the generator — expected 400 "id must be the integer id of a file" before any upstream call (ts-ai-failure-paths.md §4: every input has a limit; FileEntry ids are `int` in products/ASC.Files)
  it("passes an id above the portal's integer range on as its rounded decimal form", async () => {
    generateMeta.mockResolvedValue(GENERATED);

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 2 ** 60 } });

    expect(reply.status).toBe(200);
    expect(primeSourceMeta).toHaveBeenCalledWith("1152921504606847000", "file");
    expect(generateMeta).toHaveBeenCalledWith({
      kind: "file",
      id: "1152921504606847000",
      prompt: undefined,
    });
  });

  // node-mocks-http turns a null body into {}, so a JSON `null` goes through the real parser.
  it.each(["generate", "fill"])(
    "answers 400 to a JSON null body of meta/%s through the app",
    async (route) => {
      const reply = await request(createApp())
        .post(`${API_PREFIX}/meta/${route}`)
        .set("Authorization", "Bearer t")
        .set("Content-Type", "application/json")
        .send("null");

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "body must be an object" });
      expect(
        responseViolations(route === "generate" ? "aiMetaGenerate" : "aiMetaFill", 400, reply.body),
      ).toEqual([]);
      expect(primeSourceMeta).not.toHaveBeenCalled();
    },
  );

  it("answers the generated fields with 200 and a body that matches the published schema", async () => {
    generateMeta.mockResolvedValue(GENERATED);

    const reply = await invoke(metaController.generate, {
      body: { kind: "file", id: "10", prompt: "  dates  " },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(GENERATED);
    expect(responseViolations("aiMetaGenerate", 200, reply.body)).toEqual([]);
    expect(primeSourceMeta).toHaveBeenCalledWith("10", "file");
    expect(generateMeta).toHaveBeenCalledWith({ kind: "file", id: "10", prompt: "dates" });
  });

  it("passes a prompt of null on as no prompt and an empty one as an empty string", async () => {
    generateMeta.mockResolvedValue(GENERATED);

    await invoke(metaController.generate, { body: { kind: "file", id: 1, prompt: null } });
    await invoke(metaController.generate, { body: { kind: "file", id: 1, prompt: "   " } });

    expect(generateMeta).toHaveBeenNthCalledWith(1, { kind: "file", id: "1", prompt: undefined });
    expect(generateMeta).toHaveBeenNthCalledWith(2, { kind: "file", id: "1", prompt: "" });
  });

  it("answers the AI service's 403 for any body, before reading it", async () => {
    readDisabled.mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", "AI is off", "http://ai.internal/x"),
    );

    const reply = await invoke(metaController.generate, { body: "not even an object" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiMetaGenerate", 403, reply.body)).toEqual([]);
    expect(primeSourceMeta).not.toHaveBeenCalled();
    expect(generateMeta).not.toHaveBeenCalled();
  });

  it("answers 500 without details when the access read fails", async () => {
    readDisabled.mockRejectedValue(
      new AiServiceHttpError(500, "", "stack at http://ai.internal", "http://ai.internal/x"),
    );

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Upstream error" });
    expect(responseViolations("aiMetaGenerate", 500, reply.body)).toEqual([]);
  });

  it.each([
    [403, "Forbidden"],
    [404, "Not Found"],
  ])("relays the Files API's %i with its reason phrase only", async (status, statusText) => {
    generateMeta.mockRejectedValue(
      new DocspaceApiHttpError(status, statusText, "http://proxy.internal/api/2.0/files/file/1"),
    );

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error: statusText });
    expect(responseViolations("aiMetaGenerate", status, reply.body)).toEqual([]);
  });

  it("answers 422 with the skipped list when nothing could be read", async () => {
    generateMeta.mockRejectedValue(new NoContentError([{ id: "1", reason: "unsupported" }]));

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(422);
    expect(reply.body).toEqual({
      error: "None of the selected files could be read as text",
      skipped: [{ id: "1", reason: "unsupported" }],
    });
    expect(responseViolations("aiMetaGenerate", 422, reply.body)).toEqual([]);
  });

  it("answers 503 when no model is bound", async () => {
    generateMeta.mockRejectedValue(new NoProfileError());

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(503);
    expect(reply.body).toEqual({ error: "No model is configured for metadata generation" });
    expect(responseViolations("aiMetaGenerate", 503, reply.body)).toEqual([]);
  });

  it.each([
    [402, "insufficient_funds", "402 Insufficient Balance"],
    [429, "rate_limit", "429 Rate limit reached"],
    [502, "auth", undefined],
    [504, "timeout", undefined],
  ] as const)(
    "answers a model-call failure with %i, its code and the provider's words",
    async (status, code, providerMessage) => {
      generateMeta.mockRejectedValue(
        new ModelCallError(status, code, "The model request failed", providerMessage),
      );

      const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

      expect(reply.status).toBe(status);
      expect(reply.body).toEqual({
        error: "The model request failed",
        code,
        ...(providerMessage !== undefined && { providerMessage }),
      });
      expect(responseViolations("aiMetaGenerate", status, reply.body)).toEqual([]);
    },
  );

  it("answers 500 without details for an unexpected failure of the generator", async () => {
    generateMeta.mockRejectedValue(new Error("secret http://proxy.internal"));

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiMetaGenerate", 500, reply.body)).toEqual([]);
  });

  it("answers 500 when priming the entry fails, before the generator runs", async () => {
    primeSourceMeta.mockRejectedValue(new Error("down"));

    const reply = await invoke(metaController.generate, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(generateMeta).not.toHaveBeenCalled();
  });
});

describe("metaController.fill answers", () => {
  it.each([
    ...ENTRY_CASES,
    [
      "overwrite a string",
      { kind: "file", id: 1, overwrite: "true" },
      "overwrite must be a boolean",
    ],
    ["overwrite a number", { kind: "file", id: 1, overwrite: 1 }, "overwrite must be a boolean"],
    ["overwrite an object", { kind: "file", id: 1, overwrite: {} }, "overwrite must be a boolean"],
    ["dryRun a string", { kind: "file", id: 1, dryRun: "false" }, "dryRun must be a boolean"],
    ["dryRun a number", { kind: "file", id: 1, dryRun: 0 }, "dryRun must be a boolean"],
    ["dryRun an array", { kind: "file", id: 1, dryRun: [] }, "dryRun must be a boolean"],
  ] as [string, unknown, string][])(
    "answers 400 for a body with %s, before the entry is primed",
    async (_what, body, error) => {
      const reply = await invoke(metaController.fill, { body });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error });
      expect(responseViolations("aiMetaFill", 400, reply.body)).toEqual([]);
      expect(primeSourceMeta).not.toHaveBeenCalled();
      expect(fillMeta).not.toHaveBeenCalled();
    },
  );

  it("checks the entry before the prompt and the prompt before the flags", async () => {
    const both = await invoke(metaController.fill, {
      body: { kind: "room", id: 1, prompt: 5, overwrite: "x" },
    });
    const flags = await invoke(metaController.fill, {
      body: { kind: "file", id: 1, prompt: 5, overwrite: "x", dryRun: "x" },
    });
    const order = await invoke(metaController.fill, {
      body: { kind: "file", id: 1, overwrite: "x", dryRun: "x" },
    });

    expect(both.body).toEqual({ error: 'kind must be "file"' });
    expect(flags.body).toEqual({ error: "prompt must be a string" });
    expect(order.body).toEqual({ error: "overwrite must be a boolean" });
  });

  it("answers the fill result with 200 and a body that matches the published schema", async () => {
    fillMeta.mockResolvedValue(FILLED);

    const reply = await invoke(metaController.fill, {
      body: { kind: "file", id: 10, overwrite: null, dryRun: true },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(FILLED);
    expect(responseViolations("aiMetaFill", 200, reply.body)).toEqual([]);
    expect(fillMeta).toHaveBeenCalledWith({
      kind: "file",
      id: "10",
      prompt: undefined,
      overwrite: false,
      dryRun: true,
    });
    expect(primeSourceMeta).toHaveBeenCalledWith("10", "file");
  });

  it("answers the AI service's 403 for any body, before reading it", async () => {
    readDisabled.mockRejectedValue(new AiServiceHttpError(403, "Forbidden", "", "http://ai/x"));

    const reply = await invoke(metaController.fill, { body: null });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiMetaFill", 403, reply.body)).toEqual([]);
    expect(fillMeta).not.toHaveBeenCalled();
  });

  it("answers 422 when the entry has no metadata fields to fill", async () => {
    fillMeta.mockRejectedValue(new NothingToFillError());

    const reply = await invoke(metaController.fill, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(422);
    expect(reply.body).toEqual({
      error:
        "The entry has no metadata fields to fill: assign a template or add custom fields first",
    });
    expect(responseViolations("aiMetaFill", 422, reply.body)).toEqual([]);
  });

  it("answers 422 with the skipped list when the content could not be read", async () => {
    fillMeta.mockRejectedValue(new NoContentError([]));

    const reply = await invoke(metaController.fill, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(422);
    expect(reply.body).toEqual({ error: "The selection holds no files", skipped: [] });
    expect(responseViolations("aiMetaFill", 422, reply.body)).toEqual([]);
  });

  it("answers 503 when no model is bound and a model-call failure with its status", async () => {
    fillMeta.mockRejectedValueOnce(new NoProfileError());
    fillMeta.mockRejectedValueOnce(new ModelCallError(504, "timeout", "late"));

    const noModel = await invoke(metaController.fill, { body: { kind: "file", id: 1 } });
    const late = await invoke(metaController.fill, { body: { kind: "file", id: 1 } });

    expect(noModel.status).toBe(503);
    expect(responseViolations("aiMetaFill", 503, noModel.body)).toEqual([]);
    expect(late.status).toBe(504);
    expect(late.body).toEqual({ error: "late", code: "timeout" });
    expect(responseViolations("aiMetaFill", 504, late.body)).toEqual([]);
  });

  it("relays a refused write of the Files API with its status", async () => {
    fillMeta.mockRejectedValue(new DocspaceApiHttpError(403, "Forbidden", "http://proxy/x"));

    const reply = await invoke(metaController.fill, {
      body: { kind: "file", id: 1, dryRun: false },
    });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiMetaFill", 403, reply.body)).toEqual([]);
  });

  it("answers 500 without details for an unexpected failure of the filler", async () => {
    fillMeta.mockRejectedValue(new Error("secret http://proxy.internal"));

    const reply = await invoke(metaController.fill, { body: { kind: "file", id: 1 } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiMetaFill", 500, reply.body)).toEqual([]);
  });
});
