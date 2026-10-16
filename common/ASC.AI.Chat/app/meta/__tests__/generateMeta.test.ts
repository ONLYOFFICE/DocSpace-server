import { beforeEach, describe, expect, it, vi } from "vitest";
import { assistantText, FakeDocspaceApiHttpError, logger } from "./mocks.js";

vi.mock("../../log.js", () => ({ default: logger }));
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
}));

const { storage } = await import("../../storage/index.js");
const { generateMeta, NoContentError, NoProfileError, parseFields } =
  await import("../generateMeta.js");

const PROFILE = { id: "p1", name: "GPT", modelId: "gpt-x" };

describe("parseFields", () => {
  it("reads names and types, defaults the type, deduplicates ignoring case and caps the list", () => {
    const fields = parseFields(
      '```json\n{"fields":[{"name":" Contract  date ","type":"date"},{"name":"Amount","type":"NUMBER"},' +
        '{"name":"contract date","type":"text"},{"name":"Notes"},{"name":"","type":"text"},' +
        '{"name":"Weird","type":"boolean"},"Plain string"]}\n```',
    );
    expect(fields).toEqual([
      { name: "Contract date", type: "date" },
      { name: "Amount", type: "number" },
      { name: "Notes", type: "text" },
      { name: "Weird", type: "text" },
      { name: "Plain string", type: "text" },
    ]);
    const many = parseFields(
      JSON.stringify({ fields: Array.from({ length: 60 }, (_, i) => ({ name: `F${i}` })) }),
    );
    expect(many).toHaveLength(50);
  });

  it("drops a name over the portal's limit and answers nothing for a non-object", () => {
    expect(parseFields(JSON.stringify({ fields: [{ name: "x".repeat(256) }] }))).toEqual([]);
    expect(parseFields("no json here")).toEqual([]);
    expect(parseFields('{"other":[]}')).toEqual([]);
  });
});

describe("generateMeta", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(storage.assignments.readByType).mockResolvedValue("p1");
    vi.mocked(storage.profiles.readById).mockResolvedValue(PROFILE as never);
    readFileAsText.mockImplementation(async ({ id }) => `Contract ${id} with ACME, 2024-03-01.`);
  });

  it("reads the files, runs the model once with the user's note as a hint and reports the fields", async () => {
    customSync.mockResolvedValue(
      assistantText(
        '{"fields":[{"name":"Counterparty","type":"text"},{"name":"Date","type":"date"}]}',
      ),
    );
    const result = await generateMeta({
      files: ["1", "2"],
      folders: [],
      prompt: "<b>dates</b> only",
    });
    expect(result.fields).toEqual([
      { name: "Counterparty", type: "text" },
      { name: "Date", type: "date" },
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
    expect(content).toContain("===== Document 1: 1.docx =====");
    expect(content).toContain("Contract 2 with ACME");
  });

  it("answers 503 when no profile is bound and 422 when nothing could be read", async () => {
    vi.mocked(storage.assignments.readByType).mockResolvedValue(null);
    await expect(generateMeta({ files: ["1"], folders: [] })).rejects.toBeInstanceOf(
      NoProfileError,
    );

    vi.mocked(storage.assignments.readByType).mockResolvedValue("p1");
    readFileAsText.mockRejectedValue(new Error("boom"));
    const failure = await generateMeta({ files: ["1"], folders: [] }).catch((e: unknown) => e);
    expect(failure).toBeInstanceOf(NoContentError);
    expect((failure as InstanceType<typeof NoContentError>).skipped).toEqual([
      { id: "1", title: "1.docx", reason: "failed" },
    ]);
    expect(customSync).not.toHaveBeenCalled();
  });

  it("answers the model's failure as 502 and a throttled call as 429", async () => {
    customSync.mockRejectedValueOnce(new Error("invalid api key"));
    await expect(generateMeta({ files: ["1"], folders: [] })).rejects.toMatchObject({
      status: 502,
    });
    customSync.mockRejectedValueOnce({ status: 429, message: "slow down" });
    await expect(generateMeta({ files: ["1"], folders: [] })).rejects.toMatchObject({
      status: 429,
    });
  });

  it("answers 422 for a folder without files", async () => {
    const failure = await generateMeta({ files: [], folders: ["7"] }).catch((e: unknown) => e);
    expect(failure).toBeInstanceOf(NoContentError);
    expect((failure as Error).message).toBe("The selection holds no files");
  });

  it("answers an empty field list when the model does not follow the format", async () => {
    customSync.mockResolvedValue(assistantText("I cannot help with that."));
    const result = await generateMeta({ files: ["1"], folders: [] });
    expect(result.fields).toEqual([]);
    expect(result.sources).toHaveLength(1);
  });
});
