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

const readById = vi.fn();
vi.mock("../../storage/index.js", () => ({ storage: { profiles: { readById } } }));

const { resolveStoredProfile } = await import("../storedProfile.js");

describe("resolveStoredProfile", () => {
  beforeEach(() => {
    readById.mockReset();
  });

  it("answers the stored profile, whatever else the client sent with the id", async () => {
    const stored = { id: "p1", baseUrl: "https://api.example.com/v1", key: "stored" };
    readById.mockResolvedValue(stored);

    const profile = await resolveStoredProfile({
      id: "p1",
      baseUrl: "http://169.254.169.254/latest",
      key: "sent",
      headers: { "X-Forwarded-For": "127.0.0.1" },
    });

    expect(profile).toBe(stored);
    expect(readById).toHaveBeenCalledExactlyOnceWith("p1");
  });

  it("answers 404 for a profile the store does not hold or cannot read", async () => {
    readById.mockResolvedValueOnce(undefined);
    await expect(resolveStoredProfile({ id: "gone" })).rejects.toMatchObject({ status: 404 });

    readById.mockRejectedValueOnce(new Error("bad id"));
    await expect(resolveStoredProfile({ id: "not-a-guid" })).rejects.toMatchObject({ status: 404 });
  });

  it("answers 404 without a read when no id was sent", async () => {
    await expect(resolveStoredProfile({ baseUrl: "http://127.0.0.1" })).rejects.toMatchObject({
      status: 404,
    });
    await expect(resolveStoredProfile(undefined)).rejects.toMatchObject({ status: 404 });
    expect(readById).not.toHaveBeenCalled();
  });
});
