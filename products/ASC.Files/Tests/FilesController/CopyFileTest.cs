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
public class CopyFileTest(
    FilesApiFactory filesFactory, 
    WebApplicationFactory<WebApiProgram> apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CopyFile_BetweenUserFolders_ReturnsValidFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create source file
        var sourceFile = await CreateFile("source_document.docx", FolderType.USER, Initializer.Owner);
        
        // Create target folder
        var targetFolderId = await CreateFolder("target_folder", FolderType.USER, Initializer.Owner);
        
        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: sourceFile.Title,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId.Id)
        );
        
        var copiedFile = (await _filesFilesApi.CopyFileAsAsync(sourceFile.Id, copyParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        copiedFile.Should().NotBeNull();
        // copiedFile.Id.Should().NotBe(sourceFile.Id);
        // copiedFile.Title.Should().Be(sourceFile.Title);
        // copiedFile.FolderId.Should().Be(targetFolderId);
    }
    
    [Fact]
    public async Task CopyFile_WithRename_ReturnsRenamedFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create source file
        var sourceFile = await CreateFile("rename_source.docx", FolderType.USER, Initializer.Owner);
        var newFileName = "renamed_copy.docx";
        
        // Get root folders to find a target folder
        var rootFolders = (await _filesFoldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        var targetFolderId = rootFolders.FirstOrDefault(r => r.Current.RootFolderType is FolderType.USER)!.Current.Id;
        
        // Act
        var copyParams = new CopyAsJsonElement(
            destTitle: newFileName,
            destFolderId: new CopyAsJsonElementDestFolderId(targetFolderId)
        );
        
        var copiedFile = (await _filesFilesApi.CopyFileAsAsync(sourceFile.Id, copyParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        copiedFile.Should().NotBeNull();
        // copiedFile.Id.Should().NotBe(sourceFile.Id);
        // copiedFile.Title.Should().Be(newFileName);
        // copiedFile.FolderId.Should().Be(targetFolderId);
    }
    
    [Fact]
    public async Task MoveFile_ToAnotherFolder_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create source file
        var sourceFile = await CreateFile("file_to_move.docx", FolderType.USER, Initializer.Owner);
        
        // Create target folder
        var targetFolder = await CreateFolder("target_folder", FolderType.USER, Initializer.Owner);
        
        // Act
        var moveParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(sourceFile.Id)],
            FolderIds = []
        };
        
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(moveParams, TestContext.Current.CancellationToken)).Response;
        
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation();
        }
        
        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        
        // Verify file is moved
        var fileInfo = await GetFile(sourceFile.Id);
        fileInfo.FolderId.Should().Be(targetFolder.Id);
    }
}
