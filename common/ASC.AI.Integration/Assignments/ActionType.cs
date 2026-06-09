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

namespace ASC.AI.Integration.Assignments;

/// <summary>
/// Identifies the AI action a profile can be assigned to. Each member is a distinct
/// assignment slot within a tenant/entry scope (see <see cref="AssignmentsStorage"/>).
/// </summary>
[EnumExtensions]
public enum ActionType
{
    /// <summary>
    /// The global fallback slot used during profile resolution when no profile is bound
    /// to the requested action. This is a valid, persisted assignment value — <b>not</b> a
    /// sentinel or "unset" marker. Note that, as the first member, its underlying value is
    /// <c>0</c> (i.e. <c>default(ActionType)</c>), so reads such as
    /// <see cref="Database.AiIntegrationContext.GetAllAssignmentsAsync"/> intentionally
    /// include it; do not add a filter or constraint that excludes it.
    /// </summary>
    Default,
    Chat,
    Code,
    Summarization,
    Translation,
    TextAnalyze,
    ImageGeneration,
    OCR,
    Vision
}
