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
/// Contract tests for the balance top-ups of <see cref="AccountingClient"/>: the request line and query of
/// <c>customer/{portalId}/deposits</c>, the deposit model read from the answer, and the paging over it.
/// </summary>
public class AccountingClientDepositTests
{
    [Fact]
    public async Task GetCustomerDeposits_BuildsDepositsPathAndQuery_AndReadsTheDeposit()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => AccountingClientTests.Json(HttpStatusCode.OK,
            """{"collection":[{"id":15342,"date":"2026-10-23T11:13:53Z","sum":40.0,"currency":"USD","status":"Completed","bankName":"Stripe","paymentRef":"pi_3QabcDEF","metadata":{"details":"Top-up"}}],"offset":5,"limit":10,"totalQuantity":6,"totalPage":1,"currentPage":1}"""));

        var filter = new DepositFilter
        {
            UtcStartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UtcEndDate = new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc),
            Status = OperationStatus.Completed,
            Offset = 5,
            Limit = 10
        };

        var report = await client.GetCustomerDepositsAsync("portal-1", filter);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/customer/portal-1/deposits");

        var query = AccountingClientTests.ParseQuery(handler.LastUri);
        query["startDate"].Should().Be("2026-01-01T00:00:00.0000000Z");
        query["endDate"].Should().Be("2026-10-31T23:59:59.0000000Z");
        query["status"].Should().Be("Completed");
        query["offset"].Should().Be("5");
        query["limit"].Should().Be("10");

        report.TotalQuantity.Should().Be(6);
        var deposit = report.Collection.Should().ContainSingle().Subject;
        deposit.Id.Should().Be(15342);
        deposit.Date.Should().Be(new DateTime(2026, 10, 23, 11, 13, 53, DateTimeKind.Utc));
        deposit.Sum.Should().Be(40m);
        deposit.Currency.Should().Be("USD");
        deposit.Status.Should().Be(OperationStatus.Completed);
        deposit.BankName.Should().Be("Stripe");
        deposit.PaymentRef.Should().Be("pi_3QabcDEF");
        deposit.Metadata.Should().Equal(new Dictionary<string, string> { ["details"] = "Top-up" });
        deposit.HasInvoice.Should().BeTrue();
    }

    [Fact]
    public async Task GetCustomerDepositPages_AdvancesOffsetByItemsUntilTotalQuantity()
    {
        var requests = new List<Uri>();
        var (client, _) = AccountingClientTests.CreateClient(request =>
        {
            requests.Add(request.RequestUri!);
            var offset = int.Parse(AccountingClientTests.ParseQuery(request.RequestUri!)["offset"]);

            return DepositsPage(Enumerable.Range(offset + 1, Math.Min(2, 5 - offset)), totalQuantity: 5);
        });

        var ids = new List<long>();
        await foreach (var deposit in client.GetCustomerDepositPages("portal-1", new DepositFilter { Limit = 2 }).WithCancellation(TestContext.Current.CancellationToken))
        {
            ids.Add(deposit.Id);
        }

        ids.Should().Equal(1, 2, 3, 4, 5);
        requests.Should().AllSatisfy(uri => uri.AbsolutePath.Should().Be("/api/customer/portal-1/deposits"));
        requests.Select(uri => AccountingClientTests.ParseQuery(uri)["offset"]).Should().Equal("0", "2", "4");
        requests.Should().AllSatisfy(uri => AccountingClientTests.ParseQuery(uri)["limit"].Should().Be("2"));
    }

    [Fact]
    public void GetCustomerDepositPages_FilterOffsetSet_ThrowsBeforeAnyRequest()
    {
        var (client, handler) = AccountingClientTests.CreateClient(_ => DepositsPage([], totalQuantity: 0));

        var act = () => client.GetCustomerDepositPages("portal-1", new DepositFilter { Offset = 10 });

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("filter");
        handler.CallCount.Should().Be(0);
    }

    // Only a Stripe payment intent or invoice has an invoice behind it; the prefixes are matched case-sensitively, as
    // Stripe writes them.
    [Theory]
    [InlineData("pi_3QabcDEF", true)]
    [InlineData("in_1PxyzGHI", true)]
    [InlineData("ch_3QabcDEF", false)]
    [InlineData("PI_3QabcDEF", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasInvoice_DependsOnThePaymentReference(string? paymentRef, bool expected)
    {
        var deposit = new Deposit { PaymentRef = paymentRef! };

        deposit.HasInvoice.Should().Be(expected);
    }

    internal static HttpResponseMessage DepositsPage(IEnumerable<int> ids, int totalQuantity)
    {
        var items = ids.Select(id => $$"""{"id":{{id}},"sum":40.0,"currency":"USD","status":"Completed","paymentRef":"pi_{{id}}"}""");

        return AccountingClientTests.Json(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", items)}}],"totalQuantity":{{totalQuantity}}}""");
    }
}
