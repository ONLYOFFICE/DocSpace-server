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
/// The request parameters for accessing a file by its ID.
/// </summary>
public class FileIdRequestDto<T>
{
    /// <summary>
    /// The file the operation addresses. Take the identifier from a listing such as `GET api/2.0/files/{folderId}`: a
    /// file stored on the portal is numbered, while a file in a connected third-party account is named by an opaque
    /// string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }
}

/// <summary>
/// The request that names one file, and how much of a list to answer with.
/// </summary>
public class FilePrimaryIdRequestDto<T>
{
    /// <summary>
    /// The file the operation addresses. Take the identifier from a listing such as `GET api/2.0/files/{folderId}`: a
    /// file stored on the portal is numbered, while a file in a connected third-party account is named by an opaque
    /// string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// How many entries at most to answer with, in the operations of this file that return a list; an operation that
    /// answers with a single object is not affected by it.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many entries of such a list to skip before answering, used together with `count` to walk through it page
    /// by page.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}

/// <summary>
/// The operation to cancel.
/// </summary>
public class OperationIdRequestDto
{
    /// <summary>
    /// The operation to cancel, as returned in `id` when it was started. A call that leaves the route segment out
    /// cancels every operation of the caller, and an id that is not among their operations cancels nothing without
    /// being an error.
    /// </summary>
    /// <example>b2f3e9a4-7c15-4d8e-9f60-3a1c5e7d0b42</example>
    [FromRoute(Name = "id")]
    public required string Id { get; set; } = null;
}