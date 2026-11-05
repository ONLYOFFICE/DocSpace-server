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

namespace ASC.Webhooks.Core.EF.Context;

public class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : BaseDbContext(options)
{
    public DbSet<DbWebhook> Webhooks { get; set; } // TODO: Deprecated
    public DbSet<DbWebhooksConfig> WebhooksConfigs { get; set; }
    public DbSet<DbWebhooksLog> WebhooksLogs { get; set; }
    public DbSet<DbTenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelBuilderWrapper
            .From(modelBuilder, Database)
            .AddDbWebhooks()
            .AddWebhooksConfig()
            .AddWebhooksLog()
            .AddDbTenant();
    }

    [PreCompileQuery]
    public Task<DbWebhooksConfig> WebhooksConfigByUriAsync(int tenantId, string uri, string name)
    {
        return Queries.WebhooksConfigByUriAsync(this, tenantId, uri, name);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<WebhooksConfigWithStatus> WebhooksConfigWithStatusAsync(int tenantId, Guid? userId)
    {
        return Queries.WebhooksConfigWithStatusAsync(this, tenantId, userId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbWebhooksConfig> WebhooksConfigsAsync(int tenantId, bool? enabled)
    {
        return Queries.WebhooksConfigsAsync(this, tenantId, enabled);
    }

    [PreCompileQuery]
    public Task<DbWebhooksConfig> WebhooksConfigAsync(int tenantId, int id)
    {
        return Queries.WebhooksConfigAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<DbWebhooks> WebhooksLogAsync(int tenantId, int id)
    {
        return Queries.WebhooksLogAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<int?> WebhooksLogAttemptsAsync(int tenantId, int id)
    {
        return Queries.WebhooksLogAttemptsAsync(this, tenantId, id);
    }

    // A running lease marks a row of the event that is being sent right now; an expired one belongs to an instance
    // that died mid-send.
    [PreCompileQuery]
    public Task<bool> WebhooksEventInFlightAsync(int tenantId, int eventId, DateTime now)
    {
        return Queries.WebhooksEventInFlightAsync(this, tenantId, eventId, now);
    }

    // The candidates are read without a lock: several instances may read the same ones, and ClaimWebhooksLogsAsync
    // decides which of them each instance gets. Driven by the next_attempt_on index: the portal is checked per
    // candidate (EXISTS by the primary key of tenants_tenants) rather than joined, so that the plan walks the few due
    // rows in schedule order and stops at `count`, instead of starting from the active portals, which every poll of
    // every instance would otherwise read in full even with nothing to send.
    [PreCompileQuery]
    public IAsyncEnumerable<int> DueWebhooksLogIdsAsync(DateTime now, int count)
    {
        return Queries.DueWebhooksLogIdsAsync(this, now, count);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Leases the candidates that are still due, in one statement: NextAttemptOn moves forward to leaseUntil, so no
    // instance claims them again before the lease expires, and leaseId marks them as being delivered. The due check
    // runs again on the current row inside the UPDATE (MySQL and PostgreSQL both re-read a row a concurrent UPDATE has
    // changed), so a row another instance has just claimed is no longer due and is left to it.
    public Task<int> ClaimWebhooksLogsAsync(List<int> ids, DateTime now, DateTime leaseUntil, Guid leaseId)
    {
        return WebhooksLogs
            .Where(r => ids.Contains(r.Id) && r.NextAttemptOn != null && r.NextAttemptOn <= now)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, leaseUntil)
                .SetProperty(p => p.LeaseId, leaseId));
    }

    // The rows of ids that ClaimWebhooksLogsAsync leased with leaseId, which are this instance's to deliver.
    [PreCompileQuery]
    public IAsyncEnumerable<ClaimedWebhooksLog> ClaimedWebhooksLogsAsync(IEnumerable<int> ids, Guid leaseId)
    {
        return Queries.ClaimedWebhooksLogsAsync(this, ids, leaseId);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Must run inside a transaction. Locks the given rows of one event (the original first, then its manual retries)
    // until the transaction ends, so a sender cannot claim one of them, and an attempt in flight cannot schedule its
    // next retry, between the in-flight check of a manual retry (WebhooksEventInFlightAsync) and the requeue. It
    // writes a column back unchanged: an UPDATE takes the same exclusive row locks as SELECT ... FOR UPDATE on MySQL
    // and PostgreSQL, and LINQ has no FOR UPDATE. The row count is not used: MySQL reports changed rows rather than
    // matched ones when the connection sets UseAffectedRows.
    // By primary key only. A statement over the (tenant_id, retry_of) index takes next-key and gap locks under MySQL's
    // REPEATABLE READ; two manual retries of neighbouring events then hold compatible gap locks on the same gap and
    // deadlock on each other's INSERT of the new retry row. A primary-key lookup of existing rows locks those rows only.
    public Task<int> LockWebhooksLogsAsync(int tenantId, IEnumerable<int> ids)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && ids.Contains(r.Id))
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.Attempts, p => p.Attempts));
    }

    // The ids of the manual retries of one event. A plain read, no lock: DbWorker.RetryJournalAsync reads it on a
    // connection of its own while it holds the lock of the original row, which every manual retry of the event takes
    // before it adds a row, so no retry row can appear or go between this read and the locks taken on the result.
    [PreCompileQuery]
    public IAsyncEnumerable<int> WebhooksEventRetryIdsAsync(int tenantId, int eventId)
    {
        return Queries.WebhooksEventRetryIdsAsync(this, tenantId, eventId);
    }

    // The original row and the retries are looked up separately (UNION ALL), so each half uses its own index rather
    // than a scan of the tenant's range. The same goes for WebhooksEventInFlightAsync.
    [PreCompileQuery]
    public Task<bool> WebhooksEventExistsAsync(int tenantId, int eventId)
    {
        return Queries.WebhooksEventExistsAsync(this, tenantId, eventId);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Ends the automatic retries still due for the given rows of one event, so the series a manual retry starts is
    // the only one left. By primary key, for the reason given at LockWebhooksLogsAsync.
    public Task<int> StopWebhooksLogsAttemptsAsync(int tenantId, IEnumerable<int> ids)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && ids.Contains(r.Id) && r.NextAttemptOn != null)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // True for an event whose original row was written before the attempt counter existed: that sender wrote the
    // request headers on every attempt but never counted one, and a row it never got to carries previousVersionNote
    // as its response, written by the migration for the rows that existed before the update and by
    // MarkPreviousVersionWebhooksLogAsync for the rows a publisher of that version writes during a rolling update.
    // This version writes headers only together with a counted attempt, and a response without one only as a note of
    // its own (StopWebhooksLogWithNoteAsync). The body of such a row is in the old payload format, which carried whole
    // REST objects (the password of a shared link among them), so it is not sent again.
    [PreCompileQuery]
    public Task<bool> WebhooksEventPredatesAttemptsAsync(int tenantId, int eventId, string previousVersionNote)
    {
        return Queries.WebhooksEventPredatesAttemptsAsync(this, tenantId, eventId, previousVersionNote);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Hands a claimed row back without counting an attempt: the lease is released, so a manual retry is no longer
    // refused as in flight, and the row is due again at nextAttemptOn.
    public Task<int> ReleaseWebhooksLogAsync(int tenantId, int id, Guid leaseId, DateTime nextAttemptOn)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, nextAttemptOn)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Hands back the rows of ids that a claim leased with leaseId but never returned to its sender, because the claim
    // failed after its UPDATE: they are due again at once instead of waiting out the lease unsent.
    public Task<int> ReleaseClaimedWebhooksLogsAsync(List<int> ids, Guid leaseId, DateTime nextAttemptOn)
    {
        return WebhooksLogs
            .Where(r => ids.Contains(r.Id) && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, nextAttemptOn)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // The claim skips the due rows of portals that are not active, and of portals whose tenant row is gone, but those
    // rows stay at the head of the next_attempt_on index, and every poll of every instance would read them again.
    // This finds a batch of them, oldest first, and says which ones leave the schedule: those of a portal being
    // removed or already removed. A plain read without locks, so it never holds the rows of active portals it scans
    // past; the UPDATEs that follow touch the rows it returned by primary key only. A row created before createdBefore
    // is marked as expired: its retries would have run out by now, so it stops waiting for its portal.
    [PreCompileQuery]
    public IAsyncEnumerable<InactiveTenantWebhooksLog> InactiveTenantsDueWebhooksLogsAsync(DateTime now, DateTime createdBefore, int count)
    {
        return Queries.InactiveTenantsDueWebhooksLogsAsync(this, now, createdBefore, count);
    }

    // The oldest delivery records created before `before` that nothing will send any more, across all portals, for
    // the retention of webhooks:journalRetentionDays. Walks the creation_time index from its start and stops after
    // `count`. The original row of an event is left while a manual retry still points to it (the retries go first,
    // the original with the last of them): RetryJournalAsync serialises the retries of an event on that row, and a
    // backup restores a retry only together with the row its retry_of maps to. A plain read.
    [PreCompileQuery]
    public IAsyncEnumerable<int> ExpiredWebhooksLogIdsAsync(DateTime before, int count)
    {
        return Queries.ExpiredWebhooksLogIdsAsync(this, before, count);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Must run inside a transaction. Locks rows found by ExpiredWebhooksLogIdsAsync, across portals and by primary key
    // only, the way LockWebhooksLogsAsync does: a manual retry of an event takes the lock of its original row before it
    // adds a retry row, so once these locks are held no retry can appear for them until the delete commits.
    public Task<int> LockExpiredWebhooksLogsAsync(IEnumerable<int> ids)
    {
        return WebhooksLogs
            .Where(r => ids.Contains(r.Id))
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.Attempts, p => p.Attempts));
    }

    // Which of the given rows a manual retry points to, read after LockExpiredWebhooksLogsAsync: a retry committed
    // while the candidates were read is seen here. Per row by the (tenant_id, retry_of) index.
    [PreCompileQuery]
    public IAsyncEnumerable<int> RetriedWebhooksLogIdsAsync(IEnumerable<int> ids)
    {
        return Queries.RetriedWebhooksLogIdsAsync(this, ids);
    }

    // Deletes rows found by ExpiredWebhooksLogIdsAsync, by primary key; one put back on the schedule since then
    // (a manual retry of its event does not do that, it adds a row) is kept.
    [PreCompileQuery]
    public Task<int> DeleteExpiredWebhooksLogsAsync(IEnumerable<int> ids)
    {
        return Queries.DeleteExpiredWebhooksLogsAsync(this, ids);
    }

    // The row with this id and the rows that retry it: for the id of an event's first row, the original and its
    // manual retries; for the id of a retry, that retry alone, since a retry is never the original of another row.
    // Looked up separately (UNION ALL) for the same reason as WebhooksEventExistsAsync.
    [PreCompileQuery]
    public IAsyncEnumerable<int> WebhooksEventRowIdsAsync(int tenantId, int eventId)
    {
        return Queries.WebhooksEventRowIdsAsync(this, tenantId, eventId);
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Takes rows found by InactiveTenantsDueWebhooksLogsAsync off the schedule. A row claimed since then is no longer
    // due and is left to its sender.
    public Task<int> UnscheduleWebhooksLogsAsync(IEnumerable<int> ids, DateTime now)
    {
        return WebhooksLogs
            .Where(r => ids.Contains(r.Id) && r.NextAttemptOn != null && r.NextAttemptOn <= now)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Moves rows found by InactiveTenantsDueWebhooksLogsAsync, of portals that may come back (suspended, being
    // restored or migrated), to postponeTo.
    public Task<int> PostponeWebhooksLogsAsync(IEnumerable<int> ids, DateTime now, DateTime postponeTo)
    {
        return WebhooksLogs
            .Where(r => ids.Contains(r.Id) && r.NextAttemptOn != null && r.NextAttemptOn <= now)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, postponeTo)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Takes rows found by InactiveTenantsDueWebhooksLogsAsync off the schedule like UnscheduleWebhooksLogsAsync, and
    // says why in the response: those of a portal that may come back, whose retries would have run out by now.
    // The note of this and of every method below that writes one goes only into a row nothing was sent for yet: once
    // an attempt was made, the response is the target's last answer, which the journal keeps (the restore of an
    // archive follows the same rule, WebStudioModuleSpecifics.WasAttempted).
    public Task<int> UnscheduleWebhooksLogsWithNoteAsync(IEnumerable<int> ids, DateTime now, string note)
    {
        return WebhooksLogs
            .Where(r => ids.Contains(r.Id) && r.NextAttemptOn != null && r.NextAttemptOn <= now)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.ResponsePayload, p => p.Attempts == 0 ? note : p.ResponsePayload)
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Ends the attempts of a claimed row created before createdBefore like StopWebhooksLogWithNoteAsync; a younger
    // row is left as it is. Used for a row whose attempts keep failing before the request goes out, which counts no
    // attempt and would otherwise come back for ever.
    public Task<int> StopExpiredWebhooksLogWithNoteAsync(int tenantId, int id, Guid leaseId, DateTime createdBefore, string note)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId && r.CreationTime < createdBefore)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.ResponsePayload, p => p.Attempts == 0 ? note : p.ResponsePayload)
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // A publisher of the version before the attempt counter writes its row without next_attempt_on, with a body in the
    // old payload format (whole REST objects, the password of a shared link among them), and leaves the sending to its
    // own Web.Studio. The event says which publisher wrote the row (WebhookRequestIntegrationEvent.Scheduled), so the
    // row is recognised by that mark rather than by its state, which a stopped or cancelled row of this version shares.
    // It is never sent: the note goes into the response, which a row of this version gets only together with an
    // attempt, so WebhooksEventPredatesAttemptsAsync refuses a manual retry of it too. Only a row nothing has been
    // written to yet is marked. Remove once no service of that version can publish any more.
    public Task<int> MarkPreviousVersionWebhooksLogAsync(int tenantId, int id, string note)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.NextAttemptOn == null && r.LeaseId == null &&
                        r.Attempts == 0 && r.Status == 0 && r.Delivery == null && r.RequestHeaders == null &&
                        r.ResponsePayload == null)
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.ResponsePayload, note));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Records an attempt the target accepted when the bookkeeping after the request failed: the row must not be
    // scheduled again, or the receiver would get the event twice.
    public Task<int> RecordWebhooksLogDeliveryAsync(int tenantId, int id, Guid leaseId, int status, DateTime delivery, int attempts)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Status, status)
                .SetProperty(p => p.Delivery, delivery)
                .SetProperty(p => p.Attempts, attempts)
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // The writes below take effect only while the caller still holds the lease it claimed the row with: a row that was
    // stopped or claimed again since then belongs to a newer attempt, which a late write must not overwrite.
    public Task<int> CancelWebhooksLogAttemptsAsync(int tenantId, int id, Guid leaseId)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Ends the attempts of a claimed row like CancelWebhooksLogAttemptsAsync and says why in the response, so the
    // journal shows that the event was dropped rather than lost.
    public Task<int> StopWebhooksLogWithNoteAsync(int tenantId, int id, Guid leaseId, string note)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.ResponsePayload, p => p.Attempts == 0 ? note : p.ResponsePayload)
                .SetProperty(p => p.NextAttemptOn, (DateTime?)null)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Extends the lease of a claimed row to leaseUntil right before its request goes out, but only while the row is
    // still leased with leaseId. A lease that ran out while the row waited in its batch is renewed if no other
    // instance has claimed the row since; once one has, the claim changed lease_id and nothing is updated, so the
    // request is not sent a second time. The claim re-checks the due time on the current row, so the two cannot
    // both win.
    public Task<int> RenewWebhooksLogLeaseAsync(int tenantId, int id, Guid leaseId, DateTime leaseUntil)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.NextAttemptOn, leaseUntil));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public Task<int> ScheduleWebhooksLogAttemptAsync(int tenantId, int id, Guid leaseId, int attempts, DateTime? nextAttemptOn)
    {
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Attempts, attempts)
                .SetProperty(p => p.NextAttemptOn, nextAttemptOn)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public Task<int> CompleteWebhooksLogAttemptAsync(
        int tenantId,
        int id,
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
        return WebhooksLogs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.LeaseId == leaseId)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Status, status)
                .SetProperty(p => p.Delivery, delivery)
                .SetProperty(p => p.RequestPayload, requestPayload)
                .SetProperty(p => p.RequestHeaders, requestHeaders)
                .SetProperty(p => p.ResponsePayload, responsePayload)
                .SetProperty(p => p.ResponseHeaders, responseHeaders)
                .SetProperty(p => p.Attempts, attempts)
                .SetProperty(p => p.NextAttemptOn, nextAttemptOn)
                .SetProperty(p => p.LeaseId, (Guid?)null));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // The delivery bookkeeping below touches only the fields the sender owns, so an edit the user saves while an
    // attempt is in flight is never reverted, and parallel attempts for one subscription do not move the dates back.
    public Task<int> RecordWebhooksConfigSuccessAsync(int tenantId, int id, DateTime successOn)
    {
        return WebhooksConfigs
            .Where(r => r.TenantId == tenantId && r.Id == id && (r.LastSuccessOn == null || r.LastSuccessOn < successOn))
            .ExecuteUpdateAsync(q => q.SetProperty(p => p.LastSuccessOn, successOn));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    public Task<int> RecordWebhooksConfigFailureAsync(int tenantId, int id, DateTime failureOn, string failureContent)
    {
        return WebhooksConfigs
            .Where(r => r.TenantId == tenantId && r.Id == id && (r.LastFailureOn == null || r.LastFailureOn < failureOn))
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.LastFailureOn, failureOn)
                .SetProperty(p => p.LastFailureContent, failureContent));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Disables an enabled subscription that has had no success since successBefore (counting from its creation when it
    // never had one). The check runs on the current row, so a success recorded by a parallel attempt keeps it enabled.
    public Task<int> DisableStaleWebhooksConfigAsync(int tenantId, int id, DateTime successBefore, DateTime modifiedOn)
    {
        return WebhooksConfigs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.Enabled && (r.LastSuccessOn ?? r.CreatedOn) < successBefore)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Enabled, false)
                .SetProperty(p => p.ModifiedOn, modifiedOn));
    }

    // Not precompiled on purpose: ExecuteUpdate does not translate inside EF.CompileAsyncQuery.
    // Matches the target address too: a subscription whose address was changed since it was checked stays enabled.
    public Task<int> DisableWebhooksConfigAsync(int tenantId, int id, string uri, DateTime modifiedOn)
    {
        return WebhooksConfigs
            .Where(r => r.TenantId == tenantId && r.Id == id && r.Enabled && r.Uri == uri)
            .ExecuteUpdateAsync(q => q
                .SetProperty(p => p.Enabled, false)
                .SetProperty(p => p.ModifiedOn, modifiedOn));
    }
}

