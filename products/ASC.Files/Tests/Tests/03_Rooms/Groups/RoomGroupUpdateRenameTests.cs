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

/// <summary>PUT /files/group/{id} — renaming.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupUpdateRenameTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Rename_LeavesIconAndRoomsUntouched()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Rename Room");
        var created = await CreateRoomGroup("Old Name", [roomId], "heart");

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(groupName: "New Name"), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Name.Should().Be("New Name");
        updated.Icon.Id.Should().Be("heart");
        updated.TotalRooms.Should().Be(1);
        updated.Rooms.Select(r => r.Title).Should().Contain("Rename Room");
    }

    [Fact]
    public async Task Rename_ToUnicodeName_Succeeds()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Uni Rename");
        var created = await CreateRoomGroup("Plain", [roomId]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(groupName: "Переименовано 名字 🎯"), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Name.Should().Be("Переименовано 名字 🎯");
    }

    [Fact]
    public async Task Rename_WithInternalSpaces_IsPreserved()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Space Rename");
        var created = await CreateRoomGroup("Plain", [roomId]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(groupName: "My favorite rooms"), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Name.Should().Be("My favorite rooms");
    }

    [Fact]
    public async Task Rename_ToAnExistingGroupsName_IsAllowed()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "DupRename");
        await CreateRoomGroup("Taken", [ids[0]]);
        var created = await CreateRoomGroup("Original", [ids[1]]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(groupName: "Taken"), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Name.Should().Be("Taken");
    }

    /// <summary>
    /// <c>groupName</c> is optional but not typed as nullable. Passing null is currently a silent
    /// 200 no-op, yet create rejects <c>name: null</c> with 400.
    /// </summary>
    [Fact]
    [Trait("Bug", "82590")]
    public async Task Rename_NullGroupName_ShouldBe400LikeCreate()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("NullName Room");
        var created = await CreateRoomGroup("Keep Me", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = (string?)null }, path: $"/{created.Id}");

        // Assert — no data corruption either way (null is a no-op), so the name stays "Keep Me";
        // the bug is purely the accepted-instead-of-rejected status.
        var after = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Name.Should().Be("Keep Me");
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Rename_TooLongName_Returns400WithoutChangingState()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("LongName Room");
        var created = await CreateRoomGroup("Short", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = new string('n', 300) }, path: $"/{created.Id}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Name.Should().Be("Short");
    }

    [Fact]
    [Trait("Bug", "82590")]
    public async Task Rename_EmptyName_ShouldBeRejected()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("EmptyName Room");
        var created = await CreateRoomGroup("Named", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = "" }, path: $"/{created.Id}");

        // Assert — data-corruption half of the bug first: an empty name must not overwrite the
        // stored name.
        var after = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Name.Should().Be("Named");
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82590")]
    public async Task Rename_WhitespaceOnlyName_ShouldBeRejected()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("SpaceName Room");
        var created = await CreateRoomGroup("Named", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = "   " }, path: $"/{created.Id}");

        // Assert
        var after = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Name.Should().Be("Named");
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }
}
