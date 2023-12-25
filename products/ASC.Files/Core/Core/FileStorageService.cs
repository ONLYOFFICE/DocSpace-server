// (c) Copyright Ascensio System SIA 2010-2023
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
(Global global,
        GlobalStore globalStore,
        GlobalFolderHelper globalFolderHelper,
        FilesSettingsHelper filesSettingsHelper,
        AuthContext authContext,
        UserManager userManager,
        FileUtility fileUtility,
        FilesLinkUtility filesLinkUtility,
        BaseCommonLinkUtility baseCommonLinkUtility,
        CoreBaseSettings coreBaseSettings,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        IHttpContextAccessor httpContextAccessor,
        ILoggerProvider optionMonitor,
        PathProvider pathProvider,
        FileSecurity fileSecurity,
        SocketManager socketManager,
        IDaoFactory daoFactory,
        FileMarker fileMarker,
        EntryManager entryManager,
        FilesMessageService filesMessageService,
        DocumentServiceTrackerHelper documentServiceTrackerHelper,
        DocuSignToken docuSignToken,
        DocuSignHelper docuSignHelper,
        FileShareLink fileShareLink,
        FileConverter fileConverter,
        DocumentServiceHelper documentServiceHelper,
        ThirdpartyConfiguration thirdpartyConfiguration,
        DocumentServiceConnector documentServiceConnector,
        FileSharing fileSharing,
        NotifyClient notifyClient,
        IUrlShortener urlShortener,
        IServiceProvider serviceProvider,
        FileSharingAceHelper fileSharingAceHelper,
        ConsumerFactory consumerFactory,
        EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
        SettingsManager settingsManager,
        FileOperationsManager fileOperationsManager,
        TenantManager tenantManager,
        FileTrackerHelper fileTracker,
        IEventBus eventBus,
        EntryStatusManager entryStatusManager,
        CompressToArchive compressToArchive,
        OFormRequestManager oFormRequestManager,
        MessageService messageService,
        IServiceScopeFactory serviceScopeFactory,
        ThirdPartySelector thirdPartySelector,
        ThumbnailSettings thumbnailSettings,
        FileShareParamsHelper fileShareParamsHelper,
        EncryptionLoginProvider encryptionLoginProvider,
        CountRoomChecker countRoomChecker,
        InvitationLinkService invitationLinkService,
        InvitationLinkHelper invitationLinkHelper,
        StudioNotifyService studioNotifyService,
        TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
        QuotaSocketManager quotaSocketManager,
        ExternalShare externalShare,
        TenantUtil tenantUtil,
        RoomLogoManager roomLogoManager, 
        IDistributedLockProvider distributedLockProvider)
    {

    private readonly ILogger _logger = optionMonitor.CreateLogger("ASC.Files");

    public async Task<Folder<T>> GetFolderAsync<T>(T folderId)
    {
        var folderDao = GetFolderDao<T>();
        var tagDao = GetTagDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        ErrorIf(folder == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanReadAsync(folder), FilesCommonResource.ErrorMassage_SecurityException_ReadFolder);

        var tag = await tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, folder).FirstOrDefaultAsync();
        if (tag != null)
        {
            folder.NewForMe = tag.Count;
        }

        var tags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, null).ToListAsync();
        folder.Pinned = tags.Any(r => r.Type == TagType.Pin);
        folder.IsFavorite = tags.Any(r => r.Type == TagType.Favorite);
        folder.Tags = tags.Where(r=> r.Type == TagType.Custom).ToList();
        
        return folder;
    }

    public async Task<IEnumerable<FileEntry>> GetFoldersAsync<T>(T parentId)
    {
        var folderDao = GetFolderDao<T>();
        IEnumerable<FileEntry> entries;

        try
        {
            (entries, _) = await entryManager.GetEntriesAsync(
                await folderDao.GetFolderAsync(parentId), 0, -1, FilterType.FoldersOnly,
                false, Guid.Empty, string.Empty, new string[] { }, false, false, new OrderBy(SortedByType.AZ, true));
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }

        return entries;
    }

    public async Task<DataWrapper<T>> GetFolderItemsAsync<T>(
        T parentId,
        int from,
        int count,
        FilterType filterType,
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
        ApplyFilterOption applyFilterOption = ApplyFilterOption.All)
    {
        var subjectId = string.IsNullOrEmpty(subject) ? Guid.Empty : new Guid(subject);

        var folderDao = GetFolderDao<T>();

        Folder<T> parent = null;
        try
        {
            parent = await folderDao.GetFolderAsync(parentId);
            if (parent != null && !string.IsNullOrEmpty(parent.Error))
            {
                throw new Exception(parent.Error);
            }
        }
        catch (Exception e)
        {
            if (parent is { ProviderEntry: true })
            {
                throw GenerateException(new Exception(FilesCommonResource.ErrorMassage_SharpBoxException, e));
            }

            throw GenerateException(e);
        }

        ErrorIf(parent == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanReadAsync(parent), FilesCommonResource.ErrorMassage_SecurityException_ViewFolder);
        ErrorIf(parent.RootFolderType == FolderType.TRASH && !Equals(parent.Id, await globalFolderHelper.FolderTrashAsync), FilesCommonResource.ErrorMassage_ViewTrashItem);

        if (orderBy != null)
        {
            filesSettingsHelper.DefaultOrder = orderBy;
        }
        else
        {
            orderBy = filesSettingsHelper.DefaultOrder;
        }

        if (Equals(parent.Id, await globalFolderHelper.FolderShareAsync) && orderBy.SortedBy == SortedByType.DateAndTime)
        {
            orderBy.SortedBy = SortedByType.New;
        }

        searchArea = parent.FolderType == FolderType.Archive ? SearchArea.Archive : searchArea;

        int total;
        IEnumerable<FileEntry> entries;
        try
        {
            (entries, total) = await entryManager.GetEntriesAsync(parent, from, count, filterType, subjectGroup, subjectId, searchText, extension, searchInContent, withSubfolders, orderBy, roomId, searchArea,
                withoutTags, tagNames, excludeSubject, provider, subjectFilter, applyFilterOption);
        }
        catch (Exception e)
        {
            if (parent.ProviderEntry)
            {
                throw GenerateException(new Exception(FilesCommonResource.ErrorMassage_SharpBoxException, e));
            }

            throw GenerateException(e);
        }

        var breadCrumbsTask = entryManager.GetBreadCrumbsAsync(parentId, folderDao);
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

        entries = entries.Where(x =>
        {
            if (x.FileEntryType == FileEntryType.Folder)
            {
                return true;
            }

            if (x is File<string> f1)
            {
                return !fileConverter.IsConverting(f1);
            }

            return x is File<int> f2 && !fileConverter.IsConverting(f2);
        });

        var result = new DataWrapper<T>
        {
            Total = total,
            Entries = entries.ToList(),
            FolderPathParts = new List<object>(breadCrumbs.Select(f =>
            {
                if (f.FileEntryType == FileEntryType.Folder)
                {
                    if (f is Folder<string> f1)
                    {
                        return (object)new { f1.Id, f1.Title, RoomType = DocSpaceHelper.GetRoomType(f1.FolderType) };
                    }

                    if (f is Folder<int> f2)
                    {
                        return new { f2.Id, f2.Title, RoomType = DocSpaceHelper.GetRoomType(f2.FolderType) };
                    }
                }

                return 0;
            })),
            FolderInfo = parent,
            New = await newTask
        };

        return result;
    }

    public async Task<List<FileEntry>> GetItemsAsync<TId>(IEnumerable<TId> filesId, IEnumerable<TId> foldersId, FilterType filter, bool subjectGroup, string subjectID, string search)
    {
        var subjectId = string.IsNullOrEmpty(subjectID) ? Guid.Empty : new Guid(subjectID);

        var entries = AsyncEnumerable.Empty<FileEntry<TId>>();

        var folderDao = daoFactory.GetFolderDao<TId>();
        var fileDao = daoFactory.GetFileDao<TId>();

        entries = entries.Concat(fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)));
        entries = entries.Concat(fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)));
        entries = entryManager.FilterEntries(entries, filter, subjectGroup, subjectId, search, true);

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

    public async Task<Folder<T>> CreateNewFolderAsync<T>(T parentId, string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(parentId);

        return await InternalCreateNewFolderAsync(parentId, title);
    }

    public async Task<Folder<int>> CreateRoomAsync(string title, RoomType roomType, bool @private, bool indexing, IEnumerable<FileShareParams> share, bool notify, string sharingMessage)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetRoomsCountCheckKey(tenantId)))
        {
            ArgumentNullException.ThrowIfNull(title);

        await countRoomChecker.CheckAppend();

        if (@private && (share == null || !share.Any()))
        {
            throw new ArgumentNullException(nameof(share));
        }

        List<AceWrapper> aces = null;

        if (@private)
        {
            aces = await GetFullAceWrappersAsync(share);
            await CheckEncryptionKeysAsync(aces);
        }

        var parentId = await globalFolderHelper.GetFolderVirtualRooms();

        var room = roomType switch
        {
            RoomType.CustomRoom => await CreateCustomRoomAsync(title, parentId, @private, indexing),
            RoomType.EditingRoom => await CreateEditingRoomAsync(title, parentId, @private, indexing),
            RoomType.PublicRoom => await CreatePublicRoomAsync(title, parentId, @private, indexing),
            _ => await CreateCustomRoomAsync(title, parentId, @private, indexing)
        };

        if (@private)
        {
            await SetAcesForPrivateRoomAsync(room, aces, notify, sharingMessage);
        }

        return room;
    }
    }

    private async Task<Folder<T>> CreatePublicRoomAsync<T>(string title, T parentId, bool @private, bool indexing)
    {
        var room = await InternalCreateNewFolderAsync(parentId, title, FolderType.PublicRoom, @private, indexing);

        _ = await SetExternalLinkAsync(room, Guid.NewGuid(), FileShare.Read, FilesCommonResource.DefaultExternalLinkTitle, primary: true);
        
        return room;
    }

    public async Task<Folder<T>> CreateThirdPartyRoomAsync<T>(string title, RoomType roomType, T parentId, bool @private, bool indexing, IEnumerable<FileShareParams> share, bool notify, string sharingMessage)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(parentId);

        if (@private && (share == null || !share.Any()))
        {
            throw new ArgumentNullException(nameof(share));
        }

        var folderDao = GetFolderDao<T>();
        var providerDao = GetProviderDao();

        var parent = await folderDao.GetFolderAsync(parentId);
        var providerInfo = await providerDao.GetProviderInfoAsync(parent.ProviderId);

        ErrorIf(providerInfo.RootFolderType != FolderType.VirtualRooms, FilesCommonResource.ErrorMessage_InvalidProvider);
        ErrorIf(providerInfo.FolderId != null, FilesCommonResource.ErrorMessage_ProviderAlreadyConnect);

        List<AceWrapper> aces = null;

        if (@private)
        {
            aces = await GetFullAceWrappersAsync(share);
            await CheckEncryptionKeysAsync(aces);
        }

        var result = roomType switch
        {
            RoomType.CustomRoom => (await CreateCustomRoomAsync(title, parentId, @private, indexing), FolderType.CustomRoom),
            RoomType.EditingRoom => (await CreateEditingRoomAsync(title, parentId, @private, indexing), FolderType.EditingRoom),
            RoomType.PublicRoom => (await CreatePublicRoomAsync(title, parentId, @private, indexing), FolderType.PublicRoom),
            _ => (await CreateCustomRoomAsync(title, parentId, @private, indexing), FolderType.CustomRoom)
        };

        ErrorIf(result.Item1.Id.Equals(result.Item1.RootId), FilesCommonResource.ErrorMessage_InvalidThirdPartyFolder);

        if (@private)
        {
            await SetAcesForPrivateRoomAsync(result.Item1, aces, notify, sharingMessage);
        }

        await providerDao.UpdateProviderInfoAsync(providerInfo.ProviderId, title, result.Item1.Id.ToString(), result.Item2, @private);

        return result.Item1;
    }

    private async Task<Folder<T>> CreateCustomRoomAsync<T>(string title, T parentId, bool privacy, bool indexing)
    {
        return await InternalCreateNewFolderAsync(parentId, title, FolderType.CustomRoom, privacy, indexing);
    }

    private async Task<Folder<T>> CreateEditingRoomAsync<T>(string title, T parentId, bool privacy, bool indexing)
    {
        return await InternalCreateNewFolderAsync(parentId, title, FolderType.EditingRoom, privacy, indexing);
    }

    private async ValueTask<Folder<T>> InternalCreateNewFolderAsync<T>(T parentId, string title, FolderType folderType = FolderType.DEFAULT, bool privacy = false, bool indexing = false)
    {
        var folderDao = GetFolderDao<T>();

        var parent = await folderDao.GetFolderAsync(parentId);
        var isRoom = DocSpaceHelper.IsRoom(folderType);

        ErrorIf(parent == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanCreateAsync(parent), FilesCommonResource.ErrorMassage_SecurityException_Create);
        ErrorIf(parent.RootFolderType == FolderType.Archive, FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        ErrorIf(parent.FolderType == FolderType.Archive, FilesCommonResource.ErrorMassage_SecurityException);
        ErrorIf(!isRoom && parent.FolderType == FolderType.VirtualRooms, FilesCommonResource.ErrorMassage_SecurityException_Create);

        try
        {
            var newFolder = serviceProvider.GetService<Folder<T>>();
            newFolder.Title = title;
            newFolder.ParentId = parent.Id;
            newFolder.FolderType = folderType;
            newFolder.SettingsPrivate = parent.SettingsPrivate ? parent.SettingsPrivate : privacy;
            newFolder.SettingsColor = roomLogoManager.GetRandomColour();
            newFolder.SettingsIndexing = indexing;
            var folderId = await folderDao.SaveFolderAsync(newFolder);
            var folder = await folderDao.GetFolderAsync(folderId);

            await socketManager.CreateFolderAsync(folder);

            if (isRoom)
            {
                var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<CountRoomFeature, int>();
                _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            }

            await filesMessageService.SendAsync(isRoom ? MessageAction.RoomCreated : MessageAction.FolderCreated, folder, folder.Title);

            return folder;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }


    }

    public async Task<Folder<T>> FolderRenameAsync<T>(T folderId, string title)
    {
        var tagDao = GetTagDao<T>();
        var folderDao = GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        ErrorIf(folder == null, FilesCommonResource.ErrorMassage_FolderNotFound);

        var canEdit = DocSpaceHelper.IsRoom(folder.FolderType) ? folder.RootFolderType != FolderType.Archive && await fileSecurity.CanEditRoomAsync(folder)
            : await fileSecurity.CanRenameAsync(folder);

        ErrorIf(!canEdit, FilesCommonResource.ErrorMassage_SecurityException_RenameFolder);
        if (!canEdit && await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException_RenameFolder);
        }

        ErrorIf(folder.RootFolderType == FolderType.TRASH, FilesCommonResource.ErrorMassage_ViewTrashItem);
        ErrorIf(folder.RootFolderType == FolderType.Archive, FilesCommonResource.ErrorMessage_UpdateArchivedRoom);

        var folderAccess = folder.Access;

        if (!string.Equals(folder.Title, title, StringComparison.OrdinalIgnoreCase))
        {
            var oldTitle = folder.Title;
            var newFolderID = await folderDao.RenameFolderAsync(folder, title);
            folder = await folderDao.GetFolderAsync(newFolderID);
            folder.Access = folderAccess;

            if (DocSpaceHelper.IsRoom(folder.FolderType))
            {
                await filesMessageService.SendAsync(MessageAction.RoomRenamed, oldTitle, folder, folder.Title);
            }
            else
            {
                await filesMessageService.SendAsync(MessageAction.FolderRenamed, folder, folder.Title);
            }

            //if (!folder.ProviderEntry)
            //{
            //    FoldersIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, folder));
            //}
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

        return folder;
    }

    public async Task<File<T>> GetFileAsync<T>(T fileId, int version)
    {
        var fileDao = GetFileDao<T>();
        await fileDao.InvalidateCacheAsync(fileId);

        var file = version > 0
                       ? await fileDao.GetFileAsync(fileId, version)
                       : await fileDao.GetFileAsync(fileId);
        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!await fileSecurity.CanReadAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);

        await entryStatusManager.SetFileStatusAsync(file);

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        return file;
    }

    public async IAsyncEnumerable<FileEntry<T>> GetSiblingsFileAsync<T>(T fileId, T parentId, FilterType filter, bool subjectGroup, string subjectID, string searchText, string[] extension, 
        bool searchInContent, bool withSubfolders, OrderBy orderBy)
    {
        var subjectId = string.IsNullOrEmpty(subjectID) ? Guid.Empty : new Guid(subjectID);

        var fileDao = GetFileDao<T>();
        var folderDao = GetFolderDao<T>();

        var file = await fileDao.GetFileAsync(fileId);
        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!await fileSecurity.CanReadAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);

        var parent = await folderDao.GetFolderAsync(EqualityComparer<T>.Default.Equals(parentId, default) ? file.ParentId : parentId);
        ErrorIf(parent == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(parent.RootFolderType == FolderType.TRASH, FilesCommonResource.ErrorMassage_ViewTrashItem);

        if (filter == FilterType.FoldersOnly)
        {
            yield break;
        }
        if (filter == FilterType.None)
        {
            filter = FilterType.FilesOnly;
        }

        orderBy ??= filesSettingsHelper.DefaultOrder;
        
        if (Equals(parent.Id, await globalFolderHelper.GetFolderShareAsync<T>()) && orderBy.SortedBy == SortedByType.DateAndTime)
        {
            orderBy.SortedBy = SortedByType.New;
        }

        var entries = Enumerable.Empty<FileEntry>();

        if (!await fileSecurity.CanReadAsync(parent))
        {
            file.ParentId = await globalFolderHelper.GetFolderShareAsync<T>();
            entries = entries.Append(file);
        }
        else
        {
            try
            {
                (entries, _) = await entryManager.GetEntriesAsync(parent, 0, 0, filter, subjectGroup, subjectId, searchText, extension, searchInContent, withSubfolders, orderBy);
            }
            catch (Exception e)
            {
                if (parent.ProviderEntry)
                {
                    throw GenerateException(new Exception(FilesCommonResource.ErrorMassage_SharpBoxException, e));
                }

                throw GenerateException(e);
            }
        }

        var previewedType = new[] { FileType.Image, FileType.Audio, FileType.Video };


        await foreach (var f in fileSecurity.FilterReadAsync(entries.OfType<File<T>>().ToAsyncEnumerable()))
        {
            if (f is File<T> && previewedType.Contains(FileUtility.GetFileTypeByFileName(f.Title)))
            {
                yield return f;
            }
        }
    }

    public async ValueTask<File<T>> CreateNewFileAsync<T, TTemplate>(FileModel<T, TTemplate> fileWrapper, bool enableExternalExt = false)
    {
        if (string.IsNullOrEmpty(fileWrapper.Title) || fileWrapper.ParentId == null)
        {
            throw new ArgumentException();
        }

        var fileDao = GetFileDao<T>();
        var folderDao = GetFolderDao<T>();

        Folder<T> folder = null;
        if (!EqualityComparer<T>.Default.Equals(fileWrapper.ParentId, default))
        {
            folder = await folderDao.GetFolderAsync(fileWrapper.ParentId);
            var canCreate = await fileSecurity.CanCreateAsync(folder) && folder.FolderType != FolderType.VirtualRooms
                && folder.FolderType != FolderType.Archive;

            if (!canCreate)
            {
                folder = null;
            }
        }

        folder ??= await folderDao.GetFolderAsync(await globalFolderHelper.GetFolderMyAsync<T>());

        var file = serviceProvider.GetService<File<T>>();
        file.ParentId = folder.Id;
        file.Comment = FilesCommonResource.CommentCreate;

        if (string.IsNullOrEmpty(fileWrapper.Title))
        {
            fileWrapper.Title = UserControlsCommonResource.NewDocument + ".docx";
        }

        var title = fileWrapper.Title;
        var fileExt = FileUtility.GetFileExtension(title);
        if (!enableExternalExt && fileExt != fileUtility.MasterFormExtension)
        {
            fileExt = fileUtility.GetInternalExtension(title);
            if (!fileUtility.InternalExtension.ContainsValue(fileExt))
            {
                fileExt = fileUtility.InternalExtension[FileType.Document];
                file.Title = title + fileExt;
            }
            else
            {
                file.Title = FileUtility.ReplaceFileExtension(title, fileExt);
            }
        }
        else
        {
            file.Title = FileUtility.ReplaceFileExtension(title, fileExt);
        }

        if (fileWrapper.FormId != 0)
        {
            await using var stream = await oFormRequestManager.Get(fileWrapper.FormId);
                file.ContentLength = stream.Length;
                file = await fileDao.SaveFileAsync(file, stream);
            }
        else if (EqualityComparer<TTemplate>.Default.Equals(fileWrapper.TemplateId, default))
        {
            var culture = (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).GetCulture();
            var storeTemplate = await GetStoreTemplateAsync();

            var path = FileConstant.NewDocPath + culture + "/";
            if (!await storeTemplate.IsDirectoryAsync(path))
            {
                path = FileConstant.NewDocPath + "en-US/";
            }

            try
            {

                file.ThumbnailStatus = Thumbnail.Creating;

                if (!enableExternalExt)
                {
                    var pathNew = path + "new" + fileExt;
                    await using var stream = await storeTemplate.GetReadStreamAsync("", pathNew, 0);
                        file.ContentLength = stream.CanSeek ? stream.Length : await storeTemplate.GetFileSizeAsync(pathNew);
                        file = await fileDao.SaveFileAsync(file, stream);
                    }
                else
                {
                    file = await fileDao.SaveFileAsync(file, null);
                }

                var counter = 0;

                foreach (var size in thumbnailSettings.Sizes)
                {
                    var pathThumb = $"{path}{fileExt.Trim('.')}.{size.Width}x{size.Height}.{global.ThumbnailExtension}";

                    if (!await storeTemplate.IsFileAsync("", pathThumb))
                    {
                        break;
                    }

                    await using (var streamThumb = await storeTemplate.GetReadStreamAsync("", pathThumb, 0))
                    {
                        await (await globalStore.GetStoreAsync()).SaveAsync(fileDao.GetUniqThumbnailPath(file, size.Width, size.Height), streamThumb);
                    }

                    counter++;
                }

                if (thumbnailSettings.Sizes.Count() == counter)
                {
                    await fileDao.SetThumbnailStatusAsync(file, Thumbnail.Created);

                    file.ThumbnailStatus = Thumbnail.Created;

                }
                else
                {
                    await fileDao.SetThumbnailStatusAsync(file, Thumbnail.NotRequired);

                    file.ThumbnailStatus = Thumbnail.NotRequired;
                }

            }
            catch (Exception e)
            {
                throw GenerateException(e);
            }
        }
        else
        {
            var fileTemlateDao = daoFactory.GetFileDao<TTemplate>();
            var template = await fileTemlateDao.GetFileAsync(fileWrapper.TemplateId);
            ErrorIf(template == null, FilesCommonResource.ErrorMassage_FileNotFound);
            ErrorIf(!await fileSecurity.CanReadAsync(template), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);

            file.ThumbnailStatus = template.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;

            try
            {
                await using (var stream = await fileTemlateDao.GetFileStreamAsync(template))
                {
                    file.ContentLength = template.ContentLength;
                    file = await fileDao.SaveFileAsync(file, stream);
                }

                if (template.ThumbnailStatus == Thumbnail.Created)
                {
                    foreach (var size in thumbnailSettings.Sizes)
                    {
                        await (await globalStore.GetStoreAsync()).CopyAsync(String.Empty,
                                        fileTemlateDao.GetUniqThumbnailPath(template, size.Width, size.Height),
                                        String.Empty,
                                        fileDao.GetUniqThumbnailPath(file, size.Width, size.Height));
                    }

                    await fileDao.SetThumbnailStatusAsync(file, Thumbnail.Created);

                    file.ThumbnailStatus = Thumbnail.Created;
                }
            }
            catch (Exception e)
            {
                throw GenerateException(e);
            }
        }

        await filesMessageService.SendAsync(MessageAction.FileCreated, file, file.Title);

        await fileMarker.MarkAsNewAsync(file);

        await socketManager.CreateFileAsync(file);

        return file;
    }

    public async Task<KeyValuePair<bool, string>> TrackEditFileAsync<T>(T fileId, Guid tabId, string docKeyForTrack, string doc = null, bool isFinish = false)
    {
        try
        {
            var id = await fileShareLink.ParseAsync<T>(doc);
            if (id == null)
            {
                if (!authContext.IsAuthenticated)
                {
                    throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
                }

                if (!string.IsNullOrEmpty(doc))
                {
                    throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
                }

                id = fileId;
            }

            if (docKeyForTrack != await documentServiceHelper.GetDocKeyAsync(id, -1, DateTime.MinValue))
            {
                throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
            }

            if (isFinish)
            {
                fileTracker.Remove(id, tabId);
                await socketManager.StopEditAsync(id);
            }
            else
            {
                await entryManager.TrackEditingAsync(id, tabId, authContext.CurrentAccount.ID, doc, await tenantManager.GetCurrentTenantIdAsync());
            }

            return new KeyValuePair<bool, string>(true, string.Empty);
        }
        catch (Exception ex)
        {
            return new KeyValuePair<bool, string>(false, ex.Message);
        }
    }

    public async Task<Dictionary<string, string>> CheckEditingAsync<T>(IEnumerable<T> filesId)
    {
        ErrorIf(!authContext.IsAuthenticated, FilesCommonResource.ErrorMassage_SecurityException);
        var result = new Dictionary<string, string>();

        var fileDao = GetFileDao<T>();
        var ids = filesId.Where(fileTracker.IsEditing).Select(id => id).ToList();

        await foreach (var file in fileDao.GetFilesAsync(ids))
        {
            if (file == null
                || !await fileSecurity.CanEditAsync(file)
                && !await fileSecurity.CanCustomFilterEditAsync(file)
                && !await fileSecurity.CanReviewAsync(file)
                && !await fileSecurity.CanFillFormsAsync(file)
                && !await fileSecurity.CanCommentAsync(file))
            {
                continue;
            }

            var usersId = fileTracker.GetEditingBy(file.Id).ToAsyncEnumerable();
            var value = string.Join(", ", await usersId.SelectAwait(async userId => await global.GetUserNameAsync(userId, true)).ToArrayAsync());
            result[file.Id.ToString()] = value;
        }

        return result;
    }

    public async Task<File<T>> SaveEditingAsync<T>(T fileId, string fileExtension, string fileuri, Stream stream, string doc = null, bool forcesave = false)
    {
        try
        {
            if (!forcesave && fileTracker.IsEditingAlone(fileId))
            {
                fileTracker.Remove(fileId);
                await socketManager.StopEditAsync(fileId);
            }

            var file = await entryManager.SaveEditingAsync(fileId, fileExtension, fileuri, stream, doc, forcesave: forcesave ? ForcesaveType.User : ForcesaveType.None, keepLink: true);

            if (file != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileUpdated, file, file.Title);
            }

            return file;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    public async Task<File<T>> UpdateFileStreamAsync<T>(T fileId, Stream stream, string fileExtension, bool encrypted, bool forcesave)
    {
        try
        {
            if (!forcesave && fileTracker.IsEditing(fileId))
            {
                fileTracker.Remove(fileId);
                await socketManager.StopEditAsync(fileId);
            }

            var file = await entryManager.SaveEditingAsync(fileId,
                fileExtension,
                null,
                stream,
                null,
                encrypted ? FilesCommonResource.CommentEncrypted : null,
                encrypted: encrypted,
                forcesave: forcesave ? ForcesaveType.User : ForcesaveType.None);

            if (file != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileUpdated, file, file.Title);
                await socketManager.UpdateFileAsync(file);
            }

            return file;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    public async Task<string> StartEditAsync<T>(T fileId, bool editingAlone = false, string doc = null)
    {
        try
        {
            IThirdPartyApp app;
            if (editingAlone)
            {
                ErrorIf(fileTracker.IsEditing(fileId), FilesCommonResource.ErrorMassage_SecurityException_EditFileTwice);

                app = thirdPartySelector.GetAppByFileId(fileId.ToString());
                if (app == null)
                {
                    await entryManager.TrackEditingAsync(fileId, Guid.Empty, authContext.CurrentAccount.ID, doc, await tenantManager.GetCurrentTenantIdAsync(), true);
                }

                //without StartTrack, track via old scheme
                return await documentServiceHelper.GetDocKeyAsync(fileId, -1, DateTime.MinValue);
            }

            (File<string> File, Configuration<string> Configuration, bool LocatedInPrivateRoom) fileOptions;

            app = thirdPartySelector.GetAppByFileId(fileId.ToString());
            if (app == null)
            {
                fileOptions = await documentServiceHelper.GetParamsAsync(fileId.ToString(), -1, doc, true, true, false);
            }
            else
            {
                var (file, editable) = await app.GetFileAsync(fileId.ToString());
                fileOptions = await documentServiceHelper.GetParamsAsync(file, true, editable ? FileShare.ReadWrite : FileShare.Read, false, editable, editable, editable, false);
            }

            var configuration = fileOptions.Configuration;

            ErrorIf(!configuration.EditorConfig.ModeWrite
                || !(configuration.Document.Permissions.Edit
                || configuration.Document.Permissions.ModifyFilter
                || configuration.Document.Permissions.Review
                || configuration.Document.Permissions.FillForms
                || configuration.Document.Permissions.Comment),
                !string.IsNullOrEmpty(configuration.ErrorMessage) ? configuration.ErrorMessage : FilesCommonResource.ErrorMassage_SecurityException_EditFile);
            var key = configuration.Document.Key;

            if (!await documentServiceTrackerHelper.StartTrackAsync(fileId.ToString(), key))
            {
                throw new Exception(FilesCommonResource.ErrorMassage_StartEditing);
            }

            return key;
        }
        catch (Exception e)
        {
            fileTracker.Remove(fileId);

            throw GenerateException(e);
        }
    }

    public async Task<File<T>> FileRenameAsync<T>(T fileId, string title)
    {
        try
        {
            var fileRename = await entryManager.FileRenameAsync(fileId, title);
            var file = fileRename.File;

            if (fileRename.Renamed)
            {
                await filesMessageService.SendAsync(MessageAction.FileRenamed, file, file.Title);

                //if (!file.ProviderEntry)
                //{
                //    FilesIndexer.UpdateAsync(FilesWrapper.GetFilesWrapper(ServiceProvider, file), true, r => r.Title);
                //}
            }

            if (file.RootFolderType == FolderType.USER
                && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
            {
                var folderDao = GetFolderDao<T>();
                if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
                {
                    file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
                }
            }

            return file;
        }
        catch (Exception ex)
        {
            throw GenerateException(ex);
        }
    }

    public async IAsyncEnumerable<File<T>> GetFileHistoryAsync<T>(T fileId)
    {
        var fileDao = GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        ErrorIf(!await fileSecurity.CanReadHistoryAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);

        await foreach (var r in fileDao.GetFileHistoryAsync(fileId))
        {
            await entryStatusManager.SetFileStatusAsync(r);
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
            var folderDao = GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        return new KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>(file, GetFileHistoryAsync(fileId));
    }

    public async Task<string> UpdateCommentAsync<T>(T fileId, int version, string comment)
    {
        var fileDao = GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId, version);
        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!await fileSecurity.CanEditHistoryAsync(file) || await userManager.IsUserAsync(authContext.CurrentAccount.ID), FilesCommonResource.ErrorMassage_SecurityException_EditFile);
        ErrorIf(await entryManager.FileLockedForMeAsync(file.Id), FilesCommonResource.ErrorMassage_LockedFile);
        ErrorIf(file.RootFolderType == FolderType.TRASH, FilesCommonResource.ErrorMassage_ViewTrashItem);

        comment = await fileDao.UpdateCommentAsync(fileId, version, comment);

        await filesMessageService.SendAsync(MessageAction.FileUpdatedRevisionComment, file, file.Title, version.ToString(CultureInfo.InvariantCulture));

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
            var folderDao = GetFolderDao<T>();
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
        var tagDao = GetTagDao<T>();
        var fileDao = GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!await fileSecurity.CanLockAsync(file) || lockfile && await userManager.IsUserAsync(authContext.CurrentAccount.ID), FilesCommonResource.ErrorMassage_SecurityException_EditFile);
        ErrorIf(file.RootFolderType == FolderType.TRASH, FilesCommonResource.ErrorMassage_ViewTrashItem);

        var tags = tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.Locked);
        var tagLocked = await tags.FirstOrDefaultAsync();

        if (lockfile)
        {
            if (tagLocked == null)
            {
                tagLocked = new Tag("locked", TagType.Locked, authContext.CurrentAccount.ID, 0).AddEntry(file);

                await tagDao.SaveTagsAsync(tagLocked);
            }

            var usersDrop = fileTracker.GetEditingBy(file.Id).Where(uid => uid != authContext.CurrentAccount.ID).Select(u => u.ToString()).ToArray();
            if (usersDrop.Length > 0)
            {
                var fileStable = file.Forcesave == ForcesaveType.None ? file : await fileDao.GetFileStableAsync(file.Id, file.Version);
                var docKey = await documentServiceHelper.GetDocKeyAsync(fileStable);
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
                file = await entryManager.CompleteVersionFileAsync(file.Id, 0, false);
                await UpdateCommentAsync(file.Id, file.Version, FilesCommonResource.UnlockComment);
            }
        }

        await entryStatusManager.SetFileStatusAsync(file);

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        await socketManager.UpdateFileAsync(file);

        return file;
    }

    public async IAsyncEnumerable<EditHistory> GetEditHistoryAsync<T>(T fileId, string doc = null)
    {
        var fileDao = GetFileDao<T>();
        var (readLink, file, _) = await fileShareLink.CheckAsync(doc, true, fileDao);
        file ??= await fileDao.GetFileAsync(fileId);

        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!readLink && !await fileSecurity.CanReadHistoryAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);
        ErrorIf(file.ProviderEntry, FilesCommonResource.ErrorMassage_BadRequest);

        await foreach (var f in fileDao.GetEditHistoryAsync(documentServiceHelper, file.Id))
        {
            yield return f;
        }
    }

    public async Task<EditHistoryDataDto> GetEditDiffUrlAsync<T>(T fileId, int version = 0, string doc = null)
    {
        var fileDao = GetFileDao<T>();
        var (readLink, file, _) = await fileShareLink.CheckAsync(doc, true, fileDao);

        if (file != null)
        {
            fileId = file.Id;
        }

        if (file == null
            || version > 0 && file.Version != version)
        {
            file = version > 0
                       ? await fileDao.GetFileAsync(fileId, version)
                       : await fileDao.GetFileAsync(fileId);
        }

        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!readLink && !await fileSecurity.CanReadHistoryAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);
        ErrorIf(file.ProviderEntry, FilesCommonResource.ErrorMassage_BadRequest);

        var result = new EditHistoryDataDto
        {
            FileType = file.ConvertedExtension.Trim('.'),
            Key = await documentServiceHelper.GetDocKeyAsync(file),
            Url = await documentServiceConnector.ReplaceCommunityAdressAsync(await pathProvider.GetFileStreamUrlAsync(file, doc)),
            Version = version
        };

        if (await fileDao.ContainChangesAsync(file.Id, file.Version))
        {
            string previouseKey;
            string sourceFileUrl;
            string sourceExt;

            if (file.Version > 1)
            {
                var previousFileStable = await fileDao.GetFileStableAsync(file.Id, file.Version - 1);
                ErrorIf(previousFileStable == null, FilesCommonResource.ErrorMassage_FileNotFound);

                sourceFileUrl = await pathProvider.GetFileStreamUrlAsync(previousFileStable, doc);
                sourceExt = previousFileStable.ConvertedExtension;

                previouseKey = await documentServiceHelper.GetDocKeyAsync(previousFileStable);
            }
            else
            {
                var culture = (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).GetCulture();
                var storeTemplate = await GetStoreTemplateAsync();

                var path = FileConstant.NewDocPath + culture + "/";
                if (!await storeTemplate.IsDirectoryAsync(path))
                {
                    path = FileConstant.NewDocPath + "en-US/";
                }

                var fileExt = FileUtility.GetFileExtension(file.Title);

                path += "new" + fileExt;

                var uri = await storeTemplate.GetUriAsync("", path);
                sourceFileUrl = uri.ToString();
                sourceFileUrl = baseCommonLinkUtility.GetFullAbsolutePath(sourceFileUrl);
                sourceExt = fileExt.Trim('.');

                previouseKey = DocumentServiceConnector.GenerateRevisionId(Guid.NewGuid().ToString());
            }

            result.Previous = new EditHistoryUrl
            {
                Key = previouseKey,
                Url = await documentServiceConnector.ReplaceCommunityAdressAsync(sourceFileUrl),
                FileType = sourceExt.Trim('.')
            };

            result.ChangesUrl = await documentServiceConnector.ReplaceCommunityAdressAsync(await pathProvider.GetFileChangesUrlAsync(file, doc));
        }

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    public async IAsyncEnumerable<EditHistory> RestoreVersionAsync<T>(T fileId, int version, string url = null, string doc = null)
    {
        File<T> file;
        if (string.IsNullOrEmpty(url))
        {
            file = await entryManager.UpdateToVersionFileAsync(fileId, version, doc);
        }
        else
        {
            var fileDao = GetFileDao<T>();
            var fromFile = await fileDao.GetFileAsync(fileId, version);
            var modifiedOnString = fromFile.ModifiedOnString;
            file = await entryManager.SaveEditingAsync(fileId, null, url, null, doc, string.Format(FilesCommonResource.CommentRevertChanges, modifiedOnString));
        }

        await filesMessageService.SendAsync(MessageAction.FileRestoreVersion, file, file.Title, version.ToString(CultureInfo.InvariantCulture));

        await foreach (var f in GetFileDao<T>().GetEditHistoryAsync(documentServiceHelper, file.Id))
        {
            yield return f;
        }
    }

    public async Task<FileLink> GetPresignedUriAsync<T>(T fileId)
    {
        var file = await GetFileAsync(fileId, -1);
        var result = new FileLink
        {
            FileType = FileUtility.GetFileExtension(file.Title),
            Url = await documentServiceConnector.ReplaceCommunityAdressAsync(await pathProvider.GetFileStreamUrlAsync(file))
        };

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    public async Task<EntryProperties> GetFileProperties<T>(T fileId)
    {
        var fileDao = GetFileDao<T>();

        await fileDao.InvalidateCacheAsync(fileId);

        var file = await fileDao.GetFileAsync(fileId);
        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);
        ErrorIf(!await fileSecurity.CanReadAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);

        var properties = await fileDao.GetProperties(fileId) ?? new EntryProperties();

        if (properties.FormFilling != null)
        {
            if (!await fileSharing.CanSetAccessAsync(file) || !fileUtility.CanWebRestrictedEditing(file.Title))
            {
                properties.FormFilling = null;
            }
            else
            {
                var folderId = properties.FormFilling.ToFolderId;
                if (int.TryParse(folderId, out var fId))
                {
                    await SetFormFillingFolderProps(fId);
                }
                else
                {
                    await SetFormFillingFolderProps(folderId);
                }
            }
        }

        return properties;

        async Task SetFormFillingFolderProps<TProp>(TProp toFolderId)
        {
            var folderDao = daoFactory.GetFolderDao<TProp>();
            var folder = await folderDao.GetFolderAsync(toFolderId);

            if (folder == null)
            {
                properties.FormFilling.ToFolderId = null;
            }
            else if (await fileSecurity.CanCreateAsync(folder))
            {
                properties.FormFilling.ToFolderPath = null;
                var breadCrumbs = await entryManager.GetBreadCrumbsAsync(folder.Id, folderDao);
                properties.FormFilling.ToFolderPath = string.Join("/", breadCrumbs.Select(f => f.Title));
            }
        }
    }

    public async Task<EntryProperties> SetFileProperties<T>(T fileId, EntryProperties fileProperties)
    {
        var fileDao = GetFileDao<T>();

        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMassage_FileNotFound);
        }

        if (!await fileSecurity.CanEditAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException_RenameFile);
        }

        if (await entryManager.FileLockedForMeAsync(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMassage_LockedFile);
        }

        if (file.ProviderEntry)
        {
            throw new Exception(FilesCommonResource.ErrorMassage_BadRequest);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMassage_ViewTrashItem);
        }

        var currentProperies = await fileDao.GetProperties(fileId) ?? new EntryProperties();
        if (fileProperties != null)
        {
            if (fileProperties.FormFilling != null)
            {
                if (!await fileSharing.CanSetAccessAsync(file))
                {
                    throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
                }

                if (!fileUtility.CanWebRestrictedEditing(file.Title))
                {
                    throw new Exception(FilesCommonResource.ErrorMassage_NotSupportedFormat);
                }

                if (currentProperies.FormFilling == null)
                {
                    await using var scope = serviceScopeFactory.CreateAsyncScope();
                    currentProperies.FormFilling = scope.ServiceProvider.GetService<FormFillingProperties>();
                }

                currentProperies.FormFilling.CollectFillForm = fileProperties.FormFilling.CollectFillForm;

                if (!string.IsNullOrEmpty(fileProperties.FormFilling.ToFolderId))
                {
                    if (int.TryParse(fileProperties.FormFilling.ToFolderId, out var fId))
                    {
                        currentProperies.FormFilling.ToFolderId = await GetFormFillingFolder(fId);
                    }
                    else
                    {
                        currentProperies.FormFilling.ToFolderId = await GetFormFillingFolder(fId);
                    }
                }

                currentProperies.FormFilling.CreateFolderTitle = Global.ReplaceInvalidCharsAndTruncate(fileProperties.FormFilling.CreateFolderTitle);

                currentProperies.FormFilling.CreateFileMask = fileProperties.FormFilling.CreateFileMask;
                currentProperies.FormFilling.FixFileMask();
            }

            await fileDao.SaveProperties(file.Id, currentProperies);
        }

        return currentProperies;

        async Task<string> GetFormFillingFolder<TProp>(TProp toFolderId)
        {
            var folderDao = daoFactory.GetFolderDao<TProp>();

            var folder = await folderDao.GetFolderAsync(toFolderId);

            ErrorIf(folder == null, FilesCommonResource.ErrorMassage_FolderNotFound);
            ErrorIf(!await fileSecurity.CanCreateAsync(folder), FilesCommonResource.ErrorMassage_SecurityException_Create);

            return folder.Id.ToString();
        }
    }

    public async Task SetFileOrder<T>(T fileId, int order)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        file.NotFoundIfNull();
        ErrorIf(!await fileSecurity.CanEditAsync(file), FilesCommonResource.ErrorMassage_SecurityException);
        await fileDao.SetCustomOrder(fileId, file.ParentId, order);
    }

    public async Task SetFolderOrder<T>(T folderId, int order)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        folder.NotFoundIfNull();
        ErrorIf(!await fileSecurity.CanEditAsync(folder), FilesCommonResource.ErrorMassage_SecurityException);
        await folderDao.SetCustomOrder(folderId, folder.ParentId, order);
    }

    public async Task<List<FileEntry>> GetNewItemsAsync<T>(T folderId)
    {
        try
        {
            var folderDao = GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(folderId);

            var result = await fileMarker.MarkedItemsAsync(folder).Where(e => e.FileEntryType == FileEntryType.File).ToListAsync();

            result = new List<FileEntry>(entryManager.SortEntries<T>(result, new OrderBy(SortedByType.DateAndTime, false)));

            if (result.Count == 0)
            {
                await MarkAsReadAsync(new List<JsonElement> { JsonDocument.Parse(JsonSerializer.Serialize(folderId)).RootElement }, new List<JsonElement>()); //TODO
            }

            return result;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    #region MarkAsRead

    public async Task<List<FileOperationResult>> MarkAsReadAsync(List<JsonElement> foldersId, List<JsonElement> filesId)
    {
        if (foldersId.Count == 0 && filesId.Count == 0)
        {
            return GetTasksStatuses();
        }

        var (operations, _) = fileOperationsManager.MarkAsRead(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersId, filesId, GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync());

        return operations;
    }

    public async Task<List<FileOperationResult>> MarkAsReadAsync(List<string> foldersIdString, List<string> filesIdString, List<int> foldersIdInt, List<int> filesIdInt, IDictionary<string, StringValues> headers = null, string taskId = null)
    {
        if (foldersIdString == null && filesIdString == null && foldersIdInt == null && filesIdInt == null)
        {
            return GetTasksStatuses();
        }

        var (operations, _) = fileOperationsManager.MarkAsRead(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersIdString, filesIdString, foldersIdInt, filesIdInt, headers ?? GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), enqueueTask: true, taskId);

        return operations;
    }

    public async Task<(List<FileOperationResult>, string, IDictionary<string, StringValues>)> PublishMarkAsReadAsync(List<string> foldersIdString, List<string> filesIdString, List<int> foldersIdInt, List<int> filesIdInt)
    {
        if (foldersIdString == null && filesIdString == null && foldersIdInt == null && filesIdInt == null)
        {
            return (GetTasksStatuses(), null, null);
        }

        var headers = GetHttpHeaders();

        var (operations, taskId) = fileOperationsManager.MarkAsRead(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersIdString, filesIdString, foldersIdInt, filesIdInt, headers, await externalShare.GetCurrentShareDataAsync(), enqueueTask: false);

        return (operations, taskId, headers);
    }

    #endregion

    public IAsyncEnumerable<ThirdPartyParams> GetThirdPartyAsync()
    {
        var providerDao = GetProviderDao();
        if (providerDao == null)
        {
            return AsyncEnumerable.Empty<ThirdPartyParams>();
        }

        return InternalGetThirdPartyAsync(providerDao);
    }

    private async IAsyncEnumerable<ThirdPartyParams> InternalGetThirdPartyAsync(IProviderDao providerDao)
    {
        await foreach (var r in providerDao.GetProvidersInfoAsync())
        {
            yield return new ThirdPartyParams
            {
                CustomerTitle = r.CustomerTitle,
                Corporate = r.RootFolderType == FolderType.COMMON,
                ProviderId = r.ProviderId.ToString(),
                ProviderKey = r.ProviderKey
            };
        }
    }

    public async ValueTask<Folder<string>> GetBackupThirdPartyAsync()
    {
        var providerDao = GetProviderDao();
        if (providerDao == null)
        {
            return null;
        }

        var providerInfo = await providerDao.GetProvidersInfoAsync(FolderType.ThirdpartyBackup).SingleOrDefaultAsync();

        if (providerInfo != null)
        {
            var folderDao = GetFolderDao<string>();
            var folder = await folderDao.GetFolderAsync(providerInfo.RootFolderId);
            ErrorIf(!await fileSecurity.CanReadAsync(folder), FilesCommonResource.ErrorMassage_SecurityException_ViewFolder);

            return folder;
        }

            return null;
        }

    public IAsyncEnumerable<FileEntry> GetThirdPartyFolderAsync(int folderType = 0)
    {
        if (!filesSettingsHelper.EnableThirdParty)
        {
            return AsyncEnumerable.Empty<FileEntry>();
        }

        var providerDao = GetProviderDao();
        if (providerDao == null)
        {
            return AsyncEnumerable.Empty<FileEntry>();
        }

        return InternalGetThirdPartyFolderAsync(folderType, providerDao);
    }

    private async IAsyncEnumerable<FileEntry> InternalGetThirdPartyFolderAsync(int folderType, IProviderDao providerDao)
    {
        await foreach (var providerInfo in providerDao.GetProvidersInfoAsync((FolderType)folderType))
        {
            var folder = entryManager.GetFakeThirdpartyFolder(providerInfo);
            folder.NewForMe = folder.RootFolderType == FolderType.COMMON ? 1 : 0;

            yield return folder;
        }
    }

    public async ValueTask<Folder<string>> SaveThirdPartyAsync(ThirdPartyParams thirdPartyParams)
    {
        var providerDao = GetProviderDao();

        if (providerDao == null)
        {
            return null;
        }

        var folderDaoInt = daoFactory.GetFolderDao<int>();
        var folderDao = GetFolderDao<string>();

        ErrorIf(thirdPartyParams == null, FilesCommonResource.ErrorMassage_BadRequest);

        var folderId = thirdPartyParams.Corporate && !coreBaseSettings.Personal ? await globalFolderHelper.FolderCommonAsync
            : thirdPartyParams.RoomsStorage && !coreBaseSettings.DisableDocSpace ? await globalFolderHelper.FolderVirtualRoomsAsync : await globalFolderHelper.FolderMyAsync;

        var parentFolder = await folderDaoInt.GetFolderAsync(folderId);

        ErrorIf(!await fileSecurity.CanCreateAsync(parentFolder), FilesCommonResource.ErrorMassage_SecurityException_Create);
        ErrorIf(!filesSettingsHelper.EnableThirdParty, FilesCommonResource.ErrorMassage_SecurityException_Create);

        var lostFolderType = FolderType.USER;
        var folderType = thirdPartyParams.Corporate ? FolderType.COMMON : thirdPartyParams.RoomsStorage ? FolderType.VirtualRooms : FolderType.USER;

        int curProviderId;

        MessageAction messageAction;
        if (string.IsNullOrEmpty(thirdPartyParams.ProviderId))
        {
            ErrorIf(!thirdpartyConfiguration.SupportInclusion(daoFactory)
                    ||
                    (!filesSettingsHelper.EnableThirdParty
                     && !coreBaseSettings.Personal)
                    , FilesCommonResource.ErrorMassage_SecurityException_Create);

            thirdPartyParams.CustomerTitle = Global.ReplaceInvalidCharsAndTruncate(thirdPartyParams.CustomerTitle);
            ErrorIf(string.IsNullOrEmpty(thirdPartyParams.CustomerTitle), FilesCommonResource.ErrorMassage_InvalidTitle);

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
            curProviderId = Convert.ToInt32(thirdPartyParams.ProviderId);

            var lostProvider = await providerDao.GetProviderInfoAsync(curProviderId);
            ErrorIf(lostProvider.Owner != authContext.CurrentAccount.ID, FilesCommonResource.ErrorMassage_SecurityException);

            lostFolderType = lostProvider.RootFolderType;
            if (lostProvider.RootFolderType == FolderType.COMMON && !thirdPartyParams.Corporate)
            {
                var lostFolder = await folderDao.GetFolderAsync(lostProvider.RootFolderId);
                await fileMarker.RemoveMarkAsNewForAllAsync(lostFolder);
            }

            curProviderId = await providerDao.UpdateProviderInfoAsync(curProviderId, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
            messageAction = MessageAction.ThirdPartyUpdated;
        }

        var provider = await providerDao.GetProviderInfoAsync(curProviderId);
        await provider.InvalidateStorageAsync();

        var folderDao1 = GetFolderDao<string>();
        var folder = await folderDao1.GetFolderAsync(provider.RootFolderId);
        ErrorIf(!await fileSecurity.CanReadAsync(folder), FilesCommonResource.ErrorMassage_SecurityException_ViewFolder);

        await filesMessageService.SendAsync(messageAction, parentFolder, folder.Id, provider.ProviderKey);

        if (thirdPartyParams.Corporate && lostFolderType != FolderType.COMMON)
        {
            await fileMarker.MarkAsNewAsync(folder);
        }

        return folder;
    }

    public async ValueTask<Folder<string>> SaveThirdPartyBackupAsync(ThirdPartyParams thirdPartyParams)
    {
        var providerDao = GetProviderDao();

        if (providerDao == null)
        {
            return null;
        }

        ErrorIf(thirdPartyParams == null, FilesCommonResource.ErrorMassage_BadRequest);
        ErrorIf(!filesSettingsHelper.EnableThirdParty, FilesCommonResource.ErrorMassage_SecurityException_Create);

        var folderType = FolderType.ThirdpartyBackup;

        int curProviderId;

        MessageAction messageAction;

        var thirdparty = await GetBackupThirdPartyAsync();
        if (thirdparty == null)
        {
            ErrorIf(!thirdpartyConfiguration.SupportInclusion(daoFactory)
                    ||
                    (!filesSettingsHelper.EnableThirdParty
                     && !coreBaseSettings.Personal)
                    , FilesCommonResource.ErrorMassage_SecurityException_Create);

            thirdPartyParams.CustomerTitle = Global.ReplaceInvalidCharsAndTruncate(thirdPartyParams.CustomerTitle);
            ErrorIf(string.IsNullOrEmpty(thirdPartyParams.CustomerTitle), FilesCommonResource.ErrorMassage_InvalidTitle);

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

        var folderDao1 = GetFolderDao<string>();
        var folder = await folderDao1.GetFolderAsync(provider.RootFolderId);

        await filesMessageService.SendAsync(messageAction, folder.Id, provider.ProviderKey);

        return folder;
    }

    public async ValueTask<object> DeleteThirdPartyAsync(string providerId)
    {
        var providerDao = GetProviderDao();
        if (providerDao == null)
        {
            return null;
        }

        var curProviderId = Convert.ToInt32(providerId);
        var providerInfo = await providerDao.GetProviderInfoAsync(curProviderId);

        var folder = entryManager.GetFakeThirdpartyFolder(providerInfo);
        ErrorIf(!await fileSecurity.CanDeleteAsync(folder), FilesCommonResource.ErrorMassage_SecurityException_DeleteFolder);

        if (providerInfo.RootFolderType == FolderType.COMMON)
        {
            await fileMarker.RemoveMarkAsNewForAllAsync(folder);
        }

        await providerDao.RemoveProviderInfoAsync(folder.ProviderId);
        await filesMessageService.SendAsync(MessageAction.ThirdPartyDeleted, folder, folder.Id, providerInfo.ProviderKey);

        return folder.Id;
    }

    public async Task<bool> ChangeAccessToThirdpartyAsync(bool enable)
    {
        ErrorIf(!await global.IsDocSpaceAdministratorAsync, FilesCommonResource.ErrorMassage_SecurityException);

        filesSettingsHelper.EnableThirdParty = enable;
        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsThirdPartySettingsUpdated);

        return filesSettingsHelper.EnableThirdParty;
    }

    public async Task<bool> SaveDocuSignAsync(string code)
    {
        ErrorIf(!authContext.IsAuthenticated
                || await userManager.IsUserAsync(authContext.CurrentAccount.ID)
                || !filesSettingsHelper.EnableThirdParty
                || !thirdpartyConfiguration.SupportDocuSignInclusion, FilesCommonResource.ErrorMassage_SecurityException_Create);

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
            ErrorIf(await userManager.IsUserAsync(authContext.CurrentAccount.ID)
                    || !filesSettingsHelper.EnableThirdParty
                    || !thirdpartyConfiguration.SupportDocuSignInclusion, FilesCommonResource.ErrorMassage_SecurityException_Create);

            return await docuSignHelper.SendDocuSignAsync(fileId, docuSignData);
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    public List<FileOperationResult> GetTasksStatuses()
    {
        return fileOperationsManager.GetOperationResults(authContext.CurrentAccount.ID);
    }

    public List<FileOperationResult> TerminateTasks(string id = null)
    {
        return fileOperationsManager.CancelOperations(authContext.CurrentAccount.ID, id);
    }

    #region BulkDownload

    public async Task<List<FileOperationResult>> BulkDownloadAsync(Dictionary<JsonElement, string> folders, Dictionary<JsonElement, string> files, IDictionary<string, StringValues> headers = null, string taskId = null, string baseUri = null)
    {
        ErrorIf(folders.Count == 0 && files.Count == 0, FilesCommonResource.ErrorMassage_BadRequest);

        var (operations, _) = fileOperationsManager.Download(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), folders, files, headers ?? GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), enqueueTask: true, taskId, baseUri);

        return operations;
    }

    public async Task<(List<FileOperationResult>, string, IDictionary<string, StringValues>)> PublishBulkDownloadAsync(Dictionary<JsonElement, string> folders, Dictionary<JsonElement, string> files)
    {
        ErrorIf(folders.Count == 0 && files.Count == 0, FilesCommonResource.ErrorMassage_BadRequest);

        var headers = GetHttpHeaders();

        var (operations, taskId) = fileOperationsManager.Download(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), folders, files, headers, await externalShare.GetCurrentShareDataAsync(), enqueueTask: false);

        return (operations, taskId, headers);
    }

    #endregion

    #region MoveOrCopy

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

    private async Task<(List<TFrom>, List<TFrom>)> MoveOrCopyFilesCheckAsync<TFrom, TTo>(IEnumerable<TFrom> filesId, IEnumerable<TFrom> foldersId, TTo destFolderId)
    {
        var checkedFiles = new List<TFrom>();
        var checkedFolders = new List<TFrom>();
        var folderDao = daoFactory.GetFolderDao<TFrom>();
        var fileDao = daoFactory.GetFileDao<TFrom>();
        var destFolderDao = daoFactory.GetFolderDao<TTo>();
        var destFileDao = daoFactory.GetFileDao<TTo>();

        var toFolder = await destFolderDao.GetFolderAsync(destFolderId);
        ErrorIf(toFolder == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanCreateAsync(toFolder), FilesCommonResource.ErrorMassage_SecurityException_Create);

        foreach (var id in filesId)
        {
            var file = await fileDao.GetFileAsync(id);
            if (file is { Encrypted: false }
                && await destFileDao.IsExistAsync(file.Title, toFolder.Id))
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

    public async Task<List<FileOperationResult>> MoveOrCopyItemsAsync(List<JsonElement> foldersId, List<JsonElement> filesId, JsonElement destFolderId, FileConflictResolveType resolve, bool copy, bool deleteAfter = false, bool content = false)
    {
        ErrorIf(resolve == FileConflictResolveType.Overwrite && await userManager.IsUserAsync(authContext.CurrentAccount.ID), FilesCommonResource.ErrorMassage_SecurityException);

        if (foldersId.Count > 0 || filesId.Count > 0)
        {
            var (operations, _) = await fileOperationsManager.MoveOrCopyAsync(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersId, filesId, destFolderId, copy, resolve, !deleteAfter, GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), content);

            return operations;
        }

        return fileOperationsManager.GetOperationResults(authContext.CurrentAccount.ID);
    }

    public async Task<List<FileOperationResult>> MoveOrCopyItemsAsync(List<string> foldersIdString, List<string> filesIdString, List<int> foldersIdInt, List<int> filesIdInt, JsonElement destFolderId, FileConflictResolveType resolve, bool copy, bool deleteAfter = false, bool content = false, IDictionary<string, StringValues> headers = null, string taskId = null)
    {
        ErrorIf(resolve == FileConflictResolveType.Overwrite && await userManager.IsUserAsync(authContext.CurrentAccount.ID), FilesCommonResource.ErrorMassage_SecurityException);

        if (filesIdString != null || foldersIdString != null || filesIdInt != null || foldersIdInt != null)
        {
            var (operations, _) = await fileOperationsManager.MoveOrCopyAsync(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersIdString, filesIdString, foldersIdInt, filesIdInt, destFolderId, copy, resolve, !deleteAfter, headers ?? GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), content, enqueueTask: true, taskId);

            return operations;
        }

        return fileOperationsManager.GetOperationResults(authContext.CurrentAccount.ID);
    }

    public async Task<(List<FileOperationResult>, string, IDictionary<string, StringValues>)> PublishMoveOrCopyItemsAsync(List<string> foldersIdString, List<string> filesIdString, List<int> foldersIdInt, List<int> filesIdInt, JsonElement destFolderId, FileConflictResolveType resolve, bool copy, bool deleteAfter = false, bool content = false)
    {
        ErrorIf(resolve == FileConflictResolveType.Overwrite && await userManager.IsUserAsync(authContext.CurrentAccount.ID), FilesCommonResource.ErrorMassage_SecurityException);

        if (filesIdString != null || foldersIdString != null || filesIdInt != null || foldersIdInt != null)
        {
            var headers = GetHttpHeaders();

            var (operations, taskId) = await fileOperationsManager.MoveOrCopyAsync(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersIdString, filesIdString, foldersIdInt, filesIdInt, destFolderId, copy, resolve, !deleteAfter, headers, await externalShare.GetCurrentShareDataAsync(), content, enqueueTask: false);

            return (operations, taskId, headers);
        }

        return (fileOperationsManager.GetOperationResults(authContext.CurrentAccount.ID), null, null);
    }

    #endregion

    #region Delete

    public async Task<List<FileOperationResult>> DeleteFileAsync<T>(T fileId, bool ignoreException = false, bool deleteAfter = false, bool immediately = false)
    {
        var (operations, _) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), new List<T>(), new List<T>() { fileId }, ignoreException,
            !deleteAfter, immediately, GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), false, enqueueTask: true);

        return operations;
    }

    public async Task<List<FileOperationResult>> DeleteFolderAsync<T>(T folderId, bool ignoreException = false, bool deleteAfter = false, bool immediately = false)
    {
        var (operations, _) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), new List<T>() { folderId }, new List<T>(), ignoreException,
            !deleteAfter, immediately, GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), false, enqueueTask: true);

        return operations;
    }

    public async Task<List<FileOperationResult>> DeleteItemsAsync(IEnumerable<JsonElement> files, IEnumerable<JsonElement> folders, bool ignoreException = false, bool deleteAfter = false, bool immediately = false)
    {
        var (operations, _) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), folders, files, ignoreException, !deleteAfter, immediately,
            GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), false, enqueueTask: true);

        return operations;
    }

    public async Task<List<FileOperationResult>> DeleteItemsAsync(IEnumerable<string> foldersIdString, IEnumerable<string> filesIdString, IEnumerable<int> foldersIdInt, IEnumerable<int> filesIdInt, bool ignoreException = false, bool deleteAfter = false, bool immediately = false, IDictionary<string, StringValues> headers = null, string taskId = null)
    {
        var (operations, _) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersIdString, filesIdString, foldersIdInt, filesIdInt, ignoreException, !deleteAfter, immediately,
            headers ?? GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), false, enqueueTask: true, taskId);

        return operations;
    }

    public async Task<(List<FileOperationResult>, string, IDictionary<string, StringValues>)> PublishDeleteItemsAsync(IEnumerable<string> foldersIdString, IEnumerable<string> filesIdString, IEnumerable<int> foldersIdInt, IEnumerable<int> filesIdInt, bool ignoreException = false, bool deleteAfter = false, bool immediately = false)
    {
        var headers = GetHttpHeaders();

        var (operations, taskId) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersIdString, filesIdString, foldersIdInt, filesIdInt, ignoreException, !deleteAfter, immediately,
            headers, await externalShare.GetCurrentShareDataAsync(), false, enqueueTask: false);

        return (operations, taskId, headers);
    }

    public async Task<List<FileOperationResult>> DeleteItemsAsync<T>(IEnumerable<T> files, IEnumerable<T> folders, bool ignoreException = false, bool deleteAfter = false, bool immediately = false)
    {
        var (operations, _) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), folders, files, ignoreException, !deleteAfter, immediately,
            GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), false, enqueueTask: true);

        return operations;
    }

    #endregion

    #region EmptyTrash

    public async Task<List<FileOperationResult>> EmptyTrashAsync(IDictionary<string, StringValues> headers = null, string taskId = null)
    {
        var (foldersId, filesId) = await GetTrashContentAsync();

        var (operations, _) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersId, filesId, false, true,
            false, headers ?? GetHttpHeaders(), await externalShare.GetCurrentShareDataAsync(), true, enqueueTask: true, taskId);

        return operations;
    }

    public async Task<(List<FileOperationResult>, string, IDictionary<string, StringValues>)> PublishEmptyTrashAsync()
    {
        var (foldersId, filesId) = await GetTrashContentAsync();
        var headers = GetHttpHeaders();

        var (operations, taskId) = fileOperationsManager.Delete(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantAsync(), foldersId, filesId, false, true,
            false, headers, await externalShare.GetCurrentShareDataAsync(), true, enqueueTask: false);

        return (operations, taskId, headers);
    }

    private async Task<(List<int>, List<int>)> GetTrashContentAsync()
    {
        var folderDao = GetFolderDao<int>();
        var fileDao = GetFileDao<int>();
        var trashId = await folderDao.GetFolderIDTrashAsync(true);
        var foldersIdTask = await folderDao.GetFoldersAsync(trashId).Select(f => f.Id).ToListAsync();
        var filesIdTask = await fileDao.GetFilesAsync(trashId).ToListAsync();

        return (foldersIdTask, filesIdTask);
    }

    #endregion

    public async IAsyncEnumerable<FileOperationResult> CheckConversionAsync<T>(List<CheckConversionRequestDto<T>> filesInfoJSON, bool sync = false)
    {
        if (filesInfoJSON == null || filesInfoJSON.Count == 0)
        {
            yield break;
        }

        var results = AsyncEnumerable.Empty<FileOperationResult>();
        var fileDao = GetFileDao<T>();
        var files = new List<KeyValuePair<File<T>, bool>>();
        foreach (var fileInfo in filesInfoJSON)
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

            ErrorIf(!await fileSecurity.CanConvertAsync(file), FilesCommonResource.ErrorMassage_SecurityException_ReadFile);

            if (fileInfo.StartConvert && fileConverter.MustConvert(file))
            {
                try
                {
                    if (sync)
                    {
                        results = results.Append(await fileConverter.ExecSynchronouslyAsync(file, fileInfo.Password, !fileInfo.CreateNewIfExist));
                    }
                    else
                    {
                        await fileConverter.ExecAsynchronouslyAsync(file, false, !fileInfo.CreateNewIfExist, fileInfo.Password);
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

    public async Task<string> CheckFillFormDraftAsync<T>(T fileId, int version, string doc, bool editPossible, bool view)
    {
        var (file, configuration, _) = await documentServiceHelper.GetParamsAsync(fileId, version, doc, editPossible, !view, true);
        var validShareLink = !string.IsNullOrEmpty(await fileShareLink.ParseAsync(doc));

        if (validShareLink)
        {
            configuration.Document.SharedLinkKey += doc;
        }

        if (configuration.EditorConfig.ModeWrite
            && fileUtility.CanWebRestrictedEditing(file.Title)
            && await fileSecurity.CanFillFormsAsync(file)
            && !await fileSecurity.CanEditAsync(file))
        {
            if (!await entryManager.LinkedForMeAsync(file))
            {
                await fileMarker.RemoveMarkAsNewAsync(file);

                Folder<T> folderIfNew;
                File<T> form;
                try
                {
                    (form, folderIfNew) = await entryManager.GetFillFormDraftAsync(file);
                }
                catch (Exception ex)
                {
                    _logger.ErrorDocEditor(ex);
                    throw;
                }

                var comment = folderIfNew == null
                    ? string.Empty
                    : "#message/" + HttpUtility.UrlEncode(string.Format(FilesCommonResource.MessageFillFormDraftCreated, folderIfNew.Title));

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
        ErrorIf(Equals(userTo, Constants.LostUser), FilesCommonResource.ErrorMassage_UserNotFound);

        //check user can have personal data
        ErrorIf(await userManager.IsUserAsync(userTo), FilesCommonResource.ErrorMassage_SecurityException);
    }

    public async Task DemandPermissionToDeletePersonalDataAsync(Guid userFromId)
    {
        var userFrom = await userManager.GetUsersAsync(userFromId);

        await DemandPermissionToDeletePersonalDataAsync(userFrom);
    }

    public async Task DemandPermissionToDeletePersonalDataAsync(UserInfo userFrom)
    {
        //check current user have access
        ErrorIf(!await global.IsDocSpaceAdministratorAsync, FilesCommonResource.ErrorMassage_SecurityException);

        //check exist userFrom
        ErrorIf(Equals(userFrom, Constants.LostUser), FilesCommonResource.ErrorMassage_UserNotFound);

        //check user have personal data
        ErrorIf(await userManager.IsUserAsync(userFrom), FilesCommonResource.ErrorMassage_SecurityException);
    }

    public async Task<List<T>> GetPersonalFolderIdsAsync<T>(Guid userId)
        {
        var result = new List<T>();

        var folderDao = GetFolderDao<T>();
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

    public async Task DeletePersonalDataAsync<T>(Guid userFromId, bool checkPermission = false)
        {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        var folderDao = GetFolderDao<T>();
        if (folderDao == null)
            {
            return;
        }

        _logger.InformationDeletePersonalData(userFromId);

        var folderIdFromMy = await folderDao.GetFolderIDUserAsync(false, userFromId);
        if (!Equals(folderIdFromMy, 0))
        {
            await folderDao.DeleteFolderAsync(folderIdFromMy);
        }

        var folderIdFromTrash = await folderDao.GetFolderIDTrashAsync(false, userFromId);
        if (!Equals(folderIdFromTrash, 0))
        {
            await folderDao.DeleteFolderAsync(folderIdFromTrash);
            }

        await fileSecurity.RemoveSubjectAsync<T>(userFromId, true);
    }

    public async Task ReassignProvidersAsync(Guid userFromId, Guid userToId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var providerDao = GetProviderDao();
        if (providerDao == null)
        {
            return;
        }

        //move thirdparty storage userFrom
        await foreach (var commonProviderInfo in providerDao.GetProvidersInfoAsync(userFromId))
            {
            _logger.InformationReassignProvider(commonProviderInfo.ProviderId, userFromId, userToId);
            await providerDao.UpdateProviderInfoAsync(commonProviderInfo.ProviderId, null, null, FolderType.DEFAULT, userToId);
            }
        }

    public async Task ReassignFoldersAsync<T>(Guid userFromId, Guid userToId, IEnumerable<T> exceptFolderIds, bool checkPermission = false)
                {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var folderDao = GetFolderDao<T>();
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

        var fileDao = GetFileDao<T>();
        if (fileDao == null)
        {
            return;
    }

        _logger.InformationReassignFiles(userFromId, userToId);

        await fileDao.ReassignFilesAsync(userFromId, userToId, exceptFolderIds);
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
        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
        }

        var tagDao = GetTagDao<T>();
        var fileDao = GetFileDao<T>();
        var folderDao = GetFolderDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId).Where(file => !file.Encrypted)).ToListAsync();
        var folders = fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)).ToListAsync();

        List<FileEntry<T>> entries = new();

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

    public async Task<List<FileEntry<T>>> DeleteFavoritesAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId)
    {
        var tagDao = GetTagDao<T>();
        var fileDao = GetFileDao<T>();
        var folderDao = GetFolderDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)).ToListAsync();
        var folders = fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)).ToListAsync();

        List<FileEntry<T>> entries = new();

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

        return entries;
    }

    #endregion

    #region Templates Manager

    public async ValueTask<List<FileEntry<T>>> AddToTemplatesAsync<T>(IEnumerable<T> filesId)
    {
        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
        }

        var tagDao = GetTagDao<T>();
        var fileDao = GetFileDao<T>();

        var files = await fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId))
            .Where(file => fileUtility.ExtsWebTemplate.Contains(FileUtility.GetFileExtension(file.Title), StringComparer.CurrentCultureIgnoreCase))
            .ToListAsync();

        var tags = files.Select(file => Tag.Template(authContext.CurrentAccount.ID, file));

        await tagDao.SaveTagsAsync(tags);

        return files;
    }

    public async Task<List<FileEntry<T>>> DeleteTemplatesAsync<T>(IEnumerable<T> filesId)
    {
        var tagDao = GetTagDao<T>();
        var fileDao = GetFileDao<T>();

        var files = await fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)).ToListAsync();

        var tags = files.Select(file => Tag.Template(authContext.CurrentAccount.ID, file));

        await tagDao.RemoveTagsAsync(tags);

        return files;
    }

    public async IAsyncEnumerable<FileEntry<T>> GetTemplatesAsync<T>(FilterType filter, int from, int count, bool subjectGroup, string subjectID, string searchText, string[] extension,
        bool searchInContent)
    {
        var subjectId = string.IsNullOrEmpty(subjectID) ? Guid.Empty : new Guid(subjectID);
        var folderDao = GetFolderDao<T>();
        var fileDao = GetFileDao<T>();

        var result = entryManager.GetTemplatesAsync(folderDao, fileDao, filter, subjectGroup, subjectId, searchText, extension, searchInContent);

        await foreach (var r in result.Skip(from).Take(count))
        {
            yield return r;
        }
    }

    #endregion

    public async Task<List<AceWrapper>> GetSharedInfoAsync<T>(
        IEnumerable<T> fileIds,
        IEnumerable<T> folderIds,
        IEnumerable<SubjectType> subjectTypes = null)
    {
        return await fileSharing.GetSharedInfoAsync(fileIds, folderIds, subjectTypes);
    }

    public async IAsyncEnumerable<AceWrapper> GetRoomSharedInfoAsync<T>(T roomId, ShareFilterType filterType, int offset, int count)
    {
        var room = await GetFolderDao<T>().GetFolderAsync(roomId).NotFoundIfNull();

        await foreach (var ace in fileSharing.GetPureSharesAsync(room, filterType, null, offset, count))
        {
            yield return ace;
    }
    }

    public async Task<int> GetRoomSharesCountAsync<T>(T roomId, ShareFilterType filterType)
    {
        var room = (await GetFolderDao<T>().GetFolderAsync(roomId)).NotFoundIfNull();

        return await fileSharing.GetPureSharesCountAsync(room, filterType);
    }

    public async IAsyncEnumerable<AceWrapper> GetRoomSharedInfoAsync<T>(T roomId, IEnumerable<Guid> subjects)
    {
        var room = await GetFolderDao<T>().GetFolderAsync(roomId).NotFoundIfNull();

        await foreach (var ace in fileSharing.GetPureSharesAsync(room, subjects))
        {
            yield return ace;
        }
    }

    public async Task<AceWrapper> GetPrimaryExternalLinkAsync<T>(T entryId, FileEntryType entryType)
    {
        FileEntry<T> entry = entryType == FileEntryType.File 
            ? await GetFileDao<T>().GetFileAsync(entryId)
            : await GetFolderDao<T>().GetFolderAsync(entryId);

        entry.NotFoundIfNull();

        var link = await fileSharing.GetPureSharesAsync(entry, ShareFilterType.PrimaryExternalLink, null, 0, 1)
            .FirstOrDefaultAsync();

        if (link == null)
        {
            return await SetExternalLinkAsync(entry, Guid.NewGuid(), FileShare.Read, FilesCommonResource.DefaultExternalLinkTitle, primary: true);
        }

        return link;
    }

    public async Task<string> SetAceObjectAsync<T>(AceCollection<T> aceCollection, bool notify, string culture = null)
    {
        var fileDao = GetFileDao<T>();
        var folderDao = GetFolderDao<T>();

        var entries = new List<FileEntry<T>>();
        string warning = null;

        foreach (var fileId in aceCollection.Files)
        {
            entries.Add(await fileDao.GetFileAsync(fileId));
        }

        foreach (var folderId in aceCollection.Folders)
        {
            entries.Add(await folderDao.GetFolderAsync(folderId));
        }

        foreach (var entry in entries)
        {
            try
            {
                var result = await fileSharingAceHelper.SetAceObjectAsync(aceCollection.Aces, entry, notify, aceCollection.Message, aceCollection.AdvancedSettings, culture);
                warning ??= result.Warning;

                if (!result.Changed)
                {
                        continue;
                    }

                foreach (var (eventType, ace) in result.HandledAces)
                    {
                    if (ace.IsLink)
                    {
                        continue;
                    }

                    var user = !string.IsNullOrEmpty(ace.Email)
                        ? await userManager.GetUserByEmailAsync(ace.Email)
                        : await userManager.GetUsersAsync(ace.Id);

                    var name = user.DisplayUserName(false, displayUserSettingsHelper);

                    if (entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType))
                {
                        switch (eventType)
                    {
                                case EventType.Create:
                                await filesMessageService.SendAsync(MessageAction.RoomCreateUser, entry, user.Id, name, 
                                    FileShareExtensions.GetAccessString(ace.Access, true));
                                    break;
                                case EventType.Remove:
                                await filesMessageService.SendAsync(MessageAction.RoomRemoveUser, entry, user.Id, name, 
                                    FileShareExtensions.GetAccessString(ace.Access, true));
                                    break;
                                case EventType.Update:
                                await filesMessageService.SendAsync(MessageAction.RoomUpdateAccessForUser, entry, user.Id, name,
                                    FileShareExtensions.GetAccessString(ace.Access, true));
                                    break;
                            }
                        }
                        else
                        {

                        await filesMessageService.SendAsync(
                            entry.FileEntryType == FileEntryType.Folder ? MessageAction.FolderUpdatedAccessFor : MessageAction.FileUpdatedAccessFor,entry,
                            entry.Title, name, FileShareExtensions.GetAccessString(ace.Access));
                        }
                    }
                }
            catch (Exception e)
            {
                throw GenerateException(e);
            }
        }

        return warning;
    }

    public async Task RemoveAceAsync<T>(List<T> filesId, List<T> foldersId)
    {
        ErrorIf(!authContext.IsAuthenticated, FilesCommonResource.ErrorMassage_SecurityException);

        var fileDao = GetFileDao<T>();
        var folderDao = GetFolderDao<T>();

        foreach (var fileId in filesId)
        {
            var entry = await fileDao.GetFileAsync(fileId);
            await fileSharingAceHelper.RemoveAceAsync(entry);
            await filesMessageService.SendAsync(MessageAction.FileRemovedFromList, entry, entry.Title);

        }

        foreach (var folderId in foldersId)
        {
            var entry = await folderDao.GetFolderAsync(folderId);
            await fileSharingAceHelper.RemoveAceAsync(entry);
            await filesMessageService.SendAsync(MessageAction.FolderRemovedFromList, entry, entry.Title);
        }
    }

    public async Task<string> GetShortenLinkAsync<T>(T fileId)
    {
        var fileDao = GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        ErrorIf(!await fileSharing.CanSetAccessAsync(file), FilesCommonResource.ErrorMassage_SecurityException);
        var shareLink = await fileShareLink.GetLinkAsync(file);

        try
        {
            return await urlShortener.GetShortenLinkAsync(shareLink);
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    public async Task<AceWrapper> SetInvitationLinkAsync<T>(T roomId, Guid linkId, string title, FileShare share)
    {
        var room = (await GetFolderDao<T>().GetFolderAsync(roomId)).NotFoundIfNull();

        var options = new FileShareOptions
        {
            Title = !string.IsNullOrEmpty(title) ? title : FilesCommonResource.DefaultInvitationLinkTitle,
            ExpirationDate = DateTime.UtcNow.Add(invitationLinkHelper.IndividualLinkExpirationInterval)
        };

        var result = await SetAceLinkAsync(room, SubjectType.InvitationLink, linkId, share, options, _actions[SubjectType.InvitationLink]);

        if (result != null)
            {
            linkId = result.Item2.Id;
            }

        return (await fileSharing.GetPureSharesAsync(room, new[] { linkId }).FirstOrDefaultAsync());
                }

    public async Task<AceWrapper> SetExternalLinkAsync<T>(T entryId, FileEntryType entryType, Guid linkId, string title, FileShare share, DateTime expirationDate = default,
        string password = null, bool denyDownload = false)
        {
        FileEntry<T> entry = entryType == FileEntryType.File 
            ? await GetFileDao<T>().GetFileAsync(entryId)
            : await GetFolderDao<T>().GetFolderAsync(entryId);

        return await SetExternalLinkAsync(entry.NotFoundIfNull(), linkId, share, title, expirationDate, password, denyDownload);
    }

    public async Task<bool> SetAceLinkAsync<T>(T fileId, FileShare share)
    {
        var fileDao = GetFileDao<T>();
        FileEntry<T> file = await fileDao.GetFileAsync(fileId);
        var aces = new List<AceWrapper>
            {
                new()
                {
                    Access = share,
                    Id = FileConstant.ShareLinkId,
                    SubjectGroup = true
                }
            };

        try
        {
            var result = await fileSharingAceHelper.SetAceObjectAsync(aces, file, false, null, null);
            if (result.Changed)
            {
                await filesMessageService.SendAsync(MessageAction.FileExternalLinkAccessUpdated, file, file.Title, FileShareExtensions.GetAccessString(share));
            }
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }

        var securityDao = GetSecurityDao<T>();

        return await securityDao.IsSharedAsync(file.Id, FileEntryType.File);
    }

    public async Task<List<MentionWrapper>> SharedUsersAsync<T>(T fileId)
    {
        if (!authContext.IsAuthenticated || coreBaseSettings.Personal)
        {
            return null;
        }

        return await InternalSharedUsersAsync(fileId);
    }

    public async Task<FileReference<T>> GetReferenceDataAsync<T>(T fileId, string portalName, T sourceFileId, string path)
    {
        File<T> file = null;
        var fileDao = daoFactory.GetFileDao<T>();
        if (portalName == (await tenantManager.GetCurrentTenantIdAsync()).ToString())
        {
            file = await fileDao.GetFileAsync(fileId);
        }

        if (file == null)
        {
            var source = await fileDao.GetFileAsync(sourceFileId);

            if (source == null)
            {
                return new FileReference<T>
                {
                    Error = FilesCommonResource.ErrorMassage_FileNotFound
                };
            }

            if (!await fileSecurity.CanReadAsync(source))
            {
                return new FileReference<T>
                {
                    Error = FilesCommonResource.ErrorMassage_SecurityException_ReadFile
                };
            }

            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(source.ParentId);
            if (!await fileSecurity.CanReadAsync(folder))
            {
                return new FileReference<T>
                {
                    Error = FilesCommonResource.ErrorMassage_SecurityException_ReadFolder
                };
            }

            var list = fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, path, null, false);
            file = await list.FirstOrDefaultAsync(fileItem => fileItem.Title == path);
        }

        if (!await fileSecurity.CanReadAsync(file))
        {
            return new FileReference<T>
            {
                Error = FilesCommonResource.ErrorMassage_SecurityException_ReadFile
            };
        }
        var fileStable = file;
        if (file.Forcesave != ForcesaveType.None)
        {
            fileStable = await fileDao.GetFileStableAsync(file.Id, file.Version);
        }
        var docKey = await documentServiceHelper.GetDocKeyAsync(fileStable);

        var fileReference = new FileReference<T>
        {
            Path = file.Title,
            ReferenceData = new FileReferenceData<T>
            {
                FileKey = file.Id,
                InstanceId = (await tenantManager.GetCurrentTenantIdAsync()).ToString()
            },
            Url = await documentServiceConnector.ReplaceCommunityAdressAsync(await pathProvider.GetFileStreamUrlAsync(file, lastVersion: true)),
            FileType = file.ConvertedExtension.Trim('.'),
            Key = docKey,
            Link = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id))
        };
        fileReference.Token = documentServiceHelper.GetSignature(fileReference);
        return fileReference;
    }

    public async Task<List<MentionWrapper>> InternalSharedUsersAsync<T>(T fileId)
    {
        var fileDao = GetFileDao<T>();

        FileEntry<T> file = await fileDao.GetFileAsync(fileId);

        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);

        var folderDao = GetFolderDao<T>();

        var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(file);

        var access = await fileSharing.GetSharedInfoAsync(Enumerable.Empty<int>(), new[] { roomId });
        var usersIdWithAccess = access.Where(aceWrapper => !aceWrapper.SubjectGroup
                                        && aceWrapper.Access != FileShare.Restrict)
                                      .Select(aceWrapper => aceWrapper.Id);

        var users = usersIdWithAccess
            .Where(id => !id.Equals(authContext.CurrentAccount.ID))
            .Select(userManager.GetUsers);

        var result = users
            .Where(u => u.Status != EmployeeStatus.Terminated)
            .Select(u => new MentionWrapper(u, displayUserSettingsHelper))
            .OrderBy(u => u.User, UserInfoComparer.Default)
            .ToList();

        return result;
    }

    public async Task<Folder<T>> SetPinnedStatusAsync<T>(T folderId, bool pin)
    {
        var folderDao = GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(folderId);

        ErrorIf(room == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanPinAsync(room), FilesCommonResource.ErrorrMessage_PinRoom);

        var tagDao = GetTagDao<T>();
        var tag = Tag.Pin(authContext.CurrentAccount.ID, room);

        if (pin)
        {
            await tagDao.SaveTagsAsync(tag);
        }
        else
        {
            await tagDao.RemoveTagsAsync(tag);
        }

        room.Pinned = pin;

        return room;
    }

    public async Task<Folder<T>> SetRoomSettingsAsync<T>(T folderId, bool indexing)
    {
        var folderDao = GetFolderDao<T>();
        var room = await folderDao.GetFolderAsync(folderId);

        ErrorIf(room == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanEditAsync(room), FilesCommonResource.ErrorMassage_SecurityException);

        if (DocSpaceHelper.IsRoom(room.FolderType))
        {
            if (room.SettingsIndexing != indexing)
            {
                if (indexing)
                {
                    await ReOrder(room.Id, true);
                }
                
                room.SettingsIndexing = indexing;
                await folderDao.SaveFolderAsync(room);
            }
        }

        return room;
    }
    
    
    public async Task<Folder<T>> ReOrder<T>(T folderId, bool subfolders = false)
    {        
        var folderDao = GetFolderDao<T>();
        var fileDao = GetFileDao<T>();

        var room = await folderDao.GetFolderAsync(folderId);

        ErrorIf(room == null, FilesCommonResource.ErrorMassage_FolderNotFound);
        ErrorIf(!await fileSecurity.CanEditAsync(room), FilesCommonResource.ErrorMassage_SecurityException);
        
        var folders = await folderDao.GetFoldersAsync(folderId, new OrderBy(SortedByType.AZ, true), FilterType.None, false, Guid.Empty, null).Select(r => r.Id).ToListAsync();
        await folderDao.InitCustomOrder(folders, folderId);
        
        var files = await fileDao.GetFilesAsync(folderId, new OrderBy(SortedByType.AZ, true), FilterType.None, false, Guid.Empty, null, null, false).Select(r=> r.Id).ToListAsync();
        await fileDao.InitCustomOrder(files, folderId);

        if (subfolders)
        {
            foreach (var t in folders)
            {
                await ReOrder(t, true);
            }
        }

        return room;
    }
    
    public async Task<List<AceShortWrapper>> SendEditorNotifyAsync<T>(T fileId, MentionMessageWrapper mentionMessage)
    {
        ErrorIf(!authContext.IsAuthenticated, FilesCommonResource.ErrorMassage_SecurityException);

        var fileDao = GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);

        var canRead = await fileSecurity.CanReadAsync(file);
        ErrorIf(!canRead, FilesCommonResource.ErrorMassage_SecurityException_ReadFile);
        ErrorIf(mentionMessage == null || mentionMessage.Emails == null, FilesCommonResource.ErrorMassage_BadRequest);

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

        return showSharingSettings ? await fileSharing.GetSharedInfoShortFileAsync(fileId) : null;
    }

    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccessAsync<T>(T fileId)
    {
        ErrorIf(!await PrivacyRoomSettings.GetEnabledAsync(settingsManager), FilesCommonResource.ErrorMassage_SecurityException);

        var fileKeyPair = await encryptionKeyPairHelper.GetKeyPairAsync(fileId, this);

        return new List<EncryptionKeyPairDto>(fileKeyPair);
    }

    public async Task<bool> ChangeExternalShareSettingsAsync(bool enable)
    {
        ErrorIf(!await global.IsDocSpaceAdministratorAsync, FilesCommonResource.ErrorMassage_SecurityException);

        filesSettingsHelper.ExternalShare = enable;

        if (!enable)
        {
            filesSettingsHelper.ExternalShareSocialMedia = false;
        }

        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsExternalShareSettingsUpdated);

        return filesSettingsHelper.ExternalShare;
    }

    public async Task<bool> ChangeExternalShareSocialMediaSettingsAsync(bool enable)
    {
        ErrorIf(!await global.IsDocSpaceAdministratorAsync, FilesCommonResource.ErrorMassage_SecurityException);

        filesSettingsHelper.ExternalShareSocialMedia = filesSettingsHelper.ExternalShare && enable;

        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsExternalShareSettingsUpdated);

        return filesSettingsHelper.ExternalShareSocialMedia;
    }

    public List<string> GetMailAccounts()
    {
        return null;
        //var apiServer = new ASC.Api.ApiServer();
        //var apiUrl = string.Format("{0}mail/accounts.json", SetupInfo.WebApiBaseUrl);

        //var accounts = new List<string>();

        //var responseBody = apiServer.GetApiResponse(apiUrl, "GET");
        //if (responseBody != null)
        //{
        //    var responseApi = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(responseBody)));

        //    var responseData = responseApi["response"];
        //    if (responseData is JArray)
        //    {
        //        accounts.AddRange(
        //            from account in responseData.Children()
        //            orderby account["isDefault"].Value<bool>() descending
        //            where account["enabled"].Value<bool>() && !account["isGroup"].Value<bool>()
        //            select account["email"].Value<string>()
        //            );
        //    }
        //}
        //ErrorIf(!accounts.Any(), FilesCommonResource.ErrorMassage_MailAccountNotFound);

        //return new List<string>(accounts);
    }

    public async IAsyncEnumerable<FileEntry> ChangeOwnerAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId, Guid userId)
    {
        var userInfo = await userManager.GetUsersAsync(userId);
        ErrorIf(Equals(userInfo, Constants.LostUser) || await userManager.IsUserAsync(userInfo) || await userManager.IsCollaboratorAsync(userInfo), FilesCommonResource.ErrorMassage_ChangeOwner);

        var folderDao = GetFolderDao<T>();
        var folders = folderDao.GetFoldersAsync(foldersId);

        await foreach (var folder in folders)
        {
            ErrorIf(folder.RootFolderType is not FolderType.COMMON and not FolderType.VirtualRooms, FilesCommonResource.ErrorMassage_SecurityException);
            ErrorIf(!await fileSecurity.CanEditAsync(folder), FilesCommonResource.ErrorMassage_SecurityException);

            if (folder.ProviderEntry)
            {
                continue;
            }

            var newFolder = folder;
            if (folder.CreateBy != userInfo.Id)
            {
                var createBy = folder.CreateBy;

                await SetAceObjectAsync(new AceCollection<T>
                {
                    Files = Array.Empty<T>(),
                    Folders = new[] { folder.Id },
                    Aces = new List<AceWrapper>
                    {
                        new()
                        {
                              Access = FileShare.None,
                              Id = userInfo.Id
                        },
                        new()
                        {
                              Access = FileShare.RoomAdmin,
                              Id = createBy
                        }
                    }
                }, false);

                var folderAccess = folder.Access;

                newFolder.CreateBy = userInfo.Id;

                var newFolderID = await folderDao.SaveFolderAsync(newFolder);
                newFolder = await folderDao.GetFolderAsync(newFolderID);
                newFolder.Access = folderAccess;

                await socketManager.CreateFolderAsync(newFolder);
                await entryStatusManager.SetIsFavoriteFolderAsync(folder);

                await filesMessageService.SendAsync(MessageAction.FileChangeOwner, newFolder, new[] { newFolder.Title, userInfo.DisplayUserName(false, displayUserSettingsHelper) });
            }

            yield return newFolder;
        }

        var fileDao = GetFileDao<T>();
        var files = fileDao.GetFilesAsync(filesId);

        await foreach (var file in files)
        {
            ErrorIf(!await fileSecurity.CanEditAsync(file), FilesCommonResource.ErrorMassage_SecurityException);
            ErrorIf(await entryManager.FileLockedForMeAsync(file.Id), FilesCommonResource.ErrorMassage_LockedFile);
            ErrorIf(fileTracker.IsEditing(file.Id), FilesCommonResource.ErrorMassage_UpdateEditingFile);
            ErrorIf(file.RootFolderType != FolderType.COMMON, FilesCommonResource.ErrorMassage_SecurityException);
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
                newFile.FileStatus = file.FileStatus;
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

                await filesMessageService.SendAsync(MessageAction.FileChangeOwner, newFile, new[] { newFile.Title, userInfo.DisplayUserName(false, displayUserSettingsHelper) });
            }
            yield return newFile;
        }
    }

    public async Task<bool> StoreOriginalAsync(bool set)
    {
        filesSettingsHelper.StoreOriginalFiles = set;
        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsUploadingFormatsSettingsUpdated);

        return filesSettingsHelper.StoreOriginalFiles;
    }

    public async Task<bool> KeepNewFileNameAsync(bool set)
    {
        var current = filesSettingsHelper.KeepNewFileName;
        if (current != set)
        {
            filesSettingsHelper.KeepNewFileName = set;
            await messageService.SendHeadersMessageAsync(MessageAction.DocumentsKeepNewFileNameSettingsUpdated);
    }
        return set;
    }

    public bool HideConfirmConvert(bool isForSave)
    {
        if (isForSave)
        {
            filesSettingsHelper.HideConfirmConvertSave = true;
        }
        else
        {
            filesSettingsHelper.HideConfirmConvertOpen = true;
        }

        return true;
    }

    public async Task<bool> ForcesaveAsync(bool set)
    {
        filesSettingsHelper.Forcesave = set;
        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsForcesave);

        return filesSettingsHelper.Forcesave;
    }

    public async Task<bool> StoreForcesaveAsync(bool set)
    {
        ErrorIf(!await global.IsDocSpaceAdministratorAsync, FilesCommonResource.ErrorMassage_SecurityException);

        filesSettingsHelper.StoreForcesave = set;
        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsStoreForcesave);

        return filesSettingsHelper.StoreForcesave;
    }

    public bool DisplayRecent(bool set)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
        }

        filesSettingsHelper.RecentSection = set;

        return filesSettingsHelper.RecentSection;
    }

    public bool DisplayFavorite(bool set)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
        }

        filesSettingsHelper.FavoritesSection = set;

        return filesSettingsHelper.FavoritesSection;
    }

    public bool DisplayTemplates(bool set)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException);
        }

        filesSettingsHelper.TemplatesSection = set;

        return filesSettingsHelper.TemplatesSection;
    }

    public ICompress ChangeDownloadTarGz(bool set)
    {
        filesSettingsHelper.DownloadTarGz = set;

        return compressToArchive;
    }

    public bool ChangeDeleteConfrim(bool set)
    {
        filesSettingsHelper.ConfirmDelete = set;

        return filesSettingsHelper.ConfirmDelete;
    }

    public AutoCleanUpData ChangeAutomaticallyCleanUp(bool set, DateToAutoCleanUp gap)
    {
        filesSettingsHelper.AutomaticallyCleanUp = new AutoCleanUpData { IsAutoCleanUp = set, Gap = gap };

        return filesSettingsHelper.AutomaticallyCleanUp;
    }

    public AutoCleanUpData GetSettingsAutomaticallyCleanUp()
    {
        return filesSettingsHelper.AutomaticallyCleanUp;
    }

    public List<FileShare> ChangeDafaultAccessRights(List<FileShare> value)
    {
        filesSettingsHelper.DefaultSharingAccessRights = value;

        return filesSettingsHelper.DefaultSharingAccessRights;
    }

    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(List<JsonElement> fileIds)
    {
        if (!authContext.IsAuthenticated && (await externalShare.GetLinkIdAsync()) == Guid.Empty)
        {
            throw GenerateException(new SecurityException(FilesCommonResource.ErrorMassage_SecurityException));
        }

        try
        {
            var (fileIntIds, _) = FileOperationsManager.GetIds(fileIds);

            eventBus.Publish(new ThumbnailRequestedIntegrationEvent(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantIdAsync())
            {
                BaseUrl = baseCommonLinkUtility.GetFullAbsolutePath(""),
                FileIds = fileIntIds
            });

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

        ErrorIf(!await fileSecurity.CanEditRoomAsync(room), FilesCommonResource.ErrorMassage_SecurityException);

        if (!resendAll)
    {
            await foreach (var ace in fileSharing.GetPureSharesAsync(room, usersIds))
            {
                var user = await userManager.GetUsersAsync(ace.Id);

                var link = await invitationLinkService.GetInvitationLinkAsync(user.Email, ace.Access, authContext.CurrentAccount.ID, room.Id.ToString());
                await studioNotifyService.SendEmailRoomInviteAsync(user.Email, room.Title, link);
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

            await foreach (var ace in fileSharing.GetPureSharesAsync(room, ShareFilterType.User, EmployeeActivationStatus.Pending, offset, packSize + margin))
            {
                counter++;
                
                if (counter > packSize)
                {
                    offset += packSize;
                    break;
            }

                var user = await userManager.GetUsersAsync(ace.Id);

                var link = await invitationLinkService.GetInvitationLinkAsync(user.Email, ace.Access, authContext.CurrentAccount.ID, id.ToString());
                var shortenLink = await urlShortener.GetShortenLinkAsync(link);

                await studioNotifyService.SendEmailRoomInviteAsync(user.Email, room.Title, shortenLink);
            }

            if (counter <= packSize)
            {
                finish = true;
            }
        }
    }

    public async Task<List<MentionWrapper>> ProtectUsersAsync<T>(T fileId)
    {
        if (!authContext.IsAuthenticated || coreBaseSettings.Personal)
        {
            return null;
        }

        var fileDao = GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        ErrorIf(file == null, FilesCommonResource.ErrorMassage_FileNotFound);

        var users = new List<MentionWrapper>();
        if (file.RootFolderType == FolderType.BUNCH)
        {
            //todo: request project team
            return new List<MentionWrapper>(users);
    }

        var acesForObject = await fileSharing.GetSharedInfoAsync(file);

        var usersInfo = new List<UserInfo>();
        foreach (var ace in acesForObject)
    {
            if (ace.Access == FileShare.Restrict || ace.Id.Equals(FileConstant.ShareLinkId))
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

        users = usersInfo.Distinct()
                         .Where(user => !user.Id.Equals(authContext.CurrentAccount.ID)
                                        && !user.Id.Equals(Constants.LostUser.Id))
                         .Select(user => new MentionWrapper(user, displayUserSettingsHelper))
                         .ToList();

        users = users
            .OrderBy(user => user.User, UserInfoComparer.Default)
            .ToList();

        return new List<MentionWrapper>(users);
    }

    private IFolderDao<T> GetFolderDao<T>()
    {
        return daoFactory.GetFolderDao<T>();
    }

    private IFileDao<T> GetFileDao<T>()
    {
        return daoFactory.GetFileDao<T>();
    }

    private ITagDao<T> GetTagDao<T>()
    {
        return daoFactory.GetTagDao<T>();
    }

    private async Task<IDataStore> GetStoreTemplateAsync()
    {
        return await globalStore.GetStoreTemplateAsync();
    }

    private IProviderDao GetProviderDao()
    {
        return daoFactory.ProviderDao;
    }

    private ISecurityDao<T> GetSecurityDao<T>()
    {
        return daoFactory.GetSecurityDao<T>();
    }

    private static void ErrorIf(bool condition, string errorMessage)
    {
        if (condition)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private Exception GenerateException(Exception error, bool warning = false)
    {
        if (warning)
        {
            _logger.Information(error.ToString());
        }
        else
        {
            _logger.ErrorFileStorageService(error);
        }

        return new InvalidOperationException(error.Message, error);
    }

    private IDictionary<string, StringValues> GetHttpHeaders()
    {
        var request = httpContextAccessor?.HttpContext?.Request;

        return MessageSettings.GetHttpHeaders(request);
    }

    private async Task<AceWrapper> SetExternalLinkAsync<T>(FileEntry<T> entry, Guid linkId, FileShare share, string title, DateTime expirationDate = default,
        string password = null, bool denyDownload = false, bool primary = false)
    {
        var options = new FileShareOptions
        {
            Title = !string.IsNullOrEmpty(title) ? title : FilesCommonResource.DefaultExternalLinkTitle,
            DenyDownload = denyDownload
        };
        
        var expirationDateUtc = tenantUtil.DateTimeToUtc(expirationDate);

        if (expirationDateUtc != DateTime.MinValue && expirationDateUtc > DateTime.UtcNow)
        {
            options.ExpirationDate = expirationDateUtc;
        }

        if (!string.IsNullOrEmpty(password))
        {
            options.Password = await externalShare.CreatePasswordKeyAsync(password);
    }

        var result = await SetAceLinkAsync(entry, primary ? SubjectType.PrimaryExternalLink : SubjectType.ExternalLink, linkId, share, options, _actions[SubjectType.ExternalLink]);

        if (result == null)
    {
            return (await fileSharing.GetPureSharesAsync(entry, new[] { linkId }).FirstOrDefaultAsync());
        }

        var (eventType, ace) = result;
        linkId = ace.Id;

        if (eventType == EventType.Remove && ace.SubjectType == SubjectType.PrimaryExternalLink && entry is Folder<T> { FolderType: FolderType.PublicRoom })
        {
            linkId = Guid.NewGuid();
            
            await SetAceLinkAsync(entry, SubjectType.PrimaryExternalLink, linkId, FileShare.Read, new FileShareOptions { Title = FilesCommonResource.DefaultExternalLinkTitle }, 
                _actions[SubjectType.ExternalLink]);
        }

        return (await fileSharing.GetPureSharesAsync(entry, new[] { linkId }).FirstOrDefaultAsync());
    }

    private async Task<Tuple<EventType, AceWrapper>> SetAceLinkAsync<T>(FileEntry<T> entry, SubjectType subjectType, Guid linkId, FileShare share, FileShareOptions options,
        IReadOnlyDictionary<EventType, MessageAction> messageActions)
    {
        if (linkId == Guid.Empty)
        {
            linkId = Guid.NewGuid();
        }

        var aces = new List<AceWrapper>
        {
            new()
            {
                Access = share,
                Id = linkId,
                SubjectType = subjectType,
                FileShareOptions = options
            }
        };

        try
        {
            var result = await fileSharingAceHelper.SetAceObjectAsync(aces, entry, false, null, null);

            if (!string.IsNullOrEmpty(result.Warning))
            {
                throw GenerateException(new InvalidOperationException(result.Warning));
            }

            if (result.Changed)
            {
                var (eventType, ace) = result.HandledAces[0];
                var isRoom = entry is Folder<T> folder && DocSpaceHelper.IsRoom(folder.FolderType);
                
                await filesMessageService.SendAsync(messageActions[eventType], entry, ace.Id, ace.FileShareOptions?.Title, 
                    FileShareExtensions.GetAccessString(ace.Access, isRoom));
            }

            return result.HandledAces.FirstOrDefault();
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

        foreach (var userId in userInfos.Select(r=> r.Id))
        {
            dict[userId] = new AceWrapper
            {
                Access = FileShare.ReadWrite,
                Id = userId
            };
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

    private async Task SetAcesForPrivateRoomAsync<T>(Folder<T> room, List<AceWrapper> aces, bool notify, string sharingMessage)
    {
        var advancedSettings = new AceAdvancedSettingsWrapper
        {
            AllowSharingPrivateRoom = true
        };

        var aceCollection = new AceCollection<T>
        {
            Folders = new[] { room.Id },
            Files = Array.Empty<T>(),
            Aces = aces,
            Message = sharingMessage,
            AdvancedSettings = advancedSettings
        };

        await SetAceObjectAsync(aceCollection, notify);
    }

    private static readonly IReadOnlyDictionary<SubjectType, IReadOnlyDictionary<EventType, MessageAction>> _actions =
        new Dictionary<SubjectType, IReadOnlyDictionary<EventType, MessageAction>>
    {
            {
                SubjectType.InvitationLink, new Dictionary<EventType, MessageAction>
                {
                    { EventType.Create, MessageAction.RoomInvitationLinkCreated },
                    { EventType.Update, MessageAction.RoomInvitationLinkUpdated },
                    { EventType.Remove, MessageAction.RoomInvitationLinkDeleted }
    }
            },
            {
                SubjectType.ExternalLink, new Dictionary<EventType, MessageAction>
                {
                    { EventType.Create, MessageAction.RoomExternalLinkCreated },
                    { EventType.Update, MessageAction.RoomExternalLinkUpdated },
                    { EventType.Remove, MessageAction.RoomExternalLinkDeleted }
}
            }
        };
}

public class FileModel<T, TTempate>
{
    public T ParentId { get; init; }
    public string Title { get; set; }
    public TTempate TemplateId { get; init; }
    public int FormId { get; init; }
}
