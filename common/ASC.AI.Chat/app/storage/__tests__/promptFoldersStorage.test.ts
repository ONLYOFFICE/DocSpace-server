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

import { UpstreamShapeError } from "../upstreamShape.js";
import { describe, expect, it } from "vitest";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { HttpPromptFoldersStorage } from "../promptFoldersStorage.js";

// products/ASC.AI/Server/Api/PromptFolderStorageController.cs: POST prompt-folders,
// POST prompt-folders/batch, GET prompt-folders/{id}, GET prompt-folders,
// PUT prompt-folders/{id}, DELETE prompt-folders/{id}.
const upstream = useUpstream();
const storage = new HttpPromptFoldersStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

/** `PromptFolderDto` (products/ASC.AI/Server/Models/ResponseDto/PromptFolderDto.cs). */
const folderDto = (overrides: Record<string, unknown> = {}) => ({
  id: "a1b2c3d4-0000-4000-8000-000000000020",
  name: "Reports",
  createdAt: 1_700_000_000_000,
  updatedAt: 1_700_000_005_000,
  ...overrides,
});

const folder = {
  id: "a1b2c3d4-0000-4000-8000-000000000020",
  name: "Reports",
  createdAt: 1_700_000_000_000,
  updatedAt: 1_700_000_005_000,
};

const MISSING = Symbol("missing");
function dtoWith(field: string, value: unknown): Record<string, unknown> {
  const dto: Record<string, unknown> = folderDto();
  if (value === MISSING) {
    delete dto[field];
  } else {
    dto[field] = value;
  }
  return dto;
}

describe("the PromptFolderDto mapping (read by id)", () => {
  it("maps a full PromptFolderDto and nothing else", async () => {
    upstream.use(
      http.get("*/internal/ai/prompt-folders/:id", () => docspace({ ...folderDto(), extra: 1 })),
    );
    await expect(storage.readById("f/1")).resolves.toEqual(folder);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/prompt-folders/f%2F1");
  });

  it.each([
    ["id", MISSING],
    ["id", null],
    ["id", ""],
    ["id", 1],
    ["name", MISSING],
    ["name", null],
    ["name", 1],
    ["createdAt", MISSING],
    ["createdAt", null],
    ["createdAt", "1"],
    ["updatedAt", MISSING],
    ["updatedAt", null],
    ["updatedAt", "1"],
  ])("rejects a DTO whose %s is %s as an answer of an unknown shape", async (field, value) => {
    upstream.use(
      http.get("*/internal/ai/prompt-folders/:id", () => docspace(dtoWith(field, value))),
    );
    await expect(storage.readById("f")).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });

  it.each([
    ["an empty answer", noContent()],
    ["a list", docspace([folderDto()])],
  ])("rejects %s as an answer of an unknown shape", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/prompt-folders/:id", () => answer));
    await expect(storage.readById("f")).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });

  it("reads a 404 as absent and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/prompt-folders/:id", () => docspaceError(404)));
    await expect(storage.readById("f")).resolves.toBeNull();
    upstream.use(http.get("*/internal/ai/prompt-folders/:id", () => docspaceError(500)));
    await expect(storage.readById("f")).rejects.toMatchObject({ status: 500 });
  });
});

describe("create", () => {
  it("posts the name only and answers the stored folder", async () => {
    upstream.use(http.post("*/internal/ai/prompt-folders", () => docspace(folderDto())));
    await expect(storage.create({ name: "Reports", extra: 1 } as never)).resolves.toEqual(folder);
    expect(upstream.seen).toMatchObject([
      { method: "POST", path: "/internal/ai/prompt-folders", body: { name: "Reports" } },
    ]);
  });

  it.each([
    ["an empty answer", noContent()],
    ["a DTO without name", docspace(dtoWith("name", MISSING))],
  ])("throws on %s", async (_name, answer) => {
    upstream.use(http.post("*/internal/ai/prompt-folders", () => answer));
    await expect(storage.create({ name: "n" })).rejects.toEqual(
      new UpstreamShapeError("POST prompt-folders"),
    );
  });
});

