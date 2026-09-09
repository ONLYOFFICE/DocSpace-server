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
public class RoomUnpinPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task UnpinRoom_OwnRoom_Unpinned(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        var room = await CreateCustomRoom($"Autotest Unpin {employeeType?.ToString() ?? "Owner"}");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var unpinned = (await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        unpinned.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_DocSpaceAdminForeignRoomWithoutInvitation_Unpinned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Owner Room For Admin");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var unpinned = (await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        unpinned.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_RoomAdminWithRoomManagerAccess_Unpinned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin RoomAdmin Manager");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var unpinned = (await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        unpinned.Pinned.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UnpinRoom_NonMember_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Unpin NonInvited {employeeType}");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UnpinRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Anonymous");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UnpinRoom_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Terminated");

        var user = await InviteMember(EmployeeType.User);

        // Invite and let the member pin first, then terminate — so the rejection is due to the
        // disabled account, not to a lack of membership or an unpinned room.
        await InviteToRoom(room.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UnpinRoom_FormerMember_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Removed Member");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, user, FileShare.None);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
