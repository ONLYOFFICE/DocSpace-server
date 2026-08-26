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

[Scope]
public class ClearAuditEventsService(
    ILogger<ClearAuditEventsService> logger,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration) : BackgroundService
{
    private readonly AuditTrailRetentionConfiguration _retention =
        configuration.GetSection(AuditTrailRetentionConfiguration.SectionName).Get<AuditTrailRetentionConfiguration>() ?? new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_retention.Enabled)
        {
            logger.InformationDisabled();
            return;
        }

        if (_retention.PaidLifeTimeDays <= 0 || _retention.FreeLifeTimeDays <= 0 || _retention.BatchSize <= 0 || _retention.Period <= TimeSpan.Zero)
        {
            logger.WarningInvalidConfiguration(_retention.PaidLifeTimeDays, _retention.FreeLifeTimeDays, _retention.BatchSize, _retention.Period);
            return;
        }

        logger.InformationTimerRunning(_retention.PaidLifeTimeDays, _retention.FreeLifeTimeDays);

        using var timer = new PeriodicTimer(_retention.Period);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RemoveOldAuditEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.ErrorWithException(ex);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.InformationTimerStopping();
    }

    private async Task RemoveOldAuditEventsAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        var freeThreshold = now.AddDays(-_retention.FreeLifeTimeDays);
        var paidThreshold = now.AddDays(-_retention.PaidLifeTimeDays);

        var removed = 0;
        var tenants = 0;

        foreach (var tenantId in await GetTenantIdsAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var removedForTenant = await RemoveOldAuditEventsAsync(tenantId, freeThreshold, paidThreshold, stoppingToken);

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

    private async Task<List<int>> GetTenantIdsAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        return await ef.Tenants.OrderBy(r => r.Id).Select(r => r.Id).ToListAsync(stoppingToken);
    }

    private async Task<int> RemoveOldAuditEventsAsync(int tenantId, DateTime freeThreshold, DateTime paidThreshold, CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);

        if (!await ef.AuditEvents.AnyAsync(r => r.TenantId == tenantId && r.Date < freeThreshold, stoppingToken))
        {
            return 0;
        }

        var paid = await IsPaidAsync(scope.ServiceProvider, tenantId);
        var threshold = paid ? paidThreshold : freeThreshold;

        var removed = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var ids = await ef.AuditEvents
                .Where(r => r.TenantId == tenantId && r.Date < threshold)
                .OrderBy(r => r.Date)
                .Select(r => r.Id)
                .Take(_retention.BatchSize)
                .ToListAsync(stoppingToken);

            if (ids.Count == 0)
            {
                break;
            }

            var deleted = await ef.AuditEvents.Where(r => ids.Contains(r.Id)).ExecuteDeleteAsync(stoppingToken);

            if (deleted == 0)
            {
                break;
            }

            removed += deleted;
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