static file class Queries
{
    public static readonly Func<WebhooksDbContext, int, string, string, Task<DbWebhooksConfig>> WebhooksConfigByUriAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, string uri, string name) =>
                ctx.WebhooksConfigs.FirstOrDefault(r => r.TenantId == tenantId && r.Uri == uri && r.Name == name));

    public static readonly Func<WebhooksDbContext, int, Guid?, IAsyncEnumerable<WebhooksConfigWithStatus>> WebhooksConfigWithStatusAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, Guid? userId) =>
                ctx.WebhooksConfigs.Where(it => it.TenantId == tenantId && (userId == null || it.CreatedBy == userId))
                .GroupJoin(ctx.WebhooksLogs, c => c.Id, l => l.ConfigId, (configs, logs) => new { configs, logs })
                .Select(it => new WebhooksConfigWithStatus
                {
                    WebhooksConfig = it.configs,
                    Status = it.logs.OrderByDescending(webhooksLog => webhooksLog.Delivery).FirstOrDefault().Status
                }));

    public static readonly Func<WebhooksDbContext, int, bool?, IAsyncEnumerable<DbWebhooksConfig>> WebhooksConfigsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, bool? enabled) =>
                ctx.WebhooksConfigs.Where(it => it.TenantId == tenantId && (enabled == null || it.Enabled == enabled)));

    public static readonly Func<WebhooksDbContext, int, int, Task<DbWebhooksConfig>> WebhooksConfigAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksConfigs.FirstOrDefault(it => it.TenantId == tenantId && it.Id == id));

    public static readonly Func<WebhooksDbContext, int, int, Task<DbWebhooks>> WebhooksLogAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == id)
                    .Join(ctx.WebhooksConfigs, r => r.ConfigId, r => r.Id, (log, config) => new DbWebhooks { Log = log, Config = config })
                    .FirstOrDefault());

    public static readonly Func<WebhooksDbContext, int, int, Task<int?>> WebhooksLogAttemptsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int id) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == id)
                    .Select(it => (int?)it.Attempts)
                    .FirstOrDefault());

    public static readonly Func<WebhooksDbContext, DateTime, int, IAsyncEnumerable<int>> DueWebhooksLogIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, DateTime now, int count) =>
                ctx.WebhooksLogs
                    .Where(it => it.NextAttemptOn != null && it.NextAttemptOn <= now &&
                                 ctx.Tenants.Any(t => t.Id == it.TenantId && t.Status == TenantStatus.Active))
                    .OrderBy(it => it.NextAttemptOn)
                    .ThenBy(it => it.Id)
                    .Select(it => it.Id)
                    .Take(count));

    public static readonly Func<WebhooksDbContext, DateTime, int, IAsyncEnumerable<int>> ExpiredWebhooksLogIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, DateTime before, int count) =>
                ctx.WebhooksLogs
                    .Where(it => it.CreationTime < before && it.NextAttemptOn == null && it.LeaseId == null &&
                                 !ctx.WebhooksLogs.Any(r => r.TenantId == it.TenantId && r.RetryOf == it.Id))
                    .OrderBy(it => it.CreationTime)
                    .ThenBy(it => it.Id)
                    .Select(it => it.Id)
                    .Take(count));

    public static readonly Func<WebhooksDbContext, IEnumerable<int>, IAsyncEnumerable<int>> RetriedWebhooksLogIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, IEnumerable<int> ids) =>
                ctx.WebhooksLogs
                    .Where(it => ids.Contains(it.Id) &&
                                 ctx.WebhooksLogs.Any(r => r.TenantId == it.TenantId && r.RetryOf == it.Id))
                    .Select(it => it.Id));

    public static readonly Func<WebhooksDbContext, int, int, IAsyncEnumerable<int>> WebhooksEventRowIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int eventId) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == eventId)
                    .Select(it => it.Id)
                    .Concat(ctx.WebhooksLogs
                        .Where(it => it.TenantId == tenantId && it.RetryOf == eventId)
                        .Select(it => it.Id)));

    public static readonly Func<WebhooksDbContext, IEnumerable<int>, Task<int>> DeleteExpiredWebhooksLogsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, IEnumerable<int> ids) =>
                ctx.WebhooksLogs
                    .Where(it => ids.Contains(it.Id) && it.NextAttemptOn == null && it.LeaseId == null)
                    .ExecuteDelete());

    public static readonly Func<WebhooksDbContext, DateTime, DateTime, int, IAsyncEnumerable<InactiveTenantWebhooksLog>> InactiveTenantsDueWebhooksLogsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, DateTime now, DateTime createdBefore, int count) =>
                ctx.WebhooksLogs
                    .Where(it => it.NextAttemptOn != null && it.NextAttemptOn <= now &&
                                 !ctx.Tenants.Any(t => t.Id == it.TenantId && t.Status == TenantStatus.Active))
                    .OrderBy(it => it.NextAttemptOn)
                    .ThenBy(it => it.Id)
                    .Select(it => new InactiveTenantWebhooksLog
                    {
                        Id = it.Id,
                        Unschedule = !ctx.Tenants.Any(t => t.Id == it.TenantId && t.Status != TenantStatus.RemovePending),
                        Expired = it.CreationTime < createdBefore
                    })
                    .Take(count));

    public static readonly Func<WebhooksDbContext, IEnumerable<int>, Guid, IAsyncEnumerable<ClaimedWebhooksLog>> ClaimedWebhooksLogsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, IEnumerable<int> ids, Guid leaseId) =>
                ctx.WebhooksLogs
                    .Where(it => ids.Contains(it.Id) && it.LeaseId == leaseId)
                    .Select(it => new ClaimedWebhooksLog { TenantId = it.TenantId, Id = it.Id }));

    public static readonly Func<WebhooksDbContext, int, int, IAsyncEnumerable<int>> WebhooksEventRetryIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int eventId) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.RetryOf == eventId)
                    .OrderBy(it => it.Id)
                    .Select(it => it.Id));

    public static readonly Func<WebhooksDbContext, int, int, Task<bool>> WebhooksEventExistsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int eventId) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == eventId)
                    .Select(it => it.Id)
                    .Concat(ctx.WebhooksLogs
                        .Where(it => it.TenantId == tenantId && it.RetryOf == eventId)
                        .Select(it => it.Id))
                    .Any());

    public static readonly Func<WebhooksDbContext, int, int, DateTime, Task<bool>> WebhooksEventInFlightAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int eventId, DateTime now) =>
                ctx.WebhooksLogs
                    .Where(it => it.TenantId == tenantId && it.Id == eventId && it.LeaseId != null && it.NextAttemptOn > now)
                    .Select(it => it.Id)
                    .Concat(ctx.WebhooksLogs
                        .Where(it => it.TenantId == tenantId && it.RetryOf == eventId && it.LeaseId != null && it.NextAttemptOn > now)
                        .Select(it => it.Id))
                    .Any());

    public static readonly Func<WebhooksDbContext, int, int, string, Task<bool>> WebhooksEventPredatesAttemptsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WebhooksDbContext ctx, int tenantId, int eventId, string previousVersionNote) =>
                ctx.WebhooksLogs.Any(it =>
                    it.TenantId == tenantId && it.Id == eventId && it.RetryOf == null &&
                    it.Attempts == 0 && (it.RequestHeaders != null || it.ResponsePayload == previousVersionNote)));
}