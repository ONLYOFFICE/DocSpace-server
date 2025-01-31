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

using ASC.Files.Core.Services.NotifyService;

namespace ASC.Files.Service.IntegrationEvents.EventHandling;

[Singleton]
public class RoomNotifyIntegrationEventHandler(
    ILogger<RoomNotifyIntegrationEventHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
    : IIntegrationEventHandler<RoomNotifyIntegrationEvent>
{
    
    public async Task Handle(RoomNotifyIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();

        using (logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            using var scope = serviceScopeFactory.CreateScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

            await tenantManager.SetCurrentTenantAsync(@event.TenantId);

            if (@event.Data != null)
            {
                await AddMessage(@event.Data.RoomId, @event.Data.FileId, @event.CreateBy, @event.TenantId, scope);
            }
            if (@event.ThirdPartyData != null)
            {
                await AddMessage(@event.Data.RoomId, @event.Data.FileId, @event.CreateBy, @event.TenantId, scope);
            }

        }
    }

    private async Task AddMessage<T>(T roomId, T fileId, Guid createBy, int tenantId, IServiceScope scope)
    {
        var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
        var roomNotifyEventQueue = scope.ServiceProvider.GetRequiredService<INotifyQueueManager<T>>();

        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var queue = roomNotifyEventQueue.GetOrCreateRoomQueue(tenantId, await folderDao.GetFolderAsync(roomId), createBy);
        queue.AddMessage(await fileDao.GetFileAsync(fileId));
    }
}