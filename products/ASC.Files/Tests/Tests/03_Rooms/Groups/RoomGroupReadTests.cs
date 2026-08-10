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

/// <summary>GET /files/group/{id} and GET /files/group.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupReadTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    #region GET /files/group/{id}

    [Fact]
    public async Task GetInfo_ReturnsFullDto()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Info Room");
        var created = await CreateRoomGroup("Info Group", ids, "heart");

        // Act
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertRoomGroupShape(info);
        info.Id.Should().Be(created.Id);
        info.Name.Should().Be("Info Group");
        info.Icon.Id.Should().Be("heart");
        info.UserId.Should().Be(created.UserId);
        info.TotalRooms.Should().Be(2);
        info.TotalRooms.Should().Be(info.Rooms.Count);
    }

    [Fact]
    public async Task GetInfo_SingleRoomGroup()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("One");
        var created = await CreateRoomGroup("Single", [roomId]);

        // Act
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.TotalRooms.Should().Be(1);
        info.Rooms.Select(r => r.Title).Should().Contain("One");
    }

    [Fact]
    public async Task GetInfo_IncludeMembersTrueAndFalse_ReturnConsistentCoreFields()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Members Room");
        var created = await CreateRoomGroup("Members Group", [roomId]);

        // Act
        var withMembers = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var without = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, includeMembers: false, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        withMembers.Id.Should().Be(without.Id);
        withMembers.Name.Should().Be(without.Name);
        withMembers.TotalRooms.Should().Be(without.TotalRooms);
    }

    public static TheoryData<string> NotFoundIds =>
        ["0", "-1", "999999", "1.5", "not-a-number", "99999999999999999999"];

    [Theory]
    [MemberData(nameof(NotFoundIds))]
    public async Task GetInfo_BadId_Returns404(string id)
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get, path: $"/{id}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)404);
    }

    [Fact]
    public async Task GetInfo_DeletedGroup_Returns404()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Del Room");
        var created = await CreateRoomGroup("To Delete", [roomId]);
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetInfo_InvalidIncludeMembers_Returns400()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Bad Param");
        var created = await CreateRoomGroup("Bad Param Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get, path: $"/{created.Id}", query: "includeMembers=abc");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    #endregion

    #region GET /files/group

    [Fact]
    public async Task GetList_NoGroups_ReturnsEmptyList()
    {
        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetList_SingleGroup_FullStructure()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("List One Room");
        await CreateRoomGroup("Only Group", [roomId]);

        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        list.Should().HaveCount(1);
        AssertRoomGroupShape(list[0]);
        list[0].Name.Should().Be("Only Group");
    }

    [Fact]
    public async Task GetList_AllCreatedGroups_HaveCorrectTotalRooms()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(3, "List Room");
        await CreateRoomGroup("LG1", [ids[0]]);
        await CreateRoomGroup("LG2", [ids[1], ids[2]]);

        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        list.Single(g => g.Name == "LG1").TotalRooms.Should().Be(1);
        list.Single(g => g.Name == "LG2").TotalRooms.Should().Be(2);
    }

    [Fact]
    public async Task GetList_DeletedGroup_DisappearsFromList()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Vanish Room");
        var created = await CreateRoomGroup("Vanishing", [roomId]);
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        list.Select(g => g.Id).Should().NotContain(created.Id);
    }

    [Fact]
    public async Task GetList_UpdatedNameAndIcon_AreReflected()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Reflect Room");
        var created = await CreateRoomGroup("Before Update", [roomId], "star");

        await _roomGroupsApi.UpdateRoomGroupAsync(created.Id, new UpdateRoomGroupRequest(groupName: "After Update"), TestContext.Current.CancellationToken);
        await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest("heart"), TestContext.Current.CancellationToken);

        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var g = list.Single(x => x.Id == created.Id);
        g.Name.Should().Be("After Update");
        g.Icon.Id.Should().Be("heart");
    }

    [Fact]
    public async Task GetList_UpdatedRoomSet_IsReflected()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Set Room");
        var created = await CreateRoomGroup("Set Group", [ids[0]]);

        await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken);

        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        list.Single(x => x.Id == created.Id).TotalRooms.Should().Be(2);
    }

    [Fact]
    public async Task GetList_IdParameterIsRudimentary_123BehavesLike0()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Rudiment Room");
        await CreateRoomGroup("Rudiment", [roomId]);

        // Act
        var zero = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var oneTwoThree = (await _roomGroupsApi.GetRoomGroupsAsync(123, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        oneTwoThree.Select(g => g.Name).Order().Should().Equal(zero.Select(g => g.Name).Order());
    }

    [Fact]
    public async Task GetList_RawRequestWithoutIdParameter_StillReturns200()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)200);
    }

    #endregion
}
