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

/// <summary>PUT /files/group/{id} — adding, removing and swapping rooms.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupUpdateRoomsTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    #region add rooms

    [Fact]
    public async Task AddRooms_SeveralRooms_Succeeds()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(3, "AddMulti");
        var created = await CreateRoomGroup("AddMulti Group", [ids[0]]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1]), new DuplicateRequestDtoAllOfFileIds(ids[2])]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(3);
    }

    [Fact]
    public async Task AddRooms_AlreadyPresentRoom_IsNoOp()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("AlreadyIn");
        var created = await CreateRoomGroup("AlreadyIn Group", [roomId]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(roomId)]),
            TestContext.Current.CancellationToken)).Response;

        // Assert — no duplication.
        updated.TotalRooms.Should().Be(1);
    }

    [Fact]
    public async Task AddRooms_EmptyRoomsToAdd_IsNoOp()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("EmptyAdd");
        var created = await CreateRoomGroup("EmptyAdd Group", [roomId]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(roomsToAdd: []), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(1);
    }

    /// <summary>
    /// <c>roomsToAdd</c> is optional but not nullable; create rejects <c>rooms: null</c> with 400.
    /// </summary>
    [Fact]
    [Trait("Bug", "82591")]
    public async Task AddRooms_NullRoomsToAdd_ShouldBe400LikeCreate()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("NullAdd");
        var created = await CreateRoomGroup("NullAdd Group", [roomId]);

        // Act — `roomsToAdd` is nullable on the DTO (unlike create's required `rooms`), so the
        // null value itself is directly expressible.
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(roomsToAdd: null!), TestContext.Current.CancellationToken));

        // Assert — no data corruption either way (null is a no-op), so the room set stays 1; the
        // bug is purely the accepted-instead-of-rejected status.
        var after = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.TotalRooms.Should().Be(1);
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task AddRooms_NonExistentRoom_LeavesGroupUnchanged()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("AddMissing");
        var created = await CreateRoomGroup("AddMissing Group", [roomId]);

        // Act
        try
        {
            await _roomGroupsApi.UpdateRoomGroupAsync(
                created.Id,
                new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(999999)]),
                TestContext.Current.CancellationToken);
        }
        catch (ApiException)
        {
            // Expected: the non-existent room is currently refused (see the BUG 82592 test below).
        }

        // Assert — no-side-effect invariant: the rejected room is not added.
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.TotalRooms.Should().Be(1);
    }

    [Fact]
    [Trait("Bug", "82592")]
    public async Task AddRooms_NonExistentRoom_ShouldBe404()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("AddMissing404");
        var created = await CreateRoomGroup("AddMissing404 Group", [roomId]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(999999)]),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// Confirmed contract: the update is intentionally NOT atomic, same as create. The response
    /// reports the non-existent room (403), but the rooms that could be resolved are still added.
    /// </summary>
    [Fact]
    public async Task AddRooms_MixedValidAndNonExistent_RefusedButValidRoomStillAdded()
    {
        // Arrange
        var seed = await CreateGroupRoomId("AtomicSeed");
        var valid = await CreateGroupRoomId("AtomicValidAdd");
        var created = await CreateRoomGroup("Atomic Add Group", [seed]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(valid), new DuplicateRequestDtoAllOfFileIds(999999)]),
            TestContext.Current.CancellationToken));

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Rooms.Select(r => r.Title).Should().Contain("AtomicValidAdd");
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "82594")]
    public async Task AddRooms_DuplicateRoomIds_ShouldDedupInsteadOfFail()
    {
        // Arrange
        var seed = await CreateGroupRoomId("DupAddSeed");
        var dup = await CreateGroupRoomId("DupAddRoom");
        var created = await CreateRoomGroup("Dup Add Group", [seed]);

        // Act & Assert — the server currently returns 500 instead of deduplicating; the correct
        // contract is a plain successful update (no ApiException).
        await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(dup), new DuplicateRequestDtoAllOfFileIds(dup)]),
            TestContext.Current.CancellationToken);
    }

    #endregion

    #region remove rooms

    [Fact]
    public async Task RemoveRooms_RemovingARoom_DecreasesTotalRooms()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Remove");
        var created = await CreateRoomGroup("Remove Group", ids);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(1);
        updated.Rooms.Select(r => r.Title).Should().NotContain("Remove 2");
    }

    [Fact]
    public async Task RemoveRooms_RemovingANonMemberRoom_IsNoOp()
    {
        // Arrange
        var member = await CreateGroupRoomId("Member");
        var outsider = await CreateGroupRoomId("Outsider");
        var created = await CreateRoomGroup("Outsider Group", [member]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(outsider)]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(1);
    }

    [Fact]
    [Trait("Bug", "82595")]
    public async Task RemoveRooms_NonExistentRoom_ShouldBe404()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("RemoveMissing");
        var created = await CreateRoomGroup("RemoveMissing Group", [roomId]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(999999)]),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RemoveRooms_EmptyRoomsToRemove_IsNoOp()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("EmptyRemove");
        var created = await CreateRoomGroup("EmptyRemove Group", [roomId]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(roomsToRemove: []), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(1);
    }

    /// <summary>
    /// By design: create refuses an empty group, but update MAY empty an existing group by
    /// removing its last room. The asymmetry is intentional.
    /// </summary>
    [Fact]
    public async Task RemoveRooms_RemovingTheLastRoom_LeavesAnEmptyGroup()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Last");
        var created = await CreateRoomGroup("Emptying Group", ids);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToRemove: [.. ids.Select(r => new DuplicateRequestDtoAllOfFileIds(r))]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(0);
        updated.Rooms.Should().BeEmpty();
    }

    #endregion

    #region add and remove together

    [Fact]
    public async Task AddAndRemove_InASingleRequest_BothApply()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Swap");
        var created = await CreateRoomGroup("Swap Group", [ids[0]]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(
                roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1])],
                roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(ids[0])]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(1);
        var titles = updated.Rooms.Select(r => r.Title).ToList();
        titles.Should().Contain("Swap 2");
        titles.Should().NotContain("Swap 1");
    }

    [Fact]
    public async Task RenameAndChangeRooms_InOneRequest_BothApply()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Combo");
        var created = await CreateRoomGroup("Combo Before", [ids[0]]);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(groupName: "Combo After", roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Name.Should().Be("Combo After");
        updated.TotalRooms.Should().Be(2);
    }

    #endregion
}
