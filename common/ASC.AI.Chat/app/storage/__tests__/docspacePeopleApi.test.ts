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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../httpClient.js", () => ({
  proxyBaseUrl: "http://portal",
  withTimeout: () => ({ signal: undefined, cancel: () => {} }),
}));
vi.mock("../../requestContext.js", () => ({
  countFilesApiRead: vi.fn(),
  getForwardedHeaders: () => ({ cookie: "asc_auth_key=x" }),
}));
vi.mock("../chatContextSnapshot.js", () => ({ lookupChatContextFolder: () => ({ hit: false }) }));

const { mayCreateAiRooms } = await import("../docspacePeopleApi.js");

const fetchMock = vi.fn();

function answerSelf(flags: Record<string, boolean>): void {
  fetchMock.mockResolvedValue(
    new Response(JSON.stringify({ response: { id: "u1", ...flags } }), { status: 200 }),
  );
}

describe("mayCreateAiRooms", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  it.each([
    ["the portal owner", { isOwner: true }],
    ["a DocSpace admin", { isAdmin: true }],
    ["a room admin", { isRoomAdmin: true }],
  ])("lets %s create", async (_who, flags) => {
    answerSelf(flags);
    expect(await mayCreateAiRooms()).toBe(true);
    expect(fetchMock.mock.calls[0]![0]).toBe("http://portal/api/2.0/people/@self");
  });

  it.each([
    ["a User", { isCollaborator: true }],
    ["a Guest", { isVisitor: true }],
  ])("does not let %s create", async (_who, flags) => {
    answerSelf(flags);
    expect(await mayCreateAiRooms()).toBe(false);
  });

  it("fails with the portal's status when the caller cannot be read", async () => {
    fetchMock.mockResolvedValue(new Response("", { status: 401, statusText: "Unauthorized" }));
    await expect(mayCreateAiRooms()).rejects.toMatchObject({ status: 401 });
  });
});
