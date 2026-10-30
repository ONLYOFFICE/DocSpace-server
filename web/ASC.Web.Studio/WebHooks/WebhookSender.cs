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
    /// and the claim lease of <see cref="WorkerService"/> is at least as long.
    /// </summary>
    public TimeSpan AttemptLease => _requestTimeout + _attemptBookkeeping;

    private const int MaxRequestPayloadBytes = ushort.MaxValue;
    private const int MaxResponsePayloadBytes = 8 * 1024;

    /// <summary>
    /// How long a row waits after an attempt that failed before its request went out.
    /// </summary>
    private static readonly TimeSpan _unsentAttemptDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long after its creation a row may still be sent: every retry interval at its longest, an attempt's lease for
    /// each attempt, and an hour of grace for a passing fault at the last retry. A row that is older and still not
    /// sent, because its attempts keep failing before the request goes out or its portal is not active, stops.
    /// </summary>
    public TimeSpan RetryWindow =>
        TimeSpan.FromTicks((long)(settings.RetryIntervals.Sum(i => i.Ticks) * (1 + WebhookDelivery.RetryJitter))) +
        AttemptLease * (settings.RetryIntervals.Length + 1) +
        TimeSpan.FromHours(1);

    public async Task Send(int tenantId, int webhookLogId, Guid leaseId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
        var socketManager = scope.ServiceProvider.GetRequiredService<WebhookSocketManager>();
        var journalUpdated = false;
        DbWebhooksLog entry = null;

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
                await dbWorker.StopJournalAttemptsAsync(tenantId, webhookLogId, leaseId, DbWorker.PortalRemovedNote);
                return;
            }

            if (tenant.Status != TenantStatus.Active)
            {
                // the portal stopped being active after the claim. Its rows leave the schedule when it is being removed;
                // otherwise this one is handed back, so a manual retry does not see it in flight for the whole lease,
                // and waits like the other rows of an inactive portal (DbWorker.PostponeInactiveTenantsJournalAsync)
                if (tenant.Status == TenantStatus.RemovePending)
                {
                    await dbWorker.StopJournalAttemptsAsync(tenantId, webhookLogId, leaseId, DbWorker.PortalRemovedNote);
                }
                else
                {
                    await dbWorker.ReleaseJournalAttemptAsync(tenantId, webhookLogId, leaseId, DateTime.UtcNow + DbWorker.InactiveTenantDelay);
                }

                return;
            }

            entry = await dbWorker.ReadJournal(tenantId, webhookLogId);
            if (entry == null)
            {
                await dbWorker.CancelJournalAttemptsAsync(tenantId, webhookLogId, leaseId);
                return;
            }

            // a disabled subscription receives nothing: not the retries, and not the first attempt of an event published
            // before it was disabled either. A manual retry is refused for it up front (DbWorker.RetryJournalAsync)
            if (!entry.Config.Enabled)
            {
                await dbWorker.StopJournalAttemptsAsync(tenantId, webhookLogId, leaseId, DbWorker.SubscriptionDisabledNote);
                await socketManager.UpdateWebhookLogAsync(entry.Config, webhookLogId);
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
                await socketManager.UpdateWebhookLogAsync(entry.Config, webhookLogId);
                return;
            }

            var validationResult = await urlValidator.ValidateAsync(entry.Config.Uri, new UrlValidationOptions
            {
                RequireHttps = entry.Config.SSL
            });
            // a host that could not be resolved is not a rejection: DNS may answer on the next try, so that is an
            // ordinary failed attempt below, with its retry. Only a definitive rejection (scheme, https, blacklist) stops
            if (!validationResult.IsValid && !validationResult.Transient)
            {
                await DisableWebhook(validationResult, entry, leaseId, dbWorker, messageService, socketManager);
                return;
            }

            WebhookPayload<object, object> webhookPayload;

            try
            {
                webhookPayload = JsonSerializer.Deserialize<WebhookPayload<object, object>>(entry.RequestPayload, _jsonSerializerOptions);
            }
            catch (Exception e) when (e is JsonException or ArgumentNullException)
            {
                // the body will not become readable on a later attempt; a missing one (the column is nullable on
                // PostgreSQL) fails the same way, with ArgumentNullException
                logger.ErrorFailedToSend(tenantId, webhookLogId, e);
                await dbWorker.StopJournalAttemptsAsync(tenantId, webhookLogId, leaseId, DbWorker.UnreadableBodyNote);
                await socketManager.UpdateWebhookLogAsync(entry.Config, webhookLogId);
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

            // the claim lease may have run out during the checks above, and another instance may have claimed the row
            // since: the request goes out only while the row is still this attempt's, for long enough to finish. A row
            // claimed as the service began to stop is handed back at once instead (the catch below), not renewed
            cancellationToken.ThrowIfCancellationRequested();

            if (!await dbWorker.RenewJournalLeaseAsync(tenantId, webhookLogId, leaseId, DateTime.UtcNow + AttemptLease))
            {
                logger.DebugLeaseTakenOver(webhookLogId);
                return;
            }

            requestSent = true;

            try
            {
                if (!validationResult.IsValid)
                {
                    // the host did not resolve: the attempt fails the way the handler's own lookup would have failed it
                    throw new HttpRequestException(HttpRequestError.NameResolutionError, validationResult.ErrorMessage);
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, validationResult.ParsedUri);
                request.Options.Set(UrlValidator.PinnedIpKey, validationResult.ResolvedAddresses[0]);

                request.Headers.Add("Accept", "*/*");

                request.Headers.Add(WebhookDelivery.EventIdHeader, entry.EventId.ToString(CultureInfo.InvariantCulture));
                request.Headers.Add(WebhookDelivery.EventTimestampHeader, $"{webhookPayload.Event.CreateOn:s}Z");

                request.Headers.Add(WebhookDelivery.SignatureHeader, $"sha256={GetSecretHash(entry.Config.SecretKey, requestPayload)}");

                request.Content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

                requestHeaders = JsonSerializer.Serialize(request.Headers.ToDictionary(r => r.Key, v => v.Value), _jsonSerializerOptions);

                // tied to the stopping token: a slow target must not hold the shutdown for up to the request timeout; a
                // request cut that way is handed back below and may reach the target twice, which the event id dedupes
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
            // a fault after a 2xx (reading the response for the record) skips these two: the delivery stands, see below
            catch (HttpRequestException e) when (!succeeded)
            {
                if (e.StatusCode.HasValue)
                {
                    status = (int)e.StatusCode.Value;
                }

                responsePayload = e.Message;
                mayAutoDisable = true;

                logger.WarningWithException(e);

                // the target asks for the subscription to go. Its removal takes this row with it (the records cascade
                // with their subscription), so it comes first and nothing is left to record; a removal that fails
                // leaves the row as it was, still leased, and the 410 is recorded below as an ordinary failed attempt,
                // whose retry asks again
                if (e.StatusCode == HttpStatusCode.Gone && await TryRemoveGoneWebhookAsync(entry, dbWorker, messageService, socketManager))
                {
                    return;
                }
            }
            catch (OperationCanceledException) when (!succeeded)
            {
                responsePayload = $"The target did not answer within {_requestTimeout.TotalSeconds:0} seconds";
                mayAutoDisable = true;

                logger.WarningRequestTimeout(entry.Id, _requestTimeout);
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);

                if (succeeded)
                {
                    // the target accepted the request: the delivery and its 2xx stay, the fault only leaves a note
                    responsePayload = $"Delivered, but the response could not be recorded in full: {e.Message}";
                }
                else
                {
                    responsePayload = e.Message;
                    status = (int)HttpStatusCode.InternalServerError;
                }
            }

            var autoDisabled = false;
            var nextAttemptOn = succeeded ? null : GetNextAttemptOn(attempts);

            // the column holds bytes, not characters: a body of Cyrillic text under the limit in characters may not fit
            if (Encoding.UTF8.GetByteCount(requestPayload) > MaxRequestPayloadBytes)
            {
                requestPayload = entry.RequestPayload;
            }

            // the record first: the subscription's last success or failure, and its auto-disable, follow only an attempt
            // the record keeps. One whose row was requeued or claimed again meanwhile belongs to the attempt that has it
            var recorded = await dbWorker.UpdateWebhookJournal(entry.Id, entry.TenantId, leaseId, status, delivery, requestPayload, requestHeaders, responsePayload, responseHeaders, attempts, nextAttemptOn);

            journalUpdated = true;

            if (!recorded)
            {
                logger.DebugLeaseLost(entry.Id);
                return;
            }

            // every step below follows a recorded attempt and is best effort on its own: a failure of one (a deadlock on
            // the subscription row) is logged and must neither skip the others nor send the row back into the catch
            // below, which would treat the recorded attempt as unrecorded
            if (succeeded)
            {
                await RunAfterAttemptAsync(entry.Id, "record the success", () => dbWorker.RecordWebhookConfigSuccessAsync(tenantId, entry.ConfigId, delivery.Value));
            }
            else
            {
                var failureOn = DateTime.UtcNow;

                await RunAfterAttemptAsync(entry.Id, "record the failure", () => dbWorker.RecordWebhookConfigFailureAsync(tenantId, entry.ConfigId, failureOn, responsePayload));

                // only once the retries of this event are used up: a subscription that hears rarely from the portal
                // would otherwise be disabled by its first failure after a quiet spell, before any retry could succeed
                if (mayAutoDisable && !nextAttemptOn.HasValue)
                {
                    await RunAfterAttemptAsync(entry.Id, "disable the stale subscription", async () =>
                    {
                        autoDisabled = await dbWorker.DisableStaleWebhookConfigAsync(tenantId, entry.ConfigId, failureOn - TimeSpan.FromDays(settings.TrustedDaysCount ?? WebhookDelivery.DefaultTrustedDays));

                        // the audit right after the change it records, so a later step cannot lose it
                        if (autoDisabled)
                        {
                            messageService.SendHeadersMessage(MessageAction.WebhookUpdated, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (more than {settings.TrustedDaysCount} days without success)");
                        }
                    });
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

            // the record, and the subscription, whose last success or failure every attempt moves
            await socketManager.UpdateWebhookLogAsync(entry.Config, entry.Id);
            await socketManager.UpdateWebhookAsync(entry.Config);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // the service is stopping; an accepted request is recorded, or it would go out again once the lease expires,
            // and any other row is handed back at once, rather than held for the rest of its lease: one whose request
            // never went out, and one whose request was cut by the stop, which counts no attempt since the target was
            // never given the chance to answer
            if (journalUpdated)
            {
                throw;
            }

            if (deliveredOn.HasValue)
            {
                await RegisterDeliveryAsync(dbWorker, tenantId, webhookLogId, leaseId, deliveredStatus, deliveredOn.Value, attempts);
            }
            else
            {
                await ReleaseAttemptAsync(dbWorker, tenantId, webhookLogId, leaseId, DateTime.UtcNow);
            }

            await AnnounceJournalChangeAsync(socketManager, entry, tenantId, webhookLogId);

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
                // later try that does not count, so a transient fault does not use up the retries of the event. A
                // fault that never passes would bring it back every minute for ever: past the retry window it stops
                await ReleaseUnsentAttemptAsync(dbWorker, tenantId, webhookLogId, leaseId);
            }
            else
            {
                await RegisterFailedAttemptAsync(dbWorker, tenantId, webhookLogId, leaseId);
            }

            await AnnounceJournalChangeAsync(socketManager, entry, tenantId, webhookLogId);
        }
    }

    private async Task RunAfterAttemptAsync(int webhookLogId, string step, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorAfterAttempt(webhookLogId, step, e);
        }
    }

    /// <summary>
    /// Tells the open journals that the row changed on a path that failed or was cut short. Best effort: the row is
    /// already written, and a failure here must not replace the exception that led to it.
    /// </summary>
    private async Task AnnounceJournalChangeAsync(WebhookSocketManager socketManager, DbWebhooksLog entry, int tenantId, int webhookLogId)
    {
        if (entry == null)
        {
            return;
        }

        try
        {
            await socketManager.UpdateWebhookLogAsync(entry.Config, webhookLogId);
        }
        catch (Exception e)
        {
            logger.ErrorFailedToAnnounce(tenantId, webhookLogId, e);
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
            var now = DateTime.UtcNow;

            if (!await dbWorker.ReleaseUnsentJournalAttemptAsync(tenantId, webhookLogId, leaseId, now + _unsentAttemptDelay, now - RetryWindow))
            {
                logger.WarningUnsentTooLong(tenantId, webhookLogId, RetryWindow);
            }
        }
        catch (Exception e)
        {
            logger.ErrorFailedToRegisterAttempt(tenantId, webhookLogId, e);
        }
    }

    private async Task ReleaseAttemptAsync(DbWorker dbWorker, int tenantId, int webhookLogId, Guid leaseId, DateTime nextAttemptOn)
    {
        try
        {
            await dbWorker.ReleaseJournalAttemptAsync(tenantId, webhookLogId, leaseId, nextAttemptOn);
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

    /// <summary>
    /// Removes the subscription whose target answered 410 Gone, with its records. False, with the fault logged, when
    /// the removal failed: the row is then still leased, for the caller to record the attempt.
    /// </summary>
    private async Task<bool> TryRemoveGoneWebhookAsync(DbWebhooksLog entry, DbWorker dbWorker, MessageService messageService,
        WebhookSocketManager socketManager)
    {
        try
        {
            await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorAfterAttempt(entry.Id, "remove the subscription its target reports gone", e);

            return false;
        }

        messageService.SendHeadersMessage(MessageAction.WebhookDeleted, MessageTarget.Create(entry.ConfigId), null, $"{entry.Config.Name} (HTTP status 410)");
        await socketManager.DeleteWebhookAsync(entry.Config);

        return true;
    }

    /// <summary>
    /// Takes a subscription whose target was rejected out of delivery, then ends the attempts of the row. The
    /// subscription goes first: the row keeps its lease until then, so a failure of that step leaves it to be handed
    /// back and tried again, rather than stopped with a note that says the subscription was disabled when it was not.
    /// </summary>
    private static async Task DisableWebhook(UrlValidationResult validationResult, DbWebhooksLog entry, Guid leaseId, DbWorker dbWorker,
        MessageService messageService, WebhookSocketManager socketManager)
    {
        if (validationResult.Blacklisted)
        {
            // the records go with the subscription, this row among them, so nothing is left to stop
            await dbWorker.RemoveWebhookConfigAsync(entry.ConfigId);
            messageService.SendHeadersMessage(MessageAction.WebhookDeleted,
                MessageTarget.Create(entry.ConfigId), null,
                $"{entry.Config.Name} (blacklist)");
            await socketManager.DeleteWebhookAsync(entry.Config);

            return;
        }

        var disabled = await dbWorker.DisableWebhookConfigAsync(entry.TenantId, entry.ConfigId, entry.Config.Uri);

        // the audit right after the change it records, so a later step cannot lose it
        if (disabled)
        {
            messageService.SendHeadersMessage(MessageAction.WebhookUpdated,
                MessageTarget.Create(entry.ConfigId), null,
                $"{entry.Config.Name} ({validationResult.ErrorMessage})");
        }

        // the record stays, with the note of why it was not sent
        await dbWorker.StopJournalAttemptsAsync(entry.TenantId, entry.Id, leaseId, DbWorker.TargetRejectedNote + validationResult.ErrorMessage);
        await socketManager.UpdateWebhookLogAsync(entry.Config, entry.Id);

        if (disabled)
        {
            await socketManager.UpdateWebhookAsync(entry.Config);
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
