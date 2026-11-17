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


namespace ASC.Web.Api.Billing.Tests.Tests.Wallet;

/// <summary>
/// Topping up the wallet (<c>POST payment/deposit</c>) when the outcome of the charge is unknown: billing fails or
/// answers something that cannot be read. The charge may have gone through, so the answer must not be the
/// <c>false</c> of a declined charge, which a client is free to retry.
/// </summary>
/// <remarks>Suspected defect W13 of the billing spec: the stack answers <c>false</c> in both cases.</remarks>
public class TopUpDepositOutcomeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    public async Task TopUpDeposit_BillingFailsTheCharge_IsAnErrorNotADecline()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Billing, "POST", "/billing/Deposit", """{"title":"Internal Server Error","status":500}""", HttpStatusCode.InternalServerError);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken));

        // Assert
        PaymentCalls.Count(call => call.Route == "/billing/Deposit").Should().Be(1);
        exception.Should().BeOfType<ApiException>("a charge whose outcome is unknown is not reported as declined");
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task TopUpDeposit_BillingAnswerUnreadable_IsAnErrorNotADecline()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Billing, "POST", "/billing/Deposit", "{not json");

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken));

        // Assert
        PaymentCalls.Count(call => call.Route == "/billing/Deposit").Should().Be(1);
        exception.Should().BeOfType<ApiException>("a charge whose outcome is unknown is not reported as declined");
    }
}
