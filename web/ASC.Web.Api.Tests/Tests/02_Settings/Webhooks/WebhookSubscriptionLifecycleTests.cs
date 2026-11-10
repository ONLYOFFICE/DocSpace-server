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
/// What happens to a subscription between its creation and its removal: the status of its last delivery in the list,
/// an update that replaces every field it carries, switching on (which re-checks the stored address and triggers but
/// not the target), the events lost while it is off, its removal with the delivery records, the journal's
/// <c>route</c> that follows the current address, and a User's catch-all that gets only the events open to a User.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookSubscriptionLifecycleTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task GetTenantWebhooks_StatusIsZeroBeforeAndTheLastAnswerAfterADelivery()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver(status: 202));
        var before = await StatusOfAsync(subscription.Id);

        // Act
        await FireEventAsync();
        await WaitForAttemptedDeliveriesAsync(subscription.Id);

        // Assert
        before.Should().Be(0);
        (await StatusOfAsync(subscription.Id)).Should().Be(202);
    }

    [Fact]
    public async Task UpdateWebhook_FieldsLeftOut_AreReplacedByTheirDefaults()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var created = WebhooksTestData.CreateAcceptedWebhookDto(enabled: true, triggers: WebhookTrigger.FileCreated, targetId: "12345");
        var config = (await _webhooksApi.CreateWebhookAsync(created, TestContext.Current.CancellationToken)).Response;

        // Act: the body names only name and address
        var update = new UpdateWebhooksConfigRequestDto(config.Id) { Name = created.Name, Uri = created.Uri };
        await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken);

        // Assert
        var stored = await GetSubscriptionAsync(config.Id);
        stored!.Enabled.Should().BeFalse();
        (stored.Triggers ?? WebhookTrigger.All).Should().Be(WebhookTrigger.All);
        stored.TargetId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task EnableWebhook_UnknownId_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var update = new UpdateWebhooksConfigRequestDto(int.MaxValue) { Name = "absent", Uri = CreateReceiver().TargetUri, Enabled = true };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.EnableWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task EnableWebhook_StoredAddressNowInAPrivateNetwork_ThrowsBadRequestAndStaysOff()
    {
        // Arrange: the address is planted, since the API refuses it on every save
        var subscription = await SwitchedOffSubscriptionAsync(Owner);
        await ExecuteSqlAsync("UPDATE webhooks_config SET uri = 'http://10.1.2.3/hook' WHERE id = @id", ("@id", subscription.Id));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.EnableWebhookAsync(Switch(subscription, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await GetSubscriptionAsync(subscription.Id))!.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task EnableWebhook_StoredTriggerNotOpenToTheCreator_ThrowsBadRequest()
    {
        // Arrange: a User's subscription with user.created, which only an admin may receive, planted
        var user = await InviteContact(EmployeeType.User);
        var subscription = await SwitchedOffSubscriptionAsync(user, WebhookTrigger.FileCreated);
        await ExecuteSqlAsync("UPDATE webhooks_config SET triggers = @triggers WHERE id = @id",
            ("@triggers", Convert.ToInt64(WebhookTrigger.UserCreated, CultureInfo.InvariantCulture)), ("@id", subscription.Id));

        // Act
        await _webApiClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.EnableWebhookAsync(Switch(subscription, true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task EnableWebhook_StoredTargetThatIsNotThere_IsNotRechecked()
    {
        // Arrange: a user-event target that names nobody, planted
        var subscription = await SwitchedOffSubscriptionAsync(Owner, WebhookTrigger.UserUpdated);
        await ExecuteSqlAsync("UPDATE webhooks_config SET target_id = 'not-a-guid' WHERE id = @id", ("@id", subscription.Id));

        // Act
        var result = await _webhooksApi.EnableWebhookAsync(Switch(subscription, true), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Enabled.Should().BeTrue();
        result.Response.TargetId.Should().Be("not-a-guid");
    }

    [Fact]
    public async Task EnableWebhook_EventsWhileOff_AreNotDeliveredLater()
    {
        // Arrange
        var receiver = CreateReceiver();
        var subscription = await CreateSubscriptionAsync(receiver);
        await _webhooksApi.EnableWebhookAsync(Switch(subscription, false), TestContext.Current.CancellationToken);
        await FireEventAsync();

        // Act
        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.EnableWebhookAsync(Switch(subscription, true), TestContext.Current.CancellationToken);
        await FireEventAsync();

        // Assert: the event of the switched-on time arrives; the earlier one never does
        (await receiver.WaitForDeliveriesAsync(1)).Should().HaveCount(1);
        await Task.Delay(SenderPollPeriod * 2, TestContext.Current.CancellationToken);
        receiver.GetDeliveries().Should().HaveCount(1);
        (await GetLogAsync(subscription.Id)).Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveWebhook_UnknownId_ThrowsNotFound()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RemoveWebhookAsync(int.MaxValue, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RemoveWebhook_SecondRemoval_ThrowsNotFound()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver());
        await _webhooksApi.RemoveWebhookAsync(subscription.Id, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.RemoveWebhookAsync(subscription.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RemoveWebhook_TakesItsDeliveryRecordsWithIt()
    {
        // Arrange
        var configId = await CreateSubscriptionAsync();
        await FireEventAsync();
        await WaitForAttemptedDeliveriesAsync(configId);
        var recordsBefore = await CountRecordsAsync(configId);

        // Act
        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.RemoveWebhookAsync(configId, TestContext.Current.CancellationToken);

        // Assert
        recordsBefore.Should().BeGreaterThan(0);
        (await CountRecordsAsync(configId)).Should().Be(0);
    }

    [Fact]
    public async Task GetWebhooksLogs_RouteOfAnOlderRecord_IsTheCurrentAddress()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver());
        await FireEventAsync();
        await WaitForAttemptedDeliveriesAsync(subscription.Id);
        var moved = CreateReceiver();

        // Act
        await _webApiClient.Authenticate(Owner);
        await _webhooksApi.UpdateWebhookAsync(new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = moved.TargetUri,
            Enabled = true
        }, TestContext.Current.CancellationToken);

        // Assert
        var record = (await GetLogAsync(subscription.Id)).Should().ContainSingle().Subject;
        record.Route.Should().Be(moved.TargetUri, "route is read from the subscription as it stands now (S12)");
    }

    [Fact]
    public async Task UserCatchAll_DoesNotGetTheAdminOnlyUserCreatedEvent()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var usersReceiver = CreateReceiver();
        var usersSubscription = await CreateSubscriptionAsync(usersReceiver, creator: user);
        var ownersReceiver = CreateReceiver();
        await CreateSubscriptionAsync(ownersReceiver);

        // Act
        await FireEventAsync();

        // Assert: the owner's catch-all shows the event went out; the User's never gets it
        (await ownersReceiver.WaitForDeliveriesAsync(1)).Should().NotBeEmpty();
        await Task.Delay(SenderPollPeriod * 2, TestContext.Current.CancellationToken);
        usersReceiver.GetDeliveries().Should().BeEmpty();
        (await GetLogAsync(usersSubscription.Id)).Should().BeEmpty();
    }

    /// <summary>A subscription of <paramref name="creator"/> to a receiver, switched off; the Web.Api client stays signed in as the creator.</summary>
    private async Task<WebhookSubscription> SwitchedOffSubscriptionAsync(User creator, WebhookTrigger? triggers = null)
    {
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), triggers, creator);
        await _webhooksApi.EnableWebhookAsync(Switch(subscription, false), TestContext.Current.CancellationToken);

        return subscription;
    }

    private static UpdateWebhooksConfigRequestDto Switch(WebhookSubscription subscription, bool enabled)
    {
        return new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = enabled
        };
    }

    private async Task<int> StatusOfAsync(int configId)
    {
        await _webApiClient.Authenticate(Owner);
        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);

        return list.Response.Single(w => w.Configs.Id == configId).Status;
    }

    private Task<long> CountRecordsAsync(int configId)
    {
        return QueryCountAsync("SELECT COUNT(*) FROM webhooks_logs WHERE config_id = @id", ("@id", configId));
    }
}
