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
/// Contract tests for the service limit operations of <see cref="AccountingClient"/>: the request line, query
/// string and JSON body sent to the accounting service, the mapping of its answers and errors, and the retry rule that
/// treats a missing service limit as a definitive 404. The fake primary handler of
/// <see cref="AccountingClientTests"/> stands in for the network.
/// </summary>
public class AccountingClientServiceLimitTests
{
    private const string ServiceLimitJson =
        """
        {
          "id": 42,
          "serviceGroup": {"id": 7, "name": "ai-services", "services": ["ai-tools", "ai-search"]},
          "serviceGroupName": "ai-services",
          "customerAccountNumber": 1001,
          "scope": "Custom",
          "participant": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
          "amountValue": 50.5,
          "currency": "USD",
          "quantityValue": 1000,
          "serviceUnit": "request",
          "period": "Day",
          "usageFrom": null,
          "enabled": true,
          "created": "2026-09-01T10:30:00Z",
          "modified": "2026-09-15T08:00:00Z"
        }
        """;

    private const string UsageJson =
        """
        {"periodStart":"2026-10-01T00:00:00Z","periodEnd":"2026-10-31T23:59:59Z","amountConsumed":312.4,"amountAvailable":187.6,"quantityConsumed":1840,"quantityAvailable":null}
        """;

