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
/// DELETE /api/2.0/group/{id}/members - removing members from a group.
/// "Owner removes the only member" and "Owner removes one member from group" assert the exact
/// same thing in the TS suite (a single-member group with that member removed); likewise "Owner
/// removes multiple members" and "Owner can remove all members" both remove every member from a
/// group and check the same fields. Both pairs are merged here into one case each.
/// </summary>
public class GroupMembersRemoveTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private Task<GroupWrapper> CreateGroupAsync(string name, List<Guid>? members = null)
    {
        return _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: Owner.Id, members: members!),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RemoveMembersFrom_TheOnlyMember_MemberIsRemoved()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_AllMembers_RemovesEveryoneAndKeepsTheGroupId()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [m1.Id, m2.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([m1.Id, m2.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Id.Should().Be(created.Response.Id);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().NotContain(m1.Id);
        memberIds.Should().NotContain(m2.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_KeepsOtherExistingMembers()
    {
        var keep = await InviteContact(EmployeeType.User);
        var remove = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [keep.Id, remove.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([remove.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(keep.Id);
        memberIds.Should().NotContain(remove.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_ReturnsTheUpdatedGroupInTheResponse()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName, [member.Id]);

        var result = await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        result.Response.Id.Should().Be(created.Response.Id);
        result.Response.Name.Should().Be(groupName);
        result.Response.Manager.Id.Should().Be(Owner.Id);
        result.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_MembersCountDecreasesByTheNumberRemoved()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [m1.Id, m2.Id]);
        var initialCount = created.Response.MembersCount;

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([m1.Id, m2.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.MembersCount.Should().Be(initialCount - 2);
    }

    [Fact]
    public async Task RemoveMembersFrom_RemovalPersistsAfterRefetchingTheGroup()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var first = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var second = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        first.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
        second.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_RemovedUserIsNotDeletedFromThePortal()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var profile = await _profilesApi.GetProfileByUserIdAsync(member.Id.ToString(), TestContext.Current.CancellationToken);
        profile.Response.Id.Should().Be(member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_RemovedUserDisappearsFromGetGroupWithIncludeMembers()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        var before = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        before.Response.Members.Select(m => m.Id).Should().Contain(member.Id);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var after = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        after.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_RemovedUserNoLongerHasTheGroupInGetGroupByUserId()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        var before = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        before.Response.Select(g => g.Id).Should().Contain(created.Response.Id);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var after = await _groupApi.GetGroupByUserIdAsync(member.Id, TestContext.Current.CancellationToken);
        after.Response.Select(g => g.Id).Should().NotContain(created.Response.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_GroupNameIsPreserved()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName, [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Name.Should().Be(groupName);
    }

    [Fact]
    public async Task RemoveMembersFrom_GroupManagerIsPreserved()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Manager.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_DoesNotAffectUnrelatedGroups()
    {
        var shared = await InviteContact(EmployeeType.User);
        var groupA = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [shared.Id]);
        var groupB = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [shared.Id]);

        await _groupApi.RemoveMembersFromAsync(groupA.Response.Id, new MembersRequest([shared.Id]), TestContext.Current.CancellationToken);

        var groupBData = await _groupApi.GetGroupAsync(groupB.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        groupBData.Response.Members.Select(m => m.Id).Should().Contain(shared.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_SameMemberTwice_IsIdempotent()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);
        await _groupApi.RemoveMembersFromAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }
}
