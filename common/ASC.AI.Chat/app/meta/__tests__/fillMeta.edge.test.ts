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

// The branches of `fillMeta.ts` that `fillMeta.test.ts` leaves open: a number field described to
// the model, a date answered with another type, the user's note, a write with nothing resolved,
// and a failure of the read that follows a write. Stubbed at the same module boundary as there.

import { beforeEach, describe, expect, it, vi } from "vitest";
import { assistantText, FakeDocspaceApiHttpError, logger, PROXY, withTimeout } from "./mocks.js";

vi.mock("../../log.js", () => ({ default: logger }));
vi.mock("../../storage/httpClient.js", () => ({ withTimeout, proxyBaseUrl: PROXY }));
vi.mock("../../requestContext.js", () => ({
  markForwardHeadersToProvider: vi.fn(),
  getForwardedHeaders: () => ({}),
}));
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
}));
vi.mock("../portalMetadata.js", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  getEntryMetadata: vi.fn(),
  setValues: vi.fn(),
  setCustomFields: vi.fn(),
}));

const { storage } = await import("../../storage/index.js");
const portalMetadata = await import("../portalMetadata.js");
const { FieldType } = portalMetadata;
const { buildUserMessage, fillMeta, resolveValue } = await import("../fillMeta.js");

type EntryMetadata = import("../portalMetadata.js").EntryMetadata;

const AMOUNT = { id: 3, name: "Amount", type: FieldType.Number, options: [] };
const SIGNED = { id: 2, name: "Signed on", type: FieldType.Date, options: [] };

const METADATA: EntryMetadata = {
  templates: [{ id: 1, name: "Contract", visible: true, fields: [AMOUNT, SIGNED] }],
  customFields: [{ name: "Project", value: "" }],
};

describe("buildUserMessage", () => {
  it("describes a number field as number and puts the user's note before the fields", () => {
    const message = buildUserMessage(
      "file",
      [{ key: "f3", name: "Amount", field: AMOUNT }],
      [],
      "totals only",
    );

    expect(message).toBe(
      [
        "Fill the metadata fields of the document below.",
        "\nThe user's note:\ntotals only",
        '\nFields:\n[\n {\n  "key": "f3",\n  "name": "Amount",\n  "type": "number"\n }\n]\n',
        "",
      ].join("\n"),
    );
  });
});

describe("resolveValue for a date", () => {
  it.each([
    ["a number", 20240301],
    ["null", null],
    ["an array", ["2024-03-01"]],
    ["an object", { date: "2024-03-01" }],
  ])("refuses %s as invalid_value", (_what, raw) => {
    expect(resolveValue({ key: "f2", name: "Signed on", field: SIGNED }, raw)).toEqual({
      ok: false,
      reason: "invalid_value",
    });
  });
});

describe("resolveValue for a number", () => {
  it.each([
    ["a boolean", true],
    ["null", null],
    ["an array", [12]],
    ["a fraction", 1.5],
    ["a number past the safe range", "9007199254740993"],
  ])("refuses %s as invalid_value", (_what, raw) => {
    expect(resolveValue({ key: "f3", name: "Amount", field: AMOUNT }, raw)).toEqual({
      ok: false,
      reason: "invalid_value",
    });
  });

  it("takes a negative integer given as text", () => {
    expect(resolveValue({ key: "f3", name: "Amount", field: AMOUNT }, " -12 ")).toEqual({
      ok: true,
      value: { numberValue: -12 },
    });
  });
});

