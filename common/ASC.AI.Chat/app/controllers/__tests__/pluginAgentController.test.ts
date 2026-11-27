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

// GET plugin-agent: the answer is the offer of the plugin tools, behind the AI access gate.
// The offer itself (configuration, skill, role) is tested with the adapter
// (pluginAgent/__tests__/pluginAgentTools.test.ts); here it is the boundary.

import { beforeEach, describe, expect, it, vi } from "vitest";
import { invoke } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

const mocks = vi.hoisted(() => ({ readDisabled: vi.fn(), available: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: { toolPrefs: { readDisabled: mocks.readDisabled } },
}));
vi.mock("../../pluginAgent/pluginAgentTools.js", () => ({
  pluginAgentAvailable: mocks.available,
}));

const { AiServiceHttpError } = await import("../../storage/httpClient.js");
const { pluginAgentController } = await import("../pluginAgentController.js");

beforeEach(() => {
  mocks.readDisabled.mockReset().mockResolvedValue({});
  mocks.available.mockReset();
});

describe("pluginAgentController.get", () => {
  it.each([true, false])("answers whether the tools are offered: %s", async (available) => {
    mocks.available.mockResolvedValue(available);

    const reply = await invoke(pluginAgentController.get, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ available });
    expect(responseViolations("aiSettingsGetPluginAgent", 200, reply.body)).toEqual([]);
  });

  it("answers 403 from the AI access gate, without asking for the offer", async () => {
    mocks.readDisabled.mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", "AI is off", "http://ai/x"),
    );

    const reply = await invoke(pluginAgentController.get, { method: "GET" });

    expect(reply.status).toBe(403);
    expect(responseViolations("aiSettingsGetPluginAgent", 403, reply.body)).toEqual([]);
    expect(mocks.available).not.toHaveBeenCalled();
  });
});
