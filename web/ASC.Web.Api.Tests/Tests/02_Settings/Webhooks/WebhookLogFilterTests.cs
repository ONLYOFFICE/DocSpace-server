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
/// GET /api/2.0/settings/webhooks/log — the filters of the delivery log, read as the owner. Each test answers
/// the deliveries of its subscriptions with the status classes it needs; records are told apart by the name of
/// their subscription, which the SDK model carries.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookLogFilterTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task GroupStatus_EachClass_KeepsOnlyTheRecordsAnsweredInIt()
    {
        // Arrange: one subscription per answered class, each with one attempted record of the same event
        var ok = await CreateAnsweringSubscriptionAsync(200);
        var redirect = await CreateAnsweringSubscriptionAsync(302);
        var clientError = await CreateAnsweringSubscriptionAsync(404);
        var serverError = await CreateAnsweringSubscriptionAsync(500);

        await FireEventAsync();
        await WaitForAnswersAsync(ok, redirect, clientError, serverError);

        // Act / Assert
        (await ConfigNamesAsync(WebhookGroupStatus.Status2xx)).Should().BeEquivalentTo([ok.Name]);
        (await ConfigNamesAsync(WebhookGroupStatus.Status3xx)).Should().BeEquivalentTo([redirect.Name]);
        (await ConfigNamesAsync(WebhookGroupStatus.Status4xx)).Should().BeEquivalentTo([clientError.Name]);
        (await ConfigNamesAsync(WebhookGroupStatus.Status5xx)).Should().BeEquivalentTo([serverError.Name]);
        (await ConfigNamesAsync(WebhookGroupStatus.Status2xx | WebhookGroupStatus.Status5xx))
            .Should().BeEquivalentTo([ok.Name, serverError.Name], "the flags combine as a union of classes");
        (await ConfigNamesAsync(WebhookGroupStatus.None))
            .Should().BeEquivalentTo([ok.Name, redirect.Name, clientError.Name, serverError.Name], "0 means no filter");
    }

    [Fact]
    public async Task GroupStatus_NotSent_KeepsOnlyTheRecordsWithoutAnAnswer()
    {
        // Arrange: a delivered record, then made to look unanswered (status 0) next to an answered one
        var unanswered = await CreateAnsweringSubscriptionAsync(200);
        var answered = await CreateAnsweringSubscriptionAsync(200);

        await FireEventAsync();
        await WaitForAnswersAsync(unanswered, answered);

        await ExecuteSqlAsync("UPDATE webhooks_logs SET status = 0 WHERE config_id = @id", ("@id", unanswered.Id));

        // Act / Assert
        (await ConfigNamesAsync(WebhookGroupStatus.NotSent)).Should().BeEquivalentTo([unanswered.Name]);
        (await ConfigNamesAsync(WebhookGroupStatus.Status2xx)).Should().BeEquivalentTo([answered.Name]);
    }

    [Fact]
    public async Task DeliveryRange_KeepsOnlyTheRecordsDeliveredWithinIt()
    {
        // Arrange: one delivered record and one that never was (delivery stays empty on a failure)
        var delivered = await CreateAnsweringSubscriptionAsync(200);
        var failed = await CreateAnsweringSubscriptionAsync(500);

        await FireEventAsync();
        await WaitForAnswersAsync(delivered, failed);

        var now = DateTime.UtcNow;

        // Act
        var around = await ConfigNamesAsync(deliveryFrom: now.AddDays(-1), deliveryTo: now.AddDays(1));
        var later = await ConfigNamesAsync(deliveryFrom: now.AddDays(1));
        var earlier = await ConfigNamesAsync(deliveryTo: now.AddDays(-1));

        // Assert
        around.Should().BeEquivalentTo([delivered.Name], "a record that was never delivered has no delivery moment to match");
        later.Should().BeEmpty();
        earlier.Should().BeEmpty();
    }

    [Fact]
    public async Task HookUri_KeepsOnlyTheSubscriptionWithExactlyThatAddress()
    {
        // Arrange
        var first = await CreateAnsweringSubscriptionAsync(200);
        var second = await CreateAnsweringSubscriptionAsync(200);

        await FireEventAsync();
        await WaitForAnswersAsync(first, second);

        // Act
        var exact = await ConfigNamesAsync(hookUri: first.Uri);
        var prefix = await ConfigNamesAsync(hookUri: first.Uri[..^1]);

        // Assert
        exact.Should().BeEquivalentTo([first.Name]);
        prefix.Should().BeEmpty("the address is compared as a whole, not as a prefix");
    }

    [Fact]
    public async Task Trigger_KeepsOnlyTheRecordsOfThatEvent()
    {
        // Arrange: a catch-all subscription, so the filter and not the subscription decides
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);

        await FireEventAsync();
        (await WaitForAttemptedDeliveriesAsync(subscription.Id)).Should().NotBeEmpty();

        // Act
        var created = await ConfigNamesAsync(trigger: WebhookTrigger.UserCreated);
        var fileCreated = await ConfigNamesAsync(trigger: WebhookTrigger.FileCreated);

        // Assert
        created.Should().Contain(subscription.Name);
        fileCreated.Should().BeEmpty("no file was created");
    }

    [Fact]
    public async Task UserId_Admin_KeepsOnlyTheSubscriptionsOfThatCreator()
    {
        // Arrange: a subscription of the owner and one of another admin, both receiving the same event
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var ownerSubscription = await CreateAnsweringSubscriptionAsync(200);
        var adminSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserCreated, admin);

        await FireEventAsync();
        await WaitForAnswersAsync(ownerSubscription, adminSubscription);

        // Act
        var ofAdmin = await ConfigNamesAsync(userId: admin.Id);
        var ofOwner = await ConfigNamesAsync(userId: Owner.Id);

        // Assert
        ofAdmin.Should().BeEquivalentTo([adminSubscription.Name]);
        ofOwner.Should().BeEquivalentTo([ownerSubscription.Name]);
    }

    [Fact]
    public async Task Paging_SplitsTheRecordsAndReportsTheTotal()
    {
        // Arrange: three records of one subscription
        var subscription = await CreateAnsweringSubscriptionAsync(200);

        for (var i = 0; i < 3; i++)
        {
            await FireEventAsync();
        }

        (await WaitForAttemptedDeliveriesAsync(subscription.Id, 3)).Count(l => l.Status != 0).Should().Be(3);

        // Act
        var first = await ReadPageAsync(subscription.Id, startIndex: 0, count: 2);
        var second = await ReadPageAsync(subscription.Id, startIndex: 2, count: 2);

        // Assert
        first.Total.Should().Be(3, "total counts every matching record, not the page");
        second.Total.Should().Be(3);
        first.Response.Should().HaveCount(2);
        second.Response.Should().ContainSingle();
        first.Response!.Select(r => r.Id).Should().NotIntersectWith(second.Response!.Select(r => r.Id));
        first.Response!.Select(r => r.Id).Should().BeInDescendingOrder("records come newest first");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Count_OutOfRange_ThrowsBadRequest(int count)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.GetWebhooksLogsAsync(count: count, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>A subscription of the owner to <c>user.created</c>, at a receiver that answers <paramref name="status"/>.</summary>
    private async Task<WebhookSubscription> CreateAnsweringSubscriptionAsync(int status)
    {
        return await CreateSubscriptionAsync(await CreateReceiverAsync(status), WebhookTrigger.UserCreated);
    }

    /// <summary>Waits until every subscription's record has its first answer.</summary>
    private async Task WaitForAnswersAsync(params WebhookSubscription[] subscriptions)
    {
        foreach (var subscription in subscriptions)
        {
            var record = await WaitForRecordAsync(subscription.Id, r => r.Status != 0);

            record.Should().NotBeNull("subscription {0} should have been attempted by now", subscription.Name);
            record!.Status.Should().NotBe(0);
        }
    }

    /// <summary>The names of the subscriptions whose records the log returns for the filters given, as the owner.</summary>
    private async Task<List<string>> ConfigNamesAsync(
        WebhookGroupStatus? groupStatus = null,
        DateTime? deliveryFrom = null,
        DateTime? deliveryTo = null,
        string? hookUri = null,
        WebhookTrigger? trigger = null,
        Guid? userId = null)
    {
        await _webApiClient.Authenticate(Owner);

        var logs = await _webhooksApi.GetWebhooksLogsAsync(
            deliveryFrom: deliveryFrom,
            deliveryTo: deliveryTo,
            hookUri: hookUri,
            groupStatus: groupStatus,
            userId: userId,
            trigger: trigger,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken);

        return logs.Response.Select(l => l.ConfigName).Distinct().ToList();
    }

    /// <summary>
    /// One page of a subscription's records with the <c>total</c> the response reports beside them, which the
    /// generated SDK wrapper does not carry, so it is read from the raw envelope.
    /// </summary>
    private async Task<RawApiResponse<List<DeliveryRecord>>> ReadPageAsync(int configId, int startIndex, int count)
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await _webApi.GetAsync(
            $"api/2.0/settings/webhooks/log?configId={configId}&startIndex={startIndex}&count={count}",
            TestContext.Current.CancellationToken);

        return await _webApi.ReadEnvelopeAsync<List<DeliveryRecord>>(response, TestContext.Current.CancellationToken);
    }
}
