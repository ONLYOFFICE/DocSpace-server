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

namespace ASC.Files.Core.Data;

internal abstract class BaseTagDao<T>(
    IDaoFactory daoFactory,
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider,
    IMapper mapper)
    : AbstractDao(dbContextManager,
        userManager,
        tenantManager,
        tenantUtil,
        setupInfo,
        maxTotalSizeStatistic,
        settingsManager,
        authContext,
        serviceProvider), ITagDao<T>
{
    public async IAsyncEnumerable<Tag> GetTagsAsync(Guid subject, TagType tagType, IEnumerable<FileEntry<T>> fileEntries)
    {
        var mapping = daoFactory.GetMapping<T>();
        var filesId = new HashSet<string>();
        var foldersId = new HashSet<string>();

        foreach (var f in fileEntries)
        {
            var id = await mapping.MappingIdAsync(f.Id);
            if (f.FileEntryType == FileEntryType.File)
            {
                filesId.Add(id);
            }
            else if (f.FileEntryType == FileEntryType.Folder)
            {
                foldersId.Add(id);
            }
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        List<TagLinkData> fromDb;
        await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var q = filesDbContext.TagsAsync(tenantId, tagType, filesId, foldersId);

            if (subject != Guid.Empty)
            {
                q = q.Where(r => r.Link.CreateBy == subject);
            }

            fromDb = await q.ToListAsync();
        }

        foreach (var e in fromDb)
        {
            yield return await ToTagAsync(e);
        }
    }

    public IAsyncEnumerable<Tag> GetTagsAsync(TagType tagType, IEnumerable<FileEntry<T>> fileEntries)
    {
        return GetTagsAsync(Guid.Empty, tagType, fileEntries);
    }

    public async IAsyncEnumerable<Tag> GetTagsAsync(T entryId, FileEntryType entryType, TagType? tagType)
    {
        var mapping = daoFactory.GetMapping<T>();
        var mappedId = await mapping.MappingIdAsync(entryId);

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        List<TagLinkData> fromDb;
        await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var q = filesDbContext.GetTagsByEntryTypeAsync(tenantId, tagType, entryType, mappedId);
            fromDb = await q.ToListAsync();
        }

        foreach (var e in fromDb)
        {
            yield return await ToTagAsync(e);
        }
    }

    public async IAsyncEnumerable<Tag> GetTagsAsync(Guid owner, TagType tagType)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        List<TagLinkData> fromDb;
        await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            fromDb = await filesDbContext.TagsByOwnerAsync(tenantId, tagType, owner).ToListAsync();
        }

        foreach (var e in fromDb)
        {
            yield return await ToTagAsync(e);
        }
    }

    public abstract IAsyncEnumerable<Tag> GetNewTagsAsync(Guid subject, Folder<T> parentFolder, bool deepSearch);

    public async IAsyncEnumerable<TagInfo> GetTagsInfoAsync(string searchText, TagType tagType, bool byName, int from = 0, int count = 0)
    {
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = (await Query(filesDbContext.Tag)).Where(r => r.Type == tagType);

        if (byName)
        {
            q = q.Where(r => r.Name == searchText);
        }
        else if (!string.IsNullOrEmpty(searchText))
        {
            var lowerText = searchText.ToLower().Trim().Replace("%", "\\%").Replace("_", "\\_");
            q = q.Where(r => r.Name.ToLower().Contains(lowerText));
        }

        if (count != 0)
        {
            q = q.Take(count);
        }

        q = q.Skip(from);

        await foreach (var tag in q.AsAsyncEnumerable())
        {
            yield return mapper.Map<DbFilesTag, TagInfo>(tag);
        }
    }

    public async IAsyncEnumerable<TagInfo> GetTagsInfoAsync(IEnumerable<string> names)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var q = filesDbContext.TagsInfoAsync(tenantId, names);

        await foreach (var tag in q)
        {
            yield return await ToTagAsyncInfo(tag);
        }
    }

    public async Task<TagInfo> SaveTagInfoAsync(TagInfo tagInfo)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var tagDb = mapper.Map<TagInfo, DbFilesTag>(tagInfo);
        tagDb.TenantId = tenantId;

        var tag = await filesDbContext.Tag.AddAsync(tagDb);
        await filesDbContext.SaveChangesAsync();

        return mapper.Map<DbFilesTag, TagInfo>(tag.Entity);
    }

    public async Task<IEnumerable<Tag>> SaveTagsAsync(IEnumerable<Tag> tags, Guid createdBy = default)
    {
        var result = new List<Tag>();

        if (tags == null)
        {
            return result;
        }

        tags = tags.Where(x => x != null && !x.EntryId.Equals(null) && !x.EntryId.Equals(0)).ToArray();

        if (!tags.Any())
        {
            return result;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId), TimeSpan.FromMinutes(5)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var internalFilesDbContext = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await internalFilesDbContext.Database.BeginTransactionAsync();

                await DeleteTagsBeforeSave(internalFilesDbContext, tenantId);
                var createOn = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
                var cachedTags = new Dictionary<string, DbFilesTag>();

                foreach (var t in tags)
                {
                    var key = GetCacheKey(t, tenantId);

                    if (cachedTags.ContainsKey(key))
                    {
                        continue;
                    }

                    var id = await internalFilesDbContext.TagIdAsync(t.Owner, t.Name, t.Type);

                    var toAdd = new DbFilesTag
                    {
                        Id = id,
                        Name = t.Name,
                        Owner = t.Owner,
                        Type = t.Type,
                        TenantId = tenantId
                    };

                    if (id == 0)
                    {
                        toAdd = internalFilesDbContext.Tag.Add(toAdd).Entity;
                        await internalFilesDbContext.SaveChangesAsync();
                    }

                    cachedTags.Add(key, toAdd);

                    var linkToInsert = new DbFilesTagLink
                    {
                        TenantId = tenantId,
                        TagId = toAdd.Id,
                        EntryId = int.TryParse(t.EntryId.ToString(), out var n) ? n.ToString() : (await MappingIdAsync(t.EntryId, true)).ToString(),
                        EntryType = t.EntryType,
                        CreateBy = createdBy != Guid.Empty ? createdBy : _authContext.CurrentAccount.ID,
                        CreateOn = createOn,
                        Count = t.Count
                    };

                    await internalFilesDbContext.TagLink.AddOrUpdateAsync(linkToInsert);
                    result.Add(t);
                }
                
                await internalFilesDbContext.SaveChangesAsync();
                await tx.CommitAsync();
            });
        }

        return result;
    }

    public async Task<IEnumerable<Tag>> SaveTagsAsync(Tag tag)
    {
        var result = new List<Tag>();

        if (tag == null)
        {
            return result;
        }

        if (tag.EntryId.Equals(null) || tag.EntryId.Equals(0))
        {
            return result;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await filesDbContext.Database.BeginTransactionAsync();

                await DeleteTagsBeforeSave(filesDbContext, tenantId);

                var createOn = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());
                var cacheTagId = new Dictionary<string, int>();

                result.Add(await SaveTagAsync(tag, cacheTagId, createOn));

                await tx.CommitAsync();
            });
        }
        
        return result;
    }

    private async Task DeleteTagsBeforeSave(FilesDbContext filesDbContext, int tenantId)
    {
        var date = _tenantUtil.DateTimeNow().AddMonths(-1);
        await filesDbContext.MustBeDeletedFilesAsync(tenantId, date);
        await filesDbContext.DeleteTagAsync();
    }

    private async Task<Tag> SaveTagAsync(Tag t, Dictionary<string, int> cacheTagId, DateTime createOn, Guid createdBy = default)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var cacheTagIdKey = string.Join("/", tenantId.ToString(), t.Owner.ToString(), t.Name, ((int)t.Type).ToString(CultureInfo.InvariantCulture));

        if (!cacheTagId.TryGetValue(cacheTagIdKey, out var id))
        {
            id = await filesDbContext.TagIdAsync(t.Owner, t.Name, t.Type);

            if (id == 0)
            {
                var toAdd = new DbFilesTag
                {
                    Id = 0,
                    Name = t.Name,
                    Owner = t.Owner,
                    Type = t.Type,
                    TenantId = tenantId
                };

                toAdd = filesDbContext.Tag.Add(toAdd).Entity;
                await filesDbContext.SaveChangesAsync();
                id = toAdd.Id;
            }

            cacheTagId.Add(cacheTagIdKey, id);
        }

        t.Id = id;

        var linkToInsert = new DbFilesTagLink
        {
            TenantId = tenantId,
            TagId = id,
            EntryId = (await MappingIdAsync(t.EntryId, true)).ToString(),
            EntryType = t.EntryType,
            CreateBy = createdBy != Guid.Empty ? createdBy : _authContext.CurrentAccount.ID,
            CreateOn = createOn,
            Count = t.Count
        };

        await filesDbContext.AddOrUpdateAsync(r => r.TagLink, linkToInsert);
        await filesDbContext.SaveChangesAsync();

        return t;
    }

    public async Task IncrementNewTagsAsync(IEnumerable<Tag> tags, Guid createdBy = default)
    {
        if (tags == null || !tags.Any())
        {
            return;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId), TimeSpan.FromMinutes(5)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var internalFilesDbContext = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await internalFilesDbContext.Database.BeginTransactionAsync();

                var createOn = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());

                foreach (var tagsGroup in tags.GroupBy(t => new { t.EntryId, t.EntryType }))
                {
                    var mappedId = (await MappingIdAsync(tagsGroup.Key.EntryId)).ToString();
                    
                    await filesDbContext.IncrementNewTagsAsync(tenantId, tagsGroup.Select(t => t.Id), tagsGroup.Key.EntryType, mappedId, createdBy, createOn);
                }

                await tx.CommitAsync();
            });
        }
    }

    public async Task UpdateNewTags(IEnumerable<Tag> tags, Guid createdBy = default)
    {
        if (tags == null || !tags.Any())
        {
            return;
        }

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId), TimeSpan.FromMinutes(5)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await filesDbContext.Database.BeginTransactionAsync();

                var createOn = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());

                foreach (var tag in tags)
                {
                    await UpdateNewTagsInDbAsync(tag, createOn, createdBy);
                }

                await tx.CommitAsync();
            });
        }
    }

    public async Task UpdateNewTags(Tag tag)
    {
        if (tag == null)
        {
            return;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId)))
        {
            var createOn = _tenantUtil.DateTimeToUtc(_tenantUtil.DateTimeNow());

            await UpdateNewTagsInDbAsync(tag, createOn);
        }
    }

    private async ValueTask UpdateNewTagsInDbAsync(Tag tag, DateTime createOn, Guid createdBy = default)
    {
        if (tag == null)
        {
            return;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var mappedId = (await MappingIdAsync(tag.EntryId)).ToString();
        var tagId = tag.Id;
        var tagEntryType = tag.EntryType;

        await filesDbContext.UpdateTagLinkAsync(tenantId, tagId, tagEntryType, mappedId,
            createdBy != Guid.Empty ? createdBy : _authContext.CurrentAccount.ID,
            createOn, tag.Count);
    }

    public async Task RemoveTagsAsync(Tag tag)
    {
        if (tag == null)
        {
            return;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        
        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await dbContext.Database.BeginTransactionAsync();
                await RemoveTagInDbAsync(tag);

                await tx.CommitAsync();
            });
        }
    }

    public async Task RemoveTagsAsync(FileEntry<T> entry, IEnumerable<int> tagsIds)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var entryId = await daoFactory.GetMapping<T>().MappingIdAsync(entry.Id);
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await filesDbContext.DeleteTagLinksAsync(tenantId, tagsIds, entryId, entry.FileEntryType);

        var any = await filesDbContext.AnyTagLinkByIdsAsync(tenantId, tagsIds);
        if (!any)
        {
            await filesDbContext.DeleteTagsByIdsAsync(tenantId, tagsIds);
        }
    }

    public async Task RemoveTagsAsync(IEnumerable<Tag> tags)
    {
        if (tags == null || !tags.Any())
        {
            return;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using (await distributedLockProvider.TryAcquireLockAsync(GetLockKey(tenantId), TimeSpan.FromMinutes(5)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var strategy = filesDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var ctx = await _dbContextFactory.CreateDbContextAsync();
                await using var tx = await ctx.Database.BeginTransactionAsync();

                foreach (var t in tags)
                {
                    await RemoveTagInDbAsync(t);
                }

                await tx.CommitAsync();
            });
        }
    }

    public async Task<int> RemoveTagLinksAsync(T entryId, FileEntryType entryType, TagType tagType)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var mappedId = await daoFactory.GetMapping<T>().MappingIdAsync(entryId);

        return await filesDbContext.DeleteTagLinksByEntryIdAsync(tenantId, mappedId, entryType, tagType);

    }

    private async ValueTask RemoveTagInDbAsync(Tag tag)
    {
        if (tag == null)
        {
            return;
        }
        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var id = await filesDbContext.FirstTagIdAsync(tenantId, tag.Owner, tag.Name, tag.Type);

        if (id != 0)
        {
            var entryId = (tag.EntryId is int fid ? fid : await MappingIdAsync(tag.EntryId)).ToString();

            await filesDbContext.DeleteTagLinksByTagIdAsync(tenantId, id, entryId, tag.EntryType);

            var any = await filesDbContext.AnyTagLinkByIdAsync(tenantId, id);
            if (!any)
            {
                await filesDbContext.DeleteTagByIdAsync(tenantId, id);
            }
        }
    }

    public IAsyncEnumerable<Tag> GetNewTagsAsync(Guid subject, FileEntry<T> fileEntry)
    {
        return GetNewTagsAsync(subject, new List<FileEntry<T>>(1) { fileEntry });
    }

    public async IAsyncEnumerable<Tag> GetNewTagsAsync(Guid subject, IEnumerable<FileEntry<T>> fileEntries)
    {
        var entryIds = new HashSet<string>();
        var entryTypes = new HashSet<int>();

        foreach (var r in fileEntries)
        {
            var id = await daoFactory.GetMapping<T>().MappingIdAsync(r.Id);
            var entryType = (r.FileEntryType == FileEntryType.File) ? FileEntryType.File : FileEntryType.Folder;

            entryIds.Add(id);
            entryTypes.Add((int)entryType);
        }

        if (entryIds.Count > 0)
        {
            var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
            List<TagLinkData> fromDb;
            await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                fromDb = await filesDbContext.TagLinkDataAsync(tenantId, entryIds, entryTypes, subject).ToListAsync();
            }
            
            foreach (var e in fromDb)
            {
                yield return await ToTagAsync(e);
            }
        }
    }

    protected async ValueTask<Tag> ToTagAsync(TagLinkData r)
    {
        var result = mapper.Map<DbFilesTag, Tag>(r.Tag);
        mapper.Map(r.Link, result);

        result.EntryId = await MappingIdAsync(r.Link.EntryId);

        return result;
    }
    
    protected async ValueTask<TagInfo> ToTagAsyncInfo(TagLinkData r)
    {
        var result = mapper.Map<DbFilesTag, TagInfo>(r.Tag);
        mapper.Map(r.Tag, result);

        if(r.Link != null)
        {
            result.EntryId = await MappingIdAsync(r.Link.EntryId);
            result.EntryType = r.Link.EntryType;
        }
        return result;
    }
    private string GetCacheKey(Tag tag, int tenantId)
    {
        return string.Join("/", tenantId.ToString(), tag.Owner.ToString(), tag.Name, ((int)tag.Type).ToString(CultureInfo.InvariantCulture));
    }

    private static string GetLockKey(int tenantId)
    {
        return $"tags_{tenantId}";
    }
    
    protected ValueTask<object> MappingIdAsync(object id, bool saveIfNotExist = false)
    {
        if (id == null)
        {
            return ValueTask.FromResult<object>(null);
        }

        var isNumeric = int.TryParse(id.ToString(), out var n);

        if (isNumeric)
        {
            return ValueTask.FromResult<object>(n);
        }

        return InternalMappingIdAsync(id, saveIfNotExist);
    }

    private async ValueTask<object> InternalMappingIdAsync(object id, bool saveIfNotExist = false)
    {
        object result;

        var sId = id.ToString();
        if (Selectors.All.Exists(s => sId.StartsWith(s.Id)))
        {
            result = Regex.Replace(BitConverter.ToString(Hasher.Hash(id.ToString(), HashAlg.MD5)), "-", "").ToLower();
        }
        else
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
            result = await filesDbContext.IdAsync(tenantId, id.ToString());
        }

        if (saveIfNotExist)
        {
            var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
            
            var newItem = new DbFilesThirdpartyIdMapping
            {
                Id = id.ToString(),
                HashId = result.ToString(),
                TenantId = tenantId
            };

            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
            await filesDbContext.AddOrUpdateAsync(r => r.ThirdpartyIdMapping, newItem);
            await filesDbContext.SaveChangesAsync();
        }

        return result;
    }
}


