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


namespace ASC.Web.Api.Billing.Tests.Tests.WalletServices;

/// <summary>
/// The whole wallet service catalogue (<c>GET payment/walletservices</c>), read by a DocSpace administrator.
/// </summary>
public class WalletServicesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetWalletServices_Owner_ReturnsAllServices()
    {
        // Act
        var services = await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken);

        // Assert
        services.StatusCode.Should().Be(200);
        services.Response.Select(s => s.ServiceName).Should().BeEquivalentTo(WalletCatalogue.Prices.Keys);

        foreach (var service in services.Response)
        {
            service.Id.Should().NotBe(0);
            service.Price.Value.Should().Be(WalletCatalogue.Prices[service.ServiceName], service.ServiceName);
            service.Price.IsoCurrencySymbol.Should().Be("USD");
            service.Features.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetWalletServices_DocSpaceAdmin_ReturnsAllServices()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var services = await _paymentApi.GetWalletServicesAsync(TestContext.Current.CancellationToken);

        // Assert
        services.StatusCode.Should().Be(200);
        services.Response.Select(s => s.ServiceName).Should().BeEquivalentTo(WalletCatalogue.Prices.Keys);
    }
}
