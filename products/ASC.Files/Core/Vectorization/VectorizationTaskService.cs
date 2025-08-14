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

[Singleton(GenericArguments = [typeof(CopyVectorizationTask), typeof(CopyVectorizationTaskData)])]
[Singleton(GenericArguments = [typeof(UploadVectorizationTask), typeof(UploadVectorizationTaskData)])]
public class VectorizationTaskService<T, TData>(
    IDistributedTaskQueueFactory queueFactory) 
    where T : VectorizationTask<TData> 
    where TData : VectorizationTaskData
{
    private readonly DistributedTaskQueue<T> _queue = queueFactory.CreateQueue<T>();

    public Task StartAsync(T task)
    {
        return _queue.EnqueueTask(task);
    }

    public Task<string> StoreAsync(T task)
    {
        return _queue.PublishTask(task);
    }

    public async Task<T?> GetAsync(string id)
    {
        return await _queue.PeekTask(id);
    }

    public async Task<List<T>> GetTasksAsync()
    {
        return await _queue.GetAllTasks();
    }
    
    public async Task DeleteAsync(string id)
    {
        await _queue.DequeueTask(id);
    }
}