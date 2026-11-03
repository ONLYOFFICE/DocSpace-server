import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
const getAllAssignments = vi.fn();
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@onlyoffice/ai-chat/core")>();
  return {
    ...actual,
    AssignmentsEngine: class {
      getAllAssignments = getAllAssignments;
    },
  };
});
const readAllForClient = vi.fn();
vi.mock("../../storage/index.js", () => ({
  storage: { profiles: { readAllForClient } },
}));
const assertEntityAccessible = vi.fn(async () => {});
vi.mock("../../storage/docspaceFilesApi.js", () => ({ assertEntityAccessible }));

const { assignmentsController } = await import("../assignmentsController.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const call = async (query: Record<string, string>) => {
  const res = { status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  const next = vi.fn();
  await (assignmentsController.getAllAssignments as unknown as Handler)({ query }, res, next);
  const failure = next.mock.calls[0];
  if (failure) throw failure[0];
  return res.json.mock.calls[0]?.[0];
};

describe("assignmentsController.getAllAssignments", () => {
  beforeEach(() => {
    getAllAssignments.mockReset();
    readAllForClient.mockReset();
    getAllAssignments.mockResolvedValue({ Chat: "p1", Default: "p2", Code: "gone" });
    readAllForClient.mockResolvedValue([
      { id: "p1", name: "Chatty", capabilities: 0x01 },
      { id: "p2", name: "Default", capabilities: null },
      { id: "p3", name: "Pictures", capabilities: 0x02 },
    ]);
  });

  it("answers the id map without expand", async () => {
    expect(await call({})).toEqual({ Chat: "p1", Default: "p2", Code: "gone" });
    expect(readAllForClient).not.toHaveBeenCalled();
  });

  it("answers the assigned profiles and the catalogue counts with expand=profile", async () => {
    const answer = await call({ expand: "profile" });
    expect(answer).toEqual({
      assignments: {
        Chat: { id: "p1", name: "Chatty", capabilities: 0x01 },
        Default: { id: "p2", name: "Default", capabilities: null },
      },
      profilesTotal: 3,
      // An unknown mask may chat; the image-only one may not.
      chatProfilesTotal: 2,
    });
  });

  it("forwards entityId to the engine either way", async () => {
    await call({ entityId: "room-7", expand: "profile" });
    expect(assertEntityAccessible).toHaveBeenCalledWith("room-7");
    expect(getAllAssignments).toHaveBeenCalledWith("room-7");
  });
});
