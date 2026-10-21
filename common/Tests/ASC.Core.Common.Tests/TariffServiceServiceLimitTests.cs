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
/// balance lookup never reaches the portal key, the distributed lock or the accounting service. The portal key comes
/// from a real <see cref="CoreSettings"/> over a tenant service that knows no tenants, and every other dependency of
/// <see cref="TariffService"/> is unused on these paths and passed as null.
/// </remarks>
public class TariffServiceServiceLimitTests
{
    private const int TenantId = 1;
    private const int OwnAccountNumber = 1001;
    private const int OtherAccountNumber = 2002;
    private const string PaymentRegion = "portal-";
    private const int TotalParticipantLimits = 3;
    private const string UserA = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string UserB = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
    private const string UserC = "16fd2706-8baf-433b-82eb-8c7fada847da";

    // How the accounting service answers an update or a deletion of a limit it does not have.
    private const string ServiceLimitNotFoundBody =
        """{"title":"Bad request","status":400,"detail":"Service limit '42' not found","instance":"/api/serviceLimit"}""";

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
    public async Task DeleteServiceLimit_LimitOfThisPortal_DeletesAndReturnsIt()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Delete ? new HttpResponseMessage(HttpStatusCode.NoContent) : ServiceLimitResponse(OwnAccountNumber));

        var deleted = await tariffService.DeleteServiceLimitAsync(TenantId, 42);

        deleted.Should().NotBeNull();
        deleted.Id.Should().Be(42);
        handler.CallCount.Should().Be(2);
        handler.LastMethod.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task DeleteServiceLimit_LimitOfAnotherPortal_ReturnsNullWithoutDeleting()
    {
        // A limit is deleted by its global ID, so the owner is checked first and a foreign limit is never deleted.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Delete ? new HttpResponseMessage(HttpStatusCode.NoContent) : ServiceLimitResponse(OtherAccountNumber));

        var deleted = await tariffService.DeleteServiceLimitAsync(TenantId, 42);

        deleted.Should().BeNull();
        handler.CallCount.Should().Be(1);
        handler.LastMethod.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task DeleteServiceLimit_AlreadyDeletedByAConcurrentRequest_ReturnsNull()
    {
        // The accounting service answers a deletion of a limit it does not have with 400, not 404.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Delete
                ? AccountingClientTests.Json(HttpStatusCode.BadRequest, ServiceLimitNotFoundBody)
                : ServiceLimitResponse(OwnAccountNumber));

        var deleted = await tariffService.DeleteServiceLimitAsync(TenantId, 42);

        deleted.Should().BeNull();
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateServiceLimit_AlreadyDeletedByAConcurrentRequest_ReturnsNull()
    {
        // The accounting service answers an update of a limit it does not have with 400, not 404.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Put
                ? AccountingClientTests.Json(HttpStatusCode.BadRequest, ServiceLimitNotFoundBody)
                : ServiceLimitResponse(OwnAccountNumber));

        var updated = await tariffService.UpdateServiceLimitAsync(TenantId, 42, 100m, null, null);

        updated.Should().BeNull();
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateServiceLimit_RejectedThreshold_ThrowsBadRequest()
    {
        // A 400 that explains a mistake of the caller must reach the caller, not turn into "limit not found".
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Put
                ? AccountingClientTests.Json(HttpStatusCode.BadRequest,
                    """{"title":"Bad request","status":400,"detail":"Services of group 'ai-services' have different units, a quantity threshold can not be set"}""")
                : ServiceLimitResponse(OwnAccountNumber));

        var act = async () => await tariffService.UpdateServiceLimitAsync(TenantId, 42, null, 1000, null);

        await act.Should().ThrowExactlyAsync<AccountingBadRequestException>();
    }

    [Fact]
    public async Task GetTotalServiceLimit_ReadsThePortalLimitWithItsUsage()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.OK,
                """{"id":41,"serviceGroupName":"ai-services","scope":"Total","amountValue":500,"currency":"USD","period":"Month","enabled":true,"usage":{"periodStart":"2026-10-01T00:00:00Z","amountConsumed":312.4,"amountAvailable":187.6,"quantityConsumed":0}}"""));

        var serviceLimit = await tariffService.GetTotalServiceLimitAsync(TenantId, "ai-services");

        serviceLimit.Scope.Should().Be(ServiceLimitScope.Total);
        serviceLimit.Usage.AmountAvailable.Should().Be(187.6m);
        handler.LastUri!.AbsolutePath.Should().Be($"/api/serviceLimit/by-customer/{PaymentRegion}{TenantId}/by-group/ai-services/total");
        AccountingClientTests.ParseQuery(handler.LastUri)["includeUsage"].Should().Be("true");
    }

    [Fact]
    public async Task GetTotalServiceLimit_NoneSet_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var serviceLimit = await tariffService.GetTotalServiceLimitAsync(TenantId, "ai-services");

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetGlobalServiceLimit_NoneSet_ReturnsNull()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""));

        var serviceLimit = await tariffService.GetGlobalServiceLimitAsync(TenantId, "ai-services");

        serviceLimit.Should().BeNull();
        handler.LastUri!.AbsolutePath.Should().Be($"/api/serviceLimit/by-customer/{PaymentRegion}{TenantId}/by-group/ai-services/global");
    }

    [Fact]
    public async Task TotalAndGlobalLimits_PortalUnknownToTheAccountingService_ReturnNull()
    {
        // The accounting service answers an unknown customer with 400; for the portal that simply means no limit.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => AccountingClientTests.Json(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Customer 'portal-1' not found"}"""));

        var total = await tariffService.GetTotalServiceLimitAsync(TenantId, "ai-services");
        var global = await tariffService.GetGlobalServiceLimitAsync(TenantId, "ai-services");

        total.Should().BeNull();
        global.Should().BeNull();
    }

    [Fact]
    public async Task GetParticipantServiceLimits_PortalUnknownToTheAccountingService_ReturnsAnEmptyPage()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => AccountingClientTests.Json(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400,"detail":"Customer 'portal-1' not found"}"""));

        var report = await tariffService.GetParticipantServiceLimitsAsync(TenantId, "ai-services", new ServiceLimitFilter { Offset = 50, Limit = 10 });

        report.Collection.Should().BeEmpty();
        report.TotalQuantity.Should().Be(0);
        report.Offset.Should().Be(50);
        report.Limit.Should().Be(10);
    }

    [Fact]
    public async Task GetCachedParticipantServiceLimits_ReadsEveryPageOnce_ThenServesFromCache()
    {
        var requests = new List<Uri>();
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request =>
            {
                requests.Add(request.RequestUri!);
                return AccountingClientTests.ParseQuery(request.RequestUri!)["offset"] == "0"
                    ? ParticipantLimitsPage(UserA, UserB)
                    : ParticipantLimitsPage(UserC);
            });

        var first = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");
        var second = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        first.Select(limit => limit.Participant).Should().Equal(UserA, UserB, UserC);
        second.Select(limit => limit.Participant).Should().Equal(UserA, UserB, UserC);
        handler.CallCount.Should().Be(2);

        requests.Should().AllSatisfy(uri => uri.AbsolutePath.Should().Be($"/api/serviceLimit/by-customer/{PaymentRegion}{TenantId}/by-group/ai-services/participants"));
        requests.Select(uri => AccountingClientTests.ParseQuery(uri)["offset"]).Should().Equal("0", "2");
        requests.Should().AllSatisfy(uri => AccountingClientTests.ParseQuery(uri)["limit"].Should().Be("100"));
        requests.Should().AllSatisfy(uri => AccountingClientTests.ParseQuery(uri)["includeUsage"].Should().Be("true"));
    }

    [Fact]
    public async Task GetCachedParticipantServiceLimits_PortalNotACustomer_ReturnsEmpty()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance(),
            _ => AccountingClientTests.Json(HttpStatusCode.BadRequest, "Customer not found"));

        var serviceLimits = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        serviceLimits.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetCachedParticipantServiceLimits_AccountingFails_ReturnsEmptyInsteadOfThrowing(HttpStatusCode status)
    {
        // The user lists read these limits; an unreachable accounting service, or one without the route, must not break
        // them.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(status, "boom"));

        var serviceLimits = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        serviceLimits.Should().BeEmpty();
    }

    [Fact]
    public async Task SetParticipantServiceLimits_DropsTheCache_SoTheNextReadSeesTheChange()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Put
                ? AccountingClientTests.Json(HttpStatusCode.OK, "1")
                : AccountingClientTests.ParseQuery(request.RequestUri!)["offset"] == "0"
                    ? ParticipantLimitsPage(UserA, UserB)
                    : ParticipantLimitsPage(UserC));

        await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");
        await tariffService.SetParticipantServiceLimitsAsync(TenantId, "ai-services", [UserC], 40m, null, ServiceLimitPeriod.Day);
        await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        // Two pages, the update, and the two pages again.
        handler.CallCount.Should().Be(5);
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_UserWithAPersonalLimit_ReturnsItWithTheUsage()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request) ? GlobalLimitResponse(enabled: true) : ParticipantRows(Row(42, "Custom", UserA)));

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserA));

        serviceLimit.Id.Should().Be(42);
        serviceLimit.Scope.Should().Be(ServiceLimitScope.Custom);
        serviceLimit.Usage.AmountConsumed.Should().Be(12.5m);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_UserUnderTheLimitForEveryUser_ReturnsThatRowWithTheUsage()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request) ? GlobalLimitResponse(enabled: true) : ParticipantRows(Row(42, "Custom", UserA), Row(43, "Global", UserB)));

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserB));

        serviceLimit.Id.Should().Be(43);
        serviceLimit.Scope.Should().Be(ServiceLimitScope.Global);
        serviceLimit.Usage.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_UserNotInTheList_FallsBackToTheLimitForEveryUser()
    {
        // The accounting service lists only the users it has met; the others are still under the limit for every user.
        var globalRequests = 0;
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request =>
            {
                if (IsGlobalRequest(request))
                {
                    globalRequests++;
                    return GlobalLimitResponse(enabled: true);
                }

                return ParticipantRows(Row(42, "Custom", UserA));
            });

        var first = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserC));
        var second = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserB));

        first.Id.Should().Be(43);
        first.Scope.Should().Be(ServiceLimitScope.Global);
        first.Usage.Should().BeNull();
        second.Id.Should().Be(43);
        globalRequests.Should().Be(1);
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_PersonalLimitSwitchedOff_FallsBackToTheLimitForEveryUser()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request) ? GlobalLimitResponse(enabled: true) : ParticipantRows(Row(42, "Custom", UserA, enabled: false)));

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserA));

        serviceLimit.Id.Should().Be(43);
        serviceLimit.Scope.Should().Be(ServiceLimitScope.Global);
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_LimitForEveryUserSwitchedOff_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request) ? GlobalLimitResponse(enabled: false) : ParticipantRows());

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserC));

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_NoLimitAtAll_ReturnsNull()
    {
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request)
                ? AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}""")
                : ParticipantRows());

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserC));

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task CreateServiceLimit_DropsBothCaches_SoTheNextReadSeesTheChange()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => request.Method == HttpMethod.Post
                ? ServiceLimitResponse(OwnAccountNumber)
                : IsGlobalRequest(request) ? GlobalLimitResponse(enabled: true) : ParticipantRows());

        await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserC));
        await tariffService.CreateServiceLimitAsync(TenantId, "ai-services", ServiceLimitScope.Global, null, 50m, null, ServiceLimitPeriod.Day);
        await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserC));

        // The list and the limit for every user, the creation, and both again.
        handler.CallCount.Should().Be(5);
    }

    private static async Task<(TariffService tariffService, AccountingClientTests.CapturingHandler handler)> CreateTariffServiceAsync(
        Balance balance, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var (accountingClient, handler) = AccountingClientTests.CreateClient(responder);

        var hybridCache = new FusionCache(new FusionCacheOptions());
        await hybridCache.SetAsync(TariffService.GetAccountingBalanceCacheKey(TenantId), balance, TimeSpan.FromMinutes(5),
            token: TestContext.Current.CancellationToken);

        // A SaaS portal unknown to the tenant service gets the portal ID "<payment region><tenant ID>".
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:payment:region"] = PaymentRegion })
            .Build();
        var coreSettings = new CoreSettings(DefaultResultProxy<ITenantService>.Create(), new CoreBaseSettings(configuration), configuration, null!);

        var tariffService = new TariffService(
            quotaService: null!,
            tenantService: null!,
            coreBaseSettings: null!,
            coreSettings: coreSettings,
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

    private static HttpResponseMessage ParticipantLimitsPage(params string[] participants)
    {
        var limits = participants.Select((participant, index) =>
            $$"""{"id":{{100 + index}},"serviceGroupName":"ai-services","scope":"Custom","customerAccountNumber":{{OwnAccountNumber}},"participant":"{{participant}}","amountValue":40,"currency":"USD","period":"Day","enabled":true}""");

        return AccountingClientTests.Json(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", limits)}}],"totalQuantity":{{TotalParticipantLimits}}}""");
    }

    private static bool IsGlobalRequest(HttpRequestMessage request)
    {
        return request.RequestUri!.AbsolutePath.EndsWith("/global", StringComparison.Ordinal);
    }

    private static HttpResponseMessage GlobalLimitResponse(bool enabled)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"id":43,"serviceGroupName":"ai-services","customerAccountNumber":{{OwnAccountNumber}},"scope":"Global","amountValue":50,"currency":"USD","period":"Day","enabled":{{(enabled ? "true" : "false")}}}""");
    }

    private static string Row(int id, string scope, string participant, bool enabled = true)
    {
        return $$$"""{"id":{{{id}}},"serviceGroupName":"ai-services","customerAccountNumber":{{{OwnAccountNumber}}},"scope":"{{{scope}}}","participant":"{{{participant}}}","amountValue":40,"currency":"USD","period":"Day","enabled":{{{(enabled ? "true" : "false")}}},"usage":{"periodStart":"2026-10-18T00:00:00Z","periodEnd":"2026-10-18T23:59:59Z","amountConsumed":12.5,"amountAvailable":27.5,"quantityConsumed":0}}""";
    }

    // One page holding every row, so the paged read stops after it.
    private static HttpResponseMessage ParticipantRows(params string[] rows)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", rows)}}],"totalQuantity":{{rows.Length}}}""");
    }

    private static HttpResponseMessage ServiceLimitResponse(int customerAccountNumber)
    {
        return AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"id":42,"serviceGroupName":"ai-services","scope":"Custom","customerAccountNumber":{{customerAccountNumber}},"amountValue":50,"currency":"USD","period":"Day","enabled":true}""");
    }
}
