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

namespace ASC.Files.Tests.Tests._03_Rooms.Deletion;

/// <summary>
/// <c>DELETE /files/rooms/{id}</c> — id validation (0, negative, huge, non-numeric) and request
/// body validation (an omitted <c>deleteAfter</c>, and the wrong-typed values a typed
/// <see cref="DeleteRoomRequest"/> cannot carry: <c>null</c>, a string, a number). Positive
/// functional coverage lives in <see cref="RoomDeleteTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomDeleteValidationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999999999)]
    public async Task DeleteRoom_InvalidId_NotFound(int roomId)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_NonNumericId_NotFound()
    {
        // Arrange - the route's id is a typed int, so a non-numeric value can only be sent as a
        // raw request.
        await _filesClient.Authenticate(Owner);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Delete, "api/2.0/files/rooms/abc")
        {
            Content = new StringContent("{\"deleteAfter\":false}", Encoding.UTF8, "application/json")
        };
        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRoom_DeleteAfterOmitted_AcceptedAndDefaultsToFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete No deleteAfter");

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Fact]
    [Trait("Bug", "81697")]
    public async Task DeleteRoom_DeleteAfterNull_ProducesTrackableOperation()
    {
        // Same symptom as deleteAfter:true (bug 81698): HTTP returns 200, but the operation is not
        // pushed to fileops, so waitLongOperation cannot find a record. The DTO's deleteAfter is a
        // non-nullable bool, so null can only be sent as a raw request.
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete deleteAfter null");

        // Act
        using var response = await SendRawDelete(room.Id, "{\"deleteAfter\":null}");
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().NotBeNullOrEmpty("deleteAfter:null must still produce a trackable operation");
        operations.Should().OnlyContain(o => o.Finished);
    }

    [Fact]
    public async Task DeleteRoom_DeleteAfterAsString_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete deleteAfter string");

        // Act
        using var response = await SendRawDelete(room.Id, "{\"deleteAfter\":\"false\"}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRoom_DeleteAfterAsNumber_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete deleteAfter number");

        // Act
        using var response = await SendRawDelete(room.Id, "{\"deleteAfter\":1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage> SendRawDelete(int roomId, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/2.0/files/rooms/{roomId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
