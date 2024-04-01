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

namespace ASC.Files.Core.Core.Thirdparty;

/// <inheritdoc />
[Scope]
internal class ThirdPartyFolderDao<TFile, TFolder, TItem>(IDbContextFactory<FilesDbContext> dbContextFactory,
        UserManager userManager,
        CrossDao crossDao,
        IDaoSelector<TFile, TFolder, TItem> daoSelector,
        IFileDao<int> fileDao,
        IFolderDao<int> folderDao,
        TempStream tempStream,
        SetupInfo setupInfo,
        IDaoBase<TFile, TFolder, TItem> dao,
        TenantManager tenantManager)
    : BaseFolderDao, IFolderDao<string>
    where TFile : class, TItem
    where TFolder : class, TItem
    where TItem : class
{
    private IProviderInfo<TFile, TFolder, TItem> _providerInfo;

    public void Init(string pathPrefix, IProviderInfo<TFile, TFolder, TItem> providerInfo)
    {
        dao.Init(pathPrefix, providerInfo);
        _providerInfo = providerInfo;
    }

    public async Task<Folder<string>> GetFolderAsync(string folderId)
    {
        var folder = dao.ToFolder(await dao.GetFolderAsync(folderId));

        if (folder.FolderType is not (FolderType.CustomRoom or FolderType.PublicRoom))
        {
            return folder;
    }

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        folder.Shared = await Queries.SharedAsync(filesDbContext, tenantId, folder.Id, FileEntryType.Folder, SubjectType.PrimaryExternalLink);

        return folder;
    }

    public async Task<Folder<string>> GetFolderAsync(string title, string parentId)
    {
        var items = await dao.GetItemsAsync(parentId, true);

        return dao.ToFolder(items.Find(item => dao.GetName(item).Equals(title, StringComparison.InvariantCultureIgnoreCase)) as TFolder);
    }

    public Task<Folder<string>> GetRootFolderByFileAsync(string fileId)
    {
        return dao.GetRootFolderAsync();
    }

    public async IAsyncEnumerable<Folder<string>> GetRoomsAsync(IEnumerable<string> roomsIds, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText, bool withSubfolders, bool withoutTags, bool excludeSubject, ProviderFilter provider,
        SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, IEnumerable<int> parentsIds = null)
    {
        if (dao.CheckInvalidFilter(filterType) || (provider != ProviderFilter.None && provider != _providerInfo.ProviderFilter))
        {
            yield break;
        }

        var rooms = roomsIds.ToAsyncEnumerable().SelectAwait(async e => await GetFolderAsync(e).ConfigureAwait(false));

        rooms = FilterByRoomType(rooms, filterType);
        rooms = FilterBySubject(rooms, subjectId, excludeSubject, subjectFilter, subjectEntriesIds);

        if (!string.IsNullOrEmpty(searchText))
        {
            rooms = rooms.Where(x => x.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        rooms = FilterByTags(rooms, withoutTags, tags, filesDbContext);

        await foreach (var room in rooms)
        {
            yield return room;
        }
    }
    public IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId, FolderType type)
    {
        return GetFoldersAsync(parentId);
    }
    public async IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId)
    {
        var items = await dao.GetItemsAsync(parentId, true);
        foreach (var i in items)
        {
            yield return dao.ToFolder(i as TFolder);
        }
    }

    public IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, bool withSubfolders = false, bool excludeSubject = false, int offset = 0, int count = -1, string roomId = default)
    {
        if (dao.CheckInvalidFilter(filterType))
        {
            return AsyncEnumerable.Empty<Folder<string>>();
        }

        var folders = GetFoldersAsync(parentId); //TODO:!!!

        if (subjectID != Guid.Empty)
        {
            folders = folders.Where(x => subjectGroup
                                             ? userManager.IsUserInGroup(x.CreateBy, subjectID)
                                             : x.CreateBy == subjectID);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            folders = folders.Where(x => x.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        orderBy ??= new OrderBy(SortedByType.DateAndTime, false);

        folders = orderBy.SortedBy switch
        {
            SortedByType.Author => orderBy.IsAsc ? folders.OrderBy(x => x.CreateBy) : folders.OrderByDescending(x => x.CreateBy),
            SortedByType.AZ => orderBy.IsAsc ? folders.OrderBy(x => x.Title) : folders.OrderByDescending(x => x.Title),
            SortedByType.DateAndTime => orderBy.IsAsc ? folders.OrderBy(x => x.ModifiedOn) : folders.OrderByDescending(x => x.ModifiedOn),
            SortedByType.DateAndTimeCreation => orderBy.IsAsc ? folders.OrderBy(x => x.CreateOn) : folders.OrderByDescending(x => x.CreateOn),
            _ => orderBy.IsAsc ? folders.OrderBy(x => x.Title) : folders.OrderByDescending(x => x.Title)
        };

        return folders;
    }

    public IAsyncEnumerable<Folder<string>> GetFoldersAsync(IEnumerable<string> folderIds, FilterType filterType = FilterType.None, bool subjectGroup = false, Guid? subjectID = null, string searchText = "", bool searchSubfolders = false, bool checkShare = true, bool excludeSubject = false)
    {
        if (dao.CheckInvalidFilter(filterType))
        {
            return AsyncEnumerable.Empty<Folder<string>>();
        }

        var folders = folderIds.ToAsyncEnumerable().SelectAwait(async e => await GetFolderAsync(e));

        if (subjectID.HasValue && subjectID != Guid.Empty)
        {
            folders = folders.Where(x => subjectGroup
                                             ? userManager.IsUserInGroup(x.CreateBy, subjectID.Value)
                                             : x.CreateBy == subjectID);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            folders = folders.Where(x => x.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        return folders;
    }

    public async IAsyncEnumerable<Folder<string>> GetParentFoldersAsync(string folderId)
    {
        var path = new List<Folder<string>>();

        while (folderId != null)
        {
            var folder = await dao.GetFolderAsync(folderId);

            if (folder is IErrorItem)
            {
                folderId = null;
            }
            else
            {
                path.Add(dao.ToFolder(folder));
                folderId = dao.GetParentFolderId(folder);
            }
        }

        path.Reverse();

        await foreach (var p in path.ToAsyncEnumerable())
        {
            yield return p;
        }
    }

    public async Task<string> SaveFolderAsync(Folder<string> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if (folder.Id != null)
        {
            return await RenameFolderAsync(folder, folder.Title);
        }

        if (folder.ParentId != null)
        {
            var folderId = dao.MakeThirdId(folder.ParentId);

            folder.Title = await dao.GetAvailableTitleAsync(folder.Title, folderId, IsExistAsync);

            var storage = await _providerInfo.StorageAsync;
            var thirdFolder = await storage.CreateFolderAsync(folder.Title, folderId);

            await _providerInfo.CacheResetAsync(dao.GetId(thirdFolder));
            var parentFolderId = dao.GetParentFolderId(thirdFolder);
            if (parentFolderId != null)
            {
                await _providerInfo.CacheResetAsync(parentFolderId);
            }

            return dao.MakeId(thirdFolder);
        }

        return null;
    }

    public async Task<bool> IsExistAsync(string title, string folderId)
    {
        var items = await dao.GetItemsAsync(folderId, true);

        return items.Exists(item => dao.GetName(item).Equals(title, StringComparison.InvariantCultureIgnoreCase));
    }

    public async Task DeleteFolderAsync(string folderId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var folder = await dao.GetFolderAsync(folderId);
        var id = dao.MakeId(folder);

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();
            await Queries.DeleteTagLinksAsync(context, tenantId, id);
            await Queries.DeleteDbFilesTag(context);
            await Queries.DeleteSecuritiesAsync(context, tenantId, id);
            await Queries.DeleteThirdpartyIdMappingsAsync(context, tenantId, id);

            await tx.CommitAsync();
        });

        if (folder is not IErrorItem)
        {
            var storage = await _providerInfo.StorageAsync;
            await storage.DeleteItemAsync(folder);
        }

        await _providerInfo.CacheResetAsync(dao.GetId(folder), true);
        var parentFolderId = dao.GetParentFolderId(folder);
        if (parentFolderId != null)
        {
            await _providerInfo.CacheResetAsync(parentFolderId);
        }
    }

    public async Task<string> MoveFolderAsync(string folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var folder = await dao.GetFolderAsync(folderId);
        if (folder is IErrorItem errorFolder)
        {
            throw new Exception(errorFolder.Error);
        }

        var toFolder = await dao.GetFolderAsync(toFolderId);
        if (toFolder is IErrorItem errorFolder1)
        {
            throw new Exception(errorFolder1.Error);
        }

        var fromFolderId = dao.GetParentFolderId(folder);

        var newTitle = await dao.GetAvailableTitleAsync(dao.GetName(folder), dao.GetId(toFolder), IsExistAsync);
        var storage = await _providerInfo.StorageAsync;
        var movedFolder = await storage.MoveFolderAsync(dao.GetId(folder), newTitle, dao.GetId(toFolder));

        await _providerInfo.CacheResetAsync(dao.GetId(folder), false);
        await _providerInfo.CacheResetAsync(fromFolderId);
        await _providerInfo.CacheResetAsync(dao.GetId(toFolder));

        var newId = dao.MakeId(dao.GetId(movedFolder));

        if (_providerInfo.MutableEntityId)
        {
            await dao.UpdateIdAsync(dao.MakeId(folder), newId);
    }

        return newId;
    }

    public async Task<TTo> MoveFolderAsync<TTo>(string folderId, TTo toFolderId, CancellationToken? cancellationToken)
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

    public async Task<int> MoveFolderAsync(string folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        var moved = await crossDao.PerformCrossDaoFolderCopyAsync(
        folderId, this, daoSelector.GetFileDao(folderId), daoSelector.ConvertId,
                toFolderId, folderDao, fileDao, r => r,
                true, cancellationToken);

        return moved.Id;
    }

    public async Task<Folder<TTo>> CopyFolderAsync<TTo>(string folderId, TTo toFolderId, CancellationToken? cancellationToken)
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

    public async Task<Folder<string>> CopyFolderAsync(string folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var folder = await dao.GetFolderAsync(folderId);
        if (folder is IErrorItem errorFolder)
        {
            throw new Exception(errorFolder.Error);
        }

        var toFolder = await dao.GetFolderAsync(toFolderId);
        if (toFolder is IErrorItem errorFolder1)
        {
            throw new Exception(errorFolder1.Error);
        }

        var newTitle = await dao.GetAvailableTitleAsync(dao.GetName(folder), dao.GetId(toFolder), IsExistAsync);
        var storage = await _providerInfo.StorageAsync;
        var newFolder = await storage.CopyFolderAsync(dao.GetId(folder), newTitle, dao.GetId(toFolder));

        await _providerInfo.CacheResetAsync(dao.GetId(newFolder));
        await _providerInfo.CacheResetAsync(dao.GetId(newFolder), false);
        await _providerInfo.CacheResetAsync(dao.GetId(toFolder));

        return dao.ToFolder(newFolder);
    }

    public async Task<Folder<int>> CopyFolderAsync(string folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        var moved = await crossDao.PerformCrossDaoFolderCopyAsync(
            folderId, this, daoSelector.GetFileDao(folderId), daoSelector.ConvertId,
            toFolderId, folderDao, fileDao, r => r,
            false, cancellationToken);

        return moved;
    }

    public Task<IDictionary<string, string>> CanMoveOrCopyAsync<TTo>(IEnumerable<string> folderIds, TTo to)
    {
        return to switch
        {
            int tId => CanMoveOrCopyAsync(folderIds, tId),
            string tsId => CanMoveOrCopyAsync(folderIds, tsId),
            _ => throw new NotImplementedException()
        };
        }

    public Task<IDictionary<string, string>> CanMoveOrCopyAsync(IEnumerable<string> folderIds, string to)
        {
        return Task.FromResult((IDictionary<string, string>)new Dictionary<string, string>());
    }

    public Task<IDictionary<string, string>> CanMoveOrCopyAsync(IEnumerable<string> folderIds, int to)
    {
        return Task.FromResult((IDictionary<string, string>)new Dictionary<string, string>());
    }

    public async Task<string> UpdateFolderAsync(Folder<string> folder, string newTitle, long newQuota)
    {
        return await RenameFolderAsync(folder, newTitle);
    }
    
    public async Task<string> RenameFolderAsync(Folder<string> folder, string newTitle)
    {
        var thirdFolder = await dao.GetFolderAsync(folder.Id);
        var parentFolderId = dao.GetParentFolderId(thirdFolder);
        var renamedThirdFolder = thirdFolder;

        if (dao.IsRoot(thirdFolder) || DocSpaceHelper.IsRoom(folder.FolderType))
        {
            await daoSelector.RenameProviderAsync(_providerInfo, newTitle);
        }
        else
        {
            newTitle = await dao.GetAvailableTitleAsync(newTitle, parentFolderId, IsExistAsync);

            //rename folder
            var storage = await _providerInfo.StorageAsync;
            renamedThirdFolder = await storage.RenameFolderAsync(dao.GetId(thirdFolder), newTitle);
        }

        await _providerInfo.CacheResetAsync(dao.GetId(thirdFolder));
        if (parentFolderId != null)
        {
            await _providerInfo.CacheResetAsync(parentFolderId);
        }

        var newId = dao.MakeId(dao.GetId(renamedThirdFolder));

        if (_providerInfo.MutableEntityId)
        {
            await dao.UpdateIdAsync(dao.MakeId(thirdFolder), newId);
    }

        return newId;
    }

    public async Task<bool> IsEmptyAsync(string folderId)
    {
        var thirdFolderId = dao.MakeThirdId(folderId);
        //note: without cache
        var storage = await _providerInfo.StorageAsync;

        var items = await storage.GetItemsAsync(thirdFolderId);

        return items.Count == 0;
    }

    public bool UseTrashForRemoveAsync(Folder<string> folder)
    {
        return false;
    }

    public bool UseRecursiveOperation(string folderId, string toRootFolderId)
    {
        return false;
    }

    public bool UseRecursiveOperation<TTo>(string folderId, TTo toRootFolderId)
    {
        return false;
    }

    public bool UseRecursiveOperation(string folderId, int toRootFolderId)
    {
        return false;
    }

    public bool CanCalculateSubitems(string entryId)
    {
        return false;
    }

    public async Task<long> GetMaxUploadSizeAsync(string folderId, bool chunkedUpload = false)
    {
        var storage = await _providerInfo.StorageAsync;
        var storageMaxUploadSize = await storage.GetMaxUploadSizeAsync();

        return chunkedUpload ? storageMaxUploadSize : Math.Min(storageMaxUploadSize, setupInfo.AvailableFileSize);
    }

    public Task<IDataWriteOperator> CreateDataWriteOperatorAsync(string folderId, CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder)
    {
        return Task.FromResult<IDataWriteOperator>(new ChunkZipWriteOperator(tempStream, chunkedUploadSession, sessionHolder));
    }

    public Task<string> GetBackupExtensionAsync(string folderId)
    {
        return Task.FromResult("tar.gz");
    }

    public Task ReassignFoldersAsync(Guid oldOwnerId, Guid newOwnerId, IEnumerable<string> exceptFolderIds)
    {
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<Folder<string>> SearchFoldersAsync(string text, bool bunch)
    {
        return null;
    }


    public Task<string> GetFolderIDAsync(string module, string bunch, string data, bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public IAsyncEnumerable<string> GetFolderIDsAsync(string module, string bunch, IEnumerable<string> data, bool createIfNotExists)
    {
        return AsyncEnumerable.Empty<string>();
    }

    public Task<string> GetFolderIDCommonAsync(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }


    public Task<string> GetFolderIDUserAsync(bool createIfNotExists, Guid? userId)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDShareAsync(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }


    public Task<string> GetFolderIDRecentAsync(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDFavoritesAsync(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDTemplatesAsync(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDPrivacyAsync(bool createIfNotExists, Guid? userId)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDTrashAsync(bool createIfNotExists, Guid? userId)
    {
        return Task.FromResult<string>(null);
    }
    
    public Task<string> GetFolderIDProjectsAsync(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDVirtualRooms(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetFolderIDArchive(bool createIfNotExists)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetBunchObjectIDAsync(string folderID)
    {
        return Task.FromResult<string>(null);
    }

    public Task<Dictionary<string, string>> GetBunchObjectIDsAsync(List<string> folderIDs)
    {
        return Task.FromResult<Dictionary<string, string>>(null);
    }

    public IAsyncEnumerable<FolderWithShare> GetFeedsForRoomsAsync(int tenant, DateTime from, DateTime to)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderWithShare> GetFeedsForFoldersAsync(int tenant, DateTime from, DateTime to)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ParentRoomPair> GetParentRoomsAsync(IEnumerable<int> foldersIds)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<int> GetTenantsWithFoldersFeedsAsync(DateTime fromTime)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<int> GetTenantsWithRoomsFeedsAsync(DateTime fromTime)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<OriginData> GetOriginsDataAsync(IEnumerable<string> entriesId)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<Folder<string>> GetRoomsAsync(IEnumerable<string> parentsIds, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText,
        bool withSubfolders, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, QuotaFilter quotaFilter)
    {
        return AsyncEnumerable.Empty<Folder<string>>();
    }

    public IAsyncEnumerable<Folder<string>> GetProviderBasedRoomsAsync(SearchArea searchArea, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText, 
        bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<string>>();
    }

    public IAsyncEnumerable<Folder<string>> GetProviderBasedRoomsAsync(SearchArea searchArea, IEnumerable<string> roomsIds, FilterType filterType, IEnumerable<string> tags,
        Guid subjectId, string searchText, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<string>>();
    }

    public Task<Folder<string>> GetRootFolderAsync(string folderId)
    {
        return dao.GetRootFolderAsync();
    }

    public Task<int> GetItemsCountAsync(string folderId)
    {
        throw new NotImplementedException();
    }
    public Task<FolderType> GetFirstParentTypeFromFileEntryAsync(FileEntry<string> entry)
    {
        throw new NotImplementedException();
    }
    public Task<(string RoomId, string RoomTitle)> GetParentRoomInfoFromFileEntryAsync(FileEntry<string> entry)
    {
        return Task.FromResult(entry.RootFolderType is not (FolderType.VirtualRooms or FolderType.Archive) 
            ? (string.Empty, string.Empty) 
            : (_providerInfo.FolderId, _providerInfo.CustomerTitle));
    }
    public Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        throw new NotImplementedException();
    }
    public Task<int> GetFoldersCountAsync(string parentId, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText, bool withSubfolders = false, bool excludeSubject = false, string roomId = default)
    {
        throw new NotImplementedException();
    }

    Task<IDictionary<string, string>> IFolderDao<string>.CanMoveOrCopyAsync<TTo>(IEnumerable<string> folderIds, TTo to)
    {
        throw new NotImplementedException();
    }

    public Task SetCustomOrder(string folderId, string parentFolderId, int order)
    {
        return Task.CompletedTask;
    }

    public Task InitCustomOrder(IEnumerable<string> folderIds, string parentFolderId)
    {
        return Task.CompletedTask;
    }

    public Task<string> SetWatermarkSettings(WatermarkSettings watermarkSettings, Folder<string> folder)
    {
        throw new NotImplementedException();
    }

    public Task<string> ChangeTreeFolderSizeAsync(string folderId, long size)
    {
        throw new NotImplementedException();
    }

    public Task<string> ChangeFolderQuotaAsync(Folder<string> folder, long quota)
    {
        throw new NotImplementedException();
    }

    public Task<WatermarkSettings> GetWatermarkSettings(Folder<string> room)
    {
        throw new NotImplementedException();
    }

    public Task<Folder<string>> DeleteWatermarkSettings(Folder<string> room)
    {
        throw new NotImplementedException();
    }
}

internal abstract class BaseFolderDao
{
    protected IAsyncEnumerable<Folder<string>> FilterByTags(IAsyncEnumerable<Folder<string>> rooms, bool withoutTags, IEnumerable<string> tags, FilesDbContext filesDbContext)
    {
        if (withoutTags)
        {
            return rooms.Join(filesDbContext.ThirdpartyIdMapping.ToAsyncEnumerable(), f => f.Id, m => m.Id, (folder, map) => new { folder, map.HashId })
                .WhereAwait(async r => !await filesDbContext.TagLink.Join(filesDbContext.Tag, l => l.TagId, t => t.Id, (link, tag) => new { link.EntryId, tag })
                    .Where(tag => tag.tag.Type == TagType.Custom).ToAsyncEnumerable().AnyAsync(t => t.EntryId == r.HashId))
                .Select(r => r.folder);
        }

        if (tags == null || !tags.Any())
        {
            return rooms;
        }

        var filtered = rooms.Join(filesDbContext.ThirdpartyIdMapping.ToAsyncEnumerable(), f => f.Id, m => m.Id, (folder, map) => new { folder, map.HashId })
            .Join(filesDbContext.TagLink.ToAsyncEnumerable(), r => r.HashId, t => t.EntryId, (result, tag) => new { result.folder, tag.TagId })
            .Join(filesDbContext.Tag.ToAsyncEnumerable(), r => r.TagId, t => t.Id, (result, tagInfo) => new { result.folder, tagInfo.Name })
            .Where(r => tags.Contains(r.Name))
            .Select(r => r.folder);

        return filtered;
    }

    protected IAsyncEnumerable<Folder<string>> FilterByRoomType(IAsyncEnumerable<Folder<string>> rooms, FilterType filterType)
    {
        if (filterType is FilterType.None or FilterType.FoldersOnly)
        {
            return rooms;
        }

        var typeFilter = filterType switch
        {
            FilterType.FillingFormsRooms => FolderType.FillingFormsRoom,
            FilterType.EditingRooms => FolderType.EditingRoom,
            FilterType.ReviewRooms => FolderType.ReviewRoom,
            FilterType.ReadOnlyRooms => FolderType.ReadOnlyRoom,
            FilterType.CustomRooms => FolderType.CustomRoom,
            FilterType.PublicRooms => FolderType.PublicRoom,
            FilterType.VirtualDataRooms => FolderType.VirtualDataRoom,
            FilterType.FormRooms => FolderType.FormRoom,
            _ => FolderType.DEFAULT
        };

        return rooms.Where(f => f.FolderType == typeFilter);
    }

    protected IAsyncEnumerable<Folder<string>> FilterBySubject(IAsyncEnumerable<Folder<string>> rooms, Guid subjectId, bool excludeSubject, SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntriesIds)
    {
        if (subjectId == Guid.Empty)
        {
            return rooms;
        }

        if (subjectFilter == SubjectFilter.Owner)
        {
            return excludeSubject ? rooms.Where(f => f != null && f.CreateBy != subjectId) : rooms.Where(f => f != null && f.CreateBy == subjectId);
        }
        if (subjectFilter == SubjectFilter.Member)
        {
            return excludeSubject ? rooms.Where(f => f != null && f.CreateBy != subjectId && !subjectEntriesIds.Contains(f.Id))
                : rooms.Where(f => f != null && (f.CreateBy == subjectId || subjectEntriesIds.Contains(f.Id)));
        }

        return rooms;
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteTagLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string idStart) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.ThirdpartyIdMapping
                        .Where(t => t.TenantId == tenantId)
                        .Where(t => t.Id.StartsWith(idStart))
                        .Select(t => t.HashId).Any(h => h == r.EntryId))
                        .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, Task<int>>
        DeleteThirdpartyIdMappingsAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
                (FilesDbContext ctx, int tenantId, string idStart) =>
                    ctx.ThirdpartyIdMapping
                        .Where(r => r.TenantId == tenantId)
                        .Where(t => t.Id.StartsWith(idStart))
                        .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, string, Task<int>> DeleteSecuritiesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string idStart) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.ThirdpartyIdMapping
                        .Where(t => t.TenantId == tenantId)
                        .Where(t => t.Id.StartsWith(idStart))
                        .Select(t => t.HashId).Any(h => h == r.EntryId))
                        .ExecuteDelete());

    public static readonly Func<FilesDbContext, Task<int>> DeleteDbFilesTag =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                (from ft in ctx.Tag
                 join ftl in ctx.TagLink.DefaultIfEmpty() on new { ft.TenantId, ft.Id } equals new
                 {
                     ftl.TenantId,
                     Id = ftl.TagId
                 }
                 where ftl == null
                 select ft)
                .ExecuteDelete());
    
    public static readonly Func<FilesDbContext, int, string, FileEntryType, SubjectType, Task<bool>>
        SharedAsync =
            Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
                (FilesDbContext ctx, int tenantId, string entryId, FileEntryType entryType, SubjectType subjectType) =>
                    ctx.Security
                        .Where(t => t.TenantId == tenantId && t.EntryType == entryType && t.SubjectType == subjectType)
                        .Join(ctx.ThirdpartyIdMapping, s => s.EntryId, m => m.HashId, 
                            (s, m) => new { s, m.Id })
                        .Any(r => r.Id == entryId));
}