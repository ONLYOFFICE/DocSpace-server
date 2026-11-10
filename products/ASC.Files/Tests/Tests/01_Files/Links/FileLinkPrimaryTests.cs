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

namespace ASC.Files.Tests.Tests._01_Files.Links;

/// <summary>
/// GET /files/file/{id}/link — a file's primary external link. Unlike the room endpoint, every
/// file already has an implicit primary link the first time it is requested, so a plain read is
/// enough to exercise it; there is no "create the room first" step.
/// </summary>
[Trait("Category", "Files")]
public class FileLinkPrimaryTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPrimaryLink_OwnerFileInMyDocuments_ReturnsExpectedShape()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest External Link File.docx", Owner);

        // Act
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

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
    public async Task GetPrimaryLink_FileInRoom_AlsoHasPrimaryExternalLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For External Link");
        var file = await CreateFile("Autotest Room File External Link.docx", room.Id);

        // Act
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.Primary.Should().BeTrue();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_RepeatedCalls_ReturnSameLinkId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest External Link Idempotent.docx", Owner);

        // Act
        var first = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        second.SharedLink.Id.Should().Be(first.SharedLink.Id);
    }

    [Fact]
    public async Task GetPrimaryLink_Owner_CanGetPrimaryExternalLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest External Link Owner.docx", Owner);

        // Act
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_DocSpaceAdmin_CanGetPrimaryExternalLinkForOwnFile()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var file = await CreateFileInMy("Autotest External Link Admin.docx", admin);

        // Act
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_RoomAdmin_CanGetPrimaryExternalLinkForOwnFile()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var file = await CreateFileInMy("Autotest External Link Room Admin.docx", roomAdmin);

        // Act
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPrimaryLink_User_CanGetPrimaryExternalLinkForOwnFile()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var file = await CreateFileInMy("Autotest External Link User.docx", user);

        // Act
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    /// <remarks>
    /// BUG 81571: an unauthenticated caller got 403 instead of 401 Unauthorized, unlike every other
    /// endpoint in this file (<c>SetFileExternalLink</c>, <c>GetFileLinks</c>). Fixed by the
    /// <c>DemandAuthenticatedOrLinkAsync</c> guard at the top of
    /// <c>FileStorageService.GetPrimaryExternalLinkAsync</c>.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81571")]
    public async Task GetPrimaryLink_Anonymous_ShouldBeUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest External Link Anon.docx", Owner);
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <remarks>
    /// BUG 81572: a user with no access to the file should get 403 Access Denied; the API
    /// currently answers 200 with an empty link list instead of enforcing access here.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81572")]
    public async Task GetPrimaryLink_UserWithoutAccess_AnotherUsersPrivateFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest External Link Other User Private.docx", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <remarks>
    /// BUG 84872: once a file's primary link had been revoked, POST /files/file/{id}/link — the call behind the
    /// "create external link" button — answered 404 for that file from then on, because it shared the read path that
    /// keeps a revoked link from being resurrected (bug 81807). Fixed by letting the explicit create request make a
    /// new primary link despite the revocation mark; the creation clears the mark, so the read sees the new link.
    /// </remarks>
    [Fact]
    [Trait("Bug", "84872")]
    public async Task CreatePrimaryLink_AfterThePrimaryLinkWasRevoked_CreatesItAgain()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Primary Link Recreate");
        var file = await CreateFile("Autotest Primary Link Recreate.docx", room.Id);

        var created = (await _filesApi.CreateFilePrimaryExternalLinkAsync(
            file.Id, new FileLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        await _filesApi.SetFileExternalLinkAsync(
            file.Id, new FileLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None), TestContext.Current.CancellationToken);

        // Act
        var recreated = (await _filesApi.CreateFilePrimaryExternalLinkAsync(
            file.Id, new FileLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Assert
        recreated.SharedLink.Primary.Should().BeTrue();
        recreated.SharedLink.Id.Should().NotBe(created.SharedLink.Id);

        var read = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        read.SharedLink.Id.Should().Be(recreated.SharedLink.Id);
    }
}
