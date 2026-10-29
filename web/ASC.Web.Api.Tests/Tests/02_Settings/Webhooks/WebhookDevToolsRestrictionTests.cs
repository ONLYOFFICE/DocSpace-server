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
/// The webhook endpoints while the developer tools are restricted to admins
/// (<c>TenantDevToolsAccessSettings.LimitedAccessForUsers</c>, set through
/// <c>POST api/2.0/security/devtoolsaccess</c>): every one of them refuses a member who is not an admin,
/// even for the subscription that member created before the restriction, and keeps working for an admin.
/// The trigger catalogue (<c>GET settings/webhook/triggers</c>) is not behind the restriction and is not listed.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookDevToolsRestrictionTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    public static TheoryData<string> RestrictedEndpoints =>
    [
        "list",
        "create",
        "update",
        "enable",
        "remove",
        "log",
        "retry",
        "retryBatch"
    ];

    [Theory]
    [MemberData(nameof(RestrictedEndpoints))]
    public async Task RestrictedDevTools_User_IsRefused(string endpoint)
    {
        // Arrange: the member's own subscription and a delivery record of it exist before the restriction is switched
        // on, so that the record endpoints would accept the member without it
        var member = await InviteContact(EmployeeType.User);
        var memberSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var recordId = await CreateRecordAsync(member, memberSubscription);

        await RestrictDevToolsAsync();

        // Act
        await _webApiClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CallAsync(endpoint, memberSubscription, recordId));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestrictedDevTools_RoomAdmin_IsRefused()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await RestrictDevToolsAsync();

        // Act
        await _webApiClient.Authenticate(roomAdmin);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestrictedDevTools_DocSpaceAdmin_CanStillListAndCreate()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await RestrictDevToolsAsync();

        // Act
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), creator: admin);
        await _webApiClient.Authenticate(admin);
        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);
        var logs = await _webhooksApi.GetWebhooksLogsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        list.Response.Should().Contain(w => w.Configs.Id == subscription.Id);
        logs.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RestrictedDevTools_User_CanStillReadTheTriggerCatalogue()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        await RestrictDevToolsAsync();

        // Act
        await _webApiClient.Authenticate(member);
        var triggers = await _webhooksApi.GetWebhookTriggersAsync(TestContext.Current.CancellationToken);

        // Assert
        triggers.Should().NotBeNull();
    }

    private async Task RestrictDevToolsAsync()
    {
        await _webApiClient.Authenticate(Owner);
        await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsRequestDto(true), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A delivery record of <paramref name="subscription"/>, the member's own, for the endpoints that act on a
    /// record: a record of someone else's subscription would be refused to the member with 403 anyway, restriction or
    /// not. The owner creates a file in a room the member may read, which publishes <c>file.created</c> to it.
    /// </summary>
    private async Task<int> CreateRecordAsync(User member, WebhookSubscription subscription)
    {
        var roomId = await CreateRoomAsync(member);
        await CreateFileAsync(roomId);

        // the member may read the room the file is in, so the subscription has the record
        return (await WaitForRecordAsync(subscription.Id, _ => true)).Id;
    }

    private async Task CallAsync(string endpoint, WebhookSubscription subscription, int recordId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var update = new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = false
        };

        switch (endpoint)
        {
            case "list":
                await _webhooksApi.GetTenantWebhooksAsync(cancellationToken);
                break;
            case "create":
                await _webhooksApi.CreateWebhookAsync(await WebhooksTestData.CreateWebhookDtoAsync(_webhookReceiverClient), cancellationToken);
                break;
            case "update":
                await _webhooksApi.UpdateWebhookAsync(update, cancellationToken);
                break;
            case "enable":
                await _webhooksApi.EnableWebhookAsync(update, cancellationToken);
                break;
            case "remove":
                await _webhooksApi.RemoveWebhookAsync(subscription.Id, cancellationToken);
                break;
            case "log":
                await _webhooksApi.GetWebhooksLogsAsync(cancellationToken: cancellationToken);
                break;
            case "retry":
                await _webhooksApi.RetryWebhookAsync(recordId, cancellationToken);
                break;
            case "retryBatch":
                await _webhooksApi.RetryWebhooksAsync(new WebhookRetryRequestDto([recordId]), cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null);
        }
    }
}
