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
/// PUT /api/2.0/group/{id}/manager - setting a group's manager.
/// </summary>
public class GroupManagerTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private Task<GroupWrapper> CreateGroupAsync(string name, Guid? manager = null, List<Guid>? members = null)
    {
        return _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: manager ?? Owner.Id, members: members!),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SetGroupManager_NewManager_IsAppliedToTheGroup()
    {
        var newManager = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var result = await _groupApi.SetGroupManagerAsync(
            created.Response.Id,
            new SetManagerRequest(newManager.Id),
            TestContext.Current.CancellationToken);

        result.Response.Manager.Id.Should().Be(newManager.Id);
    }

    [Fact]
    public async Task SetGroupManager_ReplacesTheExistingManager()
    {
        var managerA = await InviteContact(EmployeeType.User);
        var managerB = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), managerA.Id);

        var result = await _groupApi.SetGroupManagerAsync(
            created.Response.Id,
            new SetManagerRequest(managerB.Id),
            TestContext.Current.CancellationToken);

        result.Response.Manager.Id.Should().Be(managerB.Id);
        result.Response.Manager.Id.Should().NotBe(managerA.Id);
    }

    [Fact]
    public async Task SetGroupManager_DoesNotChangeTheGroupName()
    {
        var newManager = await InviteContact(EmployeeType.User);
        var originalName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(originalName);

        await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Name.Should().Be(originalName);
    }

    [Fact]
    public async Task SetGroupManager_ExistingMembersArePreservedAndTheNewManagerIsAddedAsMember()
    {
        var m1 = await InviteContact(EmployeeType.User);
        var m2 = await InviteContact(EmployeeType.User);
        var newManager = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [m1.Id, m2.Id]);

        var before = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var membersBefore = before.Response.Members.Select(m => m.Id).ToList();

        await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken);

        var after = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var membersAfter = after.Response.Members.Select(m => m.Id).ToList();

        membersAfter.Should().Contain(membersBefore);
        membersAfter.Should().Contain(newManager.Id);
    }

    [Fact]
    public async Task SetGroupManager_ToAnExistingGroupMember_Succeeds()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [member.Id]);

        var result = await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(member.Id), TestContext.Current.CancellationToken);

        result.Response.Manager.Id.Should().Be(member.Id);
    }

    [Fact]
    public async Task SetGroupManager_ToAUserWhoIsNotAGroupMember_Succeeds()
    {
        var outsider = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var result = await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(outsider.Id), TestContext.Current.CancellationToken);

        result.Response.Manager.Id.Should().Be(outsider.Id);
    }

    [Fact]
    public async Task SetGroupManager_PersistsAfterRefetchingTheGroup()
    {
        var newManager = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken);

        var group = await _groupApi.GetGroupAsync(created.Response.Id, cancellationToken: TestContext.Current.CancellationToken);
        group.Response.Manager.Id.Should().Be(newManager.Id);
    }

    [Fact]
    public async Task SetGroupManager_SettingTheCurrentManagerAgain_IsIdempotent()
    {
        var created = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var result = await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(Owner.Id), TestContext.Current.CancellationToken);

        result.Response.Manager.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task SetGroupManager_UpdatesManagerAndAddsThemAsMember_LeavesIdNameAndExistingMembersIntact()
    {
        var member = await InviteContact(EmployeeType.User);
        var newManager = await InviteContact(EmployeeType.User);
        var originalName = Guid.NewGuid().ToString("N");
        var created = await CreateGroupAsync(originalName, members: [member.Id]);

        var before = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var membersBefore = before.Response.Members.Select(m => m.Id).ToList();

        await _groupApi.SetGroupManagerAsync(created.Response.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken);

        var after = await _groupApi.GetGroupAsync(created.Response.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken);
        var membersAfter = after.Response.Members.Select(m => m.Id).ToList();

        after.Response.Id.Should().Be(created.Response.Id);
        after.Response.Name.Should().Be(originalName);
        after.Response.Manager.Id.Should().Be(newManager.Id);
        membersAfter.Should().Contain(membersBefore);
        membersAfter.Should().Contain(newManager.Id);
    }
}
