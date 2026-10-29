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
/// What an attempt may write once it ends: only the delivery record it still holds the lease of, only the
/// delivery bookkeeping of the subscription, and a disabled subscription only when it has really gone days
/// without a success. The states behind these — a lease taken over, a subscription failing for days — are
/// planted in the database, since no API call produces them on demand, with MySQL statements
/// (<see cref="WebhookDeliveryTestBase.ExecuteSqlAsync"/>).
/// </summary>
[Trait("Category", "Settings")]
public class WebhookDeliveryStateTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task Attempt_LeaseTakenOverWhileSending_DoesNotOverwriteTheRecord()
    {
        // Arrange: a receiver slow enough to hold the attempt in flight while the test steps in. The answer comes
        // 3 seconds before the attempt would give up on the target, which leaves 9 seconds for the wait, the read and
        // the SQL below even on a busy machine: a shorter delay let the attempt finish first under parallel load
        var answerDelay = ProfileRequestTimeout - TimeSpan.FromSeconds(3);
        var receiver = await CreateReceiverAsync(delayMilliseconds: (int)answerDelay.TotalMilliseconds);
        var subscription = await CreateSubscriptionAsync(receiver, WebhookTrigger.UserCreated);
        await FireEventAsync();
        await receiver.WaitForDeliveriesAsync(1);
        var record = (await GetLogAsync(subscription.Id)).Should().ContainSingle().Subject;

        // Act: while the attempt waits for its answer, the record passes to another sender
        await ExecuteSqlAsync(
            "UPDATE webhooks_logs SET lease_id = @leaseId WHERE id = @id",
            ("@leaseId", Guid.NewGuid().ToString()), ("@id", record.Id));

        // Assert: the subscription records the success, just before the attempt tries to write the record
        var config = await WaitForSubscriptionAsync(subscription.Id, c => c?.LastSuccessOn is not null);

        config.Should().NotBeNull();
        config!.LastSuccessOn.Should().NotBeNull("the target answered 200, which the subscription records");

        // The attempt writes the subscription first and the record right after it, so a success already on the
        // subscription does not mean the record write has been tried yet. The record is watched for a few seconds
        // instead, and any change to it fails the test. Nobody else may touch it meanwhile: the planted lease belongs
        // to no sender, and the expiry it inherited from the attempt (the request timeout plus a minute) is far off.
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (true)
        {
            var after = (await GetDeliveryRecordsAsync(subscription.Id)).Single(r => r.Id == record.Id);

            after.Status.Should().Be(0, "the outcome belongs to an attempt that no longer holds the record");
            after.Delivery.Should().BeNull();
            after.Attempts.Should().Be(0);

            if (DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Attempt_LeaseOfADeadSender_IsTakenOverByTheNextPoll()
    {
        // Arrange: a record as a sender that died mid-send leaves it: leased, unanswered, the lease already expired
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        // Act
        await ExecuteSqlAsync(
            "UPDATE webhooks_logs SET status = 0, delivery = NULL, attempts = 0, lease_id = @leaseId, " +
            "next_attempt_on = UTC_TIMESTAMP() - INTERVAL 1 SECOND WHERE id = @id",
            ("@leaseId", Guid.NewGuid().ToString()), ("@id", record.Id));

        // Assert: another poll claims it with a lease of its own and delivers it
        var redelivered = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        redelivered.Status.Should().Be(200);
        redelivered.Attempts.Should().Be(1);
        redelivered.NextAttemptOn.Should().BeNull();
        (await receiver.GetDeliveriesAsync()).Should().HaveCount(2, "the dead sender's attempt and the one that took over");
    }

    [Fact]
    public async Task Attempt_SubscriptionEditedWhileSending_KeepsTheEdit()
    {
        // Arrange
        var receiver = await CreateReceiverAsync(delayMilliseconds: 8_000);
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        await receiver.WaitForDeliveriesAsync(1);
        var newName = $"renamed-{Guid.NewGuid():N}";

        // Act
        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.UpdateWebhookAsync(new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = newName,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = true
        }, TestContext.Current.CancellationToken);

        // the edit proves something only while the attempt still waits for its answer: on a machine too slow for the
        // receiver's delay the race is lost, and the test says so instead of passing without one
        (await GetLogAsync(subscription.Id)).Should().ContainSingle().Which.Status.Should()
            .Be(0, "the edit must land while the attempt is in flight; the receiver's 8-second delay was not enough");

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);
        record.Status.Should().Be(200);

        var config = await GetSubscriptionAsync(subscription.Id);

        config!.Name.Should().Be(newName, "the attempt writes only its own bookkeeping, not the subscription it read before the edit");
        config.Enabled.Should().BeTrue();
        config.LastSuccessOn.Should().NotBeNull();
    }

    [Fact]
    public async Task Attempt_RetriesRunOutOnASubscriptionWithoutSuccessForDays_DisablesIt()
    {
        // Arrange: the subscription was created four days ago and has never been delivered to
        var receiver = await CreateReceiverAsync(500);
        var subscription = await CreateSubscriptionAsync(receiver);
        await ExecuteSqlAsync(
            "UPDATE webhooks_config SET created_on = UTC_TIMESTAMP() - INTERVAL 4 DAY WHERE id = @id",
            ("@id", subscription.Id));

        // Act
        await FireEventAsync();

        // Assert: the event used up its retries, and only then the subscription was disabled
        var record = await WaitForRecordAsync(
            subscription.Id, r => r.Attempts > ProfileRetryCount && r.NextAttemptOn is null, timeoutSeconds: 120);

        record.Attempts.Should().Be(ProfileRetryCount + 1);

        var config = await GetSubscriptionAsync(subscription.Id);
        config!.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Attempt_FirstFailureOnASubscriptionWithoutSuccessForDays_IsRetriedAndKeepsItEnabled()
    {
        // Arrange: a subscription that heard nothing for days, as one with rare events does, whose receiver
        // fails once and then answers
        var receiver = await WebhookReceiver.CreateSequenceAsync(_webhookReceiverClient, 500, 200);
        var subscription = await CreateSubscriptionAsync(receiver);
        await ExecuteSqlAsync(
            "UPDATE webhooks_config SET created_on = UTC_TIMESTAMP() - INTERVAL 4 DAY WHERE id = @id",
            ("@id", subscription.Id));

        // Act
        await FireEventAsync();

        // Assert: one failure is not enough, the retry got its chance and succeeded
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Attempts.Should().Be(2);

        var config = await GetSubscriptionAsync(subscription.Id);
        config!.Enabled.Should().BeTrue("a subscription is disabled only once the retries of an event have run out");
    }

    [Fact]
    public async Task Attempt_RetriesRunOutOnAnOldSubscriptionWithARecentSuccess_KeepsItEnabled()
    {
        // Arrange: four days old, but delivered to an hour ago
        var receiver = await CreateReceiverAsync(500);
        var subscription = await CreateSubscriptionAsync(receiver);
        await ExecuteSqlAsync(
            "UPDATE webhooks_config SET created_on = UTC_TIMESTAMP() - INTERVAL 4 DAY, last_success_on = UTC_TIMESTAMP() - INTERVAL 1 HOUR WHERE id = @id",
            ("@id", subscription.Id));

        // Act
        await FireEventAsync();

        // Assert: the event is given up on, but the subscription has not gone days without a success
        var record = await WaitForRecordAsync(
            subscription.Id, r => r.Attempts > ProfileRetryCount && r.NextAttemptOn is null, timeoutSeconds: 120);

        record.Attempts.Should().Be(ProfileRetryCount + 1);

        var config = await GetSubscriptionAsync(subscription.Id);
        config!.Enabled.Should().BeTrue();
    }
}
