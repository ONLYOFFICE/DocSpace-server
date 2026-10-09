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
/// When the count reaches the block before a warning of it went out - the warning was lost in a long
/// stop of the job, or the wallet emptied and the portal fell under a shorter schedule - and when the
/// block keeps to the day a warning named rather than to the schedule's own day.
/// </summary>
public class PortalRetentionBlockWarningTests
{
    private static readonly PortalRetentionOptions _options = new();

    /// <summary>The day the count starts. The policy first ran long before it.</summary>
    private static readonly DateTime _start = new(2026, 1, 10);

    private static readonly DateTime _policyStart = new(2025, 1, 1);

    private static PortalRetentionDecision Active(PortalRetentionCategory category, int day, PortalRetentionWarning? lastWarning = null)
    {
        return PortalRetentionSchedule.DecideActive(_options.For(category), _start, _policyStart, _start.AddDays(day), lastWarning: lastWarning);
    }

    private static PortalRetentionWarning Warning(int sentDay, int blockDay)
    {
        return new PortalRetentionWarning(_start.AddDays(sentDay), _start.AddDays(blockDay));
    }

    [Fact]
    public void Block_WithoutAWarning_IsWarnedFirst_AndBlockedOnTheDayTheWarningNamed()
    {
        var warning = Active(PortalRetentionCategory.Free, 75);

        warning.Step.Should().Be(PortalRetentionStep.Notify, "a portal is never blocked without a letter naming the day");
        warning.Letter.Should().Be(PortalRetentionLetter.FirstNotice);
        warning.BlockOn.Should().Be(_start.AddDays(105), "the free schedule warns thirty days before its block");
        warning.DeleteOn.Should().Be(_start.AddDays(135));

        Active(PortalRetentionCategory.Free, 104, Warning(75, 105)).Step.Should().Be(PortalRetentionStep.None);

        var block = Active(PortalRetentionCategory.Free, 105, Warning(75, 105));

        block.Step.Should().Be(PortalRetentionStep.Block);
        block.DeleteOn.Should().Be(_start.AddDays(135), "the deletion the warning named");
    }

    [Theory]
    [InlineData(PortalRetentionCategory.Free, 75, PortalRetentionLetter.FirstNotice, 105)]
    [InlineData(PortalRetentionCategory.FormerPaying, 100, PortalRetentionLetter.SecondNotice, 130)]
    [InlineData(PortalRetentionCategory.FreeWithBalance, 400, PortalRetentionLetter.MonthlyNotice, 431)]
    [InlineData(PortalRetentionCategory.FormerPayingWithBalance, 400, PortalRetentionLetter.MonthlyNotice, 431)]
    public void Block_WithoutAWarning_IsAsFarAwayAsTheScheduleKeepsItsLastWarning(PortalRetentionCategory category, int day, PortalRetentionLetter letter, int blockDay)
    {
        // The last warnings before the block: day 30 of 60, day 60 of 90, and the eleventh month - 10 December,
        // day 334 - of 365, thirty-one days before it.
        var decision = Active(category, day);

        decision.Step.Should().Be(PortalRetentionStep.Notify);
        decision.Letter.Should().Be(letter);
        decision.BlockOn.Should().Be(_start.AddDays(blockDay));
    }

    [Fact]
    public void Block_WarningOfAnEarlierCount_DoesNotCount()
    {
        Active(PortalRetentionCategory.Free, 75, Warning(-20, 10)).Step
            .Should().Be(PortalRetentionStep.Notify, "the portal was used after that warning, so it named the block of a count that is over");

        Active(PortalRetentionCategory.Free, 75, Warning(0, 30)).Step
            .Should().Be(PortalRetentionStep.Notify, "a warning sent on the day the count starts is the earlier count's");
    }

    [Fact]
    public void WalletEmptied_TheBlockKeepsTheDayTheWalletWarningNamed()
    {
        // Warned on the sixth month that the wallet schedule blocks on day 365; the wallet empties, and the
        // free schedule's own day 60 has long passed.
        var walletWarning = Warning(181, 365);

        var emptied = Active(PortalRetentionCategory.Free, 200, walletWarning);

        emptied.Step.Should().Be(PortalRetentionStep.None, "the owner was told day 365, so the portal is not blocked before it");
        emptied.BlockOn.Should().Be(_start.AddDays(365));
        emptied.DeleteOn.Should().Be(_start.AddDays(395), "counted on the schedule the portal is under now");

        Active(PortalRetentionCategory.Free, 364, walletWarning).Step.Should().Be(PortalRetentionStep.None);
        Active(PortalRetentionCategory.Free, 365, walletWarning).Step.Should().Be(PortalRetentionStep.Block);
    }

    [Fact]
    public void WalletFilled_TheWalletScheduleApplies_ThoughAnEarlierDayWasNamed()
    {
        // Warned on day 30 of a block on day 60, then money came in: blocking later than announced is no
        // surprise to anyone.
        var decision = Active(PortalRetentionCategory.FreeWithBalance, 60, Warning(30, 60));

        decision.Step.Should().Be(PortalRetentionStep.None);
        decision.BlockOn.Should().Be(_start.AddDays(365));
    }

    [Fact]
    public void Block_ScheduleWithoutWarnings_BlocksUnannounced()
    {
        var schedule = new PortalRetentionScheduleOptions { BlockAfterDays = 60, RetentionDays = 30, FinalDeletionNoticeDays = 7 };

        PortalRetentionSchedule.DecideActive(schedule, _start, _policyStart, _start.AddDays(60))
            .Step.Should().Be(PortalRetentionStep.Block, "a configuration with every warning switched off asks for no warning");
    }
}
