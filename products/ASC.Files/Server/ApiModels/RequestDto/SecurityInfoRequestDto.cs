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
/// The security information request parameters.
/// </summary>
public class SecurityInfoRequestDto
{
    /// <summary>
    /// The list of the shared folder IDs.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The list of the shared file IDs.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FileIds { get; set; } = [];

    /// <summary>
    /// The collection of sharing parameters.
    /// </summary>
    /// <example>[{"access": 1, "shareTo": "00000000-0000-0000-0000-000000000000"}]</example>
    [MaxEmailInvitations]
    public List<FileShareParams> Share { get; set; }

    /// <summary>
    /// Specifies whether to notify users about the shared file or not.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// The message to send when notifying about the shared file.
    /// </summary>
    /// <example>You have been granted access to the file</example>
    [StringLength(255)]
    public string SharingMessage { get; set; }
}

/// <summary>
/// The parameters of the security information request.
/// </summary>
public class SecurityInfoSimpleRequestDto
{
    /// <summary>
    /// The collection of sharing parameters.
    /// </summary>
    /// <example>[{"access": 1, "shareTo": "00000000-0000-0000-0000-000000000000"}]</example>
    [MaxEmailInvitations]
    public List<FileShareParams> Share { get; set; }

    /// <summary>
    /// Specifies whether to notify users about the shared file or not.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// The message to send when notifying about the shared file.
    /// </summary>
    /// <example>You have been granted access to the file</example>
    [StringLength(255)]
    public string SharingMessage { get; set; }
}

/// <summary>
/// The parameters of the security information request for the specified file.
/// </summary>
public class FileSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters of the security information simple request.
    /// </summary>
    [FromBody]
    public required SecurityInfoSimpleRequestDto SecurityInfoSimple { get; set; }
}

/// <summary>
/// The security information request parameters for the specified folder.
/// </summary>
public class FolderSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The parameters of the security information simple request.
    /// </summary>
    [FromBody]
    public required SecurityInfoSimpleRequestDto SecurityInfoSimple { get; set; }
}
