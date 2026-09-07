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
/// <c>PUT /files/rooms/{id}/unpin</c> — how unpinning interacts with the 10-room pin limit tested
/// in <see cref="RoomPinLimitTests"/>: it frees a slot, and doing so must not disturb the rooms
/// that remain pinned.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUnpinLimitTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Fact]
    public async Task UnpinRoom_FreesASlotInTheTenRoomPinLimit()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var pinned = new List<int>();
        for (var i = 0; i < 10; i++)
        {
            var room = await CreateCustomRoom($"Autotest Unpin Slot {i}");
            var response = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            pinned.Add(room.Id);
        }

        // An 11th room cannot be pinned while the limit is full.
        var extra = await CreateCustomRoom("Autotest Unpin Slot Extra");
        var blocked = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(extra.Id, TestContext.Current.CancellationToken));
        blocked.ErrorCode.Should().Be(403);

        // Act — unpin one -> a slot frees up -> the extra room now pins.
        await _roomsApi.UnpinRoomAsync(pinned[0], TestContext.Current.CancellationToken);
        var freed = await _roomsApi.PinRoomWithHttpInfoAsync(extra.Id, TestContext.Current.CancellationToken);

        // Assert
        freed.StatusCode.Should().Be(HttpStatusCode.OK);
        freed.Data.Response.Pinned.Should().BeTrue();

        // Exactly 10 remain pinned: the original set minus pinned[0], plus extra.
        var pinnedNow = (await GetRoomRows()).Where(r => r.Pinned).Select(r => r.Id).ToList();
        pinnedNow.Should().HaveCount(10);
        pinnedNow.Should().Contain(extra.Id);
        pinnedNow.Should().NotContain(pinned[0]);
    }

    [Trait("Bug", "80757")]
    [Fact]
    public async Task UnpinRoom_SwappingAPinnedRoomAtTheLimit_MustNotResetAllPins()
    {
        // Regression for BUG 80757 (fixed): after reaching the 10-room limit, unpinning one and
        // pinning a fresh room (back to 10) used to silently reset the whole pinned set. All 10
        // must survive the swap.
        await _filesClient.Authenticate(Owner);
        var pinned = new List<int>();
        for (var i = 0; i < 10; i++)
        {
            var room = await CreateCustomRoom($"Autotest Unpin Reset {i}");
            await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
            pinned.Add(room.Id);
        }

        // Act
        await _roomsApi.UnpinRoomAsync(pinned[0], TestContext.Current.CancellationToken);
        var fresh = await CreateCustomRoom("Autotest Unpin Reset Fresh");
        await _roomsApi.PinRoomAsync(fresh.Id, TestContext.Current.CancellationToken);

        // Assert
        var pinnedNow = (await GetRoomRows()).Where(r => r.Pinned).Select(r => r.Id).ToList();
        pinnedNow.Should().HaveCount(10);
        pinnedNow.Should().Contain(fresh.Id);
    }
}
