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
/// What the retention policy does to an unused portal on a given day. The schedule is a pure function
/// of a few dates, so every case is a single call; the defaults are the policy's own numbers.
/// </summary>
public class PortalRetentionScheduleTests
{
    private static readonly PortalRetentionOptions _options = new();

    /// <summary>The day the count starts. The policy first ran long before it.</summary>
    private static readonly DateTime _start = new(2026, 1, 10);

    private static readonly DateTime _policyStart = new(2025, 1, 1);

    /// <summary>
    /// A day of the active part. Unless <paramref name="lastRunDay"/> says otherwise, the job ran yesterday;
    /// <paramref name="lastWarning"/> is the last warning the portal was sent, if any.
    /// </summary>
    private static PortalRetentionDecision Active(PortalRetentionCategory category, int day, int noticesFromDay = 0, int? lastRunDay = null, PortalRetentionWarning? lastWarning = null)
    {
        return PortalRetentionSchedule.DecideActive(_options.For(category), _start, _policyStart, _start.AddDays(day), _start.AddDays(noticesFromDay),
            lastRunOn: DayOrNull(lastRunDay), lastWarning: lastWarning);
    }

    /// <summary>A day of the blocked part, with the day the last reminder went out if it has.</summary>
    private static PortalRetentionDecision Blocked(PortalRetentionCategory category, int dayAfterBlock, int? finalNoticeDay = null, int? lastRunDay = null)
    {
        return PortalRetentionSchedule.DecideBlocked(_options.For(category), _start, _start.AddDays(dayAfterBlock),
            lastRunOn: DayOrNull(lastRunDay), finalNoticeSentOn: DayOrNull(finalNoticeDay));
    }

    private static DateTime? DayOrNull(int? day)
    {
        return day.HasValue ? _start.AddDays(day.Value) : null;
    }

    /// <summary>
    /// Every day of the active part of a schedule that is not silent, as day → letter or block, run day by
    /// day the way the job keeps the last warning it sent.
    /// </summary>
    private static Dictionary<int, string> ActiveTimeline(PortalRetentionCategory category, int days, int noticesFromDay = 0)
    {
        var timeline = new Dictionary<int, string>();
        PortalRetentionWarning? lastWarning = null;

        for (var day = 0; day <= days; day++)
        {
            var decision = Active(category, day, noticesFromDay, lastWarning: lastWarning);

            if (decision.Step != PortalRetentionStep.None)
            {
                timeline[day] = decision.Step == PortalRetentionStep.Notify ? $"{decision.Letter}" : decision.Step.ToString();
            }

            if (decision.Step == PortalRetentionStep.Notify)
            {
                lastWarning = new PortalRetentionWarning(_start.AddDays(day), decision.BlockOn);
            }

            if (decision.Step == PortalRetentionStep.Block)
            {
                break;
            }
        }

        return timeline;
    }

    /// <summary>
    /// Every day of the blocked part of a schedule that is not silent, run day by day the way the job keeps
    /// the day its last reminder went out.
    /// </summary>
    private static Dictionary<int, string> BlockedTimeline(PortalRetentionCategory category)
    {
        var timeline = new Dictionary<int, string>();
        int? finalNoticeDay = null;

        for (var day = 0; day <= 400; day++)
        {
            var decision = Blocked(category, day, finalNoticeDay);

            if (decision.Step != PortalRetentionStep.None)
            {
                timeline[day] = decision.Step == PortalRetentionStep.Notify ? $"{decision.Letter}" : decision.Step.ToString();
            }

            if (decision.Letter == PortalRetentionLetter.FinalDeletionNotice)
            {
                finalNoticeDay = day;
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
    public void FormerPaying_GracePeriodOfAMonth_FirstWarningOnTheFirstUnpaidDay()
    {
        // A thirty-day grace period: the tariff is unpaid, and the portal the policy's, from day 31.
        ActiveTimeline(PortalRetentionCategory.FormerPaying, 400, noticesFromDay: 31).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [31] = nameof(PortalRetentionLetter.FirstNotice),
            [60] = nameof(PortalRetentionLetter.SecondNotice),
            [90] = nameof(PortalRetentionStep.Block)
        }, "a warning due in the grace period comes the day it ends, and nothing else moves");
    }

    [Fact]
    public void FormerPaying_GracePeriodPastBothWarnings_OnlyTheLaterOneIsSent()
    {
        ActiveTimeline(PortalRetentionCategory.FormerPaying, 400, noticesFromDay: 71).Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [71] = nameof(PortalRetentionLetter.SecondNotice),
            [90] = nameof(PortalRetentionStep.Block)
        }, "two warnings on one day would say the same twice");
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
        var decision = Active(PortalRetentionCategory.Free, 75, lastWarning: new PortalRetentionWarning(_start.AddDays(30), _start.AddDays(60)));

