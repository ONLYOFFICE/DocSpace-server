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

import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
const engine = {
  saveFilesMany: vi.fn(),
  getMany: vi.fn(),
  delete: vi.fn(),
  deleteMany: vi.fn(),
  linkToMessage: vi.fn(),
};
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@onlyoffice/ai-chat/core")>();
  return {
    ...actual,
    AttachmentsEngine: class {
      saveFilesMany = engine.saveFilesMany;
      getMany = engine.getMany;
      delete = engine.delete;
      deleteMany = engine.deleteMany;
      linkToMessage = engine.linkToMessage;
    },
  };
});
const readThreadId = vi.fn();
const findMissingIds = vi.fn();
vi.mock("../../storage/index.js", () => ({
  storage: { messages: { readThreadId }, attachments: { findMissingIds } },
}));
vi.mock("../../requestContext.js", () => ({ setAnalyzeEntryIds: vi.fn() }));
vi.mock("../../forms/suggestedQuestions.js", () => ({ getSuggestedQuestions: vi.fn() }));

const { attachmentsController } = await import("../attachmentsController.js");
const { MAX_ATTACHMENTS_PER_REQUEST } = await import("../_helpers.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const run = async (name: keyof typeof attachmentsController, body: unknown) => {
  const res = { status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  const next = vi.fn();
  await (attachmentsController[name] as unknown as Handler)({ body, query: {} }, res, next);
  const failure = next.mock.calls[0];
  if (failure) throw failure[0];
  return {
    status: res.status.mock.calls[0]?.[0] as number | undefined,
    body: res.json.mock.calls[0]?.[0],
  };
};

const ids = (count: number) => Array.from({ length: count }, (_, i) => `a${i}`);
const input = (path: string) => ({ path, content: "", type: 65 });

beforeEach(() => {
  for (const mock of Object.values(engine)) {
    mock.mockReset();
  }
  readThreadId.mockReset().mockResolvedValue("t1");
  findMissingIds.mockReset().mockResolvedValue([]);
});

describe("attachmentsController limits", () => {
  it("saves a batch up to the limit and refuses a larger one", async () => {
    engine.saveFilesMany.mockResolvedValue([]);
    const whole = ids(MAX_ATTACHMENTS_PER_REQUEST).map(input);
    expect((await run("saveFilesMany", [whole, undefined])).status).toBeUndefined();
    expect(engine.saveFilesMany).toHaveBeenCalledTimes(1);

    const over = await run("saveFilesMany", [[...whole, input("x")], undefined]);
    expect(over.status).toBe(400);
    expect(engine.saveFilesMany).toHaveBeenCalledTimes(1);
  });

  it("reads up to the limit, keeping repeats in their places, and refuses more", async () => {
    engine.getMany.mockImplementation(async (list: string[]) => list.map((id) => ({ id })));
    const read = await run("getMany", ["a1", "a1", "a2"]);
    expect(read.body).toEqual([{ id: "a1" }, { id: "a1" }, { id: "a2" }]);

    const over = await run("getMany", ids(MAX_ATTACHMENTS_PER_REQUEST + 1));
    expect(over.status).toBe(400);
    expect(engine.getMany).toHaveBeenCalledTimes(1);
  });

  it("binds an attachment named twice once and refuses more ids than the limit", async () => {
    await run("linkToMessage", [["a1", "a1", "a2"], "m1", "t1"]);
    expect(engine.linkToMessage).toHaveBeenCalledWith(["a1", "a2"], "m1", "t1");
    // Existence is checked on the records, not through the read that inlines images.
    expect(findMissingIds).toHaveBeenCalledWith(["a1", "a2"]);
    expect(engine.getMany).not.toHaveBeenCalled();

    findMissingIds.mockResolvedValueOnce(["a2"]);
    const gone = await run("linkToMessage", [["a1", "a2"], "m1", "t1"]);
    expect(gone.status).toBe(404);
    expect(gone.body).toEqual({ error: "attachment(s) not found: a2" });
    expect(engine.linkToMessage).toHaveBeenCalledTimes(1);

    const over = await run("linkToMessage", [ids(MAX_ATTACHMENTS_PER_REQUEST + 1), "m1", "t1"]);
    expect(over.status).toBe(400);
    expect(engine.linkToMessage).toHaveBeenCalledTimes(1);
  });
});

describe("attachmentsController.delete", () => {
  it.each([[{}], [null], [""], [42]])("answers 400 for the body %j, with no id", async (body) => {
    expect((await run("delete", body)).status).toBe(400);
    expect(engine.delete).not.toHaveBeenCalled();
  });

  it("deletes by the bare id the widget sends", async () => {
    expect((await run("delete", "a1")).body).toEqual({ success: true });
    expect(engine.delete).toHaveBeenCalledWith("a1");
  });

  // Pinned by the C# suite: an empty or absent list is a success.
  it("leaves delete-many of nothing a success", async () => {
    expect((await run("deleteMany", [])).body).toEqual({ success: true });
    expect((await run("deleteMany", null)).body).toEqual({ success: true });
  });
});
