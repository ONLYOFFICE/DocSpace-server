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

import { describe, expect, it } from "vitest";
import { errorBodyText } from "../errorBody.js";

describe("errorBodyText", () => {
  it("reads a refused answer's body as text", async () => {
    expect(await errorBodyText(new Response("upstream detail", { status: 500 }))).toBe(
      "upstream detail",
    );
  });

  it("answers an empty text for an answer without a body", async () => {
    expect(await errorBodyText(new Response(null, { status: 502 }))).toBe("");
  });

  // ts-ai-failure-paths.md §7: a body from outside is read with a byte limit. A provider that
  // answers 500 and keeps sending is cut off at 64 KiB, and the rest is never waited for.
  it("reads at most 64 KiB and stops there, even when the body never ends", async () => {
    let cancelled = false;
    const endless = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(new TextEncoder().encode("x".repeat(70 * 1024)));
      },
      cancel() {
        cancelled = true;
      },
    });

    const text = await errorBodyText(new Response(endless, { status: 500 }));

    expect(text).toHaveLength(64 * 1024);
    expect(cancelled).toBe(true);
  });

  it("answers an empty text when the body fails half-way", async () => {
    const broken = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.error(new Error("connection reset"));
      },
    });

    expect(await errorBodyText(new Response(broken, { status: 500 }))).toBe("");
  });
});
