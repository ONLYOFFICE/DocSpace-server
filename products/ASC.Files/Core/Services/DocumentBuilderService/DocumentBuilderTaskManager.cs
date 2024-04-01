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

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Singleton(Additional = typeof(DocumentBuilderTaskManagerHelperExtension))]
public class DocumentBuilderTaskManager
{
    private static readonly SemaphoreSlim _semaphore = new(1);

    private readonly DistributedTaskQueue _queue;

    public DocumentBuilderTaskManager(IDistributedTaskQueueFactory queueFactory)
    {
        _queue = queueFactory.CreateQueue(GetType());
    }

    public static string GetTaskId(int tenantId, Guid userId)
    {
        return $"DocumentBuilderTask_{tenantId}_{userId}";
    }

    public async Task<DistributedTaskProgress> GetTask(int tenantId, Guid userId)
    {
        var taskId = GetTaskId(tenantId, userId);

        return await GetTask(taskId);
    }

    private async Task<DistributedTaskProgress> GetTask(string taskId)
    {
        return await _queue.PeekTask<DistributedTaskProgress>(taskId);
    }

    public async Task TerminateTask(int tenantId, Guid userId)
    {
        var task = await GetTask(tenantId, userId);

        if (task != null)
        {
            await _queue.DequeueTask(task.Id);
        }
    }

    public async Task<DistributedTaskProgress> StartTask<T>(DocumentBuilderTask<T> newTask, bool enqueueTask = true)
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

public static class DocumentBuilderTaskManagerHelperExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<DocumentBuilderTask>();
    }
}