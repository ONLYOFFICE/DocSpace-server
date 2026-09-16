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
/// (<c>searchArea</c> - <see cref="SearchArea.Active"/> for Rooms, <see cref="SearchArea.Forms"/> for
/// Forms), only rooms of that section may be linked to it, and a listing of one section never shows
/// the other section's groups. The section is not stored: it is derived from the rooms the group
/// references, exactly as the room listings themselves are split.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomGroupSearchAreaTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Create_WithoutSearchArea_DefaultsToActive()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Default Area Room");

        // Act
        var group = await CreateRoomGroup("Default Area Group", [roomId]);

        // Assert - an old client that knows nothing about the split keeps landing in Rooms
        group.SearchArea.Should().Be(SearchArea.Active);
    }

    [Fact]
    public async Task Create_Forms_WithFormsRoom_Succeeds()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Forms Area Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        var group = await CreateRoomGroup("Forms Area Group", [room.Id], searchArea: SearchArea.Forms);

        // Assert
        group.SearchArea.Should().Be(SearchArea.Forms);
        group.TotalRooms.Should().Be(1);
    }

    // The dictionary is serialized by name now, so the typed DTO can no longer put the ordinal on the
    // wire - and an old client that still sends one has to keep working. Raw is the only way to send it.
    [Fact]
    public async Task Create_NumericSearchArea_IsStillAccepted()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Numeric Area Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: new { name = "Numeric Area Group", icon = "star", rooms = new[] { room.Id }, searchArea = (int)SearchArea.Forms });

        // Assert - and the answer always comes back as the name
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);

        json.RootElement.GetProperty("response").GetProperty("searchArea").GetString()
            .Should().Be(nameof(SearchArea.Forms));
    }

    [Fact]
    public async Task Create_Forms_WithNonFormsRoom_Returns400()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Not A Forms Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await CreateRoomGroup("Forms Group Of Rooms", [roomId], searchArea: SearchArea.Forms));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_Active_WithFormsRoom_Returns400()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Forms Room For Active Group " + Guid.NewGuid().ToString()[..8]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await CreateRoomGroup("Active Group Of Forms", [room.Id], searchArea: SearchArea.Active));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_MixedRooms_KeepsOwnAreaAndReportsRejection()
    {
        // Arrange
        var formsRoom = await CreateFillingFormsRoom("Mixed Forms Room " + Guid.NewGuid().ToString()[..8]);
        var customRoomId = await CreateGroupRoomId("Mixed Custom Room");

        // Act - the room of the other section is dropped, and dropping anything is reported
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await CreateRoomGroup("Mixed Group", [formsRoom.Id, customRoomId], searchArea: SearchArea.Forms));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var forms = await ListGroups(SearchArea.Forms);
        forms.Where(g => g.Name == "Mixed Group")
            .Should().ContainSingle()
            .Which.TotalRooms.Should().Be(1);
    }

    public static TheoryData<SearchArea> AreasThatOwnNoGroups =>
        [SearchArea.Archive, SearchArea.Any, SearchArea.Templates, SearchArea.FormTemplates];

    [Theory]
    [MemberData(nameof(AreasThatOwnNoGroups))]
    public async Task Create_AreaOutsideTheSplit_Returns400(SearchArea searchArea)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"Area {searchArea} Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await CreateRoomGroup($"Area {searchArea} Group", [roomId], searchArea: searchArea));

        // Assert - only the two sections that were split own groups
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheRequestedArea()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Listed Custom Room");
        var formsRoom = await CreateFillingFormsRoom("Listed Forms Room " + Guid.NewGuid().ToString()[..8]);

        var roomsGroup = await CreateRoomGroup("Listed Rooms Group", [customRoomId]);
        var formsGroup = await CreateRoomGroup("Listed Forms Group", [formsRoom.Id], searchArea: SearchArea.Forms);

        // Act
        var active = await ListGroups(SearchArea.Active);
        var forms = await ListGroups(SearchArea.Forms);
        var defaulted = await ListGroups();

        // Assert
        active.Select(g => g.Id).Should().Contain(roomsGroup.Id).And.NotContain(formsGroup.Id);
        forms.Select(g => g.Id).Should().Contain(formsGroup.Id).And.NotContain(roomsGroup.Id);
        // compatibility: no searchArea behaves exactly like searchArea=Active
        defaulted.Select(g => g.Id).Should().BeEquivalentTo(active.Select(g => g.Id));
    }

    [Fact]
    public async Task GetInfo_ReturnsTheGroupArea()
    {
        // Arrange
        var room = await CreateFillingFormsRoom("Info Forms Room " + Guid.NewGuid().ToString()[..8]);
        var created = await CreateRoomGroup("Info Forms Group", [room.Id], searchArea: SearchArea.Forms);

        // Act
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.SearchArea.Should().Be(SearchArea.Forms);
        info.TotalRooms.Should().Be(1);
        info.Rooms.Count.Should().Be(1);
    }

    [Fact]
    public async Task Update_AddRoomOfTheOtherArea_IsRefused()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Update Custom Room");
        var formsRoom = await CreateFillingFormsRoom("Update Forms Room " + Guid.NewGuid().ToString()[..8]);
        var group = await CreateRoomGroup("Update Rooms Group", [customRoomId]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            group.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(formsRoom.Id)]),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(group.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.TotalRooms.Should().Be(1);
    }

    // `searchArea` is deliberately absent from UpdateRoomGroupRequest - a group never moves between
    // sections - so only a raw body can prove that sending it anyway changes nothing.
    [Fact]
    public async Task Update_SearchAreaInBody_IsIgnored()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Immutable Area Room");
        var group = await CreateRoomGroup("Immutable Area Group", [customRoomId]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Put,
            body: new { groupName = "Immutable Area Group Renamed", searchArea = nameof(SearchArea.Forms) },
            path: $"/{group.Id}");

        // Assert - the rename goes through, the section does not move
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(group.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.SearchArea.Should().Be(SearchArea.Active);
        info.Name.Should().Be("Immutable Area Group Renamed");

        (await ListGroups(SearchArea.Forms)).Select(g => g.Id).Should().NotContain(group.Id);
    }

    [Fact]
    public async Task Rooms_FilteredByAGroupOfTheOtherArea_ReturnsNothing()
    {
        // Arrange
        var customRoomId = await CreateGroupRoomId("Cross Area Room");
        var group = await CreateRoomGroup("Cross Area Group", [customRoomId]);

        // Act
        var ownSection = await ListRoomTitles(SearchArea.Active, group.Id);
        var otherSection = await ListRoomTitles(SearchArea.Forms, group.Id);

        // Assert - the group filter must not be silently dropped in the other section
        ownSection.Should().Contain("Cross Area Room");
        otherSection.Should().BeEmpty();
    }

    #region helpers

    private async Task<List<RoomGroupDto>> ListGroups(SearchArea? searchArea = null)
    {
        var list = await _roomGroupsApi.GetRoomGroupsAsync(
            searchArea: searchArea,
            cancellationToken: TestContext.Current.CancellationToken);

        return list.Response;
    }

    /// <summary>
    /// The rooms a section lists when filtered by one group. Matched by title: the folder listing model
    /// types its entries as <c>FileEntryBaseDto</c>, which carries no id.
    /// </summary>
    private async Task<List<string>> ListRoomTitles(SearchArea searchArea, int groupId)
    {
        var content = await _roomsApi.GetRoomsFolderAsync(
            searchArea: searchArea,
            groupId: groupId,
            cancellationToken: TestContext.Current.CancellationToken);

        return [.. content.Response.Folders.Select(f => f.Title)];
    }

    #endregion
}
