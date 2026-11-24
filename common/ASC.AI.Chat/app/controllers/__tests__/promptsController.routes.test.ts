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
import logger from "../../log.js";

const engine = engineMock<PromptsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  PromptsEngine: engineClass(engine),
}));
const readFolderById = vi.fn<(id: string) => Promise<unknown>>();
// The access gate (aiAccess.ts) reads the tool prefs; a stand-in that answers lets the route go on.
const toolPrefs = vi.hoisted(() => ({ readDisabled: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: { promptFolders: { readById: readFolderById }, toolPrefs },
}));

const { promptsController } = await import("../promptsController.js");
// After the mocks: these modules load the library, whose engine class is replaced above.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

const prompt = {
  id: "p1",
  name: "Summary",
  text: "Summarize the file",
  folderId: "f1",
  createdAt: 1767225600000,
  updatedAt: 1767225600000,
};
const folder = { id: "f1", name: "Contracts", createdAt: 1767225600000, updatedAt: 1767225600000 };

beforeEach(() => {
  mockReset(engine);
  readFolderById.mockReset();
});

describe("promptsController.create", () => {
  const body = { name: "Summary", text: "Summarize", folderId: "f1" };

  it("passes only name, text and folderId to the engine", async () => {
    engine.create.mockResolvedValue({ success: true, prompt });

    const reply = await invoke(promptsController.create, { body: { ...body, extra: "x" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true, prompt });
    expect(engine.create).toHaveBeenCalledWith(body);
    expect(published("aiPromptsCreate", reply)).toEqual([]);
  });

  it("passes an absent folderId as undefined", async () => {
    engine.create.mockResolvedValue({ success: true, prompt });

    await invoke(promptsController.create, { body: { name: "n", text: "t" } });

    expect(engine.create).toHaveBeenCalledWith({ name: "n", text: "t", folderId: undefined });
  });

  it.each([
    ["null", null, "name", "the request body must be an object"],
    ["a string", "Summary", "name", "the request body must be an object"],
    ["an array", [body], "name", "the request body must be an object"],
    ["{}", {}, "name", "name is required and must be a string"],
    ["name null", { ...body, name: null }, "name", "name is required and must be a string"],
    ["name a number", { ...body, name: 5 }, "name", "name is required and must be a string"],
    ["text missing", { name: "n" }, "text", "text is required and must be a string"],
    ["text null", { ...body, text: null }, "text", "text is required and must be a string"],
    ["text an object", { ...body, text: {} }, "text", "text is required and must be a string"],
    ["folderId a number", { ...body, folderId: 7 }, "name", "folderId must be a string or null"],
    ["folderId an array", { ...body, folderId: [] }, "name", "folderId must be a string or null"],
  ])("answers 400 naming the field for a body with %s", async (_what, sent, field, message) => {
    const reply = await invoke(promptsController.create, { body: sent });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ success: false, error: { field, message, code: "invalid" } });
    expect(engine.create).not.toHaveBeenCalled();
    expect(published("aiPromptsCreate", reply)).toEqual([]);
  });

  // An empty name and an over-long one are the engine's to refuse: the controller passes them on.
  it.each([
    ["an empty name", { ...body, name: "" }],
    ["an empty text", { ...body, text: "" }],
    ["a name of 10 000 characters", { ...body, name: "n".repeat(10_000) }],
  ])("leaves %s to the engine", async (_what, sent) => {
    engine.create.mockResolvedValue({
      success: false,
      error: { field: "name", message: "Name is required", code: "required" },
    });

    const reply = await invoke(promptsController.create, { body: sent });

    expect(engine.create).toHaveBeenCalledTimes(1);
    expect(reply.status).toBe(400);
    expect(published("aiPromptsCreate", reply)).toEqual([]);
  });

  // The status comes from `error.code` alone: a refusal without one is a 400 whatever its
  // message says (ts-ai-failure-paths.md §1).
  it.each([
    ["Folder not found: f9", 400],
    ["A prompt NOT FOUND", 400],
    ["Prompt already exists", 400],
    ["Name is too long", 400],
  ])("maps a refusal without a code and the message %j to %i", async (message, status) => {
    const refusal = { success: false as const, error: { field: "name" as const, message } };
    engine.create.mockResolvedValue(refusal);

    const reply = await invoke(promptsController.create, { body });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(refusal);
    expect(published("aiPromptsCreate", reply)).toEqual([]);
  });

  it("answers a refusal with the code required as 400", async () => {
    const refusal = {
      success: false as const,
      error: { field: "text" as const, message: "Text is required", code: "required" as const },
    };
    engine.create.mockResolvedValue(refusal);

    const reply = await invoke(promptsController.create, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual(refusal);
  });

  it("answers 500 without details when the engine throws, and logs the failure", async () => {
    engine.create.mockRejectedValue(new Error("socket hang up at http://ai.internal:5157"));

    const reply = await invoke(promptsController.create, { body, url: "/prompts/create" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(JSON.stringify(reply.body)).not.toContain("ai.internal");
    expect(logger.error).toHaveBeenCalledTimes(1);
    expect(published("aiPromptsCreate", reply)).toEqual([]);
  });

  it("relays the status and reason phrase of a failed AI service call, not its body", async () => {
    engine.create.mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", "secret body", "http://ai.internal/prompts"),
    );

    const reply = await invoke(promptsController.create, { body });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(logger.error).not.toHaveBeenCalled();
    expect(published("aiPromptsCreate", reply)).toEqual([]);
  });

  it("answers 'Upstream error' for a failed AI service call without a reason phrase", async () => {
    engine.create.mockRejectedValue(new AiServiceHttpError(502, "", "", "http://ai.internal"));

    const reply = await invoke(promptsController.create, { body });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Upstream error" });
  });
});

describe("promptsController.update", () => {
  const updated = { success: true as const, prompt };

  it.each([
    ["a named body", { id: "p1", updates: { name: "N", text: "T", folderId: null } }],
    ["a positional body", ["p1", { name: "N", text: "T", folderId: null }]],
  ])("updates the prompt from %s", async (_what, body) => {
    engine.update.mockResolvedValue(updated);

    const reply = await invoke(promptsController.update, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(updated);
    expect(engine.update).toHaveBeenCalledWith("p1", { name: "N", text: "T", folderId: null });
    expect(published("aiPromptsUpdate", reply)).toEqual([]);
  });

  it("sends only the fields the updates carry, and none for absent updates", async () => {
    engine.update.mockResolvedValue(updated);

    await invoke(promptsController.update, { body: { id: "p1", updates: { text: "T", x: 1 } } });
    await invoke(promptsController.update, { body: { id: "p1" } });
    await invoke(promptsController.update, { body: { id: "p1", updates: null } });

    expect(engine.update.mock.calls).toEqual([
      ["p1", { text: "T" }],
      ["p1", {}],
      ["p1", {}],
    ]);
  });

  it.each([
    ["null", null, "name", "id is required and must be a string"],
    ["a string", "p1", "name", "id is required and must be a string"],
    ["{}", {}, "name", "id is required and must be a string"],
    ["an empty id", { id: "", updates: {} }, "name", "id is required and must be a string"],
    ["id a number", { id: 1, updates: {} }, "name", "id is required and must be a string"],
    ["id null", { id: null, updates: {} }, "name", "id is required and must be a string"],
    ["updates a string", { id: "p1", updates: "x" }, "name", "updates must be an object"],
    ["updates an array", { id: "p1", updates: [] }, "name", "updates must be an object"],
    ["name a number", { id: "p1", updates: { name: 1 } }, "name", "name must be a string"],
    ["name null", { id: "p1", updates: { name: null } }, "name", "name must be a string"],
    ["text a boolean", { id: "p1", updates: { text: true } }, "text", "text must be a string"],
    ["text null", { id: "p1", updates: { text: null } }, "text", "text must be a string"],
    [
      "folderId a number",
      { id: "p1", updates: { folderId: 3 } },
      "name",
      "folderId must be a string or null",
    ],
  ])("answers 400 for a body with %s", async (_what, body, field, message) => {
    const reply = await invoke(promptsController.update, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ success: false, error: { field, message, code: "invalid" } });
    expect(engine.update).not.toHaveBeenCalled();
    expect(published("aiPromptsUpdate", reply)).toEqual([]);
  });

  it.each([
    ["not_found", 404],
    ["conflict", 409],
    ["invalid", 400],
  ])("answers a refusal with the code %s as %i", async (code, status) => {
    const refusal = {
      success: false as const,
      error: { field: "name" as const, message: "refused", code: code as "invalid" },
    };
    engine.update.mockResolvedValue(refusal);

    const reply = await invoke(promptsController.update, { body: { id: "p1", updates: {} } });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(refusal);
    expect(published("aiPromptsUpdate", reply)).toEqual([]);
  });
});

describe("promptsController.move", () => {
  it.each([
    ["a named body", { id: "p1", folderId: "f1" }, "f1"],
    ["a positional body", ["p1", "f1"], "f1"],
    ["a null folderId", { id: "p1", folderId: null }, null],
    ["no folderId", { id: "p1" }, null],
    ["a positional body without a folder", ["p1"], null],
  ])("moves the prompt for %s", async (_what, body, folderId) => {
    engine.move.mockResolvedValue({ success: true, prompt });

    const reply = await invoke(promptsController.move, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true, prompt });
    expect(engine.move).toHaveBeenCalledWith("p1", folderId);
    expect(published("aiPromptsMove", reply)).toEqual([]);
  });

  it.each([
    ["null", null, "id is required and must be a string"],
    ["a string", "p1", "id is required and must be a string"],
    ["{}", {}, "id is required and must be a string"],
    ["an empty id", { id: "" }, "id is required and must be a string"],
    ["id a number", { id: 5 }, "id is required and must be a string"],
    ["folderId a number", { id: "p1", folderId: 5 }, "folderId must be a string or null"],
    ["folderId an object", { id: "p1", folderId: {} }, "folderId must be a string or null"],
  ])("answers 400 for a body with %s", async (_what, body, message) => {
    const reply = await invoke(promptsController.move, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      error: { field: "name", message, code: "invalid" },
    });
    expect(engine.move).not.toHaveBeenCalled();
    expect(published("aiPromptsMove", reply)).toEqual([]);
  });

  it.each([
    ["not_found", 404],
    ["conflict", 409],
  ])("answers a refusal with the code %s as %i", async (code, status) => {
    const refusal = {
      success: false as const,
      error: { field: "name" as const, message: "refused", code: code as "conflict" },
    };
    engine.move.mockResolvedValue(refusal);

    const reply = await invoke(promptsController.move, { body: { id: "p1", folderId: "f2" } });

    expect(reply.status).toBe(status);
    expect(published("aiPromptsMove", reply)).toEqual([]);
  });
});

