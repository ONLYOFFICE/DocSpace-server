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
const engine = { touch: vi.fn(), openOrCreate: vi.fn() };
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@onlyoffice/ai-chat/core")>();
  return {
    ...actual,
    ThreadsEngine: class {
      touch = engine.touch;
      openOrCreate = engine.openOrCreate;
    },
    AssignmentsEngine: class {
      tryResolveForAction = vi.fn();
    },
  };
});
const readById = vi.fn();
vi.mock("../../storage/index.js", () => ({ storage: { profiles: { readById } } }));
const assertEntityAccessible = vi.fn(async () => {});
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: class extends Error {},
  assertEntityAccessible,
  primeSourceMeta: vi.fn(async () => {}),
}));
vi.mock("../../requestContext.js", () => ({ getSourceMeta: vi.fn() }));
const readAiProjectChatContext = vi.fn();
vi.mock("../../storage/aiProjectChatContext.js", () => ({ readAiProjectChatContext }));

const { threadsController } = await import("../threadsController.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const run = async (name: keyof typeof threadsController, body: unknown) => {
  const res = { status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  const next = vi.fn();
  await (threadsController[name] as unknown as Handler)({ body, query: {} }, res, next);
  return {
    status: res.status.mock.calls[0]?.[0] as number | undefined,
    body: res.json.mock.calls[0]?.[0],
  };
};

const profile = { id: "p1", name: "Chatty" };

beforeEach(() => {
  engine.touch.mockReset().mockResolvedValue(undefined);
  engine.openOrCreate.mockReset().mockResolvedValue({ thread: { id: "t1" } });
  readById.mockReset().mockResolvedValue(profile);
  readAiProjectChatContext.mockReset().mockResolvedValue(undefined);
});

describe("threadsController.touch", () => {
  it.each([[{}], [null], [[undefined, "p1"]], [{ threadId: 7 }]])(
    "answers 400 for the body %j, with no thread id",
    async (body) => {
      expect((await run("touch", body)).status).toBe(400);
      expect(engine.touch).not.toHaveBeenCalled();
    },
  );

  it("touches the thread the widget names", async () => {
    expect((await run("touch", ["t1", "p1"])).body).toEqual({ success: true });
    expect(engine.touch).toHaveBeenCalledWith("t1", "p1");
  });
});

describe("threadsController.openOrCreate in an AI project", () => {
  const body = { profile: { id: "p1" }, entityId: "12", firstMessage: "hi" };

  it("refuses to create a thread for a caller who may not chat in it", async () => {
    readAiProjectChatContext.mockResolvedValue({ canChat: false });
    const outcome = await run("openOrCreate", body);
    expect(outcome.status).toBe(403);
    expect(engine.openOrCreate).not.toHaveBeenCalled();
  });

  it("refuses to create a thread while the project's model is unavailable", async () => {
    readAiProjectChatContext.mockResolvedValue({ canChat: true, profileAvailable: false });
    const outcome = await run("openOrCreate", body);
    expect(outcome.status).toBe(409);
    expect(engine.openOrCreate).not.toHaveBeenCalled();
  });

  it("creates one for a caller who may chat", async () => {
    readAiProjectChatContext.mockResolvedValue({
      canChat: true,
      profileAvailable: true,
      effectiveProfileId: "p1",
    });
    expect((await run("openOrCreate", body)).body).toEqual({ thread: { id: "t1" } });
  });

  it("opens an existing thread without the creation gate", async () => {
    readAiProjectChatContext.mockResolvedValue({ canChat: false });
    const outcome = await run("openOrCreate", { ...body, threadId: "t1" });
    expect(outcome.body).toEqual({ thread: { id: "t1" } });
    expect(readAiProjectChatContext).not.toHaveBeenCalled();
  });

  it("creates one outside a project as before", async () => {
    expect((await run("openOrCreate", { profile: { id: "p1" } })).body).toEqual({
      thread: { id: "t1" },
    });
  });
});
