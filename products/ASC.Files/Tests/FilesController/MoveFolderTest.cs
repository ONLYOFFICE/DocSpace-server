// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class MoveFolderTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CopyFolder_BetweenUserFolders_ReturnsValidFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a source folder with a test file inside
        var sourceFolder = await CreateFolder("source_folder", FolderType.USER, Initializer.Owner);
        await CreateFile("test_file.docx", sourceFolder.Id);
        
        // Create a target folder
        var targetFolderId = await GetFolderIdAsync(FolderType.USER, Initializer.Owner);
        
        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolder.Id)]
        };
        
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(copyParams, TestContext.Current.CancellationToken)).Response;
        
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation();
        }
        
        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        
        // Verify both folders exist
        var sourceFolderInfo = (await _foldersApi.GetFolderInfoAsync(sourceFolder.Id, TestContext.Current.CancellationToken)).Response;
        sourceFolderInfo.Should().NotBeNull();
        
        // Find the copied folder in the target directory
        var targetFolderContent = (await _foldersApi.GetFolderByFolderIdAsync(targetFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var copiedFolder = targetFolderContent.Folders.FirstOrDefault(f => f.Title == sourceFolder.Title);
        
        copiedFolder.Should().NotBeNull();
        copiedFolder!.Title.Should().Be(sourceFolder.Title);
        //copiedFolder.Id.Should().NotBe(sourceFolder.Id);
    }
    
    [Fact]
    public async Task MoveFolder_ToAnotherFolder_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a source folder
        var sourceFolder = await CreateFolder("folder_to_move", FolderType.USER, Initializer.Owner);
        
        // Create a target folder
        var targetFolder = await CreateFolder("target_folder", FolderType.USER, Initializer.Owner);
        
        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolder.Id)]
        };
        
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;
        
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation();
        }
        
        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        
        // Verify folder was moved
        var movedFolder = (await _foldersApi.GetFolderInfoAsync(sourceFolder.Id, TestContext.Current.CancellationToken)).Response;
        movedFolder.Should().NotBeNull();
        movedFolder.ParentId.Should().Be(targetFolder.Id);
    }
}
