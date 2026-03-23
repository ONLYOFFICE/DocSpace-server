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
public class CopyPermissionsCheck<T>(IFileDao<T> fileDao, PermissionCheckStarter<T, int> intPermissionManager, PermissionCheckStarter<T, string> stringPermissionManager)
    : IPermissionsChecker<FileMoveCopyOperationData<T>, T>,  IPermissionsChecker<FileOperationData<T>, T>
{
    public async Task RunPermissionCheckAsync(FileOperationData<T> data)
    {
        var files = data.Files?.ToList() ?? [];
        foreach (var id in files)
        {
            var file = await fileDao.GetFilesAsync([id]).FirstOrDefaultAsync();
            var copyOperationData = new FileMoveCopyOperationData<T>([], [id], data.TenantId, data.UserId, JsonSerializer.SerializeToElement(file.ParentId), true, FileConflictResolveType.Duplicate, false, true, data.Headers, data.SessionSnapshot);

            await RunPermissionCheckAsync(copyOperationData);
        }
    }

    public async Task RunPermissionCheckAsync(FileMoveCopyOperationData<T> data)
    {
        if (!int.TryParse(data.DestFolderId, out var i))
        {
            await stringPermissionManager.CheckCopyDataAsync(data, data.DestFolderId);
        }
        else
        {
            await intPermissionManager.CheckCopyDataAsync(data, i);
        }
    }
}

