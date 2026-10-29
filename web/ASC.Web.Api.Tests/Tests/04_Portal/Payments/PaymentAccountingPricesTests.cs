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
/// GET /api/2.0/portal/payment/accounting/prices/{serviceName} — the price list of a wallet service as the
/// accounting service holds it. The action demands a configured billing service before it checks who is
/// calling, so this environment answers 403 "Tariff service is not configured" to every authenticated caller;
/// what stays observable is the authentication guard (401) and the route validation that runs before the
/// action (<c>serviceName</c> over 255 characters is 400).
/// </summary>
/// <remarks>
/// Not ported, because they need a real accounting backend: the price-list shape, the <c>active</c> filter,
/// the comparison with <c>walletservices</c>, the <c>servicePrice = costPrice * (1 + extraCharge / 100)</c>
/// rule and the Owner/DocSpaceAdmin parity. The TypeScript case for a non-boolean <c>active</c> checks
/// ASP.NET query binding and the typed SDK cannot send it, so it is left out.
/// </remarks>
[Trait("Category", "Portal")]
public class PaymentAccountingPricesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetAccountingServicePrices_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetAccountingServicePricesAsync("ai-tools", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetAccountingServicePrices_ServiceNameOver255_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetAccountingServicePricesAsync(new string('a', 256), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("255");
    }

    [Fact]
    public async Task GetAccountingServicePrices_ServiceNameOf255_PassesValidation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetAccountingServicePricesAsync(new string('a', 255), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        // The name is accepted and the request reaches the configuration check, which this environment
        // without a billing service refuses.
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAccountingServicePrices_Owner_WithoutBillingService_ThrowsForbidden()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetAccountingServicePricesAsync("ai-tools", true, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
