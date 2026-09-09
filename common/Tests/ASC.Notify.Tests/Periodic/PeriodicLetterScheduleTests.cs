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
/// When each periodic letter goes out. Every case names the day the letter is due and one day it must
/// stay silent on, because a predicate that is merely too generous still passes a "does it fire" check.
///
/// These conditions used to be an <c>else if</c> chain inside <c>StudioPeriodicNotify</c>, where nothing
/// could reach them: the chain needed a tenant, a tariff service and a database. Now each letter answers
/// from a <see cref="PeriodicLetterContext"/> alone, so the schedule is testable without any of that -
/// the action is built without a constructor at all, since a predicate reads nothing but the context.
/// That is what separates these tests from the letter ones: rendering what a letter *says* needs the
/// real action out of the container, but asking *when* it goes out needs nothing.
///
/// The letters judge themselves independently, so the mutual exclusion the chain used to give for free
/// now has to be written down. <see cref="OnlyOneEnterpriseLetterClaimsAPortal"/> is where that is checked.
/// </summary>
public class PeriodicLetterScheduleTests
{
    /// <summary>An unremarkable Tuesday. Nothing about the cases depends on which day it is.</summary>
    private static readonly DateTime _today = new(2026, 6, 16);

    // The portal states themselves live in PeriodicLetterContexts, because the letter tests render
    // against the same ones: a lapsed tariff described in two places is a lapsed tariff that can be
    // described differently in two places. What stays here is only the shorthand these cases read in.

    /// <summary>
    /// A portal nothing is due for: created today, on a paid-for-nothing trial with no dates set. Each
    /// case moves only what its own letter looks at.
    /// </summary>
    private static PeriodicLetterContext Fresh => PeriodicLetterContexts.Fresh(new Tenant(1, "test"), _today);

    private static TenantQuota Quota(bool free = false, bool trial = false, bool lifetime = false, bool customization = false)
    {
        return PeriodicLetterContexts.Quota(free, trial, lifetime, customization);
    }

    private static Lazy<Task<DateTime>> Activity(DateTime date)
    {
        return PeriodicLetterContexts.Activity(date);
    }

    /// <summary>A portal on a paid tariff running out on <paramref name="due"/>.</summary>
    private static PeriodicLetterContext Paid(PeriodicLetterContext context, DateTime due, DateTime? delay = null)
    {
        return PeriodicLetterContexts.Paid(context, due, delay);
    }

    /// <summary>A portal inside its grace period, which runs out on <paramref name="delay"/>.</summary>
    private static PeriodicLetterContext Delayed(PeriodicLetterContext context, DateTime delay)
    {
        return PeriodicLetterContexts.Delayed(context, delay);
    }

    /// <summary>A portal whose paid tariff lapsed on <paramref name="due"/> and was never renewed.</summary>
    private static PeriodicLetterContext Lapsed(PeriodicLetterContext context, DateTime due)
    {
        return PeriodicLetterContexts.Lapsed(context, due);
    }

    /// <summary>A portal on a trial running out on <paramref name="due"/>.</summary>
    private static PeriodicLetterContext Trial(PeriodicLetterContext context, DateTime due)
    {
        return PeriodicLetterContexts.Trial(context, due);
    }

    /// <summary>
    /// A portal nobody has touched for <paramref name="months"/> months, checked on the anniversary of
    /// its creation - the only day the inactivity warnings look at.
    /// </summary>
    private static PeriodicLetterContext Idle(PeriodicLetterContext context, int months)
    {
        return PeriodicLetterContexts.Idle(context, months);
    }

    /// <summary>
    /// A free portal created on <paramref name="created"/> and untouched since <paramref name="lastActivity"/>,
    /// looked at on <paramref name="now"/>. Unlike <see cref="Idle"/> it does not move the creation date
    /// to a day that is bound to be an anniversary, which is the whole point of the cases that use it.
    /// </summary>
    private static PeriodicLetterContext FreeIdleSince(string created, string now, DateTime lastActivity)
    {
        return PeriodicLetterContexts.Fresh(new Tenant(1, "test"), Date(now)) with
        {
            Quota = Quota(free: true),
            CreatedDate = Date(created),
            LastActivity = Activity(lastActivity)
        };
    }

