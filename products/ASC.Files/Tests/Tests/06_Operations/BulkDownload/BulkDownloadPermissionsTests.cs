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

namespace ASC.Files.Tests.Tests._06_Operations.BulkDownload;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/bulkdownload</c> — access control. Downloading a file from a
/// <see cref="RoomType.CustomRoom"/> only needs read access to the file, so every access level in
/// <c>FileSecurity.AvailableRoomAccesses</c> down to <see cref="FileShare.Read"/> — including a
/// guest — succeeds. Functional coverage of the endpoint itself lives in
/// <see cref="BulkDownloadTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BulkDownloadPermissionsTests(
    AspireAppFixture fixture)
    : BulkDownloadTestBase(fixture)
{
    [Fact]
    public async Task BulkDownload_Owner_FromRoom_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload Owner Room");
        var file = await CreateFile("Autotest BulkDownload Owner File.docx", room.Id);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_RoomAdmin_RoomManagerAccess_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload RoomAdmin Room");
        var file = await CreateFile("Autotest BulkDownload RoomAdmin File.docx", room.Id);

        // FileSecurity.AvailableRoomAccesses only allows FileShare.RoomManager to be granted to a RoomAdmin.
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_User_EditingAccess_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload Editor Room");
        var file = await CreateFile("Autotest BulkDownload Editor File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Editing }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(user);
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_User_ReadAccess_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload Read Room");
        var file = await CreateFile("Autotest BulkDownload Read File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(user);
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_Guest_ReadAccess_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload Guest Room");
        var file = await CreateFile("Autotest BulkDownload Guest File.docx", room.Id);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(guest);
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    /// <summary>
    /// BUG 81822: bulkdownload answered 404 instead of 403 for a user with no access to the room.
    /// Fixed by making <c>DownloadPermissionsCheck</c> distinguish entries dropped by the download
    /// filter (403) from missing ones (404).
    /// </summary>
    [Fact]
    [Trait("Bug", "81822")]
    public async Task BulkDownload_UserWithoutRoomMembership_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload No Access Room");
        var file = await CreateFile("Autotest BulkDownload No Access File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// BUG 81823: bulkdownload answered 404 instead of 401 for an unauthenticated caller. Fixed by
    /// the <c>DemandAuthenticatedOrLinkAsync</c> guard in
    /// <c>FileDownloadOperationsManager.Publish</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "81823")]
    public async Task BulkDownload_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest BulkDownload Anon File.docx", Owner);

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
