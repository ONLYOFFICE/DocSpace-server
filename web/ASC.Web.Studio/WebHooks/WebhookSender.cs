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

using ASC.Core.Common.Security;
using ASC.MessagingSystem.Core;
using ASC.MessagingSystem.EF.Model;
using ASC.Webhooks.Core.EF.Model;

namespace ASC.Webhooks;

[Singleton]
public class WebhookSender(
    ILogger<WebhookSender> logger,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory clientFactory,
    Settings settings,
    IUrlValidator urlValidator)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IgnoreReadOnlyProperties = true
    };

    private const string SignatureHeader = "x-docspace-signature-256";
    private const string EventIdHeader = "x-docspace-event-id";
    private const string EventTimestampHeader = "x-docspace-event-timestamp";

    public const string WebhookHttpClient = "webhookHttpClient";
    public const string WebhookHttpClientSslIgnore = "webhookHttpClientSslIgnore";

    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(30);

    public async Task Send(int tenantId, int webhookLogId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();

            await tenantManager.SetCurrentTenantAsync(tenantId);

            var entry = await dbWorker.ReadJournal(tenantId, webhookLogId);
            if (entry == null)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId);
                return;
            }

            if (entry.Attempts > 0 && !entry.Config.Enabled)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId);
                return;
            }

            var validationResult = await urlValidator.ValidateAsync(entry.Config.Uri, new UrlValidationOptions
            {
                RequireHttps = entry.Config.SSL
            });
            if (!validationResult.IsValid)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId);
                await DisableWebhook(validationResult, entry, dbWorker, messageService);
                return;
            }

            var webhookPayload = JsonSerializer.Deserialize<WebhookPayload<object, object>>(entry.RequestPayload, _jsonSerializerOptions);

            if (webhookPayload?.Event == null || webhookPayload?.Webhook == null)
            {
                webhookPayload = new WebhookPayload<object, object>(WebhookTrigger.All, entry.Config, entry.RequestPayload, null, entry.Uid);
            }

            webhookPayload.Event.Id = entry.Id;

            if (entry.Attempts > 0)
            {
                webhookPayload.Webhook.LastFailureOn = webhookPayload.Webhook.RetryOn ?? webhookPayload.Event.CreateOn;
                webhookPayload.Webhook.LastFailureContent = entry.ResponsePayload;
                webhookPayload.Webhook.LastSuccessOn = entry.Config.LastSuccessOn;
                webhookPayload.Webhook.RetryCount = entry.Attempts;
                webhookPayload.Webhook.RetryOn = webhookPayload.GetShortUtcNow();
            }
            else
            {
                webhookPayload.Webhook.LastFailureOn = null;
                webhookPayload.Webhook.LastFailureContent = null;
                webhookPayload.Webhook.LastSuccessOn = null;
                webhookPayload.Webhook.RetryCount = 0;
                webhookPayload.Webhook.RetryOn = null;
            }

            var attempts = entry.Attempts + 1;
            var status = 0;
            var succeeded = false;
            DateTime? delivery = null;
            string responsePayload;
            string responseHeaders = null;
            var requestPayload = JsonSerializer.Serialize(webhookPayload, _jsonSerializerOptions);
            string requestHeaders = null;

            var httpClientName = entry.Config.SSL ? WebhookHttpClient : WebhookHttpClientSslIgnore;
            var httpClient = clientFactory.CreateClient(httpClientName);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, validationResult.ParsedUri);
                request.Options.Set(UrlValidator.PinnedIpKey, validationResult.ResolvedAddresses[0]);

                request.Headers.Add("Accept", "*/*");

                request.Headers.Add(EventIdHeader, entry.Id.ToString(CultureInfo.InvariantCulture));
                request.Headers.Add(EventTimestampHeader, $"{webhookPayload.Event.CreateOn:s}Z");

                request.Headers.Add(SignatureHeader, $"sha256={GetSecretHash(entry.Config.SecretKey, requestPayload)}");

                request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

                requestHeaders = JsonSerializer.Serialize(request.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_requestTimeout);

                using var response = await httpClient.SendAsync(request, timeoutCts.Token);

                response.EnsureSuccessStatusCode();

                status = (int)response.StatusCode;
                responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
                responsePayload = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                entry.Config.LastSuccessOn = delivery = DateTime.UtcNow;
                succeeded = true;

                logger.DebugResponse(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue)
                {
                    status = (int)e.StatusCode.Value;
                }

                if (e.StatusCode == HttpStatusCode.Gone)
                {
                    await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId);
                    await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);
                    messageService.SendHeadersMessage(MessageAction.WebhookDeleted, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (HTTP status 410)");
                    return;
                }

                responsePayload = e.Message;

                RegisterDeliveryFailure(entry.Config, e.Message);

                logger.WarningWithException(e);
            }
            catch (OperationCanceledException)
            {
                responsePayload = $"The target did not answer within {_requestTimeout.TotalSeconds:0} seconds";

                RegisterDeliveryFailure(entry.Config, responsePayload);

                logger.WarningRequestTimeout(entry.Id, _requestTimeout);
            }
            catch (Exception e)
            {
                responsePayload = e.Message;

                entry.Config.LastFailureContent = e.Message;
                entry.Config.LastFailureOn = DateTime.UtcNow;

                status = (int)HttpStatusCode.InternalServerError;
                logger.ErrorWithException(e);
            }

            var configDisabled = !entry.Config.Enabled;

            var nextAttemptOn = succeeded || configDisabled ? null : GetNextAttemptOn(attempts);

            if (nextAttemptOn.HasValue)
            {
                logger.DebugRetryScheduled(entry.Id, attempts, nextAttemptOn.Value);
            }
            else if (!succeeded)
            {
                logger.WarningDeliveryGivenUp(entry.Id, attempts);
            }

            await dbWorker.UpdateWebhookJournal(entry.Id, entry.TenantId, status, delivery, requestPayload, requestHeaders, responsePayload, responseHeaders, attempts, nextAttemptOn);
            await dbWorker.UpdateWebhookConfig(entry.Config, configDisabled);

            if (configDisabled)
            {
                messageService.SendHeadersMessage(MessageAction.WebhookUpdated, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (more than {settings.TrustedDaysCount} days without success)");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ErrorFailedToSend(tenantId, webhookLogId, e);
        }
    }

    private DateTime? GetNextAttemptOn(int attempts)
    {
        var intervals = settings.RetryIntervals;

        if (attempts > intervals.Length)
        {
            return null;
        }

        var jitter = 0.9 + Random.Shared.NextDouble() * 0.2;

        return DateTime.UtcNow + intervals[attempts - 1] * jitter;
    }

    private void RegisterDeliveryFailure(DbWebhooksConfig config, string message)
    {
        config.LastFailureContent = message;
        config.LastFailureOn = DateTime.UtcNow;

        var lastSuccessOn = config.LastSuccessOn ?? config.CreatedOn;

        if (lastSuccessOn.HasValue && config.LastFailureOn - lastSuccessOn.Value > TimeSpan.FromDays(settings.TrustedDaysCount ?? 3))
        {
            config.Enabled = false;
        }
    }

    private static async Task DisableWebhook(UrlValidationResult validationResult, DbWebhooksLog entry, DbWorker dbWorker,
        MessageService messageService)
    {
        if (validationResult.Blacklisted)
        {
            await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);
            messageService.SendHeadersMessage(MessageAction.WebhookDeleted,
                MessageTarget.Create(entry.ConfigId), null,
                $"{entry.Config.Name} (blacklist)");
        }
        else
        {
            entry.Config.Enabled = false;
            await dbWorker.UpdateWebhookConfig(entry.Config, true);
            messageService.SendHeadersMessage(MessageAction.WebhookUpdated,
                MessageTarget.Create(entry.ConfigId), null,
                $"{entry.Config.Name} ({validationResult.ErrorMessage})");
        }
    }

    private static string GetSecretHash(string secretKey, string body)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secretKey);

        using var hasher = new HMACSHA256(secretBytes);

        var data = Encoding.UTF8.GetBytes(body);
        var hash = hasher.ComputeHash(data);

        return Convert.ToHexString(hash);
    }
}


public static class WebhookSenderExtension
{
    public static void AddWebhookSenderHttpClient(this IServiceCollection services)
    {
        var lifeTime = TimeSpan.FromMinutes(5);

        services.AddHttpClient(WebhookSender.WebhookHttpClient)
            .SetHandlerLifetime(lifeTime)
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectCallback = UrlValidator.PinnedConnectCallback
            });

        services.AddHttpClient(WebhookSender.WebhookHttpClientSslIgnore)
            .SetHandlerLifetime(lifeTime)
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                var handler = new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    ConnectCallback = UrlValidator.PinnedConnectCallback
                };
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                return handler;
            });
    }
}
