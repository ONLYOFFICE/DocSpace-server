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

namespace ASC.Files.Tests.Tests._02_Folders.UploadCheck;

[Trait("Category", "Functional")]
[Trait("Feature", "Folders")]
public class UploadCheckTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CheckUpload_NewFileTitles_ReturnsEmptyArray()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check");

        var result = await _foldersApi.CheckUploadAsync(
            room.Id, new CheckUploadRequest(filesTitle: ["Brand New File.docx", "Another New File.xlsx"]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckUpload_ExistingFileTitle_ReturnsConflict()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Conflict");
        var file = await CreateFile("Autotest Existing File.docx", room.Id);

        var result = await _foldersApi.CheckUploadAsync(room.Id, new CheckUploadRequest(filesTitle: [file.Title]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain(file.Title);
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task CheckUpload_MixedTitles_OnlyConflictingReturned()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Mixed");
        var file = await CreateFile("Autotest Conflicting File.docx", room.Id);
        const string newTitle = "Brand New Non-Conflicting File.docx";

        var result = await _foldersApi.CheckUploadAsync(
            room.Id, new CheckUploadRequest(filesTitle: [file.Title, newTitle]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain(file.Title);
        result.Response.Should().NotContain(newTitle);
        result.Response.Should().HaveCount(1);
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task CheckUpload_MultipleExistingTitles_AllReturned()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Multiple");
        var file1 = await CreateFile("Autotest File One.docx", room.Id);
        var file2 = await CreateFile("Autotest File Two.docx", room.Id);

        var result = await _foldersApi.CheckUploadAsync(
            room.Id, new CheckUploadRequest(filesTitle: [file1.Title, file2.Title]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain([file1.Title, file2.Title]);
        result.Response.Should().HaveCount(2);
        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task CheckUpload_EmptyFilesTitleArray_ReturnsEmptyResponse()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Empty");

        var result = await _foldersApi.CheckUploadAsync(room.Id, new CheckUploadRequest(filesTitle: []), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckUpload_FilesTitleNull_ReturnsBadRequest()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Null");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CheckUploadAsync(room.Id, new CheckUploadRequest(filesTitle: null), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CheckUpload_Subfolder_ConflictDetected()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Subfolder");
        var subfolder = await CreateFolder("Autotest Subfolder For Check", room.Id);
        var file = await CreateFile("Autotest File In Subfolder.docx", subfolder.Id);

        var result = await _foldersApi.CheckUploadAsync(subfolder.Id, new CheckUploadRequest(filesTitle: [file.Title]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain(file.Title);
        result.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Bug", "81365")]
    public async Task CheckUpload_DuplicateTitlesInRequest_ReturnsSingleConflict()
    {
        var folderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Dup File.docx", folderId);

        var result = await _foldersApi.CheckUploadAsync(
            folderId, new CheckUploadRequest(filesTitle: [file.Title, file.Title]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain(file.Title);
        result.Response.Should().HaveCount(1);
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task CheckUpload_CaseInsensitiveTitle_IsDetectedAsConflict()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check Case");
        await CreateFile("Autotest Case File.docx", room.Id);

        var result = await _foldersApi.CheckUploadAsync(
            room.Id, new CheckUploadRequest(filesTitle: ["autotest case file.docx"]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Bug", "81330")]
    public async Task CheckUpload_NonExistentFolderId_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CheckUploadAsync(999999999, new CheckUploadRequest(filesTitle: ["Some File.docx"]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// The typed <see cref="CheckUploadRequest"/> always serialises a "filesTitle" member (a null
    /// list still writes "filesTitle":null, per its EmitDefaultValue=true DataMember), so a request
    /// where the field is entirely absent can only be produced with a raw body.
    /// </summary>
    [Fact]
    [Trait("Bug", "81331")]
    public async Task CheckUpload_RequestWithoutFilesTitle_ReturnsBadRequest()
    {
        var room = await CreateCustomRoom("Autotest Room For Upload Check No Body");

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PostAsync($"api/2.0/files/{room.Id}/upload/check", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckUpload_MyDocumentsFolder_ConflictDetected()
    {
        var folderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest My Docs File.docx", folderId);

        var result = await _foldersApi.CheckUploadAsync(folderId, new CheckUploadRequest(filesTitle: [file.Title]), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Contain(file.Title);
        result.Response.Should().HaveCount(1);
        result.Count.Should().Be(1);
    }
}
