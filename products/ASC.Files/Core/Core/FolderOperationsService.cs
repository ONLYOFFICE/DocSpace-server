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
public class FolderOperationsService(
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    NotifyClient notifyClient,
    IServiceProvider serviceProvider,
    TenantManager tenantManager,
    EntryStatusManager entryStatusManager,
    FileShareParamsHelper fileShareParamsHelper,
    EncryptionLoginProvider encryptionLoginProvider,
    CountRoomChecker countRoomChecker,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
    QuotaSocketManager quotaSocketManager,
    RoomLogoManager roomLogoManager,
    IDistributedLockProvider distributedLockProvider,
    WatermarkManager watermarkManager,
    CustomTagsService customTagsService,
    WebhookManager webhookManager,
    ILogger<FolderOperationsService> logger,
    SharingService sharingService)
{
    public async Task<Folder<T>> GetFolderAsync<T>(T folderId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var tagDao = daoFactory.GetTagDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanReadAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        var tag = await tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, folder).FirstOrDefaultAsync();
        if (tag != null)
        {
            folder.NewForMe = tag.Count;
        }

        var tags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, null, authContext.CurrentAccount.ID).ToListAsync();
        folder.Pinned = tags.Any(r => r.Type == TagType.Pin);
        folder.IsFavorite = tags.Any(r => r.Type == TagType.Favorite);
        folder.Tags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, TagType.Custom).ToListAsync();

        return folder;
    }

    public async Task<IEnumerable<FileEntry>> GetFoldersAsync<T>(T parentId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        IEnumerable<FileEntry> entries;

        try
        {
            var parent = await folderDao.GetFolderAsync(parentId);

            Folder<T> parentRoom = null;

            if (parent.RootFolderType == FolderType.VirtualRooms)
            {
                parentRoom = !DocSpaceHelper.IsRoom(parent.FolderType) && parent.FolderType != FolderType.VirtualRooms ? await folderDao.GetFirstParentTypeFromFileEntryAsync(parent) : parent;
            }

            (entries, _) = await entryManager.GetEntriesAsync(parent, parentRoom, 0, -1, [FilterType.FoldersOnly], false, Guid.Empty, string.Empty, [], false, false, new OrderBy(SortedByType.AZ, true));
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }

        return entries;
    }
    
    public async Task<Folder<T>> CreateFolderAsync<T>(T parentId, string title)
    {
        var folder = await InternalCreateFolderAsync(parentId, title);

        await socketManager.CreateFolderAsync(folder);

        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetParentFoldersAsync(folder.Id).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
        if (room != null && !DocSpaceHelper.FormsFillingSystemFolders.Contains(folder.FolderType))
        {
            var whoCanRead = await fileSecurity.WhoCanReadAsync(room, true);
            await notifyClient.SendFolderCreatedInRoom(room, whoCanRead, folder, authContext.CurrentAccount.ID);
        }

        await filesMessageService.SendAsync(MessageAction.FolderCreated, folder, folder.Title);

        await webhookManager.PublishAsync(WebhookTrigger.FolderCreated, folder);

        return folder;
    }
    
    public async Task<Folder<int>> CreateRoomAsync(string title, RoomType roomType, bool privacy, bool? indexing, IEnumerable<FileShareParams> share, long? quota, RoomDataLifetime lifetime, bool? denyDownload, WatermarkRequestDto watermark, string color, string cover, IEnumerable<string> tags, LogoRequest logo)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var parentId = await globalFolderHelper.GetFolderVirtualRooms();

        return await CreateRoomAsync(async () =>
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(tenantId)))
            {
                await countRoomChecker.CheckAppend();
                return await InternalCreateFolderAsync(parentId, title, DocSpaceHelper.MapToFolderType(roomType), privacy, indexing, quota, lifetime, denyDownload, watermark, color, cover, tags, logo);
            }
        }, privacy, share);
    }
    
    private async Task<Folder<T>> CreateRoomAsync<T>(Func<Task<Folder<T>>> folderFactory, bool privacy, IEnumerable<FileShareParams> shares)
    {
        ArgumentNullException.ThrowIfNull(folderFactory);

        List<AceWrapper> aces = null;

        if (privacy)
        {
            if (shares == null || !shares.Any())
            {
                throw new ArgumentNullException(nameof(shares));
            }

            aces = await GetFullAceWrappersAsync(shares);
            await CheckEncryptionKeysAsync(aces);
        }

        var folder = await folderFactory();
        if (folder == null)
        {
            return null;
        }

        await filesMessageService.SendAsync(MessageAction.RoomCreated, folder, folder.Title);

        await webhookManager.PublishAsync(WebhookTrigger.RoomCreated, folder);

        if (folder.ParentId is int parent && parent == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
        }
        else
        {
            switch (folder.FolderType)
            {
                case FolderType.PublicRoom:
                    await sharingService.SetExternalLinkAsync(folder, Guid.NewGuid(), FileShare.Read, FilesCommonResource.DefaultExternalLinkTitle, primary: true);
                    break;
                case FolderType.FillingFormsRoom:
                    await sharingService.SetExternalLinkAsync(folder, Guid.NewGuid(), FileShare.FillForms, FilesCommonResource.FillOutExternalLinkTitle, primary: true);
                    break;
            }
        }

        if (privacy)
        {
            await SetAcesForPrivateRoomAsync(folder, aces);
        }

        await socketManager.CreateFolderAsync(folder);

        if (folder.ProviderEntry)
        {
            return folder;
        }

        if (folder.SettingsIndexing)
        {
            await filesMessageService.SendAsync(MessageAction.RoomIndexingEnabled, folder);
        }

        if (folder.SettingsDenyDownload)
        {
            await filesMessageService.SendAsync(MessageAction.RoomDenyDownloadEnabled, folder, folder.Title);
        }

        if (folder.SettingsLifetime != null)
        {
            await filesMessageService.SendAsync(
                MessageAction.RoomLifeTimeSet,
                folder,
                folder.SettingsLifetime.Value.ToString(),
                folder.SettingsLifetime.Period.ToStringFast(),
                folder.SettingsLifetime.DeletePermanently.ToString());
        }

        if (folder.SettingsWatermark != null)
        {
            await filesMessageService.SendAsync(MessageAction.RoomWatermarkSet, folder, folder.Title);
        }

        return folder;
    }
    
    public async Task<Folder<string>> CreateThirdPartyRoomAsync(string title, RoomType roomType, string parentId, bool privacy, bool? indexing, bool createAsNewFolder, bool? denyDownload, string color, string cover, IEnumerable<string> tags, LogoRequest logo)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var folderDao = daoFactory.GetFolderDao<string>();
        var providerDao = daoFactory.ProviderDao;

        var parent = await folderDao.GetFolderAsync(parentId);
        var providerInfo = await providerDao.GetProviderInfoAsync(parent.ProviderId);

        if (providerInfo.RootFolderType != FolderType.VirtualRooms)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_InvalidProvider);
        }

        if (providerInfo.FolderId != null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ProviderAlreadyConnect);
        }

        var folderType = DocSpaceHelper.MapToFolderType(roomType);

        var room = await CreateRoomAsync(async () =>
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(tenantId)))
            {
                await countRoomChecker.CheckAppend();

                var folder = parent;

                if (createAsNewFolder)
                {
                    try
                    {
                        folder = await InternalCreateFolderAsync(parentId, title, folderType, false, indexing, denyDownload: denyDownload, color: color, cover: cover, names: tags, logo: logo);
                    }
                    catch
                    {
                        throw new InvalidOperationException(FilesCommonResource.ErrorMessage_InvalidThirdPartyFolder);
                    }
                }

                await providerDao.UpdateRoomProviderInfoAsync(new ProviderData
                {
                    Id = providerInfo.ProviderId,
                    Title = title,
                    FolderId = folder.Id,
                    FolderType = folderType,
                    Private = privacy
                });

                folder.FolderType = folderType;
                folder.Shared = folderType == FolderType.PublicRoom;
                folder.RootFolderType = FolderType.VirtualRooms;
                folder.FolderIdDisplay = IdConverter.Convert<string>(await globalFolderHelper.FolderVirtualRoomsAsync);

                return folder;
            }
        }, false, null);

        return room;
    }

    public async Task<Folder<int>> CreateRoomTemplateAsync(int roomId, string title, IEnumerable<FileShareParams> share, IEnumerable<string> tags, LogoRequest logo, string cover, string color)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var parentId = await globalFolderHelper.FolderRoomTemplatesAsync;
        var folderDao = daoFactory.GetFolderDao<int>();
        var room = await folderDao.GetFolderAsync(roomId);

        if (!DocSpaceHelper.IsRoom(room.FolderType) || room.RootId != await globalFolderHelper.FolderVirtualRoomsAsync || !await fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        WatermarkRequestDto watermarkRequestDto = null;
        if (room.SettingsWatermark != null)
        {
            watermarkRequestDto = new WatermarkRequestDto
            {
                Text = room.SettingsWatermark.Text,
                Additions = room.SettingsWatermark.Additions,
                Rotate = room.SettingsWatermark.Rotate,
                ImageUrl = room.SettingsWatermark.ImageUrl,
                ImageScale = room.SettingsWatermark.ImageScale,
                ImageHeight = room.SettingsWatermark.ImageHeight,
                ImageWidth = room.SettingsWatermark.ImageWidth
            };
        }

        return await CreateRoomAsync(async () =>
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(tenantId)))
            {
                return await InternalCreateFolderAsync(parentId, title, room.FolderType, room.SettingsPrivate, room.SettingsIndexing, room.SettingsQuota, room.SettingsLifetime, room.SettingsDenyDownload, watermarkRequestDto, color, cover, tags, logo);
            }
        }, room.SettingsPrivate, share);
    }

    public async Task<Folder<int>> CreateRoomFromTemplateAsync(int templateId, 
        string title,
        IEnumerable<string> tags,
        LogoRequest logo,
        string cover,
        string color,
        bool? indexing,
        bool? denyDownload,
        RoomLifetime lifetime,
        WatermarkRequest watermark,
        bool? @private)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var parentId = await globalFolderHelper.FolderVirtualRoomsAsync;
        var folderDao = daoFactory.GetFolderDao<int>();
        var template = await folderDao.GetFolderAsync(templateId);

        if (!DocSpaceHelper.IsRoom(template.FolderType) || template.RootId != await globalFolderHelper.FolderRoomTemplatesAsync || !await fileSecurity.CanReadAsync(template))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        WatermarkRequestDto watermarkDto = null;
        if (watermark != null)
        {
            watermarkDto = new WatermarkRequestDto
            {
                Text = watermark.Text,
                Additions = watermark.Additions,
                Rotate = watermark.Rotate,
                ImageUrl = watermark.ImageUrl,
                ImageScale = watermark.ImageScale,
                ImageHeight = watermark.ImageHeight,
                ImageWidth = watermark.ImageWidth
            };
        }

        RoomDataLifetime lifeTimeSetting = null;
        if (lifetime != null)
        {
            lifeTimeSetting = new RoomDataLifetime
            {
                DeletePermanently = lifetime.DeletePermanently,
                Enabled = lifetime.Enabled,
                Period = lifetime.Period,
                Value = lifetime.Value,
                StartDate = DateTime.UtcNow
            };
        }

        var settingIndex = indexing ?? template.SettingsIndexing;
        var settingDenyDownload = denyDownload ?? template.SettingsDenyDownload;
        var settingPrivate = @private ?? template.SettingsPrivate;

        return await CreateRoomAsync(async () =>
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(tenantId)))
            {
                await countRoomChecker.CheckAppend();
                return await InternalCreateFolderAsync(parentId, title, template.FolderType, settingPrivate, settingIndex, template.SettingsQuota, lifeTimeSetting, settingDenyDownload, watermarkDto, color, cover, tags, logo);
            }
        }, template.SettingsPrivate, []);
    }
    
     public async Task<Folder<T>> FolderRenameAsync<T>(T folderId, string title)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        if (folder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        var canEdit = DocSpaceHelper.IsRoom(folder.FolderType)
            ? folder.RootFolderType != FolderType.Archive && await fileSecurity.CanEditRoomAsync(folder)
            : await fileSecurity.CanRenameAsync(folder);

        if (!canEdit)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_RenameFolder);
        }

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_RenameFolder);
        }

        if (folder.RootFolderType == FolderType.TRASH)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        if (folder.RootFolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        }

        var folderAccess = folder.Access;
        var renamedFolder = folder;

        if (!string.Equals(folder.Title, title, StringComparison.Ordinal))
        {
            var oldTitle = folder.Title;
            T newFolderId = default;

            if (folder.MutableId)
            {
                await socketManager.DeleteFolder(folder, action: async () =>
                {
                    newFolderId = await folderDao.RenameFolderAsync(folder, title);
                });
            }
            else
            {
                newFolderId = await folderDao.RenameFolderAsync(folder, title);
            }

            renamedFolder = await folderDao.GetFolderAsync(newFolderId);
            renamedFolder.Access = folderAccess;

            if (DocSpaceHelper.IsRoom(renamedFolder.FolderType))
            {
                await filesMessageService.SendAsync(MessageAction.RoomRenamed, oldTitle, renamedFolder, renamedFolder.Title);

                await webhookManager.PublishAsync(WebhookTrigger.RoomUpdated, renamedFolder);
            }
            else
            {
                await filesMessageService.SendAsync(MessageAction.FolderRenamed, renamedFolder, renamedFolder.Title, oldTitle);

                await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, renamedFolder);
            }

            //if (!folder.ProviderEntry)
            //{
            //    FoldersIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, folder));
            //}
        }

        var newTags = tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, renamedFolder);
        var tag = await newTags.FirstOrDefaultAsync();
        if (tag != null)
        {
            renamedFolder.NewForMe = tag.Count;
        }

        if (renamedFolder.RootFolderType == FolderType.USER
            && !Equals(renamedFolder.RootCreateBy, authContext.CurrentAccount.ID)
            && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(renamedFolder.ParentId)))
        {
            renamedFolder.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
        }

        await entryStatusManager.SetIsFavoriteFolderAsync(renamedFolder);

        if (renamedFolder.MutableId)
        {
            await socketManager.CreateFolderAsync(renamedFolder);
        }
        else
        {
            await socketManager.UpdateFolderAsync(renamedFolder);
        }

        return renamedFolder;
    }
    
    private async Task<Folder<T>> InternalCreateFolderAsync<T>(T parentId, string title, FolderType folderType = FolderType.DEFAULT, bool privacy = false, bool? indexing = false, long? quota = TenantEntityQuotaSettings.DefaultQuotaValue, RoomDataLifetime lifetime = null, bool? denyDownload = false, WatermarkRequestDto watermark = null, string color = null, string cover = null, IEnumerable<string> names = null, LogoRequest logo = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(parentId);

        var folderDao = daoFactory.GetFolderDao<T>();

        var parent = await folderDao.GetFolderAsync(parentId);
        var isRoom = DocSpaceHelper.IsRoom(folderType);

        if (parent == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (parent.FolderType == FolderType.RoomTemplates)
        {
            if (!await fileSecurity.CanCreateFromAsync(parent))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }
        }
        else
        {
            if (!await fileSecurity.CanCreateAsync(parent))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }
        }

        if (parent.RootFolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        }

        if (parent.FolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!isRoom && parent.FolderType == FolderType.VirtualRooms)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenantId);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new InvalidOperationException(Resource.RoomQuotaGreaterPortalError);
        }

        try
        {
            var newFolder = serviceProvider.GetService<Folder<T>>();
            newFolder.Title = title;
            newFolder.ParentId = parent.Id;
            newFolder.FolderType = folderType;
            newFolder.SettingsPrivate = parent.SettingsPrivate ? parent.SettingsPrivate : privacy;
            newFolder.SettingsColor = roomLogoManager.GetRandomColour();

            if (indexing.HasValue)
            {
                newFolder.SettingsIndexing = indexing.Value;
            }

            if (denyDownload.HasValue)
            {
                newFolder.SettingsDenyDownload = denyDownload.Value;
            }

            if (quota.HasValue)
            {
                newFolder.SettingsQuota = quota.Value;
            }

            newFolder.SettingsLifetime = lifetime;
            _ = RoomLogoManager.ColorChanged(color, newFolder);
            _ = await RoomLogoManager.CoverChanged(cover, newFolder);

            var folderId = await folderDao.SaveFolderAsync(newFolder);
            if (watermark != null)
            {
                try
                {
                    await watermarkManager.SetWatermarkAsync(newFolder, watermark);
                }
                catch (Exception)
                {
                }
            }


            var folder = await folderDao.GetFolderAsync(folderId);

            if (logo != null)
            {
                await roomLogoManager.SaveLogo(logo.TmpFile, logo.X, logo.Y, logo.Width, logo.Height, folder, folderDao);
            }

            var tagDao = daoFactory.GetTagDao<T>();

            if (names != null)
            {
                var tagsInfos = await tagDao.GetTagsInfoAsync(names, TagType.Custom).ToListAsync();
                var notFoundTags = names?.Where(x => tagsInfos.All(r => r.Name != x));

                if (notFoundTags != null)
                {
                    foreach (var tagInfo in notFoundTags)
                    {
                        tagsInfos.Add(await customTagsService.CreateTagAsync(tagInfo));
                    }
                }

                if (tagsInfos.Count != 0)
                {
                    var tags = tagsInfos.Select(tagInfo => Tag.Custom(Guid.Empty, folder, tagInfo.Name));

                    await tagDao.SaveTagsAsync(tags);

                    await filesMessageService.SendAsync(MessageAction.AddedRoomTags, folder, folder.Title, string.Join(',', tags.Select(x => x.Name)));
                }
            }

            if (!isRoom)
            {
                return folder;
            }

            if (folder.Id.Equals(folder.RootId))
            {
                return null;
            }

            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);

            return folder;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }
        
    private async Task<List<AceWrapper>> GetFullAceWrappersAsync(IEnumerable<FileShareParams> share)
    {
        var dict = await share.ToAsyncEnumerable().SelectAwait(async s => await fileShareParamsHelper.ToAceObjectAsync(s)).ToDictionaryAsync(k => k.Id, v => v);

        var admins = await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);
        var onlyFilesAdmins = await userManager.GetUsersByGroupAsync(WebItemManager.DocumentsProductID);

        var userInfos = admins.Union(onlyFilesAdmins).ToList();

        foreach (var userId in userInfos.Select(r => r.Id))
        {
            dict[userId] = new AceWrapper { Access = FileShare.ReadWrite, Id = userId };
        }

        return dict.Values.ToList();
    }

    private async Task CheckEncryptionKeysAsync(IEnumerable<AceWrapper> aceWrappers)
    {
        var users = aceWrappers.Select(s => s.Id).ToList();
        var keys = await encryptionLoginProvider.GetKeysAsync(users);

        foreach (var user in users)
        {
            if (!keys.ContainsKey(user))
            {
                var userInfo = await userManager.GetUsersAsync(user);
                throw new InvalidOperationException($"The user {userInfo.DisplayUserName(displayUserSettingsHelper)} does not have an encryption key");
            }
        }
    }
    
    private async Task SetAcesForPrivateRoomAsync<T>(Folder<T> room, List<AceWrapper> aces)
    {
        var advancedSettings = new AceAdvancedSettingsWrapper { AllowSharingPrivateRoom = true };

        var aceCollection = new AceCollection<T> { Folders = [room.Id], Files = [], Aces = aces, AdvancedSettings = advancedSettings };

        await sharingService.SetAceObjectAsync(aceCollection, false);
    }
    
    private Exception GenerateException(Exception error, bool warning = false)
    {
        if (warning || error is ItemNotFoundException or SecurityException or ArgumentException or TenantQuotaException or InvalidOperationException)
        {
            logger.Information(error.ToString());
        }
        else
        {
            logger.ErrorFileStorageService(error);
        }

        if (error is ItemNotFoundException)
        {
            return !authContext.CurrentAccount.IsAuthenticated
                ? new SecurityException(FilesCommonResource.ErrorMessage_SecurityException)
                : error;
        }

        return new InvalidOperationException(error.Message, error);
    }
}