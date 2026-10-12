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
/// The query that reads one page of the contents of the .ai folder of a room.
/// </summary>
public class GetRoomAiFolderRequestDto
{
    /// <summary>
    /// The room whose .ai folder is listed, named by the identifier that `GET api/2.0/files/rooms` reports for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required int Id { get; set; }

    /// <summary>
    /// Narrows the listing to a single kind of entry, such as documents, spreadsheets or images. Omit it to list every
    /// kind the folder holds.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// The size of one page of the listing. Pair it with `startIndex` to walk through the result, and compare the two
    /// with `total` in the response to see when the last page has been read.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching entries to skip before the returned page begins; add `count` to it to ask for the next
    /// page.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the entries are ordered by, matched case-insensitively against the file sort fields,
    /// such as `DateAndTime`, `AZ`, `Size` or `Type`.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction in which the `sortBy` field is ordered.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The search string the listing is filtered by: it is matched as a substring of entry titles. Omit it to list
    /// the folder unfiltered.
    /// </summary>
    /// <example>skill</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
