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
/// <c>POST /api/2.0/files/{folderId}/upload</c> - single-request upload of a file into an arbitrary
/// folder (room, subfolder, or My Documents).
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "Upload")]
public class UploadTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    [Fact]
    public async Task UploadFile_ToRoom_ReturnsUploadedFile()
    {
        var room = await CreateCustomRoom("Autotest Room Upload File");

        var uploaded = await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-upload.txt");

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadFile_AppearsInFolderListing()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Listing Check");
        const string fileName = "autotest-listing-check.txt";

        await UploadToFolderAsync(room.Id, "Autotest file content for listing check"u8.ToArray(), fileName);

        var files = await GetFolderFilesAsync(room.Id);

        files.Should().Contain(f => f.Title == fileName);
    }

    [Fact]
    public async Task UploadFile_ResponseContainsCorrectFields()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Fields");
        const string fileName = "autotest-fields-check.txt";

        var uploaded = (await UploadToFolderAsync(room.Id, "Autotest file content for fields check"u8.ToArray(), fileName))[0];

        uploaded.Id.Should().NotBe(0);
        uploaded.Title.Should().Be(fileName);
        uploaded.FolderId.Should().Be(room.Id);
        uploaded.CreatedBy.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task UploadFile_CreateNewIfExistTrue_CreatesNewFile()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Create New");
        const string fileName = "autotest-create-new.txt";

        var first = (await UploadToFolderAsync(room.Id, "First content"u8.ToArray(), fileName))[0];
        var second = (await UploadToFolderAsync(room.Id, "Second content"u8.ToArray(), fileName, createNewIfExist: true))[0];

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task UploadFile_EmptyFile_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Empty File");

        var uploaded = await UploadToFolderAsync(room.Id, [], "autotest-empty.txt");

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadFile_PureContentLengthMatchesUploadedSize()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Content Length");
        var content = "Autotest content length check"u8.ToArray();

        var uploaded = (await UploadToFolderAsync(room.Id, content, "autotest-content-length.txt"))[0];

        uploaded.PureContentLength.Should().Be(content.Length);
    }

    [Fact]
    public async Task UploadFile_FileExstMatchesUploadedExtension()
    {
        var room = await CreateCustomRoom("Autotest Room Upload File Ext");

        var uploaded = (await UploadToFolderAsync(room.Id, "content"u8.ToArray(), "autotest-ext-check.txt"))[0];

        uploaded.FileExst.Should().Be(".txt");
    }

    [Fact]
    public async Task UploadFile_CreateNewIfExistFalse_OverwritesKeepingSameId()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Overwrite");
        const string fileName = "autotest-overwrite.txt";

        var first = (await UploadToFolderAsync(room.Id, "First content"u8.ToArray(), fileName))[0];
        var second = (await UploadToFolderAsync(room.Id, "Second content"u8.ToArray(), fileName))[0];

        second.Id.Should().Be(first.Id);
        second.Version.Should().BeGreaterThan(first.Version);
    }

    [Fact]
    public async Task UploadFile_RoundTrip_RetrievableWithCorrectSize()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Round Trip");
        var content = "Round trip content check"u8.ToArray();
        const string fileName = "autotest-round-trip.txt";

        var uploaded = (await UploadToFolderAsync(room.Id, content, fileName))[0];

        var files = await GetFolderFilesAsync(room.Id);
        files.Should().Contain(f => f.Title == fileName);

        var refetched = await GetFile(uploaded.Id);
        refetched.PureContentLength.Should().Be(content.Length);
    }

    [Fact]
    public async Task UploadFile_ToSubfolderInsideRoom_ReturnsSubfolderId()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Subfolder");
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);

        var uploaded = (await UploadToFolderAsync(subfolder.Id, "Subfolder file content"u8.ToArray(), "autotest-subfolder-file.txt"))[0];

        uploaded.FolderId.Should().Be(subfolder.Id);
    }

    [Fact]
    public async Task UploadFile_ToMyDocumentsFolder_ReturnsMyFolderId()
    {
        var myFolderId = await GetUserFolderIdAsync(Owner);

        var uploaded = (await UploadToFolderAsync(myFolderId, "My Documents file content"u8.ToArray(), "autotest-my-docs.txt"))[0];

        uploaded.FolderId.Should().Be(myFolderId);
    }

    [Fact]
    public async Task UploadFile_FilenameWithSpecialCharacters_Accepted()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Special Chars");
        const string fileName = "autotest тест (special) file.txt";

        var uploaded = (await UploadToFolderAsync(room.Id, "Special chars content"u8.ToArray(), fileName))[0];

        uploaded.Title.Should().Be(fileName);
    }

    [Fact]
    public async Task UploadFile_StoreOriginalFileTrue_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Store Original");

        var uploaded = await UploadToFolderAsync(
            room.Id, "Original format content"u8.ToArray(), "autotest-store-original.docx", storeOriginalFile: true);

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadFile_ToArchivedRoom_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Archived");
        await ArchiveRoom(room.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Content for archived room"u8.ToArray(), "autotest-archived.txt"));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// TS: "No file in request body returns 403 instead of 400" - the product should reject a
    /// bodyless upload with 400 (bad request), not 403 (forbidden); the room itself is fully
    /// accessible to the owner.
    /// </summary>
    [Trait("Bug", "81547")]
    [Fact]
    public async Task UploadFile_NoFileInBody_Returns400()
    {
        var room = await CreateCustomRoom("Autotest Room Upload No File");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, null, ""));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UploadFile_NonExistentFolderId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(999999999, "Autotest file content"u8.ToArray(), "autotest-nonexistent.txt"));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UploadFile_FolderIdZero_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(0, "Autotest file content"u8.ToArray(), "autotest-zero-folder.txt"));

        exception.ErrorCode.Should().Be(404);
    }
}
