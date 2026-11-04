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
const countFilesApiRead = vi.fn();
vi.mock("../../requestContext.js", () => ({
  countFilesApiRead,
  getForwardedHeaders: () => ({ cookie: "asc_auth_key=x" }),
}));
vi.mock("../chatContextSnapshot.js", () => ({ lookupChatContextFolder: () => ({ hit: false }) }));

const { filesApiGet, DocspaceApiHttpError } = await import("../docspaceFilesApi.js");

const fetchMock = vi.fn();

describe("filesApiGet", () => {
  beforeEach(() => {
    fetchMock.mockReset();
    countFilesApiRead.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("answers the envelope's response, with the caller's credentials and repeated array params", async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({ response: { rooms: [] } }) });

    expect(await filesApiGet("rooms/skills", { roomIds: ["7", "8"] })).toEqual({ rooms: [] });

    const [url, init] = fetchMock.mock.calls[0] ?? [];
    expect(url).toBe("http://portal/api/2.0/files/rooms/skills?roomIds=7&roomIds=8");
    expect(init.headers).toEqual({ cookie: "asc_auth_key=x" });
    expect(countFilesApiRead).toHaveBeenCalledTimes(1);
  });

  it("throws the Files API status", async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 404, statusText: "Not Found" });
    const err = await filesApiGet("rooms/42/sources").catch((e: unknown) => e);
    expect(err).toBeInstanceOf(DocspaceApiHttpError);
    expect(err).toMatchObject({ status: 404 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe("http://portal/api/2.0/files/rooms/42/sources");
  });
});
