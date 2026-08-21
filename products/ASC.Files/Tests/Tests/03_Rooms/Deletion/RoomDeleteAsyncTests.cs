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
/// <c>DELETE /files/rooms/{id}</c> — the asynchronous operation's own contract (progress reaching
/// 100, repeated polling being stable, running two deletes in parallel or in sequence) and the
/// edge cases that follow from a room already being gone (double delete, ops against a room whose
/// delete already completed). Positive functional coverage lives in <see cref="RoomDeleteTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomDeleteAsyncTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "81698")]
    public async Task DeleteRoom_DeleteAfterTrue_ProducesTrackableOperation()
    {
        // HTTP returns 200, but the delete is not pushed to fileops, so waitLongOperation cannot
        // find a matching record and the last poll returns an empty list.
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete deleteAfter true");

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(true), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().NotBeNullOrEmpty("deleteAfter:true must still produce a trackable operation");
        operations.Should().OnlyContain(o => o.Finished);
    }

    [Fact]
    public async Task DeleteRoom_Operation_TransitionsToFinishedWithProgress100()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Async");

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().OnlyContain(o => o.Finished && o.Progress == 100 && o.Error == "");
    }

    [Fact]
    public async Task DeleteRoom_RepeatedPollingOfFinishedOperation_IsStable()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Polling");

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var first = await WaitLongOperation();
        first.Should().OnlyContain(o => o.Finished);

        var again = (await _filesOperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        if (again.Count > 0)
        {
            again[^1].Finished.Should().BeTrue();
            again[^1].Error.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task DeleteRoom_TwoRoomsSequentially_BothVanish()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Seq Delete A");
        var roomB = await CreateCustomRoom("Autotest Seq Delete B");

        // Act
        await _roomsApi.DeleteRoomAsync(roomA.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var opA = await WaitLongOperation();
        opA.Should().OnlyContain(o => o.Finished);

        await _roomsApi.DeleteRoomAsync(roomB.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var opB = await WaitLongOperation();
        opB.Should().OnlyContain(o => o.Finished);

        // Assert
        var exceptionA = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(roomA.Id, TestContext.Current.CancellationToken));
        exceptionA.ErrorCode.Should().Be(404);

        var exceptionB = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(roomB.Id, TestContext.Current.CancellationToken));
        exceptionB.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_TwoConcurrentDeletes_BothSucceed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Concurrent Delete A");
        var roomB = await CreateCustomRoom("Autotest Concurrent Delete B");

        // Act
        var responseA = _roomsApi.DeleteRoomWithHttpInfoAsync(roomA.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var responseB = _roomsApi.DeleteRoomWithHttpInfoAsync(roomB.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await Task.WhenAll(responseA, responseB);

        // Assert
        responseA.Result.StatusCode.Should().Be(HttpStatusCode.OK);
        responseB.Result.StatusCode.Should().Be(HttpStatusCode.OK);

        await WaitLongOperation();

        var exceptionA = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(roomA.Id, TestContext.Current.CancellationToken));
        exceptionA.ErrorCode.Should().Be(404);

        var exceptionB = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(roomB.Id, TestContext.Current.CancellationToken));
        exceptionB.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_SecondDeleteOfAlreadyDeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Double Delete");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var op = await WaitLongOperation();
        op.Should().OnlyContain(o => o.Finished);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_ChangingCoverOnDeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover After Delete");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(room.Id, new CoverRequestDto("FF5733", coverId), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_SettingLogoOnDeletedRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Logo After Delete");

        const string base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";
        await using var stream = new MemoryStream(Convert.FromBase64String(base64Png));
        var uploaded = (await _roomsApi.UploadRoomLogoAsync(new FileParameter("logo.png", "image/png", stream), TestContext.Current.CancellationToken)).Response;
        var tmpFile = uploaded.Data?.ToString() ?? string.Empty;

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomLogoAsync(room.Id, new LogoRequest(tmpFile, 0, 0, 1, 1), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
