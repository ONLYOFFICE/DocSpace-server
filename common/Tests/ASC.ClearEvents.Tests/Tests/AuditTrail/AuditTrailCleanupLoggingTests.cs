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
    public async Task ClearAsync_CancelledBetweenPages_StillLogsWhatWasRemoved()
    {
        // Arrange: one event per page, so the pass pauses after each one it removes.
        const int pages = 4;

        var expired = new List<int>();

        for (var i = 0; i < pages; i++)
        {
            expired.Add(await SeedEventAsync(PaidLifeTimeDays * 2 - i));
        }

        var logger = new RecordingLogger<AuditTrailCleaner>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        // Act: stop the pass in the pause that follows the first page of this portal. The pass walks every portal of
        // the database before this one, a page and a pause for each event of history the other tests left behind, so
        // how long it takes to get here grows with the suite: no deadline of its own but the run's, which cancels
        // the delay below and fails the test with it.
        var pass = Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(batchSize: 1), cts.Token);

        while ((await RemainingEventsAsync(expired)).Count == pages && !pass.IsCompleted)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        await cts.CancelAsync();

        var run = async () => await pass;

        // Assert
        await run.Should().ThrowAsync<OperationCanceledException>();

        var removed = pages - (await RemainingEventsAsync(expired)).Count;

        removed.Should().BeInRange(1, pages - 1, "the pass has to be stopped after it removed some of the events and before it removed them all");

        TenantCount(logger).Should().Be(removed);
        PassCount(logger).Should().BeGreaterThanOrEqualTo(removed);
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
}
