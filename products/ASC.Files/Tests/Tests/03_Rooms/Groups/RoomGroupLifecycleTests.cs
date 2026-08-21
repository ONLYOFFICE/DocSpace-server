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

/// <summary>End-to-end room-group lifecycle scenarios spanning several endpoints.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupLifecycleTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task FullLifecycle_CreateGetListRenameAddRemoveIconDelete()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Life");

        // Act — create
        var created = await CreateRoomGroup("Lifecycle", [ids[0]], "star");
        var groupId = created.Id;

        // Assert — get by id
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(groupId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Name.Should().Be("Lifecycle");

        // Assert — appears in list
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Select(g => g.Id).Should().Contain(groupId);

        // Act & Assert — rename
        var renamed = (await _roomGroupsApi.UpdateRoomGroupAsync(
            groupId, new UpdateRoomGroupRequest(groupName: "Lifecycle Renamed"), TestContext.Current.CancellationToken)).Response;
        renamed.Name.Should().Be("Lifecycle Renamed");

        // Act & Assert — add a room
        var added = (await _roomGroupsApi.UpdateRoomGroupAsync(
            groupId,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken)).Response;
        added.TotalRooms.Should().Be(2);

        // Act & Assert — remove a room
        var removed = (await _roomGroupsApi.UpdateRoomGroupAsync(
            groupId,
            new UpdateRoomGroupRequest(roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(ids[0])]),
            TestContext.Current.CancellationToken)).Response;
        removed.TotalRooms.Should().Be(1);

        // Act & Assert — change icon
        var iconChanged = (await _roomGroupsApi.ChangeRoomGroupIconAsync(groupId, new IconRequest("heart"), TestContext.Current.CancellationToken)).Response;
        iconChanged.Icon.Id.Should().Be("heart");

        // Act & Assert — delete and confirm gone
        await _roomGroupsApi.DeleteRoomGroupAsync(groupId, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.GetRoomGroupInfoAsync(groupId, cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateThenRename_LeavesIconAndRoomsUnchangedAcrossAllReadMethods()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("CR Rename");
        var created = await CreateRoomGroup("CR Before", [roomId], "flag");

        // Act
        await _roomGroupsApi.UpdateRoomGroupAsync(created.Id, new UpdateRoomGroupRequest(groupName: "CR After"), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Icon.Id.Should().Be("flag");
        info.TotalRooms.Should().Be(1);

        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var g = list.Single(x => x.Id == created.Id);
        g.Name.Should().Be("CR After");
        g.Icon.Id.Should().Be("flag");
    }

    [Fact]
    public async Task CreateThenAddRoom_VisibleThroughInfoAndList()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "CR Add");
        var created = await CreateRoomGroup("CR Add Group", [ids[0]]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken)).Response;
        updated.TotalRooms.Should().Be(2);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.TotalRooms.Should().Be(2);

        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Single(x => x.Id == created.Id).TotalRooms.Should().Be(2);
    }

    [Fact]
    public async Task CreateThenRemoveRoom_VisibleThroughInfoAndList()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "CR Remove");
        var created = await CreateRoomGroup("CR Remove Group", ids);

        // Act
        await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.TotalRooms.Should().Be(1);

        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Single(x => x.Id == created.Id).TotalRooms.Should().Be(1);
    }

    [Fact]
    public async Task RenamingARoom_IsReflectedInTheGroupsRoomList()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Room Old Title");
        var created = await CreateRoomGroup("Room Rename Group", [roomId]);

        // Act
        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest { Title = "Room New Title" }, TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Rooms.Select(r => r.Title).Should().Contain("Room New Title");
    }

    /// <summary>
    /// Intended contract: archiving a room removes it from its group (the group of a single
    /// archived room becomes empty), and unarchiving restores its membership. The API currently
    /// keeps the archived room in the group.
    /// </summary>
    [Fact]
    [Trait("Bug", "82601")]
    public async Task ArchivingTheOnlyRoom_ShouldEmptyTheGroupAndUnarchivingShouldRestoreIt()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Archive Member");
        var created = await CreateRoomGroup("Archive Member Group", [roomId]);

        // Act — archive
        await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert — the archived room leaves the group -> empty.
        var archived = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        archived.TotalRooms.Should().Be(0);
        archived.Rooms.Should().BeEmpty();

        // Act — unarchive
        await _roomsApi.UnarchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert — membership in the same group is restored.
        var restored = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        restored.TotalRooms.Should().Be(1);
        restored.Rooms.Select(r => r.Title).Should().Contain("Archive Member");
    }

    [Fact]
    public async Task RoomArchivedThenUnarchived_ReturnsToTheSameGroup()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("RoundTrip Room");
        var created = await CreateRoomGroup("RoundTrip Group", [roomId]);

        // Act
        await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        await _roomsApi.UnarchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Rooms.Select(r => r.Title).Should().Contain("RoundTrip Room");
        info.TotalRooms.Should().Be(1);
    }

    [Fact]
    public async Task DeletingARoom_RemovesItFromTheGroup()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Doomed Room");
        var created = await CreateRoomGroup("Doomed Room Group", [roomId]);

        // Act
        await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();
        await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert — no dangling reference.
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.TotalRooms.Should().Be(0);
        info.Rooms.Should().BeEmpty();
    }

    [Fact]
    public async Task Group_PersistsAndStaysAccessible_AfterTheOwnerReAuthenticates()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Persist Room");
        var created = await CreateRoomGroup("Persistent Group", [roomId]);

        // Act
        await _filesClient.Authenticate(Owner, forceRefresh: true);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Name.Should().Be("Persistent Group");
    }
}
