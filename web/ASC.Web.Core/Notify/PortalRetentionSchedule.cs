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

namespace ASC.Web.Studio.Core.Notify;

/// <summary>
/// The four ways an unused portal drifts towards deletion. The tariff decides what the count starts
/// from, the wallet decides how long it runs.
/// </summary>
public enum PortalRetentionCategory
{
    /// <summary>A free portal with nothing left on its wallet. Counted from the last activity.</summary>
    Free,

    /// <summary>A portal whose paid tariff lapsed, with nothing left on its wallet. Counted from the due date.</summary>
    FormerPaying,

    /// <summary>A free portal that still has money on its wallet. Counted from the last activity.</summary>
    FreeWithBalance,

    /// <summary>A portal whose paid tariff lapsed and that still has money on its wallet. Counted from the due date.</summary>
    FormerPayingWithBalance
}

/// <summary>What the owner is told on a given day. The category decides the wording.</summary>
public enum PortalRetentionLetter
{
    /// <summary>The first warning, a month into the silence.</summary>
    FirstNotice,

    /// <summary>The second warning, before the portal is blocked.</summary>
    SecondNotice,

    /// <summary>One of the monthly reminders that money is left on the wallet.</summary>
    MonthlyNotice,

    /// <summary>The portal has just been blocked; the letter names the day it is deleted.</summary>
    Blocked,

    /// <summary>The early reminder that a blocked portal is about to be deleted.</summary>
    EarlyDeletionNotice,

    /// <summary>The last reminder that a blocked portal is about to be deleted.</summary>
    FinalDeletionNotice
}

/// <summary>What the daily job has to do with a portal today.</summary>
public enum PortalRetentionStep
{
    /// <summary>Nothing today: the portal is left to the ordinary periodic letters, or left alone if blocked.</summary>
    None,

    /// <summary>A warning or a reminder goes out; the portal stays as it is.</summary>
    Notify,

    /// <summary>The portal is blocked and the owner is told when it will be deleted.</summary>
    Block,

    /// <summary>The blocked portal is removed together with its data.</summary>
    Delete
}

/// <summary>
/// The answer for one portal on one day: the step, the letter that goes with it, and the dates the
/// letter has to disclose. <see cref="BlockOn"/> is the day the portal is (or was) blocked;
/// <see cref="DeleteOn"/> the day it is deleted unless someone comes back.
/// </summary>
public readonly record struct PortalRetentionDecision(PortalRetentionStep Step, PortalRetentionLetter? Letter, DateTime BlockOn, DateTime DeleteOn)
{
    public static PortalRetentionDecision Nothing(DateTime blockOn, DateTime deleteOn) => new(PortalRetentionStep.None, null, blockOn, deleteOn);
}

/// <summary>
/// The last warning an active portal was sent: the day it went out and the day of the block it named.
/// The block keeps to that day, whatever the portal's category has become since.
/// </summary>
public readonly record struct PortalRetentionWarning(DateTime SentOn, DateTime BlockOn);

/// <summary>
/// The thresholds of one category, in days (or months) from the day the count starts. Zero switches a
/// letter off.
/// </summary>
public sealed class PortalRetentionScheduleOptions
{
    /// <summary>How many days into the count the first warning goes out.</summary>
    public int FirstNoticeDays { get; set; }

    /// <summary>How many days into the count the second warning goes out. Zero means there is none.</summary>
    public int SecondNoticeDays { get; set; }

    /// <summary>The first month of the monthly reminders, inclusive. Zero means there are none.</summary>
    public int MonthlyNoticeFromMonth { get; set; }

    /// <summary>The last month of the monthly reminders, inclusive.</summary>
    public int MonthlyNoticeToMonth { get; set; }

    /// <summary>How many days into the count the portal is blocked.</summary>
    public int BlockAfterDays { get; set; }

    /// <summary>How long a blocked portal is kept before it is deleted.</summary>
    public int RetentionDays { get; set; }

    /// <summary>How many days before the deletion the early reminder goes out.</summary>
    public int EarlyDeletionNoticeDays { get; set; }

    /// <summary>How many days before the deletion the last reminder goes out.</summary>
    public int FinalDeletionNoticeDays { get; set; }
}

/// <summary>
/// The retention policy as configured under <c>core:retention</c>. The policy always applies; every
/// threshold has a default, so the section only has to name what differs.
/// </summary>
public sealed class PortalRetentionOptions
{
    public PortalRetentionScheduleOptions Free { get; set; } = new()
    {
        FirstNoticeDays = 30,
        BlockAfterDays = 60,
        RetentionDays = 30,
        FinalDeletionNoticeDays = 7
    };

