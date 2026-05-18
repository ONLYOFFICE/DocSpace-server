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

using Status = ASC.Files.Core.Security.Status;

namespace ASC.Files.Core.Helpers;

[Scope]
public class ExternalLinkHelper(
    ExternalShare externalShare,
    SecurityContext securityContext,
    IDaoFactory daoFactory,
    UserManager userManager,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    SocketManager socketManager,
    GlobalFolderHelper globalFolderHelper)
{
    public async Task<ValidationInfo> ValidateAsync(string key, string password = null, string fileId = null, string folderId = null)
    {
        var result = new ValidationInfo { Status = Status.Invalid, Access = FileShare.Restrict };

        var isAuth = securityContext.IsAuthenticated;
        result.IsAuthenticated = isAuth;

        var data = await externalShare.ParseShareKeyAsync(key);
        var securityDao = daoFactory.GetSecurityDao<string>();

        var record = await securityDao.GetSharesAsync([data.Id]).FirstOrDefaultAsync();
        if (record == null)
        {
            return result;
        }

        var status = await externalShare.ValidateRecordAsync(record, password, isAuth);
        result.Status = status;

        if (status != Status.Ok && status != Status.RequiredPassword)
        {
            return result;
        }

        var entryId = record.EntryId;

        var entry = int.TryParse(entryId, out var id)
            ? await GetEntryAndProcessAsync(id, record.EntryType, result)
            : await GetEntryAndProcessAsync(entryId, record.EntryType, result);

        if (!string.IsNullOrEmpty(fileId))
        {
            if (int.TryParse(fileId, out var entityId))
            {
                await GetSubFileAndProcessAsync(entityId, entryId, result);
            }
            else
            {
                await GetSubFileAndProcessAsync(fileId, entryId, result);
            }
        }
        else if (!string.IsNullOrEmpty(folderId))
        {
            if (int.TryParse(folderId, out var entityId))
            {
                await GetSubFolderAndProcessAsync(entityId, entryId, result);
            }
            else
            {
                await GetSubFolderAndProcessAsync(folderId, entryId, result);
            }
        }

        if (entry == null || entry.RootFolderType is FolderType.TRASH or FolderType.Archive)
        {
            result.Status = Status.Invalid;
            return result;
        }

        if (status == Status.RequiredPassword)
        {
            if (isAuth)
            {
                var canReadWithoutPassword = entry switch
                {
                    Folder<int> entryInt => entryInt.IsRoom && await fileSecurity.CanReadAsync(entryInt),
                    Folder<string> entryString => entryString.IsRoom && await fileSecurity.CanReadAsync(entryString),
                    _ => false
                };

                if (canReadWithoutPassword)
                {
                    result.Status = Status.Ok;
                }
            }

            return result;
        }

        result.Access = record.Share;
        result.TenantId = record.TenantId;
        result.LinkId = data.Id;

        if (isAuth)
        {
            var userId = securityContext.CurrentAccount.ID;
            var isDocSpaceAdmin = await userManager.IsDocSpaceAdminAsync(userId);

            if (entry.CreateBy.Equals(userId))
            {
                result.Shared = true;
            }
            else
            {
                result.Shared = entry switch
                {
                    FileEntry<int> entryInt => await IsSharedAsync(entryInt, userId, isDocSpaceAdmin),
                    FileEntry<string> entryString => await IsSharedAsync(entryString, userId, isDocSpaceAdmin),
                    _ => false
                };
            }

            if (!result.Shared && result.Status == Status.Ok)
            {
                result.Shared = entry switch
                {
                    Folder<int> folderInt => await MarkAsync(folderInt, data.Id, userId),
                    Folder<string> folderString => await MarkAsync(folderString, data.Id, userId),
                    File<int> fileInt => await MarkAsync(fileInt, data.Id, userId),
                    File<string> fileString => await MarkAsync(fileString, data.Id, userId),
                    _ => false
                };
            }

            if (!string.IsNullOrEmpty(password) && entry is IFolder { IsRoom: true })
            {
                switch (entry)
                {
                    case Folder<int> folderInt:
                        await socketManager.UpdateFolderAsync(folderInt, [userId]);
                        break;
                    case Folder<string> folderString:
                        await socketManager.UpdateFolderAsync(folderString, [userId]);
                        break;
                }
            }
        }

        if (isAuth || !string.IsNullOrEmpty(externalShare.GetAnonymousSessionKey()))
        {
            return result;
        }

        await externalShare.SetAnonymousSessionKeyAsync();

        return result;
    }

    private async Task<FileEntry> GetEntryAndProcessAsync<T>(T id, FileEntryType entryType, ValidationInfo info)
    {
        FileEntry<T> entry;
        if (entryType == FileEntryType.Folder)
        {
            entry = await daoFactory.GetFolderDao<T>().GetFolderAsync(id);
            if (entry == null)
            {
                return null;
            }

            info.Id = entry.Id.ToString();
            info.Title = entry.Title;
            info.Type = FileEntryType.Folder;
            info.IsRoom = ((Folder<T>)entry).IsRoom;
        }
        else
        {
            entry = await daoFactory.GetFileDao<T>().GetFileAsync(id);
            if (entry == null)
            {
                return null;
            }

            info.Id = entry.Id.ToString();
            info.Title = entry.Title;
            info.Type = FileEntryType.File;
        }

        FileEntry<T> room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(entry.Id).FirstOrDefaultAsync(f => f.IsRoom);
        if (info.IsAuthenticated)
        {
            info.IsRoomMember = (await daoFactory.GetSecurityDao<T>().GetSharesAsync(room, [securityContext.CurrentAccount.ID])).Select(r => r.EntryId).Any();
        }

        return entry;
    }

    private async Task GetSubFileAndProcessAsync<T>(T id, string rootId, ValidationInfo info)
    {
        var file = await daoFactory.GetFileDao<T>().GetFileAsync(id);
        if (file == null)
        {
            return;
        }

        var parentFolder = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(file.ParentId).FirstOrDefaultAsync(f => f.Id.ToString() == rootId);

        if (parentFolder == null || Equals(parentFolder.Id, null))
        {
            return;
        }

        info.EntityId = file.Id.ToString();
        info.EntityTitle = file.Title;
        info.EntityType = FileEntryType.File;
    }

    private async Task GetSubFolderAndProcessAsync<T>(T id, string rootId, ValidationInfo info)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(id);
        if (folder == null)
        {
            return;
        }

        var parentFolder = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(folder.Id).FirstOrDefaultAsync(f => f.Id.ToString() == rootId);

        if (parentFolder == null || Equals(parentFolder.Id, null))
        {
            return;
        }

        info.EntityId = folder.Id.ToString();
        info.EntityTitle = folder.Title;
        info.EntityType = FileEntryType.Folder;
    }

    private async Task<bool> MarkAsync<T>(Folder<T> room, Guid linkId, Guid userId)
    {
        await fileMarker.MarkAsRecentByLink(room, linkId);

        if (room.IsRoom)
        {
            room.FolderIdDisplay = IdConverter.Convert<T>(await globalFolderHelper.FolderVirtualRoomsAsync);
            await socketManager.CreateFolderAsync(room, [userId]);
        }
        else
        {
            await socketManager.AddToSharedAsync(room, [userId]);
        }

        return true;
    }

    private async Task<bool> MarkAsync<T>(File<T> file, Guid linkId, Guid userId)
    {
        await fileMarker.MarkAsRecentByLink(file, linkId);
        await socketManager.AddToSharedAsync(file, [userId]);

        return true;
    }

    private async Task<bool> IsSharedAsync<T>(FileEntry<T> entry, Guid userId, bool isDocSpaceAdmin)
    {
        var record = await fileSecurity.GetCurrentShareAsync(entry, userId, isDocSpaceAdmin);
        return record != null && record.Share != FileShare.Restrict && !record.IsLink;
    }
}