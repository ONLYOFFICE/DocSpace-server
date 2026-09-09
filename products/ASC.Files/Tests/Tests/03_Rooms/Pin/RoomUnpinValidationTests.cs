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

namespace ASC.Files.Tests.Tests._03_Rooms.Pin;

/// <summary>
/// <c>PUT /files/rooms/{id}/unpin</c> — invalid ids and rooms that are no longer in an unpinnable
/// state (deleted or archived). Mirrors <see cref="RoomPinValidationTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUnpinValidationTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    /// <remarks>
    /// Bug 82366, the unpin half of bug 81850: a missing room answered 403 because the service threw
    /// <c>InvalidOperationException</c>, which the middleware maps to Forbidden. Fixed in
    /// <c>FileStorageService.SetPinnedStatusAsync</c>. Asserts 404 rather than the 400 the TypeScript
    /// suite asked for — see the pin test for why the whole <c>rooms/{id}</c> family answers 404 here.
    /// </remarks>
    [Trait("Bug", "82366")]
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999999999)]
    public async Task UnpinRoom_NonExistentId_ReturnsNotFound(int id)
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UnpinRoom_NonNumericId_ReturnsNotFound()
    {
        // The typed SDK signature takes an int id, so a non-numeric path segment can only be sent
        // over raw HTTP.
        await _filesClient.Authenticate(Owner);

        using var response = await _filesClient.PutAsync("api/2.0/files/rooms/abc/unpin", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnpinRoom_DeletedRoom_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Deleted");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act & Assert — mirrors pin: the room is gone, so it is reported missing.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UnpinRoom_ArchivedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Archived");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        await ArchiveRoom(room.Id);

        // Act & Assert — mirrors pin (archived rooms reject pin/unpin with 403). Assert status
        // only - the message may differ from pin's "You can't pin a room".
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
