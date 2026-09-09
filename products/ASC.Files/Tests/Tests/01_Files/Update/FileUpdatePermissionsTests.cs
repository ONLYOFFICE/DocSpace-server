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

namespace ASC.Files.Tests.Tests._01_Files.Update;

/// <summary>
/// Who is allowed to rename a file that lives inside a room, through
/// <c>PUT /files/file/:fileId</c>. Renaming requires either <see cref="FileShare.RoomManager"/>
/// access to the room, or <see cref="FileShare.ContentCreator"/> access to a file the caller
/// created themselves - see <c>FileSecurity.CanAsync</c>, the <c>FilesSecurityActions.Rename</c>
/// case.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileUpdatePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateFile_DocSpaceAdminWithRoomManagerAccess_Updated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Admin Update File");
        var file = await CreateFile("Autotest Admin Update File.docx", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = admin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(admin);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest Admin Updated File" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(file.Id);
        updated.Title.Should().Be("Autotest Admin Updated File.docx");
    }

    /// <summary>
    /// Rename requires file ownership or <see cref="FileShare.RoomManager"/> - the TS suite used
    /// <c>FileShare.ReadWrite</c> here, but that access level is not in
    /// <c>FileSecurity.AvailableRoomAccesses</c> for a custom room and the invitation would fail
    /// in Arrange. <see cref="FileShare.Editing"/> is a valid room access that still lacks rename
    /// rights on a file it does not own, which is the behaviour this test actually covers.
    /// </summary>
    [Fact]
    public async Task UpdateFile_RoomAdminWithEditingAccess_CannotRenameFileTheyDoNotOwn()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Room Admin Update File");
        var file = await CreateFile("Autotest Room Admin Update File.docx", room.Id);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest Room Admin Updated File" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    /// <summary>
    /// Same as above, for a regular <see cref="EmployeeType.User"/> member.
    /// See <see cref="UpdateFile_RoomAdminWithEditingAccess_CannotRenameFileTheyDoNotOwn"/> for
    /// why <see cref="FileShare.Editing"/> replaces the TS suite's invalid <c>ReadWrite</c>.
    /// </summary>
    [Fact]
    public async Task UpdateFile_UserWithEditingAccess_CannotRenameFileTheyDoNotOwn()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For User Update File");
        var file = await CreateFile("Autotest User Update File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest User Updated File" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateFile_UserWithReadAccess_CannotUpdateFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Read-only Update File");
        var file = await CreateFile("Autotest Read-only Update File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest Read-only Renamed" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateFile_UserWithoutRoomAccess_CannotUpdateFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For No Access Update File");
        var file = await CreateFile("Autotest No Access Update File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest No Access Renamed" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateFile_ContentCreatorRenamesTheirOwnFile_Updated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For File Owner Rename");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var file = await CreateFile("Autotest User Own File.docx", room.Id);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest User Own File Renamed" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(file.Id);
        updated.Title.Should().Be("Autotest User Own File Renamed.docx");
    }

    [Fact]
    public async Task UpdateFile_Guest_CannotRenameFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Guest Rename");
        var file = await CreateFile("Autotest Guest Rename File.docx", room.Id);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest Guest Renamed" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateFile_DocSpaceAdminWithoutRoomMembership_CannotRenameFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Admin No Membership Rename");
        var file = await CreateFile("Autotest Admin No Room File.docx", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest Admin No Room Renamed" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    /// <summary>
    /// BUG 80752: an unauthenticated caller was rejected with 403 ("Access denied") instead of 401.
    /// Fixed by the <c>DemandAuthenticatedOrLinkAsync</c> guard in
    /// <c>FileStorageService.FileRenameAsync</c> — no session and no link key now yields 401.
    /// </summary>
    [Trait("Bug", "80752")]
    [Fact]
    public async Task UpdateFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Unauthenticated Update File");
        var file = await CreateFile("Autotest Unauthenticated Update File.docx", room.Id);

        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id, new UpdateFile { Title = "Autotest Anon Renamed" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
