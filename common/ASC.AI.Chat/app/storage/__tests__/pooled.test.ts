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
import { pooled } from "../pooled.js";

const turn = (): Promise<void> => new Promise((resolve) => setImmediate(resolve));

describe("pooled", () => {
  it("answers the results in the order of the items, whatever order they end in", async () => {
    const results = await pooled([3, 1, 2], 3, async (n) => {
      for (let i = 0; i < n; i++) {
        await turn();
      }
      return n * 10;
    });

    expect(results).toEqual([30, 10, 20]);
  });

  it.each([
    [1, 1],
    [2, 2],
    [4, 4],
    [10, 5],
  ])("runs at most %i of 5 items at a time", async (limit, expected) => {
    let inFlight = 0;
    let most = 0;

    await pooled([1, 2, 3, 4, 5], limit, async () => {
      most = Math.max(most, ++inFlight);
      await turn();
      inFlight--;
    });

    expect(most).toBe(expected);
  });

  it("answers an empty list for no items without calling", async () => {
    let calls = 0;

    await expect(pooled([], 4, async () => ++calls)).resolves.toEqual([]);
    expect(calls).toBe(0);
  });

  it("starts no further item after a failure and throws it once the ones in flight ended", async () => {
    const started: number[] = [];
    const ended: number[] = [];
    const boom = new Error("boom");

    const run = pooled([1, 2, 3, 4, 5, 6], 2, async (n) => {
      started.push(n);
      await turn();
      if (n === 1) {
        throw boom;
      }
      await turn();
      ended.push(n);
    });

    await expect(run).rejects.toBe(boom);
    expect(started).toEqual([1, 2]);
    expect(ended).toEqual([2]);
  });
});
