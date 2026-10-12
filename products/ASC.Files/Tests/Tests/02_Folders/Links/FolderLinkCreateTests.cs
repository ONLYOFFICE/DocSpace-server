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
/// POST /files/folder/{id}/link — creates (or re-reads) a folder's primary external link. Unlike
/// the file endpoint, a folder has no implicit primary link until this is called at least once.
/// </summary>
[Trait("Category", "Folders")]
public class FolderLinkCreateTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreatePrimaryLink_OwnerRoom_ReturnsExpectedShape()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Room");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.Should().NotBeNull();
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.IsLocked.Should().BeFalse();
        link.IsOwner.Should().BeFalse();
        link.CanEditAccess.Should().BeFalse();
        link.CanEditInternal.Should().BeTrue();
        link.CanEditDenyDownload.Should().BeTrue();
        link.CanEditExpirationDate.Should().BeTrue();
        link.CanRevoke.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePrimaryLink_ResponseAccessMatchesRequestedAccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Access");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task CreatePrimaryLink_ResponseContainsRequestToken()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Token");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().NotBeEmpty();
        link.SharedLink.RequestToken.Should().NotBeNullOrEmpty();
        link.SharedLink.Primary.Should().BeTrue();
    }

    /// <remarks>
    /// BUG 81573: the requested title is ignored and the server always answers with the generic
    /// "Shared link" title instead.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81573")]
    public async Task CreatePrimaryLink_Title_IsReflectedInResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Title");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, title: "My Public Link"), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Title.Should().Be("My Public Link");
    }

    [Fact]
    public async Task CreatePrimaryLink_DenyDownloadTrue_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link DenyDownload");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, denyDownload: true), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePrimaryLink_WithPassword_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Password");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, password: "Secret123!"), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Password.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// An empty body has no <c>access</c>, which the endpoint treats the same as revoking a
    /// (non-existent) primary link rather than creating one: 200 with no <c>response</c> and
    /// <c>count</c> 0. A deliberate design choice, not a bug — porting it as-is.
    /// </summary>
    [Fact]
    public async Task CreatePrimaryLink_EmptyBody_ActsAsDeleteOfNonExistentLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Empty Body");

        // Act
        var link = await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(), TestContext.Current.CancellationToken);

        // Assert
        link.Count.Should().Be(0);
        link.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreatePrimaryLink_NonExistentFolder_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                999999999, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreatePrimaryLink_FolderIdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                0, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreatePrimaryLink_SubfolderInMyDocuments_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder Link My Docs Subfolder", myFolderId);

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            folder.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePrimaryLink_InternalTrue_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Internal");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, @internal: true), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Internal.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePrimaryLink_ExpirationDate_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Expiration");

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(
                access: FileShare.Read,
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.IsExpired.Should().BeFalse();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePrimaryLink_CalledTwice_ReturnsSameLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Idempotent");

        // Act
        var first = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;
        var second = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        second.SharedLink.Id.Should().Be(first.SharedLink.Id);
    }

    /// <remarks>
    /// BUG 81575: an archived room should refuse the operation with 403; the API currently answers
    /// 200 with <c>count</c> 0 instead of enforcing access here.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81575")]
    public async Task CreatePrimaryLink_ArchivedRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Archived");
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreatePrimaryLink_DocSpaceAdminWithRoomManagerAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Admin Perm");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);
        await _filesClient.Authenticate(admin);

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePrimaryLink_RoomAdminWithRoomManagerAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link RoomAdmin Perm");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var link = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    /// <remarks>
    /// BUG 81575: a member with insufficient access should be refused with 403; the API currently
    /// answers 200 with <c>count</c> 0 instead of enforcing access here.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81575")]
    public async Task CreatePrimaryLink_UserWithReadAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link User Read Perm");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <inheritdoc cref="CreatePrimaryLink_UserWithReadAccess_Returns403"/>
    [Fact]
    [Trait("Bug", "81575")]
    public async Task CreatePrimaryLink_DocSpaceAdminWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Admin No Access Perm");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <inheritdoc cref="CreatePrimaryLink_UserWithReadAccess_Returns403"/>
    [Fact]
    [Trait("Bug", "81575")]
    public async Task CreatePrimaryLink_UserWithContentCreatorAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link User ContentCreator Perm");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <inheritdoc cref="CreatePrimaryLink_UserWithReadAccess_Returns403"/>
    [Fact]
    [Trait("Bug", "81575")]
    public async Task CreatePrimaryLink_GuestWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Guest Perm");

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreatePrimaryLink_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Anon Perm");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
