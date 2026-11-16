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

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../httpClient.js", () => ({
  proxyBaseUrl: "http://portal",
  withTimeout: () => ({ signal: undefined, cancel: () => {} }),
}));
vi.mock("../../requestContext.js", () => ({
  countFilesApiRead: vi.fn(),
  getForwardedHeaders: () => ({ cookie: "asc_auth_key=x" }),
  getFolderInfoCache: () => undefined,
  setSourceMeta: vi.fn(),
}));
const lookupChatContextFolder = vi.fn();
vi.mock("../chatContextSnapshot.js", () => ({ lookupChatContextFolder }));

const { resolveAiProjectEntityId } = await import("../docspaceFilesApi.js");

const fetchMock = vi.fn();
const folderDto = (response: Record<string, unknown>) => ({
  ok: true,
  status: 200,
  json: async () => ({ response }),
});

describe("resolveAiProjectEntityId", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    lookupChatContextFolder.mockReset();
    lookupChatContextFolder.mockReturnValue({ hit: false });
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  // What `GET files/folder/{id}` answers for a project: no `type`, since
  // FolderDto fills it for a few system folders only.
  it("names a project by the room type of the Files DTO", async () => {
    fetchMock.mockResolvedValue(folderDto({ id: 37, title: "Budget Planner", roomType: 12 }));

    expect(await resolveAiProjectEntityId("37")).toBe("37");
    expect(fetchMock.mock.calls[0]?.[0]).toBe("http://portal/api/2.0/files/folder/37");
  });

  it("names a project by the folder type of the chat-context snapshot", async () => {
    lookupChatContextFolder.mockReturnValue({ hit: true, info: { folderType: 40 } });

    expect(await resolveAiProjectEntityId("37")).toBe("37");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("answers nothing for another room, a plain folder, an unknown one or no id", async () => {
    fetchMock.mockResolvedValueOnce(folderDto({ id: 35, title: "Budgeting", roomType: 10 }));
    expect(await resolveAiProjectEntityId("35")).toBeUndefined();

    fetchMock.mockResolvedValueOnce(folderDto({ id: 4, title: "my" }));
    expect(await resolveAiProjectEntityId("4")).toBeUndefined();

    fetchMock.mockResolvedValueOnce({ ok: false, status: 404, statusText: "Not Found" });
    expect(await resolveAiProjectEntityId("999")).toBeUndefined();

    expect(await resolveAiProjectEntityId(undefined)).toBeUndefined();
    expect(await resolveAiProjectEntityId("abc")).toBeUndefined();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});
