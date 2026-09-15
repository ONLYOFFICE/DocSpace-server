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

namespace ASC.Files.Tests.Tests._03_Rooms.Groups;

/// <summary>
/// The Rooms / Forms split as the group endpoints see it: a group belongs to exactly one section
/// (<c>searchArea</c> - <c>Active</c> for Rooms, <c>Forms</c> for Forms), only rooms of that section
/// may be linked to it, and a listing of one section never shows the other section's groups.
/// Everything that carries the new field goes through raw HTTP: the typed SDK predates it.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomGroupSearchAreaTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    private const string Active = nameof(SearchArea.Active);
    private const string Forms = nameof(SearchArea.Forms);

    [Fact]
    public async Task Create_WithoutSearchArea_DefaultsToActive()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Default Area Room");

        // Act
        var group = await CreateRawGroup("Default Area Group", [roomId]);

        // Assert - an old client that knows nothing about the split keeps landing in Rooms
        SearchAreaOf(group).Should().Be(Active);
    }

    [Fact]
    public async Task Create_Forms_WithFormsRoom_Succeeds()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Forms Area Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        var group = await CreateRawGroup("Forms Area Group", [room.Id], searchArea: Forms);

        // Assert
        SearchAreaOf(group).Should().Be(Forms);
        group.GetProperty("totalRooms").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Create_NumericSearchArea_IsStillAccepted()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Numeric Area Room " + Guid.NewGuid().ToString()[..8]);

        // Act - the dictionary reads both ways, so a client that sends the ordinal keeps working
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: new { name = "Numeric Area Group", icon = "star", rooms = new[] { room.Id }, searchArea = (int)SearchArea.Forms });

        // Assert - and the answer always comes back as the name
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var group = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("response");

        SearchAreaOf(group).Should().Be(Forms);
    }

    [Fact]
    public async Task Create_Forms_WithNonFormsRoom_Returns400()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Not A Forms Room");

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: new { name = "Forms Group Of Rooms", icon = "star", rooms = new[] { roomId }, searchArea = Forms });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Create_Active_WithFormsRoom_Returns400()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Forms Room For Active Group " + Guid.NewGuid().ToString()[..8]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: new { name = "Active Group Of Forms", icon = "star", rooms = new[] { room.Id }, searchArea = Active });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Create_MixedRooms_KeepsOwnAreaAndReportsRejection()
    {
        // Arrange
        var formsRoom = await CreateFillingFormsRoom("Mixed Forms Room " + Guid.NewGuid().ToString()[..8]);
        var customRoomId = await CreateGroupRoomId("Mixed Custom Room");

        // Act - the room of the other section is dropped, and dropping anything is reported
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: new { name = "Mixed Group", icon = "star", rooms = new[] { formsRoom.Id, customRoomId }, searchArea = Forms });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)403);

        var forms = await ListRawGroups(Forms);
        forms.Where(g => NameOf(g) == "Mixed Group")
            .Should().ContainSingle()
            .Which.GetProperty("totalRooms").GetInt32().Should().Be(1);
    }

    public static TheoryData<string> AreasThatOwnNoGroups =>
        [nameof(SearchArea.Archive), nameof(SearchArea.Any), nameof(SearchArea.Templates), nameof(SearchArea.FormTemplates)];

    [Theory]
    [MemberData(nameof(AreasThatOwnNoGroups))]
    public async Task Create_AreaOutsideTheSplit_Returns400(string searchArea)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"Area {searchArea} Room");

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: new { name = $"Area {searchArea} Group", icon = "star", rooms = new[] { roomId }, searchArea });

        // Assert - only the two sections that were split own groups
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheRequestedArea()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Listed Custom Room");
        var formsRoom = await CreateFillingFormsRoom("Listed Forms Room " + Guid.NewGuid().ToString()[..8]);

        var roomsGroup = await CreateRawGroup("Listed Rooms Group", [customRoomId]);
        var formsGroup = await CreateRawGroup("Listed Forms Group", [formsRoom.Id], searchArea: Forms);

        // Act
        var active = await ListRawGroups(Active);
        var forms = await ListRawGroups(Forms);
        var defaulted = await ListRawGroups();

        // Assert
        active.Select(IdOf).Should().Contain(IdOf(roomsGroup)).And.NotContain(IdOf(formsGroup));
        forms.Select(IdOf).Should().Contain(IdOf(formsGroup)).And.NotContain(IdOf(roomsGroup));
        // compatibility: no searchArea behaves exactly like searchArea=Active
        defaulted.Select(IdOf).Should().BeEquivalentTo(active.Select(IdOf));
    }

    [Fact]
    public async Task GetInfo_ReturnsTheGroupArea()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Info Forms Room " + Guid.NewGuid().ToString()[..8]);
        var created = await CreateRawGroup("Info Forms Group", [room.Id], searchArea: Forms);

        // Act
        var info = await GetRawGroup(IdOf(created));

        // Assert
        SearchAreaOf(info).Should().Be(Forms);
        info.GetProperty("totalRooms").GetInt32().Should().Be(1);
        info.GetProperty("rooms").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Update_AddRoomOfTheOtherArea_IsRefused()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Update Custom Room");
        var formsRoom = await CreateFillingFormsRoom("Update Forms Room " + Guid.NewGuid().ToString()[..8]);
        var group = await CreateRawGroup("Update Rooms Group", [customRoomId]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Put,
            body: new { roomsToAdd = new[] { formsRoom.Id } },
            path: $"/{IdOf(group)}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);

        var info = await GetRawGroup(IdOf(group));
        info.GetProperty("totalRooms").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Update_SearchAreaInBody_IsIgnored()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Immutable Area Room");
        var group = await CreateRawGroup("Immutable Area Group", [customRoomId]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Put,
            body: new { groupName = "Immutable Area Group Renamed", searchArea = Forms },
            path: $"/{IdOf(group)}");

        // Assert - the rename goes through, the section does not move
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var info = await GetRawGroup(IdOf(group));
        SearchAreaOf(info).Should().Be(Active);
        info.GetProperty("name").GetString().Should().Be("Immutable Area Group Renamed");

        (await ListRawGroups(Forms)).Select(IdOf).Should().NotContain(IdOf(group));
    }

    [Fact]
    public async Task Rooms_FilteredByAGroupOfTheOtherArea_ReturnsNothing()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Cross Area Room");
        var group = await CreateRawGroup("Cross Area Group", [customRoomId]);

        // Act
        var ownSection = await ListRoomIds(Active, IdOf(group));
        var otherSection = await ListRoomIds(Forms, IdOf(group));

        // Assert - the group filter must not be silently dropped in the other section
        ownSection.Should().Contain(customRoomId);
        otherSection.Should().BeEmpty();
    }

    #region helpers

    private static int IdOf(JsonElement group) => group.GetProperty("id").GetInt32();

    private static string? NameOf(JsonElement group) => group.GetProperty("name").GetString();

    private static string? SearchAreaOf(JsonElement group) => group.GetProperty("searchArea").GetString();

    /// <summary>Creates a group through raw HTTP so that <c>searchArea</c> can be sent at all.</summary>
    private async Task<JsonElement> CreateRawGroup(string name, IEnumerable<int> rooms, string? searchArea = null, string icon = "star")
    {
        object body = searchArea != null
            ? new { name, icon, rooms = rooms.ToArray(), searchArea }
            : new { name, icon, rooms = rooms.ToArray() };

        using var response = await RoomGroupRaw(HttpMethod.Post, body: body);

        return await ReadResponseAsync(response);
    }

    private async Task<JsonElement> GetRawGroup(int id)
    {
        using var response = await RoomGroupRaw(HttpMethod.Get, path: $"/{id}");

        return await ReadResponseAsync(response);
    }

    private async Task<List<JsonElement>> ListRawGroups(string? searchArea = null)
    {
        using var response = await RoomGroupRaw(
            HttpMethod.Get,
            query: searchArea != null ? $"searchArea={searchArea}" : null);

        var body = await ReadResponseAsync(response);

        return [.. body.EnumerateArray()];
    }

    private async Task<List<int>> ListRoomIds(string searchArea, int groupId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/2.0/files/rooms?searchArea={searchArea}&groupId={groupId}");
        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);

        var body = await ReadResponseAsync(response);

        return [.. body.GetProperty("folders").EnumerateArray().Select(f => f.GetProperty("id").GetInt32())];
    }

    /// <summary>Unwraps the api envelope, failing loudly with the body when the call did not succeed.</summary>
    private static async Task<JsonElement> ReadResponseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unexpected {(int)response.StatusCode}: {body}");
        }

        return JsonDocument.Parse(body).RootElement.GetProperty("response").Clone();
    }

    #endregion
}
