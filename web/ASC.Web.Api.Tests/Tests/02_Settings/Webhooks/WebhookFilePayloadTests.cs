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
/// The users a file event refers to (<c>createdBy</c>, <c>updatedBy</c>) are cut for each subscription owner
/// by the rule the REST API applies to a reader: a <c>User</c> or a <c>Guest</c> gets no profile link of
/// another user, because the link is a people search by email (bug 81093). The quota of a room goes only to an
/// owner who may edit the room, as in the REST API. The payload is read from the delivery record, which holds the
/// body exactly as the subscription gets it.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookFilePayloadTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "81093")]
    public async Task FileCreated_UserSubscription_OmitsTheProfileLinkOfTheAuthor()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomSharedWithAsync(member);
        var memberSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var ownerSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);

        // Act
        var title = await CreateFileAsync(roomId);

        // Assert
        var ownerPayload = await WaitForFilePayloadAsync(ownerSubscription.Id, title);

        ownerPayload.Should().NotBeNull("the owner may read every room");
        ownerPayload!.Value.GetProperty("createdBy").TryGetProperty("profileUrl", out _).Should().BeTrue();

        var memberPayload = await WaitForFilePayloadAsync(memberSubscription.Id, title);

        memberPayload.Should().NotBeNull("the member may read the room the file is in");

        var createdBy = memberPayload!.Value.GetProperty("createdBy");
        createdBy.GetProperty("id").GetGuid().Should().Be(Owner.Id);
        createdBy.TryGetProperty("profileUrl", out _).Should().BeFalse("the profile link carries the email of the author");
        memberPayload.Value.GetProperty("updatedBy").TryGetProperty("profileUrl", out _).Should().BeFalse();
    }

    [Fact]
    public async Task RoomUpdated_QuotaLimit_GoesOnlyToASubscriberWhoMayEditTheRoom()
    {
        // Arrange: room quotas on, and a member who may only read the room
        const int defaultQuota = 512 * 1024 * 1024;
        const long quota = 1024L * 1024 * 1024;
        var cancellationToken = TestContext.Current.CancellationToken;

        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(defaultQuota)), cancellationToken);

        var member = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomSharedWithAsync(member);
        var memberSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomUpdated, member);
        var ownerSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomUpdated);

        // Act: a quota of the room's own, set together with a new title, which publishes room.updated
        var title = $"room-{Guid.NewGuid():N}";

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(title: title, quota: quota), cancellationToken);

        // Assert: the REST API shows the quota only to those who may edit the room, and so does the payload
        var ownerPayload = await WaitForFilePayloadAsync(ownerSubscription.Id, title);

        ownerPayload.Should().NotBeNull("the owner may read every room");
        ownerPayload!.Value.GetProperty("quotaLimit").GetInt64().Should().Be(quota);

        var memberPayload = await WaitForFilePayloadAsync(memberSubscription.Id, title);

        memberPayload.Should().NotBeNull("the member may read the room");
        memberPayload!.Value.TryGetProperty("quotaLimit", out _).Should().BeFalse("the member may not edit the room");
    }

    private async Task<int> CreateRoomSharedWithAsync(User member)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _filesClient.Authenticate(Owner);

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: $"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom), cancellationToken)).Response;

        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest(invitations: [new RoomInvitation(member.Id, DocSpace.API.SDK.Model.FileShare.Read)], notify: false),
            cancellationToken);

        return room.Id;
    }

    private async Task<string> CreateFileAsync(int roomId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var title = $"file-{Guid.NewGuid():N}.docx";

        await _filesClient.Authenticate(Owner);

        await _filesApi.CreateFileAsync(roomId, new CreateFileRequest(title), cancellationToken);

        return title;
    }

    private Task<JsonElement?> WaitForFilePayloadAsync(int configId, string title)
    {
        return WaitForPayloadAsync(configId, p => p.TryGetProperty("title", out var t) && t.GetString() == title);
    }
}
