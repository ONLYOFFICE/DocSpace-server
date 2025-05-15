// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Core.Core;

[Scope]
public class RoomService(
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext,
    UserManager userManager,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    FileMarker fileMarker,
    FilesMessageService filesMessageService,
    FileSharing fileSharing,
    IUrlShortener urlShortener,
    SettingsManager settingsManager,
    FileMarkAsReadOperationsManager fileOperationsManager,
    TenantManager tenantManager,
    EntryStatusManager entryStatusManager,
    InvitationService invitationService,
    StudioNotifyService studioNotifyService,
    RoomLogoManager roomLogoManager,
    CoreBaseSettings coreBaseSettings,
    IDistributedLockProvider distributedLockProvider,
    FileUtilityConfiguration fileUtilityConfiguration,
    WatermarkManager watermarkManager,
    CustomTagsService customTagsService,
    IMapper mapper,
    WebhookManager webhookManager,
    FolderService folderService,
    EntriesOrderService entriesOrderService,
    ILogger<FileService> logger)
{
    public async Task<Folder<T>> UpdateRoomAsync<T>(T folderId, UpdateRoomRequest updateData)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenantId);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (updateData.Quota != null && maxTotalSize < updateData.Quota)
        {
            throw new InvalidOperationException(Resource.RoomQuotaGreaterPortalError);
        }

        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < updateData.Quota)
                {
                    throw new InvalidOperationException(Resource.RoomQuotaGreaterPortalError);
                }
            }
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var folder = await folderDao.GetFolderAsync(folderId);
        if (folder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);
        var canEdit = folder.RootFolderType != FolderType.Archive && await fileSecurity.CanEditRoomAsync(folder);

        if (!canEdit)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_RenameFolder);
        }

        switch (folder.RootFolderType)
        {
            case FolderType.TRASH:
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
            case FolderType.Archive:
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        }

        var folderAccess = folder.Access;

        var titleChanged = !string.Equals(folder.Title, updateData.Title, StringComparison.Ordinal) && updateData.Title != null;
        var quotaChanged = folder.SettingsQuota != updateData.Quota && updateData.Quota != null;
        var indexingChanged = updateData.Indexing.HasValue && folder.SettingsIndexing != updateData.Indexing;
        var denyDownloadChanged = updateData.DenyDownload.HasValue && folder.SettingsDenyDownload != updateData.DenyDownload;
        var lifetimeChanged = updateData.Lifetime != null;
        var watermarkChanged = updateData.Watermark != null;
        var colorChanged = updateData.Color != null && folder.SettingsColor != updateData.Color;
        var coverChanged = updateData.Cover != null && folder.SettingsCover != updateData.Cover;

        if (titleChanged || quotaChanged || indexingChanged || denyDownloadChanged || lifetimeChanged || watermarkChanged || colorChanged || coverChanged)
        {
            var oldTitle = folder.Title;
            WatermarkSettings watermark = null;
            RoomDataLifetime lifetime = null;

            if (watermarkChanged)
            {
                watermark = mapper.Map<WatermarkRequestDto, WatermarkSettings>(updateData.Watermark);
                watermark.ImageUrl = await watermarkManager.GetWatermarkImageUrlAsync(folder, watermark.ImageUrl);
            }

            if (lifetimeChanged)
            {
                lifetime = mapper.Map<RoomDataLifetimeDto, RoomDataLifetime>(updateData.Lifetime);
                lifetime.StartDate = DateTime.UtcNow;
            }

            var newFolderId = await folderDao.UpdateFolderAsync(
                folder,
                titleChanged ? updateData.Title : folder.Title,
                quotaChanged ? (long)updateData.Quota : folder.SettingsQuota,
                indexingChanged ? updateData.Indexing.Value : folder.SettingsIndexing,
                denyDownloadChanged ? updateData.DenyDownload.Value : folder.SettingsDenyDownload,
                lifetimeChanged ? lifetime : folder.SettingsLifetime,
                watermarkChanged ? (updateData.Watermark.Enabled.HasValue && !updateData.Watermark.Enabled.Value ? null : watermark) : folder.SettingsWatermark,
                colorChanged ? updateData.Color : folder.SettingsColor,
                coverChanged ? updateData.Cover : folder.SettingsCover);

            folder = await folderDao.GetFolderAsync(newFolderId);

            folder.Access = folderAccess;

            if (isRoom)
            {
                if (watermarkChanged)
                {
                    if (updateData.Watermark.Enabled.HasValue && !updateData.Watermark.Enabled.Value)
                    {
                        await filesMessageService.SendAsync(MessageAction.RoomWatermarkDisabled, folder, folder.Title);
                    }
                    else
                    {
                        await filesMessageService.SendAsync(MessageAction.RoomWatermarkSet, folder, folder.Title);
                    }
                }

                if (indexingChanged)
                {
                    if (updateData.Indexing.Value)
                    {
                        await entriesOrderService.ReOrderAsync(folder.Id, true, true);
                        await filesMessageService.SendAsync(MessageAction.RoomIndexingEnabled, folder);
                    }
                    else
                    {
                        await filesMessageService.SendAsync(MessageAction.RoomIndexingDisabled, folder);
                    }
                }

                if (denyDownloadChanged)
                {
                    await filesMessageService.SendAsync(updateData.DenyDownload.Value
                            ? MessageAction.RoomDenyDownloadEnabled
                            : MessageAction.RoomDenyDownloadDisabled,
                        folder, folder.Title);
                }

                if ((colorChanged || coverChanged) && !folder.SettingsHasLogo)
                {
                    await filesMessageService.SendAsync(MessageAction.RoomCoverChanged, folder, folder.Title);
                }

                if (lifetimeChanged)
                {
                    if (updateData.Lifetime.Enabled.HasValue && !updateData.Lifetime.Enabled.Value)
                    {
                        await filesMessageService.SendAsync(MessageAction.RoomLifeTimeDisabled, folder);
                    }
                    else
                    {
                        await filesMessageService.SendAsync(MessageAction.RoomLifeTimeSet, folder, lifetime.Value.ToString(), lifetime.Period.ToStringFast(),
                            lifetime.DeletePermanently.ToString());
                    }
                }
            }

            if (titleChanged)
            {
                if (isRoom)
                {
                    await filesMessageService.SendAsync(MessageAction.RoomRenamed, oldTitle, folder, folder.Title);
                }
                else
                {
                    await filesMessageService.SendAsync(MessageAction.FolderRenamed, folder, folder.Title);
                }
            }

            if (isRoom && quotaChanged)
            {
                if (updateData.Quota >= 0)
                {
                    filesMessageService.Send(MessageAction.CustomQuotaPerRoomChanged, updateData.Quota.ToString(), [folder.Title]);
                }
                else if (updateData.Quota == -1)
                {
                    filesMessageService.Send(MessageAction.CustomQuotaPerRoomDisabled, folder.Title);
                }
                else
                {
                    var quotaRoomSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
                    filesMessageService.Send(MessageAction.CustomQuotaPerRoomDefault, quotaRoomSettings.DefaultQuota.ToString(), [folder.Title]);
                }
            }
        }

        if (updateData.Logo != null)
        {
            await roomLogoManager.SaveLogo(updateData.Logo.TmpFile, updateData.Logo.X, updateData.Logo.Y, updateData.Logo.Width, updateData.Logo.Height, folder, folderDao);
        }

        if (updateData.Tags != null)
        {
            var currentTags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, TagType.Custom).ToListAsync();
            var tagsInfos = new List<TagInfo>();

            if (updateData.Tags.Any())
            {
                tagsInfos = await tagDao.GetTagsInfoAsync(updateData.Tags, TagType.Custom).ToListAsync();
                var notFoundTags = updateData.Tags.Where(x => tagsInfos.All(r => r.Name != x));

                foreach (var tagInfo in notFoundTags)
                {
                    tagsInfos.Add(await customTagsService.CreateTagAsync(tagInfo));
                }

                if (tagsInfos.Count != 0)
                {
                    var tags = tagsInfos.Select(tagInfo => Tag.Custom(Guid.Empty, folder, tagInfo.Name)).ToList();

                    await tagDao.SaveTagsAsync(tags);

                    var addedTags = tags.Select(t => t.Name).Except(currentTags.Select(t => t.Name)).ToList();
                    if (addedTags.Count > 0)
                    {
                        await filesMessageService.SendAsync(MessageAction.AddedRoomTags, folder, folder.Title, string.Join(',', addedTags));
                    }
                }
            }

            var toDelete = currentTags.Where(r => tagsInfos.All(b => b.Name != r.Name)).ToList();
            await tagDao.RemoveTagsAsync(folder, toDelete.Select(t => t.Id).ToList());

            if (toDelete.Count > 0)
            {
                await filesMessageService.SendAsync(MessageAction.DeletedRoomTags, folder, folder.Title, string.Join(',', toDelete.Select(t => t.Name)));
            }
        }

        var newTags = tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, folder);
        var tag = await newTags.FirstOrDefaultAsync();
        if (tag != null)
        {
            folder.NewForMe = tag.Count;
        }

        if (folder.RootFolderType == FolderType.USER
            && !Equals(folder.RootCreateBy, authContext.CurrentAccount.ID)
            && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(folder.ParentId)))
        {
            folder.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
        }

        await entryStatusManager.SetIsFavoriteFolderAsync(folder);

        await socketManager.UpdateFolderAsync(folder);

        await webhookManager.PublishAsync(isRoom ? WebhookTrigger.RoomUpdated : WebhookTrigger.FolderUpdated, folder);

        return folder;
    }
        
    public async Task<IEnumerable<KeyValuePair<DateTime, IEnumerable<KeyValuePair<FileEntry, IEnumerable<FileEntry>>>>>> GetNewRoomFilesAsync()
    {
        try
        {
            var newFiles = await fileMarker.GetRoomGroupedNewItemsAsync();
            if (newFiles.Count == 0)
            {
                await fileOperationsManager.Publish([JsonSerializer.SerializeToElement(await globalFolderHelper.FolderVirtualRoomsAsync)], []);
            }

            return newFiles
                .OrderByDescending(x => x.Key)
                .Select(x =>
                    new KeyValuePair<DateTime, IEnumerable<KeyValuePair<FileEntry, IEnumerable<FileEntry>>>>(x.Key, x.Value
                        .OrderByDescending(y => y.Key.ModifiedOn)
                        .Select(y =>
                            new KeyValuePair<FileEntry, IEnumerable<FileEntry>>(y.Key, y.Value
                                .Where(y1 => y1.FileEntryType == FileEntryType.File)
                                .OrderByDescending(y1 => y1.ModifiedOn)))));
        }
        catch (Exception e)
        {
            throw FileStorageService.GenerateException(e, logger, authContext);
        }
    }

    public async Task<IEnumerable<KeyValuePair<DateTime, IEnumerable<FileEntry>>>> GetNewRoomFilesAsync<T>(T folderId)
    {
        try
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(folderId);

            var newFiles = await fileMarker.MarkedItemsAsync(folder).Where(e => e.FileEntryType == FileEntryType.File).ToListAsync();
            if (newFiles.Count == 0)
            {
                await fileOperationsManager.Publish([JsonSerializer.SerializeToElement(folderId)], []);
            }

            return newFiles
                .GroupBy(x => x.ModifiedOn.Date)
                .OrderByDescending(x => x.Key)
                .Select(x =>
                    new KeyValuePair<DateTime, IEnumerable<FileEntry>>(
                        x.Key, x.OrderByDescending(y => y.ModifiedOn)));
        }
        catch (Exception e)
        {
            throw FileStorageService.GenerateException(e, logger, authContext);
        }
    }
    
    public async Task<bool> AnyRoomsAsync(Guid user)
    {
        var any = (await folderService.GetFolderItemsAsync(
            await globalFolderHelper.GetFolderVirtualRooms(),
            0,
            -1,
            new List<FilterType> { FilterType.FoldersOnly },
            false,
            user.ToString(),
            "",
            [],
            false,
            false,
            null)).Entries.Count != 0;

        return any;
    }
    
    public async IAsyncEnumerable<AceWrapper> GetRoomSharedInfoAsync<T>(T roomId, IEnumerable<Guid> subjects)
    {
        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId).NotFoundIfNull();

        await foreach (var ace in fileSharing.GetPureSharesAsync(room, subjects))
        {
            yield return ace;
        }
    }
    
    public async Task<Folder<T>> SetPinnedStatusAsync<T>(T folderId, bool pin)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(folderId);

        if (room == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanPinAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorrMessage_PinRoom);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var tag = Tag.Pin(authContext.CurrentAccount.ID, room);

        if (pin)
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync($"pin_{authContext.CurrentAccount.ID}"))
            {
                var count = await tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.Pin).CountAsync();
                if (count >= fileUtilityConfiguration.MaxPinnedRooms)
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorrMessage_PinRoom);
                }

                await tagDao.SaveTagsAsync(tag);
            }
        }
        else
        {
            await tagDao.RemoveTagsAsync(tag);
        }

        room.Pinned = pin;

        return room;
    }
    
    public async Task ResendEmailInvitationsAsync<T>(T id, IEnumerable<Guid> usersIds, bool resendAll)
    {
        if (!resendAll && (usersIds == null || !usersIds.Any()))
        {
            return;
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetFolderAsync(id).NotFoundIfNull();

        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        Dictionary<Guid, UserRelation> userRelations = null;
        var currentUserId = authContext.CurrentAccount.ID;

        var isDocSpaceAdmin = await userManager.IsDocSpaceAdminAsync(currentUserId);

        if (!resendAll)
        {
            await foreach (var ace in fileSharing.GetPureSharesAsync(room, usersIds))
            {
                var user = await userManager.GetUsersAsync(ace.Id);
                if (!await HasAccessInviteAsync(user))
                {
                    continue;
                }

                var link = invitationService.GetInvitationLink(user.Email, ace.Access, authContext.CurrentAccount.ID, room.Id.ToString());
                await studioNotifyService.SendEmailRoomInviteAsync(user.Email, room.Title, await urlShortener.GetShortenLinkAsync(link));
                await filesMessageService.SendAsync(MessageAction.RoomInviteResend, room, user.Email, user.Id.ToString());
            }

            return;
        }

        const int margin = 1;
        const int packSize = 1000;
        var offset = 0;
        var finish = false;

        while (!finish)
        {
            var counter = 0;

            await foreach (var ace in fileSharing.GetPureSharesAsync(room, ShareFilterType.User, EmployeeActivationStatus.Pending, null, offset, packSize + margin))
            {
                counter++;

                if (counter > packSize)
                {
                    offset += packSize;
                    break;
                }

                var user = await userManager.GetUsersAsync(ace.Id);
                if (!await HasAccessInviteAsync(user))
                {
                    continue;
                }

                var link = invitationService.GetInvitationLink(user.Email, ace.Access, authContext.CurrentAccount.ID, id.ToString());
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);

                await studioNotifyService.SendEmailRoomInviteAsync(user.Email, room.Title, shortenLink);
                await filesMessageService.SendAsync(MessageAction.RoomInviteResend, room, user.Email, user.Id.ToString());
            }

            if (counter <= packSize)
            {
                finish = true;
            }
        }

        return;

        async Task<bool> HasAccessInviteAsync(UserInfo user)
        {
            if (user.Status == EmployeeStatus.Terminated)
            {
                return false;
            }

            if (isDocSpaceAdmin)
            {
                return true;
            }

            var type = await userManager.GetUserTypeAsync(user);
            if (type != EmployeeType.Guest || (user.CreatedBy.HasValue && user.CreatedBy.Value == currentUserId))
            {
                return true;
            }

            userRelations ??= await userManager.GetUserRelationsAsync(currentUserId);
            return userRelations.ContainsKey(user.Id);
        }
    }
}