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

// Placeholder values shared by every example this service emits: the parameter
// and request-body examples in `app/openapi.ts` and the schema-property
// examples in `scripts/lib/schemaDocs.ts`.
//
// One module rather than two copies, because the point of these values is that
// they agree: a payload assembled from the document carries the same thread ID
// in its path, its body and its schema example. Two independent maps kept in
// step by a comment would drift on the first edit that touched only one.
//
// The values follow the convention the rest of the repository uses for an
// identifier example - a repeated-digit GUID, unmistakably a placeholder rather
// than something that could be mistaken for a live ID (see
// `UpdateMembersRequestDto` in ASC.People, which pairs `00000000-…` with
// `11111111-…` for exactly this reason). One value per entity, so an example
// that carries several identifiers at once shows them as distinct instead of
// repeating the same GUID three times and implying they must match.
//
// `room` is the exception: a DocSpace room ID is an integer, not a GUID.
export const EXAMPLE_IDS = {
  profile: "00000000-0000-0000-0000-000000000000",
  thread: "11111111-1111-1111-1111-111111111111",
  message: "22222222-2222-2222-2222-222222222222",
  prompt: "33333333-3333-3333-3333-333333333333",
  promptFolder: "44444444-4444-4444-4444-444444444444",
  attachment: "55555555-5555-5555-5555-555555555555",
  attachmentSecond: "66666666-6666-6666-6666-666666666666",
  room: "1234",
} as const;

// Epoch milliseconds and the same instant in ISO-8601, so a payload carrying
// both forms does not look self-contradictory.
export const EXAMPLE_AT_MS = 1767225600000;
export const EXAMPLE_AT_ISO = "2026-01-01T00:00:00.000Z";
