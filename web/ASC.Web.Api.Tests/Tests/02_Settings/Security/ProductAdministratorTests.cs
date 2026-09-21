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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Security;

/// <summary>
/// GET/PUT /api/2.0/settings/security/administrator[/{productid}] — product administrators.
/// Product ID <c>00000000-0000-0000-0000-000000000000</c> stands for the portal-wide
/// (DocSpaceAdmin) group, which the owner belongs to by default. Only a portal owner may
/// promote a member to DocSpaceAdmin or demote another DocSpaceAdmin; every other role is
/// denied by <c>EditPortalSettings</c> before it can even reach that finer check.
/// </summary>
[Trait("Category", "Settings")]
public class ProductAdministratorTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static readonly Guid _productIdAll = Guid.Empty;

    [Fact]
    public async Task GetProductAdministrators_Owner_IncludesOwnerByDefault()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var admins = await _securityApi.GetProductAdministratorsAsync(_productIdAll, TestContext.Current.CancellationToken);

        // Assert
        admins.StatusCode.Should().Be(200);
        admins.Response.Select(e => e.Id).Should().Equal(Owner.Id);
    }

    [Fact]
    public async Task GetProductAdministrators_GrowsAfterPromotingDocSpaceAdmin()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var promoted = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _securityApi.SetProductAdministratorAsync(
            new SecurityRequestsDto(_productIdAll, promoted.Id, true), TestContext.Current.CancellationToken);

        // Act
        var admins = await _securityApi.GetProductAdministratorsAsync(_productIdAll, TestContext.Current.CancellationToken);

        // Assert
        admins.StatusCode.Should().Be(200);
        admins.Response.Select(e => e.Id).Should().Contain(promoted.Id);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetProductAdministrators_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetProductAdministratorsAsync(_productIdAll, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetIsProductAdministrator_Owner_IsAdministratorByDefault()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _securityApi.GetIsProductAdministratorAsync(
            _productIdAll, Owner.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Administrator.Should().BeTrue();
    }

    [Fact]
    public async Task GetIsProductAdministrator_PlainUser_IsNotAdministrator()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);

        // Act
        var result = await _securityApi.GetIsProductAdministratorAsync(
            _productIdAll, user.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Administrator.Should().BeFalse();
    }

    [Fact]
    public async Task GetIsProductAdministrator_PromotedDocSpaceAdmin_IsConfirmed()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var promoted = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _securityApi.SetProductAdministratorAsync(
            new SecurityRequestsDto(_productIdAll, promoted.Id, true), TestContext.Current.CancellationToken);

        // Act
        var result = await _securityApi.GetIsProductAdministratorAsync(
            _productIdAll, promoted.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Administrator.Should().BeTrue();
    }

    [Fact]
    public async Task GetIsProductAdministrator_User_ThrowsAccessDenied()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetIsProductAdministratorAsync(
                _productIdAll, Guid.NewGuid(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    // BUG 80586: a DocSpaceAdmin can demote another DocSpaceAdmin through this endpoint, even
    // though WebItemSecurity.SetProductAdministrator only allows that when the caller is the
    // tenant owner - the controller's DemandPermissionsAsync(EditPortalSettings) check passes
    // for any DocSpaceAdmin, and the finer owner-only check is meant to reject it afterwards.
    [Trait("Bug", "80586")]
    [Fact]
    public async Task SetProductAdministrator_DocSpaceAdmin_CannotDemoteAnotherDocSpaceAdmin()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var otherAdmin = await InviteMember(EmployeeType.DocSpaceAdmin);

        var admin1 = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin1);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.SetProductAdministratorAsync(
                new SecurityRequestsDto(_productIdAll, otherAdmin.Id, false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SetProductAdministrator_DocSpaceAdmin_CannotPromoteRoomAdminToDocSpaceAdmin()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.SetProductAdministratorAsync(
                new SecurityRequestsDto(_productIdAll, roomAdmin.Id, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
