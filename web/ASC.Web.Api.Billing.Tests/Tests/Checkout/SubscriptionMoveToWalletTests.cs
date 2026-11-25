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
/// Moving the paid subscription to the wallet (<c>POST payment/subscription/movetowallet</c>) with a bonus on the
/// portal: the wallet is topped up only for the part of the price that the wallet money and the bonus do not cover.
/// </summary>
public class SubscriptionMoveToWalletTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // The administrators wallet service of the SaaS catalogue, 20 USD each, so two of them cost 40 USD.
    private const string AdminWalletProduct = "adminwallet";
    private const int Admins = 2;

    [Theory]
    [InlineData(0, 40)]
    [InlineData(15, 25)]
    [InlineData(40, 0)]
    public async Task MoveSubscriptionToWallet_BonusOnThePortal_TopsUpOnlyWhatTheBonusDoesNotCover(double bonus, double expectedTopUp)
    {
        // Arrange: an empty wallet, nothing left of the period to move, and a bonus in the wallet currency.
        Payments.Paid(Plans.AdminProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .Customer(Owner.Email)
            .Balance(0m)
            .On(PaymentService.Billing, "POST", "/billing/SubscriptionBalanceToWallet", """{"amount":0,"currency":"usd"}""")
            .On(PaymentService.Billing, "POST", "/billing/ChangeSubscription", "true")
            .On(PaymentService.Accounting, "GET", "/customer/by-name/{portalId}/goodwill", GoodwillJson((decimal)bonus));

        // the plan is read anew, so the move sees the paid subscription instead of the cached free plan
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var bought = (await _paymentApi.MoveSubscriptionToWalletAsync(
            new QuantityRequestDto(new Dictionary<string, int> { [AdminWalletProduct] = Admins }), TestContext.Current.CancellationToken)).Response;

        // Assert
        bought.Should().BeTrue();

        var deposits = PaymentCalls.Where(c => c.Route == "/billing/Deposit").ToList();
        if (expectedTopUp == 0)
        {
            deposits.Should().BeEmpty("the bonus covers the whole price");
        }
        else
        {
            var deposit = deposits.Should().ContainSingle().Subject;
            decimal.Parse(BillingValue(deposit, "Amount")!, CultureInfo.InvariantCulture).Should().Be((decimal)expectedTopUp);
        }

        var purchase = PaymentCalls.Should().ContainSingle(c => c.Route == "/billing/ChangeSubscription").Subject;
        BillingValue(purchase, "ProductQty").Should().Be(Admins.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MoveSubscriptionToWallet_BonusInAnotherCurrency_IsNotCounted()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .Customer(Owner.Email)
            .Balance(0m)
            .On(PaymentService.Billing, "POST", "/billing/SubscriptionBalanceToWallet", """{"amount":0,"currency":"usd"}""")
            .On(PaymentService.Billing, "POST", "/billing/ChangeSubscription", "true")
            .On(PaymentService.Accounting, "GET", "/customer/by-name/{portalId}/goodwill", GoodwillJson(40m, "EUR"));

        // the plan is read anew, so the move sees the paid subscription instead of the cached free plan
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        await _paymentApi.MoveSubscriptionToWalletAsync(
            new QuantityRequestDto(new Dictionary<string, int> { [AdminWalletProduct] = Admins }), TestContext.Current.CancellationToken);

        // Assert
        var deposit = PaymentCalls.Should().ContainSingle(c => c.Route == "/billing/Deposit").Subject;
        decimal.Parse(BillingValue(deposit, "Amount")!, CultureInfo.InvariantCulture).Should().Be(40m);
    }

    // A customer who has linked a card but never topped up the wallet has no wallet sub-account, also with a bonus
    // credited: that is an empty wallet, so the bonus pays and nothing is topped up.
    [Fact]
    public async Task MoveSubscriptionToWallet_WalletNeverToppedUpAndTheBonusCovers_BuysWithoutATopUp()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", "/customer/by-name/{portalId}/balance",
                $$"""{"accountNumber":{{Payments.AccountNumber}},"subAccountNumber":0,"accountCurrency":"USD","subAccounts":[]}""")
            .On(PaymentService.Billing, "POST", "/billing/SubscriptionBalanceToWallet", """{"amount":0,"currency":"usd"}""")
            .On(PaymentService.Billing, "POST", "/billing/ChangeSubscription", "true")
            .On(PaymentService.Accounting, "GET", "/customer/by-name/{portalId}/goodwill", GoodwillJson(40m));

        // the plan is read anew, so the move sees the paid subscription instead of the cached free plan
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var bought = (await _paymentApi.MoveSubscriptionToWalletAsync(
            new QuantityRequestDto(new Dictionary<string, int> { [AdminWalletProduct] = Admins }), TestContext.Current.CancellationToken)).Response;

        // Assert
        bought.Should().BeTrue();
        PaymentCalls.Should().NotContain(c => c.Route == "/billing/Deposit", "the bonus covers the whole price");
        PaymentCalls.Should().ContainSingle(c => c.Route == "/billing/ChangeSubscription");
    }

    private string GoodwillJson(decimal amount, string currency = "USD")
    {
        return new JsonObject
        {
            ["accountNumber"] = Payments.AccountNumber,
            ["subAccountNumber"] = 53,
            ["accountName"] = "goodwill",
            ["accountCurrency"] = currency,
            ["subAccounts"] = new JsonArray(new JsonObject { ["currency"] = currency, ["amount"] = amount }),
            ["lots"] = new JsonArray(new JsonObject
                {
                    ["operationId"] = 1, ["amount"] = amount, ["remaining"] = amount, ["currency"] = currency,
                    ["date"] = "2026-11-01T09:00:00Z", ["expiresAt"] = null
                })
        }.ToJsonString();
    }
}
