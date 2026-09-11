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
/// PUT /api/2.0/group/{fromId}/members/{toId} - moving all members from one group to another.
/// </summary>
public class GroupMembersMoveTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private Task<GroupWrapper> CreateGroupAsync(string name, List<Guid>? members = null)
    {
        return _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: Owner.Id, members: members!),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveMembersTo_OneMember_MovesFromSourceToTarget()
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        targetAfter.Response.Members.Select(m => m.Id).Should().Contain(member.Id);

        var sourceAfter = await _groupApi.GetGroupAsync(source.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        sourceAfter.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task MoveMembersTo_MultipleMembers_MovesAllOfThem()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var m3 = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [m1.Id, m2.Id, m3.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var targetIds = targetAfter.Response.Members.Select(m => m.Id);
        targetIds.Should().Contain([m1.Id, m2.Id, m3.Id]);

        var sourceAfter = await _groupApi.GetGroupAsync(source.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var sourceIds = sourceAfter.Response.Members.Select(m => m.Id);
        sourceIds.Should().NotContain([m1.Id, m2.Id, m3.Id]);
    }

    [Fact]
    public async Task MoveMembersTo_PreservesExistingTargetMembers()
    {
        var sourceMember = await InviteContact(EmployeeType.User);
        var targetMember = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [sourceMember.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [targetMember.Id]);

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var targetIds = targetAfter.Response.Members.Select(m => m.Id);
        targetIds.Should().Contain(targetMember.Id);
        targetIds.Should().Contain(sourceMember.Id);
    }

    [Fact]
    public async Task MoveMembersTo_OverlappingMember_DoesNotDuplicateInTarget()
    {
        var shared = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [shared.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [shared.Id]);

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        targetAfter.Response.Members.Count(m => m.Id == shared.Id).Should().Be(1);

        var sourceAfter = await _groupApi.GetGroupAsync(source.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        sourceAfter.Response.Members.Select(m => m.Id).Should().NotContain(shared.Id);
    }

    [Fact]
    public async Task MoveMembersTo_ReturnsTheUpdatedTargetGroupWithMovedMembers()
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);
        var targetName = Guid.NewGuid().ToString("N");
        var target = await CreateGroupAsync(targetName);

        var result = await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        result.Response.Id.Should().Be(target.Response.Id);
        result.Response.Name.Should().Be(targetName);
        result.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
    }

    [Fact]
    public async Task MoveMembersTo_ChangesPersistAfterRefetchingBothGroups()
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetFirst = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var targetSecond = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        targetFirst.Response.Members.Select(m => m.Id).Should().Contain(member.Id);
        targetSecond.Response.Members.Select(m => m.Id).Should().Contain(member.Id);

        var sourceFirst = await _groupApi.GetGroupAsync(source.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var sourceSecond = await _groupApi.GetGroupAsync(source.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        sourceFirst.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
        sourceSecond.Response.Members.Select(m => m.Id).Should().NotContain(member.Id);
    }

    [Fact]
    public async Task MoveMembersTo_FromEmptySourceGroup_LeavesTargetUnchanged()
    {
        var targetMember = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [targetMember.Id]);

        var targetBefore = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var idsBefore = targetBefore.Response.Members.Select(m => m.Id).OrderBy(x => x).ToList();
        var countBefore = targetBefore.Response.MembersCount;

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var idsAfter = targetAfter.Response.Members.Select(m => m.Id).OrderBy(x => x).ToList();

        idsAfter.Should().Equal(idsBefore);
        targetAfter.Response.MembersCount.Should().Be(countBefore);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81497")]
    public async Task MoveMembersTo_MemberCountsUpdateCorrectlyInBothGroups()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [m1.Id, m2.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        var targetCountBefore = target.Response.MembersCount;

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var sourceAfter = await _groupApi.GetGroupAsync(source.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        sourceAfter.Response.MembersCount.Should().Be(0);
        targetAfter.Response.MembersCount.Should().Be(targetCountBefore + 2);
    }

    [Fact]
    public async Task MoveMembersTo_RepeatedWithSameFromAndTo_IsIdempotent()
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(Guid.NewGuid().ToString("N"), [member.Id]);
        var target = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);
        await _groupApi.MoveMembersToAsync(source.Response.Id, target.Response.Id, TestContext.Current.CancellationToken);

        var targetAfter = await _groupApi.GetGroupAsync(target.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        targetAfter.Response.Members.Count(m => m.Id == member.Id).Should().Be(1);
    }
}
