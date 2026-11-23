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
/// Whether a tariff restricts the portal, and which queue its background work takes. A grace period whose customer
/// pays by a delayed payment method (SEPA) works as a paid tariff; every other grace period and an unpaid tariff
/// restrict. The tariff is built in memory: the decision reads nothing else.
/// </summary>
public sealed class TariffRestrictionTests
{
    [Theory]
    [InlineData(TariffState.Trial, false, false, TariffState.Trial)]
    [InlineData(TariffState.Trial, true, false, TariffState.Trial)]
    [InlineData(TariffState.Paid, false, false, TariffState.Paid)]
    [InlineData(TariffState.Paid, true, false, TariffState.Paid)]
    [InlineData(TariffState.Delay, false, true, TariffState.Delay)]
    [InlineData(TariffState.Delay, true, false, TariffState.Paid)]
    [InlineData(TariffState.NotPaid, false, true, TariffState.NotPaid)]
    [InlineData(TariffState.NotPaid, true, true, TariffState.NotPaid)]
    public void IsRestricted_StateAndPaymentMethod_RestrictsUnlessPaidOrDelayedMethodInGrace(TariffState state, bool delayedPaymentMethod,
        bool restricted, TariffState laneState)
    {
        var tariff = new Tariff { State = state, DueDate = DateTime.UtcNow.Date, Quotas = [], DelayedPaymentMethod = delayedPaymentMethod };

        tariff.IsRestricted.Should().Be(restricted);
        tariff.LaneState.Should().Be(laneState);
    }
}

/// <summary>
/// The last answer of billing about a delayed payment method, which decides the grace period of a portal. A
/// <see cref="TariffService"/> over a billing service stubbed with <see cref="HttpStub"/>: every answer of billing to
/// the customer request is remembered, a failure leaves the last answer in place, and a change of the answer tells the
/// clients of the portal to read the tariff again.
/// </summary>
/// <remarks>
/// The change event is read from the channel the socket service reads, so no socket service runs. The tariff read
/// itself needs a database; <see cref="TariffRestrictionTests"/> covers the decision it takes from this answer.
/// </remarks>
public sealed class TariffServiceDelayedPaymentMethodTests : IDisposable
{
    private const int TenantId = 1;
    private const string PaymentRegion = "portal-";
    private const int GraceDays = 7;
    private const string CustomerRoute = "/billing/GetCustomerInfo";

    private readonly System.Threading.Channels.Channel<SocketData> _socketChannel = System.Threading.Channels.Channel.CreateUnbounded<SocketData>();
    private readonly FusionCache _hybridCache = new(new FusionCacheOptions());
    private readonly HttpStub _billing;
    private readonly TariffService _tariffService;
    private Func<StubResponse> _billingAnswer = Failure;

