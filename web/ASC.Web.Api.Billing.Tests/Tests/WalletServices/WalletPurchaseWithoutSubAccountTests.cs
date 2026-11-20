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
/// The check before a purchase from the wallet (<c>PUT payment/calculatewallet</c>, and <c>updatewallet</c> through
/// the same check) for a customer who has linked a card but never topped up the wallet: the accounting service gives
/// such a wallet no sub-account, also with a bonus credited. A bonus is enough to go on, and so is a bonus that could
/// not be read; only a portal known to have neither gets 404.
/// </summary>
public class WalletPurchaseWithoutSubAccountTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string BalanceRoute = "/customer/{portalId}/balance";
    private const string GoodwillRoute = "/customer/{portalId}/goodwill";

    [Fact]
    public async Task CalculateWalletPayment_NoWalletSubAccountButABonus_IsNotRefused()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", BalanceRoute, NeverToppedUpWallet())
            .On(PaymentService.Accounting, "GET", GoodwillRoute, Goodwill(5m));

        // Act
        await _paymentApi.CalculateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), TestContext.Current.CancellationToken);

        // Assert: the bonus was asked for, and the call went on to the price instead of answering 404
        PaymentCalls.Should().Contain(call => call.Route == GoodwillRoute);
        PaymentCalls.Should().Contain(call => call.Route == "/billing/CalculateSubscription");
    }

    // An unknown answer is not a refusal: the call goes on to the price, and billing decides.
    [Fact]
    public async Task CalculateWalletPayment_NoWalletSubAccountAndTheBonusCannotBeRead_IsNotRefused()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", BalanceRoute, NeverToppedUpWallet())
            .On(PaymentService.Accounting, "GET", GoodwillRoute, """{"title":"Internal Server Error","status":500}""", HttpStatusCode.InternalServerError);

        // Act
        await _paymentApi.CalculateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), TestContext.Current.CancellationToken);

        // Assert
        PaymentCalls.Should().Contain(call => call.Route == GoodwillRoute);
        PaymentCalls.Should().Contain(call => call.Route == "/billing/CalculateSubscription");
    }

    [Fact]
    public async Task CalculateWalletPayment_NoWalletSubAccountAndNoBonus_ThrowsNotFound()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", BalanceRoute, NeverToppedUpWallet())
            .On(PaymentService.Accounting, "GET", GoodwillRoute, Goodwill(0m, withLot: false));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _paymentApi.CalculateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Subaccount could not be found");
        PaymentCalls.Should().NotContain(call => call.Route == "/billing/CalculateSubscription");
    }

    // What the accounting service answers for a customer with a linked card who has never topped up the wallet.
    private string NeverToppedUpWallet()
    {
        return new JsonObject
        {
            ["accountNumber"] = Payments.AccountNumber,
            ["subAccountNumber"] = 0,
            ["accountCurrency"] = "USD",
            ["subAccounts"] = new JsonArray()
        }.ToJsonString();
    }

    private string Goodwill(decimal amount, bool withLot = true)
    {
        return new JsonObject
        {
            ["accountNumber"] = Payments.AccountNumber,
            ["subAccountNumber"] = 53,
            ["accountCurrency"] = "USD",
            ["subAccounts"] = withLot ? new JsonArray(new JsonObject { ["currency"] = "USD", ["amount"] = amount }) : new JsonArray(),
            ["lots"] = withLot
                ? new JsonArray(new JsonObject
                {
                    ["operationId"] = 1, ["amount"] = amount, ["remaining"] = amount, ["currency"] = "USD",
                    ["date"] = "2026-11-19T13:24:31Z", ["expiresAt"] = null
                })
                : new JsonArray()
        }.ToJsonString();
    }
}
