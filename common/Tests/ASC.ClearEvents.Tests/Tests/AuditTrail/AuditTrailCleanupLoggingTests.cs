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
/// What a pass logs about the events it removed. Each page is committed as it is deleted, so the count has to reach
/// the log however the pass ends, not only when it runs to completion.
/// </summary>
public class AuditTrailCleanupLoggingTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ClearAsync_Completed_LogsTheTenantAndThePassCounts()
    {
        // Arrange
        var expired = new[] { await SeedEventAsync(FreeLifeTimeDays + 30), await SeedEventAsync(FreeLifeTimeDays + 31) };

        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        await Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(), TestContext.Current.CancellationToken);

        // Assert
        (await RemainingEventsAsync(expired)).Should().BeEmpty();

        TenantCount(logger).Should().Be(expired.Length);
        PassCount(logger).Should().BeGreaterThanOrEqualTo(expired.Length);
    }

    [Fact]
    public async Task ClearAsync_CancelledBetweenBatches_StillLogsWhatWasRemoved()
    {
        // Arrange: one event per batch, so the pass pauses after each one it removes.
        const int batches = 4;

        var expired = new List<int>();

        for (var i = 0; i < batches; i++)
        {
            expired.Add(await SeedEventAsync(PaidLifeTimeDays * 2 - i));
        }

        var logger = new RecordingLogger<AuditTrailCleaner>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        // Act: stop the pass in the pause that follows the first batch of this portal. The pass walks every portal of
        // the database before this one, through the history the other tests left behind, so how long it takes to get
        // here grows with the suite: no deadline of its own but the run's, which cancels the delay below and fails
        // the test with it.
        var pass = Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(batchSize: 1), cts.Token);

        while ((await RemainingEventsAsync(expired)).Count == batches && !pass.IsCompleted)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await cts.CancelAsync();

        var run = async () => await pass;

        // Assert
        await run.Should().ThrowAsync<OperationCanceledException>();

        var removed = batches - (await RemainingEventsAsync(expired)).Count;

        removed.Should().BeInRange(1, batches - 1, "the pass has to be stopped after it removed some of the events and before it removed them all");

        TenantCount(logger).Should().Be(removed);
        PassCount(logger).Should().BeGreaterThanOrEqualTo(removed);

        logger.Entries.Should().ContainSingle(e => e.Has("walkedTenantCount")).Which.Values["stopped"].Should().Be(true, "the pass gave up on the stop");
    }

    [Fact]
    public async Task ClearAsync_StoppedBeforeTheFirstTenant_LogsAStoppedPassWithNoTenant()
    {
        // Arrange
        var logger = new RecordingLogger<AuditTrailCleaner>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await cts.CancelAsync();

        // Act
        var run = async () => await Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(), cts.Token);

        // Assert: not the line of a pass that walked tenants, with a slowest tenant 0 that does not exist.
        await run.Should().ThrowAsync<OperationCanceledException>();

        var pass = logger.Entries.Should().ContainSingle(e => e.Has("stopped")).Subject;

        pass.Has("walkedTenantCount").Should().BeFalse();
        pass.Values["stopped"].Should().Be(true);
    }

    [Fact]
    public async Task ClearAsync_TenantListUnreadable_LogsAFailedPass()
    {
        // Arrange: the database cannot be reached for the list of tenants.
        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        var run = async () => await new AuditTrailCleaner(logger, new UnreachableDatabase()).ClearAsync(Retention(), TestContext.Current.CancellationToken);

        // Assert: the failure reaches the hosting loop, and the summary does not read as a pass in which nothing
        // qualified.
        await run.Should().ThrowAsync<InvalidOperationException>();

        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Has("elapsed"), "the summary of a failed pass is a warning");
        logger.Entries.Should().NotContain(e => e.Has("stopped"), "a failed pass is not logged as a finished one");
    }

    [Fact]
    public async Task ClearAsync_NothingRemovedHere_LogsWhatThePassRead()
    {
        // Arrange: only history past the free life time, which a pass walks on every run and never removes - the
        // cost a pass has to report, since it shows in no removal.
        const int roomId = int.MaxValue - 1;

        await SeedEventAsync(PaidLifeTimeDays * 2, roomId);
        await SeedEventAsync(PaidLifeTimeDays * 2 - 1, roomId);

        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        await Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(), TestContext.Current.CancellationToken);

        // Assert: the pass walks the portals of the other tests too, so only lower bounds hold.
        TenantCount(logger).Should().BeNull();

        var pass = logger.Entries.Should().ContainSingle(e => e.Has("walkedTenantCount")).Subject;

        pass.Level.Should().Be(LogLevel.Information);
        pass.Values["stopped"].Should().Be(false);
        ((int)pass.Values["walkedTenantCount"]!).Should().BeGreaterThanOrEqualTo(1);
        ((int)pass.Values["pageCount"]!).Should().BeGreaterThanOrEqualTo(1);
        ((long)pass.Values["readCount"]!).Should().BeGreaterThanOrEqualTo(2);
        ((int)pass.Values["slowestTenantId"]!).Should().BePositive();
    }

    /// <summary>The count of the line logged for this portal, or null when there is none.</summary>
    private int? TenantCount(RecordingLogger<AuditTrailCleaner> logger)
    {
        return logger.Entries
            .Where(e => e.Has("count") && e.Has("tenantId") && Equals(e.Values["tenantId"], TenantId))
            .Select(e => (int?)e.Values["count"])
            .SingleOrDefault();
    }

    /// <summary>The count of the line that closes the pass, or null when there is none.</summary>
    private static int? PassCount(RecordingLogger<AuditTrailCleaner> logger)
    {
        return logger.Entries
            .Where(e => e.Has("tenantCount"))
            .Select(e => (int?)e.Values["count"])
            .SingleOrDefault();
    }

    /// <summary>A scope factory whose every scope fails, as a pass sees a database it cannot reach.</summary>
    private sealed class UnreachableDatabase : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            throw new InvalidOperationException("The database cannot be reached.");
        }
    }
}
