import { beforeEach, describe, expect, it, vi } from "vitest";
import { FakeDocspaceApiHttpError, logger } from "./mocks.js";

vi.mock("../../log.js", () => ({ default: logger }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
}));
vi.mock("../../storage/index.js", () => ({
  storage: {
    assignments: { readByType: vi.fn() },
    profiles: { readById: vi.fn() },
  },
}));
const customSync = vi.fn();
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  customSync,
}));

const readFileAsText = vi.fn<(file: { id: string }) => Promise<string>>();
vi.mock("../portalFiles.js", () => ({
  getPortalFile: vi.fn(),
  listFolderFiles: vi.fn(),
  PortalTextReader: class {
    readFileAsText = readFileAsText;
  },
  UnsupportedFileError: class UnsupportedFileError extends Error {},
}));

const portalFiles = await import("../portalFiles.js");
const { storage } = await import("../../storage/index.js");
type MetaSkipped = import("../common.js").MetaSkipped;
const {
  classifyModelError,
  collectFiles,
  fitBudget,
  formatDocuments,
  MAX_FILES,
  ModelCallError,
  NoContentError,
  parseJsonObject,
  readDocuments,
  resolveMetaProfile,
  runModel,
} = await import("../common.js");

const file = (id: string) => ({ id, title: `${id}.docx`, ext: ".docx" });

describe("resolveMetaProfile", () => {
  beforeEach(() => vi.clearAllMocks());

  it("prefers the GenerateMeta slot and falls back to Default", async () => {
    vi.mocked(storage.assignments.readByType).mockImplementation(async (type: string) =>
      type === "GenerateMeta" ? "p-meta" : "p-default",
    );
    vi.mocked(storage.profiles.readById).mockResolvedValue({ id: "p-meta" } as never);
    expect(await resolveMetaProfile()).toEqual({ id: "p-meta" });
    expect(storage.profiles.readById).toHaveBeenCalledWith("p-meta");

    vi.mocked(storage.assignments.readByType).mockImplementation(async (type: string) =>
      type === "GenerateMeta" ? null : "p-default",
    );
    await resolveMetaProfile();
    expect(storage.profiles.readById).toHaveBeenLastCalledWith("p-default");
  });

  it("is undefined when neither slot is bound", async () => {
    vi.mocked(storage.assignments.readByType).mockResolvedValue(null);
    expect(await resolveMetaProfile()).toBeUndefined();
    expect(storage.profiles.readById).not.toHaveBeenCalled();
  });
});

describe("collectFiles", () => {
  beforeEach(() => vi.clearAllMocks());

  it("takes the named files first, then the folders, deduplicated, and stops at the limit", async () => {
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id) => file(id));
    vi.mocked(portalFiles.listFolderFiles).mockResolvedValue(
      Array.from({ length: MAX_FILES + 1 }, (_, i) => file(`f${i}`)),
    );
    const skipped: MetaSkipped[] = [];
    const files = await collectFiles(["a", " a ", "f0"], ["10", "10"], skipped);
    expect(files).toHaveLength(MAX_FILES);
    expect(files.slice(0, 2).map((f) => f.id)).toEqual(["a", "f0"]);
    expect(portalFiles.listFolderFiles).toHaveBeenCalledTimes(1);
    expect(portalFiles.listFolderFiles).toHaveBeenCalledWith("10", 100);
    // 22 distinct candidates, 20 taken, the last two reported.
    expect(skipped.map((s) => s.reason)).toEqual(["limit", "limit"]);
  });

  it("reports a missing or forbidden file instead of failing the request", async () => {
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id) => {
      if (id === "gone") return undefined;
      if (id === "secret") throw new FakeDocspaceApiHttpError(403, "Forbidden", "u");
      return file(id);
    });
    const skipped: MetaSkipped[] = [];
    const files = await collectFiles(["gone", "secret", "ok"], [], skipped);
    expect(files.map((f) => f.id)).toEqual(["ok"]);
    expect(skipped).toEqual([
      { id: "gone", reason: "not_found" },
      { id: "secret", reason: "forbidden" },
    ]);
  });

  it("lets a folder the caller cannot open fail the request", async () => {
    vi.mocked(portalFiles.listFolderFiles).mockRejectedValue(
      new FakeDocspaceApiHttpError(404, "Not Found", "u"),
    );
    await expect(collectFiles([], ["1"], [])).rejects.toMatchObject({ status: 404 });
  });
});

