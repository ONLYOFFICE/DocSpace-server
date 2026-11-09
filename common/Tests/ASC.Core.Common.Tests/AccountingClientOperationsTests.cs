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
/// Contract tests for the paged reads of <see cref="AccountingClient"/> - the customer operations and service usage:
/// the query of each page, how the offset advances, and when the paging stops. The client talks to an
/// <see cref="HttpStub"/> over loopback.
/// </summary>
public class AccountingClientOperationsTests
{
    [Fact]
    public async Task GetCustomerOperations_NonAiService_BuildsExpectedPathAndQuery()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, "{}"));

        var filter = new OperationFilter
        {
            ServiceName = ["backup"],
            UtcStartDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            UtcEndDate = new DateTime(2024, 2, 20, 8, 0, 0, DateTimeKind.Utc),
            ParticipantName = "  participant  ",
            Credit = true,
            Debit = false,
            Offset = 5,
            Limit = 50,
            Type = OperationType.Deposit,
            Status = OperationStatus.Completed,
            OrderBy = "  date  ",
            OrderType = OperationOrderType.Ascending
        };

        await client.GetCustomerOperationsAsync("portal-1", filter);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/customer/portal-1/operations");

        var query = ParseQuery(handler.LastUri);
        query["startDate"].Should().Be("2024-01-15T10:30:00.0000000Z");
        query["endDate"].Should().Be("2024-02-20T08:00:00.0000000Z");
        query["participantName"].Should().Be("participant");
        query["credit"].Should().Be("true");
        query["debit"].Should().Be("false");
        query["offset"].Should().Be("5");
        query["limit"].Should().Be("50");
        query["types"].Should().Be("Deposit");
        query["status"].Should().Be("Completed");
        query["orderBy"].Should().Be("date");
        query["orderType"].Should().Be("Ascending");
        query["serviceName"].Should().Be("backup");
    }

    [Fact]
    public async Task GetCustomerOperationPages_AdvancesOffsetByItemsUntilTotalQuantity()
    {
        var requests = new List<Uri>();
        var (client, _) = CreateAccountingClient(request =>
        {
            requests.Add(request.Uri);
            var offset = int.Parse(ParseQuery(request.Uri)["offset"]);

            return Reply(HttpStatusCode.OK, OperationsPage(count: Math.Min(2, 5 - offset), totalQuantity: 5));
        });

        var pages = new List<Report>();
        await foreach (var page in client.GetCustomerOperationPages("portal-1", new OperationFilter { Limit = 2 }).AsPages())
        {
            pages.Add(page);
        }

        pages.Select(p => p.Collection.Count).Should().Equal(2, 2, 1);
        requests.Select(uri => ParseQuery(uri)["offset"]).Should().Equal("0", "2", "4");
        requests.Should().AllSatisfy(uri =>
        {
            // The filter's own Offset stays unset, so the page token is the only offset parameter.
            QueryValues(uri, "offset").Should().ContainSingle();
            ParseQuery(uri)["limit"].Should().Be("2");
        });
    }

    [Fact]
    public async Task GetCustomerOperationPages_StopsOnEmptyPage_EvenBelowTotalQuantity()
    {
        var calls = 0;
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, OperationsPage(count: ++calls == 1 ? 3 : 0, totalQuantity: 100)));

        var items = 0;
        await foreach (var page in client.GetCustomerOperationPages("portal-1", new OperationFilter { Limit = 3 }).AsPages())
        {
            items += page.Collection.Count;
        }

        items.Should().Be(3);
        calls.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomerOperationPages_ErrorOnLaterPage_IsMappedByExceptionFactory()
    {
        var calls = 0;
        var (client, _) = CreateAccountingClient(_ => ++calls == 1
            ? Reply(HttpStatusCode.OK, OperationsPage(count: 2, totalQuantity: 10))
            : Reply(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () =>
        {
            await foreach (var _ in client.GetCustomerOperationPages("portal-1", new OperationFilter { Limit = 2 }).AsPages())
            {
            }
        };

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
        calls.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomerServiceUsagePages_PagesTheUsagePath()
    {
        var paths = new List<string>();
        var (client, _) = CreateAccountingClient(request =>
        {
            paths.Add(request.Path + "?offset=" + ParseQuery(request.Uri)["offset"]);
            var offset = int.Parse(ParseQuery(request.Uri)["offset"]);

            return Reply(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", Enumerable.Repeat("{}", Math.Min(2, 3 - offset)))}}],"totalQuantity":3}""");
        });

        var items = 0;
        await foreach (var page in client.GetCustomerServiceUsagePages("portal-1", new UsageFilter { Limit = 2 }).AsPages())
        {
            items += page.Collection.Count;
        }

        items.Should().Be(3);
        paths.Should().Equal("/api/customer/portal-1/usage?offset=0", "/api/customer/portal-1/usage?offset=2");
    }

    [Fact]
    public async Task GetCustomerOperationPages_ServerCapsPageSize_AdvancesByItemsReceived()
    {
        var offsets = new List<string>();
        var (client, _) = CreateAccountingClient(request =>
        {
            var offset = ParseQuery(request.Uri)["offset"];
            offsets.Add(offset);

            // Asked for 1000, the service returns at most 100 per page.
            return Reply(HttpStatusCode.OK, OperationsPage(count: Math.Min(100, 250 - int.Parse(offset)), totalQuantity: 250));
        });

        var items = 0;
        await foreach (var page in client.GetCustomerOperationPages("portal-1", new OperationFilter { Limit = 1000 }).AsPages())
        {
            items += page.Collection.Count;
        }

        items.Should().Be(250);
        offsets.Should().Equal("0", "100", "200");
    }

    // The paging relies on totalQuantity: without it a full first page is all a report gets. This pins that
    // dependency, so a change of the accounting contract shows up here rather than as a truncated report.
    [Theory]
    [InlineData("""{"collection":[{},{}],"totalQuantity":0}""")]
    [InlineData("""{"collection":[{},{}]}""")]
    public async Task GetCustomerOperationPages_StopsAfterFirstPage_WhenTotalQuantityIsZeroOrMissing(string firstPage)
    {
        var calls = 0;
        var (client, _) = CreateAccountingClient(_ =>
        {
            calls++;
            return Reply(HttpStatusCode.OK, firstPage);
        });

        var items = 0;
        await foreach (var page in client.GetCustomerOperationPages("portal-1", new OperationFilter { Limit = 2 }).AsPages())
        {
            items += page.Collection.Count;
        }

        items.Should().Be(2);
        calls.Should().Be(1);
    }

    [Fact]
    public void GetCustomerOperationPages_FilterOffsetSet_ThrowsBeforeAnyRequest()
    {
        var calls = 0;
        var (client, _) = CreateAccountingClient(_ =>
        {
            calls++;
            return Reply(HttpStatusCode.OK, OperationsPage(count: 0, totalQuantity: 0));
        });

        var operations = () => client.GetCustomerOperationPages("portal-1", new OperationFilter { Offset = 10 });
        var usage = () => client.GetCustomerServiceUsagePages("portal-1", new UsageFilter { Offset = 10 });

        operations.Should().ThrowExactly<ArgumentException>().WithParameterName("filter");
        usage.Should().ThrowExactly<ArgumentException>().WithParameterName("filter");
        calls.Should().Be(0);
    }

    private static string OperationsPage(int count, int totalQuantity)
    {
        return $$"""{"collection":[{{string.Join(",", Enumerable.Repeat("""{"service":"backup"}""", count))}}],"totalQuantity":{{totalQuantity}}}""";
    }
}
