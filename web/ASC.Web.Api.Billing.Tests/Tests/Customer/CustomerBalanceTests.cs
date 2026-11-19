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


namespace ASC.Web.Api.Billing.Tests.Tests.Customer;

/// <summary>
/// The portal's wallet balance (<c>GET payment/customer/balance</c>): read from accounting for a billing customer,
/// under the portal's own payment key.
/// </summary>
public class CustomerBalanceTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetCustomerBalance_Customer_ReturnsTheWalletAsAccountingHoldsIt()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(150.25m);

        // Act
        var balance = (await _paymentApi.GetCustomerBalanceAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        balance.AccountCurrency.Should().Be("USD");
        var subAccount = balance.SubAccounts.Should().ContainSingle().Subject;
        subAccount.Currency.Should().Be("USD");
        subAccount.Amount.Should().Be(150.25);
        PaymentCalls.Should().Contain(call => call.Service == PaymentService.Accounting && call.Route == "/customer/{portalId}/balance");
    }

    // The accounting service may send its times without an offset, or with an offset of its own; the answer gives the
    // last credit the portal's offset and keeps the moment. The test portal is in UTC, so an offset taken for the
    // host's zone shows wherever the host is not in UTC.
    [Theory]
    [InlineData("2026-11-01T09:00:00")]
    [InlineData("2026-11-01T14:00:00+05:00")]
    public async Task GetCustomerBalance_LastCreditWithoutAnOffsetOrWithAnother_KeepsItsMomentWithAnOffset(string date)
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .On(PaymentService.Accounting, "GET", "/customer/{portalId}/balance", $$"""
                {
                  "accountCurrency": "USD",
                  "subAccounts": [{"currency": "USD", "amount": 150.25}],
                  "lastCredit": {"amount": 100, "currency": "USD", "date": "{{date}}"}
                }
                """);

        // Act
        var balance = await RawResponseAsync(HttpMethod.Get, "api/2.0/portal/payment/customer/balance?refresh=true");

        // Assert
        ShouldCarryAnOffset(balance.GetProperty("lastCredit").GetProperty("date"), new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetCustomerBalance_PortalIsNotACustomer_ReturnsNothingWithoutAskingAccounting()
    {
        // Act
        var balance = (await _paymentApi.GetCustomerBalanceAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        balance.Should().BeNull();
        PaymentCalls.Should().NotContain(call => call.Service == PaymentService.Accounting && call.Route == "/customer/{portalId}/balance");
    }
}
