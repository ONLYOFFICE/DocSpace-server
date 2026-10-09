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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// How the retention job reads a portal - its category, the day its count starts, whether it is ours at
/// all - against the stack's real services. The portals are built in memory and every case stops at a
/// letter, so no portal in the stack is ever blocked or removed here; that is
/// <see cref="PortalRetentionLifecycleTests"/>, on portals of its own.
/// </summary>
public class PortalRetentionJobTests
{
    private static readonly DateTime _today = DateTime.UtcNow.Date;

    private static readonly DateTime _policyStart = _today.AddYears(-2);

    private static readonly string _senderName = ASC.Core.Configuration.Constants.NotifyEMailSenderSysName;

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

    /// <summary>
    /// The job under test, with a logger that keeps what it is told, the policy's defaults overridden by
    /// <paramref name="retention"/> (keys under <c>core:retention</c>), and <paramref name="tariffService"/> in
    /// place of the stack's when given.
    /// </summary>
    private static PortalRetentionJob CreateJob(LetterScope scope, RecordingLogger<PortalRetentionJob> logger, Dictionary<string, string?>? retention = null,
        ITariffService? tariffService = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection((retention ?? []).ToDictionary(r => $"core:retention:{r.Key}", r => r.Value))
            .Build();

        object[] arguments = tariffService is null
            ? [logger, new PortalRetentionConfiguration(configuration)]
            : [logger, new PortalRetentionConfiguration(configuration), tariffService];

        return ActivatorUtilities.CreateInstance<PortalRetentionJob>(scope.Services, arguments);
    }

    /// <summary>
    /// The stack's tariff service in front of an accounting service whose answers the case writes. The stack runs
    /// none, so - as <c>AccountingClientTests</c> do - the real <see cref="AccountingClient"/> is configured with
    /// an address and its network handler is replaced: the request goes through the client's own retries and
    /// error mapping, and the cancellation reaches the handler. The stack is standalone, where every portal has
    /// the installation's accounting key, so a portal that exists only in memory is asked about as well.
    /// </summary>
    private static (ITariffService TariffService, ServiceProvider Accounting) WithAccounting(LetterScope scope,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:accounting:url"] = "https://accounting.example.com/api",
                ["core:accounting:key"] = "test-key",
                ["core:accounting:secret"] = "test-secret"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(typeof(ICache), typeof(AscCache));
        services.AddScoped<AccountingClient>();
        services.AddFusionCache();
        services.AddAccountingHttpClient(configuration);

        // AddRefitGeneratedClient sets the primary handler of its own named client, so the override targets it.
        services.AddHttpClient(Refit.UniqueName.ForType<IAccountingApi>())
                .ConfigurePrimaryHttpMessageHandler(() => new RespondingHandler(respond));

        var accounting = services.BuildServiceProvider();
        var tariffService = ActivatorUtilities.CreateInstance<TariffService>(scope.Services, accounting.GetRequiredService<AccountingClient>());

        return (tariffService, accounting);
    }

