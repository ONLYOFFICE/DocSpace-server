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

    protected override TimeSpan ExecuteTaskPeriod { get; set; } =
        ReadTimeSpan(configuration, "period", TimeSpan.FromHours(1));

    private readonly TimeSpan _lifetime =
        ReadTimeSpan(configuration, "lifetime", TimeSpan.FromHours(24));

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - _lifetime;
            var totalCount = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(stoppingToken);

                var rows = await context.GetOrphanAttachmentsAsync(cutoffDate, BatchSize)
                    .ToListAsync(stoppingToken);

                if (rows.Count == 0)
                {
                    break;
                }

                foreach (var group in rows.GroupBy(x => x.TenantId))
                {
                    totalCount += await context.DeleteAttachmentsAsync(group.Key, group.Select(x => x.Id));
                }

                if (rows.Count < BatchSize)
                {
                    break;
                }
            }

            if (totalCount > 0)
            {
                logger.InformationDeletedOrphanAttachments(totalCount, cutoffDate);
            }
        }
        catch (Exception e)
        {
            logger.ErrorCleanUpOrphanAttachments(e);
        }
    }

    private static TimeSpan ReadTimeSpan(IConfiguration configuration, string key, TimeSpan fallback)
    {
        var value = configuration.GetValue<string>($"ai:orphanAttachmentsCleaner:{key}");

        return string.IsNullOrEmpty(value) ? fallback : TimeSpan.Parse(value, CultureInfo.InvariantCulture);
    }
}
