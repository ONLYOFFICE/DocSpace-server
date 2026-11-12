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

namespace ASC.ClearEvents.Services;

/// <summary>
/// One pass of the audit trail cleanup over every tenant: removes the audit events older than the retention of
/// the tenant's tariff, except those that are part of the history of a file, folder or room, and the tenant's newest
/// event, which the portal retention policy reads as its last activity. Kept apart from
/// <see cref="ClearAuditEventsService"/>, which only decides when a pass runs, so that a pass can be run with
/// retention settings of its own.
/// </summary>
[Singleton]
public sealed class AuditTrailCleaner(ILogger<AuditTrailCleaner> logger, IServiceScopeFactory serviceScopeFactory)
{
    private static readonly TimeSpan _pageDelay = TimeSpan.FromMilliseconds(200);

    public async Task ClearAsync(AuditTrailRetentionConfiguration retention, CancellationToken stoppingToken)
    {
        if (!retention.Enabled)
        {
            return;
        }

        if (!retention.IsUsable)
        {
            logger.WarningInvalidConfiguration(
                retention.PaidLifeTimeDays,
                retention.FreeLifeTimeDays,
                retention.PaidGracePeriodDays,
                retention.BatchSize,
                retention.ReadPageSize,
                retention.Period,
                AuditTrailRetentionConfiguration.MinLifeTimeDays,
                AuditTrailRetentionConfiguration.MaxLifeTimeDays,
                AuditTrailRetentionConfiguration.MaxPaidGracePeriodDays,
                AuditTrailRetentionConfiguration.MaxBatchSize,
                AuditTrailRetentionConfiguration.MaxReadPageSize,
                AuditTrailRetentionConfiguration.MinPeriod,
                AuditTrailRetentionConfiguration.MaxPeriod);
            return;
        }

        var now = DateTime.UtcNow;

        var freeThreshold = now.AddDays(-retention.FreeLifeTimeDays);
        var paidThreshold = now.AddDays(-retention.PaidLifeTimeDays);

        // A subscription that ended after this moment still counts as paid: see PaidGracePeriodDays.
        var paidGraceStart = now.AddDays(-retention.PaidGracePeriodDays);

        // Every batch is committed as it is deleted, so what a pass removed is counted as it goes and logged however
        // the pass ends: a tenant that fails halfway, or a stop that cancels a pause, has still removed what it
        // removed. What it read is logged with it, whether anything was removed or not: the cost of a pass is in the
        // history it walks again every day, which removes nothing.
        var totals = new PassTotals();

        // Set where the pass gives up because of the stop, not read from the token at the end: a stop that arrives
        // after the last tenant is done leaves a complete pass. A pass that failed outside a tenant (the list of
        // tenants could not be read) says so, rather than reading as a pass in which nothing qualified.
        var stopped = false;
        var failed = false;

        try
        {
            foreach (var tenantId in await GetTenantIdsToClearAsync(freeThreshold, stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    stopped = true;
                    return;
                }

                try
                {
                    await RemoveOldAuditEventsAsync(tenantId, freeThreshold, paidThreshold, paidGraceStart, retention, totals, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.WarningClearTenantFailed(tenantId, ex);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            stopped = true;
            throw;
        }
        catch
        {
            // Rethrown to the hosting loop, which logs the exception itself; here only the summary learns of it.
            failed = true;
            throw;
        }
        finally
        {
            LogPass(totals, stopped, failed);
        }
    }

    private void LogPass(PassTotals totals, bool stopped, bool failed)
    {
        if (failed)
        {
            logger.WarningPassFailed(Stopwatch.GetElapsedTime(totals.Started));
        }
        else if (totals.WalkedTenants == 0)
        {
            logger.InformationPassFinishedNoTenant(stopped, Stopwatch.GetElapsedTime(totals.Started));
        }
        else
        {
            logger.InformationPassFinished(
                stopped,
                Stopwatch.GetElapsedTime(totals.Started),
                totals.WalkedTenants,
                totals.Pages,
                totals.Read,
                totals.TariffLookups,
                totals.Removed,
                totals.Tenants,
                totals.SlowestTenantId,
                totals.SlowestTenantElapsed,
                totals.SlowestTenantPages);
        }
    }

    private async Task<List<int>> GetTenantIdsToClearAsync(DateTime freeThreshold, CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        return await ef.AuditEventTenantsOlderThanAsync(freeThreshold).ToListAsync(stoppingToken);
    }

    private async Task RemoveOldAuditEventsAsync(
        int tenantId,
        DateTime freeThreshold,
        DateTime paidThreshold,
        DateTime paidGraceStart,
        AuditTrailRetentionConfiguration retention,
        PassTotals totals,
        CancellationToken stoppingToken)
    {
        var started = Stopwatch.GetTimestamp();

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        // The tenants are picked by their oldest event, history included, so a tenant whose old events are all part of
        // a history, or are its newest event, is picked on every pass. The walk starts under the free life time, the
        // later of the two thresholds, and the tariff is looked up only once an event turns up that the pass could
        // remove, outside a history and not the newest: a tenant with nothing else past the free life time - an
        // abandoned portal among them - has nothing to clear on either tariff, and no lookup is paid for it.
        DateTime? threshold = null;
        var paid = false;

        // Never removed: the portal retention policy counts from the date of the newest event (and of the last login,
        // which the login history cleanup purges), so a portal idle past the life time would otherwise lose every sign
        // of activity and be counted from its creation date. Read once, with the first event outside a history.
        int? newestId = null;

        var removed = 0;
        var pages = 0;

        var afterDate = DateTime.MinValue;
        var afterId = 0;

        try
        {
            while (true)
            {
                // Thrown, not just a loop condition: a walk cut short by the stop must not read as a finished one.
                stoppingToken.ThrowIfCancellationRequested();

                // A page reads more than a batch deletes: its rows are keys, dates and history flags from the
                // (tenant_id, date) index, cheap next to a delete, and most of them are history a pass walks past.
                var events = await ef.AuditEventsOlderThanAsync(tenantId, threshold ?? freeThreshold, afterDate, afterId, retention.ReadPageSize).ToListAsync(stoppingToken);

                if (events.Count == 0)
                {
                    break;
                }

                pages++;
                totals.Read += events.Count;

                (afterId, afterDate) = (events[^1].Id, events[^1].Date);

                if (threshold is null && events.Exists(r => !r.InHistory))
                {
                    newestId ??= await ef.NewestAuditEventIdAsync(tenantId);

                    if (events.Exists(r => !r.InHistory && r.Id != newestId))
                    {
                        totals.TariffLookups++;

                        paid = await IsPaidAsync(scope.ServiceProvider, tenantId, paidGraceStart);
                        threshold = paid ? paidThreshold : freeThreshold;
                    }
                }

                // Nothing on the pages before the tariff is known: they hold only events the pass keeps anyway.
                var ids = threshold is null
                    ? []
                    : events
                        .Where(r => !r.InHistory && r.Date < threshold && r.Id != newestId)
                        .Select(r => r.Id)
                        .ToList();

                // A pause after every batch deleted, so that the deletes go at the rate of one batch per pause however
                // large a page is.
                foreach (var batch in ids.Chunk(retention.BatchSize))
                {
                    removed += await ef.DeleteAuditEventsToClearAsync(tenantId, batch);

                    await Task.Delay(_pageDelay, stoppingToken);
                }

                if (events.Count < retention.ReadPageSize)
                {
                    break;
                }

                // And after every page that deleted nothing, the ones of history only: a page reads at most a page
                // size, and the delay keeps a tenant with years of retained history from being walked as fast as the
                // database answers.
                if (ids.Count == 0)
                {
                    await Task.Delay(_pageDelay, stoppingToken);
                }
            }
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);

            totals.Walked(tenantId, pages, elapsed);

            if (removed > 0)
            {
                logger.InformationRemovedTenantAuditEvents(removed, threshold.Value, tenantId, paid, pages, elapsed);

                totals.Removed += removed;
                totals.Tenants++;
            }
        }
    }

    /// <summary>
    /// Whether the tenant keeps the paid life time: its tariff is paid, in the payment delay, or became unpaid after
    /// <paramref name="paidGraceStart"/>, and is not on a free quota.
    /// </summary>
    private static async Task<bool> IsPaidAsync(IServiceProvider serviceProvider, int tenantId, DateTime paidGraceStart)
    {
        var tariffService = serviceProvider.GetRequiredService<ITariffService>();

        var tariff = await tariffService.GetTariffAsync(tenantId, withRequestToPaymentSystem: false);

        var paid = tariff.State switch
        {
            TariffState.Paid or TariffState.Delay => true,
            TariffState.NotPaid => NotPaidSince(tariff) > paidGraceStart,
            _ => false
        };

        return paid && !await tariffService.IsFreeTariffAsync(tariff);
    }

    /// <summary>
    /// The moment an unpaid tariff became unpaid: the midnight (UTC) after the last day of its payment delay when it
    /// went through one, after its due date otherwise. The tariff service keeps a tariff paid, or in the delay, through
    /// the whole of that last day (it compares dates, not moments), so the grace period counts from the midnight
    /// after it, not from the day itself, and a period of N days is N whole days. A due date of
    /// <see cref="DateTime.MinValue"/> gives <see cref="DateTime.MinValue"/>: no grace period reaches it. A tariff the
    /// tariff service saved as unpaid without a payment, such as an ended trial on a quota that is not free, has a due
    /// date like any other and gets the grace period too, which keeps its events at most that many days longer.
    /// </summary>
    private static DateTime NotPaidSince(Tariff tariff)
    {
        var lastDay = tariff.DelayDueDate > tariff.DueDate && tariff.DelayDueDate != DateTime.MaxValue ? tariff.DelayDueDate : tariff.DueDate;

        return lastDay == DateTime.MinValue || lastDay.Date == DateTime.MaxValue.Date ? lastDay : lastDay.Date.AddDays(1);
    }

    /// <summary>What a pass has read and removed so far, across the tenants it has been through.</summary>
    private sealed class PassTotals
    {
        public long Started { get; } = Stopwatch.GetTimestamp();

        /// <summary>The tenants walked, whether anything was removed from them or not.</summary>
        public int WalkedTenants { get; private set; }

        public int Pages { get; private set; }

        public long Read { get; set; }

        public int TariffLookups { get; set; }

        public int Removed { get; set; }

        /// <summary>The tenants something was removed from.</summary>
        public int Tenants { get; set; }

        /// <summary>The tenant that took the longest, 0 before the first one; most of its time is the pauses.</summary>
        public int SlowestTenantId { get; private set; }

        public TimeSpan SlowestTenantElapsed { get; private set; }

        public int SlowestTenantPages { get; private set; }

        public void Walked(int tenantId, int pages, TimeSpan elapsed)
        {
            WalkedTenants++;
            Pages += pages;

            if (elapsed > SlowestTenantElapsed)
            {
                (SlowestTenantId, SlowestTenantElapsed, SlowestTenantPages) = (tenantId, elapsed, pages);
            }
        }
    }
}
