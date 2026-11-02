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
/// Builds the payload of a file.*, folder.*, room.*, agent.* or form.* webhook.
/// </summary>
/// <remarks>
/// Deliberately not FileDtoHelper or FolderDtoHelper. Those answer "what may the current user see about this
/// entry": they run the security matrix, resolve external share links, read notification and badge settings,
/// and - for an image - open the file stream to measure it. A delivery has no current user in that sense and
/// must not pay for any of it, so this maps straight from the domain entity and asks only for the two link
/// utilities it cannot compute itself.
/// The helper is scoped, so it lives as long as one request or one queued file operation, and it keeps per-operation
/// memos, not caches - nothing outlives the scope: the origin folders of trashed entries and whether a subscription
/// owner may read each of them, which emptying the trash would otherwise read again for every file, and the lock and
/// custom-filter tags a bulk operation reads for a whole batch at once (<see cref="PrefetchTagsAsync{T}"/>).
/// </remarks>
[Scope]
public class FileEntryWebhookDtoHelper(
    UserWebhookDtoHelper userWebhookDtoHelper,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    FileSecurity fileSecurity,
    GlobalFolderHelper globalFolderHelper,
    IDaoFactory daoFactory)
{
    private readonly Dictionary<(Type Type, object FolderId), object> _originFolders = new();
    private readonly Dictionary<(Type Type, object FolderId, Guid SubscriberId), bool> _canReadOriginFolder = new();
    private readonly Dictionary<(Type Type, string FileId), List<Tag>> _prefetchedTags = new();

    /// <summary>
    /// Maps any entry to the payload <paramref name="trigger"/> expects: a file, a room or a plain folder. The payload
    /// follows the trigger family, not only the kind of entry: a room under a folder.* trigger (folder.downloaded of a
    /// room) is sent as a folder, since that is the schema a receiver of folder.* reads every payload with.
    /// </summary>
    /// <remarks>
    /// Declared as the base type: the envelope writes its payload by the runtime type (<c>WebhookPayload.Payload</c>),
    /// so the file, folder and room fields reach the wire all the same.
    /// </remarks>
    public async Task<FileEntryWebhookDto<T>> GetAsync<T>(FileEntry<T> entry, WebhookTrigger trigger)
    {
        return entry switch
        {
            File<T> file => await GetFileAsync(file),
            Folder<T> { IsRoom: true } room when !IsFolderTrigger(trigger) => await GetRoomAsync(room),
            Folder<T> folder => await GetFolderAsync(folder),
            _ => null
        };
    }

    private static bool IsFolderTrigger(WebhookTrigger trigger)
    {
        return trigger.GetTargetType() == WebhookTrigger.FolderCreated.GetTargetType();
    }

    /// <summary>
    /// Cuts a payload built by <see cref="GetAsync{T}"/> from <paramref name="source"/> for one subscription owner, as
    /// the REST API shows the entry to them: a User or a Guest gets the author and the last editor without the profile
    /// link and the user name, the quota of a room goes only to an owner the REST API shows it to
    /// (<see cref="CanSeeRoomQuotaAsync{T}"/>), and the origins of an entry in the trash only to an owner who may read
    /// the folder or the room it was deleted from.
    /// </summary>
    public async Task<FileEntryWebhookDto<T>> RestrictForSubscriberAsync<T>(FileEntryWebhookDto<T> entry, FileEntry<T> source, Guid? subscriberId)
    {
        if (entry == null || !subscriberId.HasValue)
        {
            return entry;
        }

        var result = await userWebhookDtoHelper.IsWithoutContactsAccessAsync(subscriberId)
            ? RestrictForSubscriber(entry, subscriberId.Value)
            : entry;

        if (result is RoomWebhookDto<T> { QuotaLimit: not null } room && !await CanSeeRoomQuotaAsync(source, subscriberId.Value))
        {
            var withoutQuota = (RoomWebhookDto<T>)(ReferenceEquals(room, entry) ? room.Clone() : room);

            withoutQuota.QuotaLimit = null;
            result = withoutQuota;
        }

        result = await RestrictOriginsForSubscriberAsync(result, entry, subscriberId.Value);

        return result;
    }

    /// <summary>
    /// The predicate FolderDtoHelper shows the quota of a room by: the reader may edit the room or create in it, holds
    /// the Editor role in it, or - for a room in the archive or the trash, where nobody may edit it or create in it -
    /// may delete it. The REST API then hides the quota of a room in the archive altogether; the payload keeps it there
    /// (see <see cref="RoomWebhookDto{T}.QuotaLimit"/>), and the last clause is what shows it to the managers of an
    /// archived room in room.archived and room.deleted.
    /// </summary>
    private async Task<bool> CanSeeRoomQuotaAsync<T>(FileEntry<T> room, Guid subscriberId)
    {
        // each check stores the checked user's share in the entry, which the payloads of all the subscribers of the
        // event are cut from: it is put back as it was
        var access = room.Access;
        var shareRecord = room.ShareRecord;

        try
        {
            if (await fileSecurity.CanEditRoomAsync(room, subscriberId) || await fileSecurity.CanCreateAsync(room, subscriberId))
            {
                return true;
            }

            if (room.RootFolderType is FolderType.Archive or FolderType.TRASH && await fileSecurity.CanDeleteAsync(room, subscriberId))
            {
                return true;
            }

            // the reader's own share, the folder.Access of the REST predicate: the read check reaches the share in
            // every section, the archive included, and a check that ends before it leaves None behind
            room.Access = FileShare.None;

            return await fileSecurity.CanReadAsync(room, subscriberId) && room.Access == FileShare.Editing;
        }
        finally
        {
            room.Access = access;
            room.ShareRecord = shareRecord;
        }
    }

    // The trash listing shows the origins to the owner of the trash only. A subscriber who may read the deleted entry
    // itself (a file shared with them out of someone's private folder) must not learn the name and the ID of a folder
    // or a room they cannot read.
    private async Task<FileEntryWebhookDto<T>> RestrictOriginsForSubscriberAsync<T>(FileEntryWebhookDto<T> result, FileEntryWebhookDto<T> entry, Guid subscriberId)
    {
        var hasFolder = !EqualityComparer<T>.Default.Equals(result.OriginId, default);
        var hasRoom = !EqualityComparer<T>.Default.Equals(result.OriginRoomId, default);

        if (!hasFolder && !hasRoom)
        {
            return result;
        }

        var hideFolder = hasFolder && !await CanReadOriginFolderAsync(result.OriginId, subscriberId);
        var hideRoom = hasRoom && !await CanReadOriginFolderAsync(result.OriginRoomId, subscriberId);

        if (!hideFolder && !hideRoom)
        {
            return result;
        }

        var restricted = ReferenceEquals(result, entry) ? result.Clone() : result;

        if (hideFolder)
        {
            restricted.OriginId = default;
            restricted.OriginTitle = null;
        }

        if (hideRoom)
        {
            restricted.OriginRoomId = default;
            restricted.OriginRoomTitle = null;
        }

        return restricted;
    }

    // Emptying the trash builds one payload per file, and the files of one trash share a handful of origins: each
    // origin is read once, and checked once per subscription owner, for the whole operation.
    private async Task<bool> CanReadOriginFolderAsync<T>(T folderId, Guid subscriberId)
    {
        var key = (typeof(T), (object)folderId, subscriberId);

        if (_canReadOriginFolder.TryGetValue(key, out var canRead))
        {
            return canRead;
        }

        var folder = await GetOriginFolderAsync(folderId);

        canRead = folder != null && await fileSecurity.CanReadAsync(folder, subscriberId);
        _canReadOriginFolder[key] = canRead;

        return canRead;
    }

    private async Task<Folder<T>> GetOriginFolderAsync<T>(T folderId)
    {
        var key = (typeof(T), (object)folderId);

        if (_originFolders.TryGetValue(key, out var folder))
        {
            return (Folder<T>)folder;
        }

        var result = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId);
        _originFolders[key] = result;

        return result;
    }

    /// <summary>
    /// The form.submit counterpart of <see cref="RestrictForSubscriberAsync{T}(FileEntryWebhookDto{T}, FileEntry{T}, Guid?)"/>.
    /// </summary>
    public async Task<FormSubmitWebhookDto<T>> RestrictForSubscriberAsync<T>(FormSubmitWebhookDto<T> payload, Guid? subscriberId)
    {
        if (!await userWebhookDtoHelper.IsWithoutContactsAccessAsync(subscriberId))
        {
            return payload;
        }

        return new FormSubmitWebhookDto<T>
        {
            OriginalForm = (FileWebhookDto<T>)RestrictForSubscriber(payload.OriginalForm, subscriberId.Value),
            SubmittedForm = (FileWebhookDto<T>)RestrictForSubscriber(payload.SubmittedForm, subscriberId.Value)
        };
    }

    private static FileEntryWebhookDto<T> RestrictForSubscriber<T>(FileEntryWebhookDto<T> entry, Guid subscriberId)
    {
        if (entry == null)
        {
            return null;
        }

        var result = entry.Clone();

        result.CreatedBy = UserWebhookDtoHelper.RestrictSummaryForSubscriber(entry.CreatedBy, subscriberId);
        result.UpdatedBy = UserWebhookDtoHelper.RestrictSummaryForSubscriber(entry.UpdatedBy, subscriberId);

        if (result is FileWebhookDto<T> file)
        {
            file.LockedBy = UserWebhookDtoHelper.RestrictSummaryForSubscriber(file.LockedBy, subscriberId);
            file.CustomFilterEnabledBy = UserWebhookDtoHelper.RestrictSummaryForSubscriber(file.CustomFilterEnabledBy, subscriberId);
        }

        return result;
    }

    public async Task<FileWebhookDto<T>> GetFileAsync<T>(File<T> file)
    {
        var result = file.MapToFileWebhookDto();

        result.ViewUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileDownloadUrl(file.Id));
        result.WebUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id, file.Version));

        await FillCommonAsync(result, file);
        await FillTagStateAsync(result, file);

        return result;
    }

    /// <summary>
    /// Reads the lock and custom-filter tags of a batch of files in one query, for a bulk operation about to build a
    /// payload for each of them: <see cref="GetFileAsync{T}"/> then takes each file's tags from here, once, instead of
    /// running a query per file.
    /// </summary>
    public async Task PrefetchTagsAsync<T>(IEnumerable<File<T>> files)
    {
        var batch = files.ToList();

        if (batch.Count == 0)
        {
            return;
        }

        var prefetched = batch
            .Select(file => (typeof(T), file.Id.ToString()))
            .Distinct()
            .ToDictionary(key => key, _ => new List<Tag>());

        await foreach (var tag in daoFactory.GetTagDao<T>().GetTagsAsync([TagType.Locked, TagType.CustomFilter], batch))
        {
            if (tag.EntryType == FileEntryType.File && tag.EntryId != null &&
                prefetched.TryGetValue((typeof(T), tag.EntryId.ToString()), out var tags))
            {
                tags.Add(tag);
            }
        }

        // stored only once the query has succeeded, so a failed prefetch leaves every file to its own query
        foreach (var (key, tags) in prefetched)
        {
            _prefetchedTags[key] = tags;
        }
    }

    /// <summary>
    /// The lock and the custom filter live in tags, which a plain DAO read of the file does not fill: read them here,
    /// with the user who set each, independent of who caused the event. One query per file payload, unless a bulk
    /// operation has read them for its batch (<see cref="PrefetchTagsAsync{T}"/>).
    /// </summary>
    private async Task FillTagStateAsync<T>(FileWebhookDto<T> result, File<T> file)
    {
        // a prefetched list is taken once: a later event about the same file in the same operation reads them again
        var tags = _prefetchedTags.Remove((typeof(T), file.Id.ToString()), out var prefetched)
            ? prefetched.ToAsyncEnumerable()
            : daoFactory.GetTagDao<T>().GetTagsAsync([TagType.Locked, TagType.CustomFilter], [file]);

        await foreach (var tag in tags)
        {
            // the same reading as EntryManager.SetFileStatusAsync gives the REST API: a lock tag without an owner is
            // an unlocked file, and the custom filter counts only for a file the editor can filter
            switch (tag.Type)
            {
                case TagType.Locked when tag.Owner != Guid.Empty:
                    result.Locked = true;
                    result.LockedBy = await userWebhookDtoHelper.GetSummaryAsync(tag.Owner);
                    break;
                case TagType.CustomFilter when fileUtility.CanWebCustomFilterEditing(file.Title):
                    result.CustomFilterEnabled = true;
                    result.CustomFilterEnabledBy = tag.Owner == Guid.Empty ? null : await userWebhookDtoHelper.GetSummaryAsync(tag.Owner);
                    break;
            }
        }
    }

    public async Task<FolderWebhookDto<T>> GetFolderAsync<T>(Folder<T> folder)
    {
        var result = folder.MapToFolderWebhookDto();

        await FillCommonAsync(result, folder);

        return result;
    }

    public async Task<RoomWebhookDto<T>> GetRoomAsync<T>(Folder<T> room)
    {
        var result = room.MapToRoomWebhookDto();

        await FillCommonAsync(result, room);

        return result;
    }

    /// <summary>
    /// The common fields a plain DAO read of the entry does not fill the way the REST API shows them.
    /// </summary>
    private async Task FillCommonAsync<T>(FileEntryWebhookDto<T> result, FileEntry<T> entry)
    {
        await FillAuthorsAsync(result, entry);
        await FillParentRoomTypeAsync(result, entry);
        await FillProviderSectionAsync(result, entry);
    }

    private async Task FillAuthorsAsync<T>(FileEntryWebhookDto<T> result, FileEntry<T> entry)
    {
        result.CreatedBy = await userWebhookDtoHelper.GetSummaryAsync(entry.CreateBy);
        result.UpdatedBy = entry.ModifiedBy == entry.CreateBy
            ? result.CreatedBy
            : await userWebhookDtoHelper.GetSummaryAsync(entry.ModifiedBy);
    }

    /// <summary>
    /// The type of the room an entry is inside. Only a listing fills <c>ParentRoomType</c> of the entity, so it is found
    /// the way FileEntryDtoHelper finds it for a single entry: from the cached chain of parent folders, the one the
    /// access check of the delivery has just read. Never for a room itself, and only in the sections that hold rooms.
    /// </summary>
    private async Task FillParentRoomTypeAsync<T>(FileEntryWebhookDto<T> result, FileEntry<T> entry)
    {
        if (entry is Folder<T> { IsRoom: true })
        {
            return;
        }

        if (entry.ParentRoomType != null)
        {
            result.ParentRoomType = entry.ParentRoomType;

            return;
        }

        if (entry.RootFolderType is not (FolderType.VirtualRooms or FolderType.Archive or FolderType.RoomTemplates or FolderType.AiAgents))
        {
            return;
        }

        var room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(entry.ParentId).FirstOrDefaultAsync(r => r.IsRoom);

        result.ParentRoomType = room?.FolderType;
    }

    /// <summary>
    /// An entry of a connected third-party storage has the root of that storage as its root, and a provider room has it
    /// as its parent: an ID nothing else in the API knows. The REST API shows the section instead (FileEntryDtoHelper
    /// for the root, FolderDtoHelper for the parent of a room), and so does the payload.
    /// </summary>
    private async Task FillProviderSectionAsync<T>(FileEntryWebhookDto<T> result, FileEntry<T> entry)
    {
        if (!entry.ProviderEntry)
        {
            return;
        }

        int? sectionId = entry.RootFolderType switch
        {
            FolderType.VirtualRooms => await globalFolderHelper.GetFolderVirtualRooms(),
            FolderType.Archive => await globalFolderHelper.GetFolderArchive(),
            FolderType.RoomTemplates => await globalFolderHelper.GetFolderRoomTemplatesAsync(),
            FolderType.DefaultTemplates => await globalFolderHelper.GetFolderDefaultTemplatesAsync(),
            _ => null
        };

        if (sectionId is not { } id)
        {
            return;
        }

        result.RootFolderId = IdConverter.Convert<T>(id);

        if (entry is Folder<T> { IsRoom: true })
        {
            result.ParentId = result.RootFolderId;
        }
    }
}
