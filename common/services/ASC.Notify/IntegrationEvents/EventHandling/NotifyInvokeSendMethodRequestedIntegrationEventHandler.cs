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

namespace ASC.Notify.IntegrationEvents.EventHandling;

[Scope]
public class NotifyInvokeSendMethodRequestedIntegrationEventHandler(ILoggerFactory loggerFactory,
        IServiceScopeFactory serviceScopeFactory)
    : IIntegrationEventHandler<NotifyInvokeSendMethodRequestedIntegrationEvent>
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("ASC.NotifyService");

    private async Task InvokeSendMethodAsync(NotifyInvoke notifyInvoke)
    {
        var service = notifyInvoke.Service;
        var method = notifyInvoke.Method;
        var tenant = notifyInvoke.Tenant;
        var parameters = notifyInvoke.Parameters;

        var serviceType = Type.GetType(service, true);

        using var scope = serviceScopeFactory.CreateScope();

        var instance = scope.ServiceProvider.GetService(serviceType);
        if (instance == null)
        {
            throw new Exception("Service instance not found.");
        }

        var methodInfo = serviceType.GetMethod(method);
        if (methodInfo == null)
        {
            throw new Exception("Method not found.");
        }

        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();

        await tenantManager.SetCurrentTenantAsync(tenant);

        methodInfo.Invoke(instance, parameters.ToArray());
    }


    public async Task Handle(NotifyInvokeSendMethodRequestedIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();
        using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            _logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            await InvokeSendMethodAsync(@event.NotifyInvoke);

            await Task.CompletedTask;
        }
    }
}