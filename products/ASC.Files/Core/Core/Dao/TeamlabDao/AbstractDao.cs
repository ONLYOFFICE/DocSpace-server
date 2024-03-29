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
            _ => query,
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
}