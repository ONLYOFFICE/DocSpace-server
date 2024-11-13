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

using System.Threading.Channels;

using ASC.Common.Threading;
using ASC.Common.Threading.DistributedLock.Abstractions;

namespace ASC.ElasticSearch.Service;

[Singleton]
public class ElasticSearchService(
    IDistributedTaskQueueFactory queueFactory,
    ILogger<ElasticSearchService> logger,
    IServiceScopeFactory serviceScopeFactory,
    ChannelReader<ReIndexAction> channelReader,
    IDistributedLockProvider distributedLockProvider)
    : BackgroundService
{    
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = nameof(ElasticSearchService);
    private const string LockKey = $"lock_{CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME}";
    private readonly DistributedTaskQueue _reindexQueue = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.InformationElasticSearchServiceRunning();
        
        stoppingToken.Register(logger.InformationElasticSearchServiceStopping);

        var readers = new List<ChannelReader<ReIndexAction>>
        {
            channelReader
        };
        
        var tasks = new List<Task>();

        for (var i = 0; i < readers.Count; i++)
        {
            var reader = readers[i];

            tasks.Add(Task.Run(async () =>
            {
                await foreach (var reIndexAction in reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, cancellationToken: stoppingToken))
                        {
                            var item = (await _reindexQueue.GetAllTasks<ReIndexAction>()).FirstOrDefault(r => r.Tenant != reIndexAction.Tenant);

                            if (item is { Status: DistributedTaskStatus.Completed })
                            {
                                await _reindexQueue.DequeueTask(item.Id);
                                item = null;
                            }
                            
                            if (item == null)
                            {
                                await _reindexQueue.EnqueueTask(async (_, _) => await Reindex(reIndexAction), reIndexAction);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.ErrorWithException(e);
                        throw;
                    }

                }
            }, stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task Reindex(ReIndexAction reIndexAction)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var allItems = scope.ServiceProvider.GetService<IEnumerable<IFactoryIndexer>>().ToList();

        foreach (var item in allItems)
        {
            await item.ReIndexAsync(reIndexAction.Tenant);
        }
    }
}
