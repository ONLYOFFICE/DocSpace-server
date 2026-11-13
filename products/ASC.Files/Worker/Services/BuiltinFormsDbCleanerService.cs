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
/// Drops the built-in forms database tables nobody has submitted to or analysed for the idle period, when the search
/// index can restore them whole on the next submission or analysis; the others are kept.
/// </summary>
[Singleton]
public class BuiltinFormsDbCleanerService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<BuiltinFormsDbCleanerService> logger)
    : ActivePassiveBackgroundService<BuiltinFormsDbCleanerService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    // A table in use is touched at most once an hour, so a shorter idle period would drop tables still in use.
    private TimeSpan IdleInterval { get; } = ReadInterval(configuration, logger, "files:builtinFormsDbCleaner:idle", TimeSpan.FromDays(30), TimeSpan.FromDays(1));

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = ReadInterval(configuration, logger, "files:builtinFormsDbCleaner:period", TimeSpan.FromDays(1), TimeSpan.FromMinutes(10));

    private static TimeSpan ReadInterval(IConfiguration configuration, ILogger<BuiltinFormsDbCleanerService> logger, string key, TimeSpan fallback, TimeSpan minimum)
    {
        var value = configuration.GetValue<string>(key);
        if (string.IsNullOrEmpty(value))
        {
            return fallback;
        }

        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var interval) || interval < minimum)
        {
            logger.WarnIntervalOutOfRange(key, value, fallback);
            return fallback;
        }

        return interval;
    }

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<int> tenantIds;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var provisioner = scope.ServiceProvider.GetRequiredService<FormsDbProvisioningService>();
                if (!provisioner.IsEnabled())
                {
                    return;
                }

                // Removes what a failed portal removal or an interrupted move into a portal's own database left behind.
                await provisioner.ReconcileAsync(stoppingToken);

                // A changed connection limit reaches the portals provisioned before it here; the cleanup goes on without.
                try
                {
                    var changed = await provisioner.ApplyConnectionLimitAsync(stoppingToken);
                    if (changed > 0)
                    {
                        logger.InfoConnectionLimitApplied(changed);
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.WarnConnectionLimitNotApplied(e);
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

            // The report creator shares this client within the scope, so the whole sweep stays on the existing schema.
            var client = scope.ServiceProvider.GetRequiredService<BuiltinFormsDatabaseClient>();
            if (!await client.UseProvisionedOnlyAsync())
            {
                return;
            }

            var reportCreator = scope.ServiceProvider.GetRequiredService<FormFillingReportCreator>();
            var dropped = 0;

            try
            {
                foreach (var tableName in await client.GetIdleTablesAsync(cutoff))
                {
                    // One table that cannot be checked now leaves the others to be dropped; it is checked again next time.
                    try
                    {
                        if (await reportCreator.DropIdleBuiltinTableAsync(tableName, cutoff))
                        {
                            dropped++;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.WarnIdleTableNotChecked(e, tenantId, tableName);
                    }
                }
            }
            finally
            {
                client.ReleaseConnections();
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
    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: {key} = {value} is not a valid interval or is too short, {fallback} is used")]
    public static partial void WarnIntervalOutOfRange(this ILogger<BuiltinFormsDbCleanerService> logger, string key, string value, TimeSpan fallback);

    [LoggerMessage(LogLevel.Information, "Built-in forms DB: the connection limit of {count} portal accounts was set to the configured one")]
    public static partial void InfoConnectionLimitApplied(this ILogger<BuiltinFormsDbCleanerService> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: the configured connection limit could not be applied to the existing portal accounts")]
    public static partial void WarnConnectionLimitNotApplied(this ILogger<BuiltinFormsDbCleanerService> logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Built-in forms DB: dropped {count} idle tables of tenant {tenantId}")]
    public static partial void InfoIdleTablesDropped(this ILogger<BuiltinFormsDbCleanerService> logger, int tenantId, int count);

    [LoggerMessage(LogLevel.Warning, "Built-in forms DB: idle table {tableName} of tenant {tenantId} could not be checked")]
    public static partial void WarnIdleTableNotChecked(this ILogger<BuiltinFormsDbCleanerService> logger, Exception exception, int tenantId, string tableName);
}
