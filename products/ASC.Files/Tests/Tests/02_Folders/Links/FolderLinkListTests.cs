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

namespace ASC.Files.Tests.Tests._02_Folders.Links;

/// <summary>
/// GET /files/folder/{id}/links — the list of a folder's external links. Access follows the same
/// contract as the room equivalent (<c>GET /files/rooms/{id}/links</c>, see
/// <see cref="ASC.Files.Tests.Tests._03_Rooms.Links.RoomLinkReadTests"/>): a complete non-member is
/// refused with 403 by <c>FileSharing.CheckAccessAsync</c> before link visibility is even
/// considered, while a member who can read the folder but lacks link-management access
/// (<c>FileSecurity</c> requires exactly <c>FileShare.RoomManager</c>) gets 200 with an empty list.
/// The TypeScript suite asserts 200 + empty for every non-member too; that does not hold in this
/// build, so those two cases were corrected to 403 here.
/// </summary>
[Trait("Category", "Folders")]
public class FolderLinkListTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task GetFolderLinks_OwnerRoom_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Basic");

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFolderLinks_RoomWithNoLinks_ReturnsEmptyArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Empty");

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolderLinks_ReturnsPrimaryExternalLinkAfterItIsCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links After Create");

        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeEmpty();
        var link = result.Response.Single(l => l.SharedLink.Id == linkId);
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetFolderLinks_ReturnedLink_HasCorrectAccessLevel()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Access");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var link = result.Response.Single(l => l.SubjectType == SubjectType.PrimaryExternalLink);
        link.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetFolderLinks_ReturnedLink_HasCorrectPermissionFlagsForOwner()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Flags");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var link = result.Response.Single(l => l.SubjectType == SubjectType.PrimaryExternalLink);
        link.IsLocked.Should().BeFalse();
        link.IsOwner.Should().BeFalse();
        link.CanEditAccess.Should().BeFalse();
        link.CanEditInternal.Should().BeTrue();
        link.CanEditDenyDownload.Should().BeTrue();
        link.CanEditExpirationDate.Should().BeTrue();
        link.CanRevoke.Should().BeFalse();
    }

    [Fact]
    public async Task GetFolderLinks_DenyDownloadFlag_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links DenyDownload");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, denyDownload: true), TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var link = result.Response.Single(l => l.SubjectType == SubjectType.PrimaryExternalLink);
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task GetFolderLinks_PasswordField_IsPresentForPasswordProtectedLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Password");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, password: "Secret123!"), TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var link = result.Response.Single(l => l.SubjectType == SubjectType.PrimaryExternalLink);
        link.SharedLink.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFolderLinks_InternalFlag_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Internal");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, @internal: true), TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var link = result.Response.Single(l => l.SubjectType == SubjectType.PrimaryExternalLink);
        link.SharedLink.Internal.Should().BeTrue();
    }

    [Fact]
    public async Task GetFolderLinks_ExpirationDate_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Expiration");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(
                access: FileShare.Read,
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var link = result.Response.Single(l => l.SubjectType == SubjectType.PrimaryExternalLink);
        link.SharedLink.IsExpired.Should().BeFalse();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFolderLinks_LinkDisappearsAfterItIsRevoked()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Revoke");

        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        var before = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);
        before.Response.Select(l => l.SharedLink.Id).Should().Contain(linkId);

        // Act
        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.None), TestContext.Current.CancellationToken);

        // Assert
        var after = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);
        after.Response.Select(l => l.SharedLink.Id).Should().NotContain(linkId);
    }

    [Fact]
    public async Task GetFolderLinks_WorksForSubfolderInsideARoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Subfolder");
        var folder = await CreateFolder("Autotest Subfolder For Links", room.Id);

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            folder.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(folder.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeEmpty();
        result.Response.Should().Contain(l => l.SubjectType == SubjectType.PrimaryExternalLink);
    }

    [Fact]
    public async Task GetFolderLinks_NonExistentFolder_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderLinksAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFolderLinks_IdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderLinksAsync(0, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFolderLinks_NegativeId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderLinksAsync(-1, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFolderLinks_DocSpaceAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Admin Perm");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);
        await _filesClient.Authenticate(admin);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFolderLinks_RoomAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links RoomAdmin Perm");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFolderLinks_UserWithReadAccess_ReturnsEmptyList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links User Read Perm");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var result = await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    /// <summary>
    /// The TypeScript suite expects 200 with an empty array for a user who was never invited into
    /// the room. The room equivalent of this endpoint answers 403 instead — see
    /// <see cref="ASC.Files.Tests.Tests._03_Rooms.Links.RoomLinkReadTests.GetRoomLinks_UserNotInvited_Forbidden"/> —
    /// so 403 is asserted here too.
    /// </summary>
    [Fact]
    public async Task GetFolderLinks_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links User No Access Perm");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <inheritdoc cref="GetFolderLinks_UserWithoutRoomAccess_Returns403"/>
    [Fact]
    public async Task GetFolderLinks_GuestWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Guest Perm");

        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken);

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderLinks_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Links Anon Perm");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderLinksAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
