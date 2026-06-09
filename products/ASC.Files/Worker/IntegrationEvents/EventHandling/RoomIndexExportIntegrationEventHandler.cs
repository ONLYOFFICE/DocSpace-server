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

namespace ASC.Files.Worker.IntegrationEvents.EventHandling;

[Scope]
public class RoomIndexExportIntegrationEventHandler(
    ILogger<RoomIndexExportIntegrationEventHandler> logger,
    CommonLinkUtility commonLinkUtility,
    TenantManager tenantManager,
    SecurityContext securityContext,
    DocumentBuilderTaskManager<RoomIndexExportTask<int>, int, RoomIndexExportTaskData<int>> documentBuilderTaskManagerInt,
    DocumentBuilderTaskManager<RoomIndexExportTask<string>, int, RoomIndexExportTaskData<string>> documentBuilderTaskManagerString,
    IServiceProvider serviceProvider)
    : IIntegrationEventHandler<RoomIndexExportIntegrationEvent>
{

    public async Task Handle(RoomIndexExportIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();

        using (logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            try
            {
                if (@event.Terminate)
                {
                    await documentBuilderTaskManagerInt.TerminateTask(@event.TenantId, @event.CreateBy);
                    await documentBuilderTaskManagerString.TerminateTask(@event.TenantId, @event.CreateBy);
                    return;
                }

                if (!string.IsNullOrEmpty(@event.BaseUri))
                {
                    commonLinkUtility.ServerUri = @event.BaseUri;
                }

                await tenantManager.SetCurrentTenantAsync(@event.TenantId);

                await securityContext.AuthenticateMeWithoutCookieAsync(@event.TenantId, @event.CreateBy);

                if (@event.IsThirdpartyRoom)
                {
                    var task = serviceProvider.GetService<RoomIndexExportTask<string>>();
                    task.Init(@event.BaseUri, @event.TenantId, @event.CreateBy, new RoomIndexExportTaskData<string>(@event.RoomId, @event.Headers));
                    await documentBuilderTaskManagerString.StartTask(task);
                }
                else
                {
                    var task = serviceProvider.GetService<RoomIndexExportTask<int>>();
                    task.Init(@event.BaseUri, @event.TenantId, @event.CreateBy, new RoomIndexExportTaskData<int>(int.Parse(@event.RoomId), @event.Headers));
                    await documentBuilderTaskManagerInt.StartTask(task);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorWithException(ex);
            }
        }
    }
}
