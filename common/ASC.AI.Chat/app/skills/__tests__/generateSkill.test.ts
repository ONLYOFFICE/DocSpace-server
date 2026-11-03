import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../../requestContext.js", () => ({ markForwardHeadersToProvider: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: { assignments: { readByType: vi.fn() }, profiles: { readById: vi.fn() } },
}));
vi.mock("../../storage/docspaceFilesApi.js", () => ({ getFolderInfo: vi.fn() }));
vi.mock("../../forms/suggestedQuestions.js", () => ({
  extractText: (message: { content: string }) => message.content,
}));
const customSync = vi.fn();
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  customSync,
}));

const { storage } = await import("../../storage/index.js");
const { getFolderInfo } = await import("../../storage/docspaceFilesApi.js");
const {
  buildSkillFile,
  cleanDescription,
  generateSkill,
  NoSkillProfileError,
  parseSkill,
  toSkillName,
} = await import("../generateSkill.js");

const PROFILE = { id: "p1", name: "GPT", modelId: "gpt-x" };
const INSTRUCTIONS_ROOM = { folderType: 38, roomType: 10, canCreate: true };

describe("toSkillName", () => {
  it("lower-cases the title and folds everything else into single hyphens", () => {
    expect(toSkillName("  Sales Report: Q3 (draft)!  ")).toBe("sales-report-q3-draft");
    expect(toSkillName("Отчёт по продажам")).toBe("отчёт-по-продажам");
    expect(toSkillName("snake_case__name")).toBe("snake-case-name");
  });

  it("is empty when nothing is left and stays within 64 characters", () => {
    expect(toSkillName("!!! ---")).toBe("");
    const long = toSkillName(`${"a".repeat(63)} b`);
    expect(long).toBe("a".repeat(63));
  });
});

describe("cleanDescription", () => {
  it("flattens the text and drops tags and angle brackets", () => {
    expect(cleanDescription("Drafts <b>reports</b>.\n  Use when a > b.")).toBe(
      "Drafts reports. Use when a b.",
    );
  });

  it("cuts a long text at a word boundary within the limit", () => {
    const cut = cleanDescription("word ".repeat(400));
    expect(cut.length).toBeLessThanOrEqual(1024);
    expect(cut.endsWith("word...")).toBe(true);
  });
});

describe("parseSkill", () => {
  it("splits the frontmatter description from the body", () => {
    expect(
      parseSkill('---\nname: x\ndescription: "Does things. Use when asked."\n---\n\n# X\n\nBody'),
    ).toEqual({
      description: "Does things. Use when asked.",
      body: "# X\n\nBody",
    });
  });

  it("reads a block scalar and unwraps a code fence", () => {
    expect(
      parseSkill(
        "```markdown\n---\nname: x\ndescription: >\n  Line one\n  line two\n---\n# X\n```",
      ),
    ).toEqual({
      description: "Line one line two",
      body: "# X",
    });
  });

  it("takes an answer without frontmatter as the body", () => {
    expect(parseSkill("# X\n\nBody")).toEqual({ body: "# X\n\nBody" });
  });
});

describe("buildSkillFile", () => {
  it("quotes both scalars", () => {
    expect(buildSkillFile("true", 'Says "hi"', "# X")).toBe(
      '---\nname: "true"\ndescription: "Says \\"hi\\""\n---\n\n# X\n',
    );
  });
});

describe("generateSkill", () => {
  const input = { destId: "42", title: "Sales Report", description: "Write weekly sales reports" };

  beforeEach(() => {
    vi.mocked(getFolderInfo).mockResolvedValue(INSTRUCTIONS_ROOM);
    vi.mocked(storage.assignments.readByType).mockImplementation(async (type) =>
      type === "Default" ? "p1" : null,
    );
    vi.mocked(storage.profiles.readById).mockResolvedValue(PROFILE as never);
  });

  it("writes the frontmatter itself and returns the file without saving it", async () => {
    customSync.mockResolvedValue({
      content:
        "---\nname: other\ndescription: Writes <i>weekly</i> sales reports.\n---\n# Sales report\n\n1. Do it.",
    });

    await expect(generateSkill(input)).resolves.toEqual({
      name: "sales-report",
      content:
        '---\nname: "sales-report"\ndescription: "Writes weekly sales reports."\n---\n\n# Sales report\n\n1. Do it.\n',
    });
  });

  it("falls back to the user's words when the model wrote no description", async () => {
    customSync.mockResolvedValue({ content: "# Sales report\n\nBody" });
    const { content } = await generateSkill(input);
    expect(content).toContain('description: "Write weekly sales reports"');
  });

  it("passes the description to the model as written, angle brackets included", async () => {
    customSync.mockResolvedValue({ content: "# Invoices\n\nBody" });
    await generateSkill({
      ...input,
      description: "Flag invoices where the total < 100 EUR.\nThen check List<String> fields.",
    });
    const [request] = customSync.mock.calls.at(-1)!;
    expect(JSON.stringify(request)).toContain(
      "Flag invoices where the total < 100 EUR.\\nThen check List<String> fields.",
    );
  });

  it("refuses a description of control characters only before calling the model", async () => {
    customSync.mockClear();
    await expect(
      generateSkill({ ...input, description: "\u0000\u0007\u001f" }),
    ).rejects.toMatchObject({ status: 400 });
    expect(customSync).not.toHaveBeenCalled();
  });

  it("refuses a folder that is not an Instructions room before calling the model", async () => {
    vi.mocked(getFolderInfo).mockResolvedValue({ folderType: 6, roomType: 2 });
    await expect(generateSkill(input)).rejects.toMatchObject({ status: 400 });
    expect(customSync).not.toHaveBeenCalled();
  });

  it("answers 503 without a profile", async () => {
    vi.mocked(storage.assignments.readByType).mockResolvedValue(null);
    await expect(generateSkill(input)).rejects.toBeInstanceOf(NoSkillProfileError);
  });
});
