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
/// GET /files/rooms/{id}/news — access control. Reading the new items of a room needs membership:
/// any access level will do, but a non-member gets 403 even if they are a RoomAdmin. A portal
/// admin is the exception and sees any room.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomNewItemsPermissionsTests(
    AspireAppFixture fixture)
    : RoomNewItemsTestBase(fixture)
{
    [Fact]
    public async Task GetNewRoomItems_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room News Anon");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetNewRoomItems_NonMember_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room News {employeeType} No Access");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>Every access level a User can hold is enough to read the room news.</summary>
    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.ContentCreator)]
    public async Task GetNewRoomItems_InvitedUser_ReturnsNews(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room News User {access}");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, access);

        await _filesClient.Authenticate(user);

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNewRoomItems_InvitedGuest_ReturnsNews()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room News Guest Read");

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNewRoomItems_InvitedRoomManager_ReturnsNews()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room News RoomAdmin");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNewRoomItems_DocSpaceAdminForeignRoom_ReturnsNews()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room News DocSpaceAdmin");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var news = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }
}
