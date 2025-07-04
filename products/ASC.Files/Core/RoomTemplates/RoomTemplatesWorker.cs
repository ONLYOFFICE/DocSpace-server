﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.RoomTemplates;

[Singleton]
public class RoomTemplatesWorker(
    IDistributedTaskQueueFactory queueFactory,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
{
    private readonly DistributedTaskQueue<CreateRoomTemplateOperation> _templateQueue = queueFactory.CreateQueue<CreateRoomTemplateOperation>();
    private readonly DistributedTaskQueue<CreateRoomFromTemplateOperation> _roomQueue = queueFactory.CreateQueue<CreateRoomFromTemplateOperation>();
    public const string LockKey = $"lock_room_templates";

    public async Task<string> StartCreateTemplateAsync(int tenantId,
        Guid userId,
        int roomId, 
        string title, 
        IEnumerable<string> emails, 
        LogoSettings logo,
        bool CopyLogo,
        IEnumerable<string> tags, 
        IEnumerable<Guid> groups,
        string cover,
        string color,
        long? quota,
        bool enqueueTask = true, 
        string taskId = null)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var item = (await _templateQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);

            if (item is { IsCompleted: true })
            {
                await _templateQueue.DequeueTask(item.Id);
                item = null;
            }
            if (item == null || (enqueueTask && item.Id == taskId && item.Status == DistributedTaskStatus.Created))
            {

                item = serviceProvider.GetService<CreateRoomTemplateOperation>();

                item.Init(tenantId, userId, roomId, title, emails, logo, CopyLogo, tags, groups, cover, color, quota);

                if (!string.IsNullOrEmpty(taskId))
                {
                    item.Id = taskId;
                }

                if (enqueueTask)
                {
                    await _templateQueue.EnqueueTask(item);
                }
                else
                {
                    await _templateQueue.PublishTask(item);
                }
            }

            return item.Id;
        }
    }

    public async Task<string> StartCreateRoomAsync(int tenantId,
        Guid userId,
        int templateId, 
        string title,
        LogoSettings logo,
        bool copyLogo,
        IEnumerable<string> tags,
        string cover,
        string color,
        long? quota,
        bool? indexing,
        bool? denyDownload,
        RoomLifetime lifetime,
        WatermarkRequest watermark,
        bool? @private,
        bool enqueueTask = true,
        string taskId = null)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var item = (await _roomQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);

            if (item is { IsCompleted: true })
            {
                await _roomQueue.DequeueTask(item.Id);
                item = null;
            }
            if (item == null || (enqueueTask && item.Id == taskId && item.Status == DistributedTaskStatus.Created))
            {
                item = serviceProvider.GetService<CreateRoomFromTemplateOperation>();

                item.Init(tenantId, userId, templateId, title, logo, copyLogo, tags, cover, color, quota, indexing, denyDownload, lifetime, watermark, @private);

                if (!string.IsNullOrEmpty(taskId))
                {
                    item.Id = taskId;
                }

                if (enqueueTask)
                {
                    await _roomQueue.EnqueueTask(item);
                }
                else
                {
                    await _roomQueue.PublishTask(item);
                }
            }

            return item.Id;
        }
    }

    public async Task<CreateRoomTemplateOperation> GetStatusTemplateCreatingAsync(int tenantId)
    {
        return (await _templateQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);
    }

    public async Task<CreateRoomFromTemplateOperation> GetStatusRoomCreatingAsync(int tenantId)
    {
        return (await _roomQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);
    }
}
