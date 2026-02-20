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
public class CopyPermissionsCheck(FileSecurity security, LockerManager lockerManager, FileTrackerHelper fileTracker, FileUtility fileUtility)
{
    public async Task<string> CheckJobPermissionsAsync<T, TTo>(
        IFileDao<T> fileDao, 
        IFolderDao<T> folderDao,
        List<Folder<TTo>> parentFolders,
        Folder<TTo> toFolder,
        List<T> files,
        List<T> folders,
        bool copy,
        bool check = false)
    {
        string errorMsg = null;

        if (toFolder == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FolderNotFound;
            if (check)
            {
                throw new ItemNotFoundException(errorMsg);
            }

            return errorMsg;
        }
 
        if (toFolder.FolderType != FolderType.VirtualRooms && toFolder.FolderType != FolderType.Archive && !await security.CanCreateAsync(toFolder))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_Create;
            if (check)
            {
                throw new SecurityException(errorMsg); 
            }

            return errorMsg;
        }

        if (parentFolders.Exists(parent => folders.Exists(r => r.ToString() == parent.Id.ToString())))
        {
            errorMsg = FilesCommonResource.ErrorMessage_FolderCopyError;
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }

        if (!copy && parentFolders.Exists(parent => parent.FolderType == FolderType.FillingFormsRoom))
        {
            var fromRoomId = default(T);
            var toRoom = parentFolders.FirstOrDefault(parent => parent.FolderType == FolderType.FillingFormsRoom);

            FileEntry<T> fileEntry = folders.Count > 0 ? await folderDao.GetFolderAsync(folders.FirstOrDefault()) :
                files.Count > 1 ? await fileDao.GetFileAsync(files.FirstOrDefault()) : null;

            if (fileEntry != null)
            {
                (fromRoomId, _, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(fileEntry);
            }

            if (int.TryParse(fromRoomId?.ToString(), out var frId) &&
                int.TryParse(toRoom.Id.ToString(), out var trId) &&
                trId != frId)
            {
                if (folders.Count > 0)
                {
                    errorMsg = FilesCommonResource.ErrorMessage_FolderMoveFormFillingError;
                    if (check)
                    {
                        throw new InvalidOperationException(errorMsg);
                    }

                    return errorMsg;
                }

                if (files.Count > 1)
                {
                    errorMsg = FilesCommonResource.ErrorMessage_FilesMoveFormFillingError;
                    if (check)
                    {
                        throw new InvalidOperationException(errorMsg);
                    }

                    return errorMsg;
                }
            }
        }

        var isRoom = false;

        if (0 < folders.Count)
        {
            var firstFolder = await folderDao.GetFolderAsync(folders[0]);
            isRoom = firstFolder.IsRoom;

            if (copy && !await security.CanCopyAsync(firstFolder))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyFolder;
                if (check)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }

            if (!copy && !await security.CanMoveAsync(firstFolder))
            {
                if (isRoom)
                {
                    errorMsg = toFolder.FolderType == FolderType.Archive
                        ? FilesCommonResource.ErrorMessage_SecurityException_ArchiveRoom
                        : FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;

                    if (check)
                    {
                        throw new SecurityException(errorMsg);
                    }

                    return errorMsg;

                }
                else
                {
                    errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                    if (check)
                    {
                        throw new SecurityException(errorMsg);
                    }

                    return errorMsg;
                }
            }
        }

        if (0 < files.Count)
        {
            var firstFile = await fileDao.GetFileAsync(files[0]);

            if (copy && !await security.CanCopyAsync(firstFile))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyFile;
                if (check)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }

