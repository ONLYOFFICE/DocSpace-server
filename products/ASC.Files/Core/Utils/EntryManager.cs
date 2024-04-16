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

namespace ASC.Web.Files.Utils;

[Scope]
public class LockerManager(AuthContext authContext, IDaoFactory daoFactory)
{
    public async Task<bool> FileLockedForMeAsync<T>(T fileId, Guid userId = default)
    {
        userId = userId == Guid.Empty ? authContext.CurrentAccount.ID : userId;
        var tagDao = daoFactory.GetTagDao<T>();
        var tagLock = await tagDao.GetTagsAsync(fileId, FileEntryType.File, TagType.Locked).FirstOrDefaultAsync();
        var lockedBy =  tagLock?.Owner ?? Guid.Empty;

        return lockedBy != Guid.Empty && lockedBy != userId;
    }
    }

[Scope]
public class BreadCrumbsManager(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext)
{
    public async Task<string> GetBreadCrumbsOrderAsync<T>(T folderId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        var breadcrumbs = (await GetBreadCrumbsAsync(folderId, folderDao));

        var result = breadcrumbs.Skip(2).Select(r => r.Order.ToString()).ToList();

        return result.Count != 0 ? result.Aggregate((first, second) => $"{first}.{second}") : null;
        }

    public async Task<List<FileEntry>> GetBreadCrumbsAsync<T>(T folderId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        return await GetBreadCrumbsAsync(folderId, folderDao);
    }

    public async Task<List<FileEntry>> GetBreadCrumbsAsync<T>(T folderId, IFolderDao<T> folderDao)
    {
        if (folderId == null)
        {
            return new List<FileEntry>();
        }

        var breadCrumbs = await fileSecurity.FilterReadAsync(folderDao.GetParentFoldersAsync(folderId)).Cast<FileEntry>().ToListAsync();
        var firstVisible = breadCrumbs.ElementAtOrDefault(0) as Folder<T>;

        var rootId = 0;
        if (firstVisible == null)
        {
            rootId = await globalFolderHelper.FolderShareAsync;
        }
        else if (firstVisible.ProviderEntry && firstVisible.ProviderMapped && (firstVisible.RootFolderType is FolderType.VirtualRooms or FolderType.Archive))
        {
            if (authContext.IsAuthenticated && firstVisible.ShareRecord is not { IsLink: true })
            {
                rootId = firstVisible.RootFolderType == FolderType.VirtualRooms 
                    ? await globalFolderHelper.FolderVirtualRoomsAsync
                    : await globalFolderHelper.FolderArchiveAsync;
            }
                
            breadCrumbs = breadCrumbs.SkipWhile(f => f is Folder<T> folder && !DocSpaceHelper.IsRoom(folder.FolderType)).ToList();
        }
        else
        {
            switch (firstVisible.FolderType)
            {
                case FolderType.DEFAULT:
                    if (!firstVisible.ProviderEntry)
                    {
                        rootId = await globalFolderHelper.FolderShareAsync;
                    }
                    else
                    {
                        rootId = firstVisible.RootFolderType switch
                        {
                            FolderType.USER => authContext.CurrentAccount.ID == firstVisible.RootCreateBy
                                ? await globalFolderHelper.FolderMyAsync
                                : await globalFolderHelper.FolderShareAsync,
                            FolderType.COMMON => await globalFolderHelper.FolderCommonAsync,
                            _ => rootId
                        };
                    }
                    break;

                case FolderType.BUNCH:
                    rootId = await globalFolderHelper.FolderProjectsAsync;
                    break;
            }
        }

        var folderDaoInt = daoFactory.GetFolderDao<int>();

        if (rootId != 0)
        {
            breadCrumbs.Insert(0, await folderDaoInt.GetFolderAsync(rootId));
        }

        return breadCrumbs;
    }
}

[Scope]
public class EntryStatusManager(IDaoFactory daoFactory, AuthContext authContext, Global global)
{
    public async Task SetFileStatusAsync<T>(File<T> file)
    {
        if (file == null || file.Id == null)
        {
            return;
        }

        await SetFileStatusAsync(new List<File<T>>(1) { file });
    }

    public async Task SetFileStatusAsync<T>(IEnumerable<File<T>> files)
    {
        if (!files.Any())
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsTask = tagDao.GetTagsAsync(TagType.Locked, files).ToDictionaryAsync(k => k.EntryId, v => v);
        var tagsNewTask = tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, files).ToListAsync();

        var tags = await tagsTask;
        var tagsNew = await tagsNewTask;

        foreach (var file in files)
        {
            if (tags.TryGetValue(file.Id, out var lockedTag))
            {
                var lockedBy = lockedTag.Owner;
                file.Locked = lockedBy != Guid.Empty;
                file.LockedBy = lockedBy != Guid.Empty && lockedBy != authContext.CurrentAccount.ID
                    ? await global.GetUserNameAsync(lockedBy)
                    : null;

                continue;
            }

            if (tagsNew.Exists(r => r.EntryId.Equals(file.Id)))
            {
                file.IsNew = true;
            }
        }
    }

    public async Task SetIsFavoriteFolderAsync<T>(Folder<T> folder)
    {
        if (folder == null || folder.Id == null)
        {
            return;
        }

        await SetIsFavoriteFoldersAsync(new List<Folder<T>>(1) { folder });
    }

    public async Task SetIsFavoriteFoldersAsync<T>(IEnumerable<Folder<T>> folders)
    {
        if (!folders.Any())
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tagsFavorite = await tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.Favorite, folders).ToListAsync();

        foreach (var folder in folders.Where(f => tagsFavorite.Exists(r => r.EntryId.Equals(f.Id))))
        {
            folder.IsFavorite = true;
        }
    }
}

