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
/// The request parameters for getting groups with their sharing settings.
/// </summary>
public class GetGroupsWithSharedRequestDto<T>
{
    /// <summary>
    /// The ID of the room, folder or file whose access the search is run against, taken from the route. It is an
    /// integer for an entry stored in DocSpace and a provider-specific string for an entry in a connected
    /// third-party storage.
    /// </summary>
    /// <example>1234</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// Keeps only the groups that do not have access to the entry yet, which is the set to offer when granting
    /// access. Every returned entry then has `shared` set to false; without the flag every matching group comes back
    /// and `shared` tells them apart.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeShared")]
    public bool? ExcludeShared { get; set; }

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
    /// The text to match against the group name. Omit it to get every group the caller may grant access to.
    /// </summary>
    /// <example>Marketing</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}