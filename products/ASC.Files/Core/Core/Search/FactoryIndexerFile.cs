// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
        if (!await base.BeforeIndexAsync(data))
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
    ILoggerFactory loggerFactory,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerFile baseIndexer,
    IServiceProvider serviceProvider,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    ICache cache,
    Settings settings,
    FileUtility fileUtility)
    : FactoryIndexer<DbFile>(loggerFactory, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider, cache)
{
    public override async Task IndexAllAsync()
    {
        try
        {
            var j = 0;
            var tasks = new List<Task>();
            var now = DateTime.UtcNow;

            await foreach (var data in _indexer.IndexAllAsync(GetCount, GetIds, GetData))
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

        return;

        List<int> GetIds(DateTime lastIndexed)
        {
            var start = 0;
            var result = new List<int>();

            using var filesDbContext = dbContextFactory.CreateDbContext();

            while (true)
            {
                var id = Queries.FileId(filesDbContext, lastIndexed, start);
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

        List<DbFile> GetData(long start, long stop, DateTime lastIndexed)
        {
            using var filesDbContext = dbContextFactory.CreateDbContext();
            return Queries.FilesFoldersPair(filesDbContext, lastIndexed, start, stop)
                .Select(r =>
                {
                    var result = r.File;
                    result.Folders = r.Folders;
                    return result;
                })
                .ToList();

        }

        (int, int, int) GetCount(DateTime lastIndexed)
        {
            using var filesDbContext = dbContextFactory.CreateDbContext();

            var minId = Queries.FileMinId(filesDbContext, lastIndexed);

            var maxId = Queries.FileMaxId(filesDbContext, lastIndexed);

            var count = Queries.FilesCount(filesDbContext, lastIndexed);

            return new ValueTuple<int, int, int>(count, maxId, minId);
        }
    }

    public override string SettingsTitle => FilesCommonResource.IndexTitle;

    public override async Task<bool> CanIndexByContentAsync(DbFile t)
    {
        return await base.CanIndexByContentAsync(t) && fileUtility.CanIndex(t.Title) && !t.Encrypted;
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
    public static readonly Func<FilesDbContext, DateTime, int> FileMinId = EF.CompileQuery(
        (FilesDbContext ctx, DateTime lastIndexed) =>
            ctx.Files
                .Where(r => r.ModifiedOn >= lastIndexed)
                .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Select(r => r.DbFile)
                .Where(r => r.Version == 1)
                .OrderBy(r => r.Id)
                .Select(r => r.Id)
                .FirstOrDefault());

    public static readonly Func<FilesDbContext, DateTime, int> FileMaxId = EF.CompileQuery(
        (FilesDbContext ctx, DateTime lastIndexed) =>
            ctx.Files
                .Where(r => r.ModifiedOn >= lastIndexed)
                .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Select(r => r.DbFile)
                .Where(r => r.Version == 1)
                .OrderByDescending(r => r.Id)
                .Select(r => r.Id)
                .FirstOrDefault());

    public static readonly Func<FilesDbContext, DateTime, int> FilesCount = EF.CompileQuery(
        (FilesDbContext ctx, DateTime lastIndexed) =>
            ctx.Files
                .Where(r => r.ModifiedOn >= lastIndexed)
                .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Select(r => r.DbFile)
                .Count(r => r.Version == 1));

    public static readonly Func<FilesDbContext, DateTime, long, long, IEnumerable<FilesFoldersPair>> FilesFoldersPair =
        EF.CompileQuery(
            (FilesDbContext ctx, DateTime lastIndexed, long start, long stop) =>
                ctx.Files
                    .Where(r => r.ModifiedOn >= lastIndexed)
                    .Join(ctx.Tenants, r => r.TenantId, r => r.Id,
                        (f, t) => new FileTenant { DbFile = f, DbTenant = t })
                    .Where(r => r.DbTenant.Status == TenantStatus.Active)
                    .Select(r => r.DbFile)
                    .Where(r => r.Id >= start && r.Id <= stop && r.CurrentVersion)
                    .Select(file => new FilesFoldersPair
                    {
                        File = file,
                        Folders = ctx.Tree.Where(b => b.FolderId == file.ParentId).ToList()
                    }));

    public static readonly Func<FilesDbContext, DateTime, long, int> FileId = EF.CompileQuery(
    (FilesDbContext ctx, DateTime lastIndexed, long start) =>
        ctx.Files
            .Where(r => r.ModifiedOn >= lastIndexed)
            .Join(ctx.Tenants, r => r.TenantId, r => r.Id, (f, t) => new FileTenant { DbFile = f, DbTenant = t })
            .Where(r => r.DbTenant.Status == TenantStatus.Active)
            .Select(r => r.DbFile)
            .Where(r => r.Id >= start)
            .Where(r => r.Version == 1)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .Skip(BaseIndexer<DbFile>.QueryLimit)
            .FirstOrDefault());
}
