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
    MessageService messageService,
    ShareLinkKeyDao shareLinkKeyDao,
    DocumentServiceHelper documentServiceHelper,
    PrivateRoomsGate privateRoomsGate,
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
    /// and the number of its private rooms, not the rooms: the caller may not be a member of them. With
    /// <paramref name="toGuest"/>, the message says that such an owner is made a guest only with a recipient of the rooms.
    /// </summary>
    public async Task DemandNoPrivateRoomOwnedAsync(IEnumerable<Guid> userIds, bool toGuest = false)
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

                var message = toGuest ? FilesCommonResource.ErrorMessage_PrivateRoomOwnerCannotBeGuest : FilesCommonResource.ErrorMessage_PrivateRoomOwnerCannotBeDisabled;

                throw new PrivateRoomOwnershipException(string.Format(message, userName, roomIds.Count));
            }
        }
    }

    /// <summary>The private rooms the account owns.</summary>
    public async Task<List<int>> GetOwnedPrivateRoomIdsAsync(Guid userId)
    {
        return daoFactory.GetFolderDao<int>() is FolderDao folderDao ? await folderDao.GetPrivateRoomIdsByOwnerAsync(userId) : [];
    }

    /// <summary>
    /// Answers 403 when the private rooms the account owns cannot be handed over: the end-to-end encrypted rooms app is
    /// off, a room is archived (a change of owner refuses both), or the recipient holds no encryption key or no key of an
    /// encrypted file of one of the rooms. Made before a type change to guest changes anything.
    /// </summary>
    public async Task DemandPrivateRoomsCanPassToAsync(Guid userId, Guid recipientId)
    {
        var roomIds = await GetOwnedPrivateRoomIdsAsync(userId);
        if (roomIds.Count == 0)
        {
            return;
        }

        await privateRoomsGate.DemandEnabledAsync();

        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();
        var canTake = await encryptionLoginProvider.HasKeysAsync(recipientId);

        foreach (var roomId in roomIds)
        {
            if ((await folderDao.GetFolderAsync(roomId)) is { RootFolderType: FolderType.Archive })
            {
                var ownerName = (await userManager.GetUsersAsync(userId)).DisplayUserName(false, displayUserSettingsHelper);

                throw new PrivateRoomOwnershipException(string.Format(FilesCommonResource.ErrorMessage_PrivateRoomOwnedInArchive, ownerName));
            }

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

            // As a member who leaves: an open editor session of the room's files ends, then the client is told.
            try
            {
                await documentServiceHelper.CheckRoomUsersForDropAsync(room);
                await socketManager.DeleteFolder(room, [userId]);
            }
            catch (Exception exception)
            {
                logger.WarningLeavePrivateRoomSocketFailed(userId, room.Id, exception);
            }
        }

        var stored = await encryptionLoginProvider.GetKeysAsync(userId);
        var keyIds = string.IsNullOrEmpty(stored) ? [] : EncryptionKeyPairDtoHelper.ParseKeyIds(stored, userId);

        await encryptionLoginProvider.DeleteAllKeysAsync(userId);

        // The same record as the deletion of a key through the privacy room routes, once per key and after the deletion;
        // the key pair is gone either way, so a failure here is only logged. The type change runs as a background job,
        // and only the headers overload records outside a request.
        try
        {
            foreach (var keyId in keyIds)
            {
                messageService.SendHeadersMessage(MessageAction.PrivacyRoomKeyDeleted, MessageTarget.Create(keyId), null, keyId.ToString());
            }
        }
        catch (Exception exception)
        {
            logger.WarningAuditGuestKeysDeletedFailed(userId, exception);
        }
    }

    /// <summary>
    /// Deletes every wrapped file key of a disabled account. Each private room it held keys in gets a revocation record
    /// in the audit trail. The wraps of link secrets made for the account go too, in every room, also where it held no
    /// file key: such an account manages no link.
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

        await shareLinkKeyDao.DeleteSecretWrapsOfUserAsync(userId);
    }
}
