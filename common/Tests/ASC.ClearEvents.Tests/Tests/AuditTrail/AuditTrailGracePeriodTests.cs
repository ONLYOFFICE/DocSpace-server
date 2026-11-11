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

namespace ASC.ClearEvents.Tests.Tests.AuditTrail;

/// <summary>
/// A portal whose subscription ended keeps the paid retention for <c>paidGracePeriodDays</c> more, counted from the
/// end of the payment delay, or from the due date when there was none: a customer who renews a few days late must
/// not have lost the years of events past the free life time on the first pass after the delay.
/// </summary>
public class AuditTrailGracePeriodTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Theory]
    [InlineData(13, 3, false, PaidGracePeriodDays, true)]
    [InlineData(16, 6, false, PaidGracePeriodDays, true)]
    [InlineData(18, 8, false, PaidGracePeriodDays, false)]
    // The last day of the delay, or the due date, still counts as paid with the tariff service, which compares dates:
    // the tariff is unpaid from the midnight after it, and a grace period of N days is N whole days from there.
    [InlineData(17, 7, false, PaidGracePeriodDays, true)]
    [InlineData(11, 1, false, 1, true)]
    [InlineData(7, null, false, PaidGracePeriodDays, true)]
    [InlineData(3, null, false, PaidGracePeriodDays, true)]
    [InlineData(8, null, false, PaidGracePeriodDays, false)]
    [InlineData(13, 3, true, PaidGracePeriodDays, false)]
    [InlineData(13, 3, false, 0, false)]
    [InlineData(40, 20, false, 30, true)]
    public async Task ClearAsync_SubscriptionEnded_KeepsThePaidRetentionForTheGracePeriod(
        int dueDaysAgo, int? delayEndedDaysAgo, bool freeQuota, int paidGracePeriodDays, bool keepsPaidRetention)
    {
        // The rows on the boundary of the grace period hold only while the pass runs on the day the dates are set
        // from, so a case that runs across midnight (UTC) is run once more, with events of its own.
        List<int> remaining;
        int pastFreeLifeTime;

        for (var attempt = 1; ; attempt++)
        {
            // Arrange: the dates as the tariff service computes them, at the start of a day.
            var today = DateTime.UtcNow.Date;

            TestTariffs.Set(
                TenantId,
                TariffState.NotPaid,
                freeQuota,
                dueDate: today.AddDays(-dueDaysAgo),
                delayDueDate: delayEndedDaysAgo is { } days ? today.AddDays(-days) : null);

            pastFreeLifeTime = await SeedEventAsync(FreeLifeTimeDays + 30);

            // Past both life times: removed on every tariff, so a pass that skipped the tenant cannot pass for one
            // that kept the paid retention.
            var pastPaidLifeTime = await SeedEventAsync(PaidLifeTimeDays + 30);

            // Act
            await RunCleanupAsync(Retention(paidGracePeriodDays: paidGracePeriodDays));

            remaining = await RemainingEventsAsync([pastFreeLifeTime, pastPaidLifeTime]);

            if (DateTime.UtcNow.Date == today || attempt == 2)
            {
                break;
            }
        }

        // Assert

        if (keepsPaidRetention)
        {
            remaining.Should().BeEquivalentTo([pastFreeLifeTime], "the subscription ended within the grace period");
        }
        else
        {
            remaining.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task ClearAsync_NeverPaid_GetsNoGracePeriod()
    {
        // Arrange: unpaid without a due date, as the tariff service has a portal that never paid.
        TestTariffs.Set(TenantId, TariffState.NotPaid, dueDate: DateTime.MinValue);

        var pastFreeLifeTime = await SeedEventAsync(FreeLifeTimeDays + 30);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync([pastFreeLifeTime])).Should().BeEmpty();
    }
}
