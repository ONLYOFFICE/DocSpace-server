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

extern alias ASCWebApi;
extern alias ASCPeople;
using ASC.Files.Tests.ApiFactories;

namespace ASC.Files.Tests.Tests._02_Folders;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderCreateTests(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
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
                await GetFolderIdAsync(FolderType.USER, Initializer.Owner), 
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
        var parentFolderId = await GetFolderIdAsync(FolderType.USER, Initializer.Owner);
        
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
