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
/// Provides functionality for reassigning various resources and data from one user to another within the system or for deleting user-specific data when necessary.
/// </summary>
[Scope]
public class ReassignService(
    Global global,
    UserManager userManager,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    FileMarker fileMarker,
    TenantManager tenantManager,
    ICacheNotify<ClearMyFolderItem> notifyMyFolder,
    ILogger<ReassignService> logger,
    GlobalFolderHelper globalFolderHelper,
    SecurityContext securityContext,
    FolderService folderService,
    SharingService sharingService,
    RecentService recentService)
{
    /// <summary>
    /// Reassigns providers associated with a user to another user, optionally checking permissions before performing the operation.
    /// </summary>
    /// <param name="userFromId">The unique identifier of the user whose providers are being reassigned.</param>
    /// <param name="userToId">The unique identifier of the target user to whom the providers are reassigned.</param>
    /// <param name="checkPermission">Indicates whether to verify permissions before executing the reassignment.</param>
    /// <returns>A task that represents the asynchronous operation of reassigning providers.</returns>
    public async Task ReassignProvidersAsync(Guid userFromId, Guid userToId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return;
        }

        //move thirdParty storage userFrom
        await foreach (var commonProviderInfo in providerDao.GetProvidersInfoAsync(userFromId))
        {
            logger.InformationReassignProvider(commonProviderInfo.ProviderId, userFromId, userToId);
            await providerDao.UpdateProviderInfoAsync(commonProviderInfo.ProviderId, null, null, FolderType.DEFAULT, userToId);
        }
    }

    /// <summary>
    /// Reassigns the ownership of folders associated with virtual rooms for a specified user, optionally checking permissions before performing the operation.
    /// </summary>
    /// <param name="userFromId">The unique identifier of the user whose ownership of room folders is being reassigned.</param>
    /// <param name="checkPermission">Indicates whether to verify permissions before performing the reassignment.</param>
    /// <returns>A task that represents the asynchronous operation of reassigning room folders.</returns>
    public async Task ReassignRoomsFoldersAsync(Guid userFromId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        if (daoFactory.GetFolderDao<int>() is not FolderDao folderDao)
        {
            return;
        }

        await folderDao.ReassignRoomFoldersAsync(userFromId);

        var folderIdVirtualRooms = await folderDao.GetFolderIDVirtualRooms(false);
        var folderVirtualRooms = await folderDao.GetFolderAsync(folderIdVirtualRooms);

        await fileMarker.RemoveMarkAsNewAsync(folderVirtualRooms, userFromId);
    }

    /// <summary>
    /// Reassigns ownership of folders from one user to another, with options to exclude specific folders and verify permissions before the operation.
    /// </summary>
    /// <typeparam name="T">The type of the folder identifier.</typeparam>
    /// <param name="userFromId">The unique identifier of the user from whom the folders are being reassigned.</param>
    /// <param name="userToId">The unique identifier of the user to whom the folders are reassigned.</param>
    /// <param name="exceptFolderIds">A collection of folder identifiers that should be excluded from the reassignment process.</param>
    /// <param name="checkPermission">Indicates whether to verify permissions before executing the reassignment.</param>
    /// <returns>A task representing the asynchronous operation of reassigning folders.</returns>
    public async Task ReassignFoldersAsync<T>(Guid userFromId, Guid userToId, IEnumerable<T> exceptFolderIds, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        if (folderDao == null)
        {
            return;
        }

        logger.InformationReassignFolders(userFromId, userToId);

        await folderDao.ReassignFoldersAsync(userFromId, userToId, exceptFolderIds);

        var folderIdVirtualRooms = await folderDao.GetFolderIDVirtualRooms(false);
        var folderVirtualRooms = await folderDao.GetFolderAsync(folderIdVirtualRooms);

        await fileMarker.RemoveMarkAsNewAsync(folderVirtualRooms, userFromId);
    }

    /// <summary>
    /// Reassigns files associated with a user to another user, excluding specified files or folders, optionally checking permissions before performing the operation.
    /// </summary>
    /// <param name="userFromId">The unique identifier of the user whose files are being reassigned.</param>
    /// <param name="userToId">The unique identifier of the target user to whom the files are reassigned.</param>
    /// <param name="exceptFolderIds">A collection of folder identifiers that should be excluded from the reassignment.</param>
    /// <param name="checkPermission">Indicates whether to verify permissions before executing the reassignment.</param>
    /// <typeparam name="T">The type of the folder identifiers.</typeparam>
    /// <returns>A task representing the asynchronous operation of reassigning files.</returns>
    public async Task ReassignFilesAsync<T>(Guid userFromId, Guid userToId, IEnumerable<T> exceptFolderIds, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToReassignDataAsync(userFromId, userToId);
        }

        var fileDao = daoFactory.GetFileDao<T>();
        if (fileDao == null)
        {
            return;
        }

        logger.InformationReassignFiles(userFromId, userToId);

        await fileDao.ReassignFilesAsync(userFromId, userToId, exceptFolderIds);
    }

    /// <summary>
    /// Reassigns room files associated with a specific user to another user, optionally checking permissions before performing the operation.
    /// </summary>
    /// <param name="userFromId">The unique identifier of the user whose room files are being reassigned.</param>
    /// <param name="checkPermission">Indicates whether to verify permissions before executing the reassignment.</param>
    /// <returns>A task that represents the asynchronous operation of reassigning room files.</returns>
    public async Task ReassignRoomsFilesAsync(Guid userFromId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        if (daoFactory.GetFileDao<int>() is not FileDao fileDao)
        {
            return;
        }

        await fileDao.ReassignRoomsFilesAsync(userFromId);
    }
    
    public async Task DemandPermissionToReassignDataAsync(Guid userFromId, Guid userToId)
    {
        await DemandPermissionToDeletePersonalDataAsync(userFromId);

        //check exist userTo
        var userTo = await userManager.GetUsersAsync(userToId);
        if (Equals(userTo, Constants.LostUser))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UserNotFound);
        }

        //check user can have personal data
        if (await userManager.IsGuestAsync(userTo))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }
    }
    
    public async Task DemandPermissionToDeletePersonalDataAsync(UserInfo userFrom)
    {
        //check current user have access
        if (!await global.IsDocSpaceAdministratorAsync)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        //check exist userFrom
        if (Equals(userFrom, Constants.LostUser))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UserNotFound);
        }
    }
    
    public async Task DeletePersonalDataAsync(Guid userFromId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userFromId);
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var linkDao = daoFactory.GetLinkDao<int>();

        if (folderDao == null || fileDao == null || linkDao == null)
        {
            return;
        }

        logger.InformationDeletePersonalData(userFromId);

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userFromId);
        var folderIdTrash = await folderDao.GetFolderIDTrashAsync(false, userFromId);

        if (!Equals(folderIdMy, 0))
        {
            var fileIdsFromMy = await fileDao.GetFilesAsync(folderIdMy).ToListAsync();
            var folderIdsFromMy = await folderDao.GetFoldersAsync(folderIdMy).ToListAsync();

            await DeleteFilesAsync(fileIdsFromMy, folderIdTrash);
            await DeleteFoldersAsync(folderIdsFromMy, folderIdTrash);

            await folderDao.DeleteFolderAsync(folderIdMy);
        }

        if (!Equals(folderIdTrash, 0))
        {
            var fileIdsFromTrash = await fileDao.GetFilesAsync(folderIdTrash).ToListAsync();
            var folderIdsFromTrash = await folderDao.GetFoldersAsync(folderIdTrash).ToListAsync();

            await DeleteFilesAsync(fileIdsFromTrash, folderIdTrash);
            await DeleteFoldersAsync(folderIdsFromTrash, folderIdTrash);

            await folderDao.DeleteFolderAsync(folderIdTrash);
        }

        await fileSecurity.RemoveSubjectAsync(userFromId, true);
    }

    public async Task UpdatePersonalFolderModified(Guid userId)
    {
        await DemandPermissionToDeletePersonalDataAsync(userId);

        var folderDao = daoFactory.GetFolderDao<int>();

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userId);
        if (folderIdMy == 0)
        {
            return;
        }

        var my = await folderDao.GetFolderAsync(folderIdMy);
        await folderDao.SaveFolderAsync(my);
    }

    public async Task DeletePersonalFolderAsync(Guid userId, bool checkPermission = false)
    {
        if (checkPermission)
        {
            await DemandPermissionToDeletePersonalDataAsync(userId);
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var linkDao = daoFactory.GetLinkDao<int>();

        if (folderDao == null || fileDao == null || linkDao == null)
        {
            return;
        }

        logger.InformationDeletePersonalData(userId);

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userId);
        var my = await folderDao.GetFolderAsync(folderIdMy);
        var folderIdTrash = await folderDao.GetFolderIDTrashAsync(false, userId);

        if (!Equals(folderIdMy, 0))
        {
            var fileIdsFromMy = await fileDao.GetFilesAsync(folderIdMy).ToListAsync();
            var folderIdsFromMy = await folderDao.GetFoldersAsync(folderIdMy).ToListAsync();

            await DeleteFilesAsync(fileIdsFromMy, folderIdTrash);
            await DeleteFoldersAsync(folderIdsFromMy, folderIdTrash);

            await socketManager.DeleteFolder(my, action: async () => await folderDao.DeleteFolderAsync(folderIdMy));

            var cacheKey = $"my/{tenantManager.GetCurrentTenantId()}/{userId}";
            await notifyMyFolder.PublishAsync(new ClearMyFolderItem { Key = cacheKey }, CacheNotifyAction.Remove);
        }
    }
    
    public async Task ReassignRoomsAsync(Guid user, Guid? reassign)
    {
        var rooms = (await folderService.GetFolderItemsAsync(
            await globalFolderHelper.GetFolderVirtualRooms(),
            0,
            -1,
            new List<FilterType>() { FilterType.FoldersOnly },
            false,
            user.ToString(),
            "",
            [],
            false,
            false,
            null)).Entries;

        var ids = rooms.Where(r => r is Folder<int>).Select(e => ((Folder<int>)e).Id);
        var thirdIds = rooms.Where(r => r is Folder<string>).Select(e => ((Folder<string>)e).Id);

        await sharingService.ChangeOwnerAsync(ids, [], reassign ?? securityContext.CurrentAccount.ID, FileShare.ContentCreator).ToListAsync();
        await sharingService.ChangeOwnerAsync(thirdIds, [], reassign ?? securityContext.CurrentAccount.ID, FileShare.ContentCreator).ToListAsync();
    }
    
        public async Task<List<T>> GetPersonalFolderIdsAsync<T>(Guid userId)
    {
        var result = new List<T>();

        var folderDao = daoFactory.GetFolderDao<T>();
        if (folderDao == null)
        {
            return result;
        }

        var folderIdMy = await folderDao.GetFolderIDUserAsync(false, userId);
        if (!Equals(folderIdMy, 0))
        {
            result.Add(folderIdMy);
        }

        var folderIdTrash = await folderDao.GetFolderIDTrashAsync(false, userId);
        if (!Equals(folderIdTrash, 0))
        {
            result.Add(folderIdTrash);
        }

        return result;
    }
    
    public async Task<IEnumerable<FileEntry>> GetSharedFilesAsync(Guid user)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        var my = await folderDao.GetFolderIDUserAsync(false, user);
        if (my == 0)
        {
            return [];
        }
        var shared = await fileDao.GetFilesAsync(my, null, default, false, Guid.Empty, string.Empty, null, false, true, withShared: true).Where(q => q.Shared).ToListAsync();

        return shared;
    }

    public async Task MoveSharedFilesAsync(Guid user, Guid toUser)
    {
        var initUser = securityContext.CurrentAccount.ID;

        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        var my = await folderDao.GetFolderIDUserAsync(false, user);
        if (my == 0)
        {
            return;
        }

        var shared = await fileDao.GetFilesAsync(my, null, default, false, Guid.Empty, string.Empty, null, false, true, withShared: true).Where(q => q.Shared).ToListAsync();

        await securityContext.AuthenticateMeWithoutCookieAsync(toUser);
        if (shared.Count > 0)
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(toUser);
            var userInfo = await userManager.GetUsersAsync(user, false);
            var folder = await folderService.CreateFolderAsync(await globalFolderHelper.FolderMyAsync, $"Documents of user {userInfo.FirstName} {userInfo.LastName}");
            foreach (var file in shared)
            {
                await socketManager.DeleteFileAsync(file, action: async () => await fileDao.MoveFileAsync(file.Id, folder.Id));
                await socketManager.CreateFileAsync(file);
            }
            var ids = shared.Select(s => s.Id).ToList();
            await recentService.DeleteFromRecentAsync([], ids, true);
            await fileDao.ReassignFilesAsync(toUser, ids);
        }

        await securityContext.AuthenticateMeWithoutCookieAsync(initUser);
    }
    
    private async Task DemandPermissionToDeletePersonalDataAsync(Guid userFromId)
    {
        var userFrom = await userManager.GetUsersAsync(userFromId);

        await DemandPermissionToDeletePersonalDataAsync(userFrom);
    }
    
    private async Task DeleteFilesAsync<T>(IEnumerable<T> fileIds, T folderIdTrash)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var linkDao = daoFactory.GetLinkDao<T>();

        foreach (var fileId in fileIds)
        {
            var file = await fileDao.GetFileAsync(fileId);

            await fileMarker.RemoveMarkAsNewForAllAsync(file);

            await socketManager.DeleteFileAsync(file, action: async () => await fileDao.DeleteFileAsync(file.Id, file.GetFileQuotaOwner()));

            if (file.RootFolderType == FolderType.TRASH && !Equals(folderIdTrash, 0))
            {
                await folderDao.ChangeTreeFolderSizeAsync(folderIdTrash, (-1) * file.ContentLength);
            }

            await linkDao.DeleteAllLinkAsync(file.Id);

            await fileDao.SaveProperties(file.Id, null);
        }
    }

    private async Task DeleteFoldersAsync<T>(IEnumerable<Folder<T>> folders, T folderIdTrash)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        foreach (var folder in folders)
        {
            await fileMarker.RemoveMarkAsNewForAllAsync(folder);

            var files = await fileDao.GetFilesAsync(folder.Id).ToListAsync();
            await DeleteFilesAsync(files, folderIdTrash);

            var subfolders = await folderDao.GetFoldersAsync(folder.Id).ToListAsync();
            await DeleteFoldersAsync(subfolders, folderIdTrash);

            if (await folderDao.IsEmptyAsync(folder.Id))
            {
                await socketManager.DeleteFolder(folder, action: async () => await folderDao.DeleteFolderAsync(folder.Id));
            }
        }
    }
}