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
/// The EntriesOrderService class provides functionality for managing and updating the order of files and folders.
/// It includes methods for reordering entries within a folder, setting specific orders for files and folders,
/// and processing batch updates for custom orders.
/// </summary>
[Scope]
public class EntriesOrderService(
    GlobalFolderHelper globalFolderHelper,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory,
    FilesMessageService filesMessageService,
    WebhookManager webhookManager)
{
    /// <summary>
    /// Reorders the content of a folder, such as files and subfolders, based on a specified order criteria.
    /// Optionally applies reordering recursively to subfolders.
    /// </summary>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <param name="folderId">The identifier of the folder to reorder.</param>
    /// <param name="subfolders">Indicates whether to apply reordering recursively to subfolders. Default is false.</param>
    /// <param name="init">Specifies whether to use the initial sorting order or a custom order. Default is false.</param>
    /// <returns>The folder object with its updated content order.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified folder is not found.</exception>
    /// <exception cref="ItemNotFoundException">Thrown if the folder belongs to a restricted template room.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have permission to edit the folder.</exception>
    public async Task<Folder<T>> ReOrderAsync<T>(T folderId, bool subfolders = false, bool init = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var room = await folderDao.GetFolderAsync(folderId);

        if (room == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var orderBy = init ? new OrderBy(SortedByType.DateAndTime, false) : new OrderBy(SortedByType.CustomOrder, true);
        var folders = folderDao.GetFoldersAsync(folderId, orderBy, FilterType.None, false, Guid.Empty, null);
        var files = fileDao.GetFilesAsync(folderId, orderBy, FilterType.None, false, Guid.Empty, null, null, false);

        var entries = await files.Concat(folders.Cast<FileEntry>())
            .OrderBy(r => r.Order)
            .ToListAsync();

        Dictionary<T, int> fileIds = new();
        Dictionary<T, int> folderIds = new();

        for (var i = 1; i <= entries.Count; i++)
        {
            var entry = entries[i - 1];
            if (entry.Order != i)
            {
                switch (entry)
                {
                    case File<T> file:
                        fileIds.Add(file.Id, i);
                        break;
                    case Folder<T> folder:
                        folderIds.Add(folder.Id, i);
                        break;
                }
            }
        }

        if (fileIds.Count != 0)
        {
            await fileDao.InitCustomOrder(fileIds, folderId);
        }

        if (folderIds.Count != 0)
        {
            await folderDao.InitCustomOrder(folderIds, folderId);
        }

        if (subfolders)
        {
            foreach (var t in folderIds)
            {
                await ReOrderAsync(t.Key, true, init);
            }
        }

        return room;
    }

    /// <summary>
    /// Updates the order of a specified file within its parent folder.
    /// Notifies connected services and triggers webhooks upon successful update.
    /// </summary>
    /// <typeparam name="T">The type of the file identifier.</typeparam>
    /// <param name="fileId">The identifier of the file to update.</param>
    /// <param name="order">The new order value to assign to the file.</param>
    /// <returns>The updated file object with the new order applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the user does not have permission to edit the file.</exception>
    /// <exception cref="ItemNotFoundException">Thrown if the specified file is not found.</exception>
    public async Task<File<T>> SetFileOrder<T>(T fileId, int order)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        file.NotFoundIfNull();
        if (!await fileSecurity.CanEditAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var newOrder = await fileDao.SetCustomOrder(fileId, file.ParentId, order);
        if (newOrder != 0 && newOrder != file.Order)
        {
            file.Order = order;
            await filesMessageService.SendAsync(MessageAction.FileIndexChanged, file, file.Title, file.Order.ToString(), order.ToString());
            await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
        }

        return file;
    }

    /// <summary>
    /// Sets the order of file entries, such as files and folders, based on the provided items.
    /// </summary>
    /// <typeparam name="T">The type of the file entry identifier.</typeparam>
    /// <param name="items">A list of order items specifying the entries and their new order positions.</param>
    /// <returns>An asynchronous enumerable of file entries updated with their new order.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided items list is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if any entry is not found or invalid for reordering.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have sufficient permissions to modify the entries.</exception>
    public async IAsyncEnumerable<FileEntry<T>> SetOrderAsync<T>(List<OrdersItemRequestDto<T>> items)
    {
        var contextId = Guid.NewGuid().ToString();

        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var folders = await folderDao.GetFoldersAsync(items.Where(x => x.EntryType == FileEntryType.Folder).Select(x => x.EntryId))
            .ToDictionaryAsync(x => x.Id);

        var files = await fileDao.GetFilesAsync(items.Where(x => x.EntryType == FileEntryType.File).Select(x => x.EntryId))
            .ToDictionaryAsync(x => x.Id);

        foreach (var item in items)
        {
            FileEntry<T> entry = item.EntryType == FileEntryType.File ? files.Get(item.EntryId) : folders.Get(item.EntryId);
            entry.NotFoundIfNull();

            var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(entry);
            var room = await daoFactory.GetCacheFolderDao<T>().GetFolderAsync(roomId);

            if (!await fileSecurity.CanEditRoomAsync(room))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (!await fileSecurity.CanEditAsync(entry))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            switch (entry)
            {
                case File<T> file:
                    {
                        var newOrder = await fileDao.SetCustomOrder(file.Id, file.ParentId, item.Order);
                        if (newOrder != 0)
                        {
                            entry.Order = item.Order;
                            await filesMessageService.SendAsync(MessageAction.FileIndexChanged, file, file.Title, file.Order.ToString(), item.Order.ToString(), contextId);
                            await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
                        }

                        break;
                    }
                case Folder<T> folder:
                    {
                        var newOrder = await folderDao.SetCustomOrder(folder.Id, folder.ParentId, item.Order);
                        if (newOrder != 0)
                        {
                            entry.Order = item.Order;
                            await filesMessageService.SendAsync(MessageAction.FolderIndexChanged, folder, folder.Title, folder.Order.ToString(), item.Order.ToString(), contextId);
                            await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, folder);
                        }

                        break;
                    }
            }

            yield return entry;
        }
    }

    /// <summary>
    /// Updates the order of a given folder based on the specified order value.
    /// Triggers folder update notifications if the order is successfully changed.
    /// </summary>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <param name="folderId">The identifier of the folder whose order is to be updated.</param>
    /// <param name="order">The new order value to assign to the folder.</param>
    /// <returns>The updated folder object with the modified order value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the user does not have permission to edit the folder.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the folder is not found.</exception>
    public async Task<Folder<T>> SetFolderOrder<T>(T folderId, int order)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);
        folder.NotFoundIfNull();
        if (!await fileSecurity.CanEditAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var newOrder = await folderDao.SetCustomOrder(folderId, folder.ParentId, order);

        if (newOrder != 0 && newOrder != folder.Order)
        {
            folder.Order = order;
            await filesMessageService.SendAsync(MessageAction.FolderIndexChanged, folder, folder.Title, folder.Order.ToString(), order.ToString());

            await webhookManager.PublishAsync(WebhookTrigger.FolderUpdated, folder);
        }

        return folder;
    }
}