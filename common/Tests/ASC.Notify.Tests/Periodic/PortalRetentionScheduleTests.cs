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
/// What the retention policy does to an unused portal on a given day. The schedule is a pure function
/// of a few dates, so every case is a single call; the defaults are the policy's own numbers.
/// </summary>
public class PortalRetentionScheduleTests
{
    private static readonly PortalRetentionOptions _options = new();

    /// <summary>The day the count starts. The policy was switched on long before it.</summary>
    private static readonly DateTime _start = new(2026, 1, 10);

    private static readonly DateTime _policyStart = new(2025, 1, 1);

    private static PortalRetentionDecision Active(PortalRetentionCategory category, int day)
    {
        return PortalRetentionSchedule.Decide(_options.For(category), _start, _policyStart, null, _start.AddDays(day));
    }

    private static PortalRetentionDecision Blocked(PortalRetentionCategory category, int dayAfterBlock)
    {
        return PortalRetentionSchedule.Decide(_options.For(category), _start, _policyStart, _start, _start.AddDays(dayAfterBlock));
    }

    /// <summary>Every day of the active part of a schedule that is not silent, as day → letter or block.</summary>
    private static Dictionary<int, string> ActiveTimeline(PortalRetentionCategory category, int days)
    {
        var timeline = new Dictionary<int, string>();

        for (var day = 0; day <= days; day++)
        {
            var decision = Active(category, day);

            if (decision.Step != PortalRetentionStep.None)
            {
                timeline[day] = decision.Step == PortalRetentionStep.Notify ? $"{decision.Letter}" : decision.Step.ToString();
            }

            if (decision.Step == PortalRetentionStep.Block)
            {
                break;
            }
        }

        return timeline;
    }

    /// <summary>Every day of the blocked part of a schedule that is not silent.</summary>
    private static Dictionary<int, string> BlockedTimeline(PortalRetentionCategory category)
    {
        var timeline = new Dictionary<int, string>();

        for (var day = 0; day <= 400; day++)
        {
            var decision = Blocked(category, day);

            if (decision.Step != PortalRetentionStep.None)
            {
                timeline[day] = decision.Step == PortalRetentionStep.Notify ? $"{decision.Letter}" : decision.Step.ToString();
            }

            if (decision.Step == PortalRetentionStep.Delete)
            {
                break;
            }
        }

        return timeline;
    }

