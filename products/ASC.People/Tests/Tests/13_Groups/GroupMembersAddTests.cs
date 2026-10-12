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
/// PUT /api/2.0/group/{id}/members - adding members to an existing group.
/// </summary>
public class GroupMembersAddTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private Task<GroupWrapper> CreateGroupAsync(string name, List<Guid>? members = null)
    {
        return _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: Owner.Id, members: members!),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddMembersTo_OneUser_UserIsAddedToTheGroup()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var result = await _groupApi.AddMembersToAsync(
            created.Response.Id,
            new MembersRequest([member.Id]),
            TestContext.Current.CancellationToken);

        result.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
    }

    [Fact]
    public async Task AddMembersTo_MultipleUsers_AllAreAddedToTheGroup()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var m3 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([m1.Id, m2.Id, m3.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(m1.Id);
        memberIds.Should().Contain(m2.Id);
        memberIds.Should().Contain(m3.Id);
    }

    [Fact]
    public async Task AddMembersTo_KeepsExistingMembers()
    {
        var existing = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [existing.Id]);

        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(existing.Id);
        memberIds.Should().Contain(newMember.Id);
    }

    [Fact]
    public async Task AddMembersTo_MembersCountIncreasesByTheNumberAdded()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        var initialCount = created.Response.MembersCount;

        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([m1.Id, m2.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.MembersCount.Should().Be(initialCount + 2);
    }

    [Fact]
    public async Task AddMembersTo_SameUserTwice_DoesNotDuplicateTheMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);
        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Members.Count(m => m.Id == member.Id).Should().Be(1);
    }

    [Fact]
    public async Task AddMembersTo_ReturnsTheUpdatedGroupWithTheAddedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var groupName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(groupName);

        var result = await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        result.Response.Id.Should().Be(created.Response.Id);
        result.Response.Name.Should().Be(groupName);
        result.Response.Manager.Id.Should().Be(Owner.Id);
        result.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
    }

    [Fact]
    public async Task AddMembersTo_AddedMembersPersistAfterRefetchingTheGroup()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.AddMembersToAsync(created.Response.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken);

        var first = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var second = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        first.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
        second.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
    }
}
