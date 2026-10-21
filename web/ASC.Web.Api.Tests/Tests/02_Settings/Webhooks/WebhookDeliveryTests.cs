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
/// What the sender in Web.Studio does with a delivery, by the answer it gets: the request it sends, how a
/// failure is retried and given up on, and what a 410 or a disabled subscription stops. The integration-test
/// profile retries every 3 seconds, three times (ConnectionStringManager.AddBaseConfig), so a delivery
/// gets four attempts in all.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookDeliveryTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    private const int AttemptsInAll = 4;

    [Fact]
    public async Task Delivery_TargetAnswers200_IsRecordedAsDeliveredOnTheFirstAttempt()
    {
        // Arrange
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);

        record.Should().NotBeNull();
        record!.Status.Should().Be(200);
        record.Delivery.Should().NotBeNull();
        record.Attempts.Should().Be(1);
        record.NextAttemptOn.Should().BeNull();
    }

    [Fact]
    public async Task Delivery_TargetAnswers200_CarriesEventIdTimestampAndSignature()
    {
        // Arrange
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var delivery = (await receiver.WaitForDeliveriesAsync(1)).Should().ContainSingle().Subject;
        var record = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);

        using var body = JsonDocument.Parse(delivery.Body);
        var eventInfo = body.RootElement.GetProperty("event");

        eventInfo.GetProperty("id").GetInt32().Should().Be(record!.Id);
        delivery.Header("x-docspace-event-id").Should().Be(record.Id.ToString());

        DateTime.Parse(delivery.Header("x-docspace-event-timestamp")!).ToUniversalTime()
            .Should().Be(eventInfo.GetProperty("createOn").GetDateTime().ToUniversalTime());

        delivery.Header("x-docspace-signature-256").Should()
            .BeEquivalentTo($"sha256={Sign(subscription.SecretKey, delivery.Body)}", "the receiver checks the signature of the exact body it got");
    }

    [Fact]
    public async Task Delivery_TargetFailsTwiceThenAnswers200_IsRetriedUnderTheSameEventId()
    {
        // Arrange
        var receiver = await WebhookReceiver.CreateSequenceAsync(_webhookReceiverClient, 500, 500, 200);
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        record.Should().NotBeNull();
        record!.Status.Should().Be(200);
        record.Attempts.Should().Be(3);
        record.NextAttemptOn.Should().BeNull();

        var deliveries = await receiver.GetDeliveriesAsync();

        deliveries.Should().HaveCount(3);
        deliveries.Should().AllSatisfy(d => d.Header("x-docspace-event-id").Should().Be(record.Id.ToString()));
        deliveries.Select(d => RetryCountOf(d.Body)).Should().Equal(0, 1, 2);
    }

    [Fact]
    public async Task Delivery_TargetAlwaysFails_IsGivenUpAfterTheLastRetry()
    {
        // Arrange
        var receiver = await CreateReceiverAsync(500);
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.Attempts >= AttemptsInAll && r.NextAttemptOn is null, timeoutSeconds: 120);

        record.Should().NotBeNull();
        record!.Attempts.Should().Be(AttemptsInAll);
        record.NextAttemptOn.Should().BeNull("the retries have run out");
        record.Delivery.Should().BeNull();
        record.Status.Should().Be(500);

        (await receiver.GetDeliveriesAsync()).Should().HaveCount(AttemptsInAll);

        var config = await GetSubscriptionAsync(subscription.Id);
        config!.Enabled.Should().BeTrue("a subscription is only disabled once it has gone days without a success");
    }

    [Fact]
    public async Task Delivery_TargetAnswers410_RemovesTheSubscription()
    {
        // Arrange
        var receiver = await CreateReceiverAsync(410);
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act
        await FireEventAsync();

        // Assert
        await receiver.WaitForDeliveriesAsync(1);

        var deadline = DateTime.UtcNow.AddSeconds(30);
        WebhooksConfigDto? config;

        while ((config = await GetSubscriptionAsync(subscription.Id)) is not null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        config.Should().BeNull("410 Gone tells the portal the subscriber is gone for good");
        (await receiver.GetDeliveriesAsync()).Should().ContainSingle("a removed subscription is not retried");
    }

    [Fact]
    public async Task Delivery_SubscriptionDisabledWhileRetrying_StopsTheRetries()
    {
        // Arrange
        var receiver = await CreateReceiverAsync(500);
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        await WaitForRecordAsync(subscription.Id, r => r.NextAttemptOn is not null && r.Status != 0);

        // Act
        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.EnableWebhookAsync(new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = false
        }, TestContext.Current.CancellationToken);

        // Assert
        var record = await WaitForRecordAsync(subscription.Id, r => r.NextAttemptOn is null);

        record.Should().NotBeNull();
        record!.NextAttemptOn.Should().BeNull();
        record.Attempts.Should().BeLessThan(AttemptsInAll, "the retries were cancelled, not run out");

        var received = (await receiver.GetDeliveriesAsync()).Count;

        // two retry intervals plus a poll of the sender: anything still scheduled would have gone out by now
        await Task.Delay(TimeSpan.FromSeconds(12), TestContext.Current.CancellationToken);

        (await receiver.GetDeliveriesAsync()).Should().HaveCount(received);
    }

    [Fact]
    public async Task Delivery_RetryAfterAnEarlierSuccess_CarriesLastSuccessOnInUtc()
    {
        // Arrange: the first event is delivered at once, which records the success on the subscription
        var receiver = await WebhookReceiver.CreateSequenceAsync(_webhookReceiverClient, 200, 500, 200);
        var subscription = await CreateSubscriptionAsync(receiver);

        await FireEventAsync();
        (await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null)).Should().NotBeNull();

        // Act: the second event fails once and is retried
        await FireEventAsync();

        // Assert
        var deliveries = await receiver.WaitForDeliveriesAsync(3);

        deliveries.Should().HaveCount(3);

        using var retry = JsonDocument.Parse(deliveries[2].Body);
        var webhook = retry.RootElement.GetProperty("webhook");

        webhook.GetProperty("retryCount").GetInt32().Should().Be(1);

        // a date-time without an offset is rejected by the Java, Kotlin and Go SDKs, which drops the whole delivery
        var lastSuccessOn = webhook.GetProperty("lastSuccessOn").GetString();
        lastSuccessOn.Should().EndWith("Z");
        DateTimeOffset.Parse(lastSuccessOn!, CultureInfo.InvariantCulture).Should()
            .BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), "it is the time of the first delivery, in UTC");
    }

    private static int RetryCountOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        // zero is left out of the body, as every default value is
        return document.RootElement.GetProperty("webhook").TryGetProperty("retryCount", out var retryCount)
            ? retryCount.GetInt32()
            : 0;
    }
}
