// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.Files.Tests.ApiFactories;

namespace ASC.Files.Tests.Tests._06_Operations.Move;

[Collection("Test Collection")]
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMoveTests(
    FilesApiFactory filesFactory,
    WepApiFactory apiFactory,
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram)
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
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
