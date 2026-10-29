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

// A copy of FileEntryDto, deliberately not derived from it, and populated straight from the domain entity rather than
// through FileEntryDtoHelper. Three groups of the REST fields are not carried:
// - caller-relative state: access, security, securityByUsers, availableShareRights, shareSettings, canShare, shared,
//   sharedForUser, sharedExternal, parentShared, isFavorite, requestToken, external, shareRecord. A delivery has no
//   caller; who may receive it is already decided by WebhookFileEntryAccessChecker against the subscription owner, and
//   putting one user's permission matrix on the wire would be both meaningless and a disclosure.
// - anything the REST helper pays for per request: thumbnails, image dimensions (it opens the file stream), form-filling
//   roles, view accessibility, external share links.
// - subtype fields belong on the subtype; the earlier untyped payload erased the File and Folder members through the
//   declared parameter type, so they never reached the wire at all.
// Timestamps are UTC, matching the delivery envelope, rather than the tenant-local ApiDateTime.

/// <summary>
/// What every file, folder and room webhook payload has in common.
/// </summary>
/// <remarks>
/// All dates are in UTC. A delivery goes only to a subscription owner who may see the entry, and it carries no
/// per-user state: no access rights, share settings, favourite flag or thumbnail. Read those from the REST API by the
/// entry ID when they are needed.
/// </remarks>
public abstract class FileEntryWebhookDto<T>
{
    /// <summary>
    /// The ID of the file, folder or room the event is about: a number for an entry in the portal's own storage, a
    /// string for one in a connected third-party storage.
    /// </summary>
    /// <example>10</example>
    public T Id { get; set; }

    /// <summary>
    /// The ID of the folder or room that directly contains the entry.
    /// </summary>
    /// <example>1</example>
    public T ParentId { get; set; }

    /// <summary>
    /// The ID of the top-level section the entry sits in, such as My documents, Rooms, Archive or Trash;
    /// `rootFolderType` says which one.
    /// </summary>
    /// <example>1</example>
    public T RootFolderId { get; set; }

    /// <summary>
    /// The name of the entry as shown in the portal; for a file it includes the extension.
    /// </summary>
    /// <example>Contract 2026.docx</example>
    public string Title { get; set; }

    /// <summary>
    /// Tells a folder or room payload (1) from a file payload (2), so a receiver can pick the schema to read the rest
    /// of the object with.
    /// </summary>
    /// <example>2</example>
    public abstract FileEntryType FileEntryType { get; }

    /// <summary>
    /// When the entry was created, in UTC.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public DateTime Created { get; set; }

    /// <summary>
    /// The user who created the entry; absent when that user has been deleted from the portal.
    /// </summary>
    public UserSummaryWebhookDto CreatedBy { get; set; }

    /// <summary>
    /// When the entry was last changed, in UTC; equal to `created` for an entry that has not changed since.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public DateTime Updated { get; set; }

    /// <summary>
    /// The user who last changed the entry; absent when that user has been deleted from the portal.
    /// </summary>
    public UserSummaryWebhookDto UpdatedBy { get; set; }

    /// <summary>
    /// The kind of top-level section the entry sits in, such as My documents, Rooms, Archive or Trash.
    /// </summary>
    /// <example>14</example>
    public FolderType RootFolderType { get; set; }

    /// <summary>
    /// The type of the room the entry is inside, at any depth; absent for an entry outside rooms.
    /// </summary>
    /// <example>19</example>
    public FolderType? ParentRoomType { get; set; }

    /// <summary>
    /// For an entry in the trash, the ID of the folder it was deleted from; absent anywhere else.
    /// </summary>
    /// <example>12</example>
    public T OriginId { get; set; }

    /// <summary>
    /// For an entry in the trash, the ID of the room it was deleted from; absent anywhere else and for an entry that
    /// was not in a room.
    /// </summary>
    /// <example>22</example>
    public T OriginRoomId { get; set; }

    /// <summary>
    /// For an entry in the trash, the name of the folder it was deleted from.
    /// </summary>
    /// <example>Q3 Reports</example>
    public string OriginTitle { get; set; }

    /// <summary>
    /// For an entry in the trash, the name of the room it was deleted from.
    /// </summary>
    /// <example>Marketing Room</example>
    public string OriginRoomTitle { get; set; }

    /// <summary>
    /// True when the entry lives in a connected third-party storage; absent for the portal's own storage.
    /// </summary>
    /// <example>true</example>
    public bool? ProviderItem { get; set; }

