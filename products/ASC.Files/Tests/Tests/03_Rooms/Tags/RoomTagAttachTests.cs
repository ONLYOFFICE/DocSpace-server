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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// PUT /files/rooms/{id}/tags (addRoomTags) — functional and body-validation coverage.
/// Access-level coverage lives in <c>Permissions/RoomTagAttachPermissionsTests</c> and is not
/// duplicated here.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagAttachTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AddRoomTags_OneExistingTag_AddsToRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("SingleTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Single Tag");

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["SingleTag"]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Should().HaveCount(1);
        updated.Tags.Should().Contain("SingleTag");
    }

    [Fact]
    public async Task AddRoomTags_ReturnsFullRoomObject()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("FullRoomTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Full Object");

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["FullRoomTag"]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
        updated.Title.Should().Be("Autotest Room Full Object");
        updated.RoomType.Should().Be(RoomType.CustomRoom);
        updated.Tags.Should().Contain("FullRoomTag");
    }

    [Fact]
    public async Task AddRoomTags_PreservesAlreadyAssignedTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("PreservedA"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("PreservedB"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Preserve Tags");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["PreservedA"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["PreservedB"]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Should().HaveCount(2);
        updated.Tags.Should().Contain("PreservedA").And.Contain("PreservedB");
    }

    [Fact]
    public async Task AddRoomTags_Idempotent_WhenTagAlreadyAssigned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("IdempotentTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Idempotent");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["IdempotentTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["IdempotentTag"]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Should().HaveCount(1);
        updated.Tags.Should().Contain("IdempotentTag");
    }

    [Fact]
    public async Task AddRoomTags_TagCanBeReAddedAfterDetach()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("ReAddTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room ReAdd");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ReAddTag"]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ReAddTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["ReAddTag"]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Should().Contain("ReAddTag");
    }

    [Fact]
    public async Task AddRoomTags_DuplicateNamesInRequest_NotDuplicatedInResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("DupTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Duplicate Names");

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["DupTag", "DupTag"]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Count(t => t == "DupTag").Should().Be(1);
    }

    // The TS suite asserts on `data.statusCode`, but the SDK throws ApiException on any non-2xx
    // response — an ApiException with the matching status code is the equivalent typed assertion.

    [Fact]
    public async Task AddRoomTags_NonExistentRoomId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("GhostRoomTag"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.AddRoomTagsAsync(
                999999999,
                new BatchTagsRequestDto(["GhostRoomTag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task AddRoomTags_DeletedRoom_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("DeletedRoomTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room To Delete For Tag");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.AddRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto(["DeletedRoomTag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task AddRoomTags_EmptyNamesArray_NoOpReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room Empty Names");

        // Act
        var updated = (await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto([]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().BeEmpty();
    }

    // The typed DTO's constructor requires a non-null Names list and throws client-side for null,
    // and there is no way to construct it with the field omitted entirely, so both bodies go
    // through raw HTTP.

    [Fact]
    public async Task AddRoomTags_MissingNamesField_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room Missing Names");

        // Act
        using var response = await AddRoomTagsRaw(room.Id, "{}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddRoomTags_NullNames_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room Null Names");

        // Act
        using var response = await AddRoomTagsRaw(room.Id, """{"names":null}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddRoomTags_AddedTags_AppearInGetRoomInfo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("VisibleTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Get Info Tags");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["VisibleTag"]), TestContext.Current.CancellationToken);

        // Act
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        roomInfo.Tags.Should().Contain("VisibleTag");
    }

    /// <summary>
    /// Sends a raw PUT /api/2.0/files/rooms/{id}/tags with an arbitrary JSON body, bypassing the
    /// typed SDK so that bodies with a missing or null <c>names</c> field can be tested.
    /// </summary>
    private async Task<HttpResponseMessage> AddRoomTagsRaw(int roomId, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/2.0/files/rooms/{roomId}/tags")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
