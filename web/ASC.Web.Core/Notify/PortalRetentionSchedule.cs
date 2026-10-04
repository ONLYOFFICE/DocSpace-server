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
    None,
    Notify,
    Block,
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
/// The thresholds of one category, in days (or months) from the day the count starts. Zero switches a
/// letter off.
/// </summary>
public sealed class PortalRetentionScheduleOptions
{
    public int FirstNoticeDays { get; set; }

    public int SecondNoticeDays { get; set; }

    /// <summary>The first month of the monthly reminders, inclusive. Zero means there are none.</summary>
    public int MonthlyNoticeFromMonth { get; set; }

    /// <summary>The last month of the monthly reminders, inclusive.</summary>
    public int MonthlyNoticeToMonth { get; set; }

    public int BlockAfterDays { get; set; }

    /// <summary>How long a blocked portal is kept before it is deleted.</summary>
    public int RetentionDays { get; set; }

    /// <summary>How many days before the deletion the early reminder goes out.</summary>
    public int EarlyDeletionNoticeDays { get; set; }

    /// <summary>How many days before the deletion the last reminder goes out.</summary>
    public int FinalDeletionNoticeDays { get; set; }
}

/// <summary>
/// The retention policy as configured under <c>core:retention</c>. Every threshold has a default, so
/// the section only has to name what differs; <see cref="Enabled"/> is off unless switched on.
/// </summary>
public sealed class PortalRetentionOptions
{
    /// <summary>Whether the daily job applies the policy at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>Whether the job only logs what it would do, without sending, blocking or deleting.</summary>
    public bool DryRun { get; set; }

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
/// Decides what happens to an unused portal today. It keeps no state of its own: everything it needs
/// is already stored - the day the count starts, and for a blocked portal the day its status changed.
/// </summary>
/// <remarks>
/// Blocking and deletion catch up: a portal past its threshold is blocked (or deleted) on the first run
/// that sees it, even if the job missed the exact day. The letter that goes with the block is sent at
/// that moment, so the owner is always told, and the deletion is counted from the block, so the whole
/// retention period lies between that letter and the deletion - whatever happened before, a change of
/// category included. The reminders around them are sent on their exact day only, like every other
/// periodic letter: a day the job did not run is a reminder that is not sent.
/// </remarks>
public static class PortalRetentionSchedule
{
    /// <param name="schedule">The thresholds of the portal's category.</param>
    /// <param name="anchor">
    /// The day the count starts from: the last activity for a free portal, the later of the due date and
    /// the last status change for a former paying one.
    /// </param>
    /// <param name="policyStart">
    /// The day the policy was switched on. No count starts before it, so switching it on does not block
    /// every long-idle portal on the first night without the warnings that should have come first.
    /// </param>
    /// <param name="blockedOn">The day the portal was blocked, or null while it is still active.</param>
    /// <param name="today">The day the run is for.</param>
    public static PortalRetentionDecision Decide(PortalRetentionScheduleOptions schedule, DateTime anchor, DateTime policyStart, DateTime? blockedOn, DateTime today)
    {
        today = today.Date;

        return blockedOn.HasValue
            ? DecideBlocked(schedule, blockedOn.Value.Date, today)
            : DecideActive(schedule, anchor.Date > policyStart.Date ? anchor.Date : policyStart.Date, today);
    }

    private static PortalRetentionDecision DecideActive(PortalRetentionScheduleOptions schedule, DateTime start, DateTime today)
    {
        var blockOn = start.AddDays(schedule.BlockAfterDays);
        var deleteOn = blockOn.AddDays(schedule.RetentionDays);

        if (today >= blockOn)
        {
            // Blocked today, so the count of the retention period starts today as well.
            return new PortalRetentionDecision(PortalRetentionStep.Block, PortalRetentionLetter.Blocked, today, today.AddDays(schedule.RetentionDays));
        }

        PortalRetentionLetter? letter = null;

        if (IsDay(start, schedule.FirstNoticeDays, today))
        {
            letter = PortalRetentionLetter.FirstNotice;
        }
        else if (IsDay(start, schedule.SecondNoticeDays, today))
        {
            letter = PortalRetentionLetter.SecondNotice;
        }
        else if (IsMonthlyNoticeDay(schedule, start, today))
        {
            letter = PortalRetentionLetter.MonthlyNotice;
        }

        return letter.HasValue
            ? new PortalRetentionDecision(PortalRetentionStep.Notify, letter, blockOn, deleteOn)
            : PortalRetentionDecision.Nothing(blockOn, deleteOn);
    }

    private static PortalRetentionDecision DecideBlocked(PortalRetentionScheduleOptions schedule, DateTime blockedOn, DateTime today)
    {
        var deleteOn = blockedOn.AddDays(schedule.RetentionDays);

        if (today >= deleteOn)
        {
            return new PortalRetentionDecision(PortalRetentionStep.Delete, null, blockedOn, deleteOn);
        }

        PortalRetentionLetter? letter = null;

        // A reminder that would fall on the day of the block, or before it, is already covered by the
        // letter that announced the block.
        if (IsReminderDay(blockedOn, deleteOn, schedule.EarlyDeletionNoticeDays, today))
        {
            letter = PortalRetentionLetter.EarlyDeletionNotice;
        }
        else if (IsReminderDay(blockedOn, deleteOn, schedule.FinalDeletionNoticeDays, today))
        {
            letter = PortalRetentionLetter.FinalDeletionNotice;
        }

        return letter.HasValue
            ? new PortalRetentionDecision(PortalRetentionStep.Notify, letter, blockedOn, deleteOn)
            : PortalRetentionDecision.Nothing(blockedOn, deleteOn);
    }

    private static bool IsDay(DateTime start, int days, DateTime today)
    {
        return days > 0 && start.AddDays(days) == today;
    }

    private static bool IsReminderDay(DateTime blockedOn, DateTime deleteOn, int daysBefore, DateTime today)
    {
        return daysBefore > 0 && deleteOn.AddDays(-daysBefore) == today && today > blockedOn;
    }

    private static bool IsMonthlyNoticeDay(PortalRetentionScheduleOptions schedule, DateTime start, DateTime today)
    {
        if (schedule.MonthlyNoticeFromMonth <= 0)
        {
            return false;
        }

        for (var month = schedule.MonthlyNoticeFromMonth; month <= schedule.MonthlyNoticeToMonth; month++)
        {
            if (start.AddMonths(month) == today)
            {
                return true;
            }
        }

        return false;
    }
}
