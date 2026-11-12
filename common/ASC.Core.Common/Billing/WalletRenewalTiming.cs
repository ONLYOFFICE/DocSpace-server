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

namespace ASC.Core.Billing;

/// <summary>
/// The two intervals the renewal of wallet subscriptions (<c>RenewSubscriptionService</c> of Web.Studio) runs by,
/// read together because the second depends on the first.
/// </summary>
/// <remarks>
/// <c>core:accounting:renewperiod</c> - how often the service looks for subscriptions that are due, and how long a
/// queued renewal is left to the process that queued it: <c>00:00:01</c> to <c>01:00:00</c>, <c>00:01:00</c> when not
/// set. The maximum keeps it well under the lifetime of the renewal journal, one day.
/// <c>core:accounting:renewadvance</c> - how long before its due date a subscription is renewed, so that it is
/// extended before it expires: the period to <c>1.00:00:00</c>, the period when not set. Not less than the period: a
/// row that comes due between two runs would be renewed by the later one, past its due date.
/// </remarks>
public static class WalletRenewalTiming
{
    public const string PeriodKey = "core:accounting:renewperiod";
    public const string AdvanceKey = "core:accounting:renewadvance";

    public static readonly TimeSpan DefaultPeriod = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MinPeriod = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaxAdvance = TimeSpan.FromDays(1);

    public static (TimeSpan Period, TimeSpan Advance) Read(IConfiguration configuration, ILogger logger)
    {
        var period = configuration.ReadInterval(PeriodKey, DefaultPeriod, MinPeriod, MaxPeriod, logger);
        var advance = configuration.ReadInterval(AdvanceKey, period, period, MaxAdvance, logger);

        return (period, advance);
    }
}
