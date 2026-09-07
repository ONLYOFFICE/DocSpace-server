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

namespace ASC.Files.Tests.Tests._01_Files.Lock;

/// <summary>
/// Access-level and role scenarios of <c>PUT /files/file/:fileId/lock</c> that are not already
/// covered by the theories in <c>FileUpdateTests</c> (RoomManager/ContentCreator granting the
/// same caller rights over their own file, Editing/FillForms/Read denying them). This class only
/// adds the scenarios that vary the *caller's identity* relative to the file: a different employee
/// type, no access at all, an anonymous caller, or unlocking a file someone else locked.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class FileLockPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task LockFile_DocSpaceAdminWithRoomManagerAccess_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Admin Lock File");
        var file = await CreateFile("Autotest Admin Lock File.docx", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = admin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(admin);
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().BeTrue();
    }

    [Fact]
    public async Task LockFile_RoomManagerLocksOwnersFile_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Room Manager Lock File");
        var file = await CreateFile("Autotest Room Manager Lock File.docx", room.Id);

        var roomManager = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = roomManager.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        // Act: the room manager locks a file created (and owned) by the room's owner.
        await _filesClient.Authenticate(roomManager);
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().BeTrue();
    }

    [Fact]
    public async Task LockFile_RegularUserWithContentCreatorLocksOwnFile_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For File Owner Lock");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var file = await CreateFile("Autotest User Own Lock File.docx", room.Id);

        // Act
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().BeTrue();
    }

    [Fact]
    public async Task LockFile_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For No Access Lock File");
        var file = await CreateFile("Autotest No Access Lock File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task LockFile_Guest_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Guest Lock File");
        var file = await CreateFile("Autotest Guest Lock File.docx", room.Id);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task LockFile_Unauthenticated_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Unauthenticated Lock File");
        var file = await CreateFile("Autotest Unauthenticated Lock File.docx", room.Id);

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task LockFile_RoomManagerUnlocksFileLockedByAnotherUser_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Cross-user Unlock");

        var user = await InviteContact(EmployeeType.User);
        var roomManager = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator },
                new RoomInvitation { Id = roomManager.Id, Access = FileShare.RoomManager }
            ]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var file = await CreateFile("Autotest Cross-user Lock File.docx", room.Id);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        // Act: a room manager unlocks a file that a content creator locked.
        await _filesClient.Authenticate(roomManager);
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(false), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().NotBe(true);
    }

    [Fact]
    public async Task LockFile_CreatorCannotUnlockFileLockedByOwner_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Creator Unlock");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var file = await CreateFile("Autotest Creator Unlock File.docx", room.Id);

        await _filesClient.Authenticate(Owner);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        // Act & Assert: the file's own creator cannot unlock it once the portal owner has locked it.
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(file.Id, new LockFileParameters(false), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
