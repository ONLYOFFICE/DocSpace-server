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

namespace ASC.Files.Tests.Tests._06_Operations.Duplicate;

/// <summary>
/// Basic <c>PUT /api/2.0/files/fileops/duplicate</c> scenarios: files and folders duplicated in
/// place (My Documents, rooms, subfolders), nested content, mixed batches and request-shape edge
/// cases. Who may call the endpoint has its own class, <see cref="DuplicatePermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class DuplicateBatchItemsTests(
    AspireAppFixture fixture)
    : DuplicateTestBase(fixture)
{
    [Fact]
    public async Task DuplicateFile_InMyDocuments_AppearsAsSecondCopyInSameFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileBase = "Autotest Dup Single File";
        var sourceFile = await CreateFile($"{fileBase}.docx", myDocsId);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        CountTitlesContaining(FileTitles(await GetFolderContent(myDocsId)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFolder_InMyDocuments_AppearsAsSecondCopyInSameLocation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string folderBase = "Autotest Dup Single Folder";
        var sourceFolder = await CreateFolder(folderBase, myDocsId);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(folderIds: [sourceFolder.Id]));

        // Assert
        CountTitlesContaining(FolderTitles(await GetFolderContent(myDocsId)), folderBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateMultipleFiles_AllAppearInSameFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string base1 = "Autotest Dup Multi FileA";
        const string base2 = "Autotest Dup Multi FileB";
        var file1 = await CreateFile($"{base1}.docx", myDocsId);
        var file2 = await CreateFile($"{base2}.docx", myDocsId);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [file1.Id, file2.Id]));

        // Assert
        var titles = FileTitles(await GetFolderContent(myDocsId));
        CountTitlesContaining(titles, base1).Should().BeGreaterThanOrEqualTo(2);
        CountTitlesContaining(titles, base2).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFile_InCustomRoom_AppearsInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Dup Room");
        const string fileBase = "Autotest Dup Room File";
        var sourceFile = await CreateFile($"{fileBase}.docx", room.Id);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        CountTitlesContaining(FileTitles(await GetFolderContent(room.Id)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFile_WithDiacriticalCharacters_BothAppearInFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileBase = "Autotest Dup Üñó Résumé";
        var sourceFile = await CreateFile($"{fileBase}.docx", myDocsId);
        sourceFile.Title.Should().Contain("Üñó");

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        CountTitlesContaining(FileTitles(await GetFolderContent(myDocsId)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateBatch_EmptyFileAndFolderIds_ReturnsOk()
    {
        // Arrange: an empty batch is accepted gracefully, consistent with the copy endpoint.
        await _filesClient.Authenticate(Owner);

        // Act
        var result = await _filesOperationsApi.DuplicateBatchItemsAsync(BuildDuplicateRequest(), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("Bug", "82210")]
    public async Task DuplicateFile_NonExistentFileId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [999999999]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "82210")]
    public async Task DuplicateFolder_NonExistentFolderId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(folderIds: [999999999]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DuplicateFile_InArchivedRoom_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Dup Archived Room");
        var sourceFile = await CreateFile("Autotest Dup Archived File.docx", room.Id);

        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DuplicateFile_InTrash_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest Dup Trash File.docx", myDocsId);

        await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto(fileIds: [new DeleteBatchRequestDtoAllOfFileIds(sourceFile.Id)], immediately: false),
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DuplicateFolder_WithNestedFile_NestedFileAppearsInDuplicate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string folderBase = "Autotest Dup Nested Folder";
        var sourceFolder = await CreateFolder(folderBase, myDocsId);
        var innerFile = await CreateFile("Autotest Dup Nested File.docx", sourceFolder.Id);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(folderIds: [sourceFolder.Id]));

        // Assert
        var matchingFolders = (await GetRawEntries(myDocsId, inFolders: true))
            .Where(e => e.Title.Contains(folderBase, StringComparison.Ordinal))
            .ToList();
        matchingFolders.Should().HaveCountGreaterThanOrEqualTo(2);

        var duplicateFolderId = matchingFolders.First(e => e.Id != sourceFolder.Id).Id;
        FileTitles(await GetFolderContent(duplicateFolderId)).Should().Contain(innerFile.Title);
    }

    [Fact]
    public async Task DuplicateFileAndFolder_Together_BothAppear()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileBase = "Autotest Dup Mixed File";
        var sourceFile = await CreateFile($"{fileBase}.docx", myDocsId);
        const string folderBase = "Autotest Dup Mixed Folder";
        var sourceFolder = await CreateFolder(folderBase, myDocsId);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id], folderIds: [sourceFolder.Id]));

        // Assert
        var content = await GetFolderContent(myDocsId);
        CountTitlesContaining(FileTitles(content), fileBase).Should().BeGreaterThanOrEqualTo(2);
        CountTitlesContaining(FolderTitles(content), folderBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFile_InNestedSubfolder_AppearsInSameSubfolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var subFolder = await CreateFolder("Autotest Dup Subfolder", myDocsId);
        const string fileBase = "Autotest Dup Subfolder File";
        var sourceFile = await CreateFile($"{fileBase}.docx", subFolder.Id);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        CountTitlesContaining(FileTitles(await GetFolderContent(subFolder.Id)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }
}
