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

    private const int MaxRequestPayloadLength = ushort.MaxValue;
    private const int MaxResponsePayloadBytes = 8 * 1024;

    public async Task Send(int tenantId, int webhookLogId, Guid leaseId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
        var journalUpdated = false;

        try
        {
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();

            var tenant = await tenantManager.SetCurrentTenantAsync(tenantId);
            if (tenant == null)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                return;
            }

            if (tenant.Status != TenantStatus.Active)
            {
                // the claim skips inactive portals, so the entry waits here until the portal is active again
                return;
            }

            var entry = await dbWorker.ReadJournal(tenantId, webhookLogId);
            if (entry == null)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                return;
            }

            if (entry.Attempts > 0 && !entry.Config.Enabled)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                return;
            }

            var validationResult = await urlValidator.ValidateAsync(entry.Config.Uri, new UrlValidationOptions
            {
                RequireHttps = entry.Config.SSL
            });
            if (!validationResult.IsValid)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
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
                // stored as UtcNow, but a datetime column reads back as Unspecified, which would go out without the Z
                webhookPayload.Webhook.LastSuccessOn = entry.Config.LastSuccessOn is { } lastSuccessOn
                    ? DateTime.SpecifyKind(lastSuccessOn, DateTimeKind.Utc)
                    : null;
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
            var mayAutoDisable = false;
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

                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

                response.EnsureSuccessStatusCode();

                status = (int)response.StatusCode;
                responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
                responsePayload = await ReadResponsePayloadAsync(response.Content, timeoutCts.Token);

                delivery = DateTime.UtcNow;
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
                    await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                    await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);
                    messageService.SendHeadersMessage(MessageAction.WebhookDeleted, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (HTTP status 410)");
                    return;
                }

                responsePayload = e.Message;
                mayAutoDisable = true;

                logger.WarningWithException(e);
            }
            catch (OperationCanceledException)
            {
                responsePayload = $"The target did not answer within {_requestTimeout.TotalSeconds:0} seconds";
                mayAutoDisable = true;

                logger.WarningRequestTimeout(entry.Id, _requestTimeout);
            }
            catch (Exception e)
            {
                responsePayload = e.Message;

                status = (int)HttpStatusCode.InternalServerError;
                logger.ErrorWithException(e);
            }

            var autoDisabled = false;

            if (succeeded)
            {
                await dbWorker.RecordWebhookConfigSuccessAsync(tenantId, entry.ConfigId, delivery.Value);
            }
            else
            {
                var failureOn = DateTime.UtcNow;

                await dbWorker.RecordWebhookConfigFailureAsync(tenantId, entry.ConfigId, failureOn, responsePayload);

                if (mayAutoDisable)
                {
                    autoDisabled = await dbWorker.DisableStaleWebhookConfigAsync(tenantId, entry.ConfigId, failureOn - TimeSpan.FromDays(settings.TrustedDaysCount ?? 3));
                }
            }

            var nextAttemptOn = succeeded || autoDisabled || !entry.Config.Enabled ? null : GetNextAttemptOn(attempts);

            if (nextAttemptOn.HasValue)
            {
                logger.DebugRetryScheduled(entry.Id, attempts, nextAttemptOn.Value);
            }
            else if (!succeeded)
            {
                logger.WarningDeliveryGivenUp(entry.Id, attempts);
            }

            if (requestPayload.Length > MaxRequestPayloadLength)
            {
                requestPayload = entry.RequestPayload;
            }

            if (!await dbWorker.UpdateWebhookJournal(entry.Id, entry.TenantId, leaseId, status, delivery, requestPayload, requestHeaders, ToStorableText(responsePayload), responseHeaders, attempts, nextAttemptOn))
            {
                logger.DebugLeaseLost(entry.Id);
            }

            journalUpdated = true;

            if (autoDisabled)
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

            if (!journalUpdated)
            {
                await RegisterFailedAttemptAsync(dbWorker, tenantId, webhookLogId, leaseId);
            }
        }
    }

    private async Task RegisterFailedAttemptAsync(DbWorker dbWorker, int tenantId, int webhookLogId, Guid leaseId)
    {
        try
        {
            var attempts = await dbWorker.GetJournalAttemptsAsync(tenantId, webhookLogId);
            if (attempts == null)
            {
                return;
            }

            var nextAttempts = attempts.Value + 1;
            var nextAttemptOn = GetNextAttemptOn(nextAttempts);

            if (nextAttemptOn.HasValue)
            {
                logger.DebugRetryScheduled(webhookLogId, nextAttempts, nextAttemptOn.Value);
            }
            else
            {
                logger.WarningDeliveryGivenUp(webhookLogId, nextAttempts);
            }

            await dbWorker.ScheduleJournalAttemptAsync(tenantId, webhookLogId, leaseId, nextAttempts, nextAttemptOn);
        }
        catch (Exception e)
        {
            logger.ErrorFailedToRegisterAttempt(tenantId, webhookLogId, e);
        }
    }

    private static async Task<string> ReadResponsePayloadAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[MaxResponsePayloadBytes];
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static string ToStorableText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '\0')
            {
                continue;
            }

            if (char.IsSurrogate(c))
            {
                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    i++;
                }

                builder.Append('\uFFFD');
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
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
        else if (await dbWorker.DisableWebhookConfigAsync(entry.TenantId, entry.ConfigId, entry.Config.Uri))
        {
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
