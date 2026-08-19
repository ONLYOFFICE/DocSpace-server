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
/// Everything a periodic letter needs to answer "is today my day for this portal?", gathered once per
/// portal before the letters are asked.
///
/// It exists so that twenty-five predicates do not each go fetch the tariff and the quota for
/// themselves: those lookups are cached, but not free, and they would be multiplied by every portal in
/// the installation. <see cref="LastActivity"/> stays lazy for the same reason in reverse — the audit
/// and login queries behind it are real database work that only the inactivity letters ever need.
/// </summary>
public sealed record PeriodicLetterContext
{
    public required Tenant Tenant { get; init; }

    public required Tariff Tariff { get; init; }

    public required TenantQuota Quota { get; init; }

    /// <summary>
    /// The quota this portal would need for what it actually uses, i.e. <c>TenantExtra.GetRightQuota</c>
    /// — the source of the price shown in the payment letters.
    /// </summary>
    public required TenantQuota RightQuota { get; init; }

    /// <summary>The day the run is for: the scheduler's timestamp, truncated.</summary>
    public required DateTime NowDate { get; init; }

    public required DateTime CreatedDate { get; init; }

    /// <summary>When the tariff runs out. Meaningless unless <see cref="DueDateIsNotMax"/>.</summary>
    public required DateTime DueDate { get; init; }

    public required bool DueDateIsNotMax { get; init; }

    /// <summary>When the grace period runs out. Meaningless unless <see cref="DelayDueDateIsNotMax"/>.</summary>
    public required DateTime DelayDueDate { get; init; }

    public required bool DelayDueDateIsNotMax { get; init; }

    /// <summary>
    /// Whether the portal still shows ONLYOFFICE branding. Only the Enterprise trial letter asks: a
    /// white-labelled portal must not send letters about our own apps.
    /// </summary>
    public bool DefaultRebranding { get; init; } = true;

    /// <summary>
    /// The day the installation started counting towards deleting unused portals. Warnings are silent
    /// before it, so an upgrade does not mail every idle portal at once on the first night.
    /// </summary>
    public required DateTime UnusedPortalNotifyFrom { get; init; }

    /// <summary>
    /// The last time anyone did anything on the portal — the later of the last audit event and the last
    /// successful login, falling back to the creation date. Two database queries, so it is resolved on
    /// first use and only for the letters that ask.
    /// </summary>
    public required Lazy<Task<DateTime>> LastActivity { get; init; }

    public Task<DateTime> GetLastActivityDateAsync()
    {
        return LastActivity.Value;
    }

    /// <summary>
    /// True when <see cref="NowDate"/>, shifted by <paramref name="offsetDays"/>, is the monthly
    /// anniversary of the portal's creation. The day is clamped to the length of the month, so a portal
    /// created on the 29th-31st still gets its check in February and in the 30-day months instead of
    /// silently skipping them — the inactivity warnings are one-month-wide windows, and a skipped month
    /// means a warning is never sent at all.
    /// </summary>
    public bool IsCreationAnniversary(int offsetDays = 0)
    {
        var date = NowDate.AddDays(offsetDays);

        return date.Day == Math.Min(CreatedDate.Day, DateTime.DaysInMonth(date.Year, date.Month));
    }
}
