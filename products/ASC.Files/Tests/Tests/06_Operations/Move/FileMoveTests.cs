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

[Collection("Test Collection")]
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
        await _filesClient.Authenticate(Initializer.Owner);

        // Create a source file
        var sourceFile = await CreateFileInMy("file_to_move.docx", Initializer.Owner);

        // Create a target folder
        var targetFolder = await CreateFolder("target_folder", FolderType.USER, Initializer.Owner);

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
        await _filesClient.Authenticate(Initializer.Owner);
        var sourceFile = await CreateFileInMy("source_file.docx", Initializer.Owner);

        var user = await Initializer.InviteContact(EmployeeType.User);
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
    public async Task MoveFile_FormToFillingFormsRoom_ReturnsError()
    {
        // Assert
        await _filesClient.Authenticate(Initializer.Owner);
        var sourseFile = await CreateFileInMy("source_file.docx", Initializer.Owner);
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
}
