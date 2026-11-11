import { describe, expect, it, vi } from "vitest";

// The provider table: one model with three depths it cannot switch off, one
// with two, the rest unknown.
vi.mock("@onlyoffice/ai-chat/providers", () => ({
  getReasoningSupport: (_type: string, model: string) => {
    if (model === "gpt-oss") {
      return { thinks: true, canDisable: false, depths: ["low", "medium", "high"] };
    }
    if (model === "gemini-3-pro") {
      return { thinks: true, canDisable: false, depths: ["low", "high"] };
    }
    return { thinks: false, canDisable: true, depths: [] };
  },
}));

const { reasoningFor } = await import("../documentAgentReasoning.js");

const base = { providerType: "openai", modelId: "x", reasoning: true };

describe("reasoningFor", () => {
  it("runs a model that cannot think with reasoning off, whatever was picked", () => {
    expect(reasoningFor({ ...base, reasoning: false }, "high")).toBe("off");
    expect(reasoningFor({ ...base, reasoning: undefined }, "default")).toBe("off");
  });

  it("takes the catalogue's record on the profile first", () => {
    const profile = {
      ...base,
      reasoningSupport: {
        thinks: true,
        canDisable: true,
        depths: ["low", "medium"] as const,
        defaultDepth: "low" as const,
      },
    };
    expect(reasoningFor(profile, "default")).toBe("low");
    expect(reasoningFor(profile, "off")).toBe("off");
    expect(reasoningFor(profile, "max")).toBe("medium");
  });

  it("snaps the pick to the provider table's depths, upwards on a tie", () => {
    const oss = { ...base, modelId: "gpt-oss" };
    expect(reasoningFor(oss, "max")).toBe("high");
    expect(reasoningFor(oss, "default")).toBe("medium");
    // Off is not on offer: the model runs at its default instead.
    expect(reasoningFor(oss, "off")).toBe("medium");
    const gemini = { ...base, modelId: "gemini-3-pro" };
    expect(reasoningFor(gemini, "medium")).toBe("high");
    expect(reasoningFor(gemini, "default")).toBe("high");
  });

  it("falls back to the legacy boolean: every depth, off allowed", () => {
    expect(reasoningFor(base, "default")).toBe("medium");
    expect(reasoningFor(base, "max")).toBe("max");
    expect(reasoningFor(base, "off")).toBe("off");
  });

  it("keeps a profile's thinking off when the user switched it off on a model that may", () => {
    const profile = {
      ...base,
      reasoning: false,
      reasoningSupport: { thinks: true, canDisable: true, depths: ["low", "high"] as const },
    };
    expect(reasoningFor(profile, "high")).toBe("off");
  });
});
