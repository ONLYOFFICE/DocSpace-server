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
/// The read a pass pages through the expired events with, called directly. A page reads at most <c>count</c> events
/// whatever part of them is history, and flags the history ones instead of leaving them out: left out, one page
/// would walk every history event in its way before it found <c>count</c> others, and a pass over a portal with
/// years of retained history would be a single unbounded read. No pass-level test can tell the two apart, since
/// both remove the same events.
/// </summary>
public class AuditEventsPageQueryTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>A room id no portal has: the query only checks that a reference exists.</summary>
    private const int RoomId = int.MaxValue - 1;

    private const int Count = 2;

    [Fact]
    public async Task AuditEventsOlderThanAsync_HistoryBeforeExpiredEvents_ReadsAtMostCountAndFlagsTheHistory()
    {
        // Arrange: oldest first, more history than one page, then the events a pass would remove.
        var inHistory = new List<int>();

        for (var i = 0; i < Count + 1; i++)
        {
            inHistory.Add(await SeedEventAsync(PaidLifeTimeDays * 2 - i, RoomId));
        }

        var outsideHistory = new List<int>();

        for (var i = 0; i < Count; i++)
        {
            outsideHistory.Add(await SeedEventAsync(PaidLifeTimeDays + 30 - i));
        }

        var threshold = DateTime.UtcNow.AddDays(-FreeLifeTimeDays);

        // Act
        var pages = await ReadAllPagesAsync(threshold);

        // Assert
        pages[0].Should().HaveCount(Count).And.OnlyContain(r => r.InHistory, "the first page is history only, and has to stop at count all the same");

        pages.Should().OnlyContain(page => page.Count <= Count);

        pages.SelectMany(page => page).Select(r => (r.Id, r.InHistory)).Should().Equal(
            inHistory.Select(id => (id, true)).Concat(outsideHistory.Select(id => (id, false))),
            "every expired event is read once, oldest first, with its history flag");
    }

    /// <summary>Every page of this portal's events older than <paramref name="threshold"/>, as a pass reads them.</summary>
    private async Task<List<List<AuditEventKey>>> ReadAllPagesAsync(DateTime threshold)
    {
        await using var ef = await Fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var pages = new List<List<AuditEventKey>>();

        var afterDate = DateTime.MinValue;
        var afterId = 0;

        while (true)
        {
            var page = await ef.AuditEventsOlderThanAsync(TenantId, threshold, afterDate, afterId, Count).ToListAsync(TestContext.Current.CancellationToken);

            // A cursor that does not move would read the same page forever; the assertions show what it read instead.
            if (page.Count == 0 || pages.Count > 10)
            {
                return pages;
            }

            pages.Add(page);

            (afterId, afterDate) = (page[^1].Id, page[^1].Date);
        }
    }
}
