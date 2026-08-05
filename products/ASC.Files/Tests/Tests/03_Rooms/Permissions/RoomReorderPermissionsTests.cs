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
public class RoomReorderPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    // Reordering the room index is a room-management action: only the room owner or a member invited
    // with management access (RoomManager) may run it. Unlike PUT /archive (which a DocSpaceAdmin may
    // run on any room), reorder is membership-scoped — a portal admin who is not a member of the room
    // gets the same 403 as any other non-member.

    [Fact]
    public async Task ReorderRoom_OwnerOwnRoom_Reordered()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Perm Owner");

        // Act
        var reordered = (await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        reordered.Id.Should().Be(room.Id);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task ReorderRoom_NotInvitedAdmin_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom($"Autotest Reorder Perm {employeeType}");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Only management access (RoomManager) lets an invited RoomAdmin reorder the index.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.AllRoomAccesses), MemberType = typeof(RoomAccessData))]
    public async Task ReorderRoom_InvitedRoomAdmin_MatchesAccessLevel(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom($"Autotest Reorder Perm Invited {access}");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, access);

        await _filesClient.Authenticate(roomAdmin);

        // Act & Assert
        if (access == FileShare.RoomManager)
        {
            var reordered = (await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
            reordered.Id.Should().Be(room.Id);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
        }
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ReorderRoom_InvitedUserOrGuest_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom($"Autotest Reorder Perm {employeeType}");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ReorderRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Perm Anonymous");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
