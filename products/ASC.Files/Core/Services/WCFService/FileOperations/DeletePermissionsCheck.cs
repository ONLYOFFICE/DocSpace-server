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
public class DeletePermissionsCheck<T>(
    IFileDao<T> fileDao,
    IFolderDao<T> folderDao,
    LockerManager lockerManager,
    FileTrackerHelper fileTracker,
    FileSecurity security,
    IDaoFactory daoFactory,
    AuthContext authContext)
    : IPermissionsChecker<FileDeleteOperationData<T>, T>
{
    public async Task RunPermissionCheckAsync(FileDeleteOperationData<T> data)
    {
        if (data.FilesVersions != null && data.FilesVersions.Any() && data.Files.Any())
        {
            await CheckVersionsAsync(data.Files.ToList(), data.FilesVersions.ToList());
        }
        else
        {
            await CheckFolderAsync(data.Folders.ToList(), data.Immediately);
            await CheckFilesAsync(data.Files.ToList());
        }
    }

    private async Task CheckVersionsAsync(List<T> files, List<int> versions)
    {
        var fileId = files.FirstOrDefault();
        var file = await fileDao.GetFileAsync(fileId);
        await CheckVersionPermissionsAsync(file, versions, true);
    }

    private async Task CheckFolderAsync(List<T> data, bool immediately)
    {
        foreach (var folderId in data)
        {
            var folder = await folderDao.GetFolderAsync(folderId);
            await CheckFolderPermissionsAsync([folder], immediately, throwException: true);
        }
    }

    private async Task CheckFilesAsync(List<T> data)
    {
        foreach (var fileId in data)
        {
            var file = await fileDao.GetFileAsync(fileId);
            await CheckFilePermissionsAsync([file], false, true, true);
        }
    }

    // Batched analog of CheckFilePermissionsAsync: per-file verdicts with the same error precedence
    // (delete rights -> lock -> editing), but delete rights and lock tags are resolved for the whole
    // set at once instead of one query per file. Missing files are the caller's concern.
    public async Task<Dictionary<T, string>> CheckFilesPermissionsAsync(IReadOnlyCollection<File<T>> files, bool checkPermissions)
    {
        var errors = new Dictionary<T, string>();

        if (files.Count == 0)
        {
            return errors;
        }

        HashSet<T> denied = null;
        HashSet<string> lockedForMe = null;

        if (checkPermissions)
        {
            denied = [];

            await foreach (var (entry, allowed) in security.CanDeleteAsync(files.ToAsyncEnumerable()))
            {
                if (!allowed)
                {
                    denied.Add(entry.Id);
                }
            }

            var userId = authContext.CurrentAccount.ID;
            var tagDao = daoFactory.GetTagDao<T>();

            lockedForMe = await tagDao.GetTagsAsync([TagType.Locked], files)
                .Where(t => t.EntryType == FileEntryType.File && t.EntryId != null && t.Owner != Guid.Empty && t.Owner != userId)
                .Select(t => t.EntryId.ToString())
                .ToHashSetAsync();
        }

        foreach (var file in files)
        {
            if (checkPermissions && denied.Contains(file.Id))
            {
                errors[file.Id] = FilesCommonResource.ErrorMessage_SecurityException_DeleteFile;
                continue;
            }

            if (checkPermissions && lockedForMe.Contains(file.Id.ToString()))
            {
                errors[file.Id] = FilesCommonResource.ErrorMessage_LockedFile;
                continue;
            }

            if (await fileTracker.IsEditingAsync(file.Id, false))
            {
                errors[file.Id] = FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFile;
            }
        }

        return errors;
    }

    /// <summary>
    /// A folder delete is all-or-nothing from the caller's point of view: a single file that is
    /// open for editing must block the whole subtree instead of leaving it half-deleted. This is
    /// check-then-delete, not a transaction — a file opened after the check still wins the race.
    /// </summary>
    public async Task<string> CheckSubtreeFilesPermissionsAsync(Folder<T> folder, bool checkPermissions)
    {
        const int pageSize = 500;
        var offset = 0;

        while (true)
        {
            var page = await fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false,
                    withSubfolders: true, offset: offset, count: pageSize)
                .ToListAsync();

            if (page.Count == 0)
            {
                return null;
            }

            var errors = await CheckFilesPermissionsAsync(page, checkPermissions);
            if (errors.Count > 0)
            {
                return errors.Values.First();
            }

            if (page.Count < pageSize)
            {
                return null;
            }

            offset += page.Count;
        }
    }

    public async Task<string> CheckFilePermissionsAsync(IEnumerable<File<T>> files, bool folder, bool checkPermissions, bool throwException = false)
    {
        foreach (var file in files)
        {
            string errorMsg;

            if (file == null)
            {
                errorMsg = FilesCommonResource.ErrorMessage_FileNotFound;
                return throwException ? throw new FileNotFoundException(errorMsg) : errorMsg;
            }

            if (checkPermissions && !await security.CanDeleteAsync(file))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_DeleteFile;
                return throwException ? throw new SecurityException(errorMsg) : errorMsg;
            }

            if (checkPermissions && await lockerManager.FileLockedForMeAsync(file.Id))
            {
                errorMsg = FilesCommonResource.ErrorMessage_LockedFile;
                return throwException ? throw new SecurityException(errorMsg) : errorMsg;
            }

            if (await fileTracker.IsEditingAsync(file.Id, false))
            {
                errorMsg = folder ?
                    FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFolder :
                    FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFile;

                return throwException ? throw new SecurityException(errorMsg) : errorMsg;
            }
        }

        return null;
    }

    public async Task<string> CheckFolderPermissionsAsync(
        IEnumerable<Folder<T>> folders,
        bool immediately,
        bool checkPermissions = true,
        bool throwException = false)
    {
        foreach (var folder in folders)
        {
            string errorMsg;

            if (folder == null)
            {
                errorMsg = FilesCommonResource.ErrorMessage_FolderNotFound;
                return throwException ? throw new FileNotFoundException(errorMsg) : errorMsg;
            }

            if (!immediately && folder.IsRoom)
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder;
                return throwException ? throw new SecurityException(errorMsg) : errorMsg;
            }

            if (checkPermissions && !await security.CanDeleteAsync(folder))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder;
                return throwException ? throw new SecurityException(errorMsg) : errorMsg;
            }
        }

        return null;
    }

    public async Task<string> CheckVersionPermissionsAsync(File<T> file, IEnumerable<int> versions = null, bool throwException = false)
    {
        string errorMsg;

        if (file == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FileNotFound;
            return throwException ? throw new FileNotFoundException(errorMsg) : errorMsg;
        }

        if (file.RootFolderType is FolderType.Archive or FolderType.TRASH)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
            return throwException ? throw new SecurityException(errorMsg) : errorMsg;
        }

        if (versions != null)
        {
            foreach (var version in versions)
            {
                if (file.Version == version)
                {
                    errorMsg = FilesCommonResource.ErrorMessage_SecurityException_FileVersion;
                    return throwException ? throw new SecurityException(errorMsg) : errorMsg;
                }
            }
        }

        errorMsg = await CheckFilePermissionsAsync([file], false, true, throwException);
        return errorMsg;
    }
}
