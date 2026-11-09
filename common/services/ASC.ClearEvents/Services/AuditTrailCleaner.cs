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

        if (retention.BindingError is not null)
        {
            logger.WarningUnreadableConfiguration(AuditTrailRetentionConfiguration.SectionName, retention.BindingError);
            return;
        }

        if (!retention.IsUsable)
        {
            logger.WarningInvalidConfiguration(
                retention.PaidLifeTimeDays,
                retention.FreeLifeTimeDays,
                retention.BatchSize,
                retention.Period,
                AuditTrailRetentionConfiguration.MinLifeTimeDays,
                AuditTrailRetentionConfiguration.MaxLifeTimeDays,
                AuditTrailRetentionConfiguration.MaxBatchSize,
                AuditTrailRetentionConfiguration.MinPeriod,
                AuditTrailRetentionConfiguration.MaxPeriod);
            return;
        }

        var now = DateTime.UtcNow;

        var freeThreshold = now.AddDays(-retention.FreeLifeTimeDays);
        var paidThreshold = now.AddDays(-retention.PaidLifeTimeDays);

        // Every page is committed as it is deleted, so what a pass removed is counted as it goes and logged however
        // the pass ends: a tenant that fails halfway, or a stop that cancels the pause between pages, has still
        // removed what it removed.
        var totals = new PassTotals();

        try
        {
            foreach (var tenantId in await GetTenantIdsToClearAsync(freeThreshold, stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await RemoveOldAuditEventsAsync(tenantId, freeThreshold, paidThreshold, retention.BatchSize, totals, stoppingToken);
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
        finally
        {
            if (totals.Removed > 0)
            {
                logger.InformationRemovedAuditEvents(totals.Removed, totals.Tenants);
            }
        }
    }

    private async Task<List<int>> GetTenantIdsToClearAsync(DateTime freeThreshold, CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        return await ef.AuditEventTenantsOlderThanAsync(freeThreshold).ToListAsync(stoppingToken);
    }

    private async Task RemoveOldAuditEventsAsync(int tenantId, DateTime freeThreshold, DateTime paidThreshold, int batchSize, PassTotals totals, CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        // The tenants are picked by their oldest event, history included, so a tenant whose old events are all part of
        // a history is picked on every pass. The walk starts under the free life time, the later of the two thresholds,
        // and the tariff is looked up only once an event outside a history turns up: a tenant with nothing but history
        // past the free life time has nothing to clear on either tariff, and no lookup is paid for it.
        DateTime? threshold = null;
        var paid = false;

        // Never removed: the portal retention policy counts from the date of the newest event (and of the last login,
        // which the login history cleanup purges), so a portal idle past the life time would otherwise lose every sign
        // of activity and be counted from its creation date.
        var newestId = 0;

        var removed = 0;

        var afterDate = DateTime.MinValue;
        var afterId = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var events = await ef.AuditEventsOlderThanAsync(tenantId, threshold ?? freeThreshold, afterDate, afterId, batchSize).ToListAsync(stoppingToken);

                if (events.Count == 0)
                {
                    break;
                }

                (afterId, afterDate) = (events[^1].Id, events[^1].Date);

                if (threshold is null && events.Exists(r => !r.InHistory))
                {
                    paid = await IsPaidAsync(scope.ServiceProvider, tenantId);
                    threshold = paid ? paidThreshold : freeThreshold;
                    newestId = await ef.NewestAuditEventIdAsync(tenantId);
                }

                var ids = events
                    .Where(r => !r.InHistory && r.Date < threshold && r.Id != newestId)
                    .Select(r => r.Id)
                    .ToList();

                if (ids.Count > 0)
                {
                    removed += await ef.DeleteAuditEventsToClearAsync(tenantId, ids);
                }

                if (events.Count < batchSize)
                {
                    break;
                }

                // After every page, the ones of history only too: a page reads at most a batch, and the delay keeps a
                // tenant with years of retained history from being walked as fast as the database answers.
                await Task.Delay(_pageDelay, stoppingToken);
            }
        }
        finally
        {
            if (removed > 0)
            {
                logger.InformationRemovedTenantAuditEvents(removed, threshold.Value, tenantId, paid);

                totals.Removed += removed;
                totals.Tenants++;
            }
        }
    }

    private static async Task<bool> IsPaidAsync(IServiceProvider serviceProvider, int tenantId)
    {
        var tariffService = serviceProvider.GetRequiredService<ITariffService>();

        var tariff = await tariffService.GetTariffAsync(tenantId, withRequestToPaymentSystem: false);

        return tariff.State is TariffState.Paid or TariffState.Delay && !await tariffService.IsFreeTariffAsync(tariff);
    }

    /// <summary>What a pass has removed so far, across the tenants it has been through.</summary>
    private sealed class PassTotals
    {
        public int Removed { get; set; }

        public int Tenants { get; set; }
    }
}
