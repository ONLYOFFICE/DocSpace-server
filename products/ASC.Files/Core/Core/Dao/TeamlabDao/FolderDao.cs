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

namespace ASC.Files.Core.Data;

[Scope(typeof(IFolderDao<int>))]
internal class FolderDao(
        FactoryIndexerFolder factoryIndexer,
        UserManager userManager,
        IDbContextFactory<FilesDbContext> dbContextManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        IDaoFactory daoFactory,
        SelectorFactory selectorFactory,
        CrossDao crossDao,
        IMapper mapper,
        GlobalStore globalStore,
    GlobalFolder globalFolder,
    IDistributedLockProvider distributedLockProvider,
    StorageFactory storageFactory)
    : AbstractDao(dbContextManager,
              userManager,
              tenantManager,
              tenantUtil,
              setupInfo,
              maxTotalSizeStatistic,
              settingsManager,
              authContext,
              serviceProvider, 
              distributedLockProvider), IFolderDao<int>
    {
    private const string My = "my";
    private const string Common = "common";
    private const string Share = "share";
    private const string Recent = "recent";
    private const string Favorites = "favorites";
    private const string Templates = "templates";
    private const string Privacy = "privacy";
    private const string Trash = "trash";
    private const string Projects = "projects";
    private const string VirtualRooms = "virtualrooms";
    private const string RoomTemplates = "roomtemplates";
    private const string Archive = "archive";

    public virtual async Task<Folder<int>> GetFolderAsync(int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFolder = await filesDbContext.DbFolderQueryWithSharedAsync(tenantId, folderId);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }
    public async Task<WatermarkSettings> GetWatermarkSettings(Folder<int> room)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var roomSettings = await filesDbContext.RoomSettingsAsync(tenantId, room.Id);

        return mapper.Map<DbRoomWatermark, WatermarkSettings>(roomSettings.Watermark);
    }
    public async Task<Folder<int>> GetFolderAsync(string title, int parentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFolder = await filesDbContext.DbFolderQueryByTitleAndParentIdAsync(tenantId, title, parentId);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async Task<Folder<int>> GetRootFolderAsync(int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var id = await filesDbContext.ParentIdAsync(folderId);

        var dbFolder = await filesDbContext.DbFolderQueryAsync(tenantId, id);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async Task<Folder<int>> GetRootFolderByFileAsync(int fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var id = await filesDbContext.ParentIdByFileIdAsync(tenantId, fileId);

        var dbFolder = await filesDbContext.DbFolderQueryAsync(tenantId, id);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async IAsyncEnumerable<Folder<int>> GetFoldersAsync(int parentId, FolderType type)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = GetFolderQuery(filesDbContext, r => r.ParentId == parentId);

        q = q.Where(f => f.FolderType == type);

        await foreach (var e in FromQuery(filesDbContext, q).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }

    }
    public IAsyncEnumerable<Folder<int>> GetFoldersAsync(int parentId)
    {
        return GetFoldersAsync(parentId, null, FilterType.None, false, Guid.Empty, string.Empty);
    }

    public async IAsyncEnumerable<Folder<int>> GetRoomsAsync(
        IEnumerable<int> parentsIds, 
        IEnumerable<FilterType> filterTypes, 
        IEnumerable<string> tags, 
        Guid subjectId, 
        string searchText, 
        bool withSubfolders, 
        bool withoutTags,
        bool excludeSubject, 
        ProviderFilter provider,
        SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntriesIds,
        QuotaFilter quotaFilter = QuotaFilter.All)
    {
        if (CheckInvalidFilters(filterTypes) || (provider != ProviderFilter.None && provider != ProviderFilter.Storage))
        {
            yield break;
        }

        var filter = DocSpaceHelper.MapToFolderTypes(filterTypes);

        var searchByTags = tags != null && tags.Any() && !withoutTags;
        var searchByTypes = filterTypes != null && filterTypes.Any() && !filterTypes.Contains(FilterType.FoldersOnly) && !filterTypes.Contains(FilterType.None);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = GetFolderQuery(filesDbContext, r => parentsIds.Contains(r.ParentId));

        q = !withSubfolders ? 
            BuildRoomsQuery(filesDbContext, q, filter, tags, subjectId, searchByTags, withoutTags, searchByTypes, false, excludeSubject, subjectFilter, subjectEntriesIds, quotaFilter) : 
            BuildRoomsWithSubfoldersQuery(filesDbContext, parentsIds, filter, tags, searchByTags, searchByTypes, withoutTags, excludeSubject, subjectId, subjectFilter, subjectEntriesIds);

        if (!string.IsNullOrEmpty(searchText))
        {
            var (success, searchIds) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(searchText));
            q = success ? q.Where(r => searchIds.Contains(r.Id)) : BuildSearch(q, searchText, SearchType.Any);
        }

        await foreach (var e in (FromQueryWithShared(filesDbContext, q)).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public async IAsyncEnumerable<Folder<int>> GetRoomsAsync(
        IEnumerable<int> roomsIds, 
        IEnumerable<FilterType> filterTypes, 
        IEnumerable<string> tags, 
        Guid subjectId, 
        string searchText, 
        bool withSubfolders, 
        bool withoutTags, 
        bool excludeSubject, 
        ProviderFilter provider, 
        SubjectFilter subjectFilter, 
        IEnumerable<string> subjectEntriesIds, 
        IEnumerable<int> parentsIds)
    {
        if (CheckInvalidFilters(filterTypes) || provider != ProviderFilter.None)
        {
            yield break;
        }

        var filter = DocSpaceHelper.MapToFolderTypes(filterTypes);

        var searchByTags = tags != null && tags.Any() && !withoutTags;
        var searchByTypes = filterTypes != null && filterTypes.Any() && !filterTypes.Contains(FilterType.FoldersOnly) && !filterTypes.Contains(FilterType.None);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = GetFolderQuery(filesDbContext, f => roomsIds.Contains(f.Id) || (f.CreateBy == _authContext.CurrentAccount.ID && parentsIds != null && parentsIds.Contains(f.ParentId)));

        q = !withSubfolders ? 
            BuildRoomsQuery(filesDbContext, q, filter, tags, subjectId, searchByTags, withoutTags, searchByTypes, false, excludeSubject, subjectFilter, subjectEntriesIds) : 
            BuildRoomsWithSubfoldersQuery(filesDbContext, roomsIds, filter, tags, searchByTags, searchByTypes, withoutTags, excludeSubject, subjectId, subjectFilter, subjectEntriesIds);

        if (!string.IsNullOrEmpty(searchText))
        {
            var (success, searchIds) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(searchText));

            q = success ? q.Where(r => searchIds.Contains(r.Id)) : BuildSearch(q, searchText, SearchType.Any);
        }

        await foreach (var e in (FromQueryWithShared(filesDbContext, q)).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public async Task<int> GetFoldersCountAsync(int parentId, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText,
        bool withSubfolders = false, bool excludeSubject = false, int roomId = 0, FolderType parentType = FolderType.DEFAULT, AdditionalFilterOption additionalFilterOption = AdditionalFilterOption.All)
    {
        if (CheckInvalidFilter(filterType))
        {
            return 0;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (filterType == FilterType.None && subjectId == Guid.Empty && string.IsNullOrEmpty(searchText) && !withSubfolders && !excludeSubject && roomId == 0)
        {
            return await filesDbContext.Tree.CountAsync(r => r.ParentId == parentId && r.Level == 1);
        }

        var q = await GetFoldersQueryWithFilters(parentId, null, filterType, subjectGroup, subjectId, searchText, withSubfolders, excludeSubject, roomId, filesDbContext);

        if (additionalFilterOption != AdditionalFilterOption.All)
        {
            q = ApplyAdditionalFolderFilters(q, filesDbContext, parentId, parentType, additionalFilterOption);
        }

        return await q.CountAsync();
    }

    public async IAsyncEnumerable<Folder<int>> GetFoldersAsync(int parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, bool withSubfolders = false,
        bool excludeSubject = false, int offset = 0, int count = -1, int roomId = 0, bool containingMyFiles = false, FolderType parentType = FolderType.DEFAULT, bool containingForms = false)
    {
        if (CheckInvalidFilter(filterType) || count == 0)
        {
            yield break;
        }

        var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = await GetFoldersQueryWithFilters(parentId, orderBy, filterType, subjectGroup, subjectID, searchText, withSubfolders, excludeSubject, roomId, filesDbContext);
        if (containingMyFiles)
        {
            q = ApplyAdditionalFolderFilters(q, filesDbContext, parentId, parentType, AdditionalFilterOption.MyFilesAndFolders);
        }
        if (containingForms && parentType is FolderType.VirtualDataRoom)
        {
            q = ApplyAdditionalFolderFilters(q, filesDbContext, parentId, parentType, AdditionalFilterOption.FormsWithFillingRole);
        }
        q = q.Skip(offset);

        if (count > 0)
        {
            q = q.Take(count);
        }

        await foreach (var e in FromQuery(filesDbContext, q).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }
    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        var fileRootFolders = new List<FolderType> { FolderType.USER, FolderType.Archive, FolderType.TRASH, FolderType.VirtualRooms, FolderType.RoomTemplates };
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();
        var result = new FilesStatisticsResultDto();
        await foreach (var rootFolder in filesDbContext.FolderTypeUsedSpaceAsync(tenantId, fileRootFolders))
        {
            switch (rootFolder.FolderType)
            {
                case FolderType.USER:
                    result.MyDocumentsUsedSpace = new FilesStatisticsFolder
                    {
                        Title = FilesUCResource.MyFiles,
                        UsedSpace = rootFolder.UsedSpace
                    };
                    break;
                case FolderType.Archive:
                    result.ArchiveUsedSpace = new FilesStatisticsFolder
                    {
                        Title = FilesUCResource.Archive,
                        UsedSpace = rootFolder.UsedSpace
                    };
                    break;
                case FolderType.TRASH:
                    result.TrashUsedSpace = new FilesStatisticsFolder
                    {
                        Title = FilesUCResource.Trash,
                        UsedSpace = rootFolder.UsedSpace
                    };
                    break;
                case FolderType.VirtualRooms:
                    result.RoomsUsedSpace = new FilesStatisticsFolder
                    {
                        Title = FilesUCResource.VirtualRooms,
                        UsedSpace = rootFolder.UsedSpace
                    };
                    break;
            }
        }
        return result;
    }
    public virtual async Task<bool> ContainsFormsInFolder(Folder<int> folder)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await filesDbContext.ContainsFormsInFolder(tenantId, folder.Id);
    }
    public async IAsyncEnumerable<Folder<int>> GetFoldersAsync(IEnumerable<int> folderIds, FilterType filterType = FilterType.None, bool subjectGroup = false, Guid? subjectID = null, string searchText = "", bool searchSubfolders = false, bool checkShare = true, bool excludeSubject = false)
    {
        if (CheckInvalidFilter(filterType))
        {
            yield break;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = GetFolderQuery(filesDbContext, r => folderIds.Contains(r.Id));

        if (searchSubfolders)
        {
            q =  GetFolderQuery(filesDbContext)
                .Join(filesDbContext.Tree, r => r.Id, a => a.FolderId, (folder, tree) => new { folder, tree })
                .Where(r => folderIds.Contains(r.tree.ParentId))
                .Select(r => r.folder);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            var (success, searchIds) = await factoryIndexer.TrySelectIdsAsync(s =>
                                                searchSubfolders
                                                    ? s.MatchAll(searchText)
                                                    : s.MatchAll(searchText).In(r => r.Id, folderIds.ToArray()));
            q = success ? q.Where(r => searchIds.Contains(r.Id)) : BuildSearch(q, searchText, SearchType.Any);
        }


        if (subjectID.HasValue && subjectID != Guid.Empty)
        {
            if (subjectGroup)
            {
                var users = (await _userManager.GetUsersByGroupAsync(subjectID.Value)).Select(u => u.Id).ToArray();
                q = q.Where(r => users.Contains(r.CreateBy));
            }
            else
            {
                q = excludeSubject ? q.Where(r => r.CreateBy != subjectID) : q.Where(r => r.CreateBy == subjectID);
            }
        }

        await foreach (var e in FromQuery(filesDbContext, q).Distinct().AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public virtual async IAsyncEnumerable<Folder<int>> GetParentFoldersAsync(int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = filesDbContext.DbFolderQueriesAsync(tenantId, folderId);

        await foreach (var e in query)
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public Task<int> SaveFolderAsync(Folder<int> folder)
    {
        return SaveFolderAsync(folder, null, null);
    }

    private async Task<int> SaveFolderAsync(Folder<int> folder, IDbContextTransaction transaction, FilesDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var folderId = folder.Id;

        if (transaction == null)
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await filesDbContext.Database.BeginTransactionAsync();

                folderId = await InternalSaveFolderToDbAsync(filesDbContext, folder);

                await tx.CommitAsync();
            });
        }
        else
        {
            folderId = await InternalSaveFolderToDbAsync(dbContext, folder);
        }

        //FactoryIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, folder));
        return folderId;
    }

    private async Task<int> InternalSaveFolderToDbAsync(FilesDbContext filesDbContext, Folder<int> folder)
    {
        folder.Title = Global.ReplaceInvalidCharsAndTruncate(folder.Title);

        folder.ModifiedOn = _tenantUtil.DateTimeNow();
        folder.ModifiedBy = _authContext.CurrentAccount.ID;

        if (folder.CreateOn == default)
        {
            folder.CreateOn = _tenantUtil.DateTimeNow();
        }
        if (folder.CreateBy == Guid.Empty)
        {
            folder.CreateBy = _authContext.CurrentAccount.ID;
        }

        var isNew = false;

        var tenantId = _tenantManager.GetCurrentTenantId();
        var toUpdate = folder.Id != 0 ? await filesDbContext.FolderForUpdateAsync(tenantId, folder.Id) : null;

        if (toUpdate != null)
        {
            toUpdate.Title = folder.Title;
            toUpdate.CreateBy = folder.CreateBy;
            toUpdate.ModifiedOn = _tenantUtil.DateTimeToUtc(folder.ModifiedOn);
            toUpdate.ModifiedBy = folder.ModifiedBy;

            if (DocSpaceHelper.IsRoom(toUpdate.FolderType))
            {
                toUpdate.Settings = new DbRoomSettings
                {
                    RoomId = toUpdate.Id,
                    TenantId = tenantId,
                    Private = folder.SettingsPrivate,
                    HasLogo = folder.SettingsHasLogo,
                    Color = folder.SettingsColor,
                    Cover = folder.SettingsCover,
                    Indexing = folder.SettingsIndexing,
                    DenyDownload = folder.SettingsDenyDownload,
                    Watermark = mapper.Map<WatermarkSettings, DbRoomWatermark>(folder.SettingsWatermark),
                    Quota = folder.SettingsQuota,
                    Lifetime = mapper.Map<RoomDataLifetime, DbRoomDataLifetime>(folder.SettingsLifetime)
                };
            }

            filesDbContext.Update(toUpdate);

            await filesDbContext.SaveChangesAsync();

            if (folder.FolderType is FolderType.DEFAULT or FolderType.BUNCH)
            {
                _ = factoryIndexer.IndexAsync(toUpdate);
            }
        }
        else
        {
            isNew = true;
            var newFolder = new DbFolder
            {
                Id = 0,
                ParentId = folder.ParentId,
                Title = folder.Title,
                CreateOn = _tenantUtil.DateTimeToUtc(folder.CreateOn),
                CreateBy = folder.CreateBy,
                ModifiedOn = _tenantUtil.DateTimeToUtc(folder.ModifiedOn),
                ModifiedBy = folder.ModifiedBy,
                FolderType = folder.FolderType,
                TenantId = tenantId
            };
            
            if (DocSpaceHelper.IsRoom(newFolder.FolderType))
            {
                newFolder.Settings = new DbRoomSettings
                {
                    RoomId = newFolder.Id,
                    TenantId = tenantId,
                    Private = folder.SettingsPrivate,
                    HasLogo = folder.SettingsHasLogo,
                    Color = folder.SettingsColor,
                    Cover = folder.SettingsCover,
                    Indexing = folder.SettingsIndexing,
                    DenyDownload = folder.SettingsDenyDownload,
                    Watermark = mapper.Map<WatermarkSettings, DbRoomWatermark>(folder.SettingsWatermark),
                    Quota = folder.SettingsQuota,
                    Lifetime = mapper.Map<RoomDataLifetime, DbRoomDataLifetime>(folder.SettingsLifetime)
                };
            }
            
            var entityEntry = await filesDbContext.Folders.AddAsync(newFolder);
            newFolder = entityEntry.Entity;
            await filesDbContext.SaveChangesAsync();

            if (folder.FolderType is FolderType.DEFAULT or FolderType.BUNCH)
            {
                _ = factoryIndexer.IndexAsync(newFolder);
            }

            folder.Id = newFolder.Id;

            //itself link
            List<DbFolderTree> treeToAdd =
            [
                new() { FolderId = folder.Id, ParentId = folder.Id, Level = 0 }
            ];

            //full path to root
            treeToAdd.AddRange(await filesDbContext.FolderTreeAsync(folder.Id, folder.ParentId).ToListAsync());
            await filesDbContext.AddRangeAsync(treeToAdd);
            await filesDbContext.SaveChangesAsync();
        }

        if (isNew)
        {
            await IncrementCountAsync(filesDbContext, folder.ParentId, tenantId, FileEntryType.Folder);
            await SetCustomOrder(filesDbContext, folder.Id, folder.ParentId);
        }

        return folder.Id;

    }

    public async Task<int> SetWatermarkSettings(WatermarkSettings watermarkSettings, Folder<int> room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var roomSettings = await filesDbContext.RoomSettingsAsync(tenantId, room.Id);

        if (roomSettings != null)
        {
            roomSettings.Watermark = mapper.Map<WatermarkSettings, DbRoomWatermark>(watermarkSettings);
            filesDbContext.Update(roomSettings);
        await filesDbContext.SaveChangesAsync();
        }

        return room.Id;
    }

    public async Task<Folder<int>> DeleteWatermarkSettings(Folder<int> room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var roomSettings = await filesDbContext.RoomSettingsAsync(tenantId, room.Id);

        if (roomSettings != null)
        {
        roomSettings.Watermark = null;
        filesDbContext.Update(roomSettings);
        await filesDbContext.SaveChangesAsync();
        }

        return room;
    }

    public async Task<Folder<int>> DeleteLifetimeSettings(Folder<int> room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var roomSettings = await filesDbContext.RoomSettingsAsync(tenantId, room.Id);

        if (roomSettings?.Lifetime != null)
        {
            roomSettings.Lifetime = null;
            filesDbContext.Update(roomSettings);
            await filesDbContext.SaveChangesAsync();
        }

        return room;
    }

    public async Task DeleteFolderAsync(int folderId)
    {
        if (folderId == 0)
        {
            throw new ArgumentNullException(nameof(folderId));
        }

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();
            var subfolders = await filesDbContext.SubfolderIdsAsync(folderId).ToListAsync();

            if (!subfolders.Contains(folderId))
            {
                subfolders.Add(folderId); // chashed folder_tree
            }

            var parent = await filesDbContext.ParentIdByIdAsync(tenantId, folderId);

            var folderToDelete = await filesDbContext.DbFoldersForDeleteAsync(tenantId, subfolders).ToListAsync();

            foreach (var f in folderToDelete)
            {
                await factoryIndexer.DeleteAsync(f);
            }
            
            context.Folders.RemoveRange(folderToDelete);

            await filesDbContext.DeleteOrderAsync(subfolders);
            
            var subfoldersStrings = subfolders.Select(r => r.ToString()).ToList();

            await filesDbContext.DeleteTagLinksAsync(tenantId, subfoldersStrings);

            await filesDbContext.DeleteTagsAsync(tenantId);

            await filesDbContext.DeleteTagLinkByTagOriginAsync(tenantId, folderId.ToString(), subfoldersStrings);

            await filesDbContext.DeleteTagOriginAsync(tenantId, folderId.ToString(), subfoldersStrings);

            await filesDbContext.DeleteFilesSecurityAsync(tenantId, subfoldersStrings);

            await filesDbContext.DeleteBunchObjectsAsync(tenantId, folderId.ToString());

            await DeleteCustomOrder(filesDbContext, folderId);

            await filesDbContext.DeleteAuditReferencesAsync(folderId, FileEntryType.Folder);

            await context.SaveChangesAsync();
            await tx.CommitAsync();
            await RecalculateFoldersCountAsync(parent, tenantId);
        });

        //FactoryIndexer.DeleteAsync(new FoldersWrapper { Id = id });
    }

    public async Task<TTo> MoveFolderAsync<TTo>(int folderId, TTo toFolderId, CancellationToken? cancellationToken)
    {
        if (toFolderId is int tId)
        {
            return IdConverter.Convert<TTo>(await MoveFolderAsync(folderId, tId, cancellationToken));
        }

        if (toFolderId is string tsId)
        {
            return IdConverter.Convert<TTo>(await MoveFolderAsync(folderId, tsId, cancellationToken));
        }

        throw new NotImplementedException();
    }

    public async Task<int> MoveFolderAsync(int folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var currentAccount = _authContext.CurrentAccount.ID;
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();
        var trashIdTask = globalFolder.GetFolderTrashAsync(daoFactory);
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();
            var folder = await GetFolderAsync(folderId);
            var oldParentId = folder.ParentId;

            if (folder.FolderType is not (FolderType.DEFAULT or FolderType.FormFillingFolderInProgress or FolderType.FormFillingFolderDone) &&
                !DocSpaceHelper.IsRoom(folder.FolderType))
            {
                throw new ArgumentException("It is forbidden to move the System folder.", nameof(folderId));
            }

            await filesDbContext.UpdateFoldersAsync(tenantId, folderId, toFolderId, currentAccount);
            var subfolders = await filesDbContext.SubfolderAsync(folderId).ToDictionaryAsync(r => r.FolderId, r => r.Level);

            await filesDbContext.DeleteTreesBySubfoldersDictionaryAsync(subfolders.Select(r => r.Key));
            var toInsert = await filesDbContext.TreesOrderByLevel(toFolderId).ToListAsync();

            foreach (var subfolder in subfolders)
            {
                foreach (var f in toInsert)
                {
                    var newTree = new DbFolderTree { FolderId = subfolder.Key, ParentId = f.ParentId, Level = subfolder.Value + 1 + f.Level };
                    await context.AddOrUpdateAsync(r => r.Tree, newTree);
                }
            }

            var trashId = await trashIdTask;
            var tagDao = daoFactory.GetTagDao<int>();
            var toFolder = await GetFolderAsync(toFolderId);
            var (roomId, _) = await GetParentRoomInfoFromFileEntryAsync(folder);
            var (toFolderRoomId, _) = await GetParentRoomInfoFromFileEntryAsync(toFolder);
            if (toFolderId == trashId)
            {
                var tagList = new List<Tag>();

                if (roomId != -1)
                {
                    tagList.Add(Tag.FromRoom(folder.Id, FileEntryType.Folder, currentAccount));
                }

                var origin = Tag.Origin(folderId, FileEntryType.Folder, oldParentId, currentAccount);
                tagList.Add(origin);
                await tagDao.SaveTagsAsync(tagList);
            }
            else if (oldParentId == trashId || roomId != -1 || toFolderRoomId != -1)
            {
                var archiveId = await GetFolderIDArchive(false);
                var fromRoomTag = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, TagType.FromRoom).FirstOrDefaultAsync();
                if (folder.ParentId != archiveId && toFolder.Id != archiveId &&
                    toFolderRoomId == -1 &&
                    ((oldParentId == trashId && fromRoomTag != null) || roomId != -1))
                {
                    await storageFactory.QuotaUsedAddAsync(
                        _tenantManager.GetCurrentTenantId(),
                        FileConstant.ModuleId, "",
                        WebItemManager.DocumentsProductID.ToString(),
                        folder.Counter, toFolder.RootCreateBy);
                }

                if (folder.ParentId != archiveId && toFolder.Id != archiveId &&
                    toFolderRoomId != -1 &&
                    ((oldParentId == trashId && fromRoomTag == null) || (oldParentId != trashId && roomId == -1)))
                {
                    await storageFactory.QuotaUsedDeleteAsync(
                        _tenantManager.GetCurrentTenantId(),
                        FileConstant.ModuleId, "",
                        WebItemManager.DocumentsProductID.ToString(),
                        folder.Counter, toFolder.RootCreateBy);
                }

                if (oldParentId == trashId)
                {
                    await tagDao.RemoveTagLinksAsync(folderId, FileEntryType.Folder, TagType.Origin);
                    await tagDao.RemoveTagLinksAsync(folderId, FileEntryType.Folder, TagType.FromRoom);
                }
            }

            if (!trashId.Equals(toFolderId))
            {
                await SetCustomOrder(context, folderId, toFolderId);
            }
            else
            {
                await DeleteCustomOrder(context, folderId);
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();
            await ChangeTreeFolderSizeAsync(toFolderId, folder.Counter);
            await ChangeTreeFolderSizeAsync(folder.ParentId, -1 * folder.Counter);
            var recalculateFolders = new HashSet<int> { toFolderId, folderId, folder.ParentId };
            await filesDbContext.UpdateFoldersCountsAsync(tenantId, recalculateFolders);

            await foreach (var f in filesDbContext.FoldersAsync(tenantId, recalculateFolders))
            {
                f.FilesCount = await filesDbContext.FilesCountAsync(f.TenantId, f.Id);
            }

            await filesDbContext.SaveChangesAsync();
        });

        return folderId;
    }

    public async Task<string> MoveFolderAsync(int folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var toSelector = selectorFactory.GetSelector(toFolderId);

        var moved = await crossDao.PerformCrossDaoFolderCopyAsync(
            folderId, this, daoFactory.GetFileDao<int>(), r => r,
            toFolderId, toSelector.GetFolderDao(toFolderId), toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
            true, cancellationToken)
            ;

        return moved.Id;
    }

    public async Task<Folder<TTo>> CopyFolderAsync<TTo>(int folderId, TTo toFolderId, CancellationToken? cancellationToken)
    {
        if (toFolderId is int tId)
        {
            return await CopyFolderAsync(folderId, tId, cancellationToken) as Folder<TTo>;
        }

        if (toFolderId is string tsId)
        {
            return await CopyFolderAsync(folderId, tsId, cancellationToken) as Folder<TTo>;
        }

        throw new NotImplementedException();
    }

    public async Task<Folder<int>> CopyFolderAsync(int folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        var folder = await GetFolderAsync(folderId);

        var toFolder = await GetFolderAsync(toFolderId);

        if (folder.FolderType == FolderType.BUNCH)
        {
            folder.FolderType = FolderType.DEFAULT;
        }

        var copy = _serviceProvider.GetService<Folder<int>>();
        copy.ParentId = toFolderId;
        copy.RootId = toFolder.RootId;
        copy.RootCreateBy = toFolder.RootCreateBy;
        copy.RootFolderType = toFolder.RootFolderType;
        copy.Title = folder.Title;
        copy.FolderType = folder.FolderType is 
            FolderType.ReadyFormFolder or 
            FolderType.InProcessFormFolder or 
            FolderType.FormFillingFolderDone or 
            FolderType.FormFillingFolderInProgress ? 
            FolderType.DEFAULT : folder.FolderType;
        copy.SettingsColor = folder.SettingsColor;
        copy.SettingsIndexing = folder.SettingsIndexing;
        copy.SettingsLifetime = folder.SettingsLifetime;
        copy.SettingsQuota = folder.SettingsQuota;
        copy.SettingsWatermark = folder.SettingsWatermark;
        copy.SettingsDenyDownload = folder.SettingsDenyDownload;
        copy.SettingsHasLogo = folder.SettingsHasLogo;
        copy = await GetFolderAsync(await SaveFolderAsync(copy));
        var tagDao = daoFactory.GetTagDao<int>();
        var tags = await tagDao.GetTagsAsync(folder.Id, FileEntryType.Folder, TagType.Custom).ToListAsync();
        foreach (var t in tags)
        {
            t.EntryId = copy.Id;
        }
        await tagDao.SaveTagsAsync(tags);

        //FactoryIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, copy));
        return copy;
    }

    public async Task<Folder<string>> CopyFolderAsync(int folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var toSelector = selectorFactory.GetSelector(toFolderId);

        var moved = await crossDao.PerformCrossDaoFolderCopyAsync(
            folderId, this, daoFactory.GetFileDao<int>(), r => r,
            toFolderId, toSelector.GetFolderDao(toFolderId), toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
            false, cancellationToken)
            ;

        return moved;
    }

    public Task<IDictionary<int, string>> CanMoveOrCopyAsync<TTo>(IEnumerable<int> folderIds, TTo to)
    {
        return to switch
        {
            int tId => CanMoveOrCopyAsync(folderIds, tId),
            string tsId => CanMoveOrCopyAsync(folderIds, tsId),
            _ => throw new NotImplementedException()
        };
        }

    public Task<IDictionary<int, string>> CanMoveOrCopyAsync(IEnumerable<int> folderIds, string to)
        {
        return Task.FromResult<IDictionary<int, string>>(new Dictionary<int, string>());
        }

    public async Task<IDictionary<int, string>> CanMoveOrCopyAsync(IEnumerable<int> folderIds, int to)
    {
        var result = new Dictionary<int, string>();
        if (!folderIds.Any())
        {
            return result;
        }
        
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        foreach (var folderId in folderIds)
        {
            var exists = await filesDbContext.AnyTreeAsync(folderId, to);

            if (exists)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderCopyError);
            }

            var conflict = await filesDbContext.FolderIdAsync(tenantId, folderId, to);

            if (conflict != 0)
            {
                result[folderId] = "";
                }
                }

        return result;
    }
    public async Task<int> ChangeTreeFolderSizeAsync(int folderId, long size)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        await filesDbContext.UpdateTreeFolderCounterAsync(tenantId, folderId, size);
        return folderId;
    }
    public async Task<int> ChangeFolderQuotaAsync(Folder<int> folder, long quota)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var toUpdate = await filesDbContext.FolderForUpdateAsync(tenantId, folder.Id);

        if (DocSpaceHelper.IsRoom(toUpdate.FolderType))
        {
            toUpdate.Settings = new DbRoomSettings
            {
                RoomId = toUpdate.Id,
                TenantId = tenantId,
                Private = folder.SettingsPrivate,
                HasLogo = folder.SettingsHasLogo,
                Color = folder.SettingsColor,
                Cover = folder.SettingsCover,
                Indexing = folder.SettingsIndexing,
                DenyDownload = folder.SettingsDenyDownload,
                Quota = quota >= TenantEntityQuotaSettings.NoQuota ? quota : TenantEntityQuotaSettings.DefaultQuotaValue,
                Lifetime = mapper.Map<RoomDataLifetime, DbRoomDataLifetime>(folder.SettingsLifetime)
            };
        }
        
        filesDbContext.Update(toUpdate);
        await filesDbContext.SaveChangesAsync();

        _ = factoryIndexer.IndexAsync(toUpdate);

        return folder.Id;
    }

    public async Task<int> UpdateFolderAsync(Folder<int> folder, string newTitle, long newQuota, bool indexing, bool denyDownload, RoomDataLifetime lifeTime, WatermarkSettings watermark, string color, string cover)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var toUpdate = await filesDbContext.FolderWithSettingsAsync(tenantId, folder.Id);

        if (DocSpaceHelper.IsRoom(folder.FolderType))
        {
            toUpdate.Settings.Quota = newQuota >= TenantEntityQuotaSettings.NoQuota ? newQuota : TenantEntityQuotaSettings.DefaultQuotaValue;
        }
        
        toUpdate.Title = Global.ReplaceInvalidCharsAndTruncate(newTitle);
        toUpdate.ModifiedOn = DateTime.UtcNow;
        toUpdate.ModifiedBy = _authContext.CurrentAccount.ID;
        toUpdate.Settings.Indexing = indexing;
        toUpdate.Settings.DenyDownload = denyDownload;
        
        if (lifeTime != null)
        {
            if (lifeTime.Enabled.HasValue && !lifeTime.Enabled.Value)
            {
                toUpdate.Settings.Lifetime = null;
            }
            else
            {
                toUpdate.Settings.Lifetime = mapper.Map<RoomDataLifetime, DbRoomDataLifetime>(lifeTime);
            }
        }

        toUpdate.Settings.Watermark = mapper.Map<WatermarkSettings, DbRoomWatermark>(watermark);

        if (color != null)
        {
            if (RoomLogoManager.ColorChanged(color, folder))
            {
                toUpdate.Settings.Color = color;
            }
        }

        if (cover != null)
        {
            if (await RoomLogoManager.CoverChanged(cover, folder))
            {
                toUpdate.Settings.Cover = cover;
            }
        }

        filesDbContext.Update(toUpdate);

        await filesDbContext.SaveChangesAsync();

        _ = factoryIndexer.IndexAsync(toUpdate);

        return folder.Id;
    }
    public async Task<int> RenameFolderAsync(Folder<int> folder, string newTitle)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var toUpdate = await filesDbContext.FolderAsync(tenantId, folder.Id);

        toUpdate.Title = Global.ReplaceInvalidCharsAndTruncate(newTitle);
        toUpdate.ModifiedOn = DateTime.UtcNow;
        toUpdate.ModifiedBy = _authContext.CurrentAccount.ID;
        filesDbContext.Update(toUpdate);

        await filesDbContext.SaveChangesAsync();

        _ = factoryIndexer.IndexAsync(toUpdate);

        return folder.Id;
    }

    public async Task<int> ChangeFolderTypeAsync(Folder<int> folder, FolderType folderType)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var toUpdate = await filesDbContext.FolderAsync(tenantId, folder.Id);

        toUpdate.FolderType = folderType;
        toUpdate.ModifiedOn = DateTime.UtcNow;
        toUpdate.ModifiedBy = _authContext.CurrentAccount.ID;
        filesDbContext.Update(toUpdate);

        await filesDbContext.SaveChangesAsync();

        _ = factoryIndexer.IndexAsync(toUpdate);

        return folder.Id;
    }

    public async Task<int> GetItemsCountAsync(int folderId)
    {
        return await GetFoldersCountAsync(folderId) +
               await GetFilesCountAsync(folderId);
    }

    private async Task<int> GetFoldersCountAsync(int parentId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await filesDbContext.CountTreesAsync(parentId);
    }

    private async Task<int> GetFilesCountAsync(int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await filesDbContext.CountFilesAsync(tenantId, folderId);
    }

    public async Task<bool> IsEmptyAsync(int folderId)
    {
        return await GetItemsCountAsync(folderId) == 0;
    }

    public bool UseTrashForRemoveAsync(Folder<int> folder)
    {
        return folder.RootFolderType != FolderType.TRASH && folder.RootFolderType != FolderType.Privacy && folder.FolderType != FolderType.BUNCH && !folder.SettingsPrivate;
    }

    public bool UseRecursiveOperation(int folderId, string toRootFolderId)
    {
        return true;
    }

    public bool UseRecursiveOperation(int folderId, int toRootFolderId)
    {
        return true;
    }

    public bool UseRecursiveOperation<TTo>(int folderId, TTo toRootFolderId)
    {
        return true;
    }

    public bool CanCalculateSubitems(int entryId)
    {
        return true;
    }

    public async Task<long> GetMaxUploadSizeAsync(int folderId, bool chunkedUpload = false)
    {
        var tmp = long.MaxValue;

        return Math.Min(tmp, chunkedUpload ?
            await _setupInfo.MaxChunkedUploadSize(_tenantManager, _maxTotalSizeStatistic) :
            await _setupInfo.MaxUploadSize(_tenantManager, _maxTotalSizeStatistic));
    }

    private async Task RecalculateFoldersCountAsync(int id, int tenantId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        await filesDbContext.UpdateFoldersCountAsync(tenantId, id);
    }

    #region Only for TMFolderDao



    public async Task<bool> IsExistAsync(string title, int folderId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await filesDbContext.DbFoldersAnyAsync(tenantId, title, folderId);
    }

    public async Task ReassignFoldersAsync(Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (exceptFolderIds == null || !exceptFolderIds.Any())
        {
            await filesDbContext.ReassignFoldersAsync(tenantId, oldOwnerId, newOwnerId);
        }
        else
        {
            await filesDbContext.ReassignFoldersPartiallyAsync(tenantId, oldOwnerId, newOwnerId, exceptFolderIds);
        }
    }
    
    public async Task ReassignRoomFoldersAsync(Guid oldOwnerId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var items = await filesDbContext.GetRoomsFoldersReassignInfoAsync(tenantId, oldOwnerId).ToListAsync();

        foreach (var group in items.GroupBy(x => x.RoomOwnerId))
        {
            await filesDbContext.ReassignSpecificFoldersAsync(tenantId, group.Select(f => f.FolderId), group.Key);
        }
    }

    public async IAsyncEnumerable<Folder<int>> SearchFoldersAsync(string text, bool bunch = false)
    {
        var folders = SearchAsync(text);

        await foreach (var f in folders)
        {
            if (bunch ? f.RootFolderType == FolderType.BUNCH
            : f.RootFolderType is FolderType.USER or FolderType.COMMON)
            {
                yield return f;
            }
        }
    }

    private async IAsyncEnumerable<Folder<int>> SearchAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var (success, ids) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(text));
        if (success)
        {
            await foreach (var e in filesDbContext.DbFolderQueriesByIdsAsync(tenantId, ids))
            {
                yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
            }

            yield break;
        }

        await foreach (var e in filesDbContext.DbFolderQueriesByTextAsync(tenantId, GetSearchText(text)))
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public IAsyncEnumerable<int> GetFolderIDsAsync(string module, string bunch, IEnumerable<string> data, bool createIfNotExists)
    {
        ArgumentException.ThrowIfNullOrEmpty(module);
        ArgumentException.ThrowIfNullOrEmpty(bunch);

        return InternalGetFolderIDsAsync(module, bunch, data, createIfNotExists);
    }

    private async IAsyncEnumerable<int> InternalGetFolderIDsAsync(string module, string bunch, IEnumerable<string> data, bool createIfNotExists)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        var keys = data.Select(id => $"{module}/{bunch}/{id}").ToArray();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        Dictionary<string, string> folderIdsDictionary;
        if (keys.Length > 1)
        {
            folderIdsDictionary = await filesDbContext.NodeAsync(tenantId, keys).ToDictionaryAsync(r => r.RightNode, r => r.LeftNode);
        }
        else
        {
            folderIdsDictionary = await filesDbContext.NodeOnlyAsync(tenantId, keys[0]).ToDictionaryAsync(r => r.RightNode, r => r.LeftNode);
        }

        foreach (var key in keys)
        {
            var newFolderId = 0;
            if (createIfNotExists && !folderIdsDictionary.TryGetValue(key, out _))
            {
                var folder = _serviceProvider.GetService<Folder<int>>();
                switch (bunch)
                {
                    case My:
                        folder.FolderType = FolderType.USER;
                        folder.Title = My;
                        break;
                    case Common:
                        folder.FolderType = FolderType.COMMON;
                        folder.Title = Common;
                        break;
                    case Trash:
                        folder.FolderType = FolderType.TRASH;
                        folder.Title = Trash;
                        break;
                    case Share:
                        folder.FolderType = FolderType.SHARE;
                        folder.Title = Share;
                        break;
                    case Recent:
                        folder.FolderType = FolderType.Recent;
                        folder.Title = Recent;
                        break;
                    case Favorites:
                        folder.FolderType = FolderType.Favorites;
                        folder.Title = Favorites;
                        break;
                    case Templates:
                        folder.FolderType = FolderType.Templates;
                        folder.Title = Templates;
                        break;
                    case Privacy:
                        folder.FolderType = FolderType.Privacy;
                        folder.Title = Privacy;
                        break;
                    case Projects:
                        folder.FolderType = FolderType.Projects;
                        folder.Title = Projects;
                        break;
                    case VirtualRooms:
                        folder.FolderType = FolderType.VirtualRooms;
                        folder.Title = VirtualRooms;
                        break;
                    case RoomTemplates:
                        folder.FolderType = FolderType.RoomTemplates;
                        folder.Title = RoomTemplates;
                        break;
                    case Archive:
                        folder.FolderType = FolderType.Archive;
                        folder.Title = Archive;
                        break;
                    default:
                        folder.FolderType = FolderType.BUNCH;
                        folder.Title = key;
                        break;
                }

                var strategy = filesDbContext.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await filesDbContext.Database.BeginTransactionAsync();//NOTE: Maybe we shouldn't start transaction here at all

                    newFolderId = await SaveFolderAsync(folder, tx, filesDbContext); //Save using our db manager

                    var newBunch = new DbFilesBunchObjects
                    {
                        LeftNode = newFolderId.ToString(),
                        RightNode = key,
                        TenantId = tenantId
                    };

                    await filesDbContext.AddOrUpdateAsync(r => r.BunchObjects, newBunch);
                    await filesDbContext.SaveChangesAsync();

                    await tx.CommitAsync(); //Commit changes
                });
            }

            yield return newFolderId;
        }
    }

    public async Task<int> GetFolderIDAsync(string module, string bunch, string data, bool createIfNotExists)
    {
        ArgumentException.ThrowIfNullOrEmpty(module);
        ArgumentException.ThrowIfNullOrEmpty(bunch);
        
        var key = $"{module}/{bunch}/{data}";
        var folderId = await InternalGetFolderIDAsync(key);

        if (folderId != null)
        {
            return Convert.ToInt32(folderId);
        }

        var newFolderId = 0;
        if (!createIfNotExists)
        {
            return newFolderId;
        }
        
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using (await _distributedLockProvider.TryAcquireFairLockAsync($"{key}_{tenantId}"))
        {
            folderId = await InternalGetFolderIDAsync(key);

            if (folderId != null)
            {
                return Convert.ToInt32(folderId);
            }

            var folder = _serviceProvider.GetService<Folder<int>>();
            folder.ParentId = 0;
            switch (bunch)
            {
                case My:
                    folder.FolderType = FolderType.USER;
                    folder.Title = My;
                    folder.CreateBy = new Guid(data);
                    break;
                case Common:
                    folder.FolderType = FolderType.COMMON;
                    folder.Title = Common;
                    break;
                case Trash:
                    folder.FolderType = FolderType.TRASH;
                    folder.Title = Trash;
                    folder.CreateBy = new Guid(data);
                    break;
                case Share:
                    folder.FolderType = FolderType.SHARE;
                    folder.Title = Share;
                    break;
                case Recent:
                    folder.FolderType = FolderType.Recent;
                    folder.Title = Recent;
                    break;
                case Favorites:
                    folder.FolderType = FolderType.Favorites;
                    folder.Title = Favorites;
                    break;
                case Templates:
                    folder.FolderType = FolderType.Templates;
                    folder.Title = Templates;
                    break;
                case Privacy:
                    folder.FolderType = FolderType.Privacy;
                    folder.Title = Privacy;
                    folder.CreateBy = new Guid(data);
                    break;
                case Projects:
                    folder.FolderType = FolderType.Projects;
                    folder.Title = Projects;
                    break;
                case VirtualRooms:
                    folder.FolderType = FolderType.VirtualRooms;
                    folder.Title = VirtualRooms;
                    break;
                case RoomTemplates:
                    folder.FolderType = FolderType.RoomTemplates;
                    folder.Title = RoomTemplates;
                    break;
                case Archive:
                    folder.FolderType = FolderType.Archive;
                    folder.Title = Archive;
                    break;
                default:
                    folder.FolderType = FolderType.BUNCH;
                    folder.Title = key;
                    break;
            }

            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await filesDbContext.Database.BeginTransactionAsync(); //NOTE: Maybe we shouldn't start transaction here at all
                newFolderId = await SaveFolderAsync(folder, tx, filesDbContext);
                var toInsert = new DbFilesBunchObjects
                {
                    LeftNode = newFolderId.ToString(),
                    RightNode = key,
                    TenantId = tenantId
                };

                await filesDbContext.AddOrUpdateAsync(r => r.BunchObjects, toInsert);
                await filesDbContext.SaveChangesAsync();

                await tx.CommitAsync(); //Commit changes
            });
        }

        return newFolderId;
    }

    private async Task<string> InternalGetFolderIDAsync(string key)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.LeftNodeAsync(tenantId, key);
    }

    Task<int> IFolderDao<int>.GetFolderIDProjectsAsync(bool createIfNotExists)
    {
        return (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Projects, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDTrashAsync(bool createIfNotExists, Guid? userId = null)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Trash, (userId ?? _authContext.CurrentAccount.ID).ToString(), createIfNotExists);
    }

    public async Task<int> GetFolderIDCommonAsync(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Common, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDUserAsync(bool createIfNotExists, Guid? userId = null)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, My, (userId ?? _authContext.CurrentAccount.ID).ToString(), createIfNotExists);
    }

    public async Task<int> GetFolderIDShareAsync(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Share, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDRecentAsync(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Recent, null, createIfNotExists);
    }

    public Task<int> GetFolderIDFavoritesAsync(bool createIfNotExists)
    {
        return (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Favorites, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDTemplatesAsync(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Templates, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDPrivacyAsync(bool createIfNotExists, Guid? userId = null)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Privacy, (userId ?? _authContext.CurrentAccount.ID).ToString(), createIfNotExists);
    }

    public async Task<int> GetFolderIDVirtualRooms(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, VirtualRooms, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDRoomTemplatesAsync(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, RoomTemplates, null, createIfNotExists);
    }

    public async Task<int> GetFolderIDArchive(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Archive, null, createIfNotExists);
    }

    public async IAsyncEnumerable<OriginData> GetOriginsDataAsync(IEnumerable<int> entriesIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var data in filesDbContext.OriginsDataAsync(tenantId, entriesIds))
        {
            yield return data;
        }
    }

    #endregion

    private IQueryable<DbFolder> GetFolderQuery(FilesDbContext filesDbContext, Expression<Func<DbFolder, bool>> where = null)
    {
        var q = Query(filesDbContext.Folders);
        if (where != null)
        {
            q = q.Where(where);
        }

        return q;
    }

    protected IQueryable<DbFolderQuery> FromQuery(FilesDbContext filesDbContext, IQueryable<DbFolder> dbFiles)
    {
        return dbFiles
            .Select(r => new DbFolderQuery
            {
                Folder = r,
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
                             select rs.Indexing).FirstOrDefault() && f.EntryId == r.Id && f.TenantId == r.TenantId && f.EntryType == FileEntryType.Folder
                         select f.Order
                                ).FirstOrDefault(),
                Settings = filesDbContext.RoomSettings.Where(x => x.TenantId == r.TenantId && x.RoomId == r.Id).Distinct().FirstOrDefault()
            });
    }

    private IQueryable<DbFolderQuery> FromQueryWithShared(FilesDbContext filesDbContext, IQueryable<DbFolder> dbFiles)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        return dbFiles
            .Select(r => new DbFolderQuery
            {
                Folder = r,
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
                Shared = (r.FolderType == FolderType.CustomRoom || r.FolderType == FolderType.PublicRoom || r.FolderType == FolderType.FillingFormsRoom) &&
                         filesDbContext.Security.Any(s =>
                             s.TenantId == tenantId && s.EntryId == r.Id.ToString() && s.EntryType == FileEntryType.Folder && s.SubjectType == SubjectType.PrimaryExternalLink),
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
                        select rs.Indexing).FirstOrDefault() && f.EntryId == r.Id && f.TenantId == r.TenantId && f.EntryType == FileEntryType.Folder
                    select f.Order
                ).FirstOrDefault(),
                Settings = filesDbContext.RoomSettings.Where(x => x.TenantId == r.TenantId && x.RoomId == r.Id).Distinct().FirstOrDefault()
            });
    }

    public async Task<string> GetBunchObjectIDAsync(int folderID)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await filesDbContext.RightNodeAsync(tenantId, folderID.ToString());
    }

    public IAsyncEnumerable<Folder<int>> GetProviderBasedRoomsAsync(SearchArea searchArea, IEnumerable<FilterType> filterTypes, IEnumerable<string> tags, Guid subjectId, string searchText, 
        bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<int>>();
    }

    public IAsyncEnumerable<Folder<int>> GetProviderBasedRoomsAsync(SearchArea searchArea, IEnumerable<int> roomsIds, IEnumerable<FilterType> filterTypes, IEnumerable<string> tags, Guid subjectId,
        string searchText, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<int>>();
    }

    public Task<int> GetProviderBasedRoomsCountAsync(SearchArea searchArea)
    {
        return Task.FromResult(0);
    }

    public async Task<Folder<int>> GetFirstParentTypeFromFileEntryAsync(FileEntry<int> entry)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var folderId = Convert.ToInt32(entry.ParentId);

        var parentFolder = await filesDbContext.ParentIdTypePairAsync(folderId);
        
        return mapper.Map<DbFolderQuery, Folder<int>>(parentFolder);
    }

    public Task<(int RoomId, string RoomTitle)> GetParentRoomInfoFromFileEntryAsync(FileEntry<int> entry)
    {
        var rootFolderType = entry.RootFolderType;

        if (rootFolderType != FolderType.VirtualRooms && rootFolderType != FolderType.RoomTemplates && rootFolderType != FolderType.Archive)
        {
            return Task.FromResult((-1, ""));
        }

        var rootFolderId = Convert.ToInt32(entry.RootId);
        var entryId = Convert.ToInt32(entry.Id);

        if (rootFolderId == entryId)
        {
            return Task.FromResult((-1, ""));
        }

        var folderId = Convert.ToInt32(entry.ParentId);

        if (rootFolderId == folderId)
        {
            return Task.FromResult((entryId, entry.Title));
        }

        return ParentRoomInfoFromFileEntryFromDbAsync(folderId);
    }

    private async Task<(int RoomId, string RoomTitle)> ParentRoomInfoFromFileEntryFromDbAsync(int folderId)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var parentFolders = await filesDbContext.ParentIdTitlePairAsync(folderId).ToListAsync();

        if (parentFolders.Count > 1)
        {
            return (parentFolders[1].ParentId, parentFolders[1].Title);
        }

        return (parentFolders[0].ParentId, parentFolders[0].Title);
    }

    public async Task<int> SetCustomOrder(int folderId, int parentFolderId, int order = 0)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await SetCustomOrder(filesDbContext, folderId, parentFolderId, order);
    }

    public async Task InitCustomOrder(Dictionary<int, int> folderIds, int parentFolderId)
    {
        await InitCustomOrder(folderIds, parentFolderId, FileEntryType.Folder);
    }
    
    private async Task<int> SetCustomOrder(FilesDbContext filesDbContext, int folderId, int parentFolderId, int order = 0)
    {
        return await SetCustomOrder(filesDbContext, folderId, parentFolderId, FileEntryType.Folder, order);
    }

    private async Task DeleteCustomOrder(FilesDbContext filesDbContext, int folderId)
    {
        await DeleteCustomOrder(filesDbContext, folderId, FileEntryType.Folder);
    }

    private IQueryable<DbFolder> BuildRoomsQuery(FilesDbContext filesDbContext, IQueryable<DbFolder> query, IEnumerable<FolderType> folderTypes, IEnumerable<string> tags, Guid subjectId, bool searchByTags, bool withoutTags,
        bool searchByFilter, bool withSubfolders, bool excludeSubject, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, QuotaFilter quotaFilter = QuotaFilter.All)
    {
        if (subjectId != Guid.Empty)
        {
            if (subjectFilter == SubjectFilter.Owner)
            {
                query = excludeSubject ? query.Where(f => f.CreateBy != subjectId) : query.Where(f => f.CreateBy == subjectId);
            }
            else if (subjectFilter == SubjectFilter.Member)
            {
                query = excludeSubject ? query.Where(f => f.CreateBy != subjectId && !subjectEntriesIds.Contains(f.Id.ToString()))
                    : query.Where(f => f.CreateBy == subjectId || subjectEntriesIds.Contains(f.Id.ToString()));
            }
        }

        if (searchByFilter)
        {
            query = query.Where(f => folderTypes.Contains(f.FolderType));
        }

        if (quotaFilter != QuotaFilter.All)
        {
            query = quotaFilter == QuotaFilter.Default
                ? query.Where(f => f.Settings.Quota == TenantEntityQuotaSettings.DefaultQuotaValue)
                : query.Where(f => f.Settings.Quota != TenantEntityQuotaSettings.DefaultQuotaValue);
        }

        if (withoutTags)
        {
            query = query.Where(f => !filesDbContext.TagLink.Join(filesDbContext.Tag, l => l.TagId, t => t.Id, (link, tag) => new { link.EntryId, tag })
                .Where(r => r.tag.Type == TagType.Custom).Any(t => t.EntryId == f.Id.ToString()));
        }

        if (searchByTags && !withSubfolders)
        {
            query = query.Join(filesDbContext.TagLink, f => f.Id.ToString(), t => t.EntryId, (folder, tag) => new { folder, tag.TagId })
                .Join(filesDbContext.Tag, r => r.TagId, t => t.Id, (result, tagInfo) => new { result.folder, result.TagId, tagInfo.Name })
                .Where(r => tags.Contains(r.Name))
                .Select(r => r.folder).Distinct();
        }

        return query;
    }

    private IQueryable<DbFolder> BuildRoomsWithSubfoldersQuery(FilesDbContext filesDbContext, IEnumerable<int> roomsIds, IEnumerable<FolderType> folderTypes, IEnumerable<string> tags, bool searchByTags, bool searchByFilter, bool withoutTags,
        bool withoutMe, Guid ownerId, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        var q1 = GetFolderQuery(filesDbContext, f => roomsIds.Contains(f.Id));

        q1 = BuildRoomsQuery(filesDbContext, q1, folderTypes, tags, ownerId, searchByTags, withoutTags, searchByFilter, true, withoutMe, subjectFilter, subjectEntriesIds);

        if (searchByTags)
        {
            var q2 = q1.Join(filesDbContext.TagLink, f => f.Id.ToString(), t => t.EntryId, (folder, tagLink) => new { folder, tagLink.TagId })
                .Join(filesDbContext.Tag, r => r.TagId, t => t.Id, (result, tag) => new { result.folder, tag.Name })
                .Where(r => tags.Contains(r.Name))
                .Select(r => r.folder.Id).Distinct();

            return GetFolderQuery(filesDbContext)
                .Join(filesDbContext.Tree, f => f.Id, t => t.FolderId, (folder, tree) => new { folder, tree })
                .Where(r => q2.Contains(r.tree.ParentId))
                .Select(r => r.folder);
        }

        if (!searchByFilter && !withoutTags && !withoutMe)
        {
            return GetFolderQuery(filesDbContext)
                .Join(filesDbContext.Tree, r => r.Id, a => a.FolderId, (folder, tree) => new { folder, tree })
                .Where(r => roomsIds.Contains(r.tree.ParentId))
                .Select(r => r.folder);
        }

        return GetFolderQuery(filesDbContext)
                    .Join(filesDbContext.Tree, f => f.Id, t => t.FolderId, (folder, tree) => new { folder, tree })
                    .Where(r => q1.Select(f => f.Id).Contains(r.tree.ParentId))
                    .Select(r => r.folder);
    }
    
    private static bool CheckInvalidFilters(IEnumerable<FilterType> filterTypes)
    {
        return filterTypes != null && filterTypes.Any(CheckInvalidFilter);
    }

    private static bool CheckInvalidFilter(FilterType filterType)
    {
        return filterType is
            FilterType.FilesOnly or
            FilterType.ByExtension or
            FilterType.DocumentsOnly or
            FilterType.ImagesOnly or
            FilterType.PresentationsOnly or
            FilterType.SpreadsheetsOnly or
            FilterType.ArchiveOnly or
            FilterType.MediaOnly or
            FilterType.Pdf or
            FilterType.PdfForm;
    }

    public async Task<IDataWriteOperator> CreateDataWriteOperatorAsync(
           int folderId,
           CommonChunkedUploadSession chunkedUploadSession,
           CommonChunkedUploadSessionHolder sessionHolder)
    {
        return (await globalStore.GetStoreAsync()).CreateDataWriteOperator(chunkedUploadSession, sessionHolder);
    }

    private async Task<IQueryable<DbFolder>> GetFoldersQueryWithFilters(int parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText, bool withSubfolders, bool excludeSubject,
        int roomId, FilesDbContext filesDbContext)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        var q = GetFolderQuery(filesDbContext, r => r.ParentId == parentId);

        if (withSubfolders && (filterType != FilterType.None || subjectId != Guid.Empty || !string.IsNullOrEmpty(searchText)))
        {
            q = GetFolderQuery(filesDbContext)
                    .Join(filesDbContext.Tree, r => r.Id, a => a.FolderId, (folder, tree) => new { folder, tree })
                    .Where(r => r.tree.ParentId == parentId && r.tree.Level != 0)
                    .Select(r => r.folder);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            var (success, searchIds) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(searchText));
            q = success ? q.Where(r => searchIds.Contains(r.Id)) : BuildSearch(q, searchText, SearchType.Any);
        }

        q = orderBy == null ? q : orderBy.SortedBy switch
        {
            SortedByType.Author => orderBy.IsAsc ? q.OrderBy(r => r.CreateBy) : q.OrderByDescending(r => r.CreateBy),
            SortedByType.AZ => orderBy.IsAsc ? q.OrderBy(r => r.Title) : q.OrderByDescending(r => r.Title),
            SortedByType.DateAndTime => orderBy.IsAsc ? q.OrderBy(r => r.ModifiedOn) : q.OrderByDescending(r => r.ModifiedOn),
            SortedByType.DateAndTimeCreation => orderBy.IsAsc ? q.OrderBy(r => r.CreateOn) : q.OrderByDescending(r => r.CreateOn),
            SortedByType.CustomOrder => q.Join(filesDbContext.FileOrder, a => a.Id, b => b.EntryId, (folder, order) => new { folder, order })
                                    .Where(r => r.order.EntryType == FileEntryType.Folder && r.order.TenantId == r.folder.TenantId)
                                    .OrderBy(r => r.order.Order)
                                    .Select(r => r.folder),
            _ => q.OrderBy(r => r.Title)
        };

        if (subjectId != Guid.Empty)
        {
            if (subjectGroup)
            {
                var users = (await _userManager.GetUsersByGroupAsync(subjectId)).Select(u => u.Id).ToArray();
                q = q.Where(r => users.Contains(r.CreateBy));
            }
            else
            {
                q = excludeSubject ? q.Where(r => r.CreateBy != subjectId) : q.Where(r => r.CreateBy == subjectId);
            }
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
            }), f => f.Id.ToString(), t => t.EntryId, (folder, tag) => new { folder, tag })
                .Where(r => r.tag.Type == TagType.Origin && r.tag.EntryType == FileEntryType.Folder && filesDbContext.Folders.Where(f =>
                        f.TenantId == tenantId && f.Id == filesDbContext.Tree.Where(t => t.FolderId == Convert.ToInt32(r.tag.Name))
                            .OrderByDescending(t => t.Level)
                            .Select(t => t.ParentId)
                            .Skip(1)
                            .FirstOrDefault())
                    .Select(f => f.Id)
                    .FirstOrDefault() == roomId)
                .Select(r => r.folder);
        }

        return q;
    }
    private IQueryable<DbFolder> ApplyAdditionalFolderFilters(
            IQueryable<DbFolder> q,
            FilesDbContext filesDbContext,
            int parentId,
            FolderType parentType,
            AdditionalFilterOption additionalFilterOption)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var currentUserId = _authContext.CurrentAccount.ID;
        var guestUserId = ASC.Core.Configuration.Constants.Guest.ID;
        var pdfCategories = new[] { (int)FilterType.PdfForm, (int)FilterType.Pdf };

        switch (additionalFilterOption)
        {
            case AdditionalFilterOption.MyFilesAndFolders:
                switch (parentType)
                {
                    case FolderType.FillingFormsRoom:
                        var foldersContainingMyFiles = filesDbContext.Folders
                            .Join(filesDbContext.Files, f => f.Id, file => file.ParentId, (folder, file) => new { folder, file })
                            .Join(filesDbContext.Tree, ff => ff.folder.Id, tree => tree.FolderId, (ff, tree) => new { ff.folder, ff.file, tree })
                            .Where(x => x.folder.TenantId == tenantId &&
                                        x.tree.ParentId == parentId &&
                                        x.file.CreateBy == currentUserId &&
                                        x.file.CreateBy != guestUserId)
                            .Select(x => x.folder.Id);

                        var parentFolderIds = filesDbContext.Folders
                            .Join(filesDbContext.Tree, f => f.Id, tree => tree.ParentId, (folder, tree) => new { folder, tree })
                            .Where(x => x.folder.TenantId == tenantId &&
                                        foldersContainingMyFiles.Contains(x.tree.FolderId))
                            .Select(x => x.folder.Id);

                        q = q.Where(f => parentFolderIds.Contains(f.Id) || f.FolderType == FolderType.DEFAULT);
                        break;

                    default:
                        q = q.Join(filesDbContext.Files, folder => folder.Id, file => file.ParentId, (folder, file) => new { folder, file })
                             .Where(x => x.file.CreateBy == currentUserId)
                             .Select(x => x.folder);
                        break;
                }
                break;

            case AdditionalFilterOption.FormsWithFillingRole:
                q = q.Where(r =>
                    filesDbContext.Files.Any(f =>
                        f.ParentId == r.Id &&
                        pdfCategories.Contains(f.Category) &&
                        f.TenantId == r.TenantId &&
                        filesDbContext.FilesFormRoleMapping.Any(m =>
                            m.TenantId == tenantId &&
                            m.FormId == f.Id &&
                            m.UserId == currentUserId)
                    ) ||
                    filesDbContext.Tree.Any(t =>
                        t.ParentId == r.Id &&
                        filesDbContext.Files.Any(f =>
                            f.ParentId == t.FolderId &&
                            pdfCategories.Contains(f.Category) &&
                            f.TenantId == r.TenantId &&
                            filesDbContext.FilesFormRoleMapping.Any(m =>
                                m.TenantId == tenantId &&
                                m.FormId == f.Id &&
                                m.UserId == currentUserId)
                        )
                    )
                );
                break;
        }

        return q;
    }

    public async Task<string> GetBackupExtensionAsync(int folderId)
    {
        return (await globalStore.GetStoreAsync()).GetBackupExtension();
    }
}

