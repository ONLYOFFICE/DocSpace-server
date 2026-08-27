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

namespace ASC.Files.Tests.Tests._03_Rooms.Listing;

/// <summary>
/// GET /files/rooms - pagination (count/startIndex/total) and sortBy/sortOrder.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomsFolderSortingTests(
    AspireAppFixture fixture)
    : RoomsFolderTestBase(fixture)
{
    [Fact]
    public async Task GetRoomsFolder_Count_LimitsReturnedFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(count: 2, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Folders.Should().HaveCount(2);
        result.Total.Should().Be(ActiveAreaRoomCount);
    }

    [Fact]
    public async Task GetRoomsFolder_StartIndex_SkipsFirstNFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();
        var all = await GetRoomsFolderRawAsync();

        // Act
        var raw = await GetRoomsFolderRawAsync(startIndex: 2);

        // Assert
        raw.Folders.Should().HaveCount(ActiveAreaRoomCount - 2);
        raw.Folders.Select(f => f.Id).Should().Equal(all.Folders.Skip(2).Select(f => f.Id));
    }

    [Fact]
    public async Task GetRoomsFolder_CountAndStartIndex_ReturnsExpectedSlice()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();
        var all = await GetRoomsFolderRawAsync();

        // Act
        var raw = await GetRoomsFolderRawAsync(startIndex: 1, count: 2);

        // Assert
        raw.Folders.Select(f => f.Id).Should().Equal(all.Folders.Skip(1).Take(2).Select(f => f.Id));
    }

    [Fact]
    public async Task GetRoomsFolder_StartIndexBeyondTotal_ReturnsEmptyFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(startIndex: 999, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Folders.Should().BeEmpty();
        result.Total.Should().Be(ActiveAreaRoomCount);
    }

    [Fact]
    public async Task GetRoomsFolder_PaginationMetadata_MatchesRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(
            startIndex: 1, count: 2, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.StartIndex.Should().Be(1);
        result.Count.Should().Be(2);
        result.Total.Should().Be(ActiveAreaRoomCount);
    }

    /// <remarks>
    /// Bug 81809: the listing came back in its default order however it was sorted. The cause was
    /// not an unstable sort — <c>sortBy</c> is parsed into <c>SortedByType</c>, whose member for
    /// sorting by name is <c>AZ</c>, and the <c>title</c> the TypeScript suite sent simply failed to
    /// parse and was dropped on the floor. The parameter is now rejected with 400 when it cannot be
    /// parsed (<c>VirtualRoomsController.GetRoomsFolder</c>), and these tests use the value the API
    /// defines and the client sends.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81809")]
    public async Task GetRoomsFolder_SortByTitleAscending_ReturnsRoomsInTitleOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = "Autotest" + Guid.NewGuid().ToString()[..8];

        foreach (var suffix in (string[])["C", "A", "B"])
        {
            await CreateCustomRoom($"{marker} {suffix}");
            await Task.Delay(1100, TestContext.Current.CancellationToken);
        }

        // Act - filterValue is served from the search index, which is written asynchronously, so
        // poll until all three rooms are indexed rather than racing that write with a bare read.
        var result = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(
                filterValue: marker, sortBy: "AZ", sortOrder: SortOrder.Ascending,
                cancellationToken: TestContext.Current.CancellationToken)).Response,
            page => page.Folders.Count == 3);

        // Assert
        result.Folders.Select(f => f.Title).Should().Equal($"{marker} A", $"{marker} B", $"{marker} C");
    }

    /// <inheritdoc cref="GetRoomsFolder_SortByTitleAscending_ReturnsRoomsInTitleOrder"/>
    [Fact]
    [Trait("Bug", "81809")]
    public async Task GetRoomsFolder_SortByTitleDescending_ReturnsRoomsInReverseTitleOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = "Autotest" + Guid.NewGuid().ToString()[..8];

        foreach (var suffix in (string[])["C", "A", "B"])
        {
            await CreateCustomRoom($"{marker} {suffix}");
            await Task.Delay(1100, TestContext.Current.CancellationToken);
        }

        // Act - filterValue is served from the search index, which is written asynchronously, so
        // poll until all three rooms are indexed rather than racing that write with a bare read.
        var result = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(
                filterValue: marker, sortBy: "AZ", sortOrder: SortOrder.Descending,
                cancellationToken: TestContext.Current.CancellationToken)).Response,
            page => page.Folders.Count == 3);

        // Assert
        result.Folders.Select(f => f.Title).Should().Equal($"{marker} C", $"{marker} B", $"{marker} A");
    }

    [Fact]
    public async Task GetRoomsFolder_SortByCreatedAscending_ReturnsOldestFirst()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = "Autotest" + Guid.NewGuid().ToString()[..8];
        var createdIds = new List<int>();

        foreach (var suffix in (string[])["First", "Second", "Third"])
        {
            var room = await CreateCustomRoom($"{marker} {suffix}");
            createdIds.Add(room.Id);
            await Task.Delay(1100, TestContext.Current.CancellationToken);
        }

        // Act - filterValue is served from the search index, which is written asynchronously, so
        // poll until all three rooms are indexed instead of racing the write with a bare read.
        var raw = await PollAsync(
            () => GetRoomsFolderRawAsync(filterValue: marker, sortBy: "DateAndTimeCreation", sortOrder: SortOrder.Ascending),
            page => page.Folders.Count == createdIds.Count);

        // Assert
        raw.Folders.Select(f => f.Id).Should().Equal(createdIds);
    }

    [Fact]
    public async Task GetRoomsFolder_SortByCreatedDescending_ReturnsNewestFirst()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = "Autotest" + Guid.NewGuid().ToString()[..8];
        var createdIds = new List<int>();

        foreach (var suffix in (string[])["First", "Second", "Third"])
        {
            var room = await CreateCustomRoom($"{marker} {suffix}");
            createdIds.Add(room.Id);
            await Task.Delay(1100, TestContext.Current.CancellationToken);
        }

        // Act - filterValue is served from the search index, which is written asynchronously, so
        // poll until all three rooms are indexed instead of racing the write with a bare read.
        var raw = await PollAsync(
            () => GetRoomsFolderRawAsync(filterValue: marker, sortBy: "DateAndTimeCreation", sortOrder: SortOrder.Descending),
            page => page.Folders.Count == createdIds.Count);

        // Assert
        raw.Folders.Select(f => f.Id).Should().Equal(Enumerable.Reverse(createdIds));
    }
}
