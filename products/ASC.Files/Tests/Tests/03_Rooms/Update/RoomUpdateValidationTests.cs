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
/// PUT /files/rooms/{id} - validation of the id itself (zero, negative, non-numeric, float) and of
/// updates targeting a room that no longer exists, plus the unknown-field body that the API should
/// reject but currently accepts.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUpdateValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateRoom_IdZero_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(0, new UpdateRoomRequest("x"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateRoom_IdNegative_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(-1, new UpdateRoomRequest("x"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// The typed SDK signature takes an <c>int</c> id, so a non-numeric path segment cannot be
    /// produced through it. Goes over raw HTTP (route the typed signature cannot express).
    /// </summary>
    [Fact]
    public async Task UpdateRoom_NonNumericId_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawUpdateRoom("abc", """{"title": "x"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Same reasoning as above: a float id cannot be bound to the typed <c>int</c> parameter.</summary>
    [Fact]
    public async Task UpdateRoom_FloatId_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawUpdateRoom("1.5", """{"title": "x"}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRoom_DeletedRoom_DoesNotResurrectIt()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Deleted Room");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Resurrect"), TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(403);

        var list = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Folders.Should().NotContain(f => f.Title == "Autotest Deleted Room");
    }

    /// <summary>
    /// BUG 82365: the API silently ignored undocumented parameters and applied the known fields
    /// instead of rejecting the request. Fixed by annotating <c>UpdateRoomRequest</c> with
    /// <c>JsonUnmappedMemberHandling.Disallow</c>. The DTO cannot carry an unknown property, so
    /// this goes over raw HTTP.
    /// </summary>
    [Fact]
    [Trait("Bug", "82365")]
    public async Task UpdateRoom_UnknownField_ShouldBeRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unknown Field");

        // Act
        using var response = await SendRawUpdateRoom(room.Id.ToString(), """{"title": "ok", "totallyBogus": 123}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Sends a raw PUT /api/2.0/files/rooms/{id} with an arbitrary path segment and JSON body,
    /// bypassing the typed SDK so that ids and payloads the SDK cannot express can be tested.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawUpdateRoom(string idSegment, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/2.0/files/rooms/{idSegment}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
