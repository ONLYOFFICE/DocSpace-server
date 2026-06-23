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

using ASC.Files.Core.Services.NotifyService;

namespace ASC.Files.Worker.IntegrationEvents.EventHandling;

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
                await AddMessage(@event.ThirdPartyData.RoomId, @event.ThirdPartyData.FileId, @event.CreateBy, @event.TenantId, scope);
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