    [Fact]
    public async Task GetServiceLimit_BuildsIdPathAndDeserializesResponse()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, ServiceLimitJson));

        var serviceLimit = await client.GetServiceLimitAsync(42);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/42");

        serviceLimit.Id.Should().Be(42);
        serviceLimit.ServiceGroupName.Should().Be("ai-services");
        serviceLimit.CustomerAccountNumber.Should().Be(1001);
        serviceLimit.Scope.Should().Be(ServiceLimitScope.Custom);
        serviceLimit.Participant.Should().Be("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        serviceLimit.AmountValue.Should().Be(50.5m);
        serviceLimit.Currency.Should().Be("USD");
        serviceLimit.QuantityValue.Should().Be(1000);
        serviceLimit.ServiceUnit.Should().Be("request");
        serviceLimit.Period.Should().Be(ServiceLimitPeriod.Day);
        serviceLimit.UsageFrom.Should().BeNull();
        serviceLimit.Enabled.Should().BeTrue();
        serviceLimit.Created.Should().Be(new DateTime(2026, 9, 1, 10, 30, 0, DateTimeKind.Utc));
        serviceLimit.Modified.Should().Be(new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc));
        serviceLimit.Usage.Should().BeNull();
    }

    [Fact]
    public async Task GetServiceLimit_LifetimeLimitForEveryUser_ReadsScopeAndUsageFrom()
    {
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            """{"id":43,"serviceGroupName":"ai-services","scope":"Global","participant":null,"amountValue":100,"currency":"USD","period":"Lifetime","usageFrom":"2026-10-01T09:12:00Z","enabled":false}"""));

        var serviceLimit = await client.GetServiceLimitAsync(43);

        serviceLimit.Scope.Should().Be(ServiceLimitScope.Global);
        serviceLimit.Participant.Should().BeNull();
        serviceLimit.Period.Should().Be(ServiceLimitPeriod.Lifetime);
        serviceLimit.UsageFrom.Should().Be(new DateTime(2026, 10, 1, 9, 12, 0, DateTimeKind.Utc));
        serviceLimit.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetTotalServiceLimit_BuildsTotalPathAndReadsUsage()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"id":41,"serviceGroupName":"ai-services","scope":"Total","participant":null,"amountValue":500,"currency":"USD","period":"Month","enabled":true,"usage":{{UsageJson}}}"""));

        var serviceLimit = await client.GetTotalServiceLimitAsync("portal-1", "ai-services", includeUsage: true);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/customer/portal-1/ai-services/total");
        AccountingClientTests.ParseQuery(handler.LastUri)["includeUsage"].Should().Be("true");

        serviceLimit.Scope.Should().Be(ServiceLimitScope.Total);
        serviceLimit.Usage.Should().NotBeNull();
        serviceLimit.Usage.PeriodStart.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        serviceLimit.Usage.PeriodEnd.Should().Be(new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc));
        serviceLimit.Usage.AmountConsumed.Should().Be(312.4m);
        serviceLimit.Usage.AmountAvailable.Should().Be(187.6m);
        serviceLimit.Usage.QuantityConsumed.Should().Be(1840);
        serviceLimit.Usage.QuantityAvailable.Should().BeNull();
    }

    [Fact]
    public async Task GetTotalServiceLimit_LifetimeUsage_HasNoPeriodEnd()
    {
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            """{"id":41,"scope":"Total","period":"Lifetime","usageFrom":"2026-10-01T09:12:00Z","usage":{"periodStart":"2026-10-01T09:12:00Z","periodEnd":null,"amountConsumed":5,"amountAvailable":95,"quantityConsumed":0}}"""));

        var serviceLimit = await client.GetTotalServiceLimitAsync("portal-1", "ai-services", includeUsage: true);

        serviceLimit.Usage.PeriodEnd.Should().BeNull();
        serviceLimit.Usage.AmountAvailable.Should().Be(95m);
    }

    [Fact]
    public async Task GetGlobalServiceLimit_BuildsGlobalPath()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            """{"id":43,"serviceGroupName":"ai-services","scope":"Global","amountValue":50,"currency":"USD","period":"Day","enabled":true}"""));

        var serviceLimit = await client.GetGlobalServiceLimitAsync("portal-1", "ai-services");

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/customer/portal-1/ai-services/global");
        serviceLimit.Scope.Should().Be(ServiceLimitScope.Global);
    }

    [Fact]
    public async Task GetParticipantServiceLimits_BuildsPathAndQueryAndDeserializesPage()
    {
        // The limit for every user repeats its ID for each user it applies to.
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""
            {"collection":[
              {"id":42,"scope":"Custom","participant":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","amountValue":200,"period":"Day","enabled":true,"usage":{{UsageJson}}},
              {"id":43,"scope":"Global","participant":"7c9e6679-7425-40de-944b-e07fc1f90ae7","amountValue":50,"period":"Day","enabled":true,"usage":{{UsageJson}}},
              {"id":43,"scope":"Global","participant":"16fd2706-8baf-433b-82eb-8c7fada847da","amountValue":50,"period":"Day","enabled":true,"usage":{{UsageJson}}}
            ],"offset":10,"limit":5,"totalQuantity":13,"totalPage":3,"currentPage":3}
            """));

        var filter = new ServiceLimitFilter
        {
            Offset = 10,
            Limit = 5,
            OrderBy = ServiceLimitOrderBy.Participant,
            OrderType = OperationOrderType.Ascending,
            IncludeUsage = true
        };

        var report = await client.GetParticipantServiceLimitsAsync("portal-1", "ai-services", filter);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/customer/portal-1/ai-services/participants");

        var query = AccountingClientTests.ParseQuery(handler.LastUri);
        query["offset"].Should().Be("10");
        query["limit"].Should().Be("5");
        query["orderBy"].Should().Be("Participant");
        query["orderType"].Should().Be("Ascending");
        query["includeUsage"].Should().Be("true");

        report.Collection.Select(limit => limit.Scope).Should().Equal(ServiceLimitScope.Custom, ServiceLimitScope.Global, ServiceLimitScope.Global);
        report.Collection.Select(limit => limit.Id).Should().Equal(42, 43, 43);
        report.Collection.Should().AllSatisfy(limit => limit.Usage.AmountConsumed.Should().Be(312.4m));
        report.TotalQuantity.Should().Be(13);
        report.CurrentPage.Should().Be(3);
    }

    [Fact]
    public async Task GetParticipantServiceLimits_DefaultFilter_SendsNoQuery()
    {
        // Descending is the accounting default, so it is normalized away together with the unset values.
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, """{"collection":[]}"""));

        await client.GetParticipantServiceLimitsAsync("portal-1", "ai-services", new ServiceLimitFilter { OrderType = OperationOrderType.Descending });

        AccountingClientTests.ParseQuery(handler.LastUri!).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateServiceLimit_PostsBodyWithGroupScopeAndParticipant()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.Created, ServiceLimitJson));

        var serviceLimit = await client.CreateServiceLimitAsync("portal-1", "ai-services", ServiceLimitScope.Custom,
            "3f2504e0-4f89-11d3-9a0c-0305e82c3301", 50.5m, 1000, ServiceLimitPeriod.Day);

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("customerName").GetString().Should().Be("portal-1");
        root.GetProperty("serviceGroupName").GetString().Should().Be("ai-services");
        root.GetProperty("scope").GetString().Should().Be("Custom");
        root.GetProperty("customerParticipantName").GetString().Should().Be("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        root.GetProperty("amountValue").GetDecimal().Should().Be(50.5m);
        root.GetProperty("quantityValue").GetInt32().Should().Be(1000);
        root.GetProperty("period").GetString().Should().Be("Day");

        serviceLimit.Id.Should().Be(42);
    }

    [Fact]
    public async Task CreateServiceLimit_TotalScopeWithOneThreshold_OmitsUnsetFields()
    {
        // The accounting service refuses a participant for a limit of the portal; an absent field is "not set".
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.Created, ServiceLimitJson));

        await client.CreateServiceLimitAsync("portal-1", "ai-services", ServiceLimitScope.Total, null, 500m, null, ServiceLimitPeriod.Month);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("scope").GetString().Should().Be("Total");
        root.TryGetProperty("customerParticipantName", out _).Should().BeFalse();
        root.TryGetProperty("quantityValue", out _).Should().BeFalse();
        root.GetProperty("amountValue").GetDecimal().Should().Be(500m);
        root.GetProperty("period").GetString().Should().Be("Month");
    }

    [Fact]
    public async Task UpdateServiceLimit_PutsBodyWithIdAndOnlyTheChangedFields()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, ServiceLimitJson));

        await client.UpdateServiceLimitAsync(42, null, 2000, false);

        handler.LastMethod.Should().Be(HttpMethod.Put);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("id").GetInt32().Should().Be(42);
        root.GetProperty("quantityValue").GetInt32().Should().Be(2000);
        root.GetProperty("enabled").GetBoolean().Should().BeFalse();
        root.TryGetProperty("amountValue", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SetParticipantServiceLimits_PutsBodyWithGroupAndParticipantsAndReturnsCount()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, "2"));

        var count = await client.SetParticipantServiceLimitsAsync("portal-1", "ai-services",
            ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"], 40m, null, ServiceLimitPeriod.Week);

        handler.LastMethod.Should().Be(HttpMethod.Put);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/participants");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("customerName").GetString().Should().Be("portal-1");
        root.GetProperty("serviceGroupName").GetString().Should().Be("ai-services");
        root.GetProperty("customerParticipantNames").EnumerateArray().Select(name => name.GetString())
            .Should().Equal("3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7");
        root.GetProperty("amountValue").GetDecimal().Should().Be(40m);
        root.TryGetProperty("quantityValue", out _).Should().BeFalse();
        root.GetProperty("period").GetString().Should().Be("Week");

        count.Should().Be(2);
    }

    [Fact]
    public async Task DisableParticipantServiceLimits_PutsToDisablePathAndReturnsCount()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, "5"));

        var count = await client.DisableParticipantServiceLimitsAsync("portal-1", "ai-services");

        handler.LastMethod.Should().Be(HttpMethod.Put);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/customer/portal-1/ai-services/participants/disable");
        count.Should().Be(5);
    }

    [Fact]
    public async Task DeleteServiceLimit_SendsDeleteToIdPath()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.DeleteServiceLimitAsync(42);

        handler.LastMethod.Should().Be(HttpMethod.Delete);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/42");
        handler.CallCount.Should().Be(1);
    }

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

        var balance = await client.GetCustomerBalanceAsync("portal-1");

        balance.AccountNumber.Should().Be(7);
        handler.CallCount.Should().Be(2);
    }
}
