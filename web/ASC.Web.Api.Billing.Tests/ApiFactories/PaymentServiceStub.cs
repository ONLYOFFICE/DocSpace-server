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

namespace ASC.Web.Api.Billing.Tests.ApiFactories;

/// <summary>
/// The billing, accounting and docscloud services of the <c>integration-test-billing</c> stack: one WireMock on the
/// fixed address the AppHost points the services at (<see cref="Constants.PaymentStubPort"/>), each service under a
/// path of its own.
/// </summary>
/// <remarks>
/// <para>
/// The stack is SaaS, so every portal reaches the services with a payment key of its own,
/// <see cref="Constants.PaymentStubRegion"/> followed by its tenant id. A test scripts what the services say about
/// its own portal through <see cref="For"/>; the other portals - and anything a test does not script - get the
/// answers of a portal the payment services know nothing about: no payments, so the free startup quota the stack
/// falls back to, no customer, no prices.
/// </para>
/// <para>
/// The server has to answer before the stack starts: the services ask billing about a portal as soon as it is
/// registered, and a refused connection would leave a tariff in the cache the test could not reason about.
/// </para>
/// <para>
/// Every request is recorded the moment it arrives, before the answer goes out: WireMock adds its own log entry only
/// after the answer, so a test that has just seen the service's reply could otherwise miss the request.
/// </para>
/// </remarks>
public sealed class PaymentServiceStub : IDisposable
{
    private static readonly Regex _portalInPath = new(@"/(?:customer|by-customer)/(?<portal>[^/]+)", RegexOptions.Compiled);

    /// <summary>Where the checkout addresses of the stub lead; the query names the products and the portal.</summary>
    public const string CheckoutUrl = "https://checkout.example.com/pay";

    /// <summary>The wallet service the accounting catalogue knows: <c>/service/by-name/ai-tools</c> and its price.</summary>
    public const string CatalogueServiceName = "ai-tools";
    public const int CatalogueServiceId = 7;
    public const decimal CataloguePrice = 0.5m;

    // CatalogueServiceId as it appears in a route; a pattern needs a constant.
    private const string CatalogueServiceIdText = "7";

    private readonly WireMockServer _server;
    private readonly ConcurrentDictionary<string, PortalPayments> _portals = new();
    private readonly ConcurrentQueue<PaymentServiceCall> _calls = new();
    private readonly ServiceLimitStore _serviceLimits = new();

    private PaymentServiceStub()
    {
        var url = $"http://{Constants.PaymentStubHost}:{Constants.PaymentStubPort}";

        try
        {
            _server = WireMockServer.Start(new WireMockServerSettings { Urls = [url] });
        }
        catch (Exception exception)
        {
            // The port is fixed because the AppHost writes it into the services' configuration before the stack starts.
            // WireMock's own message does not say which port it wanted or where that comes from.
            throw new InvalidOperationException(
                $"The payment services stub could not listen on {url} (Constants.PaymentStubPort). Another billing test " +
                "run, or a test process still shutting down, may hold the port.", exception);
        }

        _server
            .Given(Request.Create().WithPath(new WildcardMatcher("/*")).UsingAnyMethod())
            .RespondWith(Response.Create().WithCallback(Handle));
    }

    public static PaymentServiceStub Start()
    {
        return new PaymentServiceStub();
    }

