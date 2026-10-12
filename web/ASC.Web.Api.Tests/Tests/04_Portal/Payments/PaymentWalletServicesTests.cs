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
/// GET /api/2.0/portal/payment/walletservices — the full wallet service catalogue. Requires
/// EditPortalSettings and reads local <c>TenantQuota</c> definitions only, so it is unaffected by
/// the unconfigured billing service.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentWalletServicesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Commented out for now: the wallet-service catalog (6 services with fixed prices) is SaaS
    // seed data this standalone-style integration host does not have — the endpoint answers 200
    // with an empty list here. Re-enable on a SaaS-seeded environment.
    /*
    [Fact]
    public async Task GetWalletServices_Owner_ReturnsAllServices()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var services = await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken);

        // Assert
        services.StatusCode.Should().Be(200);
        services.Response.Should().HaveCount(6);

        var serviceNames = services.Response!.Select(s => s.ServiceName).ToList();
        serviceNames.Should().Contain([
            "disk-storage-1-hour",
            "backup",
            "ai-tools",
            "docscloud-1-hour",
            "docscloud-devpack-1-hour",
            "ai-search"
        ]);

        var expectedPrices = new Dictionary<string, double>
        {
            ["disk-storage-1-hour"] = 0.14,
            ["backup"] = 10,
            ["ai-tools"] = 0,
            ["docscloud-1-hour"] = 8,
            ["docscloud-devpack-1-hour"] = 12,
            ["ai-search"] = 0
        };

        foreach (var service in services.Response!)
        {
            service.Id.Should().NotBe(0);
            service.Price!.Value.Should().Be(expectedPrices[service.ServiceName!]);
            service.Price.IsoCurrencySymbol.Should().Be("USD");
            service.Features.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetWalletServices_DocSpaceAdmin_ReturnsAllServices()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var services = await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken);

        // Assert
        services.StatusCode.Should().Be(200);
        services.Response.Should().HaveCount(6);
    }
    */

    [Fact]
    public async Task GetWalletServices_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetWalletServices_RoomAdmin_ThrowsAccessDenied()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetWalletServices_User_ThrowsAccessDenied()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetWalletServices_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
