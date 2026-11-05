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
/// One external HTTP service (billing, accounting, docscloud) as a test scripts it: a path of its own on a WireMock
/// server shared by the whole run, answering every request with what the test's responder returns.
/// </summary>
/// <remarks>
/// <para>
/// The client under test is pointed at <see cref="Url"/> and talks to it over a real socket, so the request crosses
/// the whole handler chain - authorization, resilience, transport - exactly as it does in production.
/// </para>
/// <para>
/// Each stub has a path of its own, so parallel test classes never see each other's requests and no mapping ever
/// has to be removed. A request is recorded the moment it arrives, before the answer is written: WireMock.Net adds
/// its own log entry only after the answer has gone out, so a test reading that log right after the client call
/// could miss the request, and a test waiting on a delayed answer could not see that it arrived at all.
/// </para>
/// <para>
/// Every answer closes its connection. A client that reuses a pooled connection the server then drops silently
/// sends the request once more, which a test counting requests would see as a retry the code never made.
/// </para>
/// </remarks>
public sealed class HttpStub
{
    private static readonly Lazy<WireMockServer> _server = new(StartServer);

    // A loopback port bound but never listened on, for as long as the run lasts: a port only found free and released
    // could be taken by a server a parallel test starts, and the "unreachable" service would then answer.
    private static readonly Lazy<(Socket Socket, string Url)> _refusing = new(BindRefusingPort);

    private readonly string _prefix = $"/t/{Guid.NewGuid():N}";
    private readonly ConcurrentQueue<StubRequest> _requests = new();
    private readonly Func<StubRequest, Task<StubResponse>> _respond = _ => throw new InvalidOperationException("An unreachable service never answers");
    private readonly string? _closedUrl;

    public HttpStub(Func<StubRequest, StubResponse> respond)
        : this(request => Task.FromResult(respond(request)))
    {
    }

    public HttpStub(Func<StubRequest, Task<StubResponse>> respond)
    {
        _respond = respond;

        _server.Value
            .Given(Request.Create().WithPath(new WildcardMatcher($"{_prefix}/*")).UsingAnyMethod())
            .RespondWith(Response.Create().WithCallback(HandleAsync));
    }

    private HttpStub(string closedUrl)
    {
        _closedUrl = closedUrl;
    }

    /// <summary>The address to configure as the service's base URL; append a sub-path to test how it is joined.</summary>
    public string Url => _closedUrl ?? _server.Value.Url + _prefix;

    /// <summary>How many requests have arrived so far, answered or not.</summary>
    public int CallCount => _requests.Count;

    /// <summary>The requests in the order they arrived.</summary>
    public IReadOnlyList<StubRequest> Requests => [.. _requests];

    /// <summary>The bodies of the requests that carried one, in the order they arrived.</summary>
    public IReadOnlyList<string> RequestBodies => [.. _requests.Where(r => r.Body is not null).Select(r => r.Body!)];

    public StubRequest? Last => _requests.LastOrDefault();

    public Uri? LastUri => Last?.Uri;

    public HttpMethod? LastMethod => Last?.Method;

    public string? LastAuthorization => Last?.Authorization;

    public string? LastRequestBody => Last?.Body;

    public string? LastContentType => Last?.ContentType;

    /// <summary>
    /// A service nothing listens for: the connection is refused before any answer, as when the service is down.
    /// </summary>
    public static HttpStub Unreachable()
    {
        return new HttpStub(_refusing.Value.Url);
    }

    private static (Socket, string) BindRefusingPort()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        return (socket, $"http://127.0.0.1:{((IPEndPoint)socket.LocalEndPoint!).Port}");
    }

    // The first request through Kestrel and WireMock's pipeline pays for a JIT pass that, on a busy machine, outlasts the
    // one-second soft timeout of TariffService's cached reads; the test that happened to send it would fail on time
    // alone. One request at start-up pays it before any test runs.
    private static WireMockServer StartServer()
    {
        var server = WireMockServer.Start();

        server
            .Given(Request.Create().WithPath("/warm-up").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NoContent));

        using var client = server.CreateClient();
        using var response = client.Send(new HttpRequestMessage(HttpMethod.Get, "/warm-up"));

        return server;
    }

    private async Task<IResponseMessage> HandleAsync(IRequestMessage message)
    {
        var request = StubRequest.From(message, _prefix);
        _requests.Enqueue(request);

        var answer = await _respond(request);

        if (answer.Delay > TimeSpan.Zero)
        {
            await Task.Delay(answer.Delay);
        }

        if (answer.Fault)
        {
            // WireMock cannot drop a connection: its faults still answer (EMPTY_RESPONSE is a 200 with no body,
            // MALFORMED_RESPONSE_CHUNK a 200 with garbage), and it writes Content-Length itself. A redirect to the
            // refusing port fails this one request inside the client's own send - the HttpRequestException a service
            // that went away gives - while the stub has still counted it.
            var redirect = new ResponseMessage { StatusCode = (int)HttpStatusCode.TemporaryRedirect };
            redirect.AddHeader("Location", _refusing.Value.Url + request.Path);
            redirect.AddHeader("Connection", "close");

            return redirect;
        }

        var response = new ResponseMessage
        {
            StatusCode = (int)answer.Status,
            BodyData = new BodyData { BodyAsString = answer.Body, DetectedBodyType = BodyType.String, Encoding = Encoding.UTF8 }
        };

        response.AddHeader("Content-Type", "application/json; charset=utf-8");
        response.AddHeader("Connection", "close");

        return response;
    }
}

/// <summary>
/// A request as <see cref="HttpStub"/> received it, with the stub's own path prefix taken off, so a path reads as it
/// does behind the configured base URL (<c>/billing/GetPayments</c>, <c>/api/customer/portal-1/balance</c>).
/// </summary>
public sealed record StubRequest(HttpMethod Method, Uri Uri, string? Body, string? ContentType, string? Authorization)
{
    public string Path => Uri.AbsolutePath;

    internal static StubRequest From(IRequestMessage message, string prefix)
    {
        var url = new Uri(message.AbsoluteUrl);
        var path = url.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal) ? url.AbsolutePath[prefix.Length..] : url.AbsolutePath;

        return new StubRequest(
            new HttpMethod(message.Method),
            new Uri($"{url.Scheme}://{url.Authority}{path}{url.Query}"),
            string.IsNullOrEmpty(message.Body) ? null : message.Body,
            Header(message, "Content-Type"),
            Header(message, "Authorization"));
    }

    private static string? Header(IRequestMessage message, string name)
    {
        var header = message.Headers?.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));

        return header?.Value is { Count: > 0 } values ? values[0] : null;
    }
}

/// <summary>What <see cref="HttpStub"/> answers: a JSON body with a status, after an optional delay, or a transport failure.</summary>
public sealed record StubResponse(HttpStatusCode Status, string Body = "", TimeSpan Delay = default, bool Fault = false)
{
    public static StubResponse Json(HttpStatusCode status, string json)
    {
        return new StubResponse(status, json);
    }

    /// <summary>
    /// This one request fails in transport: the client is redirected to a port that refuses the connection and sees an
    /// <see cref="HttpRequestException"/> from its send. The stub counts the request; for a service that never accepts
    /// one at all use <see cref="HttpStub.Unreachable"/>.
    /// </summary>
    public static StubResponse TransportFault()
    {
        return new StubResponse(HttpStatusCode.OK, Fault: true);
    }

    /// <summary>The same answer, written only after <paramref name="delay"/>; the request counts as arrived at once.</summary>
    public StubResponse After(TimeSpan delay)
    {
        return this with { Delay = delay };
    }
}
