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
    /// <summary>
    /// Bug 81093: the <c>createdBy</c> and <c>updatedBy</c> of a file event carried the <c>profileUrl</c> of the
    /// author to every subscriber, a people search by the author's email, also to a <c>User</c> the REST API never
    /// shows that link to. The payload is now cut per subscriber like the REST answer, so the owner keeps the link
    /// and a <c>User</c> who may read the room gets the author without it.
    /// </summary>
    [Fact]
    [Trait("Bug", "81093")]
    public async Task FileCreated_UserSubscription_OmitsTheProfileLinkOfTheAuthor()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomAsync(member);
        var memberSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var ownerSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);

        // Act
        var (_, title) = await CreateFileAsync(roomId);

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
    public async Task RoomUpdated_QuotaLimit_IsWithheldFromAMemberWhoMayOnlyReadTheRoom()
    {
        // Arrange: room quotas on, and a member who may only read the room
        const int defaultQuota = 512 * 1024 * 1024;
        const long quota = 1024L * 1024 * 1024;
        var cancellationToken = TestContext.Current.CancellationToken;

        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(defaultQuota)), cancellationToken);

        var member = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomAsync(member);
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

    /// <summary>
    /// The lock lives in a tag, which the plain read of the file that every payload is built from does not fill: the
    /// file.updated raised by locking a file said nothing about the lock, so a receiver could not tell a lock from an
    /// unlock. The payload now reads the tag, with the user who locked the file.
    /// </summary>
    [Fact]
    public async Task FileUpdated_ByLockingTheFile_CarriesTheLockAndWhoSetIt()
    {
        // Arrange
        var roomId = await CreateRoomAsync();
        var (fileId, _) = await CreateFileAsync(roomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileUpdated);

        // Act
        await _filesClient.Authenticate(Owner);
        await _filesApi.LockFileAsync(fileId, new LockFileRequest(lockFile: true), TestContext.Current.CancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.updated",
            p => p.GetProperty("id").GetInt32() == fileId && p.TryGetProperty("locked", out _));

        payload.Should().NotBeNull("locking the file raises file.updated with the lock in it");
        payload!.Value.GetProperty("locked").GetBoolean().Should().BeTrue();
        payload.Value.GetProperty("lockedBy").GetProperty("id").GetGuid().Should().Be(Owner.Id);
    }

    /// <summary>
    /// A folder deleted at once loses its content before the folder row goes, and the payload, built from a fresh read
    /// of the folder after that, described an emptied folder (<c>filesCount</c> 0). It is now built before the content
    /// is deleted, so it describes the folder as the delete found it.
    /// </summary>
    [Fact]
    public async Task FolderDeleted_Immediately_DescribesTheFolderWithItsContent()
    {
        // Arrange: a folder with two files in a room
        var cancellationToken = TestContext.Current.CancellationToken;
        var roomId = await CreateRoomAsync();

        await _filesClient.Authenticate(Owner);

        var title = $"folder-{Guid.NewGuid():N}";
        var folder = (await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest(title), cancellationToken)).Response;

        await CreateFileAsync(folder.Id);
        await CreateFileAsync(folder.Id);

        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FolderDeleted);

        // Act: deleted at once, not moved to the trash
        await _filesClient.Authenticate(Owner);
        await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolderRequest(deleteAfter: false, immediately: true), cancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "folder.deleted", p => p.GetProperty("title").GetString() == title, 60);

        payload.Should().NotBeNull("deleting the folder raises folder.deleted once the queued operation deletes it");
        // a count of 0 is left out of the body
        var filesCount = payload!.Value.TryGetProperty("filesCount", out var count) ? count.GetInt32() : 0;
        filesCount.Should().Be(2, "the payload describes the folder as it was deleted, not after its files went");
    }

    private Task<JsonElement?> WaitForFilePayloadAsync(int configId, string title)
    {
        return WaitForPayloadAsync(configId, p => p.TryGetProperty("title", out var t) && t.GetString() == title);
    }
}
