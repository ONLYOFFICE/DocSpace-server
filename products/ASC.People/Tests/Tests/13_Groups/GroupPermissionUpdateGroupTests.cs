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
/// PUT /api/2.0/group/{id} - validation, negative cases and permissions.
/// </summary>
public class GroupPermissionUpdateGroupTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and negative cases

    [Fact]
    public async Task UpdateGroup_NonExistingGroupId_NotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.UpdateGroupAsync(
                Guid.NewGuid(),
                new UpdateGroupRequest(groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateGroup_NonExistingGroupManager_NotSet()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);
        var nonExistingUserId = Guid.NewGuid();

        var updated = (await _groupApi.UpdateGroupAsync(
            created.Id,
            new UpdateGroupRequest(groupManager: nonExistingUserId),
            TestContext.Current.CancellationToken)).Response;

        updated.Manager?.Id.Should().NotBe(nonExistingUserId);
    }

    [Fact]
    public async Task UpdateGroup_NonExistingUserInMembersToAdd_NotAdded()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);
        var nonExistingUserId = Guid.NewGuid();

        await _groupApi.UpdateGroupAsync(
            created.Id,
            new UpdateGroupRequest(membersToAdd: [nonExistingUserId]),
            TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.Members.Should().NotContain(m => m.Id == nonExistingUserId);
    }

    [Fact]
    public async Task UpdateGroup_RemovingNonMemberUser_Unchanged()
    {
        var nonMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _groupApi.UpdateGroupAsync(
            created.Id,
            new UpdateGroupRequest(membersToRemove: [nonMember.Id]),
            TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.Members.Should().NotContain(m => m.Id == nonMember.Id);
    }

    [Fact]
    public async Task UpdateGroup_MembersToRemoveHasPriorityOverMembersToAdd()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _groupApi.UpdateGroupAsync(
            created.Id,
            new UpdateGroupRequest(membersToAdd: [member.Id], membersToRemove: [member.Id]),
            TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.Members.Should().NotContain(m => m.Id == member.Id);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task UpdateGroup_DocSpaceAdmin_Updated()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var newName = RandomGroupName();
        var updated = (await _groupApi.UpdateGroupAsync(
            created.Id,
            new UpdateGroupRequest(groupName: newName),
            TestContext.Current.CancellationToken)).Response;

        updated.Name.Should().Be(newName);
    }

    [Fact]
    public async Task UpdateGroup_Anonymous_Unauthorized()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.UpdateGroupAsync(
                created.Id,
                new UpdateGroupRequest(groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UpdateGroup_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.UpdateGroupAsync(
                created.Id,
                new UpdateGroupRequest(groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
