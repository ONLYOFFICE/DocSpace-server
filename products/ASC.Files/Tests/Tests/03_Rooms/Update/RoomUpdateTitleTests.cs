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

namespace ASC.Files.Tests.Tests._03_Rooms.Update;

/// <summary>
/// PUT /files/rooms/{id} - title validation and the partial-update / empty-body contract: only the
/// fields present in the body are touched, blank titles are a no-op rather than an error, forbidden
/// characters are sanitized rather than rejected, and length is enforced only past the boundary.
/// Also covers wrong-typed field bodies, which the DTO cannot express and therefore go over raw
/// HTTP (see the tests rule's carve-out for wrong-typed JSON values).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUpdateTitleTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateRoom_PartialUpdate_KeepsOtherFields()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Partial Base");

        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(tags: ["AutotestPartialTag"], color: "AABBCC", cover: coverId),
            TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("Autotest Partial Updated"),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Partial Updated");
        info.Tags.Should().Contain("AutotestPartialTag");
        info.Logo.Color.Should().Be("AABBCC");
        info.Logo.Cover.Id.Should().Be(coverId);
    }

    [Fact]
    public async Task UpdateRoom_EmptyBody_IsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Empty Body");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest Empty Body");

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest Empty Body");
    }

    /// <summary>
    /// Forbidden chars in the title are silently replaced with `_` - one per code unit, so an emoji
    /// (a surrogate pair) becomes two underscores. No 400.
    /// </summary>
    [Theory]
    [InlineData("Room \"Test\"", "Room _Test_")]
    [InlineData("Room <Test>", "Room _Test_")]
    [InlineData("Room / Test", "Room _ Test")]
    [InlineData("Room \\ Test", "Room _ Test")]
    [InlineData("Party \U0001F389 Time", "Party __ Time")]
    public async Task UpdateRoom_TitleWithForbiddenChars_IsSanitized(string raw, string sanitized)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Sanitize Base");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(raw),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be(sanitized);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(sanitized);
    }

    [Fact]
    public async Task UpdateRoom_SingleCharTitle_IsAccepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Min Title Base");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("A"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("A");
    }

    [Fact]
    public async Task UpdateRoom_TitleAtMaxLength_IsAccepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Max Title Base");
        var title = new string('L', 170);

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(title),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be(title);
    }

    [Fact]
    public async Task UpdateRoom_TitleOverMaxLength_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string original = "Autotest Overlong Base";
        var room = await CreateCustomRoom(original);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(new string('L', 171)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(original);
    }

    [Fact]
    public async Task UpdateRoom_WhitespaceOnlyTitle_IsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string original = "Autotest Whitespace Base";
        var room = await CreateCustomRoom(original);

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest("   "),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be(original);
    }

    [Fact]
    public async Task UpdateRoom_NullTitle_IsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string original = "Autotest Null Title Base";
        var room = await CreateCustomRoom(original);

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(title: null),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be(original);
    }

    /// <summary>
    /// Wrong-typed fields are rejected with 400 and leave the room unchanged. The strongly-typed
    /// <see cref="UpdateRoomRequest"/> constructor cannot carry a number where a string is expected
    /// (and vice versa), so these go over raw HTTP with a hand-built JSON body.
    /// </summary>
    [Theory]
    [InlineData("""{"title": 123}""")]
    [InlineData("""{"tags": "notarray"}""")]
    [InlineData("""{"color": 123}""")]
    [InlineData("""{"denyDownload": "yes"}""")]
    [InlineData("""{"indexing": "no"}""")]
    public async Task UpdateRoom_WrongTypedField_Returns400AndLeavesRoomUnchanged(string json)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string original = "Autotest Wrong Type Base";
        var room = await CreateCustomRoom(original);

        // Act
        using var response = await SendRawUpdateRoom(room.Id, json);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(original);
    }

    /// <summary>
    /// Sends a raw PUT /api/2.0/files/rooms/{id} with an arbitrary JSON body, bypassing the typed
    /// SDK so that wrong-typed and malformed payloads can be tested.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawUpdateRoom(int id, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/2.0/files/rooms/{id}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
