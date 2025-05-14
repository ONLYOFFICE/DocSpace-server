// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Core.Core;

/// <summary>
/// Provides services related to managing favorite files and folders for the authenticated user.
/// </summary>
[Scope]
public class FavoritesService(
    AuthContext authContext,
    UserManager userManager,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory,
    FilesMessageService filesMessageService)
{
    /// <summary>
    /// Toggles the "favorite" status of a given file by adding or removing it from the user's favorites.
    /// </summary>
    /// <typeparam name="T">The type of the file ID.</typeparam>
    /// <param name="fileId">The unique identifier of the file to favorite or unfavorite.</param>
    /// <param name="favorite">A boolean indicating whether to add (true) or remove (false) the file as a favorite.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating the updated favorite status of the file.</returns>
    public async Task<bool> ToggleFileFavoriteAsync<T>(T fileId, bool favorite)
    {
        if (favorite)
        {
            await AddToFavoritesAsync(new List<T>(0), new List<T>(1) { fileId });
        }
        else
        {
            await DeleteFavoritesAsync(new List<T>(0), new List<T>(1) { fileId });
        }

        return favorite;
    }

    /// <summary>
    /// Adds the specified files and folders to the user's favorites, marking them as "favorite".
    /// </summary>
    /// <typeparam name="T">The type of the identifier for files and folders.</typeparam>
    /// <param name="foldersId">A collection of folder IDs to be marked as favorite.</param>
    /// <param name="filesId">A collection of file IDs to be marked as favorite.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of file entries that were successfully marked as favorite.</returns>
    public async ValueTask<List<FileEntry<T>>> AddToFavoritesAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId).Where(file => !file.Encrypted)).ToListAsync();
        var folders = fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)).ToListAsync();

        List<FileEntry<T>> entries = [];

        foreach (var items in await Task.WhenAll(files.AsTask(), folders.AsTask()))
        {
            entries.AddRange(items);
        }

        var tags = entries.Select(entry => Tag.Favorite(authContext.CurrentAccount.ID, entry));

        await tagDao.SaveTagsAsync(tags);

        foreach (var entry in entries)
        {
            await filesMessageService.SendAsync(MessageAction.FileMarkedAsFavorite, entry, entry.Title);
        }

        return entries;
    }

    /// <summary>
    /// Removes the specified files and folders from the user's favorites.
    /// </summary>
    /// <typeparam name="T">The type of the file and folder IDs.</typeparam>
    /// <param name="foldersId">A collection of folder identifiers to remove from favorites.</param>
    /// <param name="filesId">A collection of file identifiers to remove from favorites.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains no value.</returns>
    public async Task DeleteFavoritesAsync<T>(IEnumerable<T> foldersId, IEnumerable<T> filesId)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var files = fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)).ToListAsync();
        var folders = fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)).ToListAsync();

        List<FileEntry<T>> entries = [];

        foreach (var items in await Task.WhenAll(files.AsTask(), folders.AsTask()))
        {
            entries.AddRange(items);
        }

        var tags = entries.Select(entry => Tag.Favorite(authContext.CurrentAccount.ID, entry));

        await tagDao.RemoveTagsAsync(tags);

        foreach (var entry in entries)
        {
            await filesMessageService.SendAsync(MessageAction.FileRemovedFromFavorite, entry, entry.Title);
        }
    }
}