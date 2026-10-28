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

using ZiggyCreatures.Caching.Fusion;

namespace ASC.Webhooks.Core;

[Scope]
public class DbWorker(
    IDbContextFactory<WebhooksDbContext> dbContextFactory,
    TenantManager tenantManager,
    AuthContext authContext,
    WebhookCache webhookCache)
{
    /// <summary>
    /// How many times one claim reads candidates again after other instances won part of its batch.
    /// </summary>
    private const int MaxClaimRounds = 3;

    /// <summary>
    /// How long the rows of a portal that is not active wait before the claim looks at them again.
    /// </summary>
    public static readonly TimeSpan InactiveTenantDelay = TimeSpan.FromHours(1);

    /// <summary>
    /// How many rows one statement of the background upkeep touches: the rows of inactive portals it moves out of the
    /// way of the claim, or the expired rows it deletes.
    /// </summary>
    private const int PostponeBatchSize = 500;

    /// <summary>
    /// How many batches of <see cref="PostponeBatchSize"/> one run of the upkeep handles at most.
    /// </summary>
    private const int MaxPostponeRounds = 20;

    private static string GetCacheKey(int tenantId)
    {
        return $"webhooks_configs_{tenantId}";
    }

    public async Task<DbWebhooksConfig> AddWebhookConfig(string name, string uri, string secretKey, bool enabled, bool ssl, WebhookTrigger triggers, string targetId)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var tenantId = tenantManager.GetCurrentTenantId();
        var existingConfig = await webhooksDbContext.WebhooksConfigByUriAsync(tenantId, uri, name);

        if (existingConfig != null)
        {
            throw new ArgumentException("Webhook with the same name and payload URL already exists");
        }

        var toAdd = new DbWebhooksConfig
        {
            TenantId = tenantId,
            Uri = uri,
            SecretKey = secretKey,
            Name = name,
            Enabled = enabled,
            SSL = ssl,
            Triggers = triggers,
            TargetId = targetId,
            CreatedBy = authContext.CurrentAccount.ID,
            CreatedOn = DateTime.UtcNow
        };

        toAdd = await webhooksDbContext.AddOrUpdateAsync(r => r.WebhooksConfigs, toAdd);
        await webhooksDbContext.SaveChangesAsync();

        await webhookCache.ClearAsync(GetCacheKey(tenantId));

        return toAdd;
    }

    public async IAsyncEnumerable<WebhooksConfigWithStatus> GetTenantWebhooksWithStatus(Guid? userId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksConfigWithStatusAsync(tenantId, userId);

        await foreach (var webhook in q)
        {
            yield return webhook;
        }
    }

    public async Task<DbWebhooksConfig> GetWebhookConfig(int tenantId, int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var result = await webhooksDbContext.WebhooksConfigAsync(tenantId, id);

        return result;
    }

    public async Task<List<DbWebhooksConfig>> GetActiveWebhookConfigsFromCache()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var key = GetCacheKey(tenantId);

        var result = await webhookCache.GetAsync<List<DbWebhooksConfig>>(key);

        if (result != null)
        {
            return result;
        }

        result = await GetWebhookConfigs(true).ToListAsync();

        await webhookCache.InsertAsync(key, result);

        return result;
    }

    public async IAsyncEnumerable<DbWebhooksConfig> GetWebhookConfigs(bool? enabled)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var q = webhooksDbContext.WebhooksConfigsAsync(tenantId, enabled);

        await foreach (var webhook in q)
        {
            yield return webhook;
        }
    }

    public async Task<DbWebhooksConfig> UpdateWebhookConfig(DbWebhooksConfig dbWebhooksConfig, bool clearCache)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var updateObj = await webhooksDbContext.WebhooksConfigAsync(dbWebhooksConfig.TenantId, dbWebhooksConfig.Id);

        updateObj.Name = dbWebhooksConfig.Name;
        updateObj.Uri = dbWebhooksConfig.Uri;
        updateObj.SecretKey = dbWebhooksConfig.SecretKey;
        updateObj.Enabled = dbWebhooksConfig.Enabled;
        updateObj.SSL = dbWebhooksConfig.SSL;
        updateObj.Triggers = dbWebhooksConfig.Triggers;
        updateObj.TargetId = dbWebhooksConfig.TargetId;

        updateObj.ModifiedBy = dbWebhooksConfig.ModifiedBy ?? authContext.CurrentAccount.ID;
        updateObj.ModifiedOn = DateTime.UtcNow;

        updateObj.LastFailureOn = dbWebhooksConfig.LastFailureOn;
        updateObj.LastFailureContent = dbWebhooksConfig.LastFailureContent;
        updateObj.LastSuccessOn = dbWebhooksConfig.LastSuccessOn;

        webhooksDbContext.WebhooksConfigs.Update(updateObj);

        await webhooksDbContext.SaveChangesAsync();

        if (clearCache)
        {
            await webhookCache.ClearAsync(GetCacheKey(updateObj.TenantId));
        }

        return updateObj;
    }

    public async Task RecordWebhookConfigSuccessAsync(int tenantId, int id, DateTime successOn)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.RecordWebhooksConfigSuccessAsync(tenantId, id, successOn);
    }

    public async Task RecordWebhookConfigFailureAsync(int tenantId, int id, DateTime failureOn, string failureContent)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.RecordWebhooksConfigFailureAsync(tenantId, id, failureOn, failureContent);
    }

    public async Task<bool> DisableStaleWebhookConfigAsync(int tenantId, int id, DateTime successBefore)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var disabled = await webhooksDbContext.DisableStaleWebhooksConfigAsync(tenantId, id, successBefore, DateTime.UtcNow) > 0;

        if (disabled)
        {
            await webhookCache.ClearAsync(GetCacheKey(tenantId));
        }

        return disabled;
    }

    public async Task<bool> DisableWebhookConfigAsync(int tenantId, int id, string uri)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var disabled = await webhooksDbContext.DisableWebhooksConfigAsync(tenantId, id, uri, DateTime.UtcNow) > 0;

        if (disabled)
        {
            await webhookCache.ClearAsync(GetCacheKey(tenantId));
        }

        return disabled;
    }

    public async Task<DbWebhooksConfig> RemoveWebhookConfigAsync(int id)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var removeObj = await webhooksDbContext.WebhooksConfigAsync(tenantId, id);

        if (removeObj != null)
        {
            webhooksDbContext.WebhooksConfigs.Remove(removeObj);
            await webhooksDbContext.SaveChangesAsync();

            await webhookCache.ClearAsync(GetCacheKey(tenantId));
        }

        return removeObj;
    }

    public async IAsyncEnumerable<DbWebhooks> ReadJournal(
        int startIndex,
        int limit,
        DateTime? deliveryFrom,
        DateTime? deliveryTo,
        string hookUri,
        int? configId,
        int? eventId,
        WebhookGroupStatus? webhookGroupStatus,
        Guid? userId,
        WebhookTrigger? trigger)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        var q = await GetQueryForJournal(webhooksDbContext, deliveryFrom, deliveryTo, hookUri, configId, eventId, webhookGroupStatus, userId, trigger);

        if (startIndex != 0)
        {
            q = q.Skip(startIndex);
        }

        if (limit != 0)
        {
            q = q.Take(limit);
        }

        // read in full before the caller sees the first row: the caller awaits a mapping per row, and a reader left
        // open across those awaits would hold the connection for the whole response
        var page = await q.ToListAsync();

        foreach (var r in page)
        {
            yield return r;
        }
    }

    public async Task<int> GetTotalByQuery(DateTime? deliveryFrom,
        DateTime? deliveryTo,
        string hookUri,
        int? configId,
        int? eventId,
        WebhookGroupStatus? webhookGroupStatus,
        Guid? userId,
        WebhookTrigger? trigger)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();
        return await (await GetQueryForJournal(webhooksDbContext, deliveryFrom, deliveryTo, hookUri, configId, eventId, webhookGroupStatus, userId, trigger)).CountAsync();
    }

    public async Task<DbWebhooksLog> ReadJournal(int tenantId, int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var fromDb = await webhooksDbContext.WebhooksLogAsync(tenantId, id);

        fromDb?.Log.Config = fromDb.Config;

        return fromDb?.Log;
    }

    public async Task<DbWebhooksLog> WriteToJournal(DbWebhooksLog webhook)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await webhooksDbContext.WebhooksLogs.AddAsync(webhook);
        await webhooksDbContext.SaveChangesAsync();

        return entity.Entity;
    }

    public async Task<bool> UpdateWebhookJournal(
        int id,
        int tenantId,
        Guid leaseId,
        int status,
        DateTime? delivery,
        string requestPayload,
        string requestHeaders,
        string responsePayload,
        string responseHeaders,
        int attempts,
        DateTime? nextAttemptOn)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var updated = await webhooksDbContext.CompleteWebhooksLogAttemptAsync(tenantId, id, leaseId, status, delivery, requestPayload, requestHeaders, responsePayload, responseHeaders, attempts, nextAttemptOn);

        return updated > 0;
    }

    public async Task<List<(int TenantId, int Id, Guid LeaseId)>> ClaimDueJournalEntriesAsync(int count, TimeSpan lease)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var leaseId = Guid.NewGuid();
        var claimed = new List<(int TenantId, int Id, Guid LeaseId)>();

        // Instances polling at the same moment read the same candidates and each UPDATE takes only the rows still due,
        // so a batch can come back short because another instance won part of it. Reading again fills it from the rows
        // behind; a round that won everything it read, or found nothing, means there is nothing more to take.
        for (var round = 0; round < MaxClaimRounds && claimed.Count < count; round++)
        {
            var now = DateTime.UtcNow;
            var candidates = await webhooksDbContext.DueWebhooksLogIdsAsync(now, count - claimed.Count).ToListAsync();

            if (candidates.Count == 0)
            {
                break;
            }

            var won = await webhooksDbContext.ClaimWebhooksLogsAsync(candidates, now, now + lease, leaseId);

            if (won > 0)
            {
                await foreach (var row in webhooksDbContext.ClaimedWebhooksLogsAsync(candidates, leaseId))
                {
                    claimed.Add((row.TenantId, row.Id, leaseId));
                }
            }

            if (won == candidates.Count)
            {
                break;
            }
        }

        return claimed;
    }

    /// <summary>
    /// Queues a manual retry of the event <paramref name="source"/> belongs to as a new journal row, leaving the rows
    /// of the earlier attempts as they are. Nothing is queued while a row of that event is being sent right now, for
    /// a disabled subscription, or for an event written before the attempt counter, whose body is in the old format.
    /// </summary>
    public async Task<WebhookRetryResult> RetryJournalAsync(DbWebhooksLog source, Guid retriedBy)
    {
        // a disabled subscription receives nothing, so a retry for it would be queued only to be dropped by the sender
        if (source.Config is { Enabled: false })
        {
            return new WebhookRetryResult(WebhookRetryStatus.Disabled);
        }

        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var strategy = webhooksDbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // the strategy runs this again after a transient failure of SaveChanges or Commit (a deadlock, say); the row
            // added by the failed run is still tracked as Added and would be inserted next to the new one
            webhooksDbContext.ChangeTracker.Clear();

            await using var tx = await webhooksDbContext.Database.BeginTransactionAsync();

            var eventId = source.EventId;

            // the original row first: every manual retry of the event queues up on it, so the set of its retry rows
            // cannot change until the commit, and the order matches the one a claim locks rows in (by id)
            await webhooksDbContext.LockWebhooksLogsAsync(source.TenantId, [eventId]);

            // read on a connection of its own: a read in this transaction would fix its MySQL snapshot here, before the
            // retry rows are locked, and the checks below would miss a claim committed in between
            List<int> retryIds;

            await using (var readContext = await dbContextFactory.CreateDbContextAsync())
            {
                retryIds = await readContext.WebhooksEventRetryIdsAsync(source.TenantId, eventId).ToListAsync();
            }

            if (retryIds.Count > 0)
            {
                await webhooksDbContext.LockWebhooksLogsAsync(source.TenantId, retryIds);
            }

            var now = DateTime.UtcNow;

            // the rows go together with their subscription, which may have been removed since the caller read them
            if (!await webhooksDbContext.WebhooksEventExistsAsync(source.TenantId, eventId))
            {
                return new WebhookRetryResult(WebhookRetryStatus.NotFound);
            }

            if (await webhooksDbContext.WebhooksEventPredatesAttemptsAsync(source.TenantId, eventId, PreviousVersionNote))
            {
                return new WebhookRetryResult(WebhookRetryStatus.Outdated);
            }

            // a second series next to one in flight would send the event to the receiver twice at the same time
            if (await webhooksDbContext.WebhooksEventInFlightAsync(source.TenantId, eventId, now))
            {
                return new WebhookRetryResult(WebhookRetryStatus.InProgress);
            }

            await webhooksDbContext.StopWebhooksLogsAttemptsAsync(source.TenantId, [eventId, .. retryIds]);

            var retry = new DbWebhooksLog
            {
                TenantId = source.TenantId,
                ConfigId = source.ConfigId,
                Trigger = source.Trigger,
                Uid = source.Uid,
                CreationTime = now,
                RequestPayload = source.RequestPayload,
                RetryOf = eventId,
                RetriedBy = retriedBy,
                Attempts = 0,
                NextAttemptOn = now
            };

            await webhooksDbContext.WebhooksLogs.AddAsync(retry);
            await webhooksDbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return new WebhookRetryResult(WebhookRetryStatus.Requeued, retry);
        });
    }

    public async Task CancelJournalAttemptsAsync(int tenantId, int id, Guid leaseId)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.CancelWebhooksLogAttemptsAsync(tenantId, id, leaseId);
    }

    /// <summary>
    /// Ends the attempts of a claimed row, with <paramref name="note"/> as its response to say why.
    /// </summary>
    public async Task StopJournalAttemptsAsync(int tenantId, int id, Guid leaseId, string note)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.StopWebhooksLogWithNoteAsync(tenantId, id, leaseId, note);
    }

    /// <summary>
    /// Extends the lease of a claimed row to <paramref name="leaseUntil"/> right before its request goes out. False
    /// when the row is no longer leased with <paramref name="leaseId"/>: another instance has claimed it since, or it
    /// was stopped, and the request must not be sent.
    /// </summary>
    public async Task<bool> RenewJournalLeaseAsync(int tenantId, int id, Guid leaseId, DateTime leaseUntil)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        return await webhooksDbContext.RenewWebhooksLogLeaseAsync(tenantId, id, leaseId, leaseUntil) > 0;
    }

    /// <summary>
    /// Hands a claimed row back without counting an attempt; it is due again at <paramref name="nextAttemptOn"/>.
    /// </summary>
    public async Task ReleaseJournalAttemptAsync(int tenantId, int id, Guid leaseId, DateTime nextAttemptOn)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.ReleaseWebhooksLogAsync(tenantId, id, leaseId, nextAttemptOn);
    }

    /// <summary>
    /// Records an attempt the target accepted, when the regular bookkeeping after it failed, so that it is not retried.
    /// </summary>
    public async Task RecordJournalDeliveryAsync(int tenantId, int id, Guid leaseId, int status, DateTime delivery, int attempts)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.RecordWebhooksLogDeliveryAsync(tenantId, id, leaseId, status, delivery, attempts);
    }

    /// <summary>
    /// Moves the due rows of portals that are not active, or no longer exist, out of the way of the claim: see
    /// <see cref="WebhooksDbContext.InactiveTenantsDueWebhooksLogsAsync"/>. Works in batches of
    /// <see cref="PostponeBatchSize"/>, each one a short statement of its own, and stops after
    /// <see cref="MaxPostponeRounds"/> of them; what is left waits for the next run.
    /// </summary>
    public async Task<int> PostponeInactiveTenantsJournalAsync()
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        var moved = 0;

        for (var round = 0; round < MaxPostponeRounds; round++)
        {
            var now = DateTime.UtcNow;
            var rows = await webhooksDbContext.InactiveTenantsDueWebhooksLogsAsync(now, PostponeBatchSize).ToListAsync();

            if (rows.Count == 0)
            {
                break;
            }

            var unschedule = rows.Where(r => r.Unschedule).Select(r => r.Id).ToList();
            var postpone = rows.Where(r => !r.Unschedule).Select(r => r.Id).ToList();

            if (unschedule.Count > 0)
            {
                moved += await webhooksDbContext.UnscheduleWebhooksLogsAsync(unschedule, now);
            }

            if (postpone.Count > 0)
            {
                moved += await webhooksDbContext.PostponeWebhooksLogsAsync(postpone, now, now + InactiveTenantDelay);
            }

            if (rows.Count < PostponeBatchSize)
            {
                break;
            }
        }

        return moved;
    }

    /// <summary>
    /// Deletes the delivery records of every portal created before <paramref name="before"/> that nothing will send
    /// any more, oldest first, in batches of <see cref="PostponeBatchSize"/> and at most <see cref="MaxPostponeRounds"/>
    /// of them per call; what is left goes on the next one. The original row of an event stays while a manual retry
    /// points to it (see <see cref="WebhooksDbContext.ExpiredWebhooksLogIdsAsync"/>). Returns how many were deleted.
    /// </summary>
    public async Task<int> DeleteExpiredJournalAsync(DateTime before, CancellationToken cancellationToken)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var strategy = webhooksDbContext.Database.CreateExecutionStrategy();
        var deleted = 0;

        for (var round = 0; round < MaxPostponeRounds && !cancellationToken.IsCancellationRequested; round++)
        {
            var ids = await webhooksDbContext.ExpiredWebhooksLogIdsAsync(before, PostponeBatchSize).ToListAsync(cancellationToken);

            if (ids.Count == 0)
            {
                break;
            }

            deleted += await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await webhooksDbContext.Database.BeginTransactionAsync();

                // a manual retry that added a row for one of these events after the read above holds, or has
                // committed, the lock of its original row; once these locks are taken, the read below sees its row
                await webhooksDbContext.LockExpiredWebhooksLogsAsync(ids);

                var retried = await webhooksDbContext.RetriedWebhooksLogIdsAsync(ids).ToHashSetAsync();
                var toDelete = retried.Count == 0 ? ids : ids.Where(id => !retried.Contains(id)).ToList();
                var count = toDelete.Count == 0 ? 0 : await webhooksDbContext.DeleteExpiredWebhooksLogsAsync(toDelete);

                await tx.CommitAsync();

                return count;
            });

            if (ids.Count < PostponeBatchSize)
            {
                break;
            }
        }

        return deleted;
    }

    /// <summary>
    /// What the journal shows as the response of a row a publisher of the previous version wrote.
    /// </summary>
    public const string PreviousVersionNote =
        "Not sent: the event was published by a service of the previous version, in the old payload format";

    /// <summary>
    /// What the journal shows as the response of a row that was not sent because the creator of its subscription may
    /// no longer receive the event.
    /// </summary>
    public const string CreatorNotEntitledNote =
        "Not sent: the creator of the subscription may no longer receive this event";

    /// <summary>
    /// What the journal shows as the response of a row that was not sent because its stored body cannot be read.
    /// </summary>
    public const string UnreadableBodyNote =
        "Not sent: the stored body of the event cannot be read";

    /// <summary>
    /// Marks a row written by a publisher of the previous version as never to be sent: see
    /// <see cref="WebhooksDbContext.MarkPreviousVersionWebhooksLogAsync"/>. True when the row was marked.
    /// </summary>
    public async Task<bool> MarkPreviousVersionJournalEntryAsync(int tenantId, int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        return await webhooksDbContext.MarkPreviousVersionWebhooksLogAsync(tenantId, id, PreviousVersionNote) > 0;
    }

    public async Task<int?> GetJournalAttemptsAsync(int tenantId, int id)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        return await webhooksDbContext.WebhooksLogAttemptsAsync(tenantId, id);
    }

    public async Task ScheduleJournalAttemptAsync(int tenantId, int id, Guid leaseId, int attempts, DateTime? nextAttemptOn)
    {
        await using var webhooksDbContext = await dbContextFactory.CreateDbContextAsync();

        await webhooksDbContext.ScheduleWebhooksLogAttemptAsync(tenantId, id, leaseId, attempts, nextAttemptOn);
    }

    // Not precompiled on purpose: the filters of the journal are composed per call, and the page and the count share
    // them. The event filter goes through the compiled WebhooksEventRowIdsAsync.
    private async Task<IQueryable<DbWebhooks>> GetQueryForJournal(
        WebhooksDbContext webhooksDbContext,
        DateTime? deliveryFrom,
        DateTime? deliveryTo,
        string hookUri,
        int? configId,
        int? eventId,
        WebhookGroupStatus? webhookGroupStatus,
        Guid? userId,
        WebhookTrigger? trigger)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        // the row eventId and its retries are found first by primary key and the retry_of index;
        // an OR of the two in the query below would read the tenant's whole range, for the page and for the count
        List<int> eventRowIds = null;

        if (eventId != null)
        {
            eventRowIds = await webhooksDbContext.WebhooksEventRowIdsAsync(tenantId, eventId.Value).ToListAsync();
        }

        var q = webhooksDbContext.WebhooksLogs
            .OrderByDescending(t => t.Id)
            .Where(r => r.TenantId == tenantId)
            .Join(webhooksDbContext.WebhooksConfigs, r => r.ConfigId, r => r.Id, (log, config) => new DbWebhooks { Log = log, Config = config });

        if (userId.HasValue)
        {
            q = q.Where(r => r.Config.CreatedBy == userId.Value);
        }

        if (trigger.HasValue && trigger.Value != WebhookTrigger.All)
        {
            q = q.Where(r => r.Log.Trigger == trigger.Value);
        }

        if (deliveryFrom.HasValue)
        {
            var from = deliveryFrom.Value;
            q = q.Where(r => r.Log.Delivery >= from);
        }

        if (deliveryTo.HasValue)
        {
            var to = deliveryTo.Value;
            q = q.Where(r => r.Log.Delivery <= to);
        }

        if (!string.IsNullOrEmpty(hookUri))
        {
            q = q.Where(r => r.Config.Uri == hookUri);
        }

        if (configId != null)
        {
            q = q.Where(r => r.Log.ConfigId == configId);
        }

        if (eventRowIds != null)
        {
            q = q.Where(r => eventRowIds.Contains(r.Log.Id));
        }

        if (webhookGroupStatus != null && webhookGroupStatus != WebhookGroupStatus.None)
        {
            if ((webhookGroupStatus & WebhookGroupStatus.NotSent) != WebhookGroupStatus.NotSent)
            {
                q = q.Where(r => r.Log.Status != 0);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status2xx) != WebhookGroupStatus.Status2xx)
            {
                q = q.Where(r => r.Log.Status < 200 || r.Log.Status >= 300);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status3xx) != WebhookGroupStatus.Status3xx)
            {
                q = q.Where(r => r.Log.Status < 300 || r.Log.Status >= 400);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status4xx) != WebhookGroupStatus.Status4xx)
            {
                q = q.Where(r => r.Log.Status < 400 || r.Log.Status >= 500);
            }
            if ((webhookGroupStatus & WebhookGroupStatus.Status5xx) != WebhookGroupStatus.Status5xx)
            {
                q = q.Where(r => r.Log.Status < 500);
            }
        }

        return q;
    }
}
public class DbWebhooks
{
    public DbWebhooksLog Log { get; init; }
    public DbWebhooksConfig Config { get; init; }
}

