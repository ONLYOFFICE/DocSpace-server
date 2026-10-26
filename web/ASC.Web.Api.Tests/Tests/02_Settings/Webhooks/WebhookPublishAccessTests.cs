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
/// Who gets an event at all (WebhookPublisher.GetWebhookConfigsAsync): the creator of the subscription has to be
/// able to read what the event is about, and has to be entitled to the trigger at the moment of the event, not only
/// when the subscription was saved: still active, still of a type the trigger is available for, and an admin while
/// the developer tools are limited for users.
/// </summary>
/// <remarks>
/// Every test has the owner subscribe to the same event as a control: one publication serves both, so once the
/// owner's record is there the other one would be too. The subscription under test is created first, which puts
/// it before the control in the order the publication walks them. form.submit is not covered: it is published from
/// the Document Server save callback, and the integration-test profile runs no Document Server.
/// </remarks>
[Trait("Category", "Settings")]
public class WebhookPublishAccessTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task FileCreated_CreatorWhoCannotReadTheRoom_GetsNothing()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var subject = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var control = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);
        var roomId = await CreateRoomAsync();

        // Act
        var title = await CreateFileAsync(roomId);

        // Assert
        await ShouldReachOnlyTheControlAsync(subject, control, p => TitleIs(p, title));
    }

    [Fact]
    public async Task FolderCreated_CreatorWhoCannotReadTheRoom_GetsNothing()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var subject = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FolderCreated, member);
        var control = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FolderCreated);
        var roomId = await CreateRoomAsync();
        var title = $"folder-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest(title), TestContext.Current.CancellationToken);

        // Assert
        await ShouldReachOnlyTheControlAsync(subject, control, p => TitleIs(p, title));
    }

    [Fact]
    public async Task RoomCreated_RoomAdminWhoIsNotInTheRoom_GetsNothing()
    {
        // Arrange: a RoomAdmin may subscribe to room.created, but sees only the rooms they are in
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var subject = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomCreated, roomAdmin);
        var control = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomCreated);
        var title = $"room-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: title, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken);

        // Assert
        await ShouldReachOnlyTheControlAsync(subject, control, p => TitleIs(p, title));
    }

    [Fact]
    public async Task GroupCreated_RoomAdminCreator_GetsTheEvent()
    {
        // Arrange: the counterpart of the demotion below - a RoomAdmin is entitled to group.* and may read groups
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.GroupCreated, roomAdmin);

        // Act
        var group = await CreateGroupAsync();

        // Assert
        (await WaitForPayloadAsync(subscription.Id, p => IdIs(p, group))).Should().NotBeNull();
    }

    [Fact]
    public async Task GroupCreated_CreatorDemotedToUser_GetsNothing()
    {
        // Arrange: group.* is not available to a User, who may not read groups either
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var subject = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.GroupCreated, roomAdmin);
        var control = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.GroupCreated);
        await ChangeTypeAsync(roomAdmin, EmployeeType.User);

        // Act
        var group = await CreateGroupAsync();

        // Assert: the subscription was saved by a RoomAdmin, but the event goes by who its creator is now
        await ShouldReachOnlyTheControlAsync(subject, control, p => IdIs(p, group));
    }

    [Fact]
    public async Task FileCreated_CreatorTerminated_GetsNothing()
    {
        // Arrange: a member who could read the room and subscribed while active
        var member = await InviteContact(EmployeeType.User);
        var subject = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var control = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);
        var roomId = await CreateRoomAsync(member);
        await TerminateAsync(member);

        // Act
        var title = await CreateFileAsync(roomId);

        // Assert
        await ShouldReachOnlyTheControlAsync(subject, control, p => TitleIs(p, title));
    }

    [Fact]
    public async Task FileCreated_DeveloperToolsLimitedForUsers_UserCreatorGetsNothing()
    {
        // Arrange: a member who can read the room, then the owner limits the developer tools to admins
        var member = await InviteContact(EmployeeType.User);
        var subject = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var control = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);
        var roomId = await CreateRoomAsync(member);

        await _webApiClient.Authenticate(Owner);
        await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsRequestDto(true), TestContext.Current.CancellationToken);

        // Act
        var title = await CreateFileAsync(roomId);

        // Assert: the member may no longer use the developer tools, so their subscription receives nothing either
        await ShouldReachOnlyTheControlAsync(subject, control, p => TitleIs(p, title));
    }

    private async Task ShouldReachOnlyTheControlAsync(WebhookSubscription subject, WebhookSubscription control, Func<JsonElement, bool> match)
    {
        (await WaitForPayloadAsync(control.Id, match)).Should().NotBeNull("the owner's control subscription gets the event");

        (await GetDeliveryRecordsAsync(subject.Id)).Should().BeEmpty("the creator of this subscription is not entitled to the event");
    }

    private async Task<string> CreateFileAsync(int roomId)
    {
        var title = $"file-{Guid.NewGuid():N}.docx";

        await _filesClient.Authenticate(Owner);
        await _filesApi.CreateFileAsync(roomId, new CreateFileRequest(title), TestContext.Current.CancellationToken);

        return title;
    }

    private async Task<Guid> CreateGroupAsync()
    {
        await _peopleClient.Authenticate(Owner);

        var group = await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: $"group-{Guid.NewGuid():N}", groupManager: Owner.Id), TestContext.Current.CancellationToken);

        return group.Response.Id;
    }

    private static bool TitleIs(JsonElement payload, string title)
    {
        return payload.TryGetProperty("title", out var value) && value.GetString() == title;
    }

    private static bool IdIs(JsonElement payload, Guid id)
    {
        return payload.TryGetProperty("id", out var value) && value.ValueKind == JsonValueKind.String && value.GetGuid() == id;
    }
}
