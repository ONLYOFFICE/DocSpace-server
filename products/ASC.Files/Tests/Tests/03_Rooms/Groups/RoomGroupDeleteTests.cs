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

/// <summary>DELETE /files/group/{id}.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupDeleteTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Delete_Group_DisappearsFromGetRoomGroups()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("DelList Room");
        var created = await CreateRoomGroup("DelList Group", [roomId]);

        // Act
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Select(g => g.Id).Should().NotContain(created.Id);
    }

    [Fact]
    public async Task Delete_OneGroup_DoesNotAffectOthers()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Keep");
        var toDelete = await CreateRoomGroup("To Delete", [ids[0]]);
        var toKeep = await CreateRoomGroup("To Keep", [ids[1]]);

        // Act
        await _roomGroupsApi.DeleteRoomGroupAsync(toDelete.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(toKeep.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_Group_DoesNotDeleteItsRooms()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Surviving Room");
        var created = await CreateRoomGroup("Room Survives Group", [roomId]);

        // Act
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var room = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        room.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_GroupWithSeveralRooms_IsDeleted()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(3, "MultiDel");
        var created = await CreateRoomGroup("MultiDel Group", ids);

        // Act
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_IncludeMembersTrueAndFalse_BothSucceed()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "IncMembers");
        var g1 = await CreateRoomGroup("IncMembers True", [ids[0]]);
        var g2 = await CreateRoomGroup("IncMembers False", [ids[1]]);

        // Act & Assert — neither call fails.
        await _roomGroupsApi.DeleteRoomGroupAsync(g1.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        await _roomGroupsApi.DeleteRoomGroupAsync(g2.Id, includeMembers: false, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ThenCreateWithSameName_Succeeds()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Recreate");
        var created = await CreateRoomGroup("Recreatable", [ids[0]]);
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var recreated = await CreateRoomGroup("Recreatable", [ids[1]]);

        // Assert
        recreated.Name.Should().Be("Recreatable");
    }

    [Fact]
    [Trait("Bug", "82596")]
    public async Task Delete_RepeatingTheDelete_ShouldBe404OnAlreadyDeletedGroup()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Repeat Del");
        var created = await CreateRoomGroup("Repeat Del Group", [roomId]);
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Delete, path: $"/{created.Id}");

        // Assert — the group no longer exists, so a second delete must be 404 (idempotent does
        // not imply 200 for a missing resource).
        response.StatusCode.Should().Be((HttpStatusCode)404);
    }

    // A missing group must be 404 (as GET/PUT already are). The endpoint currently returns 200
    // for any addressable integer id.
    public static TheoryData<string> MissingIds => ["0", "-1", "999999"];

    [Theory]
    [MemberData(nameof(MissingIds))]
    [Trait("Bug", "82596")]
    public async Task Delete_NonExistentId_ShouldBe404(string id)
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Delete, path: $"/{id}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)404);
    }

    public static TheoryData<string> RoutingFailIds => ["1.5", "not-a-number"];

    [Theory]
    [MemberData(nameof(RoutingFailIds))]
    public async Task Delete_NonIntegerId_Returns404(string id)
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Delete, path: $"/{id}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)404);
    }

    [Fact]
    public async Task Delete_InvalidIncludeMembers_Returns400()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("DelBadParam");
        var created = await CreateRoomGroup("DelBadParam Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Delete, path: $"/{created.Id}", query: "includeMembers=abc");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }
}
