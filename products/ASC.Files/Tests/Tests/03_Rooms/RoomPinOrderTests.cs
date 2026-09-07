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

namespace ASC.Files.Tests.Tests._03_Rooms;

/// <summary>
/// What pinning does to the order of GET /files/rooms: a pinned room floats to the top of the
/// listing and drops back to where it was when it is unpinned. Who may pin is covered in
/// <c>Permissions/RoomPinPermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomPinOrderTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UnpinRoom_ReturnsRoomToItsNaturalSortPosition()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = $"Unpin{Guid.NewGuid().ToString()[..8]}";

        await CreateCustomRoom($"{marker} AAA");
        await CreateCustomRoom($"{marker} MMM");
        var z = await CreateCustomRoom($"{marker} ZZZ");

        // The natural position is whatever the order is before pinning: asserting a specific
        // (alphabetical) place would only re-test the unstable title sort of bug 81809 instead of
        // the pin round-trip itself.
        // filterValue is served from the search index, which is written asynchronously — poll until
        // all three rooms are indexed rather than racing that write with a bare read.
        var before = await GetOrderWhenIndexed(marker, 3);
        before.Should().HaveCount(3);

        // Act & Assert — pinning floats the room to the top...
        await _roomsApi.PinRoomAsync(z.Id, TestContext.Current.CancellationToken);

        var pinned = await GetOrder(marker);
        pinned[0].Should().Be(z.Title);

        // ...and unpinning drops it back exactly where it was
        await _roomsApi.UnpinRoomAsync(z.Id, TestContext.Current.CancellationToken);

        var after = await GetOrder(marker);
        after.Should().Equal(before);
    }

    private async Task<List<string>> GetOrderWhenIndexed(string marker, int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var titles = await GetOrder(marker);

            if (titles.Count == expectedCount || DateTime.UtcNow >= deadline)
            {
                return titles;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private async Task<List<string>> GetOrder(string marker)
    {
        var rooms = (await _roomsApi.GetRoomsFolderAsync(
            filterValue: marker,
            sortBy: "AZ",
            sortOrder: SortOrder.Ascending,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return rooms.Folders.ConvertAll(f => f.Title);
    }
}
