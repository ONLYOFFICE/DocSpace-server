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

const aiServiceGet = vi.fn();
class FakeAiServiceHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly body = "",
  ) {
    super(statusText);
  }
}
vi.mock("../../storage/httpClient.js", () => ({
  aiService: { get: aiServiceGet, post: vi.fn(), put: vi.fn(), delete: vi.fn() },
  AiServiceHttpError: FakeAiServiceHttpError,
}));
vi.mock("../../storage/index.js", () => ({ storage: { profiles: { readById: vi.fn() } } }));

const { assertKnowledgeRoomsLinkable } = await import("../aiProjectsController.js");

describe("assertKnowledgeRoomsLinkable", () => {
  beforeEach(() => {
    aiServiceGet.mockReset();
  });

  it("refuses a Knowledge room while vectorization is disabled", async () => {
    aiServiceGet.mockResolvedValue({ vectorizationEnabled: false });
    await expect(
      assertKnowledgeRoomsLinkable({ title: "Sales", aiProject: { knowledgeRoomIds: [9] } }),
    ).rejects.toMatchObject({ status: 400 });
    expect(aiServiceGet).toHaveBeenCalledWith("/config");
  });

  it("lets a Knowledge room through while vectorization is enabled", async () => {
    aiServiceGet.mockResolvedValue({ vectorizationEnabled: true });
    await expect(
      assertKnowledgeRoomsLinkable({ aiProject: { knowledgeRoomIds: [9] } }),
    ).resolves.toBeUndefined();
  });

  it("does not read the config for an empty or absent list", async () => {
    await assertKnowledgeRoomsLinkable({ aiProject: { knowledgeRoomIds: [] } });
    await assertKnowledgeRoomsLinkable({ aiProject: { instructionsRoomIds: [7] } });
    await assertKnowledgeRoomsLinkable({ title: "Renamed" });
    expect(aiServiceGet).not.toHaveBeenCalled();
  });

  it("does not block the save when the config cannot be read", async () => {
    aiServiceGet.mockRejectedValue(new Error("down"));
    await expect(
      assertKnowledgeRoomsLinkable({ aiProject: { knowledgeRoomIds: [9] } }),
    ).resolves.toBeUndefined();
  });
});
