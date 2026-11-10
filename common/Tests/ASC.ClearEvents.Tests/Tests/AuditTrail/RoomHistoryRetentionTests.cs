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
/// The audit events that make up the history of a file, folder or room — those with a row in
/// <c>files_audit_reference</c> — outlive any retention, and a pass that reads the trail in batches still finds
/// every event outside a history. The rows are written here directly; <see cref="RoomHistoryLifecycleTests"/>
/// has the services write them.
/// </summary>
public class RoomHistoryRetentionTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>A room id no portal has: the cleanup only checks that a reference exists.</summary>
    private const int RoomId = int.MaxValue - 1;

    [Fact]
    public async Task ClearAsync_ExpiredEventInHistory_IsKeptWithItsReference()
    {
        // Arrange
        var inHistory = await SeedEventAsync(PaidLifeTimeDays * 2, RoomId);
        var outsideHistory = await SeedEventAsync(PaidLifeTimeDays * 2);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync([inHistory, outsideHistory])).Should().BeEquivalentTo([inHistory]);
        (await HistoryEventsAsync(RoomId, FolderEntry)).Should().BeEquivalentTo([inHistory]);
    }

    [Fact]
    public async Task ClearAsync_HistoryInterleavedAcrossBatches_RemovesEveryEventOutsideHistory()
    {
        // Arrange: oldest first, every other event in a history, so that every page holds both kinds. A page is read
        // with its history events flagged rather than left out; this guards against a flagged event being deleted
        // with the rest of its page.
        const int batchSize = 2;

        var inHistory = new List<int>();
        var outsideHistory = new List<int>();

        for (var i = 0; i < 5 * batchSize; i++)
        {
            var daysAgo = PaidLifeTimeDays * 2 - i;

            if (i % 2 == 0)
            {
                inHistory.Add(await SeedEventAsync(daysAgo, RoomId));
            }
            else
            {
                outsideHistory.Add(await SeedEventAsync(daysAgo));
            }
        }

        // Act
        await RunCleanupAsync(Retention(batchSize: batchSize, readPageSize: batchSize));

        // Assert
        (await RemainingEventsAsync(inHistory.Concat(outsideHistory))).Should().BeEquivalentTo(inHistory);
    }

    [Fact]
    public async Task ClearAsync_FullPagesOfHistoryFirst_DoNotEndThePass()
    {
        // Arrange: the oldest pages are history only, so they remove nothing; the pass has to page on past them, and
        // a page that removes nothing must not be taken for the end of the expired events.
        const int batchSize = 2;

        var inHistory = new List<int>();
        var outsideHistory = new List<int>();

        for (var i = 0; i < 2 * batchSize; i++)
        {
            inHistory.Add(await SeedEventAsync(PaidLifeTimeDays * 2 - i, RoomId));
        }

        for (var i = 0; i < batchSize + 1; i++)
        {
            outsideHistory.Add(await SeedEventAsync(PaidLifeTimeDays + 30 - i));
        }

        // Act
        await RunCleanupAsync(Retention(batchSize: batchSize, readPageSize: batchSize));

        // Assert
        (await RemainingEventsAsync(inHistory.Concat(outsideHistory))).Should().BeEquivalentTo(inHistory);
    }

    [Fact]
    public async Task ClearAsync_ReadPageLargerThanBatch_ReadsOnePageAndDeletesInBatches()
    {
        // Arrange: history and expired events interleaved, more of the expired ones than one batch deletes, all on
        // one page: a page reads far more than a batch, the history it walks past being most of what it reads.
        const int batchSize = 2;

        var inHistory = new List<int>();
        var outsideHistory = new List<int>();

        for (var i = 0; i < 10; i++)
        {
            var daysAgo = PaidLifeTimeDays * 2 - i;

            if (i % 2 == 0)
            {
                inHistory.Add(await SeedEventAsync(daysAgo, RoomId));
            }
            else
            {
                outsideHistory.Add(await SeedEventAsync(daysAgo));
            }
        }

        var logger = new RecordingLogger<AuditTrailCleaner>();

        // Act
        await Fixture.Cleaner.CreateCleaner(logger).ClearAsync(Retention(batchSize: batchSize, readPageSize: 20), TestContext.Current.CancellationToken);

        // Assert
        (await RemainingEventsAsync(inHistory.Concat(outsideHistory))).Should().BeEquivalentTo(inHistory);

        var tenant = logger.Entries.Should().ContainSingle(e => e.Has("pageCount") && Equals(e.Values.GetValueOrDefault("tenantId"), TenantId)).Subject;

        tenant.Values["count"].Should().Be(outsideHistory.Count);
        tenant.Values["pageCount"].Should().Be(1, "the page reads up to 20 events, the batch deletes up to 2");
    }

    [Fact]
    public async Task ClearAsync_MoreExpiredEventsThanOneBatch_RemovesThemAll()
    {
        // Arrange
        const int batchSize = 2;

        var expired = new List<int>();

        for (var i = 0; i < 3 * batchSize + 1; i++)
        {
            expired.Add(await SeedEventAsync(PaidLifeTimeDays * 2 - i));
        }

        // Act
        await RunCleanupAsync(Retention(batchSize: batchSize, readPageSize: batchSize));

        // Assert
        (await RemainingEventsAsync(expired)).Should().BeEmpty();
    }
}
