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

using ASC.Core.Common.Hosting;

namespace ASC.TelegramService.Services;

[Singleton]
public class TelegramListenerService(
    ICacheNotify<RegisterUserProto> cacheRegisterUser,
    ICacheNotify<CreateClientProto> cacheCreateClient,
    ILogger<TelegramHandlerService> logger,
    TelegramHandlerService telegramHandler,
    ICacheNotify<DisableClientProto> cacheDisableClient,
    TenantManager singletonTenantManager,
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus) : ActivePassiveBackgroundService<TelegramListenerService>(logger, scopeFactory)
{
    private CancellationToken _stoppingToken;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.FromMilliseconds(-1); // Inf

    protected override Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        cacheRegisterUser.Subscribe(RegisterUser, CacheNotifyAction.Insert);
        cacheCreateClient.Subscribe(CreateOrUpdateClient, CacheNotifyAction.Insert);
        cacheDisableClient.Subscribe(DisableClient, CacheNotifyAction.Insert);
        _ = eventBus.SubscribeAsync<NotifySendTelegramMessageRequestedIntegrationEvent, TelegramSendMessageRequestedIntegrationEventHandler>();

        stoppingToken.Register(() =>
        {
            logger.DebugTelegramStopping();

            cacheRegisterUser.Unsubscribe(CacheNotifyAction.Insert);
            cacheCreateClient.Unsubscribe(CacheNotifyAction.Insert);
            cacheDisableClient.Unsubscribe(CacheNotifyAction.Insert);
            eventBus.Unsubscribe<NotifySendTelegramMessageRequestedIntegrationEvent, TelegramSendMessageRequestedIntegrationEventHandler>();
        });

        _ = Task.Run(async () => await CreateClientsAsync(), stoppingToken);

        return Task.CompletedTask;
    }

    private void DisableClient(DisableClientProto n)
    {
        telegramHandler.DisableClient(n.TenantId);
    }

    private void RegisterUser(RegisterUserProto registerUserProto)
    {
        telegramHandler.RegisterUser(registerUserProto.UserId, registerUserProto.TenantId, registerUserProto.Token);
    }

    private async void CreateOrUpdateClient(CreateClientProto createClientProto)
    {
        await telegramHandler.CreateOrUpdateClientForTenant(createClientProto.TenantId, createClientProto.Token, createClientProto.TokenLifespan, createClientProto.Proxy, false, _stoppingToken);
    }

    private async Task CreateClientsAsync()
    {
        var tenants = await singletonTenantManager.GetTenantsAsync();

        foreach (var tenant in tenants)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            tenantManager.SetCurrentTenant(tenant);
            var consumerFactory = scope.ServiceProvider.GetService<ConsumerFactory>();
            var telegramLoginProvider = consumerFactory.Get<TelegramLoginProvider>();
            if (telegramLoginProvider.IsEnabled())
            {
                await telegramHandler.CreateOrUpdateClientForTenant(tenant.Id, telegramLoginProvider.TelegramBotToken, telegramLoginProvider.TelegramAuthTokenLifespan, telegramLoginProvider.TelegramProxy, true, _stoppingToken, true);
            }
        }
    }
}