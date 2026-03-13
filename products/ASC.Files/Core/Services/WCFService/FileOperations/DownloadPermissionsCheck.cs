// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Core.Services.WCFService.FileOperations;

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class DownloadPermissionsCheck<T>(FileSecurity security, IFileDao<T> fileDao, IFolderDao<T> folderDao)
{
    internal async Task CheckPermissionsAsync(ItemNameValueCollection<T> entriesPathId, IEnumerable<T> files)
    {
        if (entriesPathId == null || entriesPathId.Count == 0)
        {
            if (files.Any())
            {
                throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
            }

            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }
    }

    public async Task CheckEntriesPermissionsAsync(FileDownloadOperationData<T> data)
    {
        var entriesPathId = new ItemNameValueCollection<T>();

        if (data.Files.Any())
        {
            var filesForSend = await security.FilterDownloadAsync(fileDao.GetFilesAsync(data.Files)).ToListAsync();
            foreach (var file in filesForSend)
            {
                entriesPathId.Add("", file.Id);
            }
        }

        if (data.Folders.Any())
        {
            var folderForSend = await security.FilterDownloadAsync(folderDao.GetFoldersAsync(data.Folders)).ToListAsync();

            var filesInFolder = await GetFilesInFoldersAsync(folderForSend.Select(x => x.Id), string.Empty);
            entriesPathId.Add(filesInFolder);
        }

        await CheckPermissionsAsync(entriesPathId, data.Files);
    }

    private async Task<ItemNameValueCollection<T>> GetFilesInFoldersAsync(IEnumerable<T> folderIds, string path)
    {
        var entriesPathId = new ItemNameValueCollection<T>();

        foreach (var folderId in folderIds)
        {
            var folder = await folderDao.GetFolderAsync(folderId);
            if (folder == null || !await security.CanDownloadAsync(folder))
            {
                continue;
            }

            var folderPath = path + folder.Title + "/";
            entriesPathId.Add(folderPath, default(T));

            var files = security.FilterDownloadAsync(fileDao.GetFilesAsync(folder.Id, null, FilterType.None, false, Guid.Empty, string.Empty, null, true));

            await foreach (var file in files)
            {
                entriesPathId.Add("", file.Id);
            }

            var nestedFolders = await security.FilterDownloadAsync(folderDao.GetFoldersAsync(folder.Id)).ToListAsync();

            var filesInFolder = await GetFilesInFoldersAsync(nestedFolders.Select(f => f.Id), folderPath);
            entriesPathId.Add(filesInFolder);
        }

        return entriesPathId;
    }
}
