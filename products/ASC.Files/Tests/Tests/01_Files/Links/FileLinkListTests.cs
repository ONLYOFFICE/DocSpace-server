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
/// GET /files/file/{id}/links — the list of a file's custom (non-primary) external links.
/// </summary>
[Trait("Category", "Files")]
public class FileLinkListTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetFileLinks_NewFileInMyDocuments_HasNoCustomExternalLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Links.docx", Owner);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFileLinks_NewFileInRoom_HasNoCustomExternalLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For File Links");
        var file = await CreateFile("Autotest Room File Links.docx", room.Id);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFileLinks_WithCountAndStartIndex_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Links Pagination.docx", Owner);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, count: 10, startIndex: 0, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFileLinks_FileWithCreatedExternalLink_ReturnsItInTheResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File With External Link.docx", Owner);

        await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Test External Link"),
            TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Count.Should().BeGreaterThanOrEqualTo(1);
        result.Count.Should().Be(result.Response.Count);
    }

    [Fact]
    public async Task GetFileLinks_ExternalLinkItem_HasCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Link Structure.docx", Owner);

        await _filesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, primary: false, title: "Test External Link Structure"),
            TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Count.Should().BeGreaterThanOrEqualTo(1);

        var link = result.Response[0];
        link.SubjectType.Should().Be(SubjectType.ExternalLink);
        link.Access.Should().Be(FileShare.Read);
        link.IsLocked.Should().BeFalse();
        link.SharedLink.Should().NotBeNull();
        link.SharedLink.Id.Should().NotBeEmpty();
        link.SharedLink.Title.Should().Be("Test External Link Structure");
        link.SharedLink.ShareLink.Should().NotBeNullOrEmpty();
        link.SharedLink.LinkType.Should().Be(LinkType.External);
        link.SharedLink.Primary.Should().BeFalse();
        link.SharedLink.Internal.Should().BeFalse();
        link.SharedLink.DenyDownload.Should().BeFalse();
        link.SharedLink.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileLinks_Owner_ReturnsEmptyForNewFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Links Owner.docx", Owner);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFileLinks_DocSpaceAdmin_ReturnsEmptyForNewOwnFile()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var file = await CreateFileInMy("Autotest File Links Admin.docx", admin);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFileLinks_RoomAdmin_ReturnsEmptyForNewOwnFile()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var file = await CreateFileInMy("Autotest File Links Room Admin.docx", roomAdmin);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFileLinks_User_ReturnsEmptyForNewOwnFile()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var file = await CreateFileInMy("Autotest File Links User.docx", user);

        // Act
        var result = await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFileLinks_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Links Anon.docx", Owner);
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// The TypeScript suite expects 200 with an empty array here, but the product refuses the read
    /// outright: a file that exists and is off limits answers 403, which is the contract the
    /// rooms endpoints already follow and is the safer of the two (an empty 200 confirms the file
    /// exists to a caller who may not see it). The sibling endpoint
    /// <c>GET /files/file/{id}/link</c> behaves the same way, so 403 is asserted here too.
    /// </summary>
    [Fact]
    public async Task GetFileLinks_UserWithoutAccess_AnotherUsersPrivateFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Links Other User Private.docx", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
