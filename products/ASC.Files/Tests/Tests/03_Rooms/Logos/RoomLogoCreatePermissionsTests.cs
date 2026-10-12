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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// <c>POST /files/rooms/{id}/logo</c> — access control. Applying a logo is room-management: the
/// room owner may do it, and so may a member invited with RoomManager access — but only using a
/// tmpFile they uploaded themselves, since a tmpFile belongs to the account that uploaded it.
/// Everyone else, including a DocSpaceAdmin who is not a member of the room, gets 403.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoCreatePermissionsTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Fact]
    public async Task CreateLogo_Owner_Applied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Owner Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateLogo_ForeignRoomWithoutInvitation_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Logo {employeeType} Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateLogo_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo Anon Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateLogo_InvitedRoomManagerUsingAnotherUsersTmpFile_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Logo {employeeType} RoomManager Room");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, FileShare.RoomManager);

        var tmpFile = await UploadLogo(CreateTestImageBytes()); // uploaded as the owner

        // Act
        await _filesClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateLogo_InvitedRoomManagerUsingOwnTmpFile_Applied(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Logo {employeeType} Own TmpFile");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, FileShare.RoomManager);

        await _filesClient.Authenticate(member);
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateLogo_OwnRoom_Applied(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(employeeType);

        await _filesClient.Authenticate(member);
        var room = await CreateCustomRoom($"Autotest Logo {employeeType} Own Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        var updated = await CreateLogo(room.Id, tmpFile);

        // Assert
        updated.Logo.Original.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// No access level below RoomManager lets an invited user apply a logo. RoomManager itself
    /// cannot be granted to a User, so the highest level a User can reach here is ContentCreator.
    /// </summary>
    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.ContentCreator)]
    public async Task CreateLogo_InvitedUser_Forbidden(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Logo User {access} Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, access);

        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateLogo_UserInAdminRoom_Forbidden(EmployeeType roomOwnerType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomOwner = await InviteMember(roomOwnerType);

        await _filesClient.Authenticate(roomOwner);
        var room = await CreateCustomRoom($"Autotest Logo User In {roomOwnerType} Room");
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateLogo(room.Id, tmpFile));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
