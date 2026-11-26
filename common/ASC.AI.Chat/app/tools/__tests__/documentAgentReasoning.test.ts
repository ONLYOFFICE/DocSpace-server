// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
