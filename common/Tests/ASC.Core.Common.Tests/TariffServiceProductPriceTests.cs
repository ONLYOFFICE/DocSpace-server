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
/// Tests for <see cref="TariffService.GetProductPriceInfoAsync"/>: every product price is cached on its own, only
/// the products missing from the cache go to the payment services, and a failed payment service costs neither the
/// products of the other service nor the cached ones their prices. Billing and accounting answer through
/// <see cref="HttpStub"/>s; the cache is an in-process FusionCache.
/// </summary>
public class TariffServiceProductPriceTests
{
    private const string PartnerId = "partner-1";
    private const int StripePaymentSystemId = 9;
    private const int AccountingPaymentSystemId = 11;

    [Fact]
    public async Task Wallet_AccountingFails_KeepsBillingPricesAndAsksForFailedServiceAgain()
    {
        var accountingCalls = 0;
        var (service, billing, accounting) = CreateService(
            _ => Reply(HttpStatusCode.OK, Prices(AccountingPaymentSystemId, ("1011", 20m))),
            _ => Interlocked.Increment(ref accountingCalls) == 1
                ? TransportFault()
                : Reply(HttpStatusCode.OK, ServicesPage(("backup", 10m))));

        var first = await service.GetProductPriceInfoAsync(PartnerId, true, ["1011", "backup"]);

        first["1011"].Should().Equal(Usd(20m));
        first["backup"].Should().BeEmpty();

        var second = await service.GetProductPriceInfoAsync(PartnerId, true, ["1011", "backup"]);

        second["1011"].Should().Equal(Usd(20m));
        second["backup"].Should().Equal(Usd(10m));
        billing.CallCount.Should().Be(1);
        accounting.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Plan_SomeProductsCached_AsksBillingOnlyForMissingOnes()
    {
        // Plan prices never go to accounting.
        var (service, billing, accounting) = CreateService(
            _ => Reply(HttpStatusCode.OK, Prices(StripePaymentSystemId, ("1001", 30m), ("1002", 40m))),
            NotExpected);

        await service.GetProductPriceInfoAsync(PartnerId, false, ["1001"]);
        var prices = await service.GetProductPriceInfoAsync(PartnerId, false, ["1001", "1002"]);

        prices["1001"].Should().Equal(Usd(30m));
        prices["1002"].Should().Equal(Usd(40m));
        billing.RequestBodies.Should().HaveCount(2);
        ProductIdsOf(billing.RequestBodies[0]).Should().Equal("1001");
        ProductIdsOf(billing.RequestBodies[1]).Should().Equal("1002");
        accounting.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Plan_BillingFails_KeepsCachedPricesAndDoesNotCacheFailedProducts()
    {
        var billingCalls = 0;
        var (service, billing, accounting) = CreateService(
            _ => Interlocked.Increment(ref billingCalls) == 2
                ? Reply(HttpStatusCode.InternalServerError, "boom")
                : Reply(HttpStatusCode.OK, Prices(StripePaymentSystemId, ("1001", 30m), ("1002", 40m))),
            NotExpected);

        await service.GetProductPriceInfoAsync(PartnerId, false, ["1001"]);
        var failed = await service.GetProductPriceInfoAsync(PartnerId, false, ["1001", "1002"]);
        var recovered = await service.GetProductPriceInfoAsync(PartnerId, false, ["1001", "1002"]);

        failed["1001"].Should().Equal(Usd(30m));
        failed["1002"].Should().BeEmpty();
        recovered["1002"].Should().Equal(Usd(40m));
        ProductIdsOf(billing.RequestBodies[^1]).Should().Equal("1002");
        accounting.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Wallet_ProductWithoutPrice_IsCachedAsEmpty()
    {
        // Numeric wallet products never go to accounting.
        var (service, billing, accounting) = CreateService(
            _ => Reply(HttpStatusCode.OK, Prices(AccountingPaymentSystemId)),
            NotExpected);

        var first = await service.GetProductPriceInfoAsync(PartnerId, true, ["1099"]);
        var second = await service.GetProductPriceInfoAsync(PartnerId, true, ["1099"]);

        first["1099"].Should().BeEmpty();
        second["1099"].Should().BeEmpty();
        billing.CallCount.Should().Be(1);
        accounting.CallCount.Should().Be(0);
    }

    private static (TariffService service, HttpStub billing, HttpStub accounting) CreateService(
        Func<StubRequest, StubResponse> billingResponder,
        Func<StubRequest, StubResponse> accountingResponder)
    {
        var billing = new HttpStub(billingResponder);
        var accounting = new HttpStub(accountingResponder);
        var provider = BuildProvider(billing: billing, accounting: accounting);

        // Pricing uses only the payment clients, the cache and the logger; the other dependencies stay unset.
        var service = CreateTariffService(
            provider.GetRequiredService<AccountingClient>(),
            provider.GetRequiredService<BillingClient>(),
            provider.GetRequiredService<IFusionCache>(),
            configuration: provider.GetRequiredService<IConfiguration>(),
            cache: provider.GetRequiredService<ICache>(),
            serviceProvider: provider);

        return (service, billing, accounting);
    }

    // The answer of a service the case must not ask at all; the case then checks that it was not asked.
    private static StubResponse NotExpected(StubRequest request)
    {
        return Reply(HttpStatusCode.InternalServerError, $"Unexpected request {request.Method} {request.Path}");
    }

    private static Dictionary<string, decimal> Usd(decimal price)
    {
        return new Dictionary<string, decimal> { ["USD"] = price };
    }

    private static string Prices(int paymentSystemId, params (string ProductId, decimal Price)[] products)
    {
        var prices = new Dictionary<int, Dictionary<string, Dictionary<string, decimal>>>
        {
            [paymentSystemId] = products.ToDictionary(p => p.ProductId, p => Usd(p.Price))
        };

        return JsonSerializer.Serialize(prices);
    }

    private static string ServicesPage(params (string Name, decimal Price)[] services)
    {
        var items = services.Select(s =>
            $$$"""{"name":"{{{s.Name}}}","priceValue":{{{s.Price.ToString(CultureInfo.InvariantCulture)}}},"currency":{"code":"USD"}}""");

        return $$"""{"collection":[{{string.Join(",", items)}}],"totalQuantity":{{services.Length}}}""";
    }

    private static List<string?> ProductIdsOf(string requestBody)
    {
        using var body = JsonDocument.Parse(requestBody);

        return body.RootElement.GetProperty("ProductId").EnumerateArray().Select(id => id.GetString()).ToList();
    }
}
