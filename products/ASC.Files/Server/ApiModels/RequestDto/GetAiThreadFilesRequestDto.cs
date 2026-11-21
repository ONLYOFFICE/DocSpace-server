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
/// Request to get the files of a thread in an AI project.
/// </summary>
public class GetAiThreadFilesRequestDto
{
    /// <summary>
    /// ID of the AI project the thread was started in. Take it from `GET api/2.0/files/rooms` with `searchArea`
    /// set to `AiHub`, which lists the AI projects together with the other AI rooms.
    /// </summary>
    /// <example>18432</example>
    [FromRoute(Name = "id")]
    public required int Id { get; set; }

    /// <summary>
    /// ID of a thread the caller started in the project. Take it from `GET api/2.0/ai/threads/list` with `entityId`
    /// set to the project ID.
    /// </summary>
    /// <example>0198c3f4-6a2e-7c41-9b8d-2f5e7a1c3d90</example>
    [FromRoute(Name = "threadId")]
    public required Guid ThreadId { get; set; }

    /// <summary>
    /// The maximum number of files to return.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of files to skip.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    [Range(0, int.MaxValue)]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the files are ordered by, matched case-insensitively against the file sort fields:
    /// `DateAndTime`, `AZ`, `Size`, `Author`, `Type`, `New`, `DateAndTimeCreation`, `RoomType`, `Tags`, `Room`,
    /// `CustomOrder`, `LastOpened` and `UsedSpace`. Without a recognized value the saved default order of the account applies.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The sort order.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text to search for in the file titles.
    /// </summary>
    /// <example>report</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
