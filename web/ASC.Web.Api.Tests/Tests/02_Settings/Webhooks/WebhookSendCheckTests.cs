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
/// What the sender checks again right before each attempt, whatever was checked when the subscription was saved:
/// the target URL (one that points into a blacklisted network removes the subscription, one that is no longer
/// usable disables it) and whether the subscription is still enabled. The states are planted with MySQL statements
/// (<see cref="WebhookDeliveryTestBase.ExecuteSqlAsync"/>) on a record that was delivered once and is then put
/// back as unsent, since the API refuses such a URL and an event published to a disabled subscription is sent
/// before a test could switch it off.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookSendCheckTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task Send_TargetInABlacklistedNetwork_RemovesTheSubscription()
    {
        // Arrange: 10.0.0.0/8 is on the blacklist of every profile
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        await ExecuteSqlAsync("UPDATE webhooks_config SET uri = 'http://10.0.0.1/hook' WHERE id = @id", ("@id", subscription.Id));

        // Act
        await RequeueAsUnsentAsync(record!.Id);

        // Assert
        var config = await WaitForSubscriptionAsync(subscription.Id, c => c is null);

        config.Should().BeNull("a target in a blacklisted network is removed, not just switched off");
        (await receiver.GetDeliveriesAsync()).Should().ContainSingle("nothing went out after the URL changed");
    }

    [Fact]
    public async Task Send_TargetNoLongerUsable_DisablesTheSubscription()
    {
        // Arrange: the subscription now demands HTTPS, which the plain-HTTP receiver cannot give
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);

        await ExecuteSqlAsync("UPDATE webhooks_config SET `ssl` = 1 WHERE id = @id", ("@id", subscription.Id));

        // Act
        await RequeueAsUnsentAsync(record!.Id);

        // Assert
        var config = await WaitForSubscriptionAsync(subscription.Id, c => c is { Enabled: false });

        config.Should().NotBeNull("an unusable URL disables the subscription and keeps it for the owner to fix");
        config!.Enabled.Should().BeFalse();

        var after = await WaitForRecordAsync(subscription.Id, r => r.NextAttemptOn is null);
        after!.NextAttemptOn.Should().BeNull("the attempt is dropped, not retried");
        after.Attempts.Should().Be(0, "no request was made");
        (await receiver.GetDeliveriesAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Send_SubscriptionDisabledBeforeTheFirstAttempt_SendsNothing()
    {
        // Arrange: an event published while the subscription was enabled, not yet attempted when it is switched off
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);
        await FireEventAsync();
        var record = await WaitForRecordAsync(subscription.Id, r => r.Delivery is not null);
        await RequeueAsUnsentAsync(record!.Id, due: false);

        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.EnableWebhookAsync(new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = false
        }, TestContext.Current.CancellationToken);

        // Act
        await MakeDueAsync(record.Id);

        // Assert: the first attempt is dropped like a retry would be
        var after = await WaitForRecordAsync(subscription.Id, r => r.NextAttemptOn is null);

        after!.NextAttemptOn.Should().BeNull();
        after.Attempts.Should().Be(0, "a disabled subscription receives nothing, the first attempt included");
        (await receiver.GetDeliveriesAsync()).Should().ContainSingle("only the delivery made before the switch-off");
    }
}
