﻿// (c) Copyright Ascensio System SIA 2009-2025
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

using QuotaSettingsRequestsDto = Docspace.Model.QuotaSettingsRequestsDto;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class FilesQuotaTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task ResetRoomQuota_ValidRoomIds_ResetsQuotaSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
       
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);
        
        // Create a test room
        var roomTitle = "Room for Quota Reset Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle, Initializer.Owner);
        
        // Set up request to reset quota for the room
        var resetRequest = new UpdateRoomsRoomIdsRequestDtoInteger
        {
            RoomIds = [new(room.Id)]
        };
        
        // Act
        var result = (await _filesQuotaApi.ResetRoomQuotaAsync(resetRequest, TestContext.Current.CancellationToken)).Response;
        
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
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create test rooms
        var roomTitle1 = "Room 1 for Multi Quota Reset " + Guid.NewGuid().ToString()[..8];
        var roomTitle2 = "Room 2 for Multi Quota Reset " + Guid.NewGuid().ToString()[..8];
        
        var room1 = await CreateVirtualRoom(roomTitle1, Initializer.Owner);
        var room2 = await CreateVirtualRoom(roomTitle2, Initializer.Owner);
        
        // Set up request to reset quota for multiple rooms
        var resetRequest = new UpdateRoomsRoomIdsRequestDtoInteger
        {
            RoomIds = [new(room1.Id), new(room2.Id)]
        };
        // Act
        var result = (await _filesQuotaApi.ResetRoomQuotaAsync(resetRequest, TestContext.Current.CancellationToken)).Response;
        
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
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a test room
        var roomTitle = "Room for Quota Update Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle, Initializer.Owner);
        
        // Define a quota limit (in bytes)
        var quotaLimit = 2147483648; // 2 GB
        
        // Set up request to update quota for the room
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {           
            RoomIds = [new(room.Id)],
            Quota = quotaLimit
        };
        
        // Act
        var result = (await _filesQuotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        
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
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create test rooms
        var roomTitle1 = "Room 1 for Multi Quota Update " + Guid.NewGuid().ToString()[..8];
        var roomTitle2 = "Room 2 for Multi Quota Update " + Guid.NewGuid().ToString()[..8];
        
        var room1 = await CreateVirtualRoom(roomTitle1, Initializer.Owner);
        var room2 = await CreateVirtualRoom(roomTitle2, Initializer.Owner);
        
        // Define a quota limit (in bytes)
        var quotaLimit = 2147483648; // 2 GB
        
        // Set up request to update quota for multiple rooms
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {           
            RoomIds = [new(room1.Id), new(room2.Id)],
            Quota = quotaLimit
        };
        
        // Act
        var result = (await _filesQuotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        
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
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a test room
        var roomTitle = "Room for Zero Quota Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle, Initializer.Owner);
        
        // Set up a request with zero quotas (should be the same as reset)
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {
            RoomIds = [new(room.Id)],
            Quota = 0
        };
        
        // Act
        var result = (await _filesQuotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        
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
        
        var defaultQuotaLimit = 1073741824; // 1 GB
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(defaultQuotaLimit)), TestContext.Current.CancellationToken);

        // Create a test room
        var roomTitle = "Room for Quota Lifecycle Test " + Guid.NewGuid().ToString()[..8];
        var room = await CreateVirtualRoom(roomTitle, Initializer.Owner);
        
        // Step 1: Set a quota
        var quotaLimit = 5368709120; // 5 GB
        var updateRequest = new UpdateRoomsQuotaRequestDtoInteger
        {
            RoomIds = [new(room.Id)],
            Quota = quotaLimit
        };
        
        var updateResult = (await _filesQuotaApi.UpdateRoomsQuotaAsync(updateRequest, TestContext.Current.CancellationToken)).Response;
        updateResult[0].Id.Should().Be(room.Id);
        updateResult[0].QuotaLimit.Should().Be(quotaLimit);
        
        // Step 2: Reset the quota
        var resetRequest = new UpdateRoomsRoomIdsRequestDtoInteger
        {
            RoomIds = [new(room.Id)]
        };
        
        var resetResult = (await _filesQuotaApi.ResetRoomQuotaAsync(resetRequest, TestContext.Current.CancellationToken)).Response;
        resetResult[0].Id.Should().Be(room.Id);
        resetResult[0].QuotaLimit.Should().Be(defaultQuotaLimit);
    }
}
