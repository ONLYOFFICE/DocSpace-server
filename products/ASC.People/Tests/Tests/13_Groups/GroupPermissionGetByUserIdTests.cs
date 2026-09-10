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
/// GET /api/2.0/group/user/{userid} - validation, negative cases and permissions.
/// </summary>
public class GroupPermissionGetByUserIdTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and negative cases

    [Fact]
    public async Task GetGroupByUserId_InvalidUserIdFormat_NotFound()
    {
        // A Guid-typed userid parameter cannot carry "not-a-valid-id" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Get, "api/2.0/group/user/not-a-valid-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupByUserId_NonExistingUserId_EmptyArray()
    {
        var groups = (await _groupApi.GetGroupByUserIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)).Response;

        groups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupByUserId_EmptyUserId_NotFound()
    {
        using var response = await SendRawGroupRequest(HttpMethod.Get, "api/2.0/group/user/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Permissions

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetGroupByUserId_AdminRoles_CanGetAnotherUsersGroups(EmployeeType employeeType)
    {
        var target = await InviteContact(EmployeeType.User);

        var admin = await InviteMember(employeeType);
        await _peopleClient.Authenticate(admin);

        var groups = await _groupApi.GetGroupByUserIdWithHttpInfoAsync(target.Id, TestContext.Current.CancellationToken);

        groups.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGroupByUserId_User_CannotGetOwnGroups()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupByUserIdAsync(user.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupByUserId_User_CannotGetAnotherUsersGroups()
    {
        var target = await InviteContact(EmployeeType.User);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupByUserIdAsync(target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupByUserId_Guest_CannotGetOwnGroups()
    {
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupByUserIdAsync(guest.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupByUserId_Guest_CannotGetAnotherUsersGroups()
    {
        var target = await InviteContact(EmployeeType.User);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupByUserIdAsync(target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupByUserId_Anonymous_Unauthorized()
    {
        var target = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupByUserIdAsync(target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