    /// <summary>The payment key the services send for the portal of <paramref name="tenantId"/>.</summary>
    public static string PortalIdOf(int tenantId)
    {
        return Constants.PaymentStubRegion + tenantId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>What the payment services say about the portal of <paramref name="tenantId"/>.</summary>
    public PortalPayments For(int tenantId)
    {
        return _portals.GetOrAdd(PortalIdOf(tenantId), portalId => new PortalPayments(portalId));
    }

    /// <summary>The requests the services made about the portal of <paramref name="tenantId"/>, in the order they arrived.</summary>
    public IReadOnlyList<PaymentServiceCall> CallsFor(int tenantId)
    {
        var portalId = PortalIdOf(tenantId);

        return [.. _calls.Where(call => call.PortalId == portalId)];
    }

    /// <summary>
    /// Puts a spending limit on the AI services on the account <paramref name="accountNumber"/>, as if another portal
    /// had created it; returns its id.
    /// </summary>
    public int AddServiceLimit(int accountNumber, string scope = "Total", decimal amountValue = 100m)
    {
        return _serviceLimits.Add(accountNumber, scope, amountValue);
    }

    /// <summary>
    /// Whether accounting still holds the spending limit <paramref name="id"/>. The requests by id name no portal and
    /// are missing from <see cref="CallsFor"/>, so this is how a test sees that a limit was or was not deleted.
    /// </summary>
    public bool HasServiceLimit(int id)
    {
        return _serviceLimits.Contains(id);
    }

    /// <summary>
    /// The spending limit <paramref name="id"/> is deleted by somebody else just before the next request that changes or
    /// deletes it reaches accounting - the race between the stack's own check that the limit is there and the change.
    /// </summary>
    public void ServiceLimitGoneBeforeNextChange(int id)
    {
        _serviceLimits.GoneBeforeNextChange(id);
    }

    /// <summary>
    /// The spending limits accounting holds on the account <paramref name="accountNumber"/>; equal before and after a
    /// refused request when the refusal wrote nothing.
    /// </summary>
    public string ServiceLimitsOf(int accountNumber)
    {
        return _serviceLimits.Snapshot(accountNumber);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    private IResponseMessage Handle(IRequestMessage message)
    {
        var call = PaymentServiceCall.From(message);
        _calls.Enqueue(call);

        if (!IsSigned(call.Service, Header(message, "Authorization")))
        {
            return PaymentServiceAnswer.Json(HttpStatusCode.Unauthorized, """{"title":"Unauthorized","status":401}""").ToResponse();
        }

        var answer = call.PortalId is not null && _portals.TryGetValue(call.PortalId, out var portal)
            ? portal.AnswerFor(call)
            : null;

        answer ??= _serviceLimits.TryAnswer(call, portalId => _portals.GetValueOrDefault(portalId));

        return (answer ?? DefaultAnswer(call)).ToResponse();
    }

    /// <summary>
    /// Whether <paramref name="authorization"/> is how the client of <paramref name="service"/> signs a request with the
    /// key and the secret the stack is given (<see cref="Constants.PaymentStubKey"/>, <see cref="Constants.PaymentStubSecret"/>):
    /// <c>ASC {key}:{timestamp}:{hash}</c>, the hash an HMAC-SHA1 of the timestamp and the key on two lines under the
    /// secret, in Base64Url for billing and accounting and in standard Base64 for docscloud. A request signed otherwise is
    /// refused with 401, as the services refuse it, so a stack given another key or secret fails instead of passing
    /// unnoticed.
    /// </summary>
    internal static bool IsSigned(PaymentService service, string? authorization)
    {
        const string scheme = "ASC ";

        if (authorization is null || !authorization.StartsWith(scheme, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = authorization[scheme.Length..].Split(':');
        if (parts.Length != 3 || parts[0] != Constants.PaymentStubKey)
        {
            return false;
        }

        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(Constants.PaymentStubSecret), Encoding.UTF8.GetBytes($"{parts[1]}\n{parts[0]}"));
        var expected = service == PaymentService.DocsCloud ? Convert.ToBase64String(hash) : Base64Url.EncodeToString(hash);

        return parts[2] == expected;
    }

    /// <summary>A client of the stub's own address, for the tests of the stub itself.</summary>
    public HttpClient CreateClient()
    {
        return _server.CreateClient();
    }

    private static string? Header(IRequestMessage message, string name)
    {
        var header = message.Headers?.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));

        return header?.Value is { Count: > 0 } values ? values[0] : null;
    }

    // What the services say about a portal they know nothing about.
    private static PaymentServiceAnswer DefaultAnswer(PaymentServiceCall call)
    {
        return (call.Service, call.Method, call.Route) switch
        {
            // No payments: the stack falls back to its default quota, the free startup one.
            (PaymentService.Billing, "POST", "/billing/GetActiveResources") => PaymentServiceAnswer.Json("[]"),
            (PaymentService.Billing, "POST", "/billing/GetProductsPrices") => PaymentServiceAnswer.Json("{}"),

            // The checkout address is asked for once per product set for every portal, as a template the stack fills
            // in itself: the portal comes as "__Tenant__", and so do the other per-portal values. The template names
            // the products it was asked for, so a test can see they were the right ones.
            (PaymentService.Billing, "POST", "/billing/GetSinglePaymentUrl") => PaymentServiceAnswer.Json(JsonSerializer.Serialize(
                $"{CheckoutUrl}?products={string.Join(',', MultimapValues(call.Body, "ProductId"))}"
                + "&portal=__Tenant__&quantity=__Quantity__&currency=__Currency__&back=__BackUrl__&success=__SuccessUrl__")),

            // Billing reports its errors as 200 OK with an error message in the body.
            (PaymentService.Billing, _, _) => PaymentServiceAnswer.Json($$"""{"Message":"error: cannot find {{call.PortalId}}"}"""),

            (PaymentService.Accounting, "GET", "/service") => PaymentServiceAnswer.Json("""{"collection":[],"totalQuantity":0}"""),

            // The price list of the one wallet service the catalogue holds. Its routes name no portal, and the stack
            // caches the answers for every portal, so every test sees the same catalogue.
            (PaymentService.Accounting, "GET", "/service/by-name/" + CatalogueServiceName) =>
                PaymentServiceAnswer.Json($$"""{"id":{{CatalogueServiceId}},"name":"{{CatalogueServiceName}}"}"""),
            (PaymentService.Accounting, "GET", "/servicePrice/by-service/" + CatalogueServiceIdText) =>
                PaymentServiceAnswer.Json($$"""[{"id":12,"serviceId":{{CatalogueServiceId}},"timeUnit":"Minute","servicePrice":{{CataloguePrice.ToString(CultureInfo.InvariantCulture)}},"status":"Approved"}]"""),
            (PaymentService.Accounting, "GET", _) when call.Route.StartsWith("/service/by-name/", StringComparison.Ordinal) =>
                PaymentServiceAnswer.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}"""),

            (PaymentService.Accounting, "GET", "/currency/all") => PaymentServiceAnswer.Json("[]"),
            (PaymentService.Accounting, _, _) => PaymentServiceAnswer.Json(HttpStatusCode.BadRequest,
                $$"""{"title":"Bad request","status":400,"detail":"Customer '{{call.PortalId}}' not found"}"""),

            _ => PaymentServiceAnswer.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}""")
        };
    }

    private static IEnumerable<string> MultimapValues(string? body, string key)
    {
        if (string.IsNullOrEmpty(body))
        {
            return [];
        }

        using var document = JsonDocument.Parse(body);

        return document.RootElement.TryGetProperty(key, out var values)
            ? [.. values.EnumerateArray().Select(value => value.GetString() ?? string.Empty)]
            : [];
    }

    internal static string? PortalIdFrom(PaymentService service, string route, IRequestMessage message)
    {
        switch (service)
        {
            case PaymentService.Billing:
                // Every billing request carries the portal in a multimap body: {"PortalId":["stub-42"], ...}.
                if (string.IsNullOrEmpty(message.Body))
                {
                    return null;
                }

                using (var body = JsonDocument.Parse(message.Body))
                {
                    return body.RootElement.TryGetProperty("PortalId", out var portalId) && portalId.GetArrayLength() > 0
                        ? portalId[0].GetString()
                        : null;
                }
            case PaymentService.Accounting:
                var match = _portalInPath.Match(route);
                if (match.Success)
                {
                    return Uri.UnescapeDataString(match.Groups["portal"].Value);
                }

                // A new limit names its customer in the body: {"customerName":"stub-42", ...}.
                if (string.IsNullOrEmpty(message.Body))
                {
                    return null;
                }

                using (var body = JsonDocument.Parse(message.Body))
                {
                    return body.RootElement.ValueKind == JsonValueKind.Object && body.RootElement.TryGetProperty("customerName", out var customer)
                        ? customer.GetString()
                        : null;
                }
            case PaymentService.DocsCloud:
                return message.Query is not null && message.Query.TryGetValue("portalId", out var values) && values.Count > 0 ? values[0] : null;
            default:
                return null;
        }
    }
}

