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

namespace ASC.Notify.Tests.Periodic;

/// <summary>
/// How the retention job reads a portal - its category, the day its count starts, whether it is ours at
/// all - against the stack's real services. The portals are built in memory and every case either
/// stops at a letter or runs as a dry run, so no portal in the stack is ever blocked or removed here.
/// </summary>
public class PortalRetentionJobTests
{
    private static readonly DateTime _today = DateTime.UtcNow.Date;

    private static readonly DateTime _policyStart = _today.AddYears(-2);

    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    private static async Task<LetterScope> OpenScopeAsync()
    {
        return await LetterScope.OpenAsync(await GetStackAsync(), CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));
    }

    /// <summary>The job under test, with its own options and a logger that keeps what it is told.</summary>
    private static PortalRetentionJob CreateJob(LetterScope scope, RecordingLogger logger, bool dryRun)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:retention:enabled"] = "true",
                ["core:retention:dryRun"] = dryRun ? "true" : "false"
            })
            .Build();

        return ActivatorUtilities.CreateInstance<PortalRetentionJob>(scope.Services, logger, new PortalRetentionConfiguration(configuration));
    }

    /// <summary>
    /// A portal that does not exist in the stack, with its status last changed long ago, so only what the
    /// case sets decides where its count starts.
    /// </summary>
    private static Tenant InMemoryTenant(TenantStatus status = TenantStatus.Active, DateTime? statusChanged = null)
    {
        return new Tenant(int.MaxValue - 1, "retention-in-memory")
        {
            Status = status,
            StatusChangeDate = statusChanged ?? DateTime.MinValue
        };
    }

    private static PeriodicLetterContext Free(Tenant tenant, DateTime lastActivity)
    {
        return PeriodicLetterContexts.Fresh(tenant, _today) with
        {
            Quota = PeriodicLetterContexts.Quota(free: true),
            Tariff = new Tariff { Quotas = [], State = TariffState.Paid, DueDate = DateTime.MaxValue, DelayDueDate = DateTime.MaxValue },
            LastActivity = PeriodicLetterContexts.Activity(lastActivity)
        };
    }

    [Fact]
    public async Task FreePortal_IdleForAMonth_IsWarned()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger();

        var leaveAlone = await CreateJob(scope, logger, dryRun: false).ApplyAsync(Free(InMemoryTenant(), _today.AddDays(-30)), _policyStart);

        leaveAlone.Should().BeFalse("a warning does not keep the other letters away");
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"));
    }

    [Fact]
    public async Task FreePortal_IdleForTwoMonths_IsBlocked_ButNotInADryRun()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger();
        var tenant = InMemoryTenant();

        var leaveAlone = await CreateJob(scope, logger, dryRun: true).ApplyAsync(Free(tenant, _today.AddDays(-60)), _policyStart);

        logger.Messages.Should().ContainSingle(m => m.Contains("dry run") && m.Contains("Free: Block"));
        leaveAlone.Should().BeFalse();
        tenant.Status.Should().Be(TenantStatus.Active, "a dry run only says what it would do");
    }

    [Fact]
    public async Task UnblockedPortal_CountsFromTheUnblocking()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger();

        // Idle for a year, but its status changed a month ago: the count restarts from that change.
        var context = Free(InMemoryTenant(statusChanged: _today.AddDays(-30)), _today.AddYears(-1));

        await CreateJob(scope, logger, dryRun: true).ApplyAsync(context, _policyStart);

        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"));
    }

    [Fact]
    public async Task BlockedPortal_PastItsRetention_IsDeleted_ButNotInADryRun()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger();
        var tenant = InMemoryTenant(TenantStatus.Blocked, _today.AddDays(-30));

        var leaveAlone = await CreateJob(scope, logger, dryRun: true).ApplyAsync(Free(tenant, _today.AddYears(-1)), _policyStart);

        leaveAlone.Should().BeTrue("a blocked portal gets none of the ordinary letters");
        logger.Messages.Should().ContainSingle(m => m.Contains("dry run") && m.Contains("Free: Delete"));
        tenant.Status.Should().Be(TenantStatus.Blocked);
    }

    [Fact]
    public async Task FormerPayingPortal_CountsFromTheDueDate()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger();

        // Active yesterday, but the tariff lapsed ninety days ago: the activity does not count.
        var context = PeriodicLetterContexts.Lapsed(PeriodicLetterContexts.Fresh(InMemoryTenant(), _today), _today.AddDays(-90)) with
        {
            LastActivity = PeriodicLetterContexts.Activity(_today.AddDays(-1))
        };

        await CreateJob(scope, logger, dryRun: true).ApplyAsync(context, _policyStart);

        logger.Messages.Should().ContainSingle(m => m.Contains("FormerPaying: Block"));
    }

    [Fact]
    public async Task PayingPortal_IsLeftAlone()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger();

        var paid = PeriodicLetterContexts.Paid(PeriodicLetterContexts.Fresh(InMemoryTenant(), _today), _today.AddYears(1)) with
        {
            LastActivity = PeriodicLetterContexts.Activity(_today.AddYears(-1))
        };

        var delayed = PeriodicLetterContexts.Delayed(PeriodicLetterContexts.Fresh(InMemoryTenant(), _today), _today.AddDays(3));

        (await CreateJob(scope, logger, dryRun: true).ApplyAsync(paid, _policyStart)).Should().BeFalse();
        (await CreateJob(scope, logger, dryRun: true).ApplyAsync(delayed, _policyStart)).Should().BeFalse();

        logger.Messages.Should().BeEmpty("a paid portal or one in its grace period is never counted");
    }

    /// <summary>Keeps the formatted messages, which is all these cases look at.</summary>
    private sealed class RecordingLogger : ILogger<PortalRetentionJob>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
