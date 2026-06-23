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
public class ThumbnailRequestedIntegrationEventHandler : IIntegrationEventHandler<ThumbnailRequestedIntegrationEvent>
{
    private readonly ILogger _logger;
    private readonly ChannelWriter<FileData<int>> _channelWriter;
    private readonly ITariffService _tariffService;
    private readonly TenantManager _tenantManager;

    private ThumbnailRequestedIntegrationEventHandler()
    {

    }

    public ThumbnailRequestedIntegrationEventHandler(
        ILogger<ThumbnailRequestedIntegrationEventHandler> logger,
        ITariffService tariffService,
        TenantManager tenantManager,
        ChannelWriter<FileData<int>> channelWriter)
    {
        _logger = logger;
        _channelWriter = channelWriter;
        _tariffService = tariffService;
        _tenantManager = tenantManager;
    }

    public async Task Handle(ThumbnailRequestedIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();
        using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            var tenant = await _tenantManager.GetTenantAsync(@event.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return;
            }

            _logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            _tenantManager.SetCurrentTenant(tenant);

            var tariff = await _tariffService.GetTariffAsync(@event.TenantId);

            var data = @event.FileIds.Select(fileId => new FileData<int>(@event.TenantId, @event.CreateBy,
                Convert.ToInt32(fileId), @event.BaseUrl, tariff.State));

            if (await _channelWriter.WaitToWriteAsync())
            {
                foreach (var item in data)
                {
                    await _channelWriter.WriteAsync(item);
                }
            }
        }
    }
}