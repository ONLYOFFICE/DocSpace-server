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

    public const string WebhookHttpClient = "webhookHttpClient";
    public const string WebhookHttpClientSslIgnore = "webhookHttpClientSslIgnore";

    private readonly TimeSpan _requestTimeout = settings.RequestTimeout ?? WebhookDelivery.DefaultRequestTimeout;

    /// <summary>
    /// Time a claimed row needs after its request goes out besides the request itself: the bookkeeping of the attempt.
    /// </summary>
    private static readonly TimeSpan _attemptBookkeeping = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long one attempt may hold its row: the lease is renewed for this long right before the request goes out,
    /// and the batch lease of <see cref="WorkerService"/> is a multiple of it.
    /// </summary>
    public TimeSpan AttemptLease => _requestTimeout + _attemptBookkeeping;

    private const int MaxRequestPayloadBytes = ushort.MaxValue;
    private const int MaxResponsePayloadBytes = 8 * 1024;

    /// <summary>
    /// How long a row waits after an attempt that failed before its request went out.
    /// </summary>
    private static readonly TimeSpan _unsentAttemptDelay = TimeSpan.FromMinutes(1);

    public async Task Send(int tenantId, int webhookLogId, Guid leaseId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
        var journalUpdated = false;

        // set once the request may have gone out: a failure before that is not an attempt and uses up no retry
        var requestSent = false;

        // set once the target has accepted the request: from then on a failure must not schedule the event again
        var attempts = 0;
        var deliveredStatus = 0;
        DateTime? deliveredOn = null;

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
                // the portal stopped being active after the claim. Its rows leave the schedule when it is being removed;
                // otherwise this one is handed back, so a manual retry does not see it in flight for the whole lease,
                // and waits like the other rows of an inactive portal (DbWorker.PostponeInactiveTenantsJournalAsync)
                if (tenant.Status == TenantStatus.RemovePending)
                {
                    await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                }
                else
                {
                    await dbWorker.ReleaseJournalAttemptAsync(tenantId, webhookLogId, leaseId, DateTime.UtcNow + DbWorker.InactiveTenantDelay);
                }

                return;
            }

            var entry = await dbWorker.ReadJournal(tenantId, webhookLogId);
            if (entry == null)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                return;
            }

            // a disabled subscription receives nothing: not the retries, and not the first attempt of an event published
            // before it was disabled either. A manual retry is refused for it up front (DbWorker.RetryJournalAsync)
            if (!entry.Config.Enabled)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                return;
            }

            // the publisher checked the creator when the event was published; a retry, automatic or manual, sends the
            // stored body again, possibly hours later, to an address the creator controls. A creator who was removed,
            // blocked or downgraded since then gets nothing more, and the journal says why
            if (entry.Config.CreatedBy is { } creatorId &&
                !await scope.ServiceProvider.GetRequiredService<WebhookCreatorEntitlement>().IsEntitledAsync(creatorId, entry.Trigger))
            {
                logger.InfoCreatorNotEntitled(tenantId, webhookLogId, entry.ConfigId, entry.Trigger);
                await dbWorker.StopJournalAttemptsAsync(tenantId, webhookLogId, leaseId, DbWorker.CreatorNotEntitledNote);
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

            WebhookPayload<object, object> webhookPayload;

            try
            {
                webhookPayload = JsonSerializer.Deserialize<WebhookPayload<object, object>>(entry.RequestPayload, _jsonSerializerOptions);
            }
            catch (JsonException e)
            {
                // the body will not become readable on a later attempt
                logger.ErrorFailedToSend(tenantId, webhookLogId, e);
                await dbWorker.StopJournalAttemptsAsync(tenantId, webhookLogId, leaseId, DbWorker.UnreadableBodyNote);
                return;
            }

            if (webhookPayload?.Event == null || webhookPayload?.Webhook == null)
            {
                webhookPayload = new WebhookPayload<object, object>(WebhookTrigger.All, entry.Config, entry.RequestPayload, null, entry.Uid);
            }

            // the original row's id, also on a manual retry, so the receiver recognises the event it may already have
            webhookPayload.Event.Id = entry.EventId;

            // the subscription as it is now: a row restored from a backup holds the id it had on the source portal
            webhookPayload.Webhook.Id = entry.ConfigId;

            if (entry.Attempts > 0)
            {
                // the subscription's own record of its last failure and success, which every attempt keeps current
                webhookPayload.Webhook.LastFailureOn = ToBodyTimestamp(entry.Config.LastFailureOn);
                webhookPayload.Webhook.LastFailureContent = entry.Config.LastFailureContent;
                webhookPayload.Webhook.LastSuccessOn = ToBodyTimestamp(entry.Config.LastSuccessOn);
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

            attempts = entry.Attempts + 1;
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

            // the batch lease may have run out while the row waited for its turn, and another instance may have claimed
            // the row since: the request goes out only while the row is still this attempt's, for long enough to finish
            if (!await dbWorker.RenewJournalLeaseAsync(tenantId, webhookLogId, leaseId, DateTime.UtcNow + AttemptLease))
            {
                logger.DebugLeaseTakenOver(webhookLogId);
                return;
            }

            requestSent = true;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, validationResult.ParsedUri);
                request.Options.Set(UrlValidator.PinnedIpKey, validationResult.ResolvedAddresses[0]);

                request.Headers.Add("Accept", "*/*");

                request.Headers.Add(WebhookDelivery.EventIdHeader, entry.EventId.ToString(CultureInfo.InvariantCulture));
                request.Headers.Add(WebhookDelivery.EventTimestampHeader, $"{webhookPayload.Event.CreateOn:s}Z");

                request.Headers.Add(WebhookDelivery.SignatureHeader, $"sha256={GetSecretHash(entry.Config.SecretKey, requestPayload)}");

                request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

                requestHeaders = JsonSerializer.Serialize(request.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_requestTimeout);

                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

                response.EnsureSuccessStatusCode();

                // the status decides the outcome: a 2xx is a delivery, and from here on nothing may count it as a failure
                status = (int)response.StatusCode;
                delivery = DateTime.UtcNow;
                succeeded = true;
                deliveredStatus = status;
                deliveredOn = delivery;

                responseHeaders = JsonSerializer.Serialize(response.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);
                responsePayload = await TryReadResponsePayloadAsync(response.Content, timeoutCts.Token, cancellationToken);

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
            var nextAttemptOn = succeeded ? null : GetNextAttemptOn(attempts);

            if (succeeded)
            {
                await dbWorker.RecordWebhookConfigSuccessAsync(tenantId, entry.ConfigId, delivery.Value);
            }
            else
            {
                var failureOn = DateTime.UtcNow;

                await dbWorker.RecordWebhookConfigFailureAsync(tenantId, entry.ConfigId, failureOn, responsePayload);

                // only once the retries of this event are used up: a subscription that hears rarely from the portal
                // would otherwise be disabled by its first failure after a quiet spell, before any retry could succeed
                if (mayAutoDisable && !nextAttemptOn.HasValue)
                {
                    autoDisabled = await dbWorker.DisableStaleWebhookConfigAsync(tenantId, entry.ConfigId, failureOn - TimeSpan.FromDays(settings.TrustedDaysCount ?? 3));
                }
            }

            if (nextAttemptOn.HasValue)
            {
                logger.DebugRetryScheduled(entry.Id, attempts, nextAttemptOn.Value);
            }
            else if (!succeeded)
            {
                logger.WarningDeliveryGivenUp(entry.Id, attempts);
            }

            // the column holds bytes, not characters: a body of Cyrillic text under the limit in characters may not fit
            if (Encoding.UTF8.GetByteCount(requestPayload) > MaxRequestPayloadBytes)
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
            // the service is stopping; an accepted request is recorded, or it would go out again once the lease expires
            if (deliveredOn.HasValue && !journalUpdated)
            {
                await RegisterDeliveryAsync(dbWorker, tenantId, webhookLogId, leaseId, deliveredStatus, deliveredOn.Value, attempts);
            }

            throw;
        }
        catch (Exception e)
        {
            logger.ErrorFailedToSend(tenantId, webhookLogId, e);

            if (journalUpdated)
            {
                return;
            }

            if (deliveredOn.HasValue)
            {
                // the target has the event already: record the delivery instead of scheduling it once more
                await RegisterDeliveryAsync(dbWorker, tenantId, webhookLogId, leaseId, deliveredStatus, deliveredOn.Value, attempts);
            }
            else if (!requestSent)
            {
                // nothing reached the target (the database or a check failed first): the row is handed back for a
                // later try that does not count, so a transient fault does not use up the retries of the event
                await ReleaseUnsentAttemptAsync(dbWorker, tenantId, webhookLogId, leaseId);
            }
            else
            {
                await RegisterFailedAttemptAsync(dbWorker, tenantId, webhookLogId, leaseId);
            }
        }
    }

    private async Task RegisterDeliveryAsync(DbWorker dbWorker, int tenantId, int webhookLogId, Guid leaseId, int status, DateTime delivery, int attempts)
    {
        try
        {
            await dbWorker.RecordJournalDeliveryAsync(tenantId, webhookLogId, leaseId, status, delivery, attempts);
        }
        catch (Exception e)
        {
            logger.ErrorFailedToRegisterAttempt(tenantId, webhookLogId, e);
        }
    }

    private async Task ReleaseUnsentAttemptAsync(DbWorker dbWorker, int tenantId, int webhookLogId, Guid leaseId)
    {
        try
        {
            await dbWorker.ReleaseJournalAttemptAsync(tenantId, webhookLogId, leaseId, DateTime.UtcNow + _unsentAttemptDelay);
        }
        catch (Exception e)
        {
            logger.ErrorFailedToRegisterAttempt(tenantId, webhookLogId, e);
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

    /// <summary>
    /// Reads the start of the body of an accepted request for the log. The delivery has already succeeded, so a body
    /// that is slow, cut off or broken only leaves a note in the log; only the service shutting down stops the attempt.
    /// </summary>
    private async Task<string> TryReadResponsePayloadAsync(HttpContent content, CancellationToken timeoutToken, CancellationToken cancellationToken)
    {
        try
        {
            return await ReadResponsePayloadAsync(content, timeoutToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return $"The response body did not arrive within {_requestTimeout.TotalSeconds:0} seconds";
        }
        catch (Exception e) when (e is HttpRequestException or IOException)
        {
            logger.WarningWithException(e);

            return $"The response body could not be read: {e.Message}";
        }
    }

    private static async Task<string> ReadResponsePayloadAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[MaxResponsePayloadBytes];
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    // stored as UtcNow, but a datetime column reads back as Unspecified, which would go out without the Z; cut to whole
    // seconds like every other timestamp of the body
    private static DateTime? ToBodyTimestamp(DateTime? value)
    {
        return value is { } stored ? new DateTime(stored.Ticks - stored.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc) : null;
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

        var jitter = 1 - WebhookDelivery.RetryJitter + Random.Shared.NextDouble() * 2 * WebhookDelivery.RetryJitter;

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

        // the deadline of an attempt is webhooks:requestTimeout, applied per call; the client's own 100 seconds would
        // silently cut a longer one
        services.AddHttpClient(WebhookSender.WebhookHttpClient)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .SetHandlerLifetime(lifeTime)
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectCallback = UrlValidator.PinnedConnectCallback
            });

        services.AddHttpClient(WebhookSender.WebhookHttpClientSslIgnore)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
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
