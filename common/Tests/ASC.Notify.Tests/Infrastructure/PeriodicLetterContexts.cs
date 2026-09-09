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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// The portal states a periodic letter can be asked about, built in memory.
///
/// <see cref="Fresh"/> and <see cref="Paid(Tenant)"/> are shared: the schedule tests start every case
/// from a portal nothing is due for, and the letter tests render every letter against a portal on a
/// paid tariff. The rest — a lapsed tariff, a grace period, an idle portal — only the schedule tests
/// ask for, and they live here so that the two suites cannot describe the same state differently.
///
/// A context is an input rather than a tag, which is what keeps the letter tests cheap — a letter about
/// an expiring tariff does not need a portal whose tariff actually expires.
/// </summary>
internal static class PeriodicLetterContexts
{
    /// <summary>
    /// A portal nothing is due for: created on <paramref name="today"/>, on a paid-for-nothing trial with
    /// no dates set. Every other factory here moves only what its own letter looks at.
    /// </summary>
    public static PeriodicLetterContext Fresh(Tenant tenant, DateTime today)
    {
        return new PeriodicLetterContext
        {
            Tenant = tenant,
            Tariff = new Tariff { Quotas = [], State = TariffState.Trial, DueDate = DateTime.MaxValue, DelayDueDate = DateTime.MaxValue },
            Quota = Quota(),
            NowDate = today,
            CreatedDate = today,
            DueDate = DateTime.MaxValue.Date,
            DueDateIsNotMax = false,
            DelayDueDate = DateTime.MaxValue.Date,
            DelayDueDateIsNotMax = false,
            DefaultRebranding = true,
            UnusedPortalNotifyFrom = today.AddYears(-1),
            LastActivity = Activity(today)
        };
    }

    public static TenantQuota Quota(bool free = false, bool trial = false, bool lifetime = false, bool customization = false)
    {
        return new TenantQuota { Free = free, Trial = trial, Lifetime = lifetime, Customization = customization };
    }

    public static Lazy<Task<DateTime>> Activity(DateTime date)
    {
        return new Lazy<Task<DateTime>>(() => Task.FromResult(date));
    }

    /// <summary>A portal on a paid tariff running out on <paramref name="due"/>.</summary>
    public static PeriodicLetterContext Paid(PeriodicLetterContext context, DateTime due, DateTime? delay = null)
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

    /// <summary>
    /// A portal on a trial running out on <paramref name="due"/>. Every date a payment warning looks at
    /// is the same as <see cref="Paid(PeriodicLetterContext, DateTime, DateTime?)"/>; only the state
    /// differs, which is exactly what the <c>&gt;= TariffState.Paid</c> guards are there for.
    /// </summary>
    public static PeriodicLetterContext Trial(PeriodicLetterContext context, DateTime due)
    {
        return Paid(context, due) with
        {
            Tariff = new Tariff { Quotas = [], State = TariffState.Trial, DueDate = due, DelayDueDate = DateTime.MaxValue },
            Quota = Quota(trial: true)
        };
    }

    /// <summary>A portal inside its grace period, which runs out on <paramref name="delay"/>.</summary>
    public static PeriodicLetterContext Delayed(PeriodicLetterContext context, DateTime delay)
    {
        return context with
        {
            Tariff = new Tariff { Quotas = [], State = TariffState.Delay, DueDate = context.NowDate.AddDays(-30), DelayDueDate = delay },
            DueDate = context.NowDate.AddDays(-30),
            DueDateIsNotMax = true,
            DelayDueDate = delay.Date,
            DelayDueDateIsNotMax = true
        };
    }

    /// <summary>A portal whose paid tariff lapsed on <paramref name="due"/> and was never renewed.</summary>
    public static PeriodicLetterContext Lapsed(PeriodicLetterContext context, DateTime due)
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
    /// its creation — the only day the inactivity warnings look at.
    /// </summary>
    public static PeriodicLetterContext Idle(PeriodicLetterContext context, int months)
    {
        return context with
        {
            CreatedDate = context.NowDate.AddYears(-2),
            LastActivity = Activity(context.NowDate.AddMonths(-months))
        };
    }

    /// <summary>
    /// What a letter is rendered against when it does not care about the tariff: the portal under test,
    /// on a paid tariff with a year left on it.
    /// </summary>
    public static PeriodicLetterContext Paid(Tenant tenant)
    {
        var today = DateTime.UtcNow.Date;

        return Paid(Fresh(tenant, today), today.AddYears(1));
    }
}
