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

using IDistributedLockProvider = ASC.Common.Threading.DistributedLock.Abstractions.IDistributedLockProvider;

namespace ASC.Common.Threading;

[Transient]
public class DistributedTaskQueue<T>(
    ChannelWriter<T> channelWriter,
    ICacheNotify<DistributedTaskCancelation> cancelTaskNotify,
    IFusionCache hybridCache,
    ILogger<DistributedTaskQueue<T>> logger,
    IDistributedLockProvider distributedLockProvider) where T : DistributedTask
{
    public const string QUEUE_DEFAULT_PREFIX = "asc_distributed_task_queue_v2_";
    public static readonly int INSTANCE_ID = Environment.ProcessId;

    private string _name;
    public string LockKey => $"{Name}_lock";
    public int TimeUntilUnregisterInSeconds { get; set; }

    public string Name
    {
        get => _name;
        set => _name = QUEUE_DEFAULT_PREFIX + value;
    }

    public int MaxThreadsCount
    {
        get;

        set
        {
            if (value > 0)
            {
                field = value;
            }
        }
    } = 1;

    public async Task EnqueueTask(T distributedTask)
    {
        distributedTask.InstanceId = INSTANCE_ID;

        if (distributedTask.LastModifiedOn.Equals(DateTime.MinValue))
        {
            distributedTask.LastModifiedOn = DateTime.UtcNow;
        }


        await channelWriter.WriteAsync(distributedTask);

        distributedTask.Status = DistributedTaskStatus.Running;

        await PublishTask(distributedTask);

        logger.TraceEnqueueTask(distributedTask.Id, INSTANCE_ID);
    }

    public async Task<List<T>> GetAllTasks(int? instanceId = null)
    {
        List<string> keys;

        await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKey))
        {
            keys = (await LoadKeysFromCache()).ToList();
        }

        List<T> result = [];

        foreach (var key in keys)
        {
            var task = await PeekTask(key);

            if (task == null)
            {
                continue;
            }

            if (instanceId == null || task.InstanceId == instanceId.Value)
            {
                result.Add(task);
            }

            task.Publication ??= GetPublication();
        }

        return result;
    }


    public async Task<T> PeekTask(string id)
    {
        return await hybridCache.GetOrDefaultAsync<T>(_name + id);
    }

    public async Task DequeueTask(string id)
    {
        // the record goes first: a worker that reacts to the notify writes its final state after this removal,
        // never before it - in the other order the removal could wipe the record the cancelled task had just written
        await hybridCache.RemoveAsync(_name + id);

        await cancelTaskNotify.PublishAsync(new DistributedTaskCancelation { Id = id }, CacheNotifyAction.Remove);

        logger.TraceEnqueueTask(id, INSTANCE_ID);
    }

    public async Task<string> PublishTask(T distributedTask)
    {
        distributedTask.Publication ??= GetPublication();
        await distributedTask.PublishChanges();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKey))
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
    IDistributedLockProvider distributedLockProvider,
    ICacheNotify<DistributedTaskCancelation> cancelTaskNotify,
    ILogger<DistributedTaskQueueService<T>> logger
) : BackgroundService where T : DistributedTask
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(10));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        cancelTaskNotify.Subscribe(c =>
        {
            if (_cancellations.TryGetValue(c.Id, out var s))
            {
                s.Cancel();
            }
        }, CacheNotifyAction.Remove);

        var scope = serviceProvider.CreateAsyncScope();
        var queueFactory = scope.ServiceProvider.GetRequiredService<IDistributedTaskQueueFactory>();
        var queue = queueFactory.CreateQueue<T>();
        var maxDegreeOfParallelism = queue.MaxThreadsCount;

        var readers = maxDegreeOfParallelism == 0 ? [channelReader] : channelReader.Split(maxDegreeOfParallelism, cancellationToken: stoppingToken);

        var tasks = readers.Select(reader1 => Task.Run(async () =>
        {
            await foreach (var distributedTask in reader1.ReadAllAsync(stoppingToken))
            {
                // a failure of one task (including transient Redis/cache errors while publishing its
                // status) must not fault the reader loop: that would stop the host process itself
                try
                {
                    // not disposed: the cancel notify may still hold it after the task is removed from the dictionary
                    var cancellation = new CancellationTokenSource();
                    var token = cancellation.Token;
                    _cancellations[distributedTask.Id] = cancellation;

                    try
                    {
                        var task = distributedTask.RunJob(token);
                        await task.ContinueWith(async t => await OnCompleted(t, distributedTask, queue, token), CancellationToken.None).Unwrap().ConfigureAwait(false);
                    }
                    finally
                    {
                        // ids are reused (a report is restarted under the same id), so only this run's entry goes
                        _cancellations.TryRemove(new KeyValuePair<string, CancellationTokenSource>(distributedTask.Id, cancellation));
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    logger.ErrorWithException(e);
                }
            }
        }, stoppingToken)).ToList();

        var cleanerTask = Task.Run(async () =>
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                // a failed sweep (e.g. Redis connection blip in DequeueTask) is retried on the next
                // tick; letting it fault would take down the whole host process via StopHost
                try
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
                        else if (task.LastModifiedOn.AddSeconds(queue.TimeUntilUnregisterInSeconds) < now)
                        {
                            toRemove.Add(q);
                            await queue.DequeueTask(q);
                        }
                    }

                    if (toRemove.Count > 0)
                    {
                        await using (await distributedLockProvider.TryAcquireFairLockAsync(queue.LockKey, cancellationToken: stoppingToken))
                        {
                            // a task that finished after the sweep looked at it (a cancelled one republishes its final
                            // state) has a record again and keeps its key; under the lock, as OnCompleted adds keys under it
                            var stillGone = new List<string>();
                            foreach (var id in toRemove)
                            {
                                if (await queue.PeekTask(id) == null)
                                {
                                    stillGone.Add(id);
                                }
                            }

                            var queueTasksFromCache = await queue.LoadKeysFromCache();
                            await queue.SaveKeysToCache(queueTasksFromCache.Except(stillGone).ToList());
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    logger.ErrorWithException(e);
                }
            }
        }, stoppingToken);

        tasks.Add(cleanerTask);

        await Task.WhenAll(tasks);
    }

    private static async Task OnCompleted(Task task, DistributedTask distributedTask, DistributedTaskQueue<T> queue, CancellationToken token)
    {
        if (distributedTask.Status is not (DistributedTaskStatus.Failted or DistributedTaskStatus.Canceled))
        {
            // a job that swallows its cancellation still ends normally; the token tells a cancelled one from a finished one
            distributedTask.Status = token.IsCancellationRequested ? DistributedTaskStatus.Canceled : DistributedTaskStatus.Completed;
        }

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

        // a cancel removes the record, and the cleaner may already have dropped its id from the key list, after which
        // the final state would sit where no listing finds it: the key is put back then. Only then, so that an ordinary
        // finish does not take the queue's lock
        if (!(await queue.LoadKeysFromCache()).Contains(distributedTask.Id))
        {
            await queue.PublishTask((T)distributedTask);
        }
    }
}
