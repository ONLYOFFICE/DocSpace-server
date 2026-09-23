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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Payments;

/// <summary>
/// GET /api/2.0/portal/payment/walletservice — a single wallet service's info. Requires
/// EditPortalSettings and reads local <c>TenantQuota</c> definitions only, so it is unaffected by
/// the unconfigured billing service.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentWalletServiceTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Commented out for now: the wallet-service catalog is SaaS seed data this standalone-style
    // integration host does not have — every id answers 404 "Service could not be found" here.
    // Re-enable on a SaaS-seeded environment.
    /*
    [Fact]
    public async Task GetWalletService_Owner_AITools_ReturnsFreeService()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(TenantWalletService.AITools, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response!.Id.Should().Be((int)TenantWalletService.AITools);
        service.Response.ServiceName.Should().Be("ai-tools");
        service.Response.Price!.Value.Should().Be(0);
        service.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWalletService_Owner_Backup_ReturnsPricedService()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(TenantWalletService.Backup, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response!.Id.Should().Be((int)TenantWalletService.Backup);
        service.Response.ServiceName.Should().Be("backup");
        service.Response.Price!.Value.Should().BeGreaterThan(0);
        service.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWalletService_Owner_Storage_ReturnsPricedService()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(TenantWalletService.Storage, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response!.Id.Should().Be((int)TenantWalletService.Storage);
        service.Response.ServiceName.Should().Be("disk-storage-1-hour");
        service.Response.Price!.Value.Should().BeGreaterThan(0);
        service.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWalletService_Owner_AISearch_ReturnsFreeService()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(TenantWalletService.AISearch, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response!.Id.Should().Be((int)TenantWalletService.AISearch);
        service.Response.ServiceName.Should().Be("ai-search");
        service.Response.Price!.Value.Should().Be(0);
        service.Response.Price.IsoCurrencySymbol.Should().Be("USD");
        service.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWalletService_DocSpaceAdmin_AISearch_ReturnsFreeService()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(TenantWalletService.AISearch, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response!.Id.Should().Be((int)TenantWalletService.AISearch);
        service.Response.ServiceName.Should().Be("ai-search");
    }

    [Fact]
    public async Task GetWalletService_DocSpaceAdmin_AITools_ReturnsFreeService()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(TenantWalletService.AITools, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response!.Id.Should().Be((int)TenantWalletService.AITools);
        service.Response.ServiceName.Should().Be("ai-tools");
    }
    */

    [Fact]
    public async Task GetWalletService_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServiceAsync(TenantWalletService.AITools, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetWalletService_RoomAdmin_ThrowsAccessDenied()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServiceAsync(TenantWalletService.AITools, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetWalletService_User_ThrowsAccessDenied()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServiceAsync(TenantWalletService.AITools, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetWalletService_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServiceAsync(TenantWalletService.AITools, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