    /// <summary>Inline data cannot carry a <see cref="DateTime"/>, so the cases spell their dates out.</summary>
    private static DateTime Date(string value)
    {
        return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
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

        (await AsksToSendAsync<SaasAdminHandyAppsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-2) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminHandyAppsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-3) }))
            .Should().BeFalse("the window is the day itself, not everything after it");
    }

    [Fact]
    public async Task Configure_GoesOutOnDayThree()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminConfigureV1NotifyAction>(context with { CreatedDate = _today.AddDays(-3) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminConfigureV1NotifyAction>(context with { CreatedDate = _today.AddDays(-4) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task Addons_GoesOutOnDayFour()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminAddonsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-4) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminAddonsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-5) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task AiAgents_GoesOutOnDaySeven()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminAiAgentsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-7) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminAiAgentsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-8) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task DeveloperTools_GoesOutOnDayTen()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminDeveloperToolsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-10) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminDeveloperToolsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-11) }))
            .Should().BeFalse("a day late");
    }

    [Fact]
    public async Task UserAppsTips_GoesOutOnDayFourteen()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-14) }))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-15) }))
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

        // Five, not three: three is where the other warning speaks, and a window that opened a month
        // early would still be silent there. The month below the threshold is the one that pins it.
        (await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(Idle(context, months: 5) with { Quota = Quota(free: true) }))
            .Should().BeFalse("five months of quiet is not six");
    }

    [Fact]
    public async Task GracePeriodBeforeActivation_GoesOutThreeDaysBeforeTheTariffEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(4))))
            .Should().BeFalse("four days is too early");
    }

    [Fact]
    public async Task GracePeriodBeforeActivation_IgnoresFreePortals()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(3)) with { Quota = Quota(free: true) }))
            .Should().BeFalse("a free portal has nothing to pay");
    }

    [Fact]
    public async Task GracePeriodActivation_GoesOutTheDayAfterTheTariffEnded()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: _today.AddDays(-1), delay: _today.AddDays(30))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: _today.AddDays(-1))))
            .Should().BeFalse("without a grace period there is nothing to announce");
    }

    [Fact]
    public async Task GracePeriodLastDay_GoesOutTheDayBeforeItRunsOut()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction>(Delayed(context, delay: _today.AddDays(1))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction>(Delayed(context, delay: _today.AddDays(2))))
            .Should().BeFalse("two days out is too early");
    }

    [Fact]
    public async Task GracePeriodExpired_GoesOutOnTheDayItRunsOut()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>(Delayed(context, delay: _today)))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>(Delayed(context, delay: _today.AddDays(1))))
            .Should().BeFalse("it has not run out yet");
    }

    [Fact]
    public async Task WarningAfterThreeMonths_GoesOutThreeMonthsAfterTheTariffLapsed()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminWarningAfterThreeMonthsV1NotifyAction>(Lapsed(context, due: _today.AddMonths(-3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminWarningAfterThreeMonthsV1NotifyAction>(Lapsed(context, due: _today.AddMonths(-4))))
            .Should().BeFalse("the window is that day only");
    }

    [Fact]
    public async Task WarningAfterHalfYear_GoesOutSixMonthsAfterTheTariffLapsed()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasAdminWarningAfterHalfYearV1NotifyAction>(Lapsed(context, due: _today.AddMonths(-6))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasAdminWarningAfterHalfYearV1NotifyAction>(Lapsed(context, due: _today.AddMonths(-5))))
            .Should().BeFalse("the window is that day only, and five months is not six");
    }

    [Fact]
    public async Task EnterpriseUserAppsTips_NeedsATrialThatStillCarriesOurBranding()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-14), Quota = Quota(trial: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminUserAppsTipsV1NotifyAction>(context with { CreatedDate = _today.AddDays(-14), Quota = Quota(trial: true), DefaultRebranding = false }))
            .Should().BeFalse("a white-labelled portal must not advertise our apps");
    }

    [Fact]
    public async Task EnterpriseLifetimeBeforeExpiration_ClaimsTheLifetimeLicence()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction>(Paid(context, due: _today.AddDays(7)) with { Quota = Quota(lifetime: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction>(Paid(context, due: _today.AddDays(7))))
            .Should().BeFalse("an ordinary licence is the Enterprise letter");
    }

    [Fact]
    public async Task DeveloperBeforeActivation_ClaimsTheDeveloperLicence()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(7)) with { Quota = Quota(customization: true, lifetime: true) }))
            .Should().BeFalse("a lifetime licence is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseBeforeActivation_ClaimsWhatIsLeft()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(7))))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>(Paid(context, due: _today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    [Fact]
    public async Task EnterpriseLifetimeExpiration_GoesOutOnTheDay()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction>(Paid(context, due: _today) with { Quota = Quota(lifetime: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction>(Paid(context, due: _today.AddDays(1)) with { Quota = Quota(lifetime: true) }))
            .Should().BeFalse("not yet");
    }

    [Fact]
    public async Task DeveloperActivation_GoesOutOnTheDay()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: _today) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: _today) with { Quota = Quota(customization: true, lifetime: true) }))
            .Should().BeFalse("a lifetime licence is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseActivation_GoesOutOnTheDay()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: _today)))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction>(Paid(context, due: _today) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    [Fact]
    public async Task DeveloperBeforeExpiration_GoesOutAWeekBeforeTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: _today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: _today.AddDays(7))))
            .Should().BeFalse("without customization it is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseBeforeExpiration_GoesOutAWeekBeforeTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: _today.AddDays(7))))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>(Delayed(context, delay: _today.AddDays(7)) with { Quota = Quota(customization: true) }))
            .Should().BeFalse("a Developer licence has its own letter");
    }

    [Fact]
    public async Task DeveloperExpiration_GoesOutWhenTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: _today) with { Quota = Quota(customization: true) }))
            .Should().BeTrue();

        (await AsksToSendAsync<DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: _today)))
            .Should().BeFalse("without customization it is the Enterprise letter");
    }

    [Fact]
    public async Task EnterpriseExpiration_GoesOutWhenTheGracePeriodEnds()
    {
        var context = Fresh;

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: _today)))
            .Should().BeTrue();

        (await AsksToSendAsync<EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction>(Delayed(context, delay: _today) with { Quota = Quota(customization: true) }))
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
        var context = Paid(Fresh, _today.AddDays(7)) with { Quota = Quota(lifetime: lifetime, customization: customization) };

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
        var context = Fresh with { CreatedDate = _today.AddDays(-age) };

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

    /// <summary>
    /// Which letters honour an unsubscribe. <c>RequiresSubscription</c> defaults to false — the letter
    /// goes out whatever the recipient switched off — which is right for a notice about their money or
    /// their portal being deleted, and wrong for every letter that is advertising something. Nothing but
    /// this test says which is which: a marketing letter that forgets to override it compiles, renders
    /// and sends, and only the reader who unsubscribed finds out.
    /// </summary>
    [Fact]
    public void OnlyTheMarketingLettersHonourTheUnsubscribe()
    {
        var honouring = typeof(BasePeriodicNotifyAction).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(BasePeriodicNotifyAction)) && !type.IsAbstract)
            .Where(RequiresSubscription)
            .Select(type => type.Name)
            .Order()
            .ToArray();

        honouring.Should().BeEquivalentTo(
        [
            nameof(EnterpriseAdminUserAppsTipsV1NotifyAction),
            nameof(SaasAdminAddonsV1NotifyAction),
            nameof(SaasAdminAiAgentsV1NotifyAction),
            nameof(SaasAdminConfigureV1NotifyAction),
            nameof(SaasAdminDeveloperToolsV1NotifyAction),
            nameof(SaasAdminHandyAppsV1NotifyAction),
            nameof(SaasAdminUserAppsTipsV1NotifyAction)
        ],
        "these are the letters that advertise something; the rest are about a payment falling due or a "
        + "portal being deleted, which a reader does not get to switch off");
    }

    /// <summary>Reads the letter's own answer, which is protected because only its base ever asks.</summary>
    private static bool RequiresSubscription(Type letter)
    {
        var property = letter.GetProperty("RequiresSubscription", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"{letter.Name} has no RequiresSubscription: the base class was renamed and this test went blind.");

        return (bool)property.GetValue(RuntimeHelpers.GetUninitializedObject(letter))!;
    }

    /// <summary>
    /// The inactivity warnings are asked once a month, on the day of the month the portal was created.
    /// A portal created on the 29th-31st has no such day in February and in the 30-day months, so the
    /// anniversary is clamped to the last day the month has. Without that clamp the warning for that
    /// month is not delayed - it is never sent at all, because the window it belongs to has passed by
    /// the time the next anniversary comes round.
    /// </summary>
    [Theory]
    [InlineData("2026-01-31", "2026-02-28", true, "February has no 31st, so the last day of it is the anniversary")]
    [InlineData("2026-01-31", "2026-04-30", true, "April has no 31st either")]
    [InlineData("2026-01-31", "2026-03-31", true, "March has a 31st of its own and needs no clamping")]
    [InlineData("2026-01-30", "2026-02-28", true, "the 30th clamps in February too")]
    [InlineData("2026-01-31", "2026-04-29", false, "the day before the clamped anniversary is not it")]
    [InlineData("2026-01-31", "2026-03-30", false, "March has a 31st, so the 30th is an ordinary day")]
    public async Task InactivityWarning_FindsTheAnniversaryInAMonthTooShortForIt(
        string created, string now, bool expected, string because)
    {
        // Idle for exactly six months on the day it is looked at, which is this warning's window.
        var context = FreeIdleSince(created, now, Date(now).AddMonths(-6));

        (await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(context))
            .Should().Be(expected, because);
    }

    /// <summary>
    /// The installation stamps the day it started counting towards deleting unused portals, and the
    /// warnings say nothing before it. Otherwise an upgrade mails every portal that has been idle since
    /// long before anybody was watching - and deletes them a week later.
    /// </summary>
    [Fact]
    public async Task InactivityWarnings_StaySilentUntilTheInstallationStartedCounting()
    {
        var idle = Idle(Fresh, months: 6) with { Quota = Quota(free: true) };

        (await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(idle))
            .Should().BeTrue();

        var counting = idle with { UnusedPortalNotifyFrom = _today.AddDays(1) };

        (await AsksToSendAsync<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>(counting))
            .Should().BeFalse("the installation only starts counting tomorrow");

        (await AsksToSendAsync<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>(
            Idle(Fresh, months: 3) with { Quota = Quota(free: true), UnusedPortalNotifyFrom = _today.AddDays(1) }))
            .Should().BeFalse("and the other warning carries the same guard, separately written");
    }

    /// <summary>
    /// A trial is not a subscription that lapses: the SaaS payment warnings are guarded on
    /// <c>State &gt;= TariffState.Paid</c>, and a trial sits below it. The dates are identical to a paid
    /// tariff's, so nothing but the state keeps these letters away from a portal that owes nothing.
    /// </summary>
    [Fact]
    public async Task SaasPaymentWarnings_IgnoreATrialThatWasNeverPaidFor()
    {
        var context = Fresh;

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(
            Paid(context, due: _today.AddDays(3))))
            .Should().BeTrue();

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>(
            Trial(context, due: _today.AddDays(3))))
            .Should().BeFalse("a trial running out is not a payment falling due");

        (await AsksToSendAsync<SaasOwnerPaymentWarningGracePeriodActivationNotifyAction>(
            Trial(context, due: _today.AddDays(-1)) with { DelayDueDate = _today.AddDays(30), DelayDueDateIsNotMax = true })
            ).Should().BeFalse("nor does a trial open a grace period to announce");
    }

    /// <summary>
    /// When a portal has run out of chances. This is the one predicate here that does not send a letter
    /// but deletes the portal, which is why it is worth asking directly rather than only through the
    /// deletion it triggers.
    /// </summary>
    [Fact]
    public async Task AbandonedPortal_FreeOneIsRemovedAWeekAfterTheLastWarning()
    {
        // 2026-06-09 is a week before _today, so that day is the anniversary the last warning went out on.
        var context = FreeIdleSince("2024-06-09", "2026-06-16", Date("2025-12-09"));

        (await context.GetAbandonedReasonAsync()).Should().Be(AbandonedPortalReason.Inactive,
            "six months and a week of silence, checked a week after the warning");

        (await (context with { LastActivity = Activity(Date("2025-12-10")) }).GetAbandonedReasonAsync())
            .Should().BeNull("a day short of six months and a week is not yet");
    }

    [Fact]
    public async Task AbandonedPortal_FreeOneIsOnlyLookedAtAWeekAfterAnAnniversary()
    {
        var context = FreeIdleSince("2024-06-09", "2026-06-16", Date("2025-12-09"));

        (await (context with { CreatedDate = Date("2024-06-10") }).GetAbandonedReasonAsync())
            .Should().BeNull("a week ago was not this portal's anniversary, so nothing was warned then");
    }

    [Fact]
    public async Task AbandonedPortal_FreeOneWaitsForTheInstallationToStartCounting()
    {
        var context = FreeIdleSince("2024-06-09", "2026-06-16", Date("2025-12-09"));

        (await (context with { UnusedPortalNotifyFrom = Date("2026-06-10") }).GetAbandonedReasonAsync())
            .Should().BeNull("the warning week has not passed since the installation started counting");

        (await (context with { UnusedPortalNotifyFrom = Date("2026-06-09") }).GetAbandonedReasonAsync())
            .Should().Be(AbandonedPortalReason.Inactive, "a week to the day is a week");
    }

    [Fact]
    public async Task AbandonedPortal_PaidOneIsRemovedSixMonthsAndAWeekAfterTheTariffLapsed()
    {
        var lapsed = Lapsed(Fresh, due: _today.AddMonths(-6).AddDays(-7));

        (await lapsed.GetAbandonedReasonAsync()).Should().Be(AbandonedPortalReason.Unpaid);

        (await Lapsed(Fresh, due: _today.AddMonths(-6).AddDays(-6)).GetAbandonedReasonAsync())
            .Should().BeNull("a day short of the six months and a week");

        // No anniversary anywhere in it: a lapsed tariff is counted from its own due date, and _today is
        // not the anniversary of this portal's creation.
        (await (lapsed with { CreatedDate = _today.AddDays(3) }).GetAbandonedReasonAsync())
            .Should().Be(AbandonedPortalReason.Unpaid);
    }

    [Fact]
    public async Task AbandonedPortal_PaidOneStillOnItsTariffIsLeftAlone()
    {
        (await Paid(Fresh, due: _today.AddYears(1)).GetAbandonedReasonAsync()).Should().BeNull();

        (await Delayed(Fresh, delay: _today.AddDays(3)).GetAbandonedReasonAsync())
            .Should().BeNull("a grace period is not a lapsed tariff");

        (await (Lapsed(Fresh, due: _today.AddMonths(-12)) with { DueDateIsNotMax = false }).GetAbandonedReasonAsync())
            .Should().BeNull("with no due date there is nothing to count six months from");
    }
}
