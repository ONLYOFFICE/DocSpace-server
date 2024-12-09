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

public class AbstractDao
{
    protected readonly IDbContextFactory<FilesDbContext> _dbContextFactory;
    protected readonly UserManager _userManager;
    protected readonly TenantManager _tenantManager;
    protected readonly TenantUtil _tenantUtil;
    protected readonly SetupInfo _setupInfo;
    protected readonly MaxTotalSizeStatistic _maxTotalSizeStatistic;
    protected readonly SettingsManager _settingsManager;
    protected readonly AuthContext _authContext;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly IDistributedLockProvider _distributedLockProvider;

    protected AbstractDao(
        IDbContextFactory<FilesDbContext> dbContextFactory,
        UserManager userManager,
        TenantManager tenantManager,
        TenantUtil tenantUtil,
        SetupInfo setupInfo,
        MaxTotalSizeStatistic maxTotalSizeStatistic,
        SettingsManager settingsManager,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        IDistributedLockProvider distributedLockProvider)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _tenantUtil = tenantUtil;
        _setupInfo = setupInfo;
        _maxTotalSizeStatistic = maxTotalSizeStatistic;
        _settingsManager = settingsManager;
        _authContext = authContext;
        _serviceProvider = serviceProvider;
        _distributedLockProvider = distributedLockProvider;
    }


    protected IQueryable<T> Query<T>(DbSet<T> set) where T : class, IDbFile
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        return set.Where(r => r.TenantId == tenantId);
    }

    protected IQueryable<DbFile> GetFileQuery(FilesDbContext filesDbContext, Expression<Func<DbFile, bool>> where)
    {
        return Query(filesDbContext.Files)
            .Where(where);
    }
    
        
    protected async Task IncrementCountAsync(FilesDbContext filesDbContext, int folderId, int tenantId, FileEntryType fileEntryType)
    {
        await ChangeCountAsync(filesDbContext, folderId, tenantId, fileEntryType, 1);
    }
    
    protected async Task DecrementCountAsync(FilesDbContext filesDbContext, int folderId, int tenantId,FileEntryType fileEntryType)
    {
        await ChangeCountAsync(filesDbContext, folderId, tenantId, fileEntryType, -1);
    }
    
    private async Task ChangeCountAsync(FilesDbContext filesDbContext, int folderId, int tenantId, FileEntryType fileEntryType, int counter)
    {
        if (fileEntryType == FileEntryType.File)
        {
            await filesDbContext.ChangeFilesCountAsync(tenantId, folderId, counter);
        }
        else
        {
            await filesDbContext.ChangeFoldersCountAsync(tenantId, folderId, counter);
        }

        await filesDbContext.SaveChangesAsync();
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
    
    internal static IQueryable<TQuery> BuildSearch<TQuery, TEntry>(IQueryable<TQuery> query, string text, SearchType searchType) 
        where TQuery : IQueryResult<TEntry> 
        where TEntry: IDbSearch
    {
        var lowerText = GetSearchText(text);

        return searchType switch
        {
            SearchType.Start => query.Where(r => r.Entry.Title.ToLower().StartsWith(lowerText)),
            SearchType.End => query.Where(r => r.Entry.Title.ToLower().EndsWith(lowerText)),
            SearchType.Any => query.Where(r => r.Entry.Title.ToLower().Contains(lowerText)),
            _ => query
        };
    }
    internal static IQueryable<TQuery> BuildSearch<TQuery, TEntry>(IQueryable<TQuery> query, IEnumerable<string> text, SearchType searchType) 
        where TQuery : IQueryResult<TEntry> 
        where TEntry: IDbSearch
    {
        var lowerText = text.Select(GetSearchText);

        switch (searchType)
        {
            case SearchType.Start:
                {
                    Expression<Func<TQuery, bool>> exp = p1 => false;

                    foreach (var t in lowerText)
                    {
                        exp = exp.Or(p => p.Entry.Title.ToLower().StartsWith(t));
                    }
                    return query.Where(exp);
                }
            case SearchType.End:
                {
                    Expression<Func<TQuery, bool>> exp = p1 => false;

                    foreach (var t in lowerText)
                    {
                        exp = exp.Or(p => p.Entry.Title.ToLower().EndsWith(t));
                    }
                    return query.Where(exp);
                }
            case SearchType.Any:
                {
                    Expression<Func<TQuery, bool>> exp = p1 => false;

                    foreach (var t in lowerText)
                    {
                        exp = exp.Or(p => p.Entry.Title.ToLower().Contains(t));
                    }
                    return query.Where(exp);
                }
            default:
                {
                    return query;
                }
        }
    }

    internal static IQueryable<T> BuildSearch<T>(IQueryable<T> query, IEnumerable<string> text, SearchType searchType) where T : IDbSearch
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

    internal async Task<int> SetCustomOrder(FilesDbContext filesDbContext, int fileId, int parentFolderId, FileEntryType fileEntryType, int order = 0)
    {            
        var tenantId = _tenantManager.GetCurrentTenantId();
        var indexing = await filesDbContext.IsIndexingAsync(tenantId, parentFolderId, fileEntryType);
        
        if(!indexing)
        {
            return 0;
        }

        await using (await _distributedLockProvider.TryAcquireFairLockAsync(GetCustomOrderLockKey(tenantId, parentFolderId)))
        {
            var fileOrder = await filesDbContext.GetFileOrderAsync(tenantId, fileId, fileEntryType);

            if (order == 0 || fileOrder?.ParentFolderId != parentFolderId)
            {
                var lastOrder = await filesDbContext.GetLastFileOrderAsync(tenantId, parentFolderId);
                order = ++lastOrder;
            }

            if (fileOrder != null)
            {
                if (fileOrder.ParentFolderId == parentFolderId)
                {
                    var currentOrder = fileOrder.Order;
                    if (currentOrder == order)
                    {
                        return currentOrder;
                    }

                    if (currentOrder > order)
                    {
                        await filesDbContext.IncreaseFileOrderAsync(tenantId, parentFolderId, order, currentOrder);
                    }
                    else
                    {
                        await filesDbContext.DecreaseFileOrderAsync(tenantId, parentFolderId, order, currentOrder);
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
            return order;
        }
    }

    internal async Task InitCustomOrder(Dictionary<int, int> fileIds, int parentFolderId, FileEntryType entryType)
    {
        var ids = fileIds.Select(r => r.Key).ToList();
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using (await _distributedLockProvider.TryAcquireFairLockAsync(GetCustomOrderLockKey(tenantId, parentFolderId)))
        {
            await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

            var order = await filesDbContext.FileOrder
                .AsTracking()
                .Where(r => r.TenantId == tenantId && r.EntryType == entryType && ids.Contains(r.EntryId))
                .ToListAsync();
            
            var orders = new List<DbFileOrder>();
            
            foreach (var id in fileIds)
            {
                var o = order.FirstOrDefault(r => r.EntryId == id.Key && r.EntryType == entryType);
                if (o != null)
                {
                    o.Order = fileIds[o.EntryId];
                }
                else
                {
                    orders.Add(new DbFileOrder
                    {
                        TenantId = tenantId,
                        ParentFolderId = parentFolderId,
                        EntryId = id.Key,
                        EntryType = entryType,
                        Order = id.Value
                    });
                }
            }
            
            filesDbContext.FileOrder.AddRange(orders);
            await filesDbContext.SaveChangesAsync();
        }
    }
    
    internal async Task DeleteCustomOrder(FilesDbContext filesDbContext, int fileId, FileEntryType fileEntryType)
    {        
        var tenantId = _tenantManager.GetCurrentTenantId();
        var fileOrder = await filesDbContext.GetFileOrderAsync(tenantId, fileId, fileEntryType);
        if (fileOrder != null)
        {
            filesDbContext.Remove(fileOrder);

            await filesDbContext.SaveChangesAsync();
        }
    }

    private string GetCustomOrderLockKey(int tenantId, int folderId) => $"order_{folderId}_{tenantId}";
    
    internal enum SearchType
    {
        Start,
        End,
        Any
    }
}