    /// <summary>
    /// The connected third-party storage the entry lives in; absent for the portal's own storage.
    /// </summary>
    /// <example>GoogleDrive</example>
    public string ProviderKey { get; set; }

    /// <summary>
    /// The ID of the third-party storage connection the entry comes through; absent for the portal's own storage.
    /// </summary>
    /// <example>1</example>
    public int? ProviderId { get; set; }

    /// <summary>
    /// The position of the entry in a room with indexing turned on; meaningful only inside such a room.
    /// </summary>
    /// <example>1</example>
    public int Order { get; set; }

    internal FileEntryWebhookDto<T> Clone()
    {
        return (FileEntryWebhookDto<T>)MemberwiseClone();
    }
}

/// <summary>
/// The flat part of a file.*, folder.*, room.*, agent.* or form.* webhook payload. The links, the author and last
/// editor, the type of the parent room and, for a third-party entry, the section it sits in need services and are
/// filled in by <see cref="FileEntryWebhookDtoHelper"/>: a plain DAO read of the entry, which every payload is built
/// from, leaves the parent room type unset (only listings fill it) and has the storage root as the root.
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
    [MapProperty(nameof(File<int>.IsPdf), nameof(FileWebhookDto<int>.IsForm), Use = nameof(NullIfFalse))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.ParentRoomType))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.UpdatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.ViewUrl))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.WebUrl))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.Locked))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.LockedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CustomFilterEnabled))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CustomFilterEnabledBy))]
    private static partial FileWebhookDto<int> MapFileInt(File<int> source);

    [MapProperty(nameof(File<int>.RootId), nameof(FileWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(File<int>.CreateOn), nameof(FileWebhookDto<int>.Created))]
    [MapProperty(nameof(File<int>.ProviderEntry), nameof(FileWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.ProviderId), nameof(FileWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FileWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(File<int>.Title), nameof(FileWebhookDto<int>.FileExst), Use = nameof(GetFileExst))]
    [MapProperty(nameof(File<int>.Title), nameof(FileWebhookDto<int>.FileType), Use = nameof(GetFileType))]
    [MapProperty(nameof(File<int>.Encrypted), nameof(FileWebhookDto<int>.Encrypted), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(File<int>.IsPdf), nameof(FileWebhookDto<int>.IsForm), Use = nameof(NullIfFalse))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.ParentRoomType))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.UpdatedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.ViewUrl))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.WebUrl))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.Locked))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.LockedBy))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CustomFilterEnabled))]
    [MapperIgnoreTarget(nameof(FileWebhookDto<int>.CustomFilterEnabledBy))]
    private static partial FileWebhookDto<string> MapFileString(File<string> source);

    [MapProperty(nameof(Folder<int>.RootId), nameof(FolderWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(Folder<int>.CreateOn), nameof(FolderWebhookDto<int>.Created))]
    [MapProperty(nameof(Folder<int>.ProviderEntry), nameof(FolderWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(Folder<int>.ProviderId), nameof(FolderWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FolderWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(FolderWebhookDto<int>.Type))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.ParentRoomType))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.UpdatedBy))]
    private static partial FolderWebhookDto<int> MapFolderInt(Folder<int> source);

    [MapProperty(nameof(Folder<int>.RootId), nameof(FolderWebhookDto<int>.RootFolderId))]
    [MapProperty(nameof(Folder<int>.CreateOn), nameof(FolderWebhookDto<int>.Created))]
    [MapProperty(nameof(Folder<int>.ProviderEntry), nameof(FolderWebhookDto<int>.ProviderItem), Use = nameof(NullIfFalse))]
    [MapProperty(nameof(Folder<int>.ProviderId), nameof(FolderWebhookDto<int>.ProviderId), Use = nameof(NullIfZero))]
    [MapPropertyFromSource(nameof(FolderWebhookDto<int>.Updated), Use = nameof(GetUpdated))]
    [MapProperty(nameof(Folder<int>.FolderType), nameof(FolderWebhookDto<int>.Type))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.CreatedBy))]
    [MapperIgnoreTarget(nameof(FolderWebhookDto<int>.ParentRoomType))]
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
    [MapperIgnoreTarget(nameof(RoomWebhookDto<int>.ParentRoomType))]
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
    [MapperIgnoreTarget(nameof(RoomWebhookDto<int>.ParentRoomType))]
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
