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
public class RoomResendPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region POST /files/rooms/{id}/resend - access control

    // Resending room invitations is membership-scoped: only the room owner and members granted
    // RoomManager access may do it. Unlike most endpoints, a DocSpaceAdmin who is NOT a member of
    // the room is not auto-allowed (the same model as reorder).

    [Fact]
    public async Task ResendInvitations_Owner_Resent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithPendingUser();

        // Act & Assert - the call completes without throwing
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id,
            new UserInvitation { ResendAll = true },
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_InvitedRoomManager_Resent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithPendingUser();

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);

        // Act & Assert - the call completes without throwing
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id,
            new UserInvitation { ResendAll = true },
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task ResendInvitations_NotInvitedAdmin_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithPendingUser();

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                room.Id,
                new UserInvitation { ResendAll = true },
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to perform the operation");
    }

    [Fact]
    public async Task ResendInvitations_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithPendingUser();

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                room.Id,
                new UserInvitation { ResendAll = true },
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task ResendInvitations_TerminatedRoomManager_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoomWithPendingUser();

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);
        await TerminateUser(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                room.Id,
                new UserInvitation { ResendAll = true },
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// An invited member may resend invitations only with RoomManager access; every lower level is
    /// forbidden. RoomManager cannot be granted to a User or a Guest, so that pair is excluded.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.InvitedMemberAccessesForTagging), MemberType = typeof(RoomAccessData))]
    public async Task ResendInvitations_InvitedMember_MatchesAccessLevel(EmployeeType employeeType, FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Resend Access {employeeType} {access}");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act & Assert
        if (access == FileShare.RoomManager)
        {
            // The call completes without throwing
            await _roomsApi.ResendEmailInvitationsAsync(
                room.Id,
                new UserInvitation { ResendAll = true },
                TestContext.Current.CancellationToken);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.ResendEmailInvitationsAsync(
                    room.Id,
                    new UserInvitation { ResendAll = true },
                    TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
            exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to perform the operation");
        }
    }

    #endregion
}
