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

namespace ASC.Files.Tests.Tests._02_Folders;

[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task RenameFolder_ChangesFolderName_ReturnsUpdatedFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        // Create a folder to rename
        var folder = await CreateFolder("folder_to_rename", FolderType.USER, Owner);
        var newFolderName = "renamed_folder";
        
        // Act
        var renameParams = new CreateFolder(newFolderName);
        var renamedFolder = (await _foldersApi.RenameFolderAsync(folder.Id, renameParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        renamedFolder.Should().NotBeNull();
        renamedFolder.Id.Should().Be(folder.Id);
        renamedFolder.Title.Should().Be(newFolderName);
    }
    
    [Fact]
    public async Task RenameFolder_NameLongerThan165Chars_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdFolder = await CreateFolder("folder_to_rename", FolderType.USER, Owner);
        var longFolderName = new string('a', 166);
        var updateParams = new CreateFolder(longFolderName);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.RenameFolderAsync(
                createdFolder.Id, 
                updateParams, 
                cancellationToken: TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(400);
    }
    
    [Fact]
    public async Task DeleteFolder_RemovesFolderAndContents_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        // Create folder with file inside
        var folder = await CreateFolder("folder_to_delete", FolderType.USER, Owner);
        await CreateFile("test_file.docx", folder.Id);
        
        // Act
        var deleteParams = new DeleteFolder(deleteAfter: false, immediately: true);
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, deleteParams, TestContext.Current.CancellationToken)).Response;
        
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation();
        }
        
        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        
        // Verify folder no longer exists or has been moved to trash
        await Assert.ThrowsAsync<ApiException>(async () => 
            await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task GetFolderContent_ReturnsFilesAndFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        // Create parent folder
        var parentFolder = await CreateFolder("parent_folder", FolderType.USER, Owner);
        
        // Create subfolders and files
        await CreateFolder("subfolder1", parentFolder.Id);
        await CreateFolder("subfolder2", parentFolder.Id);
        await CreateFile("file1.docx", parentFolder.Id);
        await CreateFile("file2.docx", parentFolder.Id);
        
        // Act
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(parentFolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
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