describe("createMany", () => {
  it("asks nothing for no folders", async () => {
    await expect(storage.createMany([])).resolves.toEqual([]);
    expect(upstream.seen).toEqual([]);
  });

  it("posts the names in one batch and answers the stored folders in the order received", async () => {
    upstream.use(
      http.post("*/internal/ai/prompt-folders/batch", () =>
        docspace([folderDto({ id: "a", createdAt: 1 }), folderDto({ id: "b", createdAt: 2 })]),
      ),
    );
    const created = await storage.createMany([{ name: "A" }, { name: "B" }]);
    expect(created.map((f) => f.id)).toEqual(["a", "b"]);
    expect(upstream.seen).toMatchObject([
      { method: "POST", path: "/internal/ai/prompt-folders/batch", body: { names: ["A", "B"] } },
    ]);
  });

  it("throws when the batch answers another number of folders", async () => {
    upstream.use(http.post("*/internal/ai/prompt-folders/batch", () => docspace([folderDto()])));
    await expect(storage.createMany([{ name: "A" }, { name: "B" }])).rejects.toEqual(
      new UpstreamShapeError("POST prompt-folders/batch"),
    );
  });

  it.each([
    ["an empty answer", noContent()],
    ["an object", docspace({ items: [] })],
    ["an invalid folder", docspace([folderDto(), {}])],
  ])("throws on %s", async (_name, answer) => {
    upstream.use(http.post("*/internal/ai/prompt-folders/batch", () => answer));
    await expect(storage.createMany([{ name: "A" }, { name: "B" }])).rejects.toEqual(
      new UpstreamShapeError("POST prompt-folders/batch"),
    );
  });
});

describe("readAll", () => {
  it("reads every folder, newest first", async () => {
    upstream.use(
      http.get("*/internal/ai/prompt-folders", () =>
        docspace([folderDto({ id: "old", createdAt: 1 }), folderDto({ id: "new", createdAt: 2 })]),
      ),
    );
    const all = await storage.readAll();
    expect(all.map((f) => f.id)).toEqual(["new", "old"]);
  });

  it("throws on a non-list and rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/prompt-folders", () => docspace(null)));
    await expect(storage.readAll()).rejects.toEqual(new UpstreamShapeError("GET prompt-folders"));
    upstream.use(http.get("*/internal/ai/prompt-folders", () => docspaceError(500)));
    await expect(storage.readAll()).rejects.toMatchObject({ status: 500 });
  });
});

describe("update and delete", () => {
  it("renames by the encoded id", async () => {
    upstream.use(http.put("*/internal/ai/prompt-folders/:id", noContent));
    await storage.update("f/1", "New");
    expect(upstream.seen).toMatchObject([
      { method: "PUT", path: "/internal/ai/prompt-folders/f%2F1", body: { name: "New" } },
    ]);
    upstream.use(http.put("*/internal/ai/prompt-folders/:id", () => docspaceError(404)));
    await expect(storage.update("f", "x")).rejects.toMatchObject({ status: 404 });
  });

  it("deletes, treats a 404 as done and rethrows a 500", async () => {
    upstream.use(http.delete("*/internal/ai/prompt-folders/:id", noContent));
    await storage.delete("f/1");
    expect(upstream.seen).toMatchObject([
      { method: "DELETE", path: "/internal/ai/prompt-folders/f%2F1" },
    ]);
    upstream.use(http.delete("*/internal/ai/prompt-folders/:id", () => docspaceError(404)));
    await expect(storage.delete("f")).resolves.toBeUndefined();
    upstream.use(http.delete("*/internal/ai/prompt-folders/:id", () => docspaceError(500)));
    await expect(storage.delete("f")).rejects.toMatchObject({ status: 500 });
  });
});
