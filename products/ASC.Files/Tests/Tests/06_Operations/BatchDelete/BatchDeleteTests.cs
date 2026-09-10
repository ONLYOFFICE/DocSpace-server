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

namespace ASC.Files.Tests.Tests._06_Operations.BatchDelete;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/delete</c> — functional coverage: moving a file/folder to Trash,
/// permanent deletion, deleting a folder with files inside, a mixed batch of files and folders, an
/// empty selection, a non-existent file id, and deleting a file that is already in Trash. Access
/// control lives in <see cref="BatchDeletePermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BatchDeleteTests(
    AspireAppFixture fixture)
    : BatchDeleteTestBase(fixture)
{
    [Fact]
    public async Task DeleteBatchItems_MoveFileToTrash_AppearsInTrashAndLeavesSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Delete ToTrash File.docx";
        var file = await CreateFile(fileName, folderId);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = false },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await TrashContainsFileTitleAsync(fileName)).Should().BeTrue();
        (await FolderContainsFileTitleAsync(folderId, fileName)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBatchItems_PermanentDelete_NotInTrashOrSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Delete Permanent File.docx";
        var file = await CreateFile(fileName, folderId);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await TrashContainsFileTitleAsync(fileName)).Should().BeFalse();
        (await FolderContainsFileTitleAsync(folderId, fileName)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBatchItems_MoveFolderToTrash_AppearsInTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string folderTitle = "Autotest Delete FolderToTrash";
        var folder = await CreateFolderInMy(folderTitle, Owner);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FolderIds = [new(folder.Id)], Immediately = false },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await TrashContainsFolderTitleAsync(folderTitle)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatchItems_PermanentDeleteFolderWithFiles_RemovedFromSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var parentId = await GetUserFolderIdAsync(Owner);
        const string folderTitle = "Autotest Delete FolderWithFiles";
        var folder = await CreateFolder(folderTitle, parentId);
        await CreateFile("Autotest Delete InnerFile.docx", folder.Id);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FolderIds = [new(folder.Id)], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await FolderContainsFolderTitleAsync(parentId, folderTitle)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBatchItems_MultipleFilesAndFolders_AllRemovedFromSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var parentId = await GetUserFolderIdAsync(Owner);

        const string fileName1 = "Autotest Batch Delete File1.docx";
        const string fileName2 = "Autotest Batch Delete File2.docx";
        const string folderTitle = "Autotest Batch Delete Folder1";

        var file1 = await CreateFile(fileName1, parentId);
        var file2 = await CreateFile(fileName2, parentId);
        var folder1 = await CreateFolder(folderTitle, parentId);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file1.Id), new(file2.Id)], FolderIds = [new(folder1.Id)], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await FolderContainsFileTitleAsync(parentId, fileName1)).Should().BeFalse();
        (await FolderContainsFileTitleAsync(parentId, fileName2)).Should().BeFalse();
        (await FolderContainsFolderTitleAsync(parentId, folderTitle)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBatchItems_EmptySelection_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [], FolderIds = [], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert - an empty selection is a legal (if pointless) request, and must not throw.
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteBatchItems_NonExistentFileId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(999999999)], Immediately = true },
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteBatchItems_FileAlreadyInTrash_PermanentlyDeletesAndRemovesFromTrash()
    {
        // Catches: double-delete must work -- move to trash, then delete permanently from trash.
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileName = "Autotest Delete FromTrash File.docx";
        var file = await CreateFileInMy(fileName, Owner);

        var moveResults = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = false },
            TestContext.Current.CancellationToken)).Response;
        await WaitForBatchDelete(moveResults);

        // Act
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(file.Id)], Immediately = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBatchDelete(results);
        operation.Operation.Should().Be(FileOperationType.Delete);

        (await TrashContainsFileTitleAsync(fileName)).Should().BeFalse();
    }
}
