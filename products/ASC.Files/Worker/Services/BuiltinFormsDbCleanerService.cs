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

namespace ASC.Files.Worker.Services;

/// <summary>
/// Drops the built-in forms database tables nobody has submitted to or analysed for the idle period. They are restored
/// from the search index on the next submission or analysis.
/// </summary>
[Singleton]
public class BuiltinFormsDbCleanerService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<BuiltinFormsDbCleanerService> logger)
    : ActivePassiveBackgroundService<BuiltinFormsDbCleanerService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private TimeSpan IdleInterval { get; } = TimeSpan.Parse(configuration.GetValue<string>("files:builtinFormsDbCleaner:idle") ?? "30.0:0:0");

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration.GetValue<string>("files:builtinFormsDbCleaner:period") ?? "1.0:0:0");

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<int> tenantIds;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                if (!scope.ServiceProvider.GetRequiredService<FormsDbProvisioningService>().IsEnabled())
                {
                    return;
                }

                await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<WebstudioDbContext>>().CreateDbContextAsync(stoppingToken);
                tenantIds = await dbContext.ActiveTenantsWithSettingsAsync(BuiltinFormsDbSettings.ID).ToListAsync(stoppingToken);
            }

            var cutoff = DateTime.UtcNow - IdleInterval;

            foreach (var tenantId in tenantIds)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                await CleanUpTenantAsync(tenantId, cutoff);
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async Task CleanUpTenantAsync(int tenantId, DateTime cutoff)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(tenantId);

            // A removed schema leaves an empty settings row behind; the cleanup must not provision it again.
            var settings = await scope.ServiceProvider.GetRequiredService<SettingsManager>().LoadAsync<BuiltinFormsDbSettings>(tenantId);
            if (!settings.IsProvisioned)
            {
                return;
            }

            var client = scope.ServiceProvider.GetRequiredService<BuiltinFormsDatabaseClient>();
            var dropped = 0;

            foreach (var tableName in await client.GetIdleTablesAsync(cutoff))
            {
                if (await client.DropIdleTableAsync(tableName, cutoff))
                {
                    dropped++;
                }
            }

            if (dropped > 0)
            {
                logger.InfoIdleTablesDropped(tenantId, dropped);
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }
}

internal static partial class BuiltinFormsDbCleanerServiceLogger
{
    [LoggerMessage(LogLevel.Information, "Built-in forms DB: dropped {count} idle tables of tenant {tenantId}")]
    public static partial void InfoIdleTablesDropped(this ILogger<BuiltinFormsDbCleanerService> logger, int tenantId, int count);
}
