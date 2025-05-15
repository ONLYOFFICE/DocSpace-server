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

[Scope]
public class RecentService(
    AuthContext authContext,
    SocketManager socketManager,
    IDaoFactory daoFactory)
{
    public async Task DeleteFromRecentAsync<T>(List<T> foldersIds, List<T> filesIds, bool recentByLinks)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var tagDao = daoFactory.GetTagDao<T>();

        var entries = new List<FileEntry<T>>(foldersIds.Count + filesIds.Count);

        var folders = folderDao.GetFoldersAsync(foldersIds).Cast<FileEntry<T>>().ToListAsync().AsTask();
        var files = fileDao.GetFilesAsync(filesIds).Cast<FileEntry<T>>().ToListAsync().AsTask();

        foreach (var items in await Task.WhenAll(folders, files))
        {
            entries.AddRange(items);
        }

        var tags = recentByLinks
            ? await tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.RecentByLink, entries).ToListAsync()
            : entries.Select(f => Tag.Recent(authContext.CurrentAccount.ID, f));

        await tagDao.RemoveTagsAsync(tags);

        var users = new[] { authContext.CurrentAccount.ID };

        var tasks = new List<Task>(entries.Count);

        foreach (var e in entries)
        {
            switch (e)
            {
                case File<T> file:
                    tasks.Add(socketManager.DeleteFileAsync(file, users: users));
                    break;
                case Folder<T> folder:
                    tasks.Add(socketManager.DeleteFolder(folder, users: users));
                    break;
            }
        }

        await Task.WhenAll(tasks);
    }
}