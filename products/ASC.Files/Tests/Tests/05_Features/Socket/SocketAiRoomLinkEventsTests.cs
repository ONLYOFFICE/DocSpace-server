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

namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// What a change of the links between AI projects and rooms pushes to clients: the update of each room whose number
/// of projects changed, to whoever watches the AI rooms section and may read the room. The pinned SDK knows neither
/// the room types of the section nor the sources routes, so the rooms are created and linked over raw HTTP. A
/// Knowledge room needs vectorization, which the test environment does not have, so the rooms here are Instructions
/// rooms.
/// </summary>
[Trait("Category", "Socket")]
[Trait("Feature", "AiHub")]
public class SocketAiRoomLinkEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    private const int InstructionsRoomType = 10;
    private const int AiProjectRoomType = 12;

    private RawApiClient Files => new(_filesClient);

    [Fact]
    public async Task LinkSources_ByAProjectManager_ReachesTheOwnerWatchingTheSection()
    {
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateHubRoomAsync(AiProjectRoomType);
        var roomId = await CreateHubRoomAsync(InstructionsRoomType);
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(projectId, manager, FileShare.RoomManager);
        await InviteToRoom(roomId, manager, FileShare.RoomManager);
        await using var socket = await ConnectAndWatch(Owner, await GetAiHubIdAsync());

        await _filesClient.Authenticate(manager);
        await LinkAsync(projectId, roomId);

        await WaitForModified(socket, "update", "folder", roomId);
    }

    [Fact]
    public async Task UnlinkSources_ByAProjectManager_ReachesTheOwnerWatchingTheSection()
    {
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateHubRoomAsync(AiProjectRoomType);
        var roomId = await CreateHubRoomAsync(InstructionsRoomType);
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(projectId, manager, FileShare.RoomManager);
        await using var socket = await ConnectAndWatch(Owner, await GetAiHubIdAsync());
        await LinkAsync(projectId, roomId);
        await WaitForModified(socket, "update", "folder", roomId);

        await _filesClient.Authenticate(manager);
        using var unlinked = await Files.DeleteAsync($"api/2.0/files/rooms/{projectId}/sources", new { roomIds = new[] { roomId } }, TestContext.Current.CancellationToken);

        unlinked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await WaitForUpdates(socket, roomId, 2);
    }

    [Fact]
    public async Task DeleteProject_WithTwoRooms_ReachesTheOwnerForEachRoom()
    {
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateHubRoomAsync(AiProjectRoomType);
        var firstRoomId = await CreateHubRoomAsync(InstructionsRoomType);
        var secondRoomId = await CreateHubRoomAsync(InstructionsRoomType);
        await using var socket = await ConnectAndWatch(Owner, await GetAiHubIdAsync());
        await LinkAsync(projectId, firstRoomId, secondRoomId);
        await WaitForModified(socket, "update", "folder", firstRoomId);
        await WaitForModified(socket, "update", "folder", secondRoomId);

        await _roomsApi.DeleteRoomAsync(projectId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        await WaitForUpdates(socket, firstRoomId, 2);
        await WaitForUpdates(socket, secondRoomId, 2);
    }

    [Fact]
    public async Task LinkSources_RoomAlreadyLinked_SendsNoSecondEvent()
    {
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateHubRoomAsync(AiProjectRoomType);
        var roomId = await CreateHubRoomAsync(InstructionsRoomType);
        var laterRoomId = await CreateHubRoomAsync(InstructionsRoomType);
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(projectId, manager, FileShare.RoomManager);
        await InviteToRoom(roomId, manager, FileShare.RoomManager);
        await InviteToRoom(laterRoomId, manager, FileShare.RoomManager);
        await using var socket = await ConnectAndWatch(Owner, await GetAiHubIdAsync());
        await _filesClient.Authenticate(manager);
        await LinkAsync(projectId, roomId);
        await WaitForModified(socket, "update", "folder", roomId);

        await LinkAsync(projectId, roomId);
        await LinkAsync(projectId, laterRoomId);
        await WaitForModified(socket, "update", "folder", laterRoomId);

        var second = () => socket.WaitForCountAsync(ModifyFolder, Modified("update", "folder", roomId), 2, _noDeliveryWindow, TestContext.Current.CancellationToken);
        await second.Should().ThrowAsync<TimeoutException>("the repeated link changed the number of projects of no room");
    }

    private static Task<SocketEvent> WaitForUpdates(PortalSocket socket, int roomId, int count)
    {
        return socket.WaitForCountAsync(ModifyFolder, Modified("update", "folder", roomId), count, cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<int> CreateHubRoomAsync(int roomType)
    {
        using var response = await Files.PostAsync("api/2.0/files/rooms", new { title = $"Autotest Socket {Guid.NewGuid():N}", roomType }, TestContext.Current.CancellationToken);

        return (await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();
    }

    private async Task<int> GetAiHubIdAsync()
    {
        using var response = await Files.GetAsync("api/2.0/files/rooms?searchArea=AiHub", TestContext.Current.CancellationToken);

        return (await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).GetProperty("current").GetProperty("id").GetInt32();
    }

    private async Task LinkAsync(int projectId, params int[] roomIds)
    {
        using var response = await Files.PostAsync($"api/2.0/files/rooms/{projectId}/sources", new { roomIds }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