describe("promptsController.delete", () => {
  it.each([
    ["the bare id as the body", { body: "p1" }],
    ["a named body", { body: { id: "p1" } }],
    ["the query", { body: undefined, query: { id: "p1" } }],
    ["the query when the body id is not a string", { body: { id: 5 }, query: { id: "p1" } }],
  ])("deletes the prompt named by %s", async (_what, init) => {
    engine.delete.mockResolvedValue(undefined);

    const reply = await invoke(promptsController.delete, { method: "DELETE", ...init });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.delete).toHaveBeenCalledWith("p1");
    expect(published("aiPromptsDelete", reply)).toEqual([]);
  });

  it.each([
    ["no body and no query", {}],
    ["an empty id", { body: "" }],
    ["null", { body: null }],
    ["an id that is a number", { body: { id: 1 } }],
    ["a query id given twice", { query: { id: ["a", "b"] } }],
  ])("answers 400 for %s", async (_what, init) => {
    const reply = await invoke(promptsController.delete, init);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "id required" });
    expect(engine.delete).not.toHaveBeenCalled();
    expect(published("aiPromptsDelete", reply)).toEqual([]);
  });
});

describe("promptsController.list", () => {
  it.each([
    ["no folderId", {}, null],
    ["an empty folderId", { folderId: "" }, null],
    ["a folderId", { folderId: "f1" }, "f1"],
  ])("lists the prompts for %s", async (_what, query, folderId) => {
    engine.list.mockResolvedValue([prompt]);

    const reply = await invoke(promptsController.list, { method: "GET", query });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([prompt]);
    expect(engine.list).toHaveBeenCalledWith(folderId);
    expect(published("aiPromptsList", reply)).toEqual([]);
  });

  it("answers 400 when folderId is repeated in the query, instead of listing the root", async () => {
    const reply = await invoke(promptsController.list, {
      method: "GET",
      query: { folderId: ["f1", "f2"] },
    });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "folderId must be a single value" });
    expect(responseViolations("aiPromptsList", 400, reply.body)).toEqual([]);
    expect(engine.list).not.toHaveBeenCalled();
  });
});

