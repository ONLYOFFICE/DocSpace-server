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
public class DistributedTaskQueue(IServiceProvider serviceProvider,
    ICacheNotify<DistributedTaskCancelation> cancelTaskNotify,
    IDistributedCache distributedCache,
    ILogger<DistributedTaskQueue> logger)
{
    public const string QUEUE_DEFAULT_PREFIX = "asc_distributed_task_queue_";
    public static readonly int INSTANCE_ID = Process.GetCurrentProcess().Id;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancelations = new();
    private bool _subscribed;

    /// <summary>
    /// setup -1 for infinity thread counts
    /// </summary>
    private int _maxThreadsCount = 1;
    private string _name;
    private int _timeUntilUnregisterInSeconds;
    private TaskScheduler Scheduler { get; set; } = TaskScheduler.Default;

    public int TimeUntilUnregisterInSeconds
    {
        get => _timeUntilUnregisterInSeconds;
        set => _timeUntilUnregisterInSeconds = value;
    }

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

    public async Task EnqueueTask(DistributedTaskProgress taskProgress)
    {
        await EnqueueTask(taskProgress.RunJob, taskProgress);
    }

    public async Task EnqueueTask(Func<DistributedTask, CancellationToken, Task> action, DistributedTask distributedTask = null)
    {
        distributedTask ??= new DistributedTask();

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

        var task = new Task(() =>
    {
        var t = action(distributedTask, token);
        t.ContinueWith(async a => await OnCompleted(a, distributedTask.Id), token).ConfigureAwait(false);
        t.ConfigureAwait(false);
    }, token, TaskCreationOptions.LongRunning);

        _ = task.ConfigureAwait(false);

        distributedTask.Status = DistributedTaskStatus.Running;

        await PublishTask(distributedTask);

        task.Start(Scheduler);

        logger.TraceEnqueueTask(distributedTask.Id, INSTANCE_ID);

    }

    public async Task<List<DistributedTask>> GetAllTasks(int? instanceId = null)
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

    public async Task<IEnumerable<T>> GetAllTasks<T>() where T : DistributedTask
    {
        return (await GetAllTasks()).Select(x => Map(x, serviceProvider.GetService<T>())).ToList();
    }

    public async Task<T> PeekTask<T>(string id) where T : DistributedTask
    {
        var taskById = (await GetAllTasks()).FirstOrDefault(x => x.Id == id);

        return taskById == null ? null : Map(taskById, serviceProvider.GetService<T>());
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
            await distributedCache.RemoveAsync(_name);
        }
        else
        {
            await SaveToCache(queueTasks);
        }

        logger.TraceEnqueueTask(id, INSTANCE_ID);

    }

    public async Task<string> PublishTask(DistributedTask distributedTask)
    {
        distributedTask.Publication ??= GetPublication();
        await distributedTask.PublishChanges();

        return distributedTask.Id;
    }

    private async Task OnCompleted(Task task, string id)
    {
        var distributedTask = (await GetAllTasks()).FirstOrDefault(x => x.Id == id);
        if (distributedTask != null)
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

            _cancelations.TryRemove(id, out _);

            await distributedTask.PublishChanges();
        }
    }

    private Func<DistributedTask, Task> GetPublication()
    {
        return async task =>
        {
            var allTasks = (await GetAllTasks()).ToList();
            var queueTasks = allTasks.FindAll(x => x.Id != task.Id);

            task.LastModifiedOn = DateTime.UtcNow;

            queueTasks.Add(task);

            await SaveToCache(queueTasks);
            logger.TracePublicationDistributedTask(task.Id, task.InstanceId);
        };
    }


    private async Task SaveToCache(List<DistributedTask> queueTasks)
    {
        if (queueTasks.Count == 0)
        {
            await distributedCache.RemoveAsync(_name);

            return;
        }

        using var ms = new MemoryStream();

        Serializer.Serialize(ms, queueTasks);

        await distributedCache.SetAsync(_name, ms.ToArray(), new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.UtcNow.AddDays(1)
        });

    }

    private async Task<List<DistributedTask>> LoadFromCache()
    {
        var serializedObject = await distributedCache.GetAsync(_name);

        if (serializedObject == null)
        {
            return [];
        }

        using var ms = new MemoryStream(serializedObject);

        return Serializer.Deserialize<List<DistributedTask>>(ms);
    }

    private async Task<List<DistributedTask>> DeleteOrphanCacheItem(IEnumerable<DistributedTask> queueTasks)
    {
        var listTasks = queueTasks.ToList();

        if (listTasks.RemoveAll(IsOrphanCacheItem) > 0)
        {
            await SaveToCache(listTasks);
        }

        return listTasks;
    }

    private bool IsOrphanCacheItem(DistributedTask obj)
    {
        return obj.LastModifiedOn.AddSeconds(TimeUntilUnregisterInSeconds) < DateTime.UtcNow;
    }


    /// <summary>
    /// Maps the source object to destination object.
    /// </summary>
    /// <typeparam name="T">Type of destination object.</typeparam>
    /// <typeparam name="TU">Type of source object.</typeparam>
    /// <param name="destination">Destination object.</param>
    /// <param name="source">Source object.</param>
    /// <returns>Updated destination object.</returns>
    private T Map<T, TU>(TU source, T destination)
    {
        destination.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .ToList()
                    .ForEach(field =>
                    {
                        var sf = source.GetType().GetField(field.Name, BindingFlags.NonPublic | BindingFlags.Instance);

                        if (sf != null)
                        {
                            var value = sf.GetValue(source);
                            destination.GetType().GetField(field.Name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(destination, value);
                        }
                    });

        destination.GetType().GetProperties().Where(p => p.CanWrite && !p.GetIndexParameters().Any())
                    .ToList()
                    .ForEach(prop =>
                    {
                        var sp = source.GetType().GetProperty(prop.Name);
                        if (sp != null)
                        {
                            var value = sp.GetValue(source, null);
                            destination.GetType().GetProperty(prop.Name).SetValue(destination, value, null);
                        }
                    });



        return destination;
    }
}