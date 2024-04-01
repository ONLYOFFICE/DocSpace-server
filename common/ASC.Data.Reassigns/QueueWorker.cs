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

namespace ASC.Data.Reassigns;

public class QueueWorker<T>(IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider,
    IDistributedTaskQueueFactory queueFactory,
    string queueName,
    IDistributedLockProvider distributedLockProvider)
    where T : DistributedTaskProgress
{
    protected readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly DistributedTaskQueue _queue = queueFactory.CreateQueue(queueName);
    protected readonly IDictionary<string, StringValues> _httpHeaders = httpContextAccessor.HttpContext?.Request.Headers;

    public static string GetProgressItemId(int tenantId, Guid userId)
    {
        return $"{tenantId}_{userId}_{typeof(T).Name}";
    }

    public async Task<T> GetProgressItemStatus(int tenantId, Guid userId)
    {
        var id = GetProgressItemId(tenantId, userId);

        return await _queue.PeekTask<T>(id);
    }

    public async Task Terminate(int tenantId, Guid userId)
    {
        var item = await GetProgressItemStatus(tenantId, userId);

        if (item != null)
        {
            await _queue.DequeueTask(item.Id);
        }
    }

    protected async Task<T> StartAsync(int tenantId, Guid userId, T newTask)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync($"lock_{_queue.Name}"))
        {
            var task = await GetProgressItemStatus(tenantId, userId);

            if (task is { IsCompleted: true })
            {
                await _queue.DequeueTask(task.Id);
                task = null;
            }

            if (task == null)
            {
                task = newTask;
                await _queue.EnqueueTask(task);
            }

            return task;
        }
    }
}

[Scope(Additional = typeof(ReassignProgressItemExtension))]
public class QueueWorkerReassign(IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedTaskQueueFactory queueFactory,
        IDistributedLockProvider distributedLockProvider)
    : QueueWorker<ReassignProgressItem>(httpContextAccessor, serviceProvider, queueFactory, CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME, distributedLockProvider)
{
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "user_data_reassign";

    public async Task<ReassignProgressItem> StartAsync(int tenantId, Guid fromUserId, Guid toUserId, Guid currentUserId, bool notify, bool deleteProfile)
    {
        var result = _serviceProvider.GetService<ReassignProgressItem>();

        result.Init(_httpHeaders, tenantId, fromUserId, toUserId, currentUserId, notify, deleteProfile);

        return await StartAsync(tenantId, fromUserId, result);
    }
}

[Scope(Additional = typeof(RemoveProgressItemExtension))]
public class QueueWorkerRemove(IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedTaskQueueFactory queueFactory,
        IDistributedLockProvider distributedLockProvider)
    : QueueWorker<RemoveProgressItem>(httpContextAccessor, serviceProvider, queueFactory, CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME, distributedLockProvider)
{
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "user_data_remove";

    public async Task<RemoveProgressItem> StartAsync(int tenantId, UserInfo user, Guid currentUserId, bool notify, bool deleteProfile)
    {
        var result = _serviceProvider.GetService<RemoveProgressItem>();

        result.Init(_httpHeaders, tenantId, user, currentUserId, notify, deleteProfile);

        return await StartAsync(tenantId, user.Id, result);
    }
}
