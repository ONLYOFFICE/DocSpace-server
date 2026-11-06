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
/// Buying a plan (<c>PUT payment/url</c>): the checkout address billing gives as a template for every portal, filled
/// in with this portal and the request.
/// </summary>
public class PaymentUrlTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string BackUrl = "https://portal.example.com/back";
    private const string SuccessUrl = "https://portal.example.com/success";

    [Fact]
    public async Task GetPaymentUrl_FreePortal_ReturnsTheCheckoutFilledInForThePortal()
    {
        // Act
        var url = (await _paymentApi.GetPaymentUrlAsync(
            new PaymentUrlRequestDto(BackUrl, SuccessUrl, new Dictionary<string, int> { [Plans.AdminPlanName] = 3 }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Should().StartWith(PaymentServiceStub.CheckoutUrl + "?");

        var query = HttpUtility.ParseQueryString(new Uri(url).Query);
        query["products"].Should().Be(Plans.AdminProductId.ToString(CultureInfo.InvariantCulture));
        query["portal"].Should().Be(Payments.PortalId);
        query["quantity"].Should().Be("3");
        // The back address is where a cancelled checkout returns, the success one where a paid one lands.
        query["back"].Should().Be(BackUrl);
        query["success"].Should().Be(SuccessUrl);
    }

    [Fact]
    public async Task GetPaymentUrl_CustomerWhoseTariffIsPaid_ReturnsNothing()
    {
        // Arrange: the free tariff a portal without payments has counts as paid as well.
        Payments.Customer(Owner.Email);

        // Act
        var url = (await _paymentApi.GetPaymentUrlAsync(
            new PaymentUrlRequestDto(BackUrl, SuccessUrl, new Dictionary<string, int> { [Plans.AdminPlanName] = 3 }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Should().BeNull("a customer whose tariff is paid changes the quantity of its plan instead of buying one");
    }

    [Fact]
    public async Task GetPaymentUrl_CustomerWhoseTariffHasRunOut_ReturnsTheCheckout()
    {
        // Arrange
        Payments.Customer(Owner.Email).Paid(Plans.AdminProductId, quantity: 3, DateTime.UtcNow.Date.AddMonths(-2));

        // The checkout reads the cached tariff, so it is re-read once the plan has run out, as a client does.
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;
        tariff.State.Should().NotBe(TariffState.Paid, "the plan ended two months ago");

        // Act
        var url = (await _paymentApi.GetPaymentUrlAsync(
            new PaymentUrlRequestDto(BackUrl, SuccessUrl, new Dictionary<string, int> { [Plans.AdminPlanName] = 3 }),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Should().StartWith(PaymentServiceStub.CheckoutUrl + "?");
        HttpUtility.ParseQueryString(new Uri(url).Query)["portal"].Should().Be(Payments.PortalId);
    }

    [Fact]
    public async Task GetPaymentUrl_YearlyProduct_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(
                new PaymentUrlRequestDto(BackUrl, SuccessUrl, new Dictionary<string, int> { ["adminyear"] = 1 }),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Only monthly product");
    }
}
