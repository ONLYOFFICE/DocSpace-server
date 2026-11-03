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
/// PUT /api/2.0/settings/webhook/{id}/retry — re-queuing one webhook delivery.
/// </summary>
[Trait("Category", "Settings")]
public class RetryWebhookTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task RetryWebhook_AttemptedDelivery_QueuesANewRecordOfTheSameEvent()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();

        // Act
        var result = await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken);

        // Assert: a record of its own, not yet attempted, with the body of the retried one
        result.Response.Id.Should().NotBe(delivery.Id);
        result.Response.Status.Should().Be(0);
        result.Response.Delivery.Should().BeNull();
        result.Response.RequestPayload.Should().Be(delivery.RequestPayload);
    }

    [Fact]
    public async Task RetryWebhook_AttemptedDelivery_NewRecordIsAttempted()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();

        // Act
        var result = await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken);

        // Assert
        var record = await WaitForAttemptAsync(delivery.Id, result.Response.Id);

        record.Status.Should().NotBe(0);
        record.ResponsePayload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RetryWebhook_FailedDelivery_KeepsTheEarlierOutcomeAndNamesWhoRetried()
    {
        // Arrange: a first attempt that failed, with an automatic retry scheduled. It is planted on a delivered
        // record and held a day ahead: a real retry comes half a second later and could be in flight (409)
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var original = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);
        await ExecuteSqlAsync(
            "UPDATE webhooks_logs SET status = 500, delivery = NULL, next_attempt_on = UTC_TIMESTAMP() + INTERVAL 1 DAY WHERE id = @id",
            ("@id", original.Id));

        // Act
        await _webApiClient.Authenticate(Owner);
        var result = await _webhooksApi.RetryWebhookAsync(original.Id, TestContext.Current.CancellationToken);

        // Assert: the failed record keeps its answer, and its automatic retries give way to the manual one
        var records = await GetLogAsync(subscription.Id);

        var earlier = records.Should().ContainSingle(r => r.Id == original.Id).Subject;
        earlier.Status.Should().Be(500);
        earlier.EventId.Should().Be(original.Id);
        earlier.RetriedBy.Should().BeNull();
        earlier.NextAttemptOn.Should().BeNull();

        var retry = records.Should().ContainSingle(r => r.Id == result.Response.Id).Subject;
        retry.EventId.Should().Be(original.Id);
        retry.RetriedBy.Should().NotBeNull();
        retry.RetriedBy!.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task RetryWebhook_RetryOfARetry_SendsTheOriginalEventId()
    {
        // Arrange
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var original = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);
        var firstRetry = await _webhooksApi.RetryWebhookAsync(original.Id, TestContext.Current.CancellationToken);
        (await WaitForAttemptAsync(original.Id, firstRetry.Response.Id)).Status.Should().NotBe(0, "the first retry should have been attempted by now");

        // Act
        var secondRetry = await _webhooksApi.RetryWebhookAsync(firstRetry.Response.Id, TestContext.Current.CancellationToken);

        // Assert: every resend carries the id of the event's first record
        var deliveries = await receiver.WaitForDeliveriesAsync(3);

        deliveries.Should().HaveCount(3);
        deliveries.Should().AllSatisfy(d => d.Header("x-docspace-event-id").Should().Be(original.Id.ToString()));

        var records = await GetLogAsync(subscription.Id);
        records.Single(r => r.Id == secondRetry.Response.Id).EventId.Should().Be(original.Id);
    }

    [Fact]
    public async Task RetryWebhook_EventIdFilter_ReturnsEveryRecordOfTheEvent()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var retry = await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken);

        // Act
        var records = (await _webhooksApi.GetWebhooksLogsAsync(
            eventId: delivery.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert: newest first
        records.Select(r => r.Id).Should().Equal(retry.Response.Id, delivery.Id);
    }

    [Fact]
    public async Task RetryWebhook_EventIdFilterWithTheRetryId_ReturnsThatRecordAlone()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var retry = await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken);

        // Act
        var records = (await _webhooksApi.GetWebhooksLogsAsync(
            eventId: retry.Response.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert: the id a retry returns is what its caller polls the outcome with
        records.Select(r => r.Id).Should().Equal(retry.Response.Id);
    }

    [Fact]
    public async Task RetryWebhook_DeliveredRecord_ResendsTheSameBodyAndSignature()
    {
        // Arrange
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);

        // Act
        await _webhooksApi.RetryWebhookAsync(record.Id, TestContext.Current.CancellationToken);

        // Assert: byte for byte, so a receiver that verifies the signature and drops a repeated event id
        // recognises the retry as the event it already has
        var deliveries = await receiver.WaitForDeliveriesAsync(2);

        deliveries.Should().HaveCount(2);
        deliveries[1].Body.Should().Be(deliveries[0].Body);
        deliveries[1].Header("x-docspace-signature-256").Should().Be(deliveries[0].Header("x-docspace-signature-256"));
        deliveries[1].Header("x-docspace-event-id").Should().Be(record.Id.ToString());
    }

    [Fact]
    public async Task RetryWebhook_DeliveryBeingSent_ThrowsConflict()
    {
        // Arrange: the record as a sender holds it while it is sending: leased, with the lease still running
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);
        await PlantInFlightAsync(record.Id);

        // Act
        await _webApiClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(record.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent?.ToString().Should().Contain("The event is being sent right now");
    }

    [Fact]
    public async Task RetryWebhook_DisabledSubscription_ThrowsConflict()
    {
        // Arrange: a delivered event, then the subscription is switched off
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.EnableWebhookAsync(new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = false
        }, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(record.Id, TestContext.Current.CancellationToken));

        // Assert: a disabled subscription receives nothing, so nothing is queued for it
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent?.ToString().Should().Contain("The webhook is disabled");
        (await GetLogAsync(subscription.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task RetryWebhook_RecordOfThePreviousDeliveryFormat_ThrowsConflict()
    {
        // Arrange: a record as the sender before the attempt counter left it, sent but with no attempt counted
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);
        await ExecuteSqlAsync("UPDATE webhooks_logs SET attempts = 0 WHERE id = @id", ("@id", record.Id));

        // Act
        await _webApiClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(record.Id, TestContext.Current.CancellationToken));

        // Assert: its body is in the old format, which carried whole REST objects, so it is not sent again
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent?.ToString().Should().Contain("The delivery was recorded in an earlier webhook format");
        (await GetLogAsync(subscription.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task RetryWebhook_UnsentRecordOfAPreviousVersionPublisher_ThrowsConflict()
    {
        // Arrange: a record as Web.Studio leaves one that a publisher of the previous version wrote during a rolling
        // update: never attempted, off the schedule, with the note in place of a response
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);
        await ExecuteSqlAsync(
            """
            UPDATE webhooks_logs
            SET attempts = 0, status = 0, delivery = NULL, request_headers = NULL, response_headers = NULL,
                next_attempt_on = NULL, lease_id = NULL, response_payload = @note
            WHERE id = @id
            """,
            ("@id", record.Id),
            ("@note", ASC.Webhooks.Core.DbWorker.PreviousVersionNote));

        // Act
        await _webApiClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(record.Id, TestContext.Current.CancellationToken));

        // Assert: the body of such a record is in the old format too, sent or not
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent?.ToString().Should().Contain("The delivery was recorded in an earlier webhook format");
        (await GetLogAsync(subscription.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task RetryWebhook_UnknownId_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(int.MaxValue, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RetryWebhook_ZeroId_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(0, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task RetryWebhook_AnotherMembersSubscription_ThrowsAccessDenied()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RetryWebhook_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RetryWebhookAsync(delivery.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
