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
/// PUT /api/2.0/group/{id}/manager - validation, negative cases and permissions.
/// </summary>
public class GroupPermissionSetManagerTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and negative cases

    [Fact]
    public async Task SetGroupManager_NonExistingGroupId_NotFound()
    {
        var member = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetGroupManagerAsync(Guid.NewGuid(), new SetManagerRequest(member.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetGroupManager_NonExistingUserId_NotFound()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetGroupManagerAsync(created.Id, new SetManagerRequest(Guid.NewGuid()), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetGroupManager_EmptyUserId_BadRequest()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        // SetManagerRequest.UserId is a non-nullable Guid and cannot carry "" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Put, $"api/2.0/group/{created.Id}/manager",
            """{"userId":""}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetGroupManager_NullUserId_BadRequest()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        using var response = await SendRawGroupRequest(HttpMethod.Put, $"api/2.0/group/{created.Id}/manager",
            """{"userId":null}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetGroupManager_MissingUserId_BadRequest()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        using var response = await SendRawGroupRequest(HttpMethod.Put, $"api/2.0/group/{created.Id}/manager", "{}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetGroupManager_NullRequestBody_RejectedBySdk()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetGroupManagerAsync(created.Id, null!, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task SetGroupManager_DocSpaceAdmin_Set()
    {
        var newManager = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var updated = (await _groupApi.SetGroupManagerAsync(created.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken)).Response;

        updated.Manager?.Id.Should().Be(newManager.Id);
    }

    [Fact]
    public async Task SetGroupManager_Anonymous_Unauthorized()
    {
        var newManager = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetGroupManagerAsync(created.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetGroupManager_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var newManager = await InviteContact(EmployeeType.User);
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetGroupManagerAsync(created.Id, new SetManagerRequest(newManager.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
