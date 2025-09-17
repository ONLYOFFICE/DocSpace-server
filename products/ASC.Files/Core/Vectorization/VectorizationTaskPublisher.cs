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

namespace ASC.Files.Core.Vectorization;

[Scope]
public class VectorizationTaskPublisher(
    TenantManager tenantManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    VectorizationTaskService vectorizationTaskService,
    IDaoFactory daoFactory,
    IDistributedLockProvider distributedLockProvider,
    FileSecurity fileSecurity,
    SocketManager socketManager)
{
    public async Task<VectorizationTask> PublishAsync(int fileId)
    {
        if (fileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileId), @"File id must be greater than 0");
        }
        
        var fileDao = daoFactory.GetFileDao<int>();
        
        var file = await fileDao.GetFileAsync(fileId);
        if (file.VectorizationStatus is not VectorizationStatus.Failed)
        {
            throw new InvalidOperationException();
        }

        if (!await fileSecurity.CanReadAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        
        var parentFolder = await folderDao.GetFolderAsync(file.ParentId);
        if (parentFolder is not { FolderType: FolderType.Knowledge })
        {
            throw new InvalidOperationException();
        }
        
        var task = await PublishAsync(file);
        
        file.VectorizationStatus = VectorizationStatus.InProgress;
        await fileDao.SaveFileAsync(file, null);
        
        await socketManager.UpdateFileAsync(file);
        
        return task;
    }
    
    public async Task<VectorizationTask> PublishAsync(File<int> file)
    {
        if (file == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var room = await DocSpaceHelper.GetParentRoom(file, daoFactory.GetFolderDao<int>());
        if (room is not { FolderType: FolderType.AiRoom })
        {
            throw new InvalidOperationException();
        }
        
        await using (await distributedLockProvider.TryAcquireLockAsync($"vectorization_task_{file.Id}"))
        {
            var tasks = await vectorizationTaskService.GetTasksAsync();
            var existingTask = tasks.FirstOrDefault(x => x.FileId == file.Id);
            if (existingTask is { IsCompleted: false })
            {
                return existingTask;
            }
        
            var task = serviceProvider.GetRequiredService<VectorizationTask>();
            var tenantId = tenantManager.GetCurrentTenantId();
            var userId = authContext.CurrentAccount.ID;
        
            task.Init(tenantId, userId, file.Id, room.Id);
        
            var taskId = await vectorizationTaskService.StoreAsync(task);
        
            await eventBus.PublishAsync(new VectorizationIntegrationEvent(userId, tenantId)
            {
                TaskId = taskId,
                FileId = file.Id,
                RoomId = room.Id
            });
            
            return (await vectorizationTaskService.GetAsync(taskId))!;
        }
    }
}