describe("promptsController.createFolder", () => {
  it.each([
    ["the bare name", "Contracts"],
    ["a named body", { name: "Contracts" }],
  ])("creates the folder from %s", async (_what, body) => {
    engine.createFolder.mockResolvedValue({ success: true, folder });

    const reply = await invoke(promptsController.createFolder, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true, folder });
    expect(engine.createFolder).toHaveBeenCalledWith("Contracts");
    expect(published("aiPromptsCreateFolder", reply)).toEqual([]);
  });

  it.each([
    ["null", null],
    ["{}", {}],
    ["an array", ["Contracts"]],
    ["a name that is a number", { name: 5 }],
    ["a name that is null", { name: null }],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await invoke(promptsController.createFolder, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      error: { field: "name", message: "name is required and must be a string", code: "invalid" },
    });
    expect(engine.createFolder).not.toHaveBeenCalled();
    expect(published("aiPromptsCreateFolder", reply)).toEqual([]);
  });

  it("answers a clash with an existing folder as 409", async () => {
    const refusal = {
      success: false as const,
      error: { field: "name" as const, message: "exists", code: "conflict" as const },
    };
    engine.createFolder.mockResolvedValue(refusal);

    const reply = await invoke(promptsController.createFolder, { body: "Contracts" });

    expect(reply.status).toBe(409);
    expect(reply.body).toEqual(refusal);
    expect(published("aiPromptsCreateFolder", reply)).toEqual([]);
  });

  it("answers a not_found refusal as the published 404", async () => {
    engine.createFolder.mockResolvedValue({
      success: false,
      error: { field: "name", message: "gone", code: "not_found" },
    });

    const reply = await invoke(promptsController.createFolder, { body: "Contracts" });

    expect(reply.status).toBe(404);
    expect(published("aiPromptsCreateFolder", reply)).toEqual([]);
  });
});

