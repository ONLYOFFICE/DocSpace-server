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

public class AiAgentsMigrationService(
    ILogger<AiAgentsMigrationService> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration)
    : ActivePassiveBackgroundService<AiAgentsMigrationService>(logger, scopeFactory)
{
    private const int PageSize = 1000;

    private static readonly TimeSpan _defaultPeriod = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _minPeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _maxPeriod = TimeSpan.FromDays(1);

    private static readonly TimeSpan _defaultOverlap = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _maxOverlap = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    // true or false; false switches the pass off (logged once), the internal route still converts
    private readonly bool _enabled = configuration.ReadBool("ai:agentsMigration:enabled", true, logger);

    // d.hh:mm:ss, from 5 seconds to 1 day; five minutes is "0.00:05:00"
    protected override TimeSpan ExecuteTaskPeriod { get; set; } =
        configuration.ReadInterval("ai:agentsMigration:period", _defaultPeriod, _minPeriod, _maxPeriod, logger);

    // d.hh:mm:ss, from 0 to 1 hour; five minutes is "0.00:05:00". How far before the start of the previous pass
    // the window over the last-modified time of the portals opens: it covers the clock difference of the hosts.
    private readonly TimeSpan _overlap =
        configuration.ReadInterval("ai:agentsMigration:overlap", _defaultOverlap, TimeSpan.Zero, _maxOverlap, logger);

    // a count, from 1 to 1000; ten is "10". The passes that may end with a failed agent before a portal is left alone.
    private readonly int _maxAttempts = configuration.ReadInt("ai:agentsMigration:maxAttempts", 10, 1, 1000, logger);

    private bool _disabledLogged;

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            if (_disabledLogged)
            {
                return;
            }

            _disabledLogged = true;
            logger.InformationPassDisabled();

            return;
        }

        var passStart = DateTime.UtcNow;
        var checkedCount = 0;
        var completed = false;

        try
        {
            IReadOnlyDictionary<int, int> attempts;
            List<int> changedTenantIds;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var state = await scope.ServiceProvider.GetRequiredService<SettingsManager>().LoadForDefaultTenantAsync<AiAgentsMigrationPassState>();

                attempts = state.Attempts ?? [];
                changedTenantIds = await ReadChangedTenantIdsAsync(scope.ServiceProvider, AiAgentsMigrationPlan.GetWindowStart(state.LastPassStart, _overlap), stoppingToken);
            }

            foreach (var tenantId in AiAgentsMigrationPlan.GetCandidates(changedTenantIds, attempts, _maxAttempts))
            {
                stoppingToken.ThrowIfCancellationRequested();

                attempts = await MigratePortalAsync(tenantId, attempts, stoppingToken);
                checkedCount++;
            }

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var saved = await scope.ServiceProvider.GetRequiredService<SettingsManager>().SaveForDefaultTenantAsync(new AiAgentsMigrationPassState
                {
                    LastPassStart = passStart,
                    Attempts = new Dictionary<int, int>(attempts)
                });

                if (!saved)
                {
                    logger.WarningPassStateNotSaved();
                }
            }

            completed = true;
        }
        finally
        {
            if (checkedCount > 0)
            {
                logger.InformationPassEnded(checkedCount, completed);
            }
        }
    }

    private static async Task<List<int>> ReadChangedTenantIdsAsync(IServiceProvider serviceProvider, DateTime? windowStart, CancellationToken cancellationToken)
    {
        await using var tenantDbContext = await serviceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>().CreateDbContextAsync(cancellationToken);

        if (windowStart is { } from)
        {
            return await tenantDbContext.ActiveTenantIdsModifiedSinceAsync(from).ToListAsync(cancellationToken);
        }

        var tenantIds = new List<int>();
        var afterId = 0;

        while (true)
        {
            var page = await tenantDbContext.ActiveTenantIdsAfterAsync(afterId, PageSize).ToListAsync(cancellationToken);

            tenantIds.AddRange(page);

            if (page.Count < PageSize)
            {
                return tenantIds;
            }

            afterId = page[^1];
        }
    }

    private async Task<IReadOnlyDictionary<int, int>> MigratePortalAsync(int tenantId, IReadOnlyDictionary<int, int> attempts, CancellationToken cancellationToken)
    {
        bool failed;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

            var tenant = await tenantManager.GetTenantAsync(tenantId);
            if (tenant is not { Status: TenantStatus.Active })
            {
                return AiAgentsMigrationPlan.GetNextAttempts(attempts, tenantId, failed: false);
            }

            tenantManager.SetCurrentTenant(tenant);
            await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);

            var result = await scope.ServiceProvider.GetRequiredService<AiAgentsPortalMigration>().RunAsync(cancellationToken);
            if (result is not { } migration)
            {
                logger.DebugLegacyMigrationBusy(tenantId);

                return AiAgentsMigrationPlan.GetNextAttemptsWhenBusy(attempts, tenantId);
            }

            failed = migration.Failed > 0;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorPortalFailed(tenantId, e);
            failed = true;
        }

        var next = AiAgentsMigrationPlan.GetNextAttempts(attempts, tenantId, failed);

        if (AiAgentsMigrationPlan.IsLeftAlone(next, tenantId, _maxAttempts))
        {
            logger.ErrorPortalLeftAlone(tenantId, _maxAttempts);
        }

        return next;
    }
}