    public PortalRetentionScheduleOptions FormerPaying { get; set; } = new()
    {
        FirstNoticeDays = 30,
        SecondNoticeDays = 60,
        BlockAfterDays = 90,
        RetentionDays = 90,
        EarlyDeletionNoticeDays = 30,
        FinalDeletionNoticeDays = 7
    };

    /// <summary>Both categories with money left on the wallet share one schedule.</summary>
    public PortalRetentionScheduleOptions WithBalance { get; set; } = new()
    {
        FirstNoticeDays = 30,
        MonthlyNoticeFromMonth = 6,
        MonthlyNoticeToMonth = 11,
        BlockAfterDays = 365,
        RetentionDays = 90,
        EarlyDeletionNoticeDays = 30,
        FinalDeletionNoticeDays = 7
    };

    /// <summary>
    /// How long a run waits for the accounting service to say whether money is left on a portal's wallet, in
    /// seconds. A portal it does not answer for in time waits for the next run.
    /// </summary>
    public int BalanceTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// After this many failed balance requests in a row the run stops asking for the rest of the night, so an
    /// accounting service that is down does not hold up the run portal after portal; the portals it would have
    /// asked about wait for the next run.
    /// </summary>
    public int BalanceFailuresBeforeStop { get; set; } = 20;

    public PortalRetentionScheduleOptions For(PortalRetentionCategory category)
    {
        return category switch
        {
            PortalRetentionCategory.Free => Free,
            PortalRetentionCategory.FormerPaying => FormerPaying,
            PortalRetentionCategory.FreeWithBalance or PortalRetentionCategory.FormerPayingWithBalance => WithBalance,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }
}

/// <summary>Reads <see cref="PortalRetentionOptions"/> once from <c>core:retention</c>.</summary>
[Singleton]
public class PortalRetentionConfiguration(IConfiguration configuration)
{
    public PortalRetentionOptions Options => field ??= configuration.GetSection("core:retention").Get<PortalRetentionOptions>() ?? new PortalRetentionOptions();
}

/// <summary>
/// Decides what happens to an unused portal today. It keeps no state of its own: the caller hands it
/// what is stored - the day the count starts, for an active portal its last warning, for a blocked portal
/// the day its status changed and the day its last reminder went out, and the last day the job got through.
/// </summary>
/// <remarks>
/// A portal is never blocked earlier than the day its last warning named, nor without a warning naming
/// the day at all: the count can reach the block before any warning of it went out - the warning was
/// missed in a long stop of the job, or the wallet emptied and the portal fell under a shorter schedule -
/// and then the warning goes out first and the block follows it as far apart as the schedule keeps its
/// last warning from its block. Blocking catches up: a portal past the day it was warned of is blocked on
/// the first run that sees it. The letter that goes with the block is sent at that moment, so the owner
/// is always told, and the deletion is counted from the block, so the whole retention period lies
/// between that letter and the deletion. A warning or a reminder is sent on the first run after its day:
/// a run covers every day since the last one the job got through, and when several letters fall into
/// those days only the latest goes out, since it says everything the earlier ones would have. The last
/// reminder before the deletion is never skipped: a portal is deleted only once it has gone out, a full
/// notice period earlier, and one sent late moves the deletion back by as much. A warning due before the
/// policy could see the portal at all - a lapsed tariff still in its grace period - goes out on the
/// first day the portal is the policy's.
/// </remarks>
public static class PortalRetentionSchedule
{
    /// <summary>What today brings a portal that is still active: a warning, the block, or nothing.</summary>
    /// <param name="schedule">The thresholds of the portal's category.</param>
    /// <param name="anchor">
    /// The day the count starts from: the last activity for a free portal, the later of the due date and
    /// the last status change for a former paying one.
    /// </param>
    /// <param name="policyStart">
    /// The day the policy first ran in this installation. No count starts before it, so the first run does
    /// not block every long-idle portal without the warnings that should have come first.
    /// </param>
    /// <param name="today">The day the run is for.</param>
    /// <param name="noticesFrom">
    /// The first day the policy sees the portal: for a former paying one, the first day after the grace
    /// period of its tariff. A warning due earlier is sent on that day; the default puts no such limit.
    /// </param>
    /// <param name="lastRunOn">
    /// The last day the job got through. The letters of the days after it, up to today, are this run's;
    /// the default is yesterday, so only today's are.
    /// </param>
    /// <param name="lastWarning">
    /// The last warning the portal was sent, or null when it has none. A warning sent before the count
    /// started belongs to an earlier count and is not taken into account.
    /// </param>
    public static PortalRetentionDecision DecideActive(PortalRetentionScheduleOptions schedule, DateTime anchor, DateTime policyStart, DateTime today, DateTime noticesFrom = default, DateTime? lastRunOn = null, PortalRetentionWarning? lastWarning = null)
    {
        today = today.Date;

        var start = anchor.Date > policyStart.Date ? anchor.Date : policyStart.Date;
        var blockOn = start.AddDays(schedule.BlockAfterDays);
        var deleteOn = blockOn.AddDays(schedule.RetentionDays);

        if (today >= blockOn)
        {
            return BlockOrWarn(schedule, start, today, lastWarning);
        }

        var letter = WarningFor(schedule, start, noticesFrom.Date, today, CoveredFrom(lastRunOn, today));

        return letter.HasValue
            ? new PortalRetentionDecision(PortalRetentionStep.Notify, letter, blockOn, deleteOn)
            : PortalRetentionDecision.Nothing(blockOn, deleteOn);
    }

