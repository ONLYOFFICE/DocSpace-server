import { promises as fs } from "fs";
import os from "os";
import path from "path";

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../../config/index.js", () => ({ getDocumentAgent: () => undefined }));
vi.mock("../../log.js", () => ({ default: { info: vi.fn(), warn: vi.fn() }, logDir: "/nowhere" }));
vi.mock("../../requestContext.js", () => ({ getForwardedHeaders: () => headers }));

let headers: Record<string, string> = {};

const { DOCUMENT_AGENT_DEFAULTS, DocumentAgentSettingsStorage, tenantKey } =
  await import("../documentAgentSettingsStorage.js");

let dir: string;

beforeEach(async () => {
  dir = await fs.mkdtemp(path.join(os.tmpdir(), "docagent-settings-"));
  headers = {};
});

afterEach(async () => {
  await fs.rm(dir, { recursive: true, force: true });
});

const SAVED = {
  ...DOCUMENT_AGENT_DEFAULTS,
  enabled: false,
  profileId: "p-1",
  reasoning: "high" as const,
  maxToolCalls: 40,
  taskLog: true,
  taskLogFolderId: "f-9",
};

describe("DocumentAgentSettingsStorage", () => {
  it("answers the defaults for a portal that saved nothing, file or no file", async () => {
    const storage = new DocumentAgentSettingsStorage(path.join(dir, "s.json"));

    expect(await storage.read("a.example.com")).toEqual(DOCUMENT_AGENT_DEFAULTS);
    await storage.write("b.example.com", SAVED);
    expect(await storage.read("a.example.com")).toEqual(DOCUMENT_AGENT_DEFAULTS);
  });

  it("keeps what a portal saved, across instances, apart from other portals", async () => {
    const file = path.join(dir, "deep", "s.json");
    await new DocumentAgentSettingsStorage(file).write("a.example.com", SAVED);

    const again = new DocumentAgentSettingsStorage(file);
    expect(await again.read("a.example.com")).toEqual(SAVED);
    expect(await again.read("b.example.com")).toEqual(DOCUMENT_AGENT_DEFAULTS);
    expect(JSON.parse(await fs.readFile(file, "utf8"))).toEqual({ "a.example.com": SAVED });
  });

  it("drops a damaged entry and keeps the rest", async () => {
    const file = path.join(dir, "s.json");
    await fs.writeFile(
      file,
      JSON.stringify({
        "a.example.com": SAVED,
        "b.example.com": { ...SAVED, maxToolCalls: 10_000 },
        "c.example.com": "nonsense",
      }),
    );
    const storage = new DocumentAgentSettingsStorage(file);

    expect(await storage.read("a.example.com")).toEqual(SAVED);
    expect(await storage.read("b.example.com")).toEqual(DOCUMENT_AGENT_DEFAULTS);
    expect(await storage.read("c.example.com")).toEqual(DOCUMENT_AGENT_DEFAULTS);
  });

  it("serialises concurrent writes", async () => {
    const file = path.join(dir, "s.json");
    const storage = new DocumentAgentSettingsStorage(file);

    await Promise.all([
      storage.write("a.example.com", SAVED),
      storage.write("b.example.com", { ...SAVED, maxToolCalls: 1 }),
      storage.write("a.example.com", { ...SAVED, maxToolCalls: 2 }),
    ]);

    const again = new DocumentAgentSettingsStorage(file);
    expect((await again.read("a.example.com")).maxToolCalls).toBe(2);
    expect((await again.read("b.example.com")).maxToolCalls).toBe(1);
  });
});

describe("tenantKey", () => {
  it("is the forwarded host, lowercased, else the plain host, else a default", () => {
    headers = { "x-forwarded-host": "Portal.Example.com, proxy", host: "ignored" };
    expect(tenantKey()).toBe("portal.example.com");
    headers = { host: "Localhost:8092" };
    expect(tenantKey()).toBe("localhost:8092");
    headers = {};
    expect(tenantKey()).toBe("default");
  });
});
