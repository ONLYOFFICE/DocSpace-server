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

namespace ASC.Files.Tests.Tests._01_Files.Create;

/// <summary>
/// Creating a file where My documents is meant, by a caller who has none: <c>POST @my/file</c>, and
/// <c>POST {folderId}/file</c> with the folder id 0, which also means My documents. A guest has no My documents, so
/// <c>FileStorageService.CreateNewFileAsync</c> got no folder and dereferenced it: a 500. It is now 404 with the
/// folder-not-found text, as <c>POST @my/html</c> and <c>POST @my/text</c> answer the same guest.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Files")]
public class FileCreateWithoutMyDocumentsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string FolderNotFound = "The required folder was not found";

    [Fact]
    public async Task CreateFileInMyDocuments_Guest_Returns404()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateFileInMyDocumentsAsync(
            new CreateFileRequest("Autotest Guest My Docs.docx"),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Be(FolderNotFound);
    }

    [Fact]
    public async Task CreateFile_FolderIdZero_Guest_Returns404()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateFileAsync(
            0,
            new CreateFileRequest("Autotest Guest Folder Zero.docx"),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Be(FolderNotFound);
    }

    /// <summary>
    /// The guard: for a caller who has My documents, the folder id 0 still creates the file there.
    /// </summary>
    [Fact]
    public async Task CreateFile_FolderIdZero_User_CreatesInMyDocuments()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var my = (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        // Act
        var file = (await _filesApi.CreateFileAsync(
            0,
            new CreateFileRequest("Autotest User Folder Zero.docx"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        file.FolderId.Should().Be(my.Id);
    }
}
