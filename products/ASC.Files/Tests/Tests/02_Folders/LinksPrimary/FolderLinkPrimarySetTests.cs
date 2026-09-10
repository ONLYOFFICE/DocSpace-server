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
/// POST /files/folder/{id}/link (create) and PUT /files/folder/{id}/links (update, when the request
/// targets the existing primary link's id) — setting a folder's primary external link.
/// </summary>
[Trait("Category", "Folders")]
public class FolderLinkPrimarySetTests(
    AspireAppFixture fixture)
    : FolderLinkPrimaryTestBase(fixture)
{
    [Fact]
    public async Task SetPrimaryLink_UpdatesAccessOfExistingLink()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Access");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SharedLink.Id.Should().Be(linkId);
        link.Access.Should().Be(FileShare.Read);
        link.SharedLink.Primary.Should().BeTrue();
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
    }

    [Fact]
    public async Task SetPrimaryLink_UpdatedLink_HasSameIdAsOriginal()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Same Id");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().Be(linkId);
    }

    [Fact]
    public async Task SetPrimaryLink_TitleUpdate_IsReflectedInResponse()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Title");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, title: "Updated Title"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task SetPrimaryLink_PasswordUpdate_IsReflectedInResponseAndGet()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Password");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, password: "Secret123!"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Password.Should().NotBeNullOrEmpty();

        var read = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        read.SharedLink.Id.Should().Be(linkId);
        read.SharedLink.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetPrimaryLink_DenyDownloadUpdate_IsReflectedInResponse()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link DenyDownload");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, denyDownload: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task SetPrimaryLink_InternalUpdate_IsReflectedInResponseAndGet()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Internal");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, @internal: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Internal.Should().BeTrue();

        var read = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        read.SharedLink.Id.Should().Be(linkId);
        read.SharedLink.Internal.Should().BeTrue();
    }

    [Fact]
    public async Task SetPrimaryLink_ExpirationDateUpdate_IsReflectedInResponse()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Expiration");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(
                linkId: linkId,
                access: FileShare.Read,
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.IsExpired.Should().BeFalse();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SetPrimaryLink_AccessNoneWithLinkId_DeletesTheLink()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Delete");

        // Act
        var deleted = await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.None), TestContext.Current.CancellationToken);

        // Assert
        deleted.Count.Should().Be(0);
        deleted.Response.Should().BeNull();

        var links = (await _foldersApi.GetFolderLinksAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        links.Should().NotContain(l => l.SharedLink.Id == linkId);
    }

    /// <remarks>
    /// BUG 81807: GET /api/2.0/files/folder/{id}/link behaved as "get or create" — after a folder's
    /// primary link was explicitly deleted via PUT (access: None), GET recreated a brand-new primary
    /// link instead of answering 404. Fixed by writing a <c>TagType.PrimaryLinkRevoked</c> marker on
    /// explicit revocation, which <c>GetPrimaryExternalLinkAsync</c> now honours with a 404.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81807")]
    public async Task GetPrimaryLink_AfterPrimaryLinkOfFolderIsDeleted_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder Link Delete Bug");
        var folder = await CreateFolder("Autotest Subfolder Link Delete", room.Id);

        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            folder.Id, new FolderLinkRequest(access: FileShare.Read), cancellationToken: TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            folder.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.None), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetPrimaryLink_NonExistentFolderId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                999999999, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetPrimaryLink_FolderIdZero_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                0, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetPrimaryLink_NegativeFolderId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                -1, new FolderLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetPrimaryLink_ReadWriteAccess_Returns403()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link ReadWrite");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SetFolderPrimaryExternalLinkAsync(
                room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.ReadWrite), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetPrimaryLink_MultipleFieldsUpdatedAtOnce()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Multi");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, title: "Multi Update Title", password: "Pass123!", denyDownload: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Title.Should().Be("Multi Update Title");
        link.SharedLink.Password.Should().NotBeNullOrEmpty();
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task SetPrimaryLink_DenyDownload_ToggledFromTrueToFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Set Folder Link DenyDownload Toggle");
        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, denyDownload: true), cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.DenyDownload.Should().BeFalse();
    }

    [Fact]
    public async Task SetPrimaryLink_Internal_ToggledFromTrueToFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Set Folder Link Internal Toggle");
        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(access: FileShare.Read, @internal: true), cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, @internal: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Internal.Should().BeFalse();
    }

    [Fact]
    public async Task SetPrimaryLink_PastExpirationDate_IsSilentlyIgnored()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Past Expiry");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id,
            new FolderLinkRequest(
                linkId: linkId,
                access: FileShare.Read,
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ExpirationDate.Should().BeNull();
        link.SharedLink.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task SetPrimaryLink_NonExistentLinkId_CreatesLinkWithThatId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Set Folder Link Bad LinkId");
        var fakeLinkId = new Guid("00000000-0000-0000-0000-000000000001");

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: fakeLinkId, access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().Be(fakeLinkId);
    }

    [Fact]
    public async Task SetPrimaryLink_UpdatePersistsAfterSet()
    {
        // Arrange
        var (room, linkId) = await CreateRoomWithPrimaryLink("Autotest Set Folder Link Persist");

        await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            room.Id, new FolderLinkRequest(linkId: linkId, access: FileShare.Read, title: "Persisted Title"), TestContext.Current.CancellationToken);

        // Act
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().Be(linkId);
        link.SharedLink.Title.Should().Be("Persisted Title");
    }

    [Fact]
    public async Task SetPrimaryLink_OnSubfolder_Updates()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Set Folder Link Subfolder");
        var folder = await CreateFolder("Autotest Subfolder Set Link", room.Id);

        var created = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(
            folder.Id, new FolderLinkRequest(access: FileShare.Read), cancellationToken: TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        // Act
        var link = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            folder.Id,
            new FolderLinkRequest(linkId: linkId, access: FileShare.Read, title: "Subfolder Link Title"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().Be(linkId);
        link.SharedLink.Title.Should().Be("Subfolder Link Title");
    }
}
