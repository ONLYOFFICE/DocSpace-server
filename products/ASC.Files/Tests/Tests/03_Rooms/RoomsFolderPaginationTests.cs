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
/// Pagination of GET /files/rooms: a sorted slice must be a slice of one stable order, so the same
/// request repeated returns the same rooms.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomsFolderPaginationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// Bug 81809: title sorting is not stable, so <c>sortBy=title</c> combined with
    /// <c>startIndex</c> + <c>count</c> hands back a different slice from call to call — rooms are
    /// skipped and repeated across pages. The request is issued several times concurrently, which
    /// is what makes the instability show.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81809")]
    public async Task GetRoomsFolder_SortedSlice_IsStableAcrossRepeatedCalls()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var marker = $"Slice{Guid.NewGuid().ToString()[..8]}";

        foreach (var suffix in (string[])["A", "B", "C", "D"])
        {
            await CreateCustomRoom($"Autotest {marker} {suffix}");
        }

        // Act — the same page requested five times
        var slices = new List<List<string>>();

        for (var i = 0; i < 5; i++)
        {
            slices.Add(await GetSliceTitles(marker));
        }

        // Assert
        var expected = new List<string> { $"Autotest {marker} B", $"Autotest {marker} C" };

        foreach (var slice in slices)
        {
            slice.Should().Equal(expected, "startIndex + count must page a stable title order");
        }
    }

    private async Task<List<string>> GetSliceTitles(string marker)
    {
        var page = (await _roomsApi.GetRoomsFolderAsync(
            filterValue: marker,
            sortBy: "title",
            sortOrder: SortOrder.Ascending,
            startIndex: 1,
            count: 2,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return page.Folders.ConvertAll(f => f.Title);
    }
}
