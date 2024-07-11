﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Thirdparty;

internal abstract class ThirdPartyProviderDao
{
    #region FileDao

    public Task ReassignFilesAsync(Guid oldOwner, Guid newOwnerId, IEnumerable<string> exceptFolderIds)
    {
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<File<string>> GetFilesAsync(IEnumerable<string> parentIds, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, string[] extension, 
        bool searchInContent)
    {
        return AsyncEnumerable.Empty<File<string>>();
    }

    public IAsyncEnumerable<File<string>> SearchAsync(string text, bool bunch)
    {
        return null;
    }

    public Task<bool> IsExistOnStorageAsync(File<string> file)
    {
        return Task.FromResult(true);
    }

    public Task SaveEditHistoryAsync(File<string> file, string changes, Stream differenceStream)
    {
        //Do nothing
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<EditHistory> GetEditHistoryAsync(DocumentServiceHelper documentServiceHelper, string fileId, int fileVersion)
    {
        return null;
    }

    public Task<Stream> GetDifferenceStreamAsync(File<string> file)
    {
        return Task.FromResult<Stream>(null);
    }

    public Task<bool> ContainChangesAsync(string fileId, int fileVersion)
    {
        return Task.FromResult(false);
    }

    public string GetUniqThumbnailPath(File<string> file, int width, int height)
    {
        //Do nothing
        return null;
    }

    public Task SetThumbnailStatusAsync(File<string> file, Thumbnail status)
    {
        return Task.CompletedTask;
    }

    public virtual Task<Stream> GetThumbnailAsync(File<string> file, int width, int height)
    {
        return GetThumbnailAsync(file.Id, width, height);
    }

    public virtual Task<Stream> GetThumbnailAsync(string file, int width, int height)
    {
        return Task.FromResult<Stream>(null);
    }

    public Task<EntryProperties> GetProperties(string fileId)
    {
        return Task.FromResult<EntryProperties>(null);
    }

    public Task SaveProperties(string fileId, EntryProperties entryProperties)
    {
        return Task.CompletedTask;
    }

    public virtual Task<Stream> GetFileStreamAsync(File<string> file)
    {
        return Task.FromResult<Stream>(null);
    }

    public string GetUniqFilePath(File<string> file, string fileTitle)
    {
        throw new NotImplementedException();
    }

    #endregion
    #region FolderDao

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
    
    public IAsyncEnumerable<OriginData> GetOriginsDataAsync(IEnumerable<string> entriesId)
    {
        throw new NotImplementedException();
    }
    
    public Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        throw new NotImplementedException();
    }    
    public Task<int> GetFilesCountAsync(string parentId, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText, string[] extension, bool searchInContent, bool withSubfolders = false,
        bool excludeSubject = false, string roomId = default)
    {
        throw new NotImplementedException();
    }
    
    public Task<int> GetFoldersCountAsync(string parentId, FilterType filterType, bool subjectGroup, Guid subjectId, string searchText, bool withSubfolders = false, bool excludeSubject = false, 
        string roomId = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<File<string>> GetFilesByTagAsync(Guid? tagOwner, TagType tagType, FilterType filterType, bool subjectGroup, Guid subjectId,
        string searchText, string[] extension, bool searchInContent, bool excludeSubject, OrderBy orderBy, int offset = 0, int count = -1)
    {
        return AsyncEnumerable.Empty<File<string>>();
    }

    public Task<int> GetFilesByTagCountAsync(Guid? tagOwner, TagType tagType, FilterType filterType, bool subjectGroup, Guid subjectId,
        string searchText, string[] extension, bool searchInContent, bool excludeSubject)
    {
        return default;
    }

    public IAsyncEnumerable<Folder<string>> GetRoomsAsync(IEnumerable<string> parentsIds, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText,
        bool withSubfolders, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, QuotaFilter quotaFilter)
    {
        return AsyncEnumerable.Empty<Folder<string>>();
    }

    public virtual IAsyncEnumerable<Folder<string>> GetProviderBasedRoomsAsync(SearchArea searchArea, FilterType filterType, IEnumerable<string> tags, Guid subjectId, string searchText, 
        bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<string>>();
    }

    public virtual IAsyncEnumerable<Folder<string>> GetProviderBasedRoomsAsync(SearchArea searchArea, IEnumerable<string> roomsIds, FilterType filterType, IEnumerable<string> tags,
        Guid subjectId, string searchText, bool withoutTags, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntriesIds)
    {
        return AsyncEnumerable.Empty<Folder<string>>();
    }
    public Task<string> ChangeTreeFolderSizeAsync(string folderId, long size)
    {
        throw new NotImplementedException();
    }
    public Task<string> ChangeFolderQuotaAsync(Folder<string> folder, long quota)
    {
        throw new NotImplementedException();
    }

    protected static IAsyncEnumerable<Folder<string>> FilterRoomsAsync(IAsyncEnumerable<Folder<string>> rooms, ProviderFilter provider, FilterType filterType, Guid subjectId,
        bool excludeSubject, SubjectFilter subjectFilter, IEnumerable<string> subjectEntriesIds, string searchText, bool withoutTags, IEnumerable<string> tags,
        FilesDbContext filesDbContext)
    {
        rooms = FilterByProvidersAsync(rooms, provider);
        rooms = FilterByRoomType(rooms, filterType);
        rooms = FilterBySubject(rooms, subjectId, excludeSubject, subjectFilter, subjectEntriesIds);
        rooms = FilterByTitle(rooms, searchText);
        rooms = FilterByTags(rooms, withoutTags, tags, filesDbContext);

        return rooms;
    }

    protected static IAsyncEnumerable<Folder<string>> FilterByTags(IAsyncEnumerable<Folder<string>> rooms, bool withoutTags, IEnumerable<string> tags, FilesDbContext filesDbContext)
    {
        if (withoutTags)
        {
            return rooms.Join(Queries.AllThirdPartyIdMappingsAsync(filesDbContext), f => f.Id, m => m.Id, (folder, map) => new { folder, map.HashId })
                .WhereAwait(async r => !await Queries.AnyTagLinksAsync(filesDbContext, r.HashId))
                .Select(r => r.folder);
        }

        if (tags == null || !tags.Any())
        {
            return rooms;
        }

        var filtered = rooms.Join(Queries.AllThirdPartyIdMappingsAsync(filesDbContext), f => f.Id, m => m.Id, (folder, map) => new { folder, map.HashId })
            .Join(Queries.AllTagLinksAsync(filesDbContext), r => r.HashId, t => t.EntryId, (result, tag) => new { result.folder, tag.TagId })
            .Join(Queries.AllTagsAsync(filesDbContext), r => r.TagId, t => t.Id, (result, tagInfo) => new { result.folder, tagInfo.Name })
            .Where(r => tags.Contains(r.Name))
            .Select(r => r.folder);

        return filtered;
    }

    private static IAsyncEnumerable<Folder<string>> FilterByProvidersAsync(IAsyncEnumerable<Folder<string>> rooms, ProviderFilter providerFilter)
    {
        if (providerFilter == ProviderFilter.None)
        {
            return rooms;
        }

        var providerType = GetProviderType(providerFilter);

        return rooms.Where(f => f.ProviderKey == providerType);
    }

    protected static IAsyncEnumerable<Folder<string>> FilterByRoomType(IAsyncEnumerable<Folder<string>> rooms, FilterType filterType)
    {
        if (filterType is FilterType.None or FilterType.FoldersOnly)
        {
            return rooms;
        }

        var typeFilter = GetRoomFolderType(filterType);

        return rooms.Where(f => f.FolderType == typeFilter);
    }

    protected static IAsyncEnumerable<Folder<string>> FilterBySubject(IAsyncEnumerable<Folder<string>> rooms, Guid subjectId, bool excludeSubject, SubjectFilter subjectFilter,
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

    protected static IAsyncEnumerable<Folder<string>> FilterByTitle(IAsyncEnumerable<Folder<string>> rooms, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return rooms;
        }

        return rooms.Where(x => x.Title.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1);
    }

    internal static string GetProviderType(ProviderFilter providerFilter)
    {
        var filter = providerFilter switch
        {
            ProviderFilter.WebDav => ProviderTypes.WebDav,
            ProviderFilter.GoogleDrive => ProviderTypes.GoogleDrive,
            ProviderFilter.OneDrive => ProviderTypes.OneDrive,
            ProviderFilter.DropBox => ProviderTypes.DropboxV2,
            ProviderFilter.kDrive => ProviderTypes.kDrive,
            ProviderFilter.Yandex => ProviderTypes.Yandex,
            ProviderFilter.SharePoint => ProviderTypes.SharePoint,
            ProviderFilter.Box => ProviderTypes.Box,
            _ => throw new NotImplementedException()
        };
        
        return filter.ToStringFast();
    }
    
    internal static FolderType GetRoomFolderType(FilterType filterType)
    {
        var typeFilter = filterType switch
        {
            FilterType.FillingFormsRooms => FolderType.FillingFormsRoom,
            FilterType.EditingRooms => FolderType.EditingRoom,
            FilterType.ReviewRooms => FolderType.ReviewRoom,
            FilterType.ReadOnlyRooms => FolderType.ReadOnlyRoom,
            FilterType.CustomRooms => FolderType.CustomRoom,
            FilterType.PublicRooms => FolderType.PublicRoom,
            _ => FolderType.DEFAULT
        };
        return typeFilter;
    }

    #endregion
}

internal abstract class ThirdPartyProviderDao<TFile, TFolder, TItem>(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    FileUtility fileUtility,
    RegexDaoSelectorBase<TFile, TFolder, TItem> regexDaoSelectorBase)
    : ThirdPartyProviderDao, IDisposable
    where TFile : class, TItem
    where TFolder : class, TItem
    where TItem : class
{
    protected readonly UserManager _userManager = userManager;
    protected readonly TenantUtil _tenantUtil = tenantUtil;
    protected readonly IDbContextFactory<FilesDbContext> _dbContextFactory = dbContextFactory;
    protected readonly FileUtility _fileUtility = fileUtility;
    protected readonly IDaoFactory _daoFactory = daoFactory;
    internal RegexDaoSelectorBase<TFile, TFolder, TItem> DaoSelector { get; set; } = regexDaoSelectorBase;
    protected IProviderInfo<TFile, TFolder, TItem> ProviderInfo { get; set; }
    protected string PathPrefix { get; set; }

    protected string Id { get => ProviderInfo.Selector.Id; }

    protected Folder<string> GetFolder()
    {
        var folder = serviceProvider.GetService<Folder<string>>();

        InitFileEntry(folder);

        folder.FolderType = FolderType.DEFAULT;
        folder.Shareable = false;
        folder.FilesCount = 0;
        folder.FoldersCount = 0;

        return folder;
    }

    protected Folder<string> GetErrorFolder(ErrorEntry entry)
    {
        var folder = GetFolder();

        InitFileEntryError(folder, entry);

        folder.ParentId = null;

        return folder;
    }

    protected File<string> GetFile()
    {
        var file = serviceProvider.GetService<File<string>>();

        InitFileEntry(file);

        file.Access = FileShare.None;
        file.Shared = false;
        file.Version = 1;

        return file;
    }

    protected File<string> GetErrorFile(ErrorEntry entry)
    {
        var file = GetFile();
        InitFileEntryError(file, entry);

        return file;
    }

    private void InitFileEntry(FileEntry<string> fileEntry)
    {
        fileEntry.CreateBy = ProviderInfo.Owner;
        fileEntry.ModifiedBy = ProviderInfo.Owner;
        fileEntry.ProviderId = ProviderInfo.ProviderId;
        fileEntry.ProviderKey = ProviderInfo.ProviderKey;
        fileEntry.RootCreateBy = ProviderInfo.Owner;
        fileEntry.RootFolderType = ProviderInfo.RootFolderType;
        fileEntry.MutableId = ProviderInfo.MutableEntityId;
        fileEntry.RootId = MakeId();
    }

    private void InitFileEntryError(FileEntry<string> fileEntry, ErrorEntry entry)
    {
        fileEntry.Id = MakeId(entry.ErrorId);
        fileEntry.CreateOn = _tenantUtil.DateTimeNow();
        fileEntry.ModifiedOn = _tenantUtil.DateTimeNow();
        fileEntry.Error = entry.Error;
    }

    protected void ProcessFolderAsRoom(Folder<string> folder)
    {
        folder.ProviderMapped = !string.IsNullOrEmpty(ProviderInfo.FolderId);
        
        if (ProviderInfo.FolderId != folder.Id)
        {
            return;
        }

        folder.FolderType = ProviderInfo.FolderType;
        folder.Title = ProviderInfo.CustomerTitle;
    }

    public bool CheckInvalidFilter(FilterType filterType)
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
            FilterType.Pdf;
    }

    public async Task UpdateIdAsync(string oldValue, string newValue)
    {
        if (oldValue.Equals(newValue))
        {
            return;
        }

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();
            var oldIds = Queries.IdsAsync(dbContext, tenantId, oldValue);
            var mapping = _daoFactory.GetMapping<string>();
            
            await foreach (var oldId in oldIds)
            {
                var oldHashId = await mapping.MappingIdAsync(oldId);
                var newId = oldId.Replace(oldValue, newValue);
                var newHashId = await mapping.MappingIdAsync(newId);

                var mappingForDelete = await Queries.ThirdPartyIdMappingsAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var mappingForInsert = mappingForDelete.Select(m => new DbFilesThirdpartyIdMapping
                {
                    TenantId = m.TenantId,
                    Id = newId,
                    HashId = newHashId
                });

                dbContext.RemoveRange(mappingForDelete);
                await dbContext.AddRangeAsync(mappingForInsert);

                var securityForDelete = await Queries.DbFilesSecuritiesAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var securityForInsert = securityForDelete.Select(s => new DbFilesSecurity
                {
                    TenantId = s.TenantId,
                    TimeStamp = DateTime.Now,
                    EntryId = newHashId,
                    Share = s.Share,
                    Subject = s.Subject,
                    EntryType = s.EntryType,
                    Owner = s.Owner,
                    SubjectType = s.SubjectType,
                    Options = s.Options
                });

                dbContext.RemoveRange(securityForDelete);
                await dbContext.AddRangeAsync(securityForInsert);

                var linkForDelete = await Queries.DbFilesTagLinksAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var linkForInsert = linkForDelete.Select(l => new DbFilesTagLink
                {
                    EntryId = newHashId,
                    Count = l.Count,
                    CreateBy = l.CreateBy,
                    CreateOn = l.CreateOn,
                    EntryType = l.EntryType,
                    TagId = l.TagId,
                    TenantId = l.TenantId
                });

                dbContext.RemoveRange(linkForDelete);
                await dbContext.AddRangeAsync(linkForInsert);


                var filesSourceForDelete = await Queries.FilesLinksBySourceIdAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var filesSourceForInsert = filesSourceForDelete.Select(l => new DbFilesLink
                {
                    TenantId = l.TenantId,
                    SourceId = newHashId,
                    LinkedId = l.LinkedId,
                    LinkedFor = l.LinkedFor
                });

                dbContext.RemoveRange(filesSourceForDelete);
                await dbContext.AddRangeAsync(filesSourceForInsert);

                var filesLinkedForDelete = await Queries.FilesLinksByLinkedIdAsync(dbContext, tenantId, oldHashId).ToListAsync();

                var filesLinkedForInsert = filesLinkedForDelete.Select(l => new DbFilesLink
                {
                    TenantId = l.TenantId,
                    SourceId = l.SourceId,
                    LinkedId = newHashId,
                    LinkedFor = l.LinkedFor
                });

                dbContext.RemoveRange(filesLinkedForDelete);
                await dbContext.AddRangeAsync(filesLinkedForInsert);

                await dbContext.SaveChangesAsync();
            }

            await tx.CommitAsync();
        });
    }

    public abstract string MakeId(string path = null);

    public abstract Task<IEnumerable<string>> GetChildrenAsync(string folderId);

    public void Dispose()
    {
        if (ProviderInfo != null)
        {
            ProviderInfo.Dispose();
            ProviderInfo = null;
        }
    }
    
    public Folder<string> GetErrorRoom()
    {
        var folder = GetFolder();
        folder.Id = ProviderInfo.FolderId;
        folder.Title = ProviderInfo.CustomerTitle;
        folder.FolderType = ProviderInfo.FolderType;
        folder.CreateBy = ProviderInfo.Owner;
        folder.CreateOn = ProviderInfo.CreateOn;
        folder.ModifiedOn = ProviderInfo.ModifiedOn;
        folder.RootFolderType = ProviderInfo.RootFolderType;
        folder.SettingsPrivate = ProviderInfo.Private;
        folder.SettingsHasLogo = ProviderInfo.HasLogo;
        folder.ProviderId = ProviderInfo.ProviderId;
        folder.ProviderKey = ProviderInfo.ProviderKey;
        folder.SettingsColor = ProviderInfo.Color;
        folder.RootCreateBy = ProviderInfo.Owner;
        folder.RootId = MakeId();
        folder.ParentId = MakeId();
        folder.Error = FilesCommonResource.ErrorMessage_InvalidThirdPartyFolder;

        return folder;
    }
    
    public bool IsRoom(string folderId)
    {
        return MakeId(folderId) == ProviderInfo.FolderId;
    }
}

