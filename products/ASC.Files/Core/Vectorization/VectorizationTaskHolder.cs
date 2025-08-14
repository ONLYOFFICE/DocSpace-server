// (c) Copyright Ascensio System SIA 2009-2025
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

#nullable enable

using ASC.Files.Core.Vectorization.Copy;
using ASC.Files.Core.Vectorization.Upload;

namespace ASC.Files.Core.Vectorization;

[Scope]
public class VectorizationTaskHolder(
    AuthContext authContext,
    VectorizationTaskService<CopyVectorizationTask, CopyVectorizationTaskData> copyVectorizationTaskService,
    VectorizationTaskService<UploadVectorizationTask, UploadVectorizationTaskData> uploadVectorizationTaskService)
{
    public async Task<VectorizationTask?> GetAsync(string id)
    {
        var (taskId, type) = VectorizationTaskIdHelper.ProcessId(id);
        
        VectorizationTask? task = type is VectorizationTaskType.Copy
            ? await copyVectorizationTaskService.GetAsync(taskId)
            : await uploadVectorizationTaskService.GetAsync(taskId);
        
        if (task == null || task.UserId != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException("Task not found");
        }

        if (task.Status <= DistributedTaskStatus.Running)
        {
            return task;
        }

        task.Percentage = 100;

        if (type is VectorizationTaskType.Copy)
        {
            await copyVectorizationTaskService.DeleteAsync(task.Id);
        }
        else
        {
            await uploadVectorizationTaskService.DeleteAsync(task.Id);
        }

        return task;
    }

    public async IAsyncEnumerable<VectorizationTask> GetAsync()
    {
        var copyTasks = await copyVectorizationTaskService.GetTasksAsync();
        foreach (var task in copyTasks)
        {
            if (!await CheckTaskAsync(task, copyVectorizationTaskService))
            {
                continue;
            }

            yield return task;
        }
        
        var uploadTasks = await uploadVectorizationTaskService.GetTasksAsync();
        foreach (var task in uploadTasks)
        {
            if (!await CheckTaskAsync(task, uploadVectorizationTaskService))
            {
                continue;
            }

            yield return task;
        }

        yield break;

        async Task<bool> CheckTaskAsync<T, TData>(VectorizationTask task, VectorizationTaskService<T, TData> service) 
            where T : VectorizationTask<TData> 
            where TData : VectorizationTaskData
        {
            if (task.UserId != authContext.CurrentAccount.ID)
            {
                return false;
            }

            if (task.Status <= DistributedTaskStatus.Running)
            {
                return true;
            }

            task.Percentage = 100;
            await service.DeleteAsync(task.Id);

            return true;
        }
    }

    public async Task TerminateAsync(string id)
    {
        var task = await copyVectorizationTaskService.GetAsync(id);
        if (task == null || task.UserId != authContext.CurrentAccount.ID)
        {
            return;
        }
        
        await copyVectorizationTaskService.DeleteAsync(task.Id);
    }
}