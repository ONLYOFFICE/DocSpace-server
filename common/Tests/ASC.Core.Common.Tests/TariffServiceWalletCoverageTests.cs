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

namespace ASC.Core.Common.Tests;

/// <summary>
/// What <see cref="TariffService.EnsureWalletBalanceAsync"/> counts as money the portal can pay with: the wallet and
/// the bonus credits, which the accounting service spends first. Only the shortfall of both is topped up from the
/// payment method, and a bonus that cannot be read counts as none.
/// </summary>
/// <remarks>
/// A deposit is answered with a page that is not billing's "ok", so the top-up stops after the first deposit and its
/// amount can be read from the request without waiting for the balance to change.
/// </remarks>
public class TariffServiceWalletCoverageTests
{
    private const int TenantId = 1;
    private const string PaymentRegion = "portal-";
    private const string BalancePath = $"/api/customer/{PaymentRegion}1/balance";
    private const string GoodwillPath = $"/api/customer/{PaymentRegion}1/goodwill";

    [Fact]
    public async Task EnsureWalletBalance_BonusCoversThePrice_TopsUpNothing()
    {
        var (tariffService, billing) = CreateService(walletAmount: 0, goodwill: Goodwill(50));

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 30, "USD", null, "portal.example.com", auto: true);

        covered.Should().BeTrue();
        billing.Requests.Should().NotContain(request => request.Path == "/billing/Deposit");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusCoversPartOfThePrice_TopsUpOnlyTheRest()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Goodwill(15));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true);

        DepositAmounts(billing).Should().Equal("25");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusInAnotherCurrency_IsNotCounted()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Goodwill(15, "EUR"));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true);

        DepositAmounts(billing).Should().Equal("40");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusCannotBeRead_TopsUpAsIfThereWereNone()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10,
            goodwill: Reply(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Something went wrong"}"""));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true);

        DepositAmounts(billing).Should().Equal("40");
    }

    [Fact]
    public async Task EnsureWalletBalance_NoTopUpAllowed_CountsTheBonus()
    {
        // A delayed payment method is never topped up on the fly: what the portal already has decides alone.
        var (tariffService, billing) = CreateService(walletAmount: 5, goodwill: Goodwill(25));

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 30, "USD", null, "portal.example.com", auto: false, allowTopUp: false);

        covered.Should().BeTrue();
        billing.Requests.Should().NotContain(request => request.Path == "/billing/Deposit");
    }

    private static StubResponse Goodwill(decimal amount, string currency = "USD")
    {
        return Reply(HttpStatusCode.OK, $$"""
            {"accountNumber":42,"subAccountNumber":53,"accountName":"goodwill","accountCurrency":"{{currency}}",
             "subAccounts":[{"currency":"{{currency}}","amount":{{amount.ToString(CultureInfo.InvariantCulture)}}}],"lots":[]}
            """);
    }

    private static List<string> DepositAmounts(HttpStub billing)
    {
        return billing.Requests
            .Where(request => request.Path == "/billing/Deposit")
            .Select(request => JsonDocument.Parse(request.Body!).RootElement.GetProperty("Amount")[0].GetString()!)
            .ToList();
    }

    private static (TariffService tariffService, HttpStub billing) CreateService(decimal walletAmount, StubResponse goodwill)
    {
        var billing = new HttpStub(_ => Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));
        var (billingClient, _) = CreateBillingClient(billing);

        var wallet = $$"""{"accountNumber":42,"accountCurrency":"USD","subAccounts":[{"currency":"USD","amount":{{walletAmount.ToString(CultureInfo.InvariantCulture)}}}]}""";
        var (accountingClient, _) = CreateAccountingClient(request => request.Path switch
        {
            BalancePath => Reply(HttpStatusCode.OK, wallet),
            GoodwillPath => goodwill,
            _ => Reply(HttpStatusCode.NotFound, """{"title":"Not found","status":404}""")
        });

        var tariffService = PaymentStubs.CreateTariffService(accountingClient, billingClient,
            hybridCache: new FusionCache(new FusionCacheOptions()),
            coreSettings: CreateCoreSettings(PaymentRegion),
            distributedLockProvider: new InMemoryLockProvider());

        return (tariffService, billing);
    }
}
