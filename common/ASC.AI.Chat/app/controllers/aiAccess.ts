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

import { storage } from "../storage/index.js";

/**
 * Borrow the AI service's access gates for a route that would otherwise
 * never reach one. The Node service knows neither the caller's employee
 * type nor whether the portal has AI switched on: a route is gated only
 * when it happens to hit a gated AI-service endpoint before doing its own
 * work, and a route that goes straight to a provider, an MCP server or a
 * body validator does nothing of the kind (Bugs 82824, 84390, 84391).
 *
 * The throwaway tool-prefs read runs the same `[AiFeature]` (AI disabled)
 * and employee-type (Guest) checks as every other prefs route; its 403
 * propagates as an `AiServiceHttpError` and `clientError` relays it, while
 * a 404 (nothing stored yet) is swallowed by the storage. One indexed
 * read per call — the C# side could expose the two flags on `/config`
 * instead, which would make this a cheaper lookup.
 */
export async function assertAiAccess(entityId?: string): Promise<void> {
  await storage.toolPrefs.readDisabled(entityId);
}
