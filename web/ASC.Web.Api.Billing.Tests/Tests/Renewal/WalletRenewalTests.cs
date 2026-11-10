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


namespace ASC.Web.Api.Billing.Tests.Tests.Renewal;

/// <summary>
/// The renewal of a subscription paid from the wallet, which Web.Studio (<c>RenewSubscriptionService</c>) makes by itself
/// shortly before the due date: the wallet is topped up for what it lacks, then billing is asked for the next period.
/// </summary>
public class WalletRenewalTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // the subscription ends a few renewal periods from now, and is renewed one period before it ends
    private static readonly TimeSpan _dueIn = Constants.PaymentStubRenewPeriod * 4;

    // the due date, the run that picks it up, the renewal itself, and room for a busy machine
    private static readonly TimeSpan _renewalDeadline = _dueIn + Constants.PaymentStubRenewPeriod * 8;

    [Fact]
    public async Task Renewal_WalletCoversTheCost_PaysTheNextPeriod()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .Balance(100m)
            .Paid(Plans.AdminWalletProductId, quantity: 1, DateTime.UtcNow + _dueIn, Owner.Email)
            .On(PaymentService.Billing, "POST", "/billing/ChangeSubscription", "true");

        // the stack learns the due date from billing and stores it, which is what the renewal service looks for
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var calls = await WaitForRenewalAsync();

        // Assert
        var change = calls.Find(call => call.Route == "/billing/ChangeSubscription");
        change.Should().NotBeNull("the subscription is renewed before its due date");
        BillingValue(change!, "ProductId").Should().Be(Plans.AdminWalletProductId.ToString(CultureInfo.InvariantCulture));
        BillingValue(change!, "ProductQty").Should().Be("1");
        BillingValue(change!, "ProductQuantityType").Should().Be("3", "an unchanged subscription is renewed, not set anew");

        calls.Should().NotContain(call => call.Route == "/billing/Deposit", "the wallet already holds the price");
    }

    [Fact]
    public async Task Renewal_WalletShortOfTheCost_TopsUpTheShortfallThenPays()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .Balance(5m)
            .Paid(Plans.AdminWalletProductId, quantity: 1, DateTime.UtcNow + _dueIn, Owner.Email)
            .On(PaymentService.Billing, "POST", "/billing/ChangeSubscription", "true");

        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var calls = await WaitForRenewalAsync();

        // Assert
        var changeAt = calls.FindIndex(call => call.Route == "/billing/ChangeSubscription");
        changeAt.Should().BeGreaterThanOrEqualTo(0, "the subscription is renewed before its due date");

        var deposits = calls.Where(call => call.Route == "/billing/Deposit").ToList();
        deposits.Should().ContainSingle("the shortfall is topped up once");
        decimal.Parse(BillingValue(deposits[0], "Amount")!, CultureInfo.InvariantCulture).Should().Be(Plans.AdminWalletPrice - 5m);
        calls.IndexOf(deposits[0]).Should().BeLessThan(changeAt, "the wallet is topped up before the period is paid from it");
    }

    private async Task<List<PaymentServiceCall>> WaitForRenewalAsync()
    {
        var deadline = DateTime.UtcNow + _renewalDeadline;

        while (true)
        {
            var calls = PaymentCalls.ToList();

            if (calls.Exists(call => call.Route == "/billing/ChangeSubscription") || DateTime.UtcNow >= deadline)
            {
                return calls;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }
}
