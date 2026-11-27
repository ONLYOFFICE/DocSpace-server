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

// The plugin tasks this process runs right now: what the concurrency limit counts and what the
// package registry route checks a token against.
//
// The map lives for the process and is shared by every portal it serves
// (ts-ai-failure-paths.md §8). That is safe here: an entry holds a random id and a random token
// and nothing of a portal or a user, it is added when a task starts and removed in the `finally`
// of that task, so its size never passes the concurrency limit, and nothing reads one task's
// entry on behalf of another. A restart empties it together with the tasks themselves.

import { randomBytes, randomUUID, timingSafeEqual } from "crypto";

export interface RunningTask {
  /** A UUID: also the session the task's sandbox is created for. */
  id: string;
  /** What the task's sandbox presents to the package registry route. */
  registryToken: string;
}

const running = new Map<string, RunningTask>();

/** Takes a place for a new task; undefined when `limit` tasks already run. */
export function startTask(limit: number): RunningTask | undefined {
  if (running.size >= limit) {
    return undefined;
  }
  const task: RunningTask = {
    id: randomUUID(),
    registryToken: randomBytes(32).toString("base64url"),
  };
  running.set(task.id, task);
  return task;
}

/** Frees the task's place; its token answers nobody from here on. */
export function endTask(task: RunningTask): void {
  running.delete(task.id);
}

export function runningTaskCount(): number {
  return running.size;
}

/** Whether `token` is the registry token of a task that runs now, compared in constant time. */
export function isRunningTaskToken(token: string): boolean {
  const presented = Buffer.from(token);
  let known = false;
  for (const task of running.values()) {
    const expected = Buffer.from(task.registryToken);
    // Every token is compared, a match included: the time says nothing about which one it was.
    if (presented.length === expected.length && timingSafeEqual(presented, expected)) {
      known = true;
    }
  }
  return known;
}
