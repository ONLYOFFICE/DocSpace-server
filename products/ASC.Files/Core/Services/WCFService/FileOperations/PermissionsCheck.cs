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

[Scope]
public class PermissionsCheck(LockerManager lockerManager, FileTrackerHelper fileTracker, FileSecurity security)
{
    public async Task<string> CheckFilePermissionsAsync<T>(IEnumerable<File<T>> files, bool folder, bool checkPermissions, bool throwExeption = false)
    {
        string errorMsg = null;

        foreach (var file in files)
        {
            if (file == null)
            {
                errorMsg = FilesCommonResource.ErrorMessage_FileNotFound;

                if (throwExeption)
                {
                    throw new FileNotFoundException(errorMsg);
                }

                return errorMsg;
            }

            if (checkPermissions && !await security.CanDeleteAsync(file))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_DeleteFile;

                if (throwExeption)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }

            if (checkPermissions && await lockerManager.FileLockedForMeAsync(file.Id))
            {
                errorMsg = FilesCommonResource.ErrorMessage_LockedFile;

                if (throwExeption)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }

            if (await fileTracker.IsEditingAsync(file.Id, false))
            {
                errorMsg = folder ?
                    FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFolder :
                    FilesCommonResource.ErrorMessage_SecurityException_DeleteEditingFile;

                if (throwExeption)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }
        }
        
        return null;
    }

    public async Task<string> CheckFolderPermissionsAsync<T>(IEnumerable<Folder<T>> folders, bool immediately, bool ignoreException, bool throwException = false)
    {
        string errorMsg = null;
        foreach (var folder in folders)
        {
            if (folder == null)
            {
                errorMsg = FilesCommonResource.ErrorMessage_FolderNotFound;

                if (throwException)
                {
                    throw new FileNotFoundException(errorMsg);
                }

                return errorMsg;
            }

            var canDelete = await security.CanDeleteAsync(folder);
            var checkPermissions = !folder.IsRoom || !canDelete;
            if ((immediately && folder.IsRoom) || (ignoreException && checkPermissions && !canDelete))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder;

                if (throwException)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }
        }
        
        return null;
    }

    public async Task<string> CheckVersionPermissionsAsync<T>(File<T> file, IEnumerable<int> versions = null, bool throwException = false)
    {
        string errorMsg = null;

        if (file == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FileNotFound;
            if (throwException)
            {
                throw new FileNotFoundException(errorMsg);
            }

            return errorMsg;
        }

        if (file.RootFolderType is FolderType.Archive or FolderType.TRASH)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
            if (throwException)
            {
                throw new SecurityException(errorMsg); 
            }

            return errorMsg;
        }

        if (versions != null)
        {
            foreach (var version in versions)
            {
                if (file.Version == version)
                {
                    errorMsg = FilesCommonResource.ErrorMessage_SecurityException_FileVersion;
                    if (throwException)
                    {
                        throw new SecurityException(errorMsg);
                    }

                    return errorMsg;
                }
            }
        }
        
        errorMsg = await CheckFilePermissionsAsync([file], false, true, throwException);
        if (errorMsg != null)
        {
            return errorMsg;
        }

        return null;
    }
}