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
/// A group keeps its references to rooms, but it may only show the rooms its owner can still read.
/// These tests revoke the member's access to a room that is already in the member's own group.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomGroupRoomAccessTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Update_RoomTheMemberLostAccessTo_CanStillBeDetached()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(EmployeeType.User);
        var roomId = await CreateGroupRoomId("Lost Access Room");
        await InviteToRoom(roomId, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        var group = await CreateRoomGroup("Lost Access Group", [roomId]);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, member, FileShare.None);

        // premise: the access is really gone
        await _filesClient.Authenticate(member);
        var denied = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken));
        denied.ErrorCode.Should().Be(403);

        // Act
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            group.Id,
            new UpdateRoomGroupRequest(roomsToRemove: [new DuplicateRequestDtoAllOfFileIds(roomId)]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.TotalRooms.Should().Be(0);

        var after = (await _roomGroupsApi.GetRoomGroupInfoAsync(group.Id, true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.TotalRooms.Should().Be(0);
        (after.Rooms ?? []).Should().BeEmpty();

        // the room itself is untouched
        await _filesClient.Authenticate(Owner);
        (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.Title.Should().Be("Lost Access Room");
    }

    /// <summary>
    /// After the member's access to a room is revoked, the member's own group must stop serving that
    /// room: its title (which follows the owner's renames), counters, tags and authors. Both group endpoints
    /// used to keep returning all of them while <c>GET /files/rooms/{id}</c> answered 403; the group's rooms
    /// are now filtered by read access, like archived rooms the reference stays and only the room is hidden.
    /// </summary>
    [Fact]
    [Trait("Bug", "84474")]
    public async Task Read_RoomTheMemberLostAccessTo_IsNotExposed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(EmployeeType.User);
        var roomId = await CreateGroupRoomId("Leak Before");
        await InviteToRoom(roomId, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        var group = await CreateRoomGroup("Leak Group", [roomId]);

        // positive control: while access exists the member sees the room
        var seen = (await _roomGroupsApi.GetRoomGroupInfoAsync(group.Id, true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        seen.Rooms.Select(r => r.Title).Should().Contain("Leak Before");

        // access revoked, then the owner renames the room
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, member, FileShare.None);
        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(title: "Leak After Revoke"), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(member);
        var denied = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken));
        denied.ErrorCode.Should().Be(403);

        // Act
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(group.Id, true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var listed = (await _roomGroupsApi.GetRoomGroupsAsync(true, cancellationToken: TestContext.Current.CancellationToken)).Response
            .Single(g => g.Id == group.Id);

        // Assert
        info.Rooms.Select(r => r.Title).Should().NotContain("Leak After Revoke");
        info.TotalRooms.Should().Be(0);
        listed.Rooms.Select(r => r.Title).Should().NotContain("Leak After Revoke");
        listed.TotalRooms.Should().Be(0);
    }
}
