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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

[Trait("Category", "Rooms")]
public class RoomUpdatePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region PUT /files/rooms/{id} - access control

    [Fact]
    public async Task UpdateRoom_OwnerOwnRoom_Updated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("Updated Room"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Updated Room");
    }

    [Fact]
    public async Task UpdateRoom_DocSpaceAdminOwnRoom_Updated()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Room");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("Updated Room"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Updated Room");
    }

    [Fact]
    public async Task UpdateRoom_DocSpaceAdminForeignRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest("Updated Room"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateRoom_UserWithoutAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest("Updated by User"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateRoom_GuestWithoutAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest("Updated by Guest"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have permission to edit the room");
    }

    [Fact]
    public async Task UpdateRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest("Updated without auth"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// An invited member can update the room only with RoomManager access.
    /// Read / Editing / ContentCreator are not enough (403).
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.UpdateRoomAccesses), MemberType = typeof(RoomAccessData))]
    public async Task UpdateRoom_RoomAdminInvitedWithAccess_MatchesExpectation(FileShare access, int expectedStatus)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Update RoomAdmin {access}");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, access);

        await _filesClient.Authenticate(roomAdmin);

        // Act & Assert
        if (expectedStatus == 200)
        {
            var updated = (await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest($"Updated by RoomAdmin {access}"),
                TestContext.Current.CancellationToken)).Response;

            updated.Title.Should().Be($"Updated by RoomAdmin {access}");
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.UpdateRoomAsync(
                    room.Id,
                    new UpdateRoomRequest($"Updated by RoomAdmin {access}"),
                    TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(expectedStatus);
        }
    }

    [Fact]
    public async Task UpdateRoom_Outsider_LeavesRoomUnchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Isolation Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest("Hijacked"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Isolation Room");
    }

    #endregion
}
