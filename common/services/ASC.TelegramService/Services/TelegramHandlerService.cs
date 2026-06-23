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

namespace ASC.TelegramService.Services;

[Singleton]
public class TelegramHandlerService(
    IDistributedCache distributedCache,
   CommandExecutionService command,
   ILogger<TelegramHandlerService> logger,
   IServiceScopeFactory scopeFactory)
{
    private readonly Dictionary<int, TenantTgClient> _clients = [];

    public async Task SendMessage(NotifyMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Reciever))
        {
            return;
        }

        if (!_clients.TryGetValue(msg.TenantId, out var value))
        {
            return;
        }

        var scope = scopeFactory.CreateScope();
        var telegramDao = scope.ServiceProvider.GetService<TelegramDao>();

        var client = value.Client;

        try
        {
            var tgUser = await telegramDao.GetUserAsync(Guid.Parse(msg.Reciever), msg.TenantId);

            if (tgUser == null)
            {
                logger.DebugCouldntFind(msg.Reciever);
                return;
            }

            var chat = await client.GetChat(tgUser.TelegramUserId);

            _ = await client.SendMessage(chat, msg.Content, ParseMode.MarkdownV2);
        }
        catch (Exception e)
        {
            logger.DebugCouldntSend(msg.Reciever, e);
        }
    }

    public void DisableClient(int tenantId)
    {
        if (!_clients.TryGetValue(tenantId, out var client))
        {
            return;
        }

        if (client.CancellationTokenSource != null)
        {
            client.CancellationTokenSource.Cancel();
            client.CancellationTokenSource.Dispose();
            client.CancellationTokenSource = null;
        }

        _clients.Remove(tenantId);
    }

    public async Task CreateOrUpdateClientForTenant(int tenantId, string token, int tokenLifespan, string proxy, bool startTelegramService, CancellationToken stoppingToken, bool force = false)
    {
        var scope = scopeFactory.CreateScope();
        var telegramHelper = scope.ServiceProvider.GetService<TelegramHelper>();
        var newClient = telegramHelper.InitClient(token, proxy);

        if (_clients.TryGetValue(tenantId, out var client))
        {
            client.TokenLifeSpan = tokenLifespan;

            if (token != client.Token || proxy != client.Proxy)
            {
                if (startTelegramService)
                {
                    if (!await telegramHelper.TestingClient(newClient))
                    {
                        return;
                    }
                }

                if (client.CancellationTokenSource != null)
                {
                    await client.CancellationTokenSource.CancelAsync();
                    client.CancellationTokenSource.Dispose();
                    client.CancellationTokenSource = null;
                }

                client.Client = newClient;
                client.Token = token;
                client.Proxy = proxy;
                BindClient(newClient, tenantId, stoppingToken);
            }
        }
        else
        {
            if (!force && startTelegramService)
            {
                if (!await telegramHelper.TestingClient(newClient))
                {
                    return;
                }
            }

            _clients.Add(tenantId, new TenantTgClient
            {
                Token = token,
                Client = newClient,
                Proxy = proxy,
                TenantId = tenantId,
                TokenLifeSpan = tokenLifespan
            });
            BindClient(newClient, tenantId, stoppingToken);
        }
    }

    public void RegisterUser(string userId, int tenantId, string token)
    {
        if (!_clients.TryGetValue(tenantId, out var value))
        {
            return;
        }

        var userKey = UserKey(userId, tenantId);
        var dateExpires = DateTimeOffset.Now.AddMinutes(value.TokenLifeSpan);

        distributedCache.SetString(token, userKey, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = dateExpires
        });
    }

    private void BindClient(TelegramBotClient client, int tenantId, CancellationToken cancellationToken)
    {
        var cts = new CancellationTokenSource();

        _clients[tenantId].CancellationTokenSource = cts;

#pragma warning disable CA2000
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
#pragma warning restore CA2000
        
        client.StartReceiving(
            updateHandler: (botClient, update, ct) => HandleUpdate(botClient, update, tenantId, ct),
            errorHandler: (botClient, exception, ct) => HandleErrorAsync(exception, tenantId, linkedCts),
            cancellationToken: linkedCts.Token);
    }

    private Task HandleUpdate(ITelegramBotClient botClient, Update update, int tenantId, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message ||
            update.Message?.Type != MessageType.Text ||
            string.IsNullOrEmpty(update.Message.Text) || update.Message.Text[0] != '/')
        {
            return Task.CompletedTask;
        }

        command.HandleCommand(update.Message, botClient, tenantId, cancellationToken);
        return Task.CompletedTask;
    }

    private readonly int[] _stopErrorCodes = [
        401, // Unathorized
        409 // Keys Conflict
    ];

    private readonly int[] _silentErrorCodes = [
        502
    ];

    private Task HandleErrorAsync(Exception exception, int tenantId, CancellationTokenSource cts)
    {
        string message;
        if (exception is ApiRequestException apiException)
        {
            message = $"Telegram API Error:\n[{apiException.ErrorCode}]\n(TenantId: {tenantId})\n{apiException.Message}";
            if (_silentErrorCodes.Contains(apiException.ErrorCode))
            {
                logger.Information(message);
            }
            else
            {
                logger.Error(message);
            }

            if (_stopErrorCodes.Contains(apiException.ErrorCode))
            {
                cts.Cancel();
            }
        }
        else
        {
            message = exception.ToString();
            logger.Error(message);
        }

        return Task.CompletedTask;
    }

    private static string UserKey(string userId, int tenantId)
    {
        return $"{userId}:{tenantId}";
    }
}