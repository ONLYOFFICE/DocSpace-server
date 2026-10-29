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

// A copy of the room-shaped half of FolderDto. Agent rooms use this payload too: an agent is an AI room and has the
// same shape today, so agent.* sends it as well. Not carried: logo, watermark, lifetime, tags and chatSettings. Each
// needs per-request URL building or an extra query that a delivery should not pay for. Nor pinned, which is the
// reading user's own pin and means nothing to a subscriber.

/// <summary>
/// The room carried by a room.* webhook, and the AI agent carried by every agent.* webhook.
/// </summary>
/// <remarks>
/// Carries no logo, watermark, file lifetime, tags or chat settings; read them from `GET api/2.0/files/rooms/{id}`.
/// </remarks>
public class RoomWebhookDto<T> : FileEntryWebhookDto<T>
{
    /// <inheritdoc/>
    public override FileEntryType FileEntryType => FileEntryType.Folder;

    /// <summary>
    /// The kind of room, which decides what the room is for and which roles it offers.
    /// </summary>
    /// <example>2</example>
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// The same kind of room as `roomType`, in the numbering the portal uses for folders.
    /// </summary>
    /// <example>16</example>
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
    /// True for a private room, whose files are end-to-end encrypted.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// True when the room numbers its files and folders in a fixed order; `order` on each entry is that number.
    /// </summary>
    /// <example>false</example>
    public bool Indexing { get; set; }

    /// <summary>
    /// True when downloading, copying and printing the room's files is restricted for those who may only view them.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The room's own quota in bytes, -1 for an explicit "no limit". Absent when the room has no quota of its own,
    /// so the portal's default room quota applies, if room quotas are on, and absent as well when the subscription's
    /// creator may not edit the room. Unlike `quotaLimit` of the REST API, it is the stored value whether or not room
    /// quotas are turned on and wherever the room lies, the archive included, and it carries no default quota.
    /// </summary>
    /// <example>10737418240</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// The space used by the room in bytes.
    /// </summary>
    /// <example>12345</example>
    public long? UsedSpace { get; set; }

    /// <summary>
    /// The background colour of the room icon, as a hex RGB code.
    /// </summary>
    /// <example>#4781D1</example>
    public string Color { get; set; }

    /// <summary>
    /// The name of the cover picture chosen for the room icon; absent when none is chosen.
    /// </summary>
    /// <example>cover-1</example>
    public string Cover { get; set; }
}
