import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// The config module reads appsettings through nconf at import time; a fake
// store keeps the test off the disk and lets each case name its settings.
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

const { getDocumentAgent } = await import("../../config/index.js");

const URL_ENV = "AI__DOCUMENT_AGENT__URL";
const TOKEN_ENV = "AI__DOCUMENT_AGENT__TOKEN";

const withSettings = (documentAgent: Record<string, unknown> | undefined) => {
  store.values = {
    app: { appsettings: "/nowhere", environment: "test" },
    ai: documentAgent ? { documentAgent } : {},
  };
};

describe("getDocumentAgent", () => {
  beforeEach(() => {
    delete process.env[URL_ENV];
    delete process.env[TOKEN_ENV];
  });

  afterEach(() => {
    delete process.env[URL_ENV];
    delete process.env[TOKEN_ENV];
  });

  it("is off without a URL and a token", () => {
    withSettings({ url: "", token: "" });
    expect(getDocumentAgent()).toBeUndefined();

    withSettings(undefined);
    expect(getDocumentAgent()).toBeUndefined();
  });

  it("takes the URL and the token from the appsettings", () => {
    withSettings({ url: "http://agent:9838/", token: "secret" });
    expect(getDocumentAgent()).toMatchObject({
      url: "http://agent:9838",
      token: "secret",
    });
  });

  it("is switched off by environment variables that are set but empty", () => {
    withSettings({ url: "http://agent:9838", token: "secret" });
    process.env[URL_ENV] = "";
    process.env[TOKEN_ENV] = "";
    expect(getDocumentAgent()).toBeUndefined();
  });

  it("is switched off by a blank environment URL alone", () => {
    withSettings({ url: "http://agent:9838", token: "secret" });
    process.env[URL_ENV] = "  ";
    expect(getDocumentAgent()).toBeUndefined();
  });

  it("prefers environment values over the appsettings", () => {
    withSettings({ url: "http://agent:9838", token: "secret" });
    process.env[URL_ENV] = "http://other:1";
    process.env[TOKEN_ENV] = "other";
    expect(getDocumentAgent()).toMatchObject({
      url: "http://other:1",
      token: "other",
    });
  });

  it("ignores a non-string setting", () => {
    withSettings({ url: 42, token: "secret" });
    expect(getDocumentAgent()).toBeUndefined();
  });

  it("ignores a non-string portal or files URL", () => {
    withSettings({
      url: "http://agent:9838",
      token: "secret",
      portalUrl: 42,
      filesUrl: { host: "x" },
    });
    expect(getDocumentAgent()).toMatchObject({
      portalUrl: undefined,
      filesUrl: expect.stringMatching(/^http:\/\/localhost:/),
    });
  });

  it("falls back to the appsettings for an empty portal or files URL in the environment", () => {
    withSettings({
      url: "http://agent:9838",
      token: "secret",
      portalUrl: "http://portal/",
      filesUrl: "http://files:5157/",
    });
    process.env["AI__DOCUMENT_AGENT__PORTAL_URL"] = "";
    process.env["AI__DOCUMENT_AGENT__FILES_URL"] = " ";
    try {
      expect(getDocumentAgent()).toMatchObject({
        portalUrl: "http://portal",
        filesUrl: "http://files:5157",
      });
    } finally {
      delete process.env["AI__DOCUMENT_AGENT__PORTAL_URL"];
      delete process.env["AI__DOCUMENT_AGENT__FILES_URL"];
    }
  });
});
