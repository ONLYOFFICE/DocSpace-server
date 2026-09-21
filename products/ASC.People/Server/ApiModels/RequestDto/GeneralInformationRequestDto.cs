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
/// The request parameters for getting the general group information.
/// </summary>
public class GeneralInformationRequestDto
{
    /// <summary>
    /// Keeps only the groups the account with this ID takes part in. Omit it to search every group of the portal.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userId")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Narrows `userId` down to the groups that account manages, instead of every group it belongs to. It has no
    /// effect on its own and defaults to false.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "manager")]
    public bool? Manager { get; set; }

    /// <summary>
    /// The size of the page. It defaults to 100, which is also the largest value the operation accepts.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching groups to skip before the page starts. It defaults to 0, and the total number of
    /// matches is reported in the total count of the response.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// What to order the groups by: `Title`, `Manager` or `MembersCount`, compared without regard to case. Any other
    /// value, and omitting the field, orders by title.
    /// </summary>
    /// <example>Title</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction of the ordering: `Ascending`, which is the default, or `Descending`.
    /// </summary>
    /// <example>Ascending</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text to match against the group name. Omit it to get every group.
    /// </summary>
    /// <example>Marketing</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}