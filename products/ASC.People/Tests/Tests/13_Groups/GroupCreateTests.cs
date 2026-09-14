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
/// POST /api/2.0/group - creating a new group.
/// </summary>
public class GroupCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task AddGroup_WithRequiredFieldsOnly_ReturnsGroupWithGivenNameAndManager()
    {
        var groupName = Guid.NewGuid().ToString("N");

        var result = await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: groupName, groupManager: Owner.Id),
            TestContext.Current.CancellationToken);

        result.Response.Id.Should().NotBeEmpty();
        result.Response.Name.Should().Be(groupName);
        result.Response.Manager.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task AddGroup_WithMembers_MembersArePresentInTheGroup()
    {
        var member1 = await InviteContact(EmployeeType.User);
        var member2 = await InviteContact(EmployeeType.User);

        var created = await _groupApi.AddGroupAsync(
            new GroupRequestDto(
                groupName: Guid.NewGuid().ToString("N"),
                groupManager: Owner.Id,
                members: [member1.Id, member2.Id]),
            TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(member1.Id);
        memberIds.Should().Contain(member2.Id);
    }

    [Fact]
    public async Task AddGroup_WithEmptyMembersList_ManagerIsAddedAsTheOnlyMember()
    {
        var created = await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: Guid.NewGuid().ToString("N"), groupManager: Owner.Id, members: []),
            TestContext.Current.CancellationToken);

        created.Response.Id.Should().NotBeEmpty();
        created.Response.MembersCount.Should().Be(1);
    }

    [Fact]
    public async Task AddGroup_WithOneMember_MemberIsPresentInTheGroup()
    {
        var member = await InviteContact(EmployeeType.User);

        var created = await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: Guid.NewGuid().ToString("N"), groupManager: Owner.Id, members: [member.Id]),
            TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        group.Response.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task AddGroup_WithMultipleMembers_AllMembersArePresentInTheGroup()
    {
        var member1 = await InviteContact(EmployeeType.User);
        var member2 = await InviteContact(EmployeeType.User);
        var member3 = await InviteContact(EmployeeType.User);

        var created = await _groupApi.AddGroupAsync(
            new GroupRequestDto(
                groupName: Guid.NewGuid().ToString("N"),
                groupManager: Owner.Id,
                members: [member1.Id, member2.Id, member3.Id]),
            TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);

        var memberIds = group.Response.Members.Select(m => m.Id);
        memberIds.Should().Contain(member1.Id);
        memberIds.Should().Contain(member2.Id);
        memberIds.Should().Contain(member3.Id);
    }
}
