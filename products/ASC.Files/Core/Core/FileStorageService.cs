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

namespace ASC.Web.Files.Services.WCFService;

[Scope]
public class FileStorageService //: IFileStorageService
(
    Global global,
    GlobalStore globalStore,
    GlobalFolderHelper globalFolderHelper,
    FilesSettingsHelper filesSettingsHelper,
    AuthContext authContext,
    UserManager userManager,
    FileUtility fileUtility,
    FilesLinkUtility filesLinkUtility,
    BaseCommonLinkUtility baseCommonLinkUtility,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    ILoggerProvider optionMonitor,
    PathProvider pathProvider,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    FileMarker fileMarker,
    BreadCrumbsManager breadCrumbsManager,
    LockerManager lockerManager,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    DocumentServiceTrackerHelper documentServiceTrackerHelper,
    DocuSignToken docuSignToken,
    DocuSignHelper docuSignHelper,
    FileConverter fileConverter,
    DocumentServiceHelper documentServiceHelper,
    ThirdpartyConfiguration thirdpartyConfiguration,
    DocumentServiceConnector documentServiceConnector,
    FileSharing fileSharing,
    NotifyClient notifyClient,
    IUrlShortener urlShortener,
    IServiceProvider serviceProvider,
    ConsumerFactory consumerFactory,
    EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
    SettingsManager settingsManager,
    FileMarkAsReadOperationsManager fileOperationsManager,
    TenantManager tenantManager,
    FileTrackerHelper fileTracker,
    IEventBus eventBus,
    EntryStatusManager entryStatusManager,
    ThumbnailSettings thumbnailSettings,
    InvitationService invitationService,
    StudioNotifyService studioNotifyService,
    ExternalShare externalShare,
    RoomLogoManager roomLogoManager,
    CoreBaseSettings coreBaseSettings,
    IDistributedLockProvider distributedLockProvider,
    IHttpClientFactory clientFactory,
    TempStream tempStream,
    MentionWrapperCreator mentionWrapperCreator,
    SecurityContext securityContext,
    FileUtilityConfiguration fileUtilityConfiguration,
    FileChecker fileChecker,
    CommonLinkUtility commonLinkUtility,
    ShortUrl shortUrl,
    IDbContextFactory<UrlShortenerDbContext> dbContextFactory,
    WatermarkManager watermarkManager,
    CustomTagsService customTagsService,
    IMapper mapper,
    ICacheNotify<ClearMyFolderItem> notifyMyFolder,
    FormRoleDtoHelper formRoleDtoHelper,
    WebhookManager webhookManager,
    FileOperationsService fileOperationsService,
    FolderOperationsService folderOperationsService,
    SharingService sharingService)
{
    private readonly ILogger _logger = optionMonitor.CreateLogger("ASC.Files");

    public async Task<DataWrapper<T>> GetFolderItemsAsync<T>(
        T parentId,
        int from,
        int count,
        IEnumerable<FilterType> filterTypes,
        bool subjectGroup,
        string subject,
        string searchText,
        string[] extension,
        bool searchInContent,
        bool withSubfolders,
        OrderBy orderBy,
        SearchArea searchArea = SearchArea.Active,
        T roomId = default,
        bool withoutTags = false,
        IEnumerable<string> tagNames = null,
        bool excludeSubject = false,
        ProviderFilter provider = ProviderFilter.None,
        SubjectFilter subjectFilter = SubjectFilter.Owner,
        ApplyFilterOption applyFilterOption = ApplyFilterOption.All,
        QuotaFilter quotaFilter = QuotaFilter.All,
        StorageFilter storageFilter = StorageFilter.None,
        FormsItemDto formsItemDto = null)
    {
        var subjectId = string.IsNullOrEmpty(subject) ? Guid.Empty : new Guid(subject);

        var folderDao = daoFactory.GetFolderDao<T>();

        Folder<T> parent = null;
        Folder<T> parentRoom = null;

        try
        {
            parent = await folderDao.GetFolderAsync(parentId);

            if (parent == null)
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }

            if (parent != null && !string.IsNullOrEmpty(parent.Error))
            {
                throw new Exception(parent.Error);
            }

            if (parent == null)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }

            if (parent.RootFolderType == FolderType.VirtualRooms)
            {
                parentRoom = !DocSpaceHelper.IsRoom(parent.FolderType) && parent.FolderType != FolderType.VirtualRooms && !parent.ProviderEntry ? await folderDao.GetFirstParentTypeFromFileEntryAsync(parent) : parent;

                parent.ParentRoomType = parentRoom.FolderType;
            }

            if (parent.RootFolderType == FolderType.RoomTemplates)
            {
                parentRoom = !DocSpaceHelper.IsRoom(parent.FolderType) && parent.FolderType != FolderType.RoomTemplates && !parent.ProviderEntry ? await folderDao.GetFirstParentTypeFromFileEntryAsync(parent) : parent;

                parent.ParentRoomType = parentRoom.FolderType;
            }
        }
        catch (Exception e)
        {
            if (parent is { ProviderEntry: true })
            {
                throw GenerateException(new Exception(FilesCommonResource.ErrorMessage_SharpBoxException, e));
            }

            throw GenerateException(e);
        }

        if (!await fileSecurity.CanReadAsync(parent))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        if (parent.RootFolderType == FolderType.TRASH && !Equals(parent.Id, await globalFolderHelper.FolderTrashAsync))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        if (parent.FolderType is FolderType.FormFillingFolderDone or FolderType.FormFillingFolderInProgress)
        {
            if (parent.ShareRecord is { Share: FileShare.FillForms })
            {
                subjectId = authContext.CurrentAccount.ID;
            }
        }

        if (orderBy != null)
        {
            await filesSettingsHelper.SetDefaultOrder(orderBy);
        }
        else
        {
            orderBy = await filesSettingsHelper.GetDefaultOrder();
        }

        if (Equals(parent.Id, await globalFolderHelper.FolderShareAsync) && orderBy.SortedBy == SortedByType.DateAndTime)
        {
            orderBy.SortedBy = SortedByType.New;
        }

        searchArea = parent.FolderType switch
        {
            FolderType.Archive => SearchArea.Archive,
            FolderType.RoomTemplates => SearchArea.Templates,
            _ => searchArea
        };

        int total;
        IEnumerable<FileEntry> entries;

        try
        {
            (entries, total) = await entryManager.GetEntriesAsync(
                parent,
                parentRoom,
                from,
                count,
                filterTypes,
                subjectGroup,
                subjectId,
                searchText,
                extension,
                searchInContent,
                withSubfolders,
                orderBy,
                roomId,
                searchArea,
                withoutTags,
                tagNames,
                excludeSubject,
                provider,
                subjectFilter,
                applyFilterOption,
                quotaFilter,
                storageFilter,
                formsItemDto);
        }
        catch (Exception e)
        {
            if (parent.ProviderEntry)
            {
                throw GenerateException(new Exception(FilesCommonResource.ErrorMessage_SharpBoxException, e));
            }

            throw GenerateException(e);
        }

        var breadCrumbsTask = breadCrumbsManager.GetBreadCrumbsAsync(parentId, folderDao);
        var shareableTask = fileSharing.CanSetAccessAsync(parent);
        var newTask = fileMarker.GetRootFoldersIdMarkedAsNewAsync(parentId);
        var breadCrumbs = await breadCrumbsTask;

        var prevVisible = breadCrumbs.ElementAtOrDefault(breadCrumbs.Count - 2);
        if (prevVisible != null && !DocSpaceHelper.IsRoom(parent.FolderType) && prevVisible.FileEntryType == FileEntryType.Folder)
        {
            if (prevVisible is Folder<string> f1)
            {
                parent.ParentId = (T)Convert.ChangeType(f1.Id, typeof(T));
            }
            else if (prevVisible is Folder<int> f2)
            {
                parent.ParentId = (T)Convert.ChangeType(f2.Id, typeof(T));
            }
        }

        parent.Shareable =
            parent.FolderType == FolderType.SHARE ||
            parent.RootFolderType == FolderType.Privacy ||
            await shareableTask;

        entries = entries.ToAsyncEnumerable().WhereAwait(async x =>
        {
            if (x.FileEntryType == FileEntryType.Folder)
            {
                return true;
            }

            if (x is File<string> f1)
            {
                return !await fileConverter.IsConverting(f1);
            }

            return x is File<int> f2 && !await fileConverter.IsConverting(f2);
        }).ToEnumerable();

        if (parent.FolderType == FolderType.Recent && searchArea == SearchArea.RecentByLinks)
        {
            parent.Title = FilesUCResource.MyFiles;
        }

        var result = new DataWrapper<T>
        {
            Total = total,
            Entries = entries.ToList(),
            FolderPathParts =
            [
                ..breadCrumbs.Select(object (f) =>
                {
                    if (f.FileEntryType == FileEntryType.Folder)
                    {
                        switch (f)
                        {
                            case Folder<string> f1:
                                return new { f1.Id, f1.Title, RoomType = DocSpaceHelper.MapToRoomType(f1.FolderType) };
                            case Folder<int> f2:
                                {
                                    var title = f2.FolderType is FolderType.Recent && searchArea == SearchArea.RecentByLinks
                                        ? FilesUCResource.MyFiles
                                        : f2.Title;

                                    return new { f2.Id, title, RoomType = DocSpaceHelper.MapToRoomType(f2.FolderType) };
                                }
                        }
                    }

                    return 0;
                })
            ],
            FolderInfo = parent,
            New = await newTask,
            ParentRoom = parentRoom
        };

        return result;
    }

    public async Task<List<FileEntry>> GetItemsAsync<TId>(IEnumerable<TId> filesId, IEnumerable<TId> foldersId, FilterType filter, bool subjectGroup, Guid? subjectId = null, string search = "")
    {
        subjectId ??= Guid.Empty;

        var entries = AsyncEnumerable.Empty<FileEntry<TId>>();

        var folderDao = daoFactory.GetFolderDao<TId>();
        var fileDao = daoFactory.GetFileDao<TId>();

        entries = entries.Concat(fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)));
        entries = entries.Concat(fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)));
        entries = entryManager.FilterEntries(entries, filter, subjectGroup, subjectId.Value, search, true);

        var result = new List<FileEntry>();
        var files = new List<File<TId>>();
        var folders = new List<Folder<TId>>();

        await foreach (var fileEntry in entries)
        {
            if (fileEntry is File<TId> file)
            {
                if (fileEntry.RootFolderType == FolderType.USER
                    && !Equals(fileEntry.RootCreateBy, authContext.CurrentAccount.ID)
                    && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.FolderIdDisplay)))
                {
                    file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<TId>();
                }

                if (!Equals(file.Id, default(TId)))
                {
                    files.Add(file);
                }
            }
            else if (fileEntry is Folder<TId> folder)
            {
                if (fileEntry.RootFolderType == FolderType.USER
                    && !Equals(fileEntry.RootCreateBy, authContext.CurrentAccount.ID)
                    && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(folder.FolderIdDisplay)))
                {
                    folder.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<TId>();
                }

                if (!Equals(folder.Id, default(TId)))
                {
                    folders.Add(folder);
                }
            }

            result.Add(fileEntry);
        }

        var setFilesStatus = entryStatusManager.SetFileStatusAsync(files);
        var setFavorites = entryStatusManager.SetIsFavoriteFoldersAsync(folders);

        await Task.WhenAll(setFilesStatus, setFavorites);

        return result;
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
                        await ReOrderAsync(folder.Id, true, true);
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
                    var tags = tagsInfos.Select(tagInfo => Tag.Custom(Guid.Empty, folder, tagInfo.Name));

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

    public async Task<KeyValuePair<bool, string>> TrackEditFileAsync<T>(T fileId, Guid tabId, string docKeyForTrack, bool isFinish = false)
    {
        try
        {
            if (!authContext.IsAuthenticated && await externalShare.GetLinkIdAsync() == Guid.Empty)
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var (file, _) = await documentServiceHelper.GetCurFileInfoAsync(fileId, -1);

            if (docKeyForTrack != await documentServiceHelper.GetDocKeyAsync(fileId, -1, DateTime.MinValue) && docKeyForTrack != await documentServiceHelper.GetDocKeyAsync(file.Id, file.Version, file.ProviderEntry ? file.ModifiedOn : file.CreateOn))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (isFinish)
            {
                await fileTracker.RemoveAsync(fileId, tabId);
                await socketManager.StopEditAsync(fileId);
            }
            else
            {
                await entryManager.TrackEditingAsync(fileId, tabId, authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant());
            }

            return new KeyValuePair<bool, string>(true, string.Empty);
        }
        catch (Exception ex)
        {
            return new KeyValuePair<bool, string>(false, ex.Message);
        }
    }

    public async Task<File<T>> SaveEditingAsync<T>(T fileId, string fileExtension, string fileUri, Stream stream, bool forceSave = false)
    {
        try
        {
            if (!forceSave && await fileTracker.IsEditingAloneAsync(fileId))
            {
                await fileTracker.RemoveAsync(fileId);
                await socketManager.StopEditAsync(fileId);
            }

            var file = await entryManager.SaveEditingAsync(fileId, fileExtension, fileUri, stream, forceSave: forceSave ? ForcesaveType.User : ForcesaveType.None, keepLink: true);

            if (file != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileUpdated, file, file.Title);

                await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
            }

            return file;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    public async Task<File<T>> StartFillingAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var folder = await folderDao.GetFolderAsync(file.ParentId);

        if (folder.FolderType == FolderType.FillingFormsRoom && FileUtility.GetFileTypeByFileName(file.Title) == FileType.Pdf)
        {
            var ace = await fileSharing.GetPureSharesAsync(folder, new List<Guid> { authContext.CurrentAccount.ID }).FirstOrDefaultAsync();
            if (ace is { Access: FileShare.FillForms })
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
            }

            var properties = await fileDao.GetProperties(fileId) ?? new EntryProperties<T> { FormFilling = new FormFillingProperties<T>() };
            properties.FormFilling.StartFilling = true;
            properties.FormFilling.OriginalFormId = fileId;

            await fileDao.SaveProperties(fileId, properties);

            var count = await sharingService.GetPureSharesCountAsync(folder.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "");
            await socketManager.CreateFormAsync(file, securityContext.CurrentAccount.ID, count <= 1);
            await socketManager.CreateFileAsync(file);
        }

        return file;
    }

    public async Task<string> StartEditAsync<T>(T fileId, bool editingAlone = false)
    {
        try
        {
            if (editingAlone)
            {
                if (await fileTracker.IsEditingAsync(fileId))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFileTwice);
                }

                await entryManager.TrackEditingAsync(fileId, Guid.Empty, authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant(), true);

                //without StartTrack, track via old scheme
                return await documentServiceHelper.GetDocKeyAsync(fileId, -1, DateTime.MinValue);
            }

            var fileOptions = await documentServiceHelper.GetParamsAsync(fileId, -1, true, true, false, true);

            var configuration = fileOptions.Configuration;
            if (!configuration.EditorConfig.ModeWrite || !(configuration.Document.Permissions.Edit || configuration.Document.Permissions.ModifyFilter || configuration.Document.Permissions.Review
                                                           || configuration.Document.Permissions.FillForms || configuration.Document.Permissions.Comment))
            {
                throw new InvalidOperationException(!string.IsNullOrEmpty(configuration.Error) ? configuration.Error : FilesCommonResource.ErrorMessage_SecurityException_EditFile);
            }

            var key = configuration.Document.Key;

            if (!await documentServiceTrackerHelper.StartTrackAsync(fileId.ToString(), key))
            {
                throw new Exception(FilesCommonResource.ErrorMessage_StartEditing);
            }

            return key;
        }
        catch (Exception e)
        {
            await fileTracker.RemoveAsync(fileId);

            throw GenerateException(e);
        }
    }

    public async IAsyncEnumerable<File<T>> GetFileHistoryAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        var history = await fileDao.GetFileHistoryAsync(fileId).ToListAsync();

        var t1 = entryStatusManager.SetFileStatusAsync(history);
        var t2 = entryStatusManager.SetFormInfoAsync(history);
        await Task.WhenAll(t1, t2);

        foreach (var r in history)
        {
            yield return r;
        }
    }

    public async Task<KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>> UpdateToVersionAsync<T>(T fileId, int version)
    {
        var file = await entryManager.UpdateToVersionFileAsync(fileId, version);
        await filesMessageService.SendAsync(MessageAction.FileRestoreVersion, file, file.Title, version.ToString(CultureInfo.InvariantCulture));

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        await socketManager.UpdateFileAsync(file);

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        return new KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>(file, GetFileHistoryAsync(fileId));
    }

    public async Task<string> UpdateCommentAsync<T>(T fileId, int version, string comment)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId, version);
        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanEditHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (await lockerManager.FileLockedForMeAsync(file.Id))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        comment = await fileDao.UpdateCommentAsync(fileId, version, comment);

        await filesMessageService.SendAsync(MessageAction.FileUpdatedRevisionComment, file, [file.Title, version.ToString(CultureInfo.InvariantCulture)]);

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        return comment;
    }

    public async Task<KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>> CompleteVersionAsync<T>(T fileId, int version, bool continueVersion)
    {
        var file = await entryManager.CompleteVersionFileAsync(fileId, version, continueVersion);

        await filesMessageService.SendAsync(
            continueVersion ? MessageAction.FileDeletedVersion : MessageAction.FileCreatedVersion,
            file,
            file.Title, version == 0 ? (file.Version - 1).ToString(CultureInfo.InvariantCulture) : version.ToString(CultureInfo.InvariantCulture));

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        await socketManager.UpdateFileAsync(file);

        return new KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>(file, GetFileHistoryAsync(fileId));
    }

    public async Task<File<T>> LockFileAsync<T>(T fileId, bool lockfile)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanLockAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        var tags = tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.Locked);
        var tagLocked = await tags.FirstOrDefaultAsync();

        if (lockfile)
        {
            if (tagLocked == null)
            {
                tagLocked = new Tag("locked", TagType.Locked, authContext.CurrentAccount.ID).AddEntry(file);

                await tagDao.SaveTagsAsync(tagLocked);
            }

            var usersDrop = (await fileTracker.GetEditingByAsync(file.Id)).Where(uid => uid != authContext.CurrentAccount.ID).Select(u => u.ToString()).ToArray();
            if (usersDrop.Length > 0)
            {
                var docKey = await fileTracker.GetTrackerDocKey(file.Id);
                await documentServiceHelper.DropUserAsync(docKey, usersDrop, file.Id);
            }

            await filesMessageService.SendAsync(MessageAction.FileLocked, file, file.Title);
        }
        else
        {
            if (tagLocked != null)
            {
                await tagDao.RemoveTagsAsync(tagLocked);

                await filesMessageService.SendAsync(MessageAction.FileUnlocked, file, file.Title);
            }

            if (!file.ProviderEntry)
            {
                await UpdateCommentAsync(file.Id, file.Version, FilesCommonResource.UnlockComment);
            }
        }

        await entryStatusManager.SetFileStatusAsync(file);

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        await socketManager.UpdateFileAsync(file);

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        return file;
    }

    public async Task<File<T>> SetCustomFilterTagAsync<T>(T fileId, bool enabled)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        file.NotFoundIfNull();

        if (file.RootFolderType != FolderType.VirtualRooms || !fileUtility.CanWebCustomFilterEditing(file.Title))
        {
            throw new ArgumentException();
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await DocSpaceHelper.GetParentRoom(file, folderDao);

        if (room == null || !await fileSecurity.CanEditAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var tagCustomFilter = await tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.CustomFilter).FirstOrDefaultAsync();

        if (enabled)
        {
            if (tagCustomFilter == null)
            {
                tagCustomFilter = new Tag("customfilter", TagType.CustomFilter, authContext.CurrentAccount.ID).AddEntry(file);

                await tagDao.SaveTagsAsync(tagCustomFilter);

                var usersDrop = (await fileTracker.GetEditingByAsync(file.Id)).Where(uid => uid != authContext.CurrentAccount.ID).Select(u => u.ToString()).ToArray();
                if (usersDrop.Length > 0)
                {
                    var docKey = await fileTracker.GetTrackerDocKey(file.Id);
                    await documentServiceHelper.DropUserAsync(docKey, usersDrop, file.Id);
                }
            }

            await filesMessageService.SendAsync(MessageAction.FileCustomFilterEnabled, file, file.Title);
        }
        else
        {
            if (tagCustomFilter != null)
            {
                await tagDao.RemoveTagsAsync(tagCustomFilter);
            }

            await filesMessageService.SendAsync(MessageAction.FileCustomFilterDisabled, file, file.Title);
        }

        await entryStatusManager.SetFileStatusAsync(file);

        await socketManager.UpdateFileAsync(file);

        return file;
    }

    public async IAsyncEnumerable<EditHistory> GetEditHistoryAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.ProviderEntry)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        await foreach (var f in fileDao.GetEditHistoryAsync(documentServiceHelper, file.Id))
        {
            yield return f;
        }
    }

    public async Task<EditHistoryDataDto> GetEditDiffUrlAsync<T>(T fileId, int version = 0)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        var file = version > 0
            ? await fileDao.GetFileAsync(fileId, version)
            : await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.ProviderEntry)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var result = new EditHistoryDataDto { FileType = file.ConvertedExtension.Trim('.'), Key = await documentServiceHelper.GetDocKeyAsync(file), Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file)), Version = version };

        if (await fileDao.ContainChangesAsync(file.Id, file.Version))
        {
            string previousKey;
            string sourceFileUrl;
            string sourceExt;

            var history = await fileDao.GetFileHistoryAsync(file.Id).ToListAsync();
            var previousFileStable = history.OrderByDescending(r => r.Version).FirstOrDefault(r => r.Version < file.Version);
            if (previousFileStable != null)
            {
                sourceFileUrl = pathProvider.GetFileStreamUrl(previousFileStable);
                sourceExt = previousFileStable.ConvertedExtension;

                previousKey = await documentServiceHelper.GetDocKeyAsync(previousFileStable);
            }
            else
            {
                var culture = (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).GetCulture();
                var storeTemplate = await globalStore.GetStoreTemplateAsync();
                var fileExt = FileUtility.GetFileExtension(file.Title);
                var path = await globalStore.GetNewDocTemplatePath(storeTemplate, fileExt, culture);
                var uri = await storeTemplate.GetUriAsync("", path);

                sourceFileUrl = baseCommonLinkUtility.GetFullAbsolutePath(uri.ToString());
                sourceExt = fileExt.Trim('.');

                previousKey = DocumentServiceConnector.GenerateRevisionId(Guid.NewGuid().ToString());
            }

            result.Previous = new EditHistoryUrl { Key = previousKey, Url = documentServiceConnector.ReplaceCommunityAddress(sourceFileUrl), FileType = sourceExt.Trim('.') };

            result.ChangesUrl = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileChangesUrl(file));
        }

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    public async IAsyncEnumerable<EditHistory> RestoreVersionAsync<T>(T fileId, int version, string url = null)
    {
        File<T> file;
        if (string.IsNullOrEmpty(url))
        {
            file = await entryManager.UpdateToVersionFileAsync(fileId, version);
        }
        else
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var fromFile = await fileDao.GetFileAsync(fileId, version);
            var modifiedOnString = fromFile.ModifiedOnString;
            file = await entryManager.SaveEditingAsync(fileId, null, url, null, string.Format(FilesCommonResource.CommentRevertChanges, modifiedOnString));
        }

        await filesMessageService.SendAsync(MessageAction.FileRestoreVersion, file, file.Title, version.ToString(CultureInfo.InvariantCulture));

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        await foreach (var f in daoFactory.GetFileDao<T>().GetEditHistoryAsync(documentServiceHelper, file.Id))
        {
            yield return f;
        }
    }

    public async Task<FileLink> GetPresignedUriAsync<T>(T fileId)
    {
        var file = await fileOperationsService.GetFileAsync(fileId, -1);
        var result = new FileLink { FileType = FileUtility.GetFileExtension(file.Title), Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file)) };

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    public async Task<File<T>> SetFileOrder<T>(T fileId, int order)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        file.NotFoundIfNull();
        if (!await fileSecurity.CanEditAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var newOrder = await fileDao.SetCustomOrder(fileId, file.ParentId, order);
        if (newOrder != 0 && newOrder != file.Order)
        {
            file.Order = order;
            await filesMessageService.SendAsync(MessageAction.FileIndexChanged, file, file.Title, file.Order.ToString(), order.ToString());
            await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
        }

        return file;
    }

    public async IAsyncEnumerable<FileEntry<T>> SetOrderAsync<T>(List<OrdersItemRequestDto<T>> items)
    {
        var contextId = Guid.NewGuid().ToString();

        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var folders = await folderDao.GetFoldersAsync(items.Where(x => x.EntryType == FileEntryType.Folder).Select(x => x.EntryId))
            .ToDictionaryAsync(x => x.Id);

        var files = await fileDao.GetFilesAsync(items.Where(x => x.EntryType == FileEntryType.File).Select(x => x.EntryId))
            .ToDictionaryAsync(x => x.Id);

        foreach (var item in items)
        {
            FileEntry<T> entry = item.EntryType == FileEntryType.File ? files.Get(item.EntryId) : folders.Get(item.EntryId);
            entry.NotFoundIfNull();

            var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(entry);
            var room = await daoFactory.GetCacheFolderDao<T>().GetFolderAsync(roomId);

            if (!await fileSecurity.CanEditRoomAsync(room))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (!await fileSecurity.CanEditAsync(entry))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            switch (entry)
            {
                case File<T> file:
                    {
                        var newOrder = await fileDao.SetCustomOrder(file.Id, file.ParentId, item.Order);
                        if (newOrder != 0)
                        {
                            entry.Order = item.Order;
                            await filesMessageService.SendAsync(MessageAction.FileIndexChanged, file, file.Title, file.Order.ToString(), item.Order.ToString(), contextId);
                            await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
                        }

                        break;
                    }
                case Folder<T> folder:
                    {
                        var newOrder = await folderDao.SetCustomOrder(folder.Id, folder.ParentId, item.Order);
                        if (newOrder != 0)
                        {
                            entry.Order = item.Order;
                            await filesMessageService.SendAsync(MessageAction.FolderIndexChanged, folder, folder.Title, folder.Order.ToString(), item.Order.ToString(), contextId);
                            await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, folder);
                        }

                        break;
                    }
            }

            yield return entry;
        }
    }

    public async Task<Folder<T>> SetFolderOrder<T>(T folderId, int order)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        folder.NotFoundIfNull();
        if (!await fileSecurity.CanEditAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var newOrder = await folderDao.SetCustomOrder(folderId, folder.ParentId, order);

        if (newOrder != 0 && newOrder != folder.Order)
        {
            folder.Order = order;
            await filesMessageService.SendAsync(MessageAction.FolderIndexChanged, folder, folder.Title, folder.Order.ToString(), order.ToString());

            await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, folder);
        }

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
            throw GenerateException(e);
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
            throw GenerateException(e);
        }
    }

    public async Task<List<FileEntry>> GetNewItemsAsync<T>(T folderId)
    {
        try
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(folderId);

            var result = await fileMarker.MarkedItemsAsync(folder).Where(e => e.FileEntryType == FileEntryType.File).ToListAsync();

            result = [..await entryManager.SortEntries<T>(result, new OrderBy(SortedByType.DateAndTime, false))];

            if (result.Count == 0)
            {
                await fileOperationsManager.Publish([JsonSerializer.SerializeToElement(folderId)], []);
            }

            return result;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    public IAsyncEnumerable<ThirdPartyParams> GetThirdPartyAsync()
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return AsyncEnumerable.Empty<ThirdPartyParams>();
        }

        return InternalGetThirdPartyAsync(providerDao);
    }

    private static IAsyncEnumerable<ThirdPartyParams> InternalGetThirdPartyAsync(IProviderDao providerDao)
    {
        return providerDao.GetProvidersInfoAsync().Select(r => new ThirdPartyParams
        {
            CustomerTitle = r.CustomerTitle,
            Corporate = r.RootFolderType == FolderType.COMMON,
            RoomsStorage = r.RootFolderType is FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.Archive,
            ProviderId = r.ProviderId,
            ProviderKey = r.ProviderKey
        });
    }

    public async ValueTask<Folder<string>> GetBackupThirdPartyAsync()
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return null;
        }

        var providerInfo = await providerDao.GetProvidersInfoAsync(FolderType.ThirdpartyBackup).SingleOrDefaultAsync();

        if (providerInfo != null)
        {
            var folderDao = daoFactory.GetFolderDao<string>();
            var folder = await folderDao.GetFolderAsync(providerInfo.RootFolderId);
            if (!await fileSecurity.CanReadAsync(folder))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
            }

            return folder;
        }

        return null;
    }


    public async ValueTask<Folder<string>> SaveThirdPartyAsync(ThirdPartyParams thirdPartyParams)
    {
        var providerDao = daoFactory.ProviderDao;

        if (providerDao == null)
        {
            return null;
        }

        var folderDaoInt = daoFactory.GetFolderDao<int>();
        var folderDao = daoFactory.GetFolderDao<string>();

        if (thirdPartyParams == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        int folderId;
        FolderType folderType;

        if (thirdPartyParams.Corporate)
        {
            folderId = await globalFolderHelper.FolderCommonAsync;
            folderType = FolderType.COMMON;
        }
        else if (thirdPartyParams.RoomsStorage)
        {
            folderId = await globalFolderHelper.FolderVirtualRoomsAsync;
            folderType = FolderType.VirtualRooms;
        }
        else
        {
            folderId = await globalFolderHelper.FolderMyAsync;
            folderType = FolderType.USER;
        }

        var parentFolder = await folderDaoInt.GetFolderAsync(folderId);

        if (!await fileSecurity.CanCreateAsync(parentFolder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        if (!await filesSettingsHelper.GetEnableThirdParty())
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var currentFolderType = FolderType.USER;
        int currentProviderId;

        MessageAction messageAction;
        if (thirdPartyParams.ProviderId == null)
        {
            if (!thirdpartyConfiguration.SupportInclusion(daoFactory) || !await filesSettingsHelper.GetEnableThirdParty())
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            thirdPartyParams.CustomerTitle = Global.ReplaceInvalidCharsAndTruncate(thirdPartyParams.CustomerTitle);

            if (string.IsNullOrEmpty(thirdPartyParams.CustomerTitle))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_InvalidTitle);
            }

            try
            {
                currentProviderId = await providerDao.SaveProviderInfoAsync(thirdPartyParams.ProviderKey, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
                messageAction = MessageAction.ThirdPartyCreated;
            }
            catch (UnauthorizedAccessException e)
            {
                throw GenerateException(e, true);
            }
            catch (Exception e)
            {
                throw GenerateException(e.InnerException ?? e);
            }
        }
        else
        {
            currentProviderId = thirdPartyParams.ProviderId.Value;

            var currentProvider = await providerDao.GetProviderInfoAsync(currentProviderId);
            if (currentProvider.Owner != authContext.CurrentAccount.ID)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            currentFolderType = currentProvider.RootFolderType;

            switch (currentProvider.RootFolderType)
            {
                case FolderType.COMMON when !thirdPartyParams.Corporate:
                    {
                        var lostFolder = await folderDao.GetFolderAsync(currentProvider.RootFolderId);
                        await fileMarker.RemoveMarkAsNewForAllAsync(lostFolder);
                        break;
                    }
                case FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.Archive:
                    {
                        var updatedProvider = await providerDao.UpdateRoomProviderInfoAsync(new ProviderData { Id = currentProviderId, AuthData = thirdPartyParams.AuthData });
                        currentProviderId = updatedProvider.ProviderId;
                        break;
                    }
                default:
                    currentProviderId = await providerDao.UpdateProviderInfoAsync(currentProviderId, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
                    break;
            }

            messageAction = MessageAction.ThirdPartyUpdated;
        }

        var provider = await providerDao.GetProviderInfoAsync(currentProviderId);
        await provider.InvalidateStorageAsync();

        var folderDao1 = daoFactory.GetFolderDao<string>();
        var folder = await folderDao1.GetFolderAsync(provider.RootFolderId);
        if (!await fileSecurity.CanReadAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        await filesMessageService.SendAsync(messageAction, parentFolder, folder.Id, provider.ProviderKey);

        if (thirdPartyParams.Corporate && currentFolderType != FolderType.COMMON)
        {
            await fileMarker.MarkAsNewAsync(folder);
        }

        return folder;
    }

    public async ValueTask<Folder<string>> SaveThirdPartyBackupAsync(ThirdPartyParams thirdPartyParams)
    {
        var providerDao = daoFactory.ProviderDao;

        if (providerDao == null)
        {
            return null;
        }

        if (thirdPartyParams == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await filesSettingsHelper.GetEnableThirdParty())
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var folderType = FolderType.ThirdpartyBackup;

        int curProviderId;

        MessageAction messageAction;

        var thirdparty = await GetBackupThirdPartyAsync();
        if (thirdparty == null)
        {
            if (!thirdpartyConfiguration.SupportInclusion(daoFactory) || !await filesSettingsHelper.GetEnableThirdParty())
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            thirdPartyParams.CustomerTitle = Global.ReplaceInvalidCharsAndTruncate(thirdPartyParams.CustomerTitle);
            if (string.IsNullOrEmpty(thirdPartyParams.CustomerTitle))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_InvalidTitle);
            }

            try
            {
                curProviderId = await providerDao.SaveProviderInfoAsync(thirdPartyParams.ProviderKey, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
                messageAction = MessageAction.ThirdPartyCreated;
            }
            catch (UnauthorizedAccessException e)
            {
                throw GenerateException(e, true);
            }
            catch (Exception e)
            {
                throw GenerateException(e.InnerException ?? e);
            }
        }
        else
        {
            curProviderId = await providerDao.UpdateBackupProviderInfoAsync(thirdPartyParams.ProviderKey, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData);
            messageAction = MessageAction.ThirdPartyUpdated;
        }

        var provider = await providerDao.GetProviderInfoAsync(curProviderId);
        await provider.InvalidateStorageAsync();

        var folderDao1 = daoFactory.GetFolderDao<string>();
        var folder = await folderDao1.GetFolderAsync(provider.RootFolderId);

        filesMessageService.Send(messageAction, folder.Id, provider.ProviderKey);

        return folder;
    }

    public async ValueTask<string> DeleteThirdPartyAsync(string providerId)
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return null;
        }

        var curProviderId = Convert.ToInt32(providerId);
        var providerInfo = await providerDao.GetProviderInfoAsync(curProviderId);

        var folder = entryManager.GetFakeThirdpartyFolder(providerInfo);
        if (!await fileSecurity.CanDeleteAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder);
        }

        if (providerInfo.RootFolderType == FolderType.COMMON)
        {
            await fileMarker.RemoveMarkAsNewForAllAsync(folder);
        }

        await providerDao.RemoveProviderInfoAsync(folder.ProviderId);
        await filesMessageService.SendAsync(MessageAction.ThirdPartyDeleted, folder, folder.Id, providerInfo.ProviderKey);

        return folder.Id;
    }

    public async Task<bool> SaveDocuSignAsync(string code)
    {
        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(authContext.CurrentAccount.ID) || !await filesSettingsHelper.GetEnableThirdParty() || !thirdpartyConfiguration.SupportDocuSignInclusion)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var token = consumerFactory.Get<DocuSignLoginProvider>().GetAccessToken(code);
        await docuSignHelper.ValidateTokenAsync(token);
        await docuSignToken.SaveTokenAsync(token);

        return true;
    }

    public async Task DeleteDocuSignAsync()
    {
        await docuSignToken.DeleteTokenAsync();
    }

    public async Task<string> SendDocuSignAsync<T>(T fileId, DocuSignData docuSignData)
    {
        try
        {
            if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID) || !await filesSettingsHelper.GetEnableThirdParty() || !thirdpartyConfiguration.SupportDocuSignInclusion)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            return await docuSignHelper.SendDocuSignAsync(fileId, docuSignData);
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }


    #region MoveOrCopy

    public async Task<List<object>> MoveOrCopyDestFolderCheckAsync<T1>(IEnumerable<JsonElement> filesId, T1 destFolderId)
    {
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(filesId);

        var checkedFiles = new List<object>();

        var filesInts = await MoveOrCopyDestFolderCheckAsync(fileIntIds, destFolderId);

        foreach (var i in filesInts)
        {
            checkedFiles.Add(i);
        }

        var filesStrings = await MoveOrCopyDestFolderCheckAsync(fileStringIds, destFolderId);

        foreach (var i in filesStrings)
        {
            checkedFiles.Add(i);
        }

        return checkedFiles;
    }

    public async Task<(List<object>, List<object>)> MoveOrCopyFilesCheckAsync<T1>(IEnumerable<JsonElement> filesId, IEnumerable<JsonElement> foldersId, T1 destFolderId)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(foldersId);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(filesId);

        var checkedFiles = new List<object>();
        var checkedFolders = new List<object>();

        var (filesInts, folderInts) = await MoveOrCopyFilesCheckAsync(fileIntIds, folderIntIds, destFolderId);

        foreach (var i in filesInts)
        {
            checkedFiles.Add(i);
        }

        foreach (var i in folderInts)
        {
            checkedFolders.Add(i);
        }

        var (filesStrings, folderStrings) = await MoveOrCopyFilesCheckAsync(fileStringIds, folderStringIds, destFolderId);

        foreach (var i in filesStrings)
        {
            checkedFiles.Add(i);
        }

        foreach (var i in folderStrings)
        {
            checkedFolders.Add(i);
        }

        return (checkedFiles, checkedFolders);
    }

    private async Task<List<TFrom>> MoveOrCopyDestFolderCheckAsync<TFrom, TTo>(IEnumerable<TFrom> filesId, TTo destFolderId)
    {
        var checkedFiles = new List<TFrom>();

        var destFolderDao = daoFactory.GetFolderDao<TTo>();
        var fileDao = daoFactory.GetFileDao<TFrom>();

        var toRoom = await destFolderDao.GetFolderAsync(destFolderId);

        if (!DocSpaceHelper.IsRoom(toRoom.FolderType))
        {
            var (roomId, _) = await destFolderDao.GetParentRoomInfoFromFileEntryAsync(toRoom);
            toRoom = await destFolderDao.GetFolderAsync(roomId);
        }

        if (toRoom.FolderType == FolderType.FillingFormsRoom)
        {
            foreach (var id in filesId)
            {
                var file = await fileDao.GetFileAsync(id);
                var fileType = FileUtility.GetFileTypeByFileName(file.Title);

                if (fileType == FileType.Pdf && await fileChecker.CheckExtendedPDF(file))
                {
                    checkedFiles.Add(id);
                }
            }
        }
        else
        {
            checkedFiles.AddRange(filesId);
            return checkedFiles;
        }

        return checkedFiles;
    }

    private async Task<(List<TFrom>, List<TFrom>)> MoveOrCopyFilesCheckAsync<TFrom, TTo>(IEnumerable<TFrom> filesId, IEnumerable<TFrom> foldersId, TTo destFolderId)
    {
        var checkedFiles = new List<TFrom>();
        var checkedFolders = new List<TFrom>();
        var folderDao = daoFactory.GetFolderDao<TFrom>();
        var fileDao = daoFactory.GetFileDao<TFrom>();
        var destFolderDao = daoFactory.GetFolderDao<TTo>();
        var destFileDao = daoFactory.GetFileDao<TTo>();

        var toFolder = await destFolderDao.GetFolderAsync(destFolderId);
        if (toFolder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanCreateAsync(toFolder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        foreach (var id in filesId)
        {
            var file = await fileDao.GetFileAsync(id);
            if (file is { Encrypted: false }
                && await destFileDao.IsExistAsync(file.Title, file.Category, toFolder.Id))
            {
                checkedFiles.Add(id);
            }
        }

        var folders = folderDao.GetFoldersAsync(foldersId);
        var foldersProject = folders.Where(folder => folder.FolderType == FolderType.BUNCH);
        var toSubfolders = destFolderDao.GetFoldersAsync(toFolder.Id);

        await foreach (var folderProject in foldersProject)
        {
            var toSub = await toSubfolders.FirstOrDefaultAsync(to => Equals(to.Title, folderProject.Title));
            if (toSub == null)
            {
                continue;
            }

            var filesPr = fileDao.GetFilesAsync(folderProject.Id).ToListAsync();
            var foldersTmp = folderDao.GetFoldersAsync(folderProject.Id);
            var foldersPr = foldersTmp.Select(d => d.Id).ToListAsync();

            var (cFiles, cFolders) = await MoveOrCopyFilesCheckAsync(await filesPr, await foldersPr, toSub.Id);
            checkedFiles.AddRange(cFiles);
            checkedFolders.AddRange(cFolders);
        }

        try
        {
            foreach (var pair in await folderDao.CanMoveOrCopyAsync(foldersId, toFolder.Id))
            {
                checkedFolders.Add(pair.Key);
            }
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }

        return (checkedFiles, checkedFolders);
    }

    #endregion


    public async Task<(List<int>, List<int>)> GetTrashContentAsync()
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var trashId = await folderDao.GetFolderIDTrashAsync(true);
        var foldersIdTask = await folderDao.GetFoldersAsync(trashId).Select(f => f.Id).ToListAsync();
        var filesIdTask = await fileDao.GetFilesAsync(trashId).ToListAsync();

        return (foldersIdTask, filesIdTask);
    }

    public async IAsyncEnumerable<FileOperationResult> CheckConversionAsync<T>(List<CheckConversionRequestDto<T>> filesInfoJson, bool sync = false)
    {
        if (filesInfoJson == null || filesInfoJson.Count == 0)
        {
            yield break;
        }

        var results = AsyncEnumerable.Empty<FileOperationResult>();
        var fileDao = daoFactory.GetFileDao<T>();
        var files = new List<KeyValuePair<File<T>, bool>>();
        foreach (var fileInfo in filesInfoJson)
        {
            var file = fileInfo.Version > 0
                ? await fileDao.GetFileAsync(fileInfo.FileId, fileInfo.Version)
                : await fileDao.GetFileAsync(fileInfo.FileId);

            if (file == null)
            {
                var newFile = serviceProvider.GetService<File<T>>();
                newFile.Id = fileInfo.FileId;
                newFile.Version = fileInfo.Version;

                files.Add(new KeyValuePair<File<T>, bool>(newFile, true));

                continue;
            }

            if (!await fileSecurity.CanConvertAsync(file))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
            }

            if (fileInfo.StartConvert && fileConverter.MustConvert(file))
            {
                try
                {
                    if (sync)
                    {
                        results = results.Append(await fileConverter.ExecSynchronouslyAsync(file, !fileInfo.CreateNewIfExist, fileInfo.OutputType));
                    }
                    else
                    {
                        await fileConverter.ExecAsynchronouslyAsync(file, false, !fileInfo.CreateNewIfExist, fileInfo.Password, fileInfo.OutputType);
                    }
                }
                catch (Exception e)
                {
                    throw GenerateException(e);
                }
            }

            files.Add(new KeyValuePair<File<T>, bool>(file, false));
        }

        if (!sync)
        {
            results = fileConverter.GetStatusAsync(files);
        }

        await foreach (var res in results)
        {
            yield return res;
        }
    }

    public async Task<string> CheckFillFormDraftAsync<T>(T fileId, int version, bool editPossible, bool view)
    {
        var (file, configuration, _) = await documentServiceHelper.GetParamsAsync(fileId, version, editPossible, !view, true, editPossible);
        var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);

        var linkId = await externalShare.GetLinkIdAsync();
        if (linkId != Guid.Empty)
        {
            configuration.Document.SharedLinkKey += externalShare.GetKey();
        }

        if (configuration.EditorConfig.ModeWrite
            && fileUtility.CanWebRestrictedEditing(file.Title)
            && await fileSecurity.CanFillFormsAsync(file)
            && !await fileSecurity.CanEditAsync(file)
            && (properties != null && properties.FormFilling.StartFilling))
        {
            if (!await entryManager.LinkedForMeAsync(file))
            {
                await fileMarker.RemoveMarkAsNewAsync(file);

                Folder<T> folderIfNew;
                File<T> form;
                try
                {
                    (form, folderIfNew) = await entryManager.GetFillFormDraftAsync(file, file.ParentId);
                }
                catch (Exception ex)
                {
                    _logger.ErrorDocEditor(ex);
                    throw;
                }

                var comment = folderIfNew == null
                    ? string.Empty
                    : "#message/" + HttpUtility.UrlEncode(string.Format(FilesCommonResource.MessageFillFormDraftCreated, folderIfNew.Title));

                await socketManager.StopEditAsync(fileId);
                return filesLinkUtility.GetFileWebEditorUrl(form.Id) + comment;
            }

            if (!await entryManager.CheckFillFormDraftAsync(file))
            {
                var comment = "#message/" + HttpUtility.UrlEncode(FilesCommonResource.MessageFillFormDraftDiscard);

                return filesLinkUtility.GetFileWebEditorUrl(file.Id) + comment;
            }
        }

        return filesLinkUtility.GetFileWebEditorUrl(file.Id);
    }

    #region [Reassign|Delete] Data Manager

    public async Task DemandPermissionToReassignDataAsync(Guid userFromId, Guid userToId)
    {
        await DemandPermissionToDeletePersonalDataAsync(userFromId);

        //check exist userTo
        var userTo = await userManager.GetUsersAsync(userToId);
        if (Equals(userTo, Constants.LostUser))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UserNotFound);
        }

        //check user can have personal data
        if (await userManager.IsGuestAsync(userTo))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }
    }

    private async Task DemandPermissionToDeletePersonalDataAsync(Guid userFromId)
    {
        var userFrom = await userManager.GetUsersAsync(userFromId);

        await DemandPermissionToDeletePersonalDataAsync(userFrom);
    }

    public async Task DemandPermissionToDeletePersonalDataAsync(UserInfo userFrom)
    {
        //check current user have access
        if (!await global.IsDocSpaceAdministratorAsync)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        //check exist userFrom
        if (Equals(userFrom, Constants.LostUser))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UserNotFound);
        }
    }

    public async Task<List<T>> GetPersonalFolderIdsAsync<T>(Guid userId)
    {
        var result = new List<T>();

        var folderDao = daoFactory.GetFolderDao<T>();
        if (folderDao == null)
        {
            return result;
        }

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userId);
        if (!Equals(folderIdMy, 0))
        {
            result.Add(folderIdMy);
        }

        var folderIdTrash = await folderDao.GetFolderIDTrashAsync(false, userId);
        if (!Equals(folderIdTrash, 0))
        {
            result.Add(folderIdTrash);
        }

        return result;
    }

    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        return await folderDao.GetFilesUsedSpace();
    }


    public async Task<bool> AnyRoomsAsync(Guid user)
    {
        var any = (await GetFolderItemsAsync(
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

    public async Task ReassignRoomsAsync(Guid user, Guid? reassign)
    {
        var rooms = (await GetFolderItemsAsync(
                    await globalFolderHelper.GetFolderVirtualRooms(),
                    0,
                    -1,
                    new List<FilterType>() { FilterType.FoldersOnly },
                    false,
                    user.ToString(),
                    "",
                    [],
                    false,
                    false,
                    null)).Entries;

        var ids = rooms.Where(r => r is Folder<int>).Select(e => ((Folder<int>)e).Id);
        var thirdIds = rooms.Where(r => r is Folder<string>).Select(e => ((Folder<string>)e).Id);

        await ChangeOwnerAsync(ids, [], reassign ?? securityContext.CurrentAccount.ID, FileShare.ContentCreator).ToListAsync();
        await ChangeOwnerAsync(thirdIds, [], reassign ?? securityContext.CurrentAccount.ID, FileShare.ContentCreator).ToListAsync();
    }

    public async Task<IEnumerable<FileEntry>> GetSharedFilesAsync(Guid user)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        var my = await folderDao.GetFolderIDUserAsync(false, user);
        if (my == 0)
        {
            return [];
        }
        var shared = await fileDao.GetFilesAsync(my, null, default, false, Guid.Empty, string.Empty, null, false, true, withShared: true).Where(q => q.Shared).ToListAsync();

        return shared;
    }

    public async Task MoveSharedFilesAsync(Guid user, Guid toUser)
    {
        var initUser = securityContext.CurrentAccount.ID;

        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        var my = await folderDao.GetFolderIDUserAsync(false, user);
        if (my == 0)
        {
            return;
        }

        var shared = await fileDao.GetFilesAsync(my, null, default, false, Guid.Empty, string.Empty, null, false, true, withShared: true).Where(q => q.Shared).ToListAsync();

        await securityContext.AuthenticateMeWithoutCookieAsync(toUser);
        if (shared.Count > 0)
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(toUser);
            var userInfo = await userManager.GetUsersAsync(user, false);
            var folder = await folderOperationsService.CreateFolderAsync(await globalFolderHelper.FolderMyAsync, $"Documents of user {userInfo.FirstName} {userInfo.LastName}");
            foreach (var file in shared)
            {
                await socketManager.DeleteFileAsync(file, action: async () => await fileDao.MoveFileAsync(file.Id, folder.Id));
                await socketManager.CreateFileAsync(file);
            }
            var ids = shared.Select(s => s.Id).ToList();
            await DeleteFromRecentAsync([], ids, true);
            await fileDao.ReassignFilesAsync(toUser, ids);
        }

        await securityContext.AuthenticateMeWithoutCookieAsync(initUser);
    }

    public async Task DeletePersonalDataAsync(Guid userFromId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var linkDao = daoFactory.GetLinkDao<int>();

        if (folderDao == null || fileDao == null || linkDao == null)
        {
            return;
        }

        _logger.InformationDeletePersonalData(userFromId);

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userFromId);
        var folderIdTrash = await folderDao.GetFolderIDTrashAsync(false, userFromId);

        if (!Equals(folderIdMy, 0))
        {
            var fileIdsFromMy = await fileDao.GetFilesAsync(folderIdMy).ToListAsync();
            var folderIdsFromMy = await folderDao.GetFoldersAsync(folderIdMy).ToListAsync();

            await DeleteFilesAsync(fileIdsFromMy, folderIdTrash);
            await DeleteFoldersAsync(folderIdsFromMy, folderIdTrash);

            await folderDao.DeleteFolderAsync(folderIdMy);
        }

        if (!Equals(folderIdTrash, 0))
        {
            var fileIdsFromTrash = await fileDao.GetFilesAsync(folderIdTrash).ToListAsync();
            var folderIdsFromTrash = await folderDao.GetFoldersAsync(folderIdTrash).ToListAsync();

            await DeleteFilesAsync(fileIdsFromTrash, folderIdTrash);
            await DeleteFoldersAsync(folderIdsFromTrash, folderIdTrash);

            await folderDao.DeleteFolderAsync(folderIdTrash);
        }

        await fileSecurity.RemoveSubjectAsync(userFromId, true);
        return;
    }

    public async Task UpdatePersonalFolderModified(Guid userId)
    {
        await DemandPermissionToDeletePersonalDataAsync(userId);

        var folderDao = daoFactory.GetFolderDao<int>();

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userId);
        if (folderIdMy == 0)
        {
            return;
        }

        var my = await folderDao.GetFolderAsync(folderIdMy);
        await folderDao.SaveFolderAsync(my);
    }

    public async Task DeletePersonalFolderAsync(Guid userId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userId);
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var linkDao = daoFactory.GetLinkDao<int>();

        if (folderDao == null || fileDao == null || linkDao == null)
        {
            return;
        }

        _logger.InformationDeletePersonalData(userId);

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userId);
        var my = await folderDao.GetFolderAsync(folderIdMy);
        var folderIdTrash = await folderDao.GetFolderIDTrashAsync(false, userId);

        if (!Equals(folderIdMy, 0))
        {
            var fileIdsFromMy = await fileDao.GetFilesAsync(folderIdMy).ToListAsync();
            var folderIdsFromMy = await folderDao.GetFoldersAsync(folderIdMy).ToListAsync();

            await DeleteFilesAsync(fileIdsFromMy, folderIdTrash);
            await DeleteFoldersAsync(folderIdsFromMy, folderIdTrash);

            await socketManager.DeleteFolder(my, action: async () => await folderDao.DeleteFolderAsync(folderIdMy));

            var cacheKey = $"my/{tenantManager.GetCurrentTenantId()}/{userId}";
            await notifyMyFolder.PublishAsync(new ClearMyFolderItem { Key = cacheKey }, CacheNotifyAction.Remove);
        }
        return;
    }

    private async Task DeleteFilesAsync<T>(IEnumerable<T> fileIds, T folderIdTrash)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var linkDao = daoFactory.GetLinkDao<T>();

        foreach (var fileId in fileIds)
        {
            var file = await fileDao.GetFileAsync(fileId);

            await fileMarker.RemoveMarkAsNewForAllAsync(file);

            await socketManager.DeleteFileAsync(file, action: async () => await fileDao.DeleteFileAsync(file.Id, file.GetFileQuotaOwner()));

            if (file.RootFolderType == FolderType.TRASH && !Equals(folderIdTrash, 0))
            {
                await folderDao.ChangeTreeFolderSizeAsync(folderIdTrash, (-1) * file.ContentLength);
            }

            await linkDao.DeleteAllLinkAsync(file.Id);

            await fileDao.SaveProperties(file.Id, null);
        }
    }

    private async Task DeleteFoldersAsync<T>(IEnumerable<Folder<T>> folders, T folderIdTrash)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        foreach (var folder in folders)
        {
            await fileMarker.RemoveMarkAsNewForAllAsync(folder);

            var files = await fileDao.GetFilesAsync(folder.Id).ToListAsync();
            await DeleteFilesAsync(files, folderIdTrash);

            var subfolders = await folderDao.GetFoldersAsync(folder.Id).ToListAsync();
            await DeleteFoldersAsync(subfolders, folderIdTrash);

            if (await folderDao.IsEmptyAsync(folder.Id))
            {
                await socketManager.DeleteFolder(folder, action: async () => await folderDao.DeleteFolderAsync(folder.Id));
            }
        }
    }

    public async Task ReassignProvidersAsync(Guid userFromId, Guid userToId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return;
        }

        //move thirdParty storage userFrom
        await foreach (var commonProviderInfo in providerDao.GetProvidersInfoAsync(userFromId))
        {
            _logger.InformationReassignProvider(commonProviderInfo.ProviderId, userFromId, userToId);
            await providerDao.UpdateProviderInfoAsync(commonProviderInfo.ProviderId, null, null, FolderType.DEFAULT, userToId);
        }
    }

    public async Task ReassignRoomsFoldersAsync(Guid userFromId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        if (daoFactory.GetFolderDao<int>() is not FolderDao folderDao)
        {
            return;
        }

        await folderDao.ReassignRoomFoldersAsync(userFromId);

        var folderIdVirtualRooms = await folderDao.GetFolderIDVirtualRooms(false);
        var folderVirtualRooms = await folderDao.GetFolderAsync(folderIdVirtualRooms);

        await fileMarker.RemoveMarkAsNewAsync(folderVirtualRooms, userFromId);
    }

    public async Task ReassignFoldersAsync<T>(Guid userFromId, Guid userToId, IEnumerable<T> exceptFolderIds, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        if (folderDao == null)
        {
            return;
        }

        _logger.InformationReassignFolders(userFromId, userToId);

        await folderDao.ReassignFoldersAsync(userFromId, userToId, exceptFolderIds);

        var folderIdVirtualRooms = await folderDao.GetFolderIDVirtualRooms(false);
        var folderVirtualRooms = await folderDao.GetFolderAsync(folderIdVirtualRooms);

        await fileMarker.RemoveMarkAsNewAsync(folderVirtualRooms, userFromId);
    }

    public async Task ReassignFilesAsync<T>(Guid userFromId, Guid userToId, IEnumerable<T> exceptFolderIds, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var fileDao = daoFactory.GetFileDao<T>();
        if (fileDao == null)
        {
            return;
        }

        _logger.InformationReassignFiles(userFromId, userToId);

        await fileDao.ReassignFilesAsync(userFromId, userToId, exceptFolderIds);
    }

    public async Task ReassignRoomsFilesAsync(Guid userFromId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        if (daoFactory.GetFileDao<int>() is not FileDao fileDao)
        {
            return;
        }

        await fileDao.ReassignRoomsFilesAsync(userFromId);
    }

    #endregion

    #region Favorites Manager

    public async Task<bool> ToggleFileFavoriteAsync<T>(T fileId, bool favorite)
    {
        if (favorite)
        {
            await AddToFavoritesAsync(new List<T>(0), new List<T>(1) { fileId });
        }
        else
        {
            await DeleteFavoritesAsync(new List<T>(0), new List<T>(1) { fileId });
        }

        return favorite;
    }

    public async ValueTask<List<FileEntry<T>>> AddToFavoritesAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId).Where(file => !file.Encrypted)).ToListAsync();
        var folders = fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)).ToListAsync();

        List<FileEntry<T>> entries = [];

        foreach (var items in await Task.WhenAll(files.AsTask(), folders.AsTask()))
        {
            entries.AddRange(items);
        }

        var tags = entries.Select(entry => Tag.Favorite(authContext.CurrentAccount.ID, entry));

        await tagDao.SaveTagsAsync(tags);

        foreach (var entry in entries)
        {
            await filesMessageService.SendAsync(MessageAction.FileMarkedAsFavorite, entry, entry.Title);
        }

        return entries;
    }

    public async Task DeleteFavoritesAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)).ToListAsync();
        var folders = fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)).ToListAsync();

        List<FileEntry<T>> entries = [];

        foreach (var items in await Task.WhenAll(files.AsTask(), folders.AsTask()))
        {
            entries.AddRange(items);
        }

        var tags = entries.Select(entry => Tag.Favorite(authContext.CurrentAccount.ID, entry));

        await tagDao.RemoveTagsAsync(tags);

        foreach (var entry in entries)
        {
            await filesMessageService.SendAsync(MessageAction.FileRemovedFromFavorite, entry, entry.Title);
        }
    }

    #endregion

    #region Templates Manager

    public async ValueTask<List<FileEntry<T>>> AddToTemplatesAsync<T>(IEnumerable<T> filesId)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var files = await fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId))
            .Where(file => fileUtility.ExtsWebTemplate.Contains(FileUtility.GetFileExtension(file.Title), StringComparer.CurrentCultureIgnoreCase))
            .ToListAsync();

        var tags = files.Select(file => Tag.Template(authContext.CurrentAccount.ID, file));

        await tagDao.SaveTagsAsync(tags);

        return files;
    }

    public async Task DeleteTemplatesAsync<T>(IEnumerable<T> filesId)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var files = await fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)).ToListAsync();

        var tags = files.Select(file => Tag.Template(authContext.CurrentAccount.ID, file));

        await tagDao.RemoveTagsAsync(tags);
    }

    public async Task DeleteFromRecentAsync<T>(List<T> foldersIds, List<T> filesIds, bool recentByLinks)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var tagDao = daoFactory.GetTagDao<T>();

        var entries = new List<FileEntry<T>>(foldersIds.Count + filesIds.Count);

        var folders = folderDao.GetFoldersAsync(foldersIds).Cast<FileEntry<T>>().ToListAsync().AsTask();
        var files = fileDao.GetFilesAsync(filesIds).Cast<FileEntry<T>>().ToListAsync().AsTask();

        foreach (var items in await Task.WhenAll(folders, files))
        {
            entries.AddRange(items);
        }

        var tags = recentByLinks
            ? await tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.RecentByLink, entries).ToListAsync()
            : entries.Select(f => Tag.Recent(authContext.CurrentAccount.ID, f));

        await tagDao.RemoveTagsAsync(tags);

        var users = new[] { authContext.CurrentAccount.ID };

        var tasks = new List<Task>(entries.Count);

        foreach (var e in entries)
        {
            switch (e)
            {
                case File<T> file:
                    tasks.Add(socketManager.DeleteFileAsync(file, users: users));
                    break;
                case Folder<T> folder:
                    tasks.Add(socketManager.DeleteFolder(folder, users: users));
                    break;
            }
        }

        await Task.WhenAll(tasks);
    }

    public async IAsyncEnumerable<FileEntry<T>> GetTemplatesAsync<T>(FilterType filter, int from, int count, bool subjectGroup, Guid? subjectId, string searchText, string[] extension,
        bool searchInContent)
    {
        subjectId ??= Guid.Empty;
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var result = entryManager.GetTemplatesAsync(folderDao, fileDao, filter, subjectGroup, subjectId.Value, searchText, extension, searchInContent);

        await foreach (var r in result.Skip(from).Take(count))
        {
            yield return r;
        }
    }

    #endregion

    
    public async IAsyncEnumerable<AceWrapper> GetRoomSharedInfoAsync<T>(T roomId, IEnumerable<Guid> subjects)
    {
        var room = await daoFactory.GetFolderDao<T>().GetFolderAsync(roomId).NotFoundIfNull();

        await foreach (var ace in fileSharing.GetPureSharesAsync(room, subjects))
        {
            yield return ace;
        }
    }

    public async Task<List<MentionWrapper>> GetInfoUsersAsync(List<Guid> userIds)
    {
        if (!authContext.IsAuthenticated)
        {
            return null;
        }

        var users = new List<MentionWrapper>();

        foreach (var uid in userIds)
        {
            var user = await userManager.GetUsersAsync(uid);
            if (user.Id.Equals(Constants.LostUser.Id))
            {
                continue;
            }

            users.Add(await mentionWrapperCreator.CreateMentionWrapperAsync(user));
        }

        return users;
    }



    public async Task<File<T>> SaveAsPdf<T>(T fileId, T folderId, string title)
    {
        try
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var folderDao = daoFactory.GetFolderDao<T>();

            var file = await fileDao.GetFileAsync(fileId);
            file.NotFoundIfNull();
            if (!await fileSecurity.CanReadAsync(file))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var folder = await folderDao.GetFolderAsync(folderId);
            folder.NotFoundIfNull();
            if (!await fileSecurity.CanCreateAsync(folder))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var fileUri = pathProvider.GetFileStreamUrl(file);
            var fileExtension = file.ConvertedExtension;
            var docKey = await documentServiceHelper.GetDocKeyAsync(file);

            fileUri = documentServiceConnector.ReplaceCommunityAddress(fileUri);

            var (_, convertedDocumentUri, _) = await documentServiceConnector.GetConvertedUriAsync(fileUri, fileExtension, "pdf", docKey, null, CultureInfo.CurrentUICulture.Name, null, null, null, false, false);

            var pdfFile = serviceProvider.GetService<File<T>>();
            pdfFile.Title = !string.IsNullOrEmpty(title) ? $"{title}.pdf" : FileUtility.ReplaceFileExtension(file.Title, "pdf");
            pdfFile.ParentId = folder.Id;
            pdfFile.Comment = FilesCommonResource.CommentCreate;

            var request = new HttpRequestMessage { RequestUri = new Uri(convertedDocumentUri) };

            var httpClient = clientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            await using var fileStream = await response.Content.ReadAsStreamAsync();
            File<T> result;

            if (fileStream.CanSeek)
            {
                pdfFile.ContentLength = fileStream.Length;
                result = await fileDao.SaveFileAsync(pdfFile, fileStream);
            }
            else
            {
                var (buffered, isNew) = await tempStream.TryGetBufferedAsync(fileStream);
                try
                {
                    pdfFile.ContentLength = buffered.Length;
                    result = await fileDao.SaveFileAsync(pdfFile, buffered);
                }
                finally
                {
                    if (isNew)
                    {
                        await buffered.DisposeAsync();
                    }
                }
            }

            if (result != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileCreated, result, result.Title);
                await fileMarker.MarkAsNewAsync(result);
                await socketManager.CreateFileAsync(result);
                await webhookManager.PublishAsync(WebhookTrigger.FileCreated, result);
            }

            return result;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }
    
    public Task<List<MentionWrapper>> SharedUsersAsync<T>(T fileId)
    {
        if (!authContext.IsAuthenticated)
        {
            return Task.FromResult<List<MentionWrapper>>(null);
        }

        return InternalSharedUsersAsync(fileId);
    }
    
    
    
    public async Task<FileReference> GetReferenceDataAsync<T>(string fileId, string portalName, T sourceFileId, string path, string link)
    {
        File<T> file = null;
        var fileDao = daoFactory.GetFileDao<T>();
        if (portalName == tenantManager.GetCurrentTenantId().ToString())
        {
            file = await fileDao.GetFileAsync((T)Convert.ChangeType(fileId, typeof(T)));
        }

        if (file == null && !string.IsNullOrEmpty(path) && string.IsNullOrEmpty(link))
        {
            var source = await fileDao.GetFileAsync(sourceFileId);

            if (source == null)
            {
                return new FileReference { Error = FilesCommonResource.ErrorMessage_FileNotFound };
            }

            if (!await fileSecurity.CanReadAsync(source))
            {
                return new FileReference { Error = FilesCommonResource.ErrorMessage_SecurityException_ReadFile };
            }

            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(source.ParentId);
            if (!await fileSecurity.CanReadAsync(folder))
            {
                return new FileReference { Error = FilesCommonResource.ErrorMessage_SecurityException_ReadFolder };
            }

            var list = fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, path, null, false);
            file = await list.FirstOrDefaultAsync(fileItem => fileItem.Title == path);
        }

        if (file == null && !string.IsNullOrEmpty(link))
        {
            if (!link.StartsWith(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath)))
            {
                return new FileReference { Url = link };
            }

            var start = commonLinkUtility.ServerRootPath + "/s/";
            if (link.StartsWith(start))
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();
                var decode = shortUrl.Decode(link[start.Length..]);
                var sl = await context.ShortLinks.FindAsync(decode);
                if (sl != null)
                {
                    link = sl.Link;
                }
            }

            var url = new UriBuilder(link);
            var id = HttpUtility.ParseQueryString(url.Query)[FilesLinkUtility.FileId];
            if (!string.IsNullOrEmpty(id))
            {
                if (fileId is string)
                {
                    var dao = daoFactory.GetFileDao<string>();
                    file = await dao.GetFileAsync(id) as File<T>;
                }
                else
                {
                    if (int.TryParse(id, out var resultId))
                    {
                        var dao = daoFactory.GetFileDao<int>();
                        file = await dao.GetFileAsync(resultId) as File<T>;
                    }
                }
            }
        }

        if (file == null)
        {
            return new FileReference { Error = FilesCommonResource.ErrorMessage_FileNotFound };
        }

        if (!await fileSecurity.CanReadAsync(file))
        {
            return new FileReference { Error = FilesCommonResource.ErrorMessage_SecurityException_ReadFile };
        }

        var fileStable = file;
        if (file.Forcesave != ForcesaveType.None)
        {
            fileStable = await fileDao.GetFileStableAsync(file.Id, file.Version);
        }

        var docKey = await documentServiceHelper.GetDocKeyAsync(fileStable);

        var fileReference = new FileReference
        {
            Path = file.Title,
            ReferenceData = new FileReferenceData { FileKey = file.Id.ToString(), InstanceId = (tenantManager.GetCurrentTenantId()).ToString() },
            Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file, lastVersion: true)),
            FileType = file.ConvertedExtension.Trim('.'),
            Key = docKey,
            Link = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id))
        };
        fileReference.Token = documentServiceHelper.GetSignature(fileReference);
        return fileReference;
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

    private async Task<List<MentionWrapper>> InternalSharedUsersAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        FileEntry<T> file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        
        var usersIdWithAccess = await WhoCanRead(file);
        var links = await fileSecurity.GetPureSharesAsync(file, ShareFilterType.Link, null, null)
            .Select(x => x.Subject).ToHashSetAsync();

        var users = usersIdWithAccess
            .Where(id => !id.Equals(authContext.CurrentAccount.ID) && !links.Contains(id))
            .Select(userManager.GetUsers);

        var result = await users
            .Where(u => u.Status != EmployeeStatus.Terminated)
            .ToAsyncEnumerable()
            .SelectAwait(async u => await mentionWrapperCreator.CreateMentionWrapperAsync(u))
            .OrderBy(u => u.User, UserInfoComparer.Default)
            .ToListAsync();

        return result;
    }

    private async Task<List<Guid>> WhoCanRead<T>(FileEntry<T> entry)
    {
        var whoCanReadTask = (await fileSecurity.WhoCanReadAsync(entry, true)).ToList();
        whoCanReadTask.AddRange((await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID))
            .Select(x => x.Id));

        whoCanReadTask.Add((tenantManager.GetCurrentTenant()).OwnerId);

        var userIds = whoCanReadTask
            .Concat([entry.CreateBy])
            .Distinct()
            .ToList();

        return userIds;
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

    public async Task<Folder<T>> SetRoomSettingsAsync<T>(T folderId, bool? indexing, bool? denyDownload)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetFolderAsync(folderId);

        if (room == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanEditAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!DocSpaceHelper.IsRoom(room.FolderType))
        {
            return room;
        }

        if (indexing.HasValue && room.SettingsIndexing != indexing)
        {
            if (indexing.Value)
            {
                await ReOrderAsync(room.Id, true, true);
            }

            room.SettingsIndexing = indexing.Value;
            await folderDao.SaveFolderAsync(room);

            await filesMessageService.SendAsync(indexing.Value
                ? MessageAction.RoomIndexingEnabled
                : MessageAction.RoomIndexingDisabled, room);
        }

        if (denyDownload.HasValue && room.SettingsDenyDownload != denyDownload)
        {
            room.SettingsDenyDownload = denyDownload.Value;
            await folderDao.SaveFolderAsync(room);

            await filesMessageService.SendAsync(denyDownload.Value
                ? MessageAction.RoomDenyDownloadEnabled
                : MessageAction.RoomDenyDownloadDisabled, room, room.Title);
        }

        return room;
    }

    public async Task<Folder<T>> SetRoomLifetimeSettingsAsync<T>(T folderId, RoomDataLifetime lifetime)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await folderDao.GetFolderAsync(folderId);

        if (room == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanEditAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!DocSpaceHelper.IsRoom(room.FolderType))
        {
            return room;
        }

        if (Equals(room.SettingsLifetime, lifetime))
        {
            return room;
        }

        room.SettingsLifetime = lifetime;
        await folderDao.SaveFolderAsync(room);

        if (lifetime != null)
        {
            await filesMessageService.SendAsync(MessageAction.RoomLifeTimeSet, room, lifetime.Value.ToString(), lifetime.Period.ToStringFast(),
                lifetime.DeletePermanently.ToString());
        }
        else
        {
            await filesMessageService.SendAsync(MessageAction.RoomLifeTimeDisabled, room);
        }

        return room;
    }

    public async Task<Folder<T>> ReOrderAsync<T>(T folderId, bool subfolders = false, bool init = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var room = await folderDao.GetFolderAsync(folderId);

        if (room == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var orderBy = init ? new OrderBy(SortedByType.DateAndTime, false) : new OrderBy(SortedByType.CustomOrder, true);
        var folders = folderDao.GetFoldersAsync(folderId, orderBy, FilterType.None, false, Guid.Empty, null);
        var files = fileDao.GetFilesAsync(folderId, orderBy, FilterType.None, false, Guid.Empty, null, null, false);

        var entries = await files.Concat(folders.Cast<FileEntry>())
            .OrderBy(r => r.Order)
            .ToListAsync();

        Dictionary<T, int> fileIds = new();
        Dictionary<T, int> folderIds = new();

        for (var i = 1; i <= entries.Count; i++)
        {
            var entry = entries[i - 1];
            if (entry.Order != i)
            {
                switch (entry)
                {
                    case File<T> file:
                        fileIds.Add(file.Id, i);
                        break;
                    case Folder<T> folder:
                        folderIds.Add(folder.Id, i);
                        break;
                }
            }
        }

        if (fileIds.Count != 0)
        {
            await fileDao.InitCustomOrder(fileIds, folderId);
        }

        if (folderIds.Count != 0)
        {
            await folderDao.InitCustomOrder(folderIds, folderId);
        }

        if (subfolders)
        {
            foreach (var t in folderIds)
            {
                await ReOrderAsync(t.Key, true, init);
            }
        }

        return room;
    }

    public async Task<List<AceShortWrapper>> SendEditorNotifyAsync<T>(T fileId, MentionMessageWrapper mentionMessage)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var canRead = await fileSecurity.CanReadAsync(file);

        if (!canRead)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (mentionMessage?.Emails == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var showSharingSettings = false;
        bool? canShare = null;
        if (file.Encrypted)
        {
            canShare = false;
            showSharingSettings = true;
        }


        var recipients = new List<Guid>();
        foreach (var email in mentionMessage.Emails)
        {
            canShare ??= await fileSharing.CanSetAccessAsync(file);

            var recipient = await userManager.GetUserByEmailAsync(email);
            if (recipient == null || recipient.Id == Constants.LostUser.Id)
            {
                showSharingSettings = canShare.Value;
                continue;
            }

            recipients.Add(recipient.Id);
        }

        var fileLink = filesLinkUtility.GetFileWebEditorUrl(file.Id);
        if (mentionMessage.ActionLink != null)
        {
            fileLink += "&" + FilesLinkUtility.Anchor + "=" + HttpUtility.UrlEncode(ActionLinkConfig.Serialize(mentionMessage.ActionLink));
        }

        var message = (mentionMessage.Message ?? "").Trim();
        const int maxMessageLength = 200;
        if (message.Length > maxMessageLength)
        {
            message = message[..maxMessageLength] + "...";
        }

        try
        {
            await notifyClient.SendEditorMentions(file, fileLink, recipients, message);
        }
        catch (Exception ex)
        {
            _logger.ErrorWithException(ex);
        }

        return showSharingSettings ? await fileSharing.GetSharedInfoShortFileAsync(file) : null;
    }

    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccessAsync<T>(T fileId)
    {
        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var fileKeyPair = await encryptionKeyPairHelper.GetKeyPairAsync(fileId, sharingService);

        return [..fileKeyPair];
    }

    public async IAsyncEnumerable<FileEntry> ChangeOwnerAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId, Guid userId, FileShare newShare = FileShare.RoomManager)
    {
        var userInfo = await userManager.GetUsersAsync(userId);
        if (Equals(userInfo, Constants.LostUser) ||
            userInfo.Status != EmployeeStatus.Active ||
            await userManager.IsGuestAsync(userInfo) ||
            await userManager.IsUserAsync(userInfo))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ChangeOwner);
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var folders = folderDao.GetFoldersAsync(foldersId);

        await foreach (var folder in folders)
        {
            if (folder.RootFolderType is not FolderType.COMMON and not FolderType.VirtualRooms and not FolderType.RoomTemplates)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (!await fileSecurity.CanChangeOwnerAsync(folder))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);

            if (folder.ProviderEntry && !isRoom)
            {
                continue;
            }

            var newFolder = folder;
            if (folder.CreateBy != userInfo.Id)
            {
                var createBy = folder.CreateBy;

                await sharingService.SetAceObjectAsync(new AceCollection<T>
                {
                    Files = [],
                    Folders = [folder.Id],
                    Aces =
                    [
                        new AceWrapper { Access = FileShare.None, Id = userInfo.Id },
                        new AceWrapper { Access = newShare, Id = createBy }
                    ]
                }, false, socket: false, beforeOwnerChange: true);

                var folderAccess = folder.Access;

                newFolder.CreateBy = userInfo.Id;

                if (folder.ProviderEntry && isRoom)
                {
                    var providerDao = daoFactory.ProviderDao;
                    await providerDao.UpdateRoomProviderInfoAsync(new ProviderData { Id = folder.ProviderId, CreateBy = userInfo.Id });
                }
                else
                {
                    var newFolderId = await folderDao.SaveFolderAsync(newFolder);
                    newFolder = await folderDao.GetFolderAsync(newFolderId);
                    newFolder.Access = folderAccess;

                    await entryStatusManager.SetIsFavoriteFolderAsync(folder);
                }

                await filesMessageService.SendAsync(MessageAction.FileChangeOwner, newFolder, [
                    newFolder.Title, userInfo.DisplayUserName(false, displayUserSettingsHelper)
                ]);

                await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, newFolder);
            }

            yield return newFolder;
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var files = fileDao.GetFilesAsync(filesId);

        await foreach (var file in files)
        {
            if (!await fileSecurity.CanChangeOwnerAsync(file))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (await lockerManager.FileLockedForMeAsync(file.Id))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
            }

            if (await fileTracker.IsEditingAsync(file.Id))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateEditingFile);
            }

            if (file.RootFolderType != FolderType.COMMON)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (file.ProviderEntry)
            {
                continue;
            }

            var newFile = file;
            if (file.CreateBy != userInfo.Id)
            {
                newFile = serviceProvider.GetService<File<T>>();
                newFile.Id = file.Id;
                newFile.Version = file.Version + 1;
                newFile.VersionGroup = file.VersionGroup + 1;
                newFile.Title = file.Title;
                newFile.SetFileStatus(await file.GetFileStatus());
                newFile.ParentId = file.ParentId;
                newFile.CreateBy = userInfo.Id;
                newFile.CreateOn = file.CreateOn;
                newFile.ConvertedType = file.ConvertedType;
                newFile.Comment = FilesCommonResource.CommentChangeOwner;
                newFile.Encrypted = file.Encrypted;
                newFile.ThumbnailStatus = file.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;

                await using (var stream = await fileDao.GetFileStreamAsync(file))
                {
                    newFile.ContentLength = stream.CanSeek ? stream.Length : file.ContentLength;
                    newFile = await fileDao.SaveFileAsync(newFile, stream);
                }

                if (file.ThumbnailStatus == Thumbnail.Created)
                {
                    foreach (var size in thumbnailSettings.Sizes)
                    {
                        await (await globalStore.GetStoreAsync()).CopyAsync(String.Empty,
                            fileDao.GetUniqThumbnailPath(file, size.Width, size.Height),
                            String.Empty,
                            fileDao.GetUniqThumbnailPath(newFile, size.Width, size.Height));
                    }

                    await fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Created);

                    newFile.ThumbnailStatus = Thumbnail.Created;
                }

                await fileMarker.MarkAsNewAsync(newFile);

                await entryStatusManager.SetFileStatusAsync(newFile);

                await filesMessageService.SendAsync(MessageAction.FileChangeOwner, newFile, [
                    newFile.Title, userInfo.DisplayUserName(false, displayUserSettingsHelper)
                ]);

                await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, newFile);
            }

            yield return newFile;
        }
    }

    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(List<JsonElement> fileIds)
    {
        if (!authContext.IsAuthenticated && (await externalShare.GetLinkIdAsync()) == Guid.Empty)
        {
            throw GenerateException(new SecurityException(FilesCommonResource.ErrorMessage_SecurityException));
        }

        try
        {
            var (fileIntIds, _) = FileOperationsManager.GetIds(fileIds);

            await eventBus.PublishAsync(new ThumbnailRequestedIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId()) { BaseUrl = baseCommonLinkUtility.GetFullAbsolutePath(""), FileIds = fileIntIds });
        }
        catch (Exception e)
        {
            _logger.ErrorCreateThumbnails(e);
        }

        return fileIds;
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

    public async Task<List<MentionWrapper>> ProtectUsersAsync<T>(T fileId)
    {
        if (!authContext.IsAuthenticated)
        {
            return null;
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var users = new List<MentionWrapper>();
        if (file.RootFolderType == FolderType.BUNCH)
        {
            //todo: request project team
            return [..users];
        }

        var acesForObject = await fileSharing.GetSharedInfoAsync(file);

        var usersInfo = new List<UserInfo>();
        foreach (var ace in acesForObject)
        {
            if (ace.Access == FileShare.Restrict)
            {
                continue;
            }

            if (ace.SubjectGroup)
            {
                usersInfo.AddRange(await userManager.GetUsersByGroupAsync(ace.Id));
            }
            else
            {
                usersInfo.Add(await userManager.GetUsersAsync(ace.Id));
            }
        }

        users = await usersInfo.Distinct()
            .Where(user => !user.Id.Equals(authContext.CurrentAccount.ID)
                           && !user.Id.Equals(Constants.LostUser.Id))
            .ToAsyncEnumerable()
            .SelectAwait(async user => await mentionWrapperCreator.CreateMentionWrapperAsync(user))
            .ToListAsync();

        users = users
            .OrderBy(user => user.User, UserInfoComparer.Default)
            .ToList();

        return [..users];
    }

    public async Task SaveFormRoleMapping<T>(T formId, IEnumerable<FormRole> roles)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var form = await fileDao.GetFileAsync(formId);
        var currentRoom = await DocSpaceHelper.GetParentRoom(form, folderDao);

        await ValidateChangeRolesPermission(form);

        if ((roles?.Any() == false && !await fileSecurity.CanResetFillingAsync(form, authContext.CurrentAccount.ID)) ||
            (roles?.Any() == true && !await fileSecurity.CanStartFillingAsync(form, authContext.CurrentAccount.ID)))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        await fileDao.SaveFormRoleMapping(formId, roles);

        var properties = await fileDao.GetProperties(formId) ?? new EntryProperties<T> { FormFilling = new FormFillingProperties<T>() };
        if (roles?.Any() == false)
        {
            await fileDao.SaveProperties(formId, null);
        }
        else
        {
            properties.FormFilling.StartFilling = true;
            properties.FormFilling.StartedByUserId = authContext.CurrentAccount.ID;
            await fileDao.SaveProperties(formId, properties);
            var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
            await filesMessageService.SendAsync(MessageAction.FormStartedToFill, form, MessageInitiator.DocsService, user?.DisplayUserName(false, displayUserSettingsHelper), form.Title);

            var currentUserId = authContext.CurrentAccount.ID;
            var recipients = roles
                .Where(role => role.UserId != currentUserId)
                .Select(role => role.UserId)
                .Distinct()
                .ToList();

            if (recipients.Count > 0)
            {
                await notifyClient.SendFormFillingEvent(
                    currentRoom, form, recipients, NotifyConstants.EventFormStartedFilling, currentUserId);
            }

            var roleUserIds = roles.Where(r => r.UserId != currentUserId).Select(r => r.UserId);

            var aces = fileSecurity.GetPureSharesAsync(currentRoom, roleUserIds);

            var formFillers = await aces.Where(ace => ace is { Share: FileShare.FillForms }).Select(s => s.Subject).ToListAsync();

            if (formFillers.Count != 0)
            {
                if (!form.ParentId.Equals(currentRoom.Id))
                {
                    var parentFolders = await folderDao.GetParentFoldersAsync(form.ParentId).Where(f => !DocSpaceHelper.IsRoom(f.FolderType)).ToListAsync();
                    foreach (var folder in parentFolders)
                    {
                        await socketManager.CreateFolderAsync(folder, formFillers);
                    }
                }
                await socketManager.CreateFileAsync(form, formFillers);
            }

        }

        await socketManager.UpdateFileAsync(form);
    }
    public async IAsyncEnumerable<FormRoleDto> GetAllFormRoles<T>(T formId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var form = await fileDao.GetFileAsync(formId);

        if (form == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        if (!await DocSpaceHelper.IsFormOrCompletedForm(form, daoFactory))
        {
            throw new InvalidOperationException();
        }
        var roles = await fileDao.GetFormRoles(formId).ToListAsync();
        var properties = await daoFactory.GetFileDao<T>().GetProperties(formId);
        var currentStep = roles.Where(r => !r.Submitted).Min(r => (int?)r.Sequence) ?? 0;


        foreach (var r in roles)
        {
            var role = await formRoleDtoHelper.Get(properties, r);
            if (!DateTime.MinValue.Equals(properties.FormFilling.FillingStopedDate) &&
                properties.FormFilling.FormFillingInterruption?.RoleName == role.RoleName)
            {
                role.RoleStatus = FormFillingStatus.Stoped;
            }
            else
            {
                role.RoleStatus = currentStep switch
                {
                    0 => FormFillingStatus.Complete,
                    _ when currentStep > role.Sequence => FormFillingStatus.Complete,
                    _ when currentStep < role.Sequence => FormFillingStatus.Draft,
                    _ when currentStep == role.Sequence && !role.Submitted && r.OpenedAt.Equals(DateTime.MinValue) => FormFillingStatus.YouTurn,
                    _ when currentStep == role.Sequence && !role.Submitted && !r.OpenedAt.Equals(DateTime.MinValue) => FormFillingStatus.InProgress,
                    _ => FormFillingStatus.Complete
                };
            }

            yield return role;
        }
    }
    public async Task ManageFormFilling<T>(T formId, FormFillingManageAction action)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var form = await fileDao.GetFileAsync(formId);
        await ValidateChangeRolesPermission(form);

        var properties = await daoFactory.GetFileDao<T>().GetProperties(formId);
        switch (action)
        {
            case FormFillingManageAction.Stop:
                if (!await fileSecurity.CanStopFillingAsync(form, authContext.CurrentAccount.ID))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
                }
                var role = await fileDao.GetFormRoles(formId).Where(r => r.Submitted == false).FirstOrDefaultAsync();
                properties.FormFilling.FillingStopedDate = DateTime.UtcNow;
                properties.FormFilling.FormFillingInterruption =
                    new FormFillingInterruption
                    {
                        UserId = authContext.CurrentAccount.ID,
                        RoleName = role?.RoleName
                    };
                var room = await DocSpaceHelper.GetParentRoom(form, folderDao);
                var allRoleUserIds = await fileDao.GetFormRoles(form.Id).Where(role => role.UserId != authContext.CurrentAccount.ID).Select(r => r.UserId).ToListAsync();

                var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
                await filesMessageService.SendAsync(MessageAction.FormStopped, form, MessageInitiator.DocsService, user?.DisplayUserName(false, displayUserSettingsHelper), form.Title);
                await notifyClient.SendFormFillingEvent(room, form, allRoleUserIds, NotifyConstants.EventStoppedFormFilling, authContext.CurrentAccount.ID);
                break;

            case FormFillingManageAction.Resume:
                properties.FormFilling.FillingStopedDate = DateTime.MinValue;
                properties.FormFilling.FormFillingInterruption = null;
                break;

            default:
                throw new InvalidOperationException();
        }

        await fileDao.SaveProperties(formId, properties);
        await socketManager.UpdateFileAsync(form);
    }


    private async Task ValidateChangeRolesPermission<T>(File<T> form)
    {
        if (form == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        if (!form.IsForm)
        {
            throw new InvalidOperationException();
        }


        var folderDao = daoFactory.GetFolderDao<T>();
        var currentRoom = await DocSpaceHelper.GetParentRoom(form, folderDao);

        if (currentRoom == null)
        {
            throw new InvalidOperationException();
        }
    }
    private Exception GenerateException(Exception error, bool warning = false)
    {
        if (warning || error is ItemNotFoundException or SecurityException or ArgumentException or TenantQuotaException or InvalidOperationException)
        {
            _logger.Information(error.ToString());
        }
        else
        {
            _logger.ErrorFileStorageService(error);
        }

        if (error is ItemNotFoundException)
        {
            return !authContext.CurrentAccount.IsAuthenticated
                ? new SecurityException(FilesCommonResource.ErrorMessage_SecurityException)
                : error;
        }

        return new InvalidOperationException(error.Message, error);
    }
    
    private static readonly FrozenDictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> _roomMessageActions =
        new Dictionary<SubjectType, FrozenDictionary<EventType, MessageAction>> { { SubjectType.InvitationLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.RoomInvitationLinkCreated }, { EventType.Update, MessageAction.RoomInvitationLinkUpdated }, { EventType.Remove, MessageAction.RoomInvitationLinkDeleted } }.ToFrozenDictionary() }, { SubjectType.ExternalLink, new Dictionary<EventType, MessageAction> { { EventType.Create, MessageAction.RoomExternalLinkCreated }, { EventType.Update, MessageAction.RoomExternalLinkUpdated }, { EventType.Remove, MessageAction.RoomExternalLinkDeleted } }.ToFrozenDictionary() } }.ToFrozenDictionary();

}

public class FileModel<T, TTempate>
{
    public T ParentId { get; init; }
    public string Title { get; set; }
    public TTempate TemplateId { get; init; }
    public int FormId { get; init; }
}