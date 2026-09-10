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
/// POST /api/2.0/group/{id}/members - validation, negative cases and permissions for replacing
/// the whole member list.
/// </summary>
public class GroupPermissionSetMembersTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and negative cases

    [Fact]
    public async Task SetMembersTo_NonExistingGroupId_NotFound()
    {
        var member = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(Guid.NewGuid(), new MembersRequest([member.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetMembersTo_InvalidGroupIdFormat_NotFound()
    {
        var member = await InviteContact(EmployeeType.User);

        // A Guid-typed id parameter cannot carry "not-a-uuid" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Post, "api/2.0/group/not-a-uuid/members",
            $$"""{"members":["{{member.Id}}"]}""");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMembersTo_NonExistingUserIdInMembers_BadRequest()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(created.Id, new MembersRequest([Guid.NewGuid()]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetMembersTo_InvalidUserIdFormatInMembers_BadRequest()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        // MembersRequest.Members is a List<Guid> and cannot carry "not-a-uuid" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Post, $"api/2.0/group/{created.Id}/members",
            """{"members":["not-a-uuid"]}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetMembersTo_EmptyMembersArray_BadRequest()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(created.Id, new MembersRequest([]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetMembersTo_EmptyMembersRequestBody_BadRequest()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        // MembersRequest always serialises its "members" key when built through the typed SDK
        // (EmitDefaultValue=true); an entirely absent key needs raw HTTP.
        using var response = await SendRawGroupRequest(HttpMethod.Post, $"api/2.0/group/{created.Id}/members", "{}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetMembersTo_MembersNull_BadRequest()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(created.Id, new MembersRequest(members: null!), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetMembersTo_MembersUndefined_BadRequest()
    {
        var member = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id]);

        // Same wire body as the "empty membersRequest body" case above: an omitted "members" key
        // cannot be produced by the typed SDK, which always emits it. Kept as a separate test to
        // mirror the source suite, which also collapses "{}" and "{ members: undefined }" to the
        // same JSON on the wire.
        using var response = await SendRawGroupRequest(HttpMethod.Post, $"api/2.0/group/{created.Id}/members", "{}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task SetMembersTo_DocSpaceAdmin_Replaced()
    {
        var oldMember = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [oldMember.Id]);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var updated = (await _groupApi.SetMembersToAsync(created.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken)).Response;

        updated.Members.Should().Contain(m => m.Id == newMember.Id);
        updated.Members.Should().NotContain(m => m.Id == oldMember.Id);
    }

    [Fact]
    public async Task SetMembersTo_GroupManagerWhoIsRegularUser_Forbidden()
    {
        var manager = await InviteContact(EmployeeType.User);
        var oldMember = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), manager.Id, [oldMember.Id]);

        await _peopleClient.Authenticate(manager);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(created.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetMembersTo_Anonymous_Unauthorized()
    {
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(created.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetMembersTo_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var newMember = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var invitee = await InviteMember(employeeType);
        await _peopleClient.Authenticate(invitee);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(created.Id, new MembersRequest([newMember.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
