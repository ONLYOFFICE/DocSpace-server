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
/// Topping up the wallet (<c>POST payment/deposit</c>): billing charges the payment method on file and accounting
/// credits the wallet, which the stack waits to see before it answers.
/// </summary>
public class TopUpDepositTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task TopUpDeposit_Payer_ChargesThenWaitsForTheWallet()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act
        var result = await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();

        var calls = PaymentCalls.ToList();
        var depositAt = calls.FindIndex(call => call.Route == "/billing/Deposit");
        depositAt.Should().BeGreaterThanOrEqualTo(0, "the payer's payment method is charged through billing");
        calls.Count(call => call.Route == "/billing/Deposit").Should().Be(1);

        var deposit = calls[depositAt];
        BillingValue(deposit, "Amount").Should().Be("100");
        BillingValue(deposit, "Currency").Should().Be("USD");
        BillingValue(deposit, "CustomerParticipantName").Should().Be(Owner.Id.ToString());

        // The stack answers only once accounting shows the money: it reads the balance again after the charge.
        calls.Skip(depositAt + 1).Should().Contain(call => call.Route == "/customer/{portalId}/balance",
            "a confirmed charge is waited for until the wallet moves");
    }

    [Fact]
    public async Task TopUpDeposit_BillingDeclinesTheCharge_ReturnsFalseWithoutWaitingForTheWallet()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Billing, "POST", "/billing/Deposit", "\"declined\"");

        // Act
        var result = await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();

        var calls = PaymentCalls.ToList();
        var depositAt = calls.FindIndex(call => call.Route == "/billing/Deposit");
        depositAt.Should().BeGreaterThanOrEqualTo(0);
        calls.Count(call => call.Route == "/billing/Deposit").Should().Be(1, "a declined charge is reported, not tried again");
        calls.Skip(depositAt + 1).Should().NotContain(call => call.Route == "/customer/{portalId}/balance",
            "nothing is waited for once billing declines the charge");
    }

    [Fact]
    public async Task TopUpDeposit_NoPaymentMethod_ThrowsForbidden()
    {
        // Arrange
        Payments.Customer(Owner.Email, paymentMethodStatus: 0).Balance(10m);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("payment method is not set");
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/Deposit");
    }

    [Fact]
    public async Task TopUpDeposit_NotThePayer_ThrowsForbidden()
    {
        // Arrange
        Payments.Customer("the-payer@example.com").Balance(10m);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/Deposit");
    }

    [Fact]
    public async Task TopUpDeposit_CurrencyAccountingDoesNotUse_ThrowsBadRequest()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act: XXX is the ISO code for "no currency", which no accounting configuration lists.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "XXX"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Unsupported currency");
    }

    [Fact]
    public async Task TopUpDeposit_PortalIsNotACustomer_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Customer could not be found");
    }
}
