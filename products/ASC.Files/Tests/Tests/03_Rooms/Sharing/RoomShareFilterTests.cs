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

namespace ASC.Files.Tests.Tests._03_Rooms.Sharing;

/// <summary>
/// <c>GET /files/rooms/{id}/share</c> with an explicit <c>filterType</c>, a <c>filterValue</c> and
/// paging: the user, group and user-or-group branches of the share query, the text search over
/// user and group names, and the <c>total</c> of the list. The other suites only call the endpoint
/// with its defaults.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareFilterTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task GetRoomSecurityInfo_FilterUser_ReturnsUsersOnly()
    {
        var (room, user, groupId) = await CreateRoomWithUserAndGroup("Autotest Filter User");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, ShareFilterType.User, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Should().OnlyContain(s => s.SharedToUser != null && s.SharedToGroup == null);
        info.Should().Contain(s => s.SharedToUser.Id == user.Id);
        info.Should().Contain(s => s.SharedToUser.Id == Owner.Id);
        info.Should().NotContain(s => s.SharedToGroup != null && s.SharedToGroup.Id == groupId);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_FilterGroup_ReturnsGroupsOnly()
    {
        var (room, _, groupId) = await CreateRoomWithUserAndGroup("Autotest Filter Group");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, ShareFilterType.Group, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Should().ContainSingle();
        info[0].SharedToGroup.Should().NotBeNull();
        info[0].SharedToGroup.Id.Should().Be(groupId);
        info[0].SubjectType.Should().Be(SubjectType.Group);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_FilterUserOrGroup_ReturnsUsersAndGroups()
    {
        var (room, user, groupId) = await CreateRoomWithUserAndGroup("Autotest Filter UserOrGroup");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, ShareFilterType.UserOrGroup, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);
        info.Should().Contain(s => s.SharedToGroup != null && s.SharedToGroup.Id == groupId);
        info.Should().HaveCount(3, "the owner, the invited user and the group");
    }

    [Fact]
    public async Task GetRoomSecurityInfo_SearchByGroupName_ReturnsTheGroup()
    {
        var (room, _, groupId) = await CreateRoomWithUserAndGroup("Autotest Filter Search Group");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            ShareFilterType.UserOrGroup,
            filterValue: "Zebrafilter",
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Should().ContainSingle();
        info[0].SharedToGroup?.Id.Should().Be(groupId);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_SearchByUserEmail_ReturnsTheUser()
    {
        var (room, user, _) = await CreateRoomWithUserAndGroup("Autotest Filter Search User");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            ShareFilterType.UserOrGroup,
            filterValue: user.Email,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);
        info.Should().NotContain(s => s.SharedToGroup != null);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_SearchWithoutMatch_ReturnsEmpty()
    {
        var (room, _, _) = await CreateRoomWithUserAndGroup("Autotest Filter Search None");

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            ShareFilterType.Group,
            filterValue: "nomatch" + Guid.NewGuid().ToString("N")[..8],
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomSecurityInfo_Paging_SecondPageHoldsTheGroupAndTotalCountsAll()
    {
        var (room, _, groupId) = await CreateRoomWithUserAndGroup("Autotest Filter Paging");

        var page = (await _roomsApi.GetRoomSecurityInfoAsync(
            room.Id,
            ShareFilterType.UserOrGroup,
            count: 1,
            startIndex: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        var total = await ReadShareTotal(room.Id, "filterType=UserOrGroup&count=1&startIndex=1");

        // the owner (room manager) sorts first, the group second, the invited user last
        page.Should().ContainSingle();
        page[0].SharedToGroup?.Id.Should().Be(groupId);
        total.Should().Be(3);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_FilterGroup_TotalCountsGroupsOnly()
    {
        var (room, _, _) = await CreateRoomWithUserAndGroup("Autotest Filter Group Total");

        var total = await ReadShareTotal(room.Id, "filterType=Group");

        total.Should().Be(1);
    }

    private async Task<(FolderDto Room, User User, Guid GroupId)> CreateRoomWithUserAndGroup(string title)
    {
        var user = await InviteMember(EmployeeType.User);
        var member = await InviteMember(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto([member.Id], member.Id, $"Zebrafilter Group {Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom(title);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user.Id, Access = FileShare.Read },
                new RoomInvitation { Id = group.Id, Access = FileShare.Read }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        return (room, user, group.Id);
    }

    /// <summary>
    /// Reads the <c>total</c> of the room share list from the raw JSON: <c>FileShareArrayWrapper</c>
    /// in the SDK has no <c>Total</c> property, so the typed call cannot see it.
    /// </summary>
    private async Task<int> ReadShareTotal(int roomId, string query)
    {
        using var response = await _filesClient.GetAsync($"api/2.0/files/rooms/{roomId}/share?{query}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.Should().BeTrue(body);

        using var json = JsonDocument.Parse(body);

        return json.RootElement.GetProperty("total").GetInt32();
    }
}
