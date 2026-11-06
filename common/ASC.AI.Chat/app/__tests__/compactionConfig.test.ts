import { afterEach, describe, expect, it, vi } from "vitest";

const store = vi.hoisted(() => ({
  values: {
    app: { appsettings: "/nowhere", environment: "test" },
  } as Record<string, unknown>,
}));

vi.mock("nconf", () => {
  const nconf = {
    argv: () => nconf,
    env: () => nconf,
    file: () => nconf,
    get: (key: string) => store.values[key],
  };
  return { default: nconf };
});

const { getCompaction } = await import("../../config/index.js");

const ENV = [
  "AI__COMPACTION__ENABLED",
  "AI__COMPACTION__THRESHOLD",
  "AI__COMPACTION__KEEP_RECENT",
  "AI__COMPACTION__DEFAULT_CONTEXT_WINDOW",
];

const withSettings = (compaction: Record<string, unknown> | undefined) => {
  store.values = {
    app: { appsettings: "/nowhere", environment: "test" },
    ai: compaction ? { compaction } : {},
  };
};

afterEach(() => {
  for (const name of ENV) {
    delete process.env[name];
  }
  withSettings(undefined);
});

describe("getCompaction", () => {
  it("is on with nothing configured, leaving every knob to the library", () => {
    expect(getCompaction()).toEqual({ enabled: true });
  });

  it("is switched off from the file or the environment", () => {
    withSettings({ enabled: false });
    expect(getCompaction()).toEqual({ enabled: false });
    withSettings(undefined);
    process.env["AI__COMPACTION__ENABLED"] = "FALSE";
    expect(getCompaction()).toEqual({ enabled: false });
    process.env["AI__COMPACTION__ENABLED"] = "0";
    expect(getCompaction()).toEqual({ enabled: false });
    // Not a flag: the file (here nothing, so the default) decides.
    process.env["AI__COMPACTION__ENABLED"] = "maybe";
    expect(getCompaction()).toEqual({ enabled: true });
  });

  it("reads the appsettings section", () => {
    withSettings({ enabled: true, threshold: 0.7, keepRecent: 6, defaultContextWindow: 32000 });
    expect(getCompaction()).toEqual({
      enabled: true,
      threshold: 0.7,
      keepRecent: 6,
      defaultContextWindow: 32000,
    });
  });

  it("lets the environment win and drops a malformed knob", () => {
    withSettings({ enabled: false, threshold: 0.7 });
    process.env["AI__COMPACTION__ENABLED"] = "1";
    process.env["AI__COMPACTION__THRESHOLD"] = "1.5";
    process.env["AI__COMPACTION__KEEP_RECENT"] = "0";
    // A malformed environment knob falls back to the file's value.
    expect(getCompaction()).toEqual({ enabled: true, threshold: 0.7, keepRecent: 0 });
  });
});
