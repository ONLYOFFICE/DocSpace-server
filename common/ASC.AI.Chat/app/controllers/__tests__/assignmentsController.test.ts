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

const getAllAssignments = vi.fn();
const tryResolveForAction = vi.fn();
const cascadeProfileDelete = vi.fn();
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@onlyoffice/ai-chat/core")>();
  return {
    ...actual,
    AssignmentsEngine: class {
      getAllAssignments = getAllAssignments;
      tryResolveForAction = tryResolveForAction;
      cascadeProfileDelete = cascadeProfileDelete;
    },
  };
});
const readAllForClient = vi.fn();
const readByIdRaw = vi.fn();
// The access gate (aiAccess.ts) reads the tool prefs; a stand-in that answers lets the route go on.
const toolPrefs = vi.hoisted(() => ({ readDisabled: vi.fn() }));
vi.mock("../../storage/index.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/index.js")>()),
  storage: { profiles: { readAllForClient, readByIdRaw }, toolPrefs },
}));
const assertEntityAccessible = vi.fn(async () => {});
vi.mock("../../storage/docspaceFilesApi.js", () => ({ assertEntityAccessible }));

const { assignmentsController } = await import("../assignmentsController.js");
const { handlerSettled } = await import("../_helpers.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const call = async (query: Record<string, string>) => {
  const res = { status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  const next = vi.fn();
  (assignmentsController.getAllAssignments as unknown as Handler)({ query }, res, next);
  await handlerSettled(res);
  const failure = next.mock.calls[0];
  if (failure) throw failure[0];
  return res.json.mock.calls[0]?.[0];
};

describe("assignmentsController.getAllAssignments", () => {
  beforeEach(() => {
    getAllAssignments.mockReset();
    readAllForClient.mockReset();
    getAllAssignments.mockResolvedValue({ Chat: "p1", Default: "p2", Code: "gone" });
    readAllForClient.mockResolvedValue([
      { id: "p1", name: "Chatty", capabilities: 0x01 },
      { id: "p2", name: "Default", capabilities: null },
      { id: "p3", name: "Pictures", capabilities: 0x02 },
    ]);
  });

  it("answers the id map without expand", async () => {
    expect(await call({})).toEqual({ Chat: "p1", Default: "p2", Code: "gone" });
    expect(readAllForClient).not.toHaveBeenCalled();
  });

  it("answers the assigned profiles and the catalogue counts with expand=profile", async () => {
    const answer = await call({ expand: "profile" });
    expect(answer).toEqual({
      assignments: {
        Chat: { id: "p1", name: "Chatty", capabilities: 0x01 },
        Default: { id: "p2", name: "Default", capabilities: null },
      },
      profilesTotal: 3,
      // An unknown mask may chat; the image-only one may not.
      chatProfilesTotal: 2,
    });
  });

  it("forwards entityId to the engine either way", async () => {
    await call({ entityId: "room-7", expand: "profile" });
    expect(assertEntityAccessible).toHaveBeenCalledWith("room-7");
    expect(getAllAssignments).toHaveBeenCalledWith("room-7");
  });
});

const run = async (name: keyof typeof assignmentsController, req: Record<string, unknown>) => {
  const res = { status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  const next = vi.fn();
  (assignmentsController[name] as unknown as Handler)({ query: {}, ...req }, res, next);
  await handlerSettled(res);
  const failure = next.mock.calls[0];
  if (failure) throw failure[0];
  return {
    status: res.status.mock.calls[0]?.[0] as number | undefined,
    body: res.json.mock.calls[0]?.[0],
  };
};

describe("assignmentsController resolve operations", () => {
  // What the engine resolves with is the in-process read: the provider key,
  // and for an onlyoffice profile the internal gateway and the caller's
  // forwarded headers.
  const resolved = {
    profileId: "p1",
    profile: {
      id: "p1",
      name: "Chatty",
      baseUrl: "http://internal-gateway:5157/api/2.0/ai/gateway",
      key: "sk-secret",
      headers: { cookie: "asc_auth_key=1" },
    },
  };
  const stored = {
    id: "p1",
    name: "Chatty",
    baseUrl: "https://api.example.com/v1",
    key: "sk-secret",
    headers: { "x-extra": "1" },
  };

  beforeEach(() => {
    tryResolveForAction.mockReset().mockResolvedValue(resolved);
    readByIdRaw.mockReset().mockResolvedValue(stored);
  });

  it.each(["resolveForAction", "tryResolveForAction"] as const)(
    "%s answers the stored profile without its secrets or the internal address",
    async (name) => {
      const { body } = await run(name, { query: { actionType: "Chat" } });
      expect(body).toEqual({
        profileId: "p1",
        profile: { id: "p1", name: "Chatty", baseUrl: "https://api.example.com/v1" },
      });
      expect(readByIdRaw).toHaveBeenCalledWith("p1");
    },
  );

  it("answers nothing configured as before", async () => {
    tryResolveForAction.mockResolvedValue(null);
    expect((await run("resolveForAction", { query: { actionType: "Chat" } })).status).toBe(404);
    expect((await run("tryResolveForAction", { query: { actionType: "Chat" } })).body).toBeNull();
    expect(readByIdRaw).not.toHaveBeenCalled();
  });
});

describe("assignmentsController.cascadeProfileDelete", () => {
  beforeEach(() => {
    cascadeProfileDelete.mockReset().mockResolvedValue(undefined);
  });

  it.each([
    ["the bare string the widget sends", { body: "p1" }],
    ["a named field", { body: { profileId: "p1" } }],
    ["the query", { body: undefined, query: { profileId: "p1" } }],
  ])("takes the id as %s", async (_label, req) => {
    expect((await run("cascadeProfileDelete", req)).body).toEqual({ success: true });
    expect(cascadeProfileDelete).toHaveBeenCalledWith("p1");
  });

  it("answers 400 without an id", async () => {
    expect((await run("cascadeProfileDelete", { body: {} })).status).toBe(400);
    expect(cascadeProfileDelete).not.toHaveBeenCalled();
  });
});
