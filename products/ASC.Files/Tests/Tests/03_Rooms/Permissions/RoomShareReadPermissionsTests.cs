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

/// <summary>
/// Access control of GET /files/rooms/{id}/share — who may read the access rights of a room.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareReadPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region GET /files/rooms/{id}/share - access control

    [Fact]
    public async Task GetRoomSecurityInfo_Owner_ContainsOwner()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Read Room");

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().Contain(s => s.SharedToUser.Id == Owner.Id);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_InvitedRoomManager_ContainsManager()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Read Room");

        var manager = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().Contain(s => s.SharedToUser.Id == manager.Id);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_InvitedUser_ContainsUser()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Read Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().Contain(s => s.SharedToUser.Id == user.Id);
    }

    /// <summary>
    /// A member of the portal who was never invited into the room must not be able to read its
    /// access rights: the endpoint used to answer 200 with an empty list instead of 403.
    /// </summary>
    [Fact]
    [Trait("Bug", "81787")]
    public async Task GetRoomSecurityInfo_NotInvitedUser_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Read Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(
                room.Id,
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Same as <see cref="GetRoomSecurityInfo_NotInvitedUser_Forbidden"/> for a guest, which used to
    /// get the same empty 200.
    /// </summary>
    [Fact]
    [Trait("Bug", "81788")]
    public async Task GetRoomSecurityInfo_NotInvitedGuest_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Read Room");

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(
                room.Id,
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Read Room");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(
                room.Id,
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
