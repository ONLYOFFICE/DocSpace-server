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

    // the receiver takes the deliveries, Web.Studio sends them, and Files publishes the file events
    protected override bool NeedsLazyResources => true;

    /// <summary>
    /// The automatic retries the integration-test profile configures: <see cref="ProfileRetryCount"/> of them,
    /// <see cref="ProfileRetryInterval"/> apart, instead of the production schedule of minutes to hours. The values are
    /// the ones the AppHost passes to the services (<c>webhooks:retryIntervals</c>), read from the same constants.
    /// </summary>
    protected const int ProfileRetryCount = ASC.AppHost.Configuration.Constants.WebhookTestRetryCount;

    /// <inheritdoc cref="ProfileRetryCount"/>
    protected static TimeSpan ProfileRetryInterval => ASC.AppHost.Configuration.Constants.WebhookTestRetryInterval;

    /// <summary>
    /// How long one attempt waits for the target in the integration-test profile (<c>webhooks:requestTimeout</c>, the
    /// constant the AppHost passes to the services), instead of the production 30 seconds.
    /// </summary>
    protected static TimeSpan ProfileRequestTimeout => ASC.AppHost.Configuration.Constants.WebhookTestRequestTimeout;

    /// <summary>
    /// How long the sender in Web.Studio sleeps between two polls when nothing is due: a due retry can wait that long
    /// before it is claimed. A copy of <c>WorkerService._waitingPeriod</c> (web/ASC.Web.Studio/WebHooks/Services), a
    /// private field of a service the test project does not reference and not a setting of the profile, so it cannot
    /// be shared; keep the two in step.
    /// </summary>
    protected static TimeSpan SenderPollPeriod { get; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// A <c>next_attempt_on</c> that is already due: the sender claims such a record at its next poll, at most
    /// <see cref="SenderPollPeriod"/> later.
    /// </summary>
    private const string DueNow = "UTC_TIMESTAMP() - INTERVAL 1 SECOND";

    /// <summary>
    /// A receiver endpoint of this test's own that answers every delivery with <paramref name="status"/> and
    /// <paramref name="body"/> (<c>ok</c> by default).
    /// </summary>
    protected Task<WebhookReceiver> CreateReceiverAsync(int status = 200, int delayMilliseconds = 0, string? body = null)
    {
        return WebhookReceiver.CreateAsync(_webhookReceiverClient, status, delayMilliseconds, body);
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
    /// The delivery log of one subscription as the generated SDK reads it, as the owner, newest first, retry fields
    /// (<c>attempts</c>, <c>nextAttemptOn</c>, <c>eventId</c>, <c>retriedBy</c>) included.
    /// </summary>
    protected async Task<List<WebhooksLogDto>> GetLogAsync(int configId)
    {
        await _webApiClient.Authenticate(Owner);

        return (await _webhooksApi.GetWebhooksLogsAsync(
            configId: configId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Polls the delivery log of one subscription until it holds <paramref name="count"/> records that have
    /// each been attempted (a non-zero status), and returns the last observed records either way.
    /// </summary>
    protected async Task<List<WebhooksLogDto>> WaitForAttemptedDeliveriesAsync(int configId, int count = 1)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var logs = await GetLogAsync(configId);

            if (logs.Count(l => l.Status != 0) >= count || DateTime.UtcNow >= deadline)
            {
                return logs;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls one delivery record as the owner until it has been attempted (a non-zero status), and returns the last
    /// observed state either way; fails when the record is not in the log at all. The <c>eventId</c> filter given the
    /// id of the event's first record returns it with every manual retry of it, so the record is picked out of them
    /// by its own id.
    /// </summary>
    protected async Task<WebhooksLogDto> WaitForAttemptAsync(int eventId, int deliveryId)
    {
        await _webApiClient.Authenticate(Owner);

        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var record = (await _webhooksApi.GetWebhooksLogsAsync(
                eventId: eventId, cancellationToken: TestContext.Current.CancellationToken)).Response.FirstOrDefault(r => r.Id == deliveryId);

            if (record is { Status: not 0 } || DateTime.UtcNow >= deadline)
            {
                record.Should().NotBeNull("delivery record {0} should be in the log", deliveryId);

                return record!;
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
    /// Polls the delivery records of one subscription until there is exactly one and it satisfies
    /// <paramref name="until"/>, and returns that record, or the only record there is at the deadline whether it
    /// satisfies <paramref name="until"/> or not, so the caller asserts what it waited for. Fails with the records it
    /// saw when the subscription has none or more than one by the deadline.
    /// </summary>
    protected async Task<WebhooksLogDto> WaitForRecordAsync(int configId, Func<WebhooksLogDto, bool> until, int timeoutSeconds = 90)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var records = await GetLogAsync(configId);

            if (records is [var record] && until(record))
            {
                return record;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return records.Should().ContainSingle("subscription {0} should have exactly one delivery record", configId).Subject;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls the delivery log of one subscription until one record carries a payload that satisfies
    /// <paramref name="match"/>, and returns that payload, or null when none did by the deadline. The record
    /// holds the body as published until it is sent and as sent afterwards; the payload is the same in both.
    /// </summary>
    protected async Task<JsonElement?> WaitForPayloadAsync(int configId, Func<JsonElement, bool> match, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var payloads = (await GetLogAsync(configId)).Select(r => PayloadOf(r.RequestPayload));

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

    /// <summary>
    /// Polls the delivery log of one subscription until one record was published for <paramref name="trigger"/>, named
    /// as it appears in <c>event.trigger</c>, with a payload that satisfies <paramref name="match"/>, and returns that
    /// payload, or null when none did by the deadline. Matching the name rather than the subscription's trigger
    /// bits lets a catch-all subscription stand in for the triggers the pinned SDK enum does not list yet (agent.*).
    /// </summary>
    protected async Task<JsonElement?> WaitForEventPayloadAsync(int configId, string trigger, Func<JsonElement, bool> match, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            foreach (var record in await GetLogAsync(configId))
            {
                using var document = JsonDocument.Parse(record.RequestPayload);
                var root = document.RootElement;

                if (root.GetProperty("event").GetProperty("trigger").GetString() == trigger
                    && root.GetProperty("payload") is var payload
                    && match(payload))
                {
                    return payload.Clone();
                }
            }

            if (DateTime.UtcNow >= deadline)
            {
                return null;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Asserts that <paramref name="url"/> is an absolute http(s) URL: a receiver has no portal address to resolve a
    /// relative one against.
    /// </summary>
    protected static void ShouldBeAbsoluteUrl(string? url, string because)
    {
        Uri.TryCreate(url, UriKind.Absolute, out var uri).Should().BeTrue(because);
        uri!.Scheme.Should().BeOneOf([Uri.UriSchemeHttp, Uri.UriSchemeHttps], because);
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
    /// subscription that has been failing for days, or a delivery record taken over by another sender. Returns the
    /// number of rows the statement changed.
    /// </summary>
    /// <remarks>
    /// MySQL only: the connection is the MySQL container of the integration-test profile, and the statements the
    /// tests pass are written in its dialect (<c>UTC_TIMESTAMP() - INTERVAL 4 DAY</c>, GUIDs as <c>varchar(36)</c>).
    /// A run of the suite against PostgreSQL needs both this helper and those statements ported.
    /// </remarks>
    protected async Task<int> ExecuteSqlAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await _fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Polls a subscription as the owner reads it until <paramref name="until"/> holds, and returns the last observed
    /// state either way; null once the subscription is gone.
    /// </summary>
    protected async Task<WebhooksConfigDto?> WaitForSubscriptionAsync(int configId, Func<WebhooksConfigDto?, bool> until, int timeoutSeconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var config = await GetSubscriptionAsync(configId);

            if (until(config) || DateTime.UtcNow >= deadline)
            {
                return config;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Asserts that a subscription keeps satisfying <paramref name="stays"/> for a while, on every read. The sender
    /// writes the delivery record first and only then the subscription's bookkeeping (its last failure, and the switch
    /// that disables it), so a state that must NOT change — "stays enabled" above all — cannot be read once right after
    /// the record shows up: a wrong switch would land after that read and the test would still pass. It is watched for
    /// <see cref="SenderPollPeriod"/> plus a margin, longer than those steps of the attempt can take.
    /// </summary>
    protected async Task AssertSubscriptionStaysAsync(int configId, Func<WebhooksConfigDto, bool> stays, string because)
    {
        var deadline = DateTime.UtcNow + SenderPollPeriod + TimeSpan.FromSeconds(3);

        while (true)
        {
            var config = await GetSubscriptionAsync(configId);

            config.Should().NotBeNull("the subscription must still exist");
            stays(config!).Should().BeTrue(because);

            if (DateTime.UtcNow >= deadline)
            {
                return;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Makes a settled delivery record look like one a sender is sending right now: leased, with the lease running
    /// for a day. No sender holds it, so nothing ever finishes it; the record stays "in flight" for the test.
    /// </summary>
    protected Task PlantInFlightAsync(int recordId)
    {
        return ExecuteSqlAsync(
            "UPDATE webhooks_logs SET lease_id = @leaseId, next_attempt_on = UTC_TIMESTAMP() + INTERVAL 1 DAY WHERE id = @id",
            ("@leaseId", Guid.NewGuid().ToString()), ("@id", recordId));
    }

    /// <summary>
    /// Puts a settled delivery record back to the state of an event just published and never attempted, due at the
    /// next poll of the sender (<paramref name="due"/>), or a day ahead so that the test decides when it becomes due
    /// (<see cref="MakeDueAsync"/>). The receiver and the subscription stay as they are.
    /// </summary>
    protected Task RequeueAsUnsentAsync(int recordId, bool due = true)
    {
        return ExecuteSqlAsync(
            "UPDATE webhooks_logs SET status = 0, delivery = NULL, attempts = 0, lease_id = NULL, response_payload = NULL, " +
            $"next_attempt_on = {(due ? DueNow : "UTC_TIMESTAMP() + INTERVAL 1 DAY")} WHERE id = @id",
            ("@id", recordId));
    }

    /// <summary>Makes a delivery record due at the next poll of the sender, keeping its other columns.</summary>
    protected Task MakeDueAsync(int recordId)
    {
        return ExecuteSqlAsync($"UPDATE webhooks_logs SET next_attempt_on = {DueNow} WHERE id = @id", ("@id", recordId));
    }

    /// <summary>
    /// Changes the type of <paramref name="member"/> as the owner and waits until the change is done: a demotion
    /// from <c>RoomAdmin</c> runs as a background task (<c>startUserTypeUpdate</c>), reported through its progress.
    /// </summary>
    protected async Task ChangeTypeAsync(User member, EmployeeType type)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.StartUserTypeUpdateAsync(new StartUpdateUserTypeDto(type, member.Id), cancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (true)
        {
            var progress = (await _userTypeApi.GetUserTypeUpdateProgressAsync(member.Id, cancellationToken)).Response;

            if (progress.IsCompleted || DateTime.UtcNow >= deadline)
            {
                progress.IsCompleted.Should().BeTrue("the type change of the member must have finished");
                progress.Error.Should().BeNullOrEmpty();

                return;
            }

            await Task.Delay(500, cancellationToken);
        }
    }

    /// <summary>Disables (terminates) <paramref name="member"/> as the owner.</summary>
    protected async Task TerminateAsync(User member)
    {
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated, new UpdateMembersRequestDto([member.Id], false), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A room created by the owner, shared for reading with <paramref name="readers"/> and with nobody else.
    /// </summary>
    protected async Task<int> CreateRoomAsync(params User[] readers)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _filesClient.Authenticate(Owner);

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: $"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom), cancellationToken)).Response;

        if (readers.Length > 0)
        {
            await _roomsApi.SetRoomSecurityAsync(
                room.Id,
                new RoomInvitationRequest(
                    invitations: readers.Select(r => new RoomInvitation(r.Id, DocSpace.API.SDK.Model.FileShare.Read)).ToList(),
                    notify: false),
                cancellationToken);
        }

        return room.Id;
    }

    /// <summary>
    /// Creates an empty document with a fresh title in <paramref name="folderId"/> as the owner, which publishes
    /// <c>file.created</c>, and returns its id and title.
    /// </summary>
    protected async Task<(int Id, string Title)> CreateFileAsync(int folderId)
    {
        var title = $"file-{Guid.NewGuid():N}.docx";

        await _filesClient.Authenticate(Owner);

        var file = await _filesApi.CreateFileAsync(folderId, new CreateFileRequest(title), TestContext.Current.CancellationToken);

        return (file.Response.Id, title);
    }

    /// <summary>
    /// <paramref name="subject"/> renames themselves, which publishes <c>user.updated</c>, and returns the new first
    /// name. Only the user themselves can change the name: the same call made by the owner leaves it as it is and
    /// publishes nothing.
    /// </summary>
    protected async Task<string> RenameAsync(User subject)
    {
        var firstName = $"Renamed{Initializer.Faker.Random.String2(8, "abcdefghijklmnopqrstuvwxyz")}";

        await _peopleClient.Authenticate(subject);
        await _profilesApi.UpdateMemberAsync(
            subject.Id.ToString(), new UpdateMemberRequestDto { FirstName = firstName }, TestContext.Current.CancellationToken);

        return firstName;
    }

    /// <summary>The HMAC-SHA256 the portal signs a body with, as the hex string of the signature header.</summary>
    protected static string Sign(string secretKey, string body)
    {
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secretKey), Encoding.UTF8.GetBytes(body)));
    }
}

public sealed record WebhookSubscription(int Id, string Name, string Uri, string SecretKey);
