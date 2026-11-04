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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// One subscriber endpoint on the run-wide WireMock server: a path of its own, so parallel tests never see each
/// other's stubs or requests. The server runs in the test process and starts on first use, and the deliveries are
/// recorded here rather than read from its log: WireMock.Net writes a log entry only after the answer has gone out, so
/// a test that has just seen the sender's record could still miss the delivery behind it.
/// </summary>
public sealed class WebhookReceiver
{
    /// <summary>
    /// The portal reaches the receiver on its own loopback alias, the one address the integration-test blacklist
    /// leaves open (<c>Constants.WebhookTestBlacklist</c>), so <c>localhost</c> stays refused as in production.
    /// </summary>
    private static readonly string _baseUri =
        $"http://{ASC.AppHost.Configuration.Constants.WebhookReceiverHost}:{ASC.AppHost.Configuration.Constants.WebhookReceiverPort}";

    private static readonly Lazy<WireMockServer> _server = new(() => WireMockServer.Start(new WireMockServerSettings { Urls = [_baseUri] }));

    private readonly ConcurrentQueue<ReceivedDelivery> _deliveries = new();
    private readonly string _path = $"/hooks/{Guid.NewGuid():N}";
    private int _deliveryCount;
    private int _redirectedCount;

    /// <summary>The HEAD the portal sends when a subscription is created or changed always gets 200.</summary>
    private WebhookReceiver()
    {
        _server.Value
            .Given(Request.Create().WithPath(_path).UsingHead())
            .RespondWith(Response.Create().WithStatusCode(200));
    }

    /// <summary>The address a subscription points at.</summary>
    public string TargetUri => _baseUri + _path;

    /// <summary>An address on the server that nothing is stubbed for: it answers 404 to any request.</summary>
    public static string UnstubbedUri(string name)
    {
        _ = _server.Value;

        return $"{_baseUri}/hooks/{name}";
    }

    /// <summary>Stops the server of the run, if a test started it.</summary>
    public static void StopServer()
    {
        if (_server.IsValueCreated)
        {
            _server.Value.Stop();
            _server.Value.Dispose();
        }
    }

    /// <summary>
    /// A new endpoint that answers every delivery with <paramref name="status"/> and <paramref name="body"/>
    /// (<c>ok</c> by default), after <paramref name="delayMilliseconds"/>.
    /// </summary>
    public static WebhookReceiver Create(int status = 200, int delayMilliseconds = 0, string? body = null)
    {
        var receiver = new WebhookReceiver();

        receiver.AnswerDeliveries(async _ =>
        {
            await Task.Delay(delayMilliseconds);

            return Answer(status, body ?? "ok");
        });

        return receiver;
    }

    /// <summary>
    /// A new endpoint that answers every delivery with <paramref name="status"/> at once, then sends
    /// <paramref name="body"/> in <paramref name="chunks"/> pieces spread over <paramref name="bodyDuration"/>: the
    /// status line and headers arrive first, the body only slowly.
    /// </summary>
    public static WebhookReceiver CreateSlowBody(int status, string body, int chunks, TimeSpan bodyDuration)
    {
        var receiver = new WebhookReceiver();
        var pieces = body.Chunk(Math.Max(1, body.Length / chunks)).Select(piece => new string(piece)).ToList();

        _server.Value
            .Given(Request.Create().WithPath(receiver._path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(status).WithSseBody(async (request, queue) =>
            {
                receiver._deliveries.Enqueue(ReceivedDelivery.From(request));

                foreach (var piece in pieces)
                {
                    queue.Write(piece);

                    await Task.Delay(bodyDuration / pieces.Count);
                }

                queue.Close();
            }));

        return receiver;
    }

    /// <summary>
    /// A new endpoint that answers the deliveries in turn with <paramref name="statuses"/>, the last one for
    /// every delivery after that.
    /// </summary>
    public static WebhookReceiver CreateSequence(params int[] statuses)
    {
        var receiver = new WebhookReceiver();

        receiver.AnswerDeliveries(number => Task.FromResult(Answer(statuses[Math.Min(number, statuses.Length - 1)], "ok")));

        return receiver;
    }

    /// <summary>
    /// A new endpoint whose first delivery is answered with a 302 to <see cref="RedirectPath"/>, a path of the same
    /// receiver that answers 200 to any method, and every later delivery with 200. A sender that followed the
    /// redirect would leave a request there.
    /// </summary>
    public static WebhookReceiver CreateRedirect()
    {
        var receiver = new WebhookReceiver();

        _server.Value
            .Given(Request.Create().WithPath(receiver.RedirectPath).UsingAnyMethod())
            .RespondWith(Response.Create().WithCallback(_ =>
            {
                Interlocked.Increment(ref receiver._redirectedCount);

                return Answer(200, "redirected");
            }));

        receiver.AnswerDeliveries(number => Task.FromResult(number == 0 ? Answer(302, string.Empty, receiver.RedirectPath) : Answer(200, "ok")));

        return receiver;
    }

    /// <summary>Where the endpoint of <see cref="CreateRedirect"/> redirects its first delivery to.</summary>
    public string RedirectPath => $"{_path}/moved";

    /// <summary>The requests of any method that reached <see cref="RedirectPath"/>.</summary>
    public int CountRedirectedRequests() => Volatile.Read(ref _redirectedCount);

    /// <summary>The deliveries received so far, oldest first.</summary>
    public List<ReceivedDelivery> GetDeliveries() => [.. _deliveries];

    /// <summary>
    /// Polls the deliveries until there are at least <paramref name="count"/> of them, and returns the last
    /// observed list either way.
    /// </summary>
    public async Task<List<ReceivedDelivery>> WaitForDeliveriesAsync(int count, int timeoutSeconds = 90)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var deliveries = GetDeliveries();

            if (deliveries.Count >= count || DateTime.UtcNow >= deadline)
            {
                return deliveries;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Records each delivery as it arrives, then answers it; <paramref name="answer"/> gets its number from 0.</summary>
    private void AnswerDeliveries(Func<int, Task<IResponseMessage>> answer)
    {
        Func<IRequestMessage, Task<IResponseMessage>> callback = request =>
        {
            _deliveries.Enqueue(ReceivedDelivery.From(request));

            return answer(Interlocked.Increment(ref _deliveryCount) - 1);
        };

        _server.Value
            .Given(Request.Create().WithPath(_path).UsingPost())
            .RespondWith(Response.Create().WithCallback(callback));
    }

    private static IResponseMessage Answer(int status, string body, string? location = null)
    {
        var message = new ResponseMessage
        {
            StatusCode = status,
            BodyData = new BodyData { BodyAsString = body, DetectedBodyType = BodyType.String }
        };

        if (location is not null)
        {
            message.AddHeader("Location", location);
        }

        return message;
    }
}

/// <summary>One request the receiver took: the body exactly as sent, and the headers by lower-cased name.</summary>
public sealed record ReceivedDelivery(string Body, IReadOnlyDictionary<string, string> Headers)
{
    public string? Header(string name)
    {
        return Headers.GetValueOrDefault(name.ToLowerInvariant());
    }

    public static ReceivedDelivery From(IRequestMessage request)
    {
        return new ReceivedDelivery(
            Encoding.UTF8.GetString(request.BodyAsBytes ?? []),
            request.Headers!.ToDictionary(header => header.Key.ToLowerInvariant(), header => string.Join(",", header.Value)));
    }
}
