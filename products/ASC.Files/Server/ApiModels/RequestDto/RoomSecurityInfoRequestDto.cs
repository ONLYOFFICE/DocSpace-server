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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The room security request parameters.
/// </summary>
public class RoomSecurityInfoRequestDto<T>
{
    /// <summary>
    /// The room whose access list is read, named by the identifier that `GET api/2.0/files/rooms` reports for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// What kind of access entries to list. The default covers accounts and groups and leaves the sharing links of
    /// the room out; those are read with `GET api/2.0/files/rooms/{id}/links`.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "filterType")]
    public ShareFilterType FilterType { get; set; } = ShareFilterType.UserOrGroup;

    /// <summary>
    /// How many entries to return in one answer. The total number of matching entries comes back in the response
    /// headers, so it is what tells the caller whether another page is needed.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many matching entries to skip before the page starts. Together with the page size it walks the list, which
    /// is ordered by role and then by name and is therefore stable between calls.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Keeps only the entries whose displayed name contains this text. An invitation that has not been accepted yet
    /// is listed under the email address it was sent to, so that is what has to be searched for.
    /// </summary>
    /// <example>Smith</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
