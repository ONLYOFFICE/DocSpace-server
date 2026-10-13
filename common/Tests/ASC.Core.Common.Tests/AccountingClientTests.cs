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
/// Contract tests for the Refit-backed <see cref="AccountingClient"/>: they capture the outgoing
/// <see cref="HttpRequestMessage"/> with a fake primary handler and assert the request line, query string,
/// HMAC authorization header, response error mapping and retry behaviour — without any network access.
/// </summary>
public class AccountingClientTests
{
    private const string BaseUrl = "https://accounting.example.com/api";
    private const string Key = "test-key";
    private const string Secret = "test-secret";

    [Fact]
    public async Task GetCustomerOperations_NonAiService_BuildsExpectedPathAndQuery()
    {
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.OK, "{}"));

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
        var queries = new List<string>();
        var (client, _) = CreateClient(request =>
        {
            queries.Add(request.RequestUri!.Query);
            var offset = int.Parse(ParseQuery(request.RequestUri)["offset"]);

            return Json(HttpStatusCode.OK, OperationsPage(count: Math.Min(2, 5 - offset), totalQuantity: 5));
        });

        var pages = new List<Report>();
        await foreach (var page in client.GetCustomerOperationPages("portal-1", new OperationFilter { Limit = 2 }).AsPages())
        {
            pages.Add(page);
        }

        pages.Select(p => p.Collection.Count).Should().Equal(2, 2, 1);
        queries.Select(q => ParseQuery(new Uri(BaseUrl + q))["offset"]).Should().Equal("0", "2", "4");
        queries.Should().AllSatisfy(q =>
        {
            // The filter's own Offset stays unset, so the page token is the only offset parameter.
            q.TrimStart('?').Split('&').Count(p => p.StartsWith("offset=", StringComparison.Ordinal)).Should().Be(1);
            ParseQuery(new Uri(BaseUrl + q))["limit"].Should().Be("2");
        });
    }

    [Fact]
    public async Task GetCustomerOperationPages_StopsOnEmptyPage_EvenBelowTotalQuantity()
    {
        var calls = 0;
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.OK, OperationsPage(count: ++calls == 1 ? 3 : 0, totalQuantity: 100)));

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
        var (client, _) = CreateClient(_ => ++calls == 1
            ? Json(HttpStatusCode.OK, OperationsPage(count: 2, totalQuantity: 10))
            : Json(HttpStatusCode.BadRequest, "Customer not found"));

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
        var (client, _) = CreateClient(request =>
        {
            paths.Add(request.RequestUri!.AbsolutePath + "?offset=" + ParseQuery(request.RequestUri)["offset"]);
            var offset = int.Parse(ParseQuery(request.RequestUri)["offset"]);

            return Json(HttpStatusCode.OK, $$"""{"collection":[{{string.Join(",", Enumerable.Repeat("{}", Math.Min(2, 3 - offset)))}}],"totalQuantity":3}""");
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
        var (client, _) = CreateClient(request =>
        {
            var offset = ParseQuery(request.RequestUri!)["offset"];
            offsets.Add(offset);

            // Asked for 1000, the service returns at most 100 per page.
            return Json(HttpStatusCode.OK, OperationsPage(count: Math.Min(100, 250 - int.Parse(offset)), totalQuantity: 250));
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
        var (client, _) = CreateClient(_ =>
        {
            calls++;
            return Json(HttpStatusCode.OK, firstPage);
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
        var (client, _) = CreateClient(_ =>
        {
            calls++;
            return Json(HttpStatusCode.OK, OperationsPage(count: 0, totalQuantity: 0));
        });

        var operations = () => client.GetCustomerOperationPages("portal-1", new OperationFilter { Offset = 10 });
        var usage = () => client.GetCustomerServiceUsagePages("portal-1", new UsageFilter { Offset = 10 });

        operations.Should().ThrowExactly<ArgumentException>().WithParameterName("filter");
        usage.Should().ThrowExactly<ArgumentException>().WithParameterName("filter");
        calls.Should().Be(0);
    }

    [Fact]
    public async Task GetCustomerBalance_BuildsBalancePathAndDeserializesResponse()
    {
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.OK,
            """{"accountNumber":42,"accountCurrency":"USD","subAccounts":[{"currency":"USD","amount":1500.75}]}"""));

        var balance = await client.GetCustomerBalanceAsync("portal-1");

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
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.OK, "{}"), baseUrl: "https://accounting.example.com");

        await client.GetCustomerBalanceAsync("portal-1");

        handler.LastUri!.AbsolutePath.Should().Be("/customer/portal-1/balance");
        handler.LastUri.ToString().Should().Be("https://accounting.example.com/customer/portal-1/balance");
    }

    [Fact]
    public async Task TransportFailure_IsMappedToAccountingException()
    {
        // A transport failure never reaches ExceptionFactory, which only sees HTTP responses; the
        // TransportExceptionFactory keeps it inside the AccountingException hierarchy instead of leaking
        // Refit.ApiRequestException to the callers.
        var (client, _) = CreateClient(_ => throw new HttpRequestException("connection refused"));

        var act = async () => await client.GetServiceInfoAsync("backup");

        (await act.Should().ThrowExactlyAsync<AccountingException>())
            .WithInnerException<HttpRequestException>();
    }
    [Fact]
    public async Task Requests_IncludeValidHmacAuthorizationHeader()
    {
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.OK, "{}"));

        await client.GetServiceInfoAsync("backup");

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
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.PaymentRequired, ""));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        await act.Should().ThrowExactlyAsync<AccountingPaymentRequiredException>();
    }

    [Fact]
    public async Task BadRequestNotFoundResponse_ThrowsAccountingCustomerNotFoundException()
    {
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
    }

    [Fact]
    public async Task OtherErrorResponse_ThrowsAccountingExceptionWithStatusAndBody()
    {
        var (client, _) = CreateClient(_ => Json(HttpStatusCode.InternalServerError, "boom"));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        (await act.Should().ThrowExactlyAsync<AccountingException>())
            .Which.Message.Should().Contain("InternalServerError").And.Contain("boom");
    }

    [Fact]
    public async Task GetRequest_RetriesOnTransientError_ThenSucceeds()
    {
        var calls = 0;
        var (client, handler) = CreateClient(_ =>
        {
            calls++;
            return calls == 1
                ? Json(HttpStatusCode.ServiceUnavailable, "")
                : Json(HttpStatusCode.OK, """{"accountNumber":7}""");
        });

        var balance = await client.GetCustomerBalanceAsync("portal-1");

        balance.AccountNumber.Should().Be(7);
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetRequest_IsNotRetried_WhenCustomerNotFound()
    {
        // A 400 "customer not found" on a GET is a definitive result - it must not be retried.
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.BadRequest, "Customer not found"));

        var act = async () => await client.GetCustomerBalanceAsync("portal-1");

        await act.Should().ThrowExactlyAsync<AccountingCustomerNotFoundException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task PostRequest_IsNotRetried_OnTransientError()
    {
        // POST must never be retried, otherwise a non-idempotent operation could be executed twice.
        var (client, handler) = CreateClient(_ => Json(HttpStatusCode.ServiceUnavailable, "unavailable"));

        var act = async () => await client.OpenCustomerSessionAsync("portal-1", "backup", "ref", 1, 10);

        await act.Should().ThrowAsync<AccountingException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task NotConfigured_ThrowsAccountingNotConfiguredException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(typeof(ICache), typeof(AscCache));
        services.AddScoped<AccountingClient>();
        services.AddAccountingHttpClient(configuration);
        services.AddFusionCache();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<AccountingClient>();

        client.Configured.Should().BeFalse();

        var act = async () => await client.GetCustomerBalanceAsync("portal-1");

        await act.Should().ThrowAsync<AccountingNotConfiguredException>();
    }

    private static (AccountingClient client, CapturingHandler handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder, string baseUrl = BaseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:accounting:url"] = baseUrl,
                ["core:accounting:key"] = Key,
                ["core:accounting:secret"] = Secret
            })
            .Build();

        var handler = new CapturingHandler(responder);

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(typeof(ICache), typeof(AscCache));
        services.AddScoped<AccountingClient>();
        services.AddFusionCache();

        services.AddAccountingHttpClient(configuration);

        // Replace the real network handler with our capturing one. AddRefitGeneratedClient sets a primary
        // handler on its own named client, which overrides ConfigureHttpClientDefaults - so the
        // override has to target that exact client by name.
        services.AddHttpClient(Refit.UniqueName.ForType<IAccountingApi>())
                .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<AccountingClient>(), handler);
    }

    private static string OperationsPage(int count, int totalQuantity)
    {
        return $$"""{"collection":[{{string.Join(",", Enumerable.Repeat("""{"service":"backup"}""", count))}}],"totalQuantity":{{totalQuantity}}}""";
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string ExpectedToken(string key, string secret, string timestamp)
    {
        using var hasher = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
        var hash = WebEncoders.Base64UrlEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", timestamp, key))));

        return $"ASC {key}:{timestamp}:{hash}";
    }

    private static Dictionary<string, string> ParseQuery(Uri uri)
    {
        var result = new Dictionary<string, string>();
        var query = uri.Query.TrimStart('?');

        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var name = Uri.UnescapeDataString(idx < 0 ? pair : pair[..idx]);
            var value = idx < 0 ? "" : Uri.UnescapeDataString(pair[(idx + 1)..]);
            result[name] = value;
        }

        return result;
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public Uri? LastUri { get; private set; }
        public HttpMethod? LastMethod { get; private set; }
        public string? LastAuthorization { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastUri = request.RequestUri;
            LastMethod = request.Method;
            LastAuthorization = request.Headers.Contains("Authorization")
                ? request.Headers.GetValues("Authorization").First()
                : null;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = responder(request);

            // Mimic the real primary handler so the resilience pipeline can inspect the request method.
            response.RequestMessage = request;

            return response;
        }
    }
}
