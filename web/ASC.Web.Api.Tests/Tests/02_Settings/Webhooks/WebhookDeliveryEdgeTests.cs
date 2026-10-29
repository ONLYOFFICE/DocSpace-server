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
/// How the sender reads the answers that are neither a clean 2xx nor a 410: any other status is a failure that is
/// retried, redirects are not followed, the stored response is cut to 8 KB and cleaned of what the database cannot
/// hold, a failure is noted on the subscription, and a 2xx whose body arrives slowly is still a delivery.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookDeliveryEdgeTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    /// <summary>The longest response body the sender keeps (WebhookSender.MaxResponsePayloadBytes).</summary>
    private const int MaxStoredResponseBytes = 8 * 1024;

    [Fact]
    public async Task Delivery_TargetAnswers404_IsAFailureAndRetried()
    {
        // Arrange: the first answer is the one under test, the retry gets 200
        var receiver = await WebhookReceiver.CreateSequenceAsync(_webhookReceiverClient, 404, 200);
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Status.Should().Be(200);
        record.Attempts.Should().Be(2, "404 failed the first attempt");
        (await receiver.GetDeliveriesAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Delivery_TargetRedirects_IsAFailureAndTheRedirectIsNotFollowed()
    {
        // Arrange: the first answer is a 302 to another path of the receiver, which would answer 200; the retry
        // gets 200 where it was sent
        var receiver = await WebhookReceiver.CreateRedirectAsync(_webhookReceiverClient);
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Status.Should().Be(200);
        record.Attempts.Should().Be(2, "the redirect failed the first attempt");
        (await receiver.GetDeliveriesAsync()).Should().HaveCount(2);
        (await receiver.CountRedirectedRequestsAsync()).Should().Be(0, "a sender that followed the redirect would have asked there");
    }

    [Fact]
    public async Task Delivery_ResponseBodyOver8KB_IsStoredCutTo8KB()
    {
        // Arrange
        var receiver = await CreateReceiverAsync(body: new string('a', MaxStoredResponseBytes * 2));
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Status.Should().Be(200);
        record.ResponsePayload.Should().HaveLength(MaxStoredResponseBytes, "only the start of a long answer is kept");
    }

    [Fact]
    public async Task Delivery_ResponseBodyWithNulCharacters_IsStoredWithoutThem()
    {
        // Arrange: a NUL would fail the write of the whole record on PostgreSQL
        var receiver = await CreateReceiverAsync(body: "before\0after");
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Status.Should().Be(200);
        record.ResponsePayload.Should().Be("beforeafter");
    }

    [Fact]
    public async Task Delivery_TargetFails_RecordsTheFailureOnTheSubscription()
    {
        // Arrange
        var receiver = await CreateReceiverAsync(500);
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        (await WaitForRecordAsync(subscription.Id, r => r.Status != 0)).Status.Should().Be(500, "the target answered 500");

        var config = await WaitForSubscriptionAsync(subscription.Id, c => c?.LastFailureOn is not null);

        config.Should().NotBeNull();
        config!.LastFailureOn.Should().NotBeNull();
        config.LastFailureContent.Should().Contain("500", "the subscription keeps what the target answered");
        config.LastSuccessOn.Should().BeNull();
    }

    [Fact]
    public async Task Delivery_ResponseBodyWithAFourByteCharacter_IsStoredWithItReplaced()
    {
        // Arrange: response_payload is utf8 (three bytes at most), so the emoji would fail the write of the whole
        // record in strict mode; only a delivered answer is stored (a failed one is stored as the error text)
        var receiver = await CreateReceiverAsync(body: "\U0001F525 ok");
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Status.Should().Be(200);
        record.ResponsePayload.Should().Be("\uFFFD ok", "the journal keeps the answer with the emoji replaced");
    }

    [Fact]
    public async Task Delivery_2xxWhoseBodyArrivesAfterTheTimeout_IsADelivery()
    {
        // Arrange: the status and headers come at once, the body is dribbled out over longer than the limit of an
        // attempt. This test takes that long by design: nothing shorter crosses the limit
        var receiver = await WebhookReceiver.CreateSlowBodyAsync(
            _webhookReceiverClient, 200, "accepted, slowly", chunks: 4, bodyDuration: ProfileRequestTimeout + TimeSpan.FromSeconds(4));
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert: the 200 decided the outcome; the late body only left a note in the record
        var record = await WaitForRecordAsync(subscription.Id, r => r.Status != 0, timeoutSeconds: 120);

        record.Status.Should().Be(200);
        record.Delivery.Should().NotBeNull();
        record.Attempts.Should().Be(1);
        record.NextAttemptOn.Should().BeNull("a delivered event is not sent again");
        (await receiver.GetDeliveriesAsync()).Should().ContainSingle();
    }
}
