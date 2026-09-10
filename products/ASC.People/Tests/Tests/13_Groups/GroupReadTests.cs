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

namespace ASC.People.Tests.Tests._13_Groups;

/// <summary>
/// GET /api/2.0/group/{id} and GET /api/2.0/group/user/{userid} - reading a single group and the
/// groups a given user belongs to.
/// </summary>
public class GroupReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private Task<GroupWrapper> CreateGroupAsync(string name, List<Guid>? members = null)
    {
        return _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: Owner.Id, members: members!),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetGroup_WithoutIncludeMembers_MembersAreReturnedByDefault()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName, [member.Id]);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, cancellationToken: TestContext.Current.CancellationToken);

        group.Response.Id.Should().Be(created.Response.Id);
        group.Response.Name.Should().Be(groupName);
        group.Response.Members.Should().NotBeNull();
        group.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
    }

    [Fact]
    public async Task GetGroup_IncludeMembersTrue_ReturnsMembers()
    {
        var member1 = await InviteContact(EmployeeType.User);
        var member2 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member1.Id, member2.Id]);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        group.Response.Id.Should().Be(created.Response.Id);
        group.Response.Members.Should().NotBeNull();
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(member1.Id);
        memberIds.Should().Contain(member2.Id);
    }

    [Fact]
    public async Task GetGroup_IncludeMembersFalse_MembersAreNotReturned()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: false, cancellationToken: TestContext.Current.CancellationToken);

        group.Response.Id.Should().Be(created.Response.Id);
        group.Response.Members.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetGroup_CalledTwice_IsIdempotent()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName, [member.Id]);

        var first = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var second = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        second.Response.Id.Should().Be(first.Response.Id);
        second.Response.Name.Should().Be(first.Response.Name);
        second.Response.Manager.Id.Should().Be(first.Response.Manager.Id);
        second.Response.Members.Select(m => m.Id).OrderBy(x => x)
            .Should().Equal(first.Response.Members.Select(m => m.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task GetGroupByUserId_ReturnsGroupsWhereUserIsMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        var groups = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);

        groups.Response.Select(g => g.Id).Should().Contain(created.Response.Id);
    }

    [Fact]
    public async Task GetGroupByUserId_ReturnsMultipleGroupsForOneUser()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var group = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);
            groupIds.Add(group.Response.Id);
        }

        var groups = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);

        var ids = groups.Response.Select(g => g.Id).ToList();
        ids.Should().Contain(groupIds);
    }

    [Fact]
    public async Task GetGroupByUserId_UserWithNoGroups_ReturnsEmptyArray()
    {
        var member = await InviteContact(EmployeeType.User);

        var groups = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);

        groups.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupByUserId_ResponseItemsContainGroupSummaryFields()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName, [member.Id]);

        var groups = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);

        var summary = groups.Response.Should().ContainSingle(g => g.Id == created.Response.Id).Subject;
        summary.Name.Should().Be(groupName);
    }

    [Fact]
    public async Task GetGroupByUserId_UpdatesAfterAddingUserToGroup()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var before = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        before.Response.Select(g => g.Id).Should().NotContain(created.Response.Id);

        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var after = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        after.Response.Select(g => g.Id).Should().Contain(created.Response.Id);
    }

    [Fact]
    public async Task GetGroupByUserId_UpdatesAfterRemovingUserFromGroup()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        var before = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        before.Response.Select(g => g.Id).Should().Contain(created.Response.Id);

        await _groupApi.UpdateGroupAsync(created.Response.Id, new UpdateGroupRequest(membersToRemove: [member.Id]), TestContext.Current.CancellationToken);

        var after = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        after.Response.Select(g => g.Id).Should().NotContain(created.Response.Id);
    }

    [Fact]
    public async Task GetGroupByUserId_MatchesGetGroupsFilteredByUserId()
    {
        var member = await InviteContact(EmployeeType.User);
        for (var i = 0; i < 2; i++)
        {
            await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);
        }

        var byUser = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        var filtered = await _groupApi.GetGroupsAsync(userId: member.Id, cancellationToken: TestContext.Current.CancellationToken);

        byUser.Response.Select(g => g.Id).OrderBy(x => x)
            .Should().Equal(filtered.Response.Select(g => g.Id).OrderBy(x => x));
    }
}
