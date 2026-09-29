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

namespace ASC.Files.Core.ApiModels.WebhookDto;

/// <summary>
/// The room carried by a room.* webhook, and by every agent.* webhook.
/// </summary>
/// <remarks>
/// A copy of the room-shaped half of FolderDto. Agent rooms use this payload too: an agent is an AI room and
/// has the same shape today, so agent.* is paired to the same kind. Splitting it later is one attribute change.
/// The room logo and watermark are not carried - both need per-request URL building that a delivery should not
/// pay for; receivers can read them from GET api/2.0/files/rooms/{id}.
/// </remarks>
[WebhookPayload(WebhookPayloadKind.Room)]
public class RoomWebhookDto<T> : FileEntryWebhookDto<T>
{
    /// <inheritdoc/>
    public override FileEntryType FileEntryType => FileEntryType.Folder;

    /// <summary>
    /// The room type.
    /// </summary>
    /// <example>2</example>
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// The underlying folder type of the room.
    /// </summary>
    /// <example>5</example>
    public FolderType? Type { get; set; }

    /// <summary>
    /// The number of files the room directly contains.
    /// </summary>
    /// <example>0</example>
    public int FilesCount { get; set; }

    /// <summary>
    /// The number of folders the room directly contains.
    /// </summary>
    /// <example>0</example>
    public int FoldersCount { get; set; }

    /// <summary>
    /// Specifies whether the room is private or not.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// Specifies whether the room is indexed or not.
    /// </summary>
    /// <example>false</example>
    public bool Indexing { get; set; }

    /// <summary>
    /// Specifies whether downloading from the room is denied or not.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// Specifies whether the room is pinned or not.
    /// </summary>
    /// <example>false</example>
    public bool Pinned { get; set; }

    /// <summary>
    /// The room quota limit in bytes.
    /// </summary>
    /// <example>0</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// The space used by the room in bytes.
    /// </summary>
    /// <example>12345</example>
    public long? UsedSpace { get; set; }

    /// <summary>
    /// The room cover colour.
    /// </summary>
    /// <example>#4781D1</example>
    public string Color { get; set; }

    /// <summary>
    /// The room cover name.
    /// </summary>
    /// <example>cover-1</example>
    public string Cover { get; set; }
}
