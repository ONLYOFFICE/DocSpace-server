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
/// How long a portal keeps its audit trail: the free retention unless its tariff is paid — paid or in the
/// payment delay, and not on a free quota — in which case the paid one.
/// </summary>
public class AuditTrailRetentionTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ClearAsync_FreePortal_RemovesOnlyEventsOlderThanFreeLifeTime()
    {
        // Arrange
        var expired = await SeedEventAsync(FreeLifeTimeDays + 30);
        var recent = await SeedEventAsync(FreeLifeTimeDays - 30);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync([expired, recent])).Should().BeEquivalentTo([recent]);
        TestTariffs.LookupsOf(TenantId).Should().Be(1);
    }

    [Fact]
    public async Task ClearAsync_OnlyHistoryPastFreeLifeTime_DoesNotLookUpTheTariff()
    {
        // Arrange: the portal is picked by its oldest event, which is part of a history, and nothing else is old.
        const int roomId = int.MaxValue - 1;

        var inHistory = await SeedEventAsync(PaidLifeTimeDays * 2, roomId);
        var recent = await SeedEventAsync(FreeLifeTimeDays - 30);

        // Act
        await RunCleanupAsync();

        // Assert
        TestTariffs.LookupsOf(TenantId).Should().Be(0);
        (await RemainingEventsAsync([inHistory, recent])).Should().BeEquivalentTo([inHistory, recent]);
    }

    [Fact]
    public async Task ClearAsync_OnlyTheNewestEventOutsideHistoryExpired_DoesNotLookUpTheTariff()
    {
        // Arrange: an abandoned portal. Its newest event, which a pass always keeps, is past every life time, and the
        // only other event is part of a history; the portal is picked on every pass with nothing to remove.
        const int roomId = int.MaxValue - 1;

        int inHistory, newest, lookups;
        bool disturbed;

        var deadline = DateTime.UtcNow.AddSeconds(LateEventDeadlineSeconds);

        while (true)
        {
            inHistory = await SeedEventAsync(PaidLifeTimeDays * 3, roomId);
            newest = await SeedEventAsync(PaidLifeTimeDays * 2);

            await RemoveEventsExceptAsync(inHistory, newest);

            var lookupsBefore = TestTariffs.LookupsOf(TenantId);

            // Act
            await RunCleanupAsync();

            lookups = TestTariffs.LookupsOf(TenantId) - lookupsBefore;

            // The services record the portal's registration in the audit trail after the request returns. An event
            // that landed after the arrangement, before the pass reached this portal, is newer than the seeded one
            // and takes its place: the run says nothing, and the case is arranged again until one is undisturbed.
            disturbed = await LateEventArrivedAsync(TenantId, inHistory, newest);

            if (!disturbed || DateTime.UtcNow >= deadline)
            {
                break;
            }
        }

        // Assert
        disturbed.Should().BeFalse("a late event of the portal took the place of the seeded newest one in every run");
        lookups.Should().Be(0, "an event the pass keeps anyway is no reason to look the tariff up");
        (await RemainingEventsAsync([inHistory, newest])).Should().BeEquivalentTo([inHistory, newest]);
    }

    [Fact]
    public async Task ClearAsync_PaidPortal_KeepsEventsUntilPaidLifeTime()
    {
        // Arrange
        TestTariffs.Set(TenantId, TariffState.Paid);

        var pastFreeLifeTime = await SeedEventAsync(FreeLifeTimeDays + 30);
        var expired = await SeedEventAsync(PaidLifeTimeDays + 30);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync([pastFreeLifeTime, expired])).Should().BeEquivalentTo([pastFreeLifeTime]);
    }

    [Theory]
    [InlineData(TariffState.Paid, false, true)]
    [InlineData(TariffState.Delay, false, true)]
    [InlineData(TariffState.Paid, true, false)]
    [InlineData(TariffState.Delay, true, false)]
    [InlineData(TariffState.Trial, false, false)]
    [InlineData(TariffState.NotPaid, false, false)]
    public async Task ClearAsync_Tariff_DecidesWhichRetentionApplies(TariffState state, bool freeQuota, bool keepsPaidRetention)
    {
        // Arrange
        TestTariffs.Set(TenantId, state, freeQuota);

        var pastFreeLifeTime = await SeedEventAsync(FreeLifeTimeDays + 30);

        // Past both life times: removed on every tariff, so a pass that skipped the tenant cannot pass for one that
        // kept the paid retention.
        var pastPaidLifeTime = await SeedEventAsync(PaidLifeTimeDays + 30);

        // Act
        await RunCleanupAsync();

        // Assert
        var remaining = await RemainingEventsAsync([pastFreeLifeTime, pastPaidLifeTime]);

        if (keepsPaidRetention)
        {
            remaining.Should().BeEquivalentTo([pastFreeLifeTime]);
        }
        else
        {
            remaining.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task ClearAsync_PaidAndFreePortal_EachKeepsTheRetentionOfItsOwnTariff()
    {
        // Arrange
        using var freePortal = await Fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        TestTariffs.Set(TenantId, TariffState.Paid);

        var paidPortalEvent = await SeedEventAsync(FreeLifeTimeDays + 30);
        var freePortalEvent = await SeedEventForTenantAsync(freePortal.TenantId, FreeLifeTimeDays + 30);

        // Its last activity, which a pass keeps: the expired event is then not the newest of the portal.
        await SeedEventForTenantAsync(freePortal.TenantId, 0);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync([paidPortalEvent, freePortalEvent])).Should().BeEquivalentTo([paidPortalEvent]);
    }

    [Fact]
    public async Task ClearAsync_EveryEventExpired_KeepsTheLastActivity()
    {
        // Arrange: a portal without the recent event every test portal starts with, whose events, whatever its
        // registration wrote included, are all past every life time.
        using var idlePortal = await Fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        int older, newest;
        bool disturbed;

        var deadline = DateTime.UtcNow.AddSeconds(LateEventDeadlineSeconds);

        while (true)
        {
            older = await SeedEventForTenantAsync(idlePortal.TenantId, PaidLifeTimeDays + 60);
            newest = await SeedEventForTenantAsync(idlePortal.TenantId, PaidLifeTimeDays + 30);

            // Aged past both seeded events, every other event of the portal is older than them.
            await AgeEventsExceptAsync(idlePortal.TenantId, PaidLifeTimeDays * 2, older, newest);

            // Act
            await RunCleanupAsync();

            // The services record the portal's registration in the audit trail after the request returns. An event
            // that landed after the aging, before the pass reached this portal, is newer than the seeded one and
            // takes its place: the run says nothing, and the case is arranged again until one is undisturbed.
            disturbed = await LateEventArrivedAsync(idlePortal.TenantId, older, newest);

            if (!disturbed || DateTime.UtcNow >= deadline)
            {
                break;
            }
        }

        // Assert: the portal retention policy counts from the newest event, and must not fall back to the creation
        // date because the cleanup took every event away. The event itself is checked, not the date of the last
        // activity, which a late event would hold on its own.
        disturbed.Should().BeFalse("a late event of the portal took the place of the seeded newest one in every run");
        (await RemainingEventsAsync([older, newest])).Should().BeEquivalentTo([newest], "the newest event of a portal is its last activity");
    }

    /// <summary>How long a case that a late event of its portal disturbed is arranged again.</summary>
    private const int LateEventDeadlineSeconds = 60;

    /// <summary>
    /// Whether the services wrote an event for <paramref name="tenantId"/> that is newer than the seeded ones: one
    /// dated within the free life time. Both callers have removed or aged every other event of the portal first,
    /// the recent one <see cref="BaseTest"/> seeds included, so such an event can only have been written since.
    /// </summary>
    private async Task<bool> LateEventArrivedAsync(int tenantId, params int[] seeded)
    {
        var recent = DateTime.UtcNow.AddDays(-FreeLifeTimeDays);

        await using var ef = await Fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        return await ef.AuditEvents.AnyAsync(r => r.TenantId == tenantId && !seeded.Contains(r.Id) && r.Date > recent, TestContext.Current.CancellationToken);
    }

    /// <summary>Removes every audit event of this portal but <paramref name="eventIds"/>.</summary>
    private async Task RemoveEventsExceptAsync(params int[] eventIds)
    {
        await using var ef = await Fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        await ef.AuditEvents
            .Where(r => r.TenantId == TenantId && !eventIds.Contains(r.Id))
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Moves every audit event of <paramref name="tenantId"/> but <paramref name="eventIds"/> <paramref name="days"/> days back.</summary>
    private async Task AgeEventsExceptAsync(int tenantId, int days, params int[] eventIds)
    {
        await using var ef = await Fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        await ef.AuditEvents
            .Where(r => r.TenantId == tenantId && !eventIds.Contains(r.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Date, r => r.Date.AddDays(-days)), TestContext.Current.CancellationToken);
    }
}
