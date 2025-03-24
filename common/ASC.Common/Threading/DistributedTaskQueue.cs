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

using IDistributedLockProvider = ASC.Common.Threading.DistributedLock.Abstractions.IDistributedLockProvider;

namespace ASC.Common.Threading;

[Transient]
public class DistributedTaskQueue<T>(
    ChannelWriter<T> channelWriter,
    ICacheNotify<DistributedTaskCancelation> cancelTaskNotify,
    IFusionCache hybridCache,
    ILogger<DistributedTaskQueue<T>> logger,
    IDistributedLockProvider distributedLockProvider)  where T : DistributedTask
{
    public const string QUEUE_DEFAULT_PREFIX = "asc_distributed_task_queue_";
    public static readonly int INSTANCE_ID = Environment.ProcessId;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancelations = new();
    private bool _subscribed;
    private int _maxThreadsCount = 1;
    private string _name;

    public int TimeUntilUnregisterInSeconds { get; set; }

    public string Name
    {
        get => _name;
        set => _name = QUEUE_DEFAULT_PREFIX + value;
    }

    public int MaxThreadsCount
    {
        get
        {
            return _maxThreadsCount;
        }

        set
        {
            if (value > 0)
            {
                _maxThreadsCount = value;
            }
        }
    }

    public async Task EnqueueTask(T distributedTask)
    {
        distributedTask.InstanceId = INSTANCE_ID;

        if (distributedTask.LastModifiedOn.Equals(DateTime.MinValue))
        {
            distributedTask.LastModifiedOn = DateTime.UtcNow;
        }

        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _cancelations[distributedTask.Id] = cancellation;

        if (!_subscribed)
        {
            cancelTaskNotify.Subscribe(c =>
            {
                if (_cancelations.TryGetValue(c.Id, out var s))
                {
                    s.Cancel();
                }
            }, CacheNotifyAction.Remove);

            _subscribed = true;
        }

        await channelWriter.WriteAsync(distributedTask, token);

        distributedTask.Status = DistributedTaskStatus.Running;

        await PublishTask(distributedTask);
        
        logger.TraceEnqueueTask(distributedTask.Id, INSTANCE_ID);
    }
    
    public async Task<List<T>> GetAllTasks(int? instanceId = null)
    {
        var queueTasks = await (await LoadKeysFromCache()).ToAsyncEnumerable().SelectAwait(async id => await PeekTask(id)).Where(t => t != null).ToListAsync();
        
        if (instanceId.HasValue)
        {
            queueTasks = queueTasks.Where(x => x.InstanceId == instanceId.Value).ToList();
        }

        foreach (var task in queueTasks)
        {
            task.Publication ??= GetPublication();
        }

        return queueTasks;
    }
    

    public async Task<T> PeekTask(string id)
    {
        return await hybridCache.GetOrDefaultAsync<T>(_name + id);
    }

    public async Task DequeueTask(string id)
    {
        await cancelTaskNotify.PublishAsync(new DistributedTaskCancelation { Id = id }, CacheNotifyAction.Remove);
        
        await hybridCache.RemoveAsync(_name + id);
        
        logger.TraceEnqueueTask(id, INSTANCE_ID);
    }

    public async Task<string> PublishTask(T distributedTask)
    {
        distributedTask.Publication ??= GetPublication();
        await distributedTask.PublishChanges();

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"{Name}_lock"))
        {
            var tasks = await LoadKeysFromCache();
            if (!tasks.Contains(distributedTask.Id))
            {
                tasks.Add(distributedTask.Id);
                await SaveKeysToCache(tasks);
            }
        }

        return distributedTask.Id;
    }

    private Func<DistributedTask, Task> GetPublication()
    {
        return async task =>
        {
            var fromCache = task as T ?? await PeekTask(task.Id);

            fromCache.LastModifiedOn = DateTime.UtcNow;

            await SaveToCache(fromCache);
            logger.TracePublicationDistributedTask(task.Id, task.InstanceId);
        };
    }

    private async Task SaveToCache(T queueTask)
    {
        await hybridCache.SetAsync(_name + queueTask.Id, queueTask, TimeSpan.FromDays(1));
    }
    
    public async Task SaveKeysToCache(List<string> queueTasks)
    {
        await hybridCache.SetAsync(_name, queueTasks, TimeSpan.FromDays(1));
    }
    
    public async Task<List<string>> LoadKeysFromCache()
    {
        return await hybridCache.GetOrDefaultAsync<List<string>>(_name) ?? [];
    }
}

public class DistributedTaskQueueService<T>(
    IServiceProvider serviceProvider,
    ChannelReader<T> channelReader,
    IDistributedLockProvider distributedLockProvider
) : BackgroundService   where T : DistributedTask
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(10));
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = serviceProvider.CreateAsyncScope();
        var queueFactory = scope.ServiceProvider.GetRequiredService<IDistributedTaskQueueFactory>();
        var queue = queueFactory.CreateQueue<T>();
        var maxDegreeOfParallelism = queue.MaxThreadsCount;

        var readers = maxDegreeOfParallelism == 0 ? [channelReader] : channelReader.Split(maxDegreeOfParallelism, cancellationToken: stoppingToken);
        
        var tasks = readers.Select(reader1 => Task.Run(async () =>
        {
            await foreach (var distributedTask in reader1.ReadAllAsync(stoppingToken))
            {        
                var task = distributedTask.RunJob(stoppingToken);
                await task.ContinueWith(async t => await OnCompleted(t, distributedTask), stoppingToken).ConfigureAwait(false);
            }
        }, stoppingToken)).ToList();

        var cleanerTask = Task.Run(async () =>
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var queueTasks = await queue.LoadKeysFromCache();
                var toRemove = new List<string>();
                
                foreach (var q in queueTasks)
                {
                    var task = await queue.PeekTask(q);
                    if (task == null)
                    {
                        toRemove.Add(q);
                    }
                    else if(task.LastModifiedOn.AddSeconds(queue.TimeUntilUnregisterInSeconds) < now)
                    {
                        toRemove.Add(q);
                        await queue.DequeueTask(q);
                    }
                }
                
                if (toRemove.Count > 0)
                { 
                    await using (await distributedLockProvider.TryAcquireFairLockAsync($"{queue.Name}_lock"))
                    {                    
                        var queueTasksFromCache = await queue.LoadKeysFromCache();
                        await queue.SaveKeysToCache(queueTasksFromCache.Except(toRemove).ToList());
                    }

                }
            }
        }, stoppingToken);
        
        tasks.Add(cleanerTask);
        
        await Task.WhenAll(tasks);
    }

    private static async Task OnCompleted(Task task, DistributedTask distributedTask)
    {
        distributedTask.Status = DistributedTaskStatus.Completed;
        if (task.Exception != null)
        {
            distributedTask.Exception = task.Exception;
        }

        if (task.IsFaulted)
        {
            distributedTask.Status = DistributedTaskStatus.Failted;
        }

        if (task.IsCanceled)
        {
            distributedTask.Status = DistributedTaskStatus.Canceled;
        }

        await distributedTask.PublishChanges();
    }
}