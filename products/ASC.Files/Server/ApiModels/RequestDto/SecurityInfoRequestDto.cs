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
/// The entries whose sharing rights are being changed, and the rights to apply to them.
/// </summary>
public class SecurityInfoRequestDto
{
    /// <summary>
    /// The folders and rooms whose rights are being changed, identified as a listing operation returns them - a
    /// number on the portal, a string on a connected third-party account.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The files whose rights are being changed, identified as a listing operation returns them - a number on the
    /// portal, a string on a connected third-party account.
    /// </summary>
    /// <example>[7, 8]</example>
    public List<JsonElement> FileIds { get; set; } = [];

    /// <summary>
    /// One record per account or group whose rights are being set, each naming the subject and the level it gets on
    /// all of the listed entries; a level of `None` takes the access away. An empty collection makes the call change
    /// nothing.
    /// </summary>
    /// <example>[{"access": 2, "shareTo": "9924256a-739c-462b-af15-e652a3b1b6eb"}]</example>
    [MaxEmailInvitations]
    public List<FileShareParams> Share { get; set; }

    /// <summary>
    /// Set to true to have every account named in `share` emailed about the access it just received; false changes
    /// the rights without telling anyone.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// The text put into that email, ignored while `notify` is false. Markup is stripped before sending, so only the
    /// plain text of the value survives.
    /// </summary>
    /// <example>You have been granted access to the file</example>
    [StringLength(255)]
    public string SharingMessage { get; set; }
}

/// <summary>
/// The rights to apply to a single file or folder, and how to announce them.
/// </summary>
public class SecurityInfoSimpleRequestDto
{
    /// <summary>
    /// One record per account or group whose rights are being set, each naming the subject and the level it gets; a
    /// level of `None` takes the access away. An empty collection makes the call change nothing.
    /// </summary>
    /// <example>[{"access": 2, "shareTo": "9924256a-739c-462b-af15-e652a3b1b6eb"}]</example>
    [MaxEmailInvitations]
    public List<FileShareParams> Share { get; set; }

    /// <summary>
    /// Set to true to have every account named in `share` emailed about the access it just received; false changes
    /// the rights without telling anyone.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// The text put into that email, ignored while `notify` is false. Markup is stripped before sending, so only the
    /// plain text of the value survives.
    /// </summary>
    /// <example>You have been granted access to the file</example>
    [StringLength(255)]
    public string SharingMessage { get; set; }
}

/// <summary>
/// The request that names the file whose sharing is being changed, and the change.
/// </summary>
public class FileSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// The file whose sharing is being changed. A file stored on the portal is numbered, while a file in a connected
    /// third-party account is named by an opaque string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The rights to apply to the file, and whether to announce them by mail.
    /// </summary>
    [FromBody]
    public required SecurityInfoSimpleRequestDto SecurityInfoSimple { get; set; }
}

/// <summary>
/// The request that names the folder whose sharing is being changed, and the change.
/// </summary>
public class FolderSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// The folder whose sharing is being changed. A folder stored on the portal is numbered, while a folder in a
    /// connected third-party account is named by an opaque string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The rights to apply to the folder, and whether to announce them by mail.
    /// </summary>
    [FromBody]
    public required SecurityInfoSimpleRequestDto SecurityInfoSimple { get; set; }
}
