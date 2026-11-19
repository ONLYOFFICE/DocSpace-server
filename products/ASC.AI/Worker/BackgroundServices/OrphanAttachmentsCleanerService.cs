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

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    // d.hh:mm:ss, from 5 seconds to 1 day; fifteen minutes is "0.00:15:00"
    protected override TimeSpan ExecuteTaskPeriod { get; set; } =
        configuration.ReadInterval("ai:orphanAttachmentsCleaner:period", _defaultPeriod, _minPeriod, _maxPeriod, logger);

    // d.hh:mm:ss, from 30 minutes to 7 days; two hours is "0.02:00:00"
    private readonly TimeSpan _lifetime =
        configuration.ReadInterval("ai:orphanAttachmentsCleaner:lifetime", _defaultLifetime, _minLifetime, _maxLifetime, logger);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        var cutoffDate = DateTime.UtcNow - _lifetime;
        var createdAt = DateTime.MinValue;
        var id = Guid.Empty;
        var draftsCount = 0;
        var filesCount = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                List<OrphanAttachment> rows;

                await using (var context = await dbContextFactory.CreateDbContextAsync(stoppingToken))
                {
                    rows = await context.GetOrphanAttachmentsAsync(cutoffDate, createdAt, id, BatchSize).ToListAsync(stoppingToken);
                }

                foreach (var group in rows.GroupBy(x => x.TenantId))
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        var (drafts, files) = await DeleteAsync(group.Key, group.Select(x => x.Id).ToList());
                        draftsCount += drafts;
                        filesCount += files;
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        logger.ErrorDeleteOrphanAttachments(group.Key, e);
                    }
                }

                if (rows.Count < BatchSize)
                {
                    break;
                }

                (createdAt, id) = (rows[^1].CreatedAt, rows[^1].Id);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorCleanUpOrphanAttachments(e);
        }
        finally
        {
            if (draftsCount > 0 || filesCount > 0)
            {
                logger.InformationDeletedOrphanAttachments(draftsCount, filesCount, cutoffDate);
            }
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

            return (await context.DeleteAttachmentsAsync(tenantId, ids), 0);
        }

        tenantManager.SetCurrentTenant(tenant);
        await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);

        return await scope.ServiceProvider.GetRequiredService<AiProjectThreadsService>().DeleteChatDraftsAsync(ids);
    }
}
