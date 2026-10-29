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
/// The parts of a file, folder or room payload that a plain read of the entry does not give and the payload builder
/// (FileEntryWebhookDtoHelper) has to work out: the type of the room an entry is in, the schema a folder.* event
/// carries a room in, and who of the subscription owners gets the quota of a room - the same readers the REST API
/// shows it to. The payload is read from the delivery record, which holds the body exactly as the subscription gets it.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookEntryContextTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    private const int FolderTypeCustomRoom = 19;
    private const int FileEntryTypeFolder = 1;
    private const long RoomQuota = 1024L * 1024 * 1024;

    /// <summary>
    /// <c>parentRoomType</c> was mapped from the entity, which only a listing fills, so it was absent from every
    /// payload while documented as the type of the room the entry is in.
    /// </summary>
    [Fact]
    public async Task FileCreated_InARoom_CarriesTheTypeOfTheRoom()
    {
        // Arrange
        var roomId = await CreateRoomAsync();
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);

        // Act
        var (_, title) = await CreateFileAsync(roomId);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull();
        payload!.Value.GetProperty("parentRoomType").GetInt32().Should().Be(FolderTypeCustomRoom, "the file is in a custom room");
    }

    [Fact]
    public async Task FolderCreated_InAFolderOfARoom_CarriesTheTypeOfTheRoom()
    {
        // Arrange: a folder two levels down, so the room is found up the chain and not as the direct parent
        var cancellationToken = TestContext.Current.CancellationToken;
        var roomId = await CreateRoomAsync();

        await _filesClient.Authenticate(Owner);
        var outer = (await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest($"outer-{Guid.NewGuid():N}"), cancellationToken)).Response;

        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FolderCreated);
        var title = $"inner-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        await _foldersApi.CreateFolderAsync(outer.Id, new CreateFolderRequest(title), cancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "folder.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull();
        payload!.Value.GetProperty("parentRoomType").GetInt32().Should().Be(FolderTypeCustomRoom);
        payload.Value.TryGetProperty("isShareable", out _).Should().BeFalse("whether the caller may share a folder is caller-relative state");
    }

    [Fact]
    public async Task RoomCreated_Payload_HasNoParentRoomType()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomCreated);
        var title = $"room-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(title: title, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "room.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull();
        payload!.Value.TryGetProperty("parentRoomType", out _).Should().BeFalse("a room is not inside a room");
    }

    /// <summary>
    /// A download of a whole room publishes folder.downloaded for it, and the payload was picked by the kind of entry:
    /// the room payload went out under a folder.* trigger, whose schema is the folder payload. It now follows the
    /// trigger, and <c>type</c> tells the receiver it is a room.
    /// </summary>
    [Fact]
    public async Task FolderDownloaded_OfARoom_IsTheFolderPayload()
    {
        // Arrange: the pinned SDK enum has no download bits, so a catch-all subscription stands in for folder.downloaded
        var cancellationToken = TestContext.Current.CancellationToken;
        var roomId = await CreateRoomAsync();
        await CreateFileAsync(roomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());

        // Act
        await _filesClient.Authenticate(Owner);
        await _operationsApi.BulkDownloadAsync(
            new DownloadRequestDto(folderIds: [new DownloadRequestDtoAllOfFolderIds(roomId)]), cancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "folder.downloaded", p => p.GetProperty("id").GetInt32() == roomId, 60);

        payload.Should().NotBeNull("downloading a room publishes folder.downloaded for it");
        var p = payload!.Value;

        p.GetProperty("fileEntryType").GetInt32().Should().Be(FileEntryTypeFolder);
        p.GetProperty("type").GetInt32().Should().Be(FolderTypeCustomRoom, "the folder payload tells a room by its folder type");
        p.TryGetProperty("roomType", out _).Should().BeFalse("room fields belong to the room.* payload, not to folder.*");
        p.TryGetProperty("indexing", out _).Should().BeFalse();
    }

    /// <summary>
    /// The quota of a room went only to an owner who may edit the room, and nobody may edit a room in the archive, so
    /// room.archived never carried it - although the payload keeps the quota wherever the room lies. It now goes by the
    /// predicate of the REST API, where the right to delete a room in the archive counts.
    /// </summary>
    [Fact]
    public async Task RoomArchived_QuotaLimit_GoesToTheOwnerWhoMayDeleteTheRoom()
    {
        // Arrange: a room with a quota of its own
        var cancellationToken = TestContext.Current.CancellationToken;
        var roomId = await CreateRoomWithQuotaAsync();
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomArchived);

        // Act
        await _filesClient.Authenticate(Owner);
        await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(deleteAfter: false), cancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "room.archived", p => p.GetProperty("id").GetInt32() == roomId, 60);

        payload.Should().NotBeNull("archiving the room publishes room.archived once the operation has moved it");
        payload!.Value.GetProperty("quotaLimit").GetInt64().Should().Be(RoomQuota, "the owner may delete the archived room");
    }

    /// <summary>
    /// The REST API shows the quota of a room to its editors as well (the Editor role), who may neither edit the
    /// room nor create in it; the payload had dropped it for them.
    /// </summary>
    [Fact]
    public async Task RoomUpdated_QuotaLimit_GoesToAnEditorOfTheRoom()
    {
        // Arrange: a member with the Editor role in the room
        var cancellationToken = TestContext.Current.CancellationToken;
        var editor = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomWithQuotaAsync();

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(invitations: [new RoomInvitation(editor.Id, DocSpace.API.SDK.Model.FileShare.Editing)], notify: false),
            cancellationToken);

        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomUpdated, editor);

        // Act
        var title = $"room-{Guid.NewGuid():N}";

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(title: title), cancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "room.updated", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull("the editor may read the room");
        payload!.Value.GetProperty("quotaLimit").GetInt64().Should().Be(RoomQuota, "the REST API shows the quota to an editor of the room");
    }

    /// <summary>A custom room of the owner with a quota of its own; room quotas are turned on for it.</summary>
    private async Task<int> CreateRoomWithQuotaAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(512 * 1024 * 1024)), cancellationToken);

        var roomId = await CreateRoomAsync();

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(quota: RoomQuota), cancellationToken);

        return roomId;
    }
}
