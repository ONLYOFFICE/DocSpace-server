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
  listFolderFiles: vi.fn(async () => [
    { id: "f1", title: "f1.docx", ext: ".docx" },
    { id: "f2", title: "f2.docx", ext: ".docx" },
  ]),
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
const { buildUserMessage, fillMeta, NothingToFillError, parseValues, resolveValue, selectTargets } =
  await import("../fillMeta.js");
const { NoContentError, NoProfileError } = await import("../common.js");

type EntryField = import("../portalMetadata.js").EntryField;
type EntryMetadata = import("../portalMetadata.js").EntryMetadata;
type UnresolvedField = import("../fillMeta.js").UnresolvedField;

const field = (
  id: number,
  name: string,
  type: EntryField["type"],
  extra: Partial<EntryField> = {},
): EntryField => ({ id, name, type, options: [], ...extra });

const STATUS = field(4, "Status", FieldType.SingleChoice, {
  options: [
    { id: "a1", value: "Draft" },
    { id: "b2", value: "Signed" },
  ],
});
const TAGS = field(5, "Tags", FieldType.MultiChoice, {
  options: [
    { id: "t1", value: "Legal" },
    { id: "t2", value: "Finance" },
  ],
});

const METADATA: EntryMetadata = {
  templates: [
    {
      id: 1,
      name: "Contract",
      visible: true,
      fields: [
        field(1, "Counterparty", FieldType.String),
        field(2, "Signed on", FieldType.Date),
        field(3, "Amount", FieldType.Number, { value: { numberValue: 10 } }),
        STATUS,
        TAGS,
      ],
    },
    // The same field inherited through a second template is listed once.
    {
      id: 2,
      name: "Inherited",
      visible: true,
      fields: [field(1, "Counterparty", FieldType.String)],
    },
    // A hidden template is not filled, like the UI and the search ignore it.
    { id: 3, name: "Hidden", visible: false, fields: [field(9, "Secret", FieldType.String)] },
  ],
  customFields: [
    { name: "Project", value: "" },
    { name: "Owner", value: "Alice" },
  ],
};

describe("selectTargets", () => {
  it("takes the empty fields of the visible templates, reports the set ones and never lists a field twice", () => {
    const unresolved: UnresolvedField[] = [];
    const targets = selectTargets(METADATA, false, unresolved);
    expect(targets.map((t) => t.key)).toEqual(["f1", "f2", "f4", "f5", "c0"]);
    expect(unresolved).toEqual([
      { fieldId: 3, name: "Amount", reason: "already_set" },
      { name: "Owner", reason: "already_set" },
    ]);
  });

  it("takes everything with overwrite", () => {
    const unresolved: UnresolvedField[] = [];
    const targets = selectTargets(METADATA, true, unresolved);
    expect(targets.map((t) => t.key)).toEqual(["f1", "f2", "f3", "f4", "f5", "c0", "c1"]);
    expect(targets.some((t) => t.field?.id === 9)).toBe(false);
    expect(unresolved).toEqual([]);
  });
});