            if (!copy && !await security.CanMoveAsync(firstFile))
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
                if (check)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }
        }

        if (copy && !(isRoom && toFolder.FolderType == FolderType.VirtualRooms) && !await security.CanCopyToAsync(toFolder))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyToFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!copy && !await security.CanMoveToAsync(toFolder))
        {
            errorMsg = toFolder.FolderType switch
            {
                FolderType.VirtualRooms => FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom,
                FolderType.Archive => FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom,
                _ => FilesCommonResource.ErrorMessage_SecurityException_MoveToFolder
            };

            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        return errorMsg;
    }

    public async Task<string> CheckFoldersPermissionsAsync<T, TTo>(
        Folder<T> folder, 
        bool copy, 
        bool checkPermissions,
        bool canMoveOrCopy,
        bool isRoom,
        Folder<TTo> toFolder,
        List<Folder<TTo>> toFolderParents,
        bool canUseRoomQuota,
        bool canUseUserQuota,
        long roomQuotaLimit,
        long userQuotaLimit,
        TTo toFolderId,
        Folder<TTo> toFolderRoom,
        FileConflictResolveType resolveType,
        IFileDao<T> fileDao,
        IFolderDao<TTo> ttoFolderDao,
        IFolderDao<T> folderDao,
        bool check = false) 
    {
        string errorMsg = null;

        if (folder == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FolderNotFound;
            if (check)
            {
                throw new ItemNotFoundException(errorMsg);
            }

            return errorMsg;
        }
        else if (copy && checkPermissions && !canMoveOrCopy)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (!copy && checkPermissions && !canMoveOrCopy)
        {
            if (isRoom)
            {
                errorMsg = toFolder.FolderType == FolderType.Archive
                    ? FilesCommonResource.ErrorMessage_SecurityException_ArchiveRoom
                    : FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;

                if (check)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }
            else
            {
                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                if (check)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
            }
        }
        else if (!isRoom && (toFolder.FolderType == FolderType.VirtualRooms || toFolder.RootFolderType == FolderType.Archive))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (isRoom && toFolder.FolderType != FolderType.VirtualRooms && toFolder.FolderType != FolderType.Archive)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (!isRoom && folder.SettingsPrivate && !await CompliesPrivateRoomRulesAsync(folderDao, copy, folder, toFolderParents))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (checkPermissions && folder.RootFolderType != FolderType.TRASH && !await security.CanDownloadAsync(folder))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (folder.RootFolderType == FolderType.Privacy
                 && (copy || toFolder.RootFolderType != FolderType.Privacy))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (!canUseRoomQuota)
        {
            errorMsg = FileSizeComment.GetRoomFreeSpaceException(roomQuotaLimit, toFolderRoom.FolderType is FolderType.AiRoom).Message;
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }
        else if (!canUseUserQuota)
        {
            errorMsg = FileSizeComment.GetUserFreeSpaceException(userQuotaLimit).Message;
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }

        if (check)
        {
            if (!Equals(folder.ParentId ?? default, toFolderId) || resolveType == FileConflictResolveType.Duplicate)
            {
                var files = await fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();

                errorMsg = await WithErrorAsync(files, checkPermissions);

                var conflictFolder = folder.RootFolderType == FolderType.Privacy || isRoom ||
                                             (!Equals(folder.ParentId ?? default, toFolderId) && resolveType == FileConflictResolveType.Duplicate)
                            ? null
                            : await ttoFolderDao.GetFolderAsync(folder.Title, toFolderId);



                if (copy || conflictFolder != null)
                {
                    if (toFolder.ProviderId == folder.ProviderId && folderDao.UseRecursiveOperation(folder.Id, toFolderId))
                    {
                        if (!copy)
                        {
                            if (checkPermissions && !await security.CanMoveAsync(folder))
                            {
                                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                                if (check)
                                {
                                    throw new SecurityException(errorMsg);
                                }

                                return errorMsg;
                            }
                        }
                    }
                    else
                    {
                        if (conflictFolder != null)
                        {
                            if (resolveType != FileConflictResolveType.Overwrite && !copy)
                            {
                                if (checkPermissions && !await security.CanMoveAsync(folder))
                                {
                                    errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                                    if (check)
                                    {
                                        throw new SecurityException(errorMsg);
                                    }

                                    return errorMsg;
                                }
                                else if (errorMsg != null)
                                {
                                    return errorMsg;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (checkPermissions && !await security.CanMoveAsync(folder))
                    {
                        errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                        if (check)
                        {
                            throw new SecurityException(errorMsg);
                        }

                        return errorMsg;
                    }
                    else if (errorMsg != null)
                    {
                        return errorMsg;
                    }
                }
            }
        }

        return errorMsg;
    }
        
    public async Task<string> CheckFilesPermissionsAsync<T, TTo>(
        File<T> file,
        bool checkPermissions,
        Folder<TTo> toFolder,
        bool copy,
        bool enableUploadFilter,
        IFolderDao<T> folderDao,
        VectorizationGlobalSettings vectorizationSettings,
        List<Folder<TTo>> toParentFolders,
        FileConflictResolveType resolveType,
        IFileDao<TTo> ttoFileDao,
        TTo toFolderId,
        bool check = false)
    {
        string errorMsg = null;

        errorMsg = await WithErrorAsync([file], checkPermissions);

        if (file == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FileNotFound;
            if (check)
            {
                throw new FileNotFoundException(errorMsg);
            }

            return errorMsg;
        }
        else if (toFolder.FolderType == FolderType.VirtualRooms || toFolder.RootFolderType == FolderType.Archive)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (copy && !await security.CanCopyAsync(file))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (!copy && checkPermissions && !await security.CanMoveAsync(file))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (checkPermissions && file.RootFolderType != FolderType.TRASH && !await security.CanDownloadAsync(file))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (!await CompliesPrivateRoomRulesAsync(folderDao, copy, file, toParentFolders))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (file.RootFolderType == FolderType.Privacy
                 && (copy || toFolder.RootFolderType != FolderType.Privacy))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }
        else if (enableUploadFilter &&
                !fileUtility.ExtsUploadable.Contains(FileUtility.GetFileExtension(file.Title)))
        {
            errorMsg = FilesCommonResource.ErrorMessage_NotSupportedFormat;
            if (check)
            {
                throw new NotSupportedException(errorMsg);
            }

            return errorMsg;
        }
        else if (toFolder.FolderType is FolderType.Knowledge && !vectorizationSettings.IsSupportedContentExtraction(file.Title))
        {
            errorMsg = FilesCommonResource.ErrorMessage_NotSupportedFormat;
            if (check)
            {
                throw new NotSupportedException(errorMsg);
            }

            return errorMsg;
        }
        else if (toFolder.FolderType is FolderType.Knowledge &&
                 file.ContentLength > vectorizationSettings.MaxContentLength)
        {
            errorMsg = FileSizeComment.GetFileSizeExceptionString(vectorizationSettings.MaxContentLength);
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }

        if (check)
        {
            if (toFolder.RootFolderType == FolderType.VirtualRooms)
            {
                if (toParentFolders.Any(folder => folder.FolderType == FolderType.FillingFormsRoom) && !file.IsForm)
                {
                    errorMsg = copy ? FilesCommonResource.ErrorMessage_UploadToFormRoom : FilesCommonResource.ErrorMessage_MoveToFormRoom;
                    if (check)
                    {
                        throw new InvalidOperationException(errorMsg);
                    }

                    return errorMsg;
                }
            }

            var conflict = resolveType == FileConflictResolveType.Duplicate ||
                                   file.RootFolderType == FolderType.Privacy ||
                                   file.Encrypted ||
                                   toFolder.FolderType == FolderType.Knowledge
                        ? null
                        : await ttoFileDao.GetFileAsync(toFolderId, file.Title);

            if (conflict == null || conflict.Category != file.Category)
            {
                if (!copy)
                {
                    if (errorMsg != null)
                    {
                        if (check)
                        {
                            throw new InvalidOperationException(errorMsg);
                        }

                        return errorMsg;
                    }
                }
            }
            else
            {
                if (resolveType == FileConflictResolveType.Overwrite)
                {
                    if (checkPermissions && !await security.CanEditAsync(conflict) && !await security.CanFillFormsAsync(conflict))
                    {
                        errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
                        if (check)
                        {
                            throw new InvalidOperationException(errorMsg);
                        }

                        return errorMsg;
                    }
                    else if (await lockerManager.FileLockedForMeAsync(conflict.Id))
                    {
                        errorMsg = FilesCommonResource.ErrorMessage_LockedFile;
                        if (check)
                        {
                            throw new InvalidOperationException(errorMsg);
                        }

                        return errorMsg;
                    }
                    else if (await fileTracker.IsEditingAsync(conflict.Id, false))
                    {
                        errorMsg = FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile;
                        if (check)
                        {
                            throw new SecurityException(errorMsg);
                        }

                        return errorMsg;
                    }
                    else
                    {
                        if (!copy)
                        {
                            if (!Equals(file.ParentId.ToString(), toFolderId.ToString()))
                            {
                                if (errorMsg != null)
                                {
                                    if (check)
                                    {
                                        throw new InvalidOperationException(errorMsg);
                                    }

                                    return errorMsg;
                                }
                            }
                        }
                    }
                }
            }
        }

        return errorMsg;
    }

    public async Task<string> WithErrorAsync<T>(IEnumerable<File<T>> files, bool checkPermissions = true)
    {
        foreach (var file in files)
        {
            if (checkPermissions && !await security.CanMoveAsync(file))
            {
                return FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            }

            if (checkPermissions && await lockerManager.FileLockedForMeAsync(file.Id))
            {
                return FilesCommonResource.ErrorMessage_LockedFile;
            }

            if (await fileTracker.IsEditingAsync(file.Id, false))
            {
                return FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile;
            }
        }

        return null;
    }

    private async Task<bool> CompliesPrivateRoomRulesAsync<T, TTo>(IFolderDao<T> folderDao, bool copy, FileEntry<T> entry, IEnumerable<Folder<TTo>> toFolderParents)
    {
        var entryParentRoom = await folderDao.GetParentFoldersAsync(entry.ParentId).FirstOrDefaultAsync(f => f.SettingsPrivate && f.IsRoom);

        var toFolderParentRoom = toFolderParents.FirstOrDefault(f => f.SettingsPrivate && f.IsRoom);

        if (entryParentRoom == null)
        {
            if (toFolderParentRoom == null)
            {
                return true;
            }

            return false;
        }

        if (toFolderParentRoom == null)
        {
            return false;
        }

        return entryParentRoom.Id.Equals(toFolderParentRoom.Id) && !copy;
    }
}
