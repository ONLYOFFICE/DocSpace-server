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

namespace ASC.Files.Tests.Tests._06_Operations.BatchDelete;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/delete</c> — access control. Deleting a file from a
/// <see cref="RoomType.CustomRoom"/> requires <see cref="FileShare.RoomManager"/> or
/// <see cref="FileShare.ContentCreator"/>; <see cref="FileShare.Editing"/> and
/// <see cref="FileShare.Read"/> are not enough, matching <c>FileSecurity.AvailableRoomAccesses</c>.
/// Functional coverage of the endpoint itself lives in <see cref="BatchDeleteTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BatchDeletePermissionsTests(
    AspireAppFixture fixture)
    : BatchDeleteTestBase(fixture)
{
    [Fact]
    public async Task DeleteBatchItems_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Delete Perm Anon File.docx", Owner);

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteBatchItems_Owner_DeletesFileFromRoom_RemovesFromRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Perm Owner Room");
        const string fileName = "Autotest Delete Perm Owner File.docx";
        var file = await CreateFile(fileName, room.Id);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await FolderContainsFileTitleAsync(room.Id, fileName)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBatchItems_RoomAdmin_RoomManagerAccess_DeletesFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Perm RoomAdmin Room");
        const string fileName = "Autotest Delete Perm RoomAdmin File.docx";
        var file = await CreateFile(fileName, room.Id);

        // FileSecurity.AvailableRoomAccesses only allows FileShare.RoomManager to be granted to a RoomAdmin.
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        await _filesClient.Authenticate(Owner);
        (await FolderContainsFileTitleAsync(room.Id, fileName)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBatchItems_UserWithEditingAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Perm Editing Room");
        const string fileName = "Autotest Delete Perm Editing File.docx";
        var file = await CreateFile(fileName, room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Editing }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        // The file must still be in the room -- 403 must not cause deletion.
        await _filesClient.Authenticate(Owner);
        (await FolderContainsFileTitleAsync(room.Id, fileName)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatchItems_UserWithReadAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Perm Read Room");
        const string fileName = "Autotest Delete Perm Read File.docx";
        var file = await CreateFile(fileName, room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        // The file must still be in the room -- 403 must not cause deletion.
        await _filesClient.Authenticate(Owner);
        (await FolderContainsFileTitleAsync(room.Id, fileName)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatchItems_UserNotInvitedToRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Perm NonMember Room");
        const string fileName = "Autotest Delete Perm NonMember File.docx";
        var file = await CreateFile(fileName, room.Id);

        var user = await InviteContact(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        // The file must still be in the room -- 403 must not cause deletion.
        await _filesClient.Authenticate(Owner);
        (await FolderContainsFileTitleAsync(room.Id, fileName)).Should().BeTrue();
    }
}
