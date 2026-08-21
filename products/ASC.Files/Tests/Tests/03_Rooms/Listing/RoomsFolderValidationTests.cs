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
/// GET /files/rooms - invalid query values, combining several filters at once, and how the list
/// reacts to writes that happen after it was last read.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomsFolderValidationTests(
    AspireAppFixture fixture)
    : RoomsFolderTestBase(fixture)
{
    [Fact]
    public async Task GetRoomsFolder_InvalidRoomType_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomsFolderAsync(
            type: [(RoomType)999], cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_InvalidSearchArea_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomsFolderAsync(
            searchArea: (SearchArea)999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_NegativeCount_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomsFolderAsync(
            count: -1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_NegativeStartIndex_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomsFolderAsync(
            startIndex: -1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_NonNumericCount_IsRejected()
    {
        // Arrange - count is typed int?, so a non-numeric value can only be sent raw
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await _filesClient.GetAsync("api/2.0/files/rooms?count=abc", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_NonNumericStartIndex_IsRejected()
    {
        // Arrange - startIndex is typed int?, so a non-numeric value can only be sent raw
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await _filesClient.GetAsync("api/2.0/files/rooms?startIndex=abc", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_InvalidSortOrder_IsRejected()
    {
        // Arrange - sortOrder is typed as the SortOrder enum, so an arbitrary string can only be sent raw
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await _filesClient.GetAsync("api/2.0/files/rooms?sortOrder=sideways", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task GetRoomsFolder_InvalidSortBy_IsIgnoredAndReturnsDefaultOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest SortBy Default " + Guid.NewGuid().ToString()[..8]);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(
            sortBy: "thisFieldDoesNotExist", cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Folders.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRoomsFolder_TypeAndFilterValue_ReturnsOnlyMatchingTypeAndTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Combo Custom");
        await CreatePublicRoom("Autotest Combo Public");
        await CreateCustomRoom("Autotest Other");

        // Act - filterValue is served from the search index, so poll instead of reading once.
        var raw = await PollAsync(
            () => GetRoomsFolderRawAsync(type: [RoomType.PublicRoom], filterValue: "Combo"),
            r => r.Folders.Count > 0);

        // Assert
        raw.Folders.Should().ContainSingle();
        raw.Folders[0].Title.Should().Be("Autotest Combo Public");
        raw.Folders[0].RoomType.Should().Be(RoomType.PublicRoom);
    }

    [Fact]
    public async Task GetRoomsFolder_ArchiveAndFilterValue_FindsArchivedRoomByTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest Archived Searchable " + Guid.NewGuid().ToString()[..8];
        var room = await CreateCustomRoom(title);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act - filterValue is served from the search index, so poll instead of reading once.
        var result = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(
                searchArea: SearchArea.Archive, filterValue: title, cancellationToken: TestContext.Current.CancellationToken)).Response,
            r => r.Folders.Any(f => f.Title == title));

        // Assert
        result.Folders.Should().Contain(f => f.Title == title);
    }

    [Fact]
    public async Task GetRoomsFolder_ArchiveAndType_ReturnsOnlyArchivedRoomsOfSelectedType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var vdr = await CreateVDRRoom("Autotest Archive VDR " + Guid.NewGuid().ToString()[..8]);
        var custom = await CreateCustomRoom("Autotest Archive Custom " + Guid.NewGuid().ToString()[..8]);

        foreach (var id in new[] { vdr.Id, custom.Id })
        {
            await _roomsApi.ArchiveRoomAsync(id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
            await WaitLongOperation();
        }

        // Act
        var raw = await GetRoomsFolderRawAsync(searchArea: SearchArea.Archive, type: [RoomType.VirtualDataRoom]);

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(vdr.Id);
        ids.Should().NotContain(custom.Id);
    }

    /// <remarks>
    /// Bug 81808: combining the <c>tags</c> filter with <c>filterValue</c> fails the same way a
    /// bare <c>tags</c> filter does.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81808")]
    public async Task GetRoomsFolder_TagsAndFilterValue_ReturnsOnlyTaggedRoomsMatchingTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tag = "AutotestComboTag" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tag), TestContext.Current.CancellationToken);

        var match = await CreateCustomRoom("Autotest TagCombo Match");
        await _roomsApi.AddRoomTagsAsync(match.Id, new BatchTagsRequestDto([tag]), TestContext.Current.CancellationToken);
        var titleOnly = await CreateCustomRoom("Autotest TagCombo Other");
        var tagOnly = await CreateCustomRoom("Autotest Unrelated");
        await _roomsApi.AddRoomTagsAsync(tagOnly.Id, new BatchTagsRequestDto([tag]), TestContext.Current.CancellationToken);

        // Act
        var raw = await GetRoomsFolderRawAsync(tags: JsonSerializer.Serialize(new[] { tag }), filterValue: "TagCombo");

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(match.Id);
        ids.Should().NotContain(titleOnly.Id);
        ids.Should().NotContain(tagOnly.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_DeletedRoom_IsNotReturnedInActiveList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest To Delete " + Guid.NewGuid().ToString()[..8]);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var raw = await GetRoomsFolderRawAsync();

        // Assert
        raw.Folders.Select(f => f.Id).Should().NotContain(room.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_RoomTitleUpdate_IsReflectedInFilterValueSearch()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = Guid.NewGuid().ToString()[..8];
        var room = await CreateCustomRoom($"Autotest Original {marker}");

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: $"Autotest Renamed {marker}"), TestContext.Current.CancellationToken);

        // Assert - filterValue is served from the search index, so the rename may not be visible
        // the instant the update call returns; poll instead of reading once.
        var newResult = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(
                filterValue: $"Renamed {marker}", cancellationToken: TestContext.Current.CancellationToken)).Response,
            r => r.Folders.Any(f => f.Title == $"Autotest Renamed {marker}"));
        newResult.Folders.Select(f => f.Title).Should().Contain($"Autotest Renamed {marker}");

        var oldTitles = (await _roomsApi.GetRoomsFolderAsync(
            filterValue: $"Original {marker}", cancellationToken: TestContext.Current.CancellationToken)).Response.Folders.Select(f => f.Title);
        oldTitles.Should().NotContain(t => t.Contains(marker) && t.Contains("Original"));
    }

    /// <remarks>
    /// Bug 81808: a tag added after the room already exists is invisible to the same broken
    /// <c>tags</c> filter.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81808")]
    public async Task GetRoomsFolder_RoomTagUpdate_IsReflectedInTagsFilter()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tag = "AutotestLateTag" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tag), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Untagged Then Tagged");

        // Act / Assert - before tagging, the room must not match the tag filter
        var before = await GetRoomsFolderRawAsync(tags: JsonSerializer.Serialize(new[] { tag }));
        before.Folders.Select(f => f.Id).Should().NotContain(room.Id);

        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tag]), TestContext.Current.CancellationToken);

        var after = await GetRoomsFolderRawAsync(tags: JsonSerializer.Serialize(new[] { tag }));
        after.Folders.Select(f => f.Id).Should().Contain(room.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_RepeatedCallsWithSameParams_ReturnStableResults()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var first = (await _roomsApi.GetRoomsFolderAsync(
            sortBy: "title", sortOrder: SortOrder.Ascending, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomsFolderAsync(
            sortBy: "title", sortOrder: SortOrder.Ascending, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        second.Folders.Select(f => f.Title).Should().Equal(first.Folders.Select(f => f.Title));
        second.Total.Should().Be(first.Total);
    }
}
