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

using ASC.Core.Common.Hosting;

namespace ASC.ElasticSearch;

public class ElasticSearchIndexService(
    ILogger<ElasticSearchIndexService> logger,
        ICacheNotify<IndexAction> indexNotify,
        IServiceScopeFactory serviceScopeFactory,
        Settings settings)
    : ActivePassiveBackgroundService<ElasticSearchIndexService>(logger, serviceScopeFactory)
{
    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.FromMinutes(settings.Period.Value);
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ElasticSearch Index Service running");
        
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var factoryIndexer = scope.ServiceProvider.GetService<FactoryIndexer>();

        while (!await factoryIndexer.CheckStateAsync(false))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            await Task.Delay(10000, stoppingToken);
        }
        
        await IndexAll();
    }

    private async Task IndexAll(bool reindex = false)
    {
        try
        {
            IEnumerable<Type> wrappers;

            await using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                wrappers = scope.ServiceProvider.GetService<IEnumerable<IFactoryIndexer>>().Select(r => r.GetType()).ToList();
            }

            await Parallel.ForEachAsync(wrappers, async (wrapper, _) =>
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                await IndexProductAsync((IFactoryIndexer)scope.ServiceProvider.GetRequiredService(wrapper), reindex);
            });
        }
        catch (Exception e)
        {
            logger.CriticalIndexAll(e);

            throw;
        }
    }
    
    private async Task IndexProductAsync(IFactoryIndexer product, bool reindex)
    {
        if (reindex)
        {
            try
            {
                logger.DebugProductReindex(product.IndexName);
                await product.ReIndexAsync();
            }
            catch (Exception e)
            {
                logger.ErrorProductReindex(product.IndexName, e);
            }
        }

        try
        {
            logger.DebugProduct(product.IndexName);
            await indexNotify.PublishAsync(new IndexAction { Indexing = product.IndexName, LastIndexed = 0 }, CacheNotifyAction.Any);
            await product.IndexAllAsync();
        }
        catch (Exception e)
        {
            logger.ErrorProductReindex(product.IndexName, e);
        }
    }
}