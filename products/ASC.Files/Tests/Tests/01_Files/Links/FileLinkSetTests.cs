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
/// PUT /files/file/{id}/links — creating and updating a file's non-primary and primary external
/// links.
/// </summary>
[Trait("Category", "Files")]
public class FileLinkSetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetFileExternalLink_NonPrimary_CreatesNewLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "My New Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Should().NotBeNull();
        link.SubjectType.Should().Be(SubjectType.ExternalLink);
        link.Access.Should().Be(FileShare.Read);
        link.SharedLink.Should().NotBeNull();
        link.SharedLink.Primary.Should().BeFalse();
        link.SharedLink.Title.Should().Be("My New Link");
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_Primary_CreatesPrimaryLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set Primary External Link.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: true, title: "My Primary Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.PrimaryExternalLink);
        link.SharedLink.Primary.Should().BeTrue();
        link.SharedLink.Title.Should().Be("My Primary Link");
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_CreatedLink_HasCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link Structure.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Structure Check Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SubjectType.Should().Be(SubjectType.ExternalLink);
        link.Access.Should().Be(FileShare.Read);
        link.IsLocked.Should().BeFalse();
        link.SharedLink.Id.Should().NotBeEmpty();
        link.SharedLink.Title.Should().Be("Structure Check Link");
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.Primary.Should().BeFalse();
        link.SharedLink.Internal.Should().BeFalse();
        link.SharedLink.DenyDownload.Should().BeFalse();
        link.SharedLink.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task SetFileExternalLink_DenyDownloadTrue_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link DenyDownload.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "No Download Link", denyDownload: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task SetFileExternalLink_Internal_IsReflected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link Internal.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Internal Link", @internal: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Internal.Should().BeTrue();
    }

    [Fact]
    public async Task SetFileExternalLink_UpdateExistingLink_TitleIsChanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Update External Link.docx", Owner);

        var created = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Original Title"),
            TestContext.Current.CancellationToken)).Response;
        var linkId = created.SharedLink.Id;

        // Act
        var updated = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(linkId: linkId, access: FileShare.Read, primary: false, title: "Updated Title"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.SharedLink.Id.Should().Be(linkId);
        updated.SharedLink.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task SetFileExternalLink_WithExpirationDate_IsSetAndNotExpired()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link Expiration.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(
                access: FileShare.Read,
                primary: false,
                title: "Expiring Link",
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.IsExpired.Should().BeFalse();
        link.SharedLink.ExpirationDate.Should().NotBeNull();
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_WithPassword_ShareLinkIsCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link Password.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Password Protected Link", password: "SecurePass123"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_ReadWriteAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link ReadWrite.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(
                file.Id,
                new FileLinkRequest(access: FileShare.ReadWrite, primary: false, title: "ReadWrite Link"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// A caller-supplied <c>linkId</c> that does not exist yet is treated as an upsert: the server
    /// creates a new link with exactly that id rather than rejecting it. This is a deliberate design
    /// choice, not a bug — porting it as-is.
    /// </summary>
    [Fact]
    public async Task SetFileExternalLink_NonExistentLinkId_CreatesLinkWithThatId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set External Link Bad LinkId.docx", Owner);
        var linkId = new Guid("00000000-0000-0000-0000-000000000001");

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(linkId: linkId, access: FileShare.Read, primary: false, title: "Link With Bad Id"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.Id.Should().Be(linkId);
    }

    [Fact]
    public async Task SetFileExternalLink_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(
                999999999,
                new FileLinkRequest(access: FileShare.Read, primary: false, title: "Link On Missing File"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetFileExternalLink_Owner_CanSetExternalLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set Link Owner.docx", Owner);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Owner Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_DocSpaceAdmin_CanSetExternalLinkForOwnFile()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var file = await CreateFileInMy("Autotest Set Link Admin.docx", admin);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Admin Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_RoomAdmin_CanSetExternalLinkForOwnFile()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var file = await CreateFileInMy("Autotest Set Link Room Admin.docx", roomAdmin);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Room Admin Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_User_CanSetExternalLinkForOwnFile()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var file = await CreateFileInMy("Autotest Set Link User.docx", user);

        // Act
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "User Link"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetFileExternalLink_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set Link Anon.docx", Owner);
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(
                file.Id,
                new FileLinkRequest(access: FileShare.Read, primary: false, title: "Anon Link"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetFileExternalLink_UserWithoutAccess_AnotherUsersPrivateFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set Link Other User Private.docx", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(
                file.Id,
                new FileLinkRequest(access: FileShare.Read, primary: false, title: "Other User Link"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetFileExternalLink_DocSpaceAdminWithoutAccess_AnotherUsersPrivateFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Set Link Admin Other Private.docx", Owner);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileExternalLinkAsync(
                file.Id,
                new FileLinkRequest(access: FileShare.Read, primary: false, title: "Admin Other User Link"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
