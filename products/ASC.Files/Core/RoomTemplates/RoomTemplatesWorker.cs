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

using ASC.Files.Core.RoomTemplates.Events;
using ASC.Files.Core.RoomTemplates.Operations;

namespace ASC.Files.Core.RoomTemplates;

[Singleton]
public class RoomTemplatesWorker(
    IDistributedTaskQueueFactory queueFactory,
    IServiceProvider serviceProvider)
{
    private static readonly SemaphoreSlim _semaphoreSlim = new(1);
    private readonly DistributedTaskQueue _queue = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);

    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "room_templates";

    public async Task StartCreateTemplateAsync(int tenantId, Guid userId, int roomId, string title, IEnumerable<string> emails, LogoSettings logo, IEnumerable<string> tags)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            var item = (await _queue.GetAllTasks<CreateRoomTemplateOperation>()).FirstOrDefault(t => t.TenantId == tenantId);

            if (item is { IsCompleted: true })
            {
                await _queue.DequeueTask(item.Id);
                item = null;
            }

            if (item == null)
            {
                item = serviceProvider.GetService<CreateRoomTemplateOperation>();

                item.Init(tenantId, userId, roomId, title, emails, logo, tags);

                await _queue.EnqueueTask(item);
            }

            await item.PublishChanges();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task StartCreateRoomAsync(int tenantId, Guid userId, int templateId, string title, LogoSettings logo, IEnumerable<string> tags)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            var item = (await _queue.GetAllTasks<CreateRoomFromTemplateOperation>()).FirstOrDefault(t => t.TenantId == tenantId);

            if (item is { IsCompleted: true })
            {
                await _queue.DequeueTask(item.Id);
                item = null;
            }

            if (item == null)
            {
                item = serviceProvider.GetService<CreateRoomFromTemplateOperation>();

                item.Init(tenantId, userId, templateId, title, logo, tags);

                await _queue.EnqueueTask(item);
            }

            await item.PublishChanges();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<CreateRoomTemplateOperation> GetStatusTemplateCreatingAsync(int tenantId)
    {
        return (await _queue.GetAllTasks<CreateRoomTemplateOperation>()).FirstOrDefault(t => t.TenantId == tenantId);
    }

    public async Task<CreateRoomFromTemplateOperation> GetStatusRoomCreatingAsync(int tenantId)
    {
        return (await _queue.GetAllTasks<CreateRoomFromTemplateOperation>()).FirstOrDefault(t => t.TenantId == tenantId);
    }
}
