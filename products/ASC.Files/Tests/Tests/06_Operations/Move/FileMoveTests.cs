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

namespace ASC.Files.Tests.Tests._06_Operations.Move;

[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMoveTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task MoveFile_ToAnotherFolder_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Create a source file
        var sourceFile = await CreateFileInMy("file_to_move.docx", Owner);

        // Create a target folder
        var targetFolder = await CreateFolder("target_folder", FolderType.USER, Owner);

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;

        var operationId = results.FirstOrDefault()?.Id;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(operationId);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        // Verify file is moved
        var fileInfo = await GetFile(sourceFile.Id);
        fileInfo.FolderId.Should().Be(targetFolder.Id);
    }

    [Fact]
    public async Task MoveFile_NoPermission_ReturnsError()
    {
        // Assert
        await _filesClient.Authenticate(Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var targetFolder = await CreateFolderInMy("target_folder", user);

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                moveParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_ToFormsRoot_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Owner);

        var formsRootId = await GetFolderIdAsync(FolderType.Forms, Owner);

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(formsRootId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                moveParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_ToAiAgentsRoot_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Owner);

        var aiAgentsRootId = await GetFolderIdAsync(FolderType.AiAgents, Owner);

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(aiAgentsRootId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                moveParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_FormToFillingFormsRoom_ReturnsError()
    {
        // Assert
        await _filesClient.Authenticate(Owner);
        var sourseFile = await CreateFileInMy("source_file.docx", Owner);
        sourseFile.IsForm = true;

        var parentFolder = await CreateFillingFormsRoom("parent_folder");
        var targetFolder = await CreateFolder("target_folder", parentFolder.Id);
        targetFolder.Type = FolderType.VirtualRooms;

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourseFile.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                moveParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_MultipleFiles_ToCustomRoom_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var file1 = await CreateFileInMy("Autotest MoveBatch Multi File1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest MoveBatch Multi File2.docx", Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Multi Dest Room");

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file1.Id), new(file2.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        var movedFile1 = await GetFile(file1.Id);
        var movedFile2 = await GetFile(file2.Id);
        movedFile1.FolderId.Should().Be(destRoom.Id);
        movedFile2.FolderId.Should().Be(destRoom.Id);
    }

    [Fact]
    public async Task MoveFile_BetweenTwoCustomRooms_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var srcRoom = await CreateCustomRoom("Autotest MoveBatch InterRoom Source");
        var file = await CreateFile("Autotest MoveBatch InterRoom File.docx", srcRoom.Id);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch InterRoom Dest");

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        var movedFile = await GetFile(file.Id);
        movedFile.FolderId.Should().Be(destRoom.Id);
    }

    [Fact]
    public async Task MoveFile_FromRoomToMyDocs_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var srcRoom = await CreateCustomRoom("Autotest MoveBatch RoomToMyDocs Src");
        var file = await CreateFile("Autotest MoveBatch RoomToMyDocs File.docx", srcRoom.Id);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new(myDocsFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        var movedFile = await GetFile(file.Id);
        movedFile.FolderId.Should().Be(myDocsFolderId);
    }

    [Fact]
    public async Task MoveFile_BetweenSubfoldersWithinMyDocs_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var srcFolder = await CreateFolderInMy("Autotest MoveBatch MyDocsSrc Folder", Owner);
        var file = await CreateFile("Autotest MoveBatch MyDocs Subfolder File.docx", srcFolder.Id);
        var destFolder = await CreateFolderInMy("Autotest MoveBatch MyDocsDest Folder", Owner);

        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        var movedFile = await GetFile(file.Id);
        movedFile.FolderId.Should().Be(destFolder.Id);
    }
}
