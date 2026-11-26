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
import { assistantText, FakeDocspaceApiHttpError, logger } from "./mocks.js";

vi.mock("../../requestContext.js", () => ({ markForwardHeadersToProvider: vi.fn() }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
}));
vi.mock("../../storage/index.js", () => ({
  storage: { assignments: { readByType: vi.fn() }, profiles: { readById: vi.fn() } },
}));
vi.mock("../../forms/suggestedQuestions.js", () => ({
  extractText: (message: { content: { type: string; text: string }[] }) =>
    message.content.map((p) => p.text).join(""),
}));
const customSync = vi.fn();
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  customSync,
}));
const readFileAsText = vi.fn<(file: { id: string }) => Promise<string>>();
vi.mock("../portalFiles.js", () => ({
  getPortalFile: vi.fn(async (id: string) => ({ id, title: `${id}.docx`, ext: ".docx" })),
  listFolderFiles: vi.fn(async () => []),
  PortalTextReader: class {
    readFileAsText = readFileAsText;
  },
  UnsupportedFileError: class UnsupportedFileError extends Error {},
  ConversionFailedError: class ConversionFailedError extends Error {},
  ConversionTimeoutError: class ConversionTimeoutError extends Error {},
}));

const { storage } = await import("../../storage/index.js");
const portalFiles = await import("../portalFiles.js");
const { listFolderFiles } = await import("../portalFiles.js");
const { generateMeta, NoContentError, NoProfileError, parseFields } =
  await import("../generateMeta.js");

const PROFILE = { id: "p1", name: "GPT", modelId: "gpt-x" };

describe("parseFields", () => {
  it("reads names and values, deduplicates ignoring case and caps the list", () => {
    const fields = parseFields(
      '```json\n{"fields":[{"name":" Contract  date ","value":" 2024-03-01 "},{"name":"Amount","value":1500},' +
        '{"name":"contract date","value":"other"},{"name":"Notes"},{"name":"","value":"x"},' +
        '{"name":"Missing","value":null},{"name":"Blank","value":"  "},{"name":"Odd","value":{"a":1}},"Plain string"]}\n```',
    );
    expect(fields).toEqual([
      { name: "Contract date", value: "2024-03-01" },
      { name: "Amount", value: "1500" },
      { name: "Notes", value: null },
      { name: "Missing", value: null },
      { name: "Blank", value: null },
      { name: "Odd", value: null },
      { name: "Plain string", value: null },
    ]);
    const many = parseFields(
      JSON.stringify({ fields: Array.from({ length: 60 }, (_, i) => ({ name: `F${i}` })) }),
    );
    expect(many).toHaveLength(50);
  });

  it("drops a name over the portal's limit, blanks an over-long value, and answers nothing for a non-object", () => {
    expect(parseFields(JSON.stringify({ fields: [{ name: "x".repeat(256) }] }))).toEqual([]);
    expect(
      parseFields(JSON.stringify({ fields: [{ name: "Long", value: "v".repeat(8001) }] })),
    ).toEqual([{ name: "Long", value: null }]);
    expect(parseFields("no json here")).toEqual([]);
    expect(parseFields('{"other":[]}')).toEqual([]);
  });

  it("takes a bare string as a name without a value and skips entries of any other type", () => {
    expect(
      parseFields(
        JSON.stringify({
          fields: [5, null, true, ["Nested"], "  Plain   name ", { name: 3 }, { value: "x" }],
        }),
      ),
    ).toEqual([{ name: "Plain name", value: null }]);
  });

  it.each([
    ["a number", 1200, "1200"],
    ["a boolean", true, null],
    ["an object", { amount: 1 }, null],
    ["an infinite number", "Infinity", "Infinity"],
    ["blank text", "   ", null],
  ])("reads a value that is %s", (_what, value, expected) => {
    expect(parseFields(JSON.stringify({ fields: [{ name: "A", value }] }))).toEqual([
      { name: "A", value: expected },
    ]);
  });
});

