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

namespace ASC.Files.Tests.Tests._03_Rooms.NewItems;

/// <summary>
/// GET /files/rooms/{id}/news — the shape of the response and how the endpoint reacts to rooms
/// that are empty, missing, deleted or archived.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomNewItemsContractTests(
    AspireAppFixture fixture)
    : RoomNewItemsTestBase(fixture)
{
    [Fact]
    public async Task GetNewRoomItems_EmptyRoom_ReturnsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Empty Room");

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        FlattenItems(news).Should().BeEmpty();
    }

    [Fact]
    public async Task GetNewRoomItems_EmptyPrivateRoom_ReturnsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest News Empty Private Room", RoomType.CustomRoom);

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        FlattenItems(news).Should().BeEmpty();
    }

    [Fact]
    public async Task GetNewRoomItems_ReturnedEntries_CarryTheExpectedFields()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Item Shape");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // The member opens the room first, so only what comes after counts as new for them.
        await _filesClient.Authenticate(user);
        await VisitRoom(room.Id);

        await _filesClient.Authenticate(Owner);
        await CreateFile("Autotest News Shape File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(user);
        await PollNewsTitles(room.Id, t => t.Contains("Autotest News Shape File.docx"));
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        var items = FlattenItems(news);
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => !string.IsNullOrEmpty(i.Title));
        items.Should().OnlyContain(i => i.CreatedBy != null);
        items.Should().OnlyContain(i => i.Updated != null);
    }

    [Theory]
    [InlineData(999999999)]
    [InlineData(0)]
    public async Task GetNewRoomItems_UnknownRoomId_NotFound(int roomId)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetNewRoomItemsAsync(roomId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetNewRoomItems_DeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Deleted Room");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetNewRoomItems_ArchivedRoom_StillReadableByOwner()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Archived Room");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }
}
