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
/// The <c>clearEvents:auditTrail</c> settings that stop a pass before it deletes anything: the cleanup switched
/// off, which is the default, and values that would make it delete what it must not — a life time of a few days
/// is almost the whole audit trail — or stop the host it runs in.
/// </summary>
public class AuditTrailCleanupSettingsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int MinLifeTimeDays = AuditTrailRetentionConfiguration.MinLifeTimeDays;
    private const int MaxLifeTimeDays = AuditTrailRetentionConfiguration.MaxLifeTimeDays;
    private const int MaxBatchSize = AuditTrailRetentionConfiguration.MaxBatchSize;

    private const int DayMinutes = 24 * 60;

    /// <summary>Just under the shortest period between passes (an hour).</summary>
    private const int BelowMinPeriodMinutes = 59;

    /// <summary>Just past the longest delay Task.Delay accepts (about 49.7 days).</summary>
    private const int BeyondMaxPeriodMinutes = 50 * DayMinutes;

    [Fact]
    public async Task ClearAsync_Disabled_RemovesNothing()
    {
        // Arrange
        var events = new[]
        {
            await SeedEventAsync(FreeLifeTimeDays + 30),
            await SeedEventAsync(PaidLifeTimeDays + 30)
        };

        // Act
        await RunCleanupAsync(Retention(enabled: false));

        // Assert
        (await RemainingEventsAsync(events)).Should().BeEquivalentTo(events);
    }

    [Fact]
    public void Defaults_CleanupIsDisabled()
    {
        new AuditTrailRetentionConfiguration().Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task ClearAsync_LifeTimeAtTheMinimum_IsUsed()
    {
        // Arrange
        var expired = await SeedEventAsync(MinLifeTimeDays + 30);
        var recent = await SeedEventAsync(MinLifeTimeDays - 30);

        // Act
        await RunCleanupAsync(Retention(freeLifeTimeDays: MinLifeTimeDays, paidLifeTimeDays: MinLifeTimeDays));

        // Assert
        (await RemainingEventsAsync([expired, recent])).Should().BeEquivalentTo([recent]);
    }

    [Fact]
    public async Task ClearAsync_LifeTimeAtTheMaximum_IsUsed()
    {
        // Arrange: a paid life time at the maximum must neither be refused nor fail the date arithmetic of the pass.
        var expired = await SeedEventAsync(FreeLifeTimeDays + 30);

        // Act
        await RunCleanupAsync(Retention(paidLifeTimeDays: MaxLifeTimeDays));

        // Assert
        (await RemainingEventsAsync([expired])).Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_BatchSizeAtTheMaximum_IsUsed()
    {
        // Arrange
        var expired = await SeedEventAsync(FreeLifeTimeDays + 30);

        // Act
        await RunCleanupAsync(Retention(batchSize: MaxBatchSize));

        // Assert
        (await RemainingEventsAsync([expired])).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, PaidLifeTimeDays, 1000, DayMinutes)]
    [InlineData(-1, PaidLifeTimeDays, 1000, DayMinutes)]
    [InlineData(1, PaidLifeTimeDays, 1000, DayMinutes)]
    [InlineData(MinLifeTimeDays - 1, PaidLifeTimeDays, 1000, DayMinutes)]
    [InlineData(FreeLifeTimeDays, 0, 1000, DayMinutes)]
    [InlineData(PaidLifeTimeDays, FreeLifeTimeDays, 1000, DayMinutes)]
    [InlineData(FreeLifeTimeDays, MaxLifeTimeDays + 1, 1000, DayMinutes)]
    [InlineData(FreeLifeTimeDays, 1_000_000, 1000, DayMinutes)]
    [InlineData(FreeLifeTimeDays, PaidLifeTimeDays, 0, DayMinutes)]
    [InlineData(FreeLifeTimeDays, PaidLifeTimeDays, MaxBatchSize + 1, DayMinutes)]
    [InlineData(FreeLifeTimeDays, PaidLifeTimeDays, 1000, 0)]
    [InlineData(FreeLifeTimeDays, PaidLifeTimeDays, 1000, BelowMinPeriodMinutes)]
    [InlineData(FreeLifeTimeDays, PaidLifeTimeDays, 1000, BeyondMaxPeriodMinutes)]
    public async Task ClearAsync_UnusableSettings_RemovesNothing(int freeLifeTimeDays, int paidLifeTimeDays, int batchSize, int periodMinutes)
    {
        // Arrange: older than every life time in the rows, so any of them that ran would remove it.
        var expired = await SeedEventAsync(PaidLifeTimeDays + 30);

        // Act
        await RunCleanupAsync(Retention(
            freeLifeTimeDays: freeLifeTimeDays,
            paidLifeTimeDays: paidLifeTimeDays,
            batchSize: batchSize,
            period: TimeSpan.FromMinutes(periodMinutes)));

        // Assert
        (await RemainingEventsAsync([expired])).Should().BeEquivalentTo([expired]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(MaxLifeTimeDays + 1)]
    public async Task ClearAsync_UnusableGracePeriod_RemovesNothing(int paidGracePeriodDays)
    {
        // Arrange
        var expired = await SeedEventAsync(PaidLifeTimeDays + 30);

        // Act
        await RunCleanupAsync(Retention(paidGracePeriodDays: paidGracePeriodDays));

        // Assert
        (await RemainingEventsAsync([expired])).Should().BeEquivalentTo([expired]);
    }

    [Fact]
    public async Task ClearAsync_UnreadableSection_RemovesNothing()
    {
        // Arrange
        var expired = await SeedEventAsync(PaidLifeTimeDays + 30);

        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "true"), ("period", "1d")));

        // Act
        await RunCleanupAsync(retention);

        // Assert
        (await RemainingEventsAsync([expired])).Should().BeEquivalentTo([expired]);
    }

    [Fact]
    public async Task ClearAsync_EnabledThatDoesNotConvert_WarnsAndRemovesNothing()
    {
        // Arrange: a switch the binder does not read as a bool, which used to leave the cleanup off without a word.
        var expired = await SeedEventAsync(PaidLifeTimeDays + 30);

        var retention = AuditTrailRetentionConfiguration.Read(RetentionSection.Of(("enabled", "1")));
        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        await Fixture.Cleaner.CreateCleaner(logger).ClearAsync(retention, TestContext.Current.CancellationToken);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Has("sectionName"));
        (await RemainingEventsAsync([expired])).Should().BeEquivalentTo([expired]);
    }
}
