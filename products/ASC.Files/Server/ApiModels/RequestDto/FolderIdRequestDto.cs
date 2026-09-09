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
/// The request that names one folder by its identifier.
/// </summary>
public class FolderIdRequestDto<T>
{
    /// <summary>
    /// The folder the operation acts on. Take the identifier from a listing such as `GET api/2.0/files/@root` or
    /// `GET api/2.0/files/{folderId}`: a folder stored in the portal is numbered, while a folder in a connected
    /// third-party account is named by an opaque string.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }
}

/// <summary>
/// The request that names one folder whose primary external link is read.
/// </summary>
public class FolderPrimaryIdRequestDto<T>
{
    /// <summary>
    /// The folder or room whose primary external link is read. A folder stored in the portal is numbered, while a
    /// folder in a connected third-party account is named by an opaque string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// Accepted for symmetry with the paged link listings; the single primary link answered here does not depend on
    /// it.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// Accepted for symmetry with the paged link listings; the single primary link answered here does not depend on
    /// it.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}