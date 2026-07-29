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

namespace ASC.AI.Core.Search;

[Scope]
public class BaseIndexerThread(Client client,
        ILogger<BaseIndexerThread> log,
        IDbContextFactory<WebstudioDbContext> dbContextManager,
        TenantManager tenantManager,
        BaseIndexerHelper baseIndexerHelper,
        ASC.ElasticSearch.Service.Settings settings,
        IServiceProvider serviceProvider)
    : BaseIndexer<ThreadSearchItem>(client, log, dbContextManager, tenantManager, baseIndexerHelper, settings, serviceProvider)
{
    protected override Id GetDocumentId(ThreadSearchItem data)
    {
        return data.ThreadId.ToString();
    }
}

[Scope(typeof(IFactoryIndexer))]
public class FactoryIndexerThread(
    ILoggerFactory loggerFactory,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerThread baseIndexer,
    IServiceProvider serviceProvider,
    IDbContextFactory<AiIntegrationContext> dbContextFactory,
    IDbContextFactory<WebstudioDbContext> webstudioDbContextFactory)
    : FactoryIndexer<ThreadSearchItem>(loggerFactory, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider)
{
    private const int BatchSize = 1000;

    public override async Task IndexAllAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var lastIndexed = await GetLastIndexedAsync();

            if (lastIndexed.Equals(DateTime.MinValue))
            {
                _indexer.CreateIfNotExist(new ThreadSearchItem());
            }

            var lastTenantId = int.MinValue;
            var lastId = Guid.Empty;

            while (true)
            {
                List<DbThread> batch;

                await using (var context = await dbContextFactory.CreateDbContextAsync())
                {
                    batch = await context.GetThreadsForIndexingAsync(lastIndexed, lastTenantId, lastId, BatchSize).ToListAsync();
                }

                if (batch.Count == 0)
                {
                    break;
                }

                await Index([.. batch.Select(ThreadSearchItem.FromThread)]);

                var last = batch[^1];
                lastTenantId = last.TenantId;
                lastId = last.Id;

                if (batch.Count < BatchSize)
                {
                    break;
                }
            }

            await _indexer.OnComplete(now);
        }
        catch (Exception e)
        {
            Logger.ErrorFactoryIndexerThread(e);
            throw;
        }
    }

    private async Task<DateTime> GetLastIndexedAsync()
    {
        await using var context = await webstudioDbContextFactory.CreateDbContextAsync();

        return await Queries.LastIndexedAsync(context, ThreadSearchItem.Index);
    }
}

static file class Queries
{
    public static readonly Func<WebstudioDbContext, string, Task<DateTime>> LastIndexedAsync =
        EF.CompileAsyncQuery(
            (WebstudioDbContext ctx, string indexName) =>
                ctx.WebstudioIndex
                    .Where(r => r.IndexName == indexName)
                    .Select(r => r.LastModified)
                    .FirstOrDefault());
}

internal static partial class FactoryIndexerThreadLogger
{
    [LoggerMessage(LogLevel.Error, "Failed to index all threads")]
    public static partial void ErrorFactoryIndexerThread(this ILogger logger, Exception exception);
}
