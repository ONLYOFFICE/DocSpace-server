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
/// products of the other service nor the cached ones their prices. Billing and accounting answer through fake
/// primary handlers; the cache is an in-process FusionCache.
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
            _ => Json(Prices(AccountingPaymentSystemId, ("1011", 20m))),
            _ => ++accountingCalls == 1
                ? throw new HttpRequestException("connection refused")
                : Json(ServicesPage(("backup", 10m))));

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
        var (service, billing, _) = CreateService(
            _ => Json(Prices(StripePaymentSystemId, ("1001", 30m), ("1002", 40m))),
            _ => throw new InvalidOperationException("Plan prices never go to accounting"));

        await service.GetProductPriceInfoAsync(PartnerId, false, ["1001"]);
        var prices = await service.GetProductPriceInfoAsync(PartnerId, false, ["1001", "1002"]);

        prices["1001"].Should().Equal(Usd(30m));
        prices["1002"].Should().Equal(Usd(40m));
        billing.RequestBodies.Should().HaveCount(2);
        ProductIdsOf(billing.RequestBodies[0]).Should().Equal("1001");
        ProductIdsOf(billing.RequestBodies[1]).Should().Equal("1002");
    }

    [Fact]
    public async Task Plan_BillingFails_KeepsCachedPricesAndDoesNotCacheFailedProducts()
    {
        var billingCalls = 0;
        var (service, billing, _) = CreateService(
            _ => ++billingCalls == 2
                ? Json("boom", HttpStatusCode.InternalServerError)
                : Json(Prices(StripePaymentSystemId, ("1001", 30m), ("1002", 40m))),
            _ => throw new InvalidOperationException("Plan prices never go to accounting"));

        await service.GetProductPriceInfoAsync(PartnerId, false, ["1001"]);
        var failed = await service.GetProductPriceInfoAsync(PartnerId, false, ["1001", "1002"]);
        var recovered = await service.GetProductPriceInfoAsync(PartnerId, false, ["1001", "1002"]);

        failed["1001"].Should().Equal(Usd(30m));
        failed["1002"].Should().BeEmpty();
        recovered["1002"].Should().Equal(Usd(40m));
        ProductIdsOf(billing.RequestBodies[^1]).Should().Equal("1002");
    }

    [Fact]
    public async Task Wallet_ProductWithoutPrice_IsCachedAsEmpty()
    {
        var (service, billing, _) = CreateService(
            _ => Json(Prices(AccountingPaymentSystemId)),
            _ => throw new InvalidOperationException("Numeric wallet products never go to accounting"));

        var first = await service.GetProductPriceInfoAsync(PartnerId, true, ["1099"]);
        var second = await service.GetProductPriceInfoAsync(PartnerId, true, ["1099"]);

        first["1099"].Should().BeEmpty();
        second["1099"].Should().BeEmpty();
        billing.CallCount.Should().Be(1);
    }

    private static (TariffService service, CapturingHandler billing, CapturingHandler accounting) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> billingResponder,
        Func<HttpRequestMessage, HttpResponseMessage> accountingResponder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:payment:url"] = "https://billing.example.com",
                ["core:payment:key"] = "test-key",
                ["core:payment:secret"] = "test-secret",
                ["core:accounting:url"] = "https://accounting.example.com/api",
                ["core:accounting:key"] = "test-key",
                ["core:accounting:secret"] = "test-secret"
            })
            .Build();

        var billing = new CapturingHandler(billingResponder);
        var accounting = new CapturingHandler(accountingResponder);

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(typeof(ICache), typeof(AscCache));
        services.AddScoped<BillingClient>();
        services.AddScoped<AccountingClient>();
        services.AddFusionCache();

        services.AddBillingHttpClient(configuration);
        services.AddAccountingHttpClient(configuration);

        // AddRefitGeneratedClient sets a primary handler on its own named client, so the fake handlers have to
        // target those exact clients by name.
        services.AddHttpClient(Refit.UniqueName.ForType<IBillingApi>())
                .ConfigurePrimaryHttpMessageHandler(() => billing);
        services.AddHttpClient(Refit.UniqueName.ForType<IAccountingApi>())
                .ConfigurePrimaryHttpMessageHandler(() => accounting);

        var provider = services.BuildServiceProvider();

        // Pricing uses only the payment clients, the cache and the logger; the other dependencies stay unset.
        var service = new TariffService(
            quotaService: null!,
            tenantService: null!,
            coreBaseSettings: null!,
            coreSettings: null!,
            configuration: configuration,
            coreDbContextManager: null!,
            cache: provider.GetRequiredService<ICache>(),
            hybridCache: provider.GetRequiredService<IFusionCache>(),
            distributedLockProvider: null!,
            logger: NullLogger<TariffService>.Instance,
            billingClient: provider.GetRequiredService<BillingClient>(),
            accountingClient: provider.GetRequiredService<AccountingClient>(),
            docsCloudClient: null!,
            serviceProvider: provider,
            resiliencePipelineProvider: null!,
            tenantExtraConfig: null!);

        return (service, billing, accounting);
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

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            var response = responder(request);

            // Mimic the real primary handler so the resilience pipeline can inspect the request method.
            response.RequestMessage = request;

            return response;
        }
    }
}
