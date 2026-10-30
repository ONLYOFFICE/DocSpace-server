import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));

const aiServiceGet = vi.fn();
const aiServicePost = vi.fn();
const aiServiceDelete = vi.fn();
class FakeAiServiceHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly body = "",
  ) {
    super(statusText);
  }
}
vi.mock("../httpClient.js", () => ({
  aiService: { get: aiServiceGet, post: aiServicePost, delete: aiServiceDelete },
  AiServiceHttpError: FakeAiServiceHttpError,
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
const getRoomSkills = vi.fn();
vi.mock("../roomSkills.js", () => ({ getRoomSkills }));
vi.mock("../../requestContext.js", () => ({
  countFilesApiRead: vi.fn(),
  getForwardedHeaders: () => ({}),
}));

const {
  aiProjectBlock,
  listProjectKnowledge,
  listProjectSkills,
  readProjectRooms,
  roomIdsOf,
  syncProjectRooms,
  withAiProjectBlock,
} = await import("../aiProjectRooms.js");

const rooms = {
  instructionsRooms: [{ id: 7, title: "Brand voice", logo: null }],
  knowledgeRooms: [{ id: 9, title: "Docs", logo: { color: "FF6600" } }],
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe("readProjectRooms", () => {
  it("reads the two lists the server answers, dropping malformed entries", async () => {
    aiServiceGet.mockResolvedValue({
      instructionsRooms: [{ id: 7, title: "Brand voice" }, { title: "no id" }],
      knowledgeRooms: [{ id: 9, title: "Docs", logo: { color: "FF6600" } }],
    });
    expect(await readProjectRooms(42)).toEqual(rooms);
    expect(aiServiceGet).toHaveBeenCalledWith("/projects/42/rooms");
  });
});

describe("roomIdsOf", () => {
  it("keeps a sent list as positive integers once each, and an unsent list as undefined", () => {
    expect(
      roomIdsOf({ instructionsRoomIds: [7, "8", 7, 0, -1, "x"], knowledgeRoomIds: null }),
    ).toEqual({
      instructionsRoomIds: [7, 8],
      knowledgeRoomIds: undefined,
    });
    expect(roomIdsOf(undefined)).toEqual({
      instructionsRoomIds: undefined,
      knowledgeRoomIds: undefined,
    });
  });
});

describe("syncProjectRooms", () => {
  it("links the rooms missing from a sent list and unlinks the ones left out", async () => {
    const warnings = await syncProjectRooms(
      42,
      { instructionsRoomIds: [7, 8], knowledgeRoomIds: [] },
      rooms,
    );
    expect(warnings).toEqual([]);
    expect(aiServiceDelete).toHaveBeenCalledWith("/projects/42/rooms", { body: { roomIds: [9] } });
    expect(aiServicePost).toHaveBeenCalledWith("/projects/42/rooms", { roomIds: [8] });
  });

  it("keeps the rooms of a list not sent and calls nothing when there is no change", async () => {
    await syncProjectRooms(42, { instructionsRoomIds: [7] }, rooms);
    expect(aiServiceDelete).not.toHaveBeenCalled();
    expect(aiServicePost).not.toHaveBeenCalled();
  });

  it("turns a refused link into a warning with the server's message", async () => {
    aiServicePost.mockRejectedValue(
      new FakeAiServiceHttpError(
        400,
        "Bad Request",
        JSON.stringify({ error: { message: "Room 8 is not linkable" } }),
      ),
    );
    const warnings = await syncProjectRooms(42, { instructionsRoomIds: [7, 8] }, rooms);
    expect(warnings).toEqual([
      { code: "roomUnavailable", roomId: 8, subjectId: null, message: "Room 8 is not linkable" },
    ]);
  });
});

describe("aiProjectBlock / withAiProjectBlock", () => {
  it("builds the client's block over the server's rooms", () => {
    const block = aiProjectBlock({ security: { EditRoom: true } }, rooms, [
      { code: "modelNotStored", roomId: null, subjectId: null, message: "m" },
    ]);
    expect(block).toEqual({
      profile: null,
      instructionsRooms: [
        { id: 7, title: "Brand voice", roomType: 10, available: true, archived: false, logo: null },
      ],
      knowledgeRooms: [
        {
          id: 9,
          title: "Docs",
          roomType: 11,
          available: true,
          archived: false,
          logo: { color: "FF6600" },
        },
      ],
      canEditSettings: true,
      warnings: [{ code: "modelNotStored", roomId: null, subjectId: null, message: "m" }],
    });
  });

  it("sets the block on the envelope's response and leaves anything else alone", () => {
    const out = withAiProjectBlock({ response: { id: 42, security: {} }, status: 0 }, rooms) as {
      response: { aiProject: { canEditSettings: boolean; warnings?: unknown } };
    };
    expect(out.response.aiProject.canEditSettings).toBe(false);
    expect(out.response.aiProject.warnings).toBeUndefined();
    expect(withAiProjectBlock("nope", rooms)).toBe("nope");
  });
});

describe("listProjectSkills", () => {
  it("lists each Instructions room with its skills, an unreadable one as unavailable", async () => {
    aiServiceGet.mockResolvedValue({
      instructionsRooms: [
        { id: 7, title: "A" },
        { id: 8, title: "B" },
      ],
      knowledgeRooms: [],
    });
    getRoomSkills.mockImplementation(async (id: string) => {
      if (id === "7") return [{ id: "f1", name: "tone", description: "Brand voice." }];
      throw new FakeDocspaceApiHttpError(403, "Forbidden", "u");
    });
    expect(await listProjectSkills(42)).toEqual({
      rooms: [
        {
          id: 7,
          title: "A",
          logo: null,
          available: true,
          skills: [{ id: "f1", name: "tone", description: "Brand voice." }],
        },
        { id: 8, title: "B", logo: null, available: false, skills: [] },
      ],
    });
  });
});

describe("listProjectKnowledge", () => {
  it("lists each Knowledge room's files with their vectorization state and counters", async () => {
    aiServiceGet.mockResolvedValue({
      instructionsRooms: [],
      knowledgeRooms: [{ id: 9, title: "Docs" }],
    });
    const fetchMock = vi.fn(async () => ({
      ok: true,
      json: async () => ({
        response: {
          total: 3,
          files: [
            {
              id: 1,
              title: "a.docx",
              fileExst: ".docx",
              vectorizationStatus: 1,
              security: { Vectorization: false },
            },
            {
              id: 2,
              title: "b.pdf",
              fileExst: ".pdf",
              vectorizationStatus: 2,
              security: { Vectorization: true },
            },
          ],
        },
      }),
    }));
    vi.stubGlobal("fetch", fetchMock);
    try {
      const { rooms: listed } = await listProjectKnowledge(42);
      expect(listed[0]).toMatchObject({
        id: 9,
        available: true,
        totalFiles: 3,
        indexedFiles: 1,
        failedFiles: 1,
        truncated: true,
      });
      expect((listed[0] as { files: unknown[] }).files).toEqual([
        {
          id: 1,
          title: "a.docx",
          fileExst: ".docx",
          indexed: true,
          vectorizationStatus: 1,
          security: { Vectorization: false },
        },
        {
          id: 2,
          title: "b.pdf",
          fileExst: ".pdf",
          indexed: false,
          vectorizationStatus: 2,
          security: { Vectorization: true },
        },
      ]);
      expect(String((fetchMock.mock.calls[0] as unknown[])[0])).toBe(
        "http://portal/api/2.0/files/9?filterType=1&count=100&startIndex=0",
      );
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it("reads a large room page by page, up to the limit", async () => {
    aiServiceGet.mockResolvedValue({
      instructionsRooms: [],
      knowledgeRooms: [{ id: 9, title: "Docs" }],
    });
    const file = (id: number) => ({
      id,
      title: `${id}.docx`,
      fileExst: ".docx",
      vectorizationStatus: 1,
    });
    const fetchMock = vi.fn(async (url: string) => {
      const startIndex = Number(new URL(url).searchParams.get("startIndex"));
      return {
        ok: true,
        json: async () => ({
          response: {
            total: 1250,
            files: Array.from({ length: 100 }, (_, i) => file(startIndex + i + 1)),
          },
        }),
      };
    });
    vi.stubGlobal("fetch", fetchMock);
    try {
      const { rooms: listed } = await listProjectKnowledge(42);
      expect(fetchMock).toHaveBeenCalledTimes(10);
      expect(listed[0]).toMatchObject({ totalFiles: 1250, indexedFiles: 1000, truncated: true });
      expect((listed[0] as { files: unknown[] }).files).toHaveLength(1000);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it("lists a room whose files cannot be read as unavailable", async () => {
    aiServiceGet.mockResolvedValue({
      instructionsRooms: [],
      knowledgeRooms: [{ id: 9, title: "Docs" }],
    });
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({ ok: false, status: 403, statusText: "Forbidden" })),
    );
    try {
      const { rooms: listed } = await listProjectKnowledge(42);
      expect(listed[0]).toMatchObject({ id: 9, available: false, files: [], totalFiles: 0 });
    } finally {
      vi.unstubAllGlobals();
    }
  });
});