    /// <summary>What today brings a blocked portal: a reminder, the deletion, or nothing.</summary>
    /// <param name="schedule">The thresholds of the category the portal was blocked under.</param>
    /// <param name="blockedOn">The day the portal was blocked.</param>
    /// <param name="today">The day the run is for.</param>
    /// <param name="lastRunOn">
    /// The last day the job got through. The letters of the days after it, up to today, are this run's;
    /// the default is yesterday, so only today's are.
    /// </param>
    /// <param name="finalNoticeSentOn">
    /// The day the last reminder before the deletion went out, or null while it has not.
    /// </param>
    public static PortalRetentionDecision DecideBlocked(PortalRetentionScheduleOptions schedule, DateTime blockedOn, DateTime today, DateTime? lastRunOn = null, DateTime? finalNoticeSentOn = null)
    {
        blockedOn = blockedOn.Date;
        today = today.Date;

        var coveredFrom = CoveredFrom(lastRunOn, today);
        var deleteOn = blockedOn.AddDays(schedule.RetentionDays);
        var noticeDays = schedule.FinalDeletionNoticeDays;

        // A reminder that would fall on the day of the block, or before it, is already covered by the
        // letter that announced the block; otherwise the last one is owed before the portal may go.
        var finalNoticeOwed = noticeDays > 0 && deleteOn.AddDays(-noticeDays) > blockedOn;

        if (!finalNoticeOwed)
        {
            return today >= deleteOn
                ? new PortalRetentionDecision(PortalRetentionStep.Delete, null, blockedOn, deleteOn)
                : EarlyNoticeOrNothing(schedule, blockedOn, deleteOn, today, coveredFrom);
        }

        if (finalNoticeSentOn is { } sentOn)
        {
            // A reminder sent late moved the deletion back: the date it named is the one that holds.
            var noticedDeleteOn = sentOn.Date.AddDays(noticeDays);

            if (noticedDeleteOn > deleteOn)
            {
                deleteOn = noticedDeleteOn;
            }

            return today >= deleteOn
                ? new PortalRetentionDecision(PortalRetentionStep.Delete, null, blockedOn, deleteOn)
                : PortalRetentionDecision.Nothing(blockedOn, deleteOn);
        }

        if (today >= deleteOn.AddDays(-noticeDays))
        {
            // Due today, or missed on its day: it goes now, and the deletion waits a full notice period
            // after it.
            var latestDeleteOn = today.AddDays(noticeDays);

            return new PortalRetentionDecision(PortalRetentionStep.Notify, PortalRetentionLetter.FinalDeletionNotice, blockedOn, latestDeleteOn > deleteOn ? latestDeleteOn : deleteOn);
        }

        return EarlyNoticeOrNothing(schedule, blockedOn, deleteOn, today, coveredFrom);
    }

    /// <summary>The last day the letters of which are not this run's: the last day the job got through.</summary>
    private static DateTime CoveredFrom(DateTime? lastRunOn, DateTime today)
    {
        return lastRunOn?.Date ?? today.AddDays(-1);
    }

