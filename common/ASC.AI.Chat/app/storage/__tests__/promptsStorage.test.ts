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

import { describe, expect, it } from "vitest";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { HttpPromptsStorage } from "../promptsStorage.js";

// products/ASC.AI/Server/Api/PromptStorageController.cs: POST prompts, GET prompts/{id},
// GET prompts, GET prompt-folders/{id}/prompts, PUT prompts/{id}, DELETE prompts/{id}.
const upstream = useUpstream();
const storage = new HttpPromptsStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

/** `PromptDto` (products/ASC.AI/Server/Models/ResponseDto/PromptDto.cs). */
const promptDto = (overrides: Record<string, unknown> = {}) => ({
  id: "a1b2c3d4-0000-4000-8000-000000000010",
  name: "Summary",
  text: "Summarize the file",
  folderId: "a1b2c3d4-0000-4000-8000-000000000020",
  createdAt: 1_700_000_000_000,
  updatedAt: 1_700_000_005_000,
  ...overrides,
});

const prompt = {
  id: "a1b2c3d4-0000-4000-8000-000000000010",
  name: "Summary",
  text: "Summarize the file",
  folderId: "a1b2c3d4-0000-4000-8000-000000000020",
  createdAt: 1_700_000_000_000,
  updatedAt: 1_700_000_005_000,
};

const MISSING = Symbol("missing");
function dtoWith(field: string, value: unknown): Record<string, unknown> {
  const dto: Record<string, unknown> = promptDto();
  if (value === MISSING) {
    delete dto[field];
  } else {
    dto[field] = value;
  }
  return dto;
}

function answerGet(body: unknown) {
  upstream.use(http.get("*/internal/ai/prompts/:id", () => docspace(body as never)));
}

describe("the PromptDto mapping (read by id)", () => {
  it("maps a full PromptDto", async () => {
    answerGet(promptDto());
    await expect(storage.readById("p/1")).resolves.toEqual(prompt);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/prompts/p%2F1");
  });

  // DEFECT stor2-5: a GET prompts/{id} answer of the wrong shape (a field missing, null or of another type) is read as "the prompt does not exist" — expected it to be rethrown as unknown, since the C# action answers 404 for an absent prompt (ts-ai-failure-paths.md §1)
  it.each([
    ["id", MISSING],
    ["id", null],
    ["id", ""],
    ["id", 1],
    ["name", MISSING],
    ["name", null],
    ["name", 1],
    ["text", MISSING],
    ["text", null],
    ["text", 1],
    ["createdAt", MISSING],
    ["createdAt", null],
    ["createdAt", "1"],
    ["updatedAt", MISSING],
    ["updatedAt", null],
    ["updatedAt", "1"],
  ])("reads a DTO whose %s is %s as absent", async (field, value) => {
    answerGet(dtoWith(field, value));
    await expect(storage.readById("p")).resolves.toBeNull();
  });

  it.each([null, "prompt", [promptDto()]])("reads an answer %j as absent", async (body) => {
    answerGet(body);
    await expect(storage.readById("p")).resolves.toBeNull();
  });

  it("takes an empty name and text", async () => {
    answerGet(promptDto({ name: "", text: "" }));
    await expect(storage.readById("p")).resolves.toMatchObject({ name: "", text: "" });
  });

  it.each([
    ["missing", MISSING],
    ["null", null],
    ["empty", ""],
    ["a number", 3],
  ])("leaves folderId out when it is %s", async (_name, value) => {
    answerGet(dtoWith("folderId", value));
    const { folderId: _f, ...rest } = prompt;
    await expect(storage.readById("p")).resolves.toEqual(rest);
  });

  it("reads a 404 as absent and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/prompts/:id", () => docspaceError(404)));
    await expect(storage.readById("p")).resolves.toBeNull();
    upstream.use(http.get("*/internal/ai/prompts/:id", () => docspaceError(500)));
    await expect(storage.readById("p")).rejects.toMatchObject({ status: 500 });
  });
});

describe("create", () => {
  it("posts name, text and folderId and answers the stored prompt", async () => {
    upstream.use(http.post("*/internal/ai/prompts", () => docspace(promptDto())));
    await expect(
      storage.create({ name: "Summary", text: "Summarize the file", folderId: "f" }),
    ).resolves.toEqual(prompt);
    expect(upstream.seen).toMatchObject([
      {
        method: "POST",
        path: "/internal/ai/prompts",
        body: { name: "Summary", text: "Summarize the file", folderId: "f" },
      },
    ]);
  });

  it.each([undefined, ""])("leaves a folderId %j out of the body", async (folderId) => {
    upstream.use(http.post("*/internal/ai/prompts", () => docspace(promptDto())));
    await storage.create({ name: "n", text: "t", folderId } as never);
    expect(upstream.seen[0]?.body).toEqual({ name: "n", text: "t" });
  });

  it.each([
    ["an empty answer", noContent()],
    ["a DTO without id", docspace(dtoWith("id", MISSING))],
  ])("throws on %s", async (_name, answer) => {
    upstream.use(http.post("*/internal/ai/prompts", () => answer));
    await expect(storage.create({ name: "n", text: "t" })).rejects.toThrow(
      "AI service returned an invalid prompt payload",
    );
  });

  it("rethrows a refusal", async () => {
    upstream.use(http.post("*/internal/ai/prompts", () => docspaceError(400)));
    await expect(storage.create({ name: "n", text: "t" })).rejects.toMatchObject({ status: 400 });
  });
});