[Scope(GenericArguments = [typeof(int), typeof(int)])]
[Scope(GenericArguments = [typeof(int), typeof(string)])]
[Scope(GenericArguments = [typeof(string), typeof(int)])]
[Scope(GenericArguments = [typeof(string), typeof(string)])]
public class PermissionCheckStarter<T, TTo>(
    IFileDao<T> fileDao,
    IFolderDao<T> folderDao,
    IFileDao<TTo> ttoFileDao,
    IFolderDao<TTo> ttoFolderDao,
    FileSecurity security,
    LockerManager lockerManager,
    FileTrackerHelper fileTracker,
    FileUtility fileUtility,
    SettingsManager settingsManager,
    UserManager userManager,
    TenantManager tenantManager,
    IQuotaService quotaService,
    Global global,
    VectorizationGlobalSettings vectorizationSettings)
{
    public async Task CheckCopyDataAsync(FileMoveCopyOperationData<T> data, TTo tto)
    {
        var copy = data.Copy;
        var fileIds = data.Files?.ToList() ?? [];
        var folderIds = data.Folders?.ToList() ?? [];

        var toFolder = await ttoFolderDao.GetFolderAsync(tto);

        await CheckGeneralPermissionsAsync(fileIds, folderIds, toFolder, copy, true);

        var resolveType = data.ResolveType;

        foreach (var folderId in folderIds)
        {
            var folder = await folderDao.GetFolderAsync(folderId);
            await CheckFoldersPermissionsAsync(folder, toFolder, copy, resolveType, true);
        }

        foreach (var fileId in fileIds)
        {
            var file = await fileDao.GetFileAsync(fileId);
            await CheckFilesPermissionsAsync(file, toFolder, copy, resolveType, true);
        }
    }

    public async Task<string> CheckGeneralPermissionsAsync(
        List<T> files,
        List<T> folders,
        Folder<TTo> toFolder,
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

        var parentFolders = await ttoFolderDao.GetParentFoldersAsync(toFolder.Id).ToListAsync();

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

                errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
                if (check)
                {
                    throw new SecurityException(errorMsg);
                }

                return errorMsg;
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

            return check ? throw new SecurityException(errorMsg) : errorMsg;
        }

        return null;
    }

    public async Task<string> CheckFoldersPermissionsAsync(
        Folder<T> folder,
        Folder<TTo> toFolder,
        bool copy,
        FileConflictResolveType resolveType,
        bool check = false)
    {
        string errorMsg;

        if (folder == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FolderNotFound;
            if (check)
            {
                throw new ItemNotFoundException(errorMsg);
            }

            return errorMsg;
        }

        var (rId, _, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(folder);
        var parentRoomId = rId.ToString();
        var parentFolders = await ttoFolderDao.GetParentFoldersAsync(toFolder.Id).ToListAsync();

        var isRoom = folder.IsRoom;
        var canMoveOrCopy = (copy && await security.CanCopyAsync(folder)) || (!copy && await security.CanMoveAsync(folder));
        var checkPermissions = !isRoom || !canMoveOrCopy;

        var canUseRoomQuota = true;
        var canUseUserQuota = true;
        long roomQuotaLimit = 0;
        long userQuotaLimit = 0;

        var toFolderRoom = parentFolders.FirstOrDefault(f => f.IsRoom);

        if (!isRoom &&
            toFolderRoom != null &&
            !string.Equals(parentRoomId, toFolderRoom.Id.ToString()))
        {
            TenantEntityQuotaSettings quotaSettings = toFolderRoom.FolderType is FolderType.AiRoom
               ? await settingsManager.LoadAsync<TenantAiAgentQuotaSettings>()
               : await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
            if (quotaSettings.EnableQuota)
            {
                roomQuotaLimit = toFolderRoom.SettingsQuota == TenantEntityQuotaSettings.DefaultQuotaValue ? quotaSettings.DefaultQuota : toFolderRoom.SettingsQuota;
                if (roomQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                {
                    if (roomQuotaLimit - toFolderRoom.Counter < folder.Counter)
                    {
                        canUseRoomQuota = false;
                    }
                }
            }
        }

        if (!isRoom &&
            toFolderRoom == null &&
            int.TryParse(parentRoomId, out var curRId) && curRId != -1 &&
            toFolder.FolderType is FolderType.USER or FolderType.DEFAULT)
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            if (quotaUserSettings.EnableQuota)
            {
                var user = await userManager.GetUsersAsync(toFolder.RootCreateBy);
                var userQuotaData = await settingsManager.LoadAsync<UserQuotaSettings>(user);
                userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota;
                var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenantId, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));
                if (userQuotaLimit != TenantEntityQuotaSettings.NoQuota)
                {
                    if (userQuotaLimit - userUsedSpace < folder.Counter)
                    {
                        canUseUserQuota = false;
                    }
                }
            }
        }

        if (copy && checkPermissions && !canMoveOrCopy)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!copy && checkPermissions && !canMoveOrCopy)
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

            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!isRoom && (toFolder.FolderType == FolderType.VirtualRooms || toFolder.RootFolderType == FolderType.Archive))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (isRoom && toFolder.FolderType != FolderType.VirtualRooms && toFolder.FolderType != FolderType.Archive)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_UnarchiveRoom;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!isRoom && folder.SettingsPrivate && !await CompliesPrivateRoomRulesAsync(copy, folder, parentFolders))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (checkPermissions && folder.RootFolderType != FolderType.TRASH && !await security.CanDownloadAsync(folder))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (folder.RootFolderType == FolderType.Privacy
            && (copy || toFolder.RootFolderType != FolderType.Privacy))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFolder;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!canUseRoomQuota)
        {
            errorMsg = FileSizeComment.GetRoomFreeSpaceException(roomQuotaLimit, toFolderRoom.FolderType is FolderType.AiRoom).Message;
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }

        if (!canUseUserQuota)
        {
            errorMsg = FileSizeComment.GetUserFreeSpaceException(userQuotaLimit).Message;
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }

        if (!check || (Equals(folder.ParentId ?? default, toFolder.Id) && resolveType != FileConflictResolveType.Duplicate))
        {
            return null;
        }

        var conflictFolder = folder.RootFolderType == FolderType.Privacy || isRoom ||
                                     (!Equals(folder.ParentId ?? default, toFolder.Id) && resolveType == FileConflictResolveType.Duplicate)
                    ? null
                    : await ttoFolderDao.GetFolderAsync(folder.Title, toFolder.Id);

        if (copy || conflictFolder != null)
        {
            if (toFolder.ProviderId == folder.ProviderId && folderDao.UseRecursiveOperation(folder.Id, toFolder.Id))
            {
                if (!copy && checkPermissions && !await security.CanMoveAsync(folder))
                {
                    throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_MoveFolder);
                }
            }
            else if (conflictFolder != null && resolveType != FileConflictResolveType.Overwrite && !copy && checkPermissions && !await security.CanMoveAsync(folder))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_MoveFolder);
            }
        }
        else if (checkPermissions && !await security.CanMoveAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_MoveFolder);
        }

        var files = await fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false, withSubfolders: true).ToListAsync();

        errorMsg = await CheckFilesSecurityPermissionsAsync(files, checkPermissions);
        if (errorMsg != null)
        {
            throw new SecurityException(errorMsg);
        }

        return null;
    }

    public async Task<string> CheckFilesPermissionsAsync(
        File<T> file,
        Folder<TTo> toFolder,
        bool copy,
        FileConflictResolveType resolveType,
        bool check = false)
    {
        var parentFolders = await ttoFolderDao.GetParentFoldersAsync(toFolder.Id).ToListAsync();

        string errorMsg = null;

        if (file == null)
        {
            errorMsg = FilesCommonResource.ErrorMessage_FileNotFound;
            if (check)
            {
                throw new FileNotFoundException(errorMsg);
            }

            return errorMsg;
        }

        errorMsg = await CheckFilesSecurityPermissionsAsync([file]);

        if (toFolder.FolderType == FolderType.VirtualRooms || toFolder.RootFolderType == FolderType.Archive)
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (copy && !await security.CanCopyAsync(file))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_CopyFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!copy && !await security.CanMoveAsync(file))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (file.RootFolderType != FolderType.TRASH && !await security.CanDownloadAsync(file))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (!await CompliesPrivateRoomRulesAsync(copy, file, parentFolders))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (file.RootFolderType == FolderType.Privacy
            && (copy || toFolder.RootFolderType != FolderType.Privacy))
        {
            errorMsg = FilesCommonResource.ErrorMessage_SecurityException_MoveFile;
            if (check)
            {
                throw new SecurityException(errorMsg);
            }

            return errorMsg;
        }

        if (global.EnableUploadFilter &&
            !fileUtility.ExtsUploadable.Contains(FileUtility.GetFileExtension(file.Title)))
        {
            errorMsg = FilesCommonResource.ErrorMessage_NotSupportedFormat;
            if (check)
            {
                throw new NotSupportedException(errorMsg);
            }

            return errorMsg;
        }

        if (toFolder.FolderType is FolderType.Knowledge && !vectorizationSettings.IsSupportedContentExtraction(file.Title))
        {
            errorMsg = FilesCommonResource.ErrorMessage_NotSupportedFormat;
            if (check)
            {
                throw new NotSupportedException(errorMsg);
            }

            return errorMsg;
        }

        if (toFolder.FolderType is FolderType.Knowledge &&
            file.ContentLength > vectorizationSettings.MaxContentLength)
        {
            errorMsg = FileSizeComment.GetFileSizeExceptionString(vectorizationSettings.MaxContentLength);
            if (check)
            {
                throw new InvalidOperationException(errorMsg);
            }

            return errorMsg;
        }

        if (!check)
        {
            return errorMsg;
        }

        if (toFolder.RootFolderType == FolderType.VirtualRooms &&
            parentFolders.Any(folder => folder.FolderType == FolderType.FillingFormsRoom) &&
            !file.IsForm)
        {
            throw new InvalidOperationException(copy ? FilesCommonResource.ErrorMessage_UploadToFormRoom : FilesCommonResource.ErrorMessage_MoveToFormRoom);
        }

        var conflict = resolveType == FileConflictResolveType.Duplicate ||
                               file.RootFolderType == FolderType.Privacy ||
                               file.Encrypted ||
                               toFolder.FolderType == FolderType.Knowledge
                    ? null
                    : await ttoFileDao.GetFileAsync(toFolder.Id, file.Title);

        errorMsg = await CheckFilesSecurityPermissionsAsync([file]);

        if (conflict == null || conflict.Category != file.Category)
        {
            if (!copy && errorMsg != null)
            {
                throw new SecurityException(errorMsg);
            }
        }
        else if (resolveType == FileConflictResolveType.Overwrite)
        {
            if (!await security.CanEditAsync(conflict) && !await security.CanFillFormsAsync(conflict))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (await lockerManager.FileLockedForMeAsync(conflict.Id))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
            }

            if (await fileTracker.IsEditingAsync(conflict.Id, false))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_UpdateEditingFile);
            }

            if (!copy && !Equals(file.ParentId.ToString(), toFolder.Id.ToString()) && errorMsg != null)
            {
                throw new SecurityException(errorMsg);
            }
        }

        return null;
    }

    public async Task<string> CheckFilesSecurityPermissionsAsync(IEnumerable<File<T>> files, bool checkPermissions = true)
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

    private async Task<bool> CompliesPrivateRoomRulesAsync(bool copy, FileEntry<T> entry, IEnumerable<Folder<TTo>> toFolderParents)
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
