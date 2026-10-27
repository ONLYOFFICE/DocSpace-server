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
/// its ID, which walks the pages of a period, and the single page of the list, which swallows the failures.
/// </summary>
/// <remarks>
/// The portal key comes from a real <see cref="CoreSettings"/> over a tenant service that knows no tenants, and every
/// other dependency of <see cref="TariffService"/> is unused on these paths and passed as null.
/// </remarks>
public class TariffServiceDepositTests
{
    private const int TenantId = 1;
    private const string PaymentRegion = "portal-";

    private static readonly DateTime _utcStartDate = new(2021, 3, 9, 17, 46, 59, DateTimeKind.Utc);
    private static readonly DateTime _utcEndDate = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindCustomerDeposit_IdOnALaterPage_WalksThePeriodUntilItTurnsUp()
    {
        var requests = new List<Uri>();
        var (tariffService, _) = CreateTariffService(request =>
        {
            requests.Add(request.RequestUri!);
            var offset = int.Parse(AccountingClientTests.ParseQuery(request.RequestUri!)["offset"]);

            // Asked for 1000, the service returns at most 100 per page.
            return AccountingClientDepositTests.DepositsPage(Enumerable.Range(offset + 1, Math.Min(100, 250 - offset)), totalQuantity: 250);
        });

        var deposit = await tariffService.FindCustomerDepositAsync(TenantId, 150, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        deposit.Should().NotBeNull();
        deposit!.Id.Should().Be(150);
        deposit.PaymentRef.Should().Be("pi_150");

        // The walk stops on the page that holds the ID and never reads the third one.
        requests.Select(uri => AccountingClientTests.ParseQuery(uri)["offset"]).Should().Equal("0", "100");
        requests.Should().AllSatisfy(uri =>
        {
            uri.AbsolutePath.Should().Be($"/api/customer/{PaymentRegion}{TenantId}/deposits");

            var query = AccountingClientTests.ParseQuery(uri);
            query["startDate"].Should().Be("2021-03-09T17:46:59.0000000Z");
            query["endDate"].Should().Be("2026-10-26T12:00:00.0000000Z");
            query["limit"].Should().Be("1000");
        });
    }

    [Fact]
    public async Task FindCustomerDeposit_UnknownId_ReturnsNullAfterTheLastPage()
    {
        var (tariffService, handler) = CreateTariffService(request =>
        {
            var offset = int.Parse(AccountingClientTests.ParseQuery(request.RequestUri!)["offset"]);

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
        var (tariffService, _) = CreateTariffService(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () => await tariffService.FindCustomerDepositAsync(TenantId, 150, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
    }

    [Fact]
    public async Task FindCustomerDeposit_AccountingFailure_IsNotSwallowed()
    {
        var (tariffService, _) = CreateTariffService(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest, "The limit is out of range"));

        var act = async () => await tariffService.FindCustomerDepositAsync(TenantId, 150, _utcStartDate, _utcEndDate, TestContext.Current.CancellationToken);

        await act.Should().ThrowExactlyAsync<AccountingException>();
    }

    [Fact]
    public async Task GetCustomerDeposits_ReadsThePageOfThePortal()
    {
        var (tariffService, handler) = CreateTariffService(_ => AccountingClientDepositTests.DepositsPage([7, 8], totalQuantity: 2));

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
        var (tariffService, _) = CreateTariffService(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest, body));

        var report = await tariffService.GetCustomerDepositsAsync(TenantId, new DepositFilter { Offset = 0, Limit = 25 });

        report.Should().BeNull();
    }

    private static (TariffService tariffService, AccountingClientTests.CapturingHandler handler) CreateTariffService(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var (accountingClient, handler) = AccountingClientTests.CreateClient(responder);

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
            hybridCache: null!,
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
}
