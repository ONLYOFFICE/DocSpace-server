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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The advanced search parameters.
/// </summary>
public class AdvancedSearchDto
{
    /// <summary>
    /// The account state to search in, taken from the route: `Active` for working accounts, `Terminated` for
    /// disabled ones, `Pending` for open invitations, or `All` for every state.
    /// </summary>
    /// <example>Active</example>
    [FromRoute(Name = "status")]
    public required EmployeeStatus Status { get; set; }

    /// <summary>
    /// The term to look for, matched as a case-insensitive substring of the first name, the last name, the user
    /// name, the email and the contacts. It is required in practice, because the search cannot run without it.
    /// </summary>
    /// <example>John</example>
    [FromQuery(Name = "query")]
    public string Query { get; set; }

    /// <summary>
    /// The only recognised value is `group`, which turns `filterValue` into a group ID and keeps only the members of
    /// that group. Any other value, and omitting the field, applies no group filter.
    /// </summary>
    /// <example>group</example>
    [FromQuery(Name = "filterBy")]
    public string FilterBy { get; set; }

    /// <summary>
    /// The group ID to keep the members of, used only when `filterBy` is `group`. It has to be a valid identifier -
    /// a group name is not accepted.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
