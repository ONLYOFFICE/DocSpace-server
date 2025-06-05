﻿// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.TelegramService;

[Singleton]
public class TelegramListenerService(
    ICacheNotify<RegisterUserProto> cacheRegisterUser,
    ICacheNotify<CreateClientProto> cacheCreateClient,
    ILogger<TelegramHandler> logger,
    TelegramHandler telegramHandler,
    ICacheNotify<DisableClientProto> cacheDisableClient,
    TenantManager tenantManager,
    ConsumerFactory consumerFactory) : BackgroundService
{
    private readonly ILogger<TelegramHandler> _logger = logger;
    private readonly TelegramLoginProvider _telegramLoginProvider = consumerFactory.Get<TelegramLoginProvider>();
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        await CreateClientsAsync();

        cacheRegisterUser.Subscribe(RegisterUser, CacheNotifyAction.Insert);
        cacheCreateClient.Subscribe(CreateOrUpdateClient, CacheNotifyAction.Insert);
        cacheDisableClient.Subscribe(DisableClient, CacheNotifyAction.Insert);

        stoppingToken.Register(() =>
        {
            _logger.DebugTelegramStopping();

            cacheRegisterUser.Unsubscribe(CacheNotifyAction.Insert);
            cacheCreateClient.Unsubscribe(CacheNotifyAction.Insert);
            cacheDisableClient.Unsubscribe(CacheNotifyAction.Insert);
        });
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
        var tenants = await tenantManager.GetTenantsAsync();

        foreach (var tenant in tenants)
        {
            tenantManager.SetCurrentTenant(tenant);

            if (_telegramLoginProvider.IsEnabled())
            {
                await telegramHandler.CreateOrUpdateClientForTenant(tenant.Id, _telegramLoginProvider.TelegramBotToken, _telegramLoginProvider.TelegramAuthTokenLifespan, _telegramLoginProvider.TelegramProxy, true, _stoppingToken, true);
            }
        }
    }
}
