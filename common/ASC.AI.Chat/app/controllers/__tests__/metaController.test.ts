import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
class FakeAiServiceHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
  ) {
    super(statusText);
  }
}
vi.mock("../../storage/httpClient.js", () => ({ AiServiceHttpError: FakeAiServiceHttpError }));
const assertAiAccess = vi.fn(async () => {});
vi.mock("../aiAccess.js", () => ({ assertAiAccess }));
class FakeDocspaceApiHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
  ) {
    super(statusText);
  }
}
const primeSourceMeta = vi.fn();
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
  primeSourceMeta,
}));
const generateMeta = vi.fn();
vi.mock("../../meta/generateMeta.js", () => ({
  generateMeta,
  MAX_FILES: 20,
  NoContentError: class NoContentError extends Error {
    readonly status = 422;
    constructor(public readonly skipped: unknown[]) {
      super("none readable");
    }
  },
  NoProfileError: class NoProfileError extends Error {
    readonly status = 503;
    constructor() {
      super("No model is configured");
    }
  },
  ModelCallError: class ModelCallError extends Error {
    constructor(
      public readonly status: number,
      public readonly code: string,
      message: string,
      public readonly providerMessage?: string,
    ) {
      super(message);
    }
  },
}));
const fillMeta = vi.fn();
vi.mock("../../meta/fillMeta.js", () => ({ fillMeta }));

const { metaController } = await import("../metaController.js");
const { ModelCallError, NoContentError, NoProfileError } =
  await import("../../meta/generateMeta.js");

type Handler = (req: never, res: never, next: never) => unknown;

function run(handler: unknown, body: unknown) {
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
  const req = { body, method: "POST", originalUrl: "/ai/meta/x" };
  return Promise.resolve((handler as Handler)(req as never, res as never, vi.fn() as never)).then(
    () => res,
  );
}

describe("metaController.generate", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    assertAiAccess.mockResolvedValue(undefined);
  });

  it("answers the AI-access refusal before touching the body", async () => {
    assertAiAccess.mockRejectedValueOnce(new FakeAiServiceHttpError(403, "Forbidden"));
    const res = await run(metaController.generate, { kind: "file", id: 1 });
    expect(res.statusCode).toBe(403);
    expect(generateMeta).not.toHaveBeenCalled();
    expect(primeSourceMeta).not.toHaveBeenCalled();
  });

  it("relays the model-call outcomes with their status, code and the provider's words", async () => {
    for (const [status, code, message, providerMessage] of [
      [429, "rate_limit", "The model provider is rate-limiting requests", "429 Rate limit reached"],
      [
        402,
        "insufficient_funds",
        "The model provider refused the request for lack of credit",
        "402 Insufficient Balance",
      ],
      [502, "auth", "The model request failed", "401 Incorrect API key provided"],
      [504, "timeout", "The model did not answer within the time budget", undefined],
    ] as const) {
      generateMeta.mockRejectedValueOnce(
        new ModelCallError(status, code, message, providerMessage),
      );
      const res = await run(metaController.generate, { kind: "file", id: 1 });
      expect(res.statusCode).toBe(status);
      expect(res.body).toEqual(
        providerMessage === undefined
          ? { error: message, code }
          : { error: message, code, providerMessage },
      );
    }
  });

  it.each([
    [null, "body must be an object"],
    [{}, 'kind must be "file"'],
    [{ kind: "room", id: 1 }, 'kind must be "file"'],
    // Folders are refused for now, until it is settled what their metadata
    // should describe; the client offers the actions on files only too.
    [{ kind: "folder", id: 1 }, 'kind must be "file"'],
    [{ kind: "file" }, "id must be the integer id of a file"],
    [{ kind: "file", id: "" }, "id must be the integer id of a file"],
    [{ kind: "file", id: 1.5 }, "id must be the integer id of a file"],
    [{ kind: "file", id: "sbox-1" }, "id must be the integer id of a file"],
    [{ kind: "file", id: [1, 2] }, "id must be the integer id of a file"],
    [{ kind: "file", id: 1, prompt: 5 }, "prompt must be a string"],
  ])("rejects %j with 400", async (body, message) => {
    const res = await run(metaController.generate, body);
    expect(res.statusCode).toBe(400);
    expect(res.body).toEqual({ error: message });
    expect(generateMeta).not.toHaveBeenCalled();
    expect(primeSourceMeta).not.toHaveBeenCalled();
  });

  it("normalizes the entry, trims the prompt, attributes the round and answers the result", async () => {
    generateMeta.mockResolvedValue({ fields: [], sources: [], skipped: [] });
    const res = await run(metaController.generate, {
      kind: "file",
      id: " 10 ",
      prompt: "  dates ",
    });
    expect(res.statusCode).toBe(200);
    expect(generateMeta).toHaveBeenCalledWith({ kind: "file", id: "10", prompt: "dates" });
    expect(primeSourceMeta).toHaveBeenCalledWith("10", "file");
  });

  it("attributes a file request to the file", async () => {
    generateMeta.mockResolvedValue({ fields: [], sources: [], skipped: [] });
    await run(metaController.generate, { kind: "file", id: 5 });
    expect(generateMeta).toHaveBeenCalledWith({ kind: "file", id: "5", prompt: undefined });
    expect(primeSourceMeta).toHaveBeenCalledWith("5", "file");
  });

  it("answers 422 with the skipped list when nothing could be read, and relays other statuses", async () => {
    generateMeta.mockRejectedValueOnce(new NoContentError([{ id: "1", reason: "unsupported" }]));
    let res = await run(metaController.generate, { kind: "file", id: 1 });
    expect(res.statusCode).toBe(422);
    expect(res.body).toEqual({
      error: "none readable",
      skipped: [{ id: "1", reason: "unsupported" }],
    });

    generateMeta.mockRejectedValueOnce(new FakeDocspaceApiHttpError(404, "Not Found"));
    res = await run(metaController.generate, { kind: "file", id: 9 });
    expect(res.statusCode).toBe(404);

    generateMeta.mockRejectedValueOnce(new NoProfileError());
    res = await run(metaController.generate, { kind: "file", id: 1 });
    expect(res.statusCode).toBe(503);
    expect(res.body).toEqual({ error: "No model is configured" });

    generateMeta.mockRejectedValueOnce(new Error("secret internals"));
    res = await run(metaController.generate, { kind: "file", id: 1 });
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: "Internal server error" });
  });
});

