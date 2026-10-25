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
/// The cached service limits of <see cref="TariffService"/> behind the user lists: reading every page once, the limit
/// in force for one user, dropping the caches on a change, and staying usable while the accounting service fails or
/// hangs.
/// </summary>
public class TariffServiceServiceLimitCacheTests : TariffServiceServiceLimitTestBase
{
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

        first.Keys.Should().BeEquivalentTo([Guid.Parse(UserA), Guid.Parse(UserB), Guid.Parse(UserC)]);
        second.Keys.Should().BeEquivalentTo(first.Keys);
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
    public async Task GetCachedParticipantServiceLimits_AccountingFails_ReturnsNullInsteadOfThrowing(HttpStatusCode status)
    {
        // The user lists read these limits; an unreachable accounting service, or one without the route, must not break
        // them. Null says the list is unknown - an empty one would claim no user has a personal limit.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => AccountingClientTests.Json(status, "boom"));

        var serviceLimits = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        serviceLimits.Should().BeNull();
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_ListOfTheUsersFails_ReturnsNullRatherThanTheLimitForEveryUser()
    {
        // The user may have a personal limit the failed list would have shown; the limit for every user must not take
        // its place.
        var (tariffService, _) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request)
                ? GlobalLimitResponse(enabled: true)
                : AccountingClientTests.Json(HttpStatusCode.InternalServerError, "boom"));

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserA));

        serviceLimit.Should().BeNull();
    }

    [Fact]
    public async Task GetCachedParticipantServiceLimits_AccountingHangs_ReturnsNullWithinTheTimeoutAndDoesNotWaitAgain()
    {
        // Every page of users and every sign-in reads these limits, so a hanging accounting service must not stall them,
        // nor make each following request wait for it once more.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new DelayedContent("""{"collection":[],"totalQuantity":0}""", TimeSpan.FromSeconds(30)) });

        var started = DateTime.UtcNow;
        var first = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");
        var firstTook = DateTime.UtcNow - started;

        started = DateTime.UtcNow;
        var second = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");
        var secondTook = DateTime.UtcNow - started;

        first.Should().BeNull();
        firstTook.Should().BeLessThan(TimeSpan.FromSeconds(10));
        second.Should().BeNull();
        secondTook.Should().BeLessThan(TimeSpan.FromSeconds(1));
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateServiceLimit_ReadInFlightDuringTheChange_DoesNotBringTheOldListBack()
    {
        // A read that began before the change and ends after it must not leave the list from before the change in the
        // cache: the clients asked to re-read would get the old limits again for minutes.
        var listReads = 0;
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request =>
            {
                if (request.Method == HttpMethod.Post)
                {
                    return ServiceLimitResponse(OwnAccountNumber);
                }

                return Interlocked.Increment(ref listReads) == 1
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new DelayedContent($"{{\"collection\":[{Row(42, "Custom", UserA)}],\"totalQuantity\":1}}", TimeSpan.FromSeconds(1)) }
                    : ParticipantRows(Row(43, "Custom", UserB));
            });

        var readBeforeTheChange = tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (handler.CallCount == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        await tariffService.CreateServiceLimitAsync(TenantId, "ai-services", ServiceLimitScope.Custom, UserB, 40m, null, ServiceLimitPeriod.Day);
        var before = await readBeforeTheChange;
        var after = await tariffService.GetCachedParticipantServiceLimitsAsync(TenantId, "ai-services");

        before.Keys.Should().BeEquivalentTo([Guid.Parse(UserA)]);
        after.Keys.Should().BeEquivalentTo([Guid.Parse(UserB)]);
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
        serviceLimit.AmountConsumed.Should().Be(12.5m);
        serviceLimit.PeriodEnd.Should().Be(new DateTime(2026, 10, 18, 23, 59, 59, DateTimeKind.Utc));
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_PortalNotABillingCustomer_ReturnsNullWithoutAskingTheAccountingService()
    {
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            _ => ParticipantRows(Row(42, "Custom", UserA)), new CustomerInfo());

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserA));

        serviceLimit.Should().BeNull();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCachedUserServiceLimit_CustomerNotCachedYet_ReadsTheLimitsWithoutAskingTheBillingService()
    {
        // Every sign-in builds this field, and the billing service has no short timeout: with nothing cached about the
        // customer, the limits are read as usual instead. The billing client of the fixture is null, so a call to it fails.
        var (tariffService, handler) = await CreateTariffServiceAsync(new Balance { AccountNumber = OwnAccountNumber, SubAccounts = [] },
            request => IsGlobalRequest(request) ? GlobalLimitResponse(enabled: true) : ParticipantRows(Row(42, "Custom", UserA)));

        var serviceLimit = await tariffService.GetCachedUserServiceLimitAsync(TenantId, "ai-services", Guid.Parse(UserA));

        serviceLimit.Id.Should().Be(42);
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
        serviceLimit.AmountConsumed.Should().Be(12.5m);
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
        first.AmountConsumed.Should().Be(0);
        first.PeriodEnd.Should().Be(ServiceLimitPeriod.Day.GetCurrentPeriodEnd(DateTime.UtcNow));
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
}
