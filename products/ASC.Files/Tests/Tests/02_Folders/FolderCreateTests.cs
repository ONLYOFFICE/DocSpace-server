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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<string> FolderNames =>
    [
        "Folder with spaces",
        "Special_Chars-Folder",
        "Nested.Folder"
    ];

    public static TheoryData<FolderType> SystemFolderTypesData =>
    [
        FolderType.Archive,
        FolderType.TRASH,
        FolderType.VirtualRooms
    ];
    
    [Fact]
    public async Task CreateFolder_InMyDocuments_Owner_ReturnsOk()
    {
        var folderName = "Test folder";
        var createdFolder = await CreateFolder(folderName, FolderType.USER, Initializer.Owner);
        
        createdFolder.Should().NotBeNull();
        createdFolder.Title.Should().Be(folderName);
    }
    
    [Fact]
    public async Task CreateFolder_InMyDocuments_RoomAdmin_ReturnsOk()
    {
        var folderName = "Test folder";
        var roomAdmin = await Initializer.InviteContact(EmployeeType.RoomAdmin);
        
        var createdFolder = await CreateFolder(folderName, FolderType.USER, roomAdmin);
        
        createdFolder.Should().NotBeNull();
        createdFolder.Title.Should().Be(folderName);
    }
    
    [Fact]
    public async Task CreateFolder_InMyDocuments_User_ReturnsOk()
    {
        var folderName = "Test folder";
        var user = await Initializer.InviteContact(EmployeeType.User);
        
        var createdFolder = await CreateFolder(folderName, FolderType.USER, user);

        createdFolder.Should().NotBeNull();
        createdFolder.Title.Should().Be(folderName);
    }
    
    [Theory]
    [MemberData(nameof(FolderNames))]
    public async Task CreateVariousFolder_InMyDocuments_Owner_ReturnsOk(string folderName)
    {
        var createdFolder = await CreateFolder(folderName, FolderType.USER, Initializer.Owner);
        
        createdFolder.Should().NotBeNull();
        createdFolder.Title.Should().Be(folderName);
    }
    
    [Fact]
    public async Task CreateFolder_ParentFolderDoesNotExist_ReturnsFail()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Arrange
        var folderRequest = new CreateFolder("Test Folder");
        
        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderAsync(
                Random.Shared.Next(10000, 20000), 
                folderRequest, 
                cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Theory]
    [MemberData(nameof(SystemFolderTypesData))]
    public async Task CreateFolder_InSystemFolder_Owner_ReturnsOk(FolderType folderType)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateFolder("Test System Folder", folderType, Initializer.Owner));

        exception.ErrorCode.Should().Be(403);
    }
    
    [Fact]
    public async Task CreateFolder_NameLongerThan165Chars_Returns400()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Arrange
        var longFolderName = new string('a', 166); // 166 characters
        var folderRequest = new CreateFolder(longFolderName);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderAsync(
                await GetUserFolderIdAsync( Initializer.Owner), 
                folderRequest, 
                cancellationToken: TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(400);
    }
    
    [Fact]
    public async Task CreateFolder_WithSameNameInSameParent_ReturnsFolderExistsError()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Arrange
        var folderName = "Duplicate Folder";
        var parentFolderId = await GetUserFolderIdAsync( Initializer.Owner);
        
        // Create first folder
        var firstFolder = await CreateFolder(folderName, parentFolderId);
        firstFolder.Should().NotBeNull();
        
        // Act & Assert
        var secondFolder =  await CreateFolder(folderName, parentFolderId);
        
        // Assert
        secondFolder.Should().NotBeNull();
        secondFolder.Title.Should().Be(folderName);
        secondFolder.ParentId.Should().Be(parentFolderId);
    }
    
    [Fact]
    public async Task CreateNestedFolders_ReturnsOk()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Arrange
        var parentFolderName = "Parent Folder";
        var childFolderName = "Child Folder";
        
        // Create parent folder
        var parentFolder = await CreateFolder(parentFolderName, FolderType.USER, Initializer.Owner);
        parentFolder.Should().NotBeNull();
        
        // Create child folder inside parent folder
        var childFolder = await CreateFolder(childFolderName, parentFolder.Id);
        
        // Assert
        childFolder.Should().NotBeNull();
        childFolder.Title.Should().Be(childFolderName);
        childFolder.ParentId.Should().Be(parentFolder.Id);
    }
}
