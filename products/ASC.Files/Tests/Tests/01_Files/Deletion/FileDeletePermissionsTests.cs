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

namespace ASC.Files.Tests.Tests._01_Files.Deletion;

/// <summary>
/// <c>DELETE /files/file/{fileId}</c> — access control. Who may delete a file living in a
/// <see cref="RoomType.CustomRoom"/>: the portal owner and a DocSpace admin always can; within the
/// room, only <see cref="FileShare.RoomManager"/> and <see cref="FileShare.ContentCreator"/> (for a
/// file the caller created) allow it, per <c>FileSecurity.AvailableRoomAccesses</c>. Positive/
/// functional coverage of the endpoint itself lives in <see cref="FileDeleteTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileDeletePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteFile_DocSpaceAdmin_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Admin Delete File");
        var file = await CreateFile("Autotest Admin Delete File.docx", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = admin.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(admin);
        var results = (await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotBeNullOrEmpty();
        results[0].Operation.Should().Be(FileOperationType.Delete);
        results[0].Finished.Should().BeTrue();
        results[0].Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteFile_RoomManagerAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Room Manager Delete File");
        var file = await CreateFile("Autotest Room Manager Delete File.docx", room.Id);

        // FileSecurity.AvailableRoomAccesses only allows FileShare.RoomManager to be granted to a RoomAdmin.
        var roomManager = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomManager.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(roomManager);
        var results = (await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotBeNullOrEmpty();
        results[0].Operation.Should().Be(FileOperationType.Delete);
        results[0].Finished.Should().BeTrue();
        results[0].Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteFile_ContentCreatorOwnFile_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For File Owner Delete");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var file = await CreateFile("Autotest User Own Delete File.docx", room.Id);

        // Act
        var results = (await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotBeNullOrEmpty();
        results[0].Operation.Should().Be(FileOperationType.Delete);
        results[0].Finished.Should().BeTrue();
        results[0].Error.Should().BeNullOrEmpty();
    }

    // The TS suite grants FileShare.ReadWrite here, but CustomRoom only accepts ReadWrite for
    // FolderType.USER (My documents) shares, not for room members - FileSecurity.AvailableRoomAccesses
    // has no ReadWrite entry for SubjectType.User on a CustomRoom, so that invitation would fail in
    // Arrange. FileShare.Editing carries the same intent (can edit, cannot delete someone else's file).
    [Fact]
    public async Task DeleteFile_EditingAccessOnSomeoneElsesFile_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Editing Delete File");
        var file = await CreateFile("Autotest Editing Delete File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Editing }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteFile_ReadOnlyAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Read-only Delete File");
        var file = await CreateFile("Autotest Read-only Delete File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteFile_NoRoomAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For No Access Delete File");
        var file = await CreateFile("Autotest No Access Delete File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteFile_Guest_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Guest Delete File");
        var file = await CreateFile("Autotest Guest Delete File.docx", room.Id);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Unauthenticated Delete File");
        var file = await CreateFile("Autotest Unauthenticated Delete File.docx", room.Id);

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
