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
namespace ASC.Notify.Tests;

/// <summary>
/// When each periodic letter goes out. Every case names the day the letter is due and one day it must
/// stay silent on, because a predicate that is merely too generous still passes a "does it fire" check.
///
/// These conditions used to be an <c>else if</c> chain inside <c>StudioPeriodicNotify</c>, where nothing
/// could reach them: the chain needed a tenant, a tariff service and a database. Now each letter answers
/// from a <see cref="PeriodicLetterContext"/> alone, so the schedule is testable without any of that -
/// the action is built the same way <see cref="LetterTestBase{TAction}"/> builds it, without a
/// constructor, since a predicate reads nothing but the context.
///
/// The letters judge themselves independently, so the mutual exclusion the chain used to give for free
/// now has to be written down. <see cref="OnlyOneEnterpriseLetterClaimsAPortal"/> is where that is checked.
/// </summary>
public class PeriodicLetterScheduleTests
{
    /// <summary>An unremarkable Tuesday. Nothing about the cases depends on which day it is.</summary>
    private static readonly DateTime Today = new(2026, 6, 16);

    /// <summary>
    /// A portal nothing is due for: created today, on a paid-for-nothing trial with no dates set. Each
    /// case moves only what its own letter looks at.
    /// </summary>
    private static PeriodicLetterContext Fresh => new()
    {
        Tenant = new Tenant(1, "test"),
        Tariff = new Tariff { Quotas = [], State = TariffState.Trial, DueDate = DateTime.MaxValue, DelayDueDate = DateTime.MaxValue },
        Quota = Quota(),
        RightQuota = Quota(),
        NowDate = Today,
        CreatedDate = Today,
        DueDate = DateTime.MaxValue.Date,
        DueDateIsNotMax = false,
        DelayDueDate = DateTime.MaxValue.Date,
        DelayDueDateIsNotMax = false,
        DefaultRebranding = true,
        UnusedPortalNotifyFrom = Today.AddYears(-1),
        LastActivity = Activity(Today)
    };

    private static TenantQuota Quota(bool free = false, bool trial = false, bool lifetime = false, bool customization = false)
    {
        return new TenantQuota { Free = free, Trial = trial, Lifetime = lifetime, Customization = customization };
    }

    private static Lazy<Task<DateTime>> Activity(DateTime date)
    {
        return new Lazy<Task<DateTime>>(() => Task.FromResult(date));
    }

    /// <summary>A portal on a paid tariff running out on <paramref name="due"/>.</summary>
    private static PeriodicLetterContext Paid(PeriodicLetterContext context, DateTime due, DateTime? delay = null)
    {
        return context with
        {
            Tariff = new Tariff { Quotas = [], State = TariffState.Paid, DueDate = due, DelayDueDate = delay ?? DateTime.MaxValue },
            DueDate = due.Date,
            DueDateIsNotMax = true,
            DelayDueDate = (delay ?? DateTime.MaxValue).Date,
            DelayDueDateIsNotMax = delay.HasValue
        };
    }

    /// <summary>A portal inside its grace period, which runs out on <paramref name="delay"/>.</summary>
    private static PeriodicLetterContext Delayed(PeriodicLetterContext context, DateTime delay)
    {
        return context with
        {
            Tariff = new Tariff { Quotas = [], State = TariffState.Delay, DueDate = Today.AddDays(-30), DelayDueDate = delay },
            DueDate = Today.AddDays(-30),
            DueDateIsNotMax = true,
            DelayDueDate = delay.Date,
            DelayDueDateIsNotMax = true
        };
    }

    /// <summary>A portal whose paid tariff lapsed on <paramref name="due"/> and was never renewed.</summary>
    private static PeriodicLetterContext Lapsed(PeriodicLetterContext context, DateTime due)
    {
        return context with
        {
            Tariff = new Tariff { Quotas = [], State = TariffState.NotPaid, DueDate = due, DelayDueDate = DateTime.MaxValue },
            DueDate = due.Date,
            DueDateIsNotMax = true
        };
    }

    /// <summary>
    /// A portal nobody has touched for <paramref name="months"/> months, checked on the anniversary of
    /// its creation - the only day the inactivity warnings look at.
    /// </summary>
    private static PeriodicLetterContext Idle(PeriodicLetterContext context, int months)
    {
        return context with
        {
            CreatedDate = Today.AddYears(-2),
            LastActivity = Activity(Today.AddMonths(-months))
        };
    }

