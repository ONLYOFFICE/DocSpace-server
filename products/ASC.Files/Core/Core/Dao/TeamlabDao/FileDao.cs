// (c) Copyright Ascensio System SIA 2009-2024
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

using Document = ASC.ElasticSearch.Document;

namespace ASC.Files.Core.Data;

[Scope(typeof(IFileDao<int>))]
internal class FileDao(
        ILogger<FileDao> logger,
        FactoryIndexerFile factoryIndexer,
        FactoryIndexerForm factoryIndexerFormData,
        UserManager userManager,
        FileUtility fileUtility,
        IDbContextFactory<FilesDbContext> dbContextManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        GlobalStore globalStore,
        GlobalFolder globalFolder,
        Global global,
        IDaoFactory daoFactory,
        ChunkedUploadSessionHolder chunkedUploadSessionHolder,
        SelectorFactory selectorFactory,
        CrossDao crossDao,
        Settings settings,
        IMapper mapper,
        ThumbnailSettings thumbnailSettings,
        IQuotaService quotaService,
        EmailValidationKeyProvider emailValidationKeyProvider,
        StorageFactory storageFactory,
    TenantQuotaController tenantQuotaController,
    IDistributedLockProvider distributedLockProvider,
    FileStorageService fileStorageService,
    SocketManager socketManager,
    SecurityContext securityContext,
    TempStream tempStream,
    FileChecker fileChecker,
    EntryManager entryManager,
    FileSharing fileSharing,
    FilesMessageService filesMessageService)
    : AbstractDao(dbContextManager,
              userManager,
              tenantManager,
              tenantUtil,
              setupInfo,
              maxTotalSizeStatistic,
              settingsManager,
              authContext,
            serviceProvider,
            distributedLockProvider), IFileDao<int>
{

    private const string LockKey = "file";
    private const string FilePathPart = "file_";
    private const string FolderPathPart = "folder_";
    private const string FileIdGroupName = "id";

    private static readonly Regex _pattern = new($"{FilePathPart}(?'id'\\d+)", RegexOptions.Singleline | RegexOptions.Compiled);

    public Task InvalidateCacheAsync(int fileId)
    {
        return Task.CompletedTask;
    }

    public async Task<File<int>> GetFileAsync(int fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFile = await filesDbContext.DbFileQueryAsync(tenantId, fileId);

        return mapper.Map<DbFileQuery, File<int>>(dbFile);
    }

    public async Task<File<int>> GetFileAsync(int fileId, int fileVersion)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFile = await filesDbContext.DbFileQueryByFileVersionAsync(tenantId, fileId, fileVersion);

        return mapper.Map<DbFileQuery, File<int>>(dbFile);
    }

    public async Task<File<int>> GetFileAsync(int parentId, string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFile = await filesDbContext.DbFileQueryByTitleAsync(tenantId, title, parentId);

        return mapper.Map<DbFileQuery, File<int>>(dbFile);
    }

    public async Task<File<int>> GetFileStableAsync(int fileId, int fileVersion = -1)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFile = await filesDbContext.DbFileQueryFileStableAsync(tenantId, fileId, fileVersion);

        return mapper.Map<DbFileQuery, File<int>>(dbFile);
    }

    public async IAsyncEnumerable<File<int>> GetFileHistoryAsync(int fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var e in filesDbContext.DbFileQueriesAsync(tenantId, fileId))
        {
            yield return mapper.Map<DbFileQuery, File<int>>(e);
        }
    }

    public async IAsyncEnumerable<File<int>> GetFilesAsync(IEnumerable<int> fileIds)
    {
        if (fileIds == null || !fileIds.Any())
        {
            yield break;
        }

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var e in filesDbContext.DbFileQueriesByFileIdsAsync(tenantId, fileIds))
        {
            yield return mapper.Map<DbFileQuery, File<int>>(e);
        }
    }

    public async IAsyncEnumerable<File<int>> GetFilesFilteredAsync(IEnumerable<int> fileIds, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, string[] extension, 
        bool searchInContent, bool checkShared = false)
    {
        if (fileIds == null || !fileIds.Any() || filterType == FilterType.FoldersOnly)
        {
            yield break;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var query = GetFileQuery(filesDbContext, r => fileIds.Contains(r.Id) && r.CurrentVersion);

        var searchByText = !string.IsNullOrEmpty(searchText);
        var searchByExtension = !extension.IsNullOrEmpty();
        
        if (extension.IsNullOrEmpty())
        {
            extension = [""];
        }

        if (searchByText || searchByExtension)
        {
            var searchIds = new List<int>();
            var success = false;
            foreach (var e in extension)
            {
                var func = GetFuncForSearch(null, null, filterType, subjectGroup, subjectID, searchText, e, searchInContent);

                (success, var result) = await factoryIndexer.TrySelectIdsAsync(s => func(s).In(r => r.Id, fileIds.ToArray()));
                
                if(!success)
                {
                    break;
                }
                searchIds = searchIds.Concat(result).ToList();
            }

            if (success)
            {
                query = query.Where(r => searchIds.Contains(r.Id));
            }
            else
            {
                if (searchByText)
                {
                    query = BuildSearch(query, searchText, SearchType.Any);
                }

                if (searchByExtension)
                {
                    query = BuildSearch(query, extension, SearchType.End);
                }
            }
        }

        if (subjectID != Guid.Empty)
        {
            if (subjectGroup)
            {
                var users = (await _userManager.GetUsersByGroupAsync(subjectID)).Select(u => u.Id).ToArray();
                query = query.Where(r => users.Contains(r.CreateBy));
            }
            else
            {
                query = query.Where(r => r.CreateBy == subjectID);
            }
        }

        switch (filterType)
        {
            case FilterType.DocumentsOnly:
            case FilterType.ImagesOnly:
            case FilterType.PresentationsOnly:
            case FilterType.SpreadsheetsOnly:
            case FilterType.ArchiveOnly:
            case FilterType.MediaOnly:
                query = query.Where(r => r.Category == (int)filterType);
                break;
            case FilterType.ByExtension:
                if (!string.IsNullOrEmpty(searchText))
                {
                    query = BuildSearch(query, searchText, SearchType.End);
                }
                break;
        }

        await foreach (var e in FromQuery(filesDbContext, query).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFileQuery, File<int>>(e);
        }
    }

    public async IAsyncEnumerable<int> GetFilesAsync(int parentId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var e in filesDbContext.FileIdsAsync(tenantId, parentId))
        {
            yield return e;
        }
    }

    public async IAsyncEnumerable<File<int>> GetFilesAsync(int parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, string[] extension, 
        bool searchInContent, bool withSubfolders = false, bool excludeSubject = false, int offset = 0, int count = -1, int roomId = 0, bool withShared = false, bool containingMyFiles = false, FolderType parentType = FolderType.DEFAULT, FormsItemDto formsItemDto = null)
    {
        if (filterType == FilterType.FoldersOnly || count == 0)
        {
            yield break;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = await GetFilesQueryWithFilters(parentId, orderBy, filterType, subjectGroup, subjectID, searchText, searchInContent, withSubfolders, excludeSubject, roomId, extension, filesDbContext, formsItemDto);

        if (containingMyFiles)
        {
            switch (parentType)
            {
                case FolderType.FillingFormsRoom:
                case FolderType.InProcessFormFolder:
                case FolderType.ReadyFormFolder:
                    var tenantId = _tenantManager.GetCurrentTenantId();

                    var folderIds = filesDbContext.Folders
                        .Join(filesDbContext.Tree, r => r.Id, b => b.FolderId, (folder, tree) => new { folder, tree })
                        .Where(r => r.folder.TenantId == tenantId)
                        .Where(r => r.tree.ParentId == parentId)
                        .Select(r => new
                        {
                            r.folder.Id,
                            IsSystemFolder = DocSpaceHelper.FormsFillingSystemFolders.Contains(r.folder.FolderType)
                        });

                    var roomSystemFolderIds = folderIds
                        .Where(r => r.IsSystemFolder)
                        .Select(r => r.Id);

                    var roomDefaultFolderIds = folderIds
                        .Where(r => !r.IsSystemFolder)
                        .Select(r => r.Id);

                    q = q.Where(r =>
                        (roomSystemFolderIds.Contains(r.ParentId) &&
                         r.CreateBy == securityContext.CurrentAccount.ID &&
                         r.CreateBy != ASC.Core.Configuration.Constants.Guest.ID) ||
                        roomDefaultFolderIds.Contains(r.ParentId));
                    break;
                default:
                    q = q.Where(r => r.CreateBy == securityContext.CurrentAccount.ID);
                    break;
            }
        }

        q = q.Skip(offset);

        if (count > 0)
        {
            q = q.Take(count);
        }

        var result = withShared ? FromQueryWithShared(filesDbContext, q) : FromQuery(filesDbContext, q);

        await foreach (var e in result.AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFileQuery, File<int>>(e);
        }
    }

    public async Task<Stream> GetFileStreamAsync(File<int> file, long offset)
    {
        return await (await globalStore.GetStoreAsync()).GetReadStreamAsync(string.Empty, GetUniqFilePath(file), offset);
    }

    public async Task<Stream> GetFileStreamAsync(File<int> file, long offset, long length)
    {
        return await (await globalStore.GetStoreAsync()).GetReadStreamAsync(string.Empty, GetUniqFilePath(file), offset, length);
    }

    public async Task<long> GetFileSizeAsync(File<int> file)
    {
        return await (await globalStore.GetStoreAsync()).GetFileSizeAsync(string.Empty, GetUniqFilePath(file));
    }
    
    public async Task<string> GetPreSignedUriAsync(File<int> file, TimeSpan expires, string shareKey = null)
    {
        var storage = await globalStore.GetStoreAsync();

        if (storage.IsSupportCdnUri && !fileUtility.CanWebEdit(file.Title)
            && (fileUtility.CanMediaView(file.Title) || fileUtility.CanImageView(file.Title)))
        {
            return (await storage.GetCdnPreSignedUriAsync(string.Empty, GetUniqFilePath(file), expires,
                new List<string>
                {
                    $"Content-Disposition:{ContentDispositionUtil.GetHeaderValue(file.Title, withoutBase: true)}",
                    $"Custom-Cache-Key:{file.ModifiedOn.Ticks}"
                })).ToString();
        }

        var path = GetUniqFilePath(file);
        var headers = new List<string>
        {
            string.Concat("Content-Disposition:", ContentDispositionUtil.GetHeaderValue(file.Title, withoutBase: true))
        };

        if (!_authContext.IsAuthenticated)
        {
            headers.Add(SecureHelper.GenerateSecureKeyHeader(path, emailValidationKeyProvider));
        }

        var url = (await storage.GetPreSignedUriAsync(string.Empty, path, expires, headers)).ToString();

        if (!string.IsNullOrEmpty(shareKey))
        {
            url = QueryHelpers.AddQueryString(url, FilesLinkUtility.ShareKey, shareKey);
        }

        return url;
    }

    public async Task<bool> IsSupportedPreSignedUriAsync(File<int> file)
    {
        return (await globalStore.GetStoreAsync()).IsSupportedPreSignedUri;
    }

    public async Task<Stream> GetFileStreamAsync(File<int> file)
    {
        return await GetFileStreamAsync(file, 0);
    }
    
    private async Task<Stream> GetFileStreamForTenantAsync(File<int> file, int? tenantId)
    {
        if (!tenantId.HasValue)
        {
            return await GetFileStreamAsync(file);
        }
        
        return await (await globalStore.GetStoreAsync(tenantId.Value)).GetReadStreamAsync(string.Empty, GetUniqFilePath(file), 0);
    }
    public async Task<File<int>> SaveFileAsync(File<int> file, Stream fileStream)
    {
        return await SaveFileAsync(file, fileStream, true, true);
    }
    public async Task<File<int>> SaveFileAsync(File<int> file, Stream fileStream, bool checkFolder)
    {
        return await SaveFileAsync(file, fileStream, true, checkFolder);
    }

    public async Task<File<int>> SaveFileAsync(File<int> file, Stream fileStream, bool checkQuota, bool checkFolder, ChunkedUploadSession<int> uploadSession = null)
    {
        ArgumentNullException.ThrowIfNull(file);

        var maxChunkedUploadSize = await _setupInfo.MaxChunkedUploadSize(_tenantManager, _maxTotalSizeStatistic);
        if (checkQuota && maxChunkedUploadSize < file.ContentLength)
        {
            throw FileSizeComment.GetFileSizeException(maxChunkedUploadSize);
        }
        var tenantId = _tenantManager.GetCurrentTenantId();
        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var currentFolder = await folderDao.GetFolderAsync(file.FolderIdDisplay);

        var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(currentFolder);
        UserInfo user = null;
        string quotaLockKey;

        if (roomId != -1)
        {
            quotaLockKey = $"room_{roomId}";
        }
        else
        {
            user = await _userManager.GetUsersAsync(file.Id == 0 ? _authContext.CurrentAccount.ID : file.CreateBy);
            quotaLockKey = $"user_{user.Id}";
        }

        await using (await _distributedLockProvider.TryAcquireFairLockAsync(quotaLockKey))
        {
            if (roomId != -1)
            {
                var currentRoom = await folderDao.GetFolderAsync(roomId);
                var quotaRoomSettings = await _settingsManager.LoadAsync<TenantRoomQuotaSettings>();
                if (quotaRoomSettings.EnableQuota)
                {
                    var roomQuotaLimit = currentRoom.SettingsQuota == TenantEntityQuotaSettings.DefaultQuotaValue ? quotaRoomSettings.DefaultQuota : currentRoom.SettingsQuota;
                    if (roomQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                    {
                        if (roomQuotaLimit - currentRoom.Counter < file.ContentLength)
                        {
                            throw FileSizeComment.GetRoomFreeSpaceException(roomQuotaLimit);
                        }
                    }
                }
            }
            else if (user != null)
            {
                var quotaUserSettings = await _settingsManager.LoadAsync<TenantUserQuotaSettings>();
                if (quotaUserSettings.EnableQuota)
                {
                    var userQuotaData = await _settingsManager.LoadAsync<UserQuotaSettings>(user);

                    var userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota ;

                    if (userQuotaLimit != UserQuotaSettings.NoQuota)
                    {
                        var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenantId, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));

                        if (userQuotaLimit - userUsedSpace < file.ContentLength)
                        {
                            throw FileSizeComment.GetUserFreeSpaceException(userQuotaLimit);
                        }
                    }
                }
            }
       
        var isNew = false;
        DbFile toInsert = null;
            var cloneStreamForSave = new MemoryStream();
            var streamChange = false;
            await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                var parentFolders = await filesDbContext.DbFolderTreesAsync(file.ParentId).ToListAsync();

                var parentFoldersIds = parentFolders.Select(r => r.ParentId).ToList();

                await using (await _distributedLockProvider.TryAcquireFairLockAsync(LockKey))
                {
                    var strategy = filesDbContext.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var tx = await filesDbContext.Database.BeginTransactionAsync();

                    if (file.Id == 0)
                        {
                            file.Id = await filesDbContext.FileMaxIdAsync() + 1;
                            file.Version = 1;
                            file.VersionGroup = 1;
                            isNew = true;
                        }

                        file.Title = Global.ReplaceInvalidCharsAndTruncate(file.Title);
                        //make lowerCase
                        file.Title = FileUtility.ReplaceFileExtension(file.Title, FileUtility.GetFileExtension(file.Title));

                        file.ModifiedBy = _authContext.CurrentAccount.ID;
                        file.ModifiedOn = _tenantUtil.DateTimeNow();

                    if (file.CreateBy == Guid.Empty)
                        {
                            file.CreateBy = _authContext.CurrentAccount.ID;
                        }

                        if (file.CreateOn == default)
                        {
                            file.CreateOn = _tenantUtil.DateTimeNow();
                        }

                        if (!isNew)
                        {
                            await filesDbContext.DisableCurrentVersionAsync(tenantId, file.Id);
                        }

                        var fileType = FileUtility.GetFileTypeByFileName(file.Title);

                        if (fileType == FileType.Pdf && file.Category == (int)FilterType.None)
                        {
                            var originalCopyStream = new MemoryStream();
                            await fileStream.CopyToAsync(originalCopyStream);

                            var cloneStreamForCheck = await tempStream.CloneMemoryStream(originalCopyStream, 300);
                            cloneStreamForSave = await tempStream.CloneMemoryStream(originalCopyStream);
                            streamChange = true;
                            try
                            {
                                if (await fileChecker.CheckExtendedPDFstream(cloneStreamForCheck))
                                {
                                    file.Category = (int)FilterType.PdfForm;
                                }
                            }
                            finally
                            {
                                await originalCopyStream.DisposeAsync();
                                await cloneStreamForCheck.DisposeAsync();
                            }
                        }

                        toInsert = new DbFile
                        {
                            Id = file.Id,
                            Version = file.Version,
                            VersionGroup = file.VersionGroup,
                            CurrentVersion = true,
                            ParentId = file.ParentId,
                            Title = file.Title,
                            ContentLength = file.ContentLength,
                            Category = file.Category != (int)FilterType.None ? file.Category : (int)file.FilterType,
                            CreateBy = file.CreateBy,
                            CreateOn = _tenantUtil.DateTimeToUtc(file.CreateOn),
                            ModifiedBy = file.ModifiedBy,
                            ModifiedOn = _tenantUtil.DateTimeToUtc(file.ModifiedOn),
                            ConvertedType = file.ConvertedType,
                            Comment = file.Comment,
                            Encrypted = file.Encrypted,
                            Forcesave = file.Forcesave,
                            ThumbnailStatus = file.ThumbnailStatus,
                            TenantId = tenantId
                        };

                        if (isNew)
                        {
                            await filesDbContext.Files.AddAsync(toInsert);
                        }
                        else
                        {
                            await filesDbContext.AddOrUpdateAsync(r => r.Files, toInsert);
                        }
                        await filesDbContext.SaveChangesAsync();
                        await tx.CommitAsync();
                    });

                    file.PureTitle = file.Title;
                    file.RootCreateBy = currentFolder.RootCreateBy;
                    file.RootFolderType = currentFolder.RootFolderType;

                    if (parentFoldersIds.Count > 0)
                    {
                        await filesDbContext.UpdateFoldersAsync(parentFoldersIds, _tenantUtil.DateTimeToUtc(file.ModifiedOn), file.ModifiedBy, tenantId);
                    }

                    toInsert.Folders = parentFolders;

                    if (isNew)
                    {
                        file.Order = await SetCustomOrder(filesDbContext, file.Id, file.ParentId);
                    }
                }

                if (isNew)
                {
                    await IncrementCountAsync(filesDbContext, file.ParentId, tenantId, FileEntryType.File);

                    if (roomId != -1 && checkFolder)
                    {
                        var currentRoom = await folderDao.GetFolderAsync(roomId);
                        if (currentRoom.FolderType == FolderType.FillingFormsRoom)
                        {
                            var fileProp = await fileDao.GetProperties(file.Id);
                            var extension = FileUtility.GetFileExtension(file.Title);

                            if (file.IsForm || (extension == ".csv" && fileProp != null && Equals(fileProp.FormFilling.ResultsFolderId, file.ParentId)))
                            {
                                var properties = fileProp ?? new EntryProperties<int> { FormFilling = new FormFillingProperties<int>() };
                                if (!properties.FormFilling.CollectFillForm)
                                {
                                    var inProcessFormFolder = await folderDao.GetFoldersAsync(currentRoom.Id, FolderType.InProcessFormFolder).FirstOrDefaultAsync();
                                    var readyFormFolder = await folderDao.GetFoldersAsync(currentRoom.Id, FolderType.ReadyFormFolder).FirstOrDefaultAsync();

                                    if (inProcessFormFolder == null && readyFormFolder == null)
                                    {
                                        var (readyFormFolderId, inProcessFormFolderId) = await entryManager.InitSystemFormFillingFolders(currentRoom.Id, folderDao, file.CreateBy);
                                        var systemFormFillingFolders = new List<Folder<int>>
                                        {
                                            await folderDao.GetFolderAsync(readyFormFolderId),
                                            await folderDao.GetFolderAsync(inProcessFormFolderId)
                                        };
                                        foreach (var formFolder in systemFormFillingFolders)
                                        {
                                            var a = await fileSharing.GetSharedInfoAsync(formFolder);
                                            var u = a.Where(ace => ace is not { Access: FileShare.FillForms }).Select(ace => ace.Id).ToList();

                                            await socketManager.CreateFolderAsync(formFolder, u);
                                            await filesMessageService.SendAsync(MessageAction.FolderCreated, formFolder, formFolder.Title);
                                        }
                                    }
                                    properties.FormFilling.StartFilling = true;
                                    properties.FormFilling.CollectFillForm = true;
                                    properties.FormFilling.OriginalFormId = file.Id;
                                    await fileDao.SaveProperties(file.Id, properties);
                                    var count = await fileStorageService.GetPureSharesCountAsync(currentRoom.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "");
                                    if (file.IsForm)
                                    {
                                        await socketManager.CreateFormAsync(file, securityContext.CurrentAccount.ID, count <= 1);
                                    }
                                }
                            }
                            else
                            {
                                var stored = await (await globalStore.GetStoreAsync()).IsDirectoryAsync(GetUniqFileDirectory(file.Id));
                                await DeleteFileAsync(file.Id, stored, file.GetFileQuotaOwner());

                                throw new Exception(FilesCommonResource.ErrorMessage_UploadToFormRoom);
                            }
                        }
                    }
                }
            }

            if (fileStream != null)
            {
                try
                {
                    await SaveFileStreamAsync(file, streamChange ? cloneStreamForSave : fileStream, currentFolder);
                }
                catch (Exception saveException)
                {
                    try
                    {
                        if (isNew)
                        {
                            var stored = await (await globalStore.GetStoreAsync()).IsDirectoryAsync(GetUniqFileDirectory(file.Id));
                            await DeleteFileAsync(file.Id, stored, file.GetFileQuotaOwner());
                        }
                        else if (!await IsExistOnStorageAsync(file))
                        {
                            await DeleteFileVersionAsync(file);
                        }
                    }
                    catch (Exception deleteException)
                    {
                        throw new Exception(saveException.Message, deleteException);
                    }
                    throw;
                }
                finally
                {
                    await cloneStreamForSave.DisposeAsync();
                }
            }
            else
            {
                if (uploadSession != null)
                {
                    await chunkedUploadSessionHolder.MoveAsync(uploadSession, GetUniqFilePath(file), file.GetFileQuotaOwner());

                    await folderDao.ChangeTreeFolderSizeAsync(currentFolder.Id, file.ContentLength);
                }
            }

            _ = factoryIndexer.IndexAsync(await InitDocumentAsync(toInsert));

            return await GetFileAsync(file.Id);
        }
    }

    public async Task<int> GetFilesCountAsync(int parentId, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText, string[] extension, bool searchInContent, 
        bool withSubfolders = false, bool excludeSubject = false, int roomId = 0, FormsItemDto formsItemDto = null)
    {
        if (filterType == FilterType.FoldersOnly)
        {
            return 0;
        }

        var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await (await GetFilesQueryWithFilters(parentId, null, filterType, subjectGroup, subjectId, searchText, searchInContent, withSubfolders, excludeSubject, roomId, extension, filesDbContext, formsItemDto))
            .CountAsync();
    }

    public async Task<File<int>> ReplaceFileVersionAsync(File<int> file, Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Id == 0)
        {
            throw new ArgumentException("No file id or folder id toFolderId determine provider");
        }

        var maxChunkedUploadSize = await _setupInfo.MaxChunkedUploadSize(_tenantManager, _maxTotalSizeStatistic);

        if (maxChunkedUploadSize < file.ContentLength)
        {
            throw FileSizeComment.GetFileSizeException(maxChunkedUploadSize);
        }

        var tenantId = _tenantManager.GetCurrentTenantId();
        DbFile toUpdate = null;

        await using (await _distributedLockProvider.TryAcquireFairLockAsync(LockKey))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await context.Database.BeginTransactionAsync();

                file.Title = Global.ReplaceInvalidCharsAndTruncate(file.Title);
                //make lowerCase
                file.Title = FileUtility.ReplaceFileExtension(file.Title, FileUtility.GetFileExtension(file.Title));

                file.ModifiedBy = _authContext.CurrentAccount.ID;
                file.ModifiedOn = _tenantUtil.DateTimeNow();
                if (file.CreateBy == Guid.Empty)
                {
                    file.CreateBy = _authContext.CurrentAccount.ID;
                }

                if (file.CreateOn == default)
                {
                    file.CreateOn = _tenantUtil.DateTimeNow();
                }

                toUpdate = await context.DbFileByVersionAsync(tenantId, file.Id, file.Version);

                toUpdate.Version = file.Version;
                toUpdate.VersionGroup = file.VersionGroup;
                toUpdate.ParentId = file.ParentId;
                toUpdate.Title = file.Title;
                toUpdate.ContentLength = file.ContentLength;
                toUpdate.Category = (int)file.FilterType;
                toUpdate.CreateBy = file.CreateBy;
                toUpdate.CreateOn = _tenantUtil.DateTimeToUtc(file.CreateOn);
                toUpdate.ModifiedBy = file.ModifiedBy;
                toUpdate.ModifiedOn = _tenantUtil.DateTimeToUtc(file.ModifiedOn);
                toUpdate.ConvertedType = file.ConvertedType;
                toUpdate.Comment = file.Comment;
                toUpdate.Encrypted = file.Encrypted;
                toUpdate.Forcesave = file.Forcesave;
                toUpdate.ThumbnailStatus = file.ThumbnailStatus;

                context.Update(toUpdate);
                await context.SaveChangesAsync();

                await tx.CommitAsync();
            });

            file.PureTitle = file.Title;

            var parentFolders = await filesDbContext.DbFolderTeesAsync(file.ParentId).ToListAsync();

            var parentFoldersIds = parentFolders.Select(r => r.ParentId).ToList();

            if (parentFoldersIds.Count > 0)
            {
                await filesDbContext.UpdateFoldersAsync(parentFoldersIds, _tenantUtil.DateTimeToUtc(file.ModifiedOn), file.ModifiedBy, tenantId);
            }

            toUpdate.Folders = parentFolders;
        }

        if (fileStream != null)
        {
            try
            {
                await DeleteVersionStreamAsync(file);
                await SaveFileStreamAsync(file, fileStream);
            }
            catch
            {
                if (!await IsExistOnStorageAsync(file))
                {
                    await DeleteFileVersionAsync(file);
                }

                throw;
            }
        }

        _ = factoryIndexer.IndexAsync(await InitDocumentAsync(toUpdate));

        return await GetFileAsync(file.Id);
    }
    
    public async Task DeleteFileVersionAsync(File<int> file, int version = 0)
    {
        if (file == null
            || file.Id == 0
            || file.Version <= 1)
        {
            return;
        }

        if (version == 0)
        {
            version = file.Version;
        }
        
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tr = await context.Database.BeginTransactionAsync();

            await context.DeleteDbFilesByVersionAsync(tenantId, file.Id, version);
            if (version == file.Version)
            {
                await context.UpdateDbFilesByVersionAsync(tenantId, file.Id, version - 1);
            }

            await tr.CommitAsync();
        });
    }

    private async Task DeleteVersionStreamAsync(File<int> file)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        tenantQuotaController.Init(tenantId, ThumbnailTitle);
        var store = await storageFactory.GetStorageAsync(tenantId, FileConstant.StorageModule, tenantQuotaController);
        await store.DeleteDirectoryAsync(GetUniqFileVersionPath(file.Id, file.Version));
    }

    private async Task SaveFileStreamAsync(File<int> file, Stream stream, Folder<int> currentFolder = null)
    {
        var folderDao = daoFactory.GetFolderDao<int>();

        await (await globalStore.GetStoreAsync()).SaveAsync(string.Empty, GetUniqFilePath(file), file.GetFileQuotaOwner(), stream, file.Title);

        currentFolder ??= await folderDao.GetFolderAsync(file.FolderIdDisplay);

        await folderDao.ChangeTreeFolderSizeAsync(currentFolder.Id, file.ContentLength);
    }

    public async Task DeleteFileAsync(int fileId)
    {
        await DeleteFileAsync(fileId, Guid.Empty);
    }

    public async Task DeleteFileAsync(int fileId, Guid ownerId)
    {
        await DeleteFileAsync(fileId, true, ownerId);
    }

    private async ValueTask DeleteFileAsync(int fileId, bool deleteFolder, Guid ownerId)
    {
        if (fileId == 0)
        {
            return;
        }

        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            var fromFolders = await context.ParentIdsAsync(tenantId, fileId).ToListAsync();

            await context.DeleteTagLinksAsync(tenantId, fileId.ToString());

            var toDeleteFiles = await context.DbFilesAsync(tenantId, fileId).ToListAsync();
            var toDeleteFile = toDeleteFiles.FirstOrDefault(r => r.CurrentVersion);

            foreach (var d in toDeleteFiles)
            {
                await factoryIndexer.DeleteAsync(d);
            }

            context.RemoveRange(toDeleteFiles);

            await context.DeleteTagsAsync(tenantId);

            await context.DeleteSecurityAsync(tenantId, fileId.ToString());

            await DeleteCustomOrder(filesDbContext, fileId);

            await context.DeleteAuditReferencesAsync(fileId, FileEntryType.File);

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            foreach (var folderId in fromFolders)
            {                
                await DecrementCountAsync(context, folderId, tenantId, FileEntryType.File);
            }

            if (deleteFolder)
            {
                tenantQuotaController.Init(tenantId, ThumbnailTitle);
                var store = await storageFactory.GetStorageAsync(tenantId, FileConstant.StorageModule, tenantQuotaController);
                await store.DeleteDirectoryAsync(ownerId, GetUniqFileDirectory(fileId));
            }

            if (toDeleteFile != null)
            {
                await factoryIndexer.DeleteAsync(toDeleteFile);
                await factoryIndexerFormData.DeleteAsync(r=> r.Where(a => a.Id, toDeleteFile.Id));
            }
        });
    }

    public async Task<bool> IsExistAsync(string title, int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await filesDbContext.DbFilesAnyAsync(tenantId, title, folderId);
    }

    public async Task<TTo> MoveFileAsync<TTo>(int fileId, TTo toFolderId, bool deleteLinks = false)
    {
        if (toFolderId is int tId)
        {
            return IdConverter.Convert<TTo>(await MoveFileAsync(fileId, tId, deleteLinks));
        }

        if (toFolderId is string tsId)
        {
            return IdConverter.Convert<TTo>(await MoveFileAsync(fileId, tsId, deleteLinks));
        }

        throw new NotImplementedException();
    }

    public async Task<int> MoveFileAsync(int fileId, int toFolderId, bool deleteLinks = false)
    {
        if (fileId == 0)
        {
            return 0;
        }

        var tagDao = daoFactory.GetTagDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();
        var toFolder = await folderDao.GetFolderAsync(toFolderId);
        var file = await GetFileAsync(fileId);
        var fromFolder = await folderDao.GetFolderAsync(file.ParentId);
        var fileContentLength = file.ContentLength;
        var tenantId = _tenantManager.GetCurrentTenantId();

        var (fromRoomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(fromFolder);
        var (toRoomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(toFolder);

        var fromRoomTags = tagDao.GetTagsAsync(fileId, FileEntryType.File, TagType.FromRoom);
        var fromRoomTag = await fromRoomTags.FirstOrDefaultAsync();

        var trashId = await globalFolder.GetFolderTrashAsync(daoFactory);

        if (toRoomId != -1 && fromRoomId != toRoomId)
        {
            var toRoom = DocSpaceHelper.IsRoom(toFolder.FolderType) ? toFolder : await folderDao.GetFolderAsync(toRoomId);
            var quotaRoomSettings = await _settingsManager.LoadAsync<TenantRoomQuotaSettings>();
            if (quotaRoomSettings.EnableQuota)
            {
                var roomQuotaLimit = toRoom.SettingsQuota == TenantEntityQuotaSettings.DefaultQuotaValue ? quotaRoomSettings.DefaultQuota : toRoom.SettingsQuota;
                if (roomQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                {
                    if (roomQuotaLimit - toRoom.Counter < fileContentLength)
                    {
                        throw FileSizeComment.GetRoomFreeSpaceException(roomQuotaLimit);
                    }
                }
            }
        }
        else if (fromRoomId != -1 &&
                toFolder.FolderType is FolderType.USER or FolderType.DEFAULT &&
                toRoomId == -1 ||
                (fromFolder.Id == trashId && fromRoomTag != null))
        {
            var quotaUserSettings = await _settingsManager.LoadAsync<TenantUserQuotaSettings>();
            if (quotaUserSettings.EnableQuota)
            {
                var user = await _userManager.GetUsersAsync(toFolder.RootCreateBy);
                var userQuotaData = await _settingsManager.LoadAsync<UserQuotaSettings>(user);
                var userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota;
                var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenantId, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
                if (userQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                {
                    if (userQuotaLimit - userUsedSpace < fileContentLength)
                    {
                        throw FileSizeComment.GetUserFreeSpaceException(userQuotaLimit);
                    }
                }
            }
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var fromFolders = await context.ParentIdsAsync(tenantId, fileId).ToListAsync();

            var q = Query(context.Files).Where(r => r.Id == fileId);

            await using (var tx = await context.Database.BeginTransactionAsync())
            {
                var oldParentId = (await q.FirstOrDefaultAsync())?.ParentId;

                if (trashId.Equals(toFolderId))
                {
                    await q.ExecuteUpdateAsync(f => f
                    .SetProperty(p => p.ParentId, toFolderId)
                    .SetProperty(p => p.ModifiedBy, _authContext.CurrentAccount.ID)
                    .SetProperty(p => p.ModifiedOn, DateTime.UtcNow));
                    await DeleteCustomOrder(filesDbContext, fileId);
                }
                else
                {
                    await q.ExecuteUpdateAsync(f => f.SetProperty(p => p.ParentId, toFolderId));
                    await SetCustomOrder(filesDbContext, fileId, toFolderId);
                }

                var oldFolder = await folderDao.GetFolderAsync(oldParentId.Value);
                var (toFolderRoomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(toFolder);
                var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(oldFolder);
                var archiveId = await folderDao.GetFolderIDArchive(false);

                if (toFolderId == trashId && oldParentId.HasValue)
                {
                    var tagList = new List<Tag>();

                    if(roomId != -1)
                    {
                        tagList.Add(Tag.FromRoom(fileId, FileEntryType.File, _authContext.CurrentAccount.ID));
                    }

                    var origin = Tag.Origin(fileId, FileEntryType.File, oldParentId.Value, _authContext.CurrentAccount.ID);
                    tagList.Add(origin);
                    await tagDao.SaveTagsAsync(tagList);
                }
                else if (oldParentId == trashId || roomId != -1 || toFolderRoomId != -1)
                {
                    if ((toFolderId != archiveId && oldFolder.Id != archiveId) && 
                        toFolderRoomId == -1 && 
                        ((oldParentId == trashId && fromRoomTag != null) || roomId != -1))
                    {
                        file.RootCreateBy = toFolder.RootCreateBy;
                        file.RootFolderType = toFolder.FolderType;
                        await storageFactory.QuotaUsedAddAsync(
                            _tenantManager.GetCurrentTenantId(),
                            FileConstant.ModuleId, "",
                            WebItemManager.DocumentsProductID.ToString(), 
                            file.ContentLength, file.GetFileQuotaOwner());
                    }
                    if ((toFolderId != archiveId && oldFolder.Id != archiveId) && 
                        toFolderRoomId != -1 && 
                        ((oldParentId == trashId && fromRoomTag == null) || roomId == -1))
                    {
                        await storageFactory.QuotaUsedDeleteAsync(
                            _tenantManager.GetCurrentTenantId(),
                            FileConstant.ModuleId, "",
                            WebItemManager.DocumentsProductID.ToString(), 
                            file.ContentLength, file.GetFileQuotaOwner());
                    }
                    if (oldParentId == trashId)
                    {
                        await tagDao.RemoveTagLinksAsync(fileId, FileEntryType.File, TagType.FromRoom);
                        await tagDao.RemoveTagLinksAsync(fileId, FileEntryType.File, TagType.Origin);
                    }
                }

                if (deleteLinks)
                {
                    var id = fileId.ToString();
                    
                    await filesDbContext.DeleteTagLinksByTypeAsync(tenantId, id, FileEntryType.File, TagType.RecentByLink);
                    await filesDbContext.DeleteTagsAsync(tenantId);
                    await filesDbContext.DeleteLinksAsync(tenantId, id, FileEntryType.File);
                }

                await tx.CommitAsync();
                
                foreach (var f in fromFolders)
                {
                    await DecrementCountAsync(context, f, tenantId, FileEntryType.File);
                }
                

                if (oldParentId.HasValue)
                {
                    await UpdateUsedFileSpace(
                        folderDao,
                        oldFolder,
                        toFolder,
                        file,
                        fileContentLength);
                }
                
                await IncrementCountAsync(context, toFolderId, tenantId, FileEntryType.File);
            }

            var toUpdateFile = await q.FirstOrDefaultAsync(r => r.CurrentVersion);

            if (toUpdateFile != null)
            {
                toUpdateFile.Folders = await context.DbFolderTreesAsync(toFolderId).ToListAsync();

                _ = factoryIndexer.UpdateAsync(toUpdateFile, UpdateAction.Replace, w => w.Folders);
            }
        });

        return fileId;
    }

    public async Task<string> MoveFileAsync(int fileId, string toFolderId, bool deleteLinks = false)
    {
        var toSelector = selectorFactory.GetSelector(toFolderId);

        var moved = await crossDao.PerformCrossDaoFileCopyAsync(
            fileId, this, r => r,
            toFolderId, toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
            true);

        return moved.Id;
    }

    public async Task<File<TTo>> CopyFileAsync<TTo>(int fileId, TTo toFolderId)
    {
        if (toFolderId is int tId)
        {
            return await CopyFileAsync(fileId, tId) as File<TTo>;
        }

        if (toFolderId is string tsId)
        {
            return await CopyFileAsync(fileId, tsId) as File<TTo>;
        }

        throw new NotImplementedException();
    }

    public async Task<File<int>> CopyFileAsync(int fileId, int toFolderId)
    {
        var file = await GetFileAsync(fileId);
        if (file != null)
        {
            var copy = _serviceProvider.GetService<File<int>>();
            copy.SetFileStatus(await file.GetFileStatus());
            copy.ParentId = toFolderId;
            copy.Title = file.Title;
            copy.ConvertedType = file.ConvertedType;
            copy.Comment = FilesCommonResource.CommentCopy;
            copy.Encrypted = file.Encrypted;
            copy.Category = file.Category;
            copy.ThumbnailStatus = file.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;

            await using (var stream = await GetFileStreamAsync(file))
            {
                copy.ContentLength = stream.CanSeek ? stream.Length : file.ContentLength;
                copy = await SaveFileAsync(copy, stream);
            }

            if (file.ThumbnailStatus == Thumbnail.Created)
            {
                var tenantId = _tenantManager.GetCurrentTenantId();
                var dataStore = await storageFactory.GetStorageAsync(tenantId, FileConstant.StorageModule, (IQuotaController)null);

                foreach (var size in thumbnailSettings.Sizes)
                {
                    await dataStore.CopyAsync(string.Empty,
                                         GetUniqThumbnailPath(file, size.Width, size.Height),
                                         string.Empty,
                                         GetUniqThumbnailPath(copy, size.Width, size.Height));
                }

                await SetThumbnailStatusAsync(copy, Thumbnail.Created);

                copy.ThumbnailStatus = Thumbnail.Created;
            }

            return copy;
        }
        return null;
    }

    public async Task<File<string>> CopyFileAsync(int fileId, string toFolderId)
    {
        var toSelector = selectorFactory.GetSelector(toFolderId);

        var moved = await crossDao.PerformCrossDaoFileCopyAsync(
            fileId, this, r => r,
            toFolderId, toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
            false);

        return moved;
    }

    public async Task<int> FileRenameAsync(File<int> file, string newTitle)
    {
        newTitle = Global.ReplaceInvalidCharsAndTruncate(newTitle);

        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var toUpdate = await filesDbContext.DbFileAsync(tenantId, file.Id);

        toUpdate.Title = newTitle;
        toUpdate.ModifiedOn = DateTime.UtcNow;
        toUpdate.ModifiedBy = _authContext.CurrentAccount.ID;
        filesDbContext.Update(toUpdate);

        await filesDbContext.SaveChangesAsync();

        await factoryIndexer.UpdateAsync(toUpdate, true, r => r.Title, r => r.ModifiedBy, r => r.ModifiedOn);

        if (!Path.HasExtension(file.Title))
        {
            var storage = await storageFactory.GetStorageAsync(tenantId, FileConstant.StorageModule);
            await storage.MoveAsync(GetUniqFilePath(file), "", GetUniqFilePath(file, "content" + FileUtility.GetFileExtension(newTitle)));
        }

        return file.Id;
    }

    public async Task<string> UpdateCommentAsync(int fileId, int fileVersion, string comment)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        comment ??= string.Empty;
        comment = comment[..Math.Min(comment.Length, 255)];

        await filesDbContext.UpdateDbFilesCommentAsync(tenantId, fileId, fileVersion, comment);

        return comment;
    }

    public async Task CompleteVersionAsync(int fileId, int fileVersion)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await filesDbContext.UpdateDbFilesVersionGroupAsync(tenantId, fileId, fileVersion);
    }

    public async Task ContinueVersionAsync(int fileId, int fileVersion)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var versionGroup = await filesDbContext.VersionGroupAsync(tenantId, fileId, fileVersion);

        await filesDbContext.UpdateVersionGroupAsync(tenantId, fileId, fileVersion, versionGroup);
    }

    public bool UseTrashForRemove(File<int> file)
    {
        if (file.Encrypted && file.RootFolderType == FolderType.VirtualRooms)
        {
            return false;
        }

        return file.RootFolderType != FolderType.TRASH && file.RootFolderType != FolderType.Privacy;
    }

    public string GetUniqFileDirectory(int fileId)
    {
        if (fileId == 0)
        {
            throw new ArgumentNullException(nameof(fileId));
        }

        var folderId = (fileId / 1000 + 1) * 1000;

        return $"{FolderPathPart}{folderId}/{FilePathPart}{fileId}";
    }

    public string GetUniqFilePath(File<int> file)
    {
        return file != null
                   ? GetUniqFilePath(file, "content" + FileUtility.GetFileExtension(file.PureTitle))
                   : null;
    }

    public string GetUniqFilePath(File<int> file, string fileTitle)
    {
        return file != null
                   ? $"{GetUniqFileVersionPath(file.Id, file.Version)}/{fileTitle}"
                   : null;
    }

    public string GetUniqFileVersionPath(int fileId, int version)
    {
        return fileId != 0
                   ? string.Format("{0}/v{1}", GetUniqFileDirectory(fileId), version)
                   : null;
    }

    private async Task UpdateUsedFileSpace(IFolderDao<int> folderDao, Folder<int> fromFolder, Folder<int> toFolder, File<int> file, long size)
    {
        var (toFolderRoomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(toFolder);
        var (oldFolderRoomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(fromFolder);
       
        await folderDao.ChangeTreeFolderSizeAsync(toFolder.Id, file.ContentLength);
        await folderDao.ChangeTreeFolderSizeAsync(fromFolder.Id, (-1) * file.ContentLength);

        if (toFolderRoomId != -1 || oldFolderRoomId != -1)
        {
            var tenantId = _tenantManager.GetCurrentTenantId();

            if (toFolder.FolderType is FolderType.USER or FolderType.DEFAULT)
            {
                file.RootCreateBy = toFolder.RootCreateBy;
                file.RootFolderType = toFolder.FolderType;
                await storageFactory.QuotaUsedAddAsync(tenantId, FileConstant.ModuleId, "", WebItemManager.DocumentsProductID.ToString(), size, file.GetFileQuotaOwner());
            }
            if (fromFolder.FolderType is FolderType.USER or FolderType.DEFAULT)
            {
                await storageFactory.QuotaUsedDeleteAsync(tenantId, FileConstant.ModuleId, "", WebItemManager.DocumentsProductID.ToString(), size, file.GetFileQuotaOwner());
            }
        }

    }
    public static bool TryGetFileId(string path, out int fileId)
    {
        fileId = 0;
        
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        var match = _pattern.Match(path);
        
        return match.Success && match.Groups.TryGetValue(FileIdGroupName, out var group) && int.TryParse(group.Value, out fileId);
    }

    #region chunking

    public async Task<ChunkedUploadSession<int>> CreateUploadSessionAsync(File<int> file, long contentLength)
    {
        return await chunkedUploadSessionHolder.CreateUploadSessionAsync(file, contentLength);
    }

    public async Task<File<int>> UploadChunkAsync(ChunkedUploadSession<int> uploadSession, Stream stream, long chunkLength, int? chunkNumber = null)
    {
        if (!uploadSession.UseChunks)
        {
            if (uploadSession.BytesTotal == 0)
            {
                uploadSession.BytesTotal = chunkLength;
            }

            if (uploadSession.BytesTotal >= chunkLength)
            {
                uploadSession.File = await SaveFileAsync(await GetFileForCommitAsync(uploadSession), stream);
            }

            return uploadSession.File;
        }

        if (!chunkNumber.HasValue)
        {
            int.TryParse(uploadSession.GetItemOrDefault<string>("ChunksUploaded"), out var number);
            number++;
            uploadSession.Items["ChunksUploaded"] = number.ToString();
            chunkNumber = number;
        }
        await chunkedUploadSessionHolder.UploadChunkAsync(uploadSession, stream, chunkLength, chunkNumber.Value);

        return uploadSession.File;
    }

    public async Task<File<int>> FinalizeUploadSessionAsync(ChunkedUploadSession<int> uploadSession)
    {
        await chunkedUploadSessionHolder.FinalizeUploadSessionAsync(uploadSession);

        var file = await GetFileForCommitAsync(uploadSession);
        await SaveFileAsync(file, null, uploadSession.CheckQuota, true, uploadSession);

        return file;
    }

    public async Task AbortUploadSessionAsync(ChunkedUploadSession<int> uploadSession)
    {
        await chunkedUploadSessionHolder.AbortUploadSessionAsync(uploadSession);
    }

    private async Task<File<int>> GetFileForCommitAsync(ChunkedUploadSession<int> uploadSession)
    {
        if (uploadSession.File.Id != 0)
        {
            var file = await GetFileAsync(uploadSession.File.Id);
            if (!uploadSession.KeepVersion)
            {
                file.Version++;
                file.VersionGroup++;
            }
            file.ContentLength = uploadSession.BytesTotal;
            file.Category = uploadSession.File.Category;
            file.ConvertedType = null;
            file.Comment = FilesCommonResource.CommentUpload;
            file.Encrypted = uploadSession.Encrypted;
            file.ThumbnailStatus = Thumbnail.Waiting;
            //file.Order = uploadSession.Order;
            return file;
        }

        var result = _serviceProvider.GetService<File<int>>();
        result.ParentId = uploadSession.File.ParentId;
        result.Title = uploadSession.File.Title;
        result.ContentLength = uploadSession.BytesTotal;
        result.Comment = FilesCommonResource.CommentUpload;
        result.Encrypted = uploadSession.Encrypted;
        result.CreateOn = uploadSession.File.CreateOn;
        result.Category = uploadSession.File.Category;

        return result;
    }

    #endregion

    #region Only in TMFileDao

    public async Task ReassignFilesAsync(Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (exceptFolderIds == null || !exceptFolderIds.Any())
        {
            await filesDbContext.ReassignFilesAsync(tenantId, oldOwnerId, newOwnerId);
        }
        else
        {
            await filesDbContext.ReassignFilesPartiallyAsync(tenantId, oldOwnerId, newOwnerId, exceptFolderIds);
        }
    }

    public IAsyncEnumerable<File<int>> GetFilesAsync(IEnumerable<int> parentIds, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, string[] extension, bool searchInContent)
    {
        if (parentIds == null || !parentIds.Any() || filterType == FilterType.FoldersOnly)
        {
            return AsyncEnumerable.Empty<File<int>>();
        }

        return InternalGetFilesAsync(parentIds, filterType, subjectGroup, subjectID, searchText, extension, searchInContent);
    }

    public async Task ReassignRoomsFilesAsync(Guid fromOwner)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var items = await filesDbContext.GetRoomsFilesReassignInfoAsync(tenantId, fromOwner).ToListAsync();

        foreach (var group in items.GroupBy(x => x.RoomOwnerId))
        {
            await filesDbContext.ReassignSpecificFilesAsync(tenantId, group.Select(f => f.FileId), group.Key);
        }
    }

    private async IAsyncEnumerable<File<int>> InternalGetFilesAsync(IEnumerable<int> parentIds, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, string[] extension,
        bool searchInContent)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = GetFileQuery(filesDbContext, r => r.CurrentVersion)
            .Join(filesDbContext.Tree, a => a.ParentId, t => t.FolderId, (file, tree) => new { file, tree })
            .Where(r => parentIds.Contains(r.tree.ParentId))
            .Select(r => r.file);

        var searchByText = !string.IsNullOrEmpty(searchText);
        var searchByExtension = !extension.IsNullOrEmpty();
        
        if (extension.IsNullOrEmpty())
        {
            extension = [""];
        }

        if (searchByText || searchByExtension)
        {
            var searchIds = new List<int>();
            var success = false;
            foreach (var e in extension)
            {
                var func = GetFuncForSearch(null, null, filterType, subjectGroup, subjectID, searchText, e, searchInContent);

                (success, var result) = await factoryIndexer.TrySelectIdsAsync(s => func(s));
                if (!success)
                {
                    break;
                }
                searchIds = searchIds.Concat(result).ToList();
            }

            if (success)
            {
                q = q.Where(r => searchIds.Contains(r.Id));
            }
            else
            {
                if (searchByText)
                {
                    q = BuildSearch(q, searchText, SearchType.Any);
                }

                if (searchByExtension)
                {
                    q = BuildSearch(q, extension, SearchType.End);
                }
            }
        }

        if (subjectID != Guid.Empty)
        {
            if (subjectGroup)
            {
                var users = (await _userManager.GetUsersByGroupAsync(subjectID)).Select(u => u.Id).ToArray();
                q = q.Where(r => users.Contains(r.CreateBy));
            }
            else
            {
                q = q.Where(r => r.CreateBy == subjectID);
            }
        }

        switch (filterType)
        {
            case FilterType.DocumentsOnly:
            case FilterType.ImagesOnly:
            case FilterType.PresentationsOnly:
            case FilterType.SpreadsheetsOnly:
            case FilterType.ArchiveOnly:
            case FilterType.MediaOnly:
                q = q.Where(r => r.Category == (int)filterType);
                break;
            case FilterType.PdfForm:
                q = q.Where(r => (r.Category == (int)filterType || r.Category == (int)FilterType.None) && r.Title.ToLower().EndsWith(".pdf"));
                break;
            case FilterType.Pdf:
                q = q.Where(r => r.Category == (int)filterType || r.Title.ToLower().EndsWith(".pdf"));
                break;
            case FilterType.ByExtension:
                if (!string.IsNullOrEmpty(searchText))
                {
                    q = BuildSearch(q, searchText, SearchType.End);
                }
                break;
        }

        await foreach (var e in FromQuery(filesDbContext, q).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFileQuery, File<int>>(e);
        }
    }

    public async IAsyncEnumerable<File<int>> SearchAsync(string searchText, bool bunch = false)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var (success, ids) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(searchText));
        if (success)
        {
            var files = filesDbContext.DbFileQueriesByFileIdsAsync(tenantId, ids)
                .Select(mapper.Map<DbFileQuery, File<int>>)
                .Where(
                    f =>
                    bunch
                        ? f.RootFolderType == FolderType.BUNCH
                        : f.RootFolderType is FolderType.USER or FolderType.COMMON);
            await foreach (var file in files)
            {
                yield return file;
            }
        }
        else
        {
            var files = filesDbContext.DbFileQueriesByTextAsync(tenantId, GetSearchText(searchText))
                .Select(mapper.Map<DbFileQuery, File<int>>)
                .Where(f =>
                       bunch
                            ? f.RootFolderType == FolderType.BUNCH
                            : f.RootFolderType is FolderType.USER or FolderType.COMMON);
            await foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    public async Task<bool> IsExistOnStorageAsync(File<int> file)
    {
        return await (await globalStore.GetStoreAsync()).IsFileAsync(string.Empty, GetUniqFilePath(file));
    }
    
    private const string DiffTitle = "diff.zip";

    public async Task SaveEditHistoryAsync(File<int> file, string changes, Stream differenceStream)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrEmpty(changes);
        ArgumentNullException.ThrowIfNull(differenceStream);

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await filesDbContext.UpdateChangesAsync(tenantId, file.Id, file.Version, changes.Trim());

        await (await globalStore.GetStoreAsync()).SaveAsync(string.Empty, GetUniqFilePath(file, DiffTitle), differenceStream, DiffTitle);
    }

    public async IAsyncEnumerable<EditHistory> GetEditHistoryAsync(DocumentServiceHelper documentServiceHelper, int fileId, int fileVersion = 0)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        await foreach (var r in filesDbContext.DbFilesByVersionAndWithoutForcesaveAsync(tenantId, fileId, fileVersion))
        {
            var item = _serviceProvider.GetService<EditHistory>();

            item.ID = r.Id;
            item.Version = r.Version;
            item.VersionGroup = r.VersionGroup;
            item.ModifiedOn = _tenantUtil.DateTimeFromUtc(r.ModifiedOn);
            item.ModifiedBy = r.ModifiedBy;
            item.ChangesString = r.Changes;
            item.Key = await documentServiceHelper.GetDocKeyAsync(item.ID, item.Version, _tenantUtil.DateTimeFromUtc(r.CreateOn));

            yield return item;
        }
    }

    public async Task<Stream> GetDifferenceStreamAsync(File<int> file)
    {
        return await (await globalStore.GetStoreAsync()).GetReadStreamAsync(string.Empty, GetUniqFilePath(file, DiffTitle), 0);
    }

    public async Task<bool> ContainChangesAsync(int fileId, int fileVersion)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.DbFileAnyAsync(tenantId, fileId, fileVersion);
    }

    private const string ThumbnailTitle = "thumb";


    public async Task SetThumbnailStatusAsync(File<int> file, Thumbnail status)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        await filesDbContext.UpdateThumbnailStatusAsync(tenantId, file.Id, file.Version, status);
    }


    public string GetUniqThumbnailPath(File<int> file, int width, int height)
    {
        var thumbnailName = GetThumbnailName(width, height);

        return GetUniqFilePath(file, thumbnailName);
    }

    public async Task<Stream> GetThumbnailAsync(int fileId, int width, int height)
    {
        var file = await GetFileAsync(fileId);
        return await GetThumbnailAsync(file, width, height);
    }

    public async Task<Stream> GetThumbnailAsync(File<int> file, int width, int height)
    {
        var thumnailName = GetThumbnailName(width, height);
        var path = GetUniqFilePath(file, thumnailName);
        var storage = await globalStore.GetStoreAsync();
        var isFile = await storage.IsFileAsync(string.Empty, path);

        if (!isFile)
        {
            throw new FileNotFoundException();
        }

        return await storage.GetReadStreamAsync(string.Empty, path, 0);
    }

    public async IAsyncEnumerable<File<int>> GetFilesByTagAsync(Guid tagOwner, TagType tagType, FilterType filterType, bool subjectGroup, Guid subjectId,
        string searchText, string[] extension, bool searchInContent, bool excludeSubject, OrderBy orderBy, int offset = 0, int count = -1)
    {
        if (filterType == FilterType.FoldersOnly)
        {
            yield break;
        }
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var q = GetFilesByTagQuery(tagOwner, tagType, filesDbContext);

        q = await GetFilesQueryWithFilters(q, filterType, subjectGroup, subjectId, searchText, extension, searchInContent, excludeSubject);

        q = orderBy == null
            ? q
            : orderBy.SortedBy switch
            {
                SortedByType.Author => orderBy.IsAsc ? q.OrderBy(r => r.Entry.CreateBy) : q.OrderByDescending(r => r.Entry.CreateBy),
                SortedByType.Size => orderBy.IsAsc ? q.OrderBy(r => r.Entry.ContentLength) : q.OrderByDescending(r => r.Entry.ContentLength),
                SortedByType.AZ => orderBy.IsAsc ? q.OrderBy(r => r.Entry.Title) : q.OrderByDescending(r => r.Entry.Title),
                SortedByType.DateAndTime => orderBy.IsAsc ? q.OrderBy(r => r.Entry.ModifiedOn) : q.OrderByDescending(r => r.Entry.ModifiedOn),
                SortedByType.DateAndTimeCreation => orderBy.IsAsc ? q.OrderBy(r => r.Entry.CreateOn) : q.OrderByDescending(r => r.Entry.CreateOn),
                SortedByType.Type => orderBy.IsAsc
                    ? q.OrderBy(r => DbFunctionsExtension.SubstringIndex(r.Entry.Title, '.', -1))
                    : q.OrderByDescending(r => DbFunctionsExtension.SubstringIndex(r.Entry.Title, '.', -1)),
                SortedByType.LastOpened => orderBy.IsAsc ? q.OrderBy(r => r.TagLink.CreateOn) : q.OrderByDescending(r => r.TagLink.CreateOn),
                _ => q.OrderBy(r => r.Entry.Title)
            };
        
        if (offset > 0)
        {
            q = q.Skip(offset);
        }

        if (count > 0)
        {
            q = q.Take(count);
        }

        await foreach (var file in FromQuery(filesDbContext, q).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFileQuery, File<int>>(file);
        }
    }

    public async Task<int> GetFilesByTagCountAsync(Guid tagOwner, TagType tagType, FilterType filterType, bool subjectGroup, Guid subjectId,
        string searchText, string[] extension, bool searchInContent, bool excludeSubject)
    {
        if (filterType == FilterType.FoldersOnly)
        {
            return 0;
        }
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var q = GetFilesByTagQuery(tagOwner, tagType, filesDbContext);
        
        q = await GetFilesQueryWithFilters(q, filterType, subjectGroup, subjectId, searchText, extension, searchInContent, excludeSubject);

        return await q.CountAsync();
    }

    public async Task<long> GetTransferredBytesCountAsync(ChunkedUploadSession<int> uploadSession)
    {
        var chunks = await chunkedUploadSessionHolder.GetChunksAsync(uploadSession);
        
        return chunks.Sum(c => c.Value?.Length ?? 0);
    }

    private string GetThumbnailName(int width, int height)
    {
        return $"{ThumbnailTitle}.{width}x{height}.{global.ThumbnailExtension}";
    }

    public async Task<EntryProperties<int>> GetProperties(int fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var data = await filesDbContext.DataAsync(tenantId, fileId.ToString());
        return data != null ? EntryProperties<int>.Deserialize(data, logger) : null;
    }

    public async Task<Dictionary<int, EntryProperties<int>>> GetPropertiesAsync(IEnumerable<int> filesIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var properties = await filesDbContext.FilesPropertiesAsync(tenantId, filesIds.Select(f => f.ToString())).ToListAsync();
        
        var propertiesMap = new Dictionary<int, EntryProperties<int>>(properties.Count);
        foreach (var property in properties)
        {
            if (int.TryParse(property.EntryId, out var id))
            {
                propertiesMap.TryAdd(id, EntryProperties<int>.Deserialize(property.Data, logger));
            }
        }

        return propertiesMap;
    }

    public async Task SaveProperties(int fileId, EntryProperties<int> entryProperties)
    {
        string data;

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (entryProperties == null || string.IsNullOrEmpty(data = EntryProperties<int>.Serialize(entryProperties, logger)))
        {
            await filesDbContext.DeleteFilesPropertiesAsync(tenantId, fileId.ToString());
            return;
        }

        await filesDbContext.AddOrUpdateAsync(r => r.FilesProperties, new DbFilesProperties { TenantId = tenantId, EntryId = fileId.ToString(), Data = data });
        await filesDbContext.SaveChangesAsync();
    }

    public async Task<int> SetCustomOrder(int fileId, int parentFolderId, int order = 0)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await SetCustomOrder(filesDbContext, fileId, parentFolderId, order);
    }
    
    public async Task InitCustomOrder(Dictionary<int, int> fileIds, int parentFolderId)
    {
        await InitCustomOrder(fileIds, parentFolderId, FileEntryType.File);
    }
    
    private async Task<int> SetCustomOrder(FilesDbContext filesDbContext, int fileId, int parentFolderId, int order = 0)
    {
        return await SetCustomOrder(filesDbContext, fileId, parentFolderId, FileEntryType.File, order);
    }

    private async Task DeleteCustomOrder(FilesDbContext filesDbContext, int fileId)
    {
        await DeleteCustomOrder(filesDbContext, fileId, FileEntryType.File);
    }

    #endregion

    private Func<Selector<DbFile>, Selector<DbFile>> GetFuncForSearch(int? parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText,
        string extension, bool searchInContent, bool withSubfolders = false)
    {
        return s =>
        {
            Selector<DbFile> result = null;

            if (!string.IsNullOrEmpty(searchText))
            {
                result = searchInContent ? s.MatchAll(searchText) : s.Match(r => r.Title, searchText);
            }

            if (!string.IsNullOrEmpty(extension))
            {
                var pattern = $"*{extension}";
                
                if (result != null)
                {
                    result.Match(r => r.Title, pattern);
                }
                else
                {
                    result = s.Match(r => r.Title, pattern);
                }
            }

            if (parentId != null)
            {
                if (withSubfolders)
                {
                    result.In(a => a.Folders.Select(r => r.ParentId), [parentId]);
                }
                else
                {
                    result.InAll(a => a.Folders.Select(r => r.ParentId), [parentId]);
                }
            }

            if (orderBy != null)
            {
                switch (orderBy.SortedBy)
                {
                    case SortedByType.Author:
                        result.Sort(r => r.CreateBy, orderBy.IsAsc);
                        break;
                    case SortedByType.Size:
                        result.Sort(r => r.ContentLength, orderBy.IsAsc);
                        break;
                    //case SortedByType.AZ:
                    //    result.Sort(r => r.Title, orderBy.IsAsc);
                    //    break;
                    case SortedByType.DateAndTime:
                        result.Sort(r => r.ModifiedOn, orderBy.IsAsc);
                        break;
                    case SortedByType.DateAndTimeCreation:
                        result.Sort(r => r.CreateOn, orderBy.IsAsc);
                        break;
                }
            }

            if (subjectID != Guid.Empty)
            {
                if (subjectGroup)
                {
                    var users = _userManager.GetUsersByGroupAsync(subjectID).Result.Select(u => u.Id).ToArray();
                    result.In(r => r.CreateBy, users);
                }
                else
                {
                    result.Where(r => r.CreateBy, subjectID);
                }
            }

            switch (filterType)
            {

                case FilterType.DocumentsOnly:
                case FilterType.ImagesOnly:
                case FilterType.PresentationsOnly:
                case FilterType.SpreadsheetsOnly:
                case FilterType.ArchiveOnly:
                case FilterType.MediaOnly:
                    result.Where(r => r.Category, (int)filterType);
                    break;
            }

            return result;
        };
    }
    private Func<Selector<DbFormsItemDataSearch>, Selector<DbFormsItemDataSearch>> GetFuncForSearchInFormsData(int parentId, string searchText, FormsItemDto formsItemDto)
    {
        return s =>
        {
            s.Where(r => r.ParentId, parentId);
            s.Nested(a => a.FormsData, b => b.Term(c => c.FormsData.Select(r=> r.Key), char.ToLower(formsItemDto.Key[0]) + formsItemDto.Key[1..]) && b.Term(c => c.FormsData.Select(r=> r.Value), char.ToLower(searchText[0]) + searchText[1..]));

            return s;
        };
    }

    private IQueryable<DbFileQuery> FromQuery(FilesDbContext filesDbContext, IQueryable<DbFile> dbFiles)
    {
        return dbFiles
            .Select(r => new DbFileQuery
            {
                File = r,
                Root = (from f in filesDbContext.Folders
                        where f.Id ==
                        (from t in filesDbContext.Tree
                         where t.FolderId == r.ParentId
                         orderby t.Level descending
                         select t.ParentId
                         ).FirstOrDefault()
                        where f.TenantId == r.TenantId
                        select f
                          ).FirstOrDefault(),
                Order = (
                    from f in filesDbContext.FileOrder
                    where (
                        from rs in filesDbContext.RoomSettings 
                        where rs.TenantId == f.TenantId && rs.RoomId ==
                            (from t in filesDbContext.Tree
                                where t.FolderId == r.ParentId
                                orderby t.Level descending
                                select t.ParentId
                            ).Skip(1).FirstOrDefault()
                        select rs.Indexing).FirstOrDefault() && f.EntryId == r.Id && f.TenantId == r.TenantId && f.EntryType == FileEntryType.File
                    select f.Order
                ).FirstOrDefault()
            });
    }

    private static IQueryable<DbFileQuery> FromQuery(FilesDbContext filesDbContext, IQueryable<FileByTagQuery> dbFilesByTag)
    {
        return dbFilesByTag
            .Select(r => new DbFileQuery
            {
                File = r.Entry,
                Root = (from f in filesDbContext.Folders
                        where f.Id ==
                              (from t in filesDbContext.Tree
                                  where t.FolderId == r.Entry.ParentId
                                  orderby t.Level descending
                                  select t.ParentId
                              ).FirstOrDefault()
                        where f.TenantId == r.Entry.TenantId
                        select f
                    ).FirstOrDefault(),
                SharedRecord = r.Security,
                LastOpened = r.TagLink.CreateOn
            });
    }
    
    private static IQueryable<DbFileQuery> FromQueryWithShared(FilesDbContext filesDbContext, IQueryable<DbFile> dbFiles)
    {
        return dbFiles
            .Select(r => new DbFileQuery
            {
                File = r,
                Root = (from f in filesDbContext.Folders
                        where f.Id ==
                              (from t in filesDbContext.Tree
                                  where t.FolderId == r.ParentId
                                  orderby t.Level descending
                                  select t.ParentId
                              ).FirstOrDefault()
                        where f.TenantId == r.TenantId
                        select f
                    ).FirstOrDefault(),
                Shared = filesDbContext.Security.Any(s => 
                    s.TenantId == r.TenantId && s.EntryId == r.Id.ToString() && s.EntryType == FileEntryType.File && 
                    (s.SubjectType == SubjectType.PrimaryExternalLink || s.SubjectType == SubjectType.ExternalLink))
            });
    }
    
    private readonly ConcurrentDictionary<int, bool> _currentTenantStore = new();
    
    protected internal async Task<DbFile> InitDocumentAsync(DbFile dbFile, int? tenantId = null)
    {
        dbFile.Document = new Document
        {
            Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(""))
        };

        if (!await factoryIndexer.CanIndexByContentAsync(dbFile))
        {
            return dbFile;
        }

        var file = _serviceProvider.GetService<File<int>>();
        file.Id = dbFile.Id;
        file.Title = dbFile.Title;
        file.Version = dbFile.Version;
        file.ContentLength = dbFile.ContentLength;

        
        if (file.ContentLength > settings.MaxFileSize)
        {
            return dbFile;
        }


        if (tenantId.HasValue)
        {
            if (!_currentTenantStore.TryGetValue(tenantId.Value, out var result))
            {
                result = await (await globalStore.GetStoreAsync(tenantId.Value)).IsDirectoryAsync(string.Empty, String.Empty);
                _currentTenantStore.TryAdd(tenantId.Value, result);
            }

            if (!result)
            {            
                return dbFile;
            }
        }

        try
        {
            byte[] buffer;
            await using(var stream = await GetFileStreamForTenantAsync(file, tenantId))
            {
                if (stream == null)
                {
                    return dbFile;
                }

                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                buffer = ms.GetBuffer();
            }
        
            dbFile.Document = new Document
            {
                Data = Convert.ToBase64String(buffer)
            };
        }
        catch (FileNotFoundException )
        {
        }

        return dbFile;
    }

    private async Task<IQueryable<DbFile>> GetFilesQueryWithFilters(
        int parentId, 
        OrderBy orderBy, 
        FilterType filterType, 
        bool subjectGroup, 
        Guid subjectID, 
        string searchText, 
        bool searchInContent,
        bool withSubfolders, 
        bool excludeSubject, 
        int roomId, 
        string[] extension, 
        FilesDbContext filesDbContext,
        FormsItemDto formsItemDto)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();        
        var q = GetFileQuery(filesDbContext, r => r.ParentId == parentId && r.CurrentVersion);

        if (withSubfolders)
        {
            q = GetFileQuery(filesDbContext, r => r.CurrentVersion)
                .Join(filesDbContext.Tree, r => r.ParentId, a => a.FolderId, (file, tree) => new { file, tree })
                .Where(r => r.tree.ParentId == parentId)
                .Select(r => r.file);
        }

        var searchByText = !string.IsNullOrEmpty(searchText);
        var searchByExtension = !extension.IsNullOrEmpty();

        if (extension.IsNullOrEmpty())
        {
            extension = [""];
        }

        if (searchByText || searchByExtension)
        {
            var searchIds = new List<int>();
            var success = false;
            
            foreach (var e in extension)
            {
                var func = GetFuncForSearch(parentId, null, filterType, subjectGroup, subjectID, searchText, e, searchInContent);

                Expression<Func<Selector<DbFile>, Selector<DbFile>>> expression = s => func(s);

                (success, var result) = await factoryIndexer.TrySelectIdsAsync(expression);
                if (!success)
                {
                    break;
                }
                searchIds = searchIds.Concat(result).ToList();
            }

            if (searchInContent && formsItemDto != null)
            {
                var funcForSearchInFormsData = GetFuncForSearchInFormsData(parentId, searchText, formsItemDto);
                Expression<Func<Selector<DbFormsItemDataSearch>, Selector<DbFormsItemDataSearch>>> expressionSearchText = s => funcForSearchInFormsData(s);

                (success, var resultForm) = await factoryIndexerFormData.TrySelectIdsAsync(expressionSearchText);
            if (success)
            {
                    searchIds = searchIds.Intersect(resultForm).ToList();
                }
            }

            if (success)
            {
                q = q.Where(r => searchIds.Contains(r.Id));
            }
            else
            {
                if (searchByText)
                {
                    q = BuildSearch(q, searchText, SearchType.Any);
                }

                if (searchByExtension)
                {
                    q = BuildSearch(q, extension, SearchType.End);
                }
            }
        }

        q = orderBy == null
            ? q
            : orderBy.SortedBy switch
            {
                SortedByType.Author => orderBy.IsAsc ? q.OrderBy(r => r.CreateBy) : q.OrderByDescending(r => r.CreateBy),
                SortedByType.Size => orderBy.IsAsc ? q.OrderBy(r => r.ContentLength) : q.OrderByDescending(r => r.ContentLength),
                SortedByType.AZ => orderBy.IsAsc ? q.OrderBy(r => r.Title) : q.OrderByDescending(r => r.Title),
                SortedByType.DateAndTime => orderBy.IsAsc ? q.OrderBy(r => r.ModifiedOn) : q.OrderByDescending(r => r.ModifiedOn),
                SortedByType.DateAndTimeCreation => orderBy.IsAsc ? q.OrderBy(r => r.CreateOn) : q.OrderByDescending(r => r.CreateOn),
                SortedByType.Type => orderBy.IsAsc
                    ? q.OrderBy(r => DbFunctionsExtension.SubstringIndex(r.Title, '.', -1))
                    : q.OrderByDescending(r => DbFunctionsExtension.SubstringIndex(r.Title, '.', -1)),
                SortedByType.CustomOrder => q.Join(filesDbContext.FileOrder, a => a.Id, b => b.EntryId, (file, order) => new { file, order })
                    .Where(r => r.order.EntryType == FileEntryType.File && r.order.TenantId == r.file.TenantId)
                    .OrderBy(r => r.order.Order)
                    .Select(r => r.file),
                _ => q.OrderBy(r => r.Title)
            };

        if (subjectID != Guid.Empty)
        {

            if (subjectGroup)
            {
                var users = (await _userManager.GetUsersByGroupAsync(subjectID)).Select(u => u.Id).ToArray();
                q = q.Where(r => users.Contains(r.CreateBy));
            }
            else
            {
                q = excludeSubject ? q.Where(r => r.CreateBy != subjectID) : q.Where(r => r.CreateBy == subjectID);
            }
        }

        switch (filterType)
        {

            case FilterType.DocumentsOnly:
            case FilterType.ImagesOnly:
            case FilterType.PresentationsOnly:
            case FilterType.SpreadsheetsOnly:
            case FilterType.ArchiveOnly:
            case FilterType.MediaOnly:
                q = q.Where(r => r.Category == (int)filterType);
                break;
            case FilterType.PdfForm:
                q = q.Where(r => (r.Category == (int)filterType || r.Category == (int)FilterType.None) && r.Title.ToLower().EndsWith(".pdf"));
                break;
            case FilterType.Pdf:
                q = q.Where(r => r.Category == (int)filterType || r.Title.ToLower().EndsWith(".pdf"));
                break;
            case FilterType.ByExtension:
                if (!string.IsNullOrEmpty(searchText))
                {
                    q = BuildSearch(q, searchText, SearchType.End);
                }

                break;
        }

        if (roomId != 0)
        {
            q = q.Join(filesDbContext.TagLink.Join(filesDbContext.Tag, l => l.TagId, t => t.Id, (l, t) => new
            {
                t.TenantId,
                t.Type,
                t.Name,
                l.EntryId,
                l.EntryType
            }), f => f.Id.ToString(), t => t.EntryId, (file, tag) => new { file, tag })
                .Where(r => r.tag.Type == TagType.Origin && r.tag.EntryType == FileEntryType.File && filesDbContext.Folders.Where(f =>
                        f.TenantId == tenantId && f.Id == filesDbContext.Tree.Where(t => t.FolderId == Convert.ToInt32(r.tag.Name))
                            .OrderByDescending(t => t.Level)
                            .Select(t => t.ParentId)
                            .Skip(1)
                            .FirstOrDefault())
                    .Select(f => f.Id)
                    .FirstOrDefault() == roomId)
                .Select(r => r.file);
        }

        return q;
    }
    
    private async Task<IQueryable<T>> GetFilesQueryWithFilters<T>(IQueryable<T> q, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText, 
        string[] extension, bool searchInContent, bool excludeSubject) 
        where T: IQueryResult<DbFile>
    {
        var searchByText = !string.IsNullOrEmpty(searchText);
        var searchByExtension = !extension.IsNullOrEmpty();
        
        if (extension.IsNullOrEmpty())
        {
            extension = [string.Empty];
        }

        if (searchByText || searchByExtension)
        {
            var searchIds = new List<int>();
            var success = false;
            foreach (var e in extension)
            {
                var func = GetFuncForSearch(null, null, filterType, subjectGroup, subjectId, searchText, e, searchInContent);

                Expression<Func<Selector<DbFile>, Selector<DbFile>>> expression = s => func(s);

                (success, var result) = await factoryIndexer.TrySelectIdsAsync(expression);
                if (!success)
                {
                    break;
                }
                searchIds = searchIds.Concat(result).ToList();
            }

            if (success)
            {
                q = q.Where(r => searchIds.Contains(r.Entry.Id));
            }
            else
            {
                if (searchByText)
                {
                    q = BuildSearch<T, DbFile>(q, searchText, SearchType.Any);
                }

                if (searchByExtension)
                {
                    q = BuildSearch<T, DbFile>(q, extension, SearchType.End);
                }
            }
        }

        if (subjectId != Guid.Empty)
        {
            if (subjectGroup)
            {
                var users = (await _userManager.GetUsersByGroupAsync(subjectId)).Select(u => u.Id).ToArray();
                q = q.Where(r => users.Contains(r.Entry.CreateBy));
            }
            else
            {
                q = excludeSubject ? q.Where(r => r.Entry.CreateBy != subjectId) : q.Where(r => r.Entry.CreateBy == subjectId);
            }
        }

        switch (filterType)
        {

            case FilterType.DocumentsOnly:
            case FilterType.ImagesOnly:
            case FilterType.PresentationsOnly:
            case FilterType.SpreadsheetsOnly:
            case FilterType.ArchiveOnly:
            case FilterType.MediaOnly:
                q = q.Where(r => r.Entry.Category == (int)filterType);
                break;
            case FilterType.PdfForm:
                q = q.Where(r => (r.Entry.Category == (int)filterType || r.Entry.Category == (int)FilterType.None) && r.Entry.Title.ToLower().EndsWith(".pdf"));
                break;
            case FilterType.Pdf:
                q = q.Where(r => r.Entry.Category == (int)filterType || r.Entry.Title.ToLower().EndsWith(".pdf"));
                break;
            case FilterType.ByExtension:
                if (!string.IsNullOrEmpty(searchText))
                {
                    q = BuildSearch<T, DbFile>(q, searchText, SearchType.End);
                }
                break;
        }

        return q;
    }
    
    private IQueryable<FileByTagQuery> GetFilesByTagQuery(Guid tagOwner, TagType tagType, FilesDbContext filesDbContext)
    {
        IQueryable<FileByTagQuery> query;
        
        var tenantId = _tenantManager.GetCurrentTenantId();
        
        var initQuery = filesDbContext.Tag
            .Where(x => x.TenantId == tenantId && x.Owner == tagOwner && x.Type == tagType)
            .Join(filesDbContext.TagLink,
                t => new
                {
                    t.TenantId,
                    TagId = t.Id 
                }, 
                l => new
                {
                    l.TenantId,
                    l.TagId
                },
                (t, l) => new { t, l })
            .Where(x => x.l.EntryType == FileEntryType.File)
            .Join(filesDbContext.Files,
                x => Convert.ToInt32(x.l.EntryId),
                f => f.Id,
                (x, f) => new { f, x.l, x.t })
            .Where(x => x.f.CurrentVersion);
        
        if (tagType == TagType.RecentByLink)
        {
            query = initQuery.Join(filesDbContext.Security, 
                    x => new
                    {
                        tenantId,
                        entryId = x.l.EntryId,
                        entryType = x.l.EntryType,
                        subject = x.t.Name
                    },
                    s => new
                    {
                        tenantId = s.TenantId,
                        entryId = s.EntryId,
                        entryType = s.EntryType,
                        subject = s.Subject.ToString()
                    },
                    (x, s) => new
                    { 
                        x.f,
                        x.t,
                        x.l,
                        s, 
                        expirationDate = (DateTime)(object)DbFunctionsExtension.JsonValue(nameof(s.Options), "ExpirationDate")
                    })
                .Where(x => x.s.Share != FileShare.Restrict && (x.expirationDate == DateTime.MinValue || x.expirationDate > DateTime.UtcNow))
                .Select(x => new FileByTagQuery
                {
                    Entry = x.f, 
                    Tag = x.t, 
                    TagLink = x.l, 
                    Security = x.s
                }); 
        }
        else
        {
            query = initQuery.Select(x => new FileByTagQuery
            {
                Entry = x.f, 
                Tag = x.t, 
                TagLink = x.l
            });
        }

        return query;
    }
}

public class DbFileQuery
{
    public DbFile File { get; init; }
    public DbFolder Root { get; set; }
    public bool Shared { get; set; }
    public int Order { get; set; }
    public DbFilesSecurity SharedRecord { get; set; }
    public DateTime? LastOpened { get; set; }
}

public class FileByTagQuery : IQueryResult<DbFile>
{
    public DbFile Entry { get; set; }
    public DbFilesTag Tag { get; set; }
    public DbFilesTagLink TagLink { get; set; }
    public DbFilesSecurity Security { get; set; }
}

public class DbFileQueryWithSecurity
{
    public DbFileQuery DbFileQuery { get; init; }
    public DbFilesSecurity Security { get; init; }
}

public record FileReassignInfo 
{
    public int FileId { get; init; }
    public Guid RoomOwnerId { get; init; }
}