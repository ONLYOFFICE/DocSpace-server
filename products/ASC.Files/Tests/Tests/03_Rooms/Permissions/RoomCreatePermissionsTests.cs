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
public class RoomCreatePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoom_Owner_Created()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Should().NotBeNull();
        room.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoom_DocSpaceAdmin_Created()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Should().NotBeNull();
        room.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoom_User_Forbidden()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Room", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateRoom_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Room", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to create");
    }

    [Fact]
    public async Task CreateRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Anonymous", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateRoom_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        // Sign the user in while they are still active: the test is about the token going dead once
        // the account is terminated, so it has to be issued before the status change.
        await _filesClient.Authenticate(user);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([user.Id], resendAll: false),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Disabled", roomType: RoomType.CustomRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #region POST /files/rooms - private room access control

    [Fact]
    public async Task CreatePrivateRoom_Owner_Created()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var room = await CreatePrivateRoom("Autotest Private Room", RoomType.CustomRoom);

        // Assert
        room.Should().NotBeNull();
        room.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreatePrivateRoom_DocSpaceAdmin_Created()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var room = await CreatePrivateRoom("Autotest Private Room", RoomType.CustomRoom);

        // Assert
        room.Should().NotBeNull();
        room.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreatePrivateRoom_RoomAdmin_Created()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var room = await CreatePrivateRoom("Autotest Private Room", RoomType.CustomRoom);

        // Assert
        room.Should().NotBeNull();
        room.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreatePrivateRoom_User_Forbidden()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // A user may set their own keys but still cannot create rooms.
        await EnsureEncryptionKeys();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Private Room", roomType: RoomType.CustomRoom, @private: true),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreatePrivateRoom_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () =>
            {
                await EnsureEncryptionKeys();
                await _roomsApi.CreateRoomAsync(
                    new CreateRoomRequestDto("Autotest Private Room", roomType: RoomType.CustomRoom, @private: true),
                    TestContext.Current.CancellationToken);
            });

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreatePrivateRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Private Anonymous", roomType: RoomType.CustomRoom, @private: true),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
