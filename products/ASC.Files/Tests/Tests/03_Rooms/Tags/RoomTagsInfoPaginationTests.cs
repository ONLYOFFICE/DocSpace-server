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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// GET /files/tags (getRoomTagsInfo) — <c>count</c>/<c>startIndex</c> pagination, input
/// validation, garbage collection of unused tags and scope isolation. Contract shape and
/// <c>filterValue</c> filtering live in <see cref="RoomTagsInfoTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagsInfoPaginationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetRoomTagsInfo_CountLimitsReturnedTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        foreach (var name in new[] { "PageA", "PageB", "PageC", "PageD" })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        var tags = await GetTagCatalog(count: 2);

        // Assert
        tags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRoomTagsInfo_StartIndexSkipsFirstItems()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        foreach (var name in new[] { "SkipA", "SkipB", "SkipC" })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        var full = await GetTagCatalog();
        var skipped = await GetTagCatalog(startIndex: 1);

        // Assert
        skipped.Should().Equal(full.Skip(1));
    }

    [Fact]
    public async Task GetRoomTagsInfo_CountAndStartIndex_ReturnsExpectedSlice()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        foreach (var name in new[] { "CSA", "CSB", "CSC", "CSD", "CSE" })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        var full = await GetTagCatalog();
        var page = await GetTagCatalog(count: 2, startIndex: 2);

        // Assert
        page.Should().Equal(full.Skip(2).Take(2));
    }

    [Fact]
    public async Task GetRoomTagsInfo_StartIndexBeyondTotal_ReturnsEmptyArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("OnlyTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog(startIndex: 999999);

        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomTagsInfo_CountZero_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("ZeroCountTag"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTagsInfoAsync(count: 0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomTagsInfo_NegativeCount_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("NegCountTag"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTagsInfoAsync(count: -1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// <c>StartIndex</c> carries no <c>[Range]</c> validation on the server
    /// (<c>GetTagsInfoRequestDto</c>), unlike <c>Count</c> — a negative value is silently
    /// accepted instead of being rejected with 400.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81792")]
    public async Task GetRoomTagsInfo_NegativeStartIndex_ShouldReturnBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("NegStartTag"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTagsInfoAsync(startIndex: -1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // The typed signature accepts only int? for count/startIndex, so a non-numeric value cannot
    // be produced through the SDK and has to be sent over raw HTTP.

    [Fact]
    public async Task GetRoomTagsInfo_NonNumericCount_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await GetRoomTagsInfoRaw("count=abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRoomTagsInfo_NonNumericStartIndex_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await GetRoomTagsInfoRaw("startIndex=abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRoomTagsInfo_VeryLargeCount_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("BigCountTag"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTagsInfoAsync(count: 100000, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomTagsInfo_TagSurvivesUntilLastLinkedRoomDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "TwoStepGC";

        var room1 = await CreateCustomRoom("Autotest Two-Step GC Room 1");
        var room2 = await CreateCustomRoom("Autotest Two-Step GC Room 2");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomAsync(room1.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert — still referenced by room2.
        var afterFirst = await GetTagCatalog();
        afterFirst.Should().Contain(name);

        await _roomsApi.DeleteRoomAsync(room2.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert — last room gone, tag garbage-collected.
        var afterSecond = await PollTagCatalog(tags => !tags.Contains(name));
        afterSecond.Should().NotContain(name);
    }

    [Fact]
    public async Task GetRoomTagsInfo_TagRemainsAfterDetachFromOnlyRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "DetachKeepCatalog";
        var room = await CreateCustomRoom("Autotest Detach Keep Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        var beforeDetach = await GetTagCatalog();
        beforeDetach.Should().Contain(name);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert — detaching from the only room does not remove the tag from the catalog.
        var afterDetach = await GetTagCatalog();
        afterDetach.Should().Contain(name);
    }

    [Fact]
    public async Task GetRoomTagsInfo_TagAddedToRoom_VisibleAtPortalScope()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "PortalWideTag";
        var room = await CreateCustomRoom("Autotest Portal Scope Room");

        // Act
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().Contain(name);
    }

    [Fact]
    public async Task GetRoomTagsInfo_ResponseContainsOnlyCustomTagNames_NotTitlesOrCoverIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string roomTitle = "Autotest Unique Room Title For Tag Scope";
        const string tagName = "ScopeOnlyCustomTag";

        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom(roomTitle);
        var coverId = await GetFirstCoverId();

        // Act
        var tags = await GetTagCatalog();

        // Assert
        tags.Should().Contain(tagName);
        tags.Should().NotContain(roomTitle);
        tags.Should().NotContain(room.Id.ToString());
        tags.Should().NotContain(coverId);
    }

    /// <summary>
    /// Polls the tag catalog until <paramref name="until"/> is satisfied or the deadline passes,
    /// returning the last observed state either way — GC of the catalog runs after the operation
    /// that triggered it returns.
    /// </summary>
    private async Task<List<string>> PollTagCatalog(Func<List<string>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var tags = await GetTagCatalog();

            if (until(tags) || DateTime.UtcNow >= deadline)
            {
                return tags;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Reads the tag catalog and unwraps it into plain strings.</summary>
    private async Task<List<string>> GetTagCatalog(int? count = null, int? startIndex = null, string? filterValue = null)
    {
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(count, startIndex, filterValue, TestContext.Current.CancellationToken)).Response;

        return tags.ConvertAll(t => t.ToString()!);
    }

    /// <summary>
    /// Sends a raw GET /api/2.0/files/tags with an arbitrary query string, for query values the
    /// typed <c>int?</c> parameters cannot express (a non-numeric <c>count</c>/<c>startIndex</c>).
    /// </summary>
    private async Task<HttpResponseMessage> GetRoomTagsInfoRaw(string query)
    {
        return await _filesClient.GetAsync($"api/2.0/files/tags?{query}", TestContext.Current.CancellationToken);
    }
}
