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

namespace ASC.AI.Worker.BackgroundServices;

public class OrphanAttachmentsCleanerService(
    ILogger<OrphanAttachmentsCleanerService> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IDbContextFactory<AiIntegrationContext> dbContextFactory)
    : ActivePassiveBackgroundService<OrphanAttachmentsCleanerService>(logger, scopeFactory)
{
    private const int BatchSize = 100;

    private static readonly TimeSpan _defaultPeriod = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _minPeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _maxPeriod = TimeSpan.FromDays(1);

    private static readonly TimeSpan _defaultLifetime = TimeSpan.FromHours(2);
    private static readonly TimeSpan _minLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan _maxLifetime = TimeSpan.FromDays(7);

    private static readonly TimeSpan _defaultRetryWindow = TimeSpan.FromDays(7);
    private static readonly TimeSpan _minRetryWindow = TimeSpan.FromDays(1);
    private static readonly TimeSpan _maxRetryWindow = TimeSpan.FromDays(30);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    // d.hh:mm:ss, from 5 seconds to 1 day; fifteen minutes is "0.00:15:00"
    protected override TimeSpan ExecuteTaskPeriod { get; set; } =
        configuration.ReadInterval("ai:orphanAttachmentsCleaner:period", _defaultPeriod, _minPeriod, _maxPeriod, logger);

    // d.hh:mm:ss, from 30 minutes to 7 days; two hours is "0.02:00:00"
    private readonly TimeSpan _lifetime =
        configuration.ReadInterval("ai:orphanAttachmentsCleaner:lifetime", _defaultLifetime, _minLifetime, _maxLifetime, logger);

    // d.hh:mm:ss, from 1 day to 30 days; seven days is "7.00:00:00"
    private readonly TimeSpan _retryWindow =
        configuration.ReadInterval("ai:orphanAttachmentsCleaner:retryWindow", _defaultRetryWindow, _minRetryWindow, _maxRetryWindow, logger);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        var cutoffDate = now - _lifetime;
        var totals = new Totals();

        try
        {
            await SweepAsync(
                (context, position, id) => context.GetDeletedAttachmentsAsync(position, id, BatchSize),
                x => x.DeletedOn!.Value,
                now - _retryWindow,
                now,
                totals,
                stoppingToken);

            await SweepAsync(
                (context, position, id) => context.GetOrphanAttachmentsAsync(cutoffDate, position, id, BatchSize),
                x => x.CreatedAt,
                cutoffDate - _retryWindow,
                now,
                totals,
                stoppingToken);
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            logger.ErrorCleanUpOrphanAttachments(e);
        }
        finally
        {
            if (totals.Drafts > 0 || totals.Files > 0)
            {
                logger.InformationDeletedOrphanAttachments(totals.Drafts, totals.Files, cutoffDate);
            }
        }
    }

    private async Task SweepAsync(
        Func<AiIntegrationContext, DateTime, Guid, IAsyncEnumerable<OrphanAttachment>> read,
        Func<OrphanAttachment, DateTime> positionOf,
        DateTime from,
        DateTime passStart,
        Totals totals,
        CancellationToken stoppingToken)
    {
        var position = from;
        var id = Guid.Empty;

        while (!stoppingToken.IsCancellationRequested)
        {
            List<OrphanAttachment> rows;

            await using (var context = await dbContextFactory.CreateDbContextAsync(stoppingToken))
            {
                rows = await read(context, position, id).ToListAsync(stoppingToken);
            }

            foreach (var group in rows.GroupBy(x => x.TenantId))
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    var (drafts, files) = await DeleteAsync(group.Key, group.Select(x => x.Id).ToList());
                    totals.Drafts += drafts;
                    totals.Files += files;
                }
                catch (Exception e) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.ErrorDeleteOrphanAttachments(group.Key, e);

                    var nextFrom = from + (DateTime.UtcNow - passStart) + ExecuteTaskPeriod;

                    foreach (var row in group.Where(x => positionOf(x) < nextFrom))
                    {
                        logger.WarningOrphanAttachmentLeavesRetryWindow(row.Id, group.Key, positionOf(row));
                    }
                }
            }

            if (rows.Count < BatchSize)
            {
                break;
            }

            (position, id) = (positionOf(rows[^1]), rows[^1].Id);
        }
    }

    private async Task<(int Drafts, int Files)> DeleteAsync(int tenantId, List<Guid> ids)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

        var tenant = await tenantManager.GetTenantAsync(tenantId);
        if (tenant == null)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            await context.MarkOrphanAttachmentsDeletedAsync(tenantId, ids, DateTime.UtcNow);

            return (await context.DeleteAttachmentsAsync(tenantId, ids), 0);
        }

        tenantManager.SetCurrentTenant(tenant);
        await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);

        return await scope.ServiceProvider.GetRequiredService<AiProjectThreadsService>().DeleteChatDraftsAsync(ids);
    }

    private sealed class Totals
    {
        public int Drafts { get; set; }
        public int Files { get; set; }
    }
}