public enum PaymentService
{
    Unknown,
    Billing,
    Accounting,
    DocsCloud
}

/// <summary>
/// One request a service made to <see cref="PaymentServiceStub"/>. <see cref="Route"/> is the path below the
/// service's base URL with the portal's key put back as <c>{portalId}</c>, so it reads like the client's own route:
/// <c>/billing/GetActiveResources</c>, <c>/customer/{portalId}/balance</c>; <see cref="Query"/> is the query string
/// as it came, with its leading <c>?</c>.
/// </summary>
public sealed record PaymentServiceCall(PaymentService Service, string Method, string Route, string? PortalId, string? Body, string Query)
{
    internal static PaymentServiceCall From(IRequestMessage message)
    {
        var path = message.Path;
        var (service, route) = path switch
        {
            _ when path.StartsWith(Constants.PaymentStubBillingPath + "/", StringComparison.Ordinal) =>
                (PaymentService.Billing, path[Constants.PaymentStubBillingPath.Length..]),
            _ when path.StartsWith(Constants.PaymentStubAccountingPath + "/", StringComparison.Ordinal) =>
                (PaymentService.Accounting, path[Constants.PaymentStubAccountingPath.Length..]),
            _ when path.StartsWith(Constants.PaymentStubDocsCloudPath, StringComparison.Ordinal) =>
                (PaymentService.DocsCloud, "/" + path[Constants.PaymentStubDocsCloudPath.Length..]),
            _ => (PaymentService.Unknown, path)
        };

        var portalId = PaymentServiceStub.PortalIdFrom(service, route, message);

        if (portalId is not null && service == PaymentService.Accounting)
        {
            route = route.Replace("/" + Uri.EscapeDataString(portalId), "/{portalId}", StringComparison.Ordinal);
        }

        return new PaymentServiceCall(service, message.Method.ToUpperInvariant(), route, portalId, string.IsNullOrEmpty(message.Body) ? null : message.Body,
            new Uri(message.AbsoluteUrl).Query);
    }
}

