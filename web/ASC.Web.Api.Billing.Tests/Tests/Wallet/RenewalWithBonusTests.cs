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
/// The automatic renewal of a wallet subscription (the renewal job of Web.Studio) with a bonus on the portal: the
/// wallet is topped up only for the part of the price that the wallet money and the bonus do not cover.
/// </summary>
public class RenewalWithBonusTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // The administrators wallet service of the SaaS catalogue: product 1013, 20 USD each.
    private const int AdminWalletProductId = 1013;
    private const int Admins = 2;

    // The job runs every minute and renews what falls due before its next run. A due date further than that run
    // cannot have been passed over by a run made before this test, so the next runs renew it.
    private static readonly TimeSpan _dueIn = TimeSpan.FromSeconds(100);
    private static readonly TimeSpan _renewalDeadline = TimeSpan.FromMinutes(4);

    [Theory]
    [InlineData(15, 25)]
    [InlineData(40, 0)]
    public async Task Renewal_BonusOnThePortal_TopsUpOnlyWhatTheBonusDoesNotCover(double bonus, double expectedTopUp)
    {
        // Arrange: the administrators bought from the wallet fall due shortly, the wallet is empty, the bonus is not.
        Payments.Paid(AdminWalletProductId, Admins, DateTime.UtcNow + _dueIn, Owner.Email)
            .Customer(Owner.Email)
            .Balance(0m)
            .On(PaymentService.Billing, "POST", "/billing/ChangeSubscription", "true")
            .On(PaymentService.Accounting, "GET", "/customer/{portalId}/goodwill", new JsonObject
            {
                ["accountNumber"] = Payments.AccountNumber,
                ["subAccountNumber"] = 53,
                ["accountName"] = "goodwill",
                ["accountCurrency"] = "USD",
                ["subAccounts"] = new JsonArray(new JsonObject { ["currency"] = "USD", ["amount"] = (decimal)bonus }),
                ["lots"] = new JsonArray(new JsonObject
                {
                    ["operationId"] = 1, ["amount"] = (decimal)bonus, ["remaining"] = (decimal)bonus, ["currency"] = "USD",
                    ["date"] = "2026-11-01T09:00:00Z", ["expiresAt"] = null
                })
            }.ToJsonString());

        // reading the tariff stores the due date the renewal job looks for
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var renewal = await WaitForRenewalAsync();

        // Assert
        renewal.Should().NotBeNull("the renewal job buys the administrators again when they fall due");

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
    }

    private async Task<PaymentServiceCall?> WaitForRenewalAsync()
    {
        var deadline = DateTime.UtcNow + _renewalDeadline;

        while (true)
        {
            var renewal = PaymentCalls.FirstOrDefault(c => c.Route == "/billing/ChangeSubscription");

            if (renewal is not null || DateTime.UtcNow >= deadline)
            {
                return renewal;
            }

            await Task.Delay(2000, TestContext.Current.CancellationToken);
        }
    }
}
