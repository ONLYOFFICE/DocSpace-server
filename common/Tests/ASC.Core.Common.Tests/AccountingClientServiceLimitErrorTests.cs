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
/// Contract tests for how <see cref="AccountingClient"/> turns the errors of the service limit operations into its
/// exceptions, and for the retry rule that treats a missing service limit as a definitive answer. The fake primary
/// handler of <see cref="AccountingClientTests"/> stands in for the network.
/// </summary>
public class AccountingClientServiceLimitErrorTests
{
    [Fact]
    public async Task CreateServiceLimit_SecondLimitOfTheScope_ThrowsConflictWithTheReason()
    {
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.Conflict,
            """{"title":"Conflict","status":409,"detail":"Service group 'ai-services' already has a limit for this scope","instance":"/api/ServiceLimit"}"""));

        var act = async () => await client.CreateServiceLimitAsync("portal-1", "ai-services", ServiceLimitScope.Total, null, 500m, null, ServiceLimitPeriod.Month);

        (await act.Should().ThrowExactlyAsync<AccountingConflictException>())
            .Which.Message.Should().Be("Service group 'ai-services' already has a limit for this scope");
    }

    [Fact]
    public async Task CreateServiceLimit_RejectedThreshold_ThrowsBadRequestWithTheReason()
    {
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest,
            """{"title":"Bad request","status":400,"detail":"Services of group 'ai-services' have different units, a quantity threshold cannot be set"}"""));

        var act = async () => await client.CreateServiceLimitAsync("portal-1", "ai-services", ServiceLimitScope.Global, null, null, 1000, ServiceLimitPeriod.Day);

        (await act.Should().ThrowExactlyAsync<AccountingBadRequestException>())
            .Which.Message.Should().Be("Services of group 'ai-services' have different units, a quantity threshold cannot be set");
    }

    [Fact]
    public async Task UpdateServiceLimit_ValidationErrors_ThrowsBadRequestWithTheMessages()
    {
        // Field validation comes in the ASP.NET format, with an "errors" object instead of "detail".
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest,
            """{"title":"One or more validation errors occurred.","status":400,"errors":{"AmountValue":["The field AmountValue must be between 0 and 79228162514264337593543950335."]}}"""));

        var act = async () => await client.UpdateServiceLimitAsync(42, -1m, null, null);

        (await act.Should().ThrowExactlyAsync<AccountingBadRequestException>())
            .Which.Message.Should().Be("The field AmountValue must be between 0 and 79228162514264337593543950335.");
    }

    [Fact]
    public async Task Charge_ServiceLimitExceeded_ThrowsServiceLimitExceededWithTheReason()
    {
        // A limit refuses a charge with 403 and this title; the wallet may still have the money.
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.Forbidden,
            """{"title":"Service limit exceeded","status":403,"detail":"Service limit exceeded for customer No.10234. Service group 'ai-services', amount limit 100, available 12.50"}"""));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "ai-tools", "ref", 1, 10);

        (await act.Should().ThrowExactlyAsync<AccountingServiceLimitExceededException>())
            .Which.Message.Should().StartWith("Service limit exceeded for customer No.10234");
    }

    [Fact]
    public async Task Forbidden_WithoutTheServiceLimitTitle_StaysAPlainFailure()
    {
        // A 403 without the title means our API client lacks the access level: a failure of ours, not of the user.
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.Forbidden,
            """{"title":"Forbidden","status":403}"""));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "ai-tools", "ref", 1, 10);

        await act.Should().ThrowExactlyAsync<AccountingException>();
    }

    [Fact]
    public async Task BadRequest_OutsideTheServiceLimits_StaysAPlainFailure()
    {
        // Outside the service limits a 400 means a request the portal built wrongly, so it is not passed on as the
        // caller's mistake.
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest,
            """{"title":"Bad request","status":400,"detail":"Quantity must be positive"}"""));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "ai-tools", "ref", 1, 10);

        await act.Should().ThrowExactlyAsync<AccountingException>();
    }

    [Fact]
    public async Task CreateServiceLimit_UnknownServiceGroup_ThrowsBadRequestNotCustomerNotFound()
    {
        // The accounting service answers a missing service group with 400 "... not found" as well; the portal does have
        // a customer, so the reason must reach the caller instead of turning into "Customer not found".
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest,
            """{"title":"Bad request","status":400,"detail":"Service group 'ai-services' not found","instance":"/api/serviceLimit"}"""));

        var act = async () => await client.CreateServiceLimitAsync("portal-1", "ai-services", ServiceLimitScope.Total, null, 50m, null, ServiceLimitPeriod.Day);

        (await act.Should().ThrowExactlyAsync<AccountingBadRequestException>())
            .Which.Message.Should().Be("Service group 'ai-services' not found");
    }

    [Fact]
    public async Task UpdateServiceLimit_UnknownId_ThrowsNotFound()
    {
        // A read of a missing limit answers 404, an update or a deletion 400 with this detail; both mean the same.
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest,
            """{"title":"Bad request","status":400,"detail":"Service limit '42' not found","instance":"/api/serviceLimit"}"""));

        var act = async () => await client.UpdateServiceLimitAsync(42, 1m, null, null);

        await act.Should().ThrowExactlyAsync<AccountingNotFoundException>();
    }

    [Fact]
    public async Task GetTotalServiceLimit_UnknownCustomer_ThrowsCustomerNotFoundWithoutRetry()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.BadRequest,
            """{"title":"Bad request","status":400,"detail":"Customer account 'portal-1' not found"}"""));

        var act = async () => await client.GetTotalServiceLimitAsync("portal-1", "ai-services", includeUsage: true);

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTotalServiceLimit_NoLimitSet_ThrowsNotFoundWithoutRetry()
    {
        // A missing limit is a definitive result, so the GET must not be retried.
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.NotFound,
            """{"title":"Resource not found","status":404,"detail":"Customer 'portal-1' has no total limit on group 'ai-services'"}"""));

        var act = async () => await client.GetTotalServiceLimitAsync("portal-1", "ai-services", includeUsage: true);

        await act.Should().ThrowExactlyAsync<AccountingNotFoundException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetServiceLimit_UnknownId_ThrowsNotFoundWithoutRetry()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.NotFound,
            """{"title":"Resource not found","status":404}"""));

        var act = async () => await client.GetServiceLimitAsync(999999);

        await act.Should().ThrowExactlyAsync<AccountingNotFoundException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRequestOutsideServiceLimits_NotFound_IsStillRetried()
    {
        // Outside the service limits a 404 may be transient: a customer created a moment ago may not be visible yet.
        var calls = 0;
        var (client, handler) = AccountingClientTests.CreateClient(_ =>
        {
            calls++;
            return calls == 1
                ? AccountingClientTests.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}""")
                : AccountingClientTests.Json(HttpStatusCode.OK, """{"accountNumber":7}""");
        });

        var balance = await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        balance.AccountNumber.Should().Be(7);
        handler.CallCount.Should().Be(2);
    }
}
