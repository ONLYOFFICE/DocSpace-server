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
public class RoomArchivePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region PUT /files/rooms/{id}/archive - access control

    [Fact]
    [Trait("Bug", "80938")]
    public async Task ArchiveRoom_OwnerArchivesRoomCreatedByDocSpaceAdmin_Archived()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // DocSpaceAdmin creates a room and a file inside it
        var room = await CreateCustomRoom("Autotest Owner Room");
        var file = await CreateFile("DocSpaceAdmin Document", room.Id);
        file.Id.Should().BePositive();

        // Act - the owner archives the room
        await _filesClient.Authenticate(Owner);
        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().NotBeNull();
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Fact]
    public async Task ArchiveRoom_DocSpaceAdminOwnRoom_Archived()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Admin Own Room To Archive");

        // Act
        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().NotBeNull();
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ArchiveRoom_ForeignRoom_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Owner Room For {employeeType} Archive");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ArchiveRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Anonymous Archive");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    [Trait("Bug", "81550")]
    public async Task ArchiveRoom_NonExistentRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(999999999, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "81550")]
    public async Task ArchiveRoom_DeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room To Delete Then Archive");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var deleteOperations = await WaitLongOperation();
        deleteOperations.Should().OnlyContain(o => o.Finished);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ArchiveRoom_AlreadyArchived_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Already Archived Room");

        await ArchiveRoom(room.Id);

        // Act
        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().NotBeNull();
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    #endregion

    #region PUT /files/rooms/{id}/unarchive - access control

    // Mirrors the archive matrix: the room owner and any DocSpaceAdmin may restore a room; a plain
    // RoomAdmin/User/Guest who is not the owner cannot, even when invited to the room with a low
    // access level. The call is asynchronous, so successful restores wait for the operation to finish.

    [Fact]
    public async Task UnarchiveRoom_OwnerOwnRoom_Unarchived()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Owner Room To Unarchive");
        await ArchiveRoom(room.Id);

        // Act
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().NotBeNull();
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Fact]
    public async Task UnarchiveRoom_DocSpaceAdminForeignRoom_Unarchived()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Owner Room For Admin Unarchive");
        await ArchiveRoom(room.Id);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().NotBeNull();
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Fact]
    public async Task UnarchiveRoom_DocSpaceAdminOwnRoom_Unarchived()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Admin Own Room To Unarchive");
        await ArchiveRoom(room.Id);

        // Act
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().NotBeNull();
        operations.Should().OnlyContain(o => o.Finished);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UnarchiveRoom_NotInvitedMember_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Owner Room For {employeeType} Unarchive");
        await ArchiveRoom(room.Id);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Editing)]
    public async Task UnarchiveRoom_InvitedUser_Forbidden(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room For {access} User Unarchive");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, access);

        await ArchiveRoom(room.Id);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UnarchiveRoom_MemberRemovedFromRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Removed Member Unarchive");

        var user = await InviteMember(EmployeeType.User);

        // Invite then revoke access (FileShare.None removes the member from the room).
        await InviteToRoom(room.Id, user, FileShare.Editing);
        await InviteToRoom(room.Id, user, FileShare.None);

        await ArchiveRoom(room.Id);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UnarchiveRoom_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Anonymous Unarchive");
        await ArchiveRoom(room.Id);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
