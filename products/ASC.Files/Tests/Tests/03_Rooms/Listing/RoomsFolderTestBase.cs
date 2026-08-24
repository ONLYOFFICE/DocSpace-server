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
    protected async Task<List<FolderDtoInteger>> CreateAllRoomTypesAsync()
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

    /// <summary>A room as it comes back from a raw GET /files/rooms read.</summary>
    protected sealed record RawRoomFolder(int Id, string Title, RoomType? RoomType);

    /// <summary>The parsed body of a raw GET /files/rooms read.</summary>
    protected sealed record RawRoomsFolderResult(List<RawRoomFolder> Folders, int Total, int StartIndex, int Count);

    /// <summary>
    /// Reads GET /files/rooms straight from JSON, with the same query parameters as
    /// <c>RoomsApi.GetRoomsFolderAsync</c>.
    ///
    /// <c>FolderContentDtoInteger.Folders</c> is typed <c>List&lt;FileEntryBaseDto&gt;</c>, which
    /// carries <c>Title</c> but neither <c>Id</c> nor <c>RoomType</c> — those two are only on the
    /// concrete room DTO the endpoint actually returns. That is an SDK/OpenAPI defect, not a
    /// preference, and this helper exists solely to work around it: every test that only needs
    /// title/total/count/startIndex should keep calling <c>_roomsApi.GetRoomsFolderAsync</c>
    /// directly instead.
    /// </summary>
    protected async Task<RawRoomsFolderResult> GetRoomsFolderRawAsync(
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
        var query = HttpUtility.ParseQueryString(string.Empty);

        if (type is not null)
        {
            foreach (var t in type)
            {
                query.Add("type", ((int)t).ToString());
            }
        }

        if (subjectId is not null)
        {
            query["subjectId"] = subjectId.Value.ToString();
        }

        if (subjectOwnerId is not null)
        {
            query["subjectOwnerId"] = subjectOwnerId.Value.ToString();
        }

        if (searchArea is not null)
        {
            query["searchArea"] = ((int)searchArea.Value).ToString();
        }

        if (withoutTags is not null)
        {
            query["withoutTags"] = withoutTags.Value.ToString();
        }

        if (tags is not null)
        {
            query["tags"] = tags;
        }

        if (excludeSubject is not null)
        {
            query["excludeSubject"] = excludeSubject.Value.ToString();
        }

        if (count is not null)
        {
            query["count"] = count.Value.ToString();
        }

        if (startIndex is not null)
        {
            query["startIndex"] = startIndex.Value.ToString();
        }

        if (sortBy is not null)
        {
            query["sortBy"] = sortBy;
        }

        if (sortOrder is not null)
        {
            query["sortOrder"] = ((int)sortOrder.Value).ToString();
        }

        if (filterValue is not null)
        {
            query["filterValue"] = filterValue;
        }

        var queryString = query.ToString();
        var path = string.IsNullOrEmpty(queryString) ? "api/2.0/files/rooms" : $"api/2.0/files/rooms?{queryString}";

        using var response = await _filesClient.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to read {path} ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var responseElement = json.RootElement.GetProperty("response");

        var folders = responseElement.GetProperty("folders").EnumerateArray()
            .Select(f => new RawRoomFolder(
                f.GetProperty("id").GetInt32(),
                f.GetProperty("title").GetString() ?? string.Empty,
                f.TryGetProperty("roomType", out var rt) && rt.ValueKind == JsonValueKind.Number
                    ? (RoomType)rt.GetInt32()
                    : null))
            .ToList();

        return new RawRoomsFolderResult(
            folders,
            responseElement.GetProperty("total").GetInt32(),
            responseElement.GetProperty("startIndex").GetInt32(),
            responseElement.GetProperty("count").GetInt32());
    }
}