    private static Task<bool> AsksToSendAsync<TAction>(PeriodicLetterContext context) where TAction : BasePeriodicNotifyAction
    {
        // No constructor: a predicate reads the context and nothing else, so there is nothing to inject.
        var action = (TAction)RuntimeHelpers.GetUninitializedObject(typeof(TAction));

        return action.ShouldSendAsync(context);
    }

    [Fact]
    public async Task HandyApps_GoesOutOnDayTwo()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminHandyAppsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-2) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminHandyAppsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-3) }))
            .Should().BeFalse("the window is the day itself, not everything after it");
    }

    [Fact]
    public async Task Configure_GoesOutOnDayThree()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminConfigureV1NotifyAction>(context with { CreatedDate = Today.AddDays(-3) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminConfigureV1NotifyAction>(context with { CreatedDate = Today.AddDays(-4) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task Addons_GoesOutOnDayFour()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminAddonsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-4) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminAddonsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-5) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task AiAgents_GoesOutOnDaySeven()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminAiAgentsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-7) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminAiAgentsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-8) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task DeveloperTools_GoesOutOnDayTen()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminDeveloperToolsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-10) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminDeveloperToolsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-11) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task UserAppsTips_GoesOutOnDayFourteen()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-14) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-15) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task StartupWarningAfterThreeMonths_NeedsAFreePortalIdleForThreeMonths()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>(Idle(context, months: 3) with { Quota = Quota(free: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>(Idle(context, months: 3)))
            .Should().BeFalse("the warning is only for free portals");
    }

    [Fact]
    public async Task StartupWarningAfterThreeMonths_StaysSilentWhileThePortalIsStillUsed()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>(Idle(context, months: 3) with { Quota = Quota(free: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>(Idle(context, months: 2) with { Quota = Quota(free: true) }))
            .Should().BeFalse("two months of quiet is not three");
    }

    [Fact]
    public async Task StartupWarningAfterHalfYear_TakesOverAtSixMonths()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(Idle(context, months: 6) with { Quota = Quota(free: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(Idle(context, months: 3) with { Quota = Quota(free: true) }))
            .Should().BeFalse("at three months the other warning speaks");
    }

    [Fact]
    public async Task GracePeriodBeforeActivation_GoesOutThreeDaysBeforeTheTariffEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(4))))
            .Should().BeFalse("four days is too early");
    }

    [Fact]
    public async Task GracePeriodBeforeActivation_IgnoresFreePortals()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(3)) with { Quota = Quota(free: true) }))
            .Should().BeFalse("a free portal has nothing to pay");
    }

    [Fact]
    public async Task GracePeriodActivation_GoesOutTheDayAfterTheTariffEnded()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: Today.AddDays(-1), delay: Today.AddDays(30))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: Today.AddDays(-1))))
            .Should().BeFalse("without a grace period there is nothing to announce");
    }

    [Fact]
    public async Task GracePeriodLastDay_GoesOutTheDayBeforeItRunsOut()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction>(Delayed(context, delay: Today.AddDays(1))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction>(Delayed(context, delay: Today.AddDays(2))))
            .Should().BeFalse("two days out is too early");
    }

    [Fact]
    public async Task GracePeriodExpired_GoesOutOnTheDayItRunsOut()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>(Delayed(context, delay: Today)))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>(Delayed(context, delay: Today.AddDays(1))))
            .Should().BeFalse("it has not run out yet");
    }

    [Fact]
    public async Task WarningAfterThreeMonths_GoesOutThreeMonthsAfterTheTariffLapsed()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminWarningAfterThreeMonthsV1NotifyAction>(Lapsed(context, due: Today.AddMonths(-3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminWarningAfterThreeMonthsV1NotifyAction>(Lapsed(context, due: Today.AddMonths(-4))))
            .Should().BeFalse("the window is that day only");
    }

    [Fact]
    public async Task WarningAfterHalfYear_GoesOutSixMonthsAfterTheTariffLapsed()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminWarningAfterHalfYearV1NotifyAction>(Lapsed(context, due: Today.AddMonths(-6))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminWarningAfterHalfYearV1NotifyAction>(Lapsed(context, due: Today.AddMonths(-3))))
            .Should().BeFalse("at three months the other warning speaks");
    }

    [Fact]
    public async Task EnterpriseUserAppsTips_NeedsATrialThatStillCarriesOurBranding()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-14), Quota = Quota(trial: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = Today.AddDays(-14), Quota = Quota(trial: true), DefaultRebranding = false }))
            .Should().BeFalse("a white-labelled portal must not advertise our apps");
    }

    [Fact]
    public async Task EnterpriseLifetimeBeforeExpiration_ClaimsTheLifetimeLicence()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction>(Paid(context, due: Today.AddDays(7)) with { Quota = Quota(lifetime: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction>(Paid(context, due: Today.AddDays(7))))
            .Should().BeFalse("an ordinary licence is the Enterprise letter");
    }

    [Fact]
    public async Task DeveloperBeforeActivation_ClaimsTheDeveloperLicence()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(7)) with { Quota = Quota(customization: true, lifetime: true) }))
            .Should().BeFalse("a lifetime licence is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseBeforeActivation_ClaimsWhatIsLeft()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(7))))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: Today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    [Fact]
    public async Task EnterpriseLifetimeExpiration_GoesOutOnTheDay()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction>(Paid(context, due: Today) with { Quota = Quota(lifetime: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction>(Paid(context, due: Today.AddDays(1)) with { Quota = Quota(lifetime: true) }))
            .Should().BeFalse("not yet");
    }

    [Fact]
    public async Task DeveloperActivation_GoesOutOnTheDay()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: Today) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: Today) with { Quota = Quota(customization: true, lifetime: true) }))
            .Should().BeFalse("a lifetime licence is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseActivation_GoesOutOnTheDay()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: Today)))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: Today) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    [Fact]
    public async Task DeveloperBeforeExpiration_GoesOutAWeekBeforeTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: Today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: Today.AddDays(7))))
            .Should().BeFalse("without customization it is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseBeforeExpiration_GoesOutAWeekBeforeTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: Today.AddDays(7))))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: Today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    [Fact]
    public async Task DeveloperExpiration_GoesOutWhenTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: Today) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: Today)))
            .Should().BeFalse("without customization it is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseExpiration_GoesOutWhenTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: Today)))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: Today) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    /// <summary>
    /// The three licence flavours share a day but not a letter. The chain used to guarantee that with a
    /// nested ternary; now each letter excludes the others itself, and nothing but this test says so.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task OnlyOneEnterpriseLetterClaimsAPortal(bool lifetime, bool customization)
    {
        var context = Paid(Fresh, Today.AddDays(7)) with { Quota = Quota(lifetime: lifetime, customization: customization) };

        var claimed = new[]
        {
            await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction>(context),
            await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(context),
            await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(context)
        };

        claimed.Count(fires => fires).Should().Be(1,
            "a portal expiring in a week gets exactly one warning, whatever its licence");
    }

    /// <summary>
    /// The two inactivity warnings divide the timeline between them: the second one takes over where the
    /// first stops, and neither covers a portal that is still in use.
    /// </summary>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 1)]
    [InlineData(6, 1)]
    [InlineData(9, 0)]
    public async Task InactivityWarningsDoNotOverlap(int idleMonths, int expected)
    {
        var context = Idle(Fresh, idleMonths) with { Quota = Quota(free: true) };

        var claimed = new[]
        {
            await AsksToSendAsync<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>(context),
            await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(context)
        };

        claimed.Count(fires => fires).Should().Be(expected);
    }

    /// <summary>
    /// A portal in its first fortnight gets the letter for that day and nothing else. The registration
    /// letters are the one group where independence is free: two of them would need the same portal to be
    /// two different ages.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task ExactlyOneRegistrationLetterPerDay(int age)
    {
        var context = Fresh with { CreatedDate = Today.AddDays(-age) };

        var claimed = new[]
        {
            await AsksToSendAsync<SaasAdminHandyAppsV1NotifyAction>(context),
            await AsksToSendAsync<SaasAdminConfigureV1NotifyAction>(context),
            await AsksToSendAsync<SaasAdminAddonsV1NotifyAction>(context),
            await AsksToSendAsync<SaasAdminAiAgentsV1NotifyAction>(context),
            await AsksToSendAsync<SaasAdminDeveloperToolsV1NotifyAction>(context),
            await AsksToSendAsync<SaasAdminUserAppsTipsV1NotifyAction>(context)
        };

        claimed.Count(fires => fires).Should().Be(1);
    }
}
