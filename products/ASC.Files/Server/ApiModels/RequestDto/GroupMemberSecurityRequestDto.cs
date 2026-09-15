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
/// The request that names the folder and the group whose members are being listed.
/// </summary>
public class GroupMemberSecurityFolderRequestDto<T>
{
    /// <summary>
    /// The folder or room whose access is being read. A folder stored on the portal is numbered, while a folder in a
    /// connected third-party account is named by an opaque string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The group whose members are listed. Take it from the entries of `GET api/2.0/files/folder/{id}/share` that
    /// stand for a group; a group that holds no rights on this folder is answered with an empty list.
    /// </summary>
    /// <example>9924256a-739c-462b-af15-e652a3b1b6eb</example>
    [FromRoute(Name = "groupId")]
    public required Guid GroupId { get; set; }

    /// <summary>
    /// How many members at most to answer with.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many members to skip before answering, used together with `count` to page through a large group.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Keeps only the members whose first name, last name or email contains this value. The value is matched in lower
    /// case, so an uppercase one finds nothing.
    /// </summary>
    /// <example>john</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
/// <summary>
/// The request that names the file and the group whose members are being listed.
/// </summary>
public class GroupMemberSecurityFileRequestDto<T>
{
    /// <summary>
    /// The file whose access is being read. A file stored on the portal is numbered, while a file in a connected
    /// third-party account is named by an opaque string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The group whose members are listed. Take it from the entries of `GET api/2.0/files/file/{id}/share` that stand
    /// for a group; a group that holds no rights on this file is answered with an empty list.
    /// </summary>
    /// <example>9924256a-739c-462b-af15-e652a3b1b6eb</example>
    [FromRoute(Name = "groupId")]
    public required Guid GroupId { get; set; }

    /// <summary>
    /// How many members at most to answer with.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many members to skip before answering, used together with `count` to page through a large group.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Keeps only the members whose first name, last name or email contains this value. The value is matched in lower
    /// case, so an uppercase one finds nothing.
    /// </summary>
    /// <example>john</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}