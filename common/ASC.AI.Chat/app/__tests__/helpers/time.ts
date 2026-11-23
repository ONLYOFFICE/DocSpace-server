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

import { afterEach, beforeEach, vi } from "vitest";

/**
 * Fake `setTimeout` for the tests of the enclosing `describe`, moved forward by itself: code
 * that waits between polls or retries runs without the test waiting for the wall clock, and
 * the test body stays as it would be on real timers (`await` the call, assert the result).
 *
 * Each real millisecond moves the fake clock by `stepMs`, so a wait of seconds takes
 * milliseconds. A test that asserts on the exact moment something happens drives the clock
 * itself with `vi.useFakeTimers()` and `vi.advanceTimersByTimeAsync()` instead.
 */
export function useFastTimers(stepMs = 500): void {
  let pump: ReturnType<typeof setInterval> | undefined;

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    pump = setInterval(() => {
      void vi.advanceTimersByTimeAsync(stepMs);
    }, 1);
  });

  afterEach(() => {
    clearInterval(pump);
    vi.useRealTimers();
  });
}
