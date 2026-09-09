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

namespace ASC.Files.Tests.Tests._02_Folders.Deletion;

/// <summary>
/// <c>DELETE /files/folder/{folderId}</c> — access control. Who may delete a folder living in a
/// <see cref="RoomType.CustomRoom"/>: the portal owner and a DocSpace admin (granted
/// <see cref="FileShare.RoomManager"/>) always can; within the room, only
/// <see cref="FileShare.RoomManager"/> and <see cref="FileShare.ContentCreator"/> (for a folder the
/// caller created) allow it, per <c>FileSecurity.AvailableRoomAccesses</c>. Positive/functional
/// coverage of the endpoint itself lives in <see cref="FolderDeleteTests"/>.
/// </summary>
/// <remarks>
/// A permitted delete is asserted by the call being accepted, not by the operation array: the
/// endpoint returns whatever <c>GetOperationResults</c> currently holds, and a delete that finished
/// before the response was built is already pruned, so the array is legitimately empty here.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderDeletePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteFolder_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder Anon Delete", myId);

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_RoomAdminNoRoomAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For RoomAdmin Delete");
        var folder = await CreateFolder("Autotest Folder RoomAdmin Delete", room.Id);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        // Act & Assert
        await _filesClient.Authenticate(roomAdmin);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_UserNoRoomAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For User Delete");
        var folder = await CreateFolder("Autotest Folder User Delete", room.Id);

        var user = await InviteContact(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteFolder_DocSpaceAdminRoomManagerAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For DocSpaceAdmin Delete");
        var folder = await CreateFolder("Autotest Folder DocSpaceAdmin Delete", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = admin.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(admin);
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    [Fact]
    public async Task DeleteFolder_ContentCreatorOwnFolder_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For ContentCreator Delete");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var folder = await CreateFolder("Autotest Folder By ContentCreator", room.Id);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    [Fact]
    public async Task DeleteFolder_RoomManagerOwnFolder_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For RoomAdmin Own Delete");

        // FileSecurity.AvailableRoomAccesses only allows FileShare.RoomManager to be granted to a RoomAdmin.
        var roomManager = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomManager.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomManager);
        var folder = await CreateFolder("Autotest Folder By RoomAdmin", room.Id);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    [Fact]
    public async Task DeleteFolder_OwnerDeletesRoomManagersFolder_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Owner Deletes RoomAdmin Folder");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);
        var folder = await CreateFolder("Autotest Folder By RoomAdmin For Owner Delete", room.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_ReadOnlyAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Read Member Delete");
        var folder = await CreateFolder("Autotest Folder Read Member Delete", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_ContentCreatorOnSomeoneElsesFolder_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For ContentCreator Delete Other");
        var folder = await CreateFolder("Autotest Folder ContentCreator Delete Other", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_Guest_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Guest Delete");
        var folder = await CreateFolder("Autotest Folder Guest Delete", room.Id);

        var guest = await InviteGuest();

        // Act & Assert
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
