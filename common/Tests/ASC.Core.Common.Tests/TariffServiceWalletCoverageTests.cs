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
/// payment method, and a bonus that cannot be read before the first top-up counts as none.
/// </summary>
/// <remarks>
/// In the services <c>CreateService</c> builds, a deposit is answered with a page that is not billing's "ok", so the
/// top-up stops after the first deposit and its amount can be read from the request without waiting for the balance to
/// change. <c>CreateConfirmingService</c> confirms deposits through the real balance poll, for the tests of what the
/// loop does after a top-up.
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

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 30, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeTrue();
        billing.Requests.Should().NotContain(request => request.Path == "/billing/Deposit");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusCoversPartOfThePrice_TopsUpOnlyTheRest()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Goodwill(15));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("25");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusInAnotherCurrency_IsNotCounted()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Goodwill(15, "EUR"));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("40");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusCannotBeRead_TopsUpAsIfThereWereNone()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10,
            goodwill: Reply(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Something went wrong"}"""));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("40");
    }

    [Fact]
    public async Task EnsureWalletBalance_BonusAnswerUnreadable_TopsUpAsIfThereWereNone()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("40");
    }

    // A credit that has ended is not spent by accounting, even while it still sits on the sub-account.
    [Fact]
    public async Task EnsureWalletBalance_BonusEndedAnHourAgo_TopsUpAsIfThereWereNone()
    {
        var ended = DateTime.UtcNow.AddHours(-1).ToString("o", CultureInfo.InvariantCulture);
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Reply(HttpStatusCode.OK, $$"""
            {"accountNumber":42,"subAccountNumber":53,"accountName":"goodwill","accountCurrency":"USD",
             "subAccounts":[{"currency":"USD","amount":15}],
             "lots":[{"operationId":1,"amount":15,"remaining":15,"currency":"USD","date":"2026-11-01T09:00:00Z","expiresAt":"{{ended}}"}]}
            """));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("40");
    }

    // A wallet never topped up has no sub-account, also with a bonus credited: it is an empty wallet, not a missing one.
    [Fact]
    public async Task EnsureWalletBalance_NoWalletSubAccountAndTheBonusCovers_TopsUpNothing()
    {
        var (tariffService, billing) = CreateService(NeverToppedUpWalletJson, Goodwill(50));

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 30, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeTrue();
        billing.Requests.Should().NotContain(request => request.Path == "/billing/Deposit");
    }

    [Fact]
    public async Task EnsureWalletBalance_NoWalletSubAccountAndNoBonus_TopsUpTheWholePrice()
    {
        var (tariffService, billing) = CreateService(NeverToppedUpWalletJson,
            Reply(HttpStatusCode.OK, """{"accountNumber":42,"subAccountNumber":53,"accountCurrency":"USD","subAccounts":[],"lots":[]}"""));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 30, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("30");
    }

    [Fact]
    public async Task EnsureWalletBalance_NegativeBonus_CountsAsNone()
    {
        var (tariffService, billing) = CreateService(walletAmount: 10, goodwill: Goodwill(-20));

        await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        DepositAmounts(billing).Should().Equal("40");
    }

    // Charges running meanwhile spend the bonus before the wallet, so a bonus read before the top-up can be gone after
    // it: the wallet is topped up again for what the bonus no longer covers.
    [Fact]
    public async Task EnsureWalletBalance_BonusSpentDuringTheTopUp_TopsUpAgainForTheRest()
    {
        var (tariffService, billing) = CreateConfirmingService(Goodwill(15), Goodwill(5));

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 40, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeTrue();
        DepositAmounts(billing).Should().Equal("25", "10");
    }

    // A bonus that cannot be read after a confirmed deposit is not taken for none: that would fire a second deposit for
    // money the first one already covered.
    [Fact]
    public async Task EnsureWalletBalance_BonusUnreadableAfterTheTopUp_KeepsTheBonusReadBefore()
    {
        var (tariffService, billing) = CreateConfirmingService(Goodwill(15),
            Reply(HttpStatusCode.InternalServerError, """{"title":"Internal Server Error","status":500}"""));

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 40, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeTrue();
        DepositAmounts(billing).Should().Equal("25");
    }

    [Fact]
    public async Task EnsureWalletBalance_NoTopUpAllowed_CountsTheBonus()
    {
        // A delayed payment method is never topped up on the fly: what the portal already has decides alone.
        var (tariffService, billing) = CreateService(walletAmount: 5, goodwill: Goodwill(25));

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 30, "USD", null, "portal.example.com", auto: false, allowTopUp: false, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeTrue();
        billing.Requests.Should().NotContain(request => request.Path == "/billing/Deposit");
    }

    private static StubResponse Goodwill(decimal amount, string currency = "USD")
    {
        return Reply(HttpStatusCode.OK, $$"""
            {"accountNumber":42,"subAccountNumber":53,"accountName":"goodwill","accountCurrency":"{{currency}}",
             "subAccounts":[{"currency":"{{currency}}","amount":{{amount.ToString(CultureInfo.InvariantCulture)}}}],
             "lots":[{"operationId":1,"amount":{{amount.ToString(CultureInfo.InvariantCulture)}},"remaining":{{amount.ToString(CultureInfo.InvariantCulture)}},"currency":"{{currency}}","date":"2026-11-01T09:00:00Z","expiresAt":null}]}
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
        return CreateService(WalletJson(walletAmount), goodwill);
    }

    private static (TariffService tariffService, HttpStub billing) CreateService(string walletJson, StubResponse goodwill)
    {
        var billing = new HttpStub(_ => Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));
        var (billingClient, _) = CreateBillingClient(billing);

        var (accountingClient, _) = CreateAccountingClient(request => request.Path switch
        {
            BalancePath => Reply(HttpStatusCode.OK, walletJson),
            GoodwillPath => goodwill,
            _ => Reply(HttpStatusCode.NotFound, """{"title":"Not found","status":404}""")
        });

        return (CreateTariffService(accountingClient, billingClient), billing);
    }

    // A wallet that starts empty and shows every deposit billing accepts, the real balance poll of the accounting
    // client, which confirms a deposit once the wallet shows it, and the bonus answered in turn by goodwillAnswers,
    // the last one for every later read.
    private static (TariffService tariffService, HttpStub billing) CreateConfirmingService(params StubResponse[] goodwillAnswers)
    {
        var wallet = 0m;
        var goodwill = new Queue<StubResponse>(goodwillAnswers);
        var billing = new HttpStub(request =>
        {
            if (request.Path == "/billing/Deposit")
            {
                wallet += decimal.Parse(JsonDocument.Parse(request.Body!).RootElement.GetProperty("Amount")[0].GetString()!, CultureInfo.InvariantCulture);
            }

            return Reply(HttpStatusCode.OK, "\"ok\"");
        });
        var accounting = new HttpStub(request => request.Path switch
        {
            BalancePath => Reply(HttpStatusCode.OK, WalletJson(wallet)),
            GoodwillPath => goodwill.Count > 1 ? goodwill.Dequeue() : goodwill.Peek(),
            _ => Reply(HttpStatusCode.NotFound, """{"title":"Not found","status":404}""")
        });

        var provider = BuildProvider(billing: billing, accounting: accounting);
        var tariffService = CreateTariffService(provider.GetRequiredService<AccountingClient>(), provider.GetRequiredService<BillingClient>(),
            provider.GetRequiredService<ResiliencePipelineProvider<string>>());

        return (tariffService, billing);
    }

    private static TariffService CreateTariffService(AccountingClient accountingClient, BillingClient billingClient,
        ResiliencePipelineProvider<string>? resiliencePipelineProvider = null)
    {
        return PaymentStubs.CreateTariffService(accountingClient, billingClient,
            hybridCache: new FusionCache(new FusionCacheOptions()),
            coreSettings: CreateCoreSettings(PaymentRegion),
            distributedLockProvider: new InMemoryLockProvider(),
            resiliencePipelineProvider: resiliencePipelineProvider);
    }

    // What the accounting service answers for a customer with a linked card who has never topped up the wallet.
    private const string NeverToppedUpWalletJson = """{"accountNumber":42,"subAccountNumber":0,"accountCurrency":"USD","subAccounts":[]}""";

    private static string WalletJson(decimal amount)
    {
        return $$"""{"accountNumber":42,"accountCurrency":"USD","subAccounts":[{"currency":"USD","amount":{{amount.ToString(CultureInfo.InvariantCulture)}}}]}""";
    }
}
