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

namespace ASC.Web.Files.Core.Search;

[Scope]
public class BaseIndexerFile(Client client,
        ILogger<BaseIndexerFile> log,
        IDbContextFactory<WebstudioDbContext> dbContextManager,
        TenantManager tenantManager,
        BaseIndexerHelper baseIndexerHelper,
        Settings settings,
        IServiceProvider serviceProvider,
        IDaoFactory daoFactory)
    : BaseIndexer<DbFile>(client, log, dbContextManager, tenantManager, baseIndexerHelper, settings, serviceProvider)
{
    protected override async Task<bool> BeforeIndexAsync(DbFile data)
    {
        if (!(await base.BeforeIndexAsync(data)))
        {
            return false;
        }

        if (daoFactory.GetFileDao<int>() is FileDao fileDao)
        {
            await fileDao.InitDocumentAsync(data, data.TenantId);
        }

        return true;
    }
}


[Scope(typeof(IFactoryIndexer))]
public class FactoryIndexerFile(
    ILoggerProvider options,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerFile baseIndexer,
    IServiceProvider serviceProvider,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    ICache cache,
    Settings settings,
    FileUtility fileUtility)
    : FactoryIndexer<DbFile>(options, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider, cache)
{
    public override async Task IndexAllAsync(int tenantId = 0)
    {
        try
        {
            var j = 0;
            var tasks = new List<Task>();
            var now = DateTime.UtcNow;
            
            await foreach (var data in _indexer.IndexAllAsync(GetCount, GetIds, GetData, tenantId))
            {
                if (settings.Threads == 1)
                {
                    await Index(data);
                }
                else
                {
                    tasks.Add(Index(data));
                    j++;
                    if (j >= settings.Threads)
                    {
                        Task.WaitAll(tasks.ToArray());
                        tasks = [];
                        j = 0;
                    }
                }
            }

            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray());
            }

            await _indexer.OnComplete(now);
        }
        catch (Exception e)
        {
            Logger.ErrorFactoryIndexerFile(e);
            throw;
        }
    }

    private List<int> GetIds(DateTime lastIndexed, int tenantId)
    {
        var start = 0;
        var result = new List<int>();

        using var filesDbContext = dbContextFactory.CreateDbContext();

        while (true)
        {
            var id = Queries.FileId(filesDbContext, lastIndexed, start, tenantId);
            if (id != 0)
            {
                start = id;
                result.Add(id);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private List<DbFile> GetData(long start, long stop, DateTime lastIndexed, int tenantId)
    {
        using var filesDbContext = dbContextFactory.CreateDbContext();
        return Queries.FilesFoldersPair(filesDbContext, lastIndexed, start, stop, tenantId)
            .Select(r =>
            {
                var result = r.File;
                result.Folders = r.Folders;
                return result;
            })
            .ToList();

    }

    private (int, int, int) GetCount(DateTime lastIndexed, int tenantId)
    {
        using var filesDbContext = dbContextFactory.CreateDbContext();

        var minId = Queries.FileMinId(filesDbContext, lastIndexed, tenantId);

        var maxId = Queries.FileMaxId(filesDbContext, lastIndexed, tenantId);

        var count = Queries.FilesCount(filesDbContext, lastIndexed, tenantId);

        return new(count, maxId, minId);
    }

    public override string SettingsTitle => FilesCommonResource.IndexTitle;

    public override async Task<bool> CanIndexByContentAsync(DbFile t)
    {
        return await base.CanIndexByContentAsync(t) && fileUtility.CanIndex(t.Title);
}
}

public class FileTenant
{
    public DbTenant DbTenant { get; init; }
    public DbFile DbFile { get; init; }
}

sealed file class FilesFoldersPair
{
    public DbFile File { get; init; }
    public List<DbFolderTree> Folders { get; init; }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, DateTime, int, int> FileMinId = EF.CompileQuery(
        (FilesDbContext ctx, DateTime lastIndexed, int tenantId) =>
            ctx.Files
                .Where(r => r.ModifiedOn >= lastIndexed)
                .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Select(r => r.DbFile)
                .Where(r => r.Version == 1)
                .Where(r => tenantId == 0 || r.TenantId == tenantId)
                .OrderBy(r => r.Id)
                .Select(r => r.Id)
                .FirstOrDefault());

    public static readonly Func<FilesDbContext, DateTime, int, int> FileMaxId = EF.CompileQuery(
        (FilesDbContext ctx, DateTime lastIndexed, int tenantId) =>
            ctx.Files
                .Where(r => r.ModifiedOn >= lastIndexed)
                .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Select(r => r.DbFile)
                .Where(r => r.Version == 1)
                .Where(r => tenantId == 0 || r.TenantId == tenantId)
                .OrderByDescending(r => r.Id)
                .Select(r => r.Id)
                .FirstOrDefault());

    public static readonly Func<FilesDbContext, DateTime, int, int> FilesCount = EF.CompileQuery(
        (FilesDbContext ctx, DateTime lastIndexed, int tenantId) =>
            ctx.Files
                .Where(r => r.ModifiedOn >= lastIndexed)
                .Where(r => tenantId == 0 || r.TenantId == tenantId)
                .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Select(r => r.DbFile)
                .Count(r => r.Version == 1));

    public static readonly Func<FilesDbContext, DateTime, long, long, int, IEnumerable<FilesFoldersPair>> FilesFoldersPair =
        EF.CompileQuery(
            (FilesDbContext ctx, DateTime lastIndexed, long start, long stop, int tenantId) =>
                ctx.Files
                    .Where(r => r.ModifiedOn >= lastIndexed)
                    .Join(ctx.Tenants, r => r.TenantId, r => r.Id,
                        (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                    .Where(r => r.DbTenant.Status == TenantStatus.Active)
                    .Select(r => r.DbFile)
                    .Where(r => r.Id >= start && r.Id <= stop && r.CurrentVersion)
                    .Where(r => tenantId == 0 || r.TenantId == tenantId)
                    .Select(file => new FilesFoldersPair
                    {
                        File = file, Folders = ctx.Tree.Where(b => b.FolderId == file.ParentId).ToList()
                    }));
    
    public static readonly Func<FilesDbContext, DateTime, long, int, int> FileId = EF.CompileQuery(
    (FilesDbContext ctx, DateTime lastIndexed, long start, int tenantId) =>
        ctx.Files
            .Where(r => r.ModifiedOn >= lastIndexed)
            .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
            .Where(r => r.DbTenant.Status == TenantStatus.Active)
            .Select(r => r.DbFile)
            .Where(r => r.Id >= start)
            .Where(r => r.Version == 1)
            .Where(r => tenantId == 0 || r.TenantId == tenantId)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .Skip(BaseIndexer<DbFile>.QueryLimit)
            .FirstOrDefault());
}