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

vi.mock("../../requestContext.js", () => ({ markForwardHeadersToProvider: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: { assignments: { readByType: vi.fn() }, profiles: { readById: vi.fn() } },
}));
vi.mock("../../storage/docspaceFilesApi.js", () => ({ getFolderInfo: vi.fn() }));
vi.mock("../../storage/docspaceFileTransfer.js", () => ({ uploadFile: vi.fn() }));
vi.mock("../../forms/suggestedQuestions.js", () => ({ extractText: vi.fn() }));

const { getFolderInfo } = await import("../../storage/docspaceFilesApi.js");
const { uploadFile } = await import("../../storage/docspaceFileTransfer.js");
const { buildSkillTemplate, createSkill, MAX_TITLE_LENGTH } = await import("../createSkill.js");

const INSTRUCTIONS_ROOM = { folderType: 38, roomType: 10, canCreate: true };

beforeEach(() => {
  vi.mocked(getFolderInfo).mockReset().mockResolvedValue(INSTRUCTIONS_ROOM);
  vi.mocked(uploadFile).mockReset().mockResolvedValue({ id: "77", title: "Sales report.md" });
});

describe("buildSkillTemplate", () => {
  it("starts with the frontmatter the Files service requires and a body to fill in", () => {
    const text = buildSkillTemplate("Sales report: Q3 <draft>");
    expect(
      text.startsWith(
        '---\nname: "sales-report-q3-draft"\ndescription: "Sales report: Q3"\n---\n\n',
      ),
    ).toBe(true);
    expect(text).toContain("# Sales report: Q3 <draft>\n");
    expect(text).toContain("## Instructions");
  });
});

describe("createSkill", () => {
  it("saves the template into the room as <title>.md", async () => {
    const file = await createSkill({ destId: "12", title: "  Sales report.md " });
    expect(file).toEqual({ id: "77", title: "Sales report.md" });
    const [folderId, title, bytes, mediaType] = vi.mocked(uploadFile).mock.calls[0]!;
    expect([folderId, title, mediaType]).toEqual(["12", "Sales report.md", "text/markdown"]);
    expect(new TextDecoder().decode(bytes)).toBe(buildSkillTemplate("Sales report"));
  });

  it.each([
    ["an empty title", "   "],
    ["only an extension", ".md"],
    ["no letter or digit", "!!! ---"],
    ["a title over the limit", "a".repeat(MAX_TITLE_LENGTH + 1)],
  ])("refuses %s before the room is read", async (_case, title) => {
    await expect(createSkill({ destId: "12", title })).rejects.toMatchObject({ status: 400 });
    expect(getFolderInfo).not.toHaveBeenCalled();
    expect(uploadFile).not.toHaveBeenCalled();
  });

  it.each([
    ["a missing folder", undefined, 404],
    ["a room of another type", { folderType: 19, roomType: 5, canCreate: true }, 400],
    ["a plain folder", { folderType: 1, canCreate: true }, 400],
    ["a room the caller cannot create in", { ...INSTRUCTIONS_ROOM, canCreate: false }, 403],
  ])("writes nothing into %s", async (_case, info, status) => {
    vi.mocked(getFolderInfo).mockResolvedValue(info);
    await expect(createSkill({ destId: "12", title: "Sales report" })).rejects.toMatchObject({
      status,
    });
    expect(uploadFile).not.toHaveBeenCalled();
  });
});
