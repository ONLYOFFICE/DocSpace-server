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

namespace ASC.Files.Core.Helpers;

/// <summary>
/// What disabling an account means for the private rooms: the API does not disable the owner of one, because only the
/// owner and a member DocSpace admin can hand the room over (an LDAP synchronization still can), and an account that is
/// disabled loses its wrapped file keys, so a later return gets keys only when a member issues them again. Every way of
/// disabling reaches <see cref="RevokeFileKeysOfDisabledAsync"/> through <see cref="UserDisabledIntegrationEvent"/> in
/// the Files worker; the API actions also try it themselves, last, so that the keys are usually gone when they answer.
/// The key pair of the account and its memberships stay. A user made a guest leaves the private rooms altogether: their
/// own rooms go to the recipient of the type change, their memberships and file keys go, and so does their key pair.
/// </summary>
[Scope]
public class PrivateRoomAccountHelper(
    IDaoFactory daoFactory,
    FileSharingAceHelper fileSharingAceHelper,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    EncryptionLoginProvider encryptionLoginProvider,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    ILogger<PrivateRoomAccountHelper> logger)
{
    /// <summary>
    /// <see cref="RevokeFileKeysOfDisabledAsync"/> for an action that has already disabled the account: a failure is
    /// logged and left to the Files worker, which revokes the keys from the event the disabling published, so the
    /// action keeps its answer and its other effects.
    /// </summary>
    public async Task TryRevokeFileKeysOfDisabledAsync(Guid userId)
    {
        try
        {
            await RevokeFileKeysOfDisabledAsync(userId);
        }
        catch (Exception exception)
        {
            logger.WarningRevokeFileKeysOfDisabledFailed(userId, exception);
        }
    }

    /// <summary>
    /// Answers 403 when one of the accounts owns a private room; nothing is changed then. The message names the account
    /// and the number of its private rooms, not the rooms: the caller may not be a member of them.
    /// </summary>
    public async Task DemandNoPrivateRoomOwnedAsync(IEnumerable<Guid> userIds)
    {
        if (daoFactory.GetFolderDao<int>() is not FolderDao folderDao)
        {
            return;
        }

        foreach (var userId in userIds.Distinct())
        {
            var roomIds = await folderDao.GetPrivateRoomIdsByOwnerAsync(userId);
            if (roomIds.Count > 0)
            {
                var userName = (await userManager.GetUsersAsync(userId)).DisplayUserName(false, displayUserSettingsHelper);

                throw new PrivateRoomOwnershipException(string.Format(FilesCommonResource.ErrorMessage_PrivateRoomOwnerCannotBeDisabled, userName, roomIds.Count));
            }
        }
    }

    /// <summary>The private rooms the account owns.</summary>
    public async Task<List<int>> GetOwnedPrivateRoomIdsAsync(Guid userId)
    {
        return daoFactory.GetFolderDao<int>() is FolderDao folderDao ? await folderDao.GetPrivateRoomIdsByOwnerAsync(userId) : [];
    }

    /// <summary>
    /// Answers 403 when the recipient cannot take every private room the account owns: they hold no encryption key, or
    /// no key of an encrypted file of one of the rooms. Made before a type change to guest changes anything.
    /// </summary>
    public async Task DemandPrivateRoomsCanPassToAsync(Guid userId, Guid recipientId)
    {
        var roomIds = await GetOwnedPrivateRoomIdsAsync(userId);
        if (roomIds.Count == 0)
        {
            return;
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var canTake = await encryptionLoginProvider.HasKeysAsync(recipientId);

        foreach (var roomId in roomIds)
        {
            if (!canTake || (await DocSpaceHelper.GetEncryptedFilesWithoutKeyAsync(fileDao, roomId, recipientId)).Count != 0)
            {
                var userName = (await userManager.GetUsersAsync(recipientId)).DisplayUserName(false, displayUserSettingsHelper);

                throw new PrivateRoomOwnershipException(string.Format(FilesCommonResource.ErrorMessage_PrivateRoomsCannotPassTo, userName));
            }
        }
    }

    /// <summary>
    /// Takes an account out of every private room after it was made a guest: its file keys are revoked first (with a
    /// record per room), then its access records go, then its key pair. Its own rooms have been given to the recipient
    /// before this call. Nobody is asked for a right here: the type change is the decision of an administrator.
    /// </summary>
    public async Task LeavePrivateRoomsAsync(Guid userId)
    {
        if (daoFactory.GetFolderDao<int>() is not FolderDao folderDao)
        {
            return;
        }

        var roomIds = await folderDao.GetPrivateRoomIdsWithMemberAsync(userId);

        await RevokeFileKeysOfDisabledAsync(userId);

        foreach (var roomId in roomIds)
        {
            var room = await folderDao.GetFolderAsync(roomId);
            if (room == null)
            {
                continue;
            }

            await fileSecurity.ShareAsync(room.Id, FileEntryType.Folder, userId, FileShare.None);

            try
            {
                await socketManager.DeleteFolder(room, [userId]);
            }
            catch (Exception exception)
            {
                logger.WarningLeavePrivateRoomSocketFailed(userId, room.Id, exception);
            }
        }

        await encryptionLoginProvider.DeleteAllKeysAsync(userId);
    }

    /// <summary>
    /// Deletes every wrapped file key of a disabled account. Each private room it held keys in gets a revocation record
    /// in the audit trail.
    /// </summary>
    public async Task RevokeFileKeysOfDisabledAsync(Guid userId)
    {
        if (daoFactory.GetFolderDao<int>() is not FolderDao folderDao)
        {
            return;
        }

        foreach (var roomId in await folderDao.GetPrivateRoomIdsWithFileKeysOfUserAsync(userId))
        {
            var room = await folderDao.GetFolderAsync(roomId);
            if (room != null)
            {
                await fileSharingAceHelper.RevokeFileKeysAsync(room, [userId]);
            }
        }

        // keys of files that belong to no private room any more
        await daoFactory.GetFileDao<int>().DeleteFileKeysAsync(userId);
    }
}
