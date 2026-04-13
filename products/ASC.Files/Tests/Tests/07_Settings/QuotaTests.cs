// (c) Copyright Ascensio System SIA 2009-2026
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
using System.Reflection;


using QuotaSettingsRequestsDto = DocSpace.API.SDK.Model.QuotaSettingsRequestsDto;

namespace ASC.Files.Tests.Tests._07_Settings;

[Collection("Test Collection")]
[Trait("Category", "Settings")]
[Trait("Feature", "Quota")]
public class QuotaTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ResetRoomQuota_ValidRoomIds_ResetsQuotaSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
       
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
        
        // Create a test room
        var roomTitle = "Room for Quota Reset Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle);
        
        // Define a quota limit (in bytes)
        var quotaLimit = 2147483648; // 2 GB
        
        // Set up request to update quota for the room
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {           
            RoomIds = [new(room.Id)],
            Quota = quotaLimit
        };
        
       await _quotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken);
        
        // Set up request to reset quota for the room
        var resetRequest = new UpdateRoomsRoomIdsRequestDtoInteger
        {
            RoomIds = [new(room.Id)]
        };
        
        // Act
        var result = (await _quotaApi.ResetRoomQuotaAsync(resetRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(room.Id);
        result[0].QuotaLimit.Should().Be(defaultQuotaLimit);
    }
    
    [Fact]
    public async Task ResetRoomQuota_MultipleRooms_ResetsQuotaForAll()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create test rooms
        var roomTitle1 = "Room 1 for Multi Quota Reset " + Guid.NewGuid().ToString()[..8];
        var roomTitle2 = "Room 2 for Multi Quota Reset " + Guid.NewGuid().ToString()[..8];
        
        var room1 = await CreateVirtualRoom(roomTitle1);
        var room2 = await CreateVirtualRoom(roomTitle2);
        
        // Set up request to reset quota for multiple rooms
        var resetRequest = new UpdateRoomsRoomIdsRequestDtoInteger
        {
            RoomIds = [new(room1.Id), new(room2.Id)]
        };
        
        // Define a quota limit (in bytes)
        var quotaLimit = 2147483648; // 2 GB
        
        // Set up request to update quota for the room
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {           
            RoomIds =  [new(room1.Id), new(room2.Id)],
            Quota = quotaLimit
        };
        
        await _quotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken);
        
        // Act
        var result = (await _quotaApi.ResetRoomQuotaAsync(resetRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result[0].Id.Should().Be(room1.Id);
        result[0].QuotaLimit.Should().Be(defaultQuotaLimit);
        result[1].Id.Should().Be(room2.Id);
        result[1].QuotaLimit.Should().Be(defaultQuotaLimit);
    }
    
    [Fact]
    public async Task UpdateRoomsQuota_ValidRoomAndQuota_UpdatesQuotaSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a test room
        var roomTitle = "Room for Quota Update Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle);
        
        // Define a quota limit (in bytes)
        var quotaLimit = 2147483648; // 2 GB
        
        // Set up request to update quota for the room
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {           
            RoomIds = [new(room.Id)],
            Quota = quotaLimit
        };
        
        // Act
        var result = (await _quotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(room.Id);
        result[0].QuotaLimit.Should().Be(quotaLimit);
    }
    
    [Fact]
    public async Task UpdateRoomsQuota_MultipleRoomsWithDifferentQuotas_UpdatesSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create test rooms
        var roomTitle1 = "Room 1 for Multi Quota Update " + Guid.NewGuid().ToString()[..8];
        var roomTitle2 = "Room 2 for Multi Quota Update " + Guid.NewGuid().ToString()[..8];
        
        var room1 = await CreateVirtualRoom(roomTitle1);
        var room2 = await CreateVirtualRoom(roomTitle2);
        
        // Define a quota limit (in bytes)
        var quotaLimit = 2147483648; // 2 GB
        
        // Set up request to update quota for multiple rooms
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {           
            RoomIds = [new(room1.Id), new(room2.Id)],
            Quota = quotaLimit
        };
        
        // Act
        var result = (await _quotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result[0].Id.Should().Be(room1.Id);
        result[0].QuotaLimit.Should().Be(quotaLimit);
        result[1].Id.Should().Be(room2.Id);
        result[1].QuotaLimit.Should().Be(quotaLimit);
    }
    
    [Fact]
    public async Task UpdateRoomsQuota_ZeroQuota_ShouldResetQuota()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a test room
        var roomTitle = "Room for Zero Quota Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle);
        
        // Set up a request with zero quotas (should be the same as reset)
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {
            RoomIds = [new(room.Id)],
            Quota = 0
        };
        
        // Act
        var result = (await _quotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(room.Id);
        result[0].QuotaLimit.Should().Be(0);
    }
    
    [Fact]
    public async Task QuotaLifecycle_SetAndResetQuota_ManagesQuotaCorrectly()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a test room
        var roomTitle = "Room for Quota Lifecycle Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle);
        
        // Step 1: Set a quota
        var quotaLimit = 5368709120; // 5 GB
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {
            RoomIds = [new(room.Id)],
            Quota = quotaLimit
        };
        
        var updateResult = (await _quotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        updateResult[0].Id.Should().Be(room.Id);
        updateResult[0].QuotaLimit.Should().Be(quotaLimit);
        
        // Step 2: Reset the quota
        var resetRequest = new UpdateRoomsRoomIdsRequestDtoInteger
        {
            RoomIds = [new(room.Id)],
        };
        
        var resetResult = (await _quotaApi.ResetRoomQuotaAsync(resetRequest, TestContext.Current.CancellationToken)).Response;
        resetResult[0].Id.Should().Be(room.Id);
        resetResult[0].QuotaLimit.Should().Be(defaultQuotaLimit);
    }
    
    [Fact]
    public async Task CreateFile_InRoomWithDefaultBigQuota_SuccessfullyUploads()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
        
        // Create a room
        var roomTitle = "Room for Quota Test " + Guid.NewGuid().ToString()[..8];
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, roomType: RoomType.VirtualDataRoom), 
            TestContext.Current.CancellationToken)).Response;
        
        const string fileName = "Test Document.docx";
        _ = await CreateFile(fileName, createdRoom.Id);
        
        // Verify a file exists in the room's contents
        var roomFiles = (await _foldersApi.GetFolderByFolderIdAsync(
            createdRoom.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
            
        roomFiles.Should().NotBeNull();
        roomFiles.Files.Should().NotBeEmpty();
        roomFiles.Files.Should().Contain(f => f.Title == fileName);
    }
    
    [Fact]
    public async Task CreateFile_InRoomWithSmallerQuota_SuccessfullyUploads()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
       
        // Set a smaller quota for the room (100 KB)
        var smallQuotaLimit = 102400; // 100 KB
        
        // Create a room
        var roomTitle = "Room for Quota Test " + Guid.NewGuid().ToString()[..8];
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, roomType: RoomType.VirtualDataRoom, quota: smallQuotaLimit), 
            TestContext.Current.CancellationToken)).Response;
        
        const string fileName = "Test Document.docx";
        _ = await CreateFile(fileName, createdRoom.Id);
        
        // Verify a file exists in the room's contents
        var roomFiles = (await _foldersApi.GetFolderByFolderIdAsync(
            createdRoom.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
            
        roomFiles.Should().NotBeNull();
        roomFiles.Files.Should().NotBeEmpty();
        roomFiles.Files.Should().Contain(f => f.Title == fileName);
    }
    
    [Fact]
    public async Task CreateFile_InRoomWithSmallerQuotaThenFileSize_ReturnsFail()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true,  new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
       
        // Set a smaller quota for the room (1 B)
        var minimalQuotaLimit = 1; // 1 B
        
        // Create a room
        var roomTitle = "Room for Quota Test " + Guid.NewGuid().ToString()[..8];
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(roomTitle, roomType: RoomType.VirtualDataRoom, quota: minimalQuotaLimit), 
            TestContext.Current.CancellationToken)).Response;
        
        await Assert.ThrowsAsync<ApiException>(async () => await CreateFile("Test Document.docx", createdRoom.Id));
    }

    [Fact]
    public async Task CreateFile_RoomQuotaExceededButLessThan2x_AllowedOnce()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);

        const int defaultQuotaLimit = 4 * 1024; // 4 KB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a room
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(
                "Room quota soft-limit " + Guid.NewGuid().ToString()[..8],
                 roomType: RoomType.VirtualDataRoom),
            TestContext.Current.CancellationToken)).Response;

        const string fileName = "Test Document.docx";
        _ = await CreateFile(fileName, createdRoom.Id);

        // Verify a file exists in the room's contents
        var roomFiles = (await _foldersApi.GetFolderByFolderIdAsync(
            createdRoom.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        roomFiles.Should().NotBeNull();
        roomFiles.Files.Should().NotBeEmpty();
        roomFiles.Files.Should().Contain(f => f.Title == fileName);
    }

    [Fact]
    public async Task CreateFile_RoomQuotaExceededMoreThan2x_ReturnsFail()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);

        const int defaultQuotaLimit = 3 * 1024; // 3 KB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a room
        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(
                "Room quota soft-limit " + Guid.NewGuid().ToString()[..8],
                 roomType: RoomType.VirtualDataRoom),
            TestContext.Current.CancellationToken)).Response;

        await Assert.ThrowsAsync<ApiException>(async () => await CreateFile("Test Document.docx", createdRoom.Id));
    }

    [Fact]
    public async Task CreateFile_RoomQuotaExceededSecondTime_ReturnsFail()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);

        const int defaultQuotaLimit = 4 * 1024; // 4 KB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        var createdRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(
                "Room quota once " + Guid.NewGuid().ToString()[..8],
                roomType: RoomType.VirtualDataRoom),
            TestContext.Current.CancellationToken)).Response;

        const string firstFileName = "Test Document.docx";
        _ = await CreateFile(firstFileName, createdRoom.Id);

        var roomFiles = (await _foldersApi.GetFolderByFolderIdAsync(
            createdRoom.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        roomFiles.Should().NotBeNull();
        roomFiles.Files.Should().NotBeEmpty();
        roomFiles.Files.Should().Contain(f => f.Title == firstFileName);

        const string secondFileName = "second.docx";
        await Assert.ThrowsAsync<ApiException>(async () =>
            await CreateFile(secondFileName, createdRoom.Id));
    }

    [Fact]
    public async Task UploadFile_ToRoomWithQuotaExceededLessThan2x_Allowed()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);

        const int defaultQuotaLimit = 4 * 1024; // 4 KB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
        const string fileName = "new.docx";

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(
                "Upload room quota soft-limit " + Guid.NewGuid().ToString()[..8],
                roomType: RoomType.VirtualDataRoom),
            TestContext.Current.CancellationToken)).Response;

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(
            TestContext.Current.CancellationToken)).Response;

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream =
            assembly.GetManifestResourceStream($"ASC.Files.Tests.Data.{fileName}")!;

        var contentLength = stream.Length;

        var createdSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest(fileName, contentLength),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        createdSession.Should().NotBeNull();
        createdSession.Id.Should().NotBeEmpty();

        var chunkSize = (int)settings.ChunkUploadSize;
        var buffer = new byte[chunkSize];
        var chunkNumber = 1;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(
                   buffer.AsMemory(0, chunkSize),
                   TestContext.Current.CancellationToken)) > 0)
        {
            await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
            var fileParameter = new FileParameter(chunkStream);

            await _filesOperationsApi.UploadAsyncSessionAsync(
                room.Id,
                createdSession.Id,
                chunkNumber,
                fileParameter,
                TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        var resultFile = (await _filesOperationsApi.FinalizeSessionAsync(
            room.Id,
            createdSession.Id,
            TestContext.Current.CancellationToken)).Response;

        resultFile.Should().NotBeNull();
        resultFile.Uploaded.Should().BeTrue();
        resultFile.Title.Should().Be(fileName);
        resultFile.File.Should().NotBeNull();
        resultFile.File.FolderId.Should().Be(room.Id);

        var roomFiles = (await _foldersApi.GetFolderByFolderIdAsync(
            room.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        roomFiles.Files.Should().Contain(f => f.Title == fileName);
    }

    [Fact]
    public async Task UploadFile_ToRoomWithQuotaExceededMoreThan2x_ReturnsFail()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _webApiClient.Authenticate(Initializer.Owner);

        const int defaultQuotaLimit = 3 * 1024; // 3 KB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
        const string fileName = "new.docx";

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(
                "Upload room quota hard-limit " + Guid.NewGuid().ToString()[..8],
                roomType: RoomType.VirtualDataRoom),
            TestContext.Current.CancellationToken)).Response;

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(
            TestContext.Current.CancellationToken)).Response;

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream =
            assembly.GetManifestResourceStream($"ASC.Files.Tests.Data.{fileName}")!;

        var contentLength = stream.Length;

        var createdSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest(fileName, contentLength),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var buffer = new byte[chunkSize];
        var chunkNumber = 1;
        int bytesRead;

        // Act & Assert: upload must fail during chunk upload
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            while ((bytesRead = await stream.ReadAsync(
                       buffer.AsMemory(0, chunkSize),
                       TestContext.Current.CancellationToken)) > 0)
            {
                await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
                var fileParameter = new FileParameter(chunkStream);

                await _filesOperationsApi.UploadAsyncSessionAsync(
                    room.Id,
                    createdSession.Id,
                    chunkNumber,
                    fileParameter,
                    TestContext.Current.CancellationToken);

                chunkNumber++;
            }
        });

        exception.ErrorCode.Should().Be(402);
        exception.Message.Should().Contain("quota");
    }
}
