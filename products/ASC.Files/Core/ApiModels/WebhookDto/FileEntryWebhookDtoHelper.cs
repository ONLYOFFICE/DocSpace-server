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
/// </remarks>
[Scope]
public class FileEntryWebhookDtoHelper(
    UserWebhookDtoHelper userWebhookDtoHelper,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory)
{
    /// <summary>
    /// Maps any entry to the payload its trigger expects: a file, a room or a plain folder.
    /// </summary>
    /// <remarks>
    /// Declared as the base type: the envelope writes its payload by the runtime type (<c>WebhookPayload.Payload</c>),
    /// so the file, folder and room fields reach the wire all the same.
    /// </remarks>
    public async Task<FileEntryWebhookDto<T>> GetAsync<T>(FileEntry<T> entry)
    {
        return entry switch
        {
            File<T> file => await GetFileAsync(file),
            Folder<T> { IsRoom: true } room => await GetRoomAsync(room),
            Folder<T> folder => await GetFolderAsync(folder),
            _ => null
        };
    }

    /// <summary>
    /// Cuts a payload built by <see cref="GetAsync{T}"/> from <paramref name="source"/> for one subscription owner, as
    /// the REST API shows the entry to them: a User or a Guest gets the author and the last editor without the profile
    /// link and the user name, the quota of a room goes only to an owner who may edit the room, and the origins of an
    /// entry in the trash only to an owner who may read the folder or the room it was deleted from.
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

        if (result is RoomWebhookDto<T> { QuotaLimit: not null } room && !await fileSecurity.CanEditRoomAsync(source, subscriberId.Value))
        {
            var withoutQuota = (RoomWebhookDto<T>)(ReferenceEquals(room, entry) ? room.Clone() : room);

            withoutQuota.QuotaLimit = null;
            result = withoutQuota;
        }

        result = await RestrictOriginsForSubscriberAsync(result, entry, subscriberId.Value);

        return result;
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

        var hideFolder = hasFolder && !await CanReadFolderAsync(result.OriginId, subscriberId);
        var hideRoom = hasRoom && !await CanReadFolderAsync(result.OriginRoomId, subscriberId);

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

    private async Task<bool> CanReadFolderAsync<T>(T folderId, Guid subscriberId)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId);

        return folder != null && await fileSecurity.CanReadAsync(folder, subscriberId);
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

        await FillAuthorsAsync(result, file);
        await FillTagStateAsync(result, file);

        return result;
    }

    /// <summary>
    /// The lock and the custom filter live in tags, which a plain DAO read of the file does not fill: read them here,
    /// with the user who set each, independent of who caused the event. One query per file payload.
    /// </summary>
    private async Task FillTagStateAsync<T>(FileWebhookDto<T> result, File<T> file)
    {
        await foreach (var tag in daoFactory.GetTagDao<T>().GetTagsAsync([TagType.Locked, TagType.CustomFilter], [file]))
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

        await FillAuthorsAsync(result, folder);

        return result;
    }

    public async Task<RoomWebhookDto<T>> GetRoomAsync<T>(Folder<T> room)
    {
        var result = room.MapToRoomWebhookDto();

        await FillAuthorsAsync(result, room);

        return result;
    }

    private async Task FillAuthorsAsync<T>(FileEntryWebhookDto<T> result, FileEntry<T> entry)
    {
        result.CreatedBy = await userWebhookDtoHelper.GetSummaryAsync(entry.CreateBy);
        result.UpdatedBy = entry.ModifiedBy == entry.CreateBy
            ? result.CreatedBy
            : await userWebhookDtoHelper.GetSummaryAsync(entry.ModifiedBy);
    }
}
