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

        // Act: while the attempt waits for its answer, the record passes to another sender. The statement changes the
        // record only while it is still unanswered, so a machine too slow for the receiver's delay loses the race
        // visibly instead of passing without one
        var planted = await ExecuteSqlAsync(
            "UPDATE webhooks_logs SET lease_id = @leaseId WHERE id = @id AND status = 0 AND delivery IS NULL",
            ("@leaseId", Guid.NewGuid().ToString()), ("@id", record.Id));

        planted.Should().Be(1, "the lease must change hands while the attempt is in flight; the receiver's delay was not enough");

        // Assert: the attempt gets its answer within the receiver's delay and then finds the record taken. Neither the
        // record nor the subscription may show its outcome: the subscription's last success follows only an attempt
        // the record keeps. Nothing tells the test when the attempt is over, so both are watched for longer than the
        // answer can take. Nobody else may touch the record meanwhile: the planted lease belongs to no sender, and the
        // expiry it inherited from the attempt (the request timeout plus a minute) is far off
        var deadline = DateTime.UtcNow + answerDelay + TimeSpan.FromSeconds(5);

        while (true)
        {
            var after = (await GetLogAsync(subscription.Id)).Single(r => r.Id == record.Id);

            after.Status.Should().Be(0, "the outcome belongs to an attempt that no longer holds the record");
            after.Delivery.Should().BeNull();
            after.Attempts.Should().Be(0);

            var config = await GetSubscriptionAsync(subscription.Id);

            config!.LastSuccessOn.Should().BeNull("the success of an attempt the record does not keep is not the subscription's either");

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

        // the subscription's bookkeeping follows the record, so it is read once it shows the success
        var config = await WaitForSubscriptionAsync(subscription.Id, c => c?.LastSuccessOn is not null);

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

        // the subscription is disabled right after the record is written, so it is polled rather than read once
        var config = await WaitForSubscriptionAsync(subscription.Id, c => c?.Enabled == false);
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

        var config = await WaitForSubscriptionAsync(subscription.Id, c => c?.LastSuccessOn is not null);
        config!.LastSuccessOn.Should().NotBeNull();
        await AssertSubscriptionStaysAsync(subscription.Id, c => c.Enabled, "a subscription is disabled only once the retries of an event have run out");
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

        await AssertSubscriptionStaysAsync(subscription.Id, c => c.Enabled, "a success an hour ago keeps the subscription from being disabled");
    }
}
