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

/// <summary>A room group belongs to the user who created it; no other portal member — not even a DocSpaceAdmin — may read, update, re-icon or delete it.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupCrossUserIsolationTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task GetList_AUsersList_ContainsOnlyItsOwnGroups()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var ownerRoom = await CreateGroupRoomId("Owner List Room");
        await CreateRoomGroup("Owner Only Group", [ownerRoom]);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var adminRoom = await CreateGroupRoomId("Admin List Room");
        await CreateRoomGroup("Admin Only Group", [adminRoom]);

        // Act
        var adminList = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(Owner);
        var ownerList = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var ownerNames = ownerList.Select(g => g.Name).ToList();
        var adminNames = adminList.Select(g => g.Name).ToList();

        ownerNames.Should().Contain("Owner Only Group");
        ownerNames.Should().NotContain("Admin Only Group");
        adminNames.Should().Contain("Admin Only Group");
        adminNames.Should().NotContain("Owner Only Group");
    }

    public static TheoryData<EmployeeType> AllRoles =>
        [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest];

    [Theory]
    [MemberData(nameof(AllRoles))]
    public async Task Role_CannotReadUpdateOrReIcon_TheOwnersGroup(EmployeeType role)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateGroupRoomId($"Iso {role} Room");
        var created = await CreateRoomGroup($"Owner Group vs {role}", [roomId], "star");

        var member = await InviteMember(role);
        await _filesClient.Authenticate(member);

        // Act & Assert — read is 404.
        var readException = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));
        readException.ErrorCode.Should().Be(404);

        // Act & Assert — update is 404, name unchanged.
        var updateException = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(groupName: "Hacked"), TestContext.Current.CancellationToken));
        updateException.ErrorCode.Should().Be(404);

        await _filesClient.Authenticate(Owner);
        (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Name.Should().Be($"Owner Group vs {role}");

        // Act & Assert — change icon is 404, icon unchanged.
        await _filesClient.Authenticate(member);
        var iconException = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.ChangeRoomGroupIconAsync(
            created.Id, new IconRequest("heart"), TestContext.Current.CancellationToken));
        iconException.ErrorCode.Should().Be(404);

        await _filesClient.Authenticate(Owner);
        (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Icon.Id.Should().Be("star");
    }

    /// <summary>
    /// Deleting another user's group must be forbidden (403), consistent with read/update
    /// returning 404 for cross-user access. The API currently accepts the request as a silent 200
    /// no-op (the group survives, but the status is wrong).
    /// </summary>
    [Theory]
    [MemberData(nameof(AllRoles))]
    [Trait("Bug", "82597")]
    public async Task Role_DeletingTheOwnersGroup_ShouldBe403NotASilentNoOp(EmployeeType role)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateGroupRoomId($"DelIso {role} Room");
        var created = await CreateRoomGroup($"Owner Del Group vs {role}", [roomId]);

        var member = await InviteMember(role);
        await _filesClient.Authenticate(member);

        // Act — correct contract: the cross-user delete is refused with 403.
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert — the owner's group must survive regardless (holds under both the current
        // no-op and the correct 403 behaviour).
        await _filesClient.Authenticate(Owner);
        var survive = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        survive.Name.Should().Be($"Owner Del Group vs {role}");

        exception.ErrorCode.Should().Be(403);
    }
}
