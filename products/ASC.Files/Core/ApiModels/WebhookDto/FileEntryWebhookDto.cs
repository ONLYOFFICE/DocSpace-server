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
/// What every file, folder and room webhook payload has in common.
/// </summary>
/// <remarks>
/// A copy of FileEntryDto, deliberately not derived from it, and populated straight from the domain entity
/// rather than through FileEntryDtoHelper. Three groups of the REST fields are not carried:
/// <list type="bullet">
/// <item>caller-relative state - access, security, availableShareRights, shareSettings, canShare, shared,
/// sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external. A delivery has no caller;
/// who may receive it is already decided by WebhookFileEntryAccessChecker against the subscription owner, and
/// putting one user's permission matrix on the wire would be both meaningless and a disclosure.</item>
/// <item>anything the REST helper pays for per request - thumbnails, image dimensions (it opens the file
/// stream), form-filling roles, view accessibility, external share links.</item>
/// <item>subtype fields belong on the subtype, unlike the previous payload where File and Folder members were
/// erased by the declared parameter type and never reached the wire at all.</item>
/// </list>
/// Timestamps are UTC, matching the delivery envelope, rather than the tenant-local ApiDateTime.
/// </remarks>
public abstract class FileEntryWebhookDto<T>
{
    /// <summary>
    /// The file entry ID.
    /// </summary>
    /// <example>10</example>
    public T Id { get; set; }

    /// <summary>
    /// The parent folder ID of the file entry.
    /// </summary>
    /// <example>1</example>
    public T ParentId { get; set; }

    /// <summary>
    /// The root folder ID of the file entry.
    /// </summary>
    /// <example>1</example>
    public T RootFolderId { get; set; }

    /// <summary>
    /// The file entry title.
    /// </summary>
    /// <example>Some title.txt</example>
    public string Title { get; set; }

    /// <summary>
    /// The file entry type: 1 for a folder, 2 for a file.
    /// </summary>
    /// <example>2</example>
    public abstract FileEntryType FileEntryType { get; }

    /// <summary>
    /// The UTC date when the file entry was created.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public DateTime Created { get; set; }

    /// <summary>
    /// The file entry author.
    /// </summary>
    public UserSummaryWebhookDto CreatedBy { get; set; }

    /// <summary>
    /// The UTC date when the file entry was last updated.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public DateTime Updated { get; set; }

    /// <summary>
    /// The user who last updated the file entry.
    /// </summary>
    public UserSummaryWebhookDto UpdatedBy { get; set; }

    /// <summary>
    /// The root folder type of the file entry.
    /// </summary>
    /// <example>0</example>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The parent room type of the file entry.
    /// </summary>
    /// <example>0</example>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// The origin ID of the file entry, set when it sits in the trash.
    /// </summary>
    /// <example>12</example>
    public T OriginId { get; set; }

    /// <summary>
    /// The origin room ID of the file entry, set when it sits in the trash.
    /// </summary>
    /// <example>22</example>
    public T OriginRoomId { get; set; }

    /// <summary>
    /// The origin title of the file entry.
    /// </summary>
    /// <example>Original Title</example>
    public string OriginTitle { get; set; }

    /// <summary>
    /// The origin room title of the file entry.
    /// </summary>
    /// <example>Original Room</example>
    public string OriginRoomTitle { get; set; }

    /// <summary>
    /// Specifies whether the file entry is backed by a third-party provider or not.
    /// </summary>
    /// <example>false</example>
    public bool? ProviderItem { get; set; }

    /// <summary>
    /// The third-party provider key of the file entry.
    /// </summary>
    /// <example>google-drive</example>
    public string ProviderKey { get; set; }

    /// <summary>
    /// The third-party provider ID of the file entry.
    /// </summary>
    /// <example>1</example>
    public int? ProviderId { get; set; }

    /// <summary>
    /// The position of the file entry within an indexed room.
    /// </summary>
    /// <example>1</example>
    public int Order { get; set; }
}
