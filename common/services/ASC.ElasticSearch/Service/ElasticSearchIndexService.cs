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

namespace ASC.ElasticSearch;

public class ElasticSearchIndexService(ILoggerFactory loggerFactory,
        ICacheNotify<AscCacheItem> notify,
        ICacheNotify<IndexAction> indexNotify,
        IServiceScopeFactory serviceScopeFactory,
        Settings settings)
    : BackgroundService
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("ASC.Indexer");
    private readonly TimeSpan _period = TimeSpan.FromMinutes(settings.Period.Value);
    private bool _isStarted;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ElasticSearch Index Service running");

        try
        {
            notify.Subscribe(async _ =>
            {
                while (_isStarted)
                {
                    await Task.Delay(10000, stoppingToken);
                }
                await IndexAll(true);
            }, CacheNotifyAction.Any);
        }
        catch (Exception e)
        {
            _logger.ErrorSubscribeOnStart(e);
        }

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer>();

        while (!await factoryIndexer.CheckStateAsync(false))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            await Task.Delay(10000, stoppingToken);
        }

        var service = scope.ServiceProvider.GetService<ElasticSearchService>();
        service.Subscribe();

        while (!stoppingToken.IsCancellationRequested)
        {
            await IndexAll();

            await Task.Delay(_period, stoppingToken);
        }
    }

    public async Task IndexProductAsync(IFactoryIndexer product, bool reindex)
    {
        if (reindex)
        {
            try
            {
                if (!_isStarted)
                {
                    return;
                }

                _logger.DebugProductReindex(product.IndexName);
                await product.ReIndexAsync();
            }
            catch (Exception e)
            {
                _logger.ErrorProductReindex(product.IndexName, e);
            }
        }

        try
        {
            if (!_isStarted)
            {
                return;
            }

            _logger.DebugProduct(product.IndexName);
            await indexNotify.PublishAsync(new IndexAction { Indexing = product.IndexName, LastIndexed = 0 }, CacheNotifyAction.Any);
            await product.IndexAllAsync();
        }
        catch (Exception e)
        {
            _logger.ErrorProductReindex(product.IndexName, e);
        }
    }

    private async Task IndexAll(bool reindex = false)
    {
        try
        {
            _isStarted = true;

            IEnumerable<Type> wrappers;

            await using (var scope = serviceScopeFactory.CreateAsyncScope())
            {
                wrappers = scope.ServiceProvider.GetService<IEnumerable<IFactoryIndexer>>().Select(r => r.GetType()).ToList();
            }

            await Parallel.ForEachAsync(wrappers, async (wrapper, _) =>
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                await IndexProductAsync((IFactoryIndexer)scope.ServiceProvider.GetRequiredService(wrapper), reindex);
            });


            await indexNotify.PublishAsync(new IndexAction { Indexing = "", LastIndexed = DateTime.Now.Ticks }, CacheNotifyAction.Any);
            _isStarted = false;
        }
        catch (Exception e)
        {
            _logger.CriticalIndexAll(e);

            throw;
        }
    }
}