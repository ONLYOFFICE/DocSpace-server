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
/// DELETE /api/2.0/group/{id}/members - validation, negative cases and permissions.
/// </summary>
public class GroupPermissionRemoveMembersTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and negative cases

    [Fact]
    public async Task RemoveMembersFrom_NonExistingGroupId_NotFound()
    {
        var member = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.RemoveMembersFromAsync(Guid.NewGuid(), new MembersRequest([member.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RemoveMembersFrom_InvalidGroupIdFormat_NotFound()
    {
        // A Guid-typed id parameter cannot carry "not-a-valid-id" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Delete, "api/2.0/group/not-a-valid-id/members",
            """{"members":[]}""");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMembersFrom_NonExistingUserInMembers_SilentlyIgnored()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([Guid.NewGuid()]), TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_InvalidUserIdFormatInMembers_SilentlyIgnored()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        // MembersRequest.Members is a List<Guid> and cannot carry "not-a-valid-id" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Delete, $"api/2.0/group/{created.Id}/members",
            """{"members":["not-a-valid-id"]}""");
        ((int)response.StatusCode).Should().BeLessThan(500);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_NonMemberUser_NoOp()
    {
        var member = await InviteContact(EmployeeType.User);
        var outsider = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([outsider.Id]), TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
        group.Members.Should().NotContain(m => m.Id == outsider.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_EmptyMembersArray_Unchanged()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([]), TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_MissingMembersRequest_RejectedBySdk()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.RemoveMembersFromAsync(created.Id, null!, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81509")]
    public async Task RemoveMembersFrom_NullMembers_Unchanged()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest(members: null!), TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81510")]
    public async Task RemoveMembersFrom_UndefinedMembers_Unchanged()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        // MembersRequest always serialises its "members" key when set through the typed SDK
        // (EmitDefaultValue=true); the property has to be omitted from the body entirely to
        // reproduce the TS suite's "undefined" case, which only raw HTTP can do.
        using var response = await SendRawGroupRequest(HttpMethod.Delete, $"api/2.0/group/{created.Id}/members", "{}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_DuplicateUserIds_Idempotent()
    {
        var member = await InviteContact(EmployeeType.User);
        var keep = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id, keep.Id]);

        await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([member.Id, member.Id, member.Id]), TestContext.Current.CancellationToken);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.Members.Should().NotContain(m => m.Id == member.Id);
        group.Members.Should().Contain(m => m.Id == keep.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_MixedValidAndInvalidUserIds_Unchanged()
    {
        var member = await InviteContact(EmployeeType.User);
        var keep = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id, keep.Id]);

        // A List<Guid> cannot carry "not-a-valid-id" alongside two well-formed values - raw HTTP only.
        using var response = await SendRawGroupRequest(HttpMethod.Delete, $"api/2.0/group/{created.Id}/members",
            $$"""{"members":["{{member.Id}}","{{Guid.NewGuid()}}","not-a-valid-id"]}""");
        ((int)response.StatusCode).Should().BeLessThan(500);

        var group = await GetGroupWithMembersAsync(created.Id);

        group.MembersCount.Should().Be(created.MembersCount);
        group.Members.Should().Contain(m => m.Id == member.Id);
        group.Members.Should().Contain(m => m.Id == keep.Id);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task RemoveMembersFrom_DocSpaceAdmin_Removed()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var updated = (await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken)).Response;

        updated.Members.Should().NotContain(m => m.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMembersFrom_GroupManagerWhoIsRegularUser_Forbidden()
    {
        var manager = await InviteContact(EmployeeType.User);
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), manager.Id, [member.Id]);

        await _peopleClient.Authenticate(manager);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RemoveMembersFrom_Anonymous_Unauthorized()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task RemoveMembersFrom_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        var invitee = await InviteMember(employeeType);
        await _peopleClient.Authenticate(invitee);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.RemoveMembersFromAsync(created.Id, new MembersRequest([member.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
