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

// Wire adapter for the C# `form-analysis` internal endpoints: the analyze-intent
// gate + submission schema + the cached starter questions. The model call itself
// runs in the Node service (see forms/suggestedQuestions).

import { aiService } from "./httpClient.js";
import { getArray, getNumber, getObjectArray, getString, isObject } from "../narrow.js";

export type SuggestedQuestion = { question: string; prompt: string };
export type FormColumn = { name: string; label?: string; type: string; values?: string[] };
export type FormSchema = {
  title: string;
  rowCount: number;
  columns: FormColumn[];
  culture: string;
  cultureName: string;
};
export type FormAnalysis =
  | { status: "unavailable" }
  | { status: "ready"; questions: SuggestedQuestion[] }
  | { status: "generate"; schema: FormSchema };

function parseQuestions(value: unknown): SuggestedQuestion[] {
  const questions: SuggestedQuestion[] = [];
  for (const raw of Array.isArray(value) ? value : []) {
    if (!isObject(raw)) {
      continue;
    }
    const question = getString(raw, "question");
    const prompt = getString(raw, "prompt");
    if (question !== undefined && prompt !== undefined) {
      questions.push({ question, prompt });
    }
  }
  return questions;
}

function parseSchema(value: unknown): FormSchema | null {
  if (!isObject(value)) {
    return null;
  }
  const title = getString(value, "title");
  const culture = getString(value, "culture");
  const cultureName = getString(value, "cultureName");
  const rowCount = getNumber(value, "rowCount");
  if (
    title === undefined ||
    culture === undefined ||
    cultureName === undefined ||
    rowCount === undefined
  ) {
    return null;
  }
  const columns: FormColumn[] = [];
  for (const raw of getObjectArray(value, "columns") ?? []) {
    const name = getString(raw, "name");
    const type = getString(raw, "type");
    if (name === undefined || type === undefined) {
      continue;
    }
    const column: FormColumn = { name, type };
    const label = getString(raw, "label");
    if (label !== undefined) {
      column.label = label;
    }
    const values = getArray(raw, "values")?.filter((v): v is string => typeof v === "string");
    if (values && values.length > 0) {
      column.values = values;
    }
    columns.push(column);
  }
  return { title, rowCount, columns, culture, cultureName };
}

export class HttpFormAnalysisStorage {
  // GET internal/ai/form-analysis/{attachmentId}: gated on the analyze intent;
  // "unavailable", "ready" (+cached questions), or "generate" (+the schema).
  async read(attachmentId: string): Promise<FormAnalysis> {
    const raw = await aiService.get(`/form-analysis/${encodeURIComponent(attachmentId)}`);
    if (!isObject(raw)) {
      return { status: "unavailable" };
    }
    const status = getString(raw, "status");
    if (status === "ready") {
      return { status: "ready", questions: parseQuestions(raw["questions"]) };
    }
    if (status === "generate") {
      const schema = parseSchema(raw["schema"]);
      return schema ? { status: "generate", schema } : { status: "unavailable" };
    }
    return { status: "unavailable" };
  }

  // POST internal/ai/form-analysis/{attachmentId}/questions: cache the generated set.
  async saveQuestions(attachmentId: string, questions: SuggestedQuestion[]): Promise<void> {
    await aiService.post(`/form-analysis/${encodeURIComponent(attachmentId)}/questions`, {
      questions,
    });
  }
}
