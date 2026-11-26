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
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";
import { assertThreadCreatable } from "../threadsController.js";

// The thread gate with the real AssignmentsEngine and the real storage: only the AI service is
// stubbed. GET internal/ai/assignments/{actionType} (AssignmentsStorageController.cs),
// GET internal/ai/profiles/{id} (ProfileStorageController.cs).
const upstream = useUpstream();

const ASSIGNED = "6f1c2a3b-0000-4000-8000-000000000009";

function chatSlotNames(profileRead: () => Response): void {
  upstream.use(
    http.get("*/internal/ai/assignments/Chat", () => docspace(ASSIGNED)),
    http.get("*/internal/ai/profiles/:id", profileRead),
  );
}

describe("assertThreadCreatable over the real assignments engine", () => {
  it("refuses with 404 when the assigned chat profile is gone, as when none is assigned", async () => {
    chatSlotNames(() => docspaceError(404));

    await expect(assertThreadCreatable(undefined, undefined)).rejects.toMatchObject({
      status: 404,
      message: "no AI profile is available for this thread",
    });
  });

  it("passes the failure of the assigned profile's read on", async () => {
    chatSlotNames(() => docspaceError(500));

    await expect(assertThreadCreatable(undefined, undefined)).rejects.toMatchObject({
      status: 500,
    });
  });
});
