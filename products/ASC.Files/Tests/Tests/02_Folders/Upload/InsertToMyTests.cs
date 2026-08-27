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

namespace ASC.Files.Tests.Tests._02_Folders.Upload;

/// <summary>
/// <c>POST /api/2.0/files/@my/insert</c> - inserts a file into the caller's My Documents section,
/// with an explicit <c>title</c> that can override the uploaded file's own name.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "Upload")]
public class InsertToMyTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    [Fact]
    public async Task InsertToMy_Owner_ReturnsInsertedFile()
    {
        var inserted = await InsertToMyAsync(
            "Autotest insert content"u8.ToArray(), "autotest-insert-my.txt", "autotest-insert-my.txt", "text/plain");

        inserted.Should().NotBeNull();
    }

    [Fact]
    public async Task InsertToMy_FolderIdInResponseMatchesMyDocuments()
    {
        var myFolderId = await GetUserFolderIdAsync(Owner);

        var inserted = await InsertToMyAsync(
            "folder id check"u8.ToArray(), "autotest-insert-my-folderid.txt", "autotest-insert-my-folderid.txt", "text/plain");

        inserted.FolderId.Should().Be(myFolderId);
    }

    [Fact]
    public async Task InsertToMy_ResponseContainsCorrectFields()
    {
        var content = "response fields check"u8.ToArray();
        const string fileName = "autotest-insert-my-fields.txt";

        var inserted = await InsertToMyAsync(content, fileName, fileName, "text/plain");

        inserted.Title.Should().Be(fileName);
        inserted.FileExst.Should().Be(".txt");
        inserted.PureContentLength.Should().Be(content.Length);
    }

    [Fact]
    public async Task InsertToMy_TitleParameter_OverridesFilename()
    {
        var inserted = await InsertToMyAsync(
            "title override check"u8.ToArray(), "original-name.txt", "autotest-insert-my-title-override.txt", "text/plain");

        inserted.Title.Should().Be("autotest-insert-my-title-override.txt");
    }

    [Fact]
    public async Task InsertToMy_AppearsInMyDocumentsListing()
    {
        const string fileName = "autotest-insert-my-listing.txt";

        await InsertToMyAsync("listing check"u8.ToArray(), fileName, fileName, "text/plain");

        var myFolderId = await GetUserFolderIdAsync(Owner);
        var files = await GetFolderFilesAsync(myFolderId);

        files.Should().Contain(f => f.Title == fileName);
    }

    [Fact]
    public async Task InsertToMy_CreateNewIfExistFalse_OverwritesKeepingSameId()
    {
        const string fileName = "autotest-insert-my-overwrite.txt";

        var first = await InsertToMyAsync("original"u8.ToArray(), fileName, fileName, "text/plain");
        var second = await InsertToMyAsync("updated"u8.ToArray(), fileName, fileName, "text/plain", createNewIfExist: false);

        second.Id.Should().Be(first.Id);
        second.Version.Should().BeGreaterThan(first.Version);
    }

    [Fact]
    public async Task InsertToMy_CreateNewIfExistTrue_CreatesNewFile()
    {
        const string fileName = "autotest-insert-my-duplicate.txt";

        var first = await InsertToMyAsync("first"u8.ToArray(), fileName, fileName, "text/plain");
        var second = await InsertToMyAsync("second"u8.ToArray(), fileName, fileName, "text/plain", createNewIfExist: true);

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task InsertToMy_EmptyFile_ReturnsZeroByteContentLength()
    {
        var inserted = await InsertToMyAsync([], "autotest-insert-my-empty.txt", "autotest-insert-my-empty.txt", "text/plain");

        inserted.ContentLength.Should().Be("0 bytes");
    }

    [Fact]
    public async Task InsertToMy_NoFileInBody_Returns400()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await InsertToMyAsync(null, ""));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task InsertToMy_DocxFile_ReturnsCorrectFileExst()
    {
        var inserted = await InsertToMyAsync(
            "docx content"u8.ToArray(),
            "autotest-insert-my-format.docx",
            "autotest-insert-my-format.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        inserted.FileExst.Should().Be(".docx");
    }
}