describe("promptsController.renameFolder", () => {
  it.each([
    ["a named body", { id: "f1", name: "New" }],
    ["a positional body", ["f1", "New"]],
  ])("renames the folder from %s", async (_what, body) => {
    engine.renameFolder.mockResolvedValue({ success: true, folder });

    const reply = await invoke(promptsController.renameFolder, { method: "PUT", body });

    expect(reply.status).toBe(200);
    expect(engine.renameFolder).toHaveBeenCalledWith("f1", "New");
    expect(published("aiPromptsRenameFolder", reply)).toEqual([]);
  });

  it.each([
    ["null", null, "id is required and must be a string"],
    ["a string", "f1", "id is required and must be a string"],
    ["{}", {}, "id is required and must be a string"],
    ["an empty id", { id: "", name: "n" }, "id is required and must be a string"],
    ["id a number", { id: 1, name: "n" }, "id is required and must be a string"],
    ["no name", { id: "f1" }, "name is required and must be a string"],
    ["name null", ["f1", null], "name is required and must be a string"],
    ["name a number", { id: "f1", name: 2 }, "name is required and must be a string"],
  ])("answers 400 for a body with %s", async (_what, body, message) => {
    const reply = await invoke(promptsController.renameFolder, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      success: false,
      error: { field: "name", message, code: "invalid" },
    });
    expect(engine.renameFolder).not.toHaveBeenCalled();
    expect(published("aiPromptsRenameFolder", reply)).toEqual([]);
  });

  it.each([
    ["not_found", 404],
    ["conflict", 409],
    ["required", 400],
  ])("answers a refusal with the code %s as %i", async (code, status) => {
    engine.renameFolder.mockResolvedValue({
      success: false,
      error: { field: "name", message: "refused", code: code as "conflict" },
    });

    const reply = await invoke(promptsController.renameFolder, { body: ["f1", "New"] });

    expect(reply.status).toBe(status);
    expect(published("aiPromptsRenameFolder", reply)).toEqual([]);
  });
});

describe("promptsController.deleteFolder", () => {
  it.each([
    ["the bare id as the body", { body: "f1" }],
    ["a named body", { body: { id: "f1" } }],
    ["the query", { query: { id: "f1" } }],
  ])("deletes a folder the caller can see, named by %s", async (_what, init) => {
    readFolderById.mockResolvedValue(folder);
    engine.deleteFolder.mockResolvedValue(undefined);

    const reply = await invoke(promptsController.deleteFolder, { method: "DELETE", ...init });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(readFolderById).toHaveBeenCalledWith("f1");
    expect(engine.deleteFolder).toHaveBeenCalledWith("f1");
    expect(published("aiPromptsDeleteFolder", reply)).toEqual([]);
  });

  it.each([
    ["no id", {}],
    ["an empty id", { body: "" }],
    ["an id that is a number", { body: { id: 3 } }],
  ])("answers 400 for %s and reads nothing", async (_what, init) => {
    const reply = await invoke(promptsController.deleteFolder, init);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "id required" });
    expect(readFolderById).not.toHaveBeenCalled();
    expect(published("aiPromptsDeleteFolder", reply)).toEqual([]);
  });

  it("answers 404 for a folder the store does not show the caller (Bug 83138)", async () => {
    readFolderById.mockResolvedValue(null);

    const reply = await invoke(promptsController.deleteFolder, { body: "f9" });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Folder not found: f9" });
    expect(engine.deleteFolder).not.toHaveBeenCalled();
    expect(published("aiPromptsDeleteFolder", reply)).toEqual([]);
  });

  it("deletes a folder the store answers undefined for, as only null means absent", async () => {
    readFolderById.mockResolvedValue(undefined);

    const reply = await invoke(promptsController.deleteFolder, { body: "f1" });

    expect(reply.status).toBe(200);
    expect(engine.deleteFolder).toHaveBeenCalledWith("f1");
  });

  it.each([
    ["the AI service answering 500", new AiServiceHttpError(500, "Internal Server Error", "", "u")],
    ["a timeout", new Error("upstream timeout after 300000ms")],
  ])("answers 5xx, not 404, when the folder read fails with %s", async (_what, error) => {
    readFolderById.mockRejectedValue(error);

    const reply = await invoke(promptsController.deleteFolder, { body: "f1" });

    expect(reply.status).toBe(500);
    expect(engine.deleteFolder).not.toHaveBeenCalled();
    expect(published("aiPromptsDeleteFolder", reply)).toEqual([]);
  });
});

