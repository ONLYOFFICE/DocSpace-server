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
/// POST /api/2.0/group/{id}/members - replacing the entire member list of a group.
/// </summary>
public class GroupMembersReplaceTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private Task<GroupWrapper> CreateGroupAsync(string name, Guid? manager = null, List<Guid>? members = null)
    {
        return _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: manager ?? Owner.Id, members: members!),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SetMembersTo_OneNewUser_ReplacesTheExistingMember()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [oldMember.Id]);

        var result = await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        result.Response.Members.Select(m => m.Id).Should().Contain(newMember.Id);
    }

    [Fact]
    public async Task SetMembersTo_MultipleNewUsers_ReplacesTheExistingMembers()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var n1 = await InviteContact(EmployeeType.User);
        var n2 = await InviteContact(EmployeeType.User);
        var n3 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [oldMember.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([n1.Id, n2.Id, n3.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain([n1.Id, n2.Id, n3.Id]);
    }

    [Fact]
    public async Task SetMembersTo_OldMembersNotInTheNewList_AreRemoved()
    {
        var oldA = await InviteContact(EmployeeType.User);
        var oldB = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [oldA.Id, oldB.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().NotContain(oldA.Id);
        memberIds.Should().NotContain(oldB.Id);
    }

    [Fact]
    public async Task SetMembersTo_FinalMembersContainOnlyThoseFromTheRequestPlusTheManager()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var n1 = await InviteContact(EmployeeType.User);
        var n2 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [oldMember.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([n1.Id, n2.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var expected = new HashSet<Guid> { n1.Id, n2.Id, Owner.Id };
        group.Response.Members.Select(m => m.Id).Should().OnlyContain(id => expected.Contains(id));
        group.Response.Members.Select(m => m.Id).Should().NotContain(oldMember.Id);
    }

    [Fact]
    public async Task SetMembersTo_ReturnsTheUpdatedGroupWithIdNameAndNewMembers()
    {
        var newMember = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName);

        var result = await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        result.Response.Id.Should().Be(created.Response.Id);
        result.Response.Name.Should().Be(groupName);
        result.Response.Members.Select(m => m.Id).Should().Contain(newMember.Id);
    }

    [Fact]
    public async Task SetMembersTo_ReplacedMembersPersistAfterRefetchingTheGroup()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [oldMember.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        var first = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var second = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        var firstIds = first.Response.Members.Select(m => m.Id).ToList();
        var secondIds = second.Response.Members.Select(m => m.Id).ToList();
        firstIds.Should().Contain(newMember.Id);
        firstIds.Should().NotContain(oldMember.Id);
        secondIds.Should().Contain(newMember.Id);
        secondIds.Should().NotContain(oldMember.Id);
    }

    [Fact]
    public async Task SetMembersTo_SameUserIdTwice_DoesNotCreateADuplicateMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([member.Id, member.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Members.Count(m => m.Id == member.Id).Should().Be(1);
    }

    [Fact]
    public async Task SetMembersTo_GroupNameIsPreservedAfterReplacingMembers()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName, members: [oldMember.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Name.Should().Be(groupName);
    }

    [Fact]
    public async Task SetMembersTo_ManagerNotInNewList_IsRemovedFromMembersAndFromManager()
    {
        var manager = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), manager: manager.Id);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().NotContain(manager.Id);
        memberIds.Should().Contain(newMember.Id);
        group.Response.Manager.Should().BeNull();
    }

    [Fact]
    public async Task SetMembersTo_ManagerPassedExplicitly_StaysInMembersListOnce()
    {
        var manager = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), manager: manager.Id);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([manager.Id, newMember.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id).ToList();
        memberIds.Should().Contain(manager.Id);
        memberIds.Should().Contain(newMember.Id);
        memberIds.Count(id => id == manager.Id).Should().Be(1);
    }

    [Fact]
    public async Task SetMembersTo_MixOfExistingAndNewMembers_ResultsInExactlyThoseMembers()
    {
        var keep = await InviteContact(EmployeeType.User);
        var drop = await InviteContact(EmployeeType.User);
        var add = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [keep.Id, drop.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([keep.Id, add.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(keep.Id);
        memberIds.Should().Contain(add.Id);
        memberIds.Should().NotContain(drop.Id);
    }

    [Fact]
    public async Task SetMembersTo_ReplacedUsersRemainInThePortal()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [oldMember.Id]);

        await _groupApi.SetMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        var profile = await _profilesApi.GetProfileByUserIdAsync(oldMember.Id.ToString(), TestContext.Current.CancellationToken);
        profile.Response.Id.Should().Be(oldMember.Id);
    }
}