describe("resolveValue", () => {
  const target = (f: EntryField) => ({ key: `f${f.id}`, name: f.name, field: f });

  it("validates the typed values the way the client's info panel does", () => {
    expect(resolveValue(target(field(1, "s", FieldType.String)), "  ACME   Corp ")).toEqual({
      ok: true,
      value: { stringValue: "ACME Corp" },
    });
    expect(resolveValue(target(field(1, "s", FieldType.String)), "x".repeat(8001))).toEqual({
      ok: false,
      reason: "invalid_value",
    });

    const number = target(field(2, "n", FieldType.Number));
    expect(resolveValue(number, 1500)).toEqual({ ok: true, value: { numberValue: 1500 } });
    expect(resolveValue(number, " -42 ")).toEqual({ ok: true, value: { numberValue: -42 } });
    expect(resolveValue(number, "1 500")).toEqual({ ok: false, reason: "invalid_value" });
    expect(resolveValue(number, 12.5)).toEqual({ ok: false, reason: "invalid_value" });
    expect(resolveValue(number, "9007199254740993")).toEqual({
      ok: false,
      reason: "invalid_value",
    });

    const date = target(field(3, "d", FieldType.Date));
    expect(resolveValue(date, "2024-03-01")).toEqual({
      ok: true,
      value: { dateValue: "2024-03-01T00:00:00.000Z" },
    });
    expect(resolveValue(date, "2024-03-01T15:30:00+03:00")).toEqual({
      ok: true,
      value: { dateValue: "2024-03-01T00:00:00.000Z" },
    });
    expect(resolveValue(date, "2024-02-30")).toEqual({ ok: false, reason: "invalid_value" });
    expect(resolveValue(date, "01.03.2024")).toEqual({ ok: false, reason: "invalid_value" });
  });

  it("maps a choice to the option id by label or id and refuses an unknown one", () => {
    expect(resolveValue(target(STATUS), "signed")).toEqual({
      ok: true,
      value: { optionIds: ["b2"] },
    });
    expect(resolveValue(target(STATUS), ["Draft"])).toEqual({
      ok: true,
      value: { optionIds: ["a1"] },
    });
    expect(resolveValue(target(STATUS), "a1")).toEqual({ ok: true, value: { optionIds: ["a1"] } });
    expect(resolveValue(target(STATUS), ["Draft", "Signed"])).toEqual({
      ok: false,
      reason: "unknown_option",
    });
    expect(resolveValue(target(STATUS), "Cancelled")).toEqual({
      ok: false,
      reason: "unknown_option",
    });

    expect(resolveValue(target(TAGS), ["legal", "Finance", "Nope", "Legal"])).toEqual({
      ok: true,
      value: { optionIds: ["t1", "t2"] },
    });
    expect(resolveValue(target(TAGS), "Finance")).toEqual({
      ok: true,
      value: { optionIds: ["t2"] },
    });
    expect(resolveValue(target(TAGS), ["Nope"])).toEqual({ ok: false, reason: "unknown_option" });
    // A joined string is split, unless it is itself an option.
    expect(resolveValue(target(TAGS), "Legal, Finance")).toEqual({
      ok: true,
      value: { optionIds: ["t1", "t2"] },
    });
    expect(resolveValue(target(TAGS), "legal;nope")).toEqual({
      ok: true,
      value: { optionIds: ["t1"] },
    });
  });

  it("treats a custom field as text", () => {
    expect(resolveValue({ key: "c0", name: "Project" }, " Apollo ")).toEqual({
      ok: true,
      value: "Apollo",
    });
    expect(resolveValue({ key: "c0", name: "Project" }, 7)).toEqual({ ok: true, value: "7" });
    expect(resolveValue({ key: "c0", name: "Project" }, { a: 1 })).toEqual({
      ok: false,
      reason: "invalid_value",
    });
  });
});