describe("readDocuments", () => {
  beforeEach(() => vi.clearAllMocks());

  it("reads the files, cuts each to its budget and reports empty and unreadable ones", async () => {
    readFileAsText.mockImplementation(async ({ id }) => {
      if (id === "empty") return "  \n ";
      if (id === "img") throw new portalFiles.UnsupportedFileError(file("img") as never, "no");
      if (id === "secret") throw new FakeDocspaceApiHttpError(403, "Forbidden", "u");
      if (id === "boom") throw new Error("socket hang up");
      if (id === "long") return "x".repeat(50_000);
      return `text of ${id}`;
    });
    const skipped: MetaSkipped[] = [];
    const documents = await readDocuments(
      ["a", "empty", "img", "secret", "boom", "long"].map(file),
      skipped,
      new AbortController().signal,
    );
    expect(documents.map((d) => d.file.id)).toEqual(["a", "long"]);
    expect(documents[0]).toMatchObject({ text: "text of a", truncated: false });
    expect(documents[1]?.text).toHaveLength(40_000);
    expect(documents[1]?.truncated).toBe(true);
    expect(skipped.map((s) => [s.id, s.reason]).sort()).toEqual([
      ["boom", "failed"],
      ["empty", "empty"],
      ["img", "unsupported"],
      ["secret", "forbidden"],
    ]);
  });
});

describe("readDocuments on a portal failure", () => {
  beforeEach(() => vi.clearAllMocks());

  it("relays an expired session or an outage instead of reporting every file unreadable", async () => {
    readFileAsText.mockRejectedValue(new FakeDocspaceApiHttpError(401, "Unauthorized", "u"));
    await expect(
      readDocuments([file("a")], [], new AbortController().signal),
    ).rejects.toMatchObject({ status: 401 });
    readFileAsText.mockRejectedValue(new FakeDocspaceApiHttpError(503, "Unavailable", "u"));
    await expect(
      readDocuments([file("a")], [], new AbortController().signal),
    ).rejects.toMatchObject({ status: 503 });
  });
});

describe("fitBudget", () => {
  it("leaves a set under the total alone and cuts an oversized set evenly", () => {
    const small = [{ file: file("a"), text: "abc", truncated: false }];
    fitBudget(small);
    expect(small[0]).toMatchObject({ text: "abc", truncated: false });

    const big = Array.from({ length: 8 }, (_, i) => ({
      file: file(String(i)),
      text: "y".repeat(40_000),
      truncated: false,
    }));
    fitBudget(big);
    const total = big.reduce((sum, d) => sum + d.text.length, 0);
    expect(total).toBeLessThanOrEqual(240_000);
    expect(big.every((d) => d.truncated && d.text.length === 30_000)).toBe(true);
  });
});

describe("formatDocuments and parseJsonObject", () => {
  it("fences each document with its title and a truncation note", () => {
    const text = formatDocuments([
      { file: file("a"), text: "one", truncated: false },
      { file: file("b"), text: "two", truncated: true },
    ]);
    expect(text).toContain("===== Document 1: a.docx =====\none\n===== End of document 1 =====");
    expect(text).toContain("===== Document 2: b.docx (truncated) =====");
  });

  it("takes the outermost object out of fences and prose, and rejects anything else", () => {
    expect(parseJsonObject('```json\n{"a":1}\n```')).toEqual({ a: 1 });
    expect(parseJsonObject('Here you go: {"a":{"b":[1,2]}} thanks')).toEqual({ a: { b: [1, 2] } });
    expect(parseJsonObject("[1,2]")).toBeUndefined();
    expect(parseJsonObject("{not json}")).toBeUndefined();
    expect(parseJsonObject("")).toBeUndefined();
  });
});

describe("the model round's failures", () => {
  it("classifies a timeout, a throttled call and anything else", () => {
    const live = new AbortController().signal;
    const aborted = AbortSignal.abort();
    expect(classifyModelError(new Error("x"), aborted)).toMatchObject({ status: 504 });
    const abortError = Object.assign(new Error("aborted"), { name: "AbortError" });
    expect(classifyModelError(abortError, live)).toMatchObject({ status: 504 });
    expect(classifyModelError({ status: 429, message: "slow down" }, live)).toMatchObject({
      status: 429,
    });
    expect(classifyModelError(new Error("ECONNRESET"), live)).toMatchObject({
      status: 502,
      message: "The model request failed",
    });
    expect(classifyModelError("odd", live)).toBeInstanceOf(ModelCallError);
  });

  it("runs one round and wraps a provider failure", async () => {
    customSync.mockResolvedValueOnce({ role: "assistant", content: "ok" });
    const profile = { id: "p" } as never;
    await expect(
      runModel({ profile, systemPrompt: "s", userMessage: "u" }, new AbortController().signal),
    ).resolves.toEqual({ role: "assistant", content: "ok" });
    expect(customSync).toHaveBeenCalledWith(
      expect.objectContaining({
        profile,
        systemPrompt: "s",
        messages: [{ role: "user", content: "u" }],
      }),
    );
    customSync.mockRejectedValueOnce(new Error("invalid api key"));
    await expect(
      runModel({ profile, systemPrompt: "s", userMessage: "u" }, new AbortController().signal),
    ).rejects.toMatchObject({ status: 502 });
  });

  it("tells an empty selection from unreadable files", () => {
    expect(new NoContentError([]).message).toBe("The selection holds no files");
    expect(new NoContentError([{ id: "1", reason: "failed" }]).message).toBe(
      "None of the selected files could be read as text",
    );
  });
});
