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
/// One subscriber endpoint on the WireMock receiver: a path of its own, so parallel tests never see each
/// other's stubs or requests. The answers are stubbed through the WireMock admin API, and what the portal
/// sent is read back from its request journal.
/// </summary>
public sealed class WebhookReceiver
{
    private readonly HttpClient _admin;
    private readonly string _path;

    private WebhookReceiver(HttpClient admin, string path)
    {
        _admin = admin;
        _path = path;
    }

    /// <summary>
    /// The address a subscription points at. The portal reaches the receiver on its own loopback alias,
    /// the one address the integration-test blacklist leaves open (see
    /// <c>ConnectionStringManager.AddBaseConfig</c>), so <c>localhost</c> stays refused as in production.
    /// </summary>
    public string TargetUri => $"http://{ASC.AppHost.Configuration.Constants.WebhookReceiverHost}:{ASC.AppHost.Configuration.Constants.WebhookReceiverPort}{_path}";

    /// <summary>
    /// A new endpoint that answers every delivery with <paramref name="status"/> and <paramref name="body"/>
    /// (<c>ok</c> by default), after <paramref name="delayMilliseconds"/>. The HEAD the portal sends when a
    /// subscription is created or changed always gets 200, whatever the deliveries are stubbed to answer.
    /// </summary>
    public static async Task<WebhookReceiver> CreateAsync(HttpClient admin, int status = 200, int delayMilliseconds = 0, string? body = null)
    {
        var receiver = new WebhookReceiver(admin, $"/hooks/{Guid.NewGuid():N}");

        await receiver.StubAsync(new { method = "HEAD", urlPath = receiver._path }, new { status = 200 });
        await receiver.StubAsync(new { method = "POST", urlPath = receiver._path }, new { status, body = body ?? "ok", fixedDelayMilliseconds = delayMilliseconds });

        return receiver;
    }

    /// <summary>
    /// A new endpoint that answers every delivery with <paramref name="status"/> at once, then sends
    /// <paramref name="body"/> in <paramref name="chunks"/> pieces spread over <paramref name="bodyDuration"/>
    /// (WireMock's <c>chunkedDribbleDelay</c>): the status line and headers arrive first, the body only slowly.
    /// </summary>
    public static async Task<WebhookReceiver> CreateSlowBodyAsync(HttpClient admin, int status, string body, int chunks, TimeSpan bodyDuration)
    {
        var receiver = new WebhookReceiver(admin, $"/hooks/{Guid.NewGuid():N}");

        await receiver.StubAsync(new { method = "HEAD", urlPath = receiver._path }, new { status = 200 });
        await receiver.StubAsync(
            new { method = "POST", urlPath = receiver._path },
            new
            {
                status,
                body,
                chunkedDribbleDelay = new { numberOfChunks = chunks, totalDuration = (int)bodyDuration.TotalMilliseconds }
            });

        return receiver;
    }

    /// <summary>
    /// A new endpoint that answers the deliveries in turn with <paramref name="statuses"/>, the last one for
    /// every delivery after that.
    /// </summary>
    public static async Task<WebhookReceiver> CreateSequenceAsync(HttpClient admin, params int[] statuses)
    {
        var receiver = new WebhookReceiver(admin, $"/hooks/{Guid.NewGuid():N}");
        var scenario = receiver._path;

        await receiver.StubAsync(new { method = "HEAD", urlPath = receiver._path }, new { status = 200 });

        for (var i = 0; i < statuses.Length; i++)
        {
            var mapping = new Dictionary<string, object>
            {
                ["scenarioName"] = scenario,
                ["requiredScenarioState"] = i == 0 ? "Started" : $"answer-{i}",
                ["request"] = new { method = "POST", urlPath = receiver._path },
                ["response"] = new { status = statuses[i], body = "ok" }
            };

            if (i < statuses.Length - 1)
            {
                mapping["newScenarioState"] = $"answer-{i + 1}";
            }

            await receiver.PostMappingAsync(mapping);
        }

        return receiver;
    }

    /// <summary>The deliveries received so far, oldest first.</summary>
    public async Task<List<ReceivedDelivery>> GetDeliveriesAsync()
    {
        using var content = new StringContent(JsonSerializer.Serialize(new { method = "POST", url = _path }), Encoding.UTF8, "application/json");
        using var response = await _admin.PostAsync("__admin/requests/find", content, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        using var journal = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return journal.RootElement.GetProperty("requests")
            .EnumerateArray()
            .Select(ReceivedDelivery.Parse)
            .OrderBy(r => r.LoggedDate)
            .ToList();
    }

    /// <summary>
    /// Polls the journal until it holds at least <paramref name="count"/> deliveries, and returns the last
    /// observed list either way.
    /// </summary>
    public async Task<List<ReceivedDelivery>> WaitForDeliveriesAsync(int count, int timeoutSeconds = 90)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var deliveries = await GetDeliveriesAsync();

            if (deliveries.Count >= count || DateTime.UtcNow >= deadline)
            {
                return deliveries;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private Task StubAsync(object request, object response)
    {
        return PostMappingAsync(new { request, response });
    }

    private async Task PostMappingAsync(object mapping)
    {
        using var content = new StringContent(JsonSerializer.Serialize(mapping), Encoding.UTF8, "application/json");
        using var response = await _admin.PostAsync("__admin/mappings", content, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }
}

/// <summary>One request the receiver logged: the body exactly as sent, and the headers by lower-cased name.</summary>
public sealed record ReceivedDelivery(string Body, IReadOnlyDictionary<string, string> Headers, long LoggedDate)
{
    public string? Header(string name)
    {
        return Headers.GetValueOrDefault(name.ToLowerInvariant());
    }

    public static ReceivedDelivery Parse(JsonElement request)
    {
        var headers = new Dictionary<string, string>();

        foreach (var header in request.GetProperty("headers").EnumerateObject())
        {
            // WireMock writes a header sent once as a string and a repeated one as an array
            headers[header.Name.ToLowerInvariant()] = header.Value.ValueKind == JsonValueKind.Array
                ? string.Join(",", header.Value.EnumerateArray().Select(v => v.GetString()))
                : header.Value.GetString()!;
        }

        return new ReceivedDelivery(
            request.GetProperty("body").GetString()!,
            headers,
            request.GetProperty("loggedDate").GetInt64());
    }
}
