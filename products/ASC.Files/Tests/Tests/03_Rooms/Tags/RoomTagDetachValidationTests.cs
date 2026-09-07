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
/// Validation coverage of <c>DELETE /files/rooms/{id}/tags</c>: the room id and the names body.
/// Positive/functional behavior lives in <see cref="RoomTagDetachTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagDetachValidationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    // ── room id ──

    [Fact]
    public async Task DeleteRoomTags_NonExistentRoomId_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(999999999, new BatchTagsRequestDto(["GhostTag"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoomTags_DeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Detach From Deleted Room");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["NoTag"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoomTags_ArchivedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Detach From Archived Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ArchivedRoomTag"]), TestContext.Current.CancellationToken);
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ArchivedRoomTag"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <remarks>
    /// This is not asserting ideal behavior — the TS source documents it as a bug ("does not
    /// return 400") but still locks in the current response: a non-numeric room id resolves the
    /// same way as a numeric-but-nonexistent one, "record not found" -&gt; 404.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81703")]
    public async Task DeleteRoomTags_InvalidStringRoomId_NotFound()
    {
        // Arrange — the SDK's route parameter is a strongly typed int, so a non-numeric id cannot
        // be produced through the typed client; this goes through raw HTTP.
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawRoomTagsDelete("not-a-number", """{"names":["X"]}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <remarks>
    /// This is not asserting ideal behavior — the TS source documents it as a bug ("returns 500
    /// instead of 400") but still locks in the current response: room id 0 resolves as "record
    /// not found" -&gt; 404, same as any other nonexistent id.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81704")]
    public async Task DeleteRoomTags_RoomId0_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(0, new BatchTagsRequestDto(["X"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoomTags_NegativeRoomId_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(-1, new BatchTagsRequestDto(["X"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    // A non-integer (1.5) or empty room id segment cannot be produced through the typed int
    // route parameter, so these go through raw HTTP.
    [Theory]
    [InlineData("1.5")]
    [InlineData("")]
    public async Task DeleteRoomTags_InvalidRoomIdShape_NotFound(string roomIdSegment)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawRoomTagsDelete(roomIdSegment, """{"names":["X"]}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Note: a missing room id is enforced by the SDK's route — the endpoint cannot be invoked
    // without an id, so there is no separate test for it.

    // ── names body ──

    /// <remarks>
    /// A real client sending "no body" still sends <c>Content-Type: application/json</c> with an
    /// empty payload — that's what model binding sees as a missing body. Calling the typed SDK
    /// with a null <see cref="BatchTagsRequestDto"/> omits the header entirely, which ASP.NET
    /// rejects at the transport level (415) before the controller runs, so that would test the
    /// framework rather than DocSpace; this goes through raw HTTP instead to send the header a
    /// real client would.
    /// </remarks>
    [Fact]
    public async Task DeleteRoomTags_MissingBody_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Missing Body");

        // Act
        using var response = await SendRawRoomTagsDelete(room.Id.ToString(), "");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // The next two cases post a body BatchTagsRequestDto cannot express: its public constructor
    // requires a non-null Names and throws client-side otherwise, so a {} or {"names":null} body
    // can only be sent through raw HTTP.

    [Fact]
    public async Task DeleteRoomTags_MissingNamesField_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Missing Names");

        // Act
        using var response = await SendRawRoomTagsDelete(room.Id.ToString(), "{}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRoomTags_NullNames_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Null Names");

        // Act
        using var response = await SendRawRoomTagsDelete(room.Id.ToString(), """{"names":null}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRoomTags_EmptyNamesArray_IsNoOpAndReturns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Empty Detach");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["StayTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().Contain("StayTag");
    }

    // A non-array "names" value cannot be assigned to BatchTagsRequestDto.Names (List<string>),
    // so these go through raw HTTP.
    [Theory]
    [InlineData("""{"names":"TagA"}""")]
    [InlineData("""{"names":12345}""")]
    [InlineData("""{"names":{"foo":"bar"}}""")]
    public async Task DeleteRoomTags_NonArrayNames_BadRequest(string body)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Non-array Names");

        // Act
        using var response = await SendRawRoomTagsDelete(room.Id.ToString(), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRoomTags_NamesArrayContainingNumber_BadRequest()
    {
        // Arrange — a numeric element cannot be assigned into List<string>, so this also goes
        // through raw HTTP.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Bad Element Number");

        // Act
        using var response = await SendRawRoomTagsDelete(room.Id.ToString(), """{"names":["Valid",42]}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <remarks>
    /// A null element inside the names array used to be silently accepted (200) instead of
    /// producing a validation error (400). Unlike the cases above, a null string element is a
    /// value <see cref="BatchTagsRequestDto.Names"/> (List&lt;string&gt;) accepts client-side, so
    /// this goes through the typed SDK.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81705")]
    public async Task DeleteRoomTags_NamesArrayContainingNull_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Bad Element null");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto(["Valid", null!]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// Corrected against the product source (<c>BatchTagsRequestDto.Validate</c> in
    /// <c>ASC.Files.ApiModels.RequestDto</c>): an empty string in <c>names</c> fails model
    /// validation by design ("a tag name is never blank"), it is not treated as a no-op entry.
    /// The original assumption that the request would still succeed and leave "Keep" untouched
    /// does not hold against the current DTO.
    /// </remarks>
    [Fact]
    public async Task DeleteRoomTags_NamesArrayContainingEmptyString_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Empty String Name");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Keep"]), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([""]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// Same as above: a whitespace-only entry fails the same <c>string.IsNullOrWhiteSpace</c>
    /// check in <c>BatchTagsRequestDto.Validate</c>, so it is rejected rather than skipped.
    /// </remarks>
    [Fact]
    public async Task DeleteRoomTags_NamesArrayContainingSpacesOnlyString_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Spaces String Name");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Keep"]), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["   "]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task DeleteRoomTags_DuplicateNamesInArray_HandledAsSingleDetach()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Duplicate Detach");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["DupDetach", "OtherTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["DupDetach", "DupDetach"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain("DupDetach");
        (updated.Tags ?? []).Should().Contain("OtherTag");
    }

    /// <remarks>
    /// Mirrors bug 81689 on <c>DELETE /files/tags</c>: a 10000-character tag name is silently
    /// accepted (200) instead of a validation error (400) — no length guard on the detach path
    /// either.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81689")]
    public async Task DeleteRoomTags_VeryLongTagName_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Long Name Detach");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto([new string('a', 10000)]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// Sends a raw DELETE /api/2.0/files/rooms/{roomId}/tags with an arbitrary JSON body and room
    /// id segment, bypassing the typed SDK so that payloads the generated
    /// <see cref="BatchTagsRequestDto"/> cannot express — and route segments the typed <c>int</c>
    /// parameter cannot produce — can be tested.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawRoomTagsDelete(string roomIdSegment, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/2.0/files/rooms/{roomIdSegment}/tags")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
