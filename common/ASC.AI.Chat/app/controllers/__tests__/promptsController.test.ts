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

import type { PromptsEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { invoke } from "../../__tests__/helpers/http.js";

const engine = engineMock<PromptsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  PromptsEngine: engineClass(engine),
}));
vi.mock("../../storage/index.js", () => ({ storage: {} }));

const { promptsController } = await import("../promptsController.js");
// After the mocks: the schema helper loads the library, whose engine class is replaced above.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");

const prompt = { name: "Summary", text: "Summarize the file", folderId: null };

describe("promptsController.create", () => {
  beforeEach(() => {
    mockReset(engine);
  });

  it("answers the created prompt with 200", async () => {
    // Prompt (@onlyoffice/ai-chat/core types.d.ts): a prompt at the root has no folderId.
    const stored = { id: "p1", name: prompt.name, text: prompt.text, createdAt: 1767225600000 };
    const created = { success: true as const, prompt: { ...stored, updatedAt: 1767225600000 } };
    engine.create.mockResolvedValue(created as never);

    const reply = await invoke(promptsController.create, { body: prompt });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(created);
    expect(responseViolations("aiPromptsCreate", 200, reply.body)).toEqual([]);
    expect(engine.create).toHaveBeenCalledWith(prompt);
  });

  // The engine reports a refusal as a value; res.json alone would send it with 200.
  it.each([
    ["conflict", 409],
    ["not_found", 404],
    ["invalid", 400],
  ])("answers a refusal of the engine with code %s as %i", async (code, status) => {
    const refusal = { success: false as const, error: { field: "name", message: "refused", code } };
    engine.create.mockResolvedValue(refusal as never);

    const reply = await invoke(promptsController.create, { body: prompt });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(refusal);
    expect(responseViolations("aiPromptsCreate", status, reply.body)).toEqual([]);
  });

  it.each([
    ["null", null],
    ["a string", "Summary"],
    ["an array", [prompt]],
    ["no name", { text: "t", folderId: null }],
    ["a name that is a number", { ...prompt, name: 5 }],
    ["no text", { name: "n", folderId: null }],
    ["a folderId that is a number", { ...prompt, folderId: 7 }],
  ])("answers 400 for a body that is %s and does not reach the engine", async (_what, body) => {
    const reply = await invoke(promptsController.create, { body });

    expect(reply.status).toBe(400);
    expect(responseViolations("aiPromptsCreate", 400, reply.body)).toEqual([]);
    expect(engine.create).not.toHaveBeenCalled();
  });
});
