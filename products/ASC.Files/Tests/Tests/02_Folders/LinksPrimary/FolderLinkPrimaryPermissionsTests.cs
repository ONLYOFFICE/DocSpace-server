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

namespace ASC.Files.Tests.Tests._02_Folders.LinksPrimary;

/// <summary>
/// Access control for setting (PUT /folder/{id}/links) and reading (GET /folder/{id}/link) a
/// room's primary external link. Setting requires <see cref="FileShare.RoomManager"/>, which
/// <c>FileSecurity.AvailableRoomAccesses</c> only allows a <see cref="EmployeeType.RoomAdmin"/> or
/// <see cref="EmployeeType.DocSpaceAdmin"/> to hold in a <see cref="RoomType.CustomRoom"/>; reading
/// is open to anyone with at least <see cref="FileShare.RoomManager"/> as well, per the product.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Folders")]
public class FolderLinkPrimaryPermissionsTests(
    AspireAppFixture fixture)
    : FolderLinkPrimaryTestBase(fixture)
{
    [Fact]
    public async Task SetPrimaryLink_Owner_Returns200()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Owner Perm");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SharedLink.Id.Should().Be(linkId);
    }

    [Fact]
    public async Task SetPrimaryLink_DocSpaceAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Admin Perm");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);
        await _filesClient.Authenticate(admin);

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPrimaryLink_RoomAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link RoomAdmin Perm");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPrimaryLink_UserWithContentCreatorAccess_Returns403()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link ContentCreator Perm");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetPrimaryLink_UserWithReadAccess_Returns403()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link User Read Perm");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// A guest is a signed-in identity, so a refusal is 403, not the 401 the TypeScript suite
    /// expects — 401 is reserved for a caller with no credentials at all, which
    /// <see cref="SetPrimaryLink_Anonymous_Returns401"/> covers.
    /// </summary>
    [Fact]
    public async Task SetPrimaryLink_Guest_Returns403()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Guest Perm");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetPrimaryLink_Anonymous_Returns401()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Anon Perm");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetPrimaryLink_RoomAdminWithoutRoomAccess_Returns403()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link RoomAdmin NoAccess");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // NOTE: the TypeScript suite also has "User with RoomManager access cannot set primary external
    // link returns 403", inviting a plain User into the room with FileShare.RoomManager. That access
    // level is not one FileSecurity.AvailableRoomAccesses permits for SubjectType.User in a
    // CustomRoom (RoomManager may only be granted to a RoomAdmin), so the invitation itself would be
    // rejected in Arrange. Dropped as impossible by construction; the intent - "a plain User cannot
    // manage the room's primary link no matter how it is invited" - is already covered by the
    // ContentCreator and Read cases above.

    [Fact]
    public async Task GetPrimaryLink_Owner_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Owner Perm");

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_DocSpaceAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Admin Perm");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);
        await _filesClient.Authenticate(admin);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_RoomAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link RoomAdmin Perm");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_UserWithReadAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link User Read Perm");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <remarks>
    /// BUG 81571: an unauthenticated caller should get 401 Unauthorized, the same as the set-link
    /// endpoint and the equivalent file endpoint. The API currently returns 403 here instead - the TS
    /// suite ported this as the expected behaviour, but that is the bug, not the contract.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81571")]
    public async Task GetPrimaryLink_Anonymous_ShouldBeUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Anon");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetPrimaryLink_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link No Access");

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
