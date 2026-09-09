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
/// <c>PUT /files/rooms/{id}/pin</c> — concurrency safety and the 10-room pin limit. The rooms list
/// allows at most 10 pinned non-AI rooms; AI rooms have their own separate 10-room bucket and are
/// meant to be exempt from the regular limit (BUG 81852).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomPinLimitTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Fact]
    public async Task PinRoom_ConcurrentPinRequests_DoNotDuplicateTheRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Concurrent");

        // Act
        var results = await Task.WhenAll(
            _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken),
            _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        results.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        var occurrences = (await GetRoomRows()).Where(r => r.Id == room.Id).ToList();
        occurrences.Should().HaveCount(1);
        occurrences[0].Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinAndUnpinRoom_Concurrently_DoNotError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Race");

        // Act — neither request should crash the server; final state is whichever won.
        var pinTask = _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        var unpinTask = _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        await Task.WhenAll(pinTask, unpinTask);

        // Assert
        pinTask.Result.StatusCode.Should().Be(HttpStatusCode.OK);
        unpinTask.Result.StatusCode.Should().Be(HttpStatusCode.OK);

        // The race must not corrupt the room: it appears exactly once...
        var afterRace = await FindRoomRow(room.Id);
        afterRace.Count.Should().Be(1);

        // ...and the pin state stays deterministically settable afterwards.
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        await ExpectPinnedOnTop(room.Id);

        await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        var afterUnpin = await FindRoomRow(room.Id);
        afterUnpin.Row!.Value.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task PinRoom_ManyRoomsCanBePinnedSequentially()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var created = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            created.Add((await CreateCustomRoom($"Autotest Pin Many {i}")).Id);
        }

        // Act
        foreach (var id in created)
        {
            var response = await _roomsApi.PinRoomWithHttpInfoAsync(id, TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert — every room we pinned is actually pinned, and nothing else got pinned, and the
        // pinned rooms occupy the top contiguous block of the list.
        var rows = await GetRoomRows();
        var pinnedIds = rows.Where(r => r.Pinned).Select(r => r.Id).ToList();
        pinnedIds.Should().BeEquivalentTo(created);

        var firstUnpinned = rows.FindIndex(r => !r.Pinned);
        if (firstUnpinned != -1)
        {
            firstUnpinned.Should().Be(created.Count);
        }
    }

    [Fact]
    public async Task PinRoom_CannotPinMoreThanTenNonAiRooms()
    {
        // Arrange — pin 10 non-AI rooms, all allowed.
        await _filesClient.Authenticate(Owner);
        var pinned = new List<int>();
        for (var i = 0; i < 10; i++)
        {
            var room = await CreateCustomRoom($"Autotest Pin Cap {i}");
            var response = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            pinned.Add(room.Id);
        }

        var eleventh = await CreateCustomRoom("Autotest Pin Cap 11");

        // Act — the 11th non-AI room exceeds the limit and must be rejected.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(eleventh.Id, TestContext.Current.CancellationToken));

        // Assert — side-effect first: the 11th room is NOT pinned and exactly 10 stay pinned.
        var eleventhRow = await FindRoomRow(eleventh.Id);
        eleventhRow.Row!.Value.Pinned.Should().BeFalse();

        var rows = await GetRoomRows();
        rows.Where(r => r.Pinned).Select(r => r.Id).Should().BeEquivalentTo(pinned);

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You can't pin a room");
    }

    [Trait("Bug", "81852")]
    [Fact]
    public async Task PinRoom_AiRoom_IsExemptFromTheTenRoomPinLimit()
    {
        // Arrange — reach the limit: pin 10 non-AI rooms.
        await _filesClient.Authenticate(Owner);
        for (var i = 0; i < 10; i++)
        {
            var room = await CreateCustomRoom($"Autotest Pin Limit {i}");
            var response = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var aiRoom = await CreateAiRoom("Autotest Pin Limit AI");

        // Act — an AI room is not counted in the 10-room limit, so it should still pin.
        var response2 = await _roomsApi.PinRoomWithHttpInfoAsync(aiRoom.Id, TestContext.Current.CancellationToken);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.Data.Response.Pinned.Should().BeTrue();
        await ExpectPinnedOnTop(aiRoom.Id);
    }

    [Fact]
    public async Task PinRoom_AiRooms_HaveTheirOwnTenRoomPinLimit()
    {
        // Arrange — AI rooms are pinned in a bucket separate from regular rooms, but that bucket is
        // itself capped at 10 - the 11th AI room must be rejected.
        await _filesClient.Authenticate(Owner);
        for (var i = 0; i < 10; i++)
        {
            var room = await CreateAiRoom($"Autotest AI Pin Cap {i}");
            var response = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var eleventh = await CreateAiRoom("Autotest AI Pin Cap 11");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(eleventh.Id, TestContext.Current.CancellationToken));

        // Assert — side-effect first: the 11th AI room is NOT pinned.
        var row = await FindRoomRow(eleventh.Id);
        row.Row!.Value.Pinned.Should().BeFalse();

        exception.ErrorCode.Should().Be(403);
    }
}
