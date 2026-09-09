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

namespace ASC.Files.Tests.Tests._06_Operations.CheckMoveCopy;

/// <summary>
/// <c>GET /api/2.0/files/fileops/move</c> (<c>checkMoveOrCopyBatchItems</c>) — access control. Per
/// <c>FileSecurity.AvailableRoomAccesses</c>, a <see cref="RoomType.CustomRoom"/> accepts
/// <see cref="FileShare.RoomManager"/> (RoomAdmin only) and <see cref="FileShare.ContentCreator"/>
/// for a <see cref="EmployeeType.User"/>. The caller needs write access to the destination to check
/// a move/copy into it; owning the source item alone is not enough.
/// </summary>
[Trait("Category", "Operations")]
public class CheckMoveOrCopyPermissionsTests(
    AspireAppFixture fixture)
    : CheckMoveCopyTestBase(fixture)
{
    [Fact]
    public async Task CheckMove_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove Perm Anon.docx", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Perm Anon Room");

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckMoveOrCopyBatch(new BatchRequestDto
            {
                DestFolderId = new(destFolder.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CheckMove_OwnerOwnFile_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove Perm Owner.docx", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Perm Owner Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckMove_DocSpaceAdmin_ReturnsOk()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var adminMyDocsFolderId = await GetUserFolderIdAsync(admin);
        var file = await CreateFile("Autotest CheckMove Perm Admin.docx", adminMyDocsFolderId);

        var destFolder = await CreateCustomRoom("Autotest CheckMove Perm Admin Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckMove_RoomAdminWithRoomManagerAccess_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CheckMove Perm RoomAdmin Room");
        var file = await CreateFile("Autotest CheckMove Perm RoomAdmin File.docx", room.Id);

        // FileSecurity.AvailableRoomAccesses only allows FileShare.RoomManager to be granted to a RoomAdmin.
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        var destRoom = await CreateCustomRoom("Autotest CheckMove Perm RoomAdmin DestRoom");
        await _roomsApi.SetRoomSecurityAsync(
            destRoom.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckMove_UserWithContentCreatorAccess_OwnFileToRoom_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var userMyDocsFolderId = await GetUserFolderIdAsync(user);
        var file = await CreateFile("Autotest CheckMove Perm ContentCreator File.docx", userMyDocsFolderId);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CheckMove Perm ContentCreator Room");
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(user);
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(room.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckMove_UserWithoutRoomAccess_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var userMyDocsFolderId = await GetUserFolderIdAsync(user);
        var file = await CreateFile("Autotest CheckMove Perm NoAccess.docx", userMyDocsFolderId);

        await _filesClient.Authenticate(Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Perm NoAccess Room");

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckMoveOrCopyBatch(new BatchRequestDto
            {
                DestFolderId = new(destFolder.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CheckMove_GuestWithoutRoomAccess_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove Perm Guest NoAccess.docx", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Perm Guest NoAccess Room");

        var guest = await InviteGuest();

        // Act & Assert
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckMoveOrCopyBatch(new BatchRequestDto
            {
                DestFolderId = new(destFolder.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
