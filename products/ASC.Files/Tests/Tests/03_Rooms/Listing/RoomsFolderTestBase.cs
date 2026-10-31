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

namespace ASC.Files.Tests.Tests._03_Rooms.Listing;

/// <summary>
/// Shared setup for the GET /files/rooms (getRoomsFolder) suites.
/// </summary>
public abstract class RoomsFolderTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// How many of the rooms <see cref="CreateAllRoomTypesAsync"/> creates the default
    /// (searchArea=Active) view lists. Form filling rooms live under their own root
    /// (FolderType.Forms) and only show up for searchArea=Forms; searchArea=Any lists all five.
    /// </summary>
    protected const int ActiveAreaRoomCount = 4;

    /// <summary>One room of each type, mirroring the TS <c>createAllRoomTypes</c> helper.</summary>
    protected async Task<List<FolderDto>> CreateAllRoomTypesAsync()
    {
        return
        [
            await CreateCustomRoom("Autotest Custom"),
            await CreateCollaborationRoom("Autotest Collaboration"),
            await CreateFillingFormsRoom("Autotest FormFilling"),
            await CreatePublicRoom("Autotest Public"),
            await CreateVDRRoom("Autotest VDR")
        ];
    }

    /// <summary>
    /// GET /files/rooms' <c>filterValue</c> (and the other query filters) is served from the
    /// search index, which is written asynchronously after the room create/rename/tag change that
    /// triggered it returns. A bare read right after such a write races the index update, so every
    /// test that reads the list immediately after changing a room should poll through this helper
    /// instead of reading once — on a deadline, returning the last observed state so a failing
    /// assertion still shows what was actually there.
    /// </summary>
    /// <remarks>
    /// The default deadline is generous on purpose: in a full-suite run (3000+ tests in parallel)
    /// the OpenSearch indexer lags well past the ~10 seconds that suffice for a single-class run,
    /// and these were the only tests that flaked under that load.
    /// </remarks>
    protected async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> until, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var result = await read();

            if (until(result) || DateTime.UtcNow >= deadline)
            {
                return result;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>A room of a GET /files/rooms listing.</summary>
    protected sealed record ListedRoom(int Id, string Title, RoomType? RoomType);

    /// <summary>A GET /files/rooms listing, with the rooms as their concrete <c>FolderDto</c>.</summary>
    protected sealed record ListedRooms(List<ListedRoom> Folders, int Total, int StartIndex, int Count);

    /// <summary>
    /// GET /files/rooms through <c>RoomsApi.GetRoomsFolderAsync</c>, with the rooms read as the <c>FolderDto</c>
    /// the <c>entryKind</c> discriminator makes of them, so the <c>Id</c> and <c>RoomType</c> are at hand.
    /// </summary>
    protected async Task<ListedRooms> ListRoomsAsync(
        List<RoomType>? type = null,
        Guid? subjectId = null,
        Guid? subjectOwnerId = null,
        SearchArea? searchArea = null,
        bool? withoutTags = null,
        string? tags = null,
        bool? excludeSubject = null,
        int? count = null,
        int? startIndex = null,
        string? sortBy = null,
        SortOrder? sortOrder = null,
        string? filterValue = null)
    {
        var content = (await _roomsApi.GetRoomsFolderAsync(
            type: type,
            subjectId: subjectId,
            subjectOwnerId: subjectOwnerId,
            searchArea: searchArea,
            withoutTags: withoutTags,
            tags: tags,
            excludeSubject: excludeSubject,
            count: count,
            startIndex: startIndex,
            sortBy: sortBy,
            sortOrder: sortOrder,
            filterValue: filterValue,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var folders = (content.Folders ?? []).OfType<FolderDto>()
            .Select(f => new ListedRoom(f.Id, f.Title ?? string.Empty, f.RoomType))
            .ToList();

        return new ListedRooms(folders, content.Total, content.StartIndex, content.Count);
    }
}
