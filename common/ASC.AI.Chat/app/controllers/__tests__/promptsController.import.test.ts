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
import { invoke } from "../../__tests__/helpers/http.js";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import type { Reply } from "../../__tests__/helpers/http.js";

const engine = engineMock<PromptsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  PromptsEngine: engineClass(engine),
}));
// The access gate (aiAccess.ts) reads the tool prefs; a stand-in that answers lets the route go on.
const toolPrefs = vi.hoisted(() => ({ readDisabled: vi.fn() }));
vi.mock("../../storage/index.js", () => ({ storage: { toolPrefs } }));

const { promptsController } = await import("../promptsController.js");
// After the mocks: the schema helper loads the library through apiCatalog.ts.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");

const published = (reply: Reply): string[] =>
  responseViolations("aiPromptsImportBundle", reply.status, reply.body);

const folder = { id: "f1", name: "Contracts", createdAt: 1, updatedAt: 1 };
const prompt = {
  id: "p1",
  name: "Review",
  text: "Review it",
  folderId: "f1",
  createdAt: 1,
  updatedAt: 1,
};
const bundle = { version: 1, folders: [folder], prompts: [prompt] };
const imported = { success: true as const, imported: { folders: 1, prompts: 1 } };

const invalid = (kind: "folder" | "prompt", ref: string, message: string) => ({
  kind,
  ref,
  error: { field: "name", message, code: "invalid" },
});

beforeEach(() => {
  mockReset(engine);
  engine.importBundle.mockResolvedValue(imported);
});