public class DbFolderQuery
{
    public DbFolder Folder { get; init; }
    public DbFolder Root { get; set; }
    public DbRoomSettings Settings { get; set; }
    public bool Shared { get; set; }
    public int Order { get; set; }
}

public class DbFolderQueryWithSecurity
{
    public DbFolderQuery DbFolderQuery { get; init; }
    public DbFilesSecurity Security { get; init; }
}

public class ParentIdTitlePair
{
    public int ParentId { get; init; }
    public string Title { get; init; }
}

public class FolderTypeUsedSpacePair
{
    public FolderType FolderType { get; set; }
    public long UsedSpace { get; set; }
}

public class OriginData
{
    public DbFolder OriginRoom { get; init; }
    public DbFolder OriginFolder { get; init; }
    public HashSet<KeyValuePair<string, FileEntryType>> Entries { get; init; }
}

public record FolderReassignInfo
{
    public int FolderId { get; init; }
    public Guid RoomOwnerId { get; init; }
}

[Scope(typeof(ICacheFolderDao<int>))]
internal class CacheFolderDao(
    FactoryIndexerFolder factoryIndexer,
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    SelectorFactory selectorFactory,
    CrossDao crossDao,
    IMapper mapper,
    GlobalStore globalStore,
    GlobalFolder globalFolder,
    IDistributedLockProvider distributedLockProvider,
    StorageFactory storageFactory)
    : FolderDao(
        factoryIndexer,
        userManager,
        dbContextManager,
        tenantManager,
        tenantUtil,
        setupInfo,
        maxTotalSizeStatistic,
        settingsManager,
        authContext,
        serviceProvider,
        daoFactory,
        selectorFactory,
        crossDao,
        mapper,
        globalStore,
        globalFolder,
        distributedLockProvider,
        storageFactory), ICacheFolderDao<int>
{
    private readonly ConcurrentDictionary<int, Folder<int>> _cache = new();
    public override async Task<Folder<int>> GetFolderAsync(int folderId)
                        {
        if (!_cache.TryGetValue(folderId, out var result))
                        {
            result = await base.GetFolderAsync(folderId);
            _cache.TryAdd(folderId, result);
                        }

        return result;
                        }
    
    private readonly ConcurrentDictionary<int, IEnumerable<Folder<int>>> _parentFoldersCache = new();
    public override async IAsyncEnumerable<Folder<int>> GetParentFoldersAsync(int folderId)
    {
        if (!_parentFoldersCache.TryGetValue(folderId, out var result))
        {
            result = await base.GetParentFoldersAsync(folderId).ToListAsync();
            _parentFoldersCache.TryAdd(folderId, result);
        }

        foreach (var folder in result)
        {
            yield return folder;
        }
    }

    private readonly ConcurrentDictionary<int, bool> _containsFormsInFolderCache = new();
    public override async Task<bool> ContainsFormsInFolder(Folder<int> folder)
    {
        if (!_containsFormsInFolderCache.TryGetValue(folder.Id, out var result))
        {
            result = await base.ContainsFormsInFolder(folder);
            _containsFormsInFolderCache.TryAdd(folder.Id, result);
        }

        return result;
    }
}