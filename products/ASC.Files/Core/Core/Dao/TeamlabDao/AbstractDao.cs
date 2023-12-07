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

public class AbstractDao
{
    protected readonly IDbContextFactory<FilesDbContext> _dbContextFactory;
    protected readonly UserManager _userManager;
    protected readonly TenantManager _tenantManager;
    protected readonly TenantUtil _tenantUtil;
    protected readonly SetupInfo _setupInfo;
    protected readonly MaxTotalSizeStatistic _maxTotalSizeStatistic;
    protected readonly CoreBaseSettings _coreBaseSettings;
    protected readonly CoreConfiguration _coreConfiguration;
    protected readonly SettingsManager _settingsManager;
    protected readonly AuthContext _authContext;
    protected readonly IServiceProvider _serviceProvider;

    protected AbstractDao(
        IDbContextFactory<FilesDbContext> dbContextFactory,
        UserManager userManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        CoreBaseSettings coreBaseSettings,
        CoreConfiguration coreConfiguration,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _tenantUtil = tenantUtil;
        _setupInfo = setupInfo;
        _maxTotalSizeStatistic = maxTotalSizeStatistic;
        _coreBaseSettings = coreBaseSettings;
        _coreConfiguration = coreConfiguration;
        _settingsManager = settingsManager;
        _authContext = authContext;
        _serviceProvider = serviceProvider;
    }


