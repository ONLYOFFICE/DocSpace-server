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
/// Contract tests for the Refit-backed <see cref="AccountingClient"/>: the client talks to an <see cref="HttpStub"/>
/// over loopback, and the tests assert the request line, query string, HMAC authorization header, response error
/// mapping and retry behaviour. The paged reads are in <see cref="AccountingClientOperationsTests"/>.
/// </summary>
public class AccountingClientTests
{
    [Fact]
    public async Task GetServicePrices_ResolvesTheServiceThenBuildsByServicePath()
    {
        var paths = new List<string>();
        var (client, _) = CreateAccountingClient(request =>
        {
            paths.Add(request.Path);
            return request.Path.Contains("/servicePrice/")
                ? Reply(HttpStatusCode.OK, """[{"id":12,"serviceId":7,"timeUnit":"Minute","servicePrice":0.5}]""")
                : Reply(HttpStatusCode.OK, """{"id":7,"name":"ai-tools"}""");
        });

        var prices = await client.GetServicePricesAsync("ai-tools", active: true);

        paths.Should().Equal("/api/service/by-name/ai-tools", "/api/servicePrice/by-service/7");
        prices.Should().ContainSingle().Which.TimeUnit.Should().Be(PriceTimeUnit.Minute);
    }

    [Fact]
    public async Task CompleteCustomerSession_PostsToSessionCompletePath()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, ""));

        await client.CompleteCustomerSessionAsync("portal-1", "backup", 42, 3, "3f2504e0-4f89-11d3-9a0c-0305e82c3301");

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.AbsolutePath.Should().Be("/api/session/complete");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        body.RootElement.GetProperty("sessionId").GetInt32().Should().Be(42);
        body.RootElement.GetProperty("customerName").GetString().Should().Be("portal-1");
    }

    [Fact]
    public async Task GetProductPriceInfo_SendsNamesInOneRequest_AndLeavesUnknownServicesOut()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, ServicesPage(totalQuantity: 2,
            ("backup", 10m, "USD"), ("disk-storage", 0.14m, "EUR"))));

        var prices = await client.GetProductPriceInfoAsync(["backup", "disk-storage", "unknown"]);

        handler.CallCount.Should().Be(1);
        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/service");

        var query = ParseQuery(handler.LastUri);
        query["status"].Should().Be("Approved");
        query["limit"].Should().Be("100");
        query["offset"].Should().Be("0");
        QueryValues(handler.LastUri, "names").Should().BeEquivalentTo("backup", "disk-storage", "unknown");

        prices.Keys.Should().BeEquivalentTo("backup", "disk-storage");
        prices["backup"].Should().Equal(new Dictionary<string, decimal> { ["USD"] = 10m });
        prices["disk-storage"].Should().Equal(new Dictionary<string, decimal> { ["EUR"] = 0.14m });
    }

    [Fact]
    public async Task GetProductPriceInfo_ServiceIgnoringNamesFilter_ReturnsOnlyRequestedServices()
    {
        // An accounting service without the names filter answers with every approved service.
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, ServicesPage(totalQuantity: 3,
            ("backup", 10m, "USD"), ("ai-tools", 0m, "USD"), ("disk-storage", 0.14m, "USD"))));

        var prices = await client.GetProductPriceInfoAsync(["ai-tools"]);

        prices.Keys.Should().Equal("ai-tools");
        prices["ai-tools"].Should().Equal(new Dictionary<string, decimal> { ["USD"] = 0m });
    }

    [Fact]
    public async Task GetProductPriceInfo_NoNames_SendsNoRequest()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, ServicesPage(totalQuantity: 0)));

        var prices = await client.GetProductPriceInfoAsync([]);

        prices.Should().BeEmpty();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProductPriceInfo_AnswerLargerThanPage_ReadsEveryPage()
    {
        var offsets = new List<string>();
        var (client, _) = CreateAccountingClient(request =>
        {
            var offset = ParseQuery(request.Uri)["offset"];
            offsets.Add(offset);

            // 150 services: the first page holds 100 of them, the second the remaining 50.
            var services = Enumerable.Range(int.Parse(offset), offset == "0" ? 100 : 50)
                .Select(i => ($"service-{i}", (decimal)i, "USD"))
                .ToArray();

            return Reply(HttpStatusCode.OK, ServicesPage(150, services));
        });

        var prices = await client.GetProductPriceInfoAsync(["service-0", "service-149"]);

        offsets.Should().Equal("0", "100");
        prices["service-149"].Should().Equal(new Dictionary<string, decimal> { ["USD"] = 149m });
    }

    [Fact]
    public async Task GetCustomerBalance_BuildsBalancePathAndDeserializesResponse()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK,
            """{"accountNumber":42,"accountCurrency":"USD","subAccounts":[{"currency":"USD","amount":1500.75}]}"""));

        var balance = await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        handler.LastMethod.Should().Be(HttpMethod.Get);
        handler.LastUri!.AbsolutePath.Should().Be("/api/customer/portal-1/balance");

        balance.AccountNumber.Should().Be(42);
        balance.AccountCurrency.Should().Be("USD");
        balance.SubAccounts.Should().ContainSingle();
        balance.SubAccounts[0].Amount.Should().Be(1500.75m);
    }

    [Fact]
    public async Task RootBaseUrlWithoutSubPath_ProducesSingleSlashPath()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, "{}"), path: "");

        await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        handler.LastUri!.AbsolutePath.Should().Be("/customer/portal-1/balance");
    }

    [Fact]
    public async Task TransportFailure_IsMappedToAccountingException()
    {
        // A transport failure never reaches ExceptionFactory, which only sees HTTP responses; the
        // TransportExceptionFactory keeps it inside the AccountingException hierarchy instead of leaking
        // Refit.ApiRequestException to the callers.
        var (client, _) = CreateAccountingClient(HttpStub.Unreachable());

        var act = async () => await client.GetAllCurrenciesAsync();

        (await act.Should().ThrowExactlyAsync<AccountingException>())
            .WithInnerException<HttpRequestException>();
    }

    [Fact]
    public async Task UnreadableAnswer_IsMappedToAccountingException()
    {
        // An answer that is not the JSON the call expects must stay inside the hierarchy the callers catch.
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));

        var act = async () => await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        (await act.Should().ThrowExactlyAsync<AccountingException>())
            .WithInnerException<JsonException>();
    }

    [Fact]
    public async Task Requests_IncludeValidHmacAuthorizationHeader()
    {
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.OK, "[]"));

        await client.GetAllCurrenciesAsync();

        handler.LastAuthorization.Should().NotBeNull();

        var token = handler.LastAuthorization!;
        token.Should().StartWith($"ASC {Key}:");

        var parts = token["ASC ".Length..].Split(':');
        parts.Should().HaveCount(3);
        parts[0].Should().Be(Key);

        var timestamp = parts[1];
        timestamp.Should().MatchRegex(@"^\d{14}$");

        // The signature must be the HMAC-SHA1 of "{timestamp}\n{key}" keyed by the secret.
        token.Should().Be(ExpectedToken(Key, Secret, timestamp));
    }

    [Fact]
    public async Task PaymentRequiredResponse_ThrowsAccountingPaymentRequiredException()
    {
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.PaymentRequired, ""));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        await act.Should().ThrowExactlyAsync<AccountingPaymentRequiredException>();
    }

    [Fact]
    public async Task BadRequestNotFoundResponse_ThrowsAccountingCustomerNotFoundException()
    {
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
    }

    [Fact]
    public async Task OtherErrorResponse_ThrowsAccountingExceptionWithStatusAndBody()
    {
        var (client, _) = CreateAccountingClient(_ => Reply(HttpStatusCode.InternalServerError, "boom"));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        (await act.Should().ThrowExactlyAsync<AccountingException>())
            .Which.Message.Should().Contain("InternalServerError").And.Contain("boom");
    }

    [Fact]
    public async Task GetRequest_RetriesOnTransientError_ThenSucceeds()
    {
        var calls = 0;
        var (client, handler) = CreateAccountingClient(_ =>
        {
            calls++;
            return calls == 1
                ? Reply(HttpStatusCode.ServiceUnavailable, "")
                : Reply(HttpStatusCode.OK, """{"accountNumber":7}""");
        });

        var balance = await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        balance.AccountNumber.Should().Be(7);
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetRequest_IsNotRetried_WhenCustomerNotFound()
    {
        // A 400 "customer not found" on a GET is a definitive result - it must not be retried.
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () => await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task PostRequest_IsNotRetried_OnTransientError()
    {
        // POST must never be retried, otherwise a non-idempotent operation could be executed twice.
        var (client, handler) = CreateAccountingClient(_ => Reply(HttpStatusCode.ServiceUnavailable, "unavailable"));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        await act.Should().ThrowAsync<AccountingException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRequest_IsNotRetried_OnTransportFailure()
    {
        // Only a non-success answer is retried; a request that got no answer at all fails at once, so a caller that
        // counts failures (the retention job) sees each one as a single request.
        var (client, handler) = CreateAccountingClient(_ => TransportFault());

        var act = async () => await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        (await act.Should().ThrowExactlyAsync<AccountingException>())
            .WithInnerException<HttpRequestException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task NotConfigured_ThrowsAccountingNotConfiguredException()
    {
        var client = BuildProvider().GetRequiredService<AccountingClient>();

        client.Configured.Should().BeFalse();

        var act = async () => await client.GetCustomerBalanceAsync("portal-1", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AccountingNotConfiguredException>();
    }

    private static string ServicesPage(int totalQuantity, params (string Name, decimal Price, string Currency)[] services)
    {
        var items = services.Select(s =>
            $$$"""{"name":"{{{s.Name}}}","priceValue":{{{s.Price.ToString(CultureInfo.InvariantCulture)}}},"currency":{"code":"{{{s.Currency}}}"}}""");

        return $$"""{"collection":[{{string.Join(",", items)}}],"totalQuantity":{{totalQuantity}}}""";
    }
}
