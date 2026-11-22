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
    private const int CheckpointEvery = 20;

    private static readonly TimeSpan _defaultPeriod = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _minPeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _maxPeriod = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    // true or false; false switches the pass off (logged once), the internal route still converts
    private readonly bool _enabled = configuration.ReadBool("ai:agentsMigration:enabled", true, logger);

    // d.hh:mm:ss, from 5 seconds to 1 day; five minutes is "0.00:05:00"
    protected override TimeSpan ExecuteTaskPeriod { get; set; } =
        configuration.ReadInterval("ai:agentsMigration:period", _defaultPeriod, _minPeriod, _maxPeriod, logger);

    // d.hh:mm:ss, from 1 minute to 1 hour; five minutes is "0.00:05:00". Why the window needs at least a minute:
    // AiAgentsMigrationPlan.ReadOverlap.
    private readonly TimeSpan _overlap = AiAgentsMigrationPlan.ReadOverlap(configuration, logger);

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

        var checkedCount = 0;
        var completed = false;
        var passStart = default(DateTime);
        DateTime? lastPassStart = null;
        int? cursor = null;
        int? savedCursor = null;
        IReadOnlyDictionary<int, int> attempts = new Dictionary<int, int>();

        try
        {
            List<int> changedTenantIds;
            int? afterId;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var state = await scope.ServiceProvider.GetRequiredService<SettingsManager>().LoadForDefaultTenantAsync<AiAgentsMigrationPassState>();

                lastPassStart = state.LastPassStart;
                passStart = AiAgentsMigrationPlan.GetPassStart(state.PendingPassStart, DateTime.UtcNow);
                afterId = state.PendingPassStart is null ? null : state.Cursor;
                savedCursor = afterId;
                attempts = state.Attempts ?? [];
                changedTenantIds = await ReadChangedTenantIdsAsync(scope.ServiceProvider, AiAgentsMigrationPlan.GetWindowStart(lastPassStart, _overlap), afterId ?? 0, stoppingToken);
            }

            foreach (var tenantId in AiAgentsMigrationPlan.GetCandidates(changedTenantIds, attempts, _maxAttempts, afterId))
            {
                stoppingToken.ThrowIfCancellationRequested();

                var outcome = await MigratePortalAsync(tenantId, stoppingToken);

                attempts = AiAgentsMigrationPlan.GetNextAttempts(attempts, tenantId, outcome);
                cursor = tenantId;
                checkedCount++;

                if (outcome == AiAgentsMigrationPortalOutcome.Failed && AiAgentsMigrationPlan.IsLeftAlone(attempts, tenantId, _maxAttempts))
                {
                    logger.ErrorPortalLeftAlone(tenantId, _maxAttempts);
                }

                if (checkedCount % CheckpointEvery == 0 && await SaveStateAsync(AiAgentsMigrationPlan.GetCheckpoint(lastPassStart, passStart, tenantId, attempts)))
                {
                    savedCursor = tenantId;
                }
            }

            if (!await SaveStateAsync(AiAgentsMigrationPlan.GetCompleted(passStart, attempts)))
            {
                logger.WarningPassStateNotSaved();
            }

            completed = true;
        }
        finally
        {
            if (!completed && cursor is { } last && last != savedCursor)
            {
                try
                {
                    await SaveStateAsync(AiAgentsMigrationPlan.GetCheckpoint(lastPassStart, passStart, last, attempts));
                }
                catch (Exception e)
                {
                    logger.WarningProgressNotSaved(last, e);
                }
            }

            if (checkedCount > 0)
            {
                logger.InformationPassEnded(checkedCount, completed);
            }
        }
    }

    private async Task<bool> SaveStateAsync(AiAgentsMigrationPassState state)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var saved = await scope.ServiceProvider.GetRequiredService<SettingsManager>().SaveForDefaultTenantAsync(state);
        if (!saved && state.Cursor is { } cursor)
        {
            logger.WarningProgressNotSaved(cursor, null);
        }

        return saved;
    }

    private static async Task<List<int>> ReadChangedTenantIdsAsync(IServiceProvider serviceProvider, DateTime? windowStart, int afterId, CancellationToken cancellationToken)
    {
        await using var tenantDbContext = await serviceProvider.GetRequiredService<IDbContextFactory<TenantDbContext>>().CreateDbContextAsync(cancellationToken);

        if (windowStart is { } from)
        {
            return await tenantDbContext.ActiveTenantIdsModifiedSinceAsync(from).ToListAsync(cancellationToken);
        }

        var tenantIds = new List<int>();

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

    private async Task<AiAgentsMigrationPortalOutcome> MigratePortalAsync(int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

            var tenant = await tenantManager.GetTenantAsync(tenantId);
            if (tenant is not { Status: TenantStatus.Active })
            {
                return AiAgentsMigrationPortalOutcome.Done;
            }

            tenantManager.SetCurrentTenant(tenant);
            await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);

            var result = await scope.ServiceProvider.GetRequiredService<AiAgentsPortalMigration>().RunAsync(cancellationToken);
            if (result is not { } migration)
            {
                logger.DebugLegacyMigrationBusy(tenantId);

                return AiAgentsMigrationPortalOutcome.Busy;
            }

            return migration.Failed > 0 ? AiAgentsMigrationPortalOutcome.Failed : AiAgentsMigrationPortalOutcome.Done;
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            logger.ErrorPortalFailed(tenantId, e);

            return AiAgentsMigrationPortalOutcome.Failed;
        }
    }
}
