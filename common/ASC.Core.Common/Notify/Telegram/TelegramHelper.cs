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

namespace ASC.Core.Common.Notify;

[Scope]
public class TelegramHelper(
    ConsumerFactory consumerFactory,
    TelegramDao telegramDao,
    TelegramServiceClient telegramServiceClient,
    ILogger<TelegramHelper> logger)
{
    public async Task<string> RegisterUserAsync(Guid userId, int tenantId)
    {
        var token = GenerateToken(userId);

        var tgProvider = (ITelegramLoginProvider)consumerFactory.GetByKey("telegram");
        await telegramServiceClient.RegisterUserAsync(userId.ToString(), tenantId, tgProvider.TelegramAuthTokenLifespan, token);

        return GetLink(token);
    }

    public async Task SendMessageAsync(NotifyMessage msg)
    {
        await telegramServiceClient.SendMessage(msg);
    }

    public async Task<bool> CreateClientAsync(int tenantId, string token, int tokenLifespan, string proxy)
    {
        var client = InitClient(token, proxy);
        if (await TestingClient(client))
        {
            await telegramServiceClient.CreateOrUpdateClientAsync(tenantId, token, tokenLifespan, proxy);

            return true;
        }

        return false;
    }

    public async Task<(RegStatus, string)> GetTelegramUserStatus(Guid userId, int tenantId)
    {
        var tgUser = await telegramDao.GetUserAsync(userId, tenantId);
        return tgUser == null
            ? (await IsAwaitingRegistration(userId, tenantId) ? RegStatus.linking : RegStatus.unlinked, null)
            : (RegStatus.linked, tgUser.TelegramUsername);
    }

    public async Task<string> CurrentRegistrationLink(Guid userId, int tenantId)
    {
        var token = await GetCurrentToken(userId, tenantId);
        return string.IsNullOrEmpty(token) ? string.Empty : GetLink(token);
    }

    public async Task DisableClientAsync(int tenantId)
    {
        await telegramServiceClient.DisableClientAsync(tenantId);
    }

    public async Task DisconnectAsync(Guid userId, int tenantId)
    {
        await telegramDao.DeleteAsync(userId, tenantId);
    }

    private async Task<bool> IsAwaitingRegistration(Guid userId, int tenantId)
    {
        return await GetCurrentToken(userId, tenantId) != null;
    }

    private async Task<string> GetCurrentToken(Guid userId, int tenantId)
    {
        return await telegramServiceClient.RegistrationToken(userId.ToString(), tenantId);
    }

    private string GenerateToken(Guid userId)
    {
        var id = userId.ToByteArray();
        var d = BitConverter.GetBytes(DateTime.Now.Ticks);

        var buf = id.Concat(d).ToArray();

        return Convert.ToBase64String(SHA256.HashData(buf)).Base64ToUrlSafe();
    }

    private string GetLink(string token)
    {
        var tgProvider = (ITelegramLoginProvider)consumerFactory.GetByKey("telegram");
        var botname = tgProvider?.TelegramBotName;
        return string.IsNullOrEmpty(botname)
            ? null
            : $"t.me/{botname.TrimStart('@')}?start={token}";
    }

    public async Task<bool> TestingClient(TelegramBotClient telegramBotClient)
    {
        try
        {
            if (!await telegramBotClient.TestApi())
            {
                return false;
            }
        }
        catch (Exception e)
        {
            logger.DebugCouldNotTest(e);

            return false;
        }

        return true;
    }

    public TelegramBotClient InitClient(string token, string proxy)
    {
        if (string.IsNullOrEmpty(proxy))
        {
            return new TelegramBotClient(token);
        }

#pragma warning disable CA2000 // HttpClient and handler are owned by TelegramBotClient
        var handler = new SocketsHttpHandler
        {
            UseProxy = true,
            Proxy = new WebProxy(proxy)
        };
        var httpClient = new HttpClient(handler);

        return new TelegramBotClient(token, httpClient);
#pragma warning restore CA2000
    }
}

/// <summary>
/// The registration Telegram status.
/// </summary>
public enum RegStatus
{
    unlinked,
    linked,
    linking
}