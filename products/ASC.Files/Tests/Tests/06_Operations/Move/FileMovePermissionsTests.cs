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

namespace ASC.Files.Tests.Tests._06_Operations.Move;

/// <summary>
/// Who is allowed to call PUT /api/2.0/files/fileops/move, both for who initiates the move and for
/// what access the caller holds on the source and the destination.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMovePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task MoveFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest MoveBatch Perm Anon.docx", myDocsFolderId);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm Anon Room");

        // Act
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task MoveFile_Owner_CanMoveFileFromMyDocsToRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest MoveBatch Perm Owner.docx", myDocsFolderId);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm Owner Room");

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    [Fact]
    public async Task MoveFile_DocSpaceAdmin_CanMoveFileFromMyDocsToRoom()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var adminMyDocsFolderId = await GetUserFolderIdAsync(admin);
        var file = await CreateFile("Autotest MoveBatch Perm Admin.docx", adminMyDocsFolderId);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm Admin Room");

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    [Fact]
    public async Task MoveFile_RoomAdmin_CanMoveFileFromOwnMyDocsToManagedRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm RoomAdminMyDocs Room");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(destRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);
        var roomAdminMyDocsFolderId = await GetUserFolderIdAsync(roomAdmin);
        var file = await CreateFile("Autotest MoveBatch Perm RoomAdminMyDocs File.docx", roomAdminMyDocsFolderId);

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    [Fact]
    public async Task MoveFile_UserWithoutAccessToDestinationRoom_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm NoAccess Room");
        var file = await CreateFile("Autotest MoveBatch Perm NoAccess File.docx", myDocsFolderId);

        var user = await InviteContact(EmployeeType.User);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_Guest_CannotMoveFileToRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm Guest Room");
        var file = await CreateFile("Autotest MoveBatch Perm Guest File.docx", myDocsFolderId);

        var guest = await InviteGuest();

        // Act
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_UserWithEditingRoleInDestRoom_ReturnsForbidden()
    {
        // Arrange - Editing does not carry create/upload permission
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm Editing Room");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(destRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest MoveBatch Perm Editing File.docx", myDocsFolderId);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_UserWithReviewRoleInDestRoom_ReturnsForbidden()
    {
        // Arrange - Review does not carry create/upload permission
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm Review Room");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(destRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Review }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest MoveBatch Perm Review File.docx", myDocsFolderId);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_ContentCreator_CannotMoveFileFromRoomToMyDocs()
    {
        // Arrange - ContentCreator may add files but not remove them from the room
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest MoveBatch Perm CC FromRoom");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(srcRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest MoveBatch Perm CC FromRoom File.docx", srcRoom.Id);

        // Act
        await _filesClient.Authenticate(user);
        var userMyDocsFolderId = await GetUserFolderIdAsync(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(userMyDocsFolderId),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_RoomAdmin_CanMoveFileBetweenTwoRoomsTheyManage()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest MoveBatch Perm RoomAdmin Src");
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm RoomAdmin Dest");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(srcRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);
        await _roomsApi.SetRoomSecurityAsync(destRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);
        var roomAdminMyDocsFolderId = await GetUserFolderIdAsync(roomAdmin);
        var file = await CreateFile("Autotest MoveBatch Perm RoomAdmin File.docx", roomAdminMyDocsFolderId);

        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(srcRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    [Fact]
    public async Task MoveFile_UserWithContentCreatorInDestRoom_CanMoveFileFromMyDocs()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Perm CC Dest Room");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(destRoom.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var userMyDocsFolderId = await GetUserFolderIdAsync(user);
        var file = await CreateFile("Autotest MoveBatch Perm CC File.docx", userMyDocsFolderId);

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    [Fact]
    [Trait("Bug", "65580")]
    public async Task MoveFile_UserCannotMoveARoom()
    {
        // A room folder is not just "another folder" to move - a plain member must never be able
        // to relocate a whole room via the generic move endpoint.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest MoveBatch Perm Room Itself");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var userMyDocsFolderId = await GetUserFolderIdAsync(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(userMyDocsFolderId),
                FolderIds = [new(room.Id)],
                FileIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
