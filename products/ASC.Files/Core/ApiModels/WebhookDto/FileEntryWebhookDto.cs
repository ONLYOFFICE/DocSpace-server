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
/// <item>caller-relative state - access, security, securityByUsers, availableShareRights, shareSettings, canShare,
/// shared, sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. A delivery
/// has no caller;
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
    /// The file entry type: 1 for a folder, 2 for a file. Present on every entry payload.
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

    internal FileEntryWebhookDto<T> Clone()
    {
        return (FileEntryWebhookDto<T>)MemberwiseClone();
    }
}

/// <summary>
/// The flat part of a file.*, folder.*, room.*, agent.* or form.* webhook payload. The links and the author and
/// last editor need services and are filled in by <see cref="FileEntryWebhookDtoHelper"/>.
/// </summary>
/// <remarks>
/// Mapperly cannot generate an open generic mapping, and IncludeMappingConfiguration does not carry over from
/// File&lt;int&gt; to File&lt;string&gt;, so each payload is declared for both entry ID types with the same
/// configuration, and the public generic method dispatches to them. Keep the two lists of a pair identical.
/// The converters are not defaults: Mapperly would otherwise run every string through GetFileExst and every
/// long through GetQuotaLimit.
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class FileEntryWebhookDtoMapper
{
    public static FileWebhookDto<T> MapToFileWebhookDto<T>(this File<T> source)
    {
        return source switch
        {
            File<int> file => (FileWebhookDto<T>)(object)MapFileInt(file),
            File<string> file => (FileWebhookDto<T>)(object)MapFileString(file),
            _ => null
        };
    }

    public static FolderWebhookDto<T> MapToFolderWebhookDto<T>(this Folder<T> source)
    {
        return source switch
        {
            Folder<int> folder => (FolderWebhookDto<T>)(object)MapFolderInt(folder),
            Folder<string> folder => (FolderWebhookDto<T>)(object)MapFolderString(folder),
            _ => null
        };
    }

    public static RoomWebhookDto<T> MapToRoomWebhookDto<T>(this Folder<T> source)
    {
        return source switch
        {
            Folder<int> room => (RoomWebhookDto<T>)(object)MapRoomInt(room),
            Folder<string> room => (RoomWebhookDto<T>)(object)MapRoomString(room),
            _ => null
        };
    }

    [MapProperty(nameof(File<int>.RootId), nameof(FileWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(File<int>.CreateOn), nameof(FileWebhookDto<int>.Created))]
    [MapProperty(nameof(File<int>.ProviderEntry), nameof(FileWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.ProviderId), nameof(FileWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FileWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(File<int>.Title), nameof(FileWebhookDto<int>.FileExst), Use = nameof(GetFileExst))]
    [MapProperty(nameof(File<int>.Title), nameof(FileWebhookDto<int>.FileType), Use = nameof(GetFileType))]
    [MapProperty(nameof(File<int>.Encrypted), nameof(FileWebhookDto<int>.Encrypted), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.Locked), nameof(FileWebhookDto<int>.Locked), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.IsPdf), nameof(FileWebhookDto<int>.IsForm), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.CustomFilterEnabled), nameof(FileWebhookDto<int>.CustomFilterEnabled), Use = nameof(NullIfFalse))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.UpdatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.ViewUrl))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.WebUrl))]
    private static partial FileWebhookDto<int> MapFileInt(File<int> source);

    [MapProperty(nameof(File<int>.RootId), nameof(FileWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(File<int>.CreateOn), nameof(FileWebhookDto<int>.Created))]
    [MapProperty(nameof(File<int>.ProviderEntry), nameof(FileWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.ProviderId), nameof(FileWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FileWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(File<int>.Title), nameof(FileWebhookDto<int>.FileExst), Use = nameof(GetFileExst))]
    [MapProperty(nameof(File<int>.Title), nameof(FileWebhookDto<int>.FileType), Use = nameof(GetFileType))]
    [MapProperty(nameof(File<int>.Encrypted), nameof(FileWebhookDto<int>.Encrypted), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.Locked), nameof(FileWebhookDto<int>.Locked), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.IsPdf), nameof(FileWebhookDto<int>.IsForm), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.CustomFilterEnabled), nameof(FileWebhookDto<int>.CustomFilterEnabled), Use = nameof(NullIfFalse))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.UpdatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.ViewUrl))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.WebUrl))]
    private static partial FileWebhookDto<string> MapFileString(File<string> source);

    [MapProperty(nameof(Folder<int>.RootId), nameof(FolderWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(Folder<int>.CreateOn), nameof(FolderWebhookDto<int>.Created))]
    [MapProperty(nameof(Folder<int>.ProviderEntry), nameof(FolderWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(Folder<int>.ProviderId), nameof(FolderWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FolderWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(FolderWebhookDto<int>.Type))]
    [MapProperty(nameof(Folder<int>.Shareable), nameof(FolderWebhookDto<int>.IsShareable), Use = nameof(NullIfFalse))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.UpdatedBy))]
    private static partial FolderWebhookDto<int> MapFolderInt(Folder<int> source);

    [MapProperty(nameof(Folder<int>.RootId), nameof(FolderWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(Folder<int>.CreateOn), nameof(FolderWebhookDto<int>.Created))]
    [MapProperty(nameof(Folder<int>.ProviderEntry), nameof(FolderWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(Folder<int>.ProviderId), nameof(FolderWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FolderWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(FolderWebhookDto<int>.Type))]
    [MapProperty(nameof(Folder<int>.Shareable), nameof(FolderWebhookDto<int>.IsShareable), Use = nameof(NullIfFalse))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.UpdatedBy))]
    private static partial FolderWebhookDto<string> MapFolderString(Folder<string> source);

    [MapProperty(nameof(Folder<int>.RootId), nameof(RoomWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(Folder<int>.CreateOn), nameof(RoomWebhookDto<int>.Created))]
    [MapProperty(nameof(Folder<int>.ProviderEntry), nameof(RoomWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(Folder<int>.ProviderId), nameof(RoomWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(RoomWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(RoomWebhookDto<int>.Type))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(RoomWebhookDto<int>.RoomType), Use = nameof(GetRoomType))]
    [MapProperty(nameof(Folder<int>.SettingsPrivate), nameof(RoomWebhookDto<int>.Private))]
    [MapProperty(nameof(Folder<int>.SettingsIndexing), nameof(RoomWebhookDto<int>.Indexing))]
    [MapProperty(nameof(Folder<int>.SettingsDenyDownload), nameof(RoomWebhookDto<int>.DenyDownload))]
    [MapProperty(nameof(Folder<int>.SettingsColor), nameof(RoomWebhookDto<int>.Color))]
    [MapProperty(nameof(Folder<int>.SettingsCover), nameof(RoomWebhookDto<int>.Cover))]
    [MapProperty(nameof(Folder<int>.Counter), nameof(RoomWebhookDto<int>.UsedSpace))]
    [MapProperty(nameof(Folder<int>.SettingsQuota), nameof(RoomWebhookDto<int>.QuotaLimit), Use = nameof(GetQuotaLimit))]
    [MapperIgnoreTarget(nameof(RoomWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(RoomWebhookDto<int>.UpdatedBy))]
    private static partial RoomWebhookDto<int> MapRoomInt(Folder<int> source);

    [MapProperty(nameof(Folder<int>.RootId), nameof(RoomWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(Folder<int>.CreateOn), nameof(RoomWebhookDto<int>.Created))]
    [MapProperty(nameof(Folder<int>.ProviderEntry), nameof(RoomWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(Folder<int>.ProviderId), nameof(RoomWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(RoomWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(RoomWebhookDto<int>.Type))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(RoomWebhookDto<int>.RoomType), Use = nameof(GetRoomType))]
    [MapProperty(nameof(Folder<int>.SettingsPrivate), nameof(RoomWebhookDto<int>.Private))]
    [MapProperty(nameof(Folder<int>.SettingsIndexing), nameof(RoomWebhookDto<int>.Indexing))]
    [MapProperty(nameof(Folder<int>.SettingsDenyDownload), nameof(RoomWebhookDto<int>.DenyDownload))]
    [MapProperty(nameof(Folder<int>.SettingsColor), nameof(RoomWebhookDto<int>.Color))]
    [MapProperty(nameof(Folder<int>.SettingsCover), nameof(RoomWebhookDto<int>.Cover))]
    [MapProperty(nameof(Folder<int>.Counter), nameof(RoomWebhookDto<int>.UsedSpace))]
    [MapProperty(nameof(Folder<int>.SettingsQuota), nameof(RoomWebhookDto<int>.QuotaLimit), Use = nameof(GetQuotaLimit))]
    [MapperIgnoreTarget(nameof(RoomWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(RoomWebhookDto<int>.UpdatedBy))]
    private static partial RoomWebhookDto<string> MapRoomString(Folder<string> source);

    private static DateTime GetUpdated(FileEntry source) => source.ModifiedOn < source.CreateOn ? source.CreateOn : source.ModifiedOn;

    [UserMapping(Default = false)]
    private static bool? NullIfFalse(bool value) => value.NullIfDefault();

    [UserMapping(Default = false)]
    private static int? NullIfZero(int value) => value.NullIfDefault();

    [UserMapping(Default = false)]
    private static string GetFileExst(string title) => FileUtility.GetFileExtension(title);

    [UserMapping(Default = false)]
    private static FileType GetFileType(string title) => FileUtility.GetFileTypeByExtention(FileUtility.GetFileExtension(title));

    [UserMapping(Default = false)]
    private static RoomType? GetRoomType(FolderType folderType) => DocSpaceHelper.MapToRoomType(folderType);

    /// <summary>
    /// A room quota below -1 means the room has no quota of its own.
    /// </summary>
    [UserMapping(Default = false)]
    private static long? GetQuotaLimit(long quota) => quota > -2 ? quota : null;
}
