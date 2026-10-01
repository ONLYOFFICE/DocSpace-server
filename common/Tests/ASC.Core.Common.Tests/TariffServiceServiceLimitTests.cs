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
/// The owner check of <see cref="TariffService"/> for service limits addressed by ID. The accounting service
/// looks a limit up by its global ID, so a limit of another portal must never be returned or changed: the
/// limit's <c>customerAccountNumber</c> has to match the account number of the tenant's billing customer.
/// </summary>
/// <remarks>
/// The tenant's balance is seeded into the cache under the key <see cref="TariffService"/> reads it from, so the
/// balance lookup never reaches the portal key, the distributed lock or the accounting service. Every other
/// dependency of <see cref="TariffService"/> is unused on this path and passed as null.
/// </remarks>
public class TariffServiceServiceLimitTests
{
    private const int TenantId = 1;
    private const int OwnAccountNumber = 1001;
    private const int OtherAccountNumber = 2002;

    [Fact]
    public async Task GetServiceLimit_LimitOfThisPortal_ReturnsIt()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OwnAccountNumber));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 42);

        serviceLimit.Should().NotBeNull();
        serviceLimit.Id.Should().Be(42);
    }

    [Fact]
    public async Task GetServiceLimit_LimitOfAnotherPortal_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OtherAccountNumber));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 42);

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetServiceLimit_PortalWithoutBillingCustomer_ReturnsNull()
    {
        // An empty balance is what TariffService caches for a portal the accounting service does not know.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => ServiceLimitResponse(OwnAccountNumber));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 42);

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetServiceLimit_UnknownId_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var serviceLimit = await tariffService.GetServiceLimitAsync(TenantId, 999999);

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task UpdateServiceLimit_LimitOfThisPortal_SendsUpdate()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OwnAccountNumber));

        var serviceLimit = await tariffService.UpdateServiceLimitAsync(TenantId, 42, 100m, null, null);

        serviceLimit.Should().NotBeNull();
        handler.CallCount.Should().Be(2);
        handler.LastMethod.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task UpdateServiceLimit_LimitOfAnotherPortal_ReturnsNullWithoutSendingUpdate()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ServiceLimitResponse(OtherAccountNumber));

        var serviceLimit = await tariffService.UpdateServiceLimitAsync(TenantId, 42, 100m, null, false);

        serviceLimit.Should().BeNull();
        handler.CallCount.Should().Be(1);
        handler.LastMethod.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task GetServiceLimitUsage_LimitOfThisPortal_ReturnsUsage()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsUsageRequest(request) ? UsageResponse() : ServiceLimitResponse(OwnAccountNumber));

        var usage = await tariffService.GetServiceLimitUsageAsync(TenantId, 42);

        usage.Should().NotBeNull();
        usage.ServiceLimitId.Should().Be(42);
        usage.AmountConsumed.Should().Be(12.5m);
        handler.CallCount.Should().Be(2);
        handler.LastUri!.AbsolutePath.Should().EndWith("/usage");
    }

    [Fact]
    public async Task GetServiceLimitUsage_LimitOfAnotherPortal_ReturnsNullWithoutRequestingUsage()
    {
        // The usage has no owner of its own, so the limit is checked first and the usage of a foreign limit is never read.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsUsageRequest(request) ? UsageResponse() : ServiceLimitResponse(OtherAccountNumber));

        var usage = await tariffService.GetServiceLimitUsageAsync(TenantId, 42);

        usage.Should().BeNull();
        handler.CallCount.Should().Be(1);
        handler.LastUri!.AbsolutePath.Should().NotEndWith("/usage");
    }

    [Fact]
    public async Task GetServiceLimitUsage_UnknownId_ReturnsNull()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var usage = await tariffService.GetServiceLimitUsageAsync(TenantId, 999999);

        usage.Should().BeNull();
        handler.CallCount.Should().Be(1);
    }

    private static async Task<(TariffService tariffService, AccountingClientTests.CapturingHandler handler)> CreateTariffServiceAsync(
        Balance balance, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var (accountingClient, handler) = AccountingClientTests.CreateClient(responder);

        var hybridCache = new FusionCache(new FusionCacheOptions());
        await hybridCache.SetAsync(TariffService.GetAccountingBalanceCacheKey(TenantId), balance, TimeSpan.FromMinutes(5),
            token: TestContext.Current.CancellationToken);

        var tariffService = new TariffService(
            quotaService: null!,
            tenantService: null!,
            coreBaseSettings: null!,
            coreSettings: null!,
            configuration: null!,
            coreDbContextManager: null!,
            cache: null!,
            hybridCache: hybridCache,
            distributedLockProvider: null!,
            logger: NullLogger<TariffService>.Instance,
            billingClient: null!,
            accountingClient: accountingClient,
            docsCloudClient: null!,
            serviceProvider: null!,
            resiliencePipelineProvider: null!,
            tenantExtraConfig: null!);

        return (tariffService, handler);
    }

    private static bool IsUsageRequest(HttpRequestMessage request)
    {
        return request.RequestUri!.AbsolutePath.EndsWith("/usage", StringComparison.Ordinal);
    }

    private static HttpResponseMessage UsageResponse()
    {
        return AccountingClientTests.Json(HttpStatusCode.OK,
            """{"serviceLimitId":42,"period":"Day","periodStart":"2026-10-01T00:00:00Z","periodEnd":"2026-10-02T00:00:00Z","amountValue":50,"amountConsumed":12.5,"amountAvailable":37.5,"currency":"USD","quantityConsumed":0}""");
    }

    private static HttpResponseMessage ServiceLimitResponse(int customerAccountNumber)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"id":42,"serviceName":"ai-tools","customerAccountNumber":{{customerAccountNumber}},"amountValue":50,"currency":"USD","period":"Day","enabled":true}""");
    }
}
