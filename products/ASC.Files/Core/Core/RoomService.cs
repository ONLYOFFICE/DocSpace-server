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
    ILogger<FileService> logger,
    CountRoomChecker countRoomChecker,
    FileShareParamsHelper fileShareParamsHelper,
    EncryptionLoginProvider encryptionLoginProvider,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    SharingService sharingService)
{
        /// <summary>
    /// Creates a new virtual room asynchronously with specified parameters such as title, room type, privacy settings, and additional configurations.
    /// </summary>
    /// <param name="title">The title of the room to be created.</param>
    /// <param name="roomType">The type of the room (e.g., collaboration, virtual data room).</param>
    /// <param name="privacy">Indicates whether the room is private or public.</param>
    /// <param name="indexing">Specifies if the room should be indexed for search.</param>
    /// <param name="share">A collection of sharing parameters for the room.</param>
    /// <param name="quota">The storage quota allocated to the room, in bytes.</param>
    /// <param name="lifetime">The lifetime configuration for the room, including expiration settings.</param>
    /// <param name="denyDownload">Indicates whether downloads are restricted in the room.</param>
    /// <param name="watermark">The watermark settings for documents within the room.</param>
    /// <param name="color">The display color associated with the room.</param>
    /// <param name="cover">The cover image identifier for the room.</param>
    /// <param name="tags">A list of tags associated with the room for categorization.</param>
    /// <param name="logo">The specific logo settings for the room.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created room with its properties and metadata.</returns>
    public async Task<Folder<int>> CreateRoomAsync(string title, RoomType roomType, bool privacy, bool? indexing, IEnumerable<FileShareParams> share, long? quota, RoomDataLifetime lifetime, bool? denyDownload, WatermarkRequestDto watermark, string color, string cover, IEnumerable<string> tags, LogoRequest logo)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var parentId = await globalFolderHelper.GetFolderVirtualRooms();

        return await CreateRoomAsync(async () =>
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(tenantId)))
            {
                await countRoomChecker.CheckAppend();
                return await folderService.InternalCreateFolderAsync(parentId, title, DocSpaceHelper.MapToFolderType(roomType), privacy, indexing, quota, lifetime, denyDownload, watermark, color, cover, tags, logo);
            }
        }, privacy, share);
    }
    
    /// <summary>
    /// Creates a third-party room asynchronously with the specified parameters.
    /// </summary>
    /// <param name="title">The title of the room to be created.</param>
    /// <param name="roomType">The type of the room to be created.</param>
    /// <param name="parentId">The identifier of the parent folder where the room will be created.</param>
    /// <param name="privacy">Specifies whether the room should be private.</param>
    /// <param name="indexing">Specifies whether the room should support indexing. Optional.</param>
    /// <param name="createAsNewFolder">Indicates whether to create the room as a new folder.</param>
    /// <param name="denyDownload">Specifies whether download permissions should be denied for the room. Optional.</param>
    /// <param name="color">The color to associate with the room.</param>
    /// <param name="cover">The cover image or asset for the room.</param>
    /// <param name="tags">A collection of tags to associate with the room.</param>
    /// <param name="logo">The logo details for the room.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created room as a folder with its associated metadata.</returns>
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
                        folder = await folderService.InternalCreateFolderAsync(parentId, title, folderType, false, indexing, denyDownload: denyDownload, color: color, cover: cover, names: tags, logo: logo);
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

    /// <summary>
    /// Asynchronously creates a room template folder with the specified parameters, such as room ID, title, sharing settings, tags, logo, cover, and color.
    /// </summary>
    /// <param name="roomId">The unique identifier of the room for which the template is created.</param>
    /// <param name="title">The title of the room template to be created.</param>
    /// <param name="share">The collection of sharing parameters to apply to the room template.</param>
    /// <param name="tags">The collection of tags to associate with the room template.</param>
    /// <param name="logo">The logo information to be assigned to the room template.</param>
    /// <param name="cover">The cover image to be associated with the room template.</param>
    /// <param name="color">The color theme to assign to the room template.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created room template folder.</returns>
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
                return await folderService.InternalCreateFolderAsync(parentId, title, room.FolderType, room.SettingsPrivate, room.SettingsIndexing, room.SettingsQuota, room.SettingsLifetime, room.SettingsDenyDownload, watermarkRequestDto, color, cover, tags, logo);
            }
        }, room.SettingsPrivate, share);
    }

    /// <summary>
    /// Creates a new room from a specified template with provided properties such as title, tags, and settings.
    /// </summary>
    /// <param name="templateId">The identifier of the template to use for creating the room.</param>
    /// <param name="title">The title of the room to be created.</param>
    /// <param name="tags">A collection of tags to associate with the room.</param>
    /// <param name="logo">The logo configuration for the room.</param>
    /// <param name="cover">The cover image identifier for the room.</param>
    /// <param name="color">The color scheme identifier for the room.</param>
    /// <param name="indexing">Indicates whether indexing is enabled for the room.</param>
    /// <param name="denyDownload">Specifies whether download restrictions are enabled for the room.</param>
    /// <param name="lifetime">The lifetime configuration for the room.</param>
    /// <param name="watermark">The watermark settings for the room.</param>
    /// <param name="private">Specifies whether the room is private.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created room object, including its properties and settings.</returns>
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
                return await folderService.InternalCreateFolderAsync(parentId, title, template.FolderType, settingPrivate, settingIndex, template.SettingsQuota, lifeTimeSetting, settingDenyDownload, watermarkDto, color, cover, tags, logo);
            }
        }, template.SettingsPrivate, []);
    }

    /// <summary>
    /// Updates the details of an existing virtual room asynchronously by modifying its properties based on the provided update data.
    /// </summary>
    /// <param name="folderId">The unique identifier of the room to be updated.</param>
    /// <param name="updateData">The update request containing the new values for the room properties such as title, privacy settings, color, quota, and more.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated room with its new properties and metadata.</returns>
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

    /// <summary>
    /// Retrieves the newly added files in a specified room grouped by modification date.
    /// </summary>
    /// <param name="folderId">The identifier of the folder (or room) to retrieve the new files from.</param>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of key-value pairs, where each key is a date,
    /// and the value is a collection of file entries modified on that date.</returns>
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

    /// <summary>
    /// Determines asynchronously whether the specified user has any associated virtual rooms.
    /// </summary>
    /// <param name="user">The unique identifier of the user to check for virtual rooms.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a boolean value indicating whether the user has any associated virtual rooms.</returns>
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

    /// <summary>
    /// Retrieves shared information for a specified virtual room and a list of subjects.
    /// This includes access control entries (ACEs) associated with the room.
    /// </summary>
    /// <typeparam name="T">The type of the room identifier.</typeparam>
    /// <param name="roomId">The identifier of the room to retrieve shared information for.</param>
    /// <param name="subjects">A collection of subject identifiers (e.g., user or group IDs) for which to retrieve sharing information.</param>
    /// <returns>An asynchronous stream of <see cref="AceWrapper"/> instances containing the sharing details for the specified room and subjects.</returns>
    public async IAsyncEnumerable<AceWrapper> GetRoomSharedInfoAsync<T>(T roomId, IEnumerable<Guid> subjects)
    {
        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId).NotFoundIfNull();

        await foreach (var ace in fileSharing.GetPureSharesAsync(room, subjects))
        {
            yield return ace;
        }
    }

    /// <summary>
    /// Sets the pinned status of a specified virtual room asynchronously.
    /// </summary>
    /// <param name="folderId">The identifier of the room whose pinned state is to be changed.</param>
    /// <param name="pin">A boolean value indicating whether the room should be pinned (true) or unpinned (false).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated room with the new pinned status.</returns>
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

    /// <summary>
    /// Resends email invitations to specified users associated with a virtual room. Optionally, all invitations for the room can be resent regardless of user selection.
    /// </summary>
    /// <param name="id">The identifier of the virtual room for which the invitations will be resent.</param>
    /// <param name="usersIds">A collection of user identifiers to whom the invitations will be resent. Ignored if <paramref name="resendAll"/> is true.</param>
    /// <param name="resendAll">Determines whether to resend invitations to all room participants. If true, <paramref name="usersIds"/> will be ignored.</param>
    /// <returns>A task that represents the asynchronous operation of resending email invitations.</returns>
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
    
    public async Task<bool> ShouldPreventUserDeletion<T>(Folder<T> room, Guid userId)
    {
        if (room.FolderType != FolderType.VirtualDataRoom)
        {
            return false;
        }

        var fileDao = daoFactory.GetFileDao<T>();
        return await fileDao.GetUserFormRolesInRoom(room.Id, userId).AnyAsync();
    }
    
    public async Task<Folder<T>> FolderQuotaChangeAsync<T>(T folderId, long quota)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var tenantSpaceQuota = await tenantManager.GetTenantQuotaAsync(tenantId);
        var maxTotalSize = tenantSpaceQuota?.MaxTotalSize ?? -1;

        if (maxTotalSize < quota)
        {
            throw new InvalidOperationException(Resource.RoomQuotaGreaterPortalError);
        }

        if (coreBaseSettings.Standalone)
        {
            var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
            if (tenantQuotaSetting.EnableQuota)
            {
                if (tenantQuotaSetting.Quota < quota)
                {
                    throw new InvalidOperationException(Resource.RoomQuotaGreaterPortalError);
                }
            }
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);

        if (maxTotalSize < quota)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }
        var canEdit = isRoom ? folder.RootFolderType != FolderType.Archive && await fileSecurity.CanEditRoomAsync(folder)
            : await fileSecurity.CanRenameAsync(folder);

        if (!canEdit)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_RenameFolder);
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

        if (folder.SettingsQuota != quota)
        {
            var newFolderID = await folderDao.ChangeFolderQuotaAsync(folder, quota);
            folder = await folderDao.GetFolderAsync(newFolderID);
            folder.Access = folderAccess;
        }

        await socketManager.UpdateFolderAsync(folder);

        await webhookManager.PublishAsync(isRoom ? WebhookTrigger.RoomUpdated : WebhookTrigger.FolderUpdated, folder);

        return folder;
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
}