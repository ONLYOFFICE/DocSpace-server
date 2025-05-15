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
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext,
    UserManager userManager,
    FilesLinkUtility filesLinkUtility,
    BaseCommonLinkUtility baseCommonLinkUtility,
    ILoggerProvider optionMonitor,
    PathProvider pathProvider,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    FileSharing fileSharing,
    EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
    SettingsManager settingsManager,
    TenantManager tenantManager,
    IEventBus eventBus,
    EntryStatusManager entryStatusManager,
    ExternalShare externalShare,
    CoreBaseSettings coreBaseSettings,
    MentionWrapperCreator mentionWrapperCreator,
    SecurityContext securityContext,
    FileChecker fileChecker,
    CommonLinkUtility commonLinkUtility,
    ShortUrl shortUrl,
    IDbContextFactory<UrlShortenerDbContext> dbContextFactory,
    WebhookManager webhookManager,
    FolderService folderService,
    SharingService sharingService)
{
    private readonly ILogger _logger = optionMonitor.CreateLogger("ASC.Files");
    
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
            throw GenerateException(e, _logger, authContext);
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

   

    #region [Reassign|Delete] Data Manager
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
            var folder = await folderService.CreateFolderAsync(await globalFolderHelper.FolderMyAsync, $"Documents of user {userInfo.FirstName} {userInfo.LastName}");
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

    #endregion

    #region Templates Manager

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
    
    #endregion
    

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

    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccessAsync<T>(T fileId)
    {
        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var fileKeyPair = await encryptionKeyPairHelper.GetKeyPairAsync(fileId, sharingService);

        return [..fileKeyPair];
    }

    

    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(List<JsonElement> fileIds)
    {
        if (!authContext.IsAuthenticated && (await externalShare.GetLinkIdAsync()) == Guid.Empty)
        {
            throw GenerateException(new SecurityException(FilesCommonResource.ErrorMessage_SecurityException),  _logger, authContext);
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
    
    internal static Exception GenerateException(Exception error, ILogger logger, AuthContext authContext, bool warning = false)
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

public class FileModel<T, TTempate>
{
    public T ParentId { get; init; }
    public string Title { get; set; }
    public TTempate TemplateId { get; init; }
    public int FormId { get; init; }
}