[Scope]
public class EntryManager(IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    GlobalFolderHelper globalFolderHelper,
    PathProvider pathProvider,
    AuthContext authContext,
    FileMarker fileMarker,
    FileUtility fileUtility,
    GlobalStore globalStore,
    FilesSettingsHelper filesSettingsHelper,
    UserManager userManager,
    ILogger<EntryManager> logger,
    FileShareLink fileShareLink,
    DocumentServiceHelper documentServiceHelper,
    ThirdpartyConfiguration thirdpartyConfiguration,
    DocumentServiceConnector documentServiceConnector,
    LockerManager lockerManager,
    BreadCrumbsManager breadCrumbsManager,
    SettingsManager settingsManager,
    IServiceProvider serviceProvider,
    ICache cache,
    FileTrackerHelper fileTracker,
    EntryStatusManager entryStatusManager,
    IHttpClientFactory clientFactory,
    ThumbnailSettings thumbnailSettings,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    SocketManager socketManager,
    FilesMessageService filesMessageService,
    BaseCommonLinkUtility commonLinkUtility,
    SecurityContext securityContext,
    FormFillingReportCreator formFillingReportCreator,
    TenantUtil tenantUtil,
    IDistributedLockProvider distributedLockProvider,
    TempStream tempStream,
    FileSharing fileSharing)
{
    private const string UpdateList = "filesUpdateList";

    public async Task<(IEnumerable<FileEntry> Entries, int Total)> GetEntriesAsync<T>(Folder<T> parent, int from, int count, FilterType filterType, bool subjectGroup, Guid subjectId,
        string searchText, string[] extension, bool searchInContent, bool withSubfolders, OrderBy orderBy, T roomId = default, SearchArea searchArea = SearchArea.Active, bool withoutTags = false, IEnumerable<string> tagNames = null,
        bool excludeSubject = false, ProviderFilter provider = ProviderFilter.None, SubjectFilter subjectFilter = SubjectFilter.Owner, ApplyFilterOption applyFilterOption = ApplyFilterOption.All, QuotaFilter quotaFilter = QuotaFilter.All)
    {
        int total;
        var withShared = false;

        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent), FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (parent.ProviderEntry && !await filesSettingsHelper.GetEnableThirdParty())
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        if (parent.RootFolderType == FolderType.Privacy && (!PrivacyRoomSettings.IsAvailable() || !await PrivacyRoomSettings.GetEnabledAsync(settingsManager)))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        var entries = new List<FileEntry>();

        searchInContent = searchInContent && filterType != FilterType.ByExtension && !Equals(parent.Id, await globalFolderHelper.FolderTrashAsync);

        if (parent.FolderType == FolderType.TRASH)
        {
            withSubfolders = false;
        }

        if (parent.RootFolderType == FolderType.USER)
        {
            withShared = true;
        }

        var (foldersFilterType, foldersSearchText) = applyFilterOption != ApplyFilterOption.Files ? (filterType, searchText) : (FilterType.None, string.Empty);

        if (!extension.IsNullOrEmpty())
        {
            extension = extension.Select(e => e.Trim()).Select(e => e.StartsWith('.') ? e : $".{e}").ToArray();

            if (applyFilterOption == ApplyFilterOption.All)
            {
                filterType = foldersFilterType = FilterType.FilesOnly;
        }
        }
        
        var (filesFilterType, filesSearchText, fileExtension) = applyFilterOption != ApplyFilterOption.Folders ? (filterType, searchText, extension) : (FilterType.None, string.Empty, new string[] {});

        if (parent.FolderType == FolderType.Projects && parent.Id.Equals(await globalFolderHelper.FolderProjectsAsync))
        {

        }
        else if (parent.FolderType == FolderType.SHARE)
        {
            //share
            var shared = await fileSecurity.GetSharesForMeAsync(filterType, subjectGroup, subjectId, searchText, extension, searchInContent, withSubfolders).ToListAsync();

            entries.AddRange(shared);

            CalculateTotal();
        }
        else if (parent.FolderType == FolderType.Recent)
        {
            if (searchArea == SearchArea.RecentByLinks)
            {
                var fileDao = daoFactory.GetFileDao<T>();
                var userId = authContext.CurrentAccount.ID;

                var filesTotalCountTask = fileDao.GetFilesByTagCountAsync(userId, TagType.RecentByLink, filterType, subjectGroup, subjectId, searchText, extension, searchInContent, excludeSubject);
                var files = await fileDao.GetFilesByTagAsync(userId, TagType.RecentByLink, filterType, subjectGroup, subjectId, searchText, extension, searchInContent, excludeSubject, orderBy, from, count).ToListAsync();
                
                entries.AddRange(files);

                total = await filesTotalCountTask;

                return (entries, total);
            }
            else
            {
                var files = await GetRecentAsync(filterType, subjectGroup, subjectId, searchText, extension, searchInContent);
                entries.AddRange(files);

                CalculateTotal();
            }
        }
        else if (parent.FolderType == FolderType.Favorites)
        {
            var (files, folders) = await GetFavoritesAsync(filterType, subjectGroup, subjectId, searchText, extension, searchInContent);

            entries.AddRange(folders);
            entries.AddRange(files);

            CalculateTotal();
        }
        else if (parent.FolderType == FolderType.Templates)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var fileDao = daoFactory.GetFileDao<T>();
            var files = await GetTemplatesAsync(folderDao, fileDao, filterType, subjectGroup, subjectId, searchText, extension, searchInContent).ToListAsync();
            entries.AddRange(files);

            CalculateTotal();
        }
        else if (parent.FolderType == FolderType.Privacy)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var fileDao = daoFactory.GetFileDao<T>();

            var folders = folderDao.GetFoldersAsync(parent.Id, orderBy, filterType, subjectGroup, subjectId, searchText, withSubfolders);
            var files = fileDao.GetFilesAsync(parent.Id, orderBy, filterType, subjectGroup, subjectId, searchText, extension, searchInContent, withSubfolders);
            //share
            var shared = fileSecurity.GetPrivacyForMeAsync(filterType, subjectGroup, subjectId, searchText, extension, searchInContent, withSubfolders);

            var task1 = fileSecurity.FilterReadAsync(folders).ToListAsync();
            var task2 = fileSecurity.FilterReadAsync(files).ToListAsync();
            var task3 = shared.ToListAsync();

            entries.AddRange(await task1);
            entries.AddRange(await task2);
            entries.AddRange(await task3);

            CalculateTotal();
        }
        else if (parent.FolderType is FolderType.VirtualRooms or FolderType.Archive && !parent.ProviderEntry)
        {
            entries = await fileSecurity.GetVirtualRoomsAsync(filterType, subjectId, searchText, searchInContent, withSubfolders, searchArea, withoutTags, tagNames, excludeSubject, provider, subjectFilter, quotaFilter);

            CalculateTotal();
        }
        else if (!parent.ProviderEntry)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var fileDao = daoFactory.GetFileDao<T>();

            var allFoldersCountTask = folderDao.GetFoldersCountAsync(parent.Id, foldersFilterType, subjectGroup, subjectId, foldersSearchText, withSubfolders, excludeSubject, roomId);
            var allFilesCountTask = fileDao.GetFilesCountAsync(parent.Id, filesFilterType, subjectGroup, subjectId, filesSearchText, fileExtension, searchInContent, withSubfolders, excludeSubject, roomId);

            var containingMyFiles = false;
            if (parent.FolderType == FolderType.ReadyFormFolder || parent.FolderType == FolderType.InProcessFormFolder)
            {
                var (currentRoomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(parent);
                var room = await folderDao.GetFolderAsync((T)Convert.ChangeType(currentRoomId, typeof(T))).NotFoundIfNull();
                var ace = await fileSharing.GetPureSharesAsync(room, new List<Guid> { authContext.CurrentAccount.ID }).FirstOrDefaultAsync();

                if (ace is { Access: FileShare.FillForms })
                {
                    containingMyFiles = true;
                }
            }
            var folders = await folderDao.GetFoldersAsync(parent.Id, orderBy, foldersFilterType, subjectGroup, subjectId, foldersSearchText, withSubfolders, excludeSubject, from, count, roomId, containingMyFiles)
                .ToListAsync();

            var filesCount = count - folders.Count;
            var filesOffset = Math.Max(folders.Count > 0 ? 0 : from - await allFoldersCountTask, 0);

            var files = await fileDao.GetFilesAsync(parent.Id, orderBy, filesFilterType, subjectGroup, subjectId, filesSearchText, fileExtension, searchInContent, withSubfolders, excludeSubject, filesOffset, filesCount, roomId, withShared)
                .ToListAsync();

            if (parent.FolderType == FolderType.FillingFormsRoom )
            {
                var ace = await fileSharing.GetPureSharesAsync(parent, new List<Guid> { authContext.CurrentAccount.ID }).FirstOrDefaultAsync();

                if (ace is { Access: FileShare.FillForms })
                {
                    for (var i = files.Count - 1; i >= 0; i--)
                    {
                        var fileExt = FileUtility.GetFileExtension(files[i].Title);
                        var fileType = FileUtility.GetFileTypeByExtention(fileExt);

                        var properties = await daoFactory.GetFileDao<T>().GetProperties(files[i].Id);
                        if (fileType == FileType.Pdf && (properties == null || !properties.FormFilling.StartFilling))
                        {
                            files.Remove(files[i]);
                        }

                    }
                }
            }

            entries = new List<FileEntry>(folders.Count + files.Count);
            entries.AddRange(folders);
            entries.AddRange(files);

            var fileStatusTask = entryStatusManager.SetFileStatusAsync(files);
            var tagsNewTask = fileMarker.SetTagsNewAsync(parent, entries);
            var originsTask = SetOriginsAsync(parent, entries);

            await Task.WhenAll(fileStatusTask, tagsNewTask, originsTask);

            total = await allFoldersCountTask + await allFilesCountTask;

            return (entries, total);
        }
        else
        {
            var folders = daoFactory.GetFolderDao<T>().GetFoldersAsync(parent.Id, orderBy, foldersFilterType, subjectGroup, subjectId, foldersSearchText, withSubfolders, excludeSubject);
            var files = daoFactory.GetFileDao<T>().GetFilesAsync(parent.Id, orderBy, filesFilterType, subjectGroup, subjectId, filesSearchText, fileExtension, searchInContent, withSubfolders, excludeSubject, withShared: withShared);

            var task1 = fileSecurity.FilterReadAsync(folders).ToListAsync();
            var task2 = fileSecurity.FilterReadAsync(files).ToListAsync();

            if (filterType is FilterType.None or FilterType.FoldersOnly)
            {
                var folderList = GetThirdPartyFoldersAsync(parent, searchText);
                var thirdPartyFolder = FilterEntries(folderList, filterType, subjectGroup, subjectId, searchText, searchInContent);

                var task3 = thirdPartyFolder.ToListAsync();

                foreach (var items in await Task.WhenAll(task1.AsTask(), task2.AsTask()))
                {
                    entries.AddRange(items);
                }

                entries.AddRange(await task3);
            }
            else
            {
                foreach (var items in await Task.WhenAll(task1.AsTask(), task2.AsTask()))
                {
                    entries.AddRange(items);
                }
            }
        }

        total = entries.Count;

        IEnumerable<FileEntry> data = entries;

        if (orderBy.SortedBy != SortedByType.New)
        {
            if (parent.FolderType != FolderType.Recent)
            {
                data = await SortEntries<T>(data, orderBy, provider != ProviderFilter.Storage);
            }

            if (0 < from)
            {
                data = data.Skip(from);
            }

            if (0 < count)
            {
                data = data.Take(count);
            }

            data = data.ToList();
        }

        await fileMarker.SetTagsNewAsync(parent, data);

        //sorting after marking
        if (orderBy.SortedBy == SortedByType.New)
        {
            data = await SortEntries<T>(data, orderBy, provider != ProviderFilter.Storage);

            if (0 < from)
            {
                data = data.Skip(from);
            }

            if (0 < count)
            {
                data = data.Take(count);
            }

            data = data.ToList();
        }

        var internalFiles = new List<File<int>>();
        var internalFolders = new List<Folder<int>>();
        var thirdPartyFiles = new List<File<string>>();
        var thirdPartyFolders = new List<Folder<string>>();

        foreach (var item in data.Where(r => r != null))
        {
            if (item.FileEntryType == FileEntryType.File)
            {
                if (item is File<int> internalFile)
                {
                    internalFiles.Add(internalFile);
                }
                else if (item is File<string> thirdPartyFile)
                {
                    thirdPartyFiles.Add(thirdPartyFile);
                }
            }
            else
            {
                if (item is Folder<int> internalFolder)
                {
                    internalFolders.Add(internalFolder);
                }
                else if (item is Folder<string> thirdPartyFolder)
                {
                    thirdPartyFolders.Add(thirdPartyFolder);
                }
            }
        }

        var t1 = entryStatusManager.SetFileStatusAsync(internalFiles);
        var t2 = entryStatusManager.SetIsFavoriteFoldersAsync(internalFolders);
        var t3 = entryStatusManager.SetFileStatusAsync(thirdPartyFiles);
        var t4 = entryStatusManager.SetIsFavoriteFoldersAsync(thirdPartyFolders);
        await Task.WhenAll(t1, t2, t3, t4);

        return (data, total);

        void CalculateTotal()
        {
            foreach (var f in entries)
            {
                if (f is IFolder fold)
                {
                    parent.FilesCount += fold.FilesCount;
                    parent.FoldersCount += fold.FoldersCount + 1;
                }
                else
                {
                    parent.FilesCount += 1;
                }
            }
        }
    }

    public async IAsyncEnumerable<FileEntry<T>> GetTemplatesAsync<T>(IFolderDao<T> folderDao, IFileDao<T> fileDao, FilterType filter, bool subjectGroup, Guid subjectId, string searchText,
        string[] extension, bool searchInContent)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var tags = tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.Template);

        var fileIds = await tags.Where(tag => tag.EntryType == FileEntryType.File).Select(tag => (T)Convert.ChangeType(tag.EntryId, typeof(T))).ToArrayAsync();

        var filesAsync = fileDao.GetFilesFilteredAsync(fileIds, filter, subjectGroup, subjectId, searchText, extension, searchInContent);
        var files = fileSecurity.FilterReadAsync(filesAsync.Where(file => file.RootFolderType != FolderType.TRASH));

        await foreach (var file in files)
        {
            await CheckEntryAsync(folderDao, file);
            yield return file;
        }
    }

    public async IAsyncEnumerable<Folder<string>> GetThirdPartyFoldersAsync<T>(Folder<T> parent, string searchText = null)
    {
        if ((parent.Id.Equals(await globalFolderHelper.FolderMyAsync) || parent.Id.Equals(await globalFolderHelper.FolderCommonAsync))
            && thirdpartyConfiguration.SupportInclusion(daoFactory)
            && (await filesSettingsHelper.GetEnableThirdParty()))
        {
            var providerDao = daoFactory.ProviderDao;
            if (providerDao == null)
            {
                yield break;
            }

            var providers = providerDao.GetProvidersInfoAsync(parent.RootFolderType, searchText);
            var securityDao = daoFactory.GetSecurityDao<string>();

            await foreach (var e in providers)
            {
                var fake = GetFakeThirdpartyFolder(e, parent.Id.ToString());
                if (await fileSecurity.CanReadAsync(fake))
                {
                    var pureShareRecords = securityDao.GetPureShareRecordsAsync(fake);
                    var isShared = await pureShareRecords
                    //.Where(x => x.Owner == SecurityContext.CurrentAccount.ID)
                    .Where(x => fake.Id.Equals(x.EntryId))
                    .AnyAsync();

                    if (isShared)
                    {
                        fake.Shared = true;
                    }

                    yield return fake;
                }
            }
        }
    }

    public async Task<IEnumerable<FileEntry>> GetRecentAsync(FilterType filter, bool subjectGroup, Guid subjectId, string searchText, string[] extension, bool searchInContent)
    {
        var tagDao = daoFactory.GetTagDao<int>();
        var tags = tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.Recent).Where(tag => tag.EntryType == FileEntryType.File).Select(r => r.EntryId);

        var fileIdsInt = Enumerable.Empty<int>();
        var fileIdsString = Enumerable.Empty<string>();
        var listFileIds = new List<string>();

        await foreach (var fileId in tags)
        {
            if (fileId is int @int)
            {
                fileIdsInt = fileIdsInt.Append(@int);
            }
            if (fileId is string @string)
            {
                fileIdsString = fileIdsString.Append(@string);
            }

            listFileIds.Add(fileId.ToString());
        }

        var files = new List<FileEntry>();

        var firstTask = GetRecentByIdsAsync(fileIdsInt, filter, subjectGroup, subjectId, searchText, extension, searchInContent).ToListAsync();
        var secondTask = GetRecentByIdsAsync(fileIdsString, filter, subjectGroup, subjectId, searchText, extension, searchInContent).ToListAsync();

        foreach (var items in await Task.WhenAll(firstTask.AsTask(), secondTask.AsTask()))
        {
            files.AddRange(items);
        }

        var result = files.OrderBy(file =>
        {
            var fileId = "";
            if (file is File<int> fileInt)
            {
                fileId = fileInt.Id.ToString();
            }
            else if (file is File<string> fileString)
            {
                fileId = fileString.Id;
            }

            return listFileIds.IndexOf(fileId);
        });

        return result;
    }

    private async IAsyncEnumerable<FileEntry> GetRecentByIdsAsync<T>(IEnumerable<T> fileIds, FilterType filter, bool subjectGroup, Guid subjectId, string searchText, string[] ext, bool searchInContent)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesFilteredAsync(fileIds, filter, subjectGroup, subjectId, searchText, ext, searchInContent).Where(file => file.RootFolderType != FolderType.TRASH));

        await foreach (var file in files)
        {
            await CheckEntryAsync(folderDao, file);
            yield return file;
        }
    }

    private async Task<(IEnumerable<FileEntry>, IEnumerable<FileEntry>)> GetFavoritesAsync(FilterType filter, bool subjectGroup, Guid subjectId, string searchText, string[] extension, bool searchInContent)
    {
        var tagDao = daoFactory.GetTagDao<int>();
        var tags = tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.Favorite);

        var fileIdsInt = new List<int>();
        var fileIdsString = new List<string>();
        var folderIdsInt = new List<int>();
        var folderIdsString = new List<string>();

        await foreach (var tag in tags)
        {
            if (tag.EntryType == FileEntryType.File)
            {
                if (tag.EntryId is int eId)
                {
                    fileIdsInt.Add(eId);
                }
                else if (tag.EntryId is string esId)
                {
                    fileIdsString.Add(esId);
                }
            }
            else
            {
                if (tag.EntryId is int eId)
                {
                    folderIdsInt.Add(eId);
                }
                else if (tag.EntryId is string esId)
                {
                    folderIdsString.Add(esId);
                }
            }
        }

        var (filesInt, foldersInt) = await GetFavoritesByIdAsync(fileIdsInt, folderIdsInt, filter, subjectGroup, subjectId, searchText, extension, searchInContent);
        var (filesString, foldersString) = await GetFavoritesByIdAsync(fileIdsString, folderIdsString, filter, subjectGroup, subjectId, searchText, extension, searchInContent);

        var files = new List<FileEntry>(filesInt);
        files.AddRange(filesString);

        var folders = new List<FileEntry>(foldersInt);
        files.AddRange(foldersString);

        return (files, folders);
    }

    private async Task<(IEnumerable<FileEntry>, IEnumerable<FileEntry>)> GetFavoritesByIdAsync<T>(IEnumerable<T> fileIds, IEnumerable<T> folderIds, FilterType filter, bool subjectGroup,
            Guid subjectId, string searchText, string[] extension, bool searchInContent)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var asyncFolders = folderDao.GetFoldersAsync(folderIds, filter, subjectGroup, subjectId, searchText, false, false);
        var asyncFiles = fileDao.GetFilesFilteredAsync(fileIds, filter, subjectGroup, subjectId, searchText, extension, searchInContent, true);

        List<FileEntry<T>> files = new();
        List<FileEntry<T>> folders = new();

        if (filter is FilterType.None or FilterType.FoldersOnly)
        {
            var tmpFolders = asyncFolders.Where(folder => folder.RootFolderType != FolderType.TRASH);

            folders = await fileSecurity.FilterReadAsync(tmpFolders).ToListAsync();

            await CheckFolderIdAsync(folderDao, folders);
        }

        if (filter != FilterType.FoldersOnly)
        {
            var tmpFiles = asyncFiles.Where(file => file.RootFolderType != FolderType.TRASH);

            files = await fileSecurity.FilterReadAsync(tmpFiles).ToListAsync();

            await CheckFolderIdAsync(folderDao, folders);
        }

        return (files, folders);
    }

    public IAsyncEnumerable<FileEntry<T>> FilterEntries<T>(IAsyncEnumerable<FileEntry<T>> entries, FilterType filter, bool subjectGroup, Guid subjectId, string searchText, bool searchInContent)
    {
        if (entries == null)
        {
            return entries;
        }

        if (subjectId != Guid.Empty)
        {
            entries = entries.WhereAwait(async f =>
                                    subjectGroup
                                        ? (await userManager.GetUsersByGroupAsync(subjectId)).Any(s => s.Id == f.CreateBy)
                                        : f.CreateBy == subjectId
                );
        }

        Func<FileEntry<T>, bool> where = null;

        switch (filter)
        {
            case FilterType.SpreadsheetsOnly:
            case FilterType.PresentationsOnly:
            case FilterType.ImagesOnly:
            case FilterType.DocumentsOnly:
            case FilterType.OFormOnly:
            case FilterType.OFormTemplateOnly:
            case FilterType.ArchiveOnly:
            case FilterType.FilesOnly:
            case FilterType.MediaOnly:
                where = f => f.FileEntryType == FileEntryType.File && (((File<T>)f).FilterType == filter || filter == FilterType.FilesOnly);
                break;
            case FilterType.FoldersOnly:
                where = f => f.FileEntryType == FileEntryType.Folder;
                break;
            case FilterType.ByExtension:
                var filterExt = (searchText ?? string.Empty).ToLower().Trim();
                where = f => !string.IsNullOrEmpty(filterExt) && f.FileEntryType == FileEntryType.File && FileUtility.GetFileExtension(f.Title).Equals(filterExt);
                break;
        }

        if (where != null)
        {
            entries = entries.Where(where);
        }

        searchText = (searchText ?? string.Empty).ToLower().Trim();

        if ((!searchInContent || filter == FilterType.ByExtension) && !string.IsNullOrEmpty(searchText))
        {
            entries = entries.Where(f => f.Title.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));
        }

        return entries;
    }

    public async Task<IEnumerable<FileEntry>> SortEntries<T>(IEnumerable<FileEntry> entries, OrderBy orderBy, bool pinOnTop = true)
    {
        if (entries == null || !entries.Any())
        {
            return entries;
        }

        orderBy ??= await filesSettingsHelper.GetDefaultOrder();

        var c = orderBy.IsAsc ? 1 : -1;
        Comparison<FileEntry> sorter = orderBy.SortedBy switch
        {
            SortedByType.Type => (x, y) =>
            {
                var cmp = 0;
                if (x.FileEntryType == FileEntryType.File && y.FileEntryType == FileEntryType.File)
                {
                    cmp = c * FileUtility.GetFileExtension(x.Title).CompareTo(FileUtility.GetFileExtension(y.Title));
                }

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.RoomType => (x, y) =>
            {
                var cmp = 0;

                if (x is IFolder x1 && DocSpaceHelper.IsRoom(x1.FolderType)
                    && y is IFolder x2 && DocSpaceHelper.IsRoom(x2.FolderType))
                {
                    cmp = c * Enum.GetName(x1.FolderType).EnumerableComparer(Enum.GetName(x2.FolderType));
                }

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.Tags => (x, y) =>
            {
                var cmp = 0;

                if (x is IFolder x1 && DocSpaceHelper.IsRoom(x1.FolderType)
                    && y is IFolder x2 && DocSpaceHelper.IsRoom(x2.FolderType))
                {
                    cmp = c * x1.Tags.Count().CompareTo(x2.Tags.Count());
                }

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.Author => (x, y) =>
            {
                var cmp = c * string.Compare(x.CreateByString, y.CreateByString);

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.UsedSpace => (x, y) =>
            {
                var cmp = 0;
                if (x is Folder<T> x1 && DocSpaceHelper.IsRoom(x1.FolderType) && !x1.ProviderEntry
                     && y is Folder<T> x2 && DocSpaceHelper.IsRoom(x2.FolderType) && !x2.ProviderEntry)
                {
                    cmp = c * ((Folder<T>)x).Counter.CompareTo(((Folder<T>)y).Counter);
                }

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.Size => (x, y) =>
            {
                var cmp = 0;
                if (x.FileEntryType == FileEntryType.File && y.FileEntryType == FileEntryType.File)
                {
                    cmp = c * ((File<T>)x).ContentLength.CompareTo(((File<T>)y).ContentLength);
                }

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.AZ => (x, y) => c * x.Title.EnumerableComparer(y.Title),
            SortedByType.DateAndTime => (x, y) =>
            {
                var cmp = c * DateTime.Compare(x.ModifiedOn, y.ModifiedOn);

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.DateAndTimeCreation => (x, y) =>
            {
                var cmp = c * DateTime.Compare(x.CreateOn, y.CreateOn);

                return cmp == 0 ? x.Title.EnumerableComparer(y.Title) : cmp;
            }
            ,
            SortedByType.New => (x, y) =>
            {
                var isNewSortResult = x.IsNew.CompareTo(y.IsNew);

                return c * (isNewSortResult == 0 ? DateTime.Compare(x.ModifiedOn, y.ModifiedOn) : isNewSortResult);
            }
            ,
            SortedByType.Room => (x, y) =>
            {
                var x1 = x.OriginRoomTitle;
                var x2 = y.OriginRoomTitle;

                if (x1 == null && x2 == null)
                {
                    return 0;
                }

                if (x1 == null)
                {
                    return c * 1;
                }

                if (x2 == null)
                {
                    return c * -1;
                }

                return c * x1.EnumerableComparer(x2);
            }
            ,
            _ => (x, y) => c * x.Title.EnumerableComparer(y.Title)
        };

        var comparer = Comparer<FileEntry>.Create(sorter);

        if (orderBy.SortedBy != SortedByType.New)
        {
            var rooms = entries.Where(r => r.FileEntryType == FileEntryType.Folder && DocSpaceHelper.IsRoom(((IFolder)r).FolderType));

            if (pinOnTop)
            {
                var pinnedRooms = rooms.Where(r => ((IFolder)r).Pinned);
                var thirdpartyRooms = rooms.Where(r => r.ProviderEntry);

                if (orderBy.SortedBy == SortedByType.UsedSpace)
                {
                    rooms = rooms.Except(thirdpartyRooms).Except(pinnedRooms);
                }
                else
                {
                    rooms = rooms.Except(pinnedRooms);
                }

                var folders = orderBy.SortedBy == SortedByType.UsedSpace ?
                    entries.Where(r => r.FileEntryType == FileEntryType.Folder).Except(pinnedRooms).Except(thirdpartyRooms).Except(rooms) :
                    entries.Where(r => r.FileEntryType == FileEntryType.Folder).Except(pinnedRooms).Except(rooms);
                var files = entries.Where(r => r.FileEntryType == FileEntryType.File);
                pinnedRooms = pinnedRooms.OrderBy(r => r, comparer);
                rooms = rooms.OrderBy(r => r, comparer);
                folders = folders.OrderBy(r => r, comparer);
                files = files.OrderBy(r => r, comparer);

                if (orderBy.SortedBy == SortedByType.UsedSpace)
                {
                    return pinnedRooms.Concat(thirdpartyRooms).Concat(rooms).Concat(folders).Concat(files);
                }
                return pinnedRooms.Concat(rooms).Concat(folders).Concat(files);
            }
            else
            {
                var folders = entries.Where(r => r.FileEntryType == FileEntryType.Folder).Except(rooms);
                var files = entries.Where(r => r.FileEntryType == FileEntryType.File);

                rooms = rooms.OrderBy(r => r, comparer);
                folders = folders.OrderBy(r => r, comparer);
                files = files.OrderBy(r => r, comparer);

                return rooms.Concat(folders).Concat(files);
            }
        }

        return entries.OrderBy(r => r, comparer);
    }

    public Folder<string> GetFakeThirdpartyFolder(IProviderInfo providerInfo, string parentFolderId = null)
    {
        //Fake folder. Don't send request to third party
        var folder = serviceProvider.GetService<Folder<string>>();

        folder.ParentId = parentFolderId;

        folder.Id = providerInfo.RootFolderId;
        folder.CreateBy = providerInfo.Owner;
        folder.CreateOn = providerInfo.CreateOn;
        folder.FolderType = FolderType.DEFAULT;
        folder.ModifiedBy = providerInfo.Owner;
        folder.ModifiedOn = providerInfo.CreateOn;
        folder.ProviderId = providerInfo.ProviderId;
        folder.ProviderKey = providerInfo.ProviderKey;
        folder.RootCreateBy = providerInfo.Owner;
        folder.RootId = providerInfo.RootFolderId;
        folder.RootFolderType = providerInfo.RootFolderType;
        folder.Shareable = false;
        folder.Title = providerInfo.CustomerTitle;
        folder.FilesCount = 0;
        folder.FoldersCount = 0;

        return folder;
    }

    public async Task<List<FileEntry>> GetBreadCrumbsAsync<T>(T folderId)
    {
        return await breadCrumbsManager.GetBreadCrumbsAsync(folderId);
    }

    public async Task<List<FileEntry>> GetBreadCrumbsAsync<T>(T folderId, IFolderDao<T> folderDao)
    {
        return await breadCrumbsManager.GetBreadCrumbsAsync(folderId, folderDao);
    }

    private async Task CheckFolderIdAsync<T>(IFolderDao<T> folderDao, IEnumerable<FileEntry<T>> entries)
    {
        foreach (var entry in entries)
        {
            await CheckEntryAsync(folderDao, entry);
        }
    }

    private async Task CheckEntryAsync<T>(IFolderDao<T> folderDao, FileEntry<T> entry)
    {
        if (entry.RootFolderType == FolderType.USER
            && entry.RootCreateBy != authContext.CurrentAccount.ID)
        {
            var folderId = entry.ParentId;
            var folder = await folderDao.GetFolderAsync(folderId);
            if (!await fileSecurity.CanReadAsync(folder))
            {
                entry.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }
    }

    public async Task<bool> FileLockedForMeAsync<T>(T fileId, Guid userId = default)
    {
        return await lockerManager.FileLockedForMeAsync(fileId, userId);
    }

    public async Task<(File<T> file, Folder<T> folderIfNew)> GetFillFormDraftAsync<T>(File<T> sourceFile)
    {
        if (sourceFile == null)
        {
            return (null, null);
        }

        Folder<T> folderIfNew = null;
        File<T> linkedFile = null;
        var fileDao = daoFactory.GetFileDao<T>();
        var sourceFileDao = daoFactory.GetFileDao<T>();
        var linkDao = daoFactory.GetLinkDao();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(sourceFile.Id + "_draft"))
        {
        var linkedId = await linkDao.GetLinkedAsync(sourceFile.Id.ToString());

        if (linkedId != null)
        {
            linkedFile = await fileDao.GetFileAsync((T)Convert.ChangeType(linkedId, typeof(T)));
            if (linkedFile == null
                || !await fileSecurity.CanFillFormsAsync(linkedFile)
                || await FileLockedForMeAsync(linkedFile.Id)
                || linkedFile.RootFolderType == FolderType.TRASH)
            {
                await linkDao.DeleteLinkAsync(sourceFile.Id.ToString());
                linkedFile = null;
            }
        }

        if (linkedFile == null)
        {
            var folderId = sourceFile.ParentId;
            var folderDao = daoFactory.GetFolderDao<T>();
            folderIfNew = await folderDao.GetFolderAsync(folderId);
            if (folderIfNew == null)
            {
                throw new Exception(FilesCommonResource.ErrorMessage_FolderNotFound);
            }

            if (!await fileSecurity.CanFillFormsAsync(folderIfNew))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }
            string title;
            var ext = FileUtility.GetFileExtension(sourceFile.Title);
            var sourceTitle = Path.GetFileNameWithoutExtension(sourceFile.Title);

            linkedFile = serviceProvider.GetService<File<T>>();

            if (folderIfNew.FolderType == FolderType.FillingFormsRoom)
            {
                T inProcessFormFolderId;
                T readyFormFolderId;

                var inProcessFormFolder = (await folderDao.GetFoldersAsync(folderId, FolderType.InProcessFormFolder).ToListAsync()).FirstOrDefault();
                var readyFormFolder = (await folderDao.GetFoldersAsync(folderId, FolderType.ReadyFormFolder).ToListAsync()).FirstOrDefault();
                if (inProcessFormFolder == null && readyFormFolder == null)
                {
                    (readyFormFolderId, inProcessFormFolderId) = await InitSystemFormFillingFolders(folderId, folderDao);
                    var systemFormFillingFolders = new List<Folder<T>>()
                    {
                        await folderDao.GetFolderAsync(readyFormFolderId),
                        await folderDao.GetFolderAsync(inProcessFormFolderId)
                    };
                    foreach (var formFolder in systemFormFillingFolders)
                    {
                        await socketManager.CreateFolderAsync(formFolder);
                        await filesMessageService.SendAsync(MessageAction.FolderCreated, formFolder, formFolder.Title);
                    }
                }
                else
                {
                    readyFormFolderId = readyFormFolder.Id;
                    inProcessFormFolderId = inProcessFormFolder.Id;
                }
                var properties = await fileDao.GetProperties(sourceFile.Id);
                var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
                title = $"{user.FirstName} {user.LastName} - {sourceFile.Title}";

                if (properties.FormFilling.ResultsFileID == null)
                {
                    var initFormFillingProperties = await InitFormFillingProperties(sourceTitle, sourceFile.Id, inProcessFormFolderId, readyFormFolderId, folderIfNew.CreateBy, properties, fileDao, folderDao);
                    linkedFile.ParentId = (T)Convert.ChangeType(initFormFillingProperties.FormFilling.ToFolderId, typeof(T));
                }
                else
                {
                    linkedFile.ParentId = (T)Convert.ChangeType(properties.FormFilling.ToFolderId, typeof(T));
                }

            }
            else
            {
                title = $"{sourceTitle}-{tenantUtil.DateTimeNow():s}";

                if (sourceFile.ProviderEntry)
                {
                    var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
                    var displayedName = user.DisplayUserName(displayUserSettingsHelper);

                    title += $" ({displayedName})";
                }

                title += ext;
                linkedFile.ParentId = folderIfNew.Id;
            }

            linkedFile.Title = Global.ReplaceInvalidCharsAndTruncate(title);
                linkedFile.SetFileStatus(await sourceFile.GetFileStatus());
            linkedFile.ConvertedType = sourceFile.ConvertedType;
            linkedFile.Comment = FilesCommonResource.CommentCreateFillFormDraft;
            linkedFile.Encrypted = sourceFile.Encrypted;

            await using (var stream = await sourceFileDao.GetFileStreamAsync(sourceFile))
            {
                linkedFile.ContentLength = stream.CanSeek ? stream.Length : sourceFile.ContentLength;
                linkedFile = await fileDao.SaveFileAsync(linkedFile, stream);
            }

            if (folderIfNew.FolderType == FolderType.FillingFormsRoom)
            {
                var prop = await fileDao.GetProperties(sourceFile.Id);
                prop.FormFilling.StartFilling = false;
                await fileDao.SaveProperties(linkedFile.Id, prop);
            }

            await fileMarker.MarkAsNewAsync(linkedFile);

            await socketManager.CreateFileAsync(linkedFile);

            await linkDao.AddLinkAsync(sourceFile.Id.ToString(), linkedFile.Id.ToString());

            await socketManager.UpdateFileAsync(sourceFile);
        }
        }

        return (linkedFile, folderIfNew);
    }

    public async Task<bool> LinkedForMeAsync<T>(File<T> file)
    {
        if (file == null || !fileUtility.CanWebRestrictedEditing(file.Title))
        {
            return false;
        }

        var linkDao = daoFactory.GetLinkDao();
        var sourceId = await linkDao.GetSourceAsync(file.Id.ToString());

        return !string.IsNullOrEmpty(sourceId);
    }

    public async Task<bool> CheckFillFormDraftAsync<T>(File<T> linkedFile)
    {
        if (linkedFile == null)
        {
            return false;
        }

        var linkDao = daoFactory.GetLinkDao();
        var sourceId = await linkDao.GetSourceAsync(linkedFile.Id.ToString());
        if (sourceId == null)
        {
            return false;
        }

        if (int.TryParse(sourceId, out var sId))
        {
            return await CheckAsync(sId);
        }

        return await CheckAsync(sourceId);

        async Task<bool> CheckAsync<T1>(T1 id)
        {
            var fileDao = daoFactory.GetFileDao<T1>();
            var sourceFile = await fileDao.GetFileAsync(id);
            if (sourceFile == null
                || !await fileSecurity.CanFillFormsAsync(sourceFile)
                || sourceFile.Access != FileShare.FillForms)
            {
                await linkDao.DeleteLinkAsync(id.ToString());

                return false;
            }

            return true;
        }
    }

    public async Task<File<T>> SaveEditingAsync<T>(T fileId, string fileExtension, string downloadUri, Stream stream, string doc, string comment = null, bool checkRight = true, bool encrypted = false, ForcesaveType? forcesave = null, bool keepLink = false, string formsDataUrl = null)
    {
        var newExtension = string.IsNullOrEmpty(fileExtension)
                          ? FileUtility.GetFileExtension(downloadUri)
                          : fileExtension;

        if (!string.IsNullOrEmpty(newExtension))
        {
            newExtension = "." + newExtension.Trim('.');
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var check = await fileShareLink.CheckAsync(doc, false, fileDao);
        var editLink = check.EditLink;
        var file = check.File ?? await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (checkRight && !editLink && (!await fileSecurity.CanFillFormsAsync(file) || !await fileSecurity.CanEditAsync(file)))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (checkRight && await FileLockedForMeAsync(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (checkRight && forcesave is null or ForcesaveType.None && fileTracker.IsEditing(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        var currentExt = file.ConvertedExtension;
        if (string.IsNullOrEmpty(newExtension))
        {
            newExtension = FileUtility.GetFileExtension(file.Title);
        }

        var replaceVersion = false;
        if (file.Forcesave != ForcesaveType.None)
        {
            if (file.Forcesave == ForcesaveType.User && filesSettingsHelper.GetStoreForcesave() || encrypted)
            {
                file.Version++;
            }
            else
            {
                replaceVersion = true;
            }
        }
        else
        {
            if (file.Version != 1 || string.IsNullOrEmpty(currentExt))
            {
                file.VersionGroup++;
            }
            else
            {
                var storeTemplate = await globalStore.GetStoreTemplateAsync();

                var path = FileConstant.NewDocPath + CultureInfo.CurrentCulture + "/";
                if (!await storeTemplate.IsDirectoryAsync(path))
                {
                    path = FileConstant.NewDocPath + "en-US/";
                }

                var fileExt = currentExt != fileUtility.MasterFormExtension
                    ? fileUtility.GetInternalExtension(file.Title)
                    : currentExt;

                path += "new" + fileExt;

                //todo: think about the criteria for saving after creation
                if (!await storeTemplate.IsFileAsync(path) || file.ContentLength != await storeTemplate.GetFileSizeAsync("", path))
                {
                    file.VersionGroup++;
                }
            }
            file.Version++;

            if (file.VersionGroup == 1)
            {
                file.VersionGroup++;
            }
        }
        file.Forcesave = forcesave ?? ForcesaveType.None;

        if (string.IsNullOrEmpty(comment))
        {
            comment = FilesCommonResource.CommentEdit;
        }

        file.Encrypted = encrypted;

        file.ConvertedType = FileUtility.GetFileExtension(file.Title) != newExtension ? newExtension : null;
        file.ThumbnailStatus = encrypted ? Thumbnail.NotRequired : Thumbnail.Waiting;

        if (file.ProviderEntry && !newExtension.Equals(currentExt))
        {
            if ((await fileUtility.GetExtsConvertibleAsync()).ContainsKey(newExtension) && (await fileUtility.GetExtsConvertibleAsync())[newExtension].Contains(currentExt))
            {
                if (stream != null)
                {
                    downloadUri = await pathProvider.GetTempUrlAsync(stream, newExtension);
                    downloadUri = await documentServiceConnector.ReplaceCommunityAddressAsync(downloadUri);
                }

                var key = DocumentServiceConnector.GenerateRevisionId(downloadUri);

                var resultTuple = await documentServiceConnector.GetConvertedUriAsync(downloadUri, newExtension, currentExt, key, null, CultureInfo.CurrentUICulture.Name, null, null, false);
                downloadUri = resultTuple.ConvertedDocumentUri;

                stream = null;
            }
            else
            {
                file.Id = default;
                file.Title = FileUtility.ReplaceFileExtension(file.Title, newExtension);
            }

            file.ConvertedType = null;
        }


        using (var tmpStream = new MemoryStream())
        {
            if (stream != null)
            {
                await stream.CopyToAsync(tmpStream);
            }
            else
            {

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(downloadUri)
                };

                var httpClient = clientFactory.CreateClient();
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                await using var editedFileStream = await response.Content.ReadAsStreamAsync();
                await editedFileStream.CopyToAsync(tmpStream);
            }
            tmpStream.Position = 0;
            if (file.Forcesave == ForcesaveType.UserSubmit)
            {
                var folderDao = daoFactory.GetFolderDao<T>();
                var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(file);

                var room = await folderDao.GetFolderAsync((T)Convert.ChangeType(roomId, typeof(T))).NotFoundIfNull();
                if (room.FolderType == FolderType.FillingFormsRoom)
                {
                    var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);

                    if (properties != null)
                    {
                        await formFillingReportCreator.UpdateFormFillingReport((T)Convert.ChangeType(properties.FormFilling.ResultsFileID, typeof(T)), formsDataUrl);

                        var pdfFile = serviceProvider.GetService<File<T>>();
                        pdfFile.Title = file.Title;
                        pdfFile.ParentId = (T)Convert.ChangeType(properties.FormFilling.ResultsFolderId, typeof(T));
                        pdfFile.Comment = string.IsNullOrEmpty(comment) ? null : comment;
                        File<T> result;

                        if (tmpStream.CanSeek)
                        {
                            pdfFile.ContentLength = tmpStream.Length;
                            result = await fileDao.SaveFileAsync(pdfFile, tmpStream);
                        }
                        else
                        {
                            await using var buffered = await tempStream.GetBufferedAsync(tmpStream);
                            pdfFile.ContentLength = buffered.Length;
                            result = await fileDao.SaveFileAsync(pdfFile, buffered);
                        }

                        var linkDao = daoFactory.GetLinkDao();
                        var sourceId = await linkDao.GetSourceAsync(file.Id.ToString());
                        var sourceFile = await fileDao.GetFileAsync((T)Convert.ChangeType(sourceId, typeof(T)));

                        await linkDao.DeleteLinkAsync(sourceId);
                        await socketManager.UpdateFileAsync(sourceFile);

                        await fileMarker.MarkAsNewAsync(result);
                        await socketManager.CreateFileAsync(result);

                        await fileMarker.RemoveMarkAsNewForAllAsync(file);
                        await fileDao.DeleteFileAsync(file.Id);

                        return result;
                    }
                }
            }
            file.ContentLength = tmpStream.Length;
            file.Comment = string.IsNullOrEmpty(comment) ? null : comment;
            if (replaceVersion)
            {
                file = await fileDao.ReplaceFileVersionAsync(file, tmpStream);
            }
            else
            {
                file = await fileDao.SaveFileAsync(file, tmpStream);
            }
            if (!keepLink
               || (!file.ProviderEntry && file.CreateBy != authContext.CurrentAccount.ID)
               || !await LinkedForMeAsync(file))
            {
                var linkDao = daoFactory.GetLinkDao();
                await linkDao.DeleteAllLinkAsync(file.Id.ToString());
            }
        }

        await fileMarker.MarkAsNewAsync(file);
        await fileMarker.RemoveMarkAsNewAsync(file);

        return file;
    }

    public async Task<File<T>> TrackEditingAsync<T>(T fileId, Guid tabId, Guid userId, string doc, int tenantId, bool editingAlone = false)
    {
        bool checkRight;
        if (fileTracker.GetEditingBy(fileId).Contains(userId))
        {
            checkRight = fileTracker.ProlongEditing(fileId, tabId, userId, tenantId, commonLinkUtility.ServerRootPath, editingAlone);
            if (!checkRight)
            {
                return null;
            }
        }

        var file = await daoFactory.GetFileDao<T>().GetFileAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        
        if (!await CanEditAsync(userId, file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }
        
        if (await FileLockedForMeAsync(file.Id, userId))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        checkRight = fileTracker.ProlongEditing(fileId, tabId, userId, tenantId, commonLinkUtility.ServerRootPath, editingAlone);
        if (checkRight)
        {
            fileTracker.ChangeRight(fileId, userId, false);
        }

        return file;
        
        async Task<bool> CanEditAsync(Guid guid, FileEntry<T> entry)
        {
            return await fileSecurity.CanEditAsync(entry, guid)
                   || await fileSecurity.CanCustomFilterEditAsync(entry, guid)
                   || await fileSecurity.CanReviewAsync(entry, guid)
                   || await fileSecurity.CanFillFormsAsync(entry, guid)
                   || await fileSecurity.CanCommentAsync(entry, guid);
    }
    }

    public async Task<File<T>> UpdateToVersionFileAsync<T>(T fileId, int version, bool checkRight = true, bool finalize = false)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        if (version < 1)
        {
            throw new ArgumentNullException(nameof(version));
        }

        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (file.Version != version)
        {
            file = await fileDao.GetFileAsync(file.Id, Math.Min(file.Version, version));
        }
        else
        {
            if (!finalize)
            {
                throw new Exception(FilesCommonResource.ErrorMessage_FileUpdateToVersion);
            }
        }

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (checkRight && !await fileSecurity.CanEditHistoryAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (await FileLockedForMeAsync(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (checkRight && fileTracker.IsEditing(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        if (file.ProviderEntry)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (file.Encrypted)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_NotSupportedFormat);
        }

        var exists = cache.Get<string>(UpdateList + fileId) != null;
        if (exists)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_UpdateEditingFile);
        }

        cache.Insert(UpdateList + fileId, fileId.ToString(), TimeSpan.FromMinutes(2));

        try
        {
            var currFile = await fileDao.GetFileAsync(fileId);
            var newFile = serviceProvider.GetService<File<T>>();

            newFile.Id = file.Id;
            newFile.Version = currFile.Version + 1;
            newFile.VersionGroup = currFile.VersionGroup + 1;
            newFile.Title = FileUtility.ReplaceFileExtension(currFile.Title, FileUtility.GetFileExtension(file.Title));
            newFile.SetFileStatus(await currFile.GetFileStatus());
            newFile.ParentId = currFile.ParentId;
            newFile.CreateBy = currFile.CreateBy;
            newFile.CreateOn = currFile.CreateOn;
            newFile.ModifiedBy = file.ModifiedBy;
            newFile.ModifiedOn = file.ModifiedOn;
            newFile.ConvertedType = file.ConvertedType;
            newFile.Comment = string.Format(FilesCommonResource.CommentRevert, file.ModifiedOnString);
            newFile.Encrypted = file.Encrypted;
            newFile.ThumbnailStatus = file.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;

            await using (var stream = await fileDao.GetFileStreamAsync(file))
            {
                newFile.ContentLength = stream.CanSeek ? stream.Length : file.ContentLength;
                newFile = await fileDao.SaveFileAsync(newFile, stream);
            }

            if (file.ThumbnailStatus == Thumbnail.Created)
            {
                var CopyThumbnailsAsync = async () =>
                {
                    await using var scope = serviceProvider.CreateAsyncScope();
                        var _fileDao = scope.ServiceProvider.GetService<IDaoFactory>().GetFileDao<T>();
                        var _globalStoreLocal = scope.ServiceProvider.GetService<GlobalStore>();

                        foreach (var size in thumbnailSettings.Sizes)
                        {
                            await (await _globalStoreLocal.GetStoreAsync()).CopyAsync(String.Empty,
                                                                    _fileDao.GetUniqThumbnailPath(file, size.Width, size.Height),
                                                                    String.Empty,
                                                                    _fileDao.GetUniqThumbnailPath(newFile, size.Width, size.Height));
                        }

                        await _fileDao.SetThumbnailStatusAsync(newFile, Thumbnail.Created);
                };

                _ = Task.Run(() => CopyThumbnailsAsync().GetAwaiter().GetResult());
            }


            var linkDao = daoFactory.GetLinkDao();
            await linkDao.DeleteAllLinkAsync(newFile.Id.ToString());

            await fileMarker.MarkAsNewAsync(newFile);

            await entryStatusManager.SetFileStatusAsync(newFile);

            newFile.Access = file.Access;

            if (newFile.IsTemplate
                && !fileUtility.ExtsWebTemplate.Contains(FileUtility.GetFileExtension(newFile.Title), StringComparer.CurrentCultureIgnoreCase))
            {
                var tagTemplate = Tag.Template(authContext.CurrentAccount.ID, newFile);
                var tagDao = daoFactory.GetTagDao<T>();

                await tagDao.RemoveTagsAsync(tagTemplate);

                newFile.IsTemplate = false;
            }

            return newFile;
        }
        catch (Exception e)
        {
            logger.ErrorUpdateFile(fileId.ToString(), version, e);

            throw new Exception(e.Message, e);
        }
        finally
        {
            cache.Remove(UpdateList + file.Id);
        }
    }

    public async Task<File<T>> CompleteVersionFileAsync<T>(T fileId, int version, bool continueVersion, bool checkRight = true)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var fileVersion = version > 0
            ? await fileDao.GetFileAsync(fileId, version)
            : await fileDao.GetFileAsync(fileId);
        if (fileVersion == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (checkRight && !await fileSecurity.CanEditHistoryAsync(fileVersion))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (await FileLockedForMeAsync(fileVersion.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (fileVersion.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        if (fileVersion.ProviderEntry)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var lastVersionFile = await fileDao.GetFileAsync(fileVersion.Id);

        if (continueVersion)
        {
            if (lastVersionFile.VersionGroup > 1)
            {
                await fileDao.ContinueVersionAsync(fileVersion.Id, fileVersion.Version);
                lastVersionFile.VersionGroup--;
            }
        }
        else
        {
            if (!fileTracker.IsEditing(lastVersionFile.Id) && fileVersion.Version == lastVersionFile.Version)
            {
                    lastVersionFile = await UpdateToVersionFileAsync(fileVersion.Id, fileVersion.Version, checkRight, true);
                //await fileDao.CompleteVersionAsync(fileVersion.Id, fileVersion.Version);
                //lastVersionFile.VersionGroup++;
            }
        }

        await entryStatusManager.SetFileStatusAsync(lastVersionFile);

        return lastVersionFile;
    }

    public async Task<FileOptions<T>> FileRenameAsync<T>(File<T> file, string title)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanRenameAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_RenameFile);
        }

        if (!await fileSecurity.CanDeleteAsync(file) && await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_RenameFile);
        }

        if (await FileLockedForMeAsync(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (file.ProviderEntry && fileTracker.IsEditing(file.Id))
        {
            throw new Exception(FilesCommonResource.ErrorMessage_UpdateEditingFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        title = Global.ReplaceInvalidCharsAndTruncate(title);

        var ext = FileUtility.GetFileExtension(file.Title);
        if (!string.Equals(ext, FileUtility.GetFileExtension(title), StringComparison.InvariantCultureIgnoreCase))
        {
            title += ext;
        }

        var fileAccess = file.Access;

        var renamed = false;
        if (!string.Equals(file.Title, title))
        {
            var newFileId = await fileDao.FileRenameAsync(file, title);

            file = await fileDao.GetFileAsync(newFileId);
            file.Access = fileAccess;

            await documentServiceHelper.RenameFileAsync(file, fileDao);

            renamed = true;
        }

        await entryStatusManager.SetFileStatusAsync(file);

        return new FileOptions<T>
        {
            File = file,
            Renamed = renamed
        };
    }

    public async Task MarkAsRecentByLink<T>(File<T> file, Guid linkId)
    {
        if (await globalFolderHelper.FolderMyAsync == default)
        {
            return;
        }
        
        var tagDao = daoFactory.GetTagDao<T>();
        var userId = authContext.CurrentAccount.ID;
        var linkIdString = linkId.ToString();

        var tags = await tagDao.GetTagsAsync(userId, TagType.RecentByLink, [file])
            .ToDictionaryAsync(k => k.Name);

        if (tags.Count > 0)
        {
            var toRemove = tags.Values.Where(t => t.Name != linkIdString);

            await tagDao.RemoveTagsAsync(toRemove);
        }

        if (!tags.ContainsKey(linkIdString))
        {
            var tag = Tag.RecentByLink(authContext.CurrentAccount.ID, linkId, file);

            await tagDao.SaveTagsAsync(tag);

            file.FolderIdDisplay = await globalFolderHelper.GetFolderRecentAsync<T>();
            await socketManager.CreateFileAsync(file, [userId]);
        }
    }

    public async Task MarkAsRecent<T>(File<T> file)
    {
        if (file.Encrypted || file.ProviderEntry)
        {
            throw new NotSupportedException();
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var userID = authContext.CurrentAccount.ID;

        var tag = Tag.Recent(userID, file);

        await tagDao.SaveTagsAsync(tag);
    }
    private async Task<(T readyFormFolderId, T inProcessFolderId)> InitSystemFormFillingFolders<T>(T formFillingRoomId, IFolderDao<T> folderDao)
    {
        var readyFormFolder = serviceProvider.GetService<Folder<T>>();
        readyFormFolder.Title = FilesUCResource.ReadyFormFolder;
        readyFormFolder.ParentId = formFillingRoomId;
        readyFormFolder.FolderType = FolderType.ReadyFormFolder;

        var inProcessFolder = serviceProvider.GetService<Folder<T>>();
        inProcessFolder.Title = FilesUCResource.InProcessFormFolder;
        inProcessFolder.ParentId = formFillingRoomId;
        inProcessFolder.FolderType = FolderType.InProcessFormFolder;

        var readyFormFolderTask = folderDao.SaveFolderAsync(readyFormFolder);
        var inProcessFolderTask = folderDao.SaveFolderAsync(inProcessFolder);

        await Task.WhenAll(readyFormFolderTask, inProcessFolderTask);

        return (await readyFormFolderTask, await inProcessFolderTask);
    }
    private async Task<EntryProperties> InitFormFillingProperties<T>(string sourceTitle, T sourceFileId, T inProcessFormFolderId, T readyFormFolderId, Guid createBy, EntryProperties properties, IFileDao<T> fileDao, IFolderDao<T> folderDao)
    {
        var templatesFolder = serviceProvider.GetService<Folder<T>>();
        templatesFolder.Title = sourceTitle;
        templatesFolder.ParentId = inProcessFormFolderId;
        templatesFolder.FolderType = FolderType.FormFillingFolderInProgress;
        templatesFolder.CreateBy = createBy;

        var resultsFolder = serviceProvider.GetService<Folder<T>>();
        resultsFolder.Title = sourceTitle;
        resultsFolder.ParentId = readyFormFolderId;
        resultsFolder.FolderType = FolderType.FormFillingFolderDone;
        resultsFolder.CreateBy = createBy;

        var templatesFolderTask = folderDao.SaveFolderAsync(templatesFolder);
        var resultsFolderTask = folderDao.SaveFolderAsync(resultsFolder);

        await Task.WhenAll(templatesFolderTask, resultsFolderTask);

        var templatesFolderId = await templatesFolderTask;
        var resultsFolderId = await resultsFolderTask;

        properties.FormFilling.Title = sourceTitle;
        properties.FormFilling.ToFolderId = templatesFolderId.ToString();
        properties.FormFilling.ResultsFolderId = resultsFolderId.ToString();
        properties.FormFilling.CollectFillForm = true;

        using (var textStream = new MemoryStream(Encoding.UTF8.GetBytes(""))) {

            var csvFile = serviceProvider.GetService<File<T>>();
            csvFile.ParentId = resultsFolderId;
            csvFile.Title = Global.ReplaceInvalidCharsAndTruncate(sourceTitle + ".csv");
            csvFile.CreateBy = createBy;

            var file = await fileDao.SaveFileAsync(csvFile, textStream);
            properties.FormFilling.ResultsFileID = file.Id.ToString();
        }
        await fileDao.SaveProperties(sourceFileId, properties);

        return properties;
    }
    private async Task SetOriginsAsync(IFolder parent, IEnumerable<FileEntry> entries)
    {
        if (parent.FolderType != FolderType.TRASH || !entries.Any())
        {
            return;
        }

        var folderDao = daoFactory.GetFolderDao<int>();

        var originsData = await folderDao.GetOriginsDataAsync(entries.Cast<FileEntry<int>>().Select(e => e.Id)).ToListAsync();

        foreach (var entry in entries)
        {
            var fileEntry = (FileEntry<int>)entry;
            var data = originsData.Find(data => data.Entries.Contains(new KeyValuePair<string, FileEntryType>(fileEntry.Id.ToString(), fileEntry.FileEntryType)));

            if (data?.OriginRoom != null && DocSpaceHelper.IsRoom(data.OriginRoom.FolderType))
            {
                fileEntry.OriginRoomId = data.OriginRoom.Id;
                fileEntry.OriginRoomTitle = data.OriginRoom.Title;
            }

            if (data?.OriginFolder == null)
            {
                continue;
            }

            fileEntry.OriginId = data.OriginFolder.Id;
            fileEntry.OriginTitle = data.OriginFolder.FolderType == FolderType.USER ? FilesUCResource.MyFiles : data.OriginFolder.Title;
        }
    }
        }
