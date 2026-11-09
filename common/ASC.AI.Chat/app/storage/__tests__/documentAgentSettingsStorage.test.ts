import { promises as fs } from "fs";
import os from "os";
import path from "path";

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../../config/index.js", () => ({ getDocumentAgent: () => undefined }));
const warn = vi.fn();
vi.mock("../../log.js", () => ({ default: { info: vi.fn(), warn }, logDir: "/nowhere" }));
vi.mock("../../requestContext.js", () => ({ getForwardedHeaders: () => headers }));

let headers: Record<string, string> = {};

const {
  DOCUMENT_AGENT_DEFAULTS,
  DOCUMENT_AGENT_UNREADABLE,
  DocumentAgentSettingsStorage,
  readDocumentAgentSettingsForTask,
  tenantKey,
} = await import("../documentAgentSettingsStorage.js");

let dir: string;

beforeEach(async () => {
  dir = await fs.mkdtemp(path.join(os.tmpdir(), "docagent-settings-"));
  headers = {};
  warn.mockClear();
});

afterEach(async () => {
  vi.restoreAllMocks();
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

describe("DocumentAgentSettingsStorage when the file fails", () => {
  it("a failed write is not served as saved, and the next save writes the file", async () => {
    const file = path.join(dir, "s.json");
    const storage = new DocumentAgentSettingsStorage(file);
    await storage.write("a.example.com", SAVED);

    const rename = vi.spyOn(fs, "rename").mockRejectedValueOnce(
      Object.assign(new Error("no space left"), { code: "ENOSPC" }),
    );
    await expect(
      storage.write("a.example.com", { ...SAVED, maxToolCalls: 7 }),
    ).rejects.toThrow("no space left");
    expect((await storage.read("a.example.com")).maxToolCalls).toBe(SAVED.maxToolCalls);

    rename.mockRestore();
    await storage.write("a.example.com", { ...SAVED, maxToolCalls: 8 });
    expect((await storage.read("a.example.com")).maxToolCalls).toBe(8);
    const again = new DocumentAgentSettingsStorage(file);
    expect((await again.read("a.example.com")).maxToolCalls).toBe(8);
  });

  it("a write queued behind a failed one still lands, with the earlier entries", async () => {
    const file = path.join(dir, "s.json");
    const storage = new DocumentAgentSettingsStorage(file);
    await storage.write("a.example.com", SAVED);

    vi.spyOn(fs, "rename").mockRejectedValueOnce(new Error("disk gone"));
    const failed = storage.write("b.example.com", { ...SAVED, maxToolCalls: 1 });
    const next = storage.write("c.example.com", { ...SAVED, maxToolCalls: 3 });

    await expect(failed).rejects.toThrow("disk gone");
    await next;
    const saved = JSON.parse(await fs.readFile(file, "utf8"));
    expect(Object.keys(saved).sort()).toEqual(["a.example.com", "c.example.com"]);
  });

  it("a damaged file fails the read each time, and a repaired one is read again", async () => {
    const file = path.join(dir, "s.json");
    await fs.writeFile(file, "{ not json");
    const storage = new DocumentAgentSettingsStorage(file);

    await expect(storage.read("a.example.com")).rejects.toThrow();
    await expect(storage.write("a.example.com", SAVED)).rejects.toThrow();
    // The damaged file is left as it is: other portals' entries are in it.
    expect(await fs.readFile(file, "utf8")).toBe("{ not json");

    await fs.writeFile(file, JSON.stringify({ "a.example.com": SAVED }));
    expect(await storage.read("a.example.com")).toEqual(SAVED);
  });
});

describe("readDocumentAgentSettingsForTask", () => {
  it("is the portal's saved settings", async () => {
    headers = { "x-forwarded-host": "a.example.com" };
    const storage = new DocumentAgentSettingsStorage(path.join(dir, "s.json"));
    await storage.write("a.example.com", SAVED);

    expect(await readDocumentAgentSettingsForTask(storage)).toEqual(SAVED);
    expect(warn).not.toHaveBeenCalled();
  });

  it("turns the agent off while the settings cannot be read", async () => {
    headers = { "x-forwarded-host": "a.example.com" };
    const file = path.join(dir, "s.json");
    await fs.writeFile(file, "{ not json");

    const settings = await readDocumentAgentSettingsForTask(new DocumentAgentSettingsStorage(file));

    expect(settings).toEqual(DOCUMENT_AGENT_UNREADABLE);
    expect(settings.enabled).toBe(false);
    expect(warn).toHaveBeenCalledTimes(1);
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
