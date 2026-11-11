import { beforeEach, describe, expect, it, vi } from "vitest";

const state = vi.hoisted(() => ({
  calls: [] as { method: string; path: string; body?: unknown; raw?: boolean }[],
  answer: undefined as unknown,
  failure: undefined as unknown,
}));

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
  logDir: "/nowhere",
}));
vi.mock("../../../config/index.js", () => ({
  default: { get: () => undefined },
  getDocumentAgent: () => undefined,
  getAppConfig: () => ({ port: 1 }),
}));
vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => ({ host: "portal.example.com" }),
  getChatContextSnapshot: () => undefined,
}));
vi.mock("../../storage/httpClient.js", async (importOriginal) => {
  const original = await importOriginal<typeof import("../../storage/httpClient.js")>();
  const answer = (call: (typeof state.calls)[number]) => {
    state.calls.push(call);
    if (state.failure) {
      throw state.failure;
    }
    return state.answer;
  };
  return {
    ...original,
    aiService: {
      get: async (path: string, opts?: { raw?: boolean }) =>
        answer({ method: "GET", path, raw: opts?.raw }),
      put: async (path: string, body: unknown, opts?: { raw?: boolean }) =>
        answer({ method: "PUT", path, body, raw: opts?.raw }),
    },
  };
});

const { AiServiceHttpError } = await import("../../storage/httpClient.js");
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
  taskLogFolderId: "17",
};

const envelope = (response: unknown) => ({ response, count: 1, status: 0, statusCode: 200 });

beforeEach(() => {
  state.calls = [];
  state.answer = undefined;
  state.failure = undefined;
});

describe("documentAgentController", () => {
  it("reads the AI service's settings, envelope and all", async () => {
    state.answer = envelope(DOCUMENT_AGENT_DEFAULTS);

    const res = await run(documentAgentController.getSettings);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual(envelope(DOCUMENT_AGENT_DEFAULTS));
    expect(state.calls).toEqual([{ method: "GET", path: "/document-agent/settings", raw: true }]);
  });

  it("hands what the tab sends to the AI service and answers what it stored", async () => {
    state.answer = envelope(SETTINGS);

    const res = await run(documentAgentController.setSettings, SETTINGS);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual(envelope(SETTINGS));
    expect(state.calls).toEqual([
      { method: "PUT", path: "/document-agent/settings", body: SETTINGS, raw: true },
    ]);
  });

  it("answers the AI service's refusal with its status", async () => {
    state.failure = new AiServiceHttpError(403, "Forbidden", "", "http://ai/x");

    const res = await run(documentAgentController.setSettings, SETTINGS);

    expect(res.statusCode).toBe(403);
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
    expect(state.calls).toEqual([]);
  });
});