public class ClaimedWebhooksLog
{
    public int TenantId { get; init; }
    public int Id { get; init; }
}

public class InactiveTenantWebhooksLog
{
    public int Id { get; init; }

    /// <summary>
    /// True when the portal is being removed or is gone: the row leaves the schedule instead of waiting.
    /// </summary>
    public bool Unschedule { get; init; }
}

/// <summary>
/// The status of the webhook delivery group.
/// </summary>
[Flags]
public enum WebhookGroupStatus
{
    [Description("None")]
    None = 0,

    [Description("Not sent")]
    NotSent = 1,

    [Description("Status2xx")]
    Status2xx = 2,

    [Description("Status3xx")]
    Status3xx = 4,

    [Description("Status4xx")]
    Status4xx = 8,

    [Description("Status5xx")]
    Status5xx = 16
}

[Singleton]
public class WebhookCache(IFusionCacheProvider cacheProvider)
{
    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public async Task<T> GetAsync<T>(string key) where T : class
    {
        return await _cache.GetOrDefaultAsync<T>(key);
    }

    public async Task InsertAsync<T>(string key, T value) where T : class
    {
        await _cache.SetAsync(key, value, _cacheExpiration);
    }

    public async Task ClearAsync(string key)
    {
        await _cache.RemoveAndNotifyAsync(key);
    }
}