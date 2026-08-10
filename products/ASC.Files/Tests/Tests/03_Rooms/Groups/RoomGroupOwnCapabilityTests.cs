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
/// Room groups are a per-user feature: any role that can access the rooms it references may
/// create and manage its OWN groups. Access is gated by the rooms passed in, not by the caller's
/// role — these tests exercise that gate on create, and full CRUD capability once a role has a
/// room it can reference.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomGroupOwnCapabilityTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    #region Role capabilities - own groups

    public static TheoryData<EmployeeType> CapabilityRoles =>
        [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest];

    /// <summary>
    /// Every role gets the SAME full capability check (create, read, update, re-icon, list,
    /// delete). The only difference is how the role obtains an accessible room:
    /// DocSpaceAdmin/RoomAdmin create their own, while User/Guest get one shared by the owner.
    /// </summary>
    [Theory]
    [MemberData(nameof(CapabilityRoles))]
    public async Task Role_CanFullyManageItsOwnRoomGroup(EmployeeType role)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(role);

        int roomId;
        if (role is EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin)
        {
            await _filesClient.Authenticate(member);
            roomId = (await CreateCustomRoom($"{role} Own Room")).Id;
        }
        else
        {
            roomId = await CreateGroupRoomId($"{role} Shared Room");
            await InviteToRoom(roomId, member, FileShare.ContentCreator);
            await _filesClient.Authenticate(member);
        }

        // Act — create, belongs to THIS user with the requested contents.
        var created = await CreateRoomGroup($"{role} Own Group", [roomId]);

        // Assert
        created.UserId.Should().Be(member.Id);
        created.Name.Should().Be($"{role} Own Group");
        created.TotalRooms.Should().Be(1);

        // read
        var read = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        read.Name.Should().Be($"{role} Own Group");

        // update — renames, verified via a re-read.
        await _roomGroupsApi.UpdateRoomGroupAsync(created.Id, new UpdateRoomGroupRequest(groupName: $"{role} Renamed"), TestContext.Current.CancellationToken);
        (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Name.Should().Be($"{role} Renamed");

        // re-icon — change persists.
        await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest("heart"), TestContext.Current.CancellationToken);
        (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Icon.Id.Should().Be("heart");

        // list — the group is visible in the role's own listing.
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Select(g => g.Id).Should().Contain(created.Id);

        // delete — then gone.
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    #endregion

    #region Room-access gate on create

    // Passing a room the caller cannot access is rejected with 403 regardless of role.
    // DocSpaceAdmin is excluded: it can reach the owner's rooms, so the gate does not apply.
    public static TheoryData<EmployeeType> GatedRoles => [EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest];

    [Theory]
    [MemberData(nameof(GatedRoles))]
    public async Task Create_InaccessibleRoom_Forbidden(EmployeeType role)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateGroupRoomId($"Gate {role} Room");
        var member = await InviteMember(role);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto($"{role} No Access", "star", [new DuplicateRequestDtoAllOfFileIds(roomId)]),
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Atomicity: the rejected create must not leave a (partially created, empty) group in the
    /// caller's own listing. The API currently DOES create it, so this is a bug — the same
    /// partial-create defect seen for missing/invalid rooms, here on the access-denied path.
    /// </summary>
    [Theory]
    [MemberData(nameof(GatedRoles))]
    [Trait("Bug", "82598")]
    public async Task Create_RejectedForInaccessibleRoom_ShouldNotLeaveAPartialGroup(EmployeeType role)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateGroupRoomId($"Gate {role} Room");
        var member = await InviteMember(role);
        await _filesClient.Authenticate(member);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = $"{role} No Access", icon = "star", rooms = new[] { roomId } });

        // Assert — nothing is created in the caller's listing.
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Select(g => g.Name).Should().NotContain($"{role} No Access");
    }

    #endregion
}
