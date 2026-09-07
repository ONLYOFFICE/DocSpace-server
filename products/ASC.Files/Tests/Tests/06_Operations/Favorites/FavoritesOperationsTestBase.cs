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

namespace ASC.Files.Tests.Tests._06_Operations.Favorites;

/// <summary>
/// Shared setup for the POST/DELETE /api/2.0/files/favorites and GET /files/favorites/{fileId}
/// suites. Derives from <see cref="RoomsPermissionsTestBase"/> to reuse <c>InviteMember</c>,
/// <c>InviteToRoom</c> and <c>ArchiveRoom</c>, which the access-control suites need.
/// </summary>
public abstract class FavoritesOperationsTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    protected async Task<bool> AddFavorites(BaseBatchRequestDto? request)
    {
        var result = await _filesOperationsApi.AddFavoritesAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        return result.Response;
    }

    protected async Task<bool> AddFilesToFavorites(params int[] fileIds)
    {
        return await AddFavorites(new BaseBatchRequestDto { FileIds = fileIds.Select(id => new BaseBatchRequestDtoAllOfFileIds(id)).ToList() });
    }

    protected async Task<bool> AddFoldersToFavorites(params int[] folderIds)
    {
        return await AddFavorites(new BaseBatchRequestDto { FolderIds = folderIds.Select(id => new BaseBatchRequestDtoAllOfFolderIds(id)).ToList() });
    }

    protected async Task<bool> RemoveFavorites(BaseBatchRequestDto? request)
    {
        var result = await _filesOperationsApi.DeleteFavoritesFromBodyAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        return result.Response;
    }

    protected async Task<bool> RemoveFilesFromFavorites(params int[] fileIds)
    {
        return await RemoveFavorites(new BaseBatchRequestDto { FileIds = fileIds.Select(id => new BaseBatchRequestDtoAllOfFileIds(id)).ToList() });
    }

    protected async Task<bool> RemoveFoldersFromFavorites(params int[] folderIds)
    {
        return await RemoveFavorites(new BaseBatchRequestDto { FolderIds = folderIds.Select(id => new BaseBatchRequestDtoAllOfFolderIds(id)).ToList() });
    }

    protected async Task<bool> ToggleFavorite(int fileId, bool favorite = true)
    {
        var result = await _filesApi.ToggleFileFavoriteAsync(fileId, favorite, TestContext.Current.CancellationToken);
        return result.Response;
    }

    protected async Task<FolderContentDtoInteger> GetFavorites()
    {
        var wrapper = await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken);
        return wrapper.Response;
    }

    /// <summary>
    /// The Favorites section is served from the search index, which is written asynchronously after
    /// the add/remove/toggle request returns. Polls on a deadline instead of reading once, so a slow
    /// index write does not turn into an intermittent failure, and returns the last observed state so
    /// a failing assertion still shows what was actually there.
    /// </summary>
    protected async Task<FolderContentDtoInteger> PollFavorites(Func<FolderContentDtoInteger, bool> until, int timeoutSeconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var favorites = await GetFavorites();

            if (until(favorites) || DateTime.UtcNow >= deadline)
            {
                return favorites;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Moves a file to trash and waits for the asynchronous delete operation to finish.</summary>
    protected async Task DeleteFileToTrash(int fileId)
    {
        await _filesApi.DeleteFileAsync(fileId, new Delete(false, false), false, TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    protected async Task<FolderDtoInteger> CreateRoom(RoomType roomType, string title) => roomType switch
    {
        RoomType.CustomRoom => await CreateCustomRoom(title),
        RoomType.PublicRoom => await CreatePublicRoom(title),
        RoomType.EditingRoom => await CreateCollaborationRoom(title),
        RoomType.VirtualDataRoom => await CreateVDRRoom(title),
        RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type")
    };

    public static TheoryData<RoomType> RoomTypesForFavorites =>
    [
        RoomType.CustomRoom,
        RoomType.PublicRoom,
        RoomType.EditingRoom,
        RoomType.VirtualDataRoom,
        RoomType.FillingFormsRoom
    ];
}
