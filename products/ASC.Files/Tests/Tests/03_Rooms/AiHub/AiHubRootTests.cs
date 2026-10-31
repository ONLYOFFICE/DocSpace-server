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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// Instructions and Knowledge rooms live under their own root, the AI rooms section (<c>FolderType.AiHub</c>),
/// not under Rooms: they are created there only, are never archived, have no templates and are deleted outright.
/// A Knowledge room needs vectorization, which the test environment does not have, so the suite works with
/// Instructions rooms. The pinned SDK knows neither the Instructions room type nor the new section, so the room is
/// created and the section is listed over raw HTTP; everything else goes through the SDK.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubRootTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int InstructionsRoomType = 10;
    private const int AiHubFolderType = 42;

    [Fact]
    public async Task CreateRoom_Instructions_LandsUnderAiHubRoot()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var room = await CreateInstructionsRoomAsync("Autotest Instructions Root");
        var section = await GetAiHubSectionAsync();

        // Assert
        room.GetProperty("rootFolderType").GetInt32().Should().Be(AiHubFolderType);
        room.GetProperty("parentId").GetInt32().Should().Be(section.GetProperty("current").GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task InstructionsRoom_IsListedInAiHubSection_NotInRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomIdAsync("Autotest Instructions Listing");

        // Act
        var aiHub = IdsOf(await GetAiHubSectionAsync());
        var rooms = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        aiHub.Should().Contain(roomId);
        rooms.Folders.Should().NotContain(f => f.Title == "Autotest Instructions Listing");
    }

    [Fact]
    public async Task ArchiveRoom_Instructions_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomIdAsync("Autotest Instructions Archive");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateRoomTemplate_FromInstructionsRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomIdAsync("Autotest Instructions Template");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(roomId, "Autotest Instructions Template"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateFolder_InAiHubRoot_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var rootId = (await GetAiHubSectionAsync()).GetProperty("current").GetProperty("id").GetInt32();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateFolder("Autotest Folder In AI Rooms Root", rootId));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteRoom_Instructions_RemovesTheRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomIdAsync("Autotest Instructions Delete");

        // Act
        await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        IdsOf(await GetAiHubSectionAsync()).Should().NotContain(roomId);
    }

    private async Task<int> CreateInstructionsRoomIdAsync(string title)
    {
        return (await CreateInstructionsRoomAsync(title)).GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> CreateInstructionsRoomAsync(string title)
    {
        var files = new RawApiClient(_filesClient);
        using var response = await files.PostAsync("api/2.0/files/rooms", new { title, roomType = InstructionsRoomType }, TestContext.Current.CancellationToken);

        return await files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    private async Task<JsonElement> GetAiHubSectionAsync()
    {
        var files = new RawApiClient(_filesClient);
        using var response = await files.GetAsync("api/2.0/files/rooms?searchArea=AiHub", TestContext.Current.CancellationToken);

        return await files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    private static List<int> IdsOf(JsonElement section)
    {
        return section.GetProperty("folders").EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).ToList();
    }
}
