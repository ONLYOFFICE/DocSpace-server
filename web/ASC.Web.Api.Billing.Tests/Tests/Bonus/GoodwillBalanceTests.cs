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

namespace ASC.Web.Api.Billing.Tests.Tests.Bonus;

/// <summary>
/// The portal's bonus balance (<c>GET payment/customer/goodwill</c>): read from accounting for a billing customer on
/// every request, with the bonus credits that can still be spent.
/// </summary>
public class GoodwillBalanceTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string GoodwillRoute = "/customer/{portalId}/goodwill";

    [Fact]
    public async Task GetCustomerGoodwill_Customer_ReturnsTheBonusAccountWithItsCredits()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", GoodwillRoute, $$"""
                {
                  "accountNumber": {{Payments.AccountNumber}},
                  "subAccountNumber": 53,
                  "accountName": "goodwill",
                  "accountCurrency": "USD",
                  "subAccounts": [{"currency": "USD", "amount": 35.5}],
                  "lastCredit": {"amount": 50, "currency": "USD", "date": "2026-11-01T09:00:00Z"},
                  "lots": [
                    {"operationId": 987654, "amount": 50, "remaining": 35.5, "currency": "USD", "date": "2026-11-01T09:00:00Z", "expiresAt": "2026-12-01T09:00:00Z"},
                    {"operationId": 987655, "amount": 10, "remaining": 10, "currency": "USD", "date": "2026-11-02T09:00:00Z", "expiresAt": null}
                  ]
                }
                """);

        // Act
        var goodwill = (await _paymentApi.GetCustomerGoodwillAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        goodwill.AccountNumber.Should().Be(Payments.AccountNumber);
        goodwill.SubAccounts.Should().ContainSingle().Which.Amount.Should().Be(35.5);
        goodwill.Lots.Should().HaveCount(2);

        var first = goodwill.Lots[0];
        first.OperationId.Should().Be(987654);
        first.Amount.Should().Be(50);
        first.Remaining.Should().Be(35.5);
        first.ExpiresAt!.Value.ToUniversalTime().Should().Be(new DateTime(2026, 12, 1, 9, 0, 0, DateTimeKind.Utc));

        goodwill.Lots[1].ExpiresAt.Should().BeNull("a bonus that never expires has no expiry");
    }

    [Fact]
    public async Task GetCustomerGoodwill_PortalIsNotACustomer_ReturnsNothingWithoutAskingAccounting()
    {
        // Act
        var goodwill = (await _paymentApi.GetCustomerGoodwillAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        goodwill.Should().BeNull();
        PaymentCalls.Should().NotContain(call => call.Route == GoodwillRoute);
    }
}