internal class ErrorEntry(string error, string errorId)
{
    public string Error { get; set; } = error;
    public string ErrorId { get; set; } = errorId;
}

static file class Queries
{
    public static readonly Func<FilesDbContext, IAsyncEnumerable<DbFilesThirdpartyIdMapping>>
        AllThirdPartyIdMappingsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                ctx.ThirdpartyIdMapping.AsQueryable());

    public static readonly Func<FilesDbContext, string, Task<bool>>
        AnyTagLinksAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, string entryId) =>
                ctx.TagLink
                    .Join(ctx.Tag, l => l.TagId, t => t.Id, (link, tag) => new { link.EntryId, tag })
                    .Where(r => r.tag.Type == TagType.Custom).Any(t => t.EntryId == entryId));

    public static readonly Func<FilesDbContext, IAsyncEnumerable<DbFilesTagLink>>
        AllTagLinksAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                ctx.TagLink.AsQueryable());

    public static readonly Func<FilesDbContext, IAsyncEnumerable<DbFilesTag>>
        AllTagsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                ctx.Tag.AsQueryable());

    public static readonly Func<FilesDbContext, string, Task<string>>
        IdAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, string hashId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.HashId == hashId)
                    .Select(r => r.Id)
                    .FirstOrDefault());
    
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<string>> IdsAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string idStart) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id.StartsWith(idStart))
                    .Select(r => r.Id));
    
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesThirdpartyIdMapping>>
        ThirdPartyIdMappingsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string hashId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.HashId == hashId));
    
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesTagLink>> DbFilesTagLinksAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == entryId));
    
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesSecurity>> DbFilesSecuritiesAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string entryId) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.EntryId == entryId));
    
    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesLink>> FilesLinksBySourceIdAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string sourceId) =>
                ctx.FilesLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(l => l.SourceId == sourceId));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<DbFilesLink>> FilesLinksByLinkedIdAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string linkedId) =>
                ctx.FilesLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(l => l.LinkedId == linkedId));
}
