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
/// the tenant's tariff, except those that are part of the history of a file, folder or room. Kept apart from
/// <see cref="ClearAuditEventsService"/>, which only decides when a pass runs, so that a pass can be run with
/// retention settings of its own.
/// </summary>
[Singleton]
public sealed class AuditTrailCleaner(ILogger<AuditTrailCleaner> logger, IServiceScopeFactory serviceScopeFactory)
{
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
                retention.BatchSize,
                retention.Period,
                AuditTrailRetentionConfiguration.MinLifeTimeDays,
                AuditTrailRetentionConfiguration.MaxPeriod);
            return;
        }

        var now = DateTime.UtcNow;

        var freeThreshold = now.AddDays(-retention.FreeLifeTimeDays);
        var paidThreshold = now.AddDays(-retention.PaidLifeTimeDays);

        var removed = 0;
        var tenants = 0;

        foreach (var tenantId in await GetTenantIdsToClearAsync(freeThreshold, stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var removedForTenant = await RemoveOldAuditEventsAsync(tenantId, freeThreshold, paidThreshold, retention.BatchSize, stoppingToken);

                if (removedForTenant == 0)
                {
                    continue;
                }

                removed += removedForTenant;
                tenants++;
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

        if (removed > 0)
        {
            logger.InformationRemovedAuditEvents(removed, tenants);
        }
    }

    private async Task<List<int>> GetTenantIdsToClearAsync(DateTime freeThreshold, CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        return await ef.AuditEventTenantsOlderThanAsync(freeThreshold).ToListAsync(stoppingToken);
    }

    private async Task<int> RemoveOldAuditEventsAsync(int tenantId, DateTime freeThreshold, DateTime paidThreshold, int batchSize, CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        // The tenants are picked by their oldest event, history included, so a tenant whose old events are all part of
        // a history is picked on every pass. Nothing past the free life time outside a history means nothing to clear
        // on either tariff, the paid life time being the longer one, and the tariff is not looked up for it.
        if (!await ef.AuditEventsToClearAsync(tenantId, freeThreshold, null, 0, 1).AnyAsync(stoppingToken))
        {
            return 0;
        }

        var paid = await IsPaidAsync(scope.ServiceProvider, tenantId);
        var threshold = paid ? paidThreshold : freeThreshold;

        var removed = 0;

        DateTime? afterDate = null;
        var afterId = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var events = await ef.AuditEventsToClearAsync(tenantId, threshold, afterDate, afterId, batchSize).ToListAsync(stoppingToken);

            if (events.Count == 0)
            {
                break;
            }

            (afterId, afterDate) = (events[^1].Id, events[^1].Date);

            removed += await ef.DeleteAuditEventsToClearAsync(tenantId, events.Select(r => r.Id).ToList());

            if (events.Count < batchSize)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
        }

        if (removed > 0)
        {
            logger.DebugRemovedAuditEvents(removed, threshold, tenantId, paid);
        }

        return removed;
    }

    private static async Task<bool> IsPaidAsync(IServiceProvider serviceProvider, int tenantId)
    {
        var tariffService = serviceProvider.GetRequiredService<ITariffService>();

        var tariff = await tariffService.GetTariffAsync(tenantId, withRequestToPaymentSystem: false);

        return tariff.State is TariffState.Paid or TariffState.Delay && !await tariffService.IsFreeTariffAsync(tariff);
    }
}
