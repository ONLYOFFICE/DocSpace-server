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

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Singleton(GenericArguments = [typeof(CustomerOperationsReportTask), typeof(int), typeof(CustomerOperationsReportTaskData)])]
[Singleton(GenericArguments = [typeof(CustomerServiceUsageReportTask), typeof(int), typeof(CustomerServiceUsageReportTaskData)])]
[Singleton(GenericArguments = [typeof(CustomerMonthlyUsageReportTask), typeof(int), typeof(CustomerMonthlyUsageReportTaskData)])]
[Singleton(GenericArguments = [typeof(FormFillingReportTask), typeof(int), typeof(FormFillingReportTaskData)])]
[Singleton(GenericArguments = [typeof(RoomIndexExportTask), typeof(int), typeof(RoomIndexExportTaskData)])]
public class DocumentBuilderTaskManager<T, TId, TData> where T : DocumentBuilderTask<TId, TData>
{
    private static readonly SemaphoreSlim _semaphore = new(1);

    private readonly DistributedTaskQueue<T> _queue;

    public DocumentBuilderTaskManager(IDistributedTaskQueueFactory queueFactory)
    {
        _queue = queueFactory.CreateQueue<T>();
    }

    public async Task<T> GetTask(int tenantId, Guid userId)
    {
        var taskId = DocumentBuilderTaskManager.GetTaskId(tenantId, userId);

        return await GetTask(taskId);
    }

    public async Task<T> GetTask(int tenantId, Guid userId, int formId)
    {
        var taskId = DocumentBuilderTaskManager.GetTaskId(tenantId, userId, formId);

        return await GetTask(taskId);
    }

    private async Task<T> GetTask(string taskId)
    {
        return await _queue.PeekTask(taskId);
    }

    public async Task TerminateTask(int tenantId, Guid userId)
    {
        var task = await GetTask(tenantId, userId);

        if (task != null)
        {
            await _queue.DequeueTask(task.Id);
        }
    }

    public async Task<T> StartTask(T newTask, bool enqueueTask = true)
    {
        try
        {
            await _semaphore.WaitAsync();
            var task = await GetTask(newTask.Id);

            if (task is { IsCompleted: true })
            {
                await _queue.DequeueTask(task.Id);
                task = null;
            }

            if (task == null || (enqueueTask && task.Status == DistributedTaskStatus.Created))
            {
                task = newTask;

                if (enqueueTask)
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
}

public static class DocumentBuilderTaskManager
{
    public static string GetTaskId(int tenantId, Guid userId)
    {
        return $"DocumentBuilderTask_{tenantId}_{userId}";
    }

    public static string GetTaskId(int tenantId, Guid userId, int formId)
    {
        return $"DocumentBuilderTask_{tenantId}_{userId}_{formId}";
    }
}