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
public class RoomSharePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region PUT /files/rooms/{id}/share - access control

    [Fact]
    public async Task SetRoomSecurity_OwnerOwnRoom_MembersAdded()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Room");

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(user, FileShare.Editing),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Members.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetRoomSecurity_DocSpaceAdminOwnRoom_MembersAdded()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Share Room");

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(user, FileShare.Editing),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Members.Should().NotBeNull();
    }

    [Fact]
    public async Task SetRoomSecurity_DocSpaceAdminForeignRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Room");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(user, FileShare.Editing),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetRoomSecurity_User_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(user, FileShare.Editing),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetRoomSecurity_Guest_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Room");

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(guest, FileShare.Editing),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to view the folder content");
    }

    [Fact]
    [Trait("Bug", "79020")]
    public async Task SetRoomSecurity_DisabledUserWithNotify_SilentlySkipped()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        await TerminateUser(user);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Room");

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = user.Id }],
                Notify = true,
                Force = true
            },
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Members.Should().BeEmpty();
    }

    [Fact]
    [Trait("Bug", "79361")]
    public async Task SetRoomSecurity_RoomAdminInvitesForeignGuest_SilentlySkipped()
    {
        // Arrange - the guest is created by the owner, so it does not belong to the RoomAdmin
        await _filesClient.Authenticate(Owner);
        var guest = await InviteGuest();
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var room = await CreateCustomRoom("Autotest Room");

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(guest, FileShare.ContentCreator),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task SetRoomSecurity_InvitedRoomManager_MembersAdded()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var target = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Room");

        var manager = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(target, FileShare.Read),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Members.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetRoomSecurity_RoomManagerWithRevokedAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var target = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Room");

        var manager = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);
        await InviteToRoom(room.Id, manager, FileShare.None);

        await _filesClient.Authenticate(manager);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(target, FileShare.Read),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// RoomManager access cannot be granted to a User or a Guest — the request is rejected and the
    /// member must not end up in the room.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetRoomSecurity_GrantRoomManagerToUserOrGuest_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(employeeType);
        var room = await CreateCustomRoom("Autotest Share Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                BuildInvitation(member, FileShare.RoomManager),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotContain(s => s.SharedToUser.Id == member.Id);
    }

    #endregion
}