describe("parseValues and buildUserMessage", () => {
  it("keeps the first answer per key and ignores malformed entries", () => {
    const values = parseValues(
      '```json\n{"values":[{"key":"f1","value":"ACME"},{"key":"f1","value":"other"},{"value":1},"x",{"key":"f2","value":null}]}\n```',
    );
    expect([...values.entries()]).toEqual([
      ["f1", "ACME"],
      ["f2", null],
    ]);
    expect(parseValues("nothing").size).toBe(0);
  });

  it("describes the fields with their keys, types and option labels, then the documents", () => {
    const targets = selectTargets(METADATA, false, []);
    const message = buildUserMessage(
      "folder",
      targets,
      [{ file: { id: "1", title: "a.docx", ext: ".docx" }, text: "body", truncated: false }],
      "look at totals",
    );
    expect(message).toContain(
      "Fill the metadata fields of the folder that holds the 1 document(s)",
    );
    expect(message).toContain("The user's note:\nlook at totals");
    expect(message).toContain('"key": "f2"');
    expect(message).toContain('"type": "date"');
    expect(message).toContain('"options": [\n   "Draft",\n   "Signed"\n  ]');
    expect(message).toContain('"key": "c0"');
    expect(message).toContain("===== Document 1: a.docx =====\nbody");
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
    readFileAsText.mockResolvedValue("Contract with ACME signed 2024-03-01, status Signed.");
  });

  it("writes the values the text supports and reports the rest", async () => {
    customSync.mockResolvedValue(
      assistantText(
        JSON.stringify({
          values: [
            { key: "f1", value: "ACME" },
            { key: "f2", value: "2024-03-01" },
            { key: "f4", value: "Signed" },
            { key: "f5", value: ["Legal", "Unknown"] },
            { key: "c0", value: null },
          ],
        }),
      ),
    );
    const after: EntryMetadata = { templates: [], customFields: [{ name: "Project", value: "" }] };
    vi.mocked(portalMetadata.getEntryMetadata)
      .mockResolvedValueOnce(METADATA)
      .mockResolvedValueOnce(after);

    const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });

    expect(result.dryRun).toBe(false);
    expect(portalMetadata.setValues).toHaveBeenCalledWith("file", "42", [
      { fieldId: 1, stringValue: "ACME" },
      { fieldId: 2, dateValue: "2024-03-01T00:00:00.000Z" },
      { fieldId: 4, optionIds: ["b2"] },
      { fieldId: 5, optionIds: ["t1"] },
    ]);
    expect(portalMetadata.setCustomFields).not.toHaveBeenCalled();
    expect(result.filled.map((f) => f.name)).toEqual([
      "Counterparty",
      "Signed on",
      "Status",
      "Tags",
    ]);
    expect(result.unresolved).toEqual([
      { fieldId: 3, name: "Amount", reason: "already_set" },
      { name: "Owner", reason: "already_set" },
      { fieldId: undefined, name: "Project", reason: "not_found" },
    ]);
    expect(result.metadata).toBe(after);
    expect(result.sources).toEqual([{ id: "42", title: "42.docx", truncated: false }]);
    // The single file of a `file` entry is the one read.
    expect(readFileAsText).toHaveBeenCalledTimes(1);
  });

  it("writes custom fields separately and reports invalid typed values without writing them", async () => {
    customSync.mockResolvedValue(
      assistantText(
        JSON.stringify({
          values: [
            { key: "f2", value: "March 2024" },
            { key: "f4", value: "Cancelled" },
            { key: "c0", value: "Apollo" },
          ],
        }),
      ),
    );
    const result = await fillMeta({ kind: "folder", id: "7", overwrite: false, dryRun: false });
    expect(portalMetadata.setValues).not.toHaveBeenCalled();
    expect(portalMetadata.setCustomFields).toHaveBeenCalledWith("folder", "7", [
      { name: "Project", value: "Apollo" },
    ]);
    expect(result.filled).toEqual([{ name: "Project", value: "Apollo" }]);
    expect(result.unresolved).toEqual(
      expect.arrayContaining([
        { fieldId: 2, name: "Signed on", reason: "invalid_value" },
        { fieldId: 4, name: "Status", reason: "unknown_option" },
        { fieldId: 1, name: "Counterparty", reason: "not_found" },
      ]),
    );
    // A folder reads its top-level files.
    expect(readFileAsText).toHaveBeenCalledTimes(2);
    expect(result.sources.map((s) => s.id)).toEqual(["f1", "f2"]);
  });

  it("does nothing, model included, when every field already carries a value", async () => {
    vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue({
      templates: [
        {
          id: 1,
          name: "T",
          visible: true,
          fields: [field(1, "A", FieldType.String, { value: { stringValue: "x" } })],
        },
      ],
      customFields: [],
    });
    const result = await fillMeta({ kind: "file", id: "1", overwrite: false, dryRun: false });
    expect(result.filled).toEqual([]);
    expect(result.unresolved).toEqual([{ fieldId: 1, name: "A", reason: "already_set" }]);
    expect(customSync).not.toHaveBeenCalled();
    expect(readFileAsText).not.toHaveBeenCalled();
  });

  it("reports a refused write per field and fails only when nothing was written", async () => {
    customSync.mockResolvedValue(
      assistantText(
        JSON.stringify({
          values: [
            { key: "f1", value: "ACME" },
            { key: "c0", value: "Apollo" },
          ],
        }),
      ),
    );
    vi.mocked(portalMetadata.setValues).mockRejectedValueOnce(
      new FakeDocspaceApiHttpError(403, "Forbidden", "u"),
    );
    const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false });
    expect(result.filled).toEqual([{ name: "Project", value: "Apollo" }]);
    expect(result.unresolved).toEqual(
      expect.arrayContaining([{ fieldId: 1, name: "Counterparty", reason: "write_failed" }]),
    );

    vi.mocked(portalMetadata.setValues).mockRejectedValueOnce(
      new FakeDocspaceApiHttpError(403, "Forbidden", "u"),
    );
    vi.mocked(portalMetadata.setCustomFields).mockRejectedValueOnce(
      new FakeDocspaceApiHttpError(403, "Forbidden", "u"),
    );
    await expect(
      fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false }),
    ).rejects.toMatchObject({
      status: 403,
    });

    // A portal failure that is not a refusal is not swallowed.
    vi.mocked(portalMetadata.setValues).mockRejectedValueOnce(
      new FakeDocspaceApiHttpError(500, "Internal Server Error", "u"),
    );
    await expect(
      fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false }),
    ).rejects.toMatchObject({
      status: 500,
    });
  });

  it("answers the model's failure with its own status", async () => {
    customSync.mockRejectedValueOnce(Object.assign(new Error("aborted"), { name: "AbortError" }));
    await expect(
      fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: false }),
    ).rejects.toMatchObject({
      status: 504,
    });
    expect(portalMetadata.setValues).not.toHaveBeenCalled();
  });

  it("with dryRun resolves the values, writes nothing and says so", async () => {
    customSync.mockResolvedValue(
      assistantText(
        JSON.stringify({
          values: [
            { key: "f1", value: "ACME" },
            { key: "c0", value: "Apollo" },
          ],
        }),
      ),
    );
    const result = await fillMeta({ kind: "file", id: "42", overwrite: false, dryRun: true });
    expect(result.dryRun).toBe(true);
    expect(result.filled).toEqual([
      { fieldId: 1, name: "Counterparty", value: { stringValue: "ACME" } },
      { name: "Project", value: "Apollo" },
    ]);
    expect(result.metadata).toBe(METADATA);
    expect(portalMetadata.setValues).not.toHaveBeenCalled();
    expect(portalMetadata.setCustomFields).not.toHaveBeenCalled();
    expect(portalMetadata.getEntryMetadata).toHaveBeenCalledTimes(1);
  });

  it("refuses an entry whose only template is hidden", async () => {
    vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue({
      templates: [
        { id: 3, name: "Hidden", visible: false, fields: [field(9, "Secret", FieldType.String)] },
      ],
      customFields: [],
    });
    await expect(
      fillMeta({ kind: "file", id: "1", overwrite: false, dryRun: false }),
    ).rejects.toBeInstanceOf(NothingToFillError);
  });

  it("refuses an entry without any metadata field", async () => {
    vi.mocked(portalMetadata.getEntryMetadata).mockResolvedValue({
      templates: [],
      customFields: [],
    });
    await expect(
      fillMeta({ kind: "file", id: "1", overwrite: false, dryRun: false }),
    ).rejects.toBeInstanceOf(NothingToFillError);
  });

  it("answers 503 without a profile, relays a portal refusal and reports unreadable content", async () => {
    vi.mocked(storage.assignments.readByType).mockResolvedValue(null);
    await expect(
      fillMeta({ kind: "file", id: "1", overwrite: false, dryRun: false }),
    ).rejects.toBeInstanceOf(NoProfileError);

    vi.mocked(storage.assignments.readByType).mockResolvedValue("p1");
    vi.mocked(portalMetadata.getEntryMetadata).mockRejectedValueOnce(
      new FakeDocspaceApiHttpError(403, "Forbidden", "u"),
    );
    await expect(
      fillMeta({ kind: "file", id: "1", overwrite: false, dryRun: false }),
    ).rejects.toMatchObject({
      status: 403,
    });

    readFileAsText.mockRejectedValue(new Error("boom"));
    await expect(
      fillMeta({ kind: "file", id: "1", overwrite: false, dryRun: false }),
    ).rejects.toBeInstanceOf(NoContentError);
    expect(customSync).not.toHaveBeenCalled();
  });
});