    protected async Task<IQueryable<T>> Query<T>(DbSet<T> set) where T : class, IDbFile
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        return set.Where(r => r.TenantId == tenantId);
    }

    protected async Task<IQueryable<DbFile>> GetFileQuery(FilesDbContext filesDbContext, Expression<Func<DbFile, bool>> where)
    {
        return (await Query(filesDbContext.Files))
            .Where(where);
    }

    protected async Task GetRecalculateFilesCountUpdateAsync(int folderId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var folders = await Queries.FoldersAsync(filesDbContext, tenantId, folderId).ToListAsync();

        foreach (var f in folders)
        {
            f.FilesCount = await Queries.FilesCountAsync(filesDbContext, f.TenantId, f.Id);
        }
        await filesDbContext.SaveChangesAsync();
    }

    protected int MappingIDAsync(int id)
    {
        return id;
    }

    protected ValueTask<object> MappingIDAsync(object id, bool saveIfNotExist = false)
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

        return InternalMappingIDAsync(id, saveIfNotExist);
    }

    private async ValueTask<object> InternalMappingIDAsync(object id, bool saveIfNotExist = false)
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
            result = await Queries.IdAsync(filesDbContext, tenantId, id.ToString());
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

    internal static IQueryable<T> BuildSearch<T>(IQueryable<T> query, string text, SearchType searchType) where T : IDbSearch
    {
        var lowerText = GetSearchText(text);

        return searchType switch
        {
            SearchType.Start => query.Where(r => r.Title.ToLower().StartsWith(lowerText)),
            SearchType.End => query.Where(r => r.Title.ToLower().EndsWith(lowerText)),
            SearchType.Any => query.Where(r => r.Title.ToLower().Contains(lowerText)),
            _ => query
        };
    }

    internal static IQueryable<T> BuildSearch<T>(IQueryable<T> query, string[] text, SearchType searchType) where T : IDbSearch
    {
        var lowerText = text.Select(GetSearchText);

        switch (searchType)
        {
            case SearchType.Start:
                {
                    Expression<Func<T, bool>> exp = p1 => false;

                    foreach (var t in lowerText)
                    {
                        exp = exp.Or(p => p.Title.ToLower().StartsWith(t));
                    }
                    return query.Where(exp);
                }
            case SearchType.End:
                {
                    Expression<Func<T, bool>> exp = p1 => false;

                    foreach (var t in lowerText)
                    {
                        exp = exp.Or(p => p.Title.ToLower().EndsWith(t));
                    }
                    return query.Where(exp);
                }
            case SearchType.Any:
                {
                    Expression<Func<T, bool>> exp = p1 => false;

                    foreach (var t in lowerText)
                    {
                        exp = exp.Or(p => p.Title.ToLower().Contains(t));
                    }
                    return query.Where(exp);
                }
            default:
                {
                    return query;
                }
        }
    }

    internal static string GetSearchText(string text) => (text ?? "").ToLower().Trim();

    internal async Task SetCustomOrder(FilesDbContext filesDbContext, int fileId, int parentFolderId, FileEntryType fileEntryType, int order = 0)
    {            
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var indexing = await Queries.IsIndexingAsync(filesDbContext, tenantId, parentFolderId, fileEntryType);
        
        if(!indexing)
        {
            return;
        }

        var fileOrder = await Queries.GetFileOrderAsync(filesDbContext, tenantId, fileId, fileEntryType);

        if (order == 0 || fileOrder?.ParentFolderId != parentFolderId)
        {
            var lastOrder = await Queries.GetLastFileOrderAsync(filesDbContext, tenantId, parentFolderId, fileEntryType);
            order = ++lastOrder;
        }

        if (fileOrder != null)
        {
            if (fileOrder.ParentFolderId == parentFolderId)
            {
                var currentOrder = fileOrder.Order;

                if (currentOrder == order)
                {
                    return;
                }
                
                if (currentOrder > order)
                {
                    await Queries.IncreaseFileOrderAsync(filesDbContext, tenantId, parentFolderId, order, currentOrder);
                }
                else
                {
                    await Queries.DecreaseFileOrderAsync(filesDbContext, tenantId, parentFolderId, order, currentOrder);
                }
            }

            fileOrder.ParentFolderId = parentFolderId;
            fileOrder.Order = order;
        }
        else
        {
            await filesDbContext.FileOrder.AddAsync(new DbFileOrder
            {
                EntryId = fileId,
                EntryType = fileEntryType,
                ParentFolderId = parentFolderId,
                TenantId = tenantId,
                Order = order
            });
        }

        await filesDbContext.SaveChangesAsync();
    }

    internal async Task InitCustomOrder(IEnumerable<int> fileIds, int parentFolderId, FileEntryType entryType)
    {        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        await using var filesDbContext = _dbContextFactory.CreateDbContext();

        await Queries.ClearFileOrderAsync(filesDbContext, tenantId, parentFolderId, entryType);
        
        await filesDbContext.FileOrder.AddRangeAsync(fileIds.Select((r, i) => new DbFileOrder
        {
            TenantId = tenantId,
            ParentFolderId = parentFolderId,
            EntryId = r,
            EntryType = entryType,
            Order = i + 1
        }));
        
        await filesDbContext.SaveChangesAsync();
    }
    
    internal async Task DeleteCustomOrder(FilesDbContext filesDbContext, int fileId, FileEntryType fileEntryType)
    {        
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var fileOrder = await Queries.GetFileOrderAsync(filesDbContext, tenantId, fileId, fileEntryType);
        if (fileOrder != null)
        {
            filesDbContext.Remove(fileOrder);

            await filesDbContext.SaveChangesAsync();
        }
    }

    internal enum SearchType
    {
        Start,
        End,
        Any
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFolder>> FoldersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Folders
                    .AsTracking()
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.Tree.Where(t => t.FolderId == folderId).Any(a => a.ParentId == r.Id)));

    public static readonly Func<FilesDbContext, int, int, Task<int>> FilesCountAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int folderId) =>
                ctx.Files
                    .Join(ctx.Tree, a => a.ParentId, b => b.FolderId, (file, tree) => new { file, tree })
                    .Where(r => r.file.TenantId == tenantId)
                    .Where(r => r.tree.ParentId == folderId)
                    .Select(r => r.file.Id)
                    .Distinct()
                    .Count());

    public static readonly Func<FilesDbContext, int, string, Task<string>> IdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string hashId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.HashId == hashId)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task<bool>> IsIndexingAsync =
    Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, int parentFolderId, FileEntryType entryType) =>
            (from rs in ctx.RoomSettings 
                where rs.TenantId == tenantId && rs.RoomId ==
                    (from t in ctx.Tree
                        where t.FolderId == parentFolderId
                        orderby t.Level descending
                        select t.ParentId
                    ).Skip(1).FirstOrDefault()
                select rs.Indexing).FirstOrDefault());
    
    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task<DbFileOrder>> GetFileOrderAsync =
    Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, int entryId, FileEntryType entryType) =>
            ctx.FileOrder
                .AsTracking()
                .FirstOrDefault(r =>  r.TenantId == tenantId && r.EntryId == entryId && r.EntryType == entryType));

    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task> ClearFileOrderAsync =
    Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (FilesDbContext ctx, int tenantId, int parentFolderId, FileEntryType entryType) =>
            ctx.FileOrder
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && r.ParentFolderId == parentFolderId)
                .ExecuteDelete());


    public static readonly Func<FilesDbContext, int, int, FileEntryType, Task<int>> GetLastFileOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentFolderId, FileEntryType entryType) =>
                ctx.FileOrder
                    .AsTracking()
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentFolderId == parentFolderId)
                    .Where(r => r.EntryType == entryType)
                    .OrderBy(r => r.Order)
                    .Select(r => r.Order)
                    .LastOrDefault());

    public static readonly Func<FilesDbContext, int, int, int, int, Task> IncreaseFileOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentFolderId, int newOrder, int currentOrder) =>
                ctx.FileOrder
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentFolderId == parentFolderId)
                    .Where(r => r.Order >= newOrder && r.Order < currentOrder)
                    .ExecuteUpdate(f => f.SetProperty(p => p.Order, p => p.Order + 1)));

    public static readonly Func<FilesDbContext, int, int, int, int, Task> DecreaseFileOrderAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int parentFolderId, int newOrder, int currentOrder) =>
                ctx.FileOrder
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentFolderId == parentFolderId)
                    .Where(r => r.Order <= newOrder && r.Order > currentOrder)
                    .ExecuteUpdate(f => f.SetProperty(p => p.Order, p => p.Order - 1)));
}