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
/// <c>POST /api/2.0/files/@my/upload</c> - single-request upload of a file directly into the
/// caller's My Documents section.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "Upload")]
public class UploadToMyTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    [Fact]
    public async Task UploadToMy_Owner_ReturnsUploadedFile()
    {
        var uploaded = await UploadToMyAsync("Autotest file content"u8.ToArray(), "autotest-my-upload.txt", "text/plain");

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadToMy_AppearsInMyDocumentsListing()
    {
        const string fileName = "autotest-my-listing.txt";

        await UploadToMyAsync("listing check"u8.ToArray(), fileName, "text/plain");

        var myFolderId = await GetUserFolderIdAsync(Owner);
        var files = await GetFolderFilesAsync(myFolderId);

        files.Should().Contain(f => f.Title == fileName);
    }

    [Fact]
    public async Task UploadToMy_ResponseContainsCorrectFields()
    {
        var content = "response fields check"u8.ToArray();
        const string fileName = "autotest-my-fields.txt";

        var uploaded = (await UploadToMyAsync(content, fileName, "text/plain"))[0];

        uploaded.Title.Should().Be(fileName);
        uploaded.FileExst.Should().Be(".txt");
        uploaded.PureContentLength.Should().Be(content.Length);
    }

    [Fact]
    public async Task UploadToMy_CreateNewIfExistFalse_OverwritesKeepingSameId()
    {
        const string fileName = "autotest-my-overwrite.txt";

        var first = (await UploadToMyAsync("original content"u8.ToArray(), fileName, "text/plain"))[0];
        var second = (await UploadToMyAsync("updated content"u8.ToArray(), fileName, "text/plain", createNewIfExist: false))[0];

        second.Id.Should().Be(first.Id);
        second.Version.Should().BeGreaterThan(first.Version);
    }

    [Fact]
    public async Task UploadToMy_CreateNewIfExistTrue_CreatesNewFile()
    {
        const string fileName = "autotest-my-duplicate.txt";

        var first = (await UploadToMyAsync("first content"u8.ToArray(), fileName, "text/plain"))[0];
        var second = (await UploadToMyAsync("second content"u8.ToArray(), fileName, "text/plain", createNewIfExist: true))[0];

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task UploadToMy_FilenameWithSpecialCharacters_Accepted()
    {
        const string fileName = "autotest-my-test (special).txt";

        var uploaded = (await UploadToMyAsync("special chars content"u8.ToArray(), fileName, "text/plain"))[0];

        uploaded.Title.Should().Be(fileName);
    }

    [Fact]
    public async Task UploadToMy_EmptyFile_Returns200()
    {
        var uploaded = await UploadToMyAsync([], "autotest-my-empty.txt", "text/plain");

        uploaded.Should().ContainSingle();
    }

    /// <summary>TS: "No file in request body returns 400" - filed as BUG 81549 upstream.</summary>
    [Trait("Bug", "81549")]
    [Fact]
    public async Task UploadToMy_NoFileInBody_Returns400()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToMyAsync(null, ""));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UploadToMy_FolderIdInResponseMatchesMyDocuments()
    {
        var myFolderId = await GetUserFolderIdAsync(Owner);

        var uploaded = (await UploadToMyAsync("folder id check"u8.ToArray(), "autotest-my-folderid.txt", "text/plain"))[0];

        uploaded.FolderId.Should().Be(myFolderId);
    }

    [Fact]
    public async Task UploadToMy_CreateNewIfExistFalseWithNoExistingFile_Returns200()
    {
        var uploaded = await UploadToMyAsync(
            "first upload, no conflict"u8.ToArray(), "autotest-my-no-conflict.txt", "text/plain", createNewIfExist: false);

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadToMy_StoreOriginalFileTrue_PreservesOriginalExtension()
    {
        var uploaded = (await UploadToMyAsync(
            "fake docx content"u8.ToArray(),
            "autotest-my-store-flag.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            storeOriginalFile: true))[0];

        uploaded.FileExst.Should().Be(".docx");
    }
}