/// <summary>What <see cref="PaymentServiceStub"/> answers: a status and a JSON body.</summary>
public sealed record PaymentServiceAnswer(HttpStatusCode Status, string Body)
{
    public static PaymentServiceAnswer Json(string body)
    {
        return new PaymentServiceAnswer(HttpStatusCode.OK, body);
    }

    public static PaymentServiceAnswer Json(HttpStatusCode status, string body)
    {
        return new PaymentServiceAnswer(status, body);
    }

    internal IResponseMessage ToResponse()
    {
        var response = new ResponseMessage
        {
            StatusCode = (int)Status,
            BodyData = new BodyData { BodyAsString = Body, DetectedBodyType = BodyType.String, Encoding = Encoding.UTF8 }
        };

        response.AddHeader("Content-Type", "application/json; charset=utf-8");

        return response;
    }
}

/// <summary>
/// What the payment services say about one portal. Each answer is keyed by the service, the method and the route
/// (<see cref="PaymentServiceCall.Route"/>); a request nothing is set for gets the answer every unknown portal gets.
/// </summary>
public sealed class PortalPayments(string portalId)
{
    // Billing numbers its payments across all customers, and the stack stores a paid tariff under that number, so two
    // portals given the same number would overwrite each other's tariff.
    private static int _lastPaymentId = 1000;

    // Accounting numbers the customers' accounts the same way; the stack tells its own spending limits from those of
    // other portals by it.
    private static int _lastAccountNumber = 5000;

    private readonly Lock _walletLock = new();
    private (decimal Amount, string Currency)? _wallet;
    private readonly JsonArray _payments = [];

    /// <summary>The number of the portal's account with accounting, unique across the run.</summary>
    public int AccountNumber { get; } = Interlocked.Increment(ref _lastAccountNumber);

    /// <summary>The account number when accounting knows the portal as a customer - once it has a wallet - else null.</summary>
    internal int? AccountNumberIfCustomer
    {
        get
        {
            lock (_walletLock)
            {
                return _wallet is null ? null : AccountNumber;
            }
        }
    }

