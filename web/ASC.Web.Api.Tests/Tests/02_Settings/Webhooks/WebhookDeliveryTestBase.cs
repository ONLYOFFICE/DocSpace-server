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
/// Shared by the suites that need a webhook really delivered: subscriptions pointed at a
/// <see cref="WebhookReceiver"/>, events that publish to them, and polling of the delivery records.
/// </summary>
public abstract class WebhookDeliveryTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private readonly AspireAppFixture _fixture = fixture;

    /// <summary>
    /// A receiver endpoint of this test's own that answers every delivery with <paramref name="status"/>.
    /// </summary>
    protected Task<WebhookReceiver> CreateReceiverAsync(int status = 200, int delayMilliseconds = 0)
    {
        return WebhookReceiver.CreateAsync(_webhookReceiverClient, status, delayMilliseconds);
    }

    /// <summary>
    /// Creates an enabled subscription to <paramref name="receiver"/> as <paramref name="creator"/> (the
    /// owner by default), for <paramref name="triggers"/> or every event. Leaves the Web.Api client
    /// signed in as the creator.
    /// </summary>
    protected async Task<WebhookSubscription> CreateSubscriptionAsync(
        WebhookReceiver receiver, WebhookTrigger? triggers = null, User? creator = null)
    {
        await _webApiClient.Authenticate(creator ?? Owner);

        var dto = WebhooksTestData.CreateWebhookDto(enabled: true, triggers: triggers);
        dto.Uri = receiver.TargetUri;

        var config = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        return new WebhookSubscription(config.Response.Id, dto.Name, dto.Uri, dto.SecretKey);
    }

    /// <summary>
    /// Creates an enabled catch-all subscription as the owner, to a receiver that answers 200, and
    /// returns its id.
    /// </summary>
    protected async Task<int> CreateSubscriptionAsync()
    {
        var receiver = await CreateReceiverAsync();

        return (await CreateSubscriptionAsync(receiver)).Id;
    }

    /// <summary>
    /// Adds a member, which publishes <c>user.created</c> to every subscription of the portal.
    /// </summary>
    protected async Task FireEventAsync()
    {
        await InviteContact(EmployeeType.User);
    }

    /// <summary>
    /// Polls the delivery log of one subscription until it holds <paramref name="count"/> records that have
    /// each been attempted (a non-zero status), and returns the last observed records either way.
    /// </summary>
    protected async Task<List<WebhooksLogDto>> WaitForAttemptedDeliveriesAsync(int configId, int count = 1)
    {
        await _webApiClient.Authenticate(Owner);

        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var logs = (await _webhooksApi.GetWebhooksLogsAsync(
                configId: configId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (logs.Count(l => l.Status != 0) >= count || DateTime.UtcNow >= deadline)
            {
                return logs;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls one delivery record until it has been attempted (a non-zero status), and returns the last observed
    /// state either way.
    /// </summary>
    protected async Task<WebhooksLogDto?> WaitForAttemptAsync(int deliveryId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var record = (await _webhooksApi.GetWebhooksLogsAsync(
                eventId: deliveryId, cancellationToken: TestContext.Current.CancellationToken)).Response.SingleOrDefault();

            if (record is { Status: not 0 } || DateTime.UtcNow >= deadline)
            {
                return record;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// One attempted delivery of a fresh subscription, read as the owner.
    /// </summary>
    protected async Task<WebhooksLogDto> CreateAttemptedDeliveryAsync()
    {
        var configId = await CreateSubscriptionAsync();
        await FireEventAsync();

        var attempted = (await WaitForAttemptedDeliveriesAsync(configId)).Where(l => l.Status != 0).ToList();

        attempted.Should().ContainSingle("the sender should have attempted the delivery by now");

        return attempted[0];
    }

    /// <summary>
    /// The delivery records of one subscription with the retry fields the SDK model does not carry yet
    /// (<c>attempts</c>, <c>nextAttemptOn</c>), read as raw JSON as the owner.
    /// </summary>
    protected async Task<List<DeliveryRecord>> GetDeliveryRecordsAsync(int configId)
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await _webApi.GetAsync(
            $"api/2.0/settings/webhooks/log?configId={configId}", TestContext.Current.CancellationToken);

        return await _webApi.ReadAsync<List<DeliveryRecord>>(response, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Polls the delivery records of one subscription until there is one and it satisfies
    /// <paramref name="until"/>, and returns the last observed record either way.
    /// </summary>
    protected async Task<DeliveryRecord?> WaitForRecordAsync(int configId, Func<DeliveryRecord, bool> until, int timeoutSeconds = 90)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var record = (await GetDeliveryRecordsAsync(configId)).SingleOrDefault();

            if (record is not null && until(record) || DateTime.UtcNow >= deadline)
            {
                return record;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls the delivery records of one subscription until one carries a payload that satisfies
    /// <paramref name="match"/>, and returns that payload, or null when none did by the deadline. The record
    /// holds the body as published until it is sent and as sent afterwards; the payload is the same in both.
    /// </summary>
    protected async Task<JsonElement?> WaitForPayloadAsync(int configId, Func<JsonElement, bool> match, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var payloads = (await GetDeliveryRecordsAsync(configId)).Select(r => PayloadOf(r.RequestPayload));

            foreach (var payload in payloads)
            {
                if (match(payload))
                {
                    return payload;
                }
            }

            if (DateTime.UtcNow >= deadline)
            {
                return null;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The subscription as the owner reads it, or null once it is gone.</summary>
    protected async Task<WebhooksConfigDto?> GetSubscriptionAsync(int configId)
    {
        await _webApiClient.Authenticate(Owner);

        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);

        return list.Response.SingleOrDefault(w => w.Configs.Id == configId)?.Configs;
    }

    /// <summary>The <c>payload</c> of a delivery body: the user, group or entry the event is about.</summary>
    protected static JsonElement PayloadOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("payload").Clone();
    }

    /// <summary>
    /// Writes to the stack's database directly, for the states no API call can produce on demand: a
    /// subscription that has been failing for days, or a delivery record taken over by another sender.
    /// </summary>
    protected async Task ExecuteSqlAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await _fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>The HMAC-SHA256 the portal signs a body with, as the hex string of the signature header.</summary>
    protected static string Sign(string secretKey, string body)
    {
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secretKey), Encoding.UTF8.GetBytes(body)));
    }
}

public sealed record WebhookSubscription(int Id, string Name, string Uri, string SecretKey);

/// <summary>A delivery record as <c>GET settings/webhooks/log</c> returns it, retry fields included.</summary>
public sealed class DeliveryRecord
{
    public int Id { get; init; }
    public int Status { get; init; }
    public DateTime? Delivery { get; init; }
    public int Attempts { get; init; }
    public DateTime? NextAttemptOn { get; init; }
    public string RequestPayload { get; init; } = "";
}
