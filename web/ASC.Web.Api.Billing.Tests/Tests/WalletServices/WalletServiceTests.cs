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
/// One wallet service of the catalogue (<c>GET payment/walletservice</c>), read by a DocSpace administrator.
/// </summary>
public class WalletServiceTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Theory]
    [InlineData(TenantWalletService.AITools, "ai-tools")]
    [InlineData(TenantWalletService.Backup, "backup")]
    [InlineData(TenantWalletService.Storage, "disk-storage")]
    [InlineData(TenantWalletService.AISearch, "ai-search")]
    public async Task GetWalletService_Owner_ReturnsTheServiceWithItsPrice(TenantWalletService id, string serviceName)
    {
        // Act
        var service = await _paymentApi.GetWalletServiceAsync(id, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response.Id.Should().Be((int)id);
        service.Response.ServiceName.Should().Be(serviceName);
        service.Response.Price.Value.Should().Be(WalletCatalogue.Prices[serviceName]);
        service.Response.Price.IsoCurrencySymbol.Should().Be("USD");
        service.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(TenantWalletService.AITools, "ai-tools")]
    [InlineData(TenantWalletService.AISearch, "ai-search")]
    public async Task GetWalletService_DocSpaceAdmin_ReturnsTheService(TenantWalletService id, string serviceName)
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var service = await _paymentApi.GetWalletServiceAsync(id, TestContext.Current.CancellationToken);

        // Assert
        service.StatusCode.Should().Be(200);
        service.Response.Id.Should().Be((int)id);
        service.Response.ServiceName.Should().Be(serviceName);
    }
}
