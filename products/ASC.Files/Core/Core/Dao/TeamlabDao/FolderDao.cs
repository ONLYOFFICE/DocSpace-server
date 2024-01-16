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

namespace ASC.Files.Core.Data;

[Scope]
internal class FolderDao(
        FactoryIndexerFolder factoryIndexer,
        UserManager userManager,
        IDbContextFactory<FilesDbContext> dbContextManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        CoreBaseSettings coreBaseSettings,
        CoreConfiguration coreConfiguration,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        IDaoFactory daoFactory,
        SelectorFactory selectorFactory,
        CrossDao crossDao,
        IMapper mapper,
        GlobalStore globalStore,
    GlobalFolder globalFolder,
    IDistributedLockProvider distributedLockProvider)
    : AbstractDao(dbContextManager,
              userManager,
              tenantManager,
              tenantUtil,
              setupInfo,
              maxTotalSizeStatistic,
              coreBaseSettings,
              coreConfiguration,
              settingsManager,
              authContext,
        serviceProvider), IFolderDao<int>
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
    private const string Archive = "archive";

    public async Task<Folder<int>> GetFolderAsync(int folderId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFolder = await Queries.DbFolderQueryWithSharedAsync(filesDbContext, tenantId, folderId);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async Task<Folder<int>> GetFolderAsync(string title, int parentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var dbFolder = await Queries.DbFolderQueryByTitleAndParentIdAsync(filesDbContext, tenantId, title, parentId);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async Task<Folder<int>> GetRootFolderAsync(int folderId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var id = await Queries.ParentIdAsync(filesDbContext, folderId);

        var dbFolder = await Queries.DbFolderQueryAsync(filesDbContext, tenantId, id);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async Task<Folder<int>> GetRootFolderByFileAsync(int fileId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var id = await Queries.ParentIdByFileIdAsync(filesDbContext, tenantId, fileId);

        var dbFolder = await Queries.DbFolderQueryAsync(filesDbContext, tenantId, id);

        return mapper.Map<DbFolderQuery, Folder<int>>(dbFolder);
    }

    public async IAsyncEnumerable<Folder<int>> GetFoldersAsync(FolderType type, int parentId)
    {
        await using var filesDbContext = _dbContextFactory.CreateDbContext();

        var q = await GetFolderQuery(filesDbContext, r => r.ParentId == parentId);

        q = q.Where(f => f.FolderType == type);

        await foreach (var e in FromQuery(filesDbContext, q).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }

    }
    public IAsyncEnumerable<Folder<int>> GetFoldersAsync(int parentId)
    {
        return GetFoldersAsync(parentId, default, FilterType.None, false, default, string.Empty);
    }

    public async IAsyncEnumerable<Folder<int>> GetRoomsAsync(
        IEnumerable<int> parentsIds, 
        FilterType filterType, 
        IEnumerable<string> tags, 
        Guid subjectId, 
        string searchText, 
        bool withSubfolders, 
        bool withoutTags,
        bool excludeSubject, 
        ProviderFilter provider,
        SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntriesIds)
    {
        if (CheckInvalidFilter(filterType) || provider != ProviderFilter.None)
        {
            yield break;
        }

        var filter = GetRoomTypeFilter(filterType);

        var searchByTags = tags != null && tags.Any() && !withoutTags;
        var searchByTypes = filterType != FilterType.None && filterType != FilterType.FoldersOnly;

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = await GetFolderQuery(filesDbContext, r => parentsIds.Contains(r.ParentId));

        q = !withSubfolders ? BuildRoomsQuery(filesDbContext, q, filter, tags, subjectId, searchByTags, withoutTags, searchByTypes, false, excludeSubject, subjectFilter, subjectEntriesIds)
            : await BuildRoomsWithSubfoldersQuery(filesDbContext, parentsIds, filter, tags, searchByTags, searchByTypes, withoutTags, excludeSubject, subjectId, subjectFilter, subjectEntriesIds);

        if (!string.IsNullOrEmpty(searchText))
        {
            var (success, searchIds) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(searchText));
            q = success ? q.Where(r => searchIds.Contains(r.Id)) : BuildSearch(q, searchText, SearchType.Any);
        }

        await foreach (var e in (await FromQueryWithShared(filesDbContext, q)).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public async IAsyncEnumerable<Folder<int>> GetRoomsAsync(
        IEnumerable<int> roomsIds, 
        FilterType filterType, 
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
        if (CheckInvalidFilter(filterType) || provider != ProviderFilter.None)
        {
            yield break;
        }

        var filter = GetRoomTypeFilter(filterType);

        var searchByTags = tags != null && tags.Any() && !withoutTags;
        var searchByTypes = filterType != FilterType.None && filterType != FilterType.FoldersOnly;

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = await GetFolderQuery(filesDbContext, f => roomsIds.Contains(f.Id) || (f.CreateBy == _authContext.CurrentAccount.ID && parentsIds.Contains(f.ParentId)));

        q = !withSubfolders ? BuildRoomsQuery(filesDbContext, q, filter, tags, subjectId, searchByTags, withoutTags, searchByTypes, false, excludeSubject, subjectFilter, subjectEntriesIds)
            : await BuildRoomsWithSubfoldersQuery(filesDbContext, roomsIds, filter, tags, searchByTags, searchByTypes, withoutTags, excludeSubject, subjectId, subjectFilter, subjectEntriesIds);

        if (!string.IsNullOrEmpty(searchText))
        {
            var (success, searchIds) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(searchText));

            q = success ? q.Where(r => searchIds.Contains(r.Id)) : BuildSearch(q, searchText, SearchType.Any);
        }

        await foreach (var e in (await FromQueryWithShared(filesDbContext, q)).AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public async Task<int> GetFoldersCountAsync(int parentId, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText,
        bool withSubfolders = false, bool excludeSubject = false, int roomId = default)
    {
        if (CheckInvalidFilter(filterType))
        {
            return 0;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (filterType == FilterType.None && subjectId == default && string.IsNullOrEmpty(searchText) && !withSubfolders && !excludeSubject && roomId == default)
        {
            return await filesDbContext.Tree.CountAsync(r => r.ParentId == parentId && r.Level == 1);
        }

        var q = await GetFoldersQueryWithFilters(parentId, null, subjectGroup, subjectId, searchText, withSubfolders, excludeSubject, roomId, filesDbContext);

        return await q.CountAsync();
    }

    public async IAsyncEnumerable<Folder<int>> GetFoldersAsync(int parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, bool withSubfolders = false,
        bool excludeSubject = false, int offset = 0, int count = -1, int roomId = default)
    {
        if (CheckInvalidFilter(filterType) || count == 0)
        {
            yield break;
        }

        var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = await GetFoldersQueryWithFilters(parentId, orderBy, subjectGroup, subjectID, searchText, withSubfolders, excludeSubject, roomId, filesDbContext);

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

    public async IAsyncEnumerable<Folder<int>> GetFoldersAsync(IEnumerable<int> folderIds, FilterType filterType = FilterType.None, bool subjectGroup = false, Guid? subjectID = null, string searchText = "", bool searchSubfolders = false, bool checkShare = true, bool excludeSubject = false)
    {
        if (CheckInvalidFilter(filterType))
        {
            yield break;
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = await GetFolderQuery(filesDbContext, r => folderIds.Contains(r.Id));

        if (searchSubfolders)
        {
            q =  (await GetFolderQuery(filesDbContext))
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

        await foreach (var e in FromQuery(filesDbContext, q).AsAsyncEnumerable().Distinct())
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public async IAsyncEnumerable<Folder<int>> GetParentFoldersAsync(int folderId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = Queries.DbFolderQueriesAsync(filesDbContext, tenantId, folderId);

        await foreach (var e in query)
        {
            yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
        }
    }

    public async IAsyncEnumerable<ParentRoomPair> GetParentRoomsAsync(IEnumerable<int> foldersIds)
    {
        var roomTypes = new List<FolderType>
        {
            FolderType.CustomRoom,
            FolderType.ReviewRoom,
            FolderType.FillingFormsRoom,
            FolderType.EditingRoom,
            FolderType.ReadOnlyRoom,
            FolderType.PublicRoom,
            FolderType.FormRoom
        };

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q = Queries.ParentRoomPairAsync(filesDbContext, tenantId, foldersIds, roomTypes);
        await foreach (var e in q)
        {
            yield return e;
        }
    }

    public async Task<int> SaveFolderAsync(Folder<int> folder)
    {
        return await SaveFolderAsync(folder, null);
    }

    private async Task<int> SaveFolderAsync(Folder<int> folder, IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var folderId = folder.Id;

        if (transaction == null)
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await dbContext.Database.BeginTransactionAsync();

                folderId = await InternalSaveFolderToDbAsync(folder);

                await tx.CommitAsync();
            });
        }
        else
        {
            folderId = await InternalSaveFolderToDbAsync(folder);
        }

        //FactoryIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, folder));
        return folderId;
    }

    private async Task<int> InternalSaveFolderToDbAsync(Folder<int> folder)
    {
        folder.Title = Global.ReplaceInvalidCharsAndTruncate(folder.Title);

        folder.ModifiedOn = _tenantUtil.DateTimeNow();
        folder.ModifiedBy = _authContext.CurrentAccount.ID;

        if (folder.CreateOn == default)
        {
            folder.CreateOn = _tenantUtil.DateTimeNow();
        }
        if (folder.CreateBy == default)
        {
            folder.CreateBy = _authContext.CurrentAccount.ID;
        }

        var isnew = false;

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (folder.Id != default && await IsExistAsync(folder.Id))
        {
            var toUpdate = await Queries.FolderForUpdateAsync(filesDbContext, tenantId, folder.Id);

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
                    Indexing = folder.SettingsIndexing
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
            isnew = true;
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
                    Indexing = folder.SettingsIndexing
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
            var newTree = new DbFolderTree
            {
                FolderId = folder.Id,
                ParentId = folder.Id,
                Level = 0
            };

            await filesDbContext.Tree.AddAsync(newTree);
            await filesDbContext.SaveChangesAsync();

            //full path to root
            var oldTree = filesDbContext.Tree
                .Where(r => r.FolderId == folder.ParentId);

            foreach (var o in oldTree)
            {
                var treeToAdd = new DbFolderTree
                {
                    FolderId = folder.Id,
                    ParentId = o.ParentId,
                    Level = o.Level + 1
                };

                await filesDbContext.Tree.AddAsync(treeToAdd);
            }

            await filesDbContext.SaveChangesAsync();
        }

        if (isnew)
        {
            await RecalculateFoldersCountAsync(folder.Id);
            await SetCustomOrder(filesDbContext, folder.Id, folder.ParentId);
        }

        return folder.Id;

    }

    private async Task<bool> IsExistAsync(int folderId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await Queries.FolderIsExistAsync(filesDbContext, tenantId, folderId);
    }

    public async Task DeleteFolderAsync(int folderId)
    {
        if (folderId == default)
        {
            throw new ArgumentNullException(nameof(folderId));
        }

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();
            var subfolders = await Queries.SubfolderIdsAsync(context, folderId).ToListAsync();

            if (!subfolders.Contains(folderId))
            {
                subfolders.Add(folderId); // chashed folder_tree
            }

            var parent = await Queries.ParentIdByIdAsync(context, tenantId, folderId);

            var folderToDelete = await Queries.DbFoldersForDeleteAsync(context, tenantId, subfolders).ToListAsync();

            foreach (var f in folderToDelete)
            {
                await factoryIndexer.DeleteAsync(f);
            }
            
            context.Folders.RemoveRange(folderToDelete);

            await Queries.DeleteOrderAsync(context, subfolders);
            
            var subfoldersStrings = subfolders.Select(r => r.ToString()).ToList();

            await Queries.DeleteTagLinksAsync(context, tenantId, subfoldersStrings);

            await Queries.DeleteTagsAsync(context, tenantId);

            await Queries.DeleteTagLinkByTagOriginAsync(context, tenantId, folderId.ToString(), subfoldersStrings);

            await Queries.DeleteTagOriginAsync(context, tenantId, folderId.ToString(), subfoldersStrings);

            await Queries.DeleteFilesSecurityAsync(context, tenantId, subfoldersStrings);

            await Queries.DeleteBunchObjectsAsync(context, tenantId, folderId.ToString());

            await DeleteCustomOrder(filesDbContext, folderId);

            await context.SaveChangesAsync();
            await tx.CommitAsync();
            await RecalculateFoldersCountAsync(parent);
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
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();
        var trashIdTask = globalFolder.GetFolderTrashAsync(daoFactory);

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();
            
                var folder = await GetFolderAsync(folderId);
                var oldParentId = folder.ParentId;

                if (folder.FolderType != FolderType.DEFAULT && !DocSpaceHelper.IsRoom(folder.FolderType))
                {
                    throw new ArgumentException("It is forbidden to move the System folder.", nameof(folderId));
                }

                var recalcFolders = new List<int> { toFolderId };
            var parent = await Queries.ParentIdByIdAsync(context, tenantId, folderId);

                if (parent != 0 && !recalcFolders.Contains(parent))
                {
                    recalcFolders.Add(parent);
                }
            await Queries.UpdateFoldersAsync(context, tenantId, folderId, toFolderId, _authContext.CurrentAccount.ID);

            var subfolders = await Queries.SubfolderAsync(context, folderId).ToDictionaryAsync(r => r.FolderId, r => r.Level);

            await Queries.DeleteTreesBySubfoldersDictionaryAsync(context, subfolders.Select(r => r.Key));

            var toInsert = Queries.TreesOrderByLevel(context, toFolderId);

                foreach (var subfolder in subfolders)
                {
                    await foreach (var f in toInsert)
                    {
                        var newTree = new DbFolderTree
                        {
                            FolderId = subfolder.Key,
                            ParentId = f.ParentId,
                            Level = subfolder.Value + 1 + f.Level
                        };
                    await context.AddOrUpdateAsync(r => r.Tree, newTree);
                    }
                }

                var trashId = await trashIdTask;
                var tagDao = daoFactory.GetTagDao<int>();

                if (toFolderId == trashId)
                {
                    var origin = Tag.Origin(folderId, FileEntryType.Folder, oldParentId, _authContext.CurrentAccount.ID);
                    await tagDao.SaveTagsAsync(origin);
                }
                else if (oldParentId == trashId)
                {
                    await tagDao.RemoveTagLinksAsync(folderId, FileEntryType.Folder, TagType.Origin);
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

                foreach (var e in recalcFolders)
                {
                    await RecalculateFoldersCountAsync(e);
                }
                foreach (var e in recalcFolders)
                {
                    await GetRecalculateFilesCountUpdateAsync(e);
                }
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
        copy.FolderType = (folder.FolderType == FolderType.ReadyFormFolder || 
            folder.FolderType == FolderType.InProcessFormFolder ||
            folder.FolderType == FolderType.FormFillingFolderDone || 
            folder.FolderType == FolderType.FormFillingFolderInProgress) ? FolderType.DEFAULT : folder.FolderType;

        copy = await GetFolderAsync(await SaveFolderAsync(copy));

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
        return Task.FromResult((IDictionary<int, string>)new Dictionary<int, string>());
        }

    public async Task<IDictionary<int, string>> CanMoveOrCopyAsync(IEnumerable<int> folderIds, int to)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        var result = new Dictionary<int, string>();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        foreach (var folderId in folderIds)
        {
            var exists = await Queries.AnyTreeAsync(filesDbContext, folderId, to);

            if (exists)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderCopyError);
            }

            var conflict = await Queries.FolderIdAsync(filesDbContext, tenantId, folderId, to);

            if (conflict != 0)
            {
                var files = Queries.DbFilesAsync(filesDbContext, tenantId, folderId, conflict);

                await foreach (var file in files)
                {
                    result[file.Id] = file.Title;
                }

                var children = await Queries.ArrayAsync(filesDbContext, tenantId, folderId).ToListAsync();

                foreach (var pair in await CanMoveOrCopyAsync(children, conflict))
                {
                    result.Add(pair.Key, pair.Value);
                }
            }
        }

        return result;
    }

    public async Task<int> RenameFolderAsync(Folder<int> folder, string newTitle)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var toUpdate = await Queries.FolderAsync(filesDbContext, tenantId, folder.Id);

        toUpdate.Title = Global.ReplaceInvalidCharsAndTruncate(newTitle);
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
        return await Queries.CountTreesAsync(filesDbContext, parentId);
    }

    private async Task<int> GetFilesCountAsync(int folderId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await Queries.CountFilesAsync(filesDbContext, tenantId, folderId);
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

    private async Task RecalculateFoldersCountAsync(int id)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        await Queries.UpdateFoldersCountAsync(filesDbContext, tenantId, id);
    }

    #region Only for TMFolderDao

    public async Task ReassignFoldersAsync(Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        if (exceptFolderIds == null || !exceptFolderIds.Any())
        {
            await Queries.ReassignFoldersAsync(filesDbContext, tenantId, oldOwnerId, newOwnerId);
        }
        else
        {
            await Queries.ReassignFoldersPartiallyAsync(filesDbContext, tenantId, oldOwnerId, newOwnerId, exceptFolderIds);
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

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var (success, ids) = await factoryIndexer.TrySelectIdsAsync(s => s.MatchAll(text));
        if (success)
        {
            await foreach (var e in Queries.DbFolderQueriesByIdsAsync(filesDbContext, tenantId, ids))
            {
                yield return mapper.Map<DbFolderQuery, Folder<int>>(e);
            }

            yield break;
        }

        await foreach (var e in Queries.DbFolderQueriesByTextAsync(filesDbContext, tenantId, GetSearchText(text)))
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
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        var keys = data.Select(id => $"{module}/{bunch}/{id}").ToArray();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var folderIdsDictionary = await Queries.NodeAsync(filesDbContext, tenantId, keys).ToDictionaryAsync(r => r.RightNode, r => r.LeftNode);

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
                    await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                    await using var tx = await dbContext.Database.BeginTransactionAsync();//NOTE: Maybe we shouldn't start transaction here at all

                    newFolderId = await SaveFolderAsync(folder, tx); //Save using our db manager

                    var newBunch = new DbFilesBunchObjects
                    {
                        LeftNode = newFolderId.ToString(),
                        RightNode = key,
                        TenantId = tenantId
                    };

                    await dbContext.AddOrUpdateAsync(r => r.BunchObjects, newBunch);
                    await dbContext.SaveChangesAsync();

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
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"{key}_{tenantId}"))
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
                await using var context = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await context.Database.BeginTransactionAsync(); //NOTE: Maybe we shouldn't start transaction here at all
                newFolderId = await SaveFolderAsync(folder, tx); //Save using our db manager
                var toInsert = new DbFilesBunchObjects
                {
                    LeftNode = newFolderId.ToString(),
                    RightNode = key,
                    TenantId = tenantId
                };

                await context.AddOrUpdateAsync(r => r.BunchObjects, toInsert);
                await context.SaveChangesAsync();

                await tx.CommitAsync(); //Commit changes
            });
        }

        return newFolderId;
    }

    private async Task<string> InternalGetFolderIDAsync(string key)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await Queries.LeftNodeAsync(filesDbContext, tenantId, key);
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

    public async Task<int> GetFolderIDArchive(bool createIfNotExists)
    {
        return await (this as IFolderDao<int>).GetFolderIDAsync(FileConstant.ModuleId, Archive, null, createIfNotExists);
    }

    public async IAsyncEnumerable<OriginData> GetOriginsDataAsync(IEnumerable<int> entriesIds)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await foreach (var data in Queries.OriginsDataAsync(filesDbContext, tenantId, entriesIds))
        {
            yield return data;
        }
    }

    #endregion

    private async Task<IQueryable<DbFolder>> GetFolderQuery(FilesDbContext filesDbContext, Expression<Func<DbFolder, bool>> where = null)
    {
        var q = await Query(filesDbContext.Folders);
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
                Settings = (from f in filesDbContext.RoomSettings 
                            where f.TenantId == r.TenantId && f.RoomId == r.Id 
                            select f).FirstOrDefault()
            });
    }

    private async Task<IQueryable<DbFolderQuery>> FromQueryWithShared(FilesDbContext filesDbContext, IQueryable<DbFolder> dbFiles)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

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
                Shared = (r.FolderType == FolderType.CustomRoom || r.FolderType == FolderType.PublicRoom) &&
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
                Settings = (from f in filesDbContext.RoomSettings 
                    where f.TenantId == r.TenantId && f.RoomId == r.Id 
                    select f).FirstOrDefault()
            });
    }

    public async Task<string> GetBunchObjectIDAsync(int folderID)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        return await Queries.RightNodeAsync(filesDbContext, tenantId, folderID.ToString());
    }

    public async Task<Dictionary<string, string>> GetBunchObjectIDsAsync(List<int> folderIDs)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        var folderSIds = folderIDs.Select(r => r.ToString()).ToList();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await Queries.NodeByFolderIdsAsync(filesDbContext, tenantId, folderSIds)
                    .ToDictionaryAsync(r => r.LeftNode, r => r.RightNode);
    }

    public async IAsyncEnumerable<FolderWithShare> GetFeedsForRoomsAsync(int tenant, DateTime from, DateTime to)
    {
        var roomTypes = new List<FolderType>
        {
            FolderType.CustomRoom,
            FolderType.ReviewRoom,
            FolderType.FillingFormsRoom,
            FolderType.EditingRoom,
            FolderType.ReadOnlyRoom,
            FolderType.PublicRoom,
            FolderType.FormRoom
        };

        Expression<Func<DbFolder, bool>> filter = f => roomTypes.Contains(f.FolderType);

        await foreach (var e in GetFeedsInternalAsync(tenant, from, to, filter, null))
        {
            yield return e;
        }
    }

    public async IAsyncEnumerable<FolderWithShare> GetFeedsForFoldersAsync(int tenant, DateTime from, DateTime to)
    {
        Expression<Func<DbFolder, bool>> foldersFilter = f => f.FolderType == FolderType.DEFAULT;
        Expression<Func<DbFolderQueryWithSecurity, bool>> securityFilter = f => f.Security.Share == FileShare.Restrict;


        await foreach (var e in GetFeedsInternalAsync(tenant, from, to, foldersFilter, securityFilter))
        {
            yield return e;
        }
    }

    private async IAsyncEnumerable<FolderWithShare> GetFeedsInternalAsync(int tenant, DateTime from, DateTime to, Expression<Func<DbFolder, bool>> foldersFilter,
        Expression<Func<DbFolderQueryWithSecurity, bool>> securityFilter)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q1 = filesDbContext.Folders
            .Where(r => r.TenantId == tenant)
            .Where(foldersFilter)
            .Where(r => r.CreateOn >= from && r.ModifiedOn <= to);

        var q2 = FromQuery(filesDbContext, q1)
            .Select(r => new DbFolderQueryWithSecurity { DbFolderQuery = r, Security = null });

        var q3 = filesDbContext.Folders
            .Where(r => r.TenantId == tenant)
            .Where(foldersFilter);

        var q4 = FromQuery(filesDbContext, q3)
            .Join(filesDbContext.Security.DefaultIfEmpty(), r => r.Folder.Id.ToString(), s => s.EntryId, (f, s) => new DbFolderQueryWithSecurity { DbFolderQuery = f, Security = s })
            .Where(r => r.Security.TenantId == tenant)
            .Where(r => r.Security.EntryType == FileEntryType.Folder)
            .Where(r => r.Security.TimeStamp >= from && r.Security.TimeStamp <= to);

        if (securityFilter != null)
        {
            q4 = q4.Where(securityFilter);
        }

        await foreach (var e in q2.AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQueryWithSecurity, FolderWithShare>(e);
        }

        await foreach (var e in q4.AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFolderQueryWithSecurity, FolderWithShare>(e);
        }
    }

    public async IAsyncEnumerable<int> GetTenantsWithFoldersFeedsAsync(DateTime fromTime)
    {
        Expression<Func<DbFolder, bool>> filter = f => f.FolderType == FolderType.DEFAULT;

        await foreach (var q in GetTenantsWithFeeds(fromTime, filter, false))
        {
            yield return q;
        }
    }

    public async IAsyncEnumerable<int> GetTenantsWithRoomsFeedsAsync(DateTime fromTime)
    {
        var roomTypes = new List<FolderType>
        {
            FolderType.CustomRoom,
            FolderType.ReviewRoom,
            FolderType.FillingFormsRoom,
            FolderType.EditingRoom,
            FolderType.ReadOnlyRoom,
            FolderType.PublicRoom,
            FolderType.FormRoom
        };

        Expression<Func<DbFolder, bool>> filter = f => roomTypes.Contains(f.FolderType);

        await foreach (var q in GetTenantsWithFeeds(fromTime, filter, true))
        {
            yield return q;
        }
    }

    public IAsyncEnumerable<Folder<int>> GetFakeRoomsAsync(SearchArea searchArea, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText, 
        bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<int>>();
    }

    public IAsyncEnumerable<Folder<int>> GetFakeRoomsAsync(SearchArea searchArea, IEnumerable<int> roomsIds, FilterType filterType, IEnumerable<string> tags, Guid subjectId,
        string searchText, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<int>>();
    }

    public async Task<(int RoomId, string RoomTitle)> GetParentRoomInfoFromFileEntryAsync(FileEntry<int> entry)
    {
        var rootFolderType = entry.RootFolderType;

        if (rootFolderType != FolderType.VirtualRooms && rootFolderType != FolderType.Archive)
        {
            return (-1, "");
        }

        var rootFolderId = Convert.ToInt32(entry.RootId);
        var entryId = Convert.ToInt32(entry.Id);

        if (rootFolderId == entryId)
        {
            return (-1, "");
        }

        var folderId = Convert.ToInt32(entry.ParentId);

        if (rootFolderId == folderId)
        {
            return (entryId, entry.Title);
        }

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var parentFolders = await Queries.ParentIdTitlePairAsync(filesDbContext, folderId).ToListAsync();

        if (parentFolders.Count > 1)
        {
            return (parentFolders[1].ParentId, parentFolders[1].Title);
        }

        return (parentFolders[0].ParentId, parentFolders[0].Title);
    }

    public async Task SetCustomOrder(int folderId, int parentFolderId, int order = 0)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        await SetCustomOrder(filesDbContext, folderId, parentFolderId, order);
    }

    public async Task InitCustomOrder(IEnumerable<int> folderIds, int parentFolderId)
    {
        await InitCustomOrder(folderIds, parentFolderId, FileEntryType.Folder);
    }
    
    private async Task SetCustomOrder(FilesDbContext filesDbContext, int folderId, int parentFolderId, int order = 0)
    {
        await SetCustomOrder(filesDbContext, folderId, parentFolderId, FileEntryType.Folder, order);
    }

    private async Task DeleteCustomOrder(FilesDbContext filesDbContext, int folderId)
    {
        await DeleteCustomOrder(filesDbContext, folderId, FileEntryType.Folder);
    }

    private async IAsyncEnumerable<int> GetTenantsWithFeeds(DateTime fromTime, Expression<Func<DbFolder, bool>> filter, bool includeSecurity)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var q1 = filesDbContext.Folders
            .Where(r => r.ModifiedOn > fromTime)
            .Where(filter)
            .Select(r => r.TenantId).Distinct();

        await foreach (var q in q1.AsAsyncEnumerable())
        {
            yield return q;
        }

        if (includeSecurity)
        {
            var q2 = filesDbContext.Security.Where(r => r.TimeStamp > fromTime).Select(r => r.TenantId).Distinct();

            await foreach (var q in q2.AsAsyncEnumerable())
            {
                yield return q;
            }
        }
    }

    private IQueryable<DbFolder> BuildRoomsQuery(FilesDbContext filesDbContext, IQueryable<DbFolder> query, FolderType filterByType, IEnumerable<string> tags, Guid subjectId, bool searchByTags, bool withoutTags,
        bool searchByFilter, bool withSubfolders, bool excludeSubject, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
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
            query = query.Where(f => f.FolderType == filterByType);
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

    private async Task<IQueryable<DbFolder>> BuildRoomsWithSubfoldersQuery(FilesDbContext filesDbContext, IEnumerable<int> roomsIds, FolderType filterByType, IEnumerable<string> tags, bool searchByTags, bool searchByFilter, bool withoutTags,
        bool withoutMe, Guid ownerId, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        var q1 = await GetFolderQuery(filesDbContext, f => roomsIds.Contains(f.Id));

        q1 = BuildRoomsQuery(filesDbContext, q1, filterByType, tags, ownerId, searchByTags, withoutTags, searchByFilter, true, withoutMe, subjectFilter, subjectEntriesIds);

        if (searchByTags)
        {
            var q2 = q1.Join(filesDbContext.TagLink, f => f.Id.ToString(), t => t.EntryId, (folder, tagLink) => new { folder, tagLink.TagId })
                .Join(filesDbContext.Tag, r => r.TagId, t => t.Id, (result, tag) => new { result.folder, tag.Name })
                .Where(r => tags.Contains(r.Name))
                .Select(r => r.folder.Id).Distinct();

            return (await GetFolderQuery(filesDbContext))
                .Join(filesDbContext.Tree, f => f.Id, t => t.FolderId, (folder, tree) => new { folder, tree })
                .Where(r => q2.Contains(r.tree.ParentId))
                .Select(r => r.folder);
        }

        if (!searchByFilter && !withoutTags && !withoutMe)
        {
            return (await GetFolderQuery(filesDbContext))
                .Join(filesDbContext.Tree, r => r.Id, a => a.FolderId, (folder, tree) => new { folder, tree })
                .Where(r => roomsIds.Contains(r.tree.ParentId))
                .Select(r => r.folder);
        }

        return (await GetFolderQuery(filesDbContext))
                    .Join(filesDbContext.Tree, f => f.Id, t => t.FolderId, (folder, tree) => new { folder, tree })
                    .Where(r => q1.Select(f => f.Id).Contains(r.tree.ParentId))
                    .Select(r => r.folder);
    }

    private bool CheckInvalidFilter(FilterType filterType)
    {
        return filterType is
            FilterType.FilesOnly or
            FilterType.ByExtension or
            FilterType.DocumentsOnly or
            FilterType.OFormOnly or
            FilterType.OFormTemplateOnly or
            FilterType.ImagesOnly or
            FilterType.PresentationsOnly or
            FilterType.SpreadsheetsOnly or
            FilterType.ArchiveOnly or
            FilterType.MediaOnly;
    }

    private FolderType GetRoomTypeFilter(FilterType filterType)
    {
        return filterType switch
        {
            FilterType.FillingFormsRooms => FolderType.FillingFormsRoom,
            FilterType.EditingRooms => FolderType.EditingRoom,
            FilterType.ReviewRooms => FolderType.ReviewRoom,
            FilterType.ReadOnlyRooms => FolderType.ReadOnlyRoom,
            FilterType.CustomRooms => FolderType.CustomRoom,
            FilterType.PublicRooms => FolderType.PublicRoom,
            FilterType.FormRooms => FolderType.FormRoom,
            _ => FolderType.CustomRoom
        };
    }

    public async Task<IDataWriteOperator> CreateDataWriteOperatorAsync(
           int folderId,
           CommonChunkedUploadSession chunkedUploadSession,
           CommonChunkedUploadSessionHolder sessionHolder)
    {
        return (await globalStore.GetStoreAsync()).CreateDataWriteOperator(chunkedUploadSession, sessionHolder);
    }

    private async Task<IQueryable<DbFolder>> GetFoldersQueryWithFilters(int parentId, OrderBy orderBy, bool subjectGroup, Guid subjectId, string searchText, bool withSubfolders, bool excludeSubject,
        int roomId, FilesDbContext filesDbContext)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        var q = await GetFolderQuery(filesDbContext, r => r.ParentId == parentId);

        if (withSubfolders)
        {
            q = (await GetFolderQuery(filesDbContext))
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

        if (roomId != default)
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

public class ParentRoomPair
{
    public int FolderId { get; init; }
    public int ParentRoomId { get; init; }
}

public class OriginData
{
    public DbFolder OriginRoom { get; init; }
    public DbFolder OriginFolder { get; init; }
    public HashSet<KeyValuePair<string, FileEntryType>> Entries { get; init; }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, int, Task<DbFolderQuery>> DbFolderQueryAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == folderId)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault(),
                            Order = (
                                from f in ctx.FileOrder
                                where (
                                    from rs in ctx.RoomSettings 
                                    where rs.TenantId == f.TenantId && rs.RoomId ==
                                        (from t in ctx.Tree
                                            where t.FolderId == r.ParentId
                                            orderby t.Level descending
                                            select t.ParentId
                                        ).Skip(1).FirstOrDefault()
                                    select rs.Indexing).FirstOrDefault() && f.EntryId == r.Id && f.TenantId == r.TenantId && f.EntryType == FileEntryType.Folder
                                select f.Order
                            ).FirstOrDefault(),
                            Settings = (from f in ctx.RoomSettings 
                                where f.TenantId == r.TenantId && f.RoomId == r.Id 
                                select f).FirstOrDefault()
                        }
                    ).SingleOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<DbFolderQuery>> DbFolderQueryWithSharedAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == folderId)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault(),
                            Shared = (r.FolderType == FolderType.CustomRoom || r.FolderType == FolderType.PublicRoom) && 
                                     ctx.Security.Any(s => 
                                         s.TenantId == tenantId && 
                                         s.EntryId == r.Id.ToString() && 
                                         s.EntryType == FileEntryType.Folder && 
                                         s.SubjectType == SubjectType.PrimaryExternalLink),
                            Settings = (from f in ctx.RoomSettings 
                                where f.TenantId == r.TenantId && f.RoomId == r.Id 
                                select f).FirstOrDefault()
                        }
                    ).SingleOrDefault());

    public static readonly Func<FilesDbContext, int, string, int, Task<DbFolderQuery>>
        DbFolderQueryByTitleAndParentIdAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string title, int parentId) =>
                ctx.Folders.Where(r => r.TenantId == tenantId)
                    .Where(r => r.Title == title && r.ParentId == parentId)
                    .OrderBy(r => r.CreateOn)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault()
                        }
                    ).FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFolderQuery>> DbFolderQueriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.Id, a => a.ParentId, (folder, tree) => new { folder, tree })
                    .Where(r => r.tree.FolderId == folderId)
                    .OrderByDescending(r => r.tree.Level)
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r.folder,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.folder.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.folder.TenantId
                                    select f
                                ).FirstOrDefault(),
                            Order = (
                                from f in ctx.FileOrder
                                where (
                                    from rs in ctx.RoomSettings 
                                    where rs.TenantId == f.TenantId && rs.RoomId ==
                                        (from t in ctx.Tree
                                            where t.FolderId == r.folder.ParentId
                                            orderby t.Level descending
                                            select t.ParentId
                                        ).Skip(1).FirstOrDefault()
                                    select rs.Indexing).FirstOrDefault() && f.EntryId == r.folder.Id && f.TenantId == r.folder.TenantId && f.EntryType == FileEntryType.Folder
                                select f.Order
                            ).FirstOrDefault(),
                            Settings = (from f in ctx.RoomSettings 
                                where f.TenantId == r.folder.TenantId && f.RoomId == r.folder.Id 
                                select f).FirstOrDefault()
                        }
                    ));

    public static readonly Func<FilesDbContext, int, Task<int>> ParentIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Where(r => r.FolderId == folderId)
                    .OrderByDescending(r => r.Level)
                    .Select(r => r.ParentId)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, Task<int>> ParentIdByFileIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int fileId) =>
                ctx.Tree
                    .Where(r => ctx.Files
                        .Where(f => f.TenantId == tenantId)
                        .Where(f => f.Id == fileId && f.CurrentVersion)
                        .Select(f => f.ParentId)
                        .Distinct()
                        .Contains(r.FolderId))
                    .OrderByDescending(r => r.Level)
                    .Select(r => r.ParentId)
                    .FirstOrDefault());

    public static readonly
        Func<FilesDbContext, int, IEnumerable<int>, IEnumerable<FolderType>, IAsyncEnumerable<ParentRoomPair>>
        ParentRoomPairAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> foldersIds, IEnumerable<FolderType> roomTypes) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.Id, a => a.ParentId, (folder, tree) => new { folder, tree })
                    .Where(r => foldersIds.Contains(r.tree.FolderId))
                    .OrderByDescending(r => r.tree.Level)
                    .Where(r => roomTypes.Contains(r.folder.FolderType))
                    .Select(r => new ParentRoomPair { FolderId = r.tree.FolderId, ParentRoomId = r.folder.Id }));

    public static readonly Func<FilesDbContext, int, int, Task<DbFolder>> FolderForUpdateAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders.FirstOrDefault(r => r.TenantId == tenantId && r.Id == id));

    public static readonly Func<FilesDbContext, int, int, Task<bool>> FolderIsExistAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders.Any(r => r.Id == id && r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<int>> SubfolderIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int id) =>
                ctx.Tree
                    .Where(r => r.ParentId == id)
                    .Select(r => r.FolderId));

    public static readonly Func<FilesDbContext, int, int, Task<int>> ParentIdByIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == id)
                    .Select(r => r.ParentId)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFolder>>
        DbFoldersForDeleteAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> subfolders) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subfolders.Contains(r.Id)));


    public static readonly Func<FilesDbContext, IEnumerable<int>, Task<int>> DeleteOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, IEnumerable<int> subfolders) =>
                ctx.Tree
                    .Where(r => subfolders.Contains(r.FolderId))
                    .ExecuteDelete());
    
    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>> DeleteTagLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> subfolders) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subfolders.Contains(r.EntryId))
                    .Where(r => r.EntryType == FileEntryType.Folder)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Task<int>> DeleteTagsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => !ctx.TagLink.Where(a => a.TenantId == tenantId).Any(a => a.TagId == r.Id))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, IEnumerable<string>, Task<int>>
        DeleteTagLinkByTagOriginAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string id, IEnumerable<string> subfolders) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(l =>
                        ctx.Tag
                            .Where(r => r.TenantId == tenantId)
                            .Where(t => t.Name == id || subfolders.Contains(t.Name))
                            .Select(t => t.Id)
                            .Contains(l.TagId)
                    )
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, IEnumerable<string>, Task<int>> DeleteTagOriginAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string id, IEnumerable<string> subfolders) =>
                ctx.Tag
                    .Where(r => r.TenantId == tenantId)
                    .Where(t => t.Name == id || subfolders.Contains(t.Name))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>> DeleteFilesSecurityAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> subfolders) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => subfolders.Contains(r.EntryId))
                    .Where(r => r.EntryType == FileEntryType.Folder)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteBunchObjectsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string id) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.LeftNode == id)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, int, int, Guid, Task<int>> UpdateFoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int parentId, Guid modifiedBy) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == folderId)
                    .ExecuteUpdate(toUpdate => toUpdate
                        .SetProperty(p => p.ParentId, parentId)
                        .SetProperty(p => p.ModifiedOn, DateTime.UtcNow)
                        .SetProperty(p => p.ModifiedBy, modifiedBy)
                    ));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFolderTree>> SubfolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Where(r => r.ParentId == folderId));

    public static readonly Func<FilesDbContext, IEnumerable<int>, Task<int>>
        DeleteTreesBySubfoldersDictionaryAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, IEnumerable<int> subfolders) =>
                ctx.Tree
                    .Where(r => subfolders.Contains(r.FolderId) && !subfolders.Contains(r.ParentId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<DbFolderTree>> TreesOrderByLevel =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int toFolderId) =>
                ctx.Tree
                    .Where(r => r.FolderId == toFolderId)
                    .OrderBy(r => r.Level)
                    .AsQueryable());

    public static readonly Func<FilesDbContext, int, int, Task<bool>> AnyTreeAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int parentId, int folderId) =>
            ctx.Tree
                .Any(r => r.ParentId == parentId && r.FolderId == folderId));

    public static readonly Func<FilesDbContext, int, int, int, Task<int>> FolderIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int parentId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(a => a.Title.ToLower() == ctx.Folders
                        .Where(r => r.TenantId == tenantId)
                        .Where(r => r.Id == folderId)
                        .Select(r => r.Title.ToLower())
                        .FirstOrDefault()
                    )
                    .Where(r => r.ParentId == parentId)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, IAsyncEnumerable<DbFile>> DbFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId, int conflict) =>
                ctx.Files

                    .Join(ctx.Files, f1 => f1.Title.ToLower(), f2 => f2.Title.ToLower(), (f1, f2) => new { f1, f2 })
                    .Where(r => r.f1.TenantId == tenantId && r.f1.CurrentVersion && r.f1.ParentId == folderId)
                    .Where(r => r.f2.TenantId == tenantId && r.f2.CurrentVersion && r.f2.ParentId == conflict)
                    .Select(r => r.f1));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<int>> ArrayAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentId == folderId)
                    .Select(r => r.Id));

    public static readonly Func<FilesDbContext, int, int, Task<DbFolder>> FolderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders.FirstOrDefault(r => r.TenantId == tenantId && r.Id == folderId));

    public static readonly Func<FilesDbContext, int, Task<int>> CountTreesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int parentId) =>
                ctx.Tree
                    .Join(ctx.Folders, tree => tree.FolderId, folder => folder.Id, (tree, folder) => tree)
                    .Count(r => r.ParentId == parentId && r.Level > 0));

    public static readonly Func<FilesDbContext, int, int, Task<int>> CountFilesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.ParentId, r => r.FolderId, (file, tree) => new { tree, file })
                    .Where(r => r.tree.ParentId == folderId)
                    .Select(r => r.file.Id)
                    .Distinct()
                    .Count());

    public static readonly Func<FilesDbContext, int, int, Task<int>> UpdateFoldersCountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int id) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Join(ctx.Tree, r => r.Id, r => r.ParentId, (file, tree) => new { file, tree })
                    .Where(r => r.tree.FolderId == id)
                    .Select(r => r.file)
                    .ExecuteUpdate(q =>
                        q.SetProperty(r => r.FoldersCount, r => ctx.Tree.Count(t => t.ParentId == r.Id) - 1)
                    ));

    public static readonly Func<FilesDbContext, int, Guid, Guid, Task<int>> ReassignFoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid oldOwnerId, Guid newOwnerId) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.CreateBy == oldOwnerId)
                    .ExecuteUpdate(f => f.SetProperty(p => p.CreateBy, newOwnerId)));

    public static readonly Func<FilesDbContext, int, Guid, Guid, IEnumerable<int>, Task<int>> ReassignFoldersPartiallyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid oldOwnerId, Guid newOwnerId, IEnumerable<int> exceptFolderIds) =>
                ctx.Folders
                    .Where(f => f.TenantId == tenantId)
                    .Where(f => f.CreateBy == oldOwnerId)
                    .Where(f => ctx.Tree.FirstOrDefault(t => t.FolderId == f.Id && exceptFolderIds.Contains(t.ParentId)) == null)
                    .ExecuteUpdate(p => p.SetProperty(f => f.CreateBy, newOwnerId)));

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<DbFolderQuery>>
        DbFolderQueriesByIdsAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> ids) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ids.Contains(r.Id))
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault()
                        }
                    ));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFolderQuery>>
        DbFolderQueriesByTextAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string text) =>
                ctx.Folders
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Title.ToLower().Contains(text))
                    .Select(r =>
                        new DbFolderQuery
                        {
                            Folder = r,
                            Root = (from f in ctx.Folders
                                    where f.Id ==
                                          (from t in ctx.Tree
                                           where t.FolderId == r.ParentId
                                           orderby t.Level descending
                                           select t.ParentId
                                          ).FirstOrDefault()
                                    where f.TenantId == r.TenantId
                                    select f
                                ).FirstOrDefault()
                        }
                    ));

    public static readonly Func<FilesDbContext, int, string[], IAsyncEnumerable<DbFilesBunchObjects>>
        NodeAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string[] keys) =>
                ctx.BunchObjects

                    .Where(r => r.TenantId == tenantId)
                    .Where(r => keys.Length > 1 ? keys.Any(a => a == r.RightNode) : r.RightNode == keys[0]));

    public static readonly Func<FilesDbContext, int, string, Task<string>> LeftNodeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string key) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.RightNode == key)
                    .Select(r => r.LeftNode)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, string, Task<string>> RightNodeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string key) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.LeftNode == key)
                    .Select(r => r.RightNode)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, IAsyncEnumerable<OriginData>> OriginsDataAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entriesIds) =>
                ctx.TagLink
                    .Where(l => l.TenantId == tenantId)
                    .Where(l => entriesIds.Contains(Convert.ToInt32(l.EntryId)))
                    .Join(ctx.Tag
                            .Where(t => t.Type == TagType.Origin), l => l.TagId, t => t.Id,
                        (l, t) => new { t.Name, t.Type, l.EntryType, l.EntryId })
                    .GroupBy(r => r.Name, r => new { r.EntryId, r.EntryType })
                    .Select(r => new OriginData
                    {
                        OriginRoom = ctx.Folders.FirstOrDefault(f => f.TenantId == tenantId &&
                            f.Id == ctx.Tree
                                .Where(t => t.FolderId == Convert.ToInt32(r.Key))
                                .OrderByDescending(t => t.Level)
                                .Select(t => t.ParentId)
                                .Skip(1)
                                .FirstOrDefault()),
                        OriginFolder =
                            ctx.Folders.FirstOrDefault(f =>
                                f.TenantId == tenantId && f.Id == Convert.ToInt32(r.Key)),
                        Entries = r.Select(e => new KeyValuePair<string, FileEntryType>(e.EntryId, e.EntryType))
                            .ToHashSet()
                    }));

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, IAsyncEnumerable<DbFilesBunchObjects>>
        NodeByFolderIdsAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> folderIds) =>
                ctx.BunchObjects
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => folderIds.Any(a => a == r.LeftNode)));

    public static readonly Func<FilesDbContext, int, IAsyncEnumerable<ParentIdTitlePair>> ParentIdTitlePairAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int folderId) =>
                ctx.Tree
                    .Join(ctx.Folders, r => r.ParentId, s => s.Id, (t, f) => new { Tree = t, Folders = f })
                    .Where(r => r.Tree.FolderId == folderId)
                    .OrderByDescending(r => r.Tree.Level)
                    .Select(r => new ParentIdTitlePair { ParentId = r.Tree.ParentId, Title = r.Folders.Title }));
}