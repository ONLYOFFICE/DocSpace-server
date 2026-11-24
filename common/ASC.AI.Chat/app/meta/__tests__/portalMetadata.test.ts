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
import { FakeDocspaceApiHttpError, logger } from "./mocks.js";

const { filesApiGet, filesApiSend } = vi.hoisted(() => ({
  filesApiGet: vi.fn(),
  filesApiSend: vi.fn(),
}));

vi.mock("../../log.js", () => ({ default: logger }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
  filesApiGet,
  filesApiSend,
}));

const { FieldType, getEntryMetadata, hasValue, parseEntryMetadata, setCustomFields, setValues } =
  await import("../portalMetadata.js");

describe("parseEntryMetadata", () => {
  it("reads templates, typed fields with options and values, and custom fields", () => {
    const parsed = parseEntryMetadata({
      templates: [
        {
          id: 7,
          name: "Contract",
          visible: true,
          fields: [
            { id: 1, name: "Counterparty", type: 0, options: [], order: 0 },
            {
              id: 2,
              name: "Signed on",
              type: 1,
              value: { dateValue: "2024-03-01T00:00:00.0000000Z" },
            },
            { id: 3, name: "Amount", type: 2, value: { numberValue: 1500 } },
            {
              id: 4,
              name: "Status",
              type: 3,
              options: [
                { id: "a1", value: "Draft" },
                { id: "b2", value: "Signed" },
              ],
              value: { optionIds: ["b2"] },
            },
          ],
        },
      ],
      customFields: [{ name: "Project", value: "Apollo" }, { name: "Empty" }],
    });
    expect(parsed.templates).toHaveLength(1);
    const [template] = parsed.templates;
    expect(template?.id).toBe(7);
    expect(template?.visible).toBe(true);
    expect(template?.fields.map((f) => [f.id, f.type])).toEqual([
      [1, FieldType.String],
      [2, FieldType.Date],
      [3, FieldType.Number],
      [4, FieldType.SingleChoice],
    ]);
    expect(template?.fields[0]?.value).toBeUndefined();
    expect(template?.fields[1]?.value).toEqual({ dateValue: "2024-03-01T00:00:00.0000000Z" });
    expect(template?.fields[2]?.value).toEqual({ numberValue: 1500 });
    expect(template?.fields[3]?.options).toEqual([
      { id: "a1", value: "Draft" },
      { id: "b2", value: "Signed" },
    ]);
    expect(template?.fields[3]?.value).toEqual({ optionIds: ["b2"] });
    expect(parsed.customFields).toEqual([
      { name: "Project", value: "Apollo" },
      { name: "Empty", value: "" },
    ]);
  });

  it("accepts the enum spelled by name and drops fields it cannot type", () => {
    const parsed = parseEntryMetadata({
      templates: [
        {
          id: 1,
          visible: false,
          fields: [
            { id: 1, name: "A", type: "MultiChoice" },
            { id: 2, name: "B", type: "date" },
            { id: 3, name: "C", type: 9 },
            { id: 4, name: "D", type: "unknown" },
            { name: "no id", type: 0 },
          ],
        },
      ],
    });
    expect(parsed.templates[0]?.fields.map((f) => [f.id, f.type])).toEqual([
      [1, FieldType.MultiChoice],
      [2, FieldType.Date],
    ]);
    expect(parsed.templates[0]?.visible).toBe(false);
    expect(parsed.customFields).toEqual([]);
  });

  it("answers empty lists for anything that is not an object", () => {
    expect(parseEntryMetadata(undefined)).toEqual({ templates: [], customFields: [] });
    expect(parseEntryMetadata([])).toEqual({ templates: [], customFields: [] });
  });
});

describe("hasValue", () => {
  it("is true for exactly the members that carry something", () => {
    expect(hasValue(undefined)).toBe(false);
    expect(hasValue({})).toBe(false);
    expect(hasValue({ stringValue: "" })).toBe(false);
    expect(hasValue({ optionIds: [] })).toBe(false);
    expect(hasValue({ stringValue: "x" })).toBe(true);
    expect(hasValue({ numberValue: 0 })).toBe(true);
    expect(hasValue({ dateValue: "2024-01-01T00:00:00Z" })).toBe(true);
    expect(hasValue({ optionIds: ["a"] })).toBe(true);
  });
});

describe("the Files metadata calls", () => {
  beforeEach(() => {
    filesApiGet.mockReset();
    filesApiSend.mockReset();
  });

  it("reads an entry's metadata through the Files API client", async () => {
    filesApiGet.mockResolvedValue({ templates: [], customFields: [{ name: "A", value: "1" }] });
    const metadata = await getEntryMetadata("file", "42");
    expect(metadata.customFields).toEqual([{ name: "A", value: "1" }]);
    expect(filesApiGet.mock.calls).toEqual([["metadata/file/42"]]);
  });

  it("writes values with PUT and parses the metadata that comes back", async () => {
    filesApiSend.mockResolvedValue({
      templates: [{ id: 1, fields: [{ id: 9, name: "N", type: 2, value: { numberValue: 3 } }] }],
      customFields: [],
    });
    const after = await setValues("folder", "5", [{ fieldId: 9, numberValue: 3 }]);
    expect(after.templates[0]?.fields[0]?.value).toEqual({ numberValue: 3 });
    expect(filesApiSend.mock.calls).toEqual([
      ["PUT", "metadata/folder/5/values", { values: [{ fieldId: 9, numberValue: 3 }] }],
    ]);
  });

  it("writes custom fields with PUT", async () => {
    filesApiSend.mockResolvedValue([{ name: "A", value: "x" }]);
    await setCustomFields("file", "1", [{ name: "A", value: "x" }]);
    expect(filesApiSend.mock.calls).toEqual([
      ["PUT", "metadata/file/1/customFields", { fields: [{ name: "A", value: "x" }] }],
    ]);
  });

  it("relays a portal refusal as DocspaceApiHttpError with its status", async () => {
    filesApiGet.mockRejectedValue(new FakeDocspaceApiHttpError(403, "Forbidden", "u"));
    await expect(getEntryMetadata("file", "3")).rejects.toMatchObject({ status: 403 });
  });
});
