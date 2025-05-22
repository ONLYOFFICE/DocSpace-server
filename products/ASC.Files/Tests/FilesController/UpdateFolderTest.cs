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

using ASC.Files.Tests.Factory;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class UpdateFolderTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task RenameFolder_ChangesFolderName_ReturnsUpdatedFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a folder to rename
        var folder = await CreateFolder("folder_to_rename", FolderType.USER, Initializer.Owner);
        var newFolderName = "renamed_folder";
        
        // Act
        var renameParams = new CreateFolder(newFolderName);
        var renamedFolder = (await _filesFoldersApi.RenameFolderAsync(folder.Id, renameParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        renamedFolder.Should().NotBeNull();
        renamedFolder.Id.Should().Be(folder.Id);
        renamedFolder.Title.Should().Be(newFolderName);
    }
    
    [Fact]
    public async Task RenameFolder_NameLongerThan165Chars_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdFolder = await CreateFolder("folder_to_rename", FolderType.USER, Initializer.Owner);
        var longFolderName = new string('a', 166);
        var updateParams = new CreateFolder(longFolderName);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Docspace.Client.ApiException>(
            async () => await _filesFoldersApi.RenameFolderAsync(
                createdFolder.Id, 
                updateParams, 
                cancellationToken: TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(400);
    }
    
    [Fact]
    public async Task DeleteFolder_RemovesFolderAndContents_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create folder with file inside
        var folder = await CreateFolder("folder_to_delete", FolderType.USER, Initializer.Owner);
        await CreateFile("test_file.docx", folder.Id);
        
        // Act
        var deleteParams = new DeleteFolder(deleteAfter: false, immediately: true);
        var results = (await _filesFoldersApi.DeleteFolderAsync(folder.Id, deleteParams, TestContext.Current.CancellationToken)).Response;
        
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation();
        }
        
        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        
        // Verify folder no longer exists or has been moved to trash
        await Assert.ThrowsAsync<Docspace.Client.ApiException>(async () => 
            await _filesFoldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task GetFolderContent_ReturnsFilesAndFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create parent folder
        var parentFolder = await CreateFolder("parent_folder", FolderType.USER, Initializer.Owner);
        
        // Create subfolders and files
        await CreateFolder("subfolder1", parentFolder.Id);
        await CreateFolder("subfolder2", parentFolder.Id);
        await CreateFile("file1.docx", parentFolder.Id);
        await CreateFile("file2.docx", parentFolder.Id);
        
        // Act
        var folderContent = (await _filesFoldersApi.GetFolderByFolderIdAsync(parentFolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        folderContent.Should().NotBeNull();
        folderContent.Current.Id.Should().Be(parentFolder.Id);
        folderContent.Folders.Should().HaveCount(2);
        folderContent.Files.Should().HaveCount(2);
        
        folderContent.Folders.Should().Contain(f => f.Title == "subfolder1");
        folderContent.Folders.Should().Contain(f => f.Title == "subfolder2");
        folderContent.Files.Should().Contain(f => f.Title == "file1.docx");
        folderContent.Files.Should().Contain(f => f.Title == "file2.docx");
    }
}
