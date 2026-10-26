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
/// The shape of the file, folder, room and agent payloads (FileEntryWebhookDtoHelper): each carries the fields of
/// its own kind, enums as numbers, absolute links and avatars, and none of the caller-relative state the REST DTOs
/// compute per reader. The payload is read from the delivery record, which holds the body exactly as the
/// subscription gets it.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookEntryPayloadTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    // FolderType, FileType and FileEntryType values as the payload carries them (the contract lists them per property)
    private const int FolderTypeVirtualRooms = 14;
    private const int FolderTypeCustomRoom = 19;
    private const int FolderTypeAiRoom = 31;
    private const int FileTypeDocument = 7;
    private const int FileEntryTypeFolder = 1;
    private const int FileEntryTypeFile = 2;

    /// <summary>The REST fields that depend on who reads the entry, which a delivery never carries.</summary>
    private static readonly string[] _callerRelativeFields = ["access", "security", "shared", "canShare", "isFavorite", "shareRecord"];

    [Fact]
    public async Task FileCreated_Payload_CarriesTheFileFields()
    {
        // Arrange
        var roomId = await CreateRoomAsync(RoomType.CustomRoom);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);
        var title = $"file-{Guid.NewGuid():N}.docx";

        // Act
        await _filesClient.Authenticate(Owner);
        var file = (await _filesApi.CreateFileAsync(roomId, new CreateFileRequest(title), TestContext.Current.CancellationToken)).Response;

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull();
        var p = payload!.Value;

        p.GetProperty("id").GetInt32().Should().Be(file.Id);
        p.GetProperty("parentId").GetInt32().Should().Be(roomId);
        p.GetProperty("fileEntryType").GetInt32().Should().Be(FileEntryTypeFile);
        p.GetProperty("fileExst").GetString().Should().Be(".docx");
        p.GetProperty("fileType").GetInt32().Should().Be(FileTypeDocument);
        p.GetProperty("version").GetInt32().Should().Be(1);
        p.GetProperty("versionGroup").GetInt32().Should().Be(1);
        p.TryGetProperty("isForm", out _).Should().BeFalse("a document is not a form, and false is left out of the body");

        foreach (var link in new[] { "viewUrl", "webUrl" })
        {
            var url = p.GetProperty(link).GetString();

            ShouldBeAbsoluteUrl(url, $"{link} must be usable by a receiver outside the portal");
            url.Should().Contain(file.Id.ToString(CultureInfo.InvariantCulture));
            url.Should().NotContainAny(["share=", "key="], "{0} carries no share token", link);
        }

        var createdBy = p.GetProperty("createdBy");
        createdBy.GetProperty("id").GetGuid().Should().Be(Owner.Id);
        ShouldBeAbsoluteUrl(createdBy.GetProperty("avatar").GetString(), "the avatar of the author must be usable outside the portal");
        p.GetProperty("created").GetString().Should().EndWith("Z", "entry dates are UTC");

        ShouldCarryNoCallerRelativeState(p);
    }

    [Fact]
    public async Task FolderCreated_Payload_CarriesTheFolderShape()
    {
        // Arrange
        var roomId = await CreateRoomAsync(RoomType.CustomRoom);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FolderCreated);
        var title = $"folder-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        var folder = (await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest(title), TestContext.Current.CancellationToken)).Response;

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "folder.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull();
        var p = payload!.Value;

        p.GetProperty("id").GetInt32().Should().Be(folder.Id);
        p.GetProperty("parentId").GetInt32().Should().Be(roomId);
        p.GetProperty("fileEntryType").GetInt32().Should().Be(FileEntryTypeFolder);
        p.GetProperty("rootFolderType").GetInt32().Should().Be(FolderTypeVirtualRooms);
        p.GetProperty("createdBy").GetProperty("id").GetGuid().Should().Be(Owner.Id);
        p.TryGetProperty("roomType", out _).Should().BeFalse("a plain folder gets the folder payload, not the room one");
        p.TryGetProperty("fileExst", out _).Should().BeFalse("file fields belong to the file payload");

        ShouldCarryNoCallerRelativeState(p);
    }

    [Fact]
    public async Task RoomCreated_Payload_CarriesTheRoomSettings()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.RoomCreated);
        var title = $"room-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: title, indexing: true, denyDownload: true, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "room.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull();
        var p = payload!.Value;

        p.GetProperty("id").GetInt32().Should().Be(room.Id);
        p.GetProperty("fileEntryType").GetInt32().Should().Be(FileEntryTypeFolder);
        p.GetProperty("roomType").GetInt32().Should().Be((int)RoomType.CustomRoom);
        p.GetProperty("type").GetInt32().Should().Be(FolderTypeCustomRoom);
        p.GetProperty("indexing").GetBoolean().Should().BeTrue();
        p.GetProperty("denyDownload").GetBoolean().Should().BeTrue();
        p.GetProperty("createdBy").GetProperty("id").GetGuid().Should().Be(Owner.Id);
        p.TryGetProperty("logo", out _).Should().BeFalse("the logo needs per-request URL building and is left to the REST API");

        ShouldCarryNoCallerRelativeState(p);
    }

    [Fact]
    public async Task AgentCreated_Payload_IsTheRoomShapeOfAnAiRoom()
    {
        // Arrange: the pinned SDK enum has no agent.* bits, so a catch-all subscription stands in for agent.created
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        var title = $"agent-{Guid.NewGuid():N}";

        // Act
        await _filesClient.Authenticate(Owner);
        var agent = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: title, roomType: RoomType.AiRoom), TestContext.Current.CancellationToken)).Response;

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "agent.created", p => p.GetProperty("title").GetString() == title);

        payload.Should().NotBeNull("an AI room is published as agent.created");
        var p = payload!.Value;

        p.GetProperty("id").GetInt32().Should().Be(agent.Id);
        p.GetProperty("roomType").GetInt32().Should().Be((int)RoomType.AiRoom);
        p.GetProperty("type").GetInt32().Should().Be(FolderTypeAiRoom);
        p.GetProperty("createdBy").GetProperty("id").GetGuid().Should().Be(Owner.Id);
        p.TryGetProperty("chatSettings", out _).Should().BeFalse("the chat settings are left to the REST API");

        // both events would come from the same publication, so the agent.created record found above settles it
        (await WaitForEventPayloadAsync(subscription.Id, "room.created", r => r.GetProperty("title").GetString() == title, timeoutSeconds: 0))
            .Should().BeNull("an agent is not also announced as a room");
    }

    private async Task<int> CreateRoomAsync(RoomType roomType)
    {
        await _filesClient.Authenticate(Owner);

        var room = await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: $"room-{Guid.NewGuid():N}", roomType: roomType), TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    private static void ShouldCarryNoCallerRelativeState(JsonElement payload)
    {
        foreach (var field in _callerRelativeFields)
        {
            payload.TryGetProperty(field, out _).Should().BeFalse("{0} depends on the reader, and a delivery has none", field);
        }
    }
}