describe("promptsController.importBundle with a well-formed bundle", () => {
  it.each([
    ["a named body without options", { bundle }, undefined],
    ["a positional body without options", [bundle], undefined],
    ["options null", { bundle, options: null }, undefined],
    ["options {}", { bundle, options: {} }, undefined],
    ["options with mode undefined", [bundle, { mode: undefined }], undefined],
    ["options { mode: merge }", { bundle, options: { mode: "merge" } }, { mode: "merge" }],
    ["options { mode: replace }", [bundle, { mode: "replace" }], { mode: "replace" }],
    ["the bare mode as options", [bundle, "replace"], { mode: "replace" }],
  ])("imports it for %s", async (_what, body, options) => {
    const reply = await invoke(promptsController.importBundle, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(imported);
    expect(engine.importBundle).toHaveBeenCalledWith(bundle, options);
    expect(published(reply)).toEqual([]);
  });

  it("accepts a prompt with a null or absent folderId", async () => {
    const loose = {
      version: 1,
      folders: [],
      prompts: [
        { id: "a", name: "A", text: "t", folderId: null },
        { id: "b", name: "B", text: "t" },
      ],
    };

    const reply = await invoke(promptsController.importBundle, { body: { bundle: loose } });

    expect(reply.status).toBe(200);
    expect(engine.importBundle).toHaveBeenCalledWith(loose, undefined);
  });

  it.each([
    ["merge", 409],
    [undefined, 409],
    ["replace", 400],
  ])("answers refusals that all clash with the library in mode %s as %i", async (mode, status) => {
    const result = {
      success: false as const,
      errors: [
        {
          kind: "prompt" as const,
          ref: "Review",
          error: { field: "name" as const, message: "x", code: "conflict" as const },
        },
        {
          kind: "folder" as const,
          ref: "Contracts",
          error: {
            field: "name" as const,
            message: "Folder name already exists",
            code: "conflict" as const,
          },
        },
      ],
    };
    engine.importBundle.mockResolvedValue(result);

    const reply = await invoke(promptsController.importBundle, {
      body: mode === undefined ? [bundle] : [bundle, { mode }],
    });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(result);
    expect(published(reply)).toEqual([]);
  });

  it("answers 400 when one refusal is not a clash", async () => {
    engine.importBundle.mockResolvedValue({
      success: false,
      errors: [
        { kind: "prompt", ref: "Review", error: { field: "name", message: "x", code: "conflict" } },
        { kind: "prompt", ref: "Other", error: { field: "text", message: "x", code: "required" } },
      ],
    });

    const reply = await invoke(promptsController.importBundle, { body: [bundle] });

    expect(reply.status).toBe(400);
    expect(published(reply)).toEqual([]);
  });

  it("answers 400 for a failure that lists no refusal", async () => {
    engine.importBundle.mockResolvedValue({ success: false, errors: [] });

    const reply = await invoke(promptsController.importBundle, { body: [bundle] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ success: false, errors: [] });
  });

  it("answers 500 when the engine throws", async () => {
    engine.importBundle.mockRejectedValue(new Error("boom"));

    const reply = await invoke(promptsController.importBundle, { body: [bundle] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(published(reply)).toEqual([]);
  });
});

describe("promptsController.importBundle refuses a malformed request before the engine", () => {
  const notObject = [invalid("folder", "<bundle>", "bundle is required and must be an object")];

  it.each([
    ["null", null, notObject],
    ["a string", "bundle", notObject],
    ["{}", {}, notObject],
    ["an empty array", [], notObject],
    ["a bundle that is an array", { bundle: [bundle] }, notObject],
    ["a bundle that is null", { bundle: null }, notObject],
    [
      "no version",
      { bundle: { folders: [], prompts: [] } },
      [invalid("folder", "<bundle>", "Unsupported bundle version: undefined")],
    ],
    [
      "version 2",
      { bundle: { ...bundle, version: 2 } },
      [invalid("folder", "<bundle>", "Unsupported bundle version: 2")],
    ],
    [
      "version as a string",
      { bundle: { ...bundle, version: "1" } },
      [invalid("folder", "<bundle>", "Unsupported bundle version: 1")],
    ],
    [
      "no folders and no prompts",
      { bundle: { version: 1 } },
      [
        invalid("folder", "<bundle>", "bundle.folders must be an array"),
        invalid("prompt", "<bundle>", "bundle.prompts must be an array"),
      ],
    ],
    [
      "folders and prompts that are objects",
      { bundle: { version: 1, folders: {}, prompts: null } },
      [
        invalid("folder", "<bundle>", "bundle.folders must be an array"),
        invalid("prompt", "<bundle>", "bundle.prompts must be an array"),
      ],
    ],
  ])("answers 400 for a body that is %s", async (_what, body, errors) => {
    const reply = await invoke(promptsController.importBundle, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ success: false, errors });
    expect(engine.importBundle).not.toHaveBeenCalled();
    expect(published(reply)).toEqual([]);
  });

  const badFolder = "a folder needs string id and name";
  it.each([
    ["null", null],
    ["a string", "Contracts"],
    ["no id", { name: "A" }],
    ["an id that is a number", { id: 1, name: "A" }],
    ["no name", { id: "f1" }],
    ["a name that is null", { id: "f1", name: null }],
  ])("answers 400 naming the index of a folder that is %s", async (_what, entry) => {
    const reply = await invoke(promptsController.importBundle, {
      body: { bundle: { version: 1, folders: [folder, entry], prompts: [] } },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      errors: [invalid("folder", "folders[1]", badFolder)],
    });
    expect(published(reply)).toEqual([]);
  });

  const badPrompt = "a prompt needs string id, name and text, and a string or null folderId";
  it.each([
    ["null", null],
    ["an array", [prompt]],
    ["no id", { name: "A", text: "t" }],
    ["no name", { id: "a", text: "t" }],
    ["a name that is a number", { id: "a", name: 1, text: "t" }],
    ["no text", { id: "a", name: "A" }],
    ["a text that is null", { id: "a", name: "A", text: null }],
    ["a folderId that is a number", { id: "a", name: "A", text: "t", folderId: 1 }],
  ])("answers 400 naming the index of a prompt that is %s", async (_what, entry) => {
    const reply = await invoke(promptsController.importBundle, {
      body: { bundle: { version: 1, folders: [], prompts: [entry] } },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      errors: [invalid("prompt", "prompts[0]", badPrompt)],
    });
    expect(published(reply)).toEqual([]);
  });

  it("lists every malformed entry of folders and prompts in one answer", async () => {
    const reply = await invoke(promptsController.importBundle, {
      body: { bundle: { version: 1, folders: [1, 2], prompts: [null] } },
    });

    expect(reply.body).toEqual({
      success: false,
      errors: [
        invalid("folder", "folders[0]", badFolder),
        invalid("folder", "folders[1]", badFolder),
        invalid("prompt", "prompts[0]", badPrompt),
      ],
    });
  });

  const badMode = [invalid("folder", "<options>", "options.mode must be merge or replace")];
  it.each([
    ["an unknown mode", { mode: "overwrite" }],
    ["a mode that is a number", { mode: 1 }],
    ["a mode that is null", { mode: null }],
    ["a bare unknown mode", "append"],
    ["a number", 5],
    ["an array", ["merge"]],
    ["false", false],
  ])("answers 400 for options with %s", async (_what, options) => {
    const reply = await invoke(promptsController.importBundle, { body: { bundle, options } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ success: false, errors: badMode });
    expect(engine.importBundle).not.toHaveBeenCalled();
    expect(published(reply)).toEqual([]);
  });

  it("checks the options before the twins of the bundle", async () => {
    const twins = { version: 1, folders: [folder, { ...folder, id: "f2" }], prompts: [] };

    const reply = await invoke(promptsController.importBundle, { body: [twins, "x"] });

    expect(reply.body).toEqual({ success: false, errors: badMode });
  });
});

describe("promptsController.importBundle refuses twins inside the bundle", () => {
  it("reports the second of two folders whose names differ only in case", async () => {
    const twins = {
      version: 1,
      folders: [
        folder,
        { ...folder, id: "f2", name: " contracts " },
        { ...folder, id: "f3", name: "CONTRACTS" },
      ],
      prompts: [],
    };

    const reply = await invoke(promptsController.importBundle, { body: [twins] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      errors: [invalid("folder", "CONTRACTS", "The bundle holds two folders of this name")],
    });
    expect(engine.importBundle).not.toHaveBeenCalled();
    expect(published(reply)).toEqual([]);
  });

  it("reports the second of two prompts of one name in one folder, trimmed", async () => {
    const twins = {
      version: 1,
      folders: [folder],
      prompts: [
        prompt,
        { ...prompt, id: "p2", name: " REVIEW " },
        { ...prompt, id: "p3", name: "review" },
        { ...prompt, id: "p4", folderId: "f2" },
        { id: "p5", name: "Root", text: "t", folderId: null },
        { id: "p6", name: "root", text: "t" },
      ],
    };

    const reply = await invoke(promptsController.importBundle, { body: [twins] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      errors: [
        invalid("prompt", "review", "The bundle holds two prompts of this name in one folder"),
        invalid("prompt", "root", "The bundle holds two prompts of this name in one folder"),
      ],
    });
  });

  it("leaves blank names to the engine instead of counting them as twins", async () => {
    const blanks = {
      version: 1,
      folders: [
        { ...folder, name: " " },
        { ...folder, id: "f2", name: "" },
      ],
      prompts: [
        { ...prompt, name: "" },
        { ...prompt, id: "p2", name: "  " },
      ],
    };

    const reply = await invoke(promptsController.importBundle, { body: [blanks] });

    expect(reply.status).toBe(200);
    expect(engine.importBundle).toHaveBeenCalledWith(blanks, undefined);
  });
});