        decision.Step.Should().Be(PortalRetentionStep.Block, "a portal past its threshold is blocked on the first run that sees it");
        decision.Letter.Should().Be(PortalRetentionLetter.Blocked);
        decision.BlockOn.Should().Be(_start.AddDays(75));
        decision.DeleteOn.Should().Be(_start.AddDays(105), "the owner is promised the whole retention period from the letter on");
    }

    [Fact]
    public void Delete_CatchesUpAfterAMissedDay()
    {
        Blocked(PortalRetentionCategory.Free, 31, finalNoticeDay: 23).Step.Should().Be(PortalRetentionStep.Delete);
    }

    [Fact]
    public void Warning_MissedOnItsDay_IsSentByTheNextRun()
    {
        Active(PortalRetentionCategory.Free, 32, lastRunDay: 28).Letter.Should().Be(PortalRetentionLetter.FirstNotice, "the job did not run on day 30");
        Active(PortalRetentionCategory.Free, 31).Step.Should().Be(PortalRetentionStep.None, "a run that got through day 30 has sent it");
    }

    [Fact]
    public void Warning_RunTwiceOnOneDay_IsSentOnce()
    {
        Active(PortalRetentionCategory.Free, 30, lastRunDay: 30).Step.Should().Be(PortalRetentionStep.None);
    }

    [Fact]
    public void Warnings_SeveralMissed_OnlyTheLatestIsSent()
    {
        Active(PortalRetentionCategory.FormerPaying, 61, lastRunDay: 25).Letter.Should().Be(PortalRetentionLetter.SecondNotice);
    }

    [Fact]
    public void FinalNotice_MissedOnItsDay_IsSentLate_AndMovesTheDeletionBack()
    {
        var late = Blocked(PortalRetentionCategory.Free, 31);

        late.Letter.Should().Be(PortalRetentionLetter.FinalDeletionNotice, "the portal is never deleted without its last reminder");
        late.DeleteOn.Should().Be(_start.AddDays(38), "the reminder names a deletion a full week away");

        Blocked(PortalRetentionCategory.Free, 37, finalNoticeDay: 31).Step.Should().Be(PortalRetentionStep.None);
        Blocked(PortalRetentionCategory.Free, 38, finalNoticeDay: 31).Step.Should().Be(PortalRetentionStep.Delete);
    }

    [Fact]
    public void EarlyNotice_IsNotSentAfterTheFinalOne()
    {
        Blocked(PortalRetentionCategory.FormerPaying, 85, finalNoticeDay: 83, lastRunDay: 50).Step.Should().Be(PortalRetentionStep.None);
    }

    [Fact]
    public void PolicyStart_CountsFromThePolicysFirstRun()
    {
        var policyStart = new DateTime(2026, 9, 1);
        var schedule = _options.For(PortalRetentionCategory.Free);

        // Idle for years when the policy first runs: nothing happens on the first night ...
        PortalRetentionSchedule.DecideActive(schedule, new DateTime(2023, 1, 1), policyStart, policyStart)
            .Step.Should().Be(PortalRetentionStep.None);

        // ... it gets the whole chain counted from the first run ...
        PortalRetentionSchedule.DecideActive(schedule, new DateTime(2023, 1, 1), policyStart, policyStart.AddDays(30))
            .Letter.Should().Be(PortalRetentionLetter.FirstNotice);

        PortalRetentionSchedule.DecideActive(schedule, new DateTime(2023, 1, 1), policyStart, policyStart.AddDays(60),
                lastWarning: new PortalRetentionWarning(policyStart.AddDays(30), policyStart.AddDays(60)))
            .Step.Should().Be(PortalRetentionStep.Block);

        // ... while a portal active after the first run is counted from its own activity.
        PortalRetentionSchedule.DecideActive(schedule, policyStart.AddDays(10), policyStart, policyStart.AddDays(60))
            .Step.Should().Be(PortalRetentionStep.None);
    }

    [Fact]
    public void Activity_RestartsTheCount()
    {
        var schedule = _options.For(PortalRetentionCategory.Free);
        var today = _start.AddDays(59);

        PortalRetentionSchedule.DecideActive(schedule, today.AddDays(-1), _policyStart, today)
            .Step.Should().Be(PortalRetentionStep.None, "yesterday's activity puts the block two months away again");
    }

    [Fact]
    public void Options_ReadFromConfiguration_KeepTheDefaultsTheyDoNotName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:retention:free:blockAfterDays"] = "45"
            })
            .Build();

        var options = new PortalRetentionConfiguration(configuration).Options;

        options.Free.BlockAfterDays.Should().Be(45);
        options.Free.FirstNoticeDays.Should().Be(30, "a threshold the section does not name keeps its default");
        options.FormerPaying.RetentionDays.Should().Be(90);
    }

    [Fact]
    public void Options_WithoutTheSection_KeepTheDefaults()
    {
        var options = new PortalRetentionConfiguration(new ConfigurationBuilder().Build()).Options;

        options.Free.BlockAfterDays.Should().Be(60);
        options.WithBalance.BlockAfterDays.Should().Be(365);
    }
}