    private sealed class RespondingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return respond(request, cancellationToken);
        }
    }

    /// <summary>A balance with nothing left on it.</summary>
    private static HttpResponseMessage EmptyBalance()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"accountNumber":1,"accountCurrency":"USD","subAccounts":[]}""", Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    /// A portal that does not exist in the stack, owned by the stack portal's owner so its letters have
    /// someone to go to, with its status last changed long ago, so only what the case sets decides where
    /// its count starts.
    /// </summary>
    private static Tenant InMemoryTenant(LetterScope scope, TenantStatus status = TenantStatus.Active, DateTime? statusChanged = null, int id = int.MaxValue - 1)
    {
        return new Tenant(id, "retention-in-memory")
        {
            OwnerId = scope.Tenant.OwnerId,
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
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        var leaveAlone = await CreateJob(scope, logger).ApplyAsync(Free(InMemoryTenant(scope), _today.AddDays(-30)), _policyStart, client, _senderName);

        leaveAlone.Should().BeFalse("a warning does not keep the other letters away");
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"));
        client.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionInactivityWarningNotifyAction>("a free portal is told to sign in");
    }

    [Fact]
    public async Task UnblockedPortal_CountsFromTheUnblocking()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        // Idle for a year, but its status changed a month ago: the count restarts from that change.
        var context = Free(InMemoryTenant(scope, statusChanged: _today.AddDays(-30)), _today.AddYears(-1));

        await CreateJob(scope, logger).ApplyAsync(context, _policyStart, client, _senderName);

        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"));
    }

    [Fact]
    public async Task BlockedPortal_CountsFromTheBlock()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        // Idle for a year, but blocked twenty-three days ago: a week of its thirty days is left.
        var tenant = InMemoryTenant(scope, TenantStatus.Blocked, _today.AddDays(-23));

        var leaveAlone = await CreateJob(scope, logger).ApplyAsync(Free(tenant, _today.AddYears(-1)), _policyStart, client, _senderName);

        leaveAlone.Should().BeTrue("a blocked portal gets none of the ordinary letters");
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FinalDeletionNotice"));
        client.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionFinalReminderNotifyAction>();
    }

    [Fact]
    public async Task Run_SendsTheLettersOfTheDaysSinceTheLastOne_AtMostAWeekBack()
    {
        using var scope = await OpenScopeAsync();
        var job = CreateJob(scope, new RecordingLogger<PortalRetentionJob>());
        var settingsManager = scope.Services.GetRequiredService<SettingsManager>();

        var saved = await settingsManager.LoadForDefaultTenantAsync<PortalRetentionPolicyStartSettings>();

        try
        {
            await settingsManager.SaveForDefaultTenantAsync(new PortalRetentionPolicyStartSettings { StartedOn = _policyStart, LastRunOn = _today.AddDays(-3) });

            (await job.BeginRunAsync(_today)).Should().Be((_policyStart, (DateTime?)_today.AddDays(-3)), "the days the job did not run are this run's");

            await settingsManager.SaveForDefaultTenantAsync(new PortalRetentionPolicyStartSettings { StartedOn = _policyStart, LastRunOn = _today.AddDays(-30) });

            (await job.BeginRunAsync(_today)).LastRunOn.Should().Be(_today.AddDays(-7), "a long stop is caught up a week back only");

            await job.EndRunAsync(_today);

            var after = await settingsManager.LoadForDefaultTenantAsync<PortalRetentionPolicyStartSettings>();

            after.LastRunOn.Should().Be(_today);
            after.StartedOn.Should().Be(_policyStart, "closing a run does not move the start of the policy");
        }
        finally
        {
            await settingsManager.SaveForDefaultTenantAsync(saved);
        }
    }

    [Fact]
    public async Task FormerPayingPortal_CountsFromTheDueDate()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        // Active yesterday, but the tariff lapsed sixty days ago, past any grace period the stack is set
        // up with: the activity does not count.
        var context = PeriodicLetterContexts.Lapsed(PeriodicLetterContexts.Fresh(InMemoryTenant(scope), _today), _today.AddDays(-60)) with
        {
            LastActivity = PeriodicLetterContexts.Activity(_today.AddDays(-1))
        };

        await CreateJob(scope, logger).ApplyAsync(context, _policyStart, client, _senderName);

        logger.Messages.Should().ContainSingle(m => m.Contains("FormerPaying: Notify SecondNotice"));
        client.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionUnpaidWarningNotifyAction>("a lapsed portal is told to renew");
    }

    /// <summary>
    /// A portal of the stack's own for a case that keeps something with the portal - a warning, a sign-in -
    /// since settings are stored only for a portal that exists. It is handed to the job in memory all the
    /// same, so only what the case sets decides where its count starts; none of these cases blocks it.
    /// </summary>
    private static async Task<LetterPortalClients> CreatePortalAsync()
    {
        return await (await GetStackAsync()).CreatePortalAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FreePortal_PastItsBlockWithoutAWarning_IsWarnedOfABlockAMonthAway()
    {
        using var portal = await CreatePortalAsync();
        using var scope = await OpenScopeAsync();
        var settingsManager = scope.Services.GetRequiredService<SettingsManager>();
        var tenant = InMemoryTenant(scope, id: portal.TenantId);
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        // Idle for seventy-five days, and no warning on record: lost in a long stop of the job.
        var leaveAlone = await CreateJob(scope, logger).ApplyAsync(Free(tenant, _today.AddDays(-75)), _policyStart, client, _senderName);

        leaveAlone.Should().BeFalse("the portal is not blocked");
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"), string.Join(" | ", logger.Messages));
        client.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionInactivityWarningNotifyAction>();

        var kept = await settingsManager.LoadAsync<PortalRetentionSettings>(tenant.Id);

        kept.LastWarning.Should().Be(new PortalRetentionWarning(_today, _today.AddDays(30)), "the block waits as long after the warning as the free schedule keeps them apart");
    }

    [Fact]
    public async Task FreePortal_WarnedOnTheWalletSchedule_IsNotBlockedBeforeTheDayItWasTold()
    {
        using var portal = await CreatePortalAsync();
        using var scope = await OpenScopeAsync();
        var settingsManager = scope.Services.GetRequiredService<SettingsManager>();
        var tenant = InMemoryTenant(scope, id: portal.TenantId);
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        // Told eighteen days ago, with money on its wallet, that it is blocked in 165 days. The stack has no
        // accounting service, so the wallet reads empty now, and the free schedule's own block day is long
        // past.
        (await settingsManager.SaveAsync(new PortalRetentionSettings { LastWarning = new PortalRetentionWarning(_today.AddDays(-18), _today.AddDays(165)) }, tenant.Id))
            .Should().BeTrue();

        var leaveAlone = await CreateJob(scope, logger).ApplyAsync(Free(tenant, _today.AddDays(-200)), _policyStart, client, _senderName);

        leaveAlone.Should().BeFalse();
        logger.Messages.Should().BeEmpty("the block keeps to the day the owner was told");
        client.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task FreePortal_SignInPurgedFromItsHistory_KeepsItsCount()
    {
        using var portal = await CreatePortalAsync();
        using var scope = await OpenScopeAsync();
        var settingsManager = scope.Services.GetRequiredService<SettingsManager>();
        var tenant = InMemoryTenant(scope, id: portal.TenantId);

        // Warned forty days ago of a block ten days ago, but the owner signed in twenty days ago; the audit
        // trail has nothing newer than a year.
        (await settingsManager.SaveAsync(new PortalRetentionSettings { LastWarning = new PortalRetentionWarning(_today.AddDays(-40), _today.AddDays(-10)) }, tenant.Id))
            .Should().BeTrue();

        var signedIn = Free(tenant, _today.AddYears(-1)) with
        {
            LastActivity = PeriodicLetterContexts.Activity(_today.AddYears(-1), _today.AddDays(-20))
        };

        var logger = new RecordingLogger<PortalRetentionJob>();

        await CreateJob(scope, logger).ApplyAsync(signedIn, _policyStart, new RecordingNotifyClient(), _senderName);

        logger.Messages.Should().BeEmpty("twenty days after the sign-in nothing is due");
        (await settingsManager.LoadAsync<PortalRetentionSettings>(tenant.Id)).LastActivityOn
            .Should().Be(_today.AddDays(-20), "the latest activity is kept, since the login history is purged");

        // The next day the portal's audit settings purge its login history: the last sign-in reads as the
        // year-old audit event, which on its own would put the portal past the block it was warned of.
        var purged = Free(tenant, _today.AddYears(-1)) with { NowDate = _today.AddDays(1) };

        logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        var leaveAlone = await CreateJob(scope, logger).ApplyAsync(purged, _policyStart, client, _senderName);

        leaveAlone.Should().BeFalse();
        logger.Messages.Should().BeEmpty("the kept sign-in still starts the count, so the earlier warning is of a count that is over");
        client.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task FreePortal_ActivityIsReadOnlyOnADayTheScheduleMayAct()
    {
        using var portal = await CreatePortalAsync();
        using var scope = await OpenScopeAsync();
        var settingsManager = scope.Services.GetRequiredService<SettingsManager>();
        var tenant = InMemoryTenant(scope, id: portal.TenantId);
        var job = CreateJob(scope, new RecordingLogger<PortalRetentionJob>());

        // Seen in use ten days ago: counted from there nothing is due for another twenty days, and later
        // activity would only move that further, so the database is not asked.
        (await settingsManager.SaveAsync(new PortalRetentionSettings { LastActivityOn = _today.AddDays(-10) }, tenant.Id)).Should().BeTrue();

        var quiet = Free(tenant, _today.AddDays(-10));

        (await job.ApplyAsync(quiet, _policyStart, new RecordingNotifyClient(), _senderName)).Should().BeFalse();
        quiet.LastActivity.IsValueCreated.Should().BeFalse("nothing can be due today, so the activity is not read");

        // Seen thirty days ago: the first warning would be due today, so the activity is read first.
        (await settingsManager.SaveAsync(new PortalRetentionSettings { LastActivityOn = _today.AddDays(-30) }, tenant.Id)).Should().BeTrue();

        var logger = new RecordingLogger<PortalRetentionJob>();
        var due = Free(tenant, _today.AddDays(-30));

        await CreateJob(scope, logger).ApplyAsync(due, _policyStart, new RecordingNotifyClient(), _senderName);

        due.LastActivity.IsValueCreated.Should().BeTrue("a day the schedule may act on is decided on the real activity");
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"), string.Join(" | ", logger.Messages));
    }

    [Fact]
    public async Task FreePortal_WithAShortLoginHistory_HasItsActivityReadEveryRun()
    {
        using var portal = await CreatePortalAsync();
        using var scope = await OpenScopeAsync();
        var settingsManager = scope.Services.GetRequiredService<SettingsManager>();
        var tenant = InMemoryTenant(scope, id: portal.TenantId);
        var job = CreateJob(scope, new RecordingLogger<PortalRetentionJob>());

        // A login history kept for ten days could lose a sign-in between two of the monthly reads.
        (await settingsManager.SaveAsync(new TenantAuditSettings { LoginHistoryLifeTime = 10, AuditTrailLifeTime = TenantAuditSettings.MaxLifeTime }, tenant.Id))
            .Should().BeTrue();
        (await settingsManager.SaveAsync(new PortalRetentionSettings { LastActivityOn = _today.AddDays(-10) }, tenant.Id)).Should().BeTrue();

        var saved = await settingsManager.LoadForDefaultTenantAsync<PortalRetentionPolicyStartSettings>();

        try
        {
            await job.BeginRunAsync(_today);
        }
        finally
        {
            await settingsManager.SaveForDefaultTenantAsync(saved);
        }

        var context = Free(tenant, _today.AddDays(-10));

        await job.ApplyAsync(context, _policyStart, new RecordingNotifyClient(), _senderName);

        context.LastActivity.IsValueCreated.Should().BeTrue("the run reads the activity of such a portal every day");
    }

    [Fact(Timeout = 60000)]
    public async Task FreePortal_AccountingDoesNotAnswer_WaitsForTheNextRun()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        // An accounting service that never answers: the request ends only when the job gives up on it - or when
        // the test runs out of time, should the job never give up.
        var testCancellation = TestContext.Current.CancellationToken;

        var (tariffService, accounting) = WithAccounting(scope, async (_, cancellationToken) =>
        {
            using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, testCancellation);

            await Task.Delay(Timeout.Infinite, waiting.Token);

            return EmptyBalance();
        });

        await using var accountingServices = accounting;

        var job = CreateJob(scope, logger, new Dictionary<string, string?> { ["balanceTimeoutSeconds"] = "1" }, tariffService);
        var waited = Stopwatch.StartNew();

        var leaveAlone = await job.ApplyAsync(Free(InMemoryTenant(scope), _today.AddDays(-30)), _policyStart, client, _senderName);

        waited.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(15), "the request is cut off after the configured second, not the client's minute");
        leaveAlone.Should().BeFalse();
        logger.Messages.Should().ContainSingle(m => m.Contains("the wallet balance could not be read"));
        client.Sent.Should().BeEmpty("not knowing which schedule applies, the portal waits for the next run");
    }

    [Fact]
    public async Task Accounting_FailingInARow_IsNotAskedForTheRestOfTheRun()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger<PortalRetentionJob>();

        // Unreachable twice, answers once - which starts the count again - then unreachable for good. A refused
        // connection is not retried by the client, so every failure is one request.
        bool[] answers = [false, false, true, false, false, false, false];
        var asked = 0;

        var (tariffService, accounting) = WithAccounting(scope, (_, _) =>
            answers[asked++] ? Task.FromResult(EmptyBalance()) : throw new HttpRequestException("Connection refused"));

        await using var accountingServices = accounting;

        var job = CreateJob(scope, logger, new Dictionary<string, string?> { ["balanceFailuresBeforeStop"] = "3" }, tariffService);

        for (var run = 0; run < answers.Length; run++)
        {
            await job.ApplyAsync(Free(InMemoryTenant(scope), _today.AddDays(-30)), _policyStart, new RecordingNotifyClient(), _senderName);
        }

        asked.Should().Be(6, "after the third failure in a row the seventh portal is not asked about");
        logger.Messages.Should().ContainSingle(m => m.Contains("requests in a row failed"));
        logger.Messages.Count(m => m.Contains("the wallet balance could not be read")).Should().Be(5, "only a request that was made and failed is reported");
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Notify FirstNotice"), "the portal it got an answer for is warned");
    }

    [Fact]
    public async Task PayingPortal_IsLeftAlone()
    {
        using var scope = await OpenScopeAsync();
        var logger = new RecordingLogger<PortalRetentionJob>();
        var client = new RecordingNotifyClient();

        var paid = PeriodicLetterContexts.Paid(PeriodicLetterContexts.Fresh(InMemoryTenant(scope), _today), _today.AddYears(1)) with
        {
            LastActivity = PeriodicLetterContexts.Activity(_today.AddYears(-1))
        };

        var delayed = PeriodicLetterContexts.Delayed(PeriodicLetterContexts.Fresh(InMemoryTenant(scope), _today), _today.AddDays(3));

        (await CreateJob(scope, logger).ApplyAsync(paid, _policyStart, client, _senderName)).Should().BeFalse();
        (await CreateJob(scope, logger).ApplyAsync(delayed, _policyStart, client, _senderName)).Should().BeFalse();

        logger.Messages.Should().BeEmpty("a paid portal or one in its grace period is never counted");
        client.Sent.Should().BeEmpty();
    }
}
