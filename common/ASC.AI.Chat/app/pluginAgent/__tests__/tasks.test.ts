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

import { afterEach, describe, expect, it } from "vitest";

import { endTask, isRunningTaskToken, runningTaskCount, startTask } from "../tasks.js";
import type { RunningTask } from "../tasks.js";

const started: RunningTask[] = [];
const start = (limit: number): RunningTask | undefined => {
  const task = startTask(limit);
  if (task) {
    started.push(task);
  }
  return task;
};

afterEach(() => {
  for (const task of started.splice(0)) {
    endTask(task);
  }
});

describe("the running plugin tasks", () => {
  it("gives every task an id and a token of its own", () => {
    const first = start(2);
    const second = start(2);
    expect(first?.id).toMatch(/^[0-9a-f-]{36}$/);
    expect(first?.registryToken).toMatch(/^[A-Za-z0-9_-]{43}$/);
    expect(second?.id).not.toBe(first?.id);
    expect(second?.registryToken).not.toBe(first?.registryToken);
    expect(runningTaskCount()).toBe(2);
  });

  it("refuses one task more than the limit and takes it once a place is free", () => {
    const first = start(2);
    start(2);
    expect(start(2)).toBeUndefined();
    expect(runningTaskCount()).toBe(2);

    endTask(first!);
    expect(start(2)).toBeDefined();
  });

  it("refuses the first task under a limit of zero", () => {
    expect(start(0)).toBeUndefined();
  });

  it("knows the token of a running task only", () => {
    const first = start(2);
    const second = start(2);
    expect(isRunningTaskToken(first!.registryToken)).toBe(true);
    expect(isRunningTaskToken(second!.registryToken)).toBe(true);
    expect(isRunningTaskToken("")).toBe(false);
    expect(isRunningTaskToken("x".repeat(43))).toBe(false);
    expect(isRunningTaskToken(first!.registryToken + "x")).toBe(false);

    endTask(first!);
    expect(isRunningTaskToken(first!.registryToken)).toBe(false);
    expect(isRunningTaskToken(second!.registryToken)).toBe(true);
  });

  it("ends a task twice without harm", () => {
    const task = start(1);
    endTask(task!);
    endTask(task!);
    expect(runningTaskCount()).toBe(0);
  });
});