    public TariffServiceDelayedPaymentMethodTests()
    {
        _billing = new HttpStub(request => request.Path == CustomerRoute
            ? _billingAnswer()
            : Reply(HttpStatusCode.NotFound, """{"Message":"error: unexpected request"}"""));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:payment:delay"] = GraceDays.ToString(CultureInfo.InvariantCulture),
                ["web:hub:internal"] = "http://localhost:9899/",
                ["core:machinekey"] = "test-machine-key"
            })
            .Build();

        var quotaSocketManager = new QuotaSocketManager(
            StubProxy<ITariffService>.Create([]),
            null!,
            _socketChannel.Writer,
            new MachinePseudoKeys(configuration),
            configuration);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(quotaSocketManager)
            .BuildServiceProvider();

        _tariffService = CreateTariffService(
            billingClient: CreateBillingClient(_billing).client,
            hybridCache: _hybridCache,
            coreSettings: CreateCoreSettings(PaymentRegion),
            configuration: configuration,
            serviceProvider: serviceProvider,
            distributedLockProvider: new InMemoryLockProvider());
    }

    public void Dispose()
    {
        _billing.Dispose();
        _hybridCache.Dispose();
    }

    [Fact]
    public async Task IsDelayedPaymentMethodAsync_BillingAnswersDelayed_IsTrueAndTellsTheClients()
    {
        _billingAnswer = () => Customer(delayed: true);

        (await _tariffService.IsDelayedPaymentMethodAsync(TenantId)).Should().BeTrue();

        (await RememberedAsync()).Should().BeTrue();
        await ShouldHaveSentChangeTariffAsync();
        _billing.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task IsDelayedPaymentMethodAsync_BillingAnswersCard_IsFalseAndRemembersIt()
    {
        _billingAnswer = () => Customer(delayed: false);

        (await _tariffService.IsDelayedPaymentMethodAsync(TenantId)).Should().BeFalse();

        // No earlier answer counts as a card, so this answer changes nothing the clients show
        (await RememberedAsync()).Should().BeFalse();
        _socketChannel.Reader.TryRead(out _).Should().BeFalse();
        _billing.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task IsDelayedPaymentMethodAsync_PortalIsNoLongerACustomer_IsFalseAndTellsTheClients()
    {
        _billingAnswer = () => Customer(delayed: true);
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);
        await ShouldHaveSentChangeTariffAsync();

        // "cannot find" is an answer of billing: the portal is not its customer, so it pays by nothing delayed
        _billingAnswer = () => Reply(HttpStatusCode.OK, """{"Message":"error: cannot find portal"}""");
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);

        (await _tariffService.IsDelayedPaymentMethodAsync(TenantId)).Should().BeFalse();
        (await RememberedAsync()).Should().BeFalse();
        await ShouldHaveSentChangeTariffAsync();
    }

    [Fact]
    public async Task IsDelayedPaymentMethodAsync_BillingFailsAfterADelayedAnswer_KeepsTheDelayedAnswer()
    {
        _billingAnswer = () => Customer(delayed: true);
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);
        await ShouldHaveSentChangeTariffAsync();

        _billingAnswer = Failure;
        var callsBefore = _billing.CallCount;
        (await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true)).Should().BeNull();

        // The failed request reached billing, and its failure did not overwrite the earlier answer
        _billing.CallCount.Should().BeGreaterThan(callsBefore);
        (await _tariffService.IsDelayedPaymentMethodAsync(TenantId)).Should().BeTrue();
        (await RememberedAsync()).Should().BeTrue();
        _socketChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task IsDelayedPaymentMethodAsync_BillingFailsWithNoEarlierAnswer_IsFalseAndRemembersNothing()
    {
        _billingAnswer = Failure;

        (await _tariffService.IsDelayedPaymentMethodAsync(TenantId)).Should().BeFalse();

        _billing.CallCount.Should().BeGreaterThan(0);
        (await RememberedAsync()).Should().BeNull();
        _socketChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task IsDelayedPaymentMethodAsync_CustomerMovesFromDelayedToCard_IsFalseAndTellsTheClients()
    {
        _billingAnswer = () => Customer(delayed: true);
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);
        await ShouldHaveSentChangeTariffAsync();

        _billingAnswer = () => Customer(delayed: false);
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);

        (await _tariffService.IsDelayedPaymentMethodAsync(TenantId)).Should().BeFalse();
        await ShouldHaveSentChangeTariffAsync();
    }

    [Fact]
    public async Task GetCustomerInfoAsync_SameDelayedAnswerTwice_TellsTheClientsOnce()
    {
        _billingAnswer = () => Customer(delayed: true);
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);
        await _tariffService.GetCustomerInfoAsync(TenantId, refresh: true);

        _billing.CallCount.Should().Be(2);
        await ShouldHaveSentChangeTariffAsync();
        _socketChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(GraceDays, GraceDays + 1)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(int.MaxValue, TariffService.MaxDelayedPaymentMethodGraceDays + 1)]
    public void GetDelayedPaymentMethodCacheDuration_GraceDays_KeepsTheAnswerOneDayLongerWithinBounds(int paymentDelay, int days)
    {
        TariffService.GetDelayedPaymentMethodCacheDuration(paymentDelay).Should().Be(TimeSpan.FromDays(days));
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void DelayedPaymentMethodChanged_KnownAndNewAnswer_NoEarlierAnswerCountsAsCard(bool? known, bool delayed, bool changed)
    {
        TariffService.DelayedPaymentMethodChanged(known, delayed).Should().Be(changed);
    }

    // How billing answers the customer request; IsDelayedPaymentMethod is true for SEPA and false for a card.
    private static StubResponse Customer(bool delayed)
    {
        return Reply(HttpStatusCode.OK,
            $$"""{"PortalId":"{{PaymentRegion}}{{TenantId}}","PaymentMethodStatus":1,"PaymentMethodType":"{{(delayed ? "sepa_debit" : "card")}}","IsDelayedPaymentMethod":{{(delayed ? "true" : "false")}},"Email":"owner@example.com"}""");
    }

    private static StubResponse Failure()
    {
        return Reply(HttpStatusCode.InternalServerError, "{}");
    }

    private async Task<bool?> RememberedAsync()
    {
        return await _hybridCache.GetOrDefaultAsync<bool?>(TariffService.GetDelayedPaymentMethodCacheKey(TenantId),
            token: TestContext.Current.CancellationToken);
    }

    private async Task ShouldHaveSentChangeTariffAsync()
    {
        _socketChannel.Reader.TryRead(out var queued).Should().BeTrue("a change of the answer tells the clients to read the tariff again");
        using var request = queued!.RequestMessage;
        request.RequestUri!.AbsolutePath.Should().Be("/controller/files/change-tariff");
        (await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be($$"""{"room":"{{TenantId}}-quota"}""");
    }
}
