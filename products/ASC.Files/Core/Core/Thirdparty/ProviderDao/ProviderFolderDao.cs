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


namespace ASC.Files.Thirdparty.ProviderDao;

[Scope]
internal class ProviderFolderDao(SetupInfo setupInfo,
        IServiceProvider serviceProvider,
        TenantManager tenantManager,
        ISecurityDao<string> securityDao,
        CrossDao crossDao,
        GlobalFolderHelper globalFolderHelper,
        IProviderDao providerDao,
        IDbContextFactory<FilesDbContext> dbContextFactory,
        AuthContext authContext,
        SelectorFactory selectorFactory)
    : ProviderDaoBase(serviceProvider, tenantManager, crossDao, selectorFactory, securityDao), IFolderDao<string>
{
    public async Task<Folder<string>> GetFolderAsync(string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        if (selector == null)
        {
            return null;
        }

        var folderDao = selector.GetFolderDao(folderId);
        var result = await folderDao.GetFolderAsync(selector.ConvertId(folderId));

        return await ResolveParentAsync(result);
    }

    public async Task<Folder<string>> GetFolderAsync(string title, string parentId)
    {
        var selector = _selectorFactory.GetSelector(parentId);

        var folder = await selector.GetFolderDao(parentId).GetFolderAsync(title, selector.ConvertId(parentId));

        return await ResolveParentAsync(folder);
    }

    public async Task<Folder<string>> GetRootFolderAsync(string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);

        var folder = await folderDao.GetRootFolderAsync(selector.ConvertId(folderId));

        return await ResolveParentAsync(folder);
    }

    public async Task<Folder<string>> GetRootFolderByFileAsync(string fileId)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var folderDao = selector.GetFolderDao(fileId);

        var folder = await folderDao.GetRootFolderByFileAsync(selector.ConvertId(fileId));

        return await ResolveParentAsync(folder);
    }

    public IAsyncEnumerable<Folder<string>> GetRoomsAsync(IEnumerable<string> roomsIds, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText, bool withSubfolders, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, IEnumerable<int> parentsIds = null)
    {
        var result = AsyncEnumerable.Empty<Folder<string>>();

        foreach (var (selectorLocal, matchedIds) in _selectorFactory.GetSelectors(roomsIds))
        {
            if (selectorLocal == null)
            {
                continue;
            }

            result = result.Concat(matchedIds.GroupBy(selectorLocal.GetIdCode).ToAsyncEnumerable()
                .SelectMany(matchedId =>
                {
                    var folderDao = selectorLocal.GetFolderDao(matchedId.FirstOrDefault());

                    return folderDao.GetRoomsAsync(matchedId.Select(selectorLocal.ConvertId).ToList(), filterType, tags, subjectId, searchText, withSubfolders, withoutTags,
                        excludeSubject, provider, subjectFilter, subjectEntriesIds);
                })
                .Where(r => r != null))
                .SelectAwait(async r => await ResolveParentAsync(r));
        }

        result = FilterByProvider(result, provider);

        return result.Distinct();
    }

    public override async IAsyncEnumerable<Folder<string>> GetProviderBasedRoomsAsync(SearchArea searchArea, FilterType filterType, IEnumerable<string> tags, Guid subjectId,
        string searchText, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = filesDbContext.ThirdpartyAccount
            .Where(a => a.TenantId == tenantId && !string.IsNullOrEmpty(a.FolderId));

        var q1 = GetRoomsProvidersQuery(searchArea, filterType, tags, subjectId, searchText, withoutTags, excludeSubject, provider, subjectFilter, 
            subjectEntriesIds, q, filesDbContext, tenantId);

        var virtualRoomsFolderId = IdConverter.Convert<string>(await globalFolderHelper.GetFolderVirtualRooms());
        var archiveFolderId = IdConverter.Convert<string>(await globalFolderHelper.GetFolderArchive());

        await foreach (var queryItem in q1.ToAsyncEnumerable())
        {
            yield return ToProviderRoom(providerDao.ToProviderInfo(queryItem.Account), virtualRoomsFolderId, archiveFolderId, queryItem.Shared);
        }
    }

    public override async IAsyncEnumerable<Folder<string>> GetProviderBasedRoomsAsync(SearchArea searchArea, IEnumerable<string> roomsIds, FilterType filterType, IEnumerable<string> tags,
        Guid subjectId, string searchText, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = filesDbContext.ThirdpartyAccount
            .Where(a => a.TenantId == tenantId && !string.IsNullOrEmpty(a.FolderId) 
                                               && (a.UserId == authContext.CurrentAccount.ID || roomsIds.Contains(a.FolderId)));

        var q1 = GetRoomsProvidersQuery(searchArea, filterType, tags, subjectId, searchText, withoutTags, excludeSubject, provider, subjectFilter, 
            subjectEntriesIds, q, filesDbContext, tenantId);

        var virtualRoomsFolderId = IdConverter.Convert<string>(await globalFolderHelper.GetFolderVirtualRooms());
        var archiveFolderId = IdConverter.Convert<string>(await globalFolderHelper.GetFolderArchive());

        await foreach (var providerQuery in q1.ToAsyncEnumerable())
        {
            yield return ToProviderRoom(providerDao.ToProviderInfo(providerQuery.Account), virtualRoomsFolderId, archiveFolderId, providerQuery.Shared);
        }
    }

    public IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId, FolderType type)
    {
        return GetFoldersAsync(parentId);
    }
    
    public async IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId)
    {
        var selector = _selectorFactory.GetSelector(parentId);
        var folderDao = selector.GetFolderDao(parentId);
        var folders = folderDao.GetFoldersAsync(selector.ConvertId(parentId));

        await foreach (var folder in folders.Where(r => r != null))
        {
            yield return await ResolveParentAsync(folder);
    }
    }

    public async IAsyncEnumerable<Folder<string>> GetFoldersAsync(string parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText,
        bool withSubfolders = false, bool excludeSubject = false, int offset = 0, int count = -1, string roomId = default)
    {
        var selector = _selectorFactory.GetSelector(parentId);
        var folderDao = selector.GetFolderDao(parentId);
        var folders = folderDao.GetFoldersAsync(selector.ConvertId(parentId), orderBy, filterType, subjectGroup, subjectID, searchText, withSubfolders, excludeSubject);
        var result = await folders.Where(r => r != null).ToListAsync();

        foreach (var r in result)
        {
            yield return await ResolveParentAsync(r);
        }
    }

    public IAsyncEnumerable<Folder<string>> GetFoldersAsync(IEnumerable<string> folderIds, FilterType filterType = FilterType.None, bool subjectGroup = false, Guid? subjectID = null, string searchText = "", bool searchSubfolders = false, bool checkShare = true, bool excludeSubject = false)
    {
        var result = AsyncEnumerable.Empty<Folder<string>>();

        foreach (var (selectorLocal, matchedIds) in _selectorFactory.GetSelectors(folderIds))
        {
            if (selectorLocal == null)
            {
                continue;
            }

            result = result.Concat(matchedIds.GroupBy(selectorLocal.GetIdCode)
                .ToAsyncEnumerable()
                .SelectMany(matchedId =>
                {
                    var folderDao = selectorLocal.GetFolderDao(matchedId.FirstOrDefault());

                    return folderDao.GetFoldersAsync(matchedId.Select(selectorLocal.ConvertId).ToList(),
                        filterType, subjectGroup, subjectID, searchText, searchSubfolders, checkShare, excludeSubject);
                })
                .Where(r => r != null))
                .SelectAwait(async r => await ResolveParentAsync(r));
        }

        return result.Distinct();
    }

    public async IAsyncEnumerable<Folder<string>> GetParentFoldersAsync(string folderId)
    {
        if (string.IsNullOrEmpty(folderId))
        {
            yield break;
        }
        
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);

        await foreach (var folder in folderDao.GetParentFoldersAsync(selector.ConvertId(folderId)))
        {
            yield return await ResolveParentAsync(folder);
    }
    }

    public async Task<string> SaveFolderAsync(Folder<string> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if (folder.Id != null)
        {
            var folderId = folder.Id;
            var selector = _selectorFactory.GetSelector(folderId);
            folder.Id = selector.ConvertId(folderId);
            var folderDao = selector.GetFolderDao(folderId);
            var newFolderId = await folderDao.SaveFolderAsync(folder);
            folder.Id = folderId;

            return newFolderId;
        }
        
        if (folder.ParentId != null)
        {
            var folderId = folder.ParentId;
            var selector = _selectorFactory.GetSelector(folderId);
            folder.ParentId = selector.ConvertId(folderId);
            var folderDao = selector.GetFolderDao(folderId);
            var newFolderId = await folderDao.SaveFolderAsync(folder);
            folder.ParentId = folderId;

            return newFolderId;

        }

        throw new ArgumentException("No folder id or parent folder id to determine provider");
    }

    public async Task DeleteFolderAsync(string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);

        await folderDao.DeleteFolderAsync(selector.ConvertId(folderId));
    }

    public async Task<TTo> MoveFolderAsync<TTo>(string folderId, TTo toFolderId, CancellationToken? cancellationToken)
    {
        if (toFolderId is int tId)
        {
            return (TTo)Convert.ChangeType(await MoveFolderAsync(folderId, tId, cancellationToken), typeof(TTo));
        }

        if (toFolderId is string tsId)
        {
            return (TTo)Convert.ChangeType(await MoveFolderAsync(folderId, tsId, cancellationToken), typeof(TTo));
        }

        throw new NotImplementedException();
    }

    public async Task<string> MoveFolderAsync(string folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        if (IsCrossDao(folderId, toFolderId))
        {
            var newFolder = await PerformCrossDaoFolderCopyAsync(folderId, toFolderId, true, cancellationToken);

            return newFolder?.Id;
        }
        
        var folderDao = selector.GetFolderDao(folderId);

        return await folderDao.MoveFolderAsync(selector.ConvertId(folderId), selector.ConvertId(toFolderId), null);
    }

    public async Task<int> MoveFolderAsync(string folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        var newFolder = await PerformCrossDaoFolderCopyAsync(folderId, toFolderId, true, cancellationToken);

        return newFolder.Id;
    }

    public async Task<Folder<TTo>> CopyFolderAsync<TTo>(string folderId, TTo toFolderId, CancellationToken? cancellationToken)
    {
        return toFolderId switch
        {
            int tId => await CopyFolderAsync(folderId, tId, cancellationToken) as Folder<TTo>,
            string tsId => await ResolveParentAsync(await CopyFolderAsync(folderId, tsId, cancellationToken) as Folder<TTo>),
            _ => throw new NotImplementedException()
        };
        }

    public async Task<Folder<int>> CopyFolderAsync(string folderId, int toFolderId, CancellationToken? cancellationToken)
    {
        return await PerformCrossDaoFolderCopyAsync(folderId, toFolderId, false, cancellationToken);
    }

    public async Task<Folder<string>> CopyFolderAsync(string folderId, string toFolderId, CancellationToken? cancellationToken)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);

        return IsCrossDao(folderId, toFolderId)
                ? await PerformCrossDaoFolderCopyAsync(folderId, toFolderId, false, cancellationToken)
                : await folderDao.CopyFolderAsync(selector.ConvertId(folderId), selector.ConvertId(toFolderId), null);
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

    public Task<IDictionary<string, string>> CanMoveOrCopyAsync(IEnumerable<string> folderIds, int to)
    {
        return Task.FromResult((IDictionary<string, string>)new Dictionary<string, string>());
    }

    public Task<IDictionary<string, string>> CanMoveOrCopyAsync(IEnumerable<string> folderIds, string to)
    {
        if (!folderIds.Any())
        {
            return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());
        }

        var selector = _selectorFactory.GetSelector(to);
        var matchedIds = folderIds.Where(selector.IsMatch).ToArray();

        if (matchedIds.Length == 0)
        {
            return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());
        }

        return InternalCanMoveOrCopyAsync(to, matchedIds, selector);
    }

    private Task<IDictionary<string, string>> InternalCanMoveOrCopyAsync(string to, string[] matchedIds, IDaoSelector selector)
    {
        var folderDao = selector.GetFolderDao(matchedIds.FirstOrDefault());

        return folderDao.CanMoveOrCopyAsync(matchedIds, to);
    }
    public async Task<string> UpdateFolderAsync(Folder<string> folder, string newTitle, long newQuota)
    {
        return await RenameFolderAsync(folder, newTitle);
    }
    public async Task<string> RenameFolderAsync(Folder<string> folder, string newTitle)
    {
        var folderId = folder.Id;
        var selector = _selectorFactory.GetSelector(folderId);
        folder.Id = selector.ConvertId(folderId);
        folder.ParentId = selector.ConvertId(folder.ParentId);
        var folderDao = selector.GetFolderDao(folderId);

        var newId = await folderDao.RenameFolderAsync(folder, newTitle);
        folder.Id = folderId;
        folder.ParentId = folder.ParentId;
        
        return newId;
    }

    public async Task<int> GetItemsCountAsync(string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);

        return await folderDao.GetItemsCountAsync(selector.ConvertId(folderId));
    }

    public async Task<bool> IsEmptyAsync(string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);

        return await folderDao.IsEmptyAsync(selector.ConvertId(folderId));
    }

    public bool UseTrashForRemoveAsync(Folder<string> folder)
    {
        var selector = _selectorFactory.GetSelector(folder.Id);
        var folderDao = selector.GetFolderDao(folder.Id);

        return folderDao.UseTrashForRemoveAsync(folder);
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
        var selector = _selectorFactory.GetSelector(folderId);

        var folderDao = selector.GetFolderDao(folderId);
        var useRecursive = folderDao.UseRecursiveOperation(folderId, null);

        if (toRootFolderId != null)
        {
            var toFolderSelector = _selectorFactory.GetSelector(toRootFolderId);

            var folderDao1 = toFolderSelector.GetFolderDao(toRootFolderId);
            useRecursive = useRecursive && folderDao1.UseRecursiveOperation(folderId, toFolderSelector.ConvertId(toRootFolderId));
        }

        return useRecursive;
    }

    public bool CanCalculateSubitems(string entryId)
    {
        var selector = _selectorFactory.GetSelector(entryId);
        var folderDao = selector.GetFolderDao(entryId);

        return folderDao.CanCalculateSubitems(entryId);
    }

    public async Task<long> GetMaxUploadSizeAsync(string folderId, bool chunkedUpload = false)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);
        var storageMaxUploadSize = await folderDao.GetMaxUploadSizeAsync(selector.ConvertId(folderId), chunkedUpload);

        if (storageMaxUploadSize is -1 or long.MaxValue)
        {
            storageMaxUploadSize = setupInfo.ProviderMaxUploadSize;
        }

        return storageMaxUploadSize;
    }

    public async Task<IDataWriteOperator> CreateDataWriteOperatorAsync(
            string folderId,
            CommonChunkedUploadSession chunkedUploadSession,
            CommonChunkedUploadSessionHolder sessionHolder)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);
        return await folderDao.CreateDataWriteOperatorAsync(folderId, chunkedUploadSession, sessionHolder);
    }

    public async Task<string> GetBackupExtensionAsync(string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);
        return await folderDao.GetBackupExtensionAsync(folderId);
    }

    private static IAsyncEnumerable<Folder<string>> FilterByProvider(IAsyncEnumerable<Folder<string>> folders, ProviderFilter provider)
    {
        if (provider != ProviderFilter.kDrive && provider != ProviderFilter.WebDav && provider != ProviderFilter.Yandex)
        {
            return folders;
        }

        var providerKey = provider switch
        {
            ProviderFilter.Yandex => ProviderTypes.Yandex.ToStringFast(),
            ProviderFilter.WebDav => ProviderTypes.WebDav.ToStringFast(),
            ProviderFilter.kDrive => ProviderTypes.kDrive.ToStringFast(),
            _ => throw new NotImplementedException()
        };

        return folders.Where(x => providerKey == x.ProviderKey);
    }

    private static IQueryable<RoomProviderQuery> GetRoomsProvidersQuery(SearchArea searchArea, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText,
        bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, IQueryable<DbFilesThirdpartyAccount> q,
        FilesDbContext filesDbContext, int tenantId)
    {
        q = searchArea switch
        {
            SearchArea.Active => q.Where(a => a.FolderType == FolderType.VirtualRooms),
            SearchArea.Archive => q.Where(a => a.FolderType == FolderType.Archive),
            SearchArea.Any => q.Where(a => a.FolderType == FolderType.VirtualRooms || a.FolderType == FolderType.Archive),
            _ => q
        };

        if (provider != ProviderFilter.None)
        {
            var providers = GetProviderType(provider);

            q = q.Where(a => providers == a.Provider);
        }

        if (filterType is not (FilterType.None or FilterType.FoldersOnly))
        {
            var roomType = GetRoomFolderType(filterType);

            q = q.Where(a => a.RoomType == roomType);
        }

        if (subjectId != Guid.Empty)
        {
            q = subjectFilter switch
            {
                SubjectFilter.Owner => excludeSubject
                    ? q.Where(a => a != null && a.UserId != subjectId)
                    : q.Where(f => f != null && f.UserId == subjectId),
                SubjectFilter.Member => excludeSubject
                    ? q.Where(a => a != null && a.UserId != subjectId && !subjectEntriesIds.Contains(a.FolderId))
                    : q.Where(a => a != null && (a.UserId == subjectId || subjectEntriesIds.Contains(a.FolderId))),
                _ => q
            };
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            q = BuildSearch(q, searchText, SearchType.Any);
        }

        var q1 = from account in q
            join mapping in filesDbContext.ThirdpartyIdMapping on account.FolderId equals mapping.Id into result
            from mapping in result.DefaultIfEmpty()
            select new
            {
                Account = account,
                Hash = mapping.HashId
            };

        if (withoutTags)
        {
            q1 = q.Join(filesDbContext.ThirdpartyIdMapping, a => a.FolderId, m => m.Id, (a, m) => new { a, m.HashId })
                .Where(a => filesDbContext.TagLink.Join(filesDbContext.Tag, l => l.TagId, t => t.Id, (link, tag) => new { link.EntryId, tag })
                    .Where(r => r.tag.Type == TagType.Custom)
                    .Any(t => t.EntryId == a.HashId))
                .Select(r => new { Account = r.a, Hash = r.HashId });
    }
        else if (tags != null && tags.Any())
        {
            q1 = q.Join(filesDbContext.ThirdpartyIdMapping, f => f.FolderId, m => m.Id, (account, map) => new { account, map.HashId })
                .Join(filesDbContext.TagLink, r => r.HashId, t => t.EntryId, (result, tag) => new { result.account, result.HashId, tag.TagId })
                .Join(filesDbContext.Tag, r => r.TagId, t => t.Id, (result, tagInfo) => new { result.account, result.HashId, tagInfo.Name })
                .Where(r => tags.Contains(r.Name))
                .Select(r => new { Account = r.account, Hash = r.HashId });
        }

        var q2 = q1.Select(r => new RoomProviderQuery
    {
            Account = r.Account,
            Shared = filesDbContext.Security.Any(s => s.TenantId == tenantId && s.EntryType == FileEntryType.Folder && s.EntryId == r.Hash
                                                      && s.SubjectType == SubjectType.PrimaryExternalLink)
        });
        
        return q2;
    }

    private Folder<string> ToProviderRoom(IProviderInfo providerInfo, string roomsFolderId, string archiveFolderId, bool shared)
    {
        var rootId = providerInfo.RootFolderType == FolderType.VirtualRooms ? roomsFolderId : archiveFolderId;

        var folder = _serviceProvider.GetRequiredService<Folder<string>>();
        folder.Id = providerInfo.FolderId;
        folder.ParentId = rootId;
        folder.RootCreateBy = providerInfo.Owner;
        folder.CreateBy = providerInfo.Owner;
        folder.ProviderKey = providerInfo.ProviderKey;
        folder.RootId = rootId;
        folder.Title = providerInfo.CustomerTitle;
        folder.CreateOn = providerInfo.CreateOn;
        folder.FileEntryType = FileEntryType.Folder;
        folder.FolderType = providerInfo.FolderType;
        folder.ProviderId = providerInfo.ProviderId;
        folder.RootFolderType = providerInfo.RootFolderType;
        folder.SettingsHasLogo = providerInfo.HasLogo;
        folder.ModifiedBy = providerInfo.Owner;
        folder.ModifiedOn = providerInfo.ModifiedOn;
        folder.MutableId = providerInfo.MutableEntityId;
        folder.Shared = shared;
        folder.SettingsColor = providerInfo.Color;
        folder.ProviderMapped = !string.IsNullOrEmpty(providerInfo.FolderId);

        return folder;
    }
    public Task<FolderType> GetFirstParentTypeFromFileEntryAsync(FileEntry<string> entry)
    {
        throw new NotImplementedException();
    }
    public Task<(string RoomId, string RoomTitle)> GetParentRoomInfoFromFileEntryAsync(FileEntry<string> entry)
    {
        var selector = _selectorFactory.GetSelector(entry.Id);
        var folderDao = selector.GetFolderDao(entry.Id);

        return folderDao.GetParentRoomInfoFromFileEntryAsync(entry);
    }

    public async Task SetCustomOrder(string folderId, string parentFolderId, int order)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var folderDao = selector.GetFolderDao(folderId);
        await folderDao.SetCustomOrder(folderId, parentFolderId, order);
    }

    public async Task InitCustomOrder(IEnumerable<string> folderIds, string parentFolderId)
    {
        var selector = _selectorFactory.GetSelector(parentFolderId);
        var folderDao = selector.GetFolderDao(parentFolderId);
        await folderDao.InitCustomOrder(folderIds, parentFolderId);
    }

    private async ValueTask<Folder<T>> ResolveParentAsync<T>(Folder<T> folder)
    {
        if (folder == null)
        {
            return null;
        }
        
        if (!DocSpaceHelper.IsRoom(folder.FolderType))
        {
            return folder;
        }

        folder.FolderIdDisplay = folder.RootFolderType switch
        {
            FolderType.VirtualRooms => IdConverter.Convert<T>(await globalFolderHelper.FolderVirtualRoomsAsync),
            FolderType.Archive => IdConverter.Convert<T>(await globalFolderHelper.FolderArchiveAsync),
            _ => folder.FolderIdDisplay
        };

        return folder;
    }
    
    private class RoomProviderQuery
    {
        public DbFilesThirdpartyAccount Account { get; init; }
        public bool Shared { get; init; }
    }

    public Task<string> SetWatermarkSettings(WatermarkSettings watermarkSettings, Folder<string> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var selector = _selectorFactory.GetSelector(folder.Id);
        var folderDao = selector.GetFolderDao(folder.Id);

        return folderDao.SetWatermarkSettings(watermarkSettings, folder);
    }

    public async Task<WatermarkSettings> GetWatermarkSettings(Folder<string> room)
    {
        ArgumentNullException.ThrowIfNull(room);
        var selector = _selectorFactory.GetSelector(room.Id);
        var folderDao = selector.GetFolderDao(room.Id);
        
        return await folderDao.GetWatermarkSettings(room);
    }
    public Task<Folder<string>> DeleteWatermarkSettings(Folder<string> room)
    {
        ArgumentNullException.ThrowIfNull(room);
        var selector = _selectorFactory.GetSelector(room.Id);
        var folderDao = selector.GetFolderDao(room.Id);

        return folderDao.DeleteWatermarkSettings(room);
    }
}