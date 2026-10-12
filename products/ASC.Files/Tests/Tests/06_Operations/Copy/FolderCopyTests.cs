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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

[Trait("Category", "Operations")]
[Trait("Feature", "Folders")]
public class FolderCopyTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    [Fact]
    public async Task CopyFolder_ToItsSubfolder_ReturnError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceFolder = await CreateFolder("source_folder", FolderType.USER, Owner);
        var subFolder = await CreateFolder("subfolder", sourceFolder.Id);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(subFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolder.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFolder_NoCopyPermissions_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var targetFolder = await CreateFolderInMy("target_folder", Owner);
        var sourceFolder = await CreateFolderInMy("source_folder", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolder.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }


    [Fact]
    public async Task CopyFolder_ToFormFillingRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateFillingFormsRoom("source_room");
        var targertRoom = await CreateFillingFormsRoom("target_room");

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targertRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceRoom.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFolder_FolderNotFound_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var targetFolderId = await GetUserFolderIdAsync(Owner);
        var sourceFolderId = 999999999;

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolderId)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
    }

    [Fact]
    public async Task CopyFolder_FromMyDocsToCustomRoom_AppearsWithContents()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string folderTitle = "Autotest CopyBatch Source Folder";
        var sourceFolder = await CreateFolder(folderTitle, myDocsId);
        const string innerFileTitle = "Autotest CopyBatch File In Folder.docx";
        await CreateFile(innerFileTitle, sourceFolder.Id);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Folder Dest Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, folderIds: [sourceFolder.Id]));

        // Assert
        FolderTitles(await GetFolderContent(destRoom.Id)).Should().Contain(folderTitle);
        var copiedFolderId = await FindEntryIdByTitle(destRoom.Id, folderTitle, inFolders: true);
        FileTitles(await GetFolderContent(copiedFolderId)).Should().Contain(innerFileTitle);
    }

    [Fact]
    public async Task CopyFolder_WithContentTrue_CopiesContentsWithoutFolderItself()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string folderTitle = "Autotest CopyBatch Content Folder";
        var sourceFolder = await CreateFolder(folderTitle, myDocsId);
        const string innerFileTitle = "Autotest CopyBatch Content Inner File.docx";
        await CreateFile(innerFileTitle, sourceFolder.Id);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Content Dest Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, content: true, folderIds: [sourceFolder.Id]));

        // Assert
        var content = await GetFolderContent(destRoom.Id);
        FileTitles(content).Should().Contain(innerFileTitle);
        FolderTitles(content).Should().NotContain(folderTitle, "content=true copies the folder's contents, not the folder itself");
    }

    [Fact]
    public async Task CopyFilesAndFolders_Together_AllAppearInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileTitle = "Autotest CopyBatch Mixed File.docx";
        const string folderTitle = "Autotest CopyBatch Mixed Folder";
        var sourceFile = await CreateFile(fileTitle, myDocsId);
        var sourceFolder = await CreateFolder(folderTitle, myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Mixed Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id], folderIds: [sourceFolder.Id]));

        // Assert
        var content = await GetFolderContent(destRoom.Id);
        FileTitles(content).Should().Contain(fileTitle);
        FolderTitles(content).Should().Contain(folderTitle);
    }

    [Fact]
    public async Task CopyMultipleFolders_AllAppearInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string folder1Title = "Autotest CopyBatch MultiFolders Folder1";
        const string folder2Title = "Autotest CopyBatch MultiFolders Folder2";
        var folder1 = await CreateFolder(folder1Title, myDocsId);
        var folder2 = await CreateFolder(folder2Title, myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch MultiFolders Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, folderIds: [folder1.Id, folder2.Id]));

        // Assert
        var destFolderTitles = FolderTitles(await GetFolderContent(destRoom.Id));
        destFolderTitles.Should().Contain(folder1Title);
        destFolderTitles.Should().Contain(folder2Title);
    }

    [Fact]
    public async Task CopyEmptyFolder_AppearsEmptyInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string emptyFolderTitle = "Autotest CopyBatch EmptyFolder";
        var emptyFolder = await CreateFolder(emptyFolderTitle, myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch EmptyFolder Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, folderIds: [emptyFolder.Id]));

        // Assert
        FolderTitles(await GetFolderContent(destRoom.Id)).Should().Contain(emptyFolderTitle);
        var copiedFolderId = await FindEntryIdByTitle(destRoom.Id, emptyFolderTitle, inFolders: true);
        var copiedContent = await GetFolderContent(copiedFolderId);
        copiedContent.Files.Should().BeEmpty();
        copiedContent.Folders.Should().BeEmpty();
    }
}
