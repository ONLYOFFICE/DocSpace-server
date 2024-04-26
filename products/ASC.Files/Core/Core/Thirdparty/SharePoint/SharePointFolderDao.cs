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

using File = Microsoft.SharePoint.Client.File;
using Folder = Microsoft.SharePoint.Client.Folder;

namespace ASC.Files.Thirdparty.SharePoint;

[Scope]
internal class SharePointFolderDao(IServiceProvider serviceProvider,
        UserManager userManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        IDbContextFactory<FilesDbContext> dbContextManager,
        SetupInfo setupInfo,
        FileUtility fileUtility,
        CrossDao crossDao,
        SharePointDaoSelector sharePointDaoSelector,
        IFileDao<int> fileDao,
        IFolderDao<int> folderDao,
        TempPath tempPath,
        RegexDaoSelectorBase<File, Folder, ClientObject> regexDaoSelectorBase)
    : SharePointDaoBase(serviceProvider, userManager, tenantManager, tenantUtil, dbContextManager, setupInfo,
        fileUtility, tempPath, regexDaoSelectorBase), IFolderDao<string>
    {
        private readonly TenantManager _tenantManager1 = tenantManager;

        public async Task<Folder<string>> GetFolderAsync(string folderId, bool includeRemoved = false)
    {
        
        var folder = SharePointProviderInfo.ToFolder(await SharePointProviderInfo.GetFolderByIdAsync(folderId));
        
        if (folder.FolderType is not (FolderType.CustomRoom or FolderType.PublicRoom))
        {
            return folder;
        }
        
        var tenantId = await _tenantManager1.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        folder.Shared = await Queries.SharedAsync(filesDbContext, tenantId, folder.Id, FileEntryType.Folder, SubjectType.PrimaryExternalLink);

        return folder;
    }

    public async Task<Folder<string>> GetFolderAsync(string title, string parentId)
    {
        var folderFolders = await SharePointProviderInfo.GetFolderFoldersAsync(parentId);

        return SharePointProviderInfo.ToFolder(folderFolders
                    .FirstOrDefault(item => item.Name.Equals(title, StringComparison.InvariantCultureIgnoreCase)));
    }

    public Task<Folder<string>> GetRootFolderAsync(string folderId)
    {
        return Task.FromResult(SharePointProviderInfo.ToFolder(SharePointProviderInfo.RootFolder));
    }

    public Task<Folder<string>> GetRootFolderByFileAsync(string fileId)
    {
        return Task.FromResult(SharePointProviderInfo.ToFolder(SharePointProviderInfo.RootFolder));
    }

    public async IAsyncEnumerable<Folder<string>> GetRoomsAsync(IEnumerable<string> roomsIds, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText, bool withSubfolders, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, IEnumerable<int> parentsIds = null)
    {
        if (CheckInvalidFilter(filterType) || (provider != ProviderFilter.None && provider != SharePointProviderInfo.ProviderFilter))
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

        var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
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
    public async IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId, bool includeRemoved = false)
    {
        var folderFolders = await SharePointProviderInfo.GetFolderFoldersAsync(parentId);

        foreach (var i in folderFolders)
        {
            yield return SharePointProviderInfo.ToFolder(i);
        }
    }

    public IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, 
        bool withSubfolders = false, bool excludeSubject = false, int offset = 0, int count = -1, string roomId = default, bool containingMyFiles = false)
    {
        if (CheckInvalidFilter(filterType))
        {
            return AsyncEnumerable.Empty<Folder<string>>();
        }

        var folders = GetFoldersAsync(parentId);

        //Filter
        if (subjectID != Guid.Empty)
        {
            folders = folders.WhereAwait(async x => subjectGroup
                                             ? await _userManager.IsUserInGroupAsync(x.CreateBy, subjectID)
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
        if (CheckInvalidFilter(filterType))
        {
            return AsyncEnumerable.Empty<Folder<string>>();
        }

        var folders = folderIds.ToAsyncEnumerable().SelectAwait(async e => await GetFolderAsync(e));

        if (subjectID.HasValue && subjectID != Guid.Empty)
        {
            folders = folders.WhereAwait(async x => subjectGroup
                                             ? await _userManager.IsUserInGroupAsync(x.CreateBy, subjectID.Value)
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
        var folder = await SharePointProviderInfo.GetFolderByIdAsync(folderId);
        if (folder != null)
        {
            do
            {
                path.Add(SharePointProviderInfo.ToFolder(folder));
            } while (folder != SharePointProviderInfo.RootFolder && folder is not SharePointFolderErrorEntry &&
                     (folder = await SharePointProviderInfo.GetParentFolderAsync(folder.ServerRelativeUrl)) != null);
        }
        path.Reverse();

        await foreach (var p in path.ToAsyncEnumerable())
        {
            yield return p;
        }
    }

    public async Task<string> SaveFolderAsync(Folder<string> folder)
    {
        if (folder.Id != null)
        {
            //Create with id
            var savedfolder = await SharePointProviderInfo.CreateFolderAsync(folder.Id);

            return SharePointProviderInfo.ToFolder(savedfolder).Id;
        }

        if (folder.ParentId != null)
        {
            var parentFolder = await SharePointProviderInfo.GetFolderByIdAsync(folder.ParentId);

            folder.Title = await GetAvailableTitleAsync(folder.Title, parentFolder, IsExistAsync);

            var newFolder = await SharePointProviderInfo.CreateFolderAsync(parentFolder.ServerRelativeUrl + "/" + folder.Title);

            return SharePointProviderInfo.ToFolder(newFolder).Id;
        }

        return null;
    }

    public async Task<bool> IsExistAsync(string title, Folder folder)
    {
        var folderFolders = await SharePointProviderInfo.GetFolderFoldersAsync(folder.ServerRelativeUrl);

        return folderFolders.Any(item => item.Name.Equals(title, StringComparison.InvariantCultureIgnoreCase));
    }

    public async Task DeleteFolderAsync(string folderId)
    {
        var folder = await SharePointProviderInfo.GetFolderByIdAsync(folderId);
        var tenantId = await _tenantManager1.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();
            var link = await Queries.TagLinksAsync(dbContext, tenantId, folder.ServerRelativeUrl).ToListAsync();

            dbContext.TagLink.RemoveRange(link);
            await dbContext.SaveChangesAsync();

            var tagsToRemove = await Queries.TagsAsync(dbContext).ToListAsync();

            dbContext.Tag.RemoveRange(tagsToRemove);

            var securityToDelete = await Queries.SecuritiesAsync(dbContext, tenantId, folder.ServerRelativeUrl).ToListAsync();

            dbContext.Security.RemoveRange(securityToDelete);
            await dbContext.SaveChangesAsync();

            var mappingToDelete = await Queries.ThirdpartyIdMappingsAsync(dbContext, tenantId, folder.ServerRelativeUrl).ToListAsync();

            dbContext.ThirdpartyIdMapping.RemoveRange(mappingToDelete);
            await dbContext.SaveChangesAsync();

            await tx.CommitAsync();
        });


        await SharePointProviderInfo.DeleteFolderAsync(folderId);
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
                folderId, this, sharePointDaoSelector.GetFileDao(folderId), sharePointDaoSelector.ConvertId,
                toFolderId, folderDao, fileDao, r => r,
                true, cancellationToken)
            ;

        return moved.Id;
    }

    public async Task<string> MoveFolderAsync(string folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var newFolderId = await SharePointProviderInfo.MoveFolderAsync(folderId, toFolderId);
        await UpdatePathInDBAsync(SharePointProviderInfo.MakeId(folderId), newFolderId);

        return newFolderId;
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
        return SharePointProviderInfo.ToFolder(await SharePointProviderInfo.CopyFolderAsync(folderId, toFolderId));
    }

    public async Task<Folder<int>> CopyFolderAsync(string folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        var moved = await crossDao.PerformCrossDaoFolderCopyAsync(
            folderId, this, sharePointDaoSelector.GetFileDao(folderId), sharePointDaoSelector.ConvertId,
            toFolderId, folderDao, fileDao, r => r,
            false, cancellationToken)
            ;

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
        var oldId = SharePointProviderInfo.MakeId(folder.Id);
        var newFolderId = oldId;
        if (SharePointProviderInfo.SpRootFolderId.Equals(folder.Id))
        {
            //It's root folder
            await DaoSelector.RenameProviderAsync(SharePointProviderInfo, newTitle);
            //rename provider customer title
        }
        else
        {
            newFolderId = (string)await SharePointProviderInfo.RenameFolderAsync(folder.Id, newTitle);

            if (DocSpaceHelper.IsRoom(SharePointProviderInfo.FolderType) && SharePointProviderInfo.FolderId != null)
            {
                await DaoSelector.RenameRoomProviderAsync(SharePointProviderInfo, newTitle, newFolderId);
            }
        }

        await UpdatePathInDBAsync(oldId, newFolderId);

        return newFolderId;
    }


    public Task<int> GetItemsCountAsync(string folderId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsEmptyAsync(string folderId)
    {
        var folder = await SharePointProviderInfo.GetFolderByIdAsync(folderId);

        return folder.ItemCount == 0;
    }

    public bool UseTrashForRemoveAsync(Folder<string> folder)
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

    public bool UseRecursiveOperation(string folderId, string toRootFolderId)
    {
        return false;
    }

    public bool CanCalculateSubitems(string entryId)
    {
        return false;
    }

    public Task<long> GetMaxUploadSizeAsync(string folderId, bool chunkedUpload = false)
    {
        return Task.FromResult(2L * 1024L * 1024L * 1024L);
    }

    public Task<IDataWriteOperator> CreateDataWriteOperatorAsync(
            string folderId,
            CommonChunkedUploadSession chunkedUploadSession,
            CommonChunkedUploadSessionHolder sessionHolder)
    {
        return Task.FromResult<IDataWriteOperator>(null);
    }

    public Task<string> GetBackupExtensionAsync(string folderId)
    {
        return Task.FromResult("tar.gz");
    }

    public Task<(string RoomId, string RoomTitle)> GetParentRoomInfoFromFileEntryAsync(FileEntry<string> entry)
    {
        return Task.FromResult(entry.RootFolderType is not (FolderType.VirtualRooms or FolderType.Archive) 
            ? (string.Empty, string.Empty) 
            : (ProviderInfo.FolderId, ProviderInfo.CustomerTitle));
    }

    public Task<FolderType> GetFirstParentTypeFromFileEntryAsync(FileEntry<string> entry)
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

    public Task MarkFoldersAsRemovedAsync(IEnumerable<string> folderIds)
    {
        return Task.CompletedTask;
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesTagLink>> TagLinksAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string idStart) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.ThirdpartyIdMapping
                        .Where(m => m.TenantId == tenantId)
                        .Where(m => m.Id.StartsWith(idStart))
                        .Select(m => m.HashId).Any(h => h == r.EntryId)));

    public static readonly Func<FilesDbContext, IAsyncEnumerable<DbFilesTag>> TagsAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                from ft in ctx.Tag
                join ftl in ctx.TagLink.DefaultIfEmpty() on new { ft.TenantId, ft.Id } equals new
                {
                    ftl.TenantId,
                    Id = ftl.TagId
                }
                where ftl == null
                select ft);

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesSecurity>> SecuritiesAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string idStart) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.ThirdpartyIdMapping
                        .Where(m => m.TenantId == tenantId)
                        .Where(m => m.Id.StartsWith(idStart))
                        .Select(m => m.HashId).Any(h => h == r.EntryId)));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesThirdpartyIdMapping>>
        ThirdpartyIdMappingsAsync =
            EF.CompileAsyncQuery(
                (FilesDbContext ctx, int tenantId, string idStart) =>
                    ctx.ThirdpartyIdMapping
                        .Where(r => r.TenantId == tenantId)
                        .Where(m => m.Id.StartsWith(idStart)));
    
    public static readonly Func<FilesDbContext, int, string, FileEntryType, SubjectType, Task<bool>>
        SharedAsync =
            EF.CompileAsyncQuery(
                (FilesDbContext ctx, int tenantId, string entryId, FileEntryType entryType, SubjectType subjectType) =>
                    ctx.Security
                        .Where(t => t.TenantId == tenantId && t.EntryType == entryType && t.SubjectType == subjectType)
                        .Join(ctx.ThirdpartyIdMapping, s => s.EntryId, m => m.HashId, 
                            (s, m) => new { s, m.Id })
                        .Any(r => r.Id == entryId));
}