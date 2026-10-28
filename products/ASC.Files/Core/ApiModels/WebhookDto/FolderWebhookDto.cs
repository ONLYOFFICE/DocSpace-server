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

// A copy of FolderDto reduced to what a plain folder has. Rooms are a separate payload (RoomWebhookDto) even though
// both come from Folder<T>, because room.* and folder.* are separate triggers whose contracts should be free to move
// independently. Not carried: inRoom, mute and new, all of them per-viewer state.

/// <summary>
/// The folder carried by a folder.* webhook. A room is never sent in this shape: room.* webhooks carry their own
/// payload.
/// </summary>
public class FolderWebhookDto<T> : FileEntryWebhookDto<T>
{
    /// <inheritdoc/>
    public override FileEntryType FileEntryType => FileEntryType.Folder;

    /// <summary>
    /// The number of files the folder directly contains.
    /// </summary>
    /// <example>0</example>
    public int FilesCount { get; set; }

    /// <summary>
    /// The number of subfolders the folder directly contains.
    /// </summary>
    /// <example>0</example>
    public int FoldersCount { get; set; }

    /// <summary>
    /// The kind of folder: an ordinary one, or one the portal creates and maintains itself, such as the folders of a
    /// form-filling room.
    /// </summary>
    /// <example>0</example>
    public FolderType? Type { get; set; }

    /// <summary>
    /// True when the folder can be shared; absent otherwise.
    /// </summary>
    /// <example>true</example>
    public bool? IsShareable { get; set; }
}
