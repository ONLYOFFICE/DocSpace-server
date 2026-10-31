import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../httpClient.js", () => ({
  proxyBaseUrl: "http://portal",
  withTimeout: () => ({ signal: undefined, cancel: () => {} }),
}));
class FakeDocspaceApiHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly url: string,
  ) {
    super(statusText);
  }
}
vi.mock("../docspaceFilesApi.js", () => ({ DocspaceApiHttpError: FakeDocspaceApiHttpError }));
vi.mock("../../requestContext.js", () => ({
  countFilesApiRead: vi.fn(),
  getForwardedHeaders: () => ({}),
}));

const { getKnowledgeFolders } = await import("../roomSkills.js");

const fetchMock = vi.fn();

describe("getKnowledgeFolders", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("lists the Knowledge rooms of the AI rooms root, not of Rooms", async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => ({
        response: { folders: [{ id: 7, title: "Docs", roomType: 11 }], total: 1 },
      }),
    });

    const folders = await getKnowledgeFolders();

    const url = new URL(String(fetchMock.mock.calls[0]?.[0]));
    expect(url.pathname).toBe("/api/2.0/files/rooms");
    expect(url.searchParams.get("type")).toBe("11");
    expect(url.searchParams.get("searchArea")).toBe("AiHub");
    expect(folders).toHaveLength(1);
  });
});
