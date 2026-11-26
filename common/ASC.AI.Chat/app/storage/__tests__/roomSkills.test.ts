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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../httpClient.js", () => ({
  proxyBaseUrl: "http://portal",
  withTimeout: () => ({ signal: undefined, cancel: () => {} }),
}));
class FakeDocspaceApiHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly url: string,
  ) {
    super(statusText);
  }
}
const filesApiGet = vi.fn();
vi.mock("../docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
  filesApiGet,
}));
vi.mock("../../requestContext.js", () => ({
  countFilesApiRead: vi.fn(),
  getForwardedHeaders: () => ({}),
}));

const { getContextFolders, getKnowledgeFolders, getRoomSkill, getRoomSkills, getRoomsSkills } =
  await import("../roomSkills.js");

const fetchMock = vi.fn();

describe("getKnowledgeFolders", () => {
  beforeEach(() => {
    filesApiGet.mockReset();
  });

  it("lists the Knowledge rooms of the AI rooms root, not of Rooms", async () => {
    filesApiGet.mockResolvedValue({ folders: [{ id: 7, title: "Docs", roomType: 11 }], total: 1 });

    const folders = await getKnowledgeFolders();

    expect(filesApiGet.mock.calls).toEqual([
      ["rooms", { type: 11, searchArea: "AiHub", count: 100, startIndex: 0 }],
    ]);
    expect(folders).toHaveLength(1);
  });
});

const catalog = (
  ...rooms: { id: number; skills: { id: string; name: string; description: string }[] }[]
) => ({
  rooms: rooms.map((room) => ({ title: `Room ${room.id}`, logo: null, ...room })),
});

describe("skills catalog", () => {
  beforeEach(() => {
    filesApiGet.mockReset();
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("reads the rooms' skills from the Files API catalog, 20 rooms a call", async () => {
    filesApiGet.mockImplementation(async (_path: string, query: { roomIds: string[] }) =>
      catalog(
        ...query.roomIds.map((id) => ({
          id: Number(id),
          skills: [{ id: `f${id}`, name: "n", description: "d" }],
        })),
      ),
    );
    const ids = Array.from({ length: 25 }, (_, i) => String(i + 1));

    const skills = await getRoomsSkills(ids);

    expect(filesApiGet).toHaveBeenCalledTimes(2);
    expect(filesApiGet.mock.calls[0]?.[0]).toBe("rooms/skills");
    expect(filesApiGet.mock.calls[0]?.[1].roomIds).toHaveLength(20);
    expect(filesApiGet.mock.calls[1]?.[1].roomIds).toEqual(["21", "22", "23", "24", "25"]);
    expect(skills.get("25")).toEqual([{ id: "f25", name: "n", description: "d" }]);
  });

  it("answers 404 for a room the catalog leaves out", async () => {
    filesApiGet.mockResolvedValue(catalog());
    await expect(getRoomSkills("7")).rejects.toMatchObject({ status: 404 });
  });

  it("reads a skill body only for a skill of that room", async () => {
    filesApiGet.mockResolvedValue(
      catalog({ id: 7, skills: [{ id: "f1", name: "tone", description: "Tone." }] }),
    );
    await expect(getRoomSkill("7", "f2")).rejects.toMatchObject({ status: 404 });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("offers only the Instructions rooms that hold a skill", async () => {
    filesApiGet.mockImplementation(async (path: string) =>
      path === "rooms"
        ? {
            folders: [
              { id: 7, title: "Brand voice", roomType: 10 },
              { id: 8, title: "Empty", roomType: 10 },
            ],
            total: 2,
          }
        : catalog(
            { id: 7, skills: [{ id: "f1", name: "tone", description: "Tone." }] },
            { id: 8, skills: [] },
          ),
    );

    const folders = await getContextFolders();

    expect(folders).toEqual([{ cloud: "docspace", rooms: [{ id: "7", name: "Brand voice" }] }]);
    expect(filesApiGet.mock.calls.map(([path]) => path)).toEqual(["rooms", "rooms/skills"]);
  });
});
