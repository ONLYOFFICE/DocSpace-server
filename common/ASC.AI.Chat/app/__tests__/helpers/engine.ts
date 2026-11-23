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

import type { DeepMockProxy } from "vitest-mock-extended";
import { mockDeep } from "vitest-mock-extended";

export type { DeepMockProxy } from "vitest-mock-extended";
export { mockReset } from "vitest-mock-extended";

/**
 * A stand-in for an engine class of `@onlyoffice/ai-chat/core`, typed against the real class:
 * every method is a spy with the library's own signature, so a method the library renames or
 * a changed argument stops the test from compiling instead of leaving it green.
 *
 *   const engine = engineMock<PromptsEngine>();
 *   vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
 *     ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
 *     PromptsEngine: engineClass(engine),
 *   }));
 *   const { promptsController } = await import("../promptsController.js");
 *
 * `restoreMocks` does not reset these spies: call `mockReset(engine)` in `beforeEach`.
 */
export function engineMock<T extends object>(): DeepMockProxy<T> {
  return mockDeep<T>();
}

/**
 * A class whose `new` answers `instance`, to put in place of the library's class. Pass a
 * function, `engineClass(() => engine)`, in a file that also imports `helpers/schema.js`: that
 * import loads the library, and with it the mock's factory, before `engine` is initialized.
 */
export function engineClass<T extends object>(
  instance: T | (() => T),
): new (...args: unknown[]) => T {
  return class {
    constructor() {
      return typeof instance === "function" ? instance() : instance;
    }
  } as unknown as new (...args: unknown[]) => T;
}
