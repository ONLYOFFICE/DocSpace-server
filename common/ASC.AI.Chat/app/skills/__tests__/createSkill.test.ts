import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../../requestContext.js", () => ({ markForwardHeadersToProvider: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: { assignments: { readByType: vi.fn() }, profiles: { readById: vi.fn() } },
}));
vi.mock("../../storage/docspaceFilesApi.js", () => ({ getFolderInfo: vi.fn() }));
vi.mock("../../storage/docspaceFileTransfer.js", () => ({ uploadFile: vi.fn() }));
vi.mock("../../forms/suggestedQuestions.js", () => ({ extractText: vi.fn() }));

const { getFolderInfo } = await import("../../storage/docspaceFilesApi.js");
const { uploadFile } = await import("../../storage/docspaceFileTransfer.js");
const { buildSkillTemplate, createSkill, MAX_TITLE_LENGTH } = await import("../createSkill.js");

const INSTRUCTIONS_ROOM = { folderType: 38, roomType: 10, canCreate: true };

beforeEach(() => {
  vi.mocked(getFolderInfo).mockReset().mockResolvedValue(INSTRUCTIONS_ROOM);
  vi.mocked(uploadFile).mockReset().mockResolvedValue({ id: "77", title: "Sales report.md" });
});

describe("buildSkillTemplate", () => {
  it("starts with the frontmatter the Files service requires and a body to fill in", () => {
    const text = buildSkillTemplate("Sales report: Q3 <draft>");
    expect(
      text.startsWith('---\nname: "sales-report-q3-draft"\ndescription: "Sales report: Q3"\n---\n\n'),
    ).toBe(true);
    expect(text).toContain("# Sales report: Q3 <draft>\n");
    expect(text).toContain("## Instructions");
  });
});

describe("createSkill", () => {
  it("saves the template into the room as <title>.md", async () => {
    const file = await createSkill({ destId: "12", title: "  Sales report.md " });
    expect(file).toEqual({ id: "77", title: "Sales report.md" });
    const [folderId, title, bytes, mediaType] = vi.mocked(uploadFile).mock.calls[0]!;
    expect([folderId, title, mediaType]).toEqual(["12", "Sales report.md", "text/markdown"]);
    expect(new TextDecoder().decode(bytes)).toBe(buildSkillTemplate("Sales report"));
  });

  it.each([
    ["an empty title", "   "],
    ["only an extension", ".md"],
    ["no letter or digit", "!!! ---"],
    ["a title over the limit", "a".repeat(MAX_TITLE_LENGTH + 1)],
  ])("refuses %s before the room is read", async (_case, title) => {
    await expect(createSkill({ destId: "12", title })).rejects.toMatchObject({ status: 400 });
    expect(getFolderInfo).not.toHaveBeenCalled();
    expect(uploadFile).not.toHaveBeenCalled();
  });

  it.each([
    ["a missing folder", undefined, 404],
    ["a room of another type", { folderType: 19, roomType: 5, canCreate: true }, 400],
    ["a plain folder", { folderType: 1, canCreate: true }, 400],
    ["a room the caller cannot create in", { ...INSTRUCTIONS_ROOM, canCreate: false }, 403],
  ])("writes nothing into %s", async (_case, info, status) => {
    vi.mocked(getFolderInfo).mockResolvedValue(info);
    await expect(createSkill({ destId: "12", title: "Sales report" })).rejects.toMatchObject({
      status,
    });
    expect(uploadFile).not.toHaveBeenCalled();
  });
});
