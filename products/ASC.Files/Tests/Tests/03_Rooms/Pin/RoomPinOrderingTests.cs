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

namespace ASC.Files.Tests.Tests._03_Rooms.Pin;

/// <summary>
/// <c>PUT /files/rooms/{id}/pin</c> — how pinning affects the order of <c>GET /files/rooms</c>
/// across several rooms: sorting, pagination and filtering. The single-room round trip back to
/// its natural position is already covered by <c>RoomPinOrderTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomPinOrderingTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Fact]
    public async Task PinRoom_PinnedRoomAppearsAboveUnpinnedRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Pin Order A");
        var b = await CreateCustomRoom("Autotest Pin Order B");
        await CreateCustomRoom("Autotest Pin Order C");

        // Act
        await _roomsApi.PinRoomAsync(b.Id, TestContext.Current.CancellationToken);

        // Assert
        var rows = await GetRoomRows();
        var pinnedIndex = rows.FindIndex(r => r.Id == b.Id);
        var firstUnpinnedIndex = rows.FindIndex(r => !r.Pinned);

        rows[pinnedIndex].Pinned.Should().BeTrue();
        pinnedIndex.Should().BeLessThan(firstUnpinnedIndex);
    }

    [Fact]
    public async Task PinRoom_SeveralPinnedRoomsAppearBeforeUnpinnedRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest MultiPin A");
        var b = await CreateCustomRoom("Autotest MultiPin B");
        await CreateCustomRoom("Autotest MultiPin C");
        var d = await CreateCustomRoom("Autotest MultiPin D");

        // Act
        await _roomsApi.PinRoomAsync(b.Id, TestContext.Current.CancellationToken);
        await _roomsApi.PinRoomAsync(d.Id, TestContext.Current.CancellationToken);

        // Assert — the pinned section is a contiguous prefix: no unpinned room precedes a pinned one.
        var rows = await GetRoomRows();
        var pinnedFlags = rows.ConvertAll(r => r.Pinned);
        var lastPinned = pinnedFlags.LastIndexOf(true);
        var firstUnpinned = pinnedFlags.IndexOf(false);

        rows.Count(r => r.Pinned).Should().Be(2);
        lastPinned.Should().BeLessThan(firstUnpinned);
    }

    [Fact]
    public async Task PinRoom_StaysAboveUnpinned_WhenSortingByTitle()
    {
        // Arrange — "Z" would sort last by title ascending, but pinning floats it to the top.
        await _filesClient.Authenticate(Owner);
        var z = await CreateCustomRoom("ZZZ Autotest Pin Sort");
        await CreateCustomRoom("AAA Autotest Pin Sort");
        await CreateCustomRoom("MMM Autotest Pin Sort");

        // Act
        await _roomsApi.PinRoomAsync(z.Id, TestContext.Current.CancellationToken);

        // Assert
        var rows = await GetRoomRows(sortBy: "title", sortOrder: SortOrder.Ascending);
        var zIndex = rows.FindIndex(r => r.Id == z.Id);
        var firstUnpinned = rows.FindIndex(r => !r.Pinned);

        rows[zIndex].Pinned.Should().BeTrue();
        zIndex.Should().BeLessThan(firstUnpinned);
    }

    [Fact]
    public async Task PinRoom_StaysAboveUnpinned_WhenSortingByCreatedDate()
    {
        // Arrange — the oldest room would normally be last with Descending-by-created.
        await _filesClient.Authenticate(Owner);
        var oldest = await CreateCustomRoom("Autotest Pin Created Old");
        await CreateCustomRoom("Autotest Pin Created Mid");
        await CreateCustomRoom("Autotest Pin Created New");

        // Act
        await _roomsApi.PinRoomAsync(oldest.Id, TestContext.Current.CancellationToken);

        // Assert
        var rows = await GetRoomRows(sortBy: "DateAndTime", sortOrder: SortOrder.Descending);
        var oldestIndex = rows.FindIndex(r => r.Id == oldest.Id);
        var firstUnpinned = rows.FindIndex(r => !r.Pinned);

        rows[oldestIndex].Pinned.Should().BeTrue();
        oldestIndex.Should().BeLessThan(firstUnpinned);
    }

    [Fact]
    public async Task PinRoom_MultiplePinnedRooms_HaveStableOrderAcrossCalls()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var b = await CreateCustomRoom("Autotest Pin Stable B");
        var d = await CreateCustomRoom("Autotest Pin Stable D");
        await CreateCustomRoom("Autotest Pin Stable A");

        await _roomsApi.PinRoomAsync(b.Id, TestContext.Current.CancellationToken);
        await _roomsApi.PinRoomAsync(d.Id, TestContext.Current.CancellationToken);

        // Act
        var order1 = (await GetRoomRows()).Where(r => r.Pinned).Select(r => r.Id).ToList();
        var order2 = (await GetRoomRows()).Where(r => r.Pinned).Select(r => r.Id).ToList();

        // Assert
        order1.Should().Equal(order2);
    }

    [Fact]
    public async Task PinRoom_PinnedRoomAppearsOnFirstPage()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var created = new List<int>();
        for (var i = 0; i < 8; i++)
        {
            created.Add((await CreateCustomRoom($"Autotest Pin Page {i}")).Id);
        }

        // Pin the last-created room, then request only the first 3 rooms.
        var pinned = created[^1];
        await _roomsApi.PinRoomAsync(pinned, TestContext.Current.CancellationToken);

        // Act
        var rows = await GetRoomRows(count: 3, startIndex: 0);

        // Assert
        rows.Select(r => r.Id).Should().Contain(pinned);
        rows[0].Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_PaginationOverPinnedAndUnpinned_HasNoDuplicatesOrGaps()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var created = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            created.Add((await CreateCustomRoom($"Autotest Pin Paginate {i}")).Id);
        }

        await _roomsApi.PinRoomAsync(created[1], TestContext.Current.CancellationToken);
        await _roomsApi.PinRoomAsync(created[4], TestContext.Current.CancellationToken);

        // Act
        var page1 = (await GetRoomRows(count: 3, startIndex: 0)).ConvertAll(r => r.Id);
        var page2 = (await GetRoomRows(count: 3, startIndex: 3)).ConvertAll(r => r.Id);

        // Assert
        var all = page1.Concat(page2).ToList();
        all.Distinct().Should().HaveCount(all.Count, "no duplicates across pages");
        created.Should().BeSubsetOf(all, "all created rooms are present across the two pages");
    }

    [Fact]
    public async Task PinRoom_FilteredList_KeepsThePinnedMatchingRoomOnTop()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = $"Mark{Guid.NewGuid().ToString()[..8]}";
        var r1 = await CreateCustomRoom($"{marker} One");
        await CreateCustomRoom($"{marker} Two");
        await CreateCustomRoom($"{marker} Three");

        // Act
        await _roomsApi.PinRoomAsync(r1.Id, TestContext.Current.CancellationToken);

        // Assert — filterValue is served from the search index, so poll until all three rooms
        // have been indexed instead of racing a bare read against it.
        var rows = await GetRoomRowsUntil(r => r.Count >= 3, filterValue: marker);
        rows.Count.Should().BeGreaterThanOrEqualTo(3);

        var r1Index = rows.FindIndex(r => r.Id == r1.Id);
        var firstUnpinned = rows.FindIndex(r => !r.Pinned);

        rows[r1Index].Pinned.Should().BeTrue();
        r1Index.Should().BeLessThan(firstUnpinned);
    }
}
