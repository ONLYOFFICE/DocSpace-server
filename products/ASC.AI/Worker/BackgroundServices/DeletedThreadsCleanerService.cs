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

public class DeletedThreadsCleanerService(
    ILogger<DeletedThreadsCleanerService> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration)
    : ActivePassiveBackgroundService<DeletedThreadsCleanerService>(logger, scopeFactory)
{
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = ReadPeriod(configuration);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        var deletedOn = DateTime.MinValue;
        var id = Guid.Empty;
        var totalCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            List<DeletedThread> threads;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                threads = await scope.ServiceProvider.GetRequiredService<ThreadsStorage>().ReadDeletedAsync(deletedOn, id, BatchSize);
            }

            foreach (var thread in threads)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    await DeleteAsync(thread);
                    totalCount++;
                }
                catch (Exception e)
                {
                    logger.ErrorDeleteThread(thread.TenantId, thread.Id, e);
                }
            }

            if (threads.Count < BatchSize)
            {
                break;
            }

            (deletedOn, id) = (threads[^1].DeletedOn, threads[^1].Id);
        }

        if (totalCount > 0)
        {
            logger.InformationDeletedThreads(totalCount);
        }
    }

    private async Task DeleteAsync(DeletedThread thread)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

        var tenant = await tenantManager.GetTenantAsync(thread.TenantId);
        if (tenant == null)
        {
            await scope.ServiceProvider.GetRequiredService<ThreadsStorage>().DeleteMarkedAsync(thread.TenantId, thread.Id);

            return;
        }

        tenantManager.SetCurrentTenant(tenant);
        await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);

        await scope.ServiceProvider.GetRequiredService<AiProjectThreadsService>().DeleteMarkedAsync(thread);
    }

    private static TimeSpan ReadPeriod(IConfiguration configuration)
    {
        var value = configuration.GetValue<string>("ai:deletedThreadsCleaner:period");

        return string.IsNullOrEmpty(value) ? TimeSpan.FromMinutes(5) : TimeSpan.Parse(value, CultureInfo.InvariantCulture);
    }
}
