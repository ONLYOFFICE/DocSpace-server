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
/// The balance top-ups of <see cref="TariffService"/> over a faked accounting service: the lookup of one top-up by
/// its ID, which walks the pages of a period, the single page of the list, which swallows the failures, and the
/// top-up of the wallet, which must not deposit twice when it cannot tell whether a deposit was applied, and stops at
/// a caller's deadline.
/// </summary>
/// <remarks>
/// The portal key comes from a real <see cref="CoreSettings"/> over a tenant service that knows no tenants, and every
/// other dependency of <see cref="TariffService"/> is unused on these paths and passed as null.
/// </remarks>
public class TariffServiceDepositTests
{
    private const int TenantId = 1;
    private const string PaymentRegion = "portal-";
    private const string GoodwillPath = $"/api/customer/{PaymentRegion}1/goodwill";

    private static readonly DateTime _utcStartDate = new(2021, 3, 9, 17, 46, 59, DateTimeKind.Utc);
    private static readonly DateTime _utcEndDate = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindCustomerDeposit_IdOnALaterPage_WalksThePeriodUntilItTurnsUp()
    {
        var requests = new List<Uri>();
        var (tariffService, _) = CreateService(request =>
        {
            requests.Add(request.Uri);
            var offset = int.Parse(ParseQuery(request.Uri)["offset"]);

            // Asked for 1000, the service returns at most 100 per page.
            return AccountingClientDepositTests.DepositsPage(Enumerable.Range(offset + 1, Math.Min(100, 250 - offset)), totalQuantity: 250);
        });

        var deposit = await tariffService.FindCustomerDepositAsync(TenantId, 150, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        deposit.Should().NotBeNull();
        deposit!.Id.Should().Be(150);
        deposit.PaymentRef.Should().Be("pi_150");

        // The walk stops on the page that holds the ID and never reads the third one.
        requests.Select(uri => ParseQuery(uri)["offset"]).Should().Equal("0", "100");
        requests.Should().AllSatisfy(uri =>
        {
            uri.AbsolutePath.Should().Be($"/api/customer/{PaymentRegion}{TenantId}/deposits");

            var query = ParseQuery(uri);
            query["startDate"].Should().Be("2021-03-09T17:46:59.0000000Z");
            query["endDate"].Should().Be("2026-10-26T12:00:00.0000000Z");
            query["limit"].Should().Be("1000");
        });
    }

    [Fact]
    public async Task FindCustomerDeposit_UnknownId_ReturnsNullAfterTheLastPage()
    {
        var (tariffService, handler) = CreateService(request =>
        {
            var offset = int.Parse(ParseQuery(request.Uri)["offset"]);

            return AccountingClientDepositTests.DepositsPage(Enumerable.Range(offset + 1, Math.Min(100, 150 - offset)), totalQuantity: 150);
        });

        var deposit = await tariffService.FindCustomerDepositAsync(TenantId, 9999, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        deposit.Should().BeNull();
        handler.CallCount.Should().Be(2);
    }

    // The invoice endpoint answers 404 for a portal the accounting service does not know, and must not take any other
    // failure for a missing top-up: both reach it as exceptions.
    [Fact]
    public async Task FindCustomerDeposit_NoAccountingCustomer_ThrowsCustomerNotFound()
    {
        var (tariffService, _) = CreateService(_ => Reply(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () => await tariffService.FindCustomerDepositAsync(TenantId, 150, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
    }

    [Fact]
    public async Task FindCustomerDeposit_AccountingFailure_IsNotSwallowed()
    {
        var (tariffService, _) = CreateService(_ => Reply(HttpStatusCode.BadRequest, "The limit is out of range"));

        var act = async () => await tariffService.FindCustomerDepositAsync(TenantId, 150, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        await act.Should().ThrowExactlyAsync<AccountingException>();
    }

    [Fact]
    public async Task GetCustomerDeposits_ReadsThePageOfThePortal()
    {
        var (tariffService, handler) = CreateService(_ => AccountingClientDepositTests.DepositsPage([7, 8], totalQuantity: 2));

        var report = await tariffService.GetCustomerDepositsAsync(TenantId, new DepositFilter { Offset = 0, Limit = 25 });

        report.Collection.Select(d => d.Id).Should().Equal(7, 8);
        handler.LastUri!.AbsolutePath.Should().Be($"/api/customer/{PaymentRegion}{TenantId}/deposits");
    }

    // The list answers an empty result rather than an error, both for a portal without a wallet and for a failed read.
    [Theory]
    [InlineData("Customer not found")]
    [InlineData("The limit is out of range")]
    public async Task GetCustomerDeposits_AccountingError_ReturnsNull(string body)
    {
        var (tariffService, handler) = CreateService(_ => Reply(HttpStatusCode.BadRequest, body));

        var report = await tariffService.GetCustomerDepositsAsync(TenantId, new DepositFilter { Offset = 0, Limit = 25 });

        report.Should().BeNull();
        // The null comes from the answer, not from a failure before the request: TariffService turns either into null.
        // A 400 that names no missing customer is asked again, so only the route of every request is pinned.
        handler.Requests.Should().NotBeEmpty().And.OnlyContain(request => request.Path == $"/api/customer/{PaymentRegion}{TenantId}/deposits");
    }

    [Fact]
    public async Task EnsureWalletBalance_DepositAnswerUnreadable_StopsAfterThatDeposit()
    {
        // A proxy's page in place of billing's "ok" says nothing about the deposit: it may have been applied. Another
        // deposit for the same shortfall would then charge the customer twice, so the top-up stops instead.
        using var billing = new HttpStub(_ => Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));
        var (billingClient, _) = CreateBillingClient(billing);
        // the portal has no bonus, so the wallet alone decides
        var (accountingClient, _) = CreateAccountingClient(request => request.Path == GoodwillPath
            ? Reply(HttpStatusCode.NotFound, """{"title":"Not found","status":404}""")
            : Reply(HttpStatusCode.OK, """{"accountNumber":42,"accountCurrency":"USD","subAccounts":[{"currency":"USD","amount":10}]}"""));

        var tariffService = PaymentStubs.CreateTariffService(accountingClient, billingClient,
            hybridCache: new FusionCache(new FusionCacheOptions()),
            coreSettings: CreateCoreSettings(PaymentRegion),
            distributedLockProvider: new InMemoryLockProvider());

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeFalse("the balance never reached the required amount");
        billing.Requests.Where(request => request.Path == "/billing/Deposit").Should().ContainSingle("one deposit of unknown outcome is the last");
    }

    // The deadline of a renewal's claim has passed before the top-up begins: no deposit is sent.
    [Fact]
    public async Task EnsureWalletBalance_TokenCancelledBeforeTheTopUp_SendsNoDeposit()
    {
        var (tariffService, billing, socket) = CreateWalletService(balance: 10);

        using var deadline = new CancellationTokenSource();
        await deadline.CancelAsync();

        var act = async () => await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: deadline.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        billing.Requests.Should().NotContain(request => request.Path == "/billing/Deposit", "no deposit is begun after the deadline");
        socket.TopUps.Should().BeEmpty("there is no deposit to announce");
    }

    // The deposit is accepted and the balance does not move, so the top-up polls it - 16 reads a second apart, at
    // least 15 s. The deadline falls on the poll's first read: the top-up stops there, after the one deposit, without
    // waiting the poll out - and the clients are still told of the deposit the customer was charged for.
    [Fact]
    public async Task EnsureWalletBalance_TokenCancelledDuringThePoll_StopsAfterTheOneDeposit()
    {
        using var deadline = new CancellationTokenSource();
        var (tariffService, billing, socket) = CreateWalletService(balance: 10, onReadAfterDeposit: deadline.Cancel);

        var started = Stopwatch.StartNew();

        var act = async () => await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: deadline.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        billing.Requests.Where(request => request.Path == "/billing/Deposit").Should().ContainSingle("the deposit sent before the deadline is the only one");
        started.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(12), "the poll of 16 reads a second apart is cut short, not waited out");
        socket.TopUps.Should().Equal([true], "the automatic deposit is announced to the clients before the top-up stops");
    }

    // The deadline falls as billing accepts the deposit, so the balance is not polled at all; the deposit is still
    // announced and no second one is sent.
    [Fact]
    public async Task EnsureWalletBalance_TokenCancelledAtTheDeposit_SkipsThePoll()
    {
        using var deadline = new CancellationTokenSource();
        var readsAfterDeposit = 0;
        var (tariffService, billing, socket) = CreateWalletService(balance: 10, onDeposit: deadline.Cancel, onReadAfterDeposit: () => readsAfterDeposit++);

        var act = async () => await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: deadline.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        billing.Requests.Where(request => request.Path == "/billing/Deposit").Should().ContainSingle("the deposit sent before the deadline is the only one");
        readsAfterDeposit.Should().Be(0, "a cancelled top-up reads the balance no more");
        socket.TopUps.Should().Equal([true], "the automatic deposit is announced to the clients before the top-up stops");
    }

    // A deposit that covers the amount does not turn a cancelled top-up into a covered one: the balance is not read
    // again, so the method cannot know it, and the caller that set the deadline learns of the cancellation. The caller
    // then has to ask billing and the balance itself, as a resumed wallet renewal does.
    [Fact]
    public async Task EnsureWalletBalance_TokenCancelledAtADepositThatCovers_StillThrows()
    {
        using var deadline = new CancellationTokenSource();
        var readsAfterDeposit = 0;
        var (tariffService, billing, socket) = CreateWalletService(balance: 10, onDeposit: deadline.Cancel, balanceAfterDeposit: 60,
            onReadAfterDeposit: () => readsAfterDeposit++);

        var act = async () => await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: deadline.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        billing.Requests.Where(request => request.Path == "/billing/Deposit").Should().ContainSingle("the one deposit covers the amount");
        readsAfterDeposit.Should().Be(0, "a cancelled top-up reads the balance no more, also after a deposit that covers it");
        socket.TopUps.Should().Equal([true], "the deposit is announced to the clients before the top-up stops");
    }

    // The socket event after a deposit is best effort: a socket client that throws does not fail the top-up, whose
    // deposit is made and whose balance now covers the cost.
    [Fact]
    public async Task EnsureWalletBalance_TopUpEventFails_StillReportsTheBalanceCovered()
    {
        var (tariffService, billing, socket) = CreateWalletService(balance: 10, balanceAfterDeposit: 60, socketFails: true);

        var covered = await tariffService.EnsureWalletBalanceAsync(TenantId, requiredAmount: 50, "USD", null, "portal.example.com", auto: true, cancellationToken: TestContext.Current.CancellationToken);

        covered.Should().BeTrue("the deposit reached the wallet");
        billing.Requests.Where(request => request.Path == "/billing/Deposit").Should().ContainSingle();
        socket.TopUps.Should().Equal([true], "the event was attempted");
    }

    // A wallet that holds the given amount - and once a deposit is accepted, balanceAfterDeposit if given - with billing
    // accepting every deposit (onDeposit runs as it does), the real balance poll of the accounting client
    // (onReadAfterDeposit runs on each read after a deposit), and the top-up events the clients would receive, or a
    // socket client that throws on them.
    private static (TariffService tariffService, HttpStub billing, RecordingQuotaSocketManager socket) CreateWalletService(decimal balance,
        Action? onDeposit = null, decimal? balanceAfterDeposit = null, bool socketFails = false, Action? onReadAfterDeposit = null)
    {
        var deposited = false;

        var billing = new HttpStub(request =>
        {
            if (request.Path == "/billing/Deposit")
            {
                deposited = true;
                onDeposit?.Invoke();
            }

            return Reply(HttpStatusCode.OK, "\"ok\"");
        });

        var accounting = new HttpStub(request =>
        {
            if (deposited)
            {
                onReadAfterDeposit?.Invoke();
            }

            // the portal has no bonus, so the wallet alone decides
            if (request.Path == GoodwillPath)
            {
                return Reply(HttpStatusCode.NotFound, """{"title":"Not found","status":404}""");
            }

            var amount = deposited && balanceAfterDeposit is { } after ? after : balance;

            return Reply(HttpStatusCode.OK,
                $$"""{"accountNumber":42,"accountCurrency":"USD","subAccounts":[{"currency":"USD","amount":{{amount.ToString(CultureInfo.InvariantCulture)}}}]}""");
        });

        var provider = BuildProvider(billing: billing, accounting: accounting);

        var socket = new RecordingQuotaSocketManager(socketFails);
        var services = new ServiceCollection().AddSingleton<QuotaSocketManager>(socket).BuildServiceProvider();

        var tariffService = PaymentStubs.CreateTariffService(provider.GetRequiredService<AccountingClient>(), provider.GetRequiredService<BillingClient>(),
            hybridCache: new FusionCache(new FusionCacheOptions()),
            coreSettings: CreateCoreSettings(PaymentRegion),
            distributedLockProvider: new InMemoryLockProvider(),
            serviceProvider: services,
            resiliencePipelineProvider: provider.GetRequiredService<ResiliencePipelineProvider<string>>());

        return (tariffService, billing, socket);
    }

    // The top-up events of a portal, recorded instead of sent, then failed when asked to; nothing else of the socket
    // client is used.
    private sealed class RecordingQuotaSocketManager(bool fails) : QuotaSocketManager(null!, null!, null!, null!, null!)
    {
        public List<bool> TopUps { get; } = [];

        public override Task TopUpWallet(bool auto)
        {
            TopUps.Add(auto);

            return fails ? Task.FromException(new HttpRequestException("socket service unreachable")) : Task.CompletedTask;
        }
    }

    private static (TariffService tariffService, HttpStub handler) CreateService(
        Func<StubRequest, StubResponse> responder)
    {
        var (accountingClient, handler) = CreateAccountingClient(responder);

        var tariffService = PaymentStubs.CreateTariffService(accountingClient, coreSettings: CreateCoreSettings(PaymentRegion));

        return (tariffService, handler);
    }
}
