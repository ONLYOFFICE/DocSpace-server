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

namespace ASC.Data.Reassigns;

public class QueueWorker<T>(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider,
    IDistributedTaskQueueFactory queueFactory,
    IDistributedLockProvider distributedLockProvider)
    where T : DistributedTaskProgress
{
    protected readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly DistributedTaskQueue<T> _queue = queueFactory.CreateQueue<T>();
    protected readonly IDictionary<string, StringValues> _httpHeaders = MessagingSystem.MessageSettings.GetHttpHeaders(httpContextAccessor.HttpContext?.Request);

    public static string GetProgressItemId(int tenantId, Guid userId)
    {
        return $"{tenantId}_{userId}_{typeof(T).Name}";
    }

    public async Task<T> GetProgressItemStatus(int tenantId, Guid userId)
    {
        var id = GetProgressItemId(tenantId, userId);

        return await _queue.PeekTask(id);
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

[Scope]
public class QueueWorkerReassign(IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedTaskQueueFactory queueFactory,
        IDistributedLockProvider distributedLockProvider)
    : QueueWorker<ReassignProgressItem>(httpContextAccessor, serviceProvider, queueFactory, distributedLockProvider)
{
    public async Task<ReassignProgressItem> StartAsync(int tenantId, Guid fromUserId, Guid toUserId, Guid currentUserId, bool notify, bool deleteProfile)
    {
        var result = _serviceProvider.GetService<ReassignProgressItem>();

        result.Init(_httpHeaders, tenantId, fromUserId, toUserId, currentUserId, notify, deleteProfile);

        return await StartAsync(tenantId, fromUserId, result);
    }
}

[Scope]
public class QueueWorkerUpdateUserType(IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedTaskQueueFactory queueFactory,
        IDistributedLockProvider distributedLockProvider)
    : QueueWorker<UpdateUserTypeProgressItem>(httpContextAccessor, serviceProvider, queueFactory, distributedLockProvider)
{
    public async Task<UpdateUserTypeProgressItem> StartAsync(int tenantId, Guid userId, Guid toUserId, Guid currentUserId, EmployeeType employeeType)
    {
        var result = _serviceProvider.GetService<UpdateUserTypeProgressItem>();

        result.Init(tenantId, userId, toUserId, currentUserId, employeeType, _httpHeaders);

        return await StartAsync(tenantId, userId, result);
    }
}

[Scope]
public class QueueDeletePersonalFolder(IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedTaskQueueFactory queueFactory,
        IDistributedLockProvider distributedLockProvider,
        IDaoFactory daoFactory)
    : QueueWorker<DeletePersonalFolderProgressItem>(httpContextAccessor, serviceProvider, queueFactory, distributedLockProvider)
{
    public async Task<DeletePersonalFolderProgressItem> StartAsync(int tenantId, Guid userId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var myId = await folderDao.GetFolderIDUserAsync(false, userId);
        if (myId == 0)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var result = _serviceProvider.GetService<DeletePersonalFolderProgressItem>();

        result.Init(userId, tenantId);

        return await StartAsync(tenantId, userId, result);
    }
}

[Scope]
public class QueueWorkerRemove(IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedTaskQueueFactory queueFactory,
        IDistributedLockProvider distributedLockProvider)
    : QueueWorker<RemoveProgressItem>(httpContextAccessor, serviceProvider, queueFactory, distributedLockProvider)
{
    public async Task<RemoveProgressItem> StartAsync(int tenantId, UserInfo user, Guid currentUserId, bool notify, bool deleteProfile, bool isGuest)
    {
        var result = _serviceProvider.GetService<RemoveProgressItem>();

        result.Init(_httpHeaders, tenantId, user, currentUserId, notify, deleteProfile, isGuest);

        return await StartAsync(tenantId, user.Id, result);
    }
}