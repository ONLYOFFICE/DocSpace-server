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

namespace ASC.Files.Core.Services.WCFService.FileOperations;

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class DownloadPermissionsCheck<T>(FileSecurity security, IFileDao<T> fileDao, IFolderDao<T> folderDao)    
    : IPermissionsChecker<FileDownloadOperationData<T>, T>
{
    public async Task RunPermissionCheckAsync(FileDownloadOperationData<T> data)
    {
        var entriesPathId = new ItemNameValueCollection<T>();

        if (!data.Files.Any() && !data.Folders.Any())
        {
            return;
        }

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
