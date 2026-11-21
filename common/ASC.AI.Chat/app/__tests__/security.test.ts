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

import { describe, expect, it, vi } from "vitest";

const allowPrivate = vi.fn(() => false);
vi.mock("../../config/index.js", () => ({ allowPrivateBaseUrl: allowPrivate }));

const { assertSafeBaseUrl, InvalidUrlError } = await import("../security.js");

describe("assertSafeBaseUrl", () => {
  // The unspecified addresses connect to the host itself; no opt-in opens them.
  it.each(["http://[::]:5157/", "http://0.0.0.0:5157/", "http://169.254.169.254/latest"])(
    "refuses %s even where private ranges are allowed",
    async (url) => {
      allowPrivate.mockReturnValue(true);
      await expect(assertSafeBaseUrl(url)).rejects.toBeInstanceOf(InvalidUrlError);
      allowPrivate.mockReturnValue(false);
      await expect(assertSafeBaseUrl(url)).rejects.toBeInstanceOf(InvalidUrlError);
    },
  );

  it("refuses loopback unless private ranges are allowed", async () => {
    allowPrivate.mockReturnValue(false);
    await expect(assertSafeBaseUrl("http://[::1]:11434/v1")).rejects.toBeInstanceOf(
      InvalidUrlError,
    );
    await expect(assertSafeBaseUrl("http://127.0.0.1:11434/v1")).rejects.toBeInstanceOf(
      InvalidUrlError,
    );
    allowPrivate.mockReturnValue(true);
    await expect(assertSafeBaseUrl("http://[::1]:11434/v1")).resolves.toBeUndefined();
    await expect(assertSafeBaseUrl("http://127.0.0.1:11434/v1")).resolves.toBeUndefined();
  });

  it("accepts a public address", async () => {
    allowPrivate.mockReturnValue(false);
    await expect(assertSafeBaseUrl("https://8.8.8.8/v1")).resolves.toBeUndefined();
  });
});