describe("createMany", () => {
  it("asks nothing for no prompts", async () => {
    await expect(storage.createMany([])).resolves.toEqual([]);
    expect(upstream.seen).toEqual([]);
  });

  it("creates each prompt by itself and answers them in input order", async () => {
    upstream.use(
      http.post("*/internal/ai/prompts", async ({ request }) => {
        const body = (await request.json()) as { name: string };
        return docspace(promptDto({ id: `id-${body.name}`, name: body.name }));
      }),
    );

    const created = await storage.createMany([
      { name: "a", text: "t" },
      { name: "b", text: "t" },
    ]);

    expect(created.map((p) => p.id)).toEqual(["id-a", "id-b"]);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/prompts",
      "/internal/ai/prompts",
    ]);
  });
});

describe("readAll and readByFolderId", () => {
  const list = () => [
    promptDto({ id: "old", createdAt: 1 }),
    promptDto({ id: "new", createdAt: 3, folderId: null }),
    promptDto({ id: "mid", createdAt: 2 }),
  ];

  it("reads every prompt, newest first", async () => {
    upstream.use(http.get("*/internal/ai/prompts", () => docspace(list())));
    const all = await storage.readAll();
    expect(all.map((p) => p.id)).toEqual(["new", "mid", "old"]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/prompts");
  });

  it("reads the prompts of no folder from the whole list", async () => {
    upstream.use(http.get("*/internal/ai/prompts", () => docspace(list())));
    const loose = await storage.readByFolderId(null);
    expect(loose.map((p) => p.id)).toEqual(["new"]);
  });

  it("reads a folder's prompts from its own route, newest first", async () => {
    upstream.use(http.get("*/internal/ai/prompt-folders/:id/prompts", () => docspace(list())));
    const inFolder = await storage.readByFolderId("f/1");
    expect(inFolder.map((p) => p.id)).toEqual(["new", "mid", "old"]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/prompt-folders/f%2F1/prompts");
  });

  it.each([
    ["null", () => docspace(null)],
    ["an object", () => docspace({ items: [] })],
    ["an empty answer", noContent],
  ])("throws on a list that is %s", async (_name, answer) => {
    upstream.use(
      http.get("*/internal/ai/prompts", answer),
      http.get("*/internal/ai/prompt-folders/:id/prompts", answer),
    );
    await expect(storage.readAll()).rejects.toThrow("AI service returned a non-array prompt list");
    await expect(storage.readByFolderId("f")).rejects.toThrow(
      "AI service returned a non-array prompt list",
    );
  });

  it("throws naming the index of an invalid prompt", async () => {
    upstream.use(http.get("*/internal/ai/prompts", () => docspace([promptDto(), { id: "x" }])));
    await expect(storage.readAll()).rejects.toThrow(
      "AI service returned an invalid prompt at index 1",
    );
  });

  it("rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/prompts", () => docspaceError(500)));
    await expect(storage.readAll()).rejects.toMatchObject({ status: 500 });
  });
});

describe("update", () => {
  it.each([
    ["nothing", {}, {}],
    ["the name", { name: "n" }, { name: "n" }],
    ["the text", { text: "" }, { text: "" }],
    ["a folder", { folderId: "f" }, { changeFolder: true, folderId: "f" }],
    ["no folder", { folderId: null }, { changeFolder: true, folderId: null }],
    ["folderId undefined", { folderId: undefined }, { changeFolder: true, folderId: null }],
    [
      "everything",
      { name: "n", text: "t", folderId: "f" },
      { name: "n", text: "t", changeFolder: true, folderId: "f" },
    ],
  ])("puts %s as %j", async (_name, updates, body) => {
    upstream.use(http.put("*/internal/ai/prompts/:id", noContent));
    await storage.update("p/1", updates);
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/prompts/p%2F1", body },
    ]);
  });

  it("rethrows a refusal", async () => {
    upstream.use(http.put("*/internal/ai/prompts/:id", () => docspaceError(404)));
    await expect(storage.update("p", { name: "n" })).rejects.toMatchObject({ status: 404 });
  });
});

describe("delete and deleteByFolder", () => {
  it("deletes by the encoded id, treats a 404 as done and rethrows a 500", async () => {
    upstream.use(http.delete("*/internal/ai/prompts/:id", noContent));
    await storage.delete("p/1");
    expect(upstream.seen).toMatchObject([{ method: "DELETE", path: "/internal/ai/prompts/p%2F1" }]);

    upstream.use(http.delete("*/internal/ai/prompts/:id", () => docspaceError(404)));
    await expect(storage.delete("p")).resolves.toBeUndefined();
    upstream.use(http.delete("*/internal/ai/prompts/:id", () => docspaceError(500)));
    await expect(storage.delete("p")).rejects.toMatchObject({ status: 500 });
  });

  it("deletes every prompt of the folder one by one", async () => {
    upstream.use(
      http.get("*/internal/ai/prompt-folders/:id/prompts", () =>
        docspace([promptDto({ id: "a" }), promptDto({ id: "b" })]),
      ),
      http.delete("*/internal/ai/prompts/:id", noContent),
    );

    await storage.deleteByFolder("f");

    expect(upstream.seen.map((r) => `${r.method} ${r.path}`).sort()).toEqual([
      "DELETE /internal/ai/prompts/a",
      "DELETE /internal/ai/prompts/b",
      "GET /internal/ai/prompt-folders/f/prompts",
    ]);
  });
});