    /// <summary>
    /// The count has reached the block. The portal is blocked once the day its last warning of this count
    /// named has come; with no such warning, it is warned now, and the block it is warned of is as far
    /// away as the schedule keeps its last warning from its block.
    /// </summary>
    private static PortalRetentionDecision BlockOrWarn(PortalRetentionScheduleOptions schedule, DateTime start, DateTime today, PortalRetentionWarning? lastWarning)
    {
        if (lastWarning is { } warning && warning.SentOn.Date > start)
        {
            // The day a wallet schedule promised stays when the wallet empties and the free schedule's
            // own day has long passed: the owner was told that day, and only that day.
            var warnedBlockOn = warning.BlockOn.Date;

            return today >= warnedBlockOn
                ? BlockToday(schedule, today)
                : PortalRetentionDecision.Nothing(warnedBlockOn, warnedBlockOn.AddDays(schedule.RetentionDays));
        }

        if (LastWarningBeforeBlock(schedule, start) is not { } last)
        {
            // A schedule configured without warnings blocks unannounced.
            return BlockToday(schedule, today);
        }

        var blockOn = today.AddDays((start.AddDays(schedule.BlockAfterDays) - last.DueOn).Days);

        return new PortalRetentionDecision(PortalRetentionStep.Notify, last.Letter, blockOn, blockOn.AddDays(schedule.RetentionDays));
    }

    private static PortalRetentionDecision BlockToday(PortalRetentionScheduleOptions schedule, DateTime today)
    {
        // Blocked today, so the count of the retention period starts today as well.
        return new PortalRetentionDecision(PortalRetentionStep.Block, PortalRetentionLetter.Blocked, today, today.AddDays(schedule.RetentionDays));
    }

    /// <summary>The last warning the schedule sends before its block, or null when it sends none.</summary>
    private static (PortalRetentionLetter Letter, DateTime DueOn)? LastWarningBeforeBlock(PortalRetentionScheduleOptions schedule, DateTime start)
    {
        var blockOn = start.AddDays(schedule.BlockAfterDays);
        (PortalRetentionLetter Letter, DateTime DueOn)? last = null;

        foreach (var warning in WarningDays(schedule, start))
        {
            if (warning.DueOn < blockOn && (last is null || warning.DueOn > last.Value.DueOn))
            {
                last = warning;
            }
        }

        return last;
    }

    /// <summary>
    /// The warning that goes out today, if any: one whose day falls after <paramref name="coveredFrom"/>, up
    /// to today. A warning due before <paramref name="noticesFrom"/> is moved to that day. When several fall
    /// into this run, only the latest is sent, since it says everything the earlier ones would have.
    /// </summary>
    private static PortalRetentionLetter? WarningFor(PortalRetentionScheduleOptions schedule, DateTime start, DateTime noticesFrom, DateTime today, DateTime coveredFrom)
    {
        PortalRetentionLetter? letter = null;
        var latest = DateTime.MinValue;

        foreach (var (warning, dueOn) in WarningDays(schedule, start))
        {
            var sendOn = dueOn < noticesFrom ? noticesFrom : dueOn;

            if (sendOn > coveredFrom && sendOn <= today && dueOn > latest)
            {
                letter = warning;
                latest = dueOn;
            }
        }

        return letter;
    }

    private static IEnumerable<(PortalRetentionLetter Letter, DateTime DueOn)> WarningDays(PortalRetentionScheduleOptions schedule, DateTime start)
    {
        if (schedule.FirstNoticeDays > 0)
        {
            yield return (PortalRetentionLetter.FirstNotice, start.AddDays(schedule.FirstNoticeDays));
        }

        if (schedule.SecondNoticeDays > 0)
        {
            yield return (PortalRetentionLetter.SecondNotice, start.AddDays(schedule.SecondNoticeDays));
        }

        if (schedule.MonthlyNoticeFromMonth > 0)
        {
            for (var month = schedule.MonthlyNoticeFromMonth; month <= schedule.MonthlyNoticeToMonth; month++)
            {
                yield return (PortalRetentionLetter.MonthlyNotice, start.AddMonths(month));
            }
        }
    }

    private static PortalRetentionDecision EarlyNoticeOrNothing(PortalRetentionScheduleOptions schedule, DateTime blockedOn, DateTime deleteOn, DateTime today, DateTime coveredFrom)
    {
        var daysBefore = schedule.EarlyDeletionNoticeDays;
        var noticeOn = deleteOn.AddDays(-daysBefore);

        return daysBefore > 0 && noticeOn > blockedOn && noticeOn > coveredFrom && noticeOn <= today
            ? new PortalRetentionDecision(PortalRetentionStep.Notify, PortalRetentionLetter.EarlyDeletionNotice, blockedOn, deleteOn)
            : PortalRetentionDecision.Nothing(blockedOn, deleteOn);
    }
}