[Scope(typeof(ITagDao<int>))]
internal class TagDao(
    IDaoFactory daoFactory,
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IMapper mapper,
    IDistributedLockProvider distributedLockProvider)
    : BaseTagDao<int>(
        daoFactory,
        userManager,
          dbContextManager,
          tenantManager,
          tenantUtil,
          setupInfo,
          maxTotalSizeStatistic,
          settingsManager,
          authContext,
          serviceProvider,
          distributedLockProvider,
          mapper)
    {
    public override IAsyncEnumerable<Tag> GetNewTagsAsync(Guid subject, Folder<int> parentFolder, bool deepSearch)
    {
        if (parentFolder == null || EqualityComparer<int>.Default.Equals(parentFolder.Id, 0))
        {
            throw new ArgumentException("folderId");
        }

        return InternalGetNewTagsAsync(subject, parentFolder, deepSearch);
    }

    private async IAsyncEnumerable<Tag> InternalGetNewTagsAsync(Guid subject, Folder<int> parentFolder, bool deepSearch)
    {            
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var tempTags = AsyncEnumerable.Empty<TagLinkData>();
        var monitorFolderIds = new List<object> { parentFolder.Id };
        
        List<TagLinkData> fromDb;
        await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            if (parentFolder.FolderType == FolderType.SHARE)
            {
                tempTags = tempTags.Concat(filesDbContext.TmpShareFileTagsAsync(tenantId, subject, FolderType.USER));
                tempTags = tempTags.Concat(filesDbContext.TmpShareFolderTagsAsync(tenantId, subject, FolderType.USER));
                tempTags = tempTags.Concat(filesDbContext.TmpShareSBoxTagsAsync(tenantId, subject, Selectors.All.Select(s => s.Id).ToList()));
            }
            else if (parentFolder.FolderType == FolderType.Privacy)
            {
                tempTags = tempTags.Concat(filesDbContext.TmpShareFileTagsAsync(tenantId, subject, FolderType.Privacy));
                tempTags = tempTags.Concat(filesDbContext.TmpShareFolderTagsAsync(tenantId, subject, FolderType.Privacy));
            }
            else if (parentFolder.FolderType == FolderType.Projects)
            {
                tempTags = tempTags.Concat(filesDbContext.ProjectsAsync(tenantId, subject));
            }

            fromDb = await tempTags.ToListAsync();
        }
        

        foreach (var e in fromDb)
        {
            var tag = await ToTagAsync(e);
            yield return tag;

            if (tag.EntryType == FileEntryType.Folder)
            {
                monitorFolderIds.Add(tag.EntryId);
            }
        }


        var monitorFolderIdsInt = monitorFolderIds.OfType<int>().ToList();
        List<TagLinkData> result;
        
        await using (var filesDbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var subFoldersSqlQuery = filesDbContext.FolderAsync(monitorFolderIdsInt, deepSearch);

            monitorFolderIds.AddRange(await subFoldersSqlQuery.Select(r => (object)r).ToListAsync());

            var monitorFolderIdsStrings = monitorFolderIds.Select(r => r.ToString()).ToList();
            
            result = await filesDbContext.NewTagsForFoldersAsync(tenantId, subject, monitorFolderIdsStrings).ToListAsync();

            var where = (deepSearch ? monitorFolderIds : [parentFolder.Id])
                .Select(r => r.ToString())
                .ToList();

            result.AddRange(await filesDbContext.NewTagsForFilesAsync(tenantId, subject, where).ToListAsync());

            if (parentFolder.FolderType is FolderType.USER or FolderType.COMMON)
            {
                var folderIds = await filesDbContext.ThirdpartyAccountAsync(tenantId, parentFolder.FolderType, subject).ToListAsync();

                var thirdpartyFolderIds = folderIds.ConvertAll(r => $"{Selectors.WebDav.Id}-" + r)
                    .Concat(folderIds.ConvertAll(r => $"{Selectors.Box.Id}-{r}"))
                    .Concat(folderIds.ConvertAll(r => $"{Selectors.Dropbox.Id}-{r}"))
                    .Concat(folderIds.ConvertAll(r => $"{Selectors.SharePoint.Id}-{r}"))
                    .Concat(folderIds.ConvertAll(r => $"{Selectors.GoogleDrive.Id}-{r}"))
                    .Concat(folderIds.ConvertAll(r => $"{Selectors.OneDrive.Id}-{r}"))
                    .ToList();

                if (thirdpartyFolderIds.Count > 0)
                {
                    result.AddRange(await filesDbContext.NewTagsForSBoxAsync(tenantId, subject, thirdpartyFolderIds).ToListAsync());
                }
            }

            if (parentFolder.FolderType == FolderType.VirtualRooms)
            {
                result.AddRange(await filesDbContext.NewTagsThirdpartyRoomsAsync(tenantId, subject).ToListAsync());
            }
        }

        foreach (var e in result)
        {
            yield return await ToTagAsync(e);
        }
    }
}

[Scope(typeof(ITagDao<string>))]
internal class ThirdPartyTagDao(
    IDaoFactory daoFactory,
    UserManager userManager,
        IDbContextFactory<FilesDbContext> dbContextManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        IMapper mapper,
        IThirdPartyTagDao thirdPartyTagDao,
        IDistributedLockProvider distributedLockProvider)
    : BaseTagDao<string>(
        daoFactory,
        userManager,
          dbContextManager,
          tenantManager,
          tenantUtil,
          setupInfo,
          maxTotalSizeStatistic,
          settingsManager,
          authContext,
          serviceProvider,
          distributedLockProvider,
          mapper)
    {
    public override IAsyncEnumerable<Tag> GetNewTagsAsync(Guid subject, Folder<string> parentFolder, bool deepSearch)
    {
        return thirdPartyTagDao.GetNewTagsAsync(subject, parentFolder, deepSearch);
    }
}

public class TagLinkData
{
    public DbFilesTag Tag { get; init; }
    public DbFilesTagLink Link { get; init; }
}