describe("metaController.fill", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    assertAiAccess.mockResolvedValue(undefined);
  });

  it.each([
    [{}, 'kind must be "file"'],
    [{ kind: "file", id: 0 }, "id must be the integer id of a file"],
    [{ kind: "file", id: "-5" }, "id must be the integer id of a file"],
    [{ kind: "room", id: 1 }, 'kind must be "file"'],
    [{ kind: "folder", id: 1 }, 'kind must be "file"'],
    [{ kind: "file" }, "id must be the integer id of a file"],
    [{ kind: "file", id: "abc-123" }, "id must be the integer id of a file"],
    [{ kind: "file", id: 1.5 }, "id must be the integer id of a file"],
    [{ kind: "file", id: 1, prompt: [] }, "prompt must be a string"],
    [{ kind: "file", id: 1, overwrite: "yes" }, "overwrite must be a boolean"],
    [{ kind: "file", id: 1, dryRun: 1 }, "dryRun must be a boolean"],
  ])("rejects %j with 400", async (body, message) => {
    const res = await run(metaController.fill, body);
    expect(res.statusCode).toBe(400);
    expect(res.body).toEqual({ error: message });
    expect(fillMeta).not.toHaveBeenCalled();
  });

  it("passes the normalized input on, overwrite false and dryRun true by default, and attributes the round to the entry", async () => {
    fillMeta.mockResolvedValue({
      metadata: {},
      filled: [],
      unresolved: [],
      sources: [],
      skipped: [],
    });
    const res = await run(metaController.fill, { kind: "file", id: " 12 ", prompt: " totals " });
    expect(res.statusCode).toBe(200);
    expect(fillMeta).toHaveBeenCalledWith({
      kind: "file",
      id: "12",
      prompt: "totals",
      overwrite: false,
      dryRun: true,
    });
    expect(primeSourceMeta).toHaveBeenCalledWith("12", "file");

    await run(metaController.fill, { kind: "file", id: 3, overwrite: true, dryRun: false });
    expect(fillMeta).toHaveBeenLastCalledWith({
      kind: "file",
      id: "3",
      prompt: undefined,
      overwrite: true,
      dryRun: false,
    });

    await run(metaController.fill, { kind: "file", id: 3, dryRun: null });
    expect(fillMeta).toHaveBeenLastCalledWith(expect.objectContaining({ dryRun: true }));
    expect(primeSourceMeta).toHaveBeenLastCalledWith("3", "file");
  });

  it("answers 422 with the skipped list when the content could not be read, and 503 without a model", async () => {
    fillMeta.mockRejectedValueOnce(new NoContentError([{ id: "3", reason: "empty" }]));
    let res = await run(metaController.fill, { kind: "file", id: 3 });
    expect(res.statusCode).toBe(422);
    expect(res.body).toEqual({ error: "none readable", skipped: [{ id: "3", reason: "empty" }] });

    fillMeta.mockRejectedValueOnce(new NoProfileError());
    res = await run(metaController.fill, { kind: "file", id: 3 });
    expect(res.statusCode).toBe(503);
    expect(res.body).toEqual({ error: "No model is configured" });

    fillMeta.mockRejectedValueOnce(new ModelCallError(504, "timeout", "late"));
    res = await run(metaController.fill, { kind: "file", id: 3 });
    expect(res.statusCode).toBe(504);

    assertAiAccess.mockRejectedValueOnce(new FakeAiServiceHttpError(403, "Forbidden"));
    res = await run(metaController.fill, { kind: "file", id: 3 });
    expect(res.statusCode).toBe(403);
    expect(fillMeta).toHaveBeenCalledTimes(3);
  });
});
