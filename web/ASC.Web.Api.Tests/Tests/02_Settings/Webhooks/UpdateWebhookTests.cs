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
/// PUT /api/2.0/settings/webhook — replacing a webhook's parameters. The subscription is looked up and its owner
/// checked before the body is validated, so a missing one is a 404 and another member's a 403 whatever the body
/// holds. The triggers are checked against the role of the subscription's creator, who is the one receiving them,
/// not of the member editing it. An empty secret keeps the stored one.
/// </summary>
[Trait("Category", "Settings")]
public class UpdateWebhookTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task UpdateWebhook_Owner_NameAndUriAreUpdated()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var updated = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient);

        // Act
        var result = await _webhooksApi.UpdateWebhookAsync(
            UpdateOf(subscription, name: updated.Name, uri: updated.Uri), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Id.Should().Be(subscription.Id);
        result.Response.Name.Should().Be(updated.Name);
        result.Response.Uri.Should().Be(updated.Uri);
        result.Response.ModifiedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateWebhook_Owner_TriggersAndTargetAreReplaced()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);
        var update = UpdateOf(subscription);
        update.Triggers = WebhookTrigger.FileUpdated | WebhookTrigger.FolderCreated;
        update.TargetId = "12345";

        // Act
        var result = await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Triggers.Should().Be(WebhookTrigger.FileUpdated | WebhookTrigger.FolderCreated);
        result.Response.TargetId.Should().Be("12345", "a file-family target is checked by read access on every event, not here");

        var stored = await GetSubscriptionAsync(subscription.Id);
        stored!.Triggers.Should().Be(WebhookTrigger.FileUpdated | WebhookTrigger.FolderCreated);
        stored.TargetId.Should().Be("12345");
    }

    [Fact]
    public async Task UpdateWebhook_SslWithHttpTarget_ThrowsBadRequest()
    {
        // Arrange: the receiver speaks plain http, which ssl does not accept
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var update = UpdateOf(subscription);
        update.Ssl = true;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await GetSubscriptionAsync(subscription.Id))!.Ssl.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateWebhook_EmptySecret_KeepsTheStoredSecret()
    {
        // Arrange
        var receiver = await CreateReceiverAsync();
        var subscription = await CreateSubscriptionAsync(receiver);

        // Act: a new name, and an empty secret
        var update = UpdateOf(subscription, name: $"renamed-{Guid.NewGuid():N}");
        update.SecretKey = "";
        await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken);

        // Assert: what is delivered next is still signed with the secret the subscription was created with
        await FireEventAsync();
        var delivery = (await receiver.WaitForDeliveriesAsync(1)).Should().NotBeEmpty().And.Subject.First();

        delivery.Header("x-docspace-signature-256").Should().BeEquivalentTo($"sha256={Sign(subscription.SecretKey, delivery.Body)}");
    }

    [Fact]
    public async Task UpdateWebhook_UnknownId_ThrowsNotFound()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var update = UpdateOf(subscription);
        update.Id = int.MaxValue;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateWebhook_UnknownIdWithAnInvalidBody_ThrowsNotFound()
    {
        // Arrange: the address would fail validation, but there is nothing to validate it for
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var update = UpdateOf(subscription, uri: "not an address");
        update.Id = int.MaxValue;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task UpdateWebhook_AnotherMembersSubscription_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var member = await InviteContact(employeeType);

        // Act
        await _webApiClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(UpdateOf(subscription, name: "taken over"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await GetSubscriptionAsync(subscription.Id))!.Name.Should().Be(subscription.Name);
    }

    [Fact]
    public async Task UpdateWebhook_AnotherMembersSubscriptionWithAnInvalidBody_ThrowsAccessDenied()
    {
        // Arrange: the body would fail validation, which must not be what tells a stranger the subscription exists
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var member = await InviteContact(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(UpdateOf(subscription, uri: "not an address"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateWebhook_AdminAddsATriggerTheCreatorMayNotUse_ThrowsBadRequest()
    {
        // Arrange: a User's subscription; user.created is for admins and room admins only
        var member = await InviteContact(EmployeeType.User);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);

        var update = UpdateOf(subscription);
        update.Triggers = WebhookTrigger.FileCreated | WebhookTrigger.UserCreated;

        // Act: the owner may subscribe to user.created, the creator may not
        await _webApiClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await GetSubscriptionAsync(subscription.Id))!.Triggers.Should().Be(WebhookTrigger.FileCreated);
    }

    [Fact]
    public async Task UpdateWebhook_AdminAddsATriggerTheCreatorMayUse_Succeeds()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);

        var update = UpdateOf(subscription);
        update.Triggers = WebhookTrigger.FileCreated | WebhookTrigger.FileUpdated;

        // Act
        await _webApiClient.Authenticate(Owner);
        var result = await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Triggers.Should().Be(WebhookTrigger.FileCreated | WebhookTrigger.FileUpdated);
        result.Response.CreatedBy!.Id.Should().Be(member.Id, "an admin's edit does not change whose subscription it is");
    }

    /// <summary>The update that keeps everything of <paramref name="subscription"/> but what is named.</summary>
    private static UpdateWebhooksConfigRequestDto UpdateOf(WebhookSubscription subscription, string? name = null, string? uri = null)
    {
        return new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = name ?? subscription.Name,
            Uri = uri ?? subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = true
        };
    }
}
