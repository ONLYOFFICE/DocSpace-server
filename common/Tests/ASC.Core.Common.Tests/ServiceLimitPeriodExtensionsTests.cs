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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The end of the current period of a service limit, worked out the way the accounting service reports it in the
/// spending: the start of the next calendar period in UTC less one tick, a week starting on Monday.
/// </summary>
public class ServiceLimitPeriodExtensionsTests
{
    [Theory]
    [InlineData(ServiceLimitPeriod.Day, "2026-10-22T08:47:00Z", "2026-10-22T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Day, "2026-10-22T00:00:00Z", "2026-10-22T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Week, "2026-10-19T00:00:00Z", "2026-10-25T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Week, "2026-10-22T08:47:00Z", "2026-10-25T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Week, "2026-10-25T23:59:59Z", "2026-10-25T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Month, "2026-10-22T08:47:00Z", "2026-10-31T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Month, "2026-12-31T12:00:00Z", "2026-12-31T23:59:59.9999999Z")]
    [InlineData(ServiceLimitPeriod.Month, "2028-02-10T12:00:00Z", "2028-02-29T23:59:59.9999999Z")]
    public void GetCurrentPeriodEnd_CalendarPeriod_EndsOneTickBeforeTheNextOne(ServiceLimitPeriod period, string now, string expected)
    {
        var periodEnd = period.GetCurrentPeriodEnd(Utc(now));

        periodEnd.Should().Be(Utc(expected));
        periodEnd!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void GetCurrentPeriodEnd_Lifetime_HasNoEnd()
    {
        ServiceLimitPeriod.Lifetime.GetCurrentPeriodEnd(Utc("2026-10-22T08:47:00Z")).Should().BeNull();
    }

    private static DateTime Utc(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }
}
