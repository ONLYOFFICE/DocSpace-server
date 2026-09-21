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
/// Represents a data transfer object used for requesting a list of profiles.
/// </summary>
/// <remarks>
/// The request allows for pagination and filtering by specific criteria.
/// Use the available properties to customize the parameters of the request.
/// </remarks>
public class GetAllProfilesRequestDto
{
    /// <summary>
    /// The size of the page. It defaults to 100, which is also the largest value the operation accepts.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matches to skip before the page starts. It defaults to 0, and the total number of matches is
    /// reported in the total count of the response.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The only recognised value is `group`, which makes `filterValue` the ID of the group to keep the members of.
    /// Any other value, and omitting the field, applies no group filter.
    /// </summary>
    /// <example>group</example>
    [FromQuery(Name = "filterBy")]
    public string FilterBy { get; set; }

    /// <summary>
    /// What to order the accounts by, compared without regard to case: `FirstName`, `LastName`, `DisplayName`,
    /// `Type`, `Email`, `Department`, `UsedSpace`, `CreatedBy` or `RegistrationDate`.
    /// </summary>
    /// <example>DisplayName</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction of the ordering: `Ascending`, which is the default, or `Descending`.
    /// </summary>
    /// <example>Ascending</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The character that splits `filterValue` into several terms, of which any one may match. Omit it to split
    /// the value on spaces instead, in which case every term has to match.
    /// </summary>
    /// <example>,</example>
    [FromQuery(Name = "filterSeparator")]
    public string FilterSeparator { get; set; }

    /// <summary>
    /// The text to match against the name and the email of the account, case-insensitively. Omit it to apply no
    /// text filter.
    /// </summary>
    /// <example>John</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}