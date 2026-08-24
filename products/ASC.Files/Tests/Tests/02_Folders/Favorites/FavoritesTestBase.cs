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

namespace ASC.Files.Tests.Tests._02_Folders.Favorites;

/// <summary>
/// Shared setup for the GET /files/@favorites suites. Derives from
/// <see cref="RoomsPermissionsTestBase"/> to reuse <c>InviteMember</c>, <c>InviteToRoom</c> and
/// <c>ArchiveRoom</c>, which the access-control suite needs.
/// </summary>
public abstract class FavoritesTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    protected async Task<FileDtoInteger> CreateTextFile(string title, int folderId, string content = "hello")
    {
        var wrapper = await _filesApi.CreateTextFileAsync(folderId, new CreateTextOrHtmlFile(title, content, true), TestContext.Current.CancellationToken);
        return wrapper.Response;
    }

    protected async Task<FileDtoInteger> CreateHtmlFile(string title, int folderId, string content = "<p>test</p>")
    {
        var wrapper = await _filesApi.CreateHtmlFileAsync(folderId, new CreateTextOrHtmlFile(title, content, true), TestContext.Current.CancellationToken);
        return wrapper.Response;
    }

    protected async Task ToggleFavorite(int fileId, bool favorite = true)
    {
        await _filesApi.ToggleFileFavoriteAsync(fileId, favorite, TestContext.Current.CancellationToken);
    }

    protected async Task AddFoldersToFavorites(params int[] folderIds)
    {
        var request = new BaseBatchRequestDto { FolderIds = folderIds.Select(id => new BaseBatchRequestDtoAllOfFolderIds(id)).ToList() };
        await _filesOperationsApi.AddFavoritesAsync(request, TestContext.Current.CancellationToken);
    }

    protected async Task RemoveFoldersFromFavorites(params int[] folderIds)
    {
        var request = new BaseBatchRequestDto { FolderIds = folderIds.Select(id => new BaseBatchRequestDtoAllOfFolderIds(id)).ToList() };
        await _filesOperationsApi.DeleteFavoritesFromBodyAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Moves a file to trash and waits for the asynchronous delete operation to finish.</summary>
    protected async Task DeleteFileToTrash(int fileId)
    {
        await _filesApi.DeleteFileAsync(fileId, new Delete(false, false), false, TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    protected async Task<FolderContentDtoInteger> GetFavorites(
        FilterType? filterType = null,
        int? count = null,
        int? startIndex = null,
        string? sortBy = null,
        SortOrder? sortOrder = null,
        string? filterValue = null)
    {
        var wrapper = await _foldersApi.GetFavoritesFolderAsync(
            filterType: filterType,
            count: count,
            startIndex: startIndex,
            sortBy: sortBy,
            sortOrder: sortOrder,
            filterValue: filterValue,
            cancellationToken: TestContext.Current.CancellationToken);

        return wrapper.Response;
    }

    /// <summary>
    /// <c>@favorites</c> reads whatever the search index currently holds, and toggling a favorite is
    /// written to that index asynchronously. Polls the endpoint on a deadline instead of reading once,
    /// so a slow index write does not turn into an intermittent failure, and returns the last observed
    /// state so a failing assertion still shows what was actually there.
    /// </summary>
    protected async Task<FolderContentDtoInteger> PollFavorites(Func<FolderContentDtoInteger, bool> until, FilterType? filterType = null, int timeoutSeconds = 30, string? filterValue = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var favorites = await GetFavorites(filterType: filterType, filterValue: filterValue);

            if (until(favorites) || DateTime.UtcNow >= deadline)
            {
                return favorites;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>A file as it comes back from a raw GET /files/@favorites read.</summary>
    protected sealed record RawFavoriteFile(string Title, string? OriginRoomTitle, bool? IsFavorite);

    /// <summary>
    /// Reads GET /files/@favorites straight from JSON. <c>FolderContentDtoInteger.Files</c> is typed
    /// <c>List&lt;FileEntryBaseDto&gt;</c>, which carries <c>Title</c> and <c>IsFavorite</c> but not
    /// <c>OriginRoomTitle</c> - that field only exists on the concrete <c>FileEntryDtoInteger</c> the
    /// endpoint actually returns. That is an SDK/OpenAPI defect, not a preference; every other test
    /// that does not need <c>originRoomTitle</c> should keep calling <see cref="GetFavorites"/>.
    /// </summary>
    protected async Task<List<RawFavoriteFile>> GetFavoritesFilesRawAsync()
    {
        using var response = await _filesClient.GetAsync("api/2.0/files/@favorites", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to read api/2.0/files/@favorites ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var filesElement = json.RootElement.GetProperty("response").GetProperty("files");

        return filesElement.EnumerateArray()
            .Select(f => new RawFavoriteFile(
                f.GetProperty("title").GetString() ?? string.Empty,
                f.TryGetProperty("originRoomTitle", out var originRoomTitle) ? originRoomTitle.GetString() : null,
                f.TryGetProperty("isFavorite", out var isFavorite) && isFavorite.ValueKind is JsonValueKind.True or JsonValueKind.False ? isFavorite.GetBoolean() : null))
            .ToList();
    }
}
