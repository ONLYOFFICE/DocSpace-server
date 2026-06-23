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
/// The request parameters for file history.
/// </summary>
public class HistoryRequestDto
{
    /// <summary>
    /// The file ID of the history request.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required int FileId { get; set; }

    /// <summary>
    /// The start date of the history.
    /// </summary>
    /// <example>2025-01-01T00:00:00.0000000Z</example>
    [FromQuery(Name = "fromDate")]
    public ApiDateTime FromDate { get; set; }

    /// <summary>
    /// The end date of the history.
    /// </summary>
    /// <example>2025-12-31T23:59:59.0000000Z</example>
    [FromQuery(Name = "toDate")]
    public ApiDateTime ToDate { get; set; }

    /// <summary>
    /// The number of history entries to retrieve for the file log.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting index for retrieving a subset of file history entries.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}

/// <summary>
/// The request parameters for the folder history.
/// </summary>
public class HistoryFolderRequestDto
{
    /// <summary>
    /// The folder ID of the history request.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required int FolderId { get; set; }

    /// <summary>
    /// The start date of the history request.
    /// </summary>
    /// <example>2025-01-01T00:00:00.0000000Z</example>
    [FromQuery(Name = "fromDate")]
    public ApiDateTime FromDate { get; set; }

    /// <summary>
    /// The end date of the history request.
    /// </summary>
    /// <example>2025-12-31T23:59:59.0000000Z</example>
    [FromQuery(Name = "toDate")]
    public ApiDateTime ToDate { get; set; }

    /// <summary>
    /// The number of records to retrieve for the folder history.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting index from which the history records are retrieved in the request.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}