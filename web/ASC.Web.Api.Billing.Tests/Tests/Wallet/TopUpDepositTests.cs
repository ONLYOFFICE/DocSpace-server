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
    public async Task TopUpDeposit_Payer_CreditsTheWallet()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act
        var result = await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
        Payments.WalletAmount.Should().Be(110m);

        var deposit = PaymentCalls.Should().ContainSingle(call => call.Route == "/billing/Deposit").Subject;
        using var body = JsonDocument.Parse(deposit.Body!);
        body.RootElement.GetProperty("Amount")[0].GetString().Should().Be("100");
        body.RootElement.GetProperty("Currency")[0].GetString().Should().Be("USD");
        body.RootElement.GetProperty("CustomerParticipantName")[0].GetString().Should().Be(Owner.Id.ToString());

        // The stack has already read the new balance back from accounting.
        var balance = (await _paymentApi.GetCustomerBalanceAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        balance.SubAccounts.Should().ContainSingle().Which.Amount.Should().Be(110);
    }

    [Fact]
    public async Task TopUpDeposit_BillingDeclinesTheCharge_ReturnsFalseAndLeavesTheWallet()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Billing, "POST", "/billing/Deposit", "\"declined\"");

        // Act
        var result = await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();
        Payments.WalletAmount.Should().Be(10m);
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
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/Deposit");
    }

    [Fact]
    public async Task TopUpDeposit_CurrencyAccountingDoesNotUse_ThrowsBadRequest()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "EUR"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task TopUpDeposit_PortalIsNotACustomer_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
