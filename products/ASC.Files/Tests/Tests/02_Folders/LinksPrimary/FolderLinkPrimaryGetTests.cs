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
/// GET /files/folder/{id}/link — a folder's primary external link. A room already has an implicit
/// primary link the first time it is requested (get-or-create), same as the file endpoint.
/// </summary>
[Trait("Category", "Folders")]
public class FolderLinkPrimaryGetTests(
    AspireAppFixture fixture)
    : FolderLinkPrimaryTestBase(fixture)
{
    [Fact]
    public async Task GetPrimaryLink_Room_ReturnsExpectedShape()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Room");

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.Access.Should().Be(FileShare.Read);
        link.CanEditInternal.Should().BeTrue();
        link.CanEditDenyDownload.Should().BeTrue();
        link.CanEditExpirationDate.Should().BeTrue();
        link.SharedLink.Should().NotBeNull();
        link.SharedLink.Primary.Should().BeTrue();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.SharedLink.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_SubfolderInRoom_AlsoHasPrimaryExternalLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Subfolder Room");
        var folder = await CreateFolder("Autotest Get Folder Link Subfolder", room.Id);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.Access.Should().Be(FileShare.Read);
        link.SharedLink.Primary.Should().BeTrue();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_RepeatedCalls_ReturnSameLinkId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Idempotent");

        // Act
        var first = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        second.SharedLink.Id.Should().Be(first.SharedLink.Id);
    }

    [Fact]
    public async Task GetPrimaryLink_IdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrimaryLink_NonExistentFolder_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(99999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrimaryLink_NegativeFolderId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(-1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrimaryLink_CountZero_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Count Zero");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, count: 0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetPrimaryLink_StartIndexParameter_DoesNotAffectResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link StartIndex Param");

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, startIndex: 999, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_ReturnedId_IsConsistentWithGetFolderLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link Consistency");

        // Act
        var primary = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var links = (await _foldersApi.GetFolderLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var primaryInList = links.Should().ContainSingle(l => l.SharedLink.Primary == true).Subject;
        primaryInList.SharedLink.Id.Should().Be(primary.SharedLink.Id);
    }

    [Fact]
    public async Task GetPrimaryLink_AfterCreate_ReturnsTheCreatedLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Get Folder Link After Create");

        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read), cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().Be(created.SharedLink.Id);
        link.SharedLink.Primary.Should().BeTrue();
        link.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetPrimaryLink_AfterSetUpdatesTitle_ReturnsUpdatedTitle()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Get Folder Link After Set Title");

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, title: "Updated Link Title"),
            TestContext.Current.CancellationToken);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Title.Should().Be("Updated Link Title");
        link.SharedLink.Id.Should().Be(linkId);
    }

    [Fact]
    public async Task GetPrimaryLink_AfterSetUpdatesDenyDownload_ReturnsUpdatedDenyDownload()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Get Folder Link After Set DenyDownload");

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, denyDownload: true),
            TestContext.Current.CancellationToken);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_AfterSetUpdatesInternalFlag_ReturnsUpdatedInternalFlag()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Get Folder Link After Set Internal");

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, @internal: true),
            TestContext.Current.CancellationToken);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Internal.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryLink_AfterSetSetsPassword_ReflectsPasswordIsSet()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Get Folder Link After Set Password");

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, password: "Qwerty1234!"),
            TestContext.Current.CancellationToken);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_AfterSetSetsExpirationDate_ReflectsExpirationDate()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Get Folder Link After Set Expiration");

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(
                linkId: linkId,
                access: FileShare.Read,
                expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(7) }),
            TestContext.Current.CancellationToken);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ExpirationDate.Should().NotBeNull();
        link.SharedLink.IsExpired.Should().BeFalse();
    }
}
