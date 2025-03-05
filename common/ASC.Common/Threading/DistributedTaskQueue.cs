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

namespace ASC.Common.Threading;

[Transient]
public class DistributedTaskQueue<T>(
    ChannelWriter<T> channelWriter,
    ICacheNotify<DistributedTaskCancelation> cancelTaskNotify,
    IFusionCache hybridCache,
    ILogger<DistributedTaskQueue<T>> logger)  where T : DistributedTask
{
    public const string QUEUE_DEFAULT_PREFIX = "asc_distributed_task_queue_";
    public static readonly int INSTANCE_ID = Environment.ProcessId;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancelations = new();
    private bool _subscribed;

    /// <summary>
    /// setup -1 for infinity thread counts
    /// </summary>
    private int _maxThreadsCount = 1;
    private string _name;
    private TaskScheduler Scheduler { get; set; } = TaskScheduler.Default;

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
            Scheduler = value <= 0
                ? TaskScheduler.Default
                : new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, value).ConcurrentScheduler;

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
        var queueTasks = await LoadFromCache();

        queueTasks = await DeleteOrphanCacheItem(queueTasks);

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
        var taskById = (await GetAllTasks()).FirstOrDefault(x => x.Id == id);

        return taskById;
    }

    public async Task DequeueTask(string id)
    {
        var queueTasks = (await GetAllTasks()).ToList();

        if (!queueTasks.Exists(x => x.Id == id))
        {
            return;
        }

        await cancelTaskNotify.PublishAsync(new DistributedTaskCancelation { Id = id }, CacheNotifyAction.Remove);

        queueTasks = queueTasks.FindAll(x => x.Id != id);

        if (queueTasks.Count == 0)
        {
            await hybridCache.RemoveAsync(_name);
        }
        else
        {
            await SaveToCache(queueTasks);
        }

        logger.TraceEnqueueTask(id, INSTANCE_ID);

    }

    public async Task<string> PublishTask(T distributedTask)
    {
        distributedTask.Publication ??= GetPublication();
        await distributedTask.PublishChanges();

        return distributedTask.Id;
    }

    private Func<DistributedTask, Task> GetPublication()
    {
        return async task =>
        {
            var allTasks = (await GetAllTasks()).ToList();
            var queueTasks = allTasks.FindAll(x => x.Id != task.Id);

            task.LastModifiedOn = DateTime.UtcNow;

            queueTasks.Add((T)task);

            await SaveToCache(queueTasks);
            logger.TracePublicationDistributedTask(task.Id, task.InstanceId);
        };
    }


    private async Task SaveToCache(List<T> queueTasks)
    {
        if (queueTasks.Count == 0)
        {
            await hybridCache.RemoveAsync(_name);

            return;
        }
        
        await hybridCache.SetAsync(_name, queueTasks, TimeSpan.FromDays(1));

    }
    
    private async Task<List<T>> LoadFromCache()
    {
        return await hybridCache.GetOrDefaultAsync<List<T>>(_name) ?? [];
    }

    private async Task<List<T>> DeleteOrphanCacheItem(IEnumerable<T> queueTasks)
    {
        var listTasks = queueTasks.ToList();

        if (listTasks.RemoveAll(IsOrphanCacheItem) > 0)
        {
            await SaveToCache(listTasks);
        }

        return listTasks;
    }

    private bool IsOrphanCacheItem(T obj)
    {
        return obj.LastModifiedOn.AddSeconds(TimeUntilUnregisterInSeconds) < DateTime.UtcNow;
    }
}

public class DistributedTaskQueueService<T>(
    ChannelReader<T> channelReader,
    IConfiguration configuration
) : BackgroundService   where T : DistributedTask
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    { 
        if(!int.TryParse(configuration["web:hub:maxDegreeOfParallelism"], out var maxDegreeOfParallelism))
        {
            maxDegreeOfParallelism = 10;
        }

        var readers = channelReader.Split(maxDegreeOfParallelism);
        
        var tasks = readers.Select(reader1 => Task.Run(async () =>
        {
            await foreach (var distributedTask in reader1.ReadAllAsync(stoppingToken))
            {        
                var task = distributedTask.RunJob(stoppingToken);
                await task.ContinueWith(async t => await OnCompleted(t, distributedTask), stoppingToken);
                await task.ConfigureAwait(false);
            }
        }, stoppingToken)).ToList();

        await Task.WhenAll(tasks);
    }

    private async Task OnCompleted(Task task, DistributedTask distributedTask)
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