describe("fillMeta", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(storage.assignments.readByType).mockResolvedValue("p1");
    vi.mocked(storage.profiles.readById).mockResolvedValue({
      id: "p1",
      name: "GPT",
      modelId: "m",
    } as never);
    vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue(METADATA);
    readFileAsText.mockResolvedValue("Total 1200, signed 2024-03-01.");
  });

  it("hands the sanitized note to the model, cut to its limit", async () => {
    customSync.mockResolvedValue(assistantText('{"values":[]}'));

    await fillMeta({
      kind: "file",
      id: "42",
      prompt: `<script>evil()</script><b>totals</b> ${"x".repeat(3_000)}`,
      overwrite: false,
      dryRun: true,
    });

    const message = (customSync.mock.calls[0]?.[0] as { messages: { content: string }[] })
      .messages[0]?.content;
    expect(message).toContain(`\nThe user's note:\ntotals ${"x".repeat(1_993)}\n`);
    expect(message).not.toContain("evil");
    expect(message).not.toContain("x".repeat(1_994));
  });

  it("answers a write in which nothing resolved with the metadata it read, writing nothing", async () => {
    customSync.mockResolvedValue(
      assistantText(JSON.stringify({ values: [{ key: "f3", value: "many" }] })),
    );

    const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });

    expect(result).toEqual({
      dryRun: false,
      metadata: METADATA,
      filled: [],
      unresolved: [
        { fieldId: 3, name: "Amount", reason: "invalid_value" },
        { fieldId: 2, name: "Signed on", reason: "not_found" },
        { fieldId: undefined, name: "Project", reason: "not_found" },
      ],
      sources: [{ id: "42", title: "42.docx", truncated: false }],
      skipped: [],
    });
    expect(portalMetadata.setValues).not.toHaveBeenCalled();
    expect(portalMetadata.setCustomFields).not.toHaveBeenCalled();
    expect(portalMetadata.getEntryMetadata).toHaveBeenCalledTimes(1);
  });

  it("fails a write whose typed values got a 5xx, before the custom fields are written", async () => {
    customSync.mockResolvedValue(
      assistantText(
        JSON.stringify({
          values: [
            { key: "f3", value: 1200 },
            { key: "c0", value: "Apollo" },
          ],
        }),
      ),
    );
    const failure = new FakeDocspaceApiHttpError(502, "Bad Gateway", "u");
    vi.mocked(portalMetadata.setValues).mockRejectedValue(failure);

    await expect(
      fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false }),
    ).rejects.toBe(failure);
    expect(portalMetadata.setCustomFields).not.toHaveBeenCalled();
  });

  // What was written is not answered as a failure: a bare 5xx would hide it, and a retry would
  // run the model again.
  it("answers what was written, with the metadata read before, when the read after the write fails", async () => {
    customSync.mockResolvedValue(
      assistantText(JSON.stringify({ values: [{ key: "f3", value: 1200 }] })),
    );
    vi.mocked(portalMetadata.getEntryMetadata)
      .mockResolvedValueOnce(METADATA)
      .mockRejectedValueOnce(new FakeDocspaceApiHttpError(503, "Unavailable", "u"));

    const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });

    expect(portalMetadata.setValues).toHaveBeenCalledWith("file", "42", [
      { fieldId: 3, numberValue: 1200 },
    ]);
    expect(result.dryRun).toBe(false);
    expect(result.metadata).toBe(METADATA);
    expect(result.filled).toEqual([{ fieldId: 3, name: "Amount", value: { numberValue: 1200 } }]);
    expect(logger.warn).toHaveBeenCalledWith(
      "meta.fill: the values of file 42 were written, reading them back failed with 503",
    );
  });

  // ts-ai-failure-paths.md §6: a failure after the write, "a timeout and a non-HTTP error
  // included", does not answer a bare error.
  it.each([
    ["a timeout", new Error("upstream timeout after 300000 ms: http://files.internal/x")],
    ["a dropped connection", new TypeError("fetch failed")],
    ["an answer that cannot be read", new Error("DocSpace API: unparseable response")],
  ])(
    "answers what was written when the read after the write ends in %s",
    async (_what, failure) => {
      customSync.mockResolvedValue(
        assistantText(JSON.stringify({ values: [{ key: "f3", value: 1200 }] })),
      );
      vi.mocked(portalMetadata.getEntryMetadata)
        .mockResolvedValueOnce(METADATA)
        .mockRejectedValueOnce(failure);

      const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });

      expect(result.metadata).toBe(METADATA);
      expect(result.filled).toEqual([{ fieldId: 3, name: "Amount", value: { numberValue: 1200 } }]);
      expect(logger.warn).toHaveBeenCalledWith(
        "meta.fill: the values of file 42 were written, reading them back got no answer that could be read",
      );
    },
  );

  it.each([
    ["a timeout", new Error("upstream timeout after 300000 ms: http://files.internal/x")],
    ["a dropped connection", new TypeError("fetch failed")],
  ])(
    "reports the second write as write_failed when it ends in %s after the first one landed",
    async (_what, failure) => {
      customSync.mockResolvedValue(
        assistantText(
          JSON.stringify({
            values: [
              { key: "f3", value: 1200 },
              { key: "c0", value: "Apollo" },
            ],
          }),
        ),
      );
      vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue(METADATA);
      vi.mocked(portalMetadata.setValues).mockResolvedValue(undefined as never);
      vi.mocked(portalMetadata.setCustomFields).mockRejectedValue(failure);

      const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });

      expect(result.filled).toEqual([{ fieldId: 3, name: "Amount", value: { numberValue: 1200 } }]);
      expect(result.unresolved).toContainEqual(
        expect.objectContaining({ name: "Project", reason: "write_failed" }),
      );
      expect(logger.warn).toHaveBeenCalledWith(
        "meta.fill: writing 1 value(s) to file 42 got no answer that could be read",
      );
    },
  );

  it("fails the request when the first write got no answer: nothing is known to be written", async () => {
    customSync.mockResolvedValue(
      assistantText(JSON.stringify({ values: [{ key: "f3", value: 1200 }] })),
    );
    const failure = new Error("upstream timeout");
    vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue(METADATA);
    vi.mocked(portalMetadata.setValues).mockRejectedValue(failure);

    await expect(
      fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false }),
    ).rejects.toBe(failure);
  });

  it("reports the second write as write_failed when it gets a 5xx after the first one landed", async () => {
    customSync.mockResolvedValue(
      assistantText(
        JSON.stringify({
          values: [
            { key: "f3", value: 1200 },
            { key: "c0", value: "Apollo" },
          ],
        }),
      ),
    );
    vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue(METADATA);
    vi.mocked(portalMetadata.setValues).mockResolvedValue(undefined as never);
    vi.mocked(portalMetadata.setCustomFields).mockRejectedValue(
      new FakeDocspaceApiHttpError(502, "Bad Gateway", "u"),
    );

    const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });

    expect(result.filled).toEqual([{ fieldId: 3, name: "Amount", value: { numberValue: 1200 } }]);
    expect(result.unresolved).toContainEqual(
      expect.objectContaining({ name: "Project", reason: "write_failed" }),
    );
  });

  it("reads the files of a folder entry through the folder listing", async () => {
    customSync.mockResolvedValue(assistantText('{"values":[]}'));
    const portalFiles = await import("../portalFiles.js");
    vi.mocked(portalFiles.listFolderFiles).mockResolvedValue([
      { id: "7", title: "a.docx", ext: ".docx" },
    ]);

    const result = await fillMeta({ kind: "folder", id: "9", overwrite: false, dryRun: true });

    expect(portalFiles.listFolderFiles).toHaveBeenCalledWith("9", 100);
    expect(portalFiles.getPortalFile).not.toHaveBeenCalled();
    expect(result.sources).toEqual([{ id: "7", title: "a.docx", truncated: false }]);
    const message = (customSync.mock.calls[0]?.[0] as { messages: { content: string }[] })
      .messages[0]?.content;
    expect(message).toMatch(
      /^Fill the metadata fields of the folder that holds the 1 document\(s\) below\./,
    );
  });
});
