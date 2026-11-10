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


namespace ASC.Web.Api.Billing.Tests.Tests.Checkout;

/// <summary>
/// The unused balance of the subscription (<c>GET payment/subscription/balance</c>): refused for a plan that is not
/// in the paid state, before billing is asked about the subscription.
/// </summary>
public class SubscriptionBalanceTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetSubscriptionBalanceInfo_TariffNotPaid_ThrowsPaymentRequired()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(-1), Owner.Email).Customer(Owner.Email);
        (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response.State.Should().Be(TariffState.NotPaid);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetSubscriptionBalanceInfoAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Tariff is not paid");
    }

    [Fact]
    public async Task GetSubscriptionBalanceInfo_TariffInTheGracePeriod_ThrowsPaymentRequired()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddDays(-2), Owner.Email).Customer(Owner.Email);
        (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response.State.Should().Be(TariffState.Delay);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetSubscriptionBalanceInfoAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Tariff is not paid");
    }
}
