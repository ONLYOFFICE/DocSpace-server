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
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { HttpFormAnalysisStorage } from "../formAnalysisStorage.js";

// products/ASC.AI/Server/Api/FormAnalysisController.cs: GET form-analysis/{id},
// POST form-analysis/{id}/questions.
const upstream = useUpstream();
const storage = new HttpFormAnalysisStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

/** `FormSchemaDto` with its `FormColumnDto` list (products/ASC.AI/Server/Models/ResponseDto/FormAnalysisDto.cs). */
const schemaDto = (overrides: Record<string, unknown> = {}) => ({
  title: "Vacation request",
  rowCount: 12,
  columns: [
    { name: "employee", label: "Employee", type: "text", values: null },
    { name: "kind", label: null, type: "choice", values: ["paid", "unpaid"] },
  ],
  culture: "en-US",
  cultureName: "English (United States)",
  ...overrides,
});

const schema = {
  title: "Vacation request",
  rowCount: 12,
  columns: [
    { name: "employee", label: "Employee", type: "text" },
    { name: "kind", type: "choice", values: ["paid", "unpaid"] },
  ],
  culture: "en-US",
  cultureName: "English (United States)",
};

function answer(body: unknown) {
  upstream.use(http.get("*/internal/ai/form-analysis/:id", () => docspace(body as never)));
}

describe("read", () => {
  it("reads a ready analysis with its cached questions", async () => {
    answer({
      status: "ready",
      schema: null,
      questions: [{ question: "Who is away?", prompt: "List the employees" }],
    });
    await expect(storage.read("a/1")).resolves.toEqual({
      status: "ready",
      questions: [{ question: "Who is away?", prompt: "List the employees" }],
    });
    expect(upstream.seen).toMatchObject([
      { method: "GET", path: "/internal/ai/form-analysis/a%2F1" },
    ]);
  });

  it.each([
    ["missing", undefined, []],
    ["null", null, []],
    ["an object", { question: "q", prompt: "p" }, []],
    [
      "with invalid entries",
      [
        null,
        "q",
        { question: "q" },
        { prompt: "p" },
        { question: 1, prompt: "p" },
        { question: "q", prompt: "p" },
      ],
      [{ question: "q", prompt: "p" }],
    ],
  ])("reads questions %s", async (_name, questions, expected) => {
    answer({ status: "ready", questions });
    await expect(storage.read("a")).resolves.toEqual({ status: "ready", questions: expected });
  });

  it("reads a generate answer with its schema", async () => {
    answer({ status: "generate", schema: schemaDto(), questions: null });
    await expect(storage.read("a")).resolves.toEqual({ status: "generate", schema });
  });

  it.each([
    ["title", undefined],
    ["title", null],
    ["title", 1],
    ["culture", undefined],
    ["culture", null],
    ["cultureName", undefined],
    ["cultureName", 2],
    ["rowCount", undefined],
    ["rowCount", null],
    ["rowCount", "12"],
  ])("reads a generate answer whose schema %s is %j as unavailable", async (field, value) => {
    const raw: Record<string, unknown> = schemaDto({ [field]: value });
    if (value === undefined) {
      delete raw[field];
    }
    answer({ status: "generate", schema: raw });
    await expect(storage.read("a")).resolves.toEqual({ status: "unavailable" });
  });

  it.each([undefined, null, "schema", [schemaDto()]])(
    "reads a generate answer whose schema is %j as unavailable",
    async (value) => {
      answer({ status: "generate", schema: value });
      await expect(storage.read("a")).resolves.toEqual({ status: "unavailable" });
    },
  );

  it.each([
    ["columns missing", undefined, []],
    ["columns null", null, []],
    [
      "columns with invalid entries",
      [
        null,
        "c",
        { type: "text" },
        { name: "n" },
        { name: 1, type: "text" },
        { name: "a", type: "text", label: 5, values: "x" },
        { name: "b", type: "choice", values: [1, null] },
        { name: "c", type: "choice", values: ["y", 2, "z"] },
        { name: "d", type: "text", values: [] },
      ],
      [
        { name: "a", type: "text" },
        { name: "b", type: "choice" },
        { name: "c", type: "choice", values: ["y", "z"] },
        { name: "d", type: "text" },
      ],
    ],
  ])("reads %s", async (_name, columns, expected) => {
    answer({ status: "generate", schema: schemaDto({ columns }) });
    await expect(storage.read("a")).resolves.toEqual({
      status: "generate",
      schema: { ...schema, columns: expected },
    });
  });

  it.each([
    ["an unavailable answer", { status: "unavailable" }],
    ["an unknown status", { status: "pending" }],
    ["a status in other casing", { status: "Ready", questions: [] }],
    ["no status", { questions: [] }],
    ["a numeric status", { status: 1 }],
  ])("reads %s as unavailable", async (_name, body) => {
    answer(body);
    await expect(storage.read("a")).resolves.toEqual({ status: "unavailable" });
  });

  // DEFECT stor2-10: a GET form-analysis/{id} answer that is not an object (an empty body, null, a list) is read as "unavailable" — expected it to be rethrown as unknown, since the C# action always answers a FormAnalysisDto (ts-ai-failure-paths.md §1)
  it.each([
    ["an empty answer", noContent],
    ["null", () => docspace(null)],
    ["a list", () => docspace([])],
  ])("reads %s as unavailable", async (_name, reply) => {
    upstream.use(http.get("*/internal/ai/form-analysis/:id", reply));
    await expect(storage.read("a")).resolves.toEqual({ status: "unavailable" });
  });

  it.each([403, 404, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/form-analysis/:id", () => docspaceError(status)));
    await expect(storage.read("a")).rejects.toMatchObject({ status });
  });
});

describe("saveQuestions", () => {
  it("posts the questions by the encoded attachment id", async () => {
    upstream.use(http.post("*/internal/ai/form-analysis/:id/questions", noContent));
    await storage.saveQuestions("a/1", [{ question: "q", prompt: "p" }]);
    expect(upstream.seen).toMatchObject([
      {
        method: "POST",
        path: "/internal/ai/form-analysis/a%2F1/questions",
        body: { questions: [{ question: "q", prompt: "p" }] },
      },
    ]);
  });

  it("rethrows a refusal", async () => {
    upstream.use(http.post("*/internal/ai/form-analysis/:id/questions", () => docspaceError(400)));
    await expect(storage.saveQuestions("a", [])).rejects.toMatchObject({ status: 400 });
  });
});
