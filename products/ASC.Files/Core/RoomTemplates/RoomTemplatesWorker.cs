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