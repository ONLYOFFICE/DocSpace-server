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
/// GET /api/2.0/group/{id} - negative scenarios and permissions.
/// </summary>
public class GroupPermissionGetGroupTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Negative scenarios

    [Fact]
    public async Task GetGroup_NonExistingId_NotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupAsync(Guid.NewGuid(), cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetGroup_InvalidIdFormat_NotFound()
    {
        // A Guid-typed id parameter cannot carry "invalid-id-format" - only raw HTTP can.
        using var response = await SendRawGroupRequest(HttpMethod.Get, "api/2.0/group/invalid-id-format");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task GetGroup_Anonymous_Unauthorized()
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetGroup_AdminRoles_Ok(EmployeeType employeeType)
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var admin = await InviteMember(employeeType);
        await _peopleClient.Authenticate(admin);

        var group = (await _groupApi.GetGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        group.Id.Should().Be(created.Id);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetGroup_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.GetGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
