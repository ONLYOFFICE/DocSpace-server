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
/// The revocation state of a file's primary external link: a read after the revocation answers 404 instead of
/// creating a new link, <c>setFileExternalLink</c> with <c>primary</c> true creates one again and clears the state,
/// and in a Public room the revoked link is replaced at once instead of staying revoked.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Links")]
public class FileLinkPrimaryRevocationTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetFilePrimaryExternalLink_AfterRevocation_ReturnsNotFound()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Revoked Read Room");
        var file = await CreateFile("Autotest Revoked Read.docx", room.Id);
        await Revoke(file.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetFileExternalLink_PrimaryTrueAfterRevocation_CreatesALinkTheReadAnswers()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Revoked Set Room");
        var file = await CreateFile("Autotest Revoked Set.docx", room.Id);
        var revokedId = await Revoke(file.Id);

        // Act
        var created = (await _filesApi.SetFileExternalLinkAsync(
            file.Id, new FileLinkRequest(linkId: Guid.NewGuid(), access: FileShare.Read, primary: true), TestContext.Current.CancellationToken)).Response;

        // Assert
        created.SharedLink.Id.Should().NotBe(revokedId);
        var read = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        read.SharedLink.Id.Should().Be(created.SharedLink.Id);
        read.SharedLink.Primary.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeFilePrimaryExternalLink_InPublicRoom_IsReplacedByANewReadLink()
    {
        // Arrange
        var room = await CreatePublicRoom("Autotest Revoked Public Room");
        var file = await CreateFile("Autotest Revoked Public.docx", room.Id);

        // Act
        var revokedId = await Revoke(file.Id);

        // Assert - no revocation state: the read answers a fresh primary link
        var read = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        read.SharedLink.Id.Should().NotBe(revokedId);
        read.SharedLink.Primary.Should().BeTrue();
        read.Access.Should().Be(FileShare.Read);
    }

    private async Task<Guid> Revoke(int fileId)
    {
        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        await _filesApi.SetFileExternalLinkAsync(
            fileId, new FileLinkRequest(linkId: link.SharedLink.Id, access: FileShare.None), TestContext.Current.CancellationToken);

        return link.SharedLink.Id;
    }
}
