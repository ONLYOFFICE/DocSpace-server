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

using Microsoft.Extensions.Caching.Distributed;

namespace ASC.Core.Common.Notify;

[Singleton]
public class TelegramServiceClient(IEventBus eventBus,
        ICacheNotify<RegisterUserProto> cacheRegisterUser,
        ICacheNotify<CreateClientProto> cacheCreateClient,
        ICacheNotify<DisableClientProto> cacheDisableClient,
        IDistributedCache cache)
    : ITelegramService
{
    public async Task SendMessage(NotifyMessage m)
    {
        await eventBus.PublishAsync(new NotifySendTelegramMessageRequestedIntegrationEvent(Guid.Empty, m.TenantId)
        {
            NotifyMessage = m
        });
    }

    public async Task RegisterUserAsync(string userId, int tenantId, int tokenLifespan, string token)
    {
        await cache.SetStringAsync(GetCacheTokenKey(tenantId, userId), token, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(tokenLifespan)
        });

        await cacheRegisterUser.PublishAsync(new RegisterUserProto
        {
            UserId = userId,
            TenantId = tenantId,
            Token = token
        }, CacheNotifyAction.Insert);
    }

    public async Task CreateOrUpdateClientAsync(int tenantId, string token, int tokenLifespan, string proxy)
    {
        await cacheCreateClient.PublishAsync(new CreateClientProto
        {
            TenantId = tenantId,
            Token = token,
            TokenLifespan = tokenLifespan,
            Proxy = proxy
        }, CacheNotifyAction.Insert);
    }

    public async Task DisableClientAsync(int tenantId)
    {
        await cacheDisableClient.PublishAsync(new DisableClientProto { TenantId = tenantId }, CacheNotifyAction.Insert);
    }

    public async Task<string> RegistrationToken(string userId, int tenantId)
    {
        return await cache.GetStringAsync(GetCacheTokenKey(tenantId, userId));
    }

    private string GetCacheTokenKey(int tenantId, string userId)
    {
        return $"tg-token:{userId}:{tenantId}";
    }
}