describe("generateMeta", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(storage.assignments.readByType).mockResolvedValue("p1");
    vi.mocked(storage.profiles.readById).mockResolvedValue(PROFILE as never);
    readFileAsText.mockImplementation(async ({ id }) => `Contract ${id} with ACME, 2024-03-01.`);
  });

  it("reads the folder's files, runs the model once with the user's note as a hint and reports the fields", async () => {
    vi.mocked(listFolderFiles).mockResolvedValueOnce([
      { id: "1", title: "1.docx", ext: ".docx" },
      { id: "2", title: "2.docx", ext: ".docx" },
    ] as never);
    customSync.mockResolvedValue(
      assistantText(
        '{"fields":[{"name":"Counterparty","value":"ACME"},{"name":"Date","value":null}]}',
      ),
    );
    const result = await generateMeta({
      kind: "folder",
      id: "7",
      prompt: "<b>dates</b> only",
    });
    expect(result.fields).toEqual([
      { name: "Counterparty", value: "ACME" },
      { name: "Date", value: null },
    ]);
    expect(result.sources).toEqual([
      { id: "1", title: "1.docx", truncated: false },
      { id: "2", title: "2.docx", truncated: false },
    ]);
    expect(result.skipped).toEqual([]);
    expect(customSync).toHaveBeenCalledTimes(1);
    const args = customSync.mock.calls[0]![0] as {
      profile: unknown;
      systemPrompt: string;
      messages: { role: string; content: string }[];
    };
    expect(args.profile).toBe(PROFILE);
    expect(args.systemPrompt).toContain("Reply with one JSON object only");
    const content = args.messages[0]!.content;
    expect(content).toContain(
      "The user is interested in these kinds of metadata fields:\ndates only",
    );
    expect(content).not.toContain("<b>");
    expect(content).toContain("folder that holds the 2 document(s)");
    expect(content).toContain("===== Document 1: 1.docx =====");
    expect(content).toContain("Contract 2 with ACME");
  });

  it("answers 503 when no profile is bound and 422 when nothing could be read", async () => {
    vi.mocked(storage.assignments.readByType).mockResolvedValue(null);
    await expect(generateMeta({ kind: "file", id: "1" })).rejects.toBeInstanceOf(NoProfileError);

    vi.mocked(storage.assignments.readByType).mockResolvedValue("p1");
    readFileAsText.mockRejectedValue(
      new (await import("../portalFiles.js")).ConversionFailedError("boom"),
    );
    const failure = await generateMeta({ kind: "file", id: "1" }).catch((e: unknown) => e);
    expect(failure).toBeInstanceOf(NoContentError);
    expect((failure as InstanceType<typeof NoContentError>).skipped).toEqual([
      { id: "1", title: "1.docx", reason: "failed" },
    ]);
    expect(customSync).not.toHaveBeenCalled();
  });

  it("answers the model's failure as 502 and a throttled call as 429", async () => {
    customSync.mockRejectedValueOnce(new Error("invalid api key"));
    await expect(generateMeta({ kind: "file", id: "1" })).rejects.toMatchObject({
      status: 502,
    });
    customSync.mockRejectedValueOnce({ status: 429, message: "slow down" });
    await expect(generateMeta({ kind: "file", id: "1" })).rejects.toMatchObject({
      status: 429,
    });
  });

  it("answers 422 for a folder without files", async () => {
    const failure = await generateMeta({ kind: "folder", id: "7" }).catch((e: unknown) => e);
    expect(failure).toBeInstanceOf(NoContentError);
    expect((failure as Error).message).toBe("The selection holds no files");
  });

  it("answers 404 and 403 for a single file the caller cannot open, like meta/fill does", async () => {
    vi.mocked(portalFiles.getPortalFile).mockResolvedValueOnce(undefined);
    await expect(generateMeta({ kind: "file", id: "9" })).rejects.toMatchObject({ status: 404 });
    vi.mocked(portalFiles.getPortalFile).mockRejectedValueOnce(
      new FakeDocspaceApiHttpError(403, "Forbidden", "u"),
    );
    await expect(generateMeta({ kind: "file", id: "9" })).rejects.toMatchObject({ status: 403 });
    expect(customSync).not.toHaveBeenCalled();
  });

  it("answers an empty field list when the model does not follow the format", async () => {
    customSync.mockResolvedValue(assistantText("I cannot help with that."));
    const result = await generateMeta({ kind: "file", id: "1" });
    expect(result.fields).toEqual([]);
    expect(result.sources).toHaveLength(1);
  });
});