    private readonly ConcurrentDictionary<(PaymentService, string, string), Func<PaymentServiceCall, PaymentServiceAnswer>> _answers = new();

    public string PortalId { get; } = portalId;

    /// <summary>Answers <paramref name="method"/> <paramref name="route"/> of <paramref name="service"/> with <paramref name="answer"/>.</summary>
    public PortalPayments On(PaymentService service, string method, string route, Func<PaymentServiceCall, PaymentServiceAnswer> answer)
    {
        _answers[(service, method.ToUpperInvariant(), route)] = answer;

        return this;
    }

    public PortalPayments On(PaymentService service, string method, string route, string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return On(service, method, route, _ => PaymentServiceAnswer.Json(status, json));
    }

    /// <summary>
    /// The portal has paid for <paramref name="quantity"/> of the product <paramref name="productId"/> until
    /// <paramref name="endDate"/>: billing lists it among the portal's active resources, next to what it has paid for
    /// before.
    /// </summary>
    public PortalPayments Paid(int productId, int quantity, DateTime endDate, string email = "owner@example.com")
    {
        var payment = new JsonObject
        {
            ["EndDate"] = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ["PaymentEmail"] = email,
            ["PaymentId"] = Interlocked.Increment(ref _lastPaymentId),
            ["PaymentStatus"] = 1,
            ["ProductId"] = productId,
            ["Quantity"] = quantity
        };

        lock (_walletLock)
        {
            _payments.Add(payment);
        }

        return On(PaymentService.Billing, "POST", "/billing/GetActiveResources", _ =>
        {
            lock (_walletLock)
            {
                return PaymentServiceAnswer.Json(_payments.ToJsonString());
            }
        });
    }

    /// <summary>
    /// The portal is a billing customer whose e-mail is <paramref name="email"/> - the portal user with that e-mail is
    /// its payer - with a payment method set, or in the state <paramref name="paymentMethodStatus"/> (0 none, 1 set,
    /// 2 expired).
    /// </summary>
    public PortalPayments Customer(string email, int paymentMethodStatus = 1)
    {
        var customer = new JsonObject
        {
            ["PortalId"] = PortalId,
            ["PaymentMethodStatus"] = paymentMethodStatus,
            ["PaymentMethodType"] = "card",
            ["Email"] = email
        };

        return On(PaymentService.Billing, "POST", "/billing/GetCustomerInfo", customer.ToJsonString());
    }

    /// <summary>
    /// The portal has a wallet with accounting holding <paramref name="amount"/> in <paramref name="currency"/>. The wallet
    /// keeps its state: a deposit billing accepts (<c>/billing/Deposit</c>) is credited to it, and accounting reports
    /// the new amount from then on.
    /// </summary>
    public PortalPayments Balance(decimal amount, string currency = "USD")
    {
        lock (_walletLock)
        {
            _wallet = (amount, currency);
        }

        On(PaymentService.Accounting, "GET", "/customer/{portalId}/balance", _ => PaymentServiceAnswer.Json(BalanceJson()));

        return On(PaymentService.Billing, "POST", "/billing/Deposit", call =>
        {
            using var body = JsonDocument.Parse(call.Body!);
            var deposit = decimal.Parse(body.RootElement.GetProperty("Amount")[0].GetString()!, CultureInfo.InvariantCulture);

            lock (_walletLock)
            {
                _wallet = (_wallet!.Value.Amount + deposit, _wallet.Value.Currency);
            }

            return PaymentServiceAnswer.Json("\"ok\"");
        });
    }

    private string BalanceJson()
    {
        lock (_walletLock)
        {
            var (amount, currency) = _wallet!.Value;

            return new JsonObject
            {
                ["accountNumber"] = AccountNumber,
                ["accountCurrency"] = currency,
                ["subAccounts"] = new JsonArray(new JsonObject { ["currency"] = currency, ["amount"] = amount })
            }.ToJsonString();
        }
    }

    internal PaymentServiceAnswer? AnswerFor(PaymentServiceCall call)
    {
        return _answers.TryGetValue((call.Service, call.Method, call.Route), out var answer) ? answer(call) : null;
    }
}
