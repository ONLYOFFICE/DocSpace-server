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
/// PUT /api/2.0/settings/webhook/retry — re-queuing webhook deliveries for retry.
/// </summary>
[Trait("Category", "Settings")]
public class RetryWebhooksTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task RetryWebhooks_EmptyIdList_ReturnsEmptyResult()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new WebhookRetryRequestDto([]);

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task RetryWebhooks_SeveralDeliveries_QueuesANewRecordForEach()
    {
        // Arrange
        var configId = await CreateSubscriptionAsync();
        await FireEventAsync();
        await FireEventAsync();
        var deliveries = (await WaitForAttemptedDeliveriesAsync(configId, 2)).Where(l => l.Status != 0).ToList();
        deliveries.Should().HaveCount(2, "both events should have been attempted by now");
        var ids = deliveries.Select(d => d.Id).ToList();

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(new WebhookRetryRequestDto(ids), TestContext.Current.CancellationToken);

        // Assert: new records, one per retried event
        result.Response.Should().HaveCount(2);
        result.Response.Select(r => r.Id).Should().NotIntersectWith(ids);
        result.Response.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(0);
            r.Delivery.Should().BeNull();
        });

        var newIds = result.Response.Select(r => r.Id).ToList();
        var records = await GetLogAsync(configId);
        records.Where(r => newIds.Contains(r.Id)).Select(r => r.EventId).Should().BeEquivalentTo(ids);
    }

    [Fact]
    public async Task RetryWebhooks_TwoRecordsOfTheSameEvent_QueuesOneRetry()
    {
        // Arrange: an event's first record and a manual retry of it, attempted, so nothing of the event is in flight
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var original = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);
        var firstRetry = (await _webhooksApi.RetryWebhookAsync(original.Id, TestContext.Current.CancellationToken)).Response;
        (await WaitForAttemptAsync(original.Id, firstRetry.Id)).Status.Should().NotBe(0, "the first retry should have been attempted by now");

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(
            new WebhookRetryRequestDto([original.Id, firstRetry.Id]), TestContext.Current.CancellationToken);

        // Assert: one new record for the event; a second one would stop the first as soon as it was queued
        var retry = result.Response.Should().ContainSingle().Subject;
        retry.Id.Should().NotBe(original.Id).And.NotBe(firstRetry.Id);

        var records = await GetLogAsync(subscription.Id);
        records.Should().HaveCount(3, "the event's first record, the earlier retry and the one queued now");
        records.Single(r => r.Id == retry.Id).EventId.Should().Be(original.Id);
    }

    [Fact]
    public async Task RetryWebhooks_UnknownId_IsSkippedInSilence()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(
            new WebhookRetryRequestDto([delivery.Id, int.MaxValue]), TestContext.Current.CancellationToken);

        // Assert: a shorter response is the only sign that something was left out
        result.Response.Should().ContainSingle().Which.Id.Should().NotBe(delivery.Id);
    }

    [Fact]
    public async Task RetryWebhooks_DeliveryBeingSent_IsSkippedInSilence()
    {
        // Arrange: the record as a sender holds it while it is sending: leased, with the lease still running
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);
        await PlantInFlightAsync(record.Id);

        // Act
        await _webApiClient.Authenticate(Owner);
        var result = await _webhooksApi.RetryWebhooksAsync(
            new WebhookRetryRequestDto([record.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryWebhooks_DisabledSubscription_IsSkippedInSilence()
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
        var result = await _webhooksApi.RetryWebhooksAsync(
            new WebhookRetryRequestDto([record.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryWebhooks_AnotherMembersSubscription_IsSkippedInSilence()
    {
        // Arrange
        var delivery = await CreateAttemptedDeliveryAsync();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var result = await _webhooksApi.RetryWebhooksAsync(
            new WebhookRetryRequestDto([delivery.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }
}
