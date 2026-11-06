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
/// What billing, accounting and docscloud clients raise when the call does not end with an answer of the contract:
/// an answer that cannot be read is a <see cref="PaymentServiceContractException"/>, outside the hierarchies the callers
/// handle as a refusal; no answer at all stays in them. Neither names the address of the service nor the portal key.
/// </summary>
public class PaymentServiceContractTests
{
    private const string PortalId = "portal-1";
    private const string ProxyPage = "<html>Bad Gateway</html>";

    public static TheoryData<string> Services => ["Billing", "Accounting", "DocsCloud"];

    public static TheoryData<string> BillingLinks => ["AccountLink", "InvoiceLink", "PaymentUrl"];

    [Theory]
    [MemberData(nameof(Services))]
    public async Task UnreadableAnswer_IsAContractFailure_NamingTheRouteNotTheAddress(string service)
    {
        // An answer that is not the JSON the call expects - an HTML page of a proxy, a changed contract - is no refusal
        // of the service: a caller that turns a refusal into a 402 or drops a backup schedule must not see it.
        using var stub = new HttpStub(_ => Reply(HttpStatusCode.OK, ProxyPage));

        var exception = (await Call(service, stub).Should().ThrowExactlyAsync<PaymentServiceContractException>()).Which;

        exception.Service.Should().Be(service);
        exception.Operation.Should().Be(RouteOf(service));
        exception.StatusCode.Should().Be(HttpStatusCode.OK);
        exception.InnerException.Should().BeAssignableTo<JsonException>();
        ShouldNotNameTheRequest(exception, stub.Url);
    }

    [Theory]
    [MemberData(nameof(BillingLinks))]
    public async Task UnreadableBillingLink_IsAContractFailure(string link)
    {
        // These three calls take the answer as a raw string and read the JSON string in it themselves, so the factory
        // of the client never sees what they cannot read.
        using var stub = new HttpStub(_ => Reply(HttpStatusCode.OK, ProxyPage));
        var (client, _) = CreateBillingClient(stub);

        Func<Task> act = link switch
        {
            "AccountLink" => () => client.GetAccountLinkAsync(PortalId, "https://portal/back"),
            "InvoiceLink" => () => client.GetInvoiceLinkAsync(PortalId, "pi_3QabcDEF"),
            _ => () => client.GetPaymentUrlAsync(PortalId, ["product-1"], backUrl: "https://portal/back")
        };

        var exception = (await act.Should().ThrowExactlyAsync<PaymentServiceContractException>()).Which;

        exception.Service.Should().Be("Billing");
        exception.Operation.Should().StartWith("POST /billing/Get");
        exception.InnerException.Should().BeAssignableTo<JsonException>();
        ShouldNotNameTheRequest(exception, stub.Url);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public async Task NoAnswer_StaysInTheHierarchyOfTheService_NamingTheRouteNotTheAddress(string service)
    {
        // The message reaches the logs and, through an unhandled error, the API client: the route is enough to tell the
        // call, the address of an internal service is not the client's business. The cause stays as the inner exception.
        var stub = HttpStub.Unreachable();

        var exception = (await Call(service, stub).Should().ThrowAsync<Exception>()).Which;

        exception.Should().BeOfType(service switch
        {
            "Billing" => typeof(BillingTransportException),
            "Accounting" => typeof(AccountingException),
            _ => typeof(DocsCloudException)
        });
        exception.InnerException.Should().BeAssignableTo<HttpRequestException>();
        exception.Message.Should().Contain(RouteOf(service));
        ShouldNotNameTheRequest(exception, stub.Url);
    }

    [Fact]
    public void ContractFailure_IsOutsideEveryHierarchyTheCallersCatch()
    {
        typeof(PaymentServiceContractException).Should().NotBeAssignableTo<BillingException>()
            .And.NotBeAssignableTo<AccountingException>()
            .And.NotBeAssignableTo<DocsCloudException>();
    }

    private static Func<Task> Call(string service, HttpStub stub)
    {
        return service switch
        {
            "Billing" => () => CreateBillingClient(stub).client.GetPaymentsAsync(PortalId),
            "Accounting" => () => CreateAccountingClient(stub).client.GetCustomerBalanceAsync(PortalId, TestContext.Current.CancellationToken),
            _ => () => CreateDocsCloudClient(stub).client.GetTenantConfigAsync(PortalId)
        };
    }

    private static string RouteOf(string service)
    {
        return service switch
        {
            "Billing" => "POST /billing/GetPayments",
            "Accounting" => "GET /customer/{portalId}/balance",
            _ => "GET /tenant/config"
        };
    }

    private static void ShouldNotNameTheRequest(Exception exception, string url)
    {
        var address = new Uri(url);

        exception.Message.Should().NotContain(address.Host)
            .And.NotContain(address.Port.ToString(CultureInfo.InvariantCulture))
            .And.NotContain(PortalId);

        if (address.AbsolutePath.Length > 1)
        {
            exception.Message.Should().NotContain(address.AbsolutePath.TrimEnd('/'));
        }
    }
}
