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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Users;

/// <summary>
/// GET /api/2.0/portal/userscount — the total number of portal users, across every employee type.
/// </summary>
[Trait("Category", "Portal")]
public class PortalUsersCountTests(
    AspireAppFixture fixture)
    : UsersTestBase(fixture)
{
    private async Task SeedOneOfEveryRoleAsync()
    {
        await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteMember(EmployeeType.RoomAdmin);
        await InviteMember(EmployeeType.User);
        await InviteMember(EmployeeType.Guest);
    }

    [Fact]
    public async Task GetPortalUsersCount_Owner_CountsAllRoles()
    {
        // Arrange
        await SeedOneOfEveryRoleAsync();
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _portalUsersApi.GetPortalUsersCountAsync(TestContext.Current.CancellationToken);

        // Assert — Owner + the 4 seeded members.
        result.Response.Should().Be(5);
    }

    [Fact]
    public async Task GetPortalUsersCount_DocSpaceAdmin_CountsAllRoles()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteMember(EmployeeType.RoomAdmin);
        await InviteMember(EmployeeType.User);
        await InviteMember(EmployeeType.Guest);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _portalUsersApi.GetPortalUsersCountAsync(TestContext.Current.CancellationToken);

        // Assert — Owner + the admin itself + the 3 other seeded members.
        result.Response.Should().Be(5);
    }

    [Fact]
    public async Task GetPortalUsersCount_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetPortalUsersCountAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetPortalUsersCount_ByRole_ReturnsAccessDenied(EmployeeType actingRole)
    {
        // Arrange
        await SeedOneOfEveryRoleAsync();
        await ActAsAsync(actingRole);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _portalUsersApi.GetPortalUsersCountAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
