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
                    Folder<int> entryInt => DocSpaceHelper.IsRoom(entryInt.FolderType) && await fileSecurity.CanReadAsync(entryInt),
                    Folder<string> entryString => DocSpaceHelper.IsRoom(entryString.FolderType) && await fileSecurity.CanReadAsync(entryString),
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
                result.Shared = (entry switch
                {
                    FileEntry<int> entryInt => await IsSharedAsync(entryInt, userId, isDocSpaceAdmin),
                    FileEntry<string> entryString => await IsSharedAsync(entryString, userId, isDocSpaceAdmin),
                    _ => false
                });
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

            if (!string.IsNullOrEmpty(password) && entry is IFolder folder && DocSpaceHelper.IsRoom(folder.FolderType))
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
            info.IsRoom = DocSpaceHelper.IsRoom(((Folder<T>)entry).FolderType);
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

        FileEntry<T> room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(entry.Id).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
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
        
        if (DocSpaceHelper.IsRoom(room.FolderType))
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