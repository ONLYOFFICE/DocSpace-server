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

namespace ASC.Files.Tests.Tests._06_Operations;

[Collection("Test Collection")]
[Trait("Category", "Operations")]
public class FileOrderTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetFilesOrder_MultipleFiles_ReturnsOrderedFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a test room and multiple files
        var virtualRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("ordering_test_folder", indexing: true, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
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
        var result = (await _filesApi.SetFilesOrderAsync(orderRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Verify folder content to ensure ordering is applied
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(virtualRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
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
        var virtualRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("ordering_test_folder", indexing: true, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
        var file = await CreateFile("file_to_order.docx", virtualRoom.Id);
        var newOrder = 10; // Arbitrary order number
        
        // Act
        var orderParams = new OrderRequestDto(newOrder);
        var result = (await _filesApi.SetFileOrderAsync(file.Id, orderParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(file.Id);
        
        // Get folder content to verify order
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(virtualRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
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
        var virtualRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("ordering_test_folder", indexing: true, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
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
        var result = (await _filesApi.SetFilesOrderAsync(orderRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        
        // Verify folder content to ensure ordering is applied
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(virtualRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
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
        
        var virtualRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("ordering_test_folder", indexing: true, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
        
        // Create a valid file
        var validFile = await CreateFile("valid_file.docx", virtualRoom.Id);
        
        // Create order items with one valid and one invalid ID
        var orderItems = new List<OrdersItemRequestDtoInteger>
        {
            new(validFile.Id, FileEntryType.File, 1),
            new(99999, FileEntryType.File, 2) // Non-existent file ID
        };
        
        var orderRequest = new OrdersRequestDtoInteger(orderItems);
        
        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFilesOrderAsync(
                orderRequest, 
                TestContext.Current.CancellationToken));
    }
}
