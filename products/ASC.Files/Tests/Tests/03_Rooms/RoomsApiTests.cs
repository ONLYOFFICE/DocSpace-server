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

using ASC.Files.Tests.ApiFactories;

namespace ASC.Files.Tests.Tests._03_Rooms;

[Collection("Test Collection")]
[Trait("Category", "Rooms")]
public class RoomsApiTests(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CreateRoom_WithValidData_ReturnsNewRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var roomTitle = "Test Room " + Guid.NewGuid().ToString()[..8];
        
        // Act
        var createRequest = new CreateRoomRequestDto(
            title: roomTitle,
            indexing: true,
            roomType: RoomType.CustomRoom
        );
        
        var createdRoom = (await _roomsApi.CreateRoomAsync(createRequest, TestContext.Current.CancellationToken)).Response;
        
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(createdRoom.Id, TestContext.Current.CancellationToken)).Response;
        // Assert
        roomInfo.Should().NotBeNull();
        roomInfo.Title.Should().Be(roomTitle);
        roomInfo.RootFolderType.Should().Be(FolderType.VirtualRooms);
        roomInfo.RoomType.Should().Be(RoomType.CustomRoom);
    }
    
    [Fact]
    public async Task GetRoomInfo_ExistingRoom_ReturnsRoomData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var roomTitle = "Room for Info " + Guid.NewGuid().ToString()[..8];
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
        
        // Act
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(createdRoom.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        roomInfo.Should().NotBeNull();
        roomInfo.Id.Should().Be(createdRoom.Id);
        roomInfo.Title.Should().Be(roomTitle);
    }
    
    [Fact]
    public async Task SearchRoom_ExistingRoom_ReturnsRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var roomTitle = "Room for Info " + Guid.NewGuid().ToString()[..8];
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
        
        // Act
        var rooms = (await _roomsApi.GetRoomsFolderAsync(filterValue: roomTitle, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        rooms.Should().NotBeNull();
        rooms.Folders.Should().Contain(r => r.Title == createdRoom.Title);
    }
    
    [Fact]
    public async Task UpdateRoom_ChangeTitle_RoomUpdated()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var initialRoomTitle = "Initial Room " + Guid.NewGuid().ToString()[..8];
        var updatedRoomTitle = "Updated Room " + Guid.NewGuid().ToString()[..8];
        
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(initialRoomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
            
        // Act
        var updateRequest = new UpdateRoomRequest(
            title: updatedRoomTitle,
            indexing: true
        );
        
        var updatedRoom = (await _roomsApi.UpdateRoomAsync(createdRoom.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        updatedRoom.Should().NotBeNull();
        updatedRoom.Id.Should().Be(createdRoom.Id);
        updatedRoom.Title.Should().Be(updatedRoomTitle);
        
        // Verify the update persisted
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(createdRoom.Id, TestContext.Current.CancellationToken)).Response;
        roomInfo.Title.Should().Be(updatedRoomTitle);
    }
    
    [Fact]
    public async Task AddTags_ToRoom_TagsAdded()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room
        var roomTitle = "Tagged Room " + Guid.NewGuid().ToString()[..8];
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
            
        // First, create a tag
        var tagName = "TestTag" + Guid.NewGuid().ToString()[..5];
        var createTagRequest = new CreateTagRequestDto(tagName);
        
        var tag = (await _roomsApi.CreateRoomTagAsync(createTagRequest, TestContext.Current.CancellationToken)).Response;
        
        // Act
        var tagsRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        var taggedRoom = (await _roomsApi.AddRoomTagsAsync(room.Id, tagsRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        taggedRoom.Should().NotBeNull();
        
        // Verify tags were added
        var tagsInfo = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tagsInfo.Should().NotBeEmpty();
        tagsInfo.Should().Contain(t => t.ToString() == tagName);
    }
    
    [Fact]
    public async Task DeleteTags_FromRoom_TagsRemoved()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room
        var roomTitle = "Room with Tags " + Guid.NewGuid().ToString()[..8];
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
            
        // Create a tag
        var tagName = "RemovableTag" + Guid.NewGuid().ToString()[..5];
        var createTagRequest = new CreateTagRequestDto(tagName);
        
        var tag = (await _roomsApi.CreateRoomTagAsync(createTagRequest, TestContext.Current.CancellationToken)).Response;
        
        // Add the tag to the room
        var addTagsRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        await _roomsApi.AddRoomTagsAsync(room.Id, addTagsRequest, TestContext.Current.CancellationToken);
        
        // Act
        var deleteTagsRequest = new BatchTagsRequestDto
        {
            Names = [tag.ToString()!]
        };
        
        var result = (await _roomsApi.DeleteRoomTagsAsync(room.Id, deleteTagsRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task PinRoom_UnpinRoom_StatusChanges()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room
        var roomTitle = "Pinnable Room " + Guid.NewGuid().ToString()[..8];
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
            
        // Act - Pin the room
        var pinnedRoom = (await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert after pinning
        pinnedRoom.Should().NotBeNull();
        pinnedRoom.Pinned.Should().BeTrue();
        
        // Act - Unpin the room
        var unpinnedRoom = (await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert after unpinning
        unpinnedRoom.Should().NotBeNull();
        unpinnedRoom.Pinned.Should().BeFalse();
    }
    
    [Fact]
    public async Task GetRoomsFolder_ReturnsRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room to ensure we have at least one
        var roomTitle = "Room for Listing " + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken);
        
        // Act
        var roomsFolder = (await _roomsApi.GetRoomsFolderAsync(
            type: [RoomType.CustomRoom],
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        roomsFolder.Should().NotBeNull();
        roomsFolder.Folders.Should().NotBeEmpty();
        //roomsFolder.Folders.Should().Contain(f => f.Type == FolderType.CustomRoom);
    }
    
    // [Fact]
    // public async Task SetRoomSecurity_InviteUser_AccessGranted()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     
    //     // Create a room
    //     var roomTitle = "Secure Room " + Guid.NewGuid().ToString()[..8];
    //     var room = (await _filesRoomsApi.CreateRoomAsync(
    //         new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
    //         TestContext.Current.CancellationToken)).Response;
    //         
    //     // Act - Invite a user (using user2)
    //     var invitationRequest = new RoomInvitationRequest
    //     {
    //         Invitations = new List<RoomInvitation>
    //         {
    //             new()
    //             {
    //                 Id = Initializer.User2.Id.ToString(),
    //                 IsGroup = false,
    //                 Access = FileShare.ReadWrite
    //             }
    //         }
    //     };
    //     
    //     var securityResult = (await _filesRoomsApi.SetRoomSecurityAsync(
    //         room.Id, 
    //         invitationRequest, 
    //         TestContext.Current.CancellationToken)).Response;
    //     
    //     // Assert
    //     securityResult.Should().NotBeNull();
    //     
    //     // Verify security info
    //     var securityInfo = (await _filesRoomsApi.GetRoomSecurityInfoAsync(
    //         room.Id,
    //         cancellationToken: TestContext.Current.CancellationToken)).Response;
    //         
    //     securityInfo.Should().NotBeNull();
    //     securityInfo.Should().Contain(s => s.SubjectId == Initializer.User2.Id);
    // }
    
    [Fact]
    public async Task IsPublic_ChecksRoomStatus()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room
        var roomTitle = "Visibility Room " + Guid.NewGuid().ToString()[..8];
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
        
        // Act - Check if the room is public
        var isPublicResult = (await _roomsApi.GetPublicSettingsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        isPublicResult.Should().BeFalse();
        
        // Make room public
        var setPublicRequest = new SetPublicDto
        {
            Id = room.Id,
            Public = true
        };
        
        await _roomsApi.SetPublicSettingsAsync(setPublicRequest, TestContext.Current.CancellationToken);
        
        // Verify room is now public
        var isPublicAfter = (await _roomsApi.GetPublicSettingsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        isPublicAfter.Should().BeTrue();
    }
    
    [Fact]
    public async Task GetRoomsNewItems_ReturnsData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Act
        var newItems = (await _roomsApi.GetRoomsNewItemsAsync(TestContext.Current.CancellationToken)).Response;
        
        // Assert
        newItems.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CreateDocxFile_InRoom_ReturnsFileData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a room
        var roomTitle = "Room for DocX " + Guid.NewGuid().ToString()[..8];
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.CustomRoom), 
            TestContext.Current.CancellationToken)).Response;
            
        // Act
        var fileName = "Test Document.docx";
        var file = await CreateFile(fileName, createdRoom.Id);
        
        // Assert
        file.Should().NotBeNull();
        file.Title.Should().Be(fileName);
        file.FileExst.Should().Be(".docx");
        
        // Verify a file exists in the room's contents
        var roomFiles = (await _foldersApi.GetFolderByFolderIdAsync(
            createdRoom.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
            
        roomFiles.Should().NotBeNull();
        roomFiles.Files.Should().NotBeEmpty();
        roomFiles.Files.Should().Contain(f => f.Title == fileName);
    }
}
