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
/// Shared setup for the pin/unpin suites (<c>PUT /files/rooms/{id}/pin</c> and
/// <c>PUT /files/rooms/{id}/unpin</c>). Who may pin is covered separately in
/// <c>Permissions/RoomPinPermissionsTests</c> and <c>Permissions/RoomUnpinPermissionsTests</c>; this
/// folder covers everything else — contract, ordering, room types, limits, concurrency and
/// validation.
/// </summary>
public abstract class RoomPinTestsBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// A row of the rooms listing carrying the two fields <see cref="DocSpace.API.SDK.Model.FileEntryBaseDto"/>
    /// drops: <c>Id</c> and <c>Pinned</c>. <see cref="GetRoomRows"/> reads them from the raw response
    /// body instead — see the remarks there.
    /// </summary>
    protected readonly record struct RoomRow(int Id, string Title, bool Pinned);

    /// <summary>
    /// Lists rooms exactly like <see cref="DocSpace.API.SDK.Api.Rooms.RoomsApi.GetRoomsFolderAsync"/>,
    /// but returns each room's <c>id</c> and <c>pinned</c> flag alongside its title.
    /// <c>FolderContentDtoInteger.Folders</c> is typed <c>List&lt;FileEntryBaseDto&gt;</c>, which only
    /// carries the fields common to every entry type — neither <c>Id</c> nor <c>Pinned</c> is one of
    /// them. That is an SDK defect worth reporting, not a preference: the request itself still goes
    /// through the typed SDK call (so sorting/filtering/paging are exercised as intended), only the
    /// response is re-read from <see cref="ApiResponse{T}.RawContent"/> to recover the missing fields.
    /// </summary>
    protected async Task<List<RoomRow>> GetRoomRows(
        SearchArea? searchArea = null,
        string? sortBy = null,
        SortOrder? sortOrder = null,
        string? filterValue = null,
        int? count = null,
        int? startIndex = null)
    {
        var response = await _roomsApi.GetRoomsFolderWithHttpInfoAsync(
            searchArea: searchArea,
            sortBy: sortBy,
            sortOrder: sortOrder,
            filterValue: filterValue,
            count: count,
            startIndex: startIndex,
            cancellationToken: TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(response.RawContent);

        return [.. json.RootElement.GetProperty("response").GetProperty("folders")
            .EnumerateArray()
            .Select(f => new RoomRow(
                f.GetProperty("id").GetInt32(),
                f.GetProperty("title").GetString()!,
                f.TryGetProperty("pinned", out var pinned) && pinned.GetBoolean()))];
    }

    /// <summary>
    /// Polls <see cref="GetRoomRows"/> on a deadline until <paramref name="until"/> is satisfied. A
    /// filtered listing is served from the search index, so a room created moments ago may not be
    /// in it yet — see the comment on <c>RoomLogoDeleteTests.DeleteLogo_RoomsListReflectsResetLogo</c>
    /// for the same caveat. Returns the last observed rows either way, so the caller's own assertion
    /// shows what was actually there instead of the loop dying on a timeout.
    /// </summary>
    protected async Task<List<RoomRow>> GetRoomRowsUntil(
        Func<List<RoomRow>, bool> until,
        string? filterValue = null,
        SearchArea? searchArea = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var rows = await GetRoomRows(searchArea: searchArea, filterValue: filterValue);

            if (until(rows) || DateTime.UtcNow >= deadline)
            {
                return rows;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Locates a room in the caller's listing, mirroring the TS suite's <c>findRoomRow</c> helper.
    /// Form filling rooms are not in the default <see cref="SearchArea.Active"/> view — they list
    /// under <see cref="SearchArea.Forms"/> — so the area has to be passed explicitly for them.
    /// </summary>
    protected async Task<(List<RoomRow> Rows, RoomRow? Row, int Count, int Index)> FindRoomRow(
        int roomId, SearchArea searchArea = SearchArea.Active)
    {
        var rows = await GetRoomRows(searchArea);
        var matches = rows.Where(r => r.Id == roomId).ToList();
        var index = matches.Count > 0 ? rows.IndexOf(matches[0]) : -1;

        return (rows, matches.Count > 0 ? matches[0] : null, matches.Count, index);
    }

    /// <summary>
    /// Asserts the real effect of pinning: the room is present, flagged pinned, appears exactly
    /// once, and sits above the first unpinned room (if any).
    /// </summary>
    protected async Task ExpectPinnedOnTop(int roomId, SearchArea searchArea = SearchArea.Active)
    {
        var (rows, row, count, index) = await FindRoomRow(roomId, searchArea);

        count.Should().Be(1, $"room {roomId} should appear exactly once");
        row!.Value.Pinned.Should().BeTrue();

        var firstUnpinned = rows.FindIndex(r => !r.Pinned);
        if (firstUnpinned != -1)
        {
            index.Should().BeLessThan(firstUnpinned);
        }
    }

    /// <summary>Creates a room of the given type through the room-specific helpers already on <see cref="BaseTest"/>.</summary>
    protected Task<FolderDtoInteger> CreateRoomOfType(RoomType roomType, string title) => roomType switch
    {
        RoomType.CustomRoom => CreateCustomRoom(title),
        RoomType.PublicRoom => CreatePublicRoom(title),
        RoomType.FillingFormsRoom => CreateFillingFormsRoom(title),
        RoomType.EditingRoom => CreateCollaborationRoom(title),
        RoomType.VirtualDataRoom => CreateVDRRoom(title),
        RoomType.AiRoom => CreateAiRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Room type not supported by this helper.")
    };

    /// <summary>The search area a room of the given type is listed under by default.</summary>
    protected static SearchArea SearchAreaFor(RoomType roomType) =>
        roomType == RoomType.FillingFormsRoom ? SearchArea.Forms : SearchArea.Active;
}
