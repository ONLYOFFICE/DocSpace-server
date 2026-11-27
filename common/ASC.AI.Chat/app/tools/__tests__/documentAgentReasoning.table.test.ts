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

// reasoningFor when the provider's table cannot help: the library's lookup throws, or says the
// model does not think, and when a model names depths the agent does not know. The table is
// the library's (`@onlyoffice/ai-chat/providers`), stood in for here.

const getReasoningSupport = vi.hoisted(() => vi.fn());
vi.mock("@onlyoffice/ai-chat/providers", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@onlyoffice/ai-chat/providers")>()),
  getReasoningSupport,
}));

const { reasoningFor } = await import("../documentAgentReasoning.js");

const base = { providerType: "openai", modelId: "x" } as const;

describe("reasoningFor without the provider's table", () => {
  it.each([
    [
      "throws",
      () =>
        getReasoningSupport.mockImplementation(() => {
          throw new Error("unknown provider");
        }),
    ],
    [
      "says the model does not think",
      () => getReasoningSupport.mockReturnValue({ thinks: false, canDisable: true }),
    ],
  ])("falls back to the legacy boolean when the table %s", (_what, arrange) => {
    arrange();

    expect(reasoningFor({ ...base, reasoning: true }, "high")).toBe("high");
    expect(reasoningFor({ ...base, reasoning: true }, "default")).toBe("medium");
    expect(reasoningFor({ ...base, reasoning: undefined }, "high")).toBe("off");
  });

  it("looks an external profile up by the provider it is based on", () => {
    getReasoningSupport.mockReturnValue({ thinks: true, canDisable: true, depths: ["high"] });

    expect(
      reasoningFor({ providerType: "external", basedOn: "anthropic", modelId: "m" }, "low"),
    ).toBe("high");
    expect(getReasoningSupport).toHaveBeenCalledWith("anthropic", "m");
  });

  it("runs the level as asked when the model's depths are none the agent knows", () => {
    const profile = {
      ...base,
      reasoningSupport: { thinks: true, canDisable: true, depths: ["minimal"] as never },
    };

    expect(reasoningFor(profile, "high")).toBe("high");
  });
});
