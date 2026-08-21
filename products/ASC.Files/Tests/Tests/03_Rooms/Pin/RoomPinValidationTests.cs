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
/// <c>PUT /files/rooms/{id}/pin</c> — invalid ids and rooms that are no longer in a pinnable
/// state (deleted or archived).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomPinValidationTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Trait("Bug", "81850")]
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999999999)]
    public async Task PinRoom_NonExistentId_ShouldReturnBadRequestValidationError(int id)
    {
        // A non-existent/invalid numeric id should be a validation error (400), but the API
        // currently returns 403 "The required folder was not found".
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task PinRoom_NonNumericId_ReturnsNotFound()
    {
        // The typed SDK signature takes an int id, so a non-numeric path segment can only be sent
        // over raw HTTP.
        await _filesClient.Authenticate(Owner);

        using var response = await _filesClient.PutAsync("api/2.0/files/rooms/abc/pin", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PinRoom_DeletedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Deleted");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("The required folder was not found");
    }

    [Fact]
    public async Task PinRoom_ArchivedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Archived");

        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You can't pin a room");
    }

    [Fact]
    public async Task PinRoom_DoesNotSurviveArchiveThenUnarchive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin ArchiveCycle");

        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        await ArchiveRoom(room.Id);
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert — archiving resets the pin state.
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task PinRoom_PinnedRoomDisappearsFromListAfterDeletion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin DeleteGone");

        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var rows = await GetRoomRows();
        rows.Select(r => r.Id).Should().NotContain(room.Id);
    }
}