describe("promptsController.listFolders and export", () => {
  it("answers the folders as the engine lists them", async () => {
    engine.listFolders.mockResolvedValue([folder]);

    const reply = await invoke(promptsController.listFolders, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([folder]);
    expect(published("aiPromptsListFolders", reply)).toEqual([]);
  });

  it("answers the bundle as the engine exports it", async () => {
    const bundle = { version: 1 as const, folders: [folder], prompts: [prompt] };
    engine.export.mockResolvedValue(bundle);

    const reply = await invoke(promptsController.export, { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(bundle);
    expect(published("aiPromptsExport", reply)).toEqual([]);
  });

  it("answers 500 when the export fails", async () => {
    engine.export.mockRejectedValue(new Error("boom"));

    const reply = await invoke(promptsController.export, { method: "GET" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(published("aiPromptsExport", reply)).toEqual([]);
  });
});

describe("promptsController.getById and getFolderById", () => {
  it("answers the prompt of the id in the query", async () => {
    engine.getById.mockResolvedValue(prompt);

    const reply = await invoke(promptsController.getById, { method: "GET", query: { id: "p1" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(prompt);
    expect(engine.getById).toHaveBeenCalledWith("p1");
    expect(published("aiPromptsGetById", reply)).toEqual([]);
  });

  // CONTRACT data-3: an unknown prompt id answers 200 with a null body. Pinned by openspec/specs/ai-agents/spec.md ("`aiPromptsGetById` answers 200 with an empty body for an unknown id") and ASC.AI.Tests (PromptLifecycleTests).
  it("answers 200 with null for an unknown prompt", async () => {
    engine.getById.mockResolvedValue(null);

    const reply = await invoke(promptsController.getById, { method: "GET", query: { id: "p9" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(published("aiPromptsGetById", reply)).toEqual([]);
  });

  it("answers the folder of the id in the query", async () => {
    engine.getFolderById.mockResolvedValue(folder);

    const reply = await invoke(promptsController.getFolderById, {
      method: "GET",
      query: { id: "f1" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(folder);
    expect(engine.getFolderById).toHaveBeenCalledWith("f1");
    expect(published("aiPromptsGetFolderById", reply)).toEqual([]);
  });

  // CONTRACT data-4: an unknown folder id answers 200 with a null body, while delete-folder answers 404 for the same id. Pinned by openspec/specs/ai-agents/spec.md and ASC.AI.Tests (PromptFolderLifecycleTests: "a well-formed unknown id is 200 null").
  it("answers 200 with null for an unknown folder", async () => {
    engine.getFolderById.mockResolvedValue(null);

    const reply = await invoke(promptsController.getFolderById, {
      method: "GET",
      query: { id: "f9" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(published("aiPromptsGetFolderById", reply)).toEqual([]);
  });

  it.each([
    ["getById", "aiPromptsGetById"],
    ["getFolderById", "aiPromptsGetFolderById"],
  ] as const)("%s answers 400 without an id or with a repeated one", async (method, op) => {
    for (const query of [{}, { id: "" }, { id: ["a", "b"] }]) {
      const reply = await invoke(promptsController[method], { method: "GET", query });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "id required" });
      expect(published(op, reply)).toEqual([]);
    }
    expect(engine.getById).not.toHaveBeenCalled();
    expect(engine.getFolderById).not.toHaveBeenCalled();
  });
});
