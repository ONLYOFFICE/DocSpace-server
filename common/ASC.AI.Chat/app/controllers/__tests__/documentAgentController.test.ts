import { beforeEach, describe, expect, it, vi } from "vitest";

const state = vi.hoisted(() => ({
  admin: true,
  stored: new Map<string, unknown>(),
}));

vi.mock("../../storage/docspacePeopleApi.js", () => ({
  isPortalAdmin: async () => state.admin,
}));
vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn() },
  logDir: "/nowhere",
}));
vi.mock("../../../config/index.js", () => ({
  default: { get: () => undefined },
  getDocumentAgent: () => undefined,
  getAppConfig: () => ({ port: 1 }),
}));
vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => ({ host: "portal.example.com" }),
}));
vi.mock("../../storage/documentAgentSettingsStorage.js", async (importOriginal) => {
  const original =
    await importOriginal<typeof import("../../storage/documentAgentSettingsStorage.js")>();
  return {
    ...original,
    documentAgentSettingsStorage: () => ({
      read: async (key: string) => state.stored.get(key) ?? original.DOCUMENT_AGENT_DEFAULTS,
      write: async (key: string, settings: unknown) => {
        state.stored.set(key, settings);
        return settings;
      },
    }),
  };
});

const { DOCUMENT_AGENT_DEFAULTS } = await import("../../storage/documentAgentSettingsStorage.js");
const { documentAgentController } = await import("../documentAgentController.js");

type Handler = (req: unknown, res: unknown, next: unknown) => Promise<unknown>;

async function run(handler: unknown, body?: unknown) {
  const res = {
    statusCode: 200,
    body: undefined as unknown,
    headersSent: false,
    status(code: number) {
      this.statusCode = code;
      return this;
    },
    json(payload: unknown) {
      this.body = payload;
      this.headersSent = true;
      return this;
    },
    end() {
      this.headersSent = true;
    },
  };
  await (handler as Handler)({ body, method: "PUT", originalUrl: "/x" }, res, () => {});
  return res;
}

const SETTINGS = {
  ...DOCUMENT_AGENT_DEFAULTS,
  enabled: false,
  profileId: "p-1",
  reasoning: "low",
  maxToolCalls: 10,
  maxFileBytes: 1024,
  taskLog: true,
  taskLogFolderId: "f-1",
};

beforeEach(() => {
  state.admin = true;
  state.stored.clear();
});

describe("documentAgentController", () => {
  it("reads the defaults in the portal envelope", async () => {
    const res = await run(documentAgentController.getSettings);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual({
      response: DOCUMENT_AGENT_DEFAULTS,
      count: 1,
      status: 0,
      statusCode: 200,
    });
  });

  it("stores what an administrator sends, per portal, and reads it back", async () => {
    const put = await run(documentAgentController.setSettings, SETTINGS);
    expect(put.statusCode).toBe(200);
    expect(put.body).toMatchObject({ response: SETTINGS });
    expect([...state.stored.keys()]).toEqual(["portal.example.com"]);

    const get = await run(documentAgentController.getSettings);
    expect(get.body).toMatchObject({ response: SETTINGS });
  });

  it("refuses a user who is not an administrator", async () => {
    state.admin = false;

    const res = await run(documentAgentController.setSettings, SETTINGS);

    expect(res.statusCode).toBe(403);
    expect(state.stored.size).toBe(0);
  });

  it("refuses a limit over the agent's ceiling, an unknown level, a missing field", async () => {
    for (const body of [
      { ...SETTINGS, maxToolCalls: 151 },
      { ...SETTINGS, maxFileBytes: 32 * 1024 * 1024 + 1 },
      { ...SETTINGS, reasoning: "xhigh" },
      { ...SETTINGS, taskLog: undefined },
      { ...SETTINGS, profileId: "" },
      "nonsense",
    ]) {
      const res = await run(documentAgentController.setSettings, body);
      expect(res.statusCode, JSON.stringify(body)).toBe(400);
      expect(String((res.body as { error: string }).error)).toMatch(/^Invalid settings: /);
    }
    expect(state.stored.size).toBe(0);
  });
});
