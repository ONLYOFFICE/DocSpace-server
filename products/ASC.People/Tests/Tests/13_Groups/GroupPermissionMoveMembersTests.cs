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
/// PUT /api/2.0/group/{fromId}/members/{toId} - validation, negative cases and permissions.
/// </summary>
public class GroupPermissionMoveMembersTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and edge cases

    /// <summary>
    /// Was a Playwright <c>test.fail</c>: moving a group's members onto itself used to corrupt the
    /// group. The correct behaviour is that the call either succeeds or fails cleanly (never a 5xx)
    /// and the member is still there afterwards.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81710")]
    public async Task MoveMembersTo_FromIdEqualsToId_DoesNotCorruptMembers()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        try
        {
            await _groupApi.MoveMembersToAsync(created.Id, created.Id, TestContext.Current.CancellationToken);
        }
        catch (ApiException exception)
        {
            exception.ErrorCode.Should().BeLessThan(500);
        }

        var group = await GetGroupWithMembersAsync(created.Id);

        group.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task MoveMembersTo_NonExistingFromId_NotFoundAndTargetUnchanged()
    {
        var targetMember = await InviteContact(EmployeeType.User);
        var target = await CreateGroupAsync(RandomGroupName(), Owner.Id, [targetMember.Id]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.MoveMembersToAsync(Guid.NewGuid(), target.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);

        var targetAfter = await GetGroupWithMembersAsync(target.Id);
        targetAfter.Members.Should().Contain(m => m.Id == targetMember.Id);
    }

    [Fact]
    public async Task MoveMembersTo_NonExistingToId_NotFoundAndSourceUnchanged()
    {
        var sourceMember = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(RandomGroupName(), Owner.Id, [sourceMember.Id]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.MoveMembersToAsync(source.Id, Guid.NewGuid(), TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);

        var sourceAfter = await GetGroupWithMembersAsync(source.Id);
        sourceAfter.Members.Should().Contain(m => m.Id == sourceMember.Id);
    }

    [Fact]
    public async Task MoveMembersTo_DeletedSourceGroup_NotFound()
    {
        var source = await CreateGroupAsync(RandomGroupName(), Owner.Id);
        var target = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _groupApi.DeleteGroupAsync(source.Id, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.MoveMembersToAsync(source.Id, target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task MoveMembersTo_TargetGroupManagerIsPreserved()
    {
        var sourceManager = await InviteContact(EmployeeType.User);
        var sourceMember = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(RandomGroupName(), sourceManager.Id, [sourceMember.Id]);
        var target = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _groupApi.MoveMembersToAsync(source.Id, target.Id, TestContext.Current.CancellationToken);

        var targetAfter = await GetGroupWithMembersAsync(target.Id);

        targetAfter.Manager?.Id.Should().Be(Owner.Id);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task MoveMembersTo_DocSpaceAdmin_Moved()
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);
        var target = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        await _groupApi.MoveMembersToAsync(source.Id, target.Id, TestContext.Current.CancellationToken);

        var targetAfter = await GetGroupWithMembersAsync(target.Id);
        targetAfter.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task MoveMembersTo_Anonymous_Unauthorized()
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);
        var target = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.MoveMembersToAsync(source.Id, target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task MoveMembersTo_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var member = await InviteContact(EmployeeType.User);
        var source = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);
        var target = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var invitee = await InviteMember(employeeType);
        await _peopleClient.Authenticate(invitee);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.MoveMembersToAsync(source.Id, target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
