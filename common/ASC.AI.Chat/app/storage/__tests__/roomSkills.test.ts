import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
const aiServiceGet = vi.fn();
vi.mock("../httpClient.js", () => ({
  aiService: { get: aiServiceGet },
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

const { getContextFolders, getKnowledgeFolders, getRoomSkill, getRoomSkills, getRoomsSkills } =
  await import("../roomSkills.js");

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

const catalog = (...rooms: { id: number; skills: { id: string; name: string; description: string }[] }[]) => ({
  rooms: rooms.map((room) => ({ title: `Room ${room.id}`, logo: null, ...room })),
});

describe("skills catalog", () => {
  beforeEach(() => {
    aiServiceGet.mockReset();
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("reads the rooms' skills from the .NET catalog, 20 rooms a call", async () => {
    aiServiceGet.mockImplementation(async (_path: string, opts: { query: { roomIds: string[] } }) =>
      catalog(...opts.query.roomIds.map((id) => ({ id: Number(id), skills: [{ id: `f${id}`, name: "n", description: "d" }] }))),
    );
    const ids = Array.from({ length: 25 }, (_, i) => String(i + 1));

    const skills = await getRoomsSkills(ids);

    expect(aiServiceGet).toHaveBeenCalledTimes(2);
    expect(aiServiceGet.mock.calls[0]?.[0]).toBe("/rooms/skills");
    expect(aiServiceGet.mock.calls[0]?.[1].query.roomIds).toHaveLength(20);
    expect(aiServiceGet.mock.calls[1]?.[1].query.roomIds).toEqual(["21", "22", "23", "24", "25"]);
    expect(skills.get("25")).toEqual([{ id: "f25", name: "n", description: "d" }]);
  });

  it("answers 404 for a room the catalog leaves out", async () => {
    aiServiceGet.mockResolvedValue(catalog());
    await expect(getRoomSkills("7")).rejects.toMatchObject({ status: 404 });
  });

  it("reads a skill body only for a skill of that room", async () => {
    aiServiceGet.mockResolvedValue(catalog({ id: 7, skills: [{ id: "f1", name: "tone", description: "Tone." }] }));
    await expect(getRoomSkill("7", "f2")).rejects.toMatchObject({ status: 404 });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("offers only the Instructions rooms that hold a skill", async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => ({
        response: {
          folders: [
            { id: 7, title: "Brand voice", roomType: 10 },
            { id: 8, title: "Empty", roomType: 10 },
          ],
          total: 2,
        },
      }),
    });
    aiServiceGet.mockResolvedValue(
      catalog({ id: 7, skills: [{ id: "f1", name: "tone", description: "Tone." }] }, { id: 8, skills: [] }),
    );

    const folders = await getContextFolders();

    expect(folders).toEqual([{ cloud: "docspace", rooms: [{ id: "7", name: "Brand voice" }] }]);
    expect(aiServiceGet).toHaveBeenCalledTimes(1);
  });
});
