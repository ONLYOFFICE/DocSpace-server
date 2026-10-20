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
    public async Task RetryWebhooks_SeveralDeliveries_RequeuesEachKeepingItsId()
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

        // Assert
        result.Response.Select(r => r.Id).Should().BeEquivalentTo(ids);
        result.Response.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(0);
            r.Delivery.Should().BeNull();
        });
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
        result.Response.Select(r => r.Id).Should().Equal(delivery.Id);
    }

    [Fact]
    public async Task RetryWebhooks_DeliveryBeingSent_IsSkippedInSilence()
    {
        // Arrange: a receiver slow enough to keep the attempt in flight while the retry is asked for
        var receiver = await CreateReceiverAsync(delayMilliseconds: 15_000);
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        await receiver.WaitForDeliveriesAsync(1);
        var record = (await GetDeliveryRecordsAsync(subscription.Id)).Should().ContainSingle().Subject;

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
