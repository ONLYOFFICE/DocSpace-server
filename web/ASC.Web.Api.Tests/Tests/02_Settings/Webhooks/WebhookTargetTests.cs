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
/// A subscription narrowed to one entity by <c>targetId</c>: it gets the events about that entity only, and every
/// delivery names the entity in <c>webhook.target</c>. For user and group events the target has to be a user or
/// group the creator may see, by GUID, and anything else is refused with 400 on create and on update, a hidden one
/// with the same answer as an unknown one. Targets of the file family are not checked when the subscription is
/// saved: every event is checked against read access instead, and a subscription that mixes the two takes any target.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookTargetTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task UserTarget_GetsTheEventsOfThatUserOnly()
    {
        // Arrange
        var target = await InviteContact(EmployeeType.User);
        var other = await InviteContact(EmployeeType.User);
        var subscription = await CreateTargetedSubscriptionAsync(WebhookTrigger.UserUpdated, target.Id.ToString());

        // Act: the other user changes first, so their event, if published, is already in the log when the target's arrives
        await RenameAsync(other);
        await RenameAsync(target);

        // Assert
        var records = await WaitForRecordsAsync(subscription.Id);
        records.Should().ContainSingle("the target's own change is delivered and the change of another user is not");
        PayloadOf(records[0].RequestPayload).GetProperty("id").GetGuid().Should().Be(target.Id);

        var webhookTarget = WebhookOf(records[0].RequestPayload).GetProperty("target");
        webhookTarget.GetProperty("id").GetString().Should().Be(target.Id.ToString());
        webhookTarget.GetProperty("type").GetString().Should().Be("user");
    }

    [Fact]
    public async Task GroupTarget_GetsTheEventsOfThatGroupOnly()
    {
        // Arrange
        var target = await CreateGroupAsync();
        var other = await CreateGroupAsync();
        var subscription = await CreateTargetedSubscriptionAsync(WebhookTrigger.GroupUpdated, target.ToString());

        // Act
        await RenameGroupAsync(other);
        await RenameGroupAsync(target);

        // Assert
        var records = await WaitForRecordsAsync(subscription.Id);
        records.Should().ContainSingle("the target group's change is delivered and the change of another group is not");
        PayloadOf(records[0].RequestPayload).GetProperty("id").GetGuid().Should().Be(target);

        var webhookTarget = WebhookOf(records[0].RequestPayload).GetProperty("target");
        webhookTarget.GetProperty("id").GetString().Should().Be(target.ToString());
        webhookTarget.GetProperty("type").GetString().Should().Be("group");
    }

    [Fact]
    public async Task NoTarget_DeliversNoTargetBlock()
    {
        // Arrange
        var subject = await InviteContact(EmployeeType.User);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated);

        // Act
        await RenameAsync(subject);

        // Assert
        var records = await WaitForRecordsAsync(subscription.Id);
        records.Should().NotBeEmpty();
        WebhookOf(records[0].RequestPayload).TryGetProperty("target", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(WebhookTrigger.UserUpdated, "unknown-guid")]
    [InlineData(WebhookTrigger.UserUpdated, "not-a-guid")]
    [InlineData(WebhookTrigger.GroupUpdated, "unknown-guid")]
    [InlineData(WebhookTrigger.GroupUpdated, "not-a-guid")]
    public async Task CreateWebhook_UserOrGroupTriggerWithATargetThatIsNotThere_ThrowsBadRequest(WebhookTrigger trigger, string targetId)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient, triggers: trigger, targetId: Resolve(targetId));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateWebhook_GroupTriggerWithAUserAsTarget_ThrowsBadRequest()
    {
        // Arrange: an existing id, but of the wrong kind of entity
        await _webApiClient.Authenticate(Owner);
        var dto = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient, triggers: WebhookTrigger.GroupUpdated, targetId: Owner.Id.ToString());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateWebhook_UserTriggerWithAnExistingUser_StoresTheTarget()
    {
        // Arrange
        var target = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(Owner);
        var dto = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient, triggers: WebhookTrigger.UserUpdated, targetId: target.Id.ToString());

        // Act
        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        created.Response.TargetId.Should().Be(target.Id.ToString());
    }

    [Fact]
    public async Task CreateWebhook_FileTriggerWithAnyTarget_IsAccepted()
    {
        // Arrange: the file family is checked by read access on every event, not when the subscription is saved
        await _webApiClient.Authenticate(Owner);
        var dto = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient, triggers: WebhookTrigger.FileCreated, targetId: "987654321");

        // Act
        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        created.Response.TargetId.Should().Be("987654321");
    }

    [Fact]
    public async Task CreateWebhook_UserAndFileTriggersWithAnEntryId_IsAccepted()
    {
        // Arrange: a subscription that mixes families may be narrowed to an entry, as before the target was checked;
        // the user events then never match it
        await _webApiClient.Authenticate(Owner);
        var dto = await WebhooksTestData.CreateWebhookDtoAsync(
            _webhookReceiverClient, triggers: WebhookTrigger.UserUpdated | WebhookTrigger.FileCreated, targetId: "987654321");

        // Act
        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        created.Response.TargetId.Should().Be("987654321");
    }

    [Fact]
    public async Task CreateWebhook_UserTargetTheCreatorMayNotSee_IsRefusedLikeAnUnknownUser()
    {
        // Arrange: a User sees no other member, so the owner is hidden from them
        var member = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(member);
        var hidden = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient, triggers: WebhookTrigger.UserUpdated, targetId: Owner.Id.ToString());
        var unknown = await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient, triggers: WebhookTrigger.UserUpdated, targetId: Guid.NewGuid().ToString());

        // Act
        var hiddenException = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(hidden, TestContext.Current.CancellationToken));
        var unknownException = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(unknown, TestContext.Current.CancellationToken));

        // Assert: the answers are the same, so they do not tell whether the GUID exists
        hiddenException.ErrorCode.Should().Be(400);
        unknownException.ErrorCode.Should().Be(400);
        ErrorMessageOf(hiddenException).Should().NotBeNullOrEmpty().And.Be(ErrorMessageOf(unknownException));
    }

    [Fact]
    public async Task UpdateWebhook_UserTriggerWithATargetThatIsNotThere_ThrowsBadRequest()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated);

        var update = new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = true,
            Triggers = WebhookTrigger.UserUpdated,
            TargetId = Guid.NewGuid().ToString()
        };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await GetSubscriptionAsync(subscription.Id))!.TargetId.Should().BeNullOrEmpty();
    }

    /// <summary>
    /// Polls the records of one subscription until there is at least one, and returns them all, so that a record
    /// that should not be there shows up in the assertion rather than as an exception of the poll.
    /// </summary>
    private async Task<List<WebhooksLogDto>> WaitForRecordsAsync(int configId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var records = await GetLogAsync(configId);

            if (records.Count > 0 || DateTime.UtcNow >= deadline)
            {
                return records;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The <c>error.message</c> of an error response, without the parts that differ per request.</summary>
    private static string? ErrorMessageOf(ApiException exception)
    {
        using var document = JsonDocument.Parse(exception.ErrorContent?.ToString() ?? "{}");

        return document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message)
            ? message.GetString()
            : null;
    }

    private static string Resolve(string targetId)
    {
        return targetId == "unknown-guid" ? Guid.NewGuid().ToString() : targetId;
    }

    private async Task<WebhookSubscription> CreateTargetedSubscriptionAsync(WebhookTrigger trigger, string targetId)
    {
        var receiver = await CreateReceiverAsync();

        await _webApiClient.Authenticate(Owner);

        var dto = WebhooksTestData.CreateWebhookDto(enabled: true, triggers: trigger);
        dto.Uri = receiver.TargetUri;
        dto.TargetId = targetId;

        var created = await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken);

        return new WebhookSubscription(created.Response.Id, dto.Name, dto.Uri, dto.SecretKey);
    }

    private async Task<Guid> CreateGroupAsync()
    {
        await _peopleClient.Authenticate(Owner);

        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: $"group-{Guid.NewGuid():N}", groupManager: Owner.Id, members: [Owner.Id]),
            TestContext.Current.CancellationToken)).Response;

        return group.Id;
    }

    private async Task RenameGroupAsync(Guid groupId)
    {
        await _peopleClient.Authenticate(Owner);

        await _groupApi.UpdateGroupAsync(
            groupId,
            new UpdateGroupRequest(groupName: $"renamed-{Guid.NewGuid():N}", groupManager: Owner.Id),
            TestContext.Current.CancellationToken);
    }

    /// <summary>The <c>webhook</c> block of a delivery body: the subscription and, when narrowed, its target.</summary>
    private static JsonElement WebhookOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("webhook").Clone();
    }
}
