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
public class TagsApiTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CreateTag_ValidData_TagCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var tagName = "TestTag" + Guid.NewGuid().ToString()[..5];

        var createTagRequest = new CreateTagRequestDto(tagName);
        
        // Act
        var tag = (await _roomsApi.CreateRoomTagAsync(createTagRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        tag.Should().NotBeNull();
        tag.Should().NotBeNull();
        
        // Verify tag exists in system
        var tagsInfo = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tagsInfo.Should().Contain(t => t.ToString() == tagName);
    }
    
    [Fact]
    public async Task GetTagsInfo_ReturnsAllTags()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a tag to ensure we have at least one
        var tagName = "ListableTag" + Guid.NewGuid().ToString()[..5];
        var createTagRequest = new CreateTagRequestDto(tagName);
        
        await _roomsApi.CreateRoomTagAsync(createTagRequest, TestContext.Current.CancellationToken);
        
        // Act
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        tags.Should().NotBeNull();
        tags.Should().NotBeEmpty();
        tags.Should().Contain(t => t.ToString() == tagName);
    }
    
    [Fact]
    public async Task DeleteCustomTags_RemovesTags()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a tag
        var tagName = "DeletableTag" + Guid.NewGuid().ToString()[..5];
        var createTagRequest = new CreateTagRequestDto(tagName);
        
        var tag = (await _roomsApi.CreateRoomTagAsync(createTagRequest, TestContext.Current.CancellationToken)).Response;
        
        // Act
        var deleteRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        await _roomsApi.DeleteCustomTagsAsync(deleteRequest, TestContext.Current.CancellationToken);
        
        // Assert - Verify tag is gone
        var tagsAfterDelete = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tagsAfterDelete.Should().NotContain(t => t.ToString() == tagName);
    }
    
    [Fact]
    public async Task TagLifecycle_CreateAddDeleteTag_WorksCorrectly()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room
        var roomTitle = "Room for Tags " + Guid.NewGuid().ToString()[..8];
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
            
        // 1. Create a tag
        var tagName = "LifecycleTag" + Guid.NewGuid().ToString()[..5];
        var createTagRequest = new CreateTagRequestDto(tagName);
        
        var tag = (await _roomsApi.CreateRoomTagAsync(createTagRequest, TestContext.Current.CancellationToken)).Response;
        
        // Verify tag exists
        var tagsAfterCreate = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tagsAfterCreate.Should().Contain(t => t.ToString() == tagName);
        
        // 2. Add tag to a room
        var addTagsRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        var roomWithTag = (await _roomsApi.AddRoomTagsAsync(room.Id, addTagsRequest, TestContext.Current.CancellationToken)).Response;
        roomWithTag.Should().NotBeNull();
        
        // 3. Delete tag from the room
        var deleteFromRoomRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        var roomWithoutTag = (await _roomsApi.DeleteRoomTagsAsync(room.Id, deleteFromRoomRequest, TestContext.Current.CancellationToken)).Response;
        roomWithoutTag.Should().NotBeNull();
        
        // 4. Delete tag completely
        var deleteTagRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        await _roomsApi.DeleteCustomTagsAsync(deleteTagRequest, TestContext.Current.CancellationToken);
        
        // Verify tag is gone
        var tagsAfterDelete = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tagsAfterDelete.Should().NotContain(t => t.ToString() == tagName);
    }
    
    [Fact]
    public async Task AddTags_SingleAndMultipleTags_TagsAddedToRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a test room
        var roomTitle = "Room for Tags Test " + Guid.NewGuid().ToString()[..8];
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
        
        // Create first tag
        var tagName1 = "TestTag1-" + Guid.NewGuid().ToString()[..5];
        var createTagRequest1 = new CreateTagRequestDto(tagName1);
        var tag1 = (await _roomsApi.CreateRoomTagAsync(createTagRequest1, TestContext.Current.CancellationToken)).Response;
        
        // Create a second tag
        var tagName2 = "TestTag2-" + Guid.NewGuid().ToString()[..5];
        var createTagRequest2 = new CreateTagRequestDto(tagName2);
        var tag2 = (await _roomsApi.CreateRoomTagAsync(createTagRequest2, TestContext.Current.CancellationToken)).Response;
        
        // Part 1: Test adding a single tag to a room
        // Act
        var addSingleTagRequest = new BatchTagsRequestDto
        {
            Names = [tag1.ToString()!]
        };
        
        var roomWithSingleTag = (await _roomsApi.AddRoomTagsAsync(room.Id, addSingleTagRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        roomWithSingleTag.Should().NotBeNull();
        roomWithSingleTag.Tags.Should().NotBeNull();
        roomWithSingleTag.Tags.Should().Contain(t => t.ToString() == tagName1);
        
        // Part 2: Test adding multiple tags to a room
        // Act
        var addMultipleTagsRequest = new BatchTagsRequestDto
        {
            Names = [tag2.ToString()!]
        };
        
        var roomWithMultipleTags = (await _roomsApi.AddRoomTagsAsync(room.Id, addMultipleTagsRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        roomWithMultipleTags.Should().NotBeNull();
        roomWithMultipleTags.Tags.Should().NotBeNull();
        roomWithMultipleTags.Tags.Should().Contain(t => t.ToString() == tagName1); // First tag still present
        roomWithMultipleTags.Tags.Should().Contain(t => t.ToString() == tagName2); // Second tag added
        
        // Part 3: Test adding a batch of multiple tags at once
        // Create a third tag
        var tagName3 = "TestTag3-" + Guid.NewGuid().ToString()[..5];
        var createTagRequest3 = new CreateTagRequestDto(tagName3);
        var tag3 = (await _roomsApi.CreateRoomTagAsync(createTagRequest3, TestContext.Current.CancellationToken)).Response;
        
        // Create a fourth tag
        var tagName4 = "TestTag4-" + Guid.NewGuid().ToString()[..5];
        var createTagRequest4 = new CreateTagRequestDto(tagName4);
        var tag4 = (await _roomsApi.CreateRoomTagAsync(createTagRequest4, TestContext.Current.CancellationToken)).Response;
        
        // Act - add two tags at once
        var addBatchTagsRequest = new BatchTagsRequestDto
        {
            Names = [tag3.ToString()!, tag4.ToString()!]
        };
        
        var roomWithBatchTags = (await _roomsApi.AddRoomTagsAsync(room.Id, addBatchTagsRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        roomWithBatchTags.Should().NotBeNull();
        roomWithBatchTags.Tags.Should().NotBeNull();
        roomWithBatchTags.Tags.Should().Contain(t => t.ToString() == tagName1); // First tag still present
        roomWithBatchTags.Tags.Should().Contain(t => t.ToString() == tagName2); // Second tag still present
        roomWithBatchTags.Tags.Should().Contain(t => t.ToString() == tagName3); // Third tag added
        roomWithBatchTags.Tags.Should().Contain(t => t.ToString() == tagName4); // Fourth tag added
    }
}
