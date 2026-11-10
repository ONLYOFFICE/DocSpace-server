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
/// The quantity checks of the checkout (<c>PUT payment/url</c>) and of the change of a paid plan
/// (<c>PUT payment/update</c>) on a portal with billing.
/// </summary>
public class PaymentQuantityTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ValidUrl = "https://example.com/payment";

    [Fact]
    public async Task GetPaymentUrl_ZeroQuantity_ThrowsBadRequestWithoutAskingForACheckout()
    {
        // Arrange
        var request = new PaymentUrlRequestDto(ValidUrl, ValidUrl, new Dictionary<string, int> { [Plans.AdminPlanName] = 0 });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Invalid quantity");
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/GetSinglePaymentUrl");
    }

    [Fact]
    public async Task UpdatePayment_PortalIsNotACustomer_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdatePaymentAsync(Quantity(5), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Customer could not be found");
    }

    [Fact]
    public async Task UpdatePayment_SameQuantityAsPaid_ThrowsBadRequest()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(1), Owner.Email).Customer(Owner.Email);
        (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response.State.Should().Be(TariffState.Paid);
        var callsBefore = PaymentCalls.Count;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdatePaymentAsync(Quantity(10), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Invalid quantity");
        PaymentCalls.Skip(callsBefore).Should().NotContain(call => call.Service == PaymentService.Billing && call.Route.Contains("Update", StringComparison.OrdinalIgnoreCase),
            "a quantity already in effect is refused before billing is asked to change anything");
    }

    [Fact]
    public async Task UpdatePayment_NotThePayer_ThrowsForbidden()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(1)).Customer("the-payer@example.com");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdatePaymentAsync(Quantity(5), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
    }

    private static QuantityRequestDto Quantity(int quantity)
    {
        return new QuantityRequestDto(new Dictionary<string, int> { [Plans.AdminPlanName] = quantity });
    }
}
