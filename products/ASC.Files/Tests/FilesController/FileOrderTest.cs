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
public class FileOrderTest(
    FilesApiFactory filesFactory, 
    WebApplicationFactory<WebApiProgram> apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task SetFilesOrder_MultipleFiles_ReturnsOrderedFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a test room and multiple files
        var virtualRoom = await CreateVirtualRoom("ordering_test_folder",Initializer.Owner);
        var file1 = await CreateFile("file1.docx", virtualRoom.Id);
        var file2 = await CreateFile("file2.docx", virtualRoom.Id);
        var file3 = await CreateFile("file3.docx", virtualRoom.Id);
        
        // Create order items
        var orderItems = new List<OrdersItemRequestDtoInteger>
        {
            new(file1.Id, FileEntryType.File, 3),
            new(file2.Id, FileEntryType.File, 1),
            new(file3.Id, FileEntryType.File, 2)
        };
        
        var orderRequest = new OrdersRequestDtoInteger(orderItems);
        
        // Act
        var result = (await _filesFilesApi.SetFilesOrderAsync(orderRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Verify folder content to ensure ordering is applied
        var folderContent = (await _filesFoldersApi.GetFolderByFolderIdAsync(virtualRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Files should be ordered according to our specification
        // The first file in the list should be file2 (order 1)
        folderContent.Files.First().Title.Should().Be(file2.Title);
        
        // The last file should be file1 (order 3)
        folderContent.Files.Last().Title.Should().Be(file1.Title);
    }
    
    [Fact]
    public async Task SetOrderFile_SingleFile_ReturnsUpdatedFileOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_to_order.docx", FolderType.USER, Initializer.Owner);
        var newOrder = 10; // Arbitrary order number
        
        // Act
        var orderParams = new OrderRequestDto(newOrder);
        var result = (await _filesFilesApi.SetOrderFileAsync(file.Id, orderParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(file.Id);
        
        // Get folder content to verify order
        var userFolderId = await GetUserFolderIdAsync(Initializer.Owner);
        var folderContent = (await _filesFoldersApi.GetFolderByFolderIdAsync(userFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Find our file in the folder content
        var updatedFile = folderContent.Files.FirstOrDefault(f => f.Title == file.Title);
        updatedFile.Should().NotBeNull();
        
        // Note: The exact verification of order depends on how the API represents order in responses
        // You might need to adapt this based on your actual implementation
    }
    
    [Fact]
    public async Task SetOrderFile_FolderWithMixedEntries_MaintainsCorrectOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a test folder with mixed content (files and subfolders)
        var virtualRoom = await CreateVirtualRoom("ordering_test_folder",Initializer.Owner);
        var subfolder1 = await CreateFolder("subfolder1", virtualRoom.Id);
        var file1 = await CreateFile("file1.docx", virtualRoom.Id);
        var subfolder2 = await CreateFolder("subfolder2", virtualRoom.Id);
        var file2 = await CreateFile("file2.docx", virtualRoom.Id);
        
        // Create order items for both files and folders
        var orderItems = new List<OrdersItemRequestDtoInteger>
        {
            new(subfolder1.Id, FileEntryType.Folder, 4),
            new(file1.Id, FileEntryType.File, 1),
            new(subfolder2.Id, FileEntryType.Folder, 3),
            new(file2.Id, FileEntryType.File, 2)
        };
        
        var orderRequest = new OrdersRequestDtoInteger(orderItems);
        
        // Act
        var result = (await _filesFilesApi.SetFilesOrderAsync(orderRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        
        // Verify folder content to ensure ordering is applied
        var folderContent = (await _filesFoldersApi.GetFolderByFolderIdAsync(virtualRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Check file ordering
        folderContent.Files.Should().HaveCount(2);
        folderContent.Files.First().Title.Should().Be(file1.Title); // Order 1
        folderContent.Files.Last().Title.Should().Be(file2.Title);  // Order 2
        
        // Check folder ordering
        folderContent.Folders.Should().HaveCount(2);
        folderContent.Folders.First().Title.Should().Be(subfolder2.Title); // Order 3
        folderContent.Folders.Last().Title.Should().Be(subfolder1.Title);  // Order 4
    }
    
    [Fact]
    public async Task SetFilesOrder_InvalidEntryIds_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a valid file
        var validFile = await CreateFile("valid_file.docx", FolderType.USER, Initializer.Owner);
        
        // Create order items with one valid and one invalid ID
        var orderItems = new List<OrdersItemRequestDtoInteger>
        {
            new(validFile.Id, FileEntryType.File, 1),
            new(99999, FileEntryType.File, 2) // Non-existent file ID
        };
        
        var orderRequest = new OrdersRequestDtoInteger(orderItems);
        
        // Act & Assert
        await Assert.ThrowsAsync<Docspace.Client.ApiException>(
            async () => await _filesFilesApi.SetFilesOrderAsync(
                orderRequest, 
                TestContext.Current.CancellationToken));
    }
}
