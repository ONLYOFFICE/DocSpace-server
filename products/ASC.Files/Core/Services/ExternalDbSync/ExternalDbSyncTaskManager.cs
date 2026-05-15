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

namespace ASC.Files.Core.Services.ExternalDbSync;

[Singleton]
public class ExternalDbSyncTaskManager(IDistributedTaskQueueFactory queueFactory) : IDisposable
{
    private const string TaskIdPrefix = "ExternalDbSyncTask";

    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly SemaphoreSlim _busyCheckLock = new(1, 1);
    private readonly DistributedTaskQueue<ExternalDbSyncTask> _queue = queueFactory.CreateQueue<ExternalDbSyncTask>();

    public static string GetTaskId(int tenantId, int roomId) => $"{TaskIdPrefix}_{tenantId}_{roomId}";

    public async Task<ExternalDbSyncTask> GetTask(int tenantId, int roomId)
    {
        return await _queue.PeekTask(GetTaskId(tenantId, roomId));
    }

    public async Task<bool> IsTooBusy()
    {
        await _busyCheckLock.WaitAsync();
        try
        {
            var instanceTasks = await _queue.GetAllTasks(DistributedTaskQueue<ExternalDbSyncTask>.INSTANCE_ID);
            return _queue.MaxThreadsCount < instanceTasks.Count;
        }
        finally
        {
            _busyCheckLock.Release();
        }
    }

    public Task<ExternalDbSyncTask> PublishTaskAsync(ExternalDbSyncTask task) => StartTaskCoreAsync(task, enqueue: false);

    public Task<ExternalDbSyncTask> EnqueueTaskAsync(ExternalDbSyncTask task) => StartTaskCoreAsync(task, enqueue: true);

    private async Task<ExternalDbSyncTask> StartTaskCoreAsync(ExternalDbSyncTask newTask, bool enqueue)
    {
        try
        {
            await _semaphore.WaitAsync();

            var task = await _queue.PeekTask(newTask.Id);

            if (task is { IsCompleted: true })
            {
                await _queue.DequeueTask(task.Id);
                task = null;
            }

            if (task == null || (enqueue && task.Status == DistributedTaskStatus.Created))
            {
                task = newTask;

                if (enqueue)
                {
                    await _queue.EnqueueTask(task);
                }
                else
                {
                    await _queue.PublishTask(task);
                }
            }

            return task;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        _busyCheckLock?.Dispose();
    }
}
