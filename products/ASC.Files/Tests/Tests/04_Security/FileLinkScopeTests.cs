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

namespace ASC.Files.Tests.Tests._04_Security;

/// <summary>
/// A link names one entry: a file link opens that file and neither its parent folder nor a sibling,
/// and the links of a folder and of a file inside it are separate share rows, so changing the
/// folder link's right does not touch the file link.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Inheritance")]
public class FileLinkScopeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task FileLink_AnonymousVisitor_OpensTheFile_ParentFolderAndSiblingRefused()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolderInMy("Autotest Link Scope Folder", Owner);
        var file = await CreateFile("Autotest Linked File.docx", folder.Id);
        var sibling = await CreateFile("Autotest Sibling File.docx", folder.Id);
        var link = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, new FileLinkRequest(access: FileShare.Read), TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.SharedLink.RequestToken);

        var linked = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var parent = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
        var other = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(sibling.Id, cancellationToken: TestContext.Current.CancellationToken));

        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        linked.Title.Should().Be(file.Title);
        parent.ErrorCode.Should().Be(403);
        other.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task FolderLinkRightChanged_FileLinkInsideKeepsItsRight()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolderInMy("Autotest Independent Links", Owner);
        var file = await CreateFile("Autotest Independent File.docx", folder.Id);

        var folderLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            folder.Id,
            new FolderLinkRequest(access: FileShare.Read, primary: true, title: "Folder Link"),
            TestContext.Current.CancellationToken)).Response;
        var fileLink = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, new FileLinkRequest(access: FileShare.Comment), TestContext.Current.CancellationToken)).Response;

        // Act
        var changed = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            folder.Id,
            new FolderLinkRequest(linkId: folderLink.SharedLink.Id, access: FileShare.Editing, primary: true, title: folderLink.SharedLink.Title),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        changed.Access.Should().Be(FileShare.Editing);

        var fileLinks = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        fileLinks.Should().ContainSingle();
        fileLinks[0].SharedLink.Id.Should().Be(fileLink.SharedLink.Id);
        fileLinks[0].Access.Should().Be(FileShare.Comment);

        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileLink.SharedLink.RequestToken);
        var viaFileLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        viaFileLink.Access.Should().Be(FileShare.Comment);
    }
}
