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
/// The file events that come out of the background file operations (FileDeleteOperation, FileMoveCopyOperation):
/// deleted, trashed, copied, moved and restored. Each one carries the file payload of the entry it is about. A
/// deletion is published after the delete has been committed, from the entry read before it, so the payload still
/// describes the file that is gone. The operations are asynchronous, so the events are polled for.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookFileOperationPayloadTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task FileDeleted_Immediately_DescribesTheFileAsItWas()
    {
        // Arrange
        var roomId = await CreateRoomAsync();
        var file = await CreateFileAsync(roomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileDeleted);

        // Act
        await _filesClient.Authenticate(Owner);
        await _filesApi.DeleteFileAsync(file.Id, new DeleteFileRequest { Immediately = true }, true, TestContext.Current.CancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.deleted", p => IdIs(p, file.Id));

        payload.Should().NotBeNull("an immediate delete publishes file.deleted once it has been committed");
        payload!.Value.GetProperty("title").GetString().Should().Be(file.Title);
        payload.Value.GetProperty("parentId").GetInt32().Should().Be(roomId, "the payload is the file as it was before the delete");
    }

    [Fact]
    public async Task FileTrashed_DescribesTheFile()
    {
        // Arrange
        var roomId = await CreateRoomAsync();
        var file = await CreateFileAsync(roomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileTrashed);

        // Act
        await TrashAsync(file.Id);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.trashed", p => IdIs(p, file.Id));

        payload.Should().NotBeNull("a delete that is not immediate moves the file to the trash");
        payload!.Value.GetProperty("title").GetString().Should().Be(file.Title);
    }

    [Fact]
    public async Task FileCopied_DescribesTheCopy()
    {
        // Arrange
        var sourceRoomId = await CreateRoomAsync();
        var targetRoomId = await CreateRoomAsync();
        var file = await CreateFileAsync(sourceRoomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCopied);

        // Act
        await RunBatchAsync(copy: true, file.Id, targetRoomId);

        // Assert: the payload is the new file, in the target room
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.copied", p => ParentIs(p, targetRoomId));

        payload.Should().NotBeNull();
        payload!.Value.GetProperty("title").GetString().Should().Be(file.Title);
        payload.Value.GetProperty("id").GetInt32().Should().NotBe(file.Id, "a copy is a file of its own");
    }

    [Fact]
    public async Task FileMoved_DescribesTheFileInItsNewPlace()
    {
        // Arrange
        var sourceRoomId = await CreateRoomAsync();
        var targetRoomId = await CreateRoomAsync();
        var file = await CreateFileAsync(sourceRoomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileMoved);

        // Act
        await RunBatchAsync(copy: false, file.Id, targetRoomId);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.moved", p => ParentIs(p, targetRoomId));

        payload.Should().NotBeNull();
        payload!.Value.GetProperty("title").GetString().Should().Be(file.Title);
    }

    [Fact]
    public async Task FileRestored_IsAMoveOutOfTheTrash()
    {
        // Arrange: a file in the trash. A catch-all subscription gets both events, told apart by event.trigger
        var roomId = await CreateRoomAsync();
        var file = await CreateFileAsync(roomId);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());
        await TrashAsync(file.Id);
        (await WaitForEventPayloadAsync(subscription.Id, "file.trashed", p => IdIs(p, file.Id))).Should().NotBeNull();

        // Act
        await RunBatchAsync(copy: false, file.Id, roomId);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "file.restored", p => ParentIs(p, roomId));

        payload.Should().NotBeNull("moving a file out of the trash publishes file.restored, not file.moved");
        payload!.Value.GetProperty("title").GetString().Should().Be(file.Title);
    }

    private async Task<(int Id, string Title)> CreateFileAsync(int roomId)
    {
        var title = $"file-{Guid.NewGuid():N}.docx";

        await _filesClient.Authenticate(Owner);

        var file = await _filesApi.CreateFileAsync(roomId, new CreateFileRequest(title), TestContext.Current.CancellationToken);

        return (file.Response.Id, title);
    }

    private async Task TrashAsync(int fileId)
    {
        await _filesClient.Authenticate(Owner);
        await _filesApi.DeleteFileAsync(fileId, new DeleteFileRequest { Immediately = false }, true, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Copies or moves one file into <paramref name="destinationId"/> as the owner. The suite has no operations
    /// client of its own, so one is bound to the Files client here.
    /// </summary>
    private async Task RunBatchAsync(bool copy, int fileId, int destinationId)
    {
        var operationsApi = new DocSpace.API.SDK.Api.Files.OperationsApi(
            _filesClient, new Configuration { BasePath = _filesClient.BaseAddress!.ToString().TrimEnd('/') });
        var request = new BatchRequestDto
        {
            ReturnSingleOperation = true,
            FileIds = [new BatchRequestDtoAllOfFileIds(fileId)],
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destinationId),
            DeleteAfter = false
        };

        await _filesClient.Authenticate(Owner);

        if (copy)
        {
            await operationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken);
        }
        else
        {
            await operationsApi.MoveBatchItemsAsync(request, TestContext.Current.CancellationToken);
        }
    }

    private static bool IdIs(JsonElement payload, int id)
    {
        return payload.TryGetProperty("id", out var value) && value.ValueKind == JsonValueKind.Number && value.GetInt32() == id;
    }

    private static bool ParentIs(JsonElement payload, int parentId)
    {
        return payload.TryGetProperty("parentId", out var value) && value.ValueKind == JsonValueKind.Number && value.GetInt32() == parentId;
    }
}
