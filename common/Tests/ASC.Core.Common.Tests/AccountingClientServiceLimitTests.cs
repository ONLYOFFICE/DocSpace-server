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
/// string and JSON body sent to the accounting service, the mapping of its answers, and the retry rule that
/// treats a missing service limit as a definitive 404. The fake primary handler of
/// <see cref="AccountingClientTests"/> stands in for the network.
/// </summary>
public class AccountingClientServiceLimitTests
{
    private const string ServiceLimitJson =
        """
        {
          "id": 42,
          "service": {"id": 7, "name": "ai-tools", "provider": {"id": 3, "name": "provider"}},
          "serviceName": "ai-tools",
          "customerAccountNumber": 1001,
          "participant": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
          "amountValue": 50.5,
          "currency": "USD",
          "quantityValue": 1000,
          "serviceUnit": "request",
          "period": "Day",
          "enabled": true,
          "created": "2026-09-01T10:30:00Z",
          "modified": "2026-09-15T08:00:00Z"
        }
        """;

    [Fact]
    public async Task GetServiceLimit_BuildsIdPathAndDeserializesResponse()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, ServiceLimitJson));

        var serviceLimit = await client.GetServiceLimitAsync(42);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/42");

        serviceLimit.Id.Should().Be(42);
        serviceLimit.ServiceName.Should().Be("ai-tools");
        serviceLimit.CustomerAccountNumber.Should().Be(1001);
        serviceLimit.Participant.Should().Be("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        serviceLimit.AmountValue.Should().Be(50.5m);
        serviceLimit.Currency.Should().Be("USD");
        serviceLimit.QuantityValue.Should().Be(1000);
        serviceLimit.ServiceUnit.Should().Be("request");
        serviceLimit.Period.Should().Be(ServiceLimitPeriod.Day);
        serviceLimit.Enabled.Should().BeTrue();
        serviceLimit.Created.Should().Be(new DateTime(2026, 9, 1, 10, 30, 0, DateTimeKind.Utc));
        serviceLimit.Modified.Should().Be(new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetServiceLimit_PortalWideLimit_HasNoParticipantAndNoQuantityThreshold()
    {
        var (client, _) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            """{"id":43,"serviceName":"ai-tools","participant":null,"amountValue":100,"currency":"USD","quantityValue":null,"serviceUnit":null,"period":"Month","enabled":false}"""));

        var serviceLimit = await client.GetServiceLimitAsync(43);

        serviceLimit.Participant.Should().BeNull();
        serviceLimit.QuantityValue.Should().BeNull();
        serviceLimit.ServiceUnit.Should().BeNull();
        serviceLimit.Period.Should().Be(ServiceLimitPeriod.Month);
        serviceLimit.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetCustomerServiceLimit_BuildsCustomerPath()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, ServiceLimitJson));

        var serviceLimit = await client.GetCustomerServiceLimitAsync("portal-1", "ai-tools");

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/customer/portal-1/ai-tools");
        serviceLimit.Id.Should().Be(42);
    }

    [Fact]
    public async Task GetParticipantServiceLimits_BuildsPathAndQueryAndDeserializesPage()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            $$"""{"collection":[{{ServiceLimitJson}}],"offset":10,"limit":5,"totalQuantity":11,"totalPage":3,"currentPage":3}"""));

        var filter = new ServiceLimitFilter
        {
            Offset = 10,
            Limit = 5,
            OrderBy = ServiceLimitOrderBy.Created,
            OrderType = OperationOrderType.Ascending
        };

        var report = await client.GetParticipantServiceLimitsAsync("portal-1", "ai-tools", filter);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit/customer/portal-1/ai-tools/participants");

        var query = AccountingClientTests.ParseQuery(handler.LastUri);
        query["offset"].Should().Be("10");
        query["limit"].Should().Be("5");
        query["orderBy"].Should().Be("Created");
        query["orderType"].Should().Be("Ascending");

        report.Collection.Should().ContainSingle().Which.Id.Should().Be(42);
        report.Offset.Should().Be(10);
        report.Limit.Should().Be(5);
        report.TotalQuantity.Should().Be(11);
        report.TotalPage.Should().Be(3);
        report.CurrentPage.Should().Be(3);
    }

    [Fact]
    public async Task GetParticipantServiceLimits_DefaultFilter_SendsNoQuery()
    {
        // Descending is the accounting default, so it is normalized away together with the unset values.
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, """{"collection":[]}"""));

        await client.GetParticipantServiceLimitsAsync("portal-1", "ai-tools", new ServiceLimitFilter { OrderType = OperationOrderType.Descending });

        AccountingClientTests.ParseQuery(handler.LastUri!).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateServiceLimit_PostsBodyWithCustomerAndParticipant()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, ServiceLimitJson));

        var serviceLimit = await client.CreateServiceLimitAsync("portal-1", "ai-tools", "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
            50.5m, 1000, ServiceLimitPeriod.Day);

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.AbsolutePath.Should().Be("/api/serviceLimit");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("customerName").GetString().Should().Be("portal-1");
        root.GetProperty("serviceName").GetString().Should().Be("ai-tools");
        root.GetProperty("customerParticipantName").GetString().Should().Be("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
        root.GetProperty("amountValue").GetDecimal().Should().Be(50.5m);
        root.GetProperty("quantityValue").GetInt32().Should().Be(1000);
        root.GetProperty("period").GetString().Should().Be("Day");

        serviceLimit.Id.Should().Be(42);
    }

    [Fact]
    public async Task CreateServiceLimit_PortalWideLimitWithOneThreshold_OmitsUnsetFields()
    {
        // The accounting service reads an absent field as "not set"; sending explicit nulls is not part of the contract.
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK, ServiceLimitJson));

        await client.CreateServiceLimitAsync("portal-1", "ai-tools", null, 100m, null, ServiceLimitPeriod.Month);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.TryGetProperty("customerParticipantName", out _).Should().BeFalse();
        root.TryGetProperty("quantityValue", out _).Should().BeFalse();
        root.GetProperty("amountValue").GetDecimal().Should().Be(100m);
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
    public async Task GetCustomerServiceLimit_NoLimitSet_ThrowsNotFoundWithoutRetry()
    {
        // The accounting service answers 404 when the customer has no limit on the service. That is a definitive
        // result, so the GET must not be retried.
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.NotFound,
            """{"title":"Resource not found","status":404,"detail":"Customer 'portal-1' has no limit on service 'ai-tools'"}"""));

        var act = async () => await client.GetCustomerServiceLimitAsync("portal-1", "ai-tools");

        (await act.Should().ThrowExactlyAsync<AccountingNotFoundException>())
            .Which.Message.Should().Contain("has no limit on service");
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