    [Fact]
    public void Free_WarnedAtThirtyDays_BlockedAtSixty()
    {
        ActiveTimeline(PortalRetentionCategory.Free, 400).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [30] = nameof(PortalRetentionLetter.FirstNotice),
            [60] = nameof(PortalRetentionStep.Block)
        });
    }

    [Fact]
    public void Free_RemindedAWeekBefore_DeletedThirtyDaysAfterTheBlock()
    {
        BlockedTimeline(PortalRetentionCategory.Free).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [23] = nameof(PortalRetentionLetter.FinalDeletionNotice),
            [30] = nameof(PortalRetentionStep.Delete)
        });
    }

    [Fact]
    public void FormerPaying_WarnedTwice_BlockedAtNinety()
    {
        ActiveTimeline(PortalRetentionCategory.FormerPaying, 400).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [30] = nameof(PortalRetentionLetter.FirstNotice),
            [60] = nameof(PortalRetentionLetter.SecondNotice),
            [90] = nameof(PortalRetentionStep.Block)
        });
    }

    [Fact]
    public void FormerPaying_RemindedAMonthAndAWeekBefore_DeletedNinetyDaysAfterTheBlock()
    {
        BlockedTimeline(PortalRetentionCategory.FormerPaying).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [60] = nameof(PortalRetentionLetter.EarlyDeletionNotice),
            [83] = nameof(PortalRetentionLetter.FinalDeletionNotice),
            [90] = nameof(PortalRetentionStep.Delete)
        });
    }

    [Theory]
    [InlineData(PortalRetentionCategory.FreeWithBalance)]
    [InlineData(PortalRetentionCategory.FormerPayingWithBalance)]
    public void WithBalance_RemindedMonthly_BlockedAfterAYear(PortalRetentionCategory category)
    {
        var expected = new Dictionary<int, string> { [30] = nameof(PortalRetentionLetter.FirstNotice) };

        for (var month = 6; month <= 11; month++)
        {
            expected[(_start.AddMonths(month) - _start).Days] = nameof(PortalRetentionLetter.MonthlyNotice);
        }

        expected[365] = nameof(PortalRetentionStep.Block);

        ActiveTimeline(category, 400).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(PortalRetentionCategory.FreeWithBalance)]
    [InlineData(PortalRetentionCategory.FormerPayingWithBalance)]
    public void WithBalance_DeletedNinetyDaysAfterTheBlock(PortalRetentionCategory category)
    {
        BlockedTimeline(category).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [60] = nameof(PortalRetentionLetter.EarlyDeletionNotice),
            [83] = nameof(PortalRetentionLetter.FinalDeletionNotice),
            [90] = nameof(PortalRetentionStep.Delete)
        });
    }

    [Fact]
    public void Notices_DiscloseTheDaysOfTheBlockAndTheDeletion()
    {
        var notice = Active(PortalRetentionCategory.FormerPaying, 30);

        notice.BlockOn.Should().Be(_start.AddDays(90));
        notice.DeleteOn.Should().Be(_start.AddDays(180));

        var reminder = Blocked(PortalRetentionCategory.FormerPaying, 60);

        reminder.BlockOn.Should().Be(_start);
        reminder.DeleteOn.Should().Be(_start.AddDays(90));
    }

    [Fact]
    public void Block_CatchesUpAfterAMissedDay_AndCountsTheRetentionFromThatDay()
    {
        var decision = Active(PortalRetentionCategory.Free, 75);

        decision.Step.Should().Be(PortalRetentionStep.Block, "a portal past its threshold is blocked on the first run that sees it");
        decision.Letter.Should().Be(PortalRetentionLetter.Blocked);
        decision.BlockOn.Should().Be(_start.AddDays(75));
        decision.DeleteOn.Should().Be(_start.AddDays(105), "the owner is promised the whole retention period from the letter on");
    }

    [Fact]
    public void Delete_CatchesUpAfterAMissedDay()
    {
        Blocked(PortalRetentionCategory.Free, 31).Step.Should().Be(PortalRetentionStep.Delete);
    }

    [Fact]
    public void Reminders_AreSentOnTheirDayOnly()
    {
        Active(PortalRetentionCategory.Free, 31).Step.Should().Be(PortalRetentionStep.None, "a missed reminder is not sent late");
        Blocked(PortalRetentionCategory.Free, 24).Step.Should().Be(PortalRetentionStep.None);
    }

    [Fact]
    public void CategoryChange_StillLeavesTheWholeRetentionAfterTheBlockLetter()
    {
        // A portal counted on the wallet schedule loses its balance on day 200: on the free schedule it
        // is long past its block, so it is blocked now - and the deletion is counted from now.
        var decision = Active(PortalRetentionCategory.Free, 200);

        decision.Step.Should().Be(PortalRetentionStep.Block);
        decision.DeleteOn.Should().Be(_start.AddDays(230));

        Blocked(PortalRetentionCategory.Free, 29).Step.Should().NotBe(PortalRetentionStep.Delete);
    }

    [Fact]
    public void PolicyStart_CountsFromTheDayThePolicyWasSwitchedOn()
    {
        var policyStart = new DateTime(2026, 9, 1);
        var schedule = _options.For(PortalRetentionCategory.Free);

        // Idle for years when the policy is switched on: nothing happens on the first night ...
        PortalRetentionSchedule.Decide(schedule, new DateTime(2023, 1, 1), policyStart, null, policyStart)
            .Step.Should().Be(PortalRetentionStep.None);

        // ... it gets the whole chain counted from the switch ...
        PortalRetentionSchedule.Decide(schedule, new DateTime(2023, 1, 1), policyStart, null, policyStart.AddDays(30))
            .Letter.Should().Be(PortalRetentionLetter.FirstNotice);

        PortalRetentionSchedule.Decide(schedule, new DateTime(2023, 1, 1), policyStart, null, policyStart.AddDays(60))
            .Step.Should().Be(PortalRetentionStep.Block);

        // ... while a portal active after the switch is counted from its own activity.
        PortalRetentionSchedule.Decide(schedule, policyStart.AddDays(10), policyStart, null, policyStart.AddDays(60))
            .Step.Should().Be(PortalRetentionStep.None);
    }

    [Fact]
    public void Activity_RestartsTheCount()
    {
        var schedule = _options.For(PortalRetentionCategory.Free);
        var today = _start.AddDays(59);

        PortalRetentionSchedule.Decide(schedule, today.AddDays(-1), _policyStart, null, today)
            .Step.Should().Be(PortalRetentionStep.None, "yesterday's activity puts the block two months away again");
    }

    [Fact]
    public void Options_ReadFromConfiguration_KeepTheDefaultsTheyDoNotName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:retention:enabled"] = "true",
                ["core:retention:free:blockAfterDays"] = "45"
            })
            .Build();

        var options = new PortalRetentionConfiguration(configuration).Options;

        options.Enabled.Should().BeTrue();
        options.DryRun.Should().BeFalse();
        options.Free.BlockAfterDays.Should().Be(45);
        options.Free.FirstNoticeDays.Should().Be(30, "a threshold the section does not name keeps its default");
        options.FormerPaying.RetentionDays.Should().Be(90);
    }

    [Fact]
    public void Options_WithoutTheSection_AreSwitchedOff()
    {
        var options = new PortalRetentionConfiguration(new ConfigurationBuilder().Build()).Options;

        options.Enabled.Should().BeFalse();
        options.WithBalance.BlockAfterDays.Should().Be(365);
    }
}
