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

class FakeAiServiceHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly body = "",
  ) {
    super(statusText);
  }
}
const aiGet = vi.fn();
const aiPut = vi.fn();
const aiDelete = vi.fn();
vi.mock("../httpClient.js", () => ({
  aiService: { get: aiGet, put: aiPut, delete: aiDelete },
  AiServiceHttpError: FakeAiServiceHttpError,
}));
const invalidateChatContext = vi.fn();
vi.mock("../chatContextSnapshot.js", () => ({ invalidateChatContext }));
const readById = vi.fn();
const readByType = vi.fn();
vi.mock("../index.js", () => ({
  storage: { profiles: { readById }, assignments: { readByType } },
}));

const { readProjectProfileId, writeProjectProfileId } = await import("../aiProjectModel.js");

const P1 = "11111111-1111-1111-1111-111111111111";
const P2 = "22222222-2222-2222-2222-222222222222";

describe("readProjectProfileId", () => {
  beforeEach(() => {
    aiGet.mockReset();
  });

  it("reads the project-scoped Chat assignment", async () => {
    aiGet.mockResolvedValue(P1);
    expect(await readProjectProfileId(42)).toBe(P1);
    expect(aiGet).toHaveBeenCalledWith("/assignments/Chat", { query: { entityId: "42" } });
  });

  it("answers null for no assignment, a 404 and a 403", async () => {
    aiGet.mockResolvedValueOnce(null);
    expect(await readProjectProfileId("42")).toBeNull();
    aiGet.mockRejectedValueOnce(new FakeAiServiceHttpError(404, "Not Found"));
    expect(await readProjectProfileId("42")).toBeNull();
    aiGet.mockRejectedValueOnce(new FakeAiServiceHttpError(403, "Forbidden"));
    expect(await readProjectProfileId("42")).toBeNull();
  });

  it("propagates other failures", async () => {
    aiGet.mockRejectedValueOnce(new FakeAiServiceHttpError(500, "Internal"));
    await expect(readProjectProfileId("42")).rejects.toMatchObject({ status: 500 });
  });
});

describe("writeProjectProfileId", () => {
  beforeEach(() => {
    aiGet.mockReset();
    aiPut.mockReset();
    aiDelete.mockReset();
    readById.mockReset();
    readByType.mockReset();
    invalidateChatContext.mockReset();
  });

  it("upserts a chat profile as the project's scoped assignment", async () => {
    readById.mockResolvedValue({ id: P1, capabilities: 1 });
    await writeProjectProfileId(42, P1);
    expect(aiPut).toHaveBeenCalledWith("/assignments", {
      assignments: { Chat: P1 },
      entityId: "42",
    });
    expect(invalidateChatContext).toHaveBeenCalledWith("assignments");
  });

  it("refuses a malformed id, a missing profile and one that cannot chat with 400", async () => {
    await expect(writeProjectProfileId(42, "nope")).rejects.toMatchObject({ status: 400 });
    readById.mockResolvedValueOnce(undefined);
    await expect(writeProjectProfileId(42, P1)).rejects.toMatchObject({ status: 400 });
    readById.mockResolvedValueOnce({ id: P1, capabilities: 2 });
    await expect(writeProjectProfileId(42, P1)).rejects.toMatchObject({ status: 400 });
    expect(aiPut).not.toHaveBeenCalled();
  });

  it("does nothing for null when the project has no model of its own", async () => {
    aiGet.mockResolvedValue(null);
    await writeProjectProfileId(42, null);
    expect(aiPut).not.toHaveBeenCalled();
    expect(aiDelete).not.toHaveBeenCalled();
  });

  it("pins the portal default for null, never deleting the global assignment", async () => {
    aiGet.mockResolvedValue(P1);
    readByType.mockResolvedValue(P2);
    await writeProjectProfileId(42, null);
    expect(aiPut).toHaveBeenCalledWith("/assignments", {
      assignments: { Chat: P2 },
      entityId: "42",
    });
    expect(aiDelete).not.toHaveBeenCalled();
  });

  it("keeps the project's model for null when there is no portal default", async () => {
    aiGet.mockResolvedValue(P1);
    readByType.mockResolvedValue(null);
    await writeProjectProfileId(42, null);
    expect(aiPut).not.toHaveBeenCalled();
    expect(aiDelete).not.toHaveBeenCalled